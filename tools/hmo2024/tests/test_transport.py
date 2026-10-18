"""Transport-level tests: block parsing, partial reads, timeouts, close()."""

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from fake_serial import FakeSerial
from transport import (
    BlockParseError,
    HmoTransport,
    SerialConfig,
    TransportError,
    TransportNotOpen,
    TransportTimeout,
)


def make_transport(program, chunk_size=None):
    cfg = SerialConfig.scope_default("FAKE")
    fs = FakeSerial(program=program, chunk_size=chunk_size)
    t = HmoTransport(cfg, serial_factory=lambda c: fs, max_block=1_000_000)
    t.open()
    return t, fs


class BlockParsingTest(unittest.TestCase):
    def test_block_roundtrip(self):
        payload = bytes(range(256))  # binary-safe
        resp = b"#" + b"3" + b"256" + payload + b"\n"
        t, fs = make_transport([("HCOP:DATA?", resp)])
        got = t.query_block("HCOP:DATA?", timeout=1.0)
        self.assertEqual(got, payload)

    def test_split_prefix_and_payload_across_chunks(self):
        payload = b"ABCDEFGH"
        resp = b"#18" + payload + b"\n"
        # chunk_size=1 -> every byte is a separate read
        t, fs = make_transport([("HCOP:DATA?", resp)], chunk_size=1)
        got = t.query_block("HCOP:DATA?", timeout=1.0)
        self.assertEqual(got, payload)

    def test_partial_read_chunk3(self):
        payload = b"0123456789"
        resp = b"#2" + b"10" + payload + b"\n"
        t, fs = make_transport([("HCOP:DATA?", resp)], chunk_size=3)
        got = t.query_block("HCOP:DATA?", timeout=1.0)
        self.assertEqual(got, payload)

    def test_malformed_missing_digit_count(self):
        resp = b"#X123abc\n"
        t, _ = make_transport([("HCOP:DATA?", resp)])
        with self.assertRaises(BlockParseError):
            t.query_block("HCOP:DATA?", timeout=1.0)

    def test_malformed_non_digit_length(self):
        resp = b"#1" + b"x" + b"abc\n"
        t, _ = make_transport([("HCOP:DATA?", resp)])
        with self.assertRaises(BlockParseError):
            t.query_block("HCOP:DATA?", timeout=1.0)

    def test_impossible_length_field_width_zero(self):
        resp = b"#0" + b"12" + b"abcdefghijkl\n"
        t, _ = make_transport([("HCOP:DATA?", resp)])
        with self.assertRaises(BlockParseError):
            t.query_block("HCOP:DATA?", timeout=1.0)

    def test_impossible_length_over_max(self):
        resp = b"#2" + b"99" + b"x" * 99 + b"\n"
        t, _ = make_transport([("HCOP:DATA?", resp)])
        # max_block is 1_000_000, 99 is fine -> should succeed; test the ceiling:
        t2, _ = make_transport([("HCOP:DATA?", b"#3" + b"100" + b"y" * 100 + b"\n")])
        t2.max_block = 50
        with self.assertRaises(BlockParseError):
            t2.query_block("HCOP:DATA?", timeout=1.0)

    def test_zero_length_block(self):
        resp = b"#1" + b"0" + b"\n"
        t, _ = make_transport([("HCOP:DATA?", resp)])
        got = t.query_block("HCOP:DATA?", timeout=1.0)
        self.assertEqual(got, b"")

    def test_valid_trailing_terminator(self):
        t, _ = make_transport([("HCOP:DATA?", b"#13abc\n")])
        self.assertEqual(t.query_block("HCOP:DATA?", timeout=1.0), b"abc")

    def test_missing_trailing_terminator(self):
        t, _ = make_transport([("HCOP:DATA?", b"#13abc")])
        self.assertEqual(t.query_block("HCOP:DATA?", timeout=1.0), b"abc")

    def test_invalid_trailing_terminator(self):
        t, _ = make_transport([("HCOP:DATA?", b"#13abcX")])
        with self.assertRaises(BlockParseError):
            t.query_block("HCOP:DATA?", timeout=1.0)


class AsciiQueryTest(unittest.TestCase):
    def test_query_strips_terminator(self):
        t, _ = make_transport([("*IDN?", b"HAMEG,HMO2024,1,1.0\n")])
        self.assertEqual(t.query("*IDN?", timeout=1.0), "HAMEG,HMO2024,1,1.0")

    def test_empty_reply(self):
        t, _ = make_transport([("*OPC?", b"\n")])
        self.assertEqual(t.query("*OPC?", timeout=1.0), "")

    def test_no_terminator_times_out(self):
        t, _ = make_transport([("*IDN?", b"12345")])
        with self.assertRaises(TransportTimeout):
            t.query("*IDN?", timeout=0.1)

    def test_stale_bytes_flushed_before_transaction(self):
        # Preload garbage then a valid reply; flush must discard the garbage.
        t, fs = make_transport([("A?", b"GARBAGE\n"), ("B?", b"42\n")])
        # First query consumes GARBAGE; then leftover should not leak.
        self.assertEqual(t.query("A?", timeout=1.0), "GARBAGE")
        self.assertEqual(t.query("B?", timeout=1.0), "42")

    def test_ascii_query_rejects_block(self):
        t, _ = make_transport([("*IDN?", b"#13abc\n")])
        with self.assertRaises(BlockParseError):
            t.query("*IDN?", timeout=1.0)

    def test_block_query_rejects_ascii(self):
        t, _ = make_transport([("HCOP:DATA?", b"notablock\n")])
        with self.assertRaises(BlockParseError):
            t.query_block("HCOP:DATA?", timeout=1.0)


class LifecycleTest(unittest.TestCase):
    def test_context_manager_closes_on_exception(self):
        cfg = SerialConfig.scope_default("FAKE")
        fs = FakeSerial(program=[("*IDN?", b"X")])  # no terminator -> timeout
        t = HmoTransport(cfg, serial_factory=lambda c: fs)
        with self.assertRaises(TransportTimeout):
            with t:
                t.query("*IDN?", timeout=0.05)
        self.assertTrue(fs.closed)

    def test_close_idempotent(self):
        t, fs = make_transport([])
        t.close()
        t.close()
        self.assertTrue(fs.closed)

    def test_query_before_open_raises(self):
        cfg = SerialConfig.scope_default("FAKE")
        t = HmoTransport(cfg, serial_factory=lambda c: FakeSerial())
        with self.assertRaises(TransportNotOpen):
            t.query("*IDN?")

    def test_write_log_audit(self):
        t, fs = make_transport([("*IDN?", b"X\n")])
        t.query("*IDN?")
        self.assertEqual(t.write_log, ["*IDN?"])
        t.write("SINGle")
        self.assertEqual(t.write_log[-1], "SINGle")


if __name__ == "__main__":
    unittest.main()
