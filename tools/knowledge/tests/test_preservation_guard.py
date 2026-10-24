"""Tests for ``tools/preservation_guard.py`` (read-only registry preservation guard).

Runs the guard as a subprocess with the current interpreter so the actual exit
code and machine-readable output are exercised, not just the in-process return
value. The guard is stdlib-only, so it runs under any Python 3.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]          # repository root
_GUARD = _REPO / "tools" / "preservation_guard.py"

_FAKE_REGISTRY = """format_version: 1
tools:
  - id: fake-tool
    name: Fake tool
    path: tools/definitely-does-not-exist-k4/
    preservation_policy: KEEP_PROTECTED
"""


def _run(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(
        [sys.executable, str(_GUARD), *args],
        capture_output=True,
        text=True,
        cwd=str(_REPO),
    )


def test_guard_passes_against_real_repo():
    """Against the real registry + repo, every KEEP* root exists -> exit 0."""
    proc = _run()
    assert proc.returncode == 0, f"stdout={proc.stdout}\nstderr={proc.stderr}"
    assert "MISSING_TOOL_ROOT" not in proc.stdout
    assert "MCP_NOT_REGISTERED" not in proc.stdout


def test_guard_reports_missing_root(tmp_path):
    """A KEEP tool whose path is absent is reported, with a non-zero exit code."""
    reg = tmp_path / "registry.yaml"
    reg.write_text(_FAKE_REGISTRY, encoding="utf-8")
    proc = _run("--registry", str(reg))
    assert proc.returncode != 0
    assert "MISSING_TOOL_ROOT fake-tool tools/definitely-does-not-exist-k4/" in proc.stdout


def test_guard_reports_unregistered_mcp(tmp_path):
    """An opencode.json missing the accepted MCPs is reported, non-zero exit."""
    opencode = tmp_path / "opencode.json"
    opencode.write_text('{"mcp": {}}', encoding="utf-8")
    proc = _run("--opencode", str(opencode))
    assert proc.returncode != 0
    assert "MCP_NOT_REGISTERED picoscope" in proc.stdout
    assert "MCP_NOT_REGISTERED knowledge" in proc.stdout
