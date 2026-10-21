#!/usr/bin/env python
"""
Offline unit tests for flash_one.py (single-target manual flash driver).

No hardware, no sockets: a MockTransport records the emitted bytes and feeds a
scripted firmware-ack sequence back to the driver.

Run with:  python test_flash_one.py          (uses the stdlib unittest runner)
"""

import os
import struct
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from flash_one import (  # noqa: E402
    DEFAULT_DELAYS,
    FlashAbort,
    FlashOneDriver,
    HexImage,
    parse_slaveinfo_frame,
)

HEX_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                        "TrackAmplifier4.X.production.hex")

# Feedback tuple layout: (taskid, taskcommand, taskstate, taskmessage)
# Values from TrackController5/firmware/src/enums.h.
CONTROLLER, CONNECTED, DONE = 10, 6, 7
ERROR = 9
FWHANDLER = 30
MBUS = 20
FWHANDLERINIT = 31
FWFILEDOWNLOAD = 32
FWCONFIGWORDDOWNLOAD = 33
STANDBY_FW = 140
DATA_DONE = 142
FW_CHECKSUM = 143
STANDBY_CFG = 150
CFG_DONE = 152
NONE = 100
RECEIVED_WRONG_COMMAND = 101
CHECKSUM_OK = 104
MBUS_STATE_SLAVES_ON = 0x02
MBUS_STATE_SLAVE_DETECT = 0x04
MBUS_STATE_START_DATA_UPLOAD = 0x08

# Bootloader-routine acks (taskid = routine enum, taskcommand = *_RETURNED_OK/NOK).
ERASE_FLASH = 200
ERASE_FLASH_RETURNED_OK = 202
ERASE_FLASH_RETURNED_NOK = 203
WRITE_FLASH = 210
WRITE_FLASH_RETURNED_OK = 212
WRITE_FLASH_RETURNED_NOK = 213
WRITE_CONFIG_ROUTINE = 220
WRITE_CONFIG_RETURNED_OK = 222
WRITE_CONFIG_RETURNED_NOK = 223
CHECK_CHECKSUM_CONFIG = 230
CHECK_CHECKSUM_CONFIG_RETURNED_OK = 232
CHECK_CHECKSUM_CONFIG_RETURNED_NOK = 233
RESET_SLAVE = 240
RESET_SLAVE_OK = 242
RESET_SLAVE_NOK = 243

CONNECT = 250
SLAVE_FW_FLASH = 103
RECEIVE_FW_FILE = 120
FW_DATA = 141
RECEIVE_CONFIG_WORD = 121
CFG_DATA = 151
SELECT_SLAVE = 123
ERASE = 125
WRITE = 126
WRITE_CONFIG = 127
CHECK = 128
RESET = 129
EXIT = 130
SLAVES_ON = 100
START_DATA_UPLOAD = 106
SLAVE_DETECT = 101
FORBIDDEN = (122, 131, 132)


class MockTransport(object):
    """Records emitted bytes and auto-generates firmware acks per command.

    `fail_fw_download=True` reproduces the observed hardware failure: the 120
    phase acks with STANDBY/ERROR/RECEIVED_WRONG_COMMAND (and the FW handler
    acks FWHANDLER/120/ERROR), i.e. the driver must abort, not hang.

    `fail_bootloader=True` makes 125 (ERASE) ack with ERASE_FLASH_RETURNED_NOK/
    ERROR instead of ERASE_FLASH_RETURNED_OK/DONE, i.e. the driver must abort.
    """

    def __init__(self, row_packet_count, ack_120=True, ack_121=True,
                 fail_fw_download=False, fail_bootloader=False):
        self.sent = []
        self._queue = []
        self._row_packet_count = row_packet_count
        self._rows_sent = 0
        self._ack_120 = ack_120
        self._ack_121 = ack_121
        self._fail_fw_download = fail_fw_download
        self._fail_bootloader = fail_bootloader

    def send(self, packet):
        packet = bytes(packet)
        self.sent.append(packet)
        cmd = packet[1]
        if cmd == CONNECT:
            self._queue.append((CONTROLLER, CONNECTED, DONE, NONE))
        elif cmd == SLAVE_FW_FLASH:
            # 103 -> FW handler INIT acks FWHANDLERINIT/CONNECTED
            self._queue.append((FWHANDLER, FWHANDLERINIT, CONNECTED, NONE))
        elif cmd == RECEIVE_FW_FILE:
            if self._fail_fw_download:
                # slavefwhandler.c: FwFileDownload() case 1 -> RECEIVED_WRONG_COMMAND
                self._queue.append((FWFILEDOWNLOAD, STANDBY_FW, ERROR,
                                    RECEIVED_WRONG_COMMAND))
                self._queue.append((FWHANDLER, RECEIVE_FW_FILE, ERROR, NONE))
            elif self._ack_120:
                self._queue.append((FWFILEDOWNLOAD, STANDBY_FW, DONE, NONE))
        elif cmd == FW_DATA:
            self._rows_sent += 1
            if self._rows_sent >= self._row_packet_count:
                self._queue.append((FWFILEDOWNLOAD, DATA_DONE, DONE, NONE))
                if self._ack_120:
                    self._queue.append((FWFILEDOWNLOAD, FW_CHECKSUM, DONE, CHECKSUM_OK))
                    self._queue.append((FWHANDLER, RECEIVE_FW_FILE, DONE, NONE))
            else:
                self._queue.append((FWFILEDOWNLOAD, STANDBY_FW, DONE, NONE))
        elif cmd == RECEIVE_CONFIG_WORD:
            if self._ack_121:
                self._queue.append((FWCONFIGWORDDOWNLOAD, STANDBY_CFG, DONE, NONE))
        elif cmd == CFG_DATA:
            if self._ack_121:
                self._queue.append((FWCONFIGWORDDOWNLOAD, CFG_DONE, DONE, NONE))
                self._queue.append((FWHANDLER, RECEIVE_CONFIG_WORD, DONE, NONE))
        elif cmd == ERASE:
            if self._fail_bootloader:
                self._queue.append((ERASE_FLASH, ERASE_FLASH_RETURNED_NOK, ERROR, NONE))
            else:
                self._queue.append((ERASE_FLASH, ERASE_FLASH_RETURNED_OK, DONE, NONE))
        elif cmd == WRITE:
            self._queue.append((WRITE_FLASH, WRITE_FLASH_RETURNED_OK, DONE, NONE))
        elif cmd == WRITE_CONFIG:
            self._queue.append((WRITE_CONFIG_ROUTINE, WRITE_CONFIG_RETURNED_OK, DONE, NONE))
        elif cmd == CHECK:
            self._queue.append((CHECK_CHECKSUM_CONFIG, CHECK_CHECKSUM_CONFIG_RETURNED_OK, DONE, NONE))
        elif cmd == RESET:
            self._queue.append((RESET_SLAVE, RESET_SLAVE_OK, DONE, NONE))
        elif cmd == SLAVES_ON:
            self._queue.append((MBUS, MBUS_STATE_SLAVES_ON, DONE, NONE))
        elif cmd == START_DATA_UPLOAD:
            self._queue.append((MBUS, MBUS_STATE_START_DATA_UPLOAD, DONE, NONE))
        elif cmd == SLAVE_DETECT:
            self._queue.append((MBUS, MBUS_STATE_SLAVE_DETECT, DONE, NONE))
        # 123 (SELECT_SLAVE) / 130 (EXIT): no host ack by design.

    def poll(self):
        return self._queue.pop(0) if self._queue else None

    def clear(self):
        pass


def commands_of(transport):
    return [pkt[1] for pkt in transport.sent]


def make_driver(transport, image, slave_id=7, **kw):
    opts = dict(sleep=lambda s: None, delays=dict.fromkeys(DEFAULT_DELAYS, 0.0),
                log=lambda msg: None)
    opts.update(kw)
    return FlashOneDriver(transport, image, slave_id, **opts)


class TestHexImageParsing(unittest.TestCase):
    """(c) the HEX checksum + config word are correctly parsed."""

    @classmethod
    def setUpClass(cls):
        cls.image = HexImage(HEX_PATH)

    def test_config_word(self):
        self.assertEqual(self.image.config_word,
                         bytes.fromhex("8cffffff9ffffffdffffffff"))

    def test_checksum(self):
        self.assertEqual(self.image.checksum, 0x7907)

    def test_row_count_and_packet_count(self):
        self.assertEqual(len(self.image.rows_list), 1920)
        self.assertEqual(self.image.row_packet_count, 480)

    def test_last_row_carries_checksum_word(self):
        addr, data = self.image.rows_list[-1]
        self.assertEqual(bytes(addr).hex(), "7ff0")
        self.assertEqual(bytes(data[-2:]), b"\x07\x79")


class TestCommandSequence(unittest.TestCase):
    """(a) exact command order (incl. 103) and (b) 122/131/132 never emitted."""

    @classmethod
    def setUpClass(cls):
        cls.image = HexImage(HEX_PATH)
        cls.n = cls.image.row_packet_count

    def _run(self, **kw):
        transport = MockTransport(self.n)
        make_driver(transport, self.image, slave_id=7, **kw).run()
        return transport

    def test_exact_command_order(self):
        transport = self._run()
        expected = [CONNECT, SLAVE_FW_FLASH, RECEIVE_FW_FILE] + \
                   [FW_DATA] * self.n + \
                   [RECEIVE_CONFIG_WORD, CFG_DATA] + \
                   [SELECT_SLAVE, ERASE, WRITE, WRITE_CONFIG, CHECK, RESET, EXIT] + \
                   [SLAVES_ON, START_DATA_UPLOAD, SLAVE_DETECT]
        self.assertEqual(commands_of(transport), expected)

    def test_enter_fw_handler_before_receive_fw_file(self):
        """Fix 1: 103 must appear after CONNECT and before RECEIVE_FW_FILE."""
        transport = self._run()
        emitted = commands_of(transport)
        self.assertEqual(emitted[0], CONNECT)
        self.assertEqual(emitted[1], SLAVE_FW_FLASH)
        self.assertEqual(emitted[2], RECEIVE_FW_FILE)
        self.assertLess(emitted.index(SLAVE_FW_FLASH),
                        emitted.index(RECEIVE_FW_FILE))

    def test_readback_command_order(self):
        """Fix 2: readback commands 100/106/101 follow EXIT (130), in order."""
        transport = self._run()
        emitted = commands_of(transport)
        tail = emitted[emitted.index(EXIT) + 1:]
        self.assertEqual(tail, [SLAVES_ON, START_DATA_UPLOAD, SLAVE_DETECT])

    def test_manual_primitive_order(self):
        """Primitives ignoring the 141/151 data payload commands."""
        transport = self._run()
        primitives = [c for c in commands_of(transport)
                      if c not in (FW_DATA, CFG_DATA)]
        self.assertEqual(primitives,
                         [CONNECT, SLAVE_FW_FLASH, RECEIVE_FW_FILE,
                          RECEIVE_CONFIG_WORD, SELECT_SLAVE, ERASE, WRITE,
                          WRITE_CONFIG, CHECK, RESET, EXIT,
                          SLAVES_ON, START_DATA_UPLOAD, SLAVE_DETECT])

    def test_forbidden_commands_never_emitted(self):
        transport = self._run()
        emitted = commands_of(transport)
        for bad in FORBIDDEN:
            self.assertNotIn(bad, emitted,
                             "forbidden command %d was emitted" % bad)

    def test_select_slave_carries_slave_id(self):
        transport = self._run()
        select_pkts = [p for p in transport.sent if p[1] == SELECT_SLAVE]
        self.assertEqual(len(select_pkts), 1)
        # data[0] must hold the slave id (slavefwhandler.c: fwData.data = data[0])
        self.assertEqual(select_pkts[0], bytes([0xAA, SELECT_SLAVE, 7, 0x55]))

    def test_fw_data_packet_size(self):
        transport = self._run()
        for pkt in transport.sent:
            if pkt[1] == FW_DATA:
                self.assertEqual(len(pkt), 2 + 64, "FW data packet must be 66 bytes")

    def test_config_data_packet_size(self):
        transport = self._run()
        cfg_pkts = [p for p in transport.sent if p[1] == CFG_DATA]
        self.assertEqual(len(cfg_pkts), 1)
        self.assertEqual(len(cfg_pkts[0]), 2 + 12, "config packet must be 14 bytes")


class TestSlaveinfoParsing(unittest.TestCase):
    """parse_slaveinfo_frame: SLAVEINFO wire format (0xAA / 0xFF)."""

    def _frame(self, slave_number=3, detected=1, hr3=0x1122, hr9=0x3344,
               hr10=0x5566, hr11=0x7907):
        hr = [0] * 12
        hr[3] = hr3
        hr[9] = hr9
        hr[10] = hr10
        hr[11] = hr11
        return struct.pack("<6B12H2HLBBB",
                           0xAA, 0xFF, 0xAA, slave_number, detected, 0,  # hdr/cmd/Header/num/det/pad
                           *hr,
                           0x0102, 0x0304,   # MbReceiveCounter, MbSentCounter
                           0x05060708,       # MbCommError (uint32)
                           0x09, 0x0A, 0x55) # MbExceptionCode, SpiErr, Footer

    def test_parses_registers(self):
        rec = parse_slaveinfo_frame(self._frame())
        self.assertIsNotNone(rec)
        self.assertEqual(rec["slave_number"], 3)
        self.assertTrue(rec["detected"])
        self.assertEqual(rec["hr3"], 0x1122)
        self.assertEqual(rec["hr9"], 0x3344)
        self.assertEqual(rec["hr10"], 0x5566)
        self.assertEqual(rec["hr11"], 0x7907)

    def test_rejects_bad_header(self):
        frame = bytearray(self._frame())
        frame[0] = 0x55
        self.assertIsNone(parse_slaveinfo_frame(bytes(frame)))

    def test_rejects_short_frame(self):
        self.assertIsNone(parse_slaveinfo_frame(self._frame()[:30]))


class TestStopConditions(unittest.TestCase):
    """(d) documented stop condition on missing 120/121 acks."""

    @classmethod
    def setUpClass(cls):
        cls.image = HexImage(HEX_PATH)
        cls.n = cls.image.row_packet_count

    def test_missing_fw_file_ack_stops(self):
        transport = MockTransport(self.n, ack_120=False)
        driver = make_driver(transport, self.image, slave_id=7, ack_timeout=0.2)
        with self.assertRaises(FlashAbort):
            driver.run()
        self.assertEqual(commands_of(transport),
                         [CONNECT, SLAVE_FW_FLASH, RECEIVE_FW_FILE])
        self.assertNotIn(SELECT_SLAVE, commands_of(transport))

    def test_missing_config_ack_stops(self):
        transport = MockTransport(self.n, ack_121=False)
        driver = make_driver(transport, self.image, slave_id=7, ack_timeout=0.2)
        with self.assertRaises(FlashAbort):
            driver.run()
        self.assertEqual(commands_of(transport),
                         [CONNECT, SLAVE_FW_FLASH, RECEIVE_FW_FILE] +
                         [FW_DATA] * self.n + [RECEIVE_CONFIG_WORD])
        self.assertNotIn(SELECT_SLAVE, commands_of(transport))


class TestErrorAbort(unittest.TestCase):
    """Faithful mock: RECEIVED_WRONG_COMMAND/ERROR on 120 -> clean abort."""

    @classmethod
    def setUpClass(cls):
        cls.image = HexImage(HEX_PATH)
        cls.n = cls.image.row_packet_count

    def test_wrong_command_error_aborts_without_hang(self):
        transport = MockTransport(self.n, fail_fw_download=True)
        # Generous ack_timeout: proves the abort is from the ERROR detection,
        # not the ack-timeout path (which would also abort, but slowly).
        driver = make_driver(transport, self.image, slave_id=7, ack_timeout=30.0)
        with self.assertRaises(FlashAbort) as ctx:
            driver.run()
        msg = str(ctx.exception)
        self.assertIn("ERROR", msg)
        self.assertIn("RECEIVED_WRONG_COMMAND", msg)
        # Aborted during the 120 phase: no FW data or select ever emitted.
        self.assertEqual(commands_of(transport),
                         [CONNECT, SLAVE_FW_FLASH, RECEIVE_FW_FILE])
        self.assertNotIn(FW_DATA, commands_of(transport))
        self.assertNotIn(SELECT_SLAVE, commands_of(transport))


class TestBootloaderAcks(unittest.TestCase):
    """Defect 1: 125-129 wait for *_RETURNED_OK/DONE; 123/130 stay no-ack."""

    @classmethod
    def setUpClass(cls):
        cls.image = HexImage(HEX_PATH)
        cls.n = cls.image.row_packet_count

    def _run(self, **kw):
        transport = MockTransport(self.n, **kw)
        make_driver(transport, self.image, slave_id=7).run()
        return transport

    def test_manual_primitives_wait_for_bootloader_acks(self):
        # Mock acks 125-129 with *_RETURNED_OK/DONE; driver must consume them
        # and reach the readback phase (no FlashAbort).
        transport = self._run()
        emitted = commands_of(transport)
        self.assertIn(SELECT_SLAVE, emitted)
        self.assertIn(EXIT, emitted)
        # The full sequence still holds (acks are consumed, not emitted).
        expected = [CONNECT, SLAVE_FW_FLASH, RECEIVE_FW_FILE] + \
                   [FW_DATA] * self.n + \
                   [RECEIVE_CONFIG_WORD, CFG_DATA] + \
                   [SELECT_SLAVE, ERASE, WRITE, WRITE_CONFIG, CHECK, RESET, EXIT] + \
                   [SLAVES_ON, START_DATA_UPLOAD, SLAVE_DETECT]
        self.assertEqual(emitted, expected)

    def test_bootloader_error_aborts(self):
        # 125 (ERASE) acks with ERASE_FLASH_RETURNED_NOK/ERROR -> abort, not hang.
        transport = MockTransport(self.n, fail_bootloader=True)
        driver = make_driver(transport, self.image, slave_id=7,
                             bootloader_ack_timeout=30.0)
        with self.assertRaises(FlashAbort) as ctx:
            driver.run()
        self.assertIn("ERROR", str(ctx.exception))
        # 126 (WRITE) must never be emitted after the 125 failure.
        emitted = commands_of(transport)
        self.assertIn(ERASE, emitted)
        self.assertNotIn(WRITE, emitted)


class TestStaleFrameDrain(unittest.TestCase):
    """Defect 2: a stale non-141 frame is drained before the 120 download."""

    @classmethod
    def setUpClass(cls):
        cls.image = HexImage(HEX_PATH)
        cls.n = cls.image.row_packet_count

    def test_stale_frame_drained_before_120(self):
        transport = MockTransport(self.n)
        # Simulate a leftover frame from a prior session in the host RX socket.
        transport._queue.append((FWFILEDOWNLOAD, STANDBY_FW, DONE, NONE))
        logs = []
        make_driver(transport, self.image, slave_id=7, log=logs.append).run()
        drain_idx = next((i for i, m in enumerate(logs)
                          if m.startswith("Drain(") and "discarded" in m), None)
        tx120_idx = next((i for i, m in enumerate(logs)
                          if "cmd=0x78" in m), None)  # 0x78 == 120
        self.assertIsNotNone(drain_idx, "stale frame was not drained")
        self.assertIsNotNone(tx120_idx, "120 was never emitted")
        self.assertLess(drain_idx, tx120_idx,
                        "stale frame must be drained before 120")
        # Driver still completes with the correct sequence.
        self.assertEqual(commands_of(transport)[0], CONNECT)


if __name__ == "__main__":
    unittest.main(verbosity=2)
