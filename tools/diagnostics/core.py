"""Siebwalde Diagnostics Core (K4-B) — offline, typed, MCP-independent.

Analyses *saved* textual traces from the K3 fail-closed single-target flash
driver ``TrackController5/Python/flash_one.py`` and the surrounding protocol
enum contract. It is strictly OFFLINE:

* it never imports ``Comm`` / ``DataAquisition`` (which would bind UDP sockets);
* it never imports ``flash_one.py`` (which imports ``Comm``/``bootload``);
* it never opens a socket, a serial port, or any hardware connection;
* it only parses and reasons over text already captured on disk.

The protocol/ack matrix and timeout codes are mirrored (read-only) from
``TrackController5/firmware/src/enums.h`` and
``docs/K3-FAIL-CLOSED-FIRMWARE-DIAGNOSTICS.md``. The 7-state ``FlashState``
machine is re-declared verbatim from ``flash_one.py`` (do NOT import that module
here — its import graph reaches the UDP transport).

Evidence levels distinguish what was actually proven versus merely sent:

    REQUESTED           the operation was requested/planned, nothing proven
    SENT                a command was emitted by the host driver (logged "TX")
    ACKNOWLEDGED        the target acknowledged the command (matching ack)
    PHYSICALLY_OBSERVED a physical response was independently observed (not
                        reachable from a text trace; retained for vocabulary
                        completeness and never asserted from text alone)
    VERIFIED            the full operation completed with its *_RETURNED_OK/DONE
                        ack sequence (VERIFIED_COMPLETE)
    UNKNOWN             fail-closed: any timeout / ERROR / missing ack leaves the
                        hardware state unknown and is never promoted to success

A sent command is NEVER promoted to confirmed success without its matching
``*_RETURNED_OK`` / DONE acknowledgement.
"""

from __future__ import annotations

import re
from typing import List, Optional

# --------------------------------------------------------------------------- #
# Evidence levels (the core vocabulary)
# --------------------------------------------------------------------------- #
REQUESTED = "REQUESTED"
SENT = "SENT"
ACKNOWLEDGED = "ACKNOWLEDGED"
PHYSICALLY_OBSERVED = "PHYSICALLY_OBSERVED"
VERIFIED = "VERIFIED"
UNKNOWN = "UNKNOWN"

EVIDENCE_LEVELS = (
    REQUESTED,
    SENT,
    ACKNOWLEDGED,
    PHYSICALLY_OBSERVED,
    VERIFIED,
    UNKNOWN,
)

# --------------------------------------------------------------------------- #
# FlashState — re-declared from TrackController5/Python/flash_one.py `FlashState`
# (K3 fail-closed state machine). NOT imported: importing flash_one.py would
# transitively import the UDP transport (`Comm`) — forbidden for this core.
# --------------------------------------------------------------------------- #


class FlashState(object):
    """The seven K3 fail-closed flash-attempt states (source: flash_one.py)."""

    PRE_ERASE = "PRE_ERASE"
    ERASE_REQUESTED_UNCONFIRMED = "ERASE_REQUESTED_UNCONFIRMED"
    ERASE_CONFIRMED = "ERASE_CONFIRMED"
    WRITE_IN_PROGRESS = "WRITE_IN_PROGRESS"
    PROGRAMMING_UNCONFIRMED = "PROGRAMMING_UNCONFIRMED"
    VERIFIED_COMPLETE = "VERIFIED_COMPLETE"
    FAILED_UNKNOWN_HARDWARE_STATE = "FAILED_UNKNOWN_HARDWARE_STATE"


FLASH_STATES = (
    FlashState.PRE_ERASE,
    FlashState.ERASE_REQUESTED_UNCONFIRMED,
    FlashState.ERASE_CONFIRMED,
    FlashState.WRITE_IN_PROGRESS,
    FlashState.PROGRAMMING_UNCONFIRMED,
    FlashState.VERIFIED_COMPLETE,
    FlashState.FAILED_UNKNOWN_HARDWARE_STATE,
)

# Evidence level reached per FlashState (docs/K3-FAIL-CLOSED-FIRMWARE-DIAGNOSTICS.md
# section 2). Used only when a session ends cleanly short of VERIFIED_COMPLETE
# with no ERROR/timeout (e.g. a truncated trace).
_STATE_EVIDENCE = {
    FlashState.PRE_ERASE: REQUESTED,
    FlashState.ERASE_REQUESTED_UNCONFIRMED: SENT,
    FlashState.ERASE_CONFIRMED: ACKNOWLEDGED,
    FlashState.WRITE_IN_PROGRESS: SENT,
    FlashState.PROGRAMMING_UNCONFIRMED: ACKNOWLEDGED,
}

# --------------------------------------------------------------------------- #
# Protocol constants — mirrored (read-only) from TrackController5/firmware/src/enums.h
# --------------------------------------------------------------------------- #
TASK_STATE_ERROR = 9
TASK_STATE_DONE = 7

# TASK_STATE values (enums.h `TASK_STATE`)
TASK_STATE_NAMES = {
    4: "ABORT",
    5: "BUSY",
    6: "CONNECTED",
    7: "DONE",
    8: "COMMAND",
    9: "ERROR",
}

# TASK_MESSAGES values (enums.h `TASK_MESSAGES`)
TASK_MESSAGE_NAMES = {
    100: "NONE",
    101: "RECEIVED_WRONG_COMMAND",
    102: "RECEIVED_UNKNOWN_COMMAND",
    103: "RECEIVED_BAD_COMMAND",
    104: "RECEIVED_CHECKSUM_OK",
    105: "RECEIVED_CHECKSUM_NOK",
    106: "SWITCH_OUT_OF_BOUNDS",
    107: "SLAVE_ID_OUT_OF_BOUNDS",
    108: "MESSAGE_BUFFER_FULL",
    109: "DATA_SENT_TO_SLAVE",
}

# TASK_ID values (enums.h `TASK_ID`); the "taskid" field is overloaded and can
# also carry a TASK_COMMAND (FWFILEDOWNLOAD=32, ERASE_FLASH=200, ...).
TASK_ID_NAMES = {
    10: "CONTROLLER",
    20: "MBUS",
    30: "FWHANDLER",
}

# Bootloader routine result codes (enums.h `TASK_COMMAND`, SLAVEBOOTLOADERROUTINES).
# NOTE the firmware bug: WRITExCONFIG's timeout branch reports 211 (not 221).
# 221 is still listed because it is semantically a timeout code.
TIMEOUT_CODES = frozenset({193, 201, 211, 221, 231, 241})
OK_CODES = frozenset({194, 202, 212, 222, 232, 242})
NOK_CODES = frozenset({195, 203, 213, 223, 233, 243})
BOOTLOADER_DATA_RECEIVE_ERROR = 180
BOOTLOADER_START_BYTE_ERROR = 181

# Command-number -> name (enums.h + flash_one.py emitted sequence). Used as a
# fallback when a TX line does not carry a parenthesised name.
COMMAND_NAMES = {
    250: "CLIENT_CONNECTION_REQUEST",
    100: "EXEC_MBUS_STATE_SLAVES_ON",
    101: "EXEC_MBUS_STATE_SLAVE_DETECT",
    102: "EXEC_MBUS_STATE_SLAVES_BOOT_WAIT",
    103: "EXEC_MBUS_STATE_SLAVE_FW_FLASH",
    104: "EXEC_MBUS_STATE_SLAVE_INIT",
    105: "EXEC_MBUS_STATE_SLAVE_ENABLE",
    106: "EXEC_MBUS_STATE_START_DATA_UPLOAD",
    107: "EXEC_MBUS_STATE_RESET",
    108: "EXEC_MBUS_SLAVE_DATA_EXCH",
    109: "EXEC_GLOBAL_STOP",
    110: "HOST_HEARTBEAT",
    111: "GLOBAL_STOP_STATE",
    112: "REARM_GLOBAL_STOP",
    113: "GET_CONTROLLER_VERSION",
    120: "EXEC_FW_STATE_RECEIVE_FW_FILE",
    121: "EXEC_FW_STATE_RECEIVE_CONFIG_WORD",
    122: "EXEC_FW_STATE_FLASH_ALL_SLAVES",
    123: "EXEC_FW_STATE_SELECT_SLAVE",
    124: "EXEC_FW_STATE_GET_BOOTLOADER_VERSION",
    125: "EXEC_FW_STATE_ERASE_FLASH",
    126: "EXEC_FW_STATE_WRITE_FLASH",
    127: "EXEC_FW_STATE_WRITE_CONFIG",
    128: "EXEC_FW_STATE_CHECK_CHECKSUM",
    129: "EXEC_FW_STATE_SLAVE_RESET",
    130: "EXIT_SLAVExFWxHANDLER",
    140: "FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY",
    141: "FILEDOWNLOAD_STATE_FW_DATA_RECEIVE",
    142: "FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE",
    143: "FILEDOWNLOAD_STATE_FW_CHECKSUM",
    150: "CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY",
    151: "CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE",
    152: "CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE",
    180: "BOOTLOADER_DATA_RECEIVE_ERROR",
    181: "BOOTLOADER_START_BYTE_ERROR",
    192: "GET_BOOTLOADER_VERSION",
    193: "GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT",
    194: "GET_BOOTLOADER_VERSION_OK",
    195: "GET_BOOTLOADER_VERSION_NOK",
    200: "ERASE_FLASH",
    201: "ERASE_FLASH_RECEIVE_DATA_TIMEOUT",
    202: "ERASE_FLASH_RETURNED_OK",
    203: "ERASE_FLASH_RETURNED_NOK",
    210: "WRITE_FLASH",
    211: "WRITE_FLASH_RECEIVE_DATA_TIMEOUT",
    212: "WRITE_FLASH_RETURNED_OK",
    213: "WRITE_FLASH_RETURNED_NOK",
    220: "WRITE_CONFIG",
    221: "WRITE_CONFIG_RECEIVE_DATA_TIMEOUT",
    222: "WRITE_CONFIG_RETURNED_OK",
    223: "WRITE_CONFIG_RETURNED_NOK",
    230: "CHECK_CHECKSUM_CONFIG",
    231: "CHECK_CHECKSUM_CONFIG_RECEIVE_DATA_TIMEOUT",
    232: "CHECK_CHECKSUM_CONFIG_RETURNED_OK",
    233: "CHECK_CHECKSUM_CONFIG_RETURNED_NOK",
    240: "RESET_SLAVE",
    241: "RESET_SLAVE_DATA_TIMEOUT",
    242: "RESET_SLAVE_OK",
    243: "RESET_SLAVE_NOK",
}

# --------------------------------------------------------------------------- #
# Line parsers (flash_one.py log format)
# --------------------------------------------------------------------------- #
# [run=<8hex> t=+<ms>ms] <body>
_TAG_RE = re.compile(r"\[run=([0-9a-fA-F]{8})\s+t=\+([0-9.]+)ms\]\s*(.*)$")
_TX_RE = re.compile(r"TX\s+cmd=(0x[0-9a-fA-F]+)\s*\(([^)]*)\)")
_RX_RE = re.compile(
    r"RX\s+fb=\((0x[0-9a-fA-F]+),(0x[0-9a-fA-F]+),(0x[0-9a-fA-F]+),"
    r"(0x[0-9a-fA-F]+)\)"
)
_STATE_RE = re.compile(r"STATE\s+(\S+)\s*->\s*(\S+)")
_START_RE = re.compile(r"START\s+slave=(\d+)\s+hex=(.*)")
_ACK_FIELD_RE = re.compile(r"\b(taskid|taskcommand|taskstate|taskmessage)=([A-Za-z0-9_]+)\((\d+)\)")

# Feedback tuple: one 4-tuple of ints (decimal or 0x-hex) per line.
_FB_FIELD_RE = re.compile(
    r"fb\s*=\s*\(\s*([0-9a-fA-FxX]+)\s*,\s*([0-9a-fA-FxX]+)\s*,"
    r"\s*([0-9a-fA-FxX]+)\s*,\s*([0-9a-fA-FxX]+)\s*\)"
)
_TUPLE_RE = re.compile(
    r"^\s*\(?\s*([0-9a-fA-FxX]+)\s*,\s*([0-9a-fA-FxX]+)\s*,\s*"
    r"([0-9a-fA-FxX]+)\s*,\s*([0-9a-fA-FxX]+)\s*\)?\s*$"
)


def _parse_int(token: str) -> Optional[int]:
    token = (token or "").strip()
    if not token:
        return None
    try:
        if token.lower().startswith("0x"):
            return int(token, 16)
        return int(token, 10)
    except ValueError:
        return None


def _command_name(cmd: int) -> str:
    return COMMAND_NAMES.get(cmd, str(cmd))


def classify_feedback_frame(taskcommand: int, taskstate: int) -> str:
    """Classify one feedback frame as ok / timeout / nok / error / unknown.

    Rules (enums.h + docs/K3-FAIL-CLOSED-FIRMWARE-DIAGNOSTICS.md):
      * a ``*_RECEIVE_DATA_TIMEOUT`` code  -> "timeout"  (arrives with ERROR)
      * a ``*_RETURNED_NOK`` code          -> "nok"      (arrives with ERROR)
      * a ``*_RETURNED_OK`` code + DONE    -> "ok"
      * any other ERROR state              -> "error"    (180/181, wrong command)
      * everything else                    -> "unknown"
    """
    if taskcommand in TIMEOUT_CODES:
        return "timeout"
    if taskcommand in NOK_CODES:
        return "nok"
    if taskcommand in OK_CODES:
        return "ok" if taskstate == TASK_STATE_DONE else "error"
    if taskstate == TASK_STATE_ERROR:
        return "error"
    return "unknown"


def _classify_evidence_level(final_state: str, timeout_or_error: bool) -> str:
    """Map the final FlashState + failure flag to a core evidence level.

    Fail-closed: FAILED_UNKNOWN_HARDWARE_STATE or any ERROR/timeout abort is
    UNKNOWN (never VERIFIED). Only VERIFIED_COMPLETE is VERIFIED.
    """
    if final_state == FlashState.FAILED_UNKNOWN_HARDWARE_STATE or timeout_or_error:
        return UNKNOWN
    if final_state == FlashState.VERIFIED_COMPLETE:
        return VERIFIED
    return _STATE_EVIDENCE.get(final_state, UNKNOWN)


# --------------------------------------------------------------------------- #
# Public API
# --------------------------------------------------------------------------- #


def analyze_firmware_session(log_text: str) -> dict:
    """Analyse a saved flash_one.py textual log and classify the session.

    Returns a structured summary:

        {
          run_id, slave_id, hex_path,
          commands: [{cmd, name, sent, t_ms}],
          acks:    [{taskid, taskcommand, taskstate, taskmessage, t_ms,
                     classification, ...}],
          state_progression: [{from, to, t_ms}],
          final_state, evidence_level, timeout_or_error, errors
        }
    """
    if not isinstance(log_text, str):
        return {
            "run_id": None,
            "slave_id": None,
            "hex_path": None,
            "commands": [],
            "acks": [],
            "state_progression": [],
            "final_state": FlashState.PRE_ERASE,
            "evidence_level": UNKNOWN,
            "timeout_or_error": True,
            "errors": [{"kind": "error", "description": "input was not a string"}],
        }

    run_id: Optional[str] = None
    slave_id: Optional[int] = None
    hex_path: Optional[str] = None

    commands: List[dict] = []
    acks: List[dict] = []
    state_progression: List[dict] = []
    errors: List[dict] = []

    final_state = FlashState.PRE_ERASE
    timeout_or_error = False

    for raw_line in log_text.splitlines():
        line = raw_line.rstrip("\r\n")
        if not line.strip():
            continue

        # Non-tagged lines: look for explicit abort markers printed by the CLI
        # (e.g. "FLASH ABORTED: timed out waiting for ack: ...").
        tag = _TAG_RE.match(line)
        if tag is None:
            low = line.lower()
            if "flash aborted" in low or "timed out" in low or "target returned error" in low:
                timeout_or_error = True
                errors.append({"kind": "abort", "description": line.strip()})
            continue

        cur_run, t_ms_raw, body = tag.groups()
        t_ms = 0.0
        try:
            t_ms = float(t_ms_raw)
        except ValueError:
            t_ms = 0.0
        if run_id is None:
            run_id = cur_run

        m = _START_RE.match(body)
        if m:
            slave_id = _parse_int(m.group(1))
            hex_path = m.group(2).strip()
            continue

        m = _TX_RE.search(body)
        if m:
            cmd = _parse_int(m.group(1))
            if cmd is None:
                continue
            name = m.group(2).strip() or _command_name(cmd)
            commands.append({"cmd": cmd, "name": name, "sent": True, "t_ms": t_ms})
            continue

        m = _RX_RE.search(body)
        if m:
            taskid = _parse_int(m.group(1))
            taskcommand = _parse_int(m.group(2))
            taskstate = _parse_int(m.group(3))
            taskmessage = _parse_int(m.group(4))
            if None in (taskid, taskcommand, taskstate, taskmessage):
                continue
            classification = classify_feedback_frame(taskcommand, taskstate)
            ack = {
                "taskid": taskid,
                "taskcommand": taskcommand,
                "taskstate": taskstate,
                "taskmessage": taskmessage,
                "t_ms": t_ms,
                "classification": classification,
                "taskid_name": _task_id_name(taskid),
                "taskcommand_name": _command_name(taskcommand),
                "taskstate_name": TASK_STATE_NAMES.get(taskstate, str(taskstate)),
                "taskmessage_name": TASK_MESSAGE_NAMES.get(taskmessage, str(taskmessage)),
            }
            acks.append(ack)
            if classification in ("timeout", "nok", "error"):
                timeout_or_error = True
                errors.append({
                    "kind": classification,
                    "taskid": taskid,
                    "taskcommand": taskcommand,
                    "taskstate": taskstate,
                    "taskmessage": taskmessage,
                    "description": (
                        "feedback frame classified %s: taskid=%s taskcommand=%s "
                        "taskstate=%s taskmessage=%s"
                        % (classification, _task_id_name(taskid),
                           _command_name(taskcommand),
                           TASK_STATE_NAMES.get(taskstate, taskstate),
                           TASK_MESSAGE_NAMES.get(taskmessage, taskmessage))
                    ),
                })
            continue

        m = _STATE_RE.search(body)
        if m:
            frm, to = m.group(1), m.group(2)
            final_state = to
            state_progression.append({"from": frm, "to": to, "t_ms": t_ms})
            continue

        # DISCARD / Drain(...) / Readback lines: not modelled here.

    evidence_level = _classify_evidence_level(final_state, timeout_or_error)

    return {
        "run_id": run_id,
        "slave_id": slave_id,
        "hex_path": hex_path,
        "commands": commands,
        "acks": acks,
        "state_progression": state_progression,
        "final_state": final_state,
        "evidence_level": evidence_level,
        "timeout_or_error": timeout_or_error,
        "errors": errors,
    }


def _task_id_name(value: int) -> str:
    if value in TASK_ID_NAMES:
        return TASK_ID_NAMES[value]
    if value in COMMAND_NAMES:
        return COMMAND_NAMES[value]
    return str(value)


def analyze_udp_trace(trace_text: str) -> dict:
    """Parse a raw textual feedback trace and classify each frame.

    Accepts one feedback tuple per line, either decimal ``(200, 201, 9, 100)``
    or hex ``(0xC8, 0xC9, 0x09, 0x64)``, optionally wrapped as a full
    ``fb=(0x..,0x..,0x..,0x..)`` line. Malformed lines are skipped and counted.

    Returns ``{frames: [...], counts: {ok, timeout, nok, error, unknown, malformed}}``.
    """
    if not isinstance(trace_text, str):
        return {
            "frames": [],
            "counts": {"ok": 0, "timeout": 0, "nok": 0, "error": 0,
                       "unknown": 0, "malformed": 1},
        }

    counts = {"ok": 0, "timeout": 0, "nok": 0, "error": 0, "unknown": 0, "malformed": 0}
    frames: List[dict] = []

    for raw_line in trace_text.splitlines():
        line = raw_line.strip()
        if not line:
            continue

        m = _FB_FIELD_RE.search(line)
        if m is None:
            m = _TUPLE_RE.match(line)
        if m is None:
            counts["malformed"] += 1
            continue

        values = [_parse_int(m.group(i)) for i in (1, 2, 3, 4)]
        if any(v is None for v in values):
            counts["malformed"] += 1
            continue

        taskid, taskcommand, taskstate, taskmessage = values
        classification = classify_feedback_frame(taskcommand, taskstate)
        counts[classification] += 1
        frames.append({
            "taskid": taskid,
            "taskcommand": taskcommand,
            "taskstate": taskstate,
            "taskmessage": taskmessage,
            "classification": classification,
            "taskcommand_name": _command_name(taskcommand),
            "taskstate_name": TASK_STATE_NAMES.get(taskstate, str(taskstate)),
        })

    return {"frames": frames, "counts": counts}


def compare_protocol_traces(expected: list, actual: list) -> dict:
    """Compare two command sequences (lists of command ints).

    Returns ``{expected, actual, differences: [{index, expected, actual}], match}``.
    Elements present on only one side are reported with ``None`` for the other.
    """
    expected = list(expected) if expected is not None else []
    actual = list(actual) if actual is not None else []

    differences: List[dict] = []
    n = max(len(expected), len(actual))
    for i in range(n):
        e = expected[i] if i < len(expected) else None
        a = actual[i] if i < len(actual) else None
        if e != a:
            differences.append({"index": i, "expected": e, "actual": a})

    return {
        "expected": expected,
        "actual": actual,
        "differences": differences,
        "match": expected == actual,
    }


def capabilities() -> dict:
    """Describe the offline-only operations and evidence levels."""
    return {
        "offline_only": True,
        "hardware_access": False,
        "socket_access": False,
        "serial_access": False,
        "operations": [
            "analyze_firmware_session",
            "analyze_udp_trace",
            "compare_protocol_traces",
        ],
        "evidence_levels": list(EVIDENCE_LEVELS),
        "flash_states": list(FLASH_STATES),
        "protocol": {
            "timeout_codes": sorted(TIMEOUT_CODES),
            "ok_codes": sorted(OK_CODES),
            "nok_codes": sorted(NOK_CODES),
        },
        "notes": (
            "Offline textual trace analysis only. Never opens a socket, serial "
            "port, or hardware connection. A sent command is never promoted to "
            "confirmed success without its *_RETURNED_OK/DONE acknowledgement."
        ),
    }
