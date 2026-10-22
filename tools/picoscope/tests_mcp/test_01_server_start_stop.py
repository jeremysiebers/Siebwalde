"""Category 1: MCP server start/stop over stdio."""

import os
import subprocess
import sys
import time

from tools.picoscope.tests_mcp.mcp_client import REPO, run, structured

_CHANNEL = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def test_server_starts_and_handshakes(env_factory):
    env = env_factory()

    async def body(session):
        tools = await session.list_tools()
        assert tools.tools, "server should expose tools after startup"

    run(env, body)


def test_server_stops_cleanly_on_stdin_close(env_factory):
    env = env_factory()
    proc = subprocess.Popen(
        [sys.executable, "-m", "tools.picoscope.mcp"],
        cwd=REPO,
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        env=env,
    )
    try:
        time.sleep(1.5)  # allow FastMCP to boot and bind stdio
        assert proc.poll() is None, "server should be running before stdin closes"
        proc.stdin.close()  # EOF signals shutdown
        try:
            proc.wait(timeout=15)
        except subprocess.TimeoutExpired:
            proc.kill()
            raise AssertionError("server did not stop within 15s of stdin EOF")
        assert proc.returncode == 0, f"expected clean exit, got {proc.returncode}"
    finally:
        if proc.poll() is None:
            proc.kill()
