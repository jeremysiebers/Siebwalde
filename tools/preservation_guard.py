#!/usr/bin/env python3
"""Siebwalde engineering tool-registry preservation guard (K4-A).

A *read-only* checker — a guard, not a mutator. It validates the durable tool
registry (``tools/tool-registry.yaml``) against the repository on disk and
asserts that the two accepted MCP components (``picoscope``, ``knowledge``) are
still registered and enabled in ``opencode.json``. It never deletes, moves, or
modifies any tool or file.

Exit codes
----------
0   every protected root exists and both accepted MCPs are registered+enabled.
1   at least one violation was found.

Machine-readable violations (one per line on stdout)
----------------------------------------------------
MISSING_TOOL_ROOT <id> <path>
MCP_NOT_REGISTERED <name>
REGISTRY_UNPARSEABLE

Dependencies
------------
Python 3 standard library only (no PyYAML, no LadybugDB). The registry is a
small, stable, versioned document (``format_version: 1``); instead of requiring
an external YAML dependency this guard ships a minimal, purpose-built parser
for that exact shape (see ``_parse_registry``). PyYAML is deliberately not used
so the guard stays dependency-free and deterministic on any stock Python 3.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

REGISTRY_REL = Path("tools") / "tool-registry.yaml"
OPENCODE_REL = Path("opencode.json")

# The two accepted MCP components that must remain registered and enabled.
ACCEPTED_MCPS = ("picoscope", "knowledge")

_KEEP_PREFIX = "KEEP"


class _RegistryError(Exception):
    """Raised when the tool registry cannot be parsed."""


def _strip_scalar(raw: str) -> str | None:
    """Clean a single-line scalar value; a bare ``null``/``~`` becomes None."""
    v = raw.strip()
    if v in ("null", "~"):
        return None
    if len(v) >= 2 and v[0] == v[-1] and v[0] in ("'", '"'):
        v = v[1:-1]
    return v


def _parse_registry(text: str) -> list[dict]:
    """Parse the tool registry (``format_version: 1``) into a list of tool dicts.

    Minimal grammar handled (exact to this file's shape):

        format_version: N            top-level scalar (must be ``1``)
        tools:                       top-level list marker
          - id: <id>                 each tool record starts here
            key: <value>             scalar fields (4-space indent)
            (deeper-indented list items, e.g. ``capabilities:``, are ignored)

    Full-line comments (``#``) and blank lines are ignored. The parser is
    intentionally narrow: the guard only consumes the scalar fields ``id``,
    ``path`` and ``preservation_policy``; any other scalar present is stored
    but unused. If the document shape or ``format_version`` ever changes, this
    parser (and this note) must be revisited.
    """
    tools: list[dict] = []
    current: dict | None = None
    version: str | None = None
    seen_tools = False

    for lineno, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        if stripped.startswith("format_version:"):
            version = _strip_scalar(line.split(":", 1)[1])
            continue
        if stripped == "tools:":
            seen_tools = True
            continue
        m = re.match(r"^  - id:\s*(.*)$", line)
        if m:
            if not seen_tools:
                raise _RegistryError(
                    f"line {lineno}: tool record before 'tools:' marker"
                )
            current = {"id": _strip_scalar(m.group(1))}
            tools.append(current)
            continue
        m = re.match(r"^    (\w+):\s*(.*)$", line)
        if m:
            if current is None:
                raise _RegistryError(
                    f"line {lineno}: scalar field outside a tool record"
                )
            current[m.group(1)] = _strip_scalar(m.group(2))
            continue
        # Anything else (deeper list items, unknown top-level keys) is ignored.

    if version is not None and version != "1":
        raise _RegistryError(
            f"unsupported format_version {version!r} (expected '1')"
        )
    return tools


def _load_opencode(path: Path) -> dict:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return {}


def run_checks(repo_root: Path, registry_path: Path, opencode_path: Path):
    """Run all guard checks; return ``(violations, exit_code)``."""
    violations: list[str] = []

    # 1. Registry: every KEEP* tool root must exist on disk.
    try:
        tools = _parse_registry(registry_path.read_text(encoding="utf-8"))
    except Exception:
        return ["REGISTRY_UNPARSEABLE"], 1

    for tool in tools:
        policy = tool.get("preservation_policy") or ""
        if not policy.startswith(_KEEP_PREFIX):
            continue
        tid = tool.get("id") or "<unknown>"
        rel = tool.get("path")
        if not rel:
            violations.append(f"MISSING_TOOL_ROOT {tid} <no-path>")
            continue
        if not (repo_root / rel).exists():
            violations.append(f"MISSING_TOOL_ROOT {tid} {rel}")

    # 2. opencode.json: both accepted MCPs registered and enabled.
    cfg = _load_opencode(opencode_path)
    mcp = cfg.get("mcp") or {}
    for name in ACCEPTED_MCPS:
        entry = mcp.get(name)
        if not isinstance(entry, dict) or entry.get("enabled") is not True:
            violations.append(f"MCP_NOT_REGISTERED {name}")

    return violations, (0 if not violations else 1)


def _default_repo_root() -> Path:
    # The repo root is the parent of this script's ``tools`` directory.
    return Path(__file__).resolve().parent.parent


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Read-only preservation guard for the Siebwalde tool registry."
    )
    parser.add_argument(
        "--repo-root", type=Path, default=None,
        help="repository root (default: parent of this script's directory)",
    )
    parser.add_argument(
        "--registry", type=Path, default=None,
        help="path to tool-registry.yaml (default: <repo-root>/tools/tool-registry.yaml)",
    )
    parser.add_argument(
        "--opencode", type=Path, default=None,
        help="path to opencode.json (default: <repo-root>/opencode.json)",
    )
    args = parser.parse_args(argv)

    repo_root = args.repo_root or _default_repo_root()
    registry = args.registry or (repo_root / REGISTRY_REL)
    opencode = args.opencode or (repo_root / OPENCODE_REL)

    violations, code = run_checks(repo_root, registry, opencode)
    for v in violations:
        print(v)
    if code == 0:
        print("OK: all protected roots present; accepted MCPs registered", file=sys.stderr)
    else:
        print(f"{len(violations)} violation(s)", file=sys.stderr)
    return code


if __name__ == "__main__":
    raise SystemExit(main())
