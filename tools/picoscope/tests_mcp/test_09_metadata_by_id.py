"""Category 9: metadata-by-id (read existing capture metadata, no device)."""

from tools.picoscope.tests_mcp.mcp_client import run, structured

_CHANNEL = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def test_metadata_by_capture_id(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _CHANNEL,
        }})
        cid = structured(res)["capture_id"]

        meta = await session.call_tool("scope_get_capture_metadata", {"capture_id": cid})
        assert not meta.is_error, meta
        data = structured(meta)
        assert data["capture_id"] == cid
        assert data["sample_count"] == 1000
        assert data["sample_interval_ns"] == 2048
        assert data["device"]["backend"] == "simulator"
        assert data["device"]["serial"] == "SIM0001"
        # The samples key is a dtype/shape descriptor, never raw arrays.
        assert "analog" in data["samples"] or "digital" in data["samples"]

    run(env, body)
