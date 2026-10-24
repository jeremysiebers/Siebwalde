"""Category 23: MCP digital edge-trigger compatibility (simulator backend).

Exercises the MCP ``scope_capture_block`` tool with a digital trigger source
(D0..D15) through the real stdio client, proving the schema accepts digital
sources + edges, the capture reports the correct trigger state, and the
effective trigger split remains budgeted. These tests never touch hardware.
"""

import os
import sys

import pytest

_REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
if _REPO not in sys.path:
    sys.path.insert(0, _REPO)

from tools.picoscope.tests_mcp.mcp_client import error_text, run, structured  # noqa: E402


def test_digital_trigger_rising_via_mcp(env_factory):
    env = env_factory()
    profile = {
        "backend": "simulator",
        "digital_ports": [{"port": 0, "enabled": True, "threshold_mv": 1500}],
        "trigger": {"source": "D0", "threshold": 0.0, "edge": "rising",
                    "pre_samples": 100, "post_samples": 900},
    }

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": profile})
        assert not res.is_error, error_text(res)
        data = structured(res)
        assert data["result"] == "ok"
        assert data["trigger_status"] == "triggered"
        assert data["trigger_position"] == 100
        assert data["trigger_index"] == 100
        assert data["sample_count"] == 1000

    run(env, body)


def test_digital_trigger_falling_via_mcp_port1(env_factory):
    env = env_factory()
    profile = {
        "backend": "simulator",
        "digital_ports": [{"port": 1, "enabled": True, "threshold_mv": 1500}],
        "trigger": {"source": "D8", "threshold": 0.0, "edge": "falling",
                    "pre_samples": 50, "post_samples": 450},
    }

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": profile})
        assert not res.is_error, error_text(res)
        data = structured(res)
        assert data["result"] == "ok"
        assert data["trigger_status"] == "triggered"
        assert data["trigger_index"] == 50

    run(env, body)


def test_digital_trigger_invalid_source_rejected(env_factory):
    env = env_factory()
    profile = {
        "backend": "simulator",
        "digital_ports": [{"port": 0, "enabled": True, "threshold_mv": 1500}],
        "trigger": {"source": "D16", "threshold": 0.0, "edge": "rising",
                    "pre_samples": 0, "post_samples": 100},
    }

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": profile})
        assert res.is_error, "D16 must be rejected"

    run(env, body)


def test_digital_trigger_disabled_port_rejected(env_factory):
    env = env_factory()
    profile = {
        "backend": "simulator",
        "digital_ports": [{"port": 0, "enabled": False, "threshold_mv": 1500}],
        "trigger": {"source": "D0", "threshold": 0.0, "edge": "rising",
                    "pre_samples": 0, "post_samples": 100},
    }

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": profile})
        assert res.is_error, "a trigger on a disabled port must be rejected"

    run(env, body)


# -- resource-limit enforcement uses the effective digital trigger split -------


def test_digital_trigger_split_over_budget_is_rejected(tmp_path):
    from tools.picoscope.backend.errors import InvalidConfigError
    from tools.picoscope.mcp.config import ServerConfig
    from tools.picoscope.mcp.schema import AcquisitionProfile
    from tools.picoscope.mcp.service import ScopeService

    service = ScopeService(ServerConfig(
        evidence_dir=str(tmp_path / "evidence"),
        allow_real_backend=False,
        max_sample_count=5_000,
    ))
    profile = AcquisitionProfile(
        backend="simulator",
        digital_ports=[{"port": 0, "enabled": True, "threshold_mv": 1000}],
        pre_samples=0,
        post_samples=1_000,
        trigger={"source": "D0", "threshold": 0.0, "edge": "rising",
                 "pre_samples": 0, "post_samples": 100_000},
    )
    with pytest.raises(InvalidConfigError) as exc_info:
        service.capture_block(profile)
    assert "sample_count" in str(exc_info.value)
