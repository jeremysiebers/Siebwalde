"""Inventory tests: query-only discipline, summary, empty/unsupported handling."""

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import inventory as inv
import scpi
from fake_serial import FakeSerial
from transport import HmoTransport, SerialConfig


def full_program():
    """Provide a response for every inventory query (all end in '?')."""
    program = []
    for key, cmd in inv.build_query_list():
        # Generic numeric/text response; empty for a couple to test empty handling.
        if "probe" in key or "uart_source" in key:
            resp = b"10.0\n" if "probe" in key else b"CH1\n"
        elif "stat" in key:
            resp = b"ON\n"
        else:
            resp = b"1.0\n"
        program.append((cmd, resp))
    return program


class InventoryTest(unittest.TestCase):
    def test_all_commands_are_queries(self):
        for key, cmd in inv.build_query_list():
            self.assertTrue(cmd.endswith("?"), f"{key}: {cmd} is not a query")
            self.assertTrue(scpi.is_query_command(cmd))

    def test_inventory_never_writes_settings(self):
        fs = FakeSerial(program=full_program())
        t = HmoTransport(SerialConfig.scope_default("FAKE"),
                         serial_factory=lambda c: fs)
        t.open()
        results = inv.run_inventory(t, timeout=1.0)
        t.close()
        # Every written command must be a query ending in '?'.
        for w in fs.written:
            text = w.decode("ascii").rstrip("\n")
            self.assertTrue(text.endswith("?"), f"non-query write: {text!r}")
        self.assertEqual(len(results), len(inv.build_query_list()))

    def test_empty_and_error_entries_are_marked(self):
        # One query returns empty, another times out (no program entry).
        program = full_program()[:2]
        program[0] = (program[0][0], b"\n")  # empty reply
        fs = FakeSerial(program=program)
        t = HmoTransport(SerialConfig.scope_default("FAKE"),
                         serial_factory=lambda c: fs)
        t.open()
        results = inv.run_inventory(t, timeout=0.1)
        t.close()
        keys = [k for k, _ in inv.build_query_list()]
        self.assertFalse(results[keys[0]]["ok"])  # empty reply
        self.assertIsNotNone(results[keys[2]]["error"])  # timeout

    def test_summary_structure(self):
        fs = FakeSerial(program=full_program())
        t = HmoTransport(SerialConfig.scope_default("FAKE"),
                         serial_factory=lambda c: fs)
        t.open()
        results = inv.run_inventory(t, timeout=1.0)
        t.close()
        s = inv.inventory_summary(results)
        self.assertIn("identity", s)
        self.assertIn("channels", s)
        self.assertIn("1", s["channels"])
        self.assertIn("bus", s)
        self.assertIn("uart", s["bus"]["bus2"])


if __name__ == "__main__":
    unittest.main()
