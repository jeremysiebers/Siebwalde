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

Amplifier snapshot fields are modelled as **map-independent semantic fields**,
not as raw HR addresses. There are two source-verified register maps:

* ``LEGACY_12`` (pre-Phase-1, firmware ``0x251F``;
  ``TrackAmplifier4.X/modbus/General.h`` @ ``928ea7c``): 12 registers, NO
  identity/version/patch registers. HR3 = amp status, HR9 = config-ID/PWM,
  HR10 = accel params, HR11 = SW checksum.
* ``PHASE1_23`` (HEAD; ``TrackController5/firmware/src/enums.h:97,108-109``,
  C# ``TrackAmplifierRegisters.cs``): 23 registers. HR7 = identity,
  HR21 = firmware version (major<<8 | minor), HR22 = firmware patch/proto
  (patch<<8 | protocol), HR11 = SW checksum.

``HR11`` (checksum) is the single stable anchor present in BOTH maps. Register
addresses are a *derived per-map view* (``REGISTER_ADDRESS_TABLES``), never the
primary key of a snapshot.

Evidence levels are reused from ``tools.diagnostics.core``. A complete parsed
recorded response is classified ``ACKNOWLEDGED`` (the device *reported* the data
in a recorded response); it is NEVER ``PHYSICALLY_OBSERVED`` or ``VERIFIED``,
because offline parsing cannot prove physical truth. Missing fields degrade the
evidence level to ``UNKNOWN`` (fail-closed) and are never fabricated.
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
# Amplifier register maps (source-verified, read-only mirrors)
# --------------------------------------------------------------------------- #
# Register-map discriminators.
REGISTER_MAP_LEGACY_12 = "LEGACY_12"
REGISTER_MAP_PHASE1_23 = "PHASE1_23"
REGISTER_MAP_UNKNOWN = "UNKNOWN"

REGISTER_MAPS = (REGISTER_MAP_LEGACY_12, REGISTER_MAP_PHASE1_23, REGISTER_MAP_UNKNOWN)

# The single stable anchor — HR11 is the SW checksum in BOTH maps.
HR_CHECKSUM = 11

# Per-map semantic-name -> holding-register address tables. These are derived
# views only; a snapshot stores semantic fields and records which map it came
# from. LEGACY_12 has NO identity/version/patch registers.
REGISTER_ADDRESS_TABLES = {
    REGISTER_MAP_LEGACY_12: {
        "status": 3,
        "config_id_pwm": 9,
        "accel_params": 10,
        "checksum": HR_CHECKSUM,
    },
    REGISTER_MAP_PHASE1_23: {
        "identity": 7,
        "version": 21,
        "patch_proto": 22,
        "checksum": HR_CHECKSUM,
    },
}

# Wire constants mirrored from TrackController5/firmware/src/enums.h + controller.c.
_HEADER = 0xAA
_TASK_ID_CONTROLLER = 10
_CMD_GET_CONTROLLER_VERSION = 113


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
    """A recorded amplifier observation.

    Semantic fields are first-class and map-independent; ``register_map``
    records which register map the values came from. ``checksum`` (HR11) is the
    single stable anchor present in every map. Register addresses are only ever
    derived via :meth:`holding_registers`, never stored as the primary key.
    """

    slave_number: Optional[int] = None
    detected: Optional[bool] = None
    # Semantic, map-independent fields.
    identity: Optional[int] = None
    fw_version: Optional[int] = None      # major<<8 | minor (e.g. 0x0100)
    patch_proto: Optional[int] = None     # patch<<8 | protocol (e.g. 0x0001)
    checksum: Optional[int] = None        # HR11 — single stable anchor
    # Which register map the values came from.
    register_map: str = REGISTER_MAP_UNKNOWN
    freshness: str = FRESHNESS_UNKNOWN
    observation_timestamp: Optional[str] = None
    evidence_level: str = core.UNKNOWN

    def holding_registers(self) -> Dict[str, Optional[int]]:
        """Derive the HR-address -> value view for this snapshot's map.

        Addresses come from :data:`REGISTER_ADDRESS_TABLES`. When
        ``register_map`` is ``UNKNOWN`` the result is empty — HR numbers are
        never invented. ``HR11`` is the checksum address in every known map.
        """
        table = REGISTER_ADDRESS_TABLES.get(self.register_map)
        if table is None:
            return {}
        semantic = {
            "identity": self.identity,
            "version": self.fw_version,
            "patch_proto": self.patch_proto,
            "checksum": self.checksum,
        }
        out: Dict[str, Optional[int]] = {}
        for name, addr in table.items():
            if name in semantic:
                out["HR%d" % addr] = semantic[name]
        return out

    def to_dict(self) -> dict:
        return {
            "slave_number": self.slave_number,
            "detected": self.detected,
            "identity": self.identity,
            "fw_version": self.fw_version,
            "patch_proto": self.patch_proto,
            "checksum": self.checksum,
            "register_map": self.register_map,
            "holding_registers": self.holding_registers(),
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
_AMP_MAP_RE = re.compile(r"\bmap\s*=\s*([A-Za-z0-9_]+)", re.IGNORECASE)


def _normalize_map(token: Optional[str]) -> str:
    if token is None:
        return REGISTER_MAP_UNKNOWN
    t = token.strip().upper()
    if t in ("LEGACY_12", "LEGACY12", "LEGACY", "12"):
        return REGISTER_MAP_LEGACY_12
    if t in ("PHASE1_23", "PHASE1_23", "PHASE1", "23"):
        return REGISTER_MAP_PHASE1_23
    return REGISTER_MAP_UNKNOWN


def _amplifier_evidence_level(
    slave: Optional[int],
    detected: Optional[bool],
    identity: Optional[int],
    fw_version: Optional[int],
    patch_proto: Optional[int],
    checksum: Optional[int],
    register_map: str,
) -> str:
    """Fail-closed evidence level for an amplifier snapshot.

    A usable observation requires ``slave`` + ``detected``. Completeness is
    map-aware: LEGACY_12 only has a checksum; PHASE1_23 has identity/version/
    patch_proto/checksum; UNKNOWN accepts either a complete Phase-1-style read
    or a legacy-style checksum-only read. Anything incomplete is UNKNOWN.
    """
    if slave is None or detected is None:
        return core.UNKNOWN
    if register_map == REGISTER_MAP_LEGACY_12:
        return core.ACKNOWLEDGED if checksum is not None else core.UNKNOWN
    if register_map == REGISTER_MAP_PHASE1_23:
        complete = all(
            v is not None for v in (identity, fw_version, patch_proto, checksum)
        )
        return core.ACKNOWLEDGED if complete else core.UNKNOWN
    # UNKNOWN map: accept a complete Phase-1-style read or a legacy-style
    # checksum-only read. Otherwise fail closed.
    if all(v is not None for v in (identity, fw_version, patch_proto, checksum)):
        return core.ACKNOWLEDGED
    if checksum is not None and identity is None and fw_version is None and patch_proto is None:
        return core.ACKNOWLEDGED
    return core.UNKNOWN


def parse_amplifier_snapshot(text: str) -> AmplifierSnapshot:
    """Parse a recorded amplifier snapshot from a textual line.

    Accepts semantic field names, e.g. ``slave 3 detected=1 identity=0xF101
    version=0x0100 patch=0x0001 checksum=0x251F`` (optionally with a
    ``map=PHASE1_23`` / ``map=LEGACY_12`` hint). Semantic fields are stored
    map-independently; without a map hint ``register_map`` is ``UNKNOWN`` and no
    HR numbers are invented. Missing fields are recorded as ``None`` and degrade
    the evidence level to ``UNKNOWN`` (fail-closed) — they are never fabricated.
    A line with no recognisable field raises ``ValueError``.
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
    map_m = _AMP_MAP_RE.search(t)
    register_map = _normalize_map(map_m.group(1)) if map_m else REGISTER_MAP_UNKNOWN

    if all(
        v is None for v in (slave, detected, identity, version, patch, checksum)
    ):
        raise ValueError("malformed amplifier snapshot: %r" % t)

    return AmplifierSnapshot(
        slave_number=slave,
        detected=detected,
        identity=identity,
        fw_version=version,
        patch_proto=patch,
        checksum=checksum,
        register_map=register_map,
        evidence_level=_amplifier_evidence_level(
            slave, detected, identity, version, patch, checksum, register_map
        ),
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
        "register_maps": list(REGISTER_MAPS),
        "register_address_tables": {
            map_name: {name: "HR%d" % addr for name, addr in table.items()}
            for map_name, table in REGISTER_ADDRESS_TABLES.items()
        },
        "checksum_register": "HR%d" % HR_CHECKSUM,
        "evidence_levels": list(core.EVIDENCE_LEVELS),
        "notes": (
            "Offline recorded-observation modelling only. Never opens a socket, "
            "serial port, or hardware connection. Amplifier snapshots use "
            "map-independent semantic fields (identity/fw_version/patch_proto/"
            "checksum) with a register_map discriminator; HR11 (checksum) is the "
            "single stable anchor in both the LEGACY_12 and PHASE1_23 maps, and "
            "register addresses are a derived per-map view. A parsed response is "
            "classified ACKNOWLEDGED at most (never PHYSICALLY_OBSERVED/VERIFIED); "
            "missing fields degrade to UNKNOWN and are never fabricated. Capability "
            "classification is advisory and grants no execution authority."
        ),
    }
