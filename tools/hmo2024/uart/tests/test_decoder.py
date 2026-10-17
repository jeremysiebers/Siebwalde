"""Tests for the offline 9-bit UART decoder (``uart.decoder``).

Fixtures cover: 9-bit frames with address marker, 8-bit frames without a 9th
bit, malformed CRC, incomplete (truncated) frames, bit-jitter (non-integer bit
period), the self-calibrating bit-period measurement, and the real reference
capture ``REF-AMP01-003-DIGITAL``.
"""

from __future__ import annotations

import os
import sys
import tempfile
import unittest

_PARENT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
if _PARENT not in sys.path:
    sys.path.insert(0, _PARENT)

from uart import decoder  # noqa: E402

# Path to the reference capture, used for a real-data test.
_HMO2024 = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
_CAPTURE_DIR = os.path.join(_HMO2024, "captures", "REF-AMP01-003-DIGITAL")

DT_S = 100e-9  # 100 ns/sample


def synth_stream(frames, data_bits=9, t_samples=5.0, idle_len=30):
    """Synthesize a UART sample stream (list of 0/1).

    ``frames`` is a list of ``(value, ninth)`` tuples; ``ninth`` is the D8
    address marker for 9-bit frames and ignored (pass None) for 8-bit frames.
    """
    bitseq = []
    for value, ninth in frames:
        bitseq.append(0)  # start bit
        for k in range(8):
            bitseq.append((value >> k) & 1)
        if data_bits >= 9:
            bitseq.append(0 if ninth is None else ninth)
        bitseq.append(1)  # stop bit
    stream = [1] * idle_len
    pos = 0.0
    for b in bitseq:
        s0 = int(round(pos))
        pos += t_samples
        s1 = int(round(pos))
        stream.extend([b] * max(0, s1 - s0))
    stream.extend([1] * idle_len)
    return stream


def _decode(frames, data_bits=9, t_samples=5.0, direction=decoder.DIRECTION_REQUEST):
    stream = synth_stream(frames, data_bits=data_bits, t_samples=t_samples)
    fr = decoder.decode_stream(stream, t_samples, DT_S, direction, 0.0, data_bits)
    tgs = decoder.group_telegrams(fr, direction, data_bits, t_samples, len(stream))
    return stream, fr, tgs


REQUEST_FRAMES = [
    (0x04, 1), (0x03, 0), (0x00, 0), (0x08, 0),
    (0x00, 0), (0x02, 0), (0x39, 0), (0xA2, 0),
]
RESPONSE_FRAMES = [
    (0x04, None), (0x03, None), (0x04, None), (0xD3, None), (0x98, None),
    (0x00, None), (0x00, None), (0x1A, None), (0x68, None),
]


class TestBitPeriod(unittest.TestCase):
    def test_measures_integer_period(self):
        stream = synth_stream(REQUEST_FRAMES, data_bits=9, t_samples=5.0)
        period = decoder.measure_bit_period(stream)
        self.assertIsNotNone(period["fitted_period_samples"])
        self.assertAlmostEqual(period["fitted_period_samples"], 5.0, delta=0.2)

    def test_measures_fractional_period_without_subharmonic(self):
        # ~5.2 samples/bit must not alias to ~2.6 (sub-harmonic) or ~3.0.
        stream = synth_stream(REQUEST_FRAMES, data_bits=9, t_samples=5.2)
        period = decoder.measure_bit_period(stream)
        t = period["fitted_period_samples"]
        self.assertIsNotNone(t)
        self.assertGreater(t, 4.0)
        self.assertLess(t, 6.0)

    def test_no_edges(self):
        period = decoder.measure_bit_period([1] * 100)
        self.assertIsNone(period["fitted_period_samples"])


class TestDecodeNineBit(unittest.TestCase):
    def test_request_decodes_and_validates(self):
        stream, fr, tgs = _decode(REQUEST_FRAMES, data_bits=9, t_samples=5.0)
        self.assertEqual(len(fr), 8)
        self.assertEqual([f.data for f in fr], [0x04, 0x03, 0x00, 0x08, 0x00, 0x02, 0x39, 0xA2])
        self.assertEqual([f.ninth for f in fr], [1, 0, 0, 0, 0, 0, 0, 0])
        self.assertEqual(len(tgs), 1)
        t = tgs[0]
        self.assertEqual(t.crc_status, "VALID")
        self.assertEqual(t.address, 0x04)
        self.assertEqual(t.function, 0x03)
        self.assertEqual(t.register_address, 0x0008)
        self.assertEqual(t.quantity, 0x0002)
        self.assertTrue(t.crc_siebwalde_ok)
        self.assertFalse(t.crc_standard_ok)


class TestDecodeEightBit(unittest.TestCase):
    def test_response_decodes_and_validates(self):
        stream, fr, tgs = _decode(RESPONSE_FRAMES, data_bits=8, t_samples=5.0,
                                  direction=decoder.DIRECTION_RESPONSE)
        self.assertEqual(len(fr), 9)
        self.assertEqual([f.data for f in fr],
                         [0x04, 0x03, 0x04, 0xD3, 0x98, 0x00, 0x00, 0x1A, 0x68])
        self.assertTrue(all(f.ninth is None for f in fr))
        self.assertEqual(len(tgs), 1)
        t = tgs[0]
        self.assertEqual(t.crc_status, "VALID")
        self.assertEqual(t.address, 0x04)
        self.assertEqual(t.function, 0x03)
        self.assertEqual(t.byte_count, 0x04)
        self.assertEqual(t.values, [0xD398, 0x0000])
        self.assertTrue(t.crc_siebwalde_ok)


class TestErrorHandling(unittest.TestCase):
    def test_crc_mismatch(self):
        bad = list(REQUEST_FRAMES)
        bad[-1] = (0xA3, 0)  # corrupt high CRC byte
        _, _, tgs = _decode(bad, data_bits=9, t_samples=5.0)
        self.assertEqual(tgs[0].crc_status, "CRC-MISMATCH")
        self.assertFalse(tgs[0].crc_siebwalde_ok)

    def test_incomplete_truncated_stream(self):
        # Drop the last two bytes (CRC) by truncating the sample stream.
        full = synth_stream(REQUEST_FRAMES, data_bits=9, t_samples=5.0)
        truncated = full[: len(full) - int(round(11 * 5.0)) - 30]
        fr = decoder.decode_stream(truncated, 5.0, DT_S, decoder.DIRECTION_REQUEST, 0.0, 9)
        tgs = decoder.group_telegrams(fr, decoder.DIRECTION_REQUEST, 9, 5.0, len(truncated))
        self.assertEqual(tgs[0].crc_status, "INCOMPLETE")

    def test_malformed_unknown_function(self):
        frames = [(0x04, 1), (0x7F, 0), (0x00, 0), (0x00, 0)]
        _, _, tgs = _decode(frames, data_bits=9, t_samples=5.0)
        self.assertEqual(tgs[0].crc_status, "MALFORMED")

    def test_orphan_data_frame_starts_telegram(self):
        # A data frame with no preceding address marker still yields a telegram
        # flagged as not starting with an address.
        stream = synth_stream([(0x03, 0), (0x00, 0)], data_bits=9, t_samples=5.0)
        fr = decoder.decode_stream(stream, 5.0, DT_S, decoder.DIRECTION_REQUEST, 0.0, 9)
        tgs = decoder.group_telegrams(fr, decoder.DIRECTION_REQUEST, 9, 5.0, len(stream))
        self.assertEqual(len(tgs), 1)
        self.assertFalse(tgs[0].starts_with_address)


class TestBitJitter(unittest.TestCase):
    def test_fractional_period_self_calibration(self):
        # A non-integer bit period produces 4/5/6-sample runs (edge jitter).
        # Measure then decode; the result must still validate.
        stream = synth_stream(REQUEST_FRAMES, data_bits=9, t_samples=5.2)
        period = decoder.measure_bit_period(stream)
        t = period["fitted_period_samples"]
        fr = decoder.decode_stream(stream, t, DT_S, decoder.DIRECTION_REQUEST, 0.0, 9)
        tgs = decoder.group_telegrams(fr, decoder.DIRECTION_REQUEST, 9, t, len(stream))
        self.assertEqual(len(fr), 8)
        self.assertEqual(tgs[0].crc_status, "VALID")
        self.assertEqual([f.data for f in fr],
                         [0x04, 0x03, 0x00, 0x08, 0x00, 0x02, 0x39, 0xA2])

    def test_fractional_period_eight_bit(self):
        stream = synth_stream(RESPONSE_FRAMES, data_bits=8, t_samples=4.8)
        period = decoder.measure_bit_period(stream)
        t = period["fitted_period_samples"]
        fr = decoder.decode_stream(stream, t, DT_S, decoder.DIRECTION_RESPONSE, 0.0, 8)
        tgs = decoder.group_telegrams(fr, decoder.DIRECTION_RESPONSE, 8, t, len(stream))
        self.assertEqual(tgs[0].crc_status, "VALID")


class TestEndToEndPod(unittest.TestCase):
    def _write_pod(self, d6_frames, d7_frames):
        # Build a POD file where D6 and D7 streams are placed on the same
        # sample grid; other channels are idle (0).
        n6 = len(d6_frames) * (9 + 2) if d6_frames else 0
        # Use a fixed sample count that covers both streams.
        s6 = synth_stream(d6_frames, data_bits=9, t_samples=5.0)
        s7 = synth_stream(d7_frames, data_bits=8, t_samples=5.0)
        n = max(len(s6), len(s7))
        vals = []
        for i in range(n):
            v = 0
            if i < len(s6) and s6[i]:
                v |= (1 << 6)
            if i < len(s7) and s7[i]:
                v |= (1 << 7)
            vals.append(str(v))
        return ",".join(vals)

    def test_full_pipeline(self):
        pod_text = self._write_pod(REQUEST_FRAMES, RESPONSE_FRAMES)
        with tempfile.TemporaryDirectory() as d:
            pod_path = os.path.join(d, "pod1.raw")
            header_path = os.path.join(d, "ch1.header.raw")
            with open(pod_path, "w") as f:
                f.write(pod_text)
            # 6000 samples is not needed; write a header matching the count.
            n = len([p for p in pod_text.split(",") if p])
            with open(header_path, "w") as f:
                f.write(f"0,{n * 100e-9:.7e},{n},1")
            result = decoder.decode_pod(pod_path, header_path)
        req = result["directions"][decoder.DIRECTION_REQUEST]["telegrams"][0]
        resp = result["directions"][decoder.DIRECTION_RESPONSE]["telegrams"][0]
        self.assertEqual(req.crc_status, "VALID")
        self.assertEqual(resp.crc_status, "VALID")
        self.assertEqual(req.function, 0x03)
        self.assertEqual(resp.function, 0x03)
        self.assertEqual(resp.values, [0xD398, 0x0000])


@unittest.skipUnless(os.path.isfile(os.path.join(_CAPTURE_DIR, "pod1.raw")),
                     "reference capture not present")
class TestRealCapture(unittest.TestCase):
    def test_reference_capture_decodes(self):
        pod = os.path.join(_CAPTURE_DIR, "pod1.raw")
        header = os.path.join(_CAPTURE_DIR, "ch1.header.raw")
        ch1 = os.path.join(_CAPTURE_DIR, "ch1.raw")
        result = decoder.decode_pod(pod, header, ch1_path=ch1)

        req = result["directions"][decoder.DIRECTION_REQUEST]
        resp = result["directions"][decoder.DIRECTION_RESPONSE]

        # Request: 9-bit, 8 frames, one CRC-valid FC03 read.
        self.assertEqual(req["frame_format"], "9-bit (address marker)")
        self.assertEqual(req["frame_count"], 8)
        self.assertEqual(len(req["telegrams"]), 1)
        rt = req["telegrams"][0]
        self.assertEqual(rt.crc_status, "VALID")
        self.assertEqual(rt.address, 0x04)
        self.assertEqual(rt.function, 0x03)
        self.assertEqual(rt.register_address, 0x0008)
        self.assertEqual(rt.quantity, 0x0002)
        self.assertTrue(rt.crc_siebwalde_ok)

        # Response: 8-bit, 9 frames, one CRC-valid FC03 response.
        self.assertEqual(resp["frame_format"], "8-bit")
        self.assertEqual(resp["frame_count"], 9)
        self.assertEqual(len(resp["telegrams"]), 1)
        st = resp["telegrams"][0]
        self.assertEqual(st.crc_status, "VALID")
        self.assertEqual(st.address, 0x04)
        self.assertEqual(st.function, 0x03)
        self.assertEqual(st.byte_count, 0x04)
        self.assertEqual(st.values, [0xD398, 0x0000])
        self.assertTrue(st.crc_siebwalde_ok)


if __name__ == "__main__":
    unittest.main()
