"""Minimal knowledge-capture proposal helper (read-only, stdlib-only).

Emits a structured JSON proposal for a new or updated Knowledge Foundation
seed entry from command-line arguments. It writes nothing, opens no database,
and performs no ingestion. It is a proposal formatter — NOT a general-purpose
editor and NOT an ingestion tool. The authoritative write path (proposal →
validation → independent review → Git seed → deterministic rebuild → controlled
operational sync → MCP readback) is documented in
``docs/development-workflow.md`` §14.12.

Usage::

    python tools/knowledge/propose.py \
        --type Observation --id obs:example --status UNKNOWN \
        --summary "..." --source-path docs/example.md --source-revision <sha> \
        --prop what="..." --prop date="2026-10-25" --prop method="..." \
        --rel SUPPORTED_BY:hyp:example

The emitted JSON is a *proposal*; an agent must still validate, review and
persist it through the normal path. ``--type`` and ``--status`` are validated
against ``schema.py``; type-specific fields are passed through via ``--prop``
without further interpretation.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

_KNOWLEDGE = Path(__file__).resolve().parent
if str(_KNOWLEDGE) not in sys.path:
    sys.path.insert(0, str(_KNOWLEDGE))

import schema  # noqa: E402  (pure-Python; no native runtime required)


def _parse_rel(value: str) -> tuple[str, str]:
    """Parse ``RELNAME:TO`` (FROM is always the proposed node ``--id``).

    The target id may itself contain colons (e.g. ``obs:example``), so the split
    is on the *first* colon only.
    """
    rel, sep, to = value.partition(":")
    if not sep or not rel or not to:
        raise ValueError(
            f"invalid --rel '{value}': expected RELNAME:TO (FROM defaults to --id)"
        )
    return rel, to


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        description="Emit a structured knowledge-capture proposal as JSON (no writes)."
    )
    p.add_argument("--type", required=True, help="node type (schema.NODE_TABLES key)")
    p.add_argument("--id", required=True, help="stable node id (primary key)")
    p.add_argument("--status", required=True, help="assertion status")
    p.add_argument("--summary", required=True, help="one-sentence assertion")
    p.add_argument("--label", default=None, help="short human-readable label")
    p.add_argument("--source-path", default=None)
    p.add_argument("--source-location", default=None)
    p.add_argument("--source-revision", default=None)
    p.add_argument("--evidence-ref", default=None)
    p.add_argument("--evidence-status", default=None)
    p.add_argument("--observed-date", default=None)
    p.add_argument("--prop", action="append", default=[], metavar="KEY=VALUE",
                   help="type-specific property (repeatable), e.g. what='...'")
    p.add_argument("--rel", action="append", default=[], metavar="REL:TO",
                   help="relationship (repeatable); FROM defaults to --id")
    return p


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    if args.type not in schema.NODE_TABLES:
        print(f"error: unknown node type '{args.type}'", file=sys.stderr)
        return 2
    if args.status not in schema.ASSERTION_STATUSES:
        print(f"error: unknown status '{args.status}'", file=sys.stderr)
        return 2

    node = {"type": args.type, "id": args.id, "status": args.status,
            "summary": args.summary}
    if args.label is not None:
        node["label"] = args.label
    for key, value in (
        ("source_path", args.source_path),
        ("source_location", args.source_location),
        ("source_revision", args.source_revision),
        ("evidence_ref", args.evidence_ref),
        ("evidence_status", args.evidence_status),
        ("observed_date", args.observed_date),
    ):
        if value is not None:
            node[key] = value
    for kv in args.prop:
        if "=" not in kv:
            print(f"error: invalid --prop '{kv}' (expected KEY=VALUE)", file=sys.stderr)
            return 2
        key, value = kv.split("=", 1)
        node[key] = value

    rels = []
    for raw in args.rel:
        try:
            rel, to = _parse_rel(raw)
        except ValueError as exc:
            print(f"error: {exc}", file=sys.stderr)
            return 2
        rels.append({"rel": rel, "from": args.id, "to": to})

    proposal = {"proposal": {"node": node, "relationships": rels}}
    print(json.dumps(proposal, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
