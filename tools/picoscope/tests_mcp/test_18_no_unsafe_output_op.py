"""Category 18: no unsafe output operation (measurement-only, static surface)."""

import ast
import os

from tools.picoscope.tests_mcp.mcp_client import run

_MCP_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "mcp"))

#: Forbidden *operation* tokens, matched only against AST identifiers (names,
#: attributes, function defs) — never against docstring/comment prose — so the
#: safety documentation itself cannot trigger a false positive.
FORBIDDEN_OPS = (
    "awg",
    "sig_gen",
    "signal_generator",
    "flash",
    "reset",
    "motion",
    "backplane",
    "set_waveform",
)

#: The MCP layer may only call these Layer A measurement entry points.
ALLOWED_BACKEND_CALLS = {
    "discover_devices",
    "open_device",
    "get_device_info",
    "configure_analog_channel",
    "configure_digital_port",
    "configure_trigger",
    "capture_block",
    "save_capture",
    "load_capture",
    "close_device",
}


def _sources():
    out = []
    for name in sorted(os.listdir(_MCP_DIR)):
        if name.endswith(".py"):
            out.append((name, os.path.join(_MCP_DIR, name)))
    return out


def _trees():
    for name, path in _sources():
        with open(path, "r", encoding="utf-8") as fh:
            yield name, ast.parse(fh.read())


def test_no_forbidden_operation_identifiers():
    for name, tree in _trees():
        for node in ast.walk(tree):
            for check, text in (
                (isinstance(node, ast.FunctionDef), getattr(node, "name", "")),
                (isinstance(node, ast.Attribute), getattr(node, "attr", "")),
                (isinstance(node, ast.Name), getattr(node, "id", "")),
            ):
                if check:
                    lowered = text.lower()
                    for token in FORBIDDEN_OPS:
                        assert token not in lowered, f"{name}: identifier {text!r} matches {token!r}"


def test_no_shell_or_subprocess_execution():
    for name, path in _sources():
        with open(path, "r", encoding="utf-8") as fh:
            src = fh.read()
        for marker in ("subprocess", "os.system", "os.popen", "shell=True", "shell = True"):
            assert marker not in src, f"{name}: shell/subprocess marker {marker!r}"


def test_no_direct_vendor_sdk_calls():
    for name, tree in _trees():
        for node in ast.walk(tree):
            if isinstance(node, ast.Attribute) and node.attr.startswith("ps3000a"):
                raise AssertionError(f"{name}: direct vendor SDK call {node.attr!r}")


def test_only_allowlisted_backend_calls():
    for name, tree in _trees():
        for node in ast.walk(tree):
            if isinstance(node, ast.Attribute) and isinstance(node.value, ast.Name) and node.value.id == "_backend_api":
                assert node.attr in ALLOWED_BACKEND_CALLS, (
                    f"{name}: non-allowlisted backend op {node.attr!r}"
                )


def test_exactly_five_tools_via_client(env_factory):
    """The exposed tool surface is exactly the five measurement/analysis tools."""
    env = env_factory()
    expected = {
        "scope_info",
        "scope_capture_block",
        "scope_get_capture_metadata",
        "scope_export_capture",
        "scope_analyze_capture",
    }

    async def body(session):
        names = {t.name for t in (await session.list_tools()).tools}
        assert names == expected

    run(env, body)
