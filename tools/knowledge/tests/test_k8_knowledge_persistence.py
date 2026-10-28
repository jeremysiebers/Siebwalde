"""K8 knowledge-persistence tests for the Knowledge Foundation.

Verify the five provenance-aware K8 records (firewall observation, command
250-timeout / 113-success observation, the firewall hypothesis, the
protocol-mismatch alternative hypothesis, and the next-validation observation)
plus the minimal provenance-linkage schema extension. Also verify the K6/K7/K8
records are preserved (219 nodes / 170 relationships) and that the two competing
hypotheses have been resolved (firewall CONFIRMED / protocol-mismatch CONTRADICTED).

All assertions go through the read-only Knowledge MCP surface (or a fresh
read-only connection), so these tests double as an MCP-retrievability check.
"""

from __future__ import annotations

import os
import sys
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]          # repository root
_KNOWLEDGE = _REPO / "tools" / "knowledge"

sys.path.insert(0, str(_REPO))
os.environ.setdefault("LBUG_C_API_LIB_PATH", str(_KNOWLEDGE / "lib" / "lbug_shared.dll"))
os.environ["PATH"] = str(_KNOWLEDGE / "lib") + os.pathsep + os.environ.get("PATH", "")
os.environ.setdefault("SIEBWALDE_KNOWLEDGE_DB", str(_KNOWLEDGE / "db" / "siebwalde-knowledge.lbdb"))

from tools.knowledge import knowledge_seed, schema  # noqa: E402
from tools.knowledge.ingest import build_graph  # noqa: E402
from tools.knowledge.mcp_server import server  # noqa: E402


@pytest.fixture(scope="session", autouse=True)
def _built_db():
    build_graph.main()
    yield


def _props(node_id: str) -> dict:
    return server.knowledge_node(node_id)["node"]["properties"]


def _rels(node_id: str) -> list[dict]:
    return server.knowledge_node(node_id)["relationships"]


# ---------------------------------------------------------------------------
# Schema extension (justified: no existing rel type links Hypothesis->Observation
# or Observation->ProtocolCommand/FirmwareArtifact/Observation).
# ---------------------------------------------------------------------------

def test_schema_provenance_linkage_rel_tables():
    assert set(schema.REL_TABLES) >= {
        "SUPPORTED_BY", "OBSERVES_COMMAND", "OBSERVES_FIRMWARE", "RELATED_TO",
    }
    assert schema.REL_TABLES["SUPPORTED_BY"][:2] == ("Hypothesis", "Observation")
    assert schema.REL_TABLES["OBSERVES_COMMAND"][:2] == ("Observation", "ProtocolCommand")
    assert schema.REL_TABLES["OBSERVES_FIRMWARE"][:2] == ("Observation", "FirmwareArtifact")
    assert schema.REL_TABLES["RELATED_TO"][:2] == ("Observation", "Observation")


# ---------------------------------------------------------------------------
# The five new records: id, node type, status, source provenance.
# ---------------------------------------------------------------------------

NEW_RECORDS = [
    ("obs:k8-po-firewall-observation", "Observation", "UNKNOWN",
     "docs/K8-FIREWALL-CORRECTIVE.md", "UNVERIFIED", "61c31f48"),
    ("obs:k8-cmd250-timeout-cmd113-success", "Observation", "PROVEN_HARDWARE",
     "docs/K8-FIREWALL-CORRECTIVE.md", "OBSERVED", "61c31f48"),
    ("hyp:k8-firewall-blocked-udp", "Hypothesis", "PROVEN_HARDWARE",
     "docs/K8B-COMMUNICATION-RECOVERY.md", "OBSERVED", "a49ea004"),
    ("hyp:k8-protocol-mismatch-250", "Hypothesis", "CONTRADICTED",
     "docs/K8B-COMMUNICATION-RECOVERY.md", "OBSERVED", "a49ea004"),
    ("obs:k8-next-validation-firewall", "Observation", "UNKNOWN",
     "docs/K8-FIREWALL-CORRECTIVE.md", "UNVERIFIED", "61c31f48"),
]


@pytest.mark.parametrize(
    "node_id, node_type, status, source_path, evidence_status, source_revision",
    NEW_RECORDS
)
def test_new_k8_record_provenance(node_id, node_type, status, source_path, evidence_status, source_revision):
    node = server.knowledge_node(node_id)["node"]
    assert node["type"] == node_type
    props = node["properties"]
    assert props["n.id"] == node_id
    assert props["n.status"] == status
    assert props["n.source_path"] == source_path
    assert props["n.source_revision"] == source_revision
    assert props["n.evidence_status"] == evidence_status


def test_k8_firewall_observation_unverified():
    p = _props("obs:k8-po-firewall-observation")
    assert p["n.status"] == "UNKNOWN"
    assert p["n.method"] == "PO operator report"
    assert "2-3" in p["n.what"]
    # no invented executable names or rule details
    assert ".exe" not in p["n.what"].lower()


def test_k8_cmd250_timeout_cmd113_success_preserves_distinction():
    p = _props("obs:k8-cmd250-timeout-cmd113-success")
    assert p["n.status"] == "PROVEN_HARDWARE"
    assert "250" in p["n.what"] and "timed out" in p["n.what"]
    assert "113" in p["n.what"]
    assert "byte-identical" in p["n.what"]
    # observed-response vs inferred-cause distinction is explicit
    assert "OBSERVED-RESPONSE" in p["n.what"] or "REPORTS" in p["n.what"]
    assert p["n.evidence_ref"] == "Logging/K6B/k6b-observe-20261024T092944Z.json"


def test_k8_firewall_hypothesis_supports_and_missing():
    p = _props("hyp:k8-firewall-blocked-udp")
    assert p["n.status"] == "PROVEN_HARDWARE"
    assert "Windows Firewall" in p["n.hypothesis"]
    assert p["n.evidence_for"]  # supporting evidence recorded
    assert p["n.evidence_against"]  # missing verification recorded


def test_k8_protocol_mismatch_alternative_hypothesis():
    p = _props("hyp:k8-protocol-mismatch-250")
    assert p["n.status"] == "CONTRADICTED"
    assert "ALTERNATIVE" in p["n.hypothesis"] or "ALTERNATIVE" in p["n.summary"]
    assert "0.1.0" in p["n.hypothesis"] or "0.1.0" in p["n.summary"]


def test_k8_next_validation_eight_steps():
    p = _props("obs:k8-next-validation-firewall")
    assert p["n.status"] == "UNKNOWN"
    for step in ("identify", "firewall rules", "UDP ports", "socket binding",
                 "113 vs 250", "packet", "correction", "LIVE_HARDWARE"):
        assert step.lower() in p["n.what"].lower()


# ---------------------------------------------------------------------------
# Competing hypotheses remain distinct (never confirmed/promoted).
# ---------------------------------------------------------------------------

def test_competing_hypotheses_remain_distinct_resolved():
    fw = _props("hyp:k8-firewall-blocked-udp")
    pm = _props("hyp:k8-protocol-mismatch-250")
    assert fw["n.id"] != pm["n.id"]
    # K8B resolution: the firewall hypothesis was CONFIRMED, the protocol-mismatch
    # alternative was CONTRADICTED by the fresh 0x251F readback (250 ACKed 1/1).
    assert fw["n.status"] == "PROVEN_HARDWARE"
    assert pm["n.status"] == "CONTRADICTED"
    # the CONFIRMED firewall hypothesis is no longer unresolved;
    # the CONTRADICTED protocol-mismatch remains in the not-proven lens
    unresolved_ids = {r["id"] for r in server.knowledge_unresolved()["results"]}
    assert fw["n.id"] not in unresolved_ids
    assert pm["n.id"] in unresolved_ids


# ---------------------------------------------------------------------------
# Relationships (provenance linkage).
# ---------------------------------------------------------------------------

def test_cmd250_113_observation_links_to_command_firmware_and_k8_finding():
    rels = {(r["rel"], r["from"], r["to"]) for r in _rels("obs:k8-cmd250-timeout-cmd113-success")}
    assert ("OBSERVES_COMMAND", "obs:k8-cmd250-timeout-cmd113-success", "command:113") in rels
    assert ("OBSERVES_FIRMWARE", "obs:k8-cmd250-timeout-cmd113-success", "firmware:pic32-0.1.0") in rels
    assert ("RELATED_TO", "obs:k8-cmd250-timeout-cmd113-success", "obs:k8-slaveryinfo-passive-blocked") in rels


def test_hypotheses_supported_by_observations():
    fw_rels = {(r["rel"], r["from"], r["to"]) for r in _rels("hyp:k8-firewall-blocked-udp")}
    assert ("SUPPORTED_BY", "hyp:k8-firewall-blocked-udp", "obs:k8-po-firewall-observation") in fw_rels
    assert ("SUPPORTED_BY", "hyp:k8-firewall-blocked-udp", "obs:k8-cmd250-timeout-cmd113-success") in fw_rels

    pm_rels = {(r["rel"], r["from"], r["to"]) for r in _rels("hyp:k8-protocol-mismatch-250")}
    assert ("SUPPORTED_BY", "hyp:k8-protocol-mismatch-250", "obs:k8-cmd250-timeout-cmd113-success") in pm_rels


# ---------------------------------------------------------------------------
# Base preserved: K6/K7/K8 intact, MCP-retrievable.
# ---------------------------------------------------------------------------

def test_base_unchanged_total_219_nodes_170_rels():
    assert len(knowledge_seed.NODES) == 219
    assert len(knowledge_seed.RELS) == 170
    # per-table node/relationship counts in the built graph match the seed exactly
    from tools.knowledge import _runtime
    counts = _runtime.count_tables(_KNOWLEDGE / "db" / "siebwalde-knowledge.lbdb")
    assert sum(counts["nodes"].values()) == 219
    assert sum(counts["rels"].values()) == 170


def test_existing_k6_k7_k8_records_preserved():
    preserved = {
        "obs:k6b-cmd113-readback": "PROVEN_HARDWARE",
        "obs:k6b-version-mismatch": "PROVEN_HARDWARE",
        "obs:k7-pic32-identity-transition": "PROVEN_HARDWARE",
        "obs:k7-cmd124-ineligible": "PROVEN_SOURCE",
        "obs:k7-next-readonly-hr11-slaveryinfo": "PROVEN_SOURCE",
        "obs:k8-slaveryinfo-passive-blocked": "PROVEN_SOURCE",
    }
    for node_id, expected_status in preserved.items():
        p = _props(node_id)
        assert p["n.id"] == node_id
        assert p["n.status"] == expected_status


def test_k8_records_mcp_retrievable_via_search():
    rs = server.knowledge_search("firewall")["results"]
    ids = {r["id"] for r in rs}
    # labels/summaries use both "Firewall" (proper noun) and "firewall" (generic);
    # CONTAINS is case-sensitive, so also search the capitalised form.
    ids |= {r["id"] for r in server.knowledge_search("Firewall")["results"]}
    assert "obs:k8-po-firewall-observation" in ids
    assert "hyp:k8-firewall-blocked-udp" in ids
    assert "obs:k8-next-validation-firewall" in ids
    rs2 = server.knowledge_search("command-250")["results"]
    ids2 = {r["id"] for r in rs2}
    assert "obs:k8-cmd250-timeout-cmd113-success" in ids2 or "hyp:k8-protocol-mismatch-250" in ids2
