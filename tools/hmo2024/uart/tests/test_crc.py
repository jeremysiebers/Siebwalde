"""Tests for the Modbus CRC-16 helpers (``uart.crc``).

The project uses a bit-reversed Modbus CRC-16/MODBUS on the wire, sent low byte
first.  These tests lock the standard CRC, the bit-reversal, and the on-wire
convention against the firmware comment ("0x6C, 0x93 ... -> 0xDD6C") and against
the values observed in the reference capture ``REF-AMP01-003-DIGITAL``.
"""

from __future__ import annotations

import os
import sys
import unittest

_PARENT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if _PARENT not in sys.path:
    sys.path.insert(0, _PARENT)

from uart import crc  # noqa: E402


class TestCrc16Modbus(unittest.TestCase):
    def test_known_modbus_vector(self):
        # Classic Modbus RTU example: 01 03 00 00 00 0A -> CRC C5 CD (low, high).
        self.assertEqual(crc.crc16_modbus([0x01, 0x03, 0x00, 0x00, 0x00, 0x0A]), 0xCDC5)

    def test_init_and_poly_zero_length(self):
        # Over no bytes the register stays 0xFFFF (no final XOR).
        self.assertEqual(crc.crc16_modbus([]), 0xFFFF)

    def test_accepts_bytes(self):
        self.assertEqual(crc.crc16_modbus(b"\x01\x03\x00\x00\x00\x0A"), 0xCDC5)

    def test_firmware_documented_self_check(self):
        # Firmware comment in PetitModbus.c: "0x6C, 0x93 ... -> 0xDD6C" is the
        # STANDARD Modbus CRC of [0x6C, 0x93] (what the hardware produces after
        # CRC_ReverseValue).  This anchors crc16_modbus independently.
        self.assertEqual(crc.crc16_modbus([0x6C, 0x93]), 0xDD6C)
        # The on-wire form is the bit-reversal, low byte first.
        self.assertEqual(crc.crc_siebwalde_bytes([0x6C, 0x93]), (0xBB, 0x36))


class TestBitReverse16(unittest.TestCase):
    def test_identity(self):
        self.assertEqual(crc.bit_reverse16(0x0000), 0x0000)
        self.assertEqual(crc.bit_reverse16(0xFFFF), 0xFFFF)

    def test_single_bits(self):
        self.assertEqual(crc.bit_reverse16(0x0001), 0x8000)
        self.assertEqual(crc.bit_reverse16(0x8000), 0x0001)

    def test_involution(self):
        for v in (0x1234, 0x5816, 0x681A, 0xA239, 0x9C45, 0xCDC5, 0xA3B3):
            self.assertEqual(crc.bit_reverse16(crc.bit_reverse16(v)), v)


class TestSiebwaldeConvention(unittest.TestCase):
    def test_request_from_capture(self):
        # REF-AMP01-003-DIGITAL request: 04 03 00 08 00 02 -> CRC 39 A2 (lo, hi).
        payload = [0x04, 0x03, 0x00, 0x08, 0x00, 0x02]
        self.assertEqual(crc.crc_siebwalde_bytes(payload), (0x39, 0xA2))
        self.assertTrue(crc.verify_crc_siebwalde(payload, 0x39, 0xA2))
        # Standard convention would have been 45 9C (lo, hi) -> must NOT match.
        self.assertFalse(crc.verify_crc_standard(payload, 0x39, 0xA2))

    def test_response_from_capture(self):
        # REF-AMP01-003-DIGITAL response: 04 03 04 D3 98 00 00 -> CRC 1A 68.
        payload = [0x04, 0x03, 0x04, 0xD3, 0x98, 0x00, 0x00]
        self.assertEqual(crc.crc_siebwalde_bytes(payload), (0x1A, 0x68))
        self.assertTrue(crc.verify_crc_siebwalde(payload, 0x1A, 0x68))

    def test_mismatch_detected(self):
        payload = [0x04, 0x03, 0x00, 0x08, 0x00, 0x02]
        self.assertFalse(crc.verify_crc_siebwalde(payload, 0x39, 0xA3))
        self.assertFalse(crc.verify_crc_siebwalde(payload, 0x38, 0xA2))


if __name__ == "__main__":
    unittest.main()
