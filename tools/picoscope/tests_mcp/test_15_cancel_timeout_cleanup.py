"""Category 15: cancel/timeout cleanup — ownership + lock released deterministically."""

from tools.picoscope.tests_mcp.mcp_client import error_text, run, structured

_ANALOG = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def test_timeout_then_recapture_succeeds(env_factory):
    """After a timeout, the device is released so the next capture succeeds."""
    env = env_factory(PICOSCOPE_MCP_TEST_FAULT="inject_timeout")

    async def body(session):
        # First capture: the simulator injects a timeout (one-shot).
        first = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _ANALOG,
        }})
        assert first.is_error, "injected timeout should fail the capture"
        assert "timed out" in error_text(first).lower()

        # Second capture: the fault is consumed; cleanup must have released the
        # device (in-process ownership) and the cross-process lock.
        second = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _ANALOG,
        }})
        assert not second.is_error, f"recapture after timeout failed: {error_text(second)}"
        assert structured(second)["result"] == "ok"

    run(env, body)
