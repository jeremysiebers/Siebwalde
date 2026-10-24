"""Deterministic offline replay tests for the K8 SLAVEINFO decoder.

Builds synthetic SLAVEINFO frames (mirroring the C# ``TrackSlaveInfoFrame.Encode``
63-byte layout) and asserts the decoder classifies each correctly and fail-closed.
No sockets, no serial ports, no hardware, and no transmit path are exercised.

Run with:
    tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
"""

from __future__ import annotations

import struct
import sys
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics import slaveinfo_decoder as sid  # noqa: E402


# --------------------------------------------------------------------------- #
# Synthetic frame encoder (test-only; mirrors TrackSlaveInfoFrame.Encode)
# --------------------------------------------------------------------------- #


def _encode_frame(
    slave: int,
    detected: int,
    registers: list,
    received: int = 0,
    sent: int = 0,
    error: int = 0x02,  # SLAVE_DATA_OK
    exception: int = 0,
    spi_error: int = 0,
) -> bytes:
    """Build a 63-byte SLAVEINFO frame, byte-identical to the C# encoder."""
    assert len(registers) == sid.REGISTER_COUNT, "Phase-1 SLAVEINFO carries exactly 23 registers"
    buf = bytearray()
    buf += bytes([0xAA, 0xFF, 0xAA, slave & 0xFF, detected & 0xFF, 0x00])
    for value in registers:
        buf += struct.pack("<H", value & 0xFFFF)
    buf += struct.pack("<H", received & 0xFFFF)
    buf += struct.pack("<H", sent & 0xFFFF)
    buf += struct.pack("<I", error & 0xFFFFFFFF)
    buf += bytes([exception & 0xFF, spi_error & 0xFF, 0x55])
    return bytes(buf)


def _registers(hr7=0, hr11=0, hr21=0, hr22=0, **overrides) -> list:
    """A 23-register list with defaults and optional per-HR overrides."""
    regs = [0] * sid.REGISTER_COUNT
    regs[sid.HR_IDENTITY] = hr7
    regs[sid.HR_SW_CHECKSUM] = hr11
    regs[sid.HR_FW_VERSION] = hr21
    regs[sid.HR_FW_VERSION_PATCH_PROTO] = hr22
    for key, value in overrides.items():
        regs[key] = value
    return regs


# --------------------------------------------------------------------------- #
# Frame shape / decode
# --------------------------------------------------------------------------- #


def test_frame_is_23_registers_not_12():
    """The C SLAVE_INFO struct and C# map carry 23 registers, not 12."""
    assert sid.REGISTER_COUNT == 23
    assert sid.FRAME_LENGTH == 63
    # HR21/HR22 are only addressable because the map is 23 registers wide.
    assert sid.HR_FW_VERSION == 21
    assert sid.HR_FW_VERSION_PATCH_PROTO == 22


def test_decode_roundtrip_fields():
    regs = _registers(hr7=0xF101, hr11=0xE906, hr21=0x0100, hr22=0x0001)
    frame = sid.decode_slaveinfo_frame(
        _encode_frame(1, 1, regs, received=123, sent=456, error=0x02, exception=5, spi_error=6)
    )
    assert frame.slave_number == 1
    assert frame.slave_detected == 1
    assert len(frame.holding_registers) == 23
    assert frame.holding_registers[sid.HR_IDENTITY] == 0xF101
    assert frame.holding_registers[sid.HR_SW_CHECKSUM] == 0xE906
    assert frame.mb_receive_counter == 123
    assert frame.mb_sent_counter == 456
    assert frame.mb_comm_error == 0x02
    assert frame.mb_exception_code == 5
    assert frame.spi_comm_error_counter == 6


# --------------------------------------------------------------------------- #
# Legacy / Phase-1 / erased classification
# --------------------------------------------------------------------------- #


def test_legacy_0x251f_frame():
    """Legacy 0x251F: HR11 historically observed, HR7/HR21/HR22 unsupported."""
    regs = _registers(hr7=0, hr11=0x251F, hr21=0, hr22=0)
    frame = sid.decode_slaveinfo_frame(_encode_frame(3, 1, regs))
    view = frame.register_view()

    hr11 = view.field("HR11")
    assert hr11.status == sid.HISTORICALLY_OBSERVED
    assert hr11.checksum_label == "legacy Offset"
    assert hr11.confirmed == 0x251F

    hr7 = view.field("HR7")
    assert hr7.status == sid.UNSUPPORTED
    assert hr7.confirmed is None

    hr21 = view.field("HR21")
    hr22 = view.field("HR22")
    assert hr21.status == sid.UNSUPPORTED
    assert hr21.decoded is None  # not decoded as a zero-version
    assert hr22.status == sid.UNSUPPORTED
    assert hr22.decoded is None

    assert view.identity_compatible is False
    assert view.firmware_identity()["positive_identity"] is False


def test_phase1_0xE906_frame():
    """Phase-1 1.0.0: HR7 compatible, HR11 labelled, HR21/HR22 decoded."""
    regs = _registers(hr7=0xF101, hr11=0xE906, hr21=0x0100, hr22=0x0001)
    frame = sid.decode_slaveinfo_frame(_encode_frame(1, 1, regs))
    view = frame.register_view()

    assert view.identity_compatible is True

    hr11 = view.field("HR11")
    assert hr11.status == sid.HISTORICALLY_OBSERVED
    assert hr11.checksum_label == "Phase-1 1.0.0"
    assert hr11.confirmed == 0xE906

    hr7 = view.field("HR7")
    assert hr7.status == sid.HISTORICALLY_OBSERVED
    assert hr7.confirmed == 0xF101
    assert hr7.decoded == {"map_version": 1, "device_class": 1, "capabilities": 0xF}

    hr21 = view.field("HR21")
    assert hr21.status == sid.HISTORICALLY_OBSERVED
    assert hr21.decoded == {"major": 1, "minor": 0}

    hr22 = view.field("HR22")
    assert hr22.status == sid.HISTORICALLY_OBSERVED
    assert hr22.decoded == {"patch": 0, "protocol": 1}

    identity = view.firmware_identity()
    assert identity["positive_identity"] is True
    assert identity["firmware_version"] == {"major": 1, "minor": 0, "patch": 0, "protocol": 1}
    # Offline decode never asserts physical confirmation.
    assert identity["confirmation"] is None


def test_erased_0xffff_frame():
    """Erased 0xFFFF: no application firmware, no positive identity (fail-closed)."""
    regs = _registers(hr7=0, hr11=0xFFFF, hr21=0, hr22=0)
    frame = sid.decode_slaveinfo_frame(_encode_frame(4, 1, regs))
    view = frame.register_view()

    hr11 = view.field("HR11")
    assert hr11.checksum_label == "erased"
    assert hr11.status == sid.UNKNOWN
    assert hr11.confirmed is None

    hr7 = view.field("HR7")
    assert hr7.status == sid.UNKNOWN

    assert view.field("HR21").status == sid.UNSUPPORTED
    assert view.field("HR22").status == sid.UNSUPPORTED
    assert view.firmware_identity()["positive_identity"] is False


def test_wrong_amplifier_identity_fails_closed():
    """A non-compatible HR7 is never a positive identity."""
    regs = _registers(hr7=0x1234, hr11=0xE906, hr21=0x0100, hr22=0x0001)
    frame = sid.decode_slaveinfo_frame(_encode_frame(1, 1, regs))
    view = frame.register_view()

    assert sid.is_compatible_identity(0x1234) is False
    assert view.field("HR7").status == sid.UNKNOWN
    assert view.field("HR7").confirmed is None
    # 1.0.0 identity not supported -> version registers are UNSUPPORTED, not zero.
    assert view.field("HR21").status == sid.UNSUPPORTED
    assert view.field("HR21").decoded is None
    assert view.field("HR22").status == sid.UNSUPPORTED
    assert view.identity_compatible is False
    assert view.firmware_identity()["positive_identity"] is False


def test_backplane_slave_has_no_amplifier_identity():
    """Backplane/config slaves (51..55) are not identity-checked."""
    regs = _registers(hr7=0xF101, hr11=0xE906, hr21=0x0100, hr22=0x0001)
    frame = sid.decode_slaveinfo_frame(_encode_frame(51, 1, regs))
    view = frame.register_view()

    assert sid.is_track_amplifier_address(51) is False
    assert sid.is_backplane_configuration_slave(51) is True
    assert view.field("HR7").status == sid.UNKNOWN
    assert view.identity_compatible is False
    assert view.firmware_identity()["positive_identity"] is False


# --------------------------------------------------------------------------- #
# Malformed frames -> fail-closed
# --------------------------------------------------------------------------- #


@pytest.mark.parametrize(
    "mutate",
    [
        lambda b: b[:10],                    # short frame
        lambda b: b + b"\x00",               # too long
        lambda b: b[:1] + b"\x00" + b[2:],   # wrong header
        lambda b: b[:1] + b"\xFE" + b[2:],   # wrong command (not SLAVEINFO)
        lambda b: b[:2] + b"\x00" + b[3:],   # wrong inner header
        lambda b: b[:-1] + b"\x00",          # wrong footer
    ],
    ids=["short", "too-long", "bad-header", "bad-command", "bad-inner-header", "bad-footer"],
)
def test_malformed_frame_raises(mutate):
    regs = _registers(hr7=0xF101, hr11=0xE906, hr21=0x0100, hr22=0x0001)
    good = _encode_frame(1, 1, regs)
    with pytest.raises(sid.SlaveInfoDecodeError):
        sid.decode_slaveinfo_frame(mutate(good))


def test_incomplete_packet_fails_closed():
    """A truncated packet (cut mid-registers) is rejected, not partially decoded."""
    regs = _registers(hr7=0xF101, hr11=0xE906, hr21=0x0100, hr22=0x0001)
    full = _encode_frame(1, 1, regs)
    truncated = full[:30]  # cut inside the holding-register block
    with pytest.raises(sid.SlaveInfoDecodeError):
        sid.decode_slaveinfo_frame(truncated)


def test_non_bytes_input_rejected():
    with pytest.raises(sid.SlaveInfoDecodeError):
        sid.decode_slaveinfo_frame("not bytes")


# --------------------------------------------------------------------------- #
# Communication failure
# --------------------------------------------------------------------------- #


def test_comm_failure_when_mb_comm_error_not_ok():
    """MbCommError != OK makes the register values untrustworthy (COMM_FAILURE)."""
    regs = _registers(hr7=0, hr11=0, hr21=0, hr22=0)  # read-0 during BUSY
    frame = sid.decode_slaveinfo_frame(
        _encode_frame(1, 1, regs, error=0x01)  # SLAVE_DATA_BUSY
    )
    view = frame.register_view()
    for reg_name in ("HR7", "HR11", "HR21", "HR22"):
        assert view.field(reg_name).status == sid.COMM_FAILURE
        assert view.field(reg_name).confirmed is None
    assert view.firmware_identity()["positive_identity"] is False


def test_offline_decoder_never_emits_physically_confirmed():
    """PHYSICALLY_CONFIRMED is reserved and never produced by the decoder."""
    frames = [
        _encode_frame(1, 1, _registers(hr7=0xF101, hr11=0xE906, hr21=0x0100, hr22=0x0001)),
        _encode_frame(3, 1, _registers(hr7=0, hr11=0x251F, hr21=0, hr22=0)),
        _encode_frame(4, 1, _registers(hr7=0, hr11=0xFFFF, hr21=0, hr22=0)),
    ]
    for raw in frames:
        view = sid.decode_slaveinfo_frame(raw).register_view()
        assert all(
            f.status != sid.PHYSICALLY_CONFIRMED for f in view.fields
        )


# --------------------------------------------------------------------------- #
# Reordered / delayed observation (stateful replay)
# --------------------------------------------------------------------------- #


def test_reordered_delayed_observation_is_stale_and_does_not_downgrade():
    decoder = sid.SlaveInfoDecoder()
    assert decoder.status["HR7"] == sid.SOURCE_ONLY  # initial: defined but never observed

    phase1 = _encode_frame(1, 1, _registers(hr7=0xF101, hr11=0xE906, hr21=0x0100, hr22=0x0001))
    first = decoder.observe(phase1, sequence=5)
    assert first.stale is False
    assert first.register_view.identity_compatible is True
    assert decoder.last_confirmed_valid["HR11"] == 0xE906
    assert decoder.last_confirmed_valid["HR7"] == 0xF101

    # A delayed legacy observation (older sequence) arrives out of order.
    legacy = _encode_frame(3, 1, _registers(hr7=0, hr11=0x251F, hr21=0, hr22=0))
    second = decoder.observe(legacy, sequence=3)
    assert second.stale is True
    assert second.register_view.identity_compatible is False
    assert all(f.status == sid.STALE for f in second.register_view.fields)

    # The stale frame must NOT overwrite the newer confirmed identity.
    assert decoder.last_confirmed_valid["HR11"] == 0xE906
    assert decoder.last_confirmed_valid["HR7"] == 0xF101
    assert decoder.last_observed["HR11"] == 0xE906
    assert decoder.status["HR11"] == sid.HISTORICALLY_OBSERVED


def test_decode_failure_is_comm_failure_and_preserves_state():
    decoder = sid.SlaveInfoDecoder()
    phase1 = _encode_frame(1, 1, _registers(hr7=0xF101, hr11=0xE906, hr21=0x0100, hr22=0x0001))
    decoder.observe(phase1, sequence=1)

    result = decoder.observe(b"\x00\x01", sequence=2)  # malformed
    assert result.decode_ok is False
    assert result.register_view is None
    assert decoder.status["HR7"] == sid.COMM_FAILURE
    assert decoder.status["HR11"] == sid.COMM_FAILURE
    # Previously confirmed values survive a comm failure.
    assert decoder.last_confirmed_valid["HR11"] == 0xE906


def test_unknown_checksum_is_unknown_not_positive():
    regs = _registers(hr7=0xF101, hr11=0x1234, hr21=0x0100, hr22=0x0001)
    frame = sid.decode_slaveinfo_frame(_encode_frame(1, 1, regs))
    view = frame.register_view()
    hr11 = view.field("HR11")
    assert hr11.checksum_label is None
    assert hr11.status == sid.UNKNOWN
    assert hr11.confirmed is None
