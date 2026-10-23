"""PICO-4T physical validation: ONE bounded DIGITAL-EDGE-TRIGGERED capture.

Proves that a real D1 falling edge triggers the acquisition and is positioned
correctly. Exercises the exact Layer C ``ScopeService`` path:

    scope_info(backend="real")       -> device identity (non-owning discovery)
    capture_block(AcquisitionProfile) -> ONE bounded digital-triggered capture
    get_capture_metadata(capture_id)  -> compact sidecar metadata
    export_capture(capture_id)        -> validated artifact references

Real hardware is gated by ``PICOSCOPE_MCP_ALLOW_REAL=1``, set in THIS subprocess's
environment only (process-scoped, never persisted to ``opencode.json``).

Acquisition (directive section 10 preferred configuration):
  * Digital port 0 (D0..D7), logic threshold 1500 mV.
  * D1 = PIC32 UART TX (RPC2, logic side) — trigger source.
  * Trigger: D1 FALLING edge (a UART start bit is a high->low transition).
  * pre = 1 ms (62500 samples), post = 3 ms (187500 samples) at 16 ns -> 250000
    samples / ~4 ms total.
  * Bounded trigger wait (trigger_wait_ms=5000); no auto-trigger, no fallback.

No AWG / signal generation, no flash / reset / motion / Siebwalde state, no
simulator fallback. Exactly ONE acquisition; on any real-backend failure the
original exception is preserved and the script STOPS without retry. The trigger
must fire from NORMAL UART traffic only — the script never commands the PIC32.

Usage:
    python pico4t_digital_trigger_capture.py [backend]   # "real" (default) | "simulator"
"""

from __future__ import annotations

import hashlib
import json
import os
import subprocess
import sys
from datetime import datetime, timezone

_REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
if _REPO_ROOT not in sys.path:
    sys.path.insert(0, _REPO_ROOT)

import numpy as np  # noqa: E402

from tools.picoscope.mcp.config import ServerConfig  # noqa: E402
from tools.picoscope.mcp.schema import (  # noqa: E402
    AcquisitionProfile,
    DigitalPortProfile,
    TriggerProfile,
)
from tools.picoscope.mcp.service import ScopeService  # noqa: E402

EVIDENCE_DIR = os.path.dirname(os.path.abspath(__file__))

_BACKEND = "real"
_SERIAL = "JY908/0278"
_PORT = 0            # D0..D7
_THRESHOLD_MV = 1500  # logic threshold (operator-verified for 3.3 V / 5 V logic)
_TRIG_SOURCE = "D1"   # PIC32 UART TX (RPC2), falling edge = UART start bit
_TRIG_EDGE = "falling"
_PRE = 62500          # 1 ms at 16 ns
_POST = 187500        # 3 ms at 16 ns
_TIMEBASE = 4         # 16 ns/sample
_TRIGGER_WAIT_MS = 5000

NOTES = (
    "PICO-4T physical validation: ONE bounded digital-edge-triggered capture. "
    "D1 falling edge, 16 ns/sample, pre=1 ms post=3 ms (250k samples). "
    "Trigger from normal UART traffic only (no PIC32 command)."
)


def _git_head() -> str:
    try:
        out = subprocess.run(
            ["git", "-C", _REPO_ROOT, "rev-parse", "HEAD"],
            capture_output=True, text=True, timeout=5, check=True,
        )
        return out.stdout.strip()
    except Exception:
        return "unknown"


def _sha256(path: str) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def _write_result(out: dict) -> None:
    path = os.path.join(EVIDENCE_DIR, "pico4t_digital_trigger_result.json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(out, fh, indent=2, sort_keys=True)
        fh.write("\n")
    print(json.dumps(out, indent=2, sort_keys=True))


def main() -> int:
    backend = sys.argv[1] if len(sys.argv) > 1 else _BACKEND
    out = {
        "step": "PICO-4T physical validation: ONE bounded digital-edge-triggered capture",
        "timestamp_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "git_head": _git_head(),
        "backend_requested": backend,
        "backend": None,
        "simulator_fallback": False,
        "parameters": {
            "digital_port": _PORT,
            "threshold_mv": _THRESHOLD_MV,
            "trigger_source": _TRIG_SOURCE,
            "trigger_edge": _TRIG_EDGE,
            "pre_samples": _PRE,
            "post_samples": _POST,
            "sample_count": _PRE + _POST,
            "timebase": _TIMEBASE,
            "trigger_wait_ms": _TRIGGER_WAIT_MS,
            "serial": _SERIAL,
        },
        "gate": None,
        "scope_info": None,
        "capture_result": None,
        "metadata": None,
        "export": None,
        "files": None,
        "npz_reload": None,
        "checks": None,
        "ownership_after": None,
        "failure": None,
    }

    cfg = ServerConfig.from_env()
    out["gate"] = {
        "PICOSCOPE_MCP_ALLOW_REAL_env": os.environ.get("PICOSCOPE_MCP_ALLOW_REAL"),
        "allow_real_backend": cfg.allow_real_backend,
        "evidence_dir": cfg.evidence_dir,
    }

    if backend == "real" and not cfg.allow_real_backend:
        out["failure"] = {
            "type": "gate_closed",
            "message": "PICOSCOPE_MCP_ALLOW_REAL is not set/truthy; real backend gated. STOP (no hardware).",
        }
        _write_result(out)
        return 2

    svc = ScopeService(cfg)

    # --- 1. scope_info (non-owning discovery) --------------------------------
    try:
        out["scope_info"] = svc.scope_info(backend=backend)
    except Exception as exc:  # noqa: BLE001
        out["failure"] = {"type": type(exc).__name__, "error": str(exc), "stage": "scope_info"}
        _write_result(out)
        return 1

    # --- 2. ONE bounded digital-triggered capture ---------------------------
    profile = AcquisitionProfile(
        backend=backend,
        device_id=_SERIAL if backend == "real" else None,
        digital_ports=[DigitalPortProfile(port=_PORT, enabled=True, threshold_mv=_THRESHOLD_MV)],
        trigger=TriggerProfile(
            source=_TRIG_SOURCE,
            threshold=0.0,  # digital trigger threshold is the port threshold_mv; ignored here
            edge=_TRIG_EDGE,
            pre_samples=_PRE,
            post_samples=_POST,
        ),
        pre_samples=_PRE,
        post_samples=_POST,
        timebase=_TIMEBASE,
        trigger_wait_ms=_TRIGGER_WAIT_MS,
        notes=NOTES,
    )
    try:
        out["capture_result"] = svc.capture_block(profile)
    except Exception as exc:  # noqa: BLE001
        out["failure"] = {"type": type(exc).__name__, "error": str(exc), "stage": "capture_block"}
        _write_result(out)
        return 1

    capture_id = out["capture_result"].get("capture_id")

    # --- 3. metadata + export (no device, no raw array load) -----------------
    try:
        out["metadata"] = svc.get_capture_metadata(capture_id)
        out["export"] = svc.export_capture(capture_id)
    except Exception as exc:  # noqa: BLE001
        out["failure"] = {"type": type(exc).__name__, "error": str(exc), "stage": "metadata/export"}
        _write_result(out)
        return 1

    # --- 4. verify artifacts + provenance + trigger state --------------------
    try:
        paths = out["export"]["artifact_paths"]
        npz_path = paths["npz"]
        json_path = paths["json"]
        out["files"] = {
            "npz": {"path": npz_path, "sha256": _sha256(npz_path), "bytes": os.path.getsize(npz_path)},
            "json": {"path": json_path, "sha256": _sha256(json_path), "bytes": os.path.getsize(json_path)},
        }
        z = np.load(npz_path)
        out["npz_reload"] = {"keys": sorted(str(k) for k in z.files)}
        z.close()

        meta = out["metadata"]
        out["checks"] = {
            "gate_was_open": bool(cfg.allow_real_backend),
            "capture_backend": out["capture_result"].get("backend"),
            "capture_simulated": out["capture_result"].get("simulated"),
            "metadata_backend": meta.get("backend"),
            "metadata_simulated": meta.get("simulated"),
            "device_model": meta.get("device", {}).get("model"),
            "device_serial": meta.get("device", {}).get("serial"),
            "device_variant": meta.get("device", {}).get("variant"),
            "device_driver_version": meta.get("device", {}).get("driver_version"),
            "provenance": meta.get("provenance"),
            "sample_interval_ns": meta.get("sample_interval_ns"),
            "sample_count": meta.get("sample_count"),
            "acquisition_status": meta.get("acquisition_status"),
            "trigger": meta.get("trigger"),
            "trigger_position": meta.get("trigger_position"),
            "trigger_index": meta.get("trigger_index"),
            "trigger_status": meta.get("trigger_status"),
        }
    except Exception as exc:  # noqa: BLE001
        out["failure"] = {"type": type(exc).__name__, "error": str(exc), "stage": "evidence_verify"}
        _write_result(out)
        return 1

    # --- 5. ownership re-check (non-owning) ----------------------------------
    try:
        out["ownership_after"] = svc.scope_info(backend=backend).get("ownership")
    except Exception as exc:  # noqa: BLE001
        out["ownership_after"] = f"<scope_info failed: {type(exc).__name__}: {exc}>"

    out["backend"] = out["capture_result"].get("backend")
    _write_result(out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
