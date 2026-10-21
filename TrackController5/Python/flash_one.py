#!/usr/bin/env python
"""
flash_one.py -- minimal single-target flash driver for the legacy PIC32 master.

Drives the ORIGINAL legacy PIC32 manual per-slave flash primitives (the
FW-handler commands in `TrackController5/firmware/src/slavefwhandler.c`) to
flash EXACTLY ONE track amplifier (PIC18 slave).  This fills the gap left by
the legacy Python CLI, which only implements the auto `FLASH_ALL_SLAVES` (122)
path and a <3-slave recovery path (starting at `GET_BOOTLOADER_VERSION` 124) --
neither of which ever sends `SELECT_SLAVE` (123).

Emitted command sequence (byte values; each packet starts with header 0xAA):

    250  CLIENT_CONNECTION_REQUEST          (CONNECT -- wait for CONNECTED ack)
    103  EXEC_MBUS_STATE_SLAVE_FW_FLASH     (enter FW handler -- wait FWHANDLERINIT)
    120  EXEC_FW_STATE_RECEIVE_FW_FILE      (wait for FWFILEDOWNLOAD STANDBY ack)
    141  FILEDOWNLOAD_STATE_FW_DATA_RECEIVE x N   (N = 4-row / 64-byte packets)
        (after the last 141 the target acks with FW_CHECKSUM + RECEIVED_CHECKSUM_OK,
         then the FW handler acks RECEIVE_FW_FILE DONE)
    121  EXEC_FW_STATE_RECEIVE_CONFIG_WORD  (wait for FWCONFIGWORDDOWNLOAD STANDBY ack)
    151  CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE  (12-byte config word)
        (then FW handler acks RECEIVE_CONFIG_WORD DONE)
    123  EXEC_FW_STATE_SELECT_SLAVE         (data[0] = slave id)
    125  EXEC_FW_STATE_ERASE_FLASH
    126  EXEC_FW_STATE_WRITE_FLASH
    127  EXEC_FW_STATE_WRITE_CONFIG
    128  EXEC_FW_STATE_CHECK_CHECKSUM
    129  EXEC_FW_STATE_SLAVE_RESET
    130  EXIT_SLAVExFWxHANDLER
    -- post-flash read-only verification (Fix 2) --
    100  EXEC_MBUS_STATE_SLAVES_ON          (wait MBUS_STATE_SLAVES_ON ack)
    106  EXEC_MBUS_STATE_START_DATA_UPLOAD  (wait MBUS_STATE_START_DATA_UPLOAD ack)
    101  EXEC_MBUS_STATE_SLAVE_DETECT       (wait MBUS_STATE_SLAVE_DETECT ack, then
                                            collect SLAVEINFO frames 0xAA/0xFF)

It MUST NOT (and never does) emit 122 (FLASH_ALL_SLAVES) or 131/132.

The FW handler is entered via EXEC_MBUS_STATE_SLAVE_FW_FLASH (103): the
controller only routes MBUS commands + CLIENT_CONNECTION_REQUEST
(`controller.c:196-259`), and the FW handler runs only inside
MBUS_STATE_SLAVE_FW_FLASH (`mbus.c:307-318`).

Commands 123 (SELECT_SLAVE) and 130 (EXIT_SLAVExFWxHANDLER) emit NO host ack
(only LOG_Push, `slavefwhandler.c:253-273` / `:443-450`), so the driver uses a
bounded delay for them.  Commands 125-129 DO ack: the bootloader routines emit
`*_RETURNED_OK/DONE` (ERASE_FLASH_RETURNED_OK=202, WRITE_FLASH_RETURNED_OK=212,
WRITE_CONFIG_RETURNED_OK=222, CHECK_CHECKSUM_CONFIG_RETURNED_OK=232,
RESET_SLAVE_OK=242 -- `slavebootloaderroutines.c`; the C# RecoverSlavesStep waits
for exactly these), so the driver waits for each matching ack with a bounded
timeout and aborts on ERROR.

OFFLINE ONLY.  This module never talks to hardware on its own: the transport is
injected.  `--dry-run` uses a pure in-memory recording transport; the real
`Comm.DataAquisition` UDP transport is only used when `--dry-run` is NOT given.

Post-flash verification (Fix 2) sends 100/106/101 and, on hardware only,
collects the streamed SLAVEINFO frames (header 0xAA, command 0xFF, layout in
`PetitModbus.h:67-80`) and reports HR3 (identity), HR9 (version), HR10
(patch/proto) and HR11 (firmware checksum) for the amplifiers in REPORT_SLAVES.
Offline, the commands are still emitted; SLAVEINFO collection is a no-op so
`--dry-run` prints the planned readback commands without opening a socket.

F4 -- single-target sequence and the irreversible point
--------------------------------------------------------
SelectSlave (123) itself performs, in order (slavefwhandler.c:1177-1324):
Slaves_Disable_Off (case 0) -> 9-bit-modbus UART switch + backplane slot select
HOLDINGREG0 = SLOT1<<ShiftSlot1 (case 2) -> HOLDINGREG11 = 0 to force the
selected slave to reset INTO bootloader (case 4) -> release slot HOLDINGREG0 = 0
(case 6).  It does NOT switch to 8-bit: each bootloader command 125-129 performs
its own UART2_Config_8bit_Bootloader() at entry and UART2_Config_9bit_Modbus()
at exit (e.g. slavefwhandler.c:305-306/313-314), exactly as the full 122
FlashSequencer does (slavefwhandler.c:844-1127).  The driver therefore does NOT
need to (and does not) drive Slaves_Disable_Off or UART mode itself.

The first IRREVERSIBLE operation is EXEC_FW_STATE_ERASE_FLASH (125) ->
ERASExFLASH() -> CMD_ERASE_FLASH to the selected slave's bootloader (the old
firmware is destroyed).  Before 125 the driver has already validated, non-
destructively: slave id in 1..50, the HEX checksum parsed, and the entire
download acked OK (120 standby, every 141, FW_CHECKSUM=RECEIVED_CHECKSUM_OK,
FWHANDLER/120 DONE, 121 standby, 151, FWHANDLER/121 DONE).  SELECT_SLAVE (123)
emits no ack, so it is followed by a bounded delay before 125.

F5 -- timeout budget (no auto-reset, no auto-repeat)
----------------------------------------------------
(a) per-command ack timeout  : ACK_TIMEOUT (10 s) for CONNECT/103/120/121/MBUS.
(b) per-block (141) ack      : each 141 ack uses ACK_TIMEOUT (10 s).
(c) overall FW transfer      : FW_TRANSFER_TIMEOUT (120 s) bounds the whole
                               120->141xN->checksum->DONE phase.
(d) bootloader erase/write/checksum ack : BOOTLOADER_ACK_TIMEOUT (60 s) for
                               125-129 (source-supported: each bootloader op has
                               a 1 s internal timeout, slavebootloaderroutines.c:
                               114/226/355/467; WRITExFLASH streams 0x7800 bytes
                               in 64-byte blocks, so the 60 s budget is generous).
Every timeout raises FlashAbort and TERMINATES the attempt.  The driver never
auto-resets the target, never re-sends a failed command, and has no retry loop.

Feedback handling
-----------------
The driver correlates task_id + task_command + task_state + task_message in
every ack predicate; discards (clears) stale/non-matching frames; aborts
immediately on task_state == ERROR and names RECEIVED_WRONG_COMMAND explicitly;
targets only 192.168.1.193 (Comm.DataAquisition default, overridable via
--ip); never emits 122/131/132; and has no uncontrolled retry loop.
"""

from __future__ import print_function

import argparse
import io
import struct
import sys
import time
from contextlib import redirect_stdout

from Enum import (
    EnumBootloader,
    EnumClientCommands,
    EnumCommand,
    EnumMbusStatus,
    EnumTaskId,
    EnumTaskMessages,
    EnumTaskStates,
)
from bootload import BootLoader

# --------------------------------------------------------------------------- #
# Protocol constants (mirror TrackController5/firmware/src/enums.h)
# --------------------------------------------------------------------------- #
HEADER = 0xAA
FOOTER = 0x55

BOOTLOADER_OFFSET = 0x800   # SLAVE_BOOT_LOADER_OFFSET
PROGRAM_MEM_SIZE  = 0x8000  # SLAVE_FLASH_END
HEX_ROW_WIDTH     = 16      # LINEWIDTH
ROWS_PER_PACKET   = 4       # 4 rows == 64 bytes per 141 packet (BLOCKWIDTH)

# Minimum / maximum valid slave id (track amplifiers 1..50; 51..55 are
# backplane slaves and are rejected by SelectSlave -> SLAVE_ID_OUT_OF_BOUNDS).
MIN_SLAVE_ID = 1
MAX_SLAVE_ID = 50

# Seconds to wait for a *required* host ack (CONNECT / 103 / 120 / 121 / MBUS).
ACK_TIMEOUT = 10.0

# Seconds to wait for the slow bootloader-ack phase (125-129).  WRITExFLASH
# streams 0x7800 bytes in 64-byte blocks with a bootloader round-trip each
# (slavebootloaderroutines.c:271-377), so this needs a much larger budget than
# the fast FW-handler acks.
BOOTLOADER_ACK_TIMEOUT = 60.0

# Overall FW-download budget (F5): the whole 120 -> 141xN -> checksum -> DONE
# phase must complete within this wall-clock time.  Per-packet acks are bounded
# by ACK_TIMEOUT; this is the outer bound for a slow-but-progressing transfer.
FW_TRANSFER_TIMEOUT = 120.0

# SLAVEINFO upload frames are tagged with the 0xFF command byte (enums.h
# `#define SLAVEINFO 0xFF`; the legacy Comm.py names it EnumCommand.MODBUS).
SLAVEINFO_COMMAND = EnumCommand.MODBUS
SLAVEINFO_FRAME_LEN = 41          # 2 (hdr+cmd) + 39-byte SLAVE_INFO (see parse)
REPORT_SLAVES = (1, 3, 4, 6)      # amplifiers reported by the readback step

# Bounded inter-command delays (seconds).  123 (SELECT_SLAVE) and 130 (EXIT)
# emit NO host ack (slavefwhandler.c:253-273 / :443-450) so they use a bounded
# delay; 125-129 are acked (see _manual_primitives) so they need no sleep.
DEFAULT_DELAYS = {
    "connect":     0.5,   # after 250 (ack-wait already gates this)
    "select":      0.5,   # after 123 (no ack: modbus slot select + forced reset)
    "exit":        0.5,   # after 130 (no ack: return to MBUS handler)
    "drain":       0.5,   # stale-feedback drain window (pre-download idle read)
    "slaves_on":   1.0,   # after 100 (MBUS_STATE_SLAVES_BOOT_WAIT ~0.79 s)
    "data_upload": 0.5,   # after 106
    "detect":      2.0,   # after 101 (SLAVExDETECT + SLAVEINFO stream start)
    "collect":     2.0,   # SLAVEINFO collection window (hardware only)
}


class FlashAbort(RuntimeError):
    """Raised when a required ack is missing (documented stop condition)."""


def parse_slaveinfo_frame(raw):
    """Parse a 41-byte SLAVEINFO upload frame into a dict (or None).

    Wire layout (little-endian) mirrors `Comm.DataAquisition.ReadSerial` and the
    SLAVE_INFO struct in `TrackController5/firmware/src/modbus/PetitModbus.h:67-80`
    (1 byte pad after SlaveDetected for uint16 alignment; MbCommError is a C enum
    = 4 bytes):

        0          frame header  0xAA
        1          command       0xFF (SLAVEINFO)
        2          SLAVE_INFO.Header     (0xAA)
        3          SlaveNumber
        4          SlaveDetected
        5          (padding)
        6..17      HoldingReg[0..11]     (12 x uint16)
        18..19     MbReceiveCounter
        20..23     MbCommError           (uint32)
        24         MbExceptionCode
        25         SpiCommErrorCounter
        26         Footer                (0x55)

    Returns a dict with the fields the readback reports, or None if malformed.
    """
    if raw is None or len(raw) < SLAVEINFO_FRAME_LEN:
        return None
    try:
        d = struct.unpack("<6B12H2HLBBB", bytes(raw[:SLAVEINFO_FRAME_LEN]))
    except struct.error:
        return None
    if not (d[0] == HEADER and d[1] == SLAVEINFO_COMMAND
            and d[2] == HEADER and d[23] == FOOTER):
        return None
    return {
        "slave_number": d[3],
        "detected": bool(d[4]),
        "hr3": d[9],    # HoldingReg[3]  identity
        "hr9": d[15],   # HoldingReg[9]  version
        "hr10": d[16],  # HoldingReg[10] patch/proto
        "hr11": d[17],  # HoldingReg[11] firmware checksum
    }


def _enum_name(cls, value):
    """Human-readable name for an enum value (falls back to the int)."""
    for name, v in vars(cls).items():
        if v == value and not name.startswith("_"):
            return name
    return str(value)


def _taskid_name(value):
    """The 'taskid' field is overloaded: a TASK_ID (10/20/30) or a TASK_COMMAND
    (FWFILEDOWNLOAD=32, FWCONFIGWORDDOWNLOAD=33, ERASE_FLASH=200, ...)."""
    name = _enum_name(EnumTaskId, value)
    if name != str(value):
        return name
    return _enum_name(EnumCommand, value)


def _taskcommand_name(value):
    """The 'taskcommand' field is overloaded: TASK_COMMAND, TASK_STATE (e.g.
    CONNECTED=6) or MBUS_STATE (e.g. MBUS_STATE_SLAVES_ON=0x02)."""
    for cls in (EnumCommand, EnumTaskStates, EnumMbusStatus):
        name = _enum_name(cls, value)
        if name != str(value):
            return name
    return str(value)


def _default_log(msg):
    """Default logger: unbuffered one-line status (visible even on a hang)."""
    print(msg, flush=True)


def _decode_feedback(fb):
    """Render a (taskid, taskcommand, taskstate, taskmessage) tuple for logs."""
    return ("taskid=%s(%d) taskcommand=%s(%d) taskstate=%s(%d) "
            "taskmessage=%s(%d)"
            % (_taskid_name(fb[0]), fb[0],
               _taskcommand_name(fb[1]), fb[1],
               _enum_name(EnumTaskStates, fb[2]), fb[2],
               _enum_name(EnumTaskMessages, fb[3]), fb[3]))


class HexImage(object):
    """Intel HEX image parsed with the existing `bootload.BootLoader` code.

    Reuses `BootLoader.ReadHexFileToBuf`, `BootLoader.GetConfigData` and
    `BootLoader.GetFileCheckSum` verbatim so the checksum / config-word parsing
    stays identical to the legacy tool.
    """

    def __init__(self, hex_path):
        self.path = hex_path
        # BootLoader.__init__ opens a hard-coded default file and its parsing
        # methods print progress lines; suppress both, then point it at the
        # requested path.  Reuses BootLoader.ReadHexFileToBuf / GetConfigData /
        # GetFileCheckSum verbatim so the parsing stays identical to legacy.
        with redirect_stdout(io.StringIO()):
            bl = BootLoader(None)
        default_handle = getattr(bl, "file_object", None)
        if default_handle is not None:
            try:
                default_handle.close()
            except Exception:
                pass

        with redirect_stdout(io.StringIO()):
            bl.file_object = open(hex_path, "r", encoding="ascii")
            try:
                # Order matters: ReadHexFileToBuf seeks to 0 and reads the
                # program rows; GetConfigData then continues from the current
                # position to the extended-linear-address config record
                # (matches the legacy call order in StateMachines.py).
                self.rows_list = bl.ReadHexFileToBuf(BOOTLOADER_OFFSET,
                                                     PROGRAM_MEM_SIZE)
                self.config_word = bytes(bl.GetConfigData()[0])

                bl.file_object.seek(0)
                result = bl.GetFileCheckSum(BOOTLOADER_OFFSET, PROGRAM_MEM_SIZE)
            finally:
                bl.file_object.close()

        if not (isinstance(result, tuple) and len(result) == 2):
            raise FlashAbort("GetFileCheckSum returned an unexpected value")
        rc, cs = result
        if rc != EnumBootloader.COMMAND_SUCCESSFUL:
            raise FlashAbort("failed to compute FW checksum from HEX image")
        self.checksum = cs

    @property
    def row_packet_count(self):
        """Number of 141 data packets (4 rows each) required to upload the FW."""
        return (len(self.rows_list) + ROWS_PER_PACKET - 1) // ROWS_PER_PACKET


class FlashOneDriver(object):
    """Emit the manual per-slave flash sequence for exactly one amplifier.

    `transport` is duck-typed and must provide::

        send(packet: bytes) -> None
        poll() -> (taskid, taskcommand, taskstate, taskmessage) | None
        clear() -> None

    `sleep` is injectable so the offline test runs without real delays.
    """

    def __init__(self, transport, hex_image, slave_id,
                 sleep=time.sleep, delays=None, ack_timeout=ACK_TIMEOUT,
                 bootloader_ack_timeout=BOOTLOADER_ACK_TIMEOUT,
                 fw_transfer_timeout=FW_TRANSFER_TIMEOUT,
                 report_slaves=REPORT_SLAVES, log=None, clock=time.monotonic):
        self.transport = transport
        self.hex_image = hex_image
        self.slave_id = int(slave_id)
        self._sleep = sleep
        self._clock = clock
        self.ack_timeout = ack_timeout
        self.bootloader_ack_timeout = bootloader_ack_timeout
        self.fw_transfer_timeout = fw_transfer_timeout
        self.report_slaves = tuple(report_slaves)
        self._log = log if log is not None else _default_log

        self.delays = dict(DEFAULT_DELAYS)
        if delays:
            self.delays.update(delays)

        if not (MIN_SLAVE_ID <= self.slave_id <= MAX_SLAVE_ID):
            raise FlashAbort(
                "slave id %d out of bounds (valid %d..%d)"
                % (self.slave_id, MIN_SLAVE_ID, MAX_SLAVE_ID))

        self.rows_list = self.hex_image.rows_list
        self.config_word = self.hex_image.config_word
        self.file_checksum = self.hex_image.checksum

    # ------------------------------------------------------------------ #
    # Transport helpers
    # ------------------------------------------------------------------ #
    def _send_simple(self, command, data_byte=0x00):
        """4-byte command packet: [0xAA, command, data_byte, 0x55]."""
        pkt = bytes((HEADER, command, data_byte, FOOTER))
        self._log("TX  cmd=0x%02X (%s)" % (command, _enum_name(EnumCommand, command)))
        self.transport.send(pkt)
        return pkt

    def _send_payload(self, command, payload):
        """2-byte header + raw payload packet: [0xAA, command] + payload."""
        pkt = bytes((HEADER, command)) + bytes(payload)
        self._log("TX  cmd=0x%02X (%s) len=%d"
                  % (command, _enum_name(EnumCommand, command), len(pkt)))
        self.transport.send(pkt)
        return pkt

    def _wait_feedback(self, predicate, what, timeout=None):
        """Poll until `predicate(feedback)` matches or timeout -> FlashAbort.

        Aborts immediately (with a diagnostic) when the target reports
        taskstate == ERROR (e.g. RECEIVED_WRONG_COMMAND during the FW download)
        rather than silently discarding it and waiting out the ack timeout.
        Non-matching (non-ERROR) feedback is cleared and discarded.
        """
        timeout = self.ack_timeout if timeout is None else timeout
        deadline = self._clock() + timeout
        while self._clock() < deadline:
            fb = self.transport.poll()
            if fb is not None:
                self.transport.clear()
                self._log("RX  %s" % _decode_feedback(fb))
                if fb[2] == EnumTaskStates.ERROR:
                    raise FlashAbort(
                        "target returned ERROR while waiting for '%s': %s"
                        % (what, _decode_feedback(fb)))
                if predicate(fb):
                    return fb
            self._sleep(0.001)
        raise FlashAbort("timed out waiting for ack: %s" % what)

    def _drain_feedback(self, what, window=None):
        """Bounded idle read: discard stale feedback left in the host receive
        socket from a prior session/attempt (Defect 2 mitigation).

        Reads and clears pending frames until the socket is empty or `window`
        seconds elapse (polling at least once).  This does NOT send anything,
        so it does not alter the emitted command sequence.  A full clean
        power-cycle remains the safest mitigation for a stale command already
        inside the PIC32's receive mailbox (which the host cannot clear).
        """
        window = self.delays["drain"] if window is None else window
        deadline = self._clock() + window
        drained = 0
        while True:
            fb = self.transport.poll()
            if fb is None:
                break
            self.transport.clear()
            drained += 1
            self._log("Drain(%s): discarded %s" % (what, _decode_feedback(fb)))
            if self._clock() >= deadline:
                break
            self._sleep(0.005)
        if drained:
            self._log("Drain(%s): cleared %d stale frame(s)." % (what, drained))
        return drained

    # ------------------------------------------------------------------ #
    # Sequence steps
    # ------------------------------------------------------------------ #
    def run(self):
        self._drain_feedback("startup")
        self._connect()
        self._enter_fw_handler()
        self._receive_fw_file()
        self._receive_config_word()
        self._manual_primitives()
        self._verify_readback()
        return self

    def _connect(self):
        self._send_simple(EnumClientCommands.CLIENT_CONNECTION_REQUEST)  # 250
        self._wait_feedback(
            lambda f: (f[0] == EnumTaskId.CONTROLLER
                       and f[1] == EnumTaskStates.CONNECTED
                       and f[2] == EnumTaskStates.DONE),
            "CONNECT (CLIENT_CONNECTION_REQUEST)")
        self._sleep(self.delays["connect"])

    def _enter_fw_handler(self):
        # 103 -> controller routes to MBUS_STATE_SLAVE_FW_FLASH; the FW handler
        # acks with FWHANDLERINIT/CONNECTED (slavefwhandler.c:137, mbus.c:307).
        self._send_simple(EnumCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH)  # 103
        self._wait_feedback(
            lambda f: (f[0] == EnumTaskId.FWHANDLER
                       and f[1] == EnumCommand.FWHANDLERINIT
                       and f[2] == EnumTaskStates.CONNECTED),
            "SLAVE_FW_FLASH (FWHANDLERINIT)")

    def _receive_fw_file(self):
        # Bounded idle read after CONNECT/103 and before 120: clear any stale
        # status frames so they cannot be misread as FW-download acks.
        self._drain_feedback("pre-download")

        # Overall FW-transfer budget (F5c): the whole download phase is bounded
        # so a slow-but-progressing transfer still terminates.
        transfer_deadline = self._clock() + self.fw_transfer_timeout

        def _check_transfer_deadline():
            if self._clock() > transfer_deadline:
                raise FlashAbort("overall FW transfer timed out (%.1fs)"
                                 % self.fw_transfer_timeout)

        # 120 -> FW handler enters FwFileDownload(), acks STANDBY.
        self._send_simple(EnumCommand.EXEC_FW_STATE_RECEIVE_FW_FILE)  # 120
        self._wait_feedback(
            lambda f: (f[0] == EnumCommand.FWFILEDOWNLOAD
                       and f[1] == EnumCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY
                       and f[2] == EnumTaskStates.DONE),
            "RECEIVE_FW_FILE standby")

        # 141 x N -- 4 rows (64 bytes) per packet, ack between packets.
        rows = self.rows_list
        for i in range(0, len(rows), ROWS_PER_PACKET):
            _check_transfer_deadline()
            chunk = rows[i:i + ROWS_PER_PACKET]
            payload = b"".join(bytes(r[1]) for r in chunk)
            self._send_payload(
                EnumCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE, payload)  # 141
            self._wait_feedback(
                lambda f: (f[0] == EnumCommand.FWFILEDOWNLOAD
                           and f[1] in (EnumCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,
                                        EnumCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE)
                           and f[2] == EnumTaskStates.DONE),
                "RECEIVE_FW_FILE data ack")

        # Firmware self-checksums the received image and acks CHECKSUM_OK.
        _check_transfer_deadline()
        self._wait_feedback(
            lambda f: (f[0] == EnumCommand.FWFILEDOWNLOAD
                       and f[1] == EnumCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM
                       and f[2] == EnumTaskStates.DONE
                       and f[3] == EnumTaskMessages.RECEIVED_CHECKSUM_OK),
            "RECEIVE_FW_FILE checksum OK")

        # FW handler final "done" ack for 120.
        _check_transfer_deadline()
        self._wait_feedback(
            lambda f: (f[0] == EnumTaskId.FWHANDLER
                       and f[1] == EnumCommand.EXEC_FW_STATE_RECEIVE_FW_FILE
                       and f[2] == EnumTaskStates.DONE),
            "RECEIVE_FW_FILE done")

    def _receive_config_word(self):
        # 121 -> FW handler enters ConfigWordDownload(), acks STANDBY.
        self._send_simple(EnumCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD)  # 121
        self._wait_feedback(
            lambda f: (f[0] == EnumCommand.FWCONFIGWORDDOWNLOAD
                       and f[1] == EnumCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY
                       and f[2] == EnumTaskStates.DONE),
            "RECEIVE_CONFIG_WORD standby")

        # 151 -- the 12-byte config word.
        self._send_payload(
            EnumCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE,
            self.config_word)
        self._wait_feedback(
            lambda f: (f[0] == EnumCommand.FWCONFIGWORDDOWNLOAD
                       and f[1] == EnumCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE
                       and f[2] == EnumTaskStates.DONE),
            "RECEIVE_CONFIG_WORD data done")

        # FW handler final "done" ack for 121.
        self._wait_feedback(
            lambda f: (f[0] == EnumTaskId.FWHANDLER
                       and f[1] == EnumCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD
                       and f[2] == EnumTaskStates.DONE),
            "RECEIVE_CONFIG_WORD done")

    def _manual_primitives(self):
        # 123 SELECT_SLAVE: truly NO host ack (slavefwhandler.c:253-273 only
        # LOG_Push) -> bounded delay.
        self._send_simple(EnumCommand.EXEC_FW_STATE_SELECT_SLAVE, self.slave_id)
        self._sleep(self.delays["select"])

        # 125-129 DO ack: the bootloader routines emit *_RETURNED_OK/DONE
        # (slavebootloaderroutines.c: ERASExFLASH -> ERASE_FLASH_RETURNED_OK(202),
        # WRITExFLASH -> WRITE_FLASH_RETURNED_OK(212), WRITExCONFIG ->
        # WRITE_CONFIG_RETURNED_OK(222), CHECKxCHECKSUM ->
        # CHECK_CHECKSUM_CONFIG_RETURNED_OK(232), RESETxSLAVE -> RESET_SLAVE_OK(242)).
        # The C# RecoverSlavesStep waits for exactly these (task_id = the routine
        # enum, task_command = *_RETURNED_OK, task_state = DONE).  *_RETURNED_NOK
        # arrives with task_state = ERROR, which _wait_feedback aborts on.
        acked = [
            (EnumCommand.EXEC_FW_STATE_ERASE_FLASH,
             EnumCommand.ERASE_FLASH, EnumCommand.ERASE_FLASH_RETURNED_OK,
             "ERASE_FLASH"),
            (EnumCommand.EXEC_FW_STATE_WRITE_FLASH,
             EnumCommand.WRITE_FLASH, EnumCommand.WRITE_FLASH_RETURNED_OK,
             "WRITE_FLASH"),
            (EnumCommand.EXEC_FW_STATE_WRITE_CONFIG,
             EnumCommand.WRITE_CONFIG, EnumCommand.WRITE_CONFIG_RETURNED_OK,
             "WRITE_CONFIG"),
            (EnumCommand.EXEC_FW_STATE_CHECK_CHECKSUM,
             EnumCommand.CHECK_CHECKSUM_CONFIG,
             EnumCommand.CHECK_CHECKSUM_CONFIG_RETURNED_OK, "CHECK_CHECKSUM"),
            (EnumCommand.EXEC_FW_STATE_SLAVE_RESET,
             EnumCommand.RESET_SLAVE, EnumCommand.RESET_SLAVE_OK, "SLAVE_RESET"),
        ]
        for command, ack_taskid, ack_ok, name in acked:
            self._send_simple(command)
            self._wait_feedback(
                lambda f, tid=ack_taskid, ok=ack_ok: (
                    f[0] == tid and f[1] == ok and f[2] == EnumTaskStates.DONE),
                "%s *_RETURNED_OK" % name,
                timeout=self.bootloader_ack_timeout)

        # 130 EXIT_SLAVExFWxHANDLER: truly NO host ack (slavefwhandler.c:443-450
        # only sets state and returns) -> bounded delay.
        self._send_simple(EnumCommand.EXIT_SLAVExFWxHANDLER)
        self._sleep(self.delays["exit"])

    # ------------------------------------------------------------------ #
    # Post-flash readback verification (Fix 2, hardware only)
    # ------------------------------------------------------------------ #
    def _verify_readback(self):
        # F6 -- READ-ONLY verification.  This step is deliberately read-only and
        # MUST NOT enable any output/motion:
        #   100 SLAVES_ON        -> Slaves_Disable_Off() = clear the active-high
        #                           disable pin (system_config.h:516), i.e. power
        #                           the rail to a NEUTRAL, non-output-enabled state
        #                           (amplifiers idle; NOT driving the track).
        #   106 START_DATA_UPLOAD-> start the periodic modbus poll + SLAVEINFO
        #                           upload (read-only).
        #   101 SLAVE_DETECT     -> SLAVExDETECT() polls each slave's HRs
        #                           (slavestartup.c:72) -- read-only.
        # 100 is technically redundant after a successful flash (SelectSlave
        # already called Slaves_Disable_Off at slavefwhandler.c:1180) but it is
        # harmless (re-asserts neutral power) and matches the standard legacy
        # detect flow (TrackController.py: 100 -> 106 -> 101).  EXEC_MBUS_STATE_
        # SLAVE_ENABLE (105) / ENABLExAMPLIFIER (slavestartup.c:584) writes
        # HOLDINGREG1=0x8000 and actually ENABLES the outputs -- the driver
        # MUST NOT (and does not) send 105 or any motion/output command.
        self._send_simple(EnumCommand.EXEC_MBUS_STATE_SLAVES_ON)  # 100
        self._wait_feedback(
            lambda f: (f[0] == EnumTaskId.MBUS
                       and f[1] == EnumMbusStatus.MBUS_STATE_SLAVES_ON
                       and f[2] == EnumTaskStates.DONE),
            "SLAVES_ON")
        self._sleep(self.delays["slaves_on"])

        # 106 -> START_DATA_UPLOAD (MBUS acks MBUS_STATE_START_DATA_UPLOAD DONE)
        self._send_simple(EnumCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD)  # 106
        self._wait_feedback(
            lambda f: (f[0] == EnumTaskId.MBUS
                       and f[1] == EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD
                       and f[2] == EnumTaskStates.DONE),
            "START_DATA_UPLOAD")
        self._sleep(self.delays["data_upload"])

        # 101 -> SLAVE_DETECT (MBUS acks MBUS_STATE_SLAVE_DETECT DONE)
        self._send_simple(EnumCommand.EXEC_MBUS_STATE_SLAVE_DETECT)  # 101
        self._wait_feedback(
            lambda f: (f[0] == EnumTaskId.MBUS
                       and f[1] == EnumMbusStatus.MBUS_STATE_SLAVE_DETECT
                       and f[2] == EnumTaskStates.DONE),
            "SLAVE_DETECT")
        self._sleep(self.delays["detect"])

        self._collect_and_report_slaveinfo()

    def _collect_and_report_slaveinfo(self):
        poll_slaveinfo = getattr(self.transport, "poll_slaveinfo", None)
        if poll_slaveinfo is None:
            self._log("Readback: SLAVEINFO collection skipped (offline / no "
                      "slave-info source); planned commands 100/106/101 emitted.")
            return

        collected = {}
        deadline = self._clock() + self.delays["collect"]
        while self._clock() < deadline:
            records = poll_slaveinfo()
            if records:
                for rec in records:
                    sn = rec.get("slave_number")
                    if sn in self.report_slaves:
                        collected[sn] = rec
            self._sleep(0.01)
        self._report_slaveinfo(collected)

    def _report_slaveinfo(self, collected):
        self._log("Readback SLAVEINFO (amplifiers %s):"
                  % "/".join(str(s) for s in self.report_slaves))
        for sn in self.report_slaves:
            rec = collected.get(sn)
            if rec is None:
                self._log("  slave %2d: no SLAVEINFO received" % sn)
                continue
            self._log("  slave %2d: detected=%d identity=0x%04X version=0x%04X "
                      "patch=0x%04X checksum=0x%04X%s"
                      % (sn, 1 if rec["detected"] else 0, rec["hr3"], rec["hr9"],
                         rec["hr10"], rec["hr11"],
                         "  <== matches file" if rec["hr11"] == self.file_checksum
                         else ""))


# --------------------------------------------------------------------------- #
# Transports
# --------------------------------------------------------------------------- #
class DataAquisitionTransport(object):
    """Adapts the legacy `Comm.DataAquisition` (UDP) to the send/poll interface.

    Reuses `WriteSerial`-equivalent raw socket send and `ReadSerial`'s parsed
    `EthernetTarget` feedback exactly as the legacy tool does.
    """

    def __init__(self, dac):
        self._dac = dac

    def send(self, packet):
        self._dac.sock_trans.send(bytes(packet))

    def poll(self):
        self._dac.ReadSerial()
        e = self._dac.EthernetTarget
        if e.taskid == 0 and e.taskcommand == 0 and e.taskstate == 0:
            return None
        return (e.taskid, e.taskcommand, e.taskstate, e.taskmessage)

    def clear(self):
        self._dac.EthernetTarget.ClearOldData()

    def poll_slaveinfo(self):
        """Return parsed SLAVEINFO records for detected amplifiers.

        `Comm.DataAquisition.ReadSerial` already parses the streamed 0xAA/0xFF
        SLAVEINFO frames into `Trackamplifiers[]` (Comm.TrackAmplifier).  Convert
        the detected ones into the same dict shape as parse_slaveinfo_frame.
        """
        self._dac.ReadSerial()
        records = []
        for ta in self._dac.Trackamplifiers:
            if not getattr(ta, "SlaveDetected", 0):
                continue
            records.append({
                "slave_number": ta.SlaveNumber,
                "detected": bool(ta.SlaveDetected),
                "hr3": ta.HoldingReg[3],
                "hr9": ta.HoldingReg[9],
                "hr10": ta.HoldingReg[10],
                "hr11": ta.HoldingReg[11],
            })
        return records


class RecordingTransport(object):
    """Pure in-memory transport base: records every emitted packet."""

    def __init__(self):
        self.sent = []

    def send(self, packet):
        self.sent.append(bytes(packet))

    def poll(self):
        return None

    def clear(self):
        pass


class SelfAckTransport(RecordingTransport):
    """Offline loopback: records packets and self-acks exactly like the target.

    Mirrors the ack sequence produced by `controller.c` (CONNECTED) and
    `slavefwhandler.c` (FwFileDownload / ConfigWordDownload) so a `--dry-run`
    can drive the full sequence with no hardware.  123/125-130 emit no ack.
    """

    def __init__(self, row_packet_count):
        super(SelfAckTransport, self).__init__()
        self._row_packet_count = row_packet_count
        self._rows_sent = 0
        self._queue = []

    def send(self, packet):
        super(SelfAckTransport, self).send(packet)
        cmd = packet[1]
        _E = EnumCommand
        if cmd == EnumClientCommands.CLIENT_CONNECTION_REQUEST:
            self._queue.append((EnumTaskId.CONTROLLER, EnumTaskStates.CONNECTED,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_MBUS_STATE_SLAVE_FW_FLASH:
            # 103 -> FW handler INIT acks FWHANDLERINIT/CONNECTED
            self._queue.append((EnumTaskId.FWHANDLER, _E.FWHANDLERINIT,
                                EnumTaskStates.CONNECTED, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_FW_STATE_RECEIVE_FW_FILE:
            self._queue.append((_E.FWFILEDOWNLOAD,
                                _E.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE:
            self._rows_sent += 1
            if self._rows_sent >= self._row_packet_count:
                self._queue.append((_E.FWFILEDOWNLOAD,
                                    _E.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE,
                                    EnumTaskStates.DONE, EnumTaskMessages.NONE))
                self._queue.append((_E.FWFILEDOWNLOAD,
                                    _E.FILEDOWNLOAD_STATE_FW_CHECKSUM,
                                    EnumTaskStates.DONE,
                                    EnumTaskMessages.RECEIVED_CHECKSUM_OK))
                self._queue.append((EnumTaskId.FWHANDLER,
                                    _E.EXEC_FW_STATE_RECEIVE_FW_FILE,
                                    EnumTaskStates.DONE, EnumTaskMessages.NONE))
            else:
                self._queue.append((_E.FWFILEDOWNLOAD,
                                    _E.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY,
                                    EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_FW_STATE_RECEIVE_CONFIG_WORD:
            self._queue.append((_E.FWCONFIGWORDDOWNLOAD,
                                _E.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE:
            self._queue.append((_E.FWCONFIGWORDDOWNLOAD,
                                _E.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
            self._queue.append((EnumTaskId.FWHANDLER,
                                _E.EXEC_FW_STATE_RECEIVE_CONFIG_WORD,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_MBUS_STATE_SLAVES_ON:
            self._queue.append((EnumTaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVES_ON,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_MBUS_STATE_START_DATA_UPLOAD:
            self._queue.append((EnumTaskId.MBUS,
                                EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_MBUS_STATE_SLAVE_DETECT:
            self._queue.append((EnumTaskId.MBUS,
                                EnumMbusStatus.MBUS_STATE_SLAVE_DETECT,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_FW_STATE_ERASE_FLASH:
            self._queue.append((_E.ERASE_FLASH, _E.ERASE_FLASH_RETURNED_OK,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_FW_STATE_WRITE_FLASH:
            self._queue.append((_E.WRITE_FLASH, _E.WRITE_FLASH_RETURNED_OK,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_FW_STATE_WRITE_CONFIG:
            self._queue.append((_E.WRITE_CONFIG, _E.WRITE_CONFIG_RETURNED_OK,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_FW_STATE_CHECK_CHECKSUM:
            self._queue.append((_E.CHECK_CHECKSUM_CONFIG,
                                _E.CHECK_CHECKSUM_CONFIG_RETURNED_OK,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        elif cmd == _E.EXEC_FW_STATE_SLAVE_RESET:
            self._queue.append((_E.RESET_SLAVE, _E.RESET_SLAVE_OK,
                                EnumTaskStates.DONE, EnumTaskMessages.NONE))
        # 123 (SELECT_SLAVE) / 130 (EXIT): no host ack by design.

    def poll(self):
        return self._queue.pop(0) if self._queue else None


# --------------------------------------------------------------------------- #
# CLI
# --------------------------------------------------------------------------- #
def _extract_command(pkt):
    return pkt[1]


def _summarize(commands):
    """Collapse runs: [250,120,141,141,141] -> "250 120 141x3"."""
    out = []
    i = 0
    while i < len(commands):
        c = commands[i]
        j = i
        while j < len(commands) and commands[j] == c:
            j += 1
        n = j - i
        out.append(str(c) if n == 1 else "%dx%d" % (c, n))
        i = j
    return " ".join(out)


def _build_arg_parser():
    p = argparse.ArgumentParser(
        description="Flash exactly ONE track amplifier via the legacy PIC32 "
                    "manual FW-handler primitives (offline-safe: no hardware "
                    "is touched unless you omit --dry-run).")
    p.add_argument("--slave-id", type=int, required=True,
                   help="Target amplifier slave id (1..50).")
    p.add_argument("--hex", required=True,
                   help="Path to the TrackAmplifier production .hex file.")
    p.add_argument("--ip", default=None,
                   help="PIC32 master IP (default: Comm.DataAquisition default).")
    p.add_argument("--dry-run", action="store_true",
                   help="Do NOT open any socket; print the emitted command "
                        "sequence and exit (offline verification).")
    return p


def main(argv=None):
    args = _build_arg_parser().parse_args(argv)

    image = HexImage(args.hex)
    print("HEX image           : %s" % image.path, flush=True)
    print("FW rows             : %d" % len(image.rows_list), flush=True)
    print("FW data packets     : %d" % image.row_packet_count, flush=True)
    print("FW checksum         : 0x%04X" % image.checksum, flush=True)
    print("Config word         : %s" % image.config_word.hex(), flush=True)

    if args.dry_run:
        transport = SelfAckTransport(image.row_packet_count)
        driver = FlashOneDriver(
            transport, image, args.slave_id,
            sleep=lambda s: None, delays=dict.fromkeys(DEFAULT_DELAYS, 0.0),
            ack_timeout=1.0)
        driver.run()
        commands = [_extract_command(p) for p in transport.sent]
        print("Emitted command sequence (bytes):", flush=True)
        print(_summarize(commands), flush=True)
        return 0

    from Comm import DataAquisition  # deferred import: binds UDP sockets

    dac = DataAquisition(56)
    if args.ip:
        dac.IPAddr = args.ip
        dac.sock_trans.connect((dac.IPAddr, dac.UDP_PORT_TRANS))

    driver = FlashOneDriver(DataAquisitionTransport(dac), image, args.slave_id)
    try:
        driver.run()
    except FlashAbort as exc:
        # F5: terminate cleanly -- NO auto-reset, NO auto-repeat.
        print("FLASH ABORTED: %s" % exc, flush=True)
        return 1
    print("Flash-one sequence emitted for slave %d." % args.slave_id, flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
