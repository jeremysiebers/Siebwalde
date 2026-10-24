"""PICO-4 first physical UART capture: offline decode + analysis (no hardware).

Loads the real capture saved by ``pico4_uart_capture.py``, determines the
TX/RX direction from the data itself (9-bit master request vs 8-bit PIC18
reply), measures the actual bit period/baud from the edge timing, decodes each
line in the appropriate mode, and compares TX/RX timing. Results are exported
as deterministic JSON and a concise human-readable summary.

No device, no hardware, no firmware, no C#. Purely offline over the preserved
NPZ + JSON evidence.
"""

from __future__ import annotations

import json
import os
import sys

_REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
if _REPO_ROOT not in sys.path:
    sys.path.insert(0, _REPO_ROOT)

import numpy as np  # noqa: E402

from tools.picoscope.analysis import io as _aio  # noqa: E402
from tools.picoscope.analysis import uart as _uart  # noqa: E402
from tools.picoscope.backend.serialize import load_capture  # noqa: E402

EVIDENCE_DIR = os.path.dirname(os.path.abspath(__file__))

CAPTURE_ID = "aabc7f9168855cae367864b54895c9f3"
BAUD_NOMINAL = 2016000  # ~2.016 Mbaud, historically observed (HMO2024)


def _measure_bit_period(edge_indices, sample_interval_ns, nominal_spp=31.0):
    """Estimate the 1-bit period from inter-edge distances (MODE of the 1-bit
    cluster; the most common inter-edge spacing in a UART stream is one bit
    period). Returns (period_ns, baud) or (None, None)."""
    diffs = np.diff(np.asarray(edge_indices, dtype=np.float64))
    if diffs.size == 0:
        return None, None
    lo, hi = 0.75 * nominal_spp, 1.5 * nominal_spp
    mask = (diffs >= lo) & (diffs <= hi)
    if not mask.any():
        return None, None
    cluster = np.round(diffs[mask]).astype(np.int64)
    vals, counts = np.unique(cluster, return_counts=True)
    period_samples = float(vals[np.argmax(counts)])
    period_ns = period_samples * float(sample_interval_ns)
    baud = 1e9 / period_ns
    return period_ns, baud


def _line_stats(samples, interval_ns):
    arr = np.asarray(samples, dtype=np.int64)
    arr = (arr != 0).astype(np.int64)
    edges = _uart.extract_edges(arr)
    falling = [e.index for e in edges if e.kind == "falling"]
    rising = [e.index for e in edges if e.kind == "rising"]
    idle = int(arr[0]) if arr.size else None
    # Idle fraction over the first 1000 samples.
    idle_frac = float(arr[:1000].mean()) if arr.size else None
    period_ns, baud = _measure_bit_period([e.index for e in edges], interval_ns)
    return {
        "samples": int(arr.size),
        "edge_count": len(edges),
        "rising": len(rising),
        "falling": len(falling),
        "idle_first_sample": idle,
        "idle_fraction_first_1000": idle_frac,
        "measured_bit_period_ns": period_ns,
        "measured_baud": baud,
    }


def _decode(samples, interval_ns, mode, baud):
    cfg = _uart.UartConfig(mode=mode, baud=baud, sample_interval_ns=interval_ns,
                           polarity="idle_high", stop_bits=1,
                           mode_source="verified_marker" if mode == "9bit" else "explicit")
    return _uart.decode_uart(samples, cfg)


def _bit_reverse16(w: int) -> int:
    r = 0
    for _ in range(16):
        r = (r << 1) | (w & 1)
        w >>= 1
    return r


def _crc16_modbus(data) -> int:
    c = 0xFFFF
    for b in data:
        c ^= b & 0xFF
        for _ in range(8):
            c = (c >> 1) ^ 0xA001 if (c & 1) else (c >> 1)
    return c & 0xFFFF


def _siebwalde_crc_bytes(payload):
    """On-wire Siebwalde CRC = bit_reverse16(crc16_modbus(payload)), lo byte first."""
    w = _bit_reverse16(_crc16_modbus(payload))
    return w & 0xFF, (w >> 8) & 0xFF


def _verify_transactions(frames, split_mode):
    """Group frames into transactions and verify the Siebwalde bit-reversed CRC.

    ``split_mode`` is ``"ninth"`` (9-bit: a transaction starts at a ninth=1 frame)
    or ``"gap"`` (8-bit: split on an inter-frame start gap > ~100 us).
    Returns a list of per-transaction dicts.
    """
    groups = []
    for f in frames:
        if not groups:
            groups.append([f])
            continue
        if split_mode == "ninth":
            if f.get("ninth_bit") == 1:
                groups.append([f])
            else:
                groups[-1].append(f)
        else:  # "gap"
            gap_ns = f["t_start_ns"] - groups[-1][-1]["t_start_ns"]
            if gap_ns > 100000.0:
                groups.append([f])
            else:
                groups[-1].append(f)
    out = []
    for g in groups:
        payload = [f["value"] for f in g][:-2]  # strip the 2 CRC bytes
        crc_lo = g[-2]["value"]
        crc_hi = g[-1]["value"]
        exp_lo, exp_hi = _siebwalde_crc_bytes(payload)
        out.append({
            "payload": payload,
            "crc_received": [crc_lo, crc_hi],
            "crc_expected": [exp_lo, exp_hi],
            "crc_valid": (crc_lo == exp_lo) and (crc_hi == exp_hi),
            "frame_count": len(g),
        })
    return out


def main() -> int:
    base = os.path.join(_REPO_ROOT, "tools", "picoscope", "evidence", "mcp", CAPTURE_ID)
    cap = load_capture(base)
    interval_ns = int(cap.sample_interval_ns)

    out = {
        "capture_id": CAPTURE_ID,
        "sample_interval_ns": interval_ns,
        "sample_count": int(cap.sample_count),
        "backend": cap.device_identity.backend,
        "device_serial": cap.device_identity.serial,
        "provenance": dict(cap.provenance),
        "lines": {},
        "direction": None,
        "baud": {"nominal": BAUD_NOMINAL, "measured": None},
        "tx_decode": None,
        "rx_decode": None,
        "tx_rx_comparison": None,
    }

    # -- raw signal stats + per-mode decode for D0 and D1 --------------------
    for line in ("D0", "D1"):
        samples = _aio.extract_digital_line(cap, line)
        stats = _line_stats(samples, interval_ns)
        stats["decode_8bit"] = _decode(samples, interval_ns, "8bit", BAUD_NOMINAL)
        stats["decode_9bit"] = _decode(samples, interval_ns, "9bit", BAUD_NOMINAL)
        out["lines"][line] = stats

    # -- direction determination: 9-bit = master TX, 8-bit = PIC18 reply -----
    # A line that decodes cleanly in 9-bit mode (>=1 frame, low error rate) and
    # whose frames show the address-marker pattern (ninth=1 on the first byte of
    # a group, ninth=0 after) is the master TX. The other line (8-bit, tenth
    # frame = 10 bits) is the PIC18 reply.
    d0_9 = out["lines"]["D0"]["decode_9bit"]["summary"]
    d1_9 = out["lines"]["D1"]["decode_9bit"]["summary"]
    d0_8 = out["lines"]["D0"]["decode_8bit"]["summary"]
    d1_8 = out["lines"]["D1"]["decode_8bit"]["summary"]

    def _clean(s):
        return s["frame_count"] > 0 and s["error_frames"] == 0

    tx_line = rx_line = None
    if _clean(d0_9) and _clean(d1_8):
        tx_line, rx_line = "D0", "D1"
    elif _clean(d1_9) and _clean(d0_8):
        tx_line, rx_line = "D1", "D0"
    # Note: a single "clean 9-bit decode" is NOT sufficient to prove a line is
    # 9-bit. An 8-bit frame (10 bits) re-read as 9-bit puts the STOP bit into
    # the ninth-bit position, so its ninth bit is always 1 (no address pattern).
    # The discriminator is the ERROR pattern: the 9-bit master line fails the
    # 8-bit decode (wrong frame length), while the 8-bit reply line decodes
    # cleanly in 8-bit mode. Also require the ninth-bit ADDRESS pattern on the
    # TX line (some ninth=1, some ninth=0) rather than an all-set ninth bit.
    direction = {"tx": tx_line, "rx": rx_line}
    if tx_line:
        ninth = out["lines"][tx_line]["decode_9bit"]["summary"]
        if ninth["frame_count"] == ninth["ninth_set"]:
            direction["note"] = (
                f"{tx_line} ninth bits are ALL set (likely 8-bit frames re-read "
                "as 9-bit); direction is ambiguous"
            )
    out["direction"] = direction

    # -- measured baud from the TX line (the denser 9-bit line) -------------
    tx_line = tx_line or "D0"
    measured = out["lines"][tx_line].get("measured_baud")
    out["baud"]["measured"] = measured

    # -- final decode at the NOMINAL baud (which decodes cleanly) -----------
    baud = BAUD_NOMINAL
    if tx_line and rx_line:
        out["tx_decode"] = _decode(_aio.extract_digital_line(cap, tx_line), interval_ns, "9bit", baud)
        out["rx_decode"] = _decode(_aio.extract_digital_line(cap, rx_line), interval_ns, "8bit", baud)
        out["tx_rx_comparison"] = _uart.compare_tx_rx(
            out["tx_decode"]["frames"], out["rx_decode"]["frames"], window_ns=500000.0
        )
        # Transaction-level response delay: first RX frame start minus first TX
        # frame start, per transaction (the request is 8 bytes / 11 bits each).
        out["response_delay_ns"] = []
        tx_frames = out["tx_decode"]["frames"]
        rx_frames = out["rx_decode"]["frames"]
        tx_addrs = [f for f in tx_frames if f.get("ninth_bit") == 1]
        for ta in tx_addrs:
            later = [r for r in rx_frames if r["t_start_ns"] >= ta["t_start_ns"]]
            if later:
                nr = min(later, key=lambda r: r["t_start_ns"] - ta["t_start_ns"])
                out["response_delay_ns"].append({
                    "tx_addr_value": ta["value"],
                    "tx_start_ns": ta["t_start_ns"],
                    "rx_start_ns": nr["t_start_ns"],
                    "delta_ns": nr["t_start_ns"] - ta["t_start_ns"],
                })

    out["tx_decode_config"] = {"line": tx_line, "mode": "9bit", "baud": baud}
    out["rx_decode_config"] = {"line": rx_line, "mode": "8bit", "baud": baud}

    # -- Siebwalde bit-reversed CRC verification (per transaction) ----------
    if tx_line and rx_line and out["tx_decode"] and out["rx_decode"]:
        out["tx_crc"] = _verify_transactions(out["tx_decode"]["frames"], "ninth")
        out["rx_crc"] = _verify_transactions(out["rx_decode"]["frames"], "gap")

    # -- export --------------------------------------------------------------
    out_path = os.path.join(EVIDENCE_DIR, "pico4_uart_decode_result.json")
    _aio.export_analysis_json(out, out_path)

    # concise human-readable summary
    print("=== PICO-4 UART decode summary ===")
    print(f"capture_id={CAPTURE_ID} backend={out['backend']} serial={out['device_serial']}")
    print(f"sample_interval_ns={interval_ns} sample_count={out['sample_count']}")
    for line in ("D0", "D1"):
        s = out["lines"][line]
        print(f"{line}: edges={s['edge_count']} (rising={s['rising']}, falling={s['falling']}) "
              f"idle_first={s['idle_first_sample']} idle_frac={s['idle_fraction_first_1000']:.3f} "
              f"measured_baud={s['measured_baud']}")
        for mode in ("8bit", "9bit"):
            sm = s[f"decode_{mode}"]["summary"]
            print(f"  {mode}: frames={sm['frame_count']} good={sm['good_frames']} "
                  f"err={sm['error_frames']} trunc={sm['truncated_frames']} "
                  f"ninth_set={sm['ninth_set']} ninth_clear={sm['ninth_clear']}")
    print(f"direction: TX={tx_line} RX={rx_line}")
    print(f"baud: nominal={BAUD_NOMINAL} measured={measured}")
    if out["tx_decode"]:
        print(f"TX frames ({tx_line}, 9-bit): {len(out['tx_decode']['frames'])}")
        for f in out["tx_decode"]["frames"]:
            print(f"  TX#{f['index']} @{f['t_start_ns']:.0f}ns value=0x{f['value']:02X} "
                  f"ninth={f['ninth_bit']} raw=0x{f['raw_value']:03X} stop_ok={f['stop_ok']} "
                  f"err={f['errors']}")
    if out["rx_decode"]:
        print(f"RX frames ({rx_line}, 8-bit): {len(out['rx_decode']['frames'])}")
        for f in out["rx_decode"]["frames"]:
            print(f"  RX#{f['index']} @{f['t_start_ns']:.0f}ns value=0x{f['value']:02X} "
                  f"stop_ok={f['stop_ok']} err={f['errors']}")
    print(f"result_json={out_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
