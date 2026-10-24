"""PICO-4 first physical UART capture: ONE bounded triggerless digital acquisition.

Measurement-only. Exercises the exact Layer C ``ScopeService`` code path the
FastMCP tools call, so the capture is physically identical to a capture made
through OpenCode/MCP:

    scope_info(backend="real")       -> device identity (non-owning discovery)
    capture_block(AcquisitionProfile) -> ONE bounded block capture + evidence
    get_capture_metadata(capture_id)  -> compact sidecar metadata
    export_capture(capture_id)        -> validated artifact references

Real hardware is gated by ``PICOSCOPE_MCP_ALLOW_REAL=1``, set in THIS subprocess's
environment only (process-scoped, never persisted to ``opencode.json``).

Acquisition (approved Profile A, first physical UART validation):
  * Digital port 0 (D0..D7), logic threshold 1500 mV.
  * D0 = PIC32 UART TX (RPC2, logic side), D1 = PIC32 UART RX (RPC3, logic side).
  * TRIGGERLESS (trigger=None): the ps3000a digital-line trigger is NOT implemented
    in the adapter (a D0 trigger would fall back to EXTERNAL and never fire), so a
    bounded triggerless window is used. The master cyclic polling is ~1.2 ms/msg,
    so a ~4 ms window reliably contains several request/response transactions.
  * timebase 4 -> 16 ns/sample (PICO-2 sampling table), 250000 samples ~= 4 ms.

No AWG / signal generation, no flash / reset / motion / Siebwalde state, no
simulator fallback. Exactly ONE acquisition; on any real-backend failure the
original exception (PICO_STATUS where applicable) is preserved and the script
STOPS without retry.

Usage:
    python pico4_uart_capture.py [backend]   # backend: "real" (default) | "simulator"
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
from tools.picoscope.mcp.schema import AcquisitionProfile, DigitalPortProfile  # noqa: E402
from tools.picoscope.mcp.service import ScopeService  # noqa: E402

EVIDENCE_DIR = os.path.dirname(os.path.abspath(__file__))

# Acquisition parameters (approved Profile A, first physical UART capture).
_BACKEND = "real"
_SERIAL = "JY908/0278"
_PORT = 0            # D0..D7
_THRESHOLD_MV = 1500  # logic threshold (validated by the operator for 3.3 V / 5 V logic)
_PRE = 0             # triggerless: no pre-trigger samples
_POST = 250000       # ~4 ms at 16 ns/sample
_TIMEBASE = 4        # 16 ns/sample (PICO-2 sampling table)

NOTES = (
    "PICO-4 first physical UART capture (triggerless, bounded): digital port 0, "
    "threshold 1500 mV, D0=TX(RPC2), D1=RX(RPC3), 16 ns/sample, ~4 ms. No trigger "
    "(ps3000a digital-line trigger not implemented). Normal safe operation only."
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
    path = os.path.join(EVIDENCE_DIR, "pico4_uart_capture_result.json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(out, fh, indent=2, sort_keys=True)
        fh.write("\n")
    print(json.dumps(out, indent=2, sort_keys=True))


def main() -> int:
    backend = sys.argv[1] if len(sys.argv) > 1 else _BACKEND
    out = {
        "step": "PICO-4 first physical UART capture: ONE bounded triggerless digital acquisition",
        "timestamp_utc": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "git_head": _git_head(),
        "backend_requested": backend,
        "backend": None,
        "simulator_fallback": False,
        "parameters": {
            "digital_port": _PORT,
            "threshold_mv": _THRESHOLD_MV,
            "trigger": None,
            "pre_samples": _PRE,
            "post_samples": _POST,
            "sample_count": _PRE + _POST,
            "timebase": _TIMEBASE,
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

    # --- 2. ONE bounded triggerless capture ---------------------------------
    profile = AcquisitionProfile(
        backend=backend,
        device_id=_SERIAL if backend == "real" else None,
        digital_ports=[DigitalPortProfile(port=_PORT, enabled=True, threshold_mv=_THRESHOLD_MV)],
        trigger=None,
        pre_samples=_PRE,
        post_samples=_POST,
        timebase=_TIMEBASE,
        trigger_wait_ms=0,
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

    # --- 4. verify artifacts + provenance ------------------------------------
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
            "digital_ports": meta.get("digital_ports"),
            "trigger": meta.get("trigger"),
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
