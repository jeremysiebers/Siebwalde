"""Category 19: metadata response size limit is enforced."""

from tools.picoscope.tests_mcp.mcp_client import error_text, run


def test_oversized_metadata_response_refused(env_factory):
    env = env_factory(PICOSCOPE_MCP_MAX_METADATA_RESPONSE_BYTES="200")

    async def body(session):
        res = await session.call_tool("scope_info", {"backend": "simulator"})
        assert res.is_error, "oversized metadata response should be refused"
        assert "too large" in error_text(res)

    run(env, body)


def test_default_limit_allows_scope_info(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_info", {"backend": "simulator"})
        assert not res.is_error, "scope_info should fit within the default limit"

    run(env, body)
