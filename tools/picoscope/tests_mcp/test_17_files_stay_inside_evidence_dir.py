"""Category 17: evidence stays inside the dedicated evidence directory."""

import os

from tools.picoscope.tests_mcp.mcp_client import run, structured

_ANALOG = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def test_artifact_paths_are_inside_evidence_dir(env_factory):
    env = env_factory()
    evidence_dir = os.path.realpath(env["PICOSCOPE_EVIDENCE_DIR"])

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _ANALOG,
        }})
        paths = structured(res)["artifact_paths"]
        assert set(paths) == {"npz", "json"}
        for kind, path in paths.items():
            resolved = os.path.realpath(path)
            common = os.path.commonpath([evidence_dir, resolved])
            assert common == evidence_dir, f"{kind} artifact escaped evidence dir: {resolved}"

    run(env, body)


def test_capture_writes_exactly_npz_and_json(env_factory):
    env = env_factory()
    evidence_dir = os.path.realpath(env["PICOSCOPE_EVIDENCE_DIR"])

    async def body(session):
        res = await session.call_tool("scope_capture_block", {"profile": {
            "backend": "simulator", "analog_channels": _ANALOG,
        }})
        cid = structured(res)["capture_id"]
        files = set(os.listdir(evidence_dir))
        assert cid + ".npz" in files
        assert cid + ".json" in files

    run(env, body)
