"""Category 6: ``scope_info`` reports simulated identity + capabilities."""

from tools.picoscope.tests_mcp.mcp_client import run, structured


def test_scope_info_simulated_identity(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_info", {"backend": "simulator"})
        assert not res.is_error, res
        data = structured(res)
        assert data["model"] == "3406D MSO"
        assert data["backend"] == "simulator"
        assert data["serial"] == "SIM0001"
        assert data["variant"] == "SIMULATOR"
        assert data["sdk_driver"] == "simulator-1.0.0"
        assert data["simulated"] is True
        caps = data["capabilities"]
        assert caps["analog_channels"] == 4
        assert caps["digital_ports"] == 2
        assert caps["digital_lines"] == 16
        assert caps["block_capture"] is True
        assert caps["measurement_only"] is True

    run(env, body)


def test_scope_info_ownership_available(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_info", {"backend": "simulator"})
        data = structured(res)
        assert data["ownership"] == "available"

    run(env, body)
