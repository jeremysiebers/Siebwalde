"""Category 14: simulated device-error propagation (clean error, no crash)."""

from tools.picoscope.tests_mcp.mcp_client import error_text, run

_ANALOG = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def test_simulated_device_error_surfaces(env_factory):
    env = env_factory(PICOSCOPE_MCP_TEST_FAULT="inject_error:simulated-boom")

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _ANALOG,
        }})
        assert res.is_error, "device error should propagate as an error result"
        assert "simulated-boom" in error_text(res)

    run(env, body)
