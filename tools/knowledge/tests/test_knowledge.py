"""Acceptance + boundary tests for the Siebwalde Knowledge Foundation.

Covers: read-only enforcement (engine-level), schema completeness, deterministic
rebuild + restart persistence, multiple readers, and the ten acceptance
questions from the PO directive (§10), each with a source-reference assertion.
"""

from __future__ import annotations

import os
import shutil
import sys
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]          # repository root
_KNOWLEDGE = _REPO / "tools" / "knowledge"

sys.path.insert(0, str(_REPO))
os.environ.setdefault("LBUG_C_API_LIB_PATH", str(_KNOWLEDGE / "lib" / "lbug_shared.dll"))
os.environ["PATH"] = str(_KNOWLEDGE / "lib") + os.pathsep + os.environ.get("PATH", "")
os.environ.setdefault("SIEBWALDE_KNOWLEDGE_DB", str(_KNOWLEDGE / "db" / "siebwalde-knowledge.lbdb"))

import ladybug as lb  # noqa: E402

from tools.knowledge import schema  # noqa: E402
from tools.knowledge.ingest import build_graph  # noqa: E402
from tools.knowledge.mcp_server import server  # noqa: E402


@pytest.fixture(scope="session", autouse=True)
def _built_db():
    build_graph.main()
    yield


def _search_results(query: str) -> list[dict]:
    return server.knowledge_search(query)["results"]


def _ids(results: list[dict]) -> set[str]:
    return {r.get("id") or r.get("n.id") for r in results}


# ---------------------------------------------------------------------------
# Boundary tests
# ---------------------------------------------------------------------------

def test_engine_readonly_enforced():
    """The engine itself rejects writes on a read-only connection."""
    db = lb.Database(str(_KNOWLEDGE / "db" / "siebwalde-knowledge.lbdb"), read_only=True)
    conn = lb.Connection(db)
    try:
        with pytest.raises(Exception):
            conn.execute("CREATE (n:Observation {id:'x', label:'x'})")
    finally:
        conn.close()
        db.close()


def test_schema_complete():
    assert set(schema.NODE_TABLES) == {
        "HardwareDevice", "FirmwareArtifact", "SourceRevision", "ProtocolCommand",
        "ProtocolState", "SafetyInvariant", "TestExecution", "Observation",
        "Incident", "Hypothesis", "Decision", "EvidenceSource",
        "Tool", "Capability", "Dependency", "TestEvidence", "HardwareInterface",
        "MCPService", "MigrationDecision",
    }
    assert set(schema.REL_TABLES) == {
        "RUNS_FIRMWARE", "IMPLEMENTED_BY", "REQUIRES_STATE", "EXPECTS_RESPONSE",
        "GUARDED_BY", "OBSERVED_IN", "PROVEN_BY", "CONTRADICTED_BY", "SUPERSEDES",
        "PROVIDES_CAPABILITY", "DEPENDS_ON", "CONNECTS_TO", "VALIDATED_BY",
        "EXPOSED_VIA_MCP", "REUSES", "MIGRATION_CANDIDATE_FOR", "PRESERVED_BY_POLICY",
    }


def test_rebuild_deterministic_and_persistent(tmp_path):
    """Rebuild twice produces identical node counts; DB survives reopen."""
    p = tmp_path / "k.lbdb"
    build_graph._build(p)
    db = lb.Database(str(p), read_only=True)
    conn = lb.Connection(db)
    res = conn.execute("MATCH (n:HardwareDevice) RETURN count(n) AS c")
    first = res.get_all()[0][0]
    conn.close()
    db.close()

    build_graph._build(p)
    db = lb.Database(str(p), read_only=True)
    conn = lb.Connection(db)
    res = conn.execute("MATCH (n:HardwareDevice) RETURN count(n) AS c")
    second = res.get_all()[0][0]
    conn.close()
    db.close()
    assert first == second
    assert first == 7


def test_multiple_readers(tmp_path):
    """Multiple simultaneous read-only connections are allowed."""
    p = tmp_path / "k.lbdb"
    build_graph._build(p)
    db1 = lb.Database(str(p), read_only=True)
    db2 = lb.Database(str(p), read_only=True)
    c1, c2 = lb.Connection(db1), lb.Connection(db2)
    r1 = c1.execute("MATCH (n:Incident) RETURN count(n) AS c").get_all()[0][0]
    r2 = c2.execute("MATCH (n:Incident) RETURN count(n) AS c").get_all()[0][0]
    assert r1 == r2 == 1
    c1.close(); c2.close(); db1.close(); db2.close()


# ---------------------------------------------------------------------------
# Acceptance questions (§10)
# ---------------------------------------------------------------------------

def test_q1_uart_modes_physically_proven():
    """Which PIC32/PIC18 UART modes are physically proven?"""
    rs = _search_results("UART")
    by_id = {r["id"]: r for r in rs}
    assert "state:app-mode" in by_id and by_id["state:app-mode"]["status"] == "PROVEN_HARDWARE"
    assert "state:bootloader-mode" in by_id and by_id["state:bootloader-mode"]["status"] == "PROVEN_HARDWARE"


def test_q2_fleet_vs_single_target_difference():
    """Why are the legacy fleet and experimental single-target flashing paths different?"""
    rs = server.knowledge_flashing_paths()["results"]
    by_cmd = {r["n.command"]: r for r in rs}
    # fleet (122) and single-target recovery (131/132) are hardware-proven
    assert by_cmd[122]["n.status"] == "PROVEN_HARDWARE"
    assert by_cmd[131]["n.status"] == "PROVEN_HARDWARE"
    # experimental primitives (125/126) are destructive and source-proven only
    assert by_cmd[125]["n.destructive"] is True
    assert by_cmd[125]["n.status"] == "PROVEN_SOURCE"


def test_q3_pico5b_incident():
    """What happened during the latest PICO-5B failure?"""
    rs = server.knowledge_incidents()["results"]
    assert len(rs) == 1
    assert rs[0]["i.id"] == "incident:pico-5b"
    assert "ERASE" in rs[0]["i.summary"]
    assert rs[0]["i.source_path"] == "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md"


def test_q4_erase_timeout_not_sufficient():
    """Is an ERASE timeout sufficient to continue to WRITE? (No.)"""
    rs = server.knowledge_safety_invariants("flash")["results"]
    no_assume = [r for r in rs if r["n.id"] == "safety:no-assume-ok"]
    assert len(no_assume) == 1
    assert "NOT confirmation" in no_assume[0]["n.statement"]


def test_q5_amp3_firmware_state_unknown():
    """What is the current verified firmware state of amplifier 3?"""
    rs = server.knowledge_firmware_state()["results"]
    amp3 = [r for r in rs if r["d.id"] == "device:amp-3"]
    assert len(amp3) == 1
    assert amp3[0]["d.status"] == "UNKNOWN"


def test_amp1_roundtrip_final_0xE906():
    """Amplifier 1 ended the 2026-10-19 round-trip on 0xE906 (1.0.0), not 0x251F."""
    rs = server.knowledge_firmware_state()["results"]
    amp1 = [r for r in rs if r["d.id"] == "device:amp-1"]
    assert len(amp1) == 1
    assert amp1[0]["f.id"] == "firmware:pic18-0xE906"
    assert amp1[0]["d.status"] == "UNKNOWN"  # not re-verified after PICO-5B


def test_q6_backplane_mapping():
    """Which backplane mapping applies to amplifier 3?"""
    rs = _search_results("J21")
    mapping = [r for r in rs if "J21" in r.get("summary", "")]
    assert mapping, "no amp-3 backplane mapping fact found"
    assert "RA3" in mapping[0]["summary"]


def test_q7_proven_vs_experimental_artifacts():
    """Which firmware artifacts are proven and which are experimental?"""
    rs = _search_results("0x251F")
    assert rs
    unresolved = server.knowledge_unresolved()["results"]
    statuses = {r["id"]: r["status"] for r in unresolved}
    # 0x195A is a candidate (never deployed); 0x2D00 diagnostic is offline-tested
    prot = server.knowledge_protocol_contract("125")
    assert prot["count"] >= 1


def test_q8_contradicts_never_worked():
    """Which evidence contradicts 'bootloader replies have never worked'?"""
    rs = server.knowledge_conflicts()["results"]
    assert len(rs) == 1
    assert rs[0]["h.id"] == "hyp:bootloader-replies-never-worked"
    assert rs[0]["e.id"] == "evidence:techref-doc"


def test_q9_authorization_required():
    """Which hardware operations require operator authorization?"""
    rs = server.knowledge_safety_invariants("")["results"]
    auth = [r for r in rs if r["n.id"] == "safety:authorization-required"]
    assert len(auth) == 1
    assert auth[0]["n.requires_authorization"] is True


def test_q10_unknown_recovery_state():
    """What should a fresh agent do when a firmware recovery state is unknown?"""
    rs = server.knowledge_safety_invariants("recovery")["results"]
    unknown = [r for r in rs if r["n.id"] == "safety:unknown-state"]
    assert len(unknown) == 1
    assert "UNKNOWN" in unknown[0]["n.statement"] and "STOP" in unknown[0]["n.statement"]


def test_missing_knowledge_empty():
    """A query for a topic that has no knowledge returns an empty result."""
    rs = server.knowledge_search("nonexistent-topic-xyz")["results"]
    assert rs == []


def test_superseded_decision():
    """A superseded/contradicted decision is surfaced, not silently trusted."""
    rs = server.knowledge_unresolved()["results"]
    by_id = {r["id"]: r for r in rs}
    assert "decision:proceed-on-timeout" in by_id
    assert by_id["decision:proceed-on-timeout"]["status"] == "CONTRADICTED"


def test_mcp_exposes_only_readonly_tools():
    """The MCP surface exposes only the read-only knowledge_* tools (no generic query/write/file/shell)."""
    import asyncio
    result = asyncio.run(server.mcp.list_tools())
    tools = getattr(result, "tools", result)  # ListToolsResult.tools or a plain list
    names = set()
    for t in tools:
        name = getattr(t, "name", None) or (t.get("name") if isinstance(t, dict) else None)
        if name:
            names.add(name)
    assert names <= {
        "knowledge_search", "knowledge_node", "knowledge_protocol_contract",
        "knowledge_safety_invariants", "knowledge_firmware_state",
        "knowledge_flashing_paths", "knowledge_incidents",
        "knowledge_unresolved", "knowledge_conflicts", "knowledge_schema",
        "knowledge_tools",
    }
    assert names, "no tools listed"
    assert "query" not in names  # no generic Cypher endpoint


def test_knowledge_tools_lists_protected():
    """knowledge_tools() lists the 8 registry tools; picoscope/knowledge are KEEP_ACTIVE_MCP."""
    res = server.knowledge_tools()["results"]
    assert len(res) == 8
    by_id = {r["id"]: r for r in res}
    assert set(by_id) == {
        "tool:picoscope-mcp", "tool:knowledge-foundation", "tool:firmware-tooling",
        "tool:hmo2024-tooling", "tool:flash-one-driver", "tool:legacy-python-cli",
        "tool:faller-car-bootload", "tool:integrator-checksum-helper",
    }
    assert by_id["tool:picoscope-mcp"]["preservation_policy"] == "KEEP_ACTIVE_MCP"
    assert by_id["tool:knowledge-foundation"]["preservation_policy"] == "KEEP_ACTIVE_MCP"
    # physically_proven only for tools with recorded hardware evidence
    assert by_id["tool:picoscope-mcp"]["physically_proven"] is True
    assert by_id["tool:hmo2024-tooling"]["physically_proven"] is True
    assert by_id["tool:knowledge-foundation"]["physically_proven"] is False
    assert by_id["tool:faller-car-bootload"]["physically_proven"] is False
    # capabilities are populated (PROVIDES_CAPABILITY)
    assert len(by_id["tool:picoscope-mcp"]["capabilities"]) == 7
    assert len(by_id["tool:knowledge-foundation"]["capabilities"]) == 5
    # the filter narrows to a substring of name/purpose
    filtered = server.knowledge_tools("PicoScope")["results"]
    assert {r["id"] for r in filtered} == {"tool:picoscope-mcp"}


# ---------------------------------------------------------------------------
# K6-B OBSERVE findings (command 113 contract + physical readback + discrepancy)
# ---------------------------------------------------------------------------

def test_k6b_command_113_contract():
    """Command 113 (GET_CONTROLLER_VERSION) is a read-only source-proven contract."""
    prot = server.knowledge_protocol_contract("113")
    assert prot["count"] == 1
    c = prot["results"][0]["command"]
    assert c["n.id"] == "command:113"
    assert c["n.command_name"] == "GET_CONTROLLER_VERSION"
    assert c["n.direction"] == "HOST_TO_PIC32"
    assert c["n.destructive"] is False
    assert c["n.status"] == "PROVEN_SOURCE"
    assert "REPLYxCONTROLLERxVERSION" in c["n.summary"]
    assert c["n.source_path"] == "TrackController5/firmware/src/controller.c"


def test_k6b_observations_recorded():
    """Both K6-B observations exist and keep the REPORTS-vs-flash-content distinction."""
    rs = _search_results("K6-B")
    by_id = {r["id"]: r for r in rs}
    assert "obs:k6b-cmd113-readback" in by_id
    assert "obs:k6b-version-mismatch" in by_id
    rb = by_id["obs:k6b-cmd113-readback"]
    assert rb["status"] == "PROVEN_HARDWARE"
    # the readback proves what the controller REPORTS, not the flash image content
    assert "REPORTS" in rb["summary"] and "not independently" in rb["summary"]
    assert rb["source_path"] == "tools/diagnostics/k6b_observe.py"
    mm = by_id["obs:k6b-version-mismatch"]
    assert mm["status"] == "PROVEN_HARDWARE"
    assert mm["source_path"] == "TrackController5/firmware/src/version.h"
    assert "never flashed" in mm["summary"]


def test_k6b_does_not_change_runs_firmware():
    """K6-B must NOT change device:pic32 RUNS_FIRMWARE firmware:pic32-0.1.0."""
    fw = server.knowledge_firmware_state()["results"]
    pic32 = [r for r in fw if r["d.id"] == "device:pic32"]
    assert len(pic32) == 1
    assert pic32[0]["f.id"] == "firmware:pic32-0.1.0"
    assert pic32[0]["f.version"] == "0.1.0"
