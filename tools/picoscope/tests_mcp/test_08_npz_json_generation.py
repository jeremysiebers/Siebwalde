"""Category 8: NPZ + JSON evidence generation on disk."""

import json
import os

import numpy as np

from tools.picoscope.tests_mcp.mcp_client import run, structured

_CHANNEL = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def test_npz_and_json_artifacts_written(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _CHANNEL,
        }})
        data = structured(res)
        npz = data["artifact_paths"]["npz"]
        sidecar = data["artifact_paths"]["json"]
        assert os.path.isfile(npz)
        assert os.path.isfile(sidecar)
        assert os.path.getsize(npz) > 0
        assert os.path.getsize(sidecar) > 0

        with open(sidecar, "r", encoding="utf-8") as fh:
            meta = json.load(fh)
        assert meta["format_version"] == 1
        assert meta["capture_id"] == data["capture_id"]
        assert meta["sample_count"] == data["sample_count"]
        assert meta["provenance"]["simulated"] is True

        with np.load(npz) as z:
            assert "analog_A" in z.files

    run(env, body)


def test_digital_npz_has_digital_lines(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator",
            "digital_ports": [{"port": 0, "enabled": True, "threshold_mv": 1500}],
        }})
        data = structured(res)
        with np.load(data["artifact_paths"]["npz"]) as z:
            assert "digital_D0" in z.files

    run(env, body)
