"""Reusable offline 9-bit UART decoder for Siebwalde POD digital captures.

Decodes the POD logic channels ``D6`` (master request direction) and ``D7``
(slave response direction) from a ``pod1.raw`` sample file into UART frames and
Modbus-RTU telegrams, then verifies the project's bit-reversed Modbus
CRC-16/MODBUS on-wire convention.

Frame formats (VERIFIED, not assumed)
------------------------------------
The two directions use different frame formats:

* **REQUEST (D6)** — PIC32 master TX.  9-bit UART, 11 bits/frame
  ``[START=0][D0..D7][D8=address marker][STOP=1]``.  D8=1 on the address byte,
  D8=0 on all following bytes.  Source: ``TrackController5``
  ``slavefwhandler.c`` (``UART2_Config_9bit_Modbus``, ``PDSEL=0b11``) and
  ``PetitModbusPort.c`` (``PetitModBus_UART_PutchAddr`` sets the 9th bit).

* **RESPONSE (D7)** — PIC18 slave TX.  8-bit UART, 10 bits/frame
  ``[START=0][D0..D7][STOP=1]``, **no 9th bit**.  Source:
  ``TrackAmplifier4.X/modbus/PetitModbusPort.c`` (``EUSART1_Write9`` hardcodes
  ``TX9=0; TX9D=0``) and ``eusart.c`` (``TX9 8-bit``).  This is confirmed by
  the captured data: the response frames are 10 bits (the run that would be D8
  in a 9-bit reading is absent), and the 9-bit reading yields a spurious
  constant D8=1 (it samples the stop bit).

This module still implements BOTH formats and makes the per-direction format
explicit and configurable, but the defaults reflect the verified firmware.

Timing model
------------
Samples are one-per-sample 8-bit POD values (bit ``N`` = channel ``D<7-N>``).
The sample period is derived from the CH1 header ``<start>,<stop>,<count>,<vps>``
(``dt = (stop - start) / (count - 1)``; ~100.0 ns for 10 MSa/s).  The bit
period is **measured from the data** (single-bit run lengths / edge spacings),
not assumed.

Software/tooling only: no firmware, no flash, no live hardware, no train
commands.
"""

from __future__ import annotations

import math
from collections import Counter
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Sequence, Tuple

from . import crc

# POD bit indices (bit 0 = least significant bit of the 8-bit sample).
BIT_REQUEST = 6   # D6 -> REQUEST direction (master -> slave, 9-bit)
BIT_RESPONSE = 7  # D7 -> RESPONSE direction (slave -> master, 8-bit)

DIRECTION_REQUEST = "REQUEST"
DIRECTION_RESPONSE = "RESPONSE"

# Per-direction frame format (verified against firmware + capture).
REQUEST_DATA_BITS = 9   # 9-bit: 8 data bits + address marker
RESPONSE_DATA_BITS = 8  # 8-bit: no 9th bit

NOMINAL_BAUD = 2.0161e6
NOMINAL_BIT_NS = 1e9 / NOMINAL_BAUD  # ~496.0 ns

# Inter-telegram gap used for burst detection, in bit periods.  The observed
# intra-telegram inter-byte gap on the response is ~2 bit periods; Modbus RTU
# inter-frame silence is 3.5 characters.  This threshold is comfortably between
# the two.
INTER_TELEGRAM_GAP_BITS = 20.0

# Modbus function codes we understand.
FC_READ_HOLDING = 0x03
FC_READ_INPUT = 0x04
FC_WRITE_SINGLE = 0x06
FC_DIAGNOSTIC = 0x08
FC_WRITE_MULTIPLE_COILS = 0x0F
FC_WRITE_MULTIPLE = 0x10

_FUNCTION_NAMES = {
    FC_READ_HOLDING: "READ_HOLDING_REGISTERS",
    FC_READ_INPUT: "READ_INPUT_REGISTERS",
    FC_WRITE_SINGLE: "WRITE_SINGLE_REGISTER",
    FC_DIAGNOSTIC: "DIAGNOSTIC",
    FC_WRITE_MULTIPLE_COILS: "WRITE_MULTIPLE_COILS",
    FC_WRITE_MULTIPLE: "WRITE_MULTIPLE_REGISTERS",
}


class DecoderError(Exception):
    pass


def read_pod_samples(path: str) -> List[int]:
    """Read ``pod1.raw`` (ASCII comma-separated 8-bit POD values)."""
    with open(path, "r", encoding="ascii") as f:
        text = f.read()
    parts = [p for p in text.replace("\n", ",").split(",") if p.strip() != ""]
    try:
        values = [int(p) for p in parts]
    except ValueError as exc:
        raise DecoderError(f"non-integer POD sample in {path}: {exc}") from exc
    return values


def extract_bit(samples: Sequence[int], bit: int) -> List[int]:
    """Return a 0/1 stream for POD channel ``bit`` across ``samples``."""
    return [(v >> bit) & 1 for v in samples]


def read_header(path: str) -> Tuple[float, float, int, float]:
    """Parse a CH1 header ``<start>,<stop>,<count>,<vps>`` -> (start, stop, count, dt)."""
    with open(path, "r", encoding="ascii") as f:
        text = f.read().strip()
    parts = [p.strip() for p in text.split(",")]
    if len(parts) != 4:
        raise DecoderError(f"header expected 4 fields, got {len(parts)}: {text!r}")
    try:
        start = float(parts[0])
        stop = float(parts[1])
        count = int(parts[2])
        vps = int(parts[3])
    except ValueError as exc:
        raise DecoderError(f"bad header field in {path}: {exc}") from exc
    if count <= 1:
        raise DecoderError(f"header count must be > 1: {count}")
    dt = (stop - start) / (count - 1)
    return start, stop, count, dt


def read_ch1(path: str) -> List[float]:
    """Read an ASCII CH1 waveform into floats."""
    with open(path, "r", encoding="ascii") as f:
        text = f.read()
    parts = [p for p in text.replace("\n", ",").split(",") if p.strip() != ""]
    try:
        return [float(p) for p in parts]
    except ValueError as exc:
        raise DecoderError(f"non-numeric CH1 sample in {path}: {exc}") from exc


# ---------------------------------------------------------------------------
# Bit-period estimation
# ---------------------------------------------------------------------------

def _edge_spacings(stream: Sequence[int]) -> Tuple[List[int], List[int]]:
    edges = [i for i in range(1, len(stream)) if stream[i] != stream[i - 1]]
    spacings = [edges[k + 1] - edges[k] for k in range(len(edges) - 1)]
    return edges, spacings


def _run_lengths(stream: Sequence[int]) -> List[Tuple[int, int]]:
    runs: List[Tuple[int, int]] = []
    i = 0
    n = len(stream)
    while i < n:
        j = i
        while j < n and stream[j] == stream[i]:
            j += 1
        runs.append((stream[i], j - i))
        i = j
    return runs


def _fit_period(intervals: List[int], lo: float, hi: float) -> Optional[float]:
    """Least-squares fit of a fundamental period T over [lo, hi] (samples)."""
    usable = [d for d in intervals if 2 <= d <= 200]
    if not usable:
        return None
    best_t: Optional[float] = None
    best_err = math.inf
    t100_lo = int(round(lo * 100))
    t100_hi = int(round(hi * 100))
    for t100 in range(t100_lo, t100_hi + 1):
        t = t100 / 100.0
        err = 0.0
        for d in usable:
            k = max(1, round(d / t))
            err += (d - k * t) ** 2
        if err < best_err:
            best_err = err
            best_t = t
    return best_t


def measure_bit_period(stream: Sequence[int]) -> Dict:
    """Measure the bit period of a 0/1 stream from its edge geometry.

    UART runs are integer multiples of the bit period, so the *shortest*
    intervals (single bits) are the fundamental unit.  The most common short
    interval (the mode of intervals within 2 samples of the minimum) is used as
    the single-bit anchor; a least-squares fit over the whole interval set is
    then restricted to a narrow band around that anchor.  This avoids
    sub-harmonic aliasing (e.g. fitting ~2.6 samples when the true period is
    ~5 samples) while still allowing the fit to settle on the true period
    (which may be slightly above the integer single-bit length because a
    ~5.2-sample period still yields mostly 5-sample single-bit runs).
    """
    edges, spacings = _edge_spacings(stream)
    runs = _run_lengths(stream)
    internal_runs = [r[1] for r in runs[1:-1]]  # exclude capture-truncated runs
    intervals = spacings + internal_runs
    short = [d for d in intervals if 2 <= d <= 12]

    single_bit: Optional[int] = None
    if short:
        min_v = min(short)
        window = [d for d in short if d <= min_v + 2]
        counts = Counter(window)
        # mode; on ties prefer the smaller value (closer to the true period)
        single_bit = min(counts.items(), key=lambda kv: (-kv[1], kv[0]))[0]

    t: Optional[float] = None
    if single_bit is not None:
        lo = max(2.0, single_bit - 0.5)
        hi = single_bit + 1.0
        t = _fit_period(intervals, lo, hi)

    short_hist = dict(Counter(sorted([d for d in spacings if 2 <= d <= 9])))

    return {
        "edge_count": len(edges),
        "min_spacing": min(spacings) if spacings else None,
        "single_bit_mode": single_bit,
        "fitted_period_samples": t,
        "short_spacing_histogram": short_hist,
        "internal_run_count": len(internal_runs),
        "first_run": runs[0] if runs else None,
        "last_run": runs[-1] if runs else None,
    }


# ---------------------------------------------------------------------------
# Frame decoding
# ---------------------------------------------------------------------------

@dataclass
class Frame:
    direction: str
    start_index: int                # sample index of the start bit (first low)
    data: int                       # D0..D7 (8 data bits)
    ninth: Optional[int]            # D8 address marker (None for 8-bit frames)
    bit_center_indices: List[int]   # sample indices used for the data bits
    stop_center_index: int          # sample index used for the stop bit
    stop_ok: bool                   # stop bit sampled high
    t_start_s: float                # absolute time of the start bit


def _majority(stream: Sequence[int], center: float, radius: int = 1) -> int:
    c = int(round(center))
    total = 0
    count = 0
    for off in range(-radius, radius + 1):
        idx = c + off
        if 0 <= idx < len(stream):
            total += stream[idx]
            count += 1
    if count == 0:
        return 1  # out of range -> treat as idle/high (conservative)
    return 1 if total * 2 >= count else 0


def _try_decode(stream: Sequence[int], s0: int, t_samples: float, dt_s: float,
                direction: str, t0_s: float, data_bits: int) -> Optional[Frame]:
    n = len(stream)
    total_bits = data_bits + 2  # start + data + stop
    stop_center = s0 + (data_bits + 1.5) * t_samples
    end_index = s0 + int(round(total_bits * t_samples))
    if end_index >= n:
        return None

    centers = [s0 + (k + 1.5) * t_samples for k in range(data_bits)]
    center_indices = [int(round(c)) for c in centers]

    # Start bit sanity: the sampled start cell should be low.
    if _majority(stream, s0 + 0.5 * t_samples) != 0:
        return None

    bits = [_majority(stream, c) for c in centers]
    data = 0
    for k in range(8):
        data |= bits[k] << k
    ninth = bits[8] if data_bits >= 9 else None

    if _majority(stream, stop_center) != 1:
        return None

    return Frame(
        direction=direction,
        start_index=s0,
        data=data,
        ninth=ninth,
        bit_center_indices=center_indices,
        stop_center_index=int(round(stop_center)),
        stop_ok=True,
        t_start_s=t0_s + s0 * dt_s,
    )


def decode_stream(stream: Sequence[int], t_samples: float, dt_s: float,
                  direction: str, t0_s: float, data_bits: int) -> List[Frame]:
    """Decode all UART frames in ``stream`` using bit period ``t_samples``."""
    frames: List[Frame] = []
    n = len(stream)
    total_bits = data_bits + 2
    i = 1
    while i < n:
        if stream[i - 1] == 1 and stream[i] == 0:
            frame = _try_decode(stream, i, t_samples, dt_s, direction, t0_s, data_bits)
            if frame is not None:
                frames.append(frame)
                i = frame.start_index + int(round(total_bits * t_samples))
                continue
        i += 1
    return frames


# ---------------------------------------------------------------------------
# Telegram grouping and analysis
# ---------------------------------------------------------------------------

@dataclass
class Telegram:
    direction: str
    frames: List[Frame]
    data_bytes: List[int] = field(default_factory=list)
    address: Optional[int] = None
    function: Optional[int] = None
    function_name: Optional[str] = None
    expected_len: Optional[int] = None
    register_address: Optional[int] = None
    quantity: Optional[int] = None
    byte_count: Optional[int] = None
    values: List[int] = field(default_factory=list)  # 16-bit register values
    crc_received: Tuple[Optional[int], Optional[int]] = (None, None)
    crc_status: str = "INCOMPLETE"
    crc_siebwalde_ok: Optional[bool] = None
    crc_standard_ok: Optional[bool] = None
    starts_with_address: bool = True
    truncated_by_capture: bool = False
    note: str = ""


def _request_len(fn: int, b: List[int]) -> Optional[int]:
    if fn in (FC_READ_HOLDING, FC_READ_INPUT, FC_WRITE_SINGLE, FC_DIAGNOSTIC):
        return 8
    if fn in (FC_WRITE_MULTIPLE, FC_WRITE_MULTIPLE_COILS):
        if len(b) < 7:
            return None
        return 9 + b[6]
    return None


def _response_len(fn: int, b: List[int]) -> Optional[int]:
    if fn & 0x80:
        return 5  # exception: addr, fn|0x80, code, crc_lo, crc_hi
    if fn in (FC_READ_HOLDING, FC_READ_INPUT):
        if len(b) < 3:
            return None
        return 5 + b[2]  # addr, fn, bytecount, data..., crc
    if fn in (FC_WRITE_SINGLE, FC_DIAGNOSTIC, FC_WRITE_MULTIPLE, FC_WRITE_MULTIPLE_COILS):
        return 8
    return None


def _split_bursts(frames: List[Frame], gap_threshold_bits: float,
                  bit_period_samples: float) -> List[List[Frame]]:
    """Split frames into bursts on inter-frame gaps above the threshold."""
    if not frames:
        return []
    gap_samples = gap_threshold_bits * bit_period_samples
    bursts: List[List[Frame]] = [[frames[0]]]
    for prev, cur in zip(frames, frames[1:]):
        gap = cur.start_index - prev.start_index
        if gap > gap_samples:
            bursts.append([cur])
        else:
            bursts[-1].append(cur)
    return bursts


def _group_marked(burst: List[Frame]) -> List[List[Frame]]:
    """Group 9-bit frames by the address marker (ninth==1 starts a telegram)."""
    groups: List[List[Frame]] = []
    current: List[Frame] = []
    for f in burst:
        if f.ninth == 1:
            if current:
                groups.append(current)
            current = [f]
        else:
            if current:
                current.append(f)
            else:
                current = [f]  # orphan data frame (will be flagged)
    if current:
        groups.append(current)
    return groups


def _group_by_length(burst: List[Frame]) -> List[List[Frame]]:
    """Group 8-bit frames by expected Modbus response length."""
    groups: List[List[Frame]] = []
    i = 0
    n = len(burst)
    while i < n:
        remaining = burst[i:]
        if len(remaining) < 2:
            groups.append(remaining)
            break
        fn = remaining[1].data
        b = [f.data for f in remaining]
        expected = _response_len(fn, b)
        if expected is None:
            groups.append(remaining)
            break
        take = min(expected, len(remaining))
        groups.append(remaining[:take])
        i += take
    return groups


def group_telegrams(frames: List[Frame], direction: str, data_bits: int,
                    bit_period_samples: float, stream_len: int) -> List[Telegram]:
    """Group frames into telegrams (burst-split, then marker or length based)."""
    telegrams: List[Telegram] = []
    bursts = _split_bursts(frames, INTER_TELEGRAM_GAP_BITS, bit_period_samples)
    for burst in bursts:
        if data_bits >= 9:
            groups = _group_marked(burst)
        else:
            groups = _group_by_length(burst)
        for g in groups:
            telegrams.append(_make_telegram(g, direction, data_bits, stream_len))
    return telegrams


def _parse_values(fn: int, b: List[int]) -> List[int]:
    """Extract 16-bit big-endian register values from a decoded telegram body."""
    values: List[int] = []
    body = b[2:-2] if len(b) >= 4 else []  # strip addr, fn, and 2 CRC bytes
    if fn in (FC_READ_HOLDING, FC_READ_INPUT) and body:
        bytecount = body[0]
        for i in range(bytecount // 2):
            base = 1 + 2 * i
            if base + 1 < len(body):
                values.append((body[base] << 8) | body[base + 1])
    elif fn == FC_WRITE_SINGLE and len(body) >= 4:
        values.append((body[2] << 8) | body[3])
    elif fn == FC_WRITE_MULTIPLE and body:
        bytecount = body[4]
        for i in range(bytecount // 2):
            base = 5 + 2 * i
            if base + 1 < len(body):
                values.append((body[base] << 8) | body[base + 1])
    return values


def _make_telegram(frames: List[Frame], direction: str, data_bits: int,
                   stream_len: int) -> Telegram:
    tel = Telegram(direction=direction, frames=frames)
    tel.data_bytes = [f.data for f in frames]
    b = tel.data_bytes

    tel.starts_with_address = bool(frames and (data_bits < 9 or frames[0].ninth == 1))
    tel.address = b[0] if b else None
    tel.function = b[1] if len(b) >= 2 else None
    if tel.function is not None:
        tel.function_name = _FUNCTION_NAMES.get(tel.function & 0x7F,
                                                f"FC_{tel.function & 0x7F:02X}")

    tel.truncated_by_capture = bool(
        frames and frames[-1].stop_center_index >= stream_len - int(3)
    )

    if len(b) < 4:
        tel.crc_status = "INCOMPLETE"
        tel.note = "fewer than 4 bytes (addr+func+CRC) present"
        return tel

    if tel.function is None:
        tel.crc_status = "MALFORMED"
        tel.note = "no function byte"
        return tel

    if direction == DIRECTION_REQUEST:
        tel.expected_len = _request_len(tel.function, b)
    else:
        tel.expected_len = _response_len(tel.function, b)

    if tel.expected_len is None:
        tel.crc_status = "MALFORMED"
        tel.note = f"unknown/unsupported function 0x{tel.function:02X}"
        return tel

    # Best-effort field parsing (works even for truncated frames).
    if direction == DIRECTION_REQUEST:
        if len(b) >= 6:
            tel.register_address = (b[2] << 8) | b[3]
            tel.quantity = (b[4] << 8) | b[5]
        if tel.function in (FC_WRITE_MULTIPLE, FC_WRITE_MULTIPLE_COILS) and len(b) >= 7:
            tel.byte_count = b[6]
    else:
        if tel.function in (FC_READ_HOLDING, FC_READ_INPUT) and len(b) >= 3:
            tel.byte_count = b[2]
        # FC06/08/16/15 responses echo reg+qty in bytes[2..5]; FC03/04 responses
        # carry bytecount+data there, so do NOT parse reg/qty for reads.
        if tel.function in (FC_WRITE_SINGLE, FC_DIAGNOSTIC,
                            FC_WRITE_MULTIPLE, FC_WRITE_MULTIPLE_COILS) and len(b) >= 6:
            tel.register_address = (b[2] << 8) | b[3]
            tel.quantity = (b[4] << 8) | b[5]
    tel.values = _parse_values(tel.function, b)

    if len(b) < tel.expected_len:
        tel.crc_status = "INCOMPLETE"
        tel.note = (f"expected {tel.expected_len} bytes, have {len(b)}"
                    f" (capture truncated={tel.truncated_by_capture})")
        return tel
    if len(b) > tel.expected_len:
        tel.crc_status = "MALFORMED"
        tel.note = f"expected {tel.expected_len} bytes, have {len(b)}"
        return tel

    payload = b[: len(b) - 2]
    tel.crc_received = (b[-2], b[-1])
    tel.crc_siebwalde_ok = crc.verify_crc_siebwalde(payload, b[-2], b[-1])
    tel.crc_standard_ok = crc.verify_crc_standard(payload, b[-2], b[-1])
    tel.crc_status = "VALID" if tel.crc_siebwalde_ok else "CRC-MISMATCH"
    if not tel.crc_siebwalde_ok and tel.crc_standard_ok:
        tel.note = "matches standard Modbus CRC but not the Siebwalde bit-reversed convention"
    elif not tel.crc_siebwalde_ok and not tel.crc_standard_ok:
        tel.note = "matches neither convention (bit error or wrong framing)"
    return tel


# ---------------------------------------------------------------------------
# CH1 correlation (same-acquisition shared time base only)
# ---------------------------------------------------------------------------

def ch1_edges(samples: Sequence[float], dt_s: float, t0_s: float,
              threshold: Optional[float] = None) -> List[Dict]:
    """Detect CH1 threshold crossings (rising/falling) on the shared time grid."""
    if not samples:
        return []
    if threshold is None:
        threshold = min(samples) + (max(samples) - min(samples)) / 2.0
    edges: List[Dict] = []
    for i in range(1, len(samples)):
        prev = samples[i - 1]
        cur = samples[i]
        if prev < threshold <= cur:
            edges.append({"index": i, "t_s": t0_s + i * dt_s, "polarity": "rising"})
        elif prev >= threshold > cur:
            edges.append({"index": i, "t_s": t0_s + i * dt_s, "polarity": "falling"})
    return edges


def _nearest_ch1_edge(t_s: float, edges: List[Dict]) -> Optional[Dict]:
    best = None
    best_dt = math.inf
    for e in edges:
        d = abs(e["t_s"] - t_s)
        if d < best_dt:
            best_dt = d
            best = e
    return best


# ---------------------------------------------------------------------------
# Top-level pipeline
# ---------------------------------------------------------------------------

def decode_pod(pod_path: str, header_path: str,
               ch1_path: Optional[str] = None) -> Dict:
    """Decode a full capture and return a structured result dict.

    ``pod_path``    -- ``pod1.raw`` (ASCII comma-separated 8-bit POD values)
    ``header_path`` -- ``ch1.header.raw`` (shared time base)
    ``ch1_path``    -- optional ``ch1.raw`` for same-acquisition correlation
    """
    samples = read_pod_samples(pod_path)
    start, stop, count, dt_s = read_header(header_path)
    if len(samples) != count:
        raise DecoderError(
            f"pod sample count {len(samples)} != header count {count}"
        )
    dt_ns = dt_s * 1e9

    result: Dict = {
        "pod_path": pod_path,
        "header_path": header_path,
        "sample_count": len(samples),
        "dt_s": dt_s,
        "dt_ns": dt_ns,
        "t0_s": start,
        "t_stop_s": stop,
        "nominal_baud": NOMINAL_BAUD,
        "nominal_bit_ns": NOMINAL_BIT_NS,
        "directions": {},
    }

    channel_map = {
        DIRECTION_REQUEST: (BIT_REQUEST, REQUEST_DATA_BITS),
        DIRECTION_RESPONSE: (BIT_RESPONSE, RESPONSE_DATA_BITS),
    }

    for direction, (bit, data_bits) in channel_map.items():
        stream = extract_bit(samples, bit)
        period = measure_bit_period(stream)
        t_samples = period.get("fitted_period_samples")
        period["fitted_period_ns"] = (t_samples * dt_ns) if t_samples is not None else None
        period["nominal_period_samples"] = NOMINAL_BIT_NS / dt_ns if dt_ns else None

        frames: List[Frame] = []
        if t_samples is not None:
            frames = decode_stream(stream, t_samples, dt_s, direction, start, data_bits)
        telegrams = group_telegrams(frames, direction, data_bits,
                                    t_samples or 5.0, len(stream))

        # Frame-spacing derived period (start-bit to start-bit), reliable where
        # frames are back-to-back (the request).  Confounded by inter-byte gaps
        # on the response, so reported alongside the run-length estimate.
        frame_spacing = None
        if len(frames) >= 2:
            spacings = [frames[k + 1].start_index - frames[k].start_index
                        for k in range(len(frames) - 1)]
            total_bits = data_bits + 2
            median_spacing = float(sorted(spacings)[len(spacings) // 2])
            frame_spacing = median_spacing / total_bits

        result["directions"][direction] = {
            "bit": bit,
            "data_bits": data_bits,
            "frame_format": "9-bit (address marker)" if data_bits >= 9 else "8-bit",
            "stream_len": len(stream),
            "bit_period": period,
            "frame_spacing_period_samples": frame_spacing,
            "frame_count": len(frames),
            "frames": frames,
            "telegrams": telegrams,
        }

    if ch1_path is not None:
        ch1 = read_ch1(ch1_path)
        edges = ch1_edges(ch1, dt_s, start)
        # Correlate each decoded frame with the nearest CH1 edge (same time
        # base; advisory only).
        for direction, d in result["directions"].items():
            for f in d["frames"]:
                nearest = _nearest_ch1_edge(f.t_start_s, edges)
                if nearest is not None:
                    f.ch1_nearest = {  # type: ignore[attr-defined]
                        "delta_s": f.t_start_s - nearest["t_s"],
                        "polarity": nearest["polarity"],
                    }
        result["ch1"] = {
            "sample_count": len(ch1),
            "edges": edges,
        }

    result["summary"] = _summarize(result)
    return result


def _summarize(result: Dict) -> Dict:
    summary: Dict = {"directions": {}}
    for direction, d in result["directions"].items():
        tgs = d["telegrams"]
        summary["directions"][direction] = {
            "frame_count": d["frame_count"],
            "telegram_count": len(tgs),
            "crc_statuses": [t.crc_status for t in tgs],
            "addresses": [t.address for t in tgs],
            "functions": [t.function for t in tgs],
        }
    return summary


def format_report(result: Dict) -> str:
    """Render a human-readable decoding report."""
    lines: List[str] = []
    dt_ns = result["dt_ns"]
    lines.append(f"Samples: {result['sample_count']}  "
                 f"dt={dt_ns:.3f} ns  window=[{result['t0_s']*1e6:.2f}, "
                 f"{result['t_stop_s']*1e6:.2f}] us")
    lines.append(f"Nominal baud={result['nominal_baud']:.4f} Mbaud -> "
                 f"bit={result['nominal_bit_ns']:.1f} ns "
                 f"({result['nominal_bit_ns']/dt_ns:.3f} samples)")

    for direction, d in result["directions"].items():
        bp = d["bit_period"]
        ft = bp.get("fitted_period_samples")
        fn = bp.get("fitted_period_ns")
        fs = d.get("frame_spacing_period_samples")
        lines.append("")
        lines.append(f"== {direction} (bit {d['bit']}, {d['frame_format']}) ==")
        if ft is not None:
            lines.append(
                f"   measured bit period: {ft:.3f} samples = {fn:.1f} ns "
                f"(nominal {result['nominal_bit_ns']:.1f} ns, "
                f"{bp['nominal_period_samples']:.3f} samples)"
            )
        else:
            lines.append("   measured bit period: UNABLE (no edges)")
        if fs is not None:
            lines.append(f"   single-bit mode: {bp.get('single_bit_mode')} samples"
                         f"   frame-spacing period: {fs:.3f} samples")
        else:
            lines.append(f"   single-bit mode: {bp.get('single_bit_mode')} samples")
        lines.append(f"   frames decoded: {d['frame_count']}")
        hist = bp.get("short_spacing_histogram") or {}
        if hist:
            hist_s = ", ".join(f"{k}:{v}" for k, v in sorted(hist.items()))
            lines.append(f"   short edge-spacing histogram (samples): {hist_s}")

        for i, t in enumerate(d["telegrams"]):
            lines.append(_format_telegram(t, i, dt_ns))

    if "ch1" in result:
        edges = result["ch1"]["edges"]
        lines.append("")
        lines.append(f"== CH1 (analog) ==  {len(edges)} threshold crossings detected")
        rising = [e for e in edges if e["polarity"] == "rising"]
        falling = [e for e in edges if e["polarity"] == "falling"]
        lines.append(f"   rising edges: {len(rising)}  falling edges: {len(falling)}")
        for e in edges[:12]:
            lines.append(f"   {e['polarity']:>7} @ {e['t_s']*1e6:+.3f} us")

    return "\n".join(lines)


def _format_telegram(t: Telegram, idx: int, dt_ns: float) -> str:
    head = f"  telegram {idx}  frames={len(t.frames)}  status={t.crc_status}"
    if t.frames:
        f0 = t.frames[0]
        head += f"  start@{f0.t_start_s*1e6:+.3f}us"
        if hasattr(f0, "ch1_nearest"):
            n = f0.ch1_nearest
            head += f"  dCH1={n['delta_s']*1e6:+.3f}us({n['polarity']})"
    if t.address is not None:
        head += f"  addr={t.address}"
    if t.function is not None:
        head += f"  fn=0x{t.function:02X}({t.function_name or '?'})"
    if t.register_address is not None:
        head += f"  reg={t.register_address}"
    if t.quantity is not None:
        head += f"  qty={t.quantity}"
    if t.byte_count is not None:
        head += f"  bytecount={t.byte_count}"
    if t.values:
        head += "  values=" + ",".join(str(v) for v in t.values)
    if t.crc_received[0] is not None:
        head += f"  crc=[0x{t.crc_received[0]:02X},0x{t.crc_received[1]:02X}]"
        head += f"  (siebwalde={t.crc_siebwalde_ok}, standard={t.crc_standard_ok})"
    if t.note:
        head += f"  note={t.note!r}"
    bytes_hex = " ".join(f"{b:02X}" for b in t.data_bytes)
    head += f"\n        bytes: {bytes_hex}"
    if t.frames and t.frames[0].ninth is not None:
        nines = "".join(str(f.ninth) for f in t.frames)
        head += f"\n        ninth: {nines} (1=address)"
    return head
