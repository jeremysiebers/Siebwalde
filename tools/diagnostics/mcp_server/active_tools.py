"""Active/live Diagnostics MCP tools (K8B) — thin, grant-gated wrappers.

Each tool is a thin wrapper over :mod:`tools.diagnostics.active_session` with
input guards mirroring ``server.py``. Every ACTIVE/LIVE tool independently
validates a scoped grant, validates ownership, checks the allowlist, and only
then performs I/O. With no valid grant, every tool returns ``authorization_absent``
and performs NO I/O (no socket, no subprocess, no filesystem write beyond
checking for a grant file).

The four tools are registered on the shared FastMCP instance by
:func:`register`, called from ``server.py``. The functions themselves remain
plain callables so the offline test suite can invoke them directly.
"""

from __future__ import annotations

from .. import active_guard, active_session, observe

# Effect classes follow the existing vocabulary in ``observe.py``.
ACTIVE_TOOL_NAMES = (
    "diagnostics_controller_preflight",
    "diagnostics_probe_controller_version",
    "diagnostics_run_controller_session",
    "diagnostics_apply_config",
    "diagnostics_abort_active_session",
)

ACTIVE_EFFECT_CLASSES = {
    "diagnostics_controller_preflight": observe.LIVE_READ_ONLY_HARDWARE_VALIDATED,
    "diagnostics_probe_controller_version": observe.LIVE_READ_ONLY_HARDWARE_VALIDATED,
    "diagnostics_run_controller_session": observe.STATE_CHANGING,
    "diagnostics_apply_config": observe.STATE_CHANGING,
    "diagnostics_abort_active_session": observe.STATE_CHANGING,
}

# Bounded inputs for grant ids / purpose labels.
MAX_TEXT_BYTES = 1024


def _error(message: str) -> dict:
    return {"error": message, "ok": False}


def _guard_text(value, name: str):
    """Required non-empty, bounded string. Returns an error dict or None."""
    if value is None:
        return _error("%s: missing input" % name)
    if not isinstance(value, str):
        return _error("%s: expected a string, got %s" % (name, type(value).__name__))
    if not value.strip():
        return _error("%s: empty input" % name)
    if len(value.encode("utf-8", "surrogatepass")) > MAX_TEXT_BYTES:
        return _error("%s: input exceeds %d bytes" % (name, MAX_TEXT_BYTES))
    return None


def _guard_optional_text(value, name: str):
    """Optional (None or bounded non-empty string). Returns an error dict or None."""
    if value is None:
        return None
    if not isinstance(value, str):
        return _error("%s: expected a string or null, got %s" % (name, type(value).__name__))
    if not value.strip():
        return _error("%s: empty input" % name)
    if len(value.encode("utf-8", "surrogatepass")) > MAX_TEXT_BYTES:
        return _error("%s: input exceeds %d bytes" % (name, MAX_TEXT_BYTES))
    return None


def _guard_bool(value, name: str):
    if not isinstance(value, bool):
        return _error("%s: expected a bool, got %s" % (name, type(value).__name__))
    return None


def _guard_grant_id(value, name: str):
    """A grant id is None (no grant) or a bounded string. Returns error or None.

    ``None`` / empty string is "no grant present" and flows through to the
    orchestrator, which returns ``authorization_absent`` with no I/O (rather than
    a generic input error). A non-string type is a malformed input and errors.
    """
    if value is None:
        return None
    if not isinstance(value, str):
        return _error("%s: expected a string or null, got %s" % (name, type(value).__name__))
    if len(value.encode("utf-8", "surrogatepass")) > MAX_TEXT_BYTES:
        return _error("%s: input exceeds %d bytes" % (name, MAX_TEXT_BYTES))
    return None


def _guard_optional_int(value, name: str):
    if value is None:
        return None
    if isinstance(value, bool) or not isinstance(value, int):
        return _error("%s: expected an int or null, got %s" % (name, type(value).__name__))
    return None


# --------------------------------------------------------------------------- #
# Tool functions (registered via register(mcp))
# --------------------------------------------------------------------------- #


def diagnostics_controller_preflight(
    target_endpoint=None,
    include_identity_probe: bool = False,
    authorization=None,
) -> dict:
    """B1 — controller preflight: ownership bind-check + optional command-113 probe.

    The ownership bind-check is a host-side socket action with no transmission
    and needs no grant. ``include_identity_probe=true`` sends one command-113
    datagram and therefore requires ``authorization`` (a scoped read-only grant).
    """
    err = _guard_optional_text(target_endpoint, "target_endpoint")
    if err is not None:
        return err
    err = _guard_bool(include_identity_probe, "include_identity_probe")
    if err is not None:
        return err
    err = _guard_grant_id(authorization, "authorization")
    if err is not None:
        return err
    return active_session.controller_preflight(
        include_identity_probe=include_identity_probe,
        grant_id=authorization,
        target_endpoint=target_endpoint,
    )


def diagnostics_probe_controller_version(authorization) -> dict:
    """B4 — read-only command 113: the PIC32 controller identity (not PIC18)."""
    err = _guard_grant_id(authorization, "authorization")
    if err is not None:
        return err
    return active_session.probe_controller_version(authorization)


def diagnostics_run_controller_session(
    authorization,
    purpose,
    wait_ms=None,
    target_endpoint=None,
) -> dict:
    """B2+B3+B5 — the single bounded cold-start init + register readback + stop."""
    err = _guard_grant_id(authorization, "authorization")
    if err is not None:
        return err
    err = _guard_text(purpose, "purpose")
    if err is not None:
        return err
    err = _guard_optional_int(wait_ms, "wait_ms")
    if err is not None:
        return err
    err = _guard_optional_text(target_endpoint, "target_endpoint")
    if err is not None:
        return err
    return active_session.run_controller_session(
        authorization,
        purpose,
        wait_ms=wait_ms if wait_ms is not None else active_session.DEFAULT_WAIT_MS,
        target_endpoint=target_endpoint,
    )


def diagnostics_abort_active_session(session_id=None) -> dict:
    """B5 — explicit safety stop: kill the tracked child process (no flash/reset)."""
    err = _guard_optional_text(session_id, "session_id")
    if err is not None:
        return err
    return active_session.abort_session(session_id)


def diagnostics_apply_config(
    authorization,
    purpose,
    wait_ms=None,
    target_endpoint=None,
) -> dict:
    """Grant-gated safe CONFIG/APPLY diagnostic (STATE_CHANGING).

    Runs the C# host in ``apply-config`` mode as a bounded child: the seven-command
    init (250->107->100->106->101->104->105) + HOST_HEARTBEAT (110) + the re-arm
    (112, raw comm, before 107), per-amplifier identity/compatibility verification,
    the DISABLE safety gate (HR1=0x0400, verified enable=0 + inhibited) BEFORE any
    CONFIG/APPLY, then the echo-verified config download (HR3..6 + APPLY). Never
    writes HR0, never writes ENABLE (0x8000) after DISABLE. Requires a distinct
    scoped ``apply_config`` grant (single-use, consumed before any I/O); the
    combined command set is {250,107,100,106,101,104,105,108,110,112}.
    """
    err = _guard_grant_id(authorization, "authorization")
    if err is not None:
        return err
    err = _guard_text(purpose, "purpose")
    if err is not None:
        return err
    err = _guard_optional_int(wait_ms, "wait_ms")
    if err is not None:
        return err
    err = _guard_optional_text(target_endpoint, "target_endpoint")
    if err is not None:
        return err
    return active_session.apply_config_session(
        authorization,
        purpose,
        wait_ms=wait_ms if wait_ms is not None else active_session.DEFAULT_WAIT_MS,
        target_endpoint=target_endpoint,
    )


def register(mcp) -> None:
    """Register the active tools on the shared FastMCP instance."""
    mcp.tool()(diagnostics_controller_preflight)
    mcp.tool()(diagnostics_probe_controller_version)
    mcp.tool()(diagnostics_run_controller_session)
    mcp.tool()(diagnostics_apply_config)
    mcp.tool()(diagnostics_abort_active_session)
