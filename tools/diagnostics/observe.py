"""Siebwalde OBSERVE adapter (K5-A) — typed, OFFLINE, MCP-independent.

Models *offline / recorded* diagnostic observations of the PIC32 controller and
the PIC18 track amplifiers. There is NO live transport:

* it never imports ``socket`` / ``serial`` / ``Comm`` / ``DataAquisition``;
* it never imports ``flash_one.py`` (whose import graph reaches the UDP transport);
* it never opens a socket, a serial port, or any hardware connection;
* it only parses and reasons over text/bytes already captured on disk.

The wire formats are mirrored (read-only) from source:

* command 113 ``GET_CONTROLLER_VERSION`` reply
  (``TrackController5/firmware/src/controller.c`` ``REPLYxCONTROLLERxVERSION``):
  ``header=0xAA``, ``command=CONTROLLER(10)``, ``data[0]=echo 113``,
  ``data[1..3]=MAJOR/MINOR/PATCH``, ``data[4]=PROTOCOL_VERSION``,
  ``data[5]=0``, ``data[6..N]=CTRL_RELEASE_TIMESTAMP`` (null-terminated).
  Values: ``version.h`` (``CTRL_FW_MAJOR/MINOR/PATCH``, ``CTRL_PROTOCOL_VERSION``,
  ``CTRL_RELEASE_TIMESTAMP``).
* the SLAVEINFO amplifier snapshot registers (``HR3`` identity, ``HR9`` version,
  ``HR10`` patch/proto, ``HR11`` firmware checksum), presented here as a textual
  ``slave N detected=.. identity=.. version=.. patch=.. checksum=..`` line.

Evidence levels are reused from ``tools.diagnostics.core``. A complete parsed
recorded response is classified ``ACKNOWLEDGED`` (the device *reported* the data
in a recorded response); it is NEVER ``PHYSICALLY_OBSERVED`` or ``VERIFIED``,
because offline parsing cannot prove physical truth. Any missing field degrades
the evidence level to ``UNKNOWN`` (fail-closed) and is never fabricated.
"""

from __future__ import annotations

import re
from collections import Counter
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Set

from . import core

# --------------------------------------------------------------------------- #
# Capability classification vocabulary (advisory only)
# --------------------------------------------------------------------------- #
# Classification describes the *effect class* of an operation. It does NOT grant
# execution authority; it is a read-only, offline label.
OFFLINE_ONLY = "OFFLINE_ONLY"
LIVE_READ_ONLY_SOURCE_VERIFIED = "LIVE_READ_ONLY_SOURCE_VERIFIED"
LIVE_READ_ONLY_HARDWARE_VALIDATED = "LIVE_READ_ONLY_HARDWARE_VALIDATED"
STATE_CHANGING = "STATE_CHANGING"
DESTRUCTIVE = "DESTRUCTIVE"
UNKNOWN_EFFECT = "UNKNOWN_EFFECT"

CAPABILITY_CLASSIFICATIONS = (
    OFFLINE_ONLY,
    LIVE_READ_ONLY_SOURCE_VERIFIED,
    LIVE_READ_ONLY_HARDWARE_VALIDATED,
    STATE_CHANGING,
    DESTRUCTIVE,
    UNKNOWN_EFFECT,
)

# --------------------------------------------------------------------------- #
# Freshness vocabulary
# --------------------------------------------------------------------------- #
FRESHNESS_FRESH = "fresh"
FRESHNESS_STALE = "stale"
FRESHNESS_UNKNOWN = "unknown"

# --------------------------------------------------------------------------- #
# Amplifier snapshot register keys (SLAVEINFO textual view)
# --------------------------------------------------------------------------- #
# Stable keys for the holding_registers mapping. These are the SLAVEINFO
# registers of interest for the K5-A snapshot model (identity / version /
# patch+protocol / firmware checksum).
HR_IDENTITY = "HR3"
HR_VERSION = "HR9"
HR_PATCH_PROTO = "HR10"
HR_CHECKSUM = "HR11"

HR_REGISTER_KEYS = (HR_IDENTITY, HR_VERSION, HR_PATCH_PROTO, HR_CHECKSUM)

# Wire constants mirrored from TrackController5/firmware/src/enums.h + controller.c.
_HEADER = 0xAA
_TASK_ID_CONTROLLER = 10
_CMD_GET_CONTROLLER_VERSION = 113


def _empty_holding_registers() -> Dict[str, Optional[int]]:
    return {key: None for key in HR_REGISTER_KEYS}


# --------------------------------------------------------------------------- #
# Typed models
# --------------------------------------------------------------------------- #


@dataclass(frozen=True)
class ControllerVersion:
    """Semantic controller version: major/minor/patch + protocol version."""

    major: Optional[int] = None
    minor: Optional[int] = None
    patch: Optional[int] = None
    protocol: Optional[int] = None

    def to_dict(self) -> dict:
        return {
            "major": self.major,
            "minor": self.minor,
            "patch": self.patch,
            "protocol": self.protocol,
        }


@dataclass(frozen=True)
class ControllerSnapshot:
    """A recorded controller observation (command-113 version response)."""

    version: ControllerVersion = field(default_factory=ControllerVersion)
    build_timestamp: Optional[str] = None
    source_revision: Optional[str] = None
    freshness: str = FRESHNESS_UNKNOWN
    observation_timestamp: Optional[str] = None
    evidence_level: str = core.UNKNOWN

    def to_dict(self) -> dict:
        return {
            "version": self.version.to_dict(),
            "build_timestamp": self.build_timestamp,
            "source_revision": self.source_revision,
            "freshness": self.freshness,
            "observation_timestamp": self.observation_timestamp,
            "evidence_level": self.evidence_level,
        }


@dataclass(frozen=True)
class AmplifierSnapshot:
    """A recorded amplifier observation (SLAVEINFO registers, textual view)."""

    slave_number: Optional[int] = None
    detected: Optional[bool] = None
    holding_registers: Dict[str, Optional[int]] = field(
        default_factory=_empty_holding_registers
    )
    freshness: str = FRESHNESS_UNKNOWN
    observation_timestamp: Optional[str] = None
    evidence_level: str = core.UNKNOWN

    def to_dict(self) -> dict:
        return {
            "slave_number": self.slave_number,
            "detected": self.detected,
            "holding_registers": dict(self.holding_registers),
            "freshness": self.freshness,
            "observation_timestamp": self.observation_timestamp,
            "evidence_level": self.evidence_level,
        }


@dataclass(frozen=True)
class AmplifierInventory:
    """A recorded inventory of amplifiers plus the detected slave-number set."""

    amplifiers: List[AmplifierSnapshot] = field(default_factory=list)
    detected: Set[int] = field(default_factory=set)
    freshness: str = FRESHNESS_UNKNOWN
    observation_timestamp: Optional[str] = None

    @classmethod
    def from_snapshots(cls, snapshots: List[AmplifierSnapshot]) -> "AmplifierInventory":
        snapshots = list(snapshots) if snapshots is not None else []
        detected = {s.slave_number for s in snapshots if s.detected and s.slave_number is not None}
        return cls(amplifiers=snapshots, detected=detected)

    def to_dict(self) -> dict:
        return {
            "amplifiers": [s.to_dict() for s in self.amplifiers],
            "detected": sorted(self.detected),
            "freshness": self.freshness,
            "observation_timestamp": self.observation_timestamp,
        }


# --------------------------------------------------------------------------- #
# Small parsing helpers
# --------------------------------------------------------------------------- #


def _parse_hex_or_int(token: Optional[str]) -> Optional[int]:
    if token is None:
        return None
    token = token.strip()
    if not token:
        return None
    try:
        if token.lower().startswith("0x"):
            return int(token, 16)
        return int(token, 10)
    except ValueError:
        return None


def _match_int(m: Optional[re.Match]) -> Optional[int]:
    if m is None:
        return None
    return _parse_hex_or_int(m.group(1))


def _parse_detected(m: Optional[re.Match]) -> Optional[bool]:
    if m is None:
        return None
    token = m.group(1).strip().lower()
    if token in ("1", "true"):
        return True
    if token in ("0", "false"):
        return False
    return None


# --------------------------------------------------------------------------- #
# Controller version parser
# --------------------------------------------------------------------------- #
_TEXT_VERSION_RE = re.compile(r"version\s+(\d+)\.(\d+)\.(\d+)", re.IGNORECASE)
_TEXT_PROTOCOL_RE = re.compile(r"protocol\s+(0x[0-9a-fA-F]+|\d+)", re.IGNORECASE)
_TEXT_BUILD_RE = re.compile(r"build\s+(\S+)", re.IGNORECASE)
_TEXT_REV_RE = re.compile(r"(?:rev|revision|source_revision)\s+(\S+)", re.IGNORECASE)


def _parse_controller_version_bytes(raw: bytes) -> ControllerSnapshot:
    raw = bytes(raw)
    if len(raw) < 8:
        raise ValueError("controller version frame too short (%d bytes)" % len(raw))
    header, command, echo = raw[0], raw[1], raw[2]
    if header != _HEADER or command != _TASK_ID_CONTROLLER or echo != _CMD_GET_CONTROLLER_VERSION:
        raise ValueError(
            "not a controller version frame: header=0x%02X command=%d echo=%d"
            % (header, command, echo)
        )
    major, minor, patch, protocol = raw[3], raw[4], raw[5], raw[6]
    build_bytes = raw[8:]
    build = build_bytes.split(b"\x00", 1)[0].decode("ascii", "replace").strip()
    return ControllerSnapshot(
        version=ControllerVersion(major=major, minor=minor, patch=patch, protocol=protocol),
        build_timestamp=build or None,
        source_revision=None,
        evidence_level=core.ACKNOWLEDGED,
    )


def _parse_controller_version_text(text: str) -> ControllerSnapshot:
    t = text.strip()
    if not t:
        raise ValueError("empty controller version response")

    vm = _TEXT_VERSION_RE.search(t)
    if vm is not None:
        major = int(vm.group(1))
        minor = int(vm.group(2))
        patch = int(vm.group(3))
        protocol = _match_int(_TEXT_PROTOCOL_RE.search(t))
        build_m = _TEXT_BUILD_RE.search(t)
        rev_m = _TEXT_REV_RE.search(t)
        complete = protocol is not None
        return ControllerSnapshot(
            version=ControllerVersion(major=major, minor=minor, patch=patch, protocol=protocol),
            build_timestamp=build_m.group(1) if build_m else None,
            source_revision=rev_m.group(1) if rev_m else None,
            evidence_level=core.ACKNOWLEDGED if complete else core.UNKNOWN,
        )

    # Fall back: treat a purely hexadecimal string as raw bytes.
    hexstr = re.sub(r"[\s,]+", "", t)
    if re.fullmatch(r"[0-9a-fA-F]+", hexstr) and len(hexstr) % 2 == 0 and hexstr:
        try:
            return _parse_controller_version_bytes(bytes.fromhex(hexstr))
        except ValueError:
            pass
    raise ValueError("malformed controller version response: %r" % t)


def parse_controller_version_response(text_or_hex) -> ControllerSnapshot:
    """Parse a recorded command-113 controller version response.

    Accepts either the textual reply form::

        version 1.0.0 protocol 0x01 build 2026-10-17

    or the raw wire bytes ``header=0xAA, command=10, data[0]=113,
    data[1..3]=MAJOR/MINOR/PATCH, data[4]=PROTOCOL_VERSION, data[5]=0,
    data[6..N]=build timestamp`` (also accepted as a hex string). Malformed or
    non-matching input raises ``ValueError`` (rejected safely, never fabricated).
    """
    if isinstance(text_or_hex, (bytes, bytearray)):
        return _parse_controller_version_bytes(bytes(text_or_hex))
    if isinstance(text_or_hex, str):
        return _parse_controller_version_text(text_or_hex)
    raise ValueError(
        "controller version response must be str or bytes, got %s"
        % type(text_or_hex).__name__
    )


# --------------------------------------------------------------------------- #
# Amplifier snapshot parser
# --------------------------------------------------------------------------- #
_AMP_SLAVE_RE = re.compile(r"\bslave\s+(\d+)", re.IGNORECASE)
_AMP_DETECTED_RE = re.compile(r"\bdetected\s*=\s*(0|1|true|false)", re.IGNORECASE)
_AMP_IDENTITY_RE = re.compile(r"\bidentity\s*=\s*(0x[0-9a-fA-F]+|\d+)", re.IGNORECASE)
_AMP_VERSION_RE = re.compile(r"\bversion\s*=\s*(0x[0-9a-fA-F]+|\d+)", re.IGNORECASE)
_AMP_PATCH_RE = re.compile(r"\bpatch\s*=\s*(0x[0-9a-fA-F]+|\d+)", re.IGNORECASE)
_AMP_CHECKSUM_RE = re.compile(r"\bchecksum\s*=\s*(0x[0-9a-fA-F]+|\d+)", re.IGNORECASE)


def parse_amplifier_snapshot(text: str) -> AmplifierSnapshot:
    """Parse a recorded amplifier snapshot from a textual SLAVEINFO-style line.

    Accepts e.g. ``slave 3 detected=1 identity=0xF101 version=0x0100
    patch=0x0001 checksum=0x251F``. Missing fields are recorded as ``None`` and
    degrade the evidence level to ``UNKNOWN`` (fail-closed) — they are never
    fabricated. A line with no recognisable field raises ``ValueError``.
    """
    if not isinstance(text, str):
        raise ValueError(
            "amplifier snapshot must be a string, got %s" % type(text).__name__
        )
    t = text.strip()
    if not t:
        raise ValueError("empty amplifier snapshot")

    slave = _match_int(_AMP_SLAVE_RE.search(t))
    detected = _parse_detected(_AMP_DETECTED_RE.search(t))
    identity = _match_int(_AMP_IDENTITY_RE.search(t))
    version = _match_int(_AMP_VERSION_RE.search(t))
    patch = _match_int(_AMP_PATCH_RE.search(t))
    checksum = _match_int(_AMP_CHECKSUM_RE.search(t))

    if slave is None and detected is None and identity is None and version is None \
            and patch is None and checksum is None:
        raise ValueError("malformed amplifier snapshot: %r" % t)

    holding = {
        HR_IDENTITY: identity,
        HR_VERSION: version,
        HR_PATCH_PROTO: patch,
        HR_CHECKSUM: checksum,
    }
    complete = all(
        v is not None
        for v in (slave, detected, identity, version, patch, checksum)
    )
    return AmplifierSnapshot(
        slave_number=slave,
        detected=detected,
        holding_registers=holding,
        evidence_level=core.ACKNOWLEDGED if complete else core.UNKNOWN,
    )


# --------------------------------------------------------------------------- #
# Amplifier inventory comparison (detected-set)
# --------------------------------------------------------------------------- #


def compare_amplifier_inventory(expected: list, actual: list) -> dict:
    """Compare two amplifier inventories given as lists of snapshot lines.

    Each line is parsed with :func:`parse_amplifier_snapshot`. Returns a
    detected-set comparison:

    * ``missing``    — slave numbers detected in ``expected`` but not in ``actual``;
    * ``extra``      — slave numbers detected in ``actual`` but not in ``expected``;
    * ``unexpected`` — ``actual`` lines that could not be parsed (malformed).

    ``match`` is ``True`` only when there are no missing/extra/unexpected entries.
    """
    expected = list(expected) if expected is not None else []
    actual = list(actual) if actual is not None else []

    def _detected_set(lines: List[str]) -> Set[int]:
        detected: Set[int] = set()
        for line in lines:
            try:
                snap = parse_amplifier_snapshot(line)
            except ValueError:
                continue  # malformed lines contribute nothing to the detected set
            if snap.detected and snap.slave_number is not None:
                detected.add(snap.slave_number)
        return detected

    expected_detected = _detected_set(expected)
    actual_detected = _detected_set(actual)

    unexpected: List[dict] = []
    for i, line in enumerate(actual):
        try:
            parse_amplifier_snapshot(line)
        except ValueError as exc:
            unexpected.append({"index": i, "line": line, "reason": str(exc)})

    missing = sorted(expected_detected - actual_detected)
    extra = sorted(actual_detected - expected_detected)

    return {
        "expected": expected,
        "actual": actual,
        "expected_detected": sorted(expected_detected),
        "actual_detected": sorted(actual_detected),
        "missing": missing,
        "extra": extra,
        "unexpected": unexpected,
        "match": not missing and not extra and not unexpected,
    }


# --------------------------------------------------------------------------- #
# Init-discrepancy reporting (ordered sequences)
# --------------------------------------------------------------------------- #


def _lcs_index_pairs(a: List[str], b: List[str]) -> List[tuple]:
    """Return matched ``(index_in_a, index_in_b)`` pairs of a longest common
    subsequence (deterministic reconstruction)."""
    m, n = len(a), len(b)
    dp = [[0] * (n + 1) for _ in range(m + 1)]
    for i in range(m - 1, -1, -1):
        for j in range(n - 1, -1, -1):
            if a[i] == b[j]:
                dp[i][j] = dp[i + 1][j + 1] + 1
            else:
                dp[i][j] = max(dp[i + 1][j], dp[i][j + 1])

    pairs: List[tuple] = []
    i = j = 0
    while i < m and j < n:
        if a[i] == b[j]:
            pairs.append((i, j))
            i += 1
            j += 1
        elif dp[i + 1][j] >= dp[i][j + 1]:
            i += 1
        else:
            j += 1
    return pairs


def _multiset_diff(left: List[str], right: Counter) -> List[str]:
    """Elements of ``left`` not covered by ``right``, in first-appearance order."""
    avail = dict(right)
    out: List[str] = []
    for item in left:
        if avail.get(item, 0) > 0:
            avail[item] -= 1
        else:
            out.append(item)
    return out


def report_init_discrepancies(expected: list, actual: list) -> dict:
    """Report structured discrepancies between two ordered init sequences.

    Returns:

    * ``missing``    — elements in ``expected`` but not in ``actual``;
    * ``unexpected`` — elements in ``actual`` but not in ``expected``;
    * ``wrong_order`` — elements present in both but out of order (derived from
      the longest common subsequence of the common elements).

    ``match`` is ``True`` only when all three are empty.
    """
    expected = list(expected) if expected is not None else []
    actual = list(actual) if actual is not None else []

    ec = Counter(expected)
    ac = Counter(actual)
    common = ec & ac  # min-count intersection

    missing = _multiset_diff(expected, ac)
    unexpected = _multiset_diff(actual, ec)

    # Common elements in each sequence's own order.
    def _common_sequence(seq: List[str]) -> List[str]:
        avail = dict(common)
        out: List[str] = []
        for item in seq:
            if avail.get(item, 0) > 0:
                avail[item] -= 1
                out.append(item)
        return out

    seq_exp = _common_sequence(expected)
    seq_act = _common_sequence(actual)

    wrong_order: List[str] = []
    if seq_exp != seq_act:
        pairs = _lcs_index_pairs(seq_exp, seq_act)
        matched = {i for i, _ in pairs}
        wrong_order = [seq_exp[i] for i in range(len(seq_exp)) if i not in matched]

    return {
        "expected": expected,
        "actual": actual,
        "missing": missing,
        "unexpected": unexpected,
        "wrong_order": wrong_order,
        "match": not missing and not unexpected and not wrong_order,
    }


# --------------------------------------------------------------------------- #
# Operation classification
# --------------------------------------------------------------------------- #
# Offline operations (this module + the K4 Diagnostics Core) — pure text, no
# transport, no hardware. Every parse/compare/report here is OFFLINE_ONLY.
_OFFLINE_OPERATIONS = (
    "parse_controller_version_response",
    "parse_amplifier_snapshot",
    "compare_amplifier_inventory",
    "report_init_discrepancies",
    "classify_operation",
    "capabilities",
    # K4 Diagnostics Core (tools/diagnostics/core.py)
    "analyze_firmware_session",
    "analyze_udp_trace",
    "compare_protocol_traces",
    # MCP tool surface (tools/diagnostics/mcp_server/server.py)
    "diagnostics_parse_controller_snapshot",
    "diagnostics_parse_amplifier_snapshot",
    "diagnostics_compare_amplifier_inventory",
    "diagnostics_report_init_discrepancies",
    "diagnostics_classify_operation",
    "diagnostics_capabilities",
    "diagnostics_analyze_udp_trace",
    "diagnostics_analyze_firmware_session",
    "diagnostics_compare_protocol_traces",
)

# Illustrative live/hardware effect classes (ADVISORY ONLY — classification
# grants NO execution authority). Names follow the TASK_COMMAND/enums.h
# vocabulary so a caller can look up a real command's effect class.
_ILLUSTRATIVE_LIVE_CLASSIFICATIONS = {
    "GET_CONTROLLER_VERSION": LIVE_READ_ONLY_HARDWARE_VALIDATED,
    "GET_BOOTLOADER_VERSION": LIVE_READ_ONLY_HARDWARE_VALIDATED,
    "EXEC_MBUS_STATE_SLAVES_ON": STATE_CHANGING,
    "EXEC_GLOBAL_STOP": STATE_CHANGING,
    "RESET_SLAVE": STATE_CHANGING,
    "ERASE_FLASH": DESTRUCTIVE,
    "WRITE_FLASH": DESTRUCTIVE,
    "WRITE_CONFIG": DESTRUCTIVE,
}

_OPERATION_CLASSIFICATIONS: Dict[str, str] = {}
for _op in _OFFLINE_OPERATIONS:
    _OPERATION_CLASSIFICATIONS[_op] = OFFLINE_ONLY
_OPERATION_CLASSIFICATIONS.update(_ILLUSTRATIVE_LIVE_CLASSIFICATIONS)


def classify_operation(name: str) -> str:
    """Return the capability classification of an operation name.

    Offline parse/compare/report operations classify ``OFFLINE_ONLY``. Unknown
    names classify ``UNKNOWN_EFFECT``. The returned label is advisory and does
    NOT grant execution authority.
    """
    if not isinstance(name, str):
        return UNKNOWN_EFFECT
    key = name.strip()
    return _OPERATION_CLASSIFICATIONS.get(key, UNKNOWN_EFFECT)


def capabilities() -> dict:
    """Describe the offline-only OBSERVE operations, models and classifications."""
    return {
        "offline_only": True,
        "hardware_access": False,
        "socket_access": False,
        "serial_access": False,
        "operations": list(_OFFLINE_OPERATIONS),
        "capability_classifications": list(CAPABILITY_CLASSIFICATIONS),
        "operation_classifications": dict(_OPERATION_CLASSIFICATIONS),
        "models": ["ControllerSnapshot", "AmplifierSnapshot", "AmplifierInventory"],
        "holding_register_keys": list(HR_REGISTER_KEYS),
        "evidence_levels": list(core.EVIDENCE_LEVELS),
        "notes": (
            "Offline recorded-observation modelling only. Never opens a socket, "
            "serial port, or hardware connection. A parsed response is classified "
            "ACKNOWLEDGED at most (never PHYSICALLY_OBSERVED/VERIFIED); missing "
            "fields degrade to UNKNOWN and are never fabricated. Capability "
            "classification is advisory and grants no execution authority."
        ),
    }
