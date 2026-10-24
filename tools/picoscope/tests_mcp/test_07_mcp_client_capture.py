"""Category 7: an MCP-client-requested capture (analog + digital + trigger)."""

from tools.picoscope.tests_mcp.mcp_client import run, structured

_PROFILE = {
    "backend": "simulator",
    "analog_channels": [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}],
    "digital_ports": [{"port": 0, "enabled": True, "threshold_mv": 1500}],
    "trigger": {"source": "A", "threshold": 0.0, "edge": "rising", "pre_samples": 200, "post_samples": 800},
}


def test_capture_result_contract(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": _PROFILE})
        assert not res.is_error, res
        data = structured(res)
        assert set(data) >= {
            "capture_id", "sample_interval_ns", "sample_count", "trigger_status",
            "channel_mapping", "artifact_paths", "warnings", "result",
        }
        assert data["result"] == "ok"
        assert data["sample_count"] == 1000
        assert data["sample_interval_ns"] == 2048  # simulator timebase 10
        assert data["trigger_status"] == "triggered"
        assert "analog" in data["channel_mapping"]
        assert "digital" in data["channel_mapping"]
        assert data["artifact_paths"]["npz"].endswith(".npz")
        assert data["artifact_paths"]["json"].endswith(".json")

    run(env, body)
