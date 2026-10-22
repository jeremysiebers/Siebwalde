"""Category 11: invalid requests are rejected before any SDK call."""

from tools.picoscope.tests_mcp.mcp_client import error_text, run

_ANALOG = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def _assert_rejected(run, env, profile, fragment):
    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": profile})
        assert res.is_error, f"expected rejection for {profile!r}"
        text = error_text(res).lower()
        assert fragment.lower() in text, f"error text {text!r}"

    run(env, body)


def test_unknown_backend_rejected(env_factory):
    _assert_rejected(run, env_factory(), {"backend": "nope"}, "backend")


def test_invalid_analog_channel_rejected(env_factory):
    _assert_rejected(run, env_factory(),
                     {"backend": "simulator", "analog_channels": [{"channel": "E", "enabled": True, "range_v": 5.0, "coupling": "DC"}]},
                     "channel")


def test_invalid_coupling_rejected(env_factory):
    _assert_rejected(run, env_factory(),
                     {"backend": "simulator", "analog_channels": [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "XX"}]},
                     "coupling")


def test_nonpositive_range_rejected(env_factory):
    _assert_rejected(run, env_factory(),
                     {"backend": "simulator", "analog_channels": [{"channel": "A", "enabled": True, "range_v": 0.0, "coupling": "DC"}]},
                     "range_v")


def test_digital_threshold_out_of_range_rejected(env_factory):
    _assert_rejected(run, env_factory(),
                     {"backend": "simulator", "digital_ports": [{"port": 0, "enabled": True, "threshold_mv": 6000}]},
                     "threshold_mv")


def test_no_enabled_channel_rejected(env_factory):
    _assert_rejected(run, env_factory(),
                     {"backend": "simulator", "analog_channels": [], "digital_ports": []},
                     "enabled")


def test_trigger_source_not_enabled_rejected(env_factory):
    _assert_rejected(run, env_factory(),
                     {"backend": "simulator",
                      "analog_channels": _ANALOG,
                      "trigger": {"source": "B", "threshold": 0.0, "edge": "rising", "pre_samples": 10, "post_samples": 90}},
                     "trigger")
