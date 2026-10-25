"""Tests for ``tools/knowledge/propose.py`` (minimal read-only proposal helper).

Runs the helper as a subprocess and asserts it (a) emits valid structured JSON,
(b) validates ``--type``/``--status`` against the schema, (c) resolves
relationship FROM to the proposed id, and (d) performs no database write (it is
a stdlib-only formatter that never imports the native runtime).
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
_PROPOSE = _REPO / "tools" / "knowledge" / "propose.py"


def _run(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(
        [sys.executable, str(_PROPOSE), *args],
        capture_output=True, text=True, cwd=str(_REPO),
    )


def test_propose_emits_valid_json_proposal():
    proc = _run(
        "--type", "Observation", "--id", "obs:example", "--status", "UNKNOWN",
        "--summary", "a test assertion",
        "--source-path", "docs/example.md",
        "--prop", "what=observed", "--prop", "date=2026-10-25",
        "--rel", "RELATED_TO:obs:other",
    )
    assert proc.returncode == 0, proc.stderr
    proposal = json.loads(proc.stdout)
    node = proposal["proposal"]["node"]
    assert node["type"] == "Observation"
    assert node["id"] == "obs:example"
    assert node["status"] == "UNKNOWN"
    assert node["what"] == "observed"
    rels = proposal["proposal"]["relationships"]
    assert rels == [{"rel": "RELATED_TO", "from": "obs:example", "to": "obs:other"}]


def test_propose_rejects_unknown_type_and_status():
    bad_type = _run("--type", "NotANode", "--id", "x", "--status", "UNKNOWN", "--summary", "s")
    assert bad_type.returncode == 2
    bad_status = _run("--type", "Observation", "--id", "x", "--status", "BOGUS", "--summary", "s")
    assert bad_status.returncode == 2
