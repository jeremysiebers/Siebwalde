"""SCPI command registry and response parsers for the HMO2024.

Every command the tool may issue is listed here with:

* its short form (what we send),
* its long form (from the HMO72x/202x SCPI Programmer's Manual),
* a status,
* a role.

Status values:

* ``VERIFIED_QUERY`` -- confirmed working on the live instrument (COM3),
* ``VERIFIED_EMPTY`` -- confirmed EMPTY/unsupported on the live instrument
  (documented here so nobody relies on it),
* ``MANUAL_QUERY``   -- documented in the manual as query-only; syntax confirmed
  against the manual but NOT yet re-verified live in this increment,
* ``MANUAL_EVENT``   -- documented in the manual, side-effecting (event),
* ``MANUAL_SETTING`` -- documented in the manual, changes an instrument setting.

No command may be added without a manual reference or a live verification.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Dict, List, Optional


# ---------------------------------------------------------------------------
# Status constants
# ---------------------------------------------------------------------------
VERIFIED_QUERY = "VERIFIED_QUERY"
VERIFIED_EMPTY = "VERIFIED_EMPTY"
MANUAL_QUERY = "MANUAL_QUERY"
MANUAL_EVENT = "MANUAL_EVENT"
MANUAL_SETTING = "MANUAL_SETTING"


@dataclass(frozen=True)
class ScpiCommand:
    short: str
    long: str
    status: str
    role: str
    note: str = ""

    @property
    def is_query(self) -> bool:
        return self.short.endswith("?")

    @property
    def is_side_effecting(self) -> bool:
        return self.status in (MANUAL_EVENT, MANUAL_SETTING)


# ---------------------------------------------------------------------------
# The command registry. Order is significant for documentation.
# ---------------------------------------------------------------------------
COMMANDS: List[ScpiCommand] = [
    # -- IEEE 488.2 common ------------------------------------------------
    ScpiCommand("*IDN?", "*IDN?", VERIFIED_QUERY, "identity",
                "HAMEG,HMO2024,<serial>,<fw>"),
    ScpiCommand("*OPC?", "*OPC?", VERIFIED_QUERY, "completion",
                "returns 1 after prior commands complete"),
    # -- acquisition ------------------------------------------------------
    ScpiCommand("ACQ:STAT?", "ACQuire:STATe?", VERIFIED_QUERY, "acquisition-state",
                "RUN|STOP|COMPlete|BREak"),
    ScpiCommand("ACQ:SRAT?", "ACQuire:SRATe?", VERIFIED_QUERY, "sample-rate",
                "samples/second"),
    ScpiCommand("ACQ:POIN?", "ACQuire:POINts?", VERIFIED_EMPTY, "acquisition-points",
                "EMPTY on live instrument; use CHAN1:DATA:POIN? instead"),
    # -- time base --------------------------------------------------------
    ScpiCommand("TIM:SCAL?", "TIMebase:SCALe?", VERIFIED_QUERY, "timebase",
                "seconds/div"),
    ScpiCommand("TIM:POS?", "TIMebase:POSition?", VERIFIED_QUERY, "timebase",
                "trigger position offset"),
    # -- trigger ----------------------------------------------------------
    ScpiCommand("TRIG:A:MODE?", "TRIGger:A:MODE?", VERIFIED_QUERY, "trigger",
                "AUTO|NORMal"),
    ScpiCommand("TRIG:A:SOUR?", "TRIGger:A:SOURce?", VERIFIED_QUERY, "trigger",
                "CH1..4|D0..7|BUS1|BUS2|EXT|LINE|NONe|PATTern"),
    ScpiCommand("TRIG:A:TYPE?", "TRIGger:A:TYPE?", VERIFIED_QUERY, "trigger",
                "EDGE|WIDTh|TV|LOGic|BUS"),
    ScpiCommand("TRIG:A:LEV?", "TRIGger:A:LEVel1:VALue?", VERIFIED_QUERY, "trigger",
                "trigger threshold voltage"),
    ScpiCommand("TRIG:A:EDGE?", "TRIGger:A:EDGE?", VERIFIED_EMPTY, "trigger",
                "EMPTY on live instrument; EDGE has sub-commands SLOPe/COUPling"),
    # -- channels ---------------------------------------------------------
    ScpiCommand("CHAN<m>:STAT?", "CHANnel<m>:STATe?", VERIFIED_QUERY, "channel",
                "ON|OFF (m=1..4)"),
    ScpiCommand("CHAN<m>:COUP?", "CHANnel<m>:COUPling?", VERIFIED_QUERY, "channel",
                "coupling (m=1..4)"),
    ScpiCommand("CHAN<m>:SCAL?", "CHANnel<m>:SCALe?", VERIFIED_QUERY, "channel",
                "volts/div (m=1..4)"),
    ScpiCommand("CHAN<m>:OFFS?", "CHANnel<m>:OFFSet?", VERIFIED_QUERY, "channel",
                "offset volts (m=1..4)"),
    ScpiCommand("CHAN1:PROB?", "CHANnel1:PROBe?", VERIFIED_EMPTY, "probe",
                "EMPTY on live instrument; use PROB1:SET:ATT? instead"),
    # -- probes -----------------------------------------------------------
    ScpiCommand("PROB<m>:SET:ATT?",
                "PROBe<m>:SETup:ATTenuation[:AUTO]?",
                VERIFIED_QUERY, "probe", "probe attenuation (m=1..4)"),
    # -- logic pod --------------------------------------------------------
    ScpiCommand("POD1:THR?", "POD<p>:THReshold?", VERIFIED_QUERY, "logic-pod",
                "logic threshold mode"),
    # -- serial bus -------------------------------------------------------
    ScpiCommand("BUS1:TYPE?", "BUS<b>:TYPE?", VERIFIED_QUERY, "bus",
                "serial bus protocol type"),
    ScpiCommand("BUS2:TYPE?", "BUS<b>:TYPE?", VERIFIED_QUERY, "bus",
                "serial bus protocol type"),
    ScpiCommand("BUS2:UART:BAUD?", "BUS<b>:UART:BAUDrate?", VERIFIED_QUERY, "uart",
                "UART baud rate"),
    ScpiCommand("BUS2:UART:SSIZ?", "BUS<b>:UART:SSIZe?", VERIFIED_QUERY, "uart",
                "UART symbol size (bits)"),
    ScpiCommand("BUS2:UART:PAR?", "BUS<b>:UART:PARity?", VERIFIED_QUERY, "uart",
                "UART parity"),
    ScpiCommand("BUS2:UART:SBIT?", "BUS<b>:UART:SBIT?", VERIFIED_QUERY, "uart",
                "UART stop bits"),
    ScpiCommand("BUS2:UART:SOUR?", "BUS<b>:UART:SOURce?", VERIFIED_EMPTY, "uart",
                "EMPTY on live instrument; use BUS2:UART:DATA:SOUR? instead"),
    ScpiCommand("BUS2:UART:DATA:SOUR?", "BUS<b>:UART:DATA:SOURce?",
                VERIFIED_QUERY, "uart", "UART data source channel"),
    # -- waveform data ----------------------------------------------------
    ScpiCommand("CHAN1:DATA?", "CHANnel<m>:DATA?", VERIFIED_QUERY, "waveform",
                "ASCII list of vertical values, LF-terminated"),
    ScpiCommand("CHAN1:DATA:HEAD?", "CHANnel<m>:DATA:HEADer?", VERIFIED_QUERY,
                "waveform", "XStart,XStop,RecordLength,ValuesPerSample"),
    ScpiCommand("CHAN1:DATA:POIN?", "CHANnel<m>:DATA:POINts?", VERIFIED_QUERY,
                "waveform", "number of data points"),
    # -- hardcopy ---------------------------------------------------------
    ScpiCommand("HCOP:DATA?", "HCOPy:DATA?", VERIFIED_QUERY, "hardcopy",
                "screenshot as definite-length binary block (BMP)"),
    # -- acquisition control (side-effecting; used ONLY by explicit capture)
    ScpiCommand("SINGle", "SINGle / RUNSingle", MANUAL_EVENT, "acquisition-control",
                "starts a defined number of acquisition cycles (async)"),
    ScpiCommand("RUN", "RUN / RUNContinous", MANUAL_EVENT, "acquisition-control",
                "starts continuous acquisition (async)"),
    ScpiCommand("STOP", "STOP", MANUAL_EVENT, "acquisition-control",
                "stops running acquisition (async)"),
    ScpiCommand("ACQ:STAT", "ACQuire:STATe", MANUAL_SETTING, "acquisition-control",
                "set acquisition state (RUN|STOP|COMPlete|BREak)"),
]


_BY_SHORT: Dict[str, ScpiCommand] = {c.short: c for c in COMMANDS}


def get_command(short: str) -> ScpiCommand:
    return _BY_SHORT[short]


def is_query_command(short: str) -> bool:
    # Every SCPI query ends in '?'; every event/setting command does not.
    return short.endswith("?")


# ---------------------------------------------------------------------------
# Response parsers
# ---------------------------------------------------------------------------
@dataclass
class Header:
    start: float
    stop: float
    count: int
    values_per_sample: int

    @property
    def dt(self) -> float:
        """Sample interval derived from start/stop/count."""
        if self.count <= 1:
            return 0.0
        return (self.stop - self.start) / (self.count - 1)


class ScpiParseError(ValueError):
    pass


def parse_float(text: str) -> float:
    try:
        return float(text.strip())
    except (ValueError, TypeError) as exc:
        raise ScpiParseError(f"non-numeric value {text!r}") from exc


def parse_header(text: str) -> Header:
    """Parse ``CHAN1:DATA:HEAD?`` -> ``<start>,<stop>,<count>,<format>``."""
    parts = [p.strip() for p in text.split(",")]
    if len(parts) != 4:
        raise ScpiParseError(
            f"header expected 4 comma-separated fields, got {len(parts)}: {text!r}"
        )
    start = parse_float(parts[0])
    stop = parse_float(parts[1])
    try:
        count = int(parts[2])
    except ValueError as exc:
        raise ScpiParseError(f"header record-length not an integer: {parts[2]!r}") from exc
    try:
        vps = int(parts[3])
    except ValueError as exc:
        raise ScpiParseError(f"header values-per-sample not an integer: {parts[3]!r}") from exc
    if count < 0:
        raise ScpiParseError(f"header record-length negative: {count}")
    if vps < 1:
        raise ScpiParseError(f"header values-per-sample must be >=1: {vps}")
    return Header(start=start, stop=stop, count=count, values_per_sample=vps)


def parse_float_list(text: str) -> List[float]:
    """Parse an ASCII waveform ``CHAN1:DATA?`` into floats.

    Tolerates stray whitespace/commas but not non-numeric tokens.
    """
    out: List[float] = []
    for tok in text.split(","):
        tok = tok.strip()
        if tok == "":
            continue
        out.append(parse_float(tok))
    return out


def normalize_acq_state(text: str) -> str:
    """Normalize ``ACQ:STAT?`` responses to a canonical token."""
    t = text.strip().upper()
    if t in ("RUN",):
        return "RUN"
    if t in ("STOP",):
        return "STOP"
    if t.startswith("COMP"):
        return "COMP"
    if t.startswith("BRE"):
        return "BREAK"
    return t


def parse_idn(text: str) -> Dict[str, str]:
    parts = [p.strip() for p in text.split(",")]
    return {
        "vendor": parts[0] if len(parts) > 0 else "",
        "model": parts[1] if len(parts) > 1 else "",
        "serial": parts[2] if len(parts) > 2 else "",
        "firmware": parts[3] if len(parts) > 3 else "",
    }
