"""Siebwalde K8 SLAVEINFO decoder — typed, OFFLINE, read-only, MCP-independent.

Decodes a *recorded* PIC32 SLAVEINFO (``0xFF``) amplifier frame into typed
fields and derives a register view for the four identity-relevant holding
registers (HR7 / HR11 / HR21 / HR22) with an explicit freshness/status
classification. It is strictly OFFLINE and READ-ONLY:

* it never imports ``socket`` / ``serial`` / ``Comm`` / ``DataAquisition`` /
  ``flash_one`` (whose import graph reaches the UDP transport);
* it never opens a socket, a serial port, or any hardware connection;
* it never *sends* anything — there is no encoder and no transmit path here;
* it only parses and reasons over bytes already captured on disk.

Wire format (mirrored, read-only) from the source-of-truth:

* ``SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Comm/TrackSlaveInfoFrame.cs``
  (``TrackSlaveInfoFrame.Length`` = 63 bytes, ``Encode``) and
  ``TrackController5/firmware/src/modbus/PetitModbus.h`` (``SLAVE_INFO`` struct)::

      [0]  = 0xAA  HEADER
      [1]  = 0xFF  SLAVEINFO command
      [2]  = 0xAA  inner SLAVE_INFO.Header
      [3]  = SlaveNumber
      [4]  = SlaveDetected
      [5]  = struct padding byte (ignored)
      [6..51]  = HoldingReg[0..22]   (23 x uint16, little-endian)
      [52..53] = MbReceiveCounter    (uint16, little-endian)
      [54..55] = MbSentCounter       (uint16, little-endian)
      [56..59] = MbCommError         (SLAVE_DATA enum = uint32, little-endian)
      [60]  = MbExceptionCode        (uint8)
      [61]  = SpiCommErrorCounter    (uint8)
      [62]  = 0x55  FOOTER

Note (source-contract): the C ``SLAVE_INFO`` struct has ``sizeof == 64`` (61
meaningful bytes + 3 trailing alignment-padding bytes because ``SLAVE_DATA`` is
a 4-byte enum), while the C# receiver decodes a 63-byte frame (2 envelope bytes
+ the 61 meaningful struct bytes) and ignores the 3 trailing padding bytes.
This decoder mirrors the authoritative C# 63-byte frame exactly.

Register map (mirrored from ``TrackAmplifierRegisters.cs`` and
``docs/K8-SOURCE-RUNTIME-MAP.md``):

* HR7  ``HR_IDENTITY`` (identity / map-version / capability)
* HR11 ``HR_SW_CHECKSUM`` (application checksum — stable anchor in both maps)
* HR21 ``HR_FW_VERSION`` (MAJOR<<8 | MINOR — 1.0.0-only)
* HR22 ``HR_FW_VERSION_PATCH_PROTO`` (PATCH<<8 | PROTOCOL — 1.0.0-only)

Freshness/status classification (advisory, fail-closed):

    PHYSICALLY_CONFIRMED   RESERVED for live physical evidence — never produced
                           by this offline decoder.
    HISTORICALLY_OBSERVED  a value corroborates the historical record (a build
                           identity, NOT proof of current deployment).
    SOURCE_ONLY            defined in the source register map but never observed
                           at runtime (initial / never-observed state).
    UNSUPPORTED            the register is not implemented by the identified
                           firmware (legacy reads HR7/HR21/HR22 as 0 — they are
                           NOT a version and never decoded as zero-version).
    STALE                  an out-of-order / delayed (older) observation; must
                           never overwrite newer confirmed state.
    UNKNOWN                cannot be determined (erased / unknown build / wrong
                           identity) — fail-closed.
    COMM_FAILURE           the observation attempt failed (malformed frame, or a
                           frame whose ``MbCommError != OK`` makes its register
                           values untrustworthy, e.g. read-0 during BUSY).

The decoder distinguishes the **last-observed** raw value from the
**last-confirmed-valid** value and never turns a stale / unsupported / missing
value into a positive firmware identity.
"""

from __future__ import annotations

import struct
from dataclasses import dataclass
from typing import Dict, Optional, Tuple

# --------------------------------------------------------------------------- #
# Wire constants (mirrored, read-only)
# --------------------------------------------------------------------------- #

_HEADER = 0xAA
_SLAVEINFO = 0xFF
_FOOTER = 0x55

# SLAVE_DATA enum values (TrackController5/firmware/src/modbus/PetitModbus.h).
SLAVE_DATA_NAMES = {
    0x00: "IDLE",
    0x01: "BUSY",
    0x02: "OK",
    0x03: "NOK",
    0x04: "TIMEOUT",
    0x05: "EXCEPTION",
    0x06: "UNKNOWN",
}
_SLAVE_DATA_OK = 0x02

# Frame layout (TrackSlaveInfoFrame.cs + SLAVE_INFO struct in PetitModbus.h).
REGISTER_COUNT = 23
REGISTER_OFFSET = 6
FRAME_LENGTH = REGISTER_OFFSET + 2 * REGISTER_COUNT + 11  # 63

# Register addresses (TrackAmplifierRegisters.cs + enums.h HR_* defines).
HR_IDENTITY = 7
HR_SW_CHECKSUM = 11
HR_FW_VERSION = 21
HR_FW_VERSION_PATCH_PROTO = 22

# Physical-device address ranges (TrackAmplifierAddress.cs).
MIN_TRACK_AMPLIFIER = 1
MAX_TRACK_AMPLIFIER = 50
MIN_BACKPLANE_SLAVE = 51
MAX_BACKPLANE_SLAVE = 55

# The four identity-relevant registers exposed by the register view, in order.
_REGISTER_VIEW = (
    ("HR7", "identity", HR_IDENTITY),
    ("HR11", "sw_checksum", HR_SW_CHECKSUM),
    ("HR21", "fw_version", HR_FW_VERSION),
    ("HR22", "fw_version_patch_proto", HR_FW_VERSION_PATCH_PROTO),
)

# --------------------------------------------------------------------------- #
# Freshness / status vocabulary
# --------------------------------------------------------------------------- #

PHYSICALLY_CONFIRMED = "PHYSICALLY_CONFIRMED"
HISTORICALLY_OBSERVED = "HISTORICALLY_OBSERVED"
SOURCE_ONLY = "SOURCE_ONLY"
UNSUPPORTED = "UNSUPPORTED"
STALE = "STALE"
UNKNOWN = "UNKNOWN"
COMM_FAILURE = "COMM_FAILURE"

STATUSES = (
    PHYSICALLY_CONFIRMED,
    HISTORICALLY_OBSERVED,
    SOURCE_ONLY,
    UNSUPPORTED,
    STALE,
    UNKNOWN,
    COMM_FAILURE,
)

# Historical application-checksum inventory. A checksum is a **build identity**,
# NOT proof of current deployment (docs/K8-SOURCE-RUNTIME-MAP.md §3).
CHECKSUM_INVENTORY = {
    0x251F: "legacy Offset",
    0xE906: "Phase-1 1.0.0",
    0xFFFF: "erased",
}


# --------------------------------------------------------------------------- #
# Exceptions
# --------------------------------------------------------------------------- #


class SlaveInfoDecodeError(ValueError):
    """Raised when a raw SLAVEINFO frame is malformed, corrupt, or incomplete.

    A decode failure is always fail-closed: it is surfaced as COMM_FAILURE and
    never produces a (partial) identity.
    """


# --------------------------------------------------------------------------- #
# Small helpers (pure, side-effect free)
# --------------------------------------------------------------------------- #


def is_track_amplifier_address(slave: int) -> bool:
    """True when ``slave`` is a legitimate track amplifier address (1..50).

    Mirrors ``TrackAmplifierAddress.IsTrackAmplifierAddress``.
    """
    return MIN_TRACK_AMPLIFIER <= slave <= MAX_TRACK_AMPLIFIER


def is_backplane_configuration_slave(slave: int) -> bool:
    """True when ``slave`` is a backplane/configuration module (51..55)."""
    return MIN_BACKPLANE_SLAVE <= slave <= MAX_BACKPLANE_SLAVE


def is_compatible_identity(identity: int) -> bool:
    """Shape check for a compatible Phase-1 identity register (HR7).

    Mirrors ``TrackAmplifierRegisters.HasCompatibleIdentityFields``:
    ``(identity & 0xFF) == 1`` (MAP_VERSION 1), ``((identity >> 8) & 0xF) == 1``
    (DEVICE_CLASS 1), and ``(identity & 0xF000) != 0`` (capability bits present).
    This is a shape check only, NOT identity authorization.
    """
    return (identity & 0xFF) == 1 and ((identity >> 8) & 0xF) == 1 and (identity & 0xF000) != 0


def decode_identity(identity: int) -> dict:
    """Typed decode of an HR7 identity word (informational, source-mirrored).

    Returns ``{map_version, device_class, capabilities}`` per the Phase-1
    identity layout documented in ``SimulatedTrackAmplifier.cs``.
    """
    return {
        "map_version": identity & 0xFF,
        "device_class": (identity >> 8) & 0xF,
        "capabilities": (identity >> 12) & 0xF,
    }


def decode_fw_version(hr21: int) -> dict:
    """Decode HR21 (MAJOR<<8 | MINOR) into ``{major, minor}``."""
    return {"major": (hr21 >> 8) & 0xFF, "minor": hr21 & 0xFF}


def decode_fw_patch_proto(hr22: int) -> dict:
    """Decode HR22 (PATCH<<8 | PROTOCOL) into ``{patch, protocol}``."""
    return {"patch": (hr22 >> 8) & 0xFF, "protocol": hr22 & 0xFF}


def checksum_label(checksum: int) -> Optional[str]:
    """Return the historical build label for an HR11 checksum, or ``None``."""
    return CHECKSUM_INVENTORY.get(checksum)


def slave_data_name(value: int) -> str:
    """Return the SLAVE_DATA name for an ``MbCommError`` value (or the int)."""
    return SLAVE_DATA_NAMES.get(value, str(value))


# --------------------------------------------------------------------------- #
# Typed models
# --------------------------------------------------------------------------- #


@dataclass(frozen=True)
class RegisterField:
    """One identity register's value + freshness/status classification.

    ``value`` is the **last-observed** raw register value; ``confirmed`` is the
    **last-confirmed-valid** value (``None`` when the value was not a positive,
    internally-consistent identity observation). The two are deliberately kept
    distinct so a stale / unsupported / missing value is never promoted.
    """

    register: str
    semantic: str
    value: Optional[int] = None
    decoded: Optional[dict] = None
    checksum_label: Optional[str] = None
    status: str = SOURCE_ONLY
    confirmed: Optional[int] = None

    def to_dict(self) -> dict:
        return {
            "register": self.register,
            "semantic": self.semantic,
            "value": self.value,
            "decoded": self.decoded,
            "checksum_label": self.checksum_label,
            "status": self.status,
            "confirmed": self.confirmed,
        }


@dataclass(frozen=True)
class SlaveInfoFrame:
    """A fully decoded (structurally valid) SLAVEINFO frame.

    ``holding_registers`` is a tuple of 23 ints (HR0..HR22). The source of
    truth has 23 registers (``PetitModbus.h`` ``NUMBER_OF_HOLDING_PETITREGISTERS
    = 23`` / ``TrackAmplifierRegisters.Count = 23``); HR21 and HR22 exist only
    because the map is 23 registers wide, so a 12-register decode would be wrong.
    """

    slave_number: int
    slave_detected: int
    holding_registers: Tuple[int, ...]
    mb_receive_counter: int
    mb_sent_counter: int
    mb_comm_error: int
    mb_exception_code: int
    spi_comm_error_counter: int

    def to_dict(self) -> dict:
        return {
            "slave_number": self.slave_number,
            "slave_detected": self.slave_detected,
            "holding_registers": list(self.holding_registers),
            "mb_receive_counter": self.mb_receive_counter,
            "mb_sent_counter": self.mb_sent_counter,
            "mb_comm_error": self.mb_comm_error,
            "mb_comm_error_name": slave_data_name(self.mb_comm_error),
            "mb_exception_code": self.mb_exception_code,
            "spi_comm_error_counter": self.spi_comm_error_counter,
        }

    def register_view(self) -> "SlaveInfoRegisterView":
        """Derive the HR7/HR11/HR21/HR22 register view for this single frame."""
        return build_register_view(self)


@dataclass(frozen=True)
class SlaveInfoRegisterView:
    """The HR7/HR11/HR21/HR22 register view with per-register classification."""

    fields: Tuple[RegisterField, ...]
    identity_compatible: bool = False

    def field(self, register: str) -> Optional[RegisterField]:
        for f in self.fields:
            if f.register == register:
                return f
        return None

    def to_dict(self) -> dict:
        return {
            "fields": [f.to_dict() for f in self.fields],
            "identity_compatible": self.identity_compatible,
            "firmware_identity": self.firmware_identity(),
        }

    def firmware_identity(self) -> dict:
        """Fail-closed firmware-identity summary (never PHYSICALLY_CONFIRMED).

        ``positive_identity`` is True only for a complete, internally-consistent
        1.0.0 identity observation (compatible HR7 *and* decodable HR21/HR22).
        A legacy (0x251F), erased (0xFFFF), unsupported, stale, missing, unknown
        or comm-failed observation is never a positive identity. ``confirmation``
        is always ``None`` here: PHYSICALLY_CONFIRMED is reserved for live
        evidence and this offline decoder never emits it.
        """
        hr7 = self.field("HR7")
        hr11 = self.field("HR11")
        hr21 = self.field("HR21")
        hr22 = self.field("HR22")

        version = None
        if (
            self.identity_compatible
            and hr21 is not None
            and hr21.decoded is not None
            and hr22 is not None
            and hr22.decoded is not None
        ):
            version = {
                "major": hr21.decoded["major"],
                "minor": hr21.decoded["minor"],
                "patch": hr22.decoded["patch"],
                "protocol": hr22.decoded["protocol"],
            }

        return {
            "positive_identity": self.identity_compatible and version is not None,
            "identity_compatible": self.identity_compatible,
            "checksum": hr11.value if hr11 is not None else None,
            "checksum_label": hr11.checksum_label if hr11 is not None else None,
            "firmware_version": version,
            "confirmation": None,  # PHYSICALLY_CONFIRMED is reserved for live evidence.
        }


# --------------------------------------------------------------------------- #
# Pure decode + register-view classification
# --------------------------------------------------------------------------- #


def decode_slaveinfo_frame(raw: object) -> SlaveInfoFrame:
    """Decode one raw SLAVEINFO frame (bytes) into a :class:`SlaveInfoFrame`.

    The frame must be exactly ``FRAME_LENGTH`` (63) bytes with the correct
    header / command / inner-header / footer. Any other shape (short, corrupt,
    incomplete, wrong command, wrong footer) raises :class:`SlaveInfoDecodeError`
    — the decoder never returns a partial result.
    """
    if not isinstance(raw, (bytes, bytearray)):
        raise SlaveInfoDecodeError(
            "SLAVEINFO frame must be bytes, got %s" % type(raw).__name__
        )
    data = bytes(raw)

    if len(data) != FRAME_LENGTH:
        raise SlaveInfoDecodeError(
            "SLAVEINFO frame must be exactly %d bytes, got %d"
            % (FRAME_LENGTH, len(data))
        )
    if data[0] != _HEADER:
        raise SlaveInfoDecodeError(
            "bad header 0x%02X (expected 0xAA)" % data[0]
        )
    if data[1] != _SLAVEINFO:
        raise SlaveInfoDecodeError(
            "bad command 0x%02X (expected SLAVEINFO 0xFF)" % data[1]
        )
    if data[2] != _HEADER:
        raise SlaveInfoDecodeError(
            "bad inner header 0x%02X (expected 0xAA)" % data[2]
        )
    if data[-1] != _FOOTER:
        raise SlaveInfoDecodeError(
            "bad footer 0x%02X (expected 0x55)" % data[-1]
        )

    holding = tuple(
        struct.unpack_from("<H", data, REGISTER_OFFSET + 2 * i)[0]
        for i in range(REGISTER_COUNT)
    )
    mb_receive_counter = struct.unpack_from("<H", data, 52)[0]
    mb_sent_counter = struct.unpack_from("<H", data, 54)[0]
    mb_comm_error = struct.unpack_from("<I", data, 56)[0]
    mb_exception_code = data[60]
    spi_comm_error_counter = data[61]

    return SlaveInfoFrame(
        slave_number=data[3],
        slave_detected=data[4],
        holding_registers=holding,
        mb_receive_counter=mb_receive_counter,
        mb_sent_counter=mb_sent_counter,
        mb_comm_error=mb_comm_error,
        mb_exception_code=mb_exception_code,
        spi_comm_error_counter=spi_comm_error_counter,
    )


def build_register_view(frame: SlaveInfoFrame) -> SlaveInfoRegisterView:
    """Classify HR7/HR11/HR21/HR22 for one decoded frame (fail-closed).

    Rules:

    * A frame whose ``MbCommError != OK`` is a COMM_FAILURE for every register:
      the HoldingReg values are untrustworthy (e.g. read-0 during BUSY) and are
      never interpreted as an identity.
    * HR11 (checksum) is compared against the historical inventory for a *label*
      only: ``0x251F``/``0xE906`` -> HISTORICALLY_OBSERVED (build identity, not
      current-deployment proof), ``0xFFFF`` -> UNKNOWN ("erased"), anything else
      -> UNKNOWN. The decoder never asserts a unique current deployment.
    * HR7 (identity): compatible -> HISTORICALLY_OBSERVED; legacy ``0`` on a
      ``0x251F`` build -> UNSUPPORTED; otherwise (non-amplifier / wrong /
      erased / unknown) -> UNKNOWN.
    * HR21/HR22 (version) are decoded ONLY when a 1.0.0 identity is supported
      (compatible HR7 on a track-amplifier address); otherwise UNSUPPORTED —
      a zero value is never decoded as a zero-version.
    """
    regs = frame.holding_registers
    hr7 = regs[HR_IDENTITY]
    hr11 = regs[HR_SW_CHECKSUM]
    hr21 = regs[HR_FW_VERSION]
    hr22 = regs[HR_FW_VERSION_PATCH_PROTO]

    is_amp = is_track_amplifier_address(frame.slave_number)
    comm_ok = frame.mb_comm_error == _SLAVE_DATA_OK

    if not comm_ok:
        fields = (
            RegisterField("HR7", "identity", hr7, None, None, COMM_FAILURE, None),
            RegisterField("HR11", "sw_checksum", hr11, None, checksum_label(hr11), COMM_FAILURE, None),
            RegisterField("HR21", "fw_version", hr21, None, None, COMM_FAILURE, None),
            RegisterField("HR22", "fw_version_patch_proto", hr22, None, None, COMM_FAILURE, None),
        )
        return SlaveInfoRegisterView(fields, identity_compatible=False)

    identity_compatible = is_amp and is_compatible_identity(hr7)

    # HR11 — checksum inventory comparison (build identity, not deployment proof).
    hr11_label = checksum_label(hr11)
    if hr11 == 0xFFFF:
        hr11_status = UNKNOWN  # erased: no application firmware (fail-closed)
        hr11_confirmed = None
    elif hr11_label is not None:  # 0x251F (legacy) or 0xE906 (Phase-1 1.0.0)
        hr11_status = HISTORICALLY_OBSERVED
        hr11_confirmed = hr11
    else:
        hr11_status = UNKNOWN  # unknown build (an arbitrary legacy build is not excluded)
        hr11_confirmed = None

    # HR7 — identity.
    if not is_amp:
        hr7_status = UNKNOWN  # identity semantics do not apply to a backplane/config slave
        hr7_confirmed = None
        hr7_decoded = None
    elif identity_compatible:
        hr7_status = HISTORICALLY_OBSERVED
        hr7_confirmed = hr7
        hr7_decoded = decode_identity(hr7)
    elif hr7 == 0 and hr11 == 0x251F:
        hr7_status = UNSUPPORTED  # legacy firmware has no identity register (reads 0)
        hr7_confirmed = None
        hr7_decoded = None
    else:
        hr7_status = UNKNOWN  # wrong / erased / unknown identity — fail-closed
        hr7_confirmed = None
        hr7_decoded = None

    # HR21 / HR22 — decode only when a 1.0.0 identity is supported.
    if identity_compatible:
        hr21_status = HISTORICALLY_OBSERVED
        hr21_decoded = decode_fw_version(hr21)
        hr21_confirmed = hr21
        hr22_status = HISTORICALLY_OBSERVED
        hr22_decoded = decode_fw_patch_proto(hr22)
        hr22_confirmed = hr22
    else:
        hr21_status = UNSUPPORTED
        hr21_decoded = None
        hr21_confirmed = None
        hr22_status = UNSUPPORTED
        hr22_decoded = None
        hr22_confirmed = None

    fields = (
        RegisterField("HR7", "identity", hr7, hr7_decoded, None, hr7_status, hr7_confirmed),
        RegisterField("HR11", "sw_checksum", hr11, None, hr11_label, hr11_status, hr11_confirmed),
        RegisterField("HR21", "fw_version", hr21, hr21_decoded, None, hr21_status, hr21_confirmed),
        RegisterField("HR22", "fw_version_patch_proto", hr22, hr22_decoded, None, hr22_status, hr22_confirmed),
    )
    return SlaveInfoRegisterView(fields, identity_compatible=identity_compatible)


# --------------------------------------------------------------------------- #
# Stateful replay accumulator (reordered/delayed observation handling)
# --------------------------------------------------------------------------- #


@dataclass(frozen=True)
class ReplayObservation:
    """The result of one ``SlaveInfoDecoder.observe`` call."""

    decode_ok: bool
    stale: bool
    sequence: Optional[int]
    error: Optional[str]
    slave_number: Optional[int]
    slave_detected: Optional[int]
    mb_receive_counter: Optional[int]
    mb_sent_counter: Optional[int]
    register_view: Optional[SlaveInfoRegisterView]
    last_observed: Dict[str, Optional[int]]
    last_confirmed_valid: Dict[str, Optional[int]]
    status: Dict[str, str]

    def to_dict(self) -> dict:
        return {
            "decode_ok": self.decode_ok,
            "stale": self.stale,
            "sequence": self.sequence,
            "error": self.error,
            "slave_number": self.slave_number,
            "slave_detected": self.slave_detected,
            "mb_receive_counter": self.mb_receive_counter,
            "mb_sent_counter": self.mb_sent_counter,
            "register_view": self.register_view.to_dict() if self.register_view else None,
            "last_observed": self.last_observed,
            "last_confirmed_valid": self.last_confirmed_valid,
            "status": self.status,
        }


class SlaveInfoDecoder:
    """Stateful, offline, read-only SLAVEINFO replay accumulator.

    Consumes a sequence of raw frames and maintains, per identity register
    (HR7/HR11/HR21/HR22), the **last-observed** raw value and the
    **last-confirmed-valid** value plus a current status. Frames may carry an
    explicit ``sequence`` number so that a reordered/delayed (older) observation
    is classified ``STALE`` and never overwrites newer confirmed state. A
    malformed frame is a ``COMM_FAILURE`` and never mutates observed/confirmed
    state. Before any observation every register is ``SOURCE_ONLY``.
    """

    def __init__(self) -> None:
        self._highest_sequence: Optional[int] = None
        self._last_observed: Dict[str, Optional[int]] = {}
        self._last_confirmed: Dict[str, Optional[int]] = {}
        self._last_status: Dict[str, str] = {
            reg: SOURCE_ONLY for reg, _, _ in _REGISTER_VIEW
        }

    @property
    def last_observed(self) -> Dict[str, Optional[int]]:
        return dict(self._last_observed)

    @property
    def last_confirmed_valid(self) -> Dict[str, Optional[int]]:
        return dict(self._last_confirmed)

    @property
    def status(self) -> Dict[str, str]:
        return dict(self._last_status)

    def _resolve_sequence(self, sequence: Optional[int]) -> Optional[int]:
        if sequence is None:
            if self._highest_sequence is None:
                sequence = 0
            else:
                sequence = self._highest_sequence + 1
        return sequence

    def observe(self, raw: object, sequence: Optional[int] = None) -> ReplayObservation:
        """Decode and fold one frame into the replay state.

        ``sequence`` orders observations: a value lower than the highest seen so
        far is a reordered/delayed observation and is reported ``STALE`` (its
        register values are NOT folded into last-observed / last-confirmed). When
        omitted, a monotonic arrival-order sequence is assigned automatically.
        """
        seq = self._resolve_sequence(sequence)

        try:
            frame = decode_slaveinfo_frame(raw)
        except SlaveInfoDecodeError as exc:
            # Fail-closed: a decode failure is COMM_FAILURE for every register
            # and never mutates observed/confirmed state.
            for reg, _, _ in _REGISTER_VIEW:
                self._last_status[reg] = COMM_FAILURE
            return ReplayObservation(
                decode_ok=False,
                stale=False,
                sequence=seq,
                error=str(exc),
                slave_number=None,
                slave_detected=None,
                mb_receive_counter=None,
                mb_sent_counter=None,
                register_view=None,
                last_observed=self.last_observed,
                last_confirmed_valid=self.last_confirmed_valid,
                status=self.status,
            )

        stale = (
            self._highest_sequence is not None
            and seq is not None
            and seq < self._highest_sequence
        )

        view = frame.register_view()

        if stale:
            # An older observation: mark every register STALE and do not fold
            # its values into last-observed / last-confirmed. It must never be
            # treated as a (downgraded) current identity.
            stale_fields = tuple(
                RegisterField(f.register, f.semantic, f.value, None, None, STALE, None)
                for f in view.fields
            )
            stale_view = SlaveInfoRegisterView(stale_fields, identity_compatible=False)
            return ReplayObservation(
                decode_ok=True,
                stale=True,
                sequence=seq,
                error=None,
                slave_number=frame.slave_number,
                slave_detected=frame.slave_detected,
                mb_receive_counter=frame.mb_receive_counter,
                mb_sent_counter=frame.mb_sent_counter,
                register_view=stale_view,
                last_observed=self.last_observed,
                last_confirmed_valid=self.last_confirmed_valid,
                status=self.status,
            )

        # Not stale: fold in the fresh (or first) observation.
        self._highest_sequence = seq
        for reg, _, _ in _REGISTER_VIEW:
            self._last_observed[reg] = view.field(reg).value
            self._last_confirmed[reg] = view.field(reg).confirmed
            self._last_status[reg] = view.field(reg).status

        return ReplayObservation(
            decode_ok=True,
            stale=False,
            sequence=seq,
            error=None,
            slave_number=frame.slave_number,
            slave_detected=frame.slave_detected,
            mb_receive_counter=frame.mb_receive_counter,
            mb_sent_counter=frame.mb_sent_counter,
            register_view=view,
            last_observed=self.last_observed,
            last_confirmed_valid=self.last_confirmed_valid,
            status=self.status,
        )
