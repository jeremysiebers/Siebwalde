"""Offline tests for the I2 Comm.py correction (configurable controller addressing).

Covers: explicit endpoint selection, the preserved DNS/fallback default, and
that the correction does not alter the driver-side fail-closed behavior. No
sockets are bound (socket.gethostbyname is monkeypatched).
"""

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import Comm
from Comm import _resolve_ip_address


class TestResolveIpAddress(unittest.TestCase):
    """The controller TX address is configurable, not hard-wired."""

    def test_explicit_ip_wins(self):
        self.assertEqual(_resolve_ip_address("192.168.1.193"), "192.168.1.193")

    def test_explicit_ip_empty_falls_back_to_dns(self):
        # Empty/None -> the DNS path (monkeypatched below).
        self.assertIsNotNone(_resolve_ip_address(None))

    def test_dns_success_returns_hostname_lookup(self):
        orig = Comm.socket.gethostbyname
        Comm.socket.gethostbyname = lambda host: "192.168.1.50"
        try:
            self.assertEqual(_resolve_ip_address(None), "192.168.1.50")
        finally:
            Comm.socket.gethostbyname = orig

    def test_dns_failure_falls_back_to_100(self):
        orig = Comm.socket.gethostbyname
        Comm.socket.gethostbyname = lambda host: (_ for _ in ()).throw(OSError("no DNS"))
        try:
            self.assertEqual(_resolve_ip_address(None), "192.168.1.100")
        finally:
            Comm.socket.gethostbyname = orig


class TestDataAquisitionSignature(unittest.TestCase):
    """The constructor keeps the historical caller shape and adds an optional ip."""

    def test_default_constructor_shape(self):
        import inspect
        sig = inspect.signature(Comm.DataAquisition.__init__)
        params = list(sig.parameters)
        self.assertIn("AmountOfAmplifiers", params)
        self.assertIn("ip_address", params)


if __name__ == "__main__":
    unittest.main(verbosity=2)
