"""Shared fixtures for the picoscope offline test suite."""

import pytest

from tools.picoscope.backend import ownership


@pytest.fixture(autouse=True)
def _reset_ownership():
    """Ensure no test leaks device ownership into another.

    Tests open/close devices through the public API (which releases
    deterministically); this is a defensive teardown for the process-local
    ownership registry only.
    """
    ownership.release_all()
    yield
    ownership.release_all()
