"""Category 16: unknown / malformed capture id fail-safe (no device, no crash)."""

from tools.picoscope.tests_mcp.mcp_client import error_text, run


def test_unknown_capture_id_fails_cleanly(env_factory):
    env = env_factory()

    async def body(session):
        for tool in ("scope_get_capture_metadata", "scope_export_capture"):
            res = await session.call_tool(tool, {"capture_id": "deadbeefdeadbeefdeadbeefdeadbeef"})
            assert res.is_error, f"{tool} should fail on unknown id"
            assert "No capture found" in error_text(res)

    run(env, body)


def test_malformed_capture_id_is_rejected(env_factory):
    env = env_factory()

    async def body(session):
        for bad in ("../../etc/passwd", "..\\..\\win", "abc", "A" * 33):
            res = await session.call_tool("scope_get_capture_metadata", {"capture_id": bad})
            assert res.is_error, f"malformed id {bad!r} should be rejected"
            assert "Invalid capture_id" in error_text(res)

    run(env, body)
