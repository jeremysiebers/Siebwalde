"""Record-manager tests: unique dirs, atomic writes, hashes, timestamps."""

import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from records import (
    MANIFEST_NAME,
    RecordManager,
    capture_id,
    sha256_of_bytes,
    sha256_of_file,
    timestamp_fields,
)


class RecordsTest(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp(prefix="hmo2024-rec-")
        self.rm = RecordManager(self.root)

    def test_capture_id_unique(self):
        self.assertNotEqual(capture_id(), capture_id())

    def test_new_capture_dir_never_overwrites(self):
        d1 = self.rm.new_capture_dir()
        d2 = self.rm.new_capture_dir()
        self.assertNotEqual(d1, d2)
        self.assertTrue(os.path.isdir(d1))
        with self.assertRaises(FileExistsError):
            self.rm.new_capture_dir(os.path.basename(d1))

    def test_timestamps_separate_utc_and_amsterdam(self):
        ts = timestamp_fields()
        self.assertIn("utc", ts)
        self.assertIn("amsterdam", ts)
        self.assertNotEqual(ts["utc"], ts["amsterdam"])

    def test_base_manifest_scope_time_not_queried(self):
        m = self.rm.base_manifest("cap-1", "COM3", 115200, {"name": "x"})
        self.assertEqual(m["scope_time"]["status"], "NOT_QUERIED")
        self.assertIsNone(m["scope_time"]["value"])

    def test_atomic_json_write_and_read(self):
        d = self.rm.new_capture_dir("cap-test")
        m = self.rm.base_manifest("cap-test", "COM3", 115200, {"name": "x"})
        m["artifacts"].append({"x": 1})
        path = self.rm.write_manifest(d, m)
        self.assertTrue(os.path.isfile(path))
        self.assertEqual(self.rm.load_manifest(d)["capture_id"], "cap-test")
        # No .tmp residue.
        self.assertFalse(os.path.exists(path + ".tmp"))

    def test_artifact_hash_stable(self):
        d = self.rm.new_capture_dir("cap-hash")
        data = b"hello world\n"
        e1 = self.rm.record_artifact(d, "ch1.raw", data, "waveform_ascii")
        e2 = self.rm.record_artifact(d, "ch1b.raw", data, "waveform_ascii")
        self.assertEqual(e1["sha256"], e2["sha256"])
        self.assertEqual(e1["sha256"], sha256_of_bytes(data))
        self.assertEqual(sha256_of_file(os.path.join(d, "ch1.raw")), e1["sha256"])

    def test_artifact_preserves_raw_bytes(self):
        d = self.rm.new_capture_dir("cap-raw")
        data = bytes(range(256))
        e = self.rm.record_artifact(d, "shot.bmp", data, "screenshot_bmp")
        with open(os.path.join(d, "shot.bmp"), "rb") as f:
            self.assertEqual(f.read(), data)
        self.assertEqual(e["bytes"], 256)


if __name__ == "__main__":
    unittest.main()
