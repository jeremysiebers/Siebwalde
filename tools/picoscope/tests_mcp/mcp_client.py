"""Reusable *real* MCP stdio test client for the offline MCP test suite.

Tests drive the actual FastMCP stdio server as a subprocess through the official
``mcp`` SDK client (``mcp.client.stdio.stdio_client`` + ``ClientSession``) — not
by calling the service functions directly. Each test therefore exercises the
real handshake, tool discovery, schema validation and tool invocation path.
"""

from __future__ import annotations

import asyncio
import os
import sys

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

#: Repository root (parent of ``tools/picoscope``).
REPO = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "..", "..")
)


def server_env(evidence_dir, **overrides) -> dict:
    """Base environment for a server subprocess, scoped to an evidence dir."""
    env = {**os.environ, "PICOSCOPE_EVIDENCE_DIR": str(evidence_dir)}
    env.update(overrides)
    return env


def make_params(env: dict) -> StdioServerParameters:
    return StdioServerParameters(
        command=sys.executable,
        args=["-m", "tools.picoscope.mcp"],
        cwd=REPO,
        env=env,
    )


async def run_with_server(env: dict, body) -> None:
    """Open one stdio connection to a fresh server and run ``body(session)``."""
    params = make_params(env)
    async with stdio_client(params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            await body(session)


def run(env: dict, body) -> None:
    """Sync wrapper: ``asyncio.run`` a ``run_with_server`` invocation."""
    asyncio.run(run_with_server(env, body))


def structured(result):
    """The structured (JSON) payload of a successful tool result, or ``None``."""
    return result.structured_content


def error_text(result) -> str:
    """The error text of a failed tool result (empty if none)."""
    if result.is_error and result.content:
        return result.content[0].text
    return ""
