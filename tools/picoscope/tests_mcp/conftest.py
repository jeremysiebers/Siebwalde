"""Offline MCP integration tests (Layer C) — 20 categories.

These tests use a *real* MCP stdio client (see ``mcp_client.py``) against the
simulator backend. They never touch hardware and never open the real ``ps3000a``
driver (the operator gate stays closed).
"""

import os
import sys

import pytest

# Make ``tools`` importable regardless of pytest's import mode.
_REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
if _REPO not in sys.path:
    sys.path.insert(0, _REPO)

from tools.picoscope.tests_mcp.mcp_client import server_env  # noqa: E402


@pytest.fixture
def env_factory(tmp_path):
    """Return a factory for server subprocess environments scoped to ``tmp_path``."""

    def make(**overrides):
        return server_env(tmp_path, **overrides)

    return make
