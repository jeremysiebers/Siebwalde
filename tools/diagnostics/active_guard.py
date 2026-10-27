"""Siebwalde K8B active-session guard — allowlist + shutdown classification.

The *active* half of the diagnostics surface (B2/B3/B5) runs the validated cold
start through the proven C# ``SiebwaldeApp.Koploper.Validation.Host`` as a
bounded child process. This module is the executable vocabulary for that surface
and for the combined active allowlist. It contains NO socket and NO subprocess:
it only declares the command sets and classifies a terminated session.

Command allowlist (a cross-check, not the enforcement):

* ``ACTIVE_COMMANDS`` = the six telemetry-enablement commands the C# host may
  ever send (250→100→101→104→106→105).
* ``ACTIVE_ALLOWLIST`` = those six plus the read-only command 113, mirroring the
  combined reachable set across the C# host and the Python 113 transport.
* 108 (EXEC_MBUS_SLAVE_DATA_EXCH — the only HR0/PWM/movement write vector) and
  every flash/recovery command (120..130, 200..243) are structurally
  unreachable and are never present in the read-only sets.
* 107 (RESET) is part of the state-changing ``APPLY_CONFIG_COMMANDS`` set ONLY
  (the apply-config init order includes it); it is NEVER present in
  ``ACTIVE_COMMANDS``/``ACTIVE_ALLOWLIST``.

Shutdown classification (fail-closed) maps a terminated session to exactly one
of :data:`SHUTDOWN_CLASSIFICATIONS`; ``unknown``, ``ack_timeout`` and
``incomplete_init`` are never promoted to success.
"""

from __future__ import annotations

# --------------------------------------------------------------------------- #
# Active command allowlist
# --------------------------------------------------------------------------- #

# The six telemetry-enablement commands, in the mandated cold-start order
# (mirrors SiebwaldeApp.Core/Koploper/Telemetry/KoploperSessionCommandAllowlist.cs
# ``ColdStartOrder``).
ACTIVE_COMMANDS = frozenset({250, 100, 101, 104, 106, 105})

# Combined reachable set: the six state-changing commands + the read-only 113.
ACTIVE_ALLOWLIST = ACTIVE_COMMANDS | {113}

# The state-changing safe CONFIG/APPLY command set (mode ``apply_config``): the seven
# init commands (including 107 RESET) + HOST_HEARTBEAT (110) + EXEC_MBUS_SLAVE_DATA_EXCH
# (108, the only register write vector the C# apply-config host reaches for CONFIG/APPLY).
# 107 is part of THIS set only — it stays excluded from the read-only ACTIVE_COMMANDS /
# ACTIVE_ALLOWLIST above. Every flash/recovery command (120..130, 200..243) remains
# structurally unreachable.
APPLY_CONFIG_COMMANDS = frozenset({250, 107, 100, 106, 101, 104, 105, 108, 110})

COLD_START_ORDER = (250, 100, 101, 104, 106, 105)

# The apply-config init order (deployed PIC32): 250 CONNECT -> 107 RESET -> 100 SLAVES_ON
# -> 106 DATA_UPLOAD -> 101 DETECT -> 104 INIT -> 105 ENABLE.
APPLY_CONFIG_ORDER = (250, 107, 100, 106, 101, 104, 105)

# Structurally unreachable commands from the READ-ONLY active allowlist.
BLOCKED_DESTRUCTIVE = (108, 107)

# Stable display names for the reachable commands (used by the orchestrator to
# label the init sequence without decoding anything on the wire).
COMMAND_NAMES = {
    250: "CLIENT_CONNECTION_REQUEST",
    107: "EXEC_MBUS_STATE_RESET",
    100: "EXEC_MBUS_STATE_SLAVES_ON",
    106: "EXEC_MBUS_STATE_START_DATA_UPLOAD",
    101: "EXEC_MBUS_STATE_SLAVE_DETECT",
    104: "EXEC_MBUS_STATE_SLAVE_INIT",
    105: "EXEC_MBUS_STATE_SLAVE_ENABLE",
    113: "GET_CONTROLLER_VERSION",
}

# --------------------------------------------------------------------------- #
# Shutdown classification
# --------------------------------------------------------------------------- #

CLEAN_SHUTDOWN = "clean_shutdown"
ACK_TIMEOUT = "ack_timeout"
CONTROLLER_UNAVAILABLE = "controller_unavailable"
SESSION_CONFLICT = "session_conflict"
INCOMPLETE_INIT = "incomplete_init"
SAFETY_STOP = "safety_stop"
PO_INTERVENTION = "po_intervention"
UNKNOWN = "unknown"

SHUTDOWN_CLASSIFICATIONS = (
    CLEAN_SHUTDOWN,
    ACK_TIMEOUT,
    CONTROLLER_UNAVAILABLE,
    SESSION_CONFLICT,
    INCOMPLETE_INIT,
    SAFETY_STOP,
    PO_INTERVENTION,
    UNKNOWN,
)


def classify_shutdown(exit_code, stdout, timed_out: bool = False, aborted: bool = False) -> str:
    """Classify a terminated session into exactly one shutdown class.

    Precedence (first match wins, fail-closed):

    1. ``aborted``            → ``safety_stop`` (explicit abort / kill)
    2. ``sessionConflict``    → ``session_conflict`` (bind/ownership failure)
    3. ``initFailure``        → ``ack_timeout`` (DONE ack timed out)
    4. ``controllerUnavailable`` → ``controller_unavailable``
    5. ``poIntervention``     → ``po_intervention``
    6. ``timed_out``          → ``incomplete_init``
    7. ``exit_code == 0`` and ``sessionSummary`` present → ``clean_shutdown``
    8. otherwise             → ``unknown``

    ``unknown`` is never promoted to success.
    """
    text = stdout if isinstance(stdout, str) else ""
    if aborted:
        return SAFETY_STOP
    if "sessionConflict" in text:
        return SESSION_CONFLICT
    if "initFailure" in text:
        return ACK_TIMEOUT
    if "controllerUnavailable" in text:
        return CONTROLLER_UNAVAILABLE
    if "poIntervention" in text:
        return PO_INTERVENTION
    if timed_out:
        return INCOMPLETE_INIT
    if exit_code == 0 and "sessionSummary" in text:
        return CLEAN_SHUTDOWN
    return UNKNOWN


def classify_apply_config_shutdown(exit_code, stdout, timed_out: bool = False, aborted: bool = False) -> str:
    """Classify a terminated apply-config session into exactly one shutdown class.

    Mirrors :func:`classify_shutdown` but recognises the C# apply-config host's
    ``applyConfigSummary`` marker as the clean-completion signal (present on both a
    PASS and a FAILED config outcome — the exit code carries PASS(0)/FAILED(1), and
    the operation status is read from the summary's ``status`` field, NOT from this
    classification). ``unknown``/``ack_timeout``/``incomplete_init`` are never
    promoted to success.
    """
    text = stdout if isinstance(stdout, str) else ""
    if aborted:
        return SAFETY_STOP
    if "sessionConflict" in text:
        return SESSION_CONFLICT
    if "initFailure" in text:
        return ACK_TIMEOUT
    if "controllerUnavailable" in text:
        return CONTROLLER_UNAVAILABLE
    if "poIntervention" in text:
        return PO_INTERVENTION
    if timed_out:
        return INCOMPLETE_INIT
    if "applyConfigSummary" in text:
        return CLEAN_SHUTDOWN
    return UNKNOWN
