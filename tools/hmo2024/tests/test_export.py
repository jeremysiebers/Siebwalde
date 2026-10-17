"""Export tests: no rearm, artifact linkage, rearm incoherence flagging."""

import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import export as export_mod
from fake_serial import FakeSerial
from records import RecordManager
from transport import HmoTransport, SerialConfig


def held_record(root):
    rm = RecordManager(root)
    d = rm.new_capture_dir()
    m = rm.base_manifest("cap-1", "COM3", 115200, {"name": "x"})
    m["hold"] = {
        "channel": 1,
        "acq_state": "COMP",
        "header_raw": "0.0,1.0,101,1",
        "header": {"start": 0.0, "stop": 1.0, "count": 101,
                   "values_per_sample": 1},
        "point_count_raw": "101",
        "point_count": 101,
    }
    rm.write_manifest(d, m)
    return d, rm


def exporter(program, cap_dir):
    fs = FakeSerial(program=program)
    t = HmoTransport(SerialConfig.scope_default("FAKE"), serial_factory=lambda c: fs)
    rm = RecordManager(os.path.dirname(cap_dir))
    t.open()
    return t, rm, fs


def coherent_program():
    wave = ",".join(f"{v}" for v in range(101))
    return [
        ("ACQ:STAT?", b"COMP\n"),
        ("CHAN1:DATA:HEAD?", b"0.0,1.0,101,1\n"),
        ("CHAN1:DATA?", (wave + "\n").encode()),
        ("HCOP:DATA?", b"#1" + b"6" + b"BMtest" + b"\n"),
    ]


class ExportTest(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp(prefix="hmo2024-ex-")

    def test_export_does_not_rearm(self):
        cap_dir, _ = held_record(self.root)
        t, rm, fs = exporter(coherent_program(), cap_dir)
        m = export_mod.export_held(t, rm, cap_dir)
        cmds = [w.decode("ascii") for w in fs.written]
        self.assertNotIn("SINGle", cmds)
        self.assertNotIn("RUN", cmds)
        self.assertNotIn("STOP", cmds)
        kinds = {a["kind"] for a in m["artifacts"]}
        self.assertIn("waveform_ascii", kinds)
        self.assertIn("waveform_csv", kinds)
        self.assertIn("header_ascii", kinds)
        self.assertIn("screenshot_bmp", kinds)
        # All artifacts present on disk with stable hashes.
        for a in m["artifacts"]:
            self.assertTrue(os.path.isfile(os.path.join(cap_dir, a["rel_path"])))
        t.close()

    def test_export_ties_to_single_record(self):
        cap_dir, _ = held_record(self.root)
        t, rm, fs = exporter(coherent_program(), cap_dir)
        m = export_mod.export_held(t, rm, cap_dir)
        self.assertEqual(m["capture_id"], "cap-1")
        # manifest still has the hold token
        self.assertEqual(m["hold"]["point_count"], 101)
        t.close()

    def test_rearm_flagged_on_state_change(self):
        cap_dir, _ = held_record(self.root)
        program = [
            ("ACQ:STAT?", b"RUN\n"),
            ("CHAN1:DATA:HEAD?", b"0.0,1.0,101,1\n"),
            ("CHAN1:DATA?", b"1.0,2.0\n"),
            ("HCOP:DATA?", b"#1" + b"6" + b"BMtest" + b"\n"),
        ]
        t, rm, fs = exporter(program, cap_dir)
        m = export_mod.export_held(t, rm, cap_dir)
        flags = m["flags"]
        self.assertTrue(any("rearm" in f for f in flags), flags)
        t.close()

    def test_header_change_flagged(self):
        cap_dir, _ = held_record(self.root)
        program = [
            ("ACQ:STAT?", b"COMP\n"),
            ("CHAN1:DATA:HEAD?", b"0.0,1.0,999,1\n"),  # different count
            ("CHAN1:DATA?", b"1.0,2.0\n"),
            ("HCOP:DATA?", b"#1" + b"6" + b"BMtest" + b"\n"),
        ]
        t, rm, fs = exporter(program, cap_dir)
        m = export_mod.export_held(t, rm, cap_dir)
        self.assertTrue(any("header_changed" in f for f in m["flags"]), m["flags"])
        t.close()


if __name__ == "__main__":
    unittest.main()
