"""Category 12: finite sample / channel / timeout limits are enforced."""

from tools.picoscope.tests_mcp.mcp_client import error_text, run

_ANALOG = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def _assert_limit_error(env, profile, fragment):
    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": profile})
        assert res.is_error, f"expected limit rejection for {profile!r}"
        text = error_text(res)
        assert "exceed" in text, f"expected 'exceed' in {text!r}"
        assert fragment in text, f"expected {fragment!r} in {text!r}"

    run(env, body)


def test_max_sample_count(env_factory):
    env = env_factory(PICOSCOPE_MCP_MAX_SAMPLE_COUNT="100")
    _assert_limit_error(env, {"backend": "simulator", "analog_channels": _ANALOG, "post_samples": 1000}, "sample_count")


def test_max_analog_channels(env_factory):
    env = env_factory(PICOSCOPE_MCP_MAX_ANALOG_CHANNELS="1")
    profile = {
        "backend": "simulator",
        "analog_channels": [
            {"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"},
            {"channel": "B", "enabled": True, "range_v": 5.0, "coupling": "DC"},
        ],
    }
    _assert_limit_error(env, profile, "analog_channels")


def test_max_trigger_wait(env_factory):
    env = env_factory(PICOSCOPE_MCP_MAX_TRIGGER_WAIT_MS="100")
    profile = {"backend": "simulator", "analog_channels": _ANALOG, "trigger_wait_ms": 5000}
    _assert_limit_error(env, profile, "trigger_wait_ms")


def test_max_duration(env_factory):
    env = env_factory(PICOSCOPE_MCP_MAX_DURATION_S="0.000001")
    profile = {"backend": "simulator", "analog_channels": _ANALOG, "post_samples": 1000, "timebase": 10}
    _assert_limit_error(env, profile, "duration")


def test_total_acquisition_timeout(env_factory):
    env = env_factory(PICOSCOPE_MCP_TOTAL_ACQUISITION_TIMEOUT_S="0.001")
    profile = {"backend": "simulator", "analog_channels": _ANALOG, "post_samples": 1000, "timebase": 10}
    _assert_limit_error(env, profile, "total_acquisition_timeout")


def test_max_memory(env_factory):
    env = env_factory(PICOSCOPE_MCP_MAX_MEMORY_BYTES="100")
    profile = {"backend": "simulator", "analog_channels": _ANALOG, "post_samples": 1000}
    _assert_limit_error(env, profile, "memory")
