"""Offline 8-bit / 9-bit UART frame decoder (standalone, no MCP dependency).

Consumes a raw digital line (a NumPy bool/int array of 0/1 samples) plus the
integer ``sample_interval_ns`` and an explicit :class:`UartConfig`. It decodes
UART frames by edge extraction + start-bit detection + bit-center sampling and
reports per-frame errors (bad stop bit, truncation) without silently guessing a
mode: the caller must pass an explicit ``mode`` (``"8bit"`` / ``"9bit"``) or mark
``mode_source="verified_marker"``; any ambiguity is reported as uncertainty in
the result.

Wire framing (PROVEN from the Siebwalde firmware source -- see
``docs/PICOSCOPE-PICO-4-SIGNAL-ANALYSIS.md``):

* Master request  -- 9-bit: ``[START=0][D0..D7][D8=address marker][STOP=1]``,
  D8=1 on the address byte, D8=0 on all following bytes.
* PIC18 reply    -- 8-bit: ``[START=0][D0..D7][STOP=1]`` (TX9=0: no 9th bit).
* Bootloader     -- 8-bit both directions.

The decoder samples each bit at its cell center (majority of 3 adjacent
samples for robustness against +/-1 sample jitter) and preserves the full 9-bit
value AND a separate ninth-bit flag for 9-bit frames.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import List, Optional, Sequence

import numpy as np

from .errors import InvalidAnalysisConfig

#: Accepted frame modes. ``"8bit"`` = 8 data bits (no ninth bit), ``"9bit"`` =
#: 8 data bits + a ninth address/marker bit.
VALID_MODES = ("8bit", "9bit")

#: Accepted idle polarities.
VALID_POLARITIES = ("idle_high", "idle_low")

#: Accepted mode-source markers. ``"explicit"`` is a caller decision;
#: ``"verified_marker"`` means the caller has separately verified the mode.
VALID_MODE_SOURCES = ("explicit", "verified_marker")


@dataclass
class UartConfig:
    """Explicit UART decode configuration.

    Attributes:
        mode: ``"8bit"`` or ``"9bit"`` (required; never guessed).
        baud: integer baud rate (bit/s). Required.
        sample_interval_ns: integer sample interval of the capture (ns). Required.
        polarity: ``"idle_high"`` (idle=1, start=0) or ``"idle_low"``.
        lsb_first: data bits LSB-first (always True for this project).
        stop_bits: number of stop bits (1 for this project).
        mode_source: how the mode was established (see above).
        idle_gap_bits: optional idle-gap grouping threshold in bit periods
            (frames separated by more than this are split into groups). None
            disables grouping.
    """

    mode: str
    baud: int
    sample_interval_ns: int
    polarity: str = "idle_high"
    lsb_first: bool = True
    stop_bits: int = 1
    mode_source: str = "explicit"
    idle_gap_bits: Optional[float] = None

    def validate(self) -> None:
        if self.mode not in VALID_MODES:
            raise InvalidAnalysisConfig(
                f"mode must be one of {VALID_MODES}, got {self.mode!r}"
            )
        if self.polarity not in VALID_POLARITIES:
            raise InvalidAnalysisConfig(
                f"polarity must be one of {VALID_POLARITIES}, got {self.polarity!r}"
            )
        if self.mode_source not in VALID_MODE_SOURCES:
            raise InvalidAnalysisConfig(
                f"mode_source must be one of {VALID_MODE_SOURCES}, got {self.mode_source!r}"
            )
        if not isinstance(self.baud, int) or self.baud <= 0:
            raise InvalidAnalysisConfig(f"baud must be a positive int, got {self.baud!r}")
        if not isinstance(self.sample_interval_ns, int) or self.sample_interval_ns <= 0:
            raise InvalidAnalysisConfig(
                f"sample_interval_ns must be a positive int, got {self.sample_interval_ns!r}"
            )
        if not self.lsb_first:
            raise InvalidAnalysisConfig("Only LSB-first UART is supported")
        if not isinstance(self.stop_bits, int) or self.stop_bits < 1:
            raise InvalidAnalysisConfig(f"stop_bits must be >= 1, got {self.stop_bits!r}")
        if self.idle_gap_bits is not None and self.idle_gap_bits <= 0:
            raise InvalidAnalysisConfig(f"idle_gap_bits must be > 0, got {self.idle_gap_bits!r}")

    @property
    def data_bits(self) -> int:
        return 9 if self.mode == "9bit" else 8

    @property
    def samples_per_bit(self) -> float:
        """Nominal samples per bit cell at the configured baud + sample interval."""
        return 1e9 / float(self.baud) / float(self.sample_interval_ns)

    @property
    def total_bits(self) -> int:
        """Start + data + stop bits per frame."""
        return 1 + self.data_bits + self.stop_bits

    @property
    def idle_level(self) -> int:
        return 1 if self.polarity == "idle_high" else 0

    @property
    def start_level(self) -> int:
        return 1 - self.idle_level

    def to_dict(self) -> dict:
        return {
            "mode": self.mode,
            "baud": self.baud,
            "sample_interval_ns": self.sample_interval_ns,
            "polarity": self.polarity,
            "lsb_first": self.lsb_first,
            "stop_bits": self.stop_bits,
            "mode_source": self.mode_source,
            "idle_gap_bits": self.idle_gap_bits,
        }


@dataclass
class UartFrame:
    """One decoded UART frame (word)."""

    index: int
    start_index: int  # sample index of the leading edge of the start bit
    t_start_ns: float  # timestamp of the start bit (sample grid)
    data_bits: List[int]  # sampled data bit values (0/1), length = data_bits
    value: int  # 8 data bits (D0..D7) as an int
    ninth_bit: Optional[int]  # separate ninth-bit flag (None in 8-bit mode)
    raw_value: int  # full value incl. ninth bit: (ninth<<8)|value in 9-bit mode
    bit_center_indices: List[int]  # sample indices used for the data bits
    stop_center_index: int  # sample index used for the (first) stop bit
    stop_ok: bool
    truncated: bool
    frame_error: bool
    errors: List[str] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "index": self.index,
            "start_index": self.start_index,
            "t_start_ns": self.t_start_ns,
            "data_bits": list(self.data_bits),
            "value": self.value,
            "ninth_bit": self.ninth_bit,
            "raw_value": self.raw_value,
            "bit_center_indices": list(self.bit_center_indices),
            "stop_center_index": self.stop_center_index,
            "stop_ok": self.stop_ok,
            "truncated": self.truncated,
            "frame_error": self.frame_error,
            "errors": list(self.errors),
        }


@dataclass
class Edge:
    """One signal edge (transition between adjacent samples)."""

    index: int  # sample index where the new level first appears
    kind: str  # "rising" (0->1) or "falling" (1->0)


def extract_edges(samples: Sequence[int]) -> List[Edge]:
    """Return all rising/falling edges in a 0/1 sample stream.

    ``samples`` may be any sequence of ints/bools. ``Edge.index`` is the sample
    index at which the new level first appears.
    """
    arr = np.asarray(samples, dtype=np.int64)
    edges: List[Edge] = []
    for i in range(1, len(arr)):
        prev, cur = arr[i - 1], arr[i]
        if prev == 0 and cur == 1:
            edges.append(Edge(index=i, kind="rising"))
        elif prev == 1 and cur == 0:
            edges.append(Edge(index=i, kind="falling"))
    return edges


def _sample_bit(arr: np.ndarray, center: float) -> int:
    """Majority-of-3 sample at ``center`` (rounds to nearest int index)."""
    c = int(round(center))
    total = 0
    count = 0
    for off in (-1, 0, 1):
        idx = c + off
        if 0 <= idx < len(arr):
            total += int(arr[idx])
            count += 1
    if count == 0:
        return 1  # out of range -> conservative idle/high
    return 1 if total * 2 >= count else 0


def _try_decode_frame(
    arr: np.ndarray,
    start_index: int,
    cfg: UartConfig,
    word_index: int,
) -> Optional[UartFrame]:
    spp = cfg.samples_per_bit
    data_bits = cfg.data_bits
    stop_bits = cfg.stop_bits
    n = len(arr)

    # Start-bit sanity: the start cell center must sit at the start level.
    # This rejects short noise pulses whose line already returned to idle.
    start_center = start_index + 0.5 * spp
    if _sample_bit(arr, start_center) != cfg.start_level:
        return None

    # Data bit centers: bit k occupies [k+1, k+2) cell times after the start edge.
    centers = [start_index + (k + 1.5) * spp for k in range(data_bits)]
    # First stop-bit center: after start + data bits.
    stop_center = start_index + (data_bits + 1.5) * spp

    center_indices = [int(round(c)) for c in centers]
    stop_center_index = int(round(stop_center))

    bits = [_sample_bit(arr, c) for c in centers]

    # Data value from D0..D7.
    value = 0
    for k in range(8):
        value |= bits[k] << k
    ninth = bits[8] if data_bits >= 9 else None

    errors: List[str] = []
    stop_ok = True
    truncated = False
    # Sample stop bits; a stop-bit center past the array end means the frame is
    # truncated by the capture.
    for s in range(stop_bits):
        sc = start_index + (data_bits + 1 + s + 0.5) * spp
        if int(round(sc)) >= n:
            truncated = True
            break
        if _sample_bit(arr, sc) != cfg.idle_level:
            stop_ok = False
            errors.append("stop_bit")
            break

    if truncated:
        errors.append("truncated")

    return UartFrame(
        index=word_index,
        start_index=start_index,
        t_start_ns=float(start_index) * float(cfg.sample_interval_ns),
        data_bits=bits,
        value=value,
        ninth_bit=ninth,
        raw_value=(value | (ninth << 8)) if ninth is not None else value,
        bit_center_indices=center_indices,
        stop_center_index=stop_center_index,
        stop_ok=stop_ok,
        truncated=truncated,
        frame_error=(not stop_ok) or truncated,
        errors=errors,
    )


def decode_uart(samples: Sequence[int], config: UartConfig) -> dict:
    """Decode all UART frames in a digital line and return a result dict.

    The result dict (JSON-serializable) contains the config, edge counts, the
    decoded frames, optional idle-gap groups, and a list of uncertainty notes.
    ``samples`` is a NumPy bool/int array (or any 0/1 sequence) per line.
    """
    config.validate()
    arr = np.asarray(samples, dtype=np.int64)
    arr = (arr != 0).astype(np.int64)  # normalize bool/0/1 -> 0/1

    edges = extract_edges(arr)
    falling = [e.index for e in edges if e.kind == "falling"]
    rising = [e.index for e in edges if e.kind == "rising"]
    start_edges = rising if config.start_level == 1 else falling

    frames: List[UartFrame] = []
    n = len(arr)
    i = 1
    frame_span = max(1, int(round(config.total_bits * config.samples_per_bit)))
    while i < n:
        if arr[i - 1] == config.idle_level and arr[i] == config.start_level:
            frame = _try_decode_frame(arr, i, config, len(frames))
            if frame is not None:
                frames.append(frame)
                # Skip past this frame so in-frame edges aren't re-read as starts.
                i = frame.start_index + frame_span
                continue
        i += 1

    groups = None
    if config.idle_gap_bits is not None and frames:
        groups = _group_by_idle_gap(frames, config)

    uncertainty: List[str] = []
    if config.mode_source == "explicit":
        uncertainty.append(
            f"mode={config.mode!r} supplied explicitly by the caller; the mode was "
            "not independently verified against a mode marker"
        )
    if config.samples_per_bit < 3.0:
        uncertainty.append(
            f"only {config.samples_per_bit:.2f} samples/bit; below the ~3 sample/bit "
            "reliability floor -- decode is resolution-limited"
        )

    return {
        "config": config.to_dict(),
        "sample_interval_ns": config.sample_interval_ns,
        "samples": int(len(arr)),
        "edge_count": len(edges),
        "rising_edge_count": len(rising),
        "falling_edge_count": len(falling),
        "samples_per_bit": config.samples_per_bit,
        "nominal_bit_ns": 1e9 / float(config.baud),
        "frames": [f.to_dict() for f in frames],
        "groups": groups,
        "uncertainty": uncertainty,
        "summary": {
            "frame_count": len(frames),
            "good_frames": sum(1 for f in frames if not f.frame_error),
            "error_frames": sum(1 for f in frames if f.frame_error),
            "truncated_frames": sum(1 for f in frames if f.truncated),
            "ninth_set": sum(1 for f in frames if f.ninth_bit == 1),
            "ninth_clear": sum(1 for f in frames if f.ninth_bit == 0),
        },
    }


def decode_uart_8bit(samples: Sequence[int], baud: int, sample_interval_ns: int, **kwargs) -> dict:
    """Convenience wrapper: decode in explicit 8-bit mode."""
    return decode_uart(samples, UartConfig(mode="8bit", baud=baud, sample_interval_ns=sample_interval_ns, **kwargs))


def decode_uart_9bit(samples: Sequence[int], baud: int, sample_interval_ns: int, **kwargs) -> dict:
    """Convenience wrapper: decode in explicit 9-bit mode."""
    return decode_uart(samples, UartConfig(mode="9bit", baud=baud, sample_interval_ns=sample_interval_ns, **kwargs))


def _group_by_idle_gap(frames: List[UartFrame], cfg: UartConfig) -> List[dict]:
    """Group frames on idle gaps above ``idle_gap_bits`` (bit periods)."""
    gap_samples = cfg.idle_gap_bits * cfg.samples_per_bit
    groups: List[List[UartFrame]] = [[frames[0]]]
    for prev, cur in zip(frames, frames[1:]):
        gap = cur.start_index - prev.start_index
        if gap > gap_samples:
            groups.append([cur])
        else:
            groups[-1].append(cur)
    out = []
    for g in groups:
        out.append({
            "start_index": g[0].start_index,
            "end_index": g[-1].start_index,
            "word_count": len(g),
            "words": [f.value for f in g],
            "ninth_bits": [f.ninth_bit for f in g],
        })
    return out


def compare_tx_rx(
    tx_frames: Sequence[dict],
    rx_frames: Sequence[dict],
    window_ns: Optional[float] = None,
) -> List[dict]:
    """Compare TX and RX frame timing (same shared sample grid only).

    ``tx_frames``/``rx_frames`` are decoded frame dicts (as returned by
    :func:`decode_uart`), each with ``index`` and ``t_start_ns`` keys. For each
    TX frame, find the nearest RX frame whose start is at or after the TX frame
    start (or within ``window_ns`` if given) and report the delta. This is an
    advisory, same-acquisition time-base comparison -- it makes no physical
    round-trip or protocol-timing claim.
    """
    def _t(f: dict) -> float:
        return float(f["t_start_ns"])

    def _i(f: dict) -> int:
        return int(f["index"])

    pairs: List[dict] = []
    rx_sorted = sorted(rx_frames, key=_t)
    for tx in tx_frames:
        candidates = [r for r in rx_sorted if _t(r) >= _t(tx)]
        if window_ns is not None:
            candidates = [r for r in candidates if (_t(r) - _t(tx)) <= window_ns]
        if not candidates:
            pairs.append({"tx_index": _i(tx), "rx_index": None, "delta_ns": None})
            continue
        nearest = min(candidates, key=lambda r: _t(r) - _t(tx))
        pairs.append({
            "tx_index": _i(tx),
            "tx_start_ns": _t(tx),
            "rx_index": _i(nearest),
            "rx_start_ns": _t(nearest),
            "delta_ns": _t(nearest) - _t(tx),
        })
    return pairs
