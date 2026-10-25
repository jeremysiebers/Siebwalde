"""Deterministic offline tests for the cold-start SLAVEINFO capture classifier.

Builds a synthetic legacy 12-register 82-byte frame (mirroring the deployed
firmware layout) and a Phase-1 23-register 63-byte frame, then asserts
``classify_frame`` detects the layout by footer position and decodes the
identity-relevant fields correctly and fail-closed. No sockets, serial ports,
hardware, or transmit path are exercised (the socket helpers are not imported
by these tests beyond ``classify_frame`` / ``format_frame``).

Run with:
    tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
"""

from __future__ import annotations

import struct
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics import cold_start_slaveinfo_capture as cap  # noqa: E402


def _legacy_frame(slave, hr7, hr11, mb_comm_error=0x02):
    """Build an 82-byte legacy 12-register SLAVEINFO frame (deployed firmware)."""
    buf = bytearray()
    buf += bytes([0xAA, 0xFF, 0xAA, slave & 0xFF, 0x00, 0x00])  # detected=0 (latch)
    regs = [0] * 12
    regs[7] = hr7
    regs[11] = hr11
    for v in regs:
        buf += struct.pack("<H", v & 0xFFFF)
    buf += struct.pack("<H", 0)  # MbReceiveCounter
    buf += struct.pack("<H", 0)  # MbSentCounter
    buf += struct.pack("<I", mb_comm_error & 0xFFFFFFFF)
    buf += bytes([0x00, 0x00, 0x55])  # MbExceptionCode, SpiCommErrorCounter, Footer
    buf += b"\x00" * (82 - len(buf))  # trailing udpTrans_t garbage
    return bytes(buf)


def test_classify_legacy_layout_footer_at_40():
    frame = _legacy_frame(1, 0x6649, 0x251F, 0x02)
    info = cap.classify_frame(frame)
    assert info["kind"] == "SLAVEINFO"
    assert info["envelope"] == "legacy_12reg_82B"
    assert info["slave_number"] == 1
    assert info["hr7_identity"] == 0x6649
    assert info["hr11_checksum"] == 0x251F
    assert info["mb_comm_error"] == 0x02
    assert info["mb_comm_error_name"] == "OK"
    assert info["footer_ok"] is True


def test_classify_phase1_layout_footer_at_62():
    # 63-byte Phase-1 frame: 23 registers, footer at 62.
    buf = bytearray()
    buf += bytes([0xAA, 0xFF, 0xAA, 3, 1, 0x00])
    regs = [0] * 23
    regs[7] = 0xF101
    regs[11] = 0xE906
    for v in regs:
        buf += struct.pack("<H", v & 0xFFFF)
    buf += struct.pack("<H", 0)  # MbReceiveCounter
    buf += struct.pack("<H", 0)  # MbSentCounter
    buf += struct.pack("<I", 0x02)  # MbCommError OK
    buf += bytes([0x00, 0x00, 0x55])
    frame = bytes(buf)
    assert len(frame) == 63
    info = cap.classify_frame(frame)
    assert info["envelope"] == "phase1_23reg_63B"
    assert info["slave_number"] == 3
    assert info["hr7_identity"] == 0xF101
    assert info["hr11_checksum"] == 0xE906
    assert info["footer_ok"] is True


def test_classify_legacy_hr11_zero_is_not_presence():
    # An empty legacy slave (e.g. 49) reports HR11=0 and MbCommError=IDLE.
    frame = _legacy_frame(49, 0, 0, 0x00)
    info = cap.classify_frame(frame)
    assert info["kind"] == "SLAVEINFO"
    assert info["hr11_checksum"] == 0
    assert info["mb_comm_error"] == 0x00
    assert info["mb_comm_error_name"] == "IDLE"


def test_format_frame_renders_legacy():
    frame = _legacy_frame(6, 0x6655, 0x251F, 0x02)
    text = cap.format_frame(cap.classify_frame(frame))
    assert "legacy_12reg_82B" in text
    assert "HR11=0x251F" in text
    assert "mbCommError=OK" in text
