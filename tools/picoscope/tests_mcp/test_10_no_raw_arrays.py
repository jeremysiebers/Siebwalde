"""Category 10: no raw sample arrays ever appear in an MCP response."""

import json

from tools.picoscope.tests_mcp.mcp_client import run, structured

_CHANNEL = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"},
            {"channel": "B", "enabled": True, "range_v": 2.0, "coupling": "DC"}]


def _assert_no_raw_arrays(payload: dict, label: str):
    text = json.dumps(payload, sort_keys=True, default=str)
    # Raw sample keys must never leak into any response.
    for forbidden in ("analog_samples", "digital_samples", "samples\": [", "array("):
        assert forbidden not in text, f"{label}: leaked raw array marker {forbidden!r}"
    # The samples descriptor may only carry dtype/shape (strings/ints), no data.
    samples = payload.get("samples") or payload.get("metadata", {}).get("samples")
    if samples:
        for group in samples.values():
            for entry in group.values():
                assert "dtype" in entry and "shape" in entry
                assert "data" not in entry and "values" not in entry


def test_capture_result_has_no_raw_arrays(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _CHANNEL,
        }})
        _assert_no_raw_arrays(structured(res), "capture result")

    run(env, body)


def test_metadata_and_export_have_no_raw_arrays(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _CHANNEL,
        }})
        cid = structured(res)["capture_id"]

        meta = await session.call_tool("scope_get_capture_metadata", {"capture_id": cid})
        _assert_no_raw_arrays(structured(meta), "metadata")

        exp = await session.call_tool("scope_export_capture", {"capture_id": cid})
        _assert_no_raw_arrays(structured(exp), "export")

    run(env, body)
