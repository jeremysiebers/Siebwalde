"""Siebwalde Knowledge Foundation — typed property-graph schema.

This module is the single authoritative definition of the LadybugDB graph
schema (node tables + relationship tables) and the provenance/status
conventions. It is imported by the deterministic ingestion (``ingest``), the
read-only MCP server (``mcp``) and the tests, so the schema has exactly one
source.

Provenance convention (every node table carries these properties):
    id              STRING   stable URI-like identifier (PRIMARY KEY)
    label           STRING   short human-readable name
    status          STRING   assertion status (see ASSERTION_STATUSES)
    summary         STRING   the assertion, one sentence, English
    source_path     STRING   repository-relative path to the authoritative source
    source_location STRING   symbol / line / section reference within that source
    source_revision STRING   git revision the source was read from (or "unknown")
    source_hash     STRING   SHA-256 of the source/artifact, when applicable
    evidence_ref    STRING   reference to evidence (test id, capture id, log line)
    evidence_status STRING   how strong the evidence is (see EVIDENCE_STATUSES)
    observed_date   STRING   ISO date of the observation, when applicable
    superseded      BOOL     True when a later assertion replaces this one

Design note: LadybugDB (Kuzu successor) requires a pre-declared schema and a
PRIMARY KEY per node table; relationship tables have exactly one FROM and one
TO node type. There is no node-table inheritance, so the shared provenance
properties are repeated per table. The ``*_BY`` / ``SUPERSEDES`` relationships
are typed for their primary use (firmware artifacts and hypotheses); the full
provenance of any assertion is additionally carried as node properties above.
"""

from __future__ import annotations

ASSERTION_STATUSES = (
    "PROVEN_HARDWARE",
    "PROVEN_SOURCE",
    "PROVEN_OFFLINE_TEST",
    "INFERRED",
    "HYPOTHESIS",
    "UNKNOWN",
    "CONTRADICTED",
)

EVIDENCE_STATUSES = (
    "OBSERVED",        # directly observed on hardware / in a live session
    "TEST_PASS",       # an automated/offline test passed
    "SOURCE_REF",      # traced to source code / a document
    "DERIVED",         # derived from other evidence (weaker)
    "UNVERIFIED",      # recorded but not independently verified
)

# Shared provenance property columns (appended to every node table).
_PROVENANCE = [
    "label STRING",
    "status STRING",
    "summary STRING",
    "source_path STRING",
    "source_location STRING",
    "source_revision STRING",
    "source_hash STRING",
    "evidence_ref STRING",
    "evidence_status STRING",
    "observed_date STRING",
    "superseded BOOL",
]

# node table name -> type-specific property columns (the primary key and the
# shared provenance columns are added automatically).
NODE_TABLES = {
    "HardwareDevice": [
        "kind STRING",     # PIC18 | PIC32 | backplane | scope | amplifier
        "address STRING",  # Modbus slave id, IP, or COM port
        "role STRING",
    ],
    "FirmwareArtifact": [
        "kind STRING",     # application | bootloader | diagnostic
        "target STRING",   # PIC18 | PIC32
        "checksum STRING", # application-level checksum (e.g. "0x251F")
        "image_path STRING",
        "version STRING",
    ],
    "SourceRevision": [
        "revision STRING", # git SHA
        "branch STRING",
        "subject STRING",
    ],
    "ProtocolCommand": [
        "command INT64",
        "command_name STRING",
        "direction STRING",  # HOST_TO_PIC32 | PIC32_TO_PIC18 | PIC18_TO_PIC32
        "destructive BOOL",
        "description STRING",
    ],
    "ProtocolState": [
        "state_name STRING",
        "description STRING",
    ],
    "SafetyInvariant": [
        "statement STRING",
        "applies_to STRING",
        "requires_authorization BOOL",
    ],
    "TestExecution": [
        "test_name STRING",
        "result STRING",      # PASS | FAIL | BLOCKED
        "date STRING",
        "environment STRING",
    ],
    "Observation": [
        "what STRING",
        "date STRING",
        "method STRING",
    ],
    "Incident": [
        "incident_date STRING",
        "severity STRING",
        "target STRING",
    ],
    "Hypothesis": [
        "hypothesis STRING",
        "evidence_for STRING",
        "evidence_against STRING",
    ],
    "Decision": [
        "decision STRING",
        "decided_by STRING",
        "date STRING",
    ],
    "EvidenceSource": [
        "kind STRING",   # doc | source | log | capture | test | artifact
        "path STRING",
        "hash STRING",
        "format STRING",
    ],
}

# relationship table name -> (from_node, to_node, [property columns]).
REL_TABLES = {
    "RUNS_FIRMWARE": ("HardwareDevice", "FirmwareArtifact", [
        "since STRING",
        "superseded BOOL",
        "evidence_ref STRING",
    ]),
    "IMPLEMENTED_BY": ("FirmwareArtifact", "SourceRevision", [
        "note STRING",
    ]),
    "REQUIRES_STATE": ("ProtocolCommand", "ProtocolState", [
        "note STRING",
    ]),
    "EXPECTS_RESPONSE": ("ProtocolCommand", "ProtocolCommand", [
        "expected_taskcommand INT64",
        "note STRING",
    ]),
    "GUARDED_BY": ("ProtocolCommand", "SafetyInvariant", [
        "note STRING",
    ]),
    "OBSERVED_IN": ("Observation", "TestExecution", [
        "note STRING",
    ]),
    "PROVEN_BY": ("FirmwareArtifact", "EvidenceSource", [
        "note STRING",
    ]),
    "CONTRADICTED_BY": ("Hypothesis", "EvidenceSource", [
        "note STRING",
    ]),
    "SUPERSEDES": ("FirmwareArtifact", "FirmwareArtifact", [
        "note STRING",
    ]),
}


def _ddl_node(name: str, columns: list[str]) -> str:
    cols = ["id STRING PRIMARY KEY"] + list(columns) + list(_PROVENANCE)
    return f"CREATE NODE TABLE {name} ({', '.join(cols)})"


def _ddl_rel(name: str, from_node: str, to_node: str, columns: list[str]) -> str:
    cols = ", ".join(columns) if columns else ""
    if cols:
        return f"CREATE REL TABLE {name} (FROM {from_node} TO {to_node}, {cols})"
    return f"CREATE REL TABLE {name} (FROM {from_node} TO {to_node})"


def schema_ddl() -> list[str]:
    """Return the ordered list of DDL statements that build the full schema."""
    stmts = [f"CREATE NODE TABLE {name} ({', '.join(['id STRING PRIMARY KEY'] + list(cols) + list(_PROVENANCE))})"
             for name, cols in NODE_TABLES.items()]
    for name, (frm, to, cols) in REL_TABLES.items():
        stmts.append(_ddl_rel(name, frm, to, cols))
    return stmts


def schema_text() -> str:
    """Return the schema as a single Cypher DDL script."""
    return "\n".join(schema_ddl()) + "\n"


if __name__ == "__main__":
    print(schema_text())
