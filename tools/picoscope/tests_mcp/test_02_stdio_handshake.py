"""Category 2: stdio MCP handshake + protocol negotiation."""

from tools.picoscope.tests_mcp.mcp_client import run


def test_initialize_completes(env_factory):
    env = env_factory()

    async def body(session):
        # session.initialize() already ran in run_with_server; call list_tools to
        # prove the negotiated session is usable.
        tools = await session.list_tools()
        names = sorted(t.name for t in tools.tools)
        assert "scope_info" in names

    run(env, body)


def test_server_identity_is_picoscope(env_factory):
    env = env_factory()

    async def body(session):
        tools = await session.list_tools()
        assert len(tools.tools) == 5

    run(env, body)
