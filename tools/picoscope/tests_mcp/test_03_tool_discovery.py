"""Category 3: tool discovery — exactly the five allowlisted tools."""

from tools.picoscope.tests_mcp.mcp_client import run

EXPECTED = {
    "scope_info",
    "scope_capture_block",
    "scope_get_capture_metadata",
    "scope_export_capture",
    "scope_analyze_capture",
}


def test_exactly_five_allowlisted_tools(env_factory):
    env = env_factory()

    async def body(session):
        tools = await session.list_tools()
        names = {t.name for t in tools.tools}
        assert names == EXPECTED, f"expected {sorted(EXPECTED)}, got {sorted(names)}"

    run(env, body)


def test_no_extra_tools_exposed(env_factory):
    env = env_factory()

    async def body(session):
        tools = await session.list_tools()
        names = {t.name for t in tools.tools}
        # Nothing beyond the measurement-only allowlist may be exposed.
        for name in names:
            assert name in EXPECTED, f"unexpected tool {name!r}"

    run(env, body)
