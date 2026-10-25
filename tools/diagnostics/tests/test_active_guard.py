"""Offline tests for the K8B active-session guard (allowlist + shutdown class).

Covers ``tools.diagnostics.active_guard``: the combined allowlist, the exact cold
start order, that 108/107/flash are structurally unreachable, and that
``classify_shutdown`` maps (exit-code x stdout x timeout x abort) to the eight
classes including the fail-closed ``unknown``. No sockets, subprocesses, or
hardware.
"""

from __future__ import annotations

import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics import active_guard  # noqa: E402
from tools.diagnostics.active_guard import (  # noqa: E402
    ACK_TIMEOUT,
    ACTIVE_ALLOWLIST,
    ACTIVE_COMMANDS,
    CLEAN_SHUTDOWN,
    COLD_START_ORDER,
    CONTROLLER_UNAVAILABLE,
    INCOMPLETE_INIT,
    PO_INTERVENTION,
    SAFETY_STOP,
    SESSION_CONFLICT,
    SHUTDOWN_CLASSIFICATIONS,
    UNKNOWN,
    classify_shutdown,
)


# --------------------------------------------------------------------------- #
# Allowlist
# --------------------------------------------------------------------------- #

def test_active_allowlist_is_exactly_combined_set():
    assert ACTIVE_ALLOWLIST == {250, 100, 101, 104, 106, 105, 113}


def test_active_commands_are_the_six_init_commands():
    assert ACTIVE_COMMANDS == {250, 100, 101, 104, 106, 105}


def test_cold_start_order_is_exact():
    assert COLD_START_ORDER == (250, 100, 101, 104, 106, 105)


def test_destructive_and_flash_commands_are_unreachable():
    blocked = [108, 107]
    blocked += list(range(120, 131))   # flash/reset family
    blocked += list(range(200, 244))   # flash/recovery family
    for cmd in blocked:
        assert cmd not in ACTIVE_ALLOWLIST, cmd
    # 113 is the only read-only addition; 108/107 are never present.
    assert 108 not in ACTIVE_COMMANDS
    assert 107 not in ACTIVE_COMMANDS


def test_shutdown_classifications_are_exactly_eight():
    assert set(SHUTDOWN_CLASSIFICATIONS) == {
        CLEAN_SHUTDOWN,
        ACK_TIMEOUT,
        CONTROLLER_UNAVAILABLE,
        SESSION_CONFLICT,
        INCOMPLETE_INIT,
        SAFETY_STOP,
        PO_INTERVENTION,
        UNKNOWN,
    }


# --------------------------------------------------------------------------- #
# classify_shutdown
# --------------------------------------------------------------------------- #

def test_aborted_maps_to_safety_stop():
    assert classify_shutdown(None, "", timed_out=False, aborted=True) == SAFETY_STOP


def test_session_conflict_marker_maps_to_session_conflict():
    assert classify_shutdown(3, '{"type":"sessionConflict"}', False, False) == SESSION_CONFLICT


def test_init_failure_marker_maps_to_ack_timeout():
    assert classify_shutdown(4, '{"type":"initFailure","command":105}', False, False) == ACK_TIMEOUT


def test_controller_unavailable_marker_maps():
    assert classify_shutdown(1, 'controllerUnavailable', False, False) == CONTROLLER_UNAVAILABLE


def test_po_intervention_marker_maps():
    assert classify_shutdown(1, 'poIntervention', False, False) == PO_INTERVENTION


def test_timed_out_maps_to_incomplete_init():
    assert classify_shutdown(None, "", timed_out=True, aborted=False) == INCOMPLETE_INIT


def test_exit_zero_with_summary_maps_to_clean_shutdown():
    assert classify_shutdown(0, '{"type":"sessionSummary"}', False, False) == CLEAN_SHUTDOWN


def test_exit_zero_without_summary_is_unknown():
    # exit 0 but no summary marker -> fail closed, never promoted to success.
    assert classify_shutdown(0, "", False, False) == UNKNOWN


def test_nonzero_without_marker_is_unknown():
    assert classify_shutdown(1, "", False, False) == UNKNOWN


def test_non_string_stdout_is_treated_as_empty():
    assert classify_shutdown(0, None, False, False) == UNKNOWN
