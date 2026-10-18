"""READ-ONLY instrument inventory.

Queries instrument identity, acquisition state/rate, timebase, trigger,
channels (state/coupling/scale/offset/probe), POD threshold, and BUS/UART
configuration. No settings are changed: every command issued here ends in
``?`` and is classified ``VERIFIED_QUERY`` or ``MANUAL_QUERY`` in
:mod:`scpi`.

A single inventory result entry is a dict:

  {"cmd": ..., "raw": ..., "value": ..., "ok": bool, "error": str|None}

so callers can distinguish a clean value from a failed/empty query.
"""

from __future__ import annotations

from typing import Dict, List, Optional

import scpi
from transport import HmoTransport, TransportError

# Static query set. The <m> placeholders are expanded for channels 1..4.
_STATIC_QUERIES = [
    ("idn", "*IDN?"),
    ("opc", "*OPC?"),
    ("acq_state", "ACQ:STAT?"),
    ("acq_srate", "ACQ:SRAT?"),
    ("time_scale", "TIM:SCAL?"),
    ("time_pos", "TIM:POS?"),
    ("trig_mode", "TRIG:A:MODE?"),
    ("trig_source", "TRIG:A:SOUR?"),
    ("trig_type", "TRIG:A:TYPE?"),
    ("trig_level", "TRIG:A:LEV?"),
    ("pod1_threshold", "POD1:THR?"),
    ("bus1_type", "BUS1:TYPE?"),
    ("bus2_type", "BUS2:TYPE?"),
    ("bus2_uart_baud", "BUS2:UART:BAUD?"),
    ("bus2_uart_ssiz", "BUS2:UART:SSIZ?"),
    ("bus2_uart_par", "BUS2:UART:PAR?"),
    ("bus2_uart_sbit", "BUS2:UART:SBIT?"),
    ("bus2_uart_source", "BUS2:UART:DATA:SOUR?"),
]

_CHANNEL_QUERY_TEMPLATES = [
    ("stat", "CHAN{m}:STAT?"),
    ("coup", "CHAN{m}:COUP?"),
    ("scal", "CHAN{m}:SCAL?"),
    ("offs", "CHAN{m}:OFFS?"),
    ("probe_att", "PROB{m}:SET:ATT?"),
]

_CHANNEL_COUNT = 4


def build_query_list() -> List[tuple]:
    """Return ``(key, command)`` pairs for a full read-only inventory."""
    queries: List[tuple] = list(_STATIC_QUERIES)
    for m in range(1, _CHANNEL_COUNT + 1):
        for key, tpl in _CHANNEL_QUERY_TEMPLATES:
            queries.append((f"ch{m}_{key}", tpl.format(m=m)))
    return queries


def _value_for(key: str, raw: str):
    """Best-effort typed value; numeric strings become floats."""
    if raw is None:
        return None
    s = raw.strip()
    if s == "":
        return ""
    try:
        return float(s)
    except ValueError:
        return s


def run_inventory(
    transport: HmoTransport,
    timeout: Optional[float] = None,
) -> Dict[str, dict]:
    """Run the full read-only inventory. Never writes a setting."""
    from transport import DEFAULT_QUERY_TIMEOUT

    t = timeout if timeout is not None else DEFAULT_QUERY_TIMEOUT
    results: Dict[str, dict] = {}
    for key, cmd in build_query_list():
        entry = {"cmd": cmd, "raw": None, "value": None, "ok": False, "error": None}
        try:
            raw = transport.query(cmd, timeout=t)
            entry["raw"] = raw
            entry["value"] = _value_for(key, raw)
            entry["ok"] = raw != ""
        except (TransportError, UnicodeDecodeError) as exc:
            entry["error"] = f"{type(exc).__name__}: {exc}"
        results[key] = entry
    return results


def inventory_summary(results: Dict[str, dict]) -> Dict[str, object]:
    """Compact human/JSON-friendly projection of an inventory."""
    def g(key, default=None):
        e = results.get(key)
        if not e or not e["ok"]:
            return default
        return e["value"]

    channels = {}
    for m in range(1, _CHANNEL_COUNT + 1):
        channels[str(m)] = {
            "state": g(f"ch{m}_stat"),
            "coupling": g(f"ch{m}_coup"),
            "scale_v_per_div": g(f"ch{m}_scal"),
            "offset_v": g(f"ch{m}_offs"),
            "probe_attenuation": g(f"ch{m}_probe_att"),
        }

    return {
        "identity": g("idn"),
        "opc": g("opc"),
        "acquisition": {"state": g("acq_state"), "sample_rate": g("acq_srate")},
        "timebase": {"scale_s_per_div": g("time_scale"), "position_s": g("time_pos")},
        "trigger": {
            "mode": g("trig_mode"),
            "source": g("trig_source"),
            "type": g("trig_type"),
            "level_v": g("trig_level"),
        },
        "logic_pod": {"threshold": g("pod1_threshold")},
        "bus": {
            "bus1_type": g("bus1_type"),
            "bus2": {
                "type": g("bus2_type"),
                "uart": {
                    "baud": g("bus2_uart_baud"),
                    "symbol_size_bits": g("bus2_uart_ssiz"),
                    "parity": g("bus2_uart_par"),
                    "stop_bits": g("bus2_uart_sbit"),
                    "source": g("bus2_uart_source"),
                },
            },
        },
        "channels": channels,
    }
