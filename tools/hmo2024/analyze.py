"""OFFLINE verification and analysis (no instrument required).

Works purely on a held capture directory. Provides:

* ``verify_record``  -- integrity checks (hash recompute, header parse, raw vs
                        header sample-count consistency, empty/impossible
                        artifacts, rearm flags) returning a check list;
* ``raw_to_time``    -- raw ASCII -> (time, voltage) conversion using the
                        header's start/stop/count;
* ``measure``        -- evidence measurements with a documented uncertainty
                        model.

Uncertainty model (documented, conservative): the HMO2024 is an 8-bit ADC, so
the quantization step is ``full_scale / 256`` where ``full_scale = scale*8``
(8 vertical divisions). Reported uncertainty is half a quantization step PLUS a
stated caveat that the vertical scale is taken from the recorded inventory
(not independently re-measured) and that physical accuracy is not claimed from
software evidence.
"""

from __future__ import annotations

import math
import os
from typing import Dict, List, Optional, Tuple

import scpi
from records import RecordManager, sha256_of_file


class AnalyzeError(Exception):
    pass


ADC_BITS = 8
VERTICAL_DIVISIONS = 8


def raw_to_time(header: scpi.Header, samples: List[float]) -> List[Tuple[float, float]]:
    """Map raw samples to (time, voltage) using header start/stop/count."""
    if header.count <= 1:
        # Degenerate: a single sample has no well-defined interval.
        if len(samples) == 1:
            return [(header.start, samples[0])]
        return [(header.start, v) for v in samples]
    dt = header.dt
    out: List[Tuple[float, float]] = []
    for i, v in enumerate(samples):
        out.append((header.start + i * dt, v))
    return out


def _full_scale(scale: Optional[float]) -> Optional[float]:
    if scale is None:
        return None
    try:
        fs = float(scale) * VERTICAL_DIVISIONS
        return fs if fs > 0 else None
    except (TypeError, ValueError):
        return None


def quantization_step(full_scale: Optional[float]) -> Optional[float]:
    if full_scale is None:
        return None
    return full_scale / (2 ** ADC_BITS)


def measure(
    samples: List[float],
    scale: Optional[float] = None,
) -> Dict:
    """Basic evidence measurements with uncertainty."""
    if not samples:
        return {"count": 0, "note": "no samples"}

    n = len(samples)
    vmin = min(samples)
    vmax = max(samples)
    mean = sum(samples) / n
    var = sum((v - mean) ** 2 for v in samples) / max(n - 1, 1)
    std = math.sqrt(var)
    peak_to_peak = vmax - vmin

    fs = _full_scale(scale)
    q = quantization_step(fs)
    uncertainty = (q / 2.0) if q is not None else None

    return {
        "count": n,
        "min_v": vmin,
        "max_v": vmax,
        "mean_v": mean,
        "std_v": std,
        "peak_to_peak_v": peak_to_peak,
        "quantization_step_v": q,
        "reported_uncertainty_v": uncertainty,
        "uncertainty_model": (
            "half quantization step (8-bit ADC), full_scale = scale*8; "
            "vertical scale taken from recorded inventory, physical accuracy "
            "NOT claimed from software evidence"
        ),
        "uncertainty_known": uncertainty is not None,
    }


def _check(name: str, ok: bool, detail: str = "") -> Dict:
    return {"check": name, "ok": bool(ok), "detail": detail}


def verify_record(cap_dir: str) -> Dict:
    """Run offline integrity checks on a held capture directory."""
    if not os.path.isdir(cap_dir):
        raise AnalyzeError(f"capture directory not found: {cap_dir}")
    records = RecordManager(cap_dir)
    manifest = records.load_manifest(cap_dir)

    checks: List[Dict] = []
    artifacts = manifest.get("artifacts", [])
    hold = manifest.get("hold") or {}

    # Hash recompute.
    hash_ok = True
    for a in artifacts:
        rel = a.get("rel_path")
        recorded = a.get("sha256")
        path = os.path.join(cap_dir, rel)
        if not os.path.isfile(path):
            checks.append(_check(f"artifact_exists:{rel}", False, "missing"))
            hash_ok = False
            continue
        actual = sha256_of_file(path)
        ok = actual == recorded
        checks.append(_check(f"hash:{rel}", ok, f"recorded={recorded} actual={actual}"))
        hash_ok = hash_ok and ok

    # Header parse + raw sample consistency.
    header = None
    raw = None
    for a in artifacts:
        if a.get("kind") == "waveform_ascii":
            raw = _read_text(cap_dir, a["rel_path"])
        if a.get("kind") == "header_ascii":
            htxt = _read_text(cap_dir, a["rel_path"])
            try:
                header = scpi.parse_header(htxt)
                checks.append(_check("header_parse", True))
            except scpi.ScpiParseError as exc:
                checks.append(_check("header_parse", False, str(exc)))

    if header is not None:
        checks.append(_check("header_count_positive", header.count > 0,
                             f"count={header.count}"))
        checks.append(_check("header_range_valid", header.stop >= header.start,
                             f"start={header.start} stop={header.stop}"))

    if raw is not None:
        try:
            samples = scpi.parse_float_list(raw)
            checks.append(_check("raw_parse", True, f"{len(samples)} samples"))
        except scpi.ScpiParseError as exc:
            samples = []
            checks.append(_check("raw_parse", False, str(exc)))
        if header is not None and samples:
            ok = len(samples) == header.count
            checks.append(
                _check(
                    "header_sample_count_match", ok,
                    f"header.count={header.count} samples={len(samples)}",
                )
            )

    # Empty / impossible artifact lengths.
    for a in artifacts:
        nbytes = a.get("bytes")
        if nbytes == 0:
            checks.append(_check(f"nonempty:{a.get('rel_path')}", False, "0 bytes"))
        elif nbytes is not None and nbytes < 0:
            checks.append(_check(f"length:{a.get('rel_path')}", False, "negative"))

    # Screenshot block sanity: must start with 'BM' (BMP magic).
    for a in artifacts:
        if a.get("kind") == "screenshot_bmp":
            path = os.path.join(cap_dir, a["rel_path"])
            if os.path.isfile(path):
                with open(path, "rb") as f:
                    magic = f.read(2)
                checks.append(_check("screenshot_bmp_magic", magic == b"BM",
                                     f"magic={magic!r}"))

    flags = manifest.get("flags", [])
    checks.append(_check("flags_is_list", isinstance(flags, list),
                         f"{len(flags)} flags"))
    rearm = [f for f in flags if "rearm" in f or "state_changed" in f
             or "header_changed" in f]
    coherent = len(rearm) == 0
    checks.append(_check(
        "artifacts_coherent_with_hold", coherent,
        "no rearm/state/header flags" if coherent else "; ".join(rearm),
    ))

    all_ok = all(c["ok"] for c in checks)
    return {
        "capture_dir": cap_dir,
        "schema": manifest.get("schema"),
        "capture_id": manifest.get("capture_id"),
        "checks": checks,
        "passed": all_ok,
        "artifact_count": len(artifacts),
    }


def _read_text(cap_dir: str, rel_path: str) -> str:
    with open(os.path.join(cap_dir, rel_path), "r", encoding="ascii") as f:
        return f.read()
