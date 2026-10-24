"""Category 5: explicit simulator selection via the MCP client."""

from tools.picoscope.tests_mcp.mcp_client import run, structured

_CHANNEL = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def test_explicit_simulator_capture_succeeds(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator",
            "analog_channels": _CHANNEL,
        }})
        assert not res.is_error, res
        data = structured(res)
        assert data["backend"] == "simulator"
        assert data["simulated"] is True

    run(env, body)


def test_simulator_alias_is_canonicalised(env_factory):
    env = env_factory()

    async def body(session):
        info = await session.call_tool("scope_info", {"backend": "simulator"})
        assert not info.is_error
        assert structured(info)["backend"] == "simulator"

    run(env, body)
