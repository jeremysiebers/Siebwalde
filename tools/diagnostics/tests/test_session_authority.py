"""Offline unit tests for the K8B durable single-use grant authority.

Covers the grant model + validation + atomic consume contract of
``tools.diagnostics.session_authority``: valid grants pass; missing / expired /
consumed / wrong-endpoint / wrong-mode / wrong-commands are rejected; atomic
consume (two concurrent consumers -> exactly one wins); the ``.consumed`` rename
side-effect. No sockets, subprocesses, or hardware.
"""

from __future__ import annotations

import sys
import threading
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics.session_authority import (  # noqa: E402
    MODE_READ_ONLY,
    MODE_SESSION,
    Grant,
    GrantAbsent,
    GrantConsumed,
    GrantDirectory,
    GrantExpired,
    GrantScopeMismatch,
    consume_grant,
    load_grant,
    validate_grant,
)

ENDPOINT = "192.168.1.193"
SESSION_COMMANDS = [250, 100, 101, 104, 106, 105]


def _session_grant(grant_id="K8B-B3-2026-10-25", endpoint=ENDPOINT, expires="2999-01-01T00:00:00Z"):
    return Grant(
        grant_id=grant_id,
        purpose="K8B B3 physical register readback",
        target_endpoint=endpoint,
        allowed_commands=frozenset({250, 100, 101, 104, 106, 105}),
        allowed_modes=frozenset({MODE_SESSION}),
        issued_by="PO + Project Lead",
        issued_at="2026-10-25T00:00:00Z",
        expires_at=expires,
        max_uses=1,
        revision="a7df6d26",
    )


def _readonly_grant(grant_id="K8B-B4-2026-10-25", endpoint=ENDPOINT):
    return Grant(
        grant_id=grant_id,
        purpose="K8B B4 controller identity",
        target_endpoint=endpoint,
        allowed_commands=frozenset({113}),
        allowed_modes=frozenset({MODE_READ_ONLY}),
        issued_by="PO + Project Lead",
        issued_at="2026-10-25T00:00:00Z",
        expires_at="2999-01-01T00:00:00Z",
        max_uses=1,
        revision="a7df6d26",
    )


# --------------------------------------------------------------------------- #
# Load + validate
# --------------------------------------------------------------------------- #

def test_load_and_validate_valid_session_grant(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    g = load_grant("K8B-B3-2026-10-25", d)
    assert isinstance(g, Grant)
    assert validate_grant(g, MODE_SESSION, ENDPOINT, SESSION_COMMANDS) is g


def test_load_missing_grant_raises_absent(tmp_path):
    d = GrantDirectory(tmp_path)
    with pytest.raises(GrantAbsent):
        load_grant("does-not-exist", d)


def test_load_rejects_unsafe_grant_id(tmp_path):
    d = GrantDirectory(tmp_path)
    with pytest.raises(GrantAbsent):
        load_grant("../etc/passwd", d)


def test_grant_round_trip(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    g = load_grant("K8B-B3-2026-10-25", d)
    assert g.to_dict()["allowed_commands"] == sorted({250, 100, 101, 104, 106, 105})
    assert g.allowed_modes == frozenset({MODE_SESSION})


# --------------------------------------------------------------------------- #
# Validation failures (fail-closed)
# --------------------------------------------------------------------------- #

def test_expired_grant_raises(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant(expires="2000-01-01T00:00:00Z"))
    g = load_grant("K8B-B3-2026-10-25", d)
    with pytest.raises(GrantExpired):
        validate_grant(g, MODE_SESSION, ENDPOINT, [250])


def test_missing_expiry_raises(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant(expires=""))
    g = load_grant("K8B-B3-2026-10-25", d)
    with pytest.raises(GrantExpired):
        validate_grant(g, MODE_SESSION, ENDPOINT, [250])


def test_wrong_endpoint_raises(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    g = load_grant("K8B-B3-2026-10-25", d)
    with pytest.raises(GrantScopeMismatch):
        validate_grant(g, MODE_SESSION, "192.168.1.100", [250])


def test_wrong_mode_raises(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    g = load_grant("K8B-B3-2026-10-25", d)
    with pytest.raises(GrantScopeMismatch):
        validate_grant(g, MODE_READ_ONLY, ENDPOINT, [113])


def test_wrong_commands_raise(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    g = load_grant("K8B-B3-2026-10-25", d)
    # 113 is not in the session grant's allowed_commands.
    with pytest.raises(GrantScopeMismatch):
        validate_grant(g, MODE_SESSION, ENDPOINT, [113])
    # A superset that includes an out-of-scope command is rejected too.
    with pytest.raises(GrantScopeMismatch):
        validate_grant(g, MODE_SESSION, ENDPOINT, [250, 100, 101, 104, 106, 105, 108])


def test_validate_rejects_non_grant():
    with pytest.raises(GrantScopeMismatch):
        validate_grant("not-a-grant", MODE_SESSION, ENDPOINT, [250])


# --------------------------------------------------------------------------- #
# Atomic consume
# --------------------------------------------------------------------------- #

def test_consume_renames_to_consumed(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    g = load_grant("K8B-B3-2026-10-25", d)
    consume_grant(g, d)
    assert d.is_consumed("K8B-B3-2026-10-25")
    assert not d.exists("K8B-B3-2026-10-25")
    # A second consume is rejected.
    with pytest.raises(GrantConsumed):
        consume_grant(g, d)
    # And load now reports it as consumed.
    with pytest.raises(GrantConsumed):
        load_grant("K8B-B3-2026-10-25", d)


def test_consume_missing_grant_raises_absent(tmp_path):
    d = GrantDirectory(tmp_path)
    with pytest.raises(GrantAbsent):
        consume_grant(_session_grant(), d)


def test_concurrent_consume_exactly_one_wins(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    g = load_grant("K8B-B3-2026-10-25", d)

    barrier = threading.Barrier(32)
    outcomes = []
    lock = threading.Lock()

    def worker():
        barrier.wait()
        try:
            consume_grant(g, d)
            outcome = "ok"
        except (GrantConsumed, GrantAbsent):
            outcome = "blocked"
        with lock:
            outcomes.append(outcome)

    threads = [threading.Thread(target=worker) for _ in range(32)]
    for th in threads:
        th.start()
    for th in threads:
        th.join()

    assert outcomes.count("ok") == 1
    assert outcomes.count("blocked") == 31


def test_consume_is_single_use_across_instances(tmp_path):
    d = GrantDirectory(tmp_path)
    d.write(_session_grant())
    g1 = load_grant("K8B-B3-2026-10-25", d)
    consume_grant(g1, d)
    # A fresh GrantDirectory over the same root sees the same consumed artifact.
    d2 = GrantDirectory(tmp_path)
    with pytest.raises(GrantConsumed):
        load_grant("K8B-B3-2026-10-25", d2)
