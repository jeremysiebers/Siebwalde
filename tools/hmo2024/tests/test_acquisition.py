"""Acquisition state-machine tests (fake serial, no scope)."""

import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from fake_serial import FakeSerial
from transport import HmoTransport, SerialConfig
from records import RecordManager
from acquisition import (
    CaptureInterrupted,
    CaptureNotConfirmed,
    CaptureSession,
    CaptureTimeout,
    NoTrigger,
    ScopeNotIdle,
)


def session(program, timeout=0.4, poll=0.02, confirm=True, channel=1):
    fs = FakeSerial(program=program)
    t = HmoTransport(SerialConfig.scope_default("FAKE"), serial_factory=lambda c: fs)
    rec = RecordManager(tempfile.mkdtemp(prefix="hmo2024-test-"))
    s = CaptureSession(
        transport=t, records=rec, port="FAKE", baud=115200,
        tool={"name": "hmo2024", "version": "test", "git_commit": None},
        confirm=confirm, timeout=timeout, poll_interval=poll, channel=channel,
    )
    t.open()
    return s, t, fs


def idle_program():
    return [
        ("ACQ:STAT?", b"STOP\n"),
        ("TRIG:A:MODE?", b"AUTO\n"),
        ("TRIG:A:SOUR?", b"CH1\n"),
        ("*IDN?", b"HAMEG,HMO2024,1,1.0\n"),
        ("SINGle", b""),
        ("ACQ:STAT?", b"COMP\n"),
        ("ACQ:STAT?", b"COMP\n"),
        ("CHAN1:DATA:HEAD?", b"0.0,1.0,101,1\n"),
        ("CHAN1:DATA:POIN?", b"101\n"),
    ]


class CaptureStateTest(unittest.TestCase):
    def test_requires_confirmation(self):
        s, t, fs = session(idle_program(), confirm=False)
        with self.assertRaises(CaptureNotConfirmed):
            s.arm_and_wait()
        t.close()

    def test_refuses_when_running(self):
        program = [("ACQ:STAT?", b"RUN\n")]
        s, t, fs = session(program)
        with self.assertRaises(ScopeNotIdle):
            s.arm_and_wait()
        t.close()

    def test_completes_and_holds(self):
        s, t, fs = session(idle_program())
        manifest = s.arm_and_wait()
        self.assertEqual(manifest["acquisition"]["result"], "COMPLETE")
        self.assertEqual(manifest["hold"]["point_count"], 101)
        self.assertEqual(manifest["hold"]["header"]["count"], 101)
        # No RUN was issued (no silent resume).
        cmds = [w.decode("ascii").strip() for w in fs.written]
        self.assertIn("SINGle", cmds)
        self.assertNotIn("RUN", cmds)
        self.assertNotIn("STOP", cmds)
        t.close()

    def test_no_trigger_fails(self):
        program = [
            ("ACQ:STAT?", b"STOP\n"),
            ("TRIG:A:MODE?", b"AUTO\n"),
            ("TRIG:A:SOUR?", b"CH1\n"),
            ("*IDN?", b"X\n"),
            ("SINGle", b""),
        ] + [("ACQ:STAT?", b"RUN\n")] * 40
        s, t, fs = session(program, timeout=0.4, poll=0.02)
        with self.assertRaises(NoTrigger):
            s.arm_and_wait()
        t.close()

    def test_timeout_without_completion_fails(self):
        program = [
            ("ACQ:STAT?", b"STOP\n"),
            ("TRIG:A:MODE?", b"AUTO\n"),
            ("TRIG:A:SOUR?", b"CH1\n"),
            ("*IDN?", b"X\n"),
            ("SINGle", b""),
        ]
        s, t, fs = session(program, timeout=0.3, poll=0.02)
        with self.assertRaises(CaptureTimeout):
            s.arm_and_wait()
        t.close()

    def test_interrupted_fails(self):
        program = [
            ("ACQ:STAT?", b"COMP\n"),
            ("TRIG:A:MODE?", b"AUTO\n"),
            ("TRIG:A:SOUR?", b"CH1\n"),
            ("*IDN?", b"X\n"),
            ("SINGle", b""),
            ("ACQ:STAT?", b"BREAK\n"),
        ]
        s, t, fs = session(program, timeout=1.0)
        with self.assertRaises(CaptureInterrupted):
            s.arm_and_wait()
        t.close()


if __name__ == "__main__":
    unittest.main()
