"""Offline analyze/verify tests (no instrument)."""

import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import analyze as analyze_mod
import scpi
from records import RecordManager, sha256_of_bytes


def build_record(root, header_raw, wave_raw, bmp=b"BM-fake", flags=None):
    rm = RecordManager(root)
    d = rm.new_capture_dir()
    m = rm.base_manifest("cap-1", "COM3", 115200, {"name": "x"})
    artifacts = []
    if header_raw is not None:
        artifacts.append(rm.record_artifact(
            d, "ch1.header.raw", header_raw.encode(), "header_ascii"))
    if wave_raw is not None:
        artifacts.append(rm.record_artifact(
            d, "ch1.raw", wave_raw.encode(), "waveform_ascii"))
    if bmp is not None:
        artifacts.append(rm.record_artifact(
            d, "screenshot.bmp", bmp, "screenshot_bmp"))
    m["artifacts"] = artifacts
    m["flags"] = flags or []
    rm.write_manifest(d, m)
    return d


class AnalyzeTest(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp(prefix="hmo2024-an-")

    def test_verify_pass(self):
        header = "0.0,1.0,101,1"
        wave = ",".join(str(v) for v in range(101))
        d = build_record(self.root, header, wave)
        r = analyze_mod.verify_record(d)
        self.assertTrue(r["passed"], r["checks"])

    def test_header_sample_mismatch_fails(self):
        header = "0.0,1.0,101,1"
        wave = ",".join(str(v) for v in range(50))  # only 50 samples
        d = build_record(self.root, header, wave)
        r = analyze_mod.verify_record(d)
        self.assertFalse(r["passed"])
        names = [c["check"] for c in r["checks"]]
        self.assertIn("header_sample_count_match", names)
        match = [c for c in r["checks"] if c["check"] == "header_sample_count_match"][0]
        self.assertFalse(match["ok"])

    def test_zero_byte_artifact_fails(self):
        d = build_record(self.root, "0.0,1.0,2,1", "1.0,2.0", bmp=b"")
        # bmp empty -> nonempty check fails
        r = analyze_mod.verify_record(d)
        names = {c["check"] for c in r["checks"]}
        self.assertIn("nonempty:screenshot.bmp", names)

    def test_hash_mismatch_fails(self):
        d = build_record(self.root, "0.0,1.0,2,1", "1.0,2.0")
        # Corrupt the artifact on disk.
        with open(os.path.join(d, "ch1.raw"), "wb") as f:
            f.write(b"changed")
        r = analyze_mod.verify_record(d)
        self.assertFalse(r["passed"])

    def test_rearm_flag_detected(self):
        d = build_record(self.root, "0.0,1.0,2,1", "1.0,2.0",
                         flags=["scope_running_rearm_suspected"])
        r = analyze_mod.verify_record(d)
        names = [c for c in r["checks"] if c["check"] == "artifacts_coherent_with_hold"]
        self.assertFalse(names[0]["ok"])

    def test_bad_bmp_magic_fails(self):
        d = build_record(self.root, "0.0,1.0,2,1", "1.0,2.0", bmp=b"XX-fake")
        r = analyze_mod.verify_record(d)
        names = [c for c in r["checks"] if c["check"] == "screenshot_bmp_magic"]
        self.assertFalse(names[0]["ok"])

    def test_raw_to_time(self):
        h = scpi.parse_header("0.0,1.0,5,1")
        samples = [0.0, 0.25, 0.5, 0.75, 1.0]
        pts = analyze_mod.raw_to_time(h, samples)
        self.assertEqual(len(pts), 5)
        self.assertAlmostEqual(pts[0][0], 0.0)
        self.assertAlmostEqual(pts[-1][0], 1.0)
        self.assertAlmostEqual(pts[1][0], 0.25)

    def test_measure_uncertainty(self):
        samples = [0.0, 1.0, 2.0, 1.0, 0.0]
        m = analyze_mod.measure(samples, scale=1.0)
        self.assertEqual(m["count"], 5)
        self.assertAlmostEqual(m["peak_to_peak_v"], 2.0)
        # full_scale = 1.0 * 8 = 8 V; quantization = 8/256 = 0.03125
        self.assertAlmostEqual(m["quantization_step_v"], 0.03125)
        self.assertAlmostEqual(m["reported_uncertainty_v"], 0.015625)

    def test_measure_no_scale_uncertainty_unknown(self):
        m = analyze_mod.measure([1.0, 2.0], scale=None)
        self.assertFalse(m["uncertainty_known"])


if __name__ == "__main__":
    unittest.main()
