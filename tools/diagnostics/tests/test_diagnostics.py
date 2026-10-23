"""Deterministic offline tests for the Siebwalde Diagnostics Core (K4-B).

Covers the fail-closed evidence-level contract, feedback-frame classification,
malformed-input handling, and protocol-sequence comparison. No sockets, serial
ports, or hardware are ever opened.

Run with:
    tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
"""

from __future__ import annotations

import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics import core  # noqa: E402
from tools.diagnostics.core import FlashState  # noqa: E402

# Command bytes used in the synthetic logs (enums.h).
CMD = {
    "CONNECT": 250,
    "SLAVE_FW_FLASH": 103,
    "RECEIVE_FW_FILE": 120,
    "FW_DATA": 141,
    "RECEIVE_CONFIG_WORD": 121,
    "CFG_DATA": 151,
    "SELECT_SLAVE": 123,
    "ERASE": 125,
    "WRITE": 126,
    "WRITE_CONFIG": 127,
    "CHECK": 128,
    "RESET": 129,
    "EXIT": 130,
    "SLAVES_ON": 100,
    "START_DATA_UPLOAD": 106,
    "SLAVE_DETECT": 101,
}


def _successful_session(run_id="deadbeef") -> str:
    """A synthetic, fully-acked successful flash session ending VERIFIED_COMPLETE."""
    r = run_id
    lines = [
        "[run=%s t=+0.0ms] START  slave=7 hex=TrackAmplifier4.X.production.hex" % r,
        "[run=%s t=+0.5ms] TX  cmd=0xFA (CLIENT_CONNECTION_REQUEST) raw=aafa0055" % r,
        "[run=%s t=+0.6ms] RX  fb=(0x0A,0x06,0x07,0x64) taskid=CONTROLLER(10) taskcommand=CONNECTED(6) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+1.0ms] TX  cmd=0x67 (EXEC_MBUS_STATE_SLAVE_FW_FLASH) raw=aa670055" % r,
        "[run=%s t=+1.1ms] RX  fb=(0x1E,0x1F,0x06,0x64) taskid=FWHANDLER(30) taskcommand=FWHANDLERINIT(31) taskstate=CONNECTED(6) taskmessage=NONE(100)" % r,
        "[run=%s t=+2.0ms] TX  cmd=0x78 (EXEC_FW_STATE_RECEIVE_FW_FILE) raw=aa780055" % r,
        "[run=%s t=+2.1ms] RX  fb=(0x20,0x8C,0x07,0x64) taskid=FWFILEDOWNLOAD(32) taskcommand=FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY(140) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+3.0ms] TX  cmd=0x8D (FILEDOWNLOAD_STATE_FW_DATA_RECEIVE) len=66 raw=aa8d0000" % r,
        "[run=%s t=+3.1ms] RX  fb=(0x20,0x8E,0x07,0x64) taskid=FWFILEDOWNLOAD(32) taskcommand=FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE(142) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+3.2ms] RX  fb=(0x20,0x8F,0x07,0x68) taskid=FWFILEDOWNLOAD(32) taskcommand=FILEDOWNLOAD_STATE_FW_CHECKSUM(143) taskstate=DONE(7) taskmessage=RECEIVED_CHECKSUM_OK(104)" % r,
        "[run=%s t=+3.3ms] RX  fb=(0x1E,0x78,0x07,0x64) taskid=FWHANDLER(30) taskcommand=EXEC_FW_STATE_RECEIVE_FW_FILE(120) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+4.0ms] TX  cmd=0x79 (EXEC_FW_STATE_RECEIVE_CONFIG_WORD) raw=aa790055" % r,
        "[run=%s t=+4.1ms] RX  fb=(0x21,0x96,0x07,0x64) taskid=FWCONFIGWORDDOWNLOAD(33) taskcommand=CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY(150) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+5.0ms] TX  cmd=0x97 (CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE) len=14 raw=aa970000" % r,
        "[run=%s t=+5.1ms] RX  fb=(0x21,0x98,0x07,0x64) taskid=FWCONFIGWORDDOWNLOAD(33) taskcommand=CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE(152) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+5.2ms] RX  fb=(0x1E,0x79,0x07,0x64) taskid=FWHANDLER(30) taskcommand=EXEC_FW_STATE_RECEIVE_CONFIG_WORD(121) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+6.0ms] TX  cmd=0x7B (EXEC_FW_STATE_SELECT_SLAVE) raw=aa7b0755" % r,
        "[run=%s t=+7.0ms] TX  cmd=0x7D (EXEC_FW_STATE_ERASE_FLASH) raw=aa7d0055" % r,
        "[run=%s t=+7.1ms] STATE PRE_ERASE -> ERASE_REQUESTED_UNCONFIRMED" % r,
        "[run=%s t=+7.2ms] RX  fb=(0xC8,0xCA,0x07,0x64) taskid=ERASE_FLASH(200) taskcommand=ERASE_FLASH_RETURNED_OK(202) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+7.3ms] STATE ERASE_REQUESTED_UNCONFIRMED -> ERASE_CONFIRMED" % r,
        "[run=%s t=+8.0ms] TX  cmd=0x7E (EXEC_FW_STATE_WRITE_FLASH) raw=aa7e0055" % r,
        "[run=%s t=+8.1ms] STATE ERASE_CONFIRMED -> WRITE_IN_PROGRESS" % r,
        "[run=%s t=+8.2ms] RX  fb=(0xD2,0xD4,0x07,0x64) taskid=WRITE_FLASH(210) taskcommand=WRITE_FLASH_RETURNED_OK(212) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+8.3ms] STATE WRITE_IN_PROGRESS -> PROGRAMMING_UNCONFIRMED" % r,
        "[run=%s t=+9.0ms] TX  cmd=0x7F (EXEC_FW_STATE_WRITE_CONFIG) raw=aa7f0055" % r,
        "[run=%s t=+9.1ms] RX  fb=(0xDC,0xDE,0x07,0x64) taskid=WRITE_CONFIG(220) taskcommand=WRITE_CONFIG_RETURNED_OK(222) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+10.0ms] TX  cmd=0x80 (EXEC_FW_STATE_CHECK_CHECKSUM) raw=aa800055" % r,
        "[run=%s t=+10.1ms] RX  fb=(0xE6,0xE8,0x07,0x64) taskid=CHECK_CHECKSUM_CONFIG(230) taskcommand=CHECK_CHECKSUM_CONFIG_RETURNED_OK(232) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+11.0ms] TX  cmd=0x81 (EXEC_FW_STATE_SLAVE_RESET) raw=aa810055" % r,
        "[run=%s t=+11.1ms] RX  fb=(0xF0,0xF2,0x07,0x64) taskid=RESET_SLAVE(240) taskcommand=RESET_SLAVE_OK(242) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+11.2ms] STATE PROGRAMMING_UNCONFIRMED -> VERIFIED_COMPLETE" % r,
        "[run=%s t=+12.0ms] TX  cmd=0x82 (EXIT_SLAVExFWxHANDLER) raw=aa820055" % r,
        "[run=%s t=+13.0ms] TX  cmd=0x64 (EXEC_MBUS_STATE_SLAVES_ON) raw=aa640055" % r,
        "[run=%s t=+13.1ms] RX  fb=(0x14,0x02,0x07,0x64) taskid=MBUS(20) taskcommand=MBUS_STATE_SLAVES_ON(2) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+14.0ms] TX  cmd=0x6A (EXEC_MBUS_STATE_START_DATA_UPLOAD) raw=aa6a0055" % r,
        "[run=%s t=+14.1ms] RX  fb=(0x14,0x08,0x07,0x64) taskid=MBUS(20) taskcommand=MBUS_STATE_START_DATA_UPLOAD(8) taskstate=DONE(7) taskmessage=NONE(100)" % r,
        "[run=%s t=+15.0ms] TX  cmd=0x65 (EXEC_MBUS_STATE_SLAVE_DETECT) raw=aa650055" % r,
        "[run=%s t=+15.1ms] RX  fb=(0x14,0x04,0x07,0x64) taskid=MBUS(20) taskcommand=MBUS_STATE_SLAVE_DETECT(4) taskstate=DONE(7) taskmessage=NONE(100)" % r,
    ]
    return "\n".join(lines)


def test_successful_session_verified_complete():
    result = core.analyze_firmware_session(_successful_session())
    assert result["run_id"] == "deadbeef"
    assert result["final_state"] == FlashState.VERIFIED_COMPLETE
    assert result["evidence_level"] == core.VERIFIED
    assert result["timeout_or_error"] is False
    assert result["slave_id"] == 7
    assert result["hex_path"] == "TrackAmplifier4.X.production.hex"
    # ERASE + RESET acks were captured.
    ack_commands = [a["taskcommand"] for a in result["acks"]]
    assert 202 in ack_commands  # ERASE_FLASH_RETURNED_OK
    assert 242 in ack_commands  # RESET_SLAVE_OK


def test_erase_timeout_session_is_unknown_and_write_absent():
    r = "cafebabe"
    log = "\n".join([
        "[run=%s t=+0.0ms] START  slave=7 hex=TrackAmplifier4.X.production.hex" % r,
        "[run=%s t=+6.0ms] TX  cmd=0x7B (EXEC_FW_STATE_SELECT_SLAVE) raw=aa7b0755" % r,
        "[run=%s t=+7.0ms] TX  cmd=0x7D (EXEC_FW_STATE_ERASE_FLASH) raw=aa7d0055" % r,
        "[run=%s t=+7.1ms] STATE PRE_ERASE -> ERASE_REQUESTED_UNCONFIRMED" % r,
        "[run=%s t=+7.2ms] RX  fb=(0xC8,0xC9,0x09,0x64) taskid=ERASE_FLASH(200) taskcommand=ERASE_FLASH_RECEIVE_DATA_TIMEOUT(201) taskstate=ERROR(9) taskmessage=NONE(100)" % r,
        "[run=%s t=+7.3ms] STATE ERASE_REQUESTED_UNCONFIRMED -> FAILED_UNKNOWN_HARDWARE_STATE" % r,
    ])
    result = core.analyze_firmware_session(log)
    assert result["final_state"] == FlashState.FAILED_UNKNOWN_HARDWARE_STATE
    assert result["evidence_level"] == core.UNKNOWN
    assert result["timeout_or_error"] is True
    cmds = [c["cmd"] for c in result["commands"]]
    assert CMD["ERASE"] in cmds
    # WRITE (126) must never be emitted after the ERASE failure.
    assert CMD["WRITE"] not in cmds


def test_sent_but_never_acked_is_not_verified():
    r = "12345678"
    log = "\n".join([
        "[run=%s t=+0.0ms] START  slave=7 hex=TrackAmplifier4.X.production.hex" % r,
        "[run=%s t=+6.0ms] TX  cmd=0x7B (EXEC_FW_STATE_SELECT_SLAVE) raw=aa7b0755" % r,
        "[run=%s t=+7.0ms] TX  cmd=0x7D (EXEC_FW_STATE_ERASE_FLASH) raw=aa7d0055" % r,
        "[run=%s t=+7.1ms] STATE PRE_ERASE -> ERASE_REQUESTED_UNCONFIRMED" % r,
        # no ack, no further transitions: the ERASE was sent but never confirmed.
    ])
    result = core.analyze_firmware_session(log)
    assert result["final_state"] == FlashState.ERASE_REQUESTED_UNCONFIRMED
    assert result["evidence_level"] != core.VERIFIED
    assert result["evidence_level"] in (core.SENT, core.UNKNOWN)
    # No ERASE_FLASH_RETURNED_OK ack present.
    assert 202 not in [a["taskcommand"] for a in result["acks"]]
    # ERASE is recorded as sent, but nothing is promoted to a confirmed success.
    assert any(c["cmd"] == CMD["ERASE"] and c["sent"] for c in result["commands"])


def test_analyze_udp_trace_classifies_codes():
    trace = "\n".join([
        "(200, 201, 9, 100)",        # ERASE timeout
        "(210, 211, 9, 100)",        # WRITE timeout
        "(220, 222, 7, 100)",        # WRITE_CONFIG OK
        "(240, 242, 7, 100)",        # RESET OK
        "(200, 203, 9, 100)",        # ERASE NOK
        "(0xC8, 0xCA, 0x07, 0x64)",  # hex: ERASE OK (202)
    ])
    result = core.analyze_udp_trace(trace)
    classes = [f["classification"] for f in result["frames"]]
    assert classes == ["timeout", "timeout", "ok", "ok", "nok", "ok"]
    assert result["counts"]["ok"] == 3
    assert result["counts"]["timeout"] == 2
    assert result["counts"]["nok"] == 1
    assert result["counts"]["malformed"] == 0


def test_analyze_udp_trace_malformed_lines():
    trace = "\n".join([
        "(200, 201, 9, 100)",
        "this is not a tuple",
        "(1, 2, 3)",                  # only 3 fields
        "(0xC8, 0xCA, 0x07, 0x64)",
        "",                            # blank -> skipped, not malformed
    ])
    result = core.analyze_udp_trace(trace)
    assert result["counts"]["malformed"] == 2
    assert len(result["frames"]) == 2  # two valid frames survive
    assert result["counts"]["timeout"] == 1
    assert result["counts"]["ok"] == 1


def test_compare_protocol_traces_reports_difference():
    expected = [250, 103, 120, 123, 125, 126, 127, 128, 129, 130]
    actual = [250, 103, 120, 123, 125, 122, 127, 128, 129, 130]
    result = core.compare_protocol_traces(expected, actual)
    assert result["match"] is False
    assert {"index": 5, "expected": 126, "actual": 122} in result["differences"]


def test_compare_protocol_traces_match_and_length_mismatch():
    assert core.compare_protocol_traces([1, 2, 3], [1, 2, 3])["match"] is True
    r = core.compare_protocol_traces([1, 2], [1, 2, 3])
    assert r["match"] is False
    assert {"index": 2, "expected": None, "actual": 3} in r["differences"]


def test_capabilities_offline_flags():
    caps = core.capabilities()
    assert caps["offline_only"] is True
    assert caps["hardware_access"] is False
    assert set(caps["evidence_levels"]) == set(core.EVIDENCE_LEVELS)
