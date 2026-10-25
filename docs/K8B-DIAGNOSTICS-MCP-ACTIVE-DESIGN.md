# K8B — Diagnostics MCP Active-Session Extension (B1..B5) — Design

**Status:** DESIGN ONLY. No implementation is authorized by this document. It is
the Architect's proposed architecture for extending the existing, offline-only
Diagnostics MCP (`tools/diagnostics/mcp_server/server.py`) to support controlled,
authorized live controller sessions. All C# and Python paths referenced below are
verified against the worktree on branch `fix/k8b-legacy-12-register-slaveryinfo`.

---

## 1. Purpose, scope, and non-goals

### 1.1 Purpose

Extend the Diagnostics MCP so that, under an explicit scoped Product Owner /
`LIVE_HARDWARE` grant, it can:

- **B1** — discover/configure the PIC32 endpoint, verify target identity, detect
  whether another application already owns the controller session, and report
  socket/port conflicts. No unrestricted network scanning.
- **B2** — run the validated init sequence (250→100→101→104→106→105), confirm
  readiness + discovered PIC18 amps, expose init state/failures, and terminate
  safely.
- **B3** — read back fresh physical HR7/HR11/HR21/HR22 for amps 1/3/4/6 with raw
  values, identity, physical/logical mapping, timestamp, freshness,
  firmware/protocol support status, and comm-error state. Never report
  stale/unsupported values as confirmed.
- **B4** — read the PIC32 controller identity (command 113), distinct from PIC18
  identity.
- **B5** — explicit safe termination with a classified outcome.

### 1.2 Non-goals

- No second standalone divergent application. This **extends** the existing
  Diagnostics MCP.
- No new permanent independent PIC32 protocol implementation in Python.
- No flash, reset, power-cycle, or recovery path. `FIRMWARE_FLASH` is not in
  scope and is not granted.
- No implicit escalation from observation to controller modification.
- No ambient "active mode" that persists beyond one bounded session.

---

## 2. Verified current state (evidence)

| Fact | Evidence |
| --- | --- |
| Diagnostics MCP is offline/read-only, 9 tools, FastMCP stdio, no socket/fs/subprocess | `tools/diagnostics/mcp_server/server.py` (module docstring + `@mcp.tool()` list) |
| Core never opens a socket | `tools/diagnostics/core.py` docstring; `capabilities()` → `socket_access: False` |
| OBSERVE adapter never opens a socket | `tools/diagnostics/observe.py` docstring; `capabilities()` |
| Proven live path is C#: `SiebwaldeApp.Koploper.Validation.Host` | `ReadOnlyTelemetryValidationHost.EnableSessionAsync` (250→100→101→104→106→105) |
| C# init is layout-aware, decodes deployed legacy 12-register SLAVEINFO | `docs/K8B-COMMUNICATION-RECOVERY.md` §2–§4 (physically validated 2026-10-25) |
| C# cold start physically succeeded; 4 `amplifierIdentityReadback` JSON lines (amps 1/3/4/6, HR11=0x251F) | K8B session result; `Program.BuildReadRegisterLine` |
| Command 113 is implemented only in Python, single-purpose, allowlisted, proven read-only | `tools/diagnostics/live_transport.py` `Command113Transport`; `tools/diagnostics/observe_guard.py` `ALLOWED_COMMANDS = {113}` |
| C# allowlist is exactly {250,100,101,104,106,105}; 108/107/others fail-closed | `SiebwaldeApp.Core/Koploper/Telemetry/KoploperSessionCommandAllowlist.cs` (`IsPermitted`, `ColdStartOrder`) |
| The offline `slaveinfo_decoder.py` decodes **only** the 63-byte Phase-1 layout | `tools/diagnostics/slaveinfo_decoder.py` `FRAME_LENGTH = 63`, footer asserted at `data[-1]` (offset 62) |
| Both layouts are already decoded in the raw capture tool | `tools/diagnostics/cold_start_slaveinfo_capture.py` `classify_frame` (footer 40 → legacy, 62 → Phase-1) |

---

## 3. Recommended architecture

### 3.1 Decision: hybrid reuse — no new protocol stack

The MCP's active tools reach the controller through **two already-proven
mechanisms**, coordinated by a thin Python orchestrator that contains **no
protocol implementation of its own**:

```
Diagnostics MCP (FastMCP stdio)
  ├─ OFFLINE tools (unchanged, no socket)                    [core.py, observe.py]
  │
  └─ ACTIVE / LIVE tools (new)
       └─ tools/diagnostics/active_session.py  (orchestrator: grant, ownership,
          │                                     subprocess lifecycle, parse, audit,
          │                                     fail-closed classification)
          ├─ (state-changing init + SLAVEINFO readback + stop)
          │      └─ subprocess: SiebwaldeApp.Koploper.Validation.Host.exe
          │            readonly-validate --live --read-registers --wait-ms N
          │            (the ONLY layout-aware init + 12-register decode)
          │
          └─ (read-only command 113 — B1 identity / B4)
                 └─ tools/diagnostics/live_transport.Command113Transport
                    + tools/diagnostics.observe_guard.ObserveSession
```

- **State-changing init (B2) and SLAVEINFO register readback (B3) + stop (B5)**
  are delegated to the proven C# `SiebwaldeApp.Koploper.Validation.Host.exe`,
  invoked as a bounded child process under a strict allowlist. This is the only
  layout-aware, physically-validated implementation of the init sequence and the
  legacy 12-register SLAVEINFO decode.
- **Read-only command 113 (B1 identity probe, B4)** is delegated to the existing
  single-purpose Python transport (`live_transport.py` + `observe_guard.py`),
  already K6-B OBSERVE_PASS and K7-hardened. The C# host's
  `KoploperSessionCommandAllowlist` deliberately **excludes** 113 (it is a
  telemetry-enablement host, not a controller-identity host), so adding 113 to
  C# would mean widening its fail-closed guard — which is exactly what we avoid.
- **The Python orchestrator** (`active_session.py`) is **pure lifecycle/safety/
  authorization glue**: it validates and consumes the grant, performs the
  ownership pre-flight, launches/terminates the child, parses its machine-readable
  output, and classifies the outcome. It writes no wire frame, holds no SLAVEINFO
  decode, and runs no init state machine.

### 3.2 Why this avoids a second independent protocol stack

1. No framing code is added to Python. `active_session.py` never constructs a
   PIC32 frame.
2. The init sequence (250/100/101/104/106/105) and the legacy 12-register
   SLAVEINFO decode stay **exclusively** in the C# host (`KoploperSessionCommandGuard`
   + `TrackCommClientAsync`). Python reuses it via the process boundary.
3. Command 113 stays **exclusively** in the existing `Command113Transport`
   (source-mirrored framing in `build_get_controller_version_request()`). Nothing
   new is added to that path.
4. The orchestrator's "allowlist" is a **cross-check**, not the enforcement. The
   authoritative enforcement remains `KoploperSessionCommandAllowlist` (C#) and
   `observe_guard.ALLOWED_COMMANDS = {113}` (Python). This mirrors the documented
   rule (`docs/SIEBWALDE-KNOWLEDGE-AND-DIAGNOSTICS-ARCHITECTURE.md` §10): the core
   enforces safety internally; an MCP permission alone is insufficient.

### 3.3 Failure / isolation model when the C# host is unavailable

- The MCP stays **fully functional in OFFLINE mode**: all 9 existing tools and
  `diagnostics_capabilities` work with no socket, no subprocess, no filesystem.
- `diagnostics_run_controller_session` **fails closed** with
  `controller_unavailable` and a precise reason (`host_exe_missing`,
  `host_build_missing`, `dotnet_missing`) and performs **no I/O**.
- `diagnostics_probe_controller_version` (B4) and the ownership half of B1 are
  independent of the C# host (Python-only) and remain available under their own
  scoped grant.
- There is **no Python fallback** that re-implements the init sequence or
  SLAVEINFO decode. If the C# host is absent, B2/B3/B5 are simply refused. This is
  a deliberate fail-closed choice to avoid silently creating the forbidden second
  stack.

---

## 4. Proposed MCP tool surface (B1..B5)

Naming: active/state-changing tools carry `session` / `run_controller_session`;
live read-only tools carry `probe` / `preflight`. Effect classes follow the
existing vocabulary in `observe.py` (`OFFLINE_ONLY`, `LIVE_READ_ONLY_*`,
`STATE_CHANGING`, `DESTRUCTIVE`, `UNKNOWN_EFFECT`).

The **combined operation allowlist is exactly `{250, 100, 101, 104, 106, 105, 113}`**.
No other command is reachable through any tool (see §7).

### 4.0 `diagnostics_capabilities()` — EXTENDED (READ_ONLY, offline)

Add to the existing returned dict (do not remove any existing key):

```jsonc
{
  // ...existing offline fields (offline_only, hardware_access, ...)...
  "mode": "offline",                       // steady state of a fresh server
  "active_surface_available": true,        // tools exist but are grant-gated
  "active_tools": [
    "diagnostics_controller_preflight",
    "diagnostics_probe_controller_version",
    "diagnostics_run_controller_session",
    "diagnostics_abort_active_session"
  ],
  "active_allowlist": [250, 100, 101, 104, 106, 105, 113],
  "active_effect_classes": {
    "diagnostics_probe_controller_version": "LIVE_READ_ONLY_HARDWARE_VALIDATED",
    "diagnostics_controller_preflight": "LIVE_READ_ONLY_HARDWARE_VALIDATED",
    "diagnostics_run_controller_session": "STATE_CHANGING",
    "diagnostics_abort_active_session": "STATE_CHANGING"
  }
}
```

`mode` is informational; there is **no** tool that flips the server into a
persistent "active" state. A server with no valid grant is, by construction,
offline (see §5).

### 4.1 `diagnostics_controller_preflight(target_endpoint?, include_identity_probe?)` — B1

**Class: LIVE_READ_ONLY (no controller state change).** Requires a scoped
`LIVE_HARDWARE` grant only when `include_identity_probe=true` (the probe sends one
command-113 datagram). The ownership bind-check portion is a host-side socket
action with no transmission.

Parameters:

```jsonc
{
  "target_endpoint": "string | null",        // defaults to the single allowed endpoint
  "include_identity_probe": "bool"           // default false
}
```

Return shape:

```jsonc
{
  "status": "PASS | FAILED | UNKNOWN",
  "configured_endpoint": "192.168.1.193",
  "tx_port": 10000,
  "recv_port": 10001,
  "ownership": {
    "recv_port_bind": "free | in_use",        // EADDRINUSE => in_use
    "session_lock_present": "bool",
    "session_owner_pid": "int | null",
    "verdict": "available | owned_by_other"
  },
  "identity": {                                // only when include_identity_probe
    "probed": true,
    "controller_version": { "major": 0, "minor": 1, "patch": 0,
                            "protocol": 0, "build_timestamp": "..." },
    "physical_execution": "unknown",           // controller *reported*, not flash-proven
    "source_verified": true
  },
  "evidence": { "reason": "string|null" }
}
```

No unrestricted scanning: the endpoint is the single allowlisted literal
(`observe_guard.REQUIRED_ENDPOINT = "192.168.1.193"`), never resolved via DNS,
never enumerated.

### 4.2 `diagnostics_probe_controller_version(authorization)` — B4

**Class: LIVE_READ_ONLY (command 113 only).** Reuses
`Command113Transport.send_get_controller_version()` bound to a consumed
`ObserveSession`.

Parameters:

```jsonc
{ "authorization": "string" }   // grant_id of a valid, unconsumed read-only grant
```

Return shape (mirrors the K6/K7 transport result, §`live_transport.py`):

```jsonc
{
  "status": "PASS | FAILED | UNKNOWN",
  "controller_version": { "major": 0, "minor": 1, "patch": 0,
                          "protocol": 0, "build_timestamp": "..." } | null,
  "evidence": {
    "send_state": "not_attempted | bind_failed | send_failed | sent | received | validated",
    "sent": "bool",
    "request_bytes": "aa71",
    "response_sha256": "...",
    "source_address": "192.168.1.193",
    "authorization": { "status": "ALLOWED", "authorized_command_count_remaining": 0 },
    "controller_reported": "bool",
    "physical_execution": "unknown"
  }
}
```

**B4 is distinct from PIC18 identity (B3)**: this returns the PIC32
`GET_CONTROLLER_VERSION` reply; B3 returns per-PIC18 register readback.

### 4.3 `diagnostics_run_controller_session(authorization, purpose, wait_ms?)` — B2 + B3 + B5

**Class: ACTIVE / STATE_CHANGING.** This is the single bounded operation that runs
the validated cold-start init, reads back fresh registers, and terminates safely —
mirroring the physically-validated `readonly-validate --live --read-registers`
path. It is modeled as **one bounded child-process lifecycle**, not a long-lived
daemon, because the SLAVEINFO upload only flows while the client's receive loop is
alive (init and readback are inseparable in the proven host).

Parameters:

```jsonc
{
  "authorization": "string",      // grant_id; must be valid, unconsumed, session-scoped
  "purpose": "string",            // short purpose label (audit); e.g. "K8B-B3-readback"
  "target_endpoint": "string | null",  // default allowlisted endpoint
  "wait_ms": "int"                // bounded readback wait (clamped; e.g. 15000)
}
```

Return shape:

```jsonc
{
  "status": "PASS | FAILED | UNKNOWN",
  "session_id": "k8b-<grant>-<runid>",
  "authorization": { "grant_id": "...", "consumed": true },
  "init": {
    "sequence": [250, 100, 101, 104, 106, 105],
    "commands": [
      { "command": 250, "name": "CLIENT_CONNECTION_REQUEST",
        "transmitted": 1, "acknowledged": 1, "state": "DONE" },
      /* ... 100, 101, 104, 106, 105 ... */
    ],
    "rejected_commands": 0,        // allowlist violations (always 0 by construction)
    "order_violations": 0,
    "acknowledgement_timeouts": 0,
    "movement_writes": { "register_write_108": 0, "hr0": 0, "pwm": 0, "direction": 0,
                          "brake": 0, "emo": 0, "switch": 0, "loco": 0 },  // always 0
    "readiness": "READY | INCOMPLETE | FAILED"
  },
  "amplifiers": [                  // fresh B3 readback; amps 1/3/4/6 when observed
    {
      "slave_number": 1,
      "slave_detected_raw": 0,       // legacy DETECT latch, NOT presence
      "hr7_raw": "0x002D",           // legacy HR7 is NOT identity — raw dump only
      "hr11_checksum": "0x251F",     // checksum = build identity anchor
      "hr11_checksum_label": "legacy Offset",
      "hr21_raw": "0x0000",
      "hr22_raw": "0x0000",
      "fw_version": null,            // UNSUPPORTED when HR21/HR22 == 0 (legacy)
      "firmware_protocol_support": "UNSUPPORTED",
      "mb_comm_error": "OK",         // SLAVE_DATA_OK
      "physical_section": 1,         // from Profile.PhysicalAmplifierMapping
      "logical_section": "1.01",     // optional if derivable
      "observed_at_utc": "...",
      "last_data_received_utc": "...",
      "fresh": true
    }
  ],
  "shutdown": { "classification": "clean_shutdown", "reason": "string|null" },
  "audit_trail_file": "Logging/diagnostics-mcp/audit/k8b-<runid>.jsonl"
}
```

Evidence discipline: `hr7_raw` is explicitly a raw register dump for legacy amps
(never a firmware identity, per `docs/K8B-COMMUNICATION-RECOVERY.md` §4); HR21/HR22
zero → `UNSUPPORTED`, never decoded as a zero-version. `fresh` is `false` and the
amp is omitted (or marked `comm_failure`) when `MbCommError != OK` or the checksum/
identity is zero (the `IsObservedAmplifier` rule).

### 4.4 `diagnostics_abort_active_session(session_id?)` — B5 (safety stop)

**Class: ACTIVE / STATE_CHANGING** (it terminates a process; it does not command
the controller). Used when a session must be stopped before its bounded timeout.
It kills the tracked child process (no flash/reset), closes the audit trail, and
classifies the outcome.

Parameters:

```jsonc
{ "session_id": "string | null" }   // null => abort the single in-process active session
```

Return shape:

```jsonc
{
  "status": "PASS | UNKNOWN",
  "session_id": "k8b-...",
  "shutdown": { "classification": "safety_stop | po_intervention | unknown",
                 "reason": "string" }
}
```

---

## 5. Offline / active separation

- **Default is offline.** A fresh MCP server has no valid grant and no active
  child process. All 9 existing tools and `diagnostics_capabilities` are
  side-effect-free. `observe.capabilities()` / `core.capabilities()` keep
  `offline_only: true`, `hardware_access: false`, `socket_access: false`.
- **Active mode is per-call, not ambient.** There is no server-wide "activate"
  switch. Each ACTIVE/LIVE tool independently (1) validates a scoped grant, (2)
  validates ownership, (3) checks the allowlist, and (4) only then performs I/O.
  Absent/invalid/expired/consumed grant → `authorization_absent` and **no I/O**.
- **The grant is a consumable, non-transitive artifact** (not an in-memory flag
  that a restart could silently renew):

  ```jsonc
  // Logging/diagnostics-mcp/grants/<grant_id>.json  (git-ignored, scoped)
  {
    "grant_id": "K8B-B3-2026-10-25",
    "purpose": "K8B B3 physical register readback",
    "target_endpoint": "192.168.1.193",
    "allowed_commands": [250, 100, 101, 104, 106, 105],   // session grant
    "allowed_modes": ["session"],                          // or ["read_only"] for 113
    "issued_by": "PO + Project Lead",
    "issued_at": "2026-10-25T...Z",
    "expires_at": "2026-10-25T...Z",
    "max_uses": 1,
    "revision": "<git-rev>"
  }
  ```

  The orchestrator atomically consumes it (rename to `<grant_id>.json.consumed`
  under a lock) **before** any live I/O. A consumed/expired grant is never
  accepted. This encodes the K6/K7 "single-use" principle durably: a PL-authorized
  experiment does **not** become permanent blanket access; the next increment must
  issue a fresh grant.
- **A session is bounded.** `diagnostics_run_controller_session` is one child
  process with a clamped `wait_ms` and a hard kill-timeout; it never persists as a
  daemon and never holds a socket open inside the MCP after it returns. The MCP
  itself remains socket-free (the child holds the socket, not the MCP).

---

## 6. Ownership / conflict model

An in-memory token alone is **not** sufficient proof of exclusive access. The
design layers four mechanisms; the OS socket bind is authoritative.

1. **Authoritative cross-process mutual exclusion — the C# transport bind.**
   `RawUdpTransport` binds local recv port `10001` on `StartAsync`. If the
   production Siebwalde app, `Comm.py`, `flash_one.py`, or another client already
   binds `10001`, the C# host cannot start. The orchestrator classifies this as
   `session_conflict` (`owned_by_other`) and **never terminates the other app**.
2. **Python pre-flight bind (same host, best-effort).**
   `diagnostics_controller_preflight` and the run-session pre-flight attempt a
   transient exclusive bind on `10001`; `EADDRINUSE` → report conflict before
   launching the child. A race window between the pre-flight and the child's bind
   is tolerated because (1) is authoritative.
3. **In-process single-session registry.** A module-level singleton
   `ActiveSessionRegistry` in `active_session.py` permits exactly one active child
   per MCP process; a second `run_controller_session` while one is alive →
   `session_conflict`. (In-memory, single-process only — explicitly not the
   exclusive-access proof.)
4. **Durable advisory session lock (cross-process, belt-and-braces).**
   `Logging/diagnostics-mcp/session.lock` created with exclusive create (PID +
   endpoint + session-id + started-at), removed on clean end. Stale-lock detection
   via PID liveness (a dead PID's lock is treated as stale and may be reclaimed
   with an explicit note). This signals competitors that do **not** bind `10001`.

**Explicit limitation (carried from K6 preflight):** a local lock cannot rule out
an *external* network client on the wire. Mitigation for the state-changing init:
the init is enablement-only (105 writes HR1=0x8000 BEMF enable; no HR0/movement
write — 108 is structurally unreachable), and the command-113 identity probe
confirms the endpoint identity before init. This residual risk is documented, not
hidden.

---

## 7. Safety: fail-closed rules, stop conditions, no flash/reset, PO authority

### 7.1 Allowlist and no-escalation

- The combined allowlist is `{250, 100, 101, 104, 106, 105, 113}`.
- 108 (`EXEC_MBUS_SLAVE_DATA_EXCH`, the only HR0/PWM/movement write vector), 107
  (`RESET`), and every flash/recovery command (120..130, 200..243) are
  **structurally unreachable**:
  - C#: `KoploperSessionCommandAllowlist.IsPermitted` returns true only for the
    six init commands; `SendAsync` rejects everything else (counted, never
    forwarded).
  - Python: `observe_guard.ALLOWED_COMMANDS = {113}`; `Command113Transport` has a
    single I/O entry point.
  - Orchestrator: only ever invokes the C# host and the 113 transport.
- Observation (B3 readback, B4 version) never auto-escalates into modification:
  B3 sends no new command; B4 sends only 113.

### 7.2 Fail-closed shutdown classification (B5)

`diagnostics_run_controller_session` and `diagnostics_abort_active_session`
classify termination into exactly one of:

| Classification | Meaning |
| --- | --- |
| `clean_shutdown` | host completed readback and stopped; exit 0 + summary |
| `ack_timeout` | a command's DONE ack timed out; guard stopped the session |
| `controller_unavailable` | no host exe/dotnet, or controller unreachable before init |
| `session_conflict` | bind/ownership check failed (another app owns the controller) |
| `incomplete_init` | cold start did not reach 6/6 acknowledged before timeout |
| `safety_stop` | explicit abort / kill via `diagnostics_abort_active_session` |
| `po_intervention` | grant revoked/expired mid-flight, or PO ordered stop |
| `unknown` | process died without a recognized marker (fail-closed) |

`unknown` and `ack_timeout`/`incomplete_init` are never promoted to success;
physical state after an interrupted init remains `UNKNOWN` and must be re-observed
(Workflow v1 §9.8).

### 7.3 Stop conditions

- bounded `wait_ms` (clamped) and a hard child kill-timeout;
- per-command acknowledgement timeout (the guard's `DefaultAcknowledgementTimeout`
  = 10 s);
- bind/ownership conflict;
- absent/expired/consumed/wrong-scope grant;
- explicit `diagnostics_abort_active_session`.

### 7.4 No flash / reset / power-cycle path

None of the reused mechanisms (C# host, Python 113 transport, orchestrator)
invoke a flash/reset state machine. No `FIRMWARE_FLASH` authority is requested or
granted. Any recovery beyond "stop the host + report `unknown`" requires a
separate PO decision.

### 7.5 What requires PO authority (Workflow v1 gates)

- Any live operation (command-113 probe **and** the init session) requires scoped
  `LIVE_HARDWARE` authority. `LIVE_HARDWARE != FIRMWARE_FLASH` (Workflow v1 §9.4);
  this design needs only the former.
- Physical reset/power-cycle/flash remain **out of scope** and require PO +
  `FIRMWARE_FLASH` if ever needed.
- The MCP grant artifact is the MCP-side *encoding* of the PO's scoped grant; it
  does **not** substitute for the Workflow v1 authority gate. The Project Lead must
  still hold valid `LIVE_HARDWARE` authority for the increment.

---

## 8. File / symbol-level change plan

### 8.1 New files (Python, under `tools/diagnostics/`)

| File | Contents |
| --- | --- |
| `tools/diagnostics/session_authority.py` | Grant model + validation + atomic consume. Symbols: `Grant`, `GrantDirectory`, `load_grant(grant_id)`, `validate_grant(grant, mode, endpoint, allowed_commands)`, `consume_grant(grant)` (rename-to-`.consumed` under `threading.Lock`), exceptions `GrantAbsent`, `GrantExpired`, `GrantConsumed`, `GrantScopeMismatch`. Stdlib only. |
| `tools/diagnostics/active_guard.py` | Active-operation allowlist + vocabulary. Symbols: `ACTIVE_COMMANDS = frozenset({250,100,101,104,106,105})`, `ACTIVE_ALLOWLIST = ACTIVE_COMMANDS | {113}`, `COLD_START_ORDER = (250,100,101,104,106,105)`, `SHUTDOWN_CLASSIFICATIONS`, `classify_shutdown(exit_code, stdout, timed_out, aborted)`. Reuses `observe_guard` for the 113 half. No socket, no subprocess. |
| `tools/diagnostics/active_session.py` | Orchestrator. Symbols: `ActiveSessionRegistry` (singleton), `run_controller_session(grant_id, purpose, wait_ms)`, `abort_session(session_id)`, `controller_preflight(include_identity_probe)`, `probe_controller_version(grant_id)`, subprocess launcher (`_spawn_host`, `_parse_host_output`), ownership pre-flight (`_check_recv_port`), session lock (`_acquire_session_lock`, `_release_session_lock`, `_stale_lock`). **No framing, no SLAVEINFO decode, no init state machine.** |
| `tools/diagnostics/mcp_server/active_tools.py` | FastMCP tool defs `diagnostics_controller_preflight`, `diagnostics_probe_controller_version`, `diagnostics_run_controller_session`, `diagnostics_abort_active_session` (thin wrappers over `active_session.py`, with input guards mirroring `server.py`). |
| `tools/diagnostics/tests/test_session_authority.py` | Offline grant unit tests. |
| `tools/diagnostics/tests/test_active_guard.py` | Offline allowlist + shutdown-classification tests. |
| `tools/diagnostics/tests/test_active_session.py` | Offline orchestration tests (injected fake subprocess/socket factory; no hardware). |
| `tools/diagnostics/tests/test_mcp_active_tools.py` | Offline MCP tool-surface tests (grant-gating, no-I/O refusal, capability shape). |

### 8.2 Modified files

| File | Change |
| --- | --- |
| `tools/diagnostics/mcp_server/server.py` | Import + register the four active tools from `active_tools.py`; extend `diagnostics_capabilities()` to report `mode`/`active_surface`/`active_allowlist`/`active_effect_classes`; keep the 9 offline tools byte-for-byte equivalent. |
| `tools/diagnostics/mcp_server/__init__.py` | (if needed) re-export; no functional change expected. |
| `SiebwaldeApp/SiebwaldeApp.Koploper.Validation.Host/Program.cs` | Small, non-protocol extension of the CLI surface: (a) catch `StartAsync` bind failures and emit `{"type":"sessionConflict","reason":"..."}` + non-zero exit; (b) catch `KoploperSessionCommandAcknowledgementTimeoutException` and emit `{"type":"initFailure","command":...,"reason":"ack_timeout"}` + non-zero exit; (c) add `--json` to switch the summary to a single `{"type":"sessionSummary",...}` JSON object; (d) enrich `BuildReadRegisterLine` with `mbCommError`/`mbCommErrorName`, `physicalSection` (from `host.Profile.PhysicalAmplifierMapping`), and relabel legacy HR7 as `hr7Raw` (not `identity`). No change to the guard, allowlist, or transport. |
| `SiebwaldeApp/SiebwaldeApp.Koploper.Validation.Host/ReadOnlyTelemetryValidationHost.cs` | **No change** (reused as-is). |
| `docs/analysis-coverage.md` | Record the new investigation status (per `AGENTS.md` Knowledge Maintenance). |

### 8.3 C# CLI surface reused (exact)

Reused as-is (existing):

```text
SiebwaldeApp.Koploper.Validation.Host.exe
  readonly-validate --live --read-registers --wait-ms N
    flags: --live | --warm | --read-registers | --wait-ms | --cycles | --interval-ms | --trace
```

Proposed additive (see §8.2): `--json`; structured `sessionConflict` / `initFailure`
JSON lines; enriched `amplifierIdentityReadback` fields.

Build/artifact path (reused): `dotnet build SiebwaldeApp/SiebwaldeApp.Koploper.Validation.Host/SiebwaldeApp.Koploper.Validation.Host.csproj -c Debug` →
`.../bin/Debug/net8.0-windows7.0/SiebwaldeApp.Koploper.Validation.Host.exe`
(`AssemblyName` in the csproj; standalone, not in `SiebwaldeApp.sln`).

### 8.4 Explicit non-reuse (why)

- `tools/diagnostics/slaveinfo_decoder.py` is **not** used for B3's live readback:
  it decodes only the 63-byte Phase-1 layout (`FRAME_LENGTH = 63`, footer at
  offset 62) and would reject a legacy 12-register frame (footer at offset 40).
  B3 readback therefore flows from the C# host's layout-aware `amplifierIdentityReadback`.
  (If a layout-aware offline decoder is ever wanted, `cold_start_slaveinfo_capture.py.classify_frame`
  already demonstrates the footer-position discrimination and could be promoted to
  a shared decoder in a separate increment — out of scope here.)

---

## 9. Test plan

### 9.1 Offline unit tests (no hardware, no real socket/subprocess)

- `test_session_authority.py`: valid grant passes; missing/expired/consumed/
  wrong-endpoint/wrong-mode/wrong-commands rejected; atomic consume (two concurrent
  consumers → exactly one wins, mirroring `ObserveSession`); `.consumed` rename
  side-effect asserted.
- `test_active_guard.py`: `ACTIVE_ALLOWLIST == {250,100,101,104,106,105,113}`;
  `COLD_START_ORDER` exact; 108/107/120..130/200..243 classified `DESTRUCTIVE`/blocked;
  `classify_shutdown` maps (exit-code × stdout × timeout × abort) to the 8 classes,
  including the fail-closed `unknown`.
- `test_active_session.py` (injected fakes): ownership pre-flight bind failure →
  `session_conflict`; fake subprocess stdout fixture (a real `amplifierIdentityReadback`
  line + summary) parses to the B3 shape; ack-timeout fixture → `ack_timeout`;
  missing exe → `controller_unavailable`; `ActiveSessionRegistry` rejects a second
  concurrent session; `abort_session` → `safety_stop`; no Python code path constructs
  a wire frame or decodes SLAVEINFO (asserted by import/source guard).
- `test_mcp_active_tools.py`: each ACTIVE/LIVE tool refuses with
  `authorization_absent` when no grant; `diagnostics_capabilities` includes the new
  fields and keeps `offline_only: true`; offline tools unchanged.
- Regression: the existing `tools/diagnostics/tests` suite (currently 111 tests)
  must remain green, plus the tool-preservation guard and the K3/Comm/Knowledge
  suites.

### 9.2 Live validation step (V4, requires fresh scoped `LIVE_HARDWARE` + PO grant)

- Preflight (B1): `diagnostics_controller_preflight` reports `192.168.1.193`,
  `recv_port_bind: free`, no competing session (production app closed).
- B4: `diagnostics_probe_controller_version` returns the PIC32 version the
  controller *reports* (recorded physical: 0.1.0 / protocol 0 / build "2026-10-20";
  do **not** treat as flash-proven).
- B2+B3+B5: `diagnostics_run_controller_session` reproduces the already-physically-
  proven result — cold start 250→100→101→104→106→105 all `1/1`, 4
  `amplifierIdentityReadback` lines (amps 1/3/4/6, HR11=0x251F, HR7 raw 0x002D/0x002E,
  HR21/HR22 UNSUPPORTED), `shutdown.classification == "clean_shutdown"`.
- Evidence captured to `Logging/diagnostics-mcp/audit/` (git-ignored) with request/
  response bytes, SHA-256, timestamps, and grant-id — the same discipline as K6-B.

---

## 10. Assumptions and open questions

### Assumptions

- The PO will issue a fresh, scoped, single-use `LIVE_HARDWARE` grant for the
  implementation/live validation; no standing access.
- The production Siebwalde app is closed during any live MCP session (the K8B/K6
  pre-flight pattern).
- The legacy 12-register SLAVEINFO layout (footer at wire offset 40) is the
  deployed target; HR21/HR22 are absent (UNSUPPORTED), HR7 is a raw value, HR11 is
  the identity anchor.
- The MCP server process is trusted to spawn the C# host; subprocess execution is a
  deliberate, gated expansion of the MCP's current no-subprocess posture (isolated
  to ACTIVE tools; offline tools remain subprocess-free).

### Open questions (for the Project Lead / Product Owner)

1. Is a **durable session lock file** (bounded filesystem write under
   `Logging/diagnostics-mcp/`) acceptable for the Diagnostics MCP, or should
   ownership rest on the OS socket bind + in-process registry only? (The lock file
   is advisory; the socket bind is authoritative.)
2. Should the ACTIVE init/readback stay a **single bounded tool**
   (`diagnostics_run_controller_session`) — recommended — or be split into
   `session_begin`/`session_read_registers`/`session_end` with a C# "hold" mode?
   Splitting adds a stdin-driven hold + event stream to the C# host (more surface,
   more risk); a single bounded run matches the proven CLI and is preferred.
3. Should B4 command 113 be exposed as a standalone tool, or folded into the B1
   preflight's `include_identity_probe`? (Recommend: both — standalone for B4,
   optional probe inside preflight.)
4. Enrichment of the C# `amplifierIdentityReadback` (mbCommError, physicalSection,
   `hr7Raw` relabel) changes output consumed by other tooling — confirm no other
   consumer depends on the current field names before renaming `identity`.
5. Physical/logical mapping for B3: is `Profile.PhysicalAmplifierMapping`
   (section → physical amplifier) sufficient, or is a logical-section label
   (`1.01..1.04`) also required in the readback line?

---

## 10a. Project Lead decisions on the open questions (2026-10-25)

Resolved by the Project Lead (routine engineering/design decisions within the
K8B autonomy envelope; no PO authority gate is involved in any of these):

1. **Durable session lock file** — ACCEPT the advisory lock file
   (`Logging/diagnostics-mcp/session.lock`, bounded, git-ignored) as belt-and-braces
   cross-process signal. The OS UDP bind on port 10001 remains authoritative and is
   the only mechanism that gates a run; the lock file never silently terminates
   another application. Stale-lock reclamation stays PID-liveness based and logged.
2. **Single bounded tool vs split begin/read/end** — ACCEPT the single bounded
   `diagnostics_run_controller_session` (recommended). It matches the proven CLI,
   avoids a stdin-driven hold mode and event stream on the C# host, and keeps the
   surface minimal. No split.
3. **B4 standalone vs folded** — ACCEPT BOTH: standalone
   `diagnostics_probe_controller_version` (B4) plus the optional
   `include_identity_probe` inside the B1 preflight.
4. **Relabel legacy HR7 `identity` → `hr7Raw`** — ACCEPT the relabel. Verified
   (git grep) that no consumer depends on the current `identity`/`identityHex`
   field names other than `Program.cs` itself and human output. Legacy HR7 is NOT
   an identity register (see K8B-COMMUNICATION-RECOVERY.md §4), so the field is
   renamed to `hr7Raw` and the raw value is surfaced without an identity meaning.
5. **Physical/logical mapping** — ACCEPT physical section
   (`Profile.PhysicalAmplifierMapping`) as sufficient; include the logical-section
   label when derivable, otherwise omit it (do not fabricate).

Implementation proceeds on branch `fix/k8b-legacy-12-register-slaveryinfo` under
the existing K8B `LIVE_HARDWARE` authority; the live V4 validation step requires
the already-granted K8B authority (no new grant is needed beyond K8B closure).

---

## 11. Workflow v1 authority-gate implications (advisory)

- **Change class:** `ARCHITECTURE_INTERFACE` + `HARDWARE_SENSITIVE` +
  `WORKFLOW_TOOLING` (the MCP is agent infrastructure).
- **Risk class:** `ELEVATED` (state-changing hardware commands; no flash/reset).
  `SAFETY_RELEVANT` review treatment for the physical command path.
- **Review class:** `R2` (cross-component lifecycle, shared ownership, physical
  hardware command path). Architect design (this document) does **not** replace
  Integrator R2 review or final feature acceptance.
- **Validation class:** `V1` for offline unit tests; `V4` for the live B2/B3/B4
  step (physical hardware), under scoped `LIVE_HARDWARE`.
- **Authority:** `LIVE_HARDWARE` (scoped, single-use, non-transitive — Workflow v1
  §11.5). No `FIRMWARE_FLASH`. Merge remains separately PO-authorized.
- **Governance:** registering the extended MCP in `opencode.json` is a
  project-scoped, reversible config change; it does **not** relax the Workflow v1
  authority boundary (§2.3 — tool permission ≠ workflow authority). Any change
  that broadens the command allowlist or adds a flash/reset path would itself be a
  `GOVERNANCE_CHANGE` requiring PO approval.
- Only the Project Lead changes workflow state. This document is advisory.

---

## 12. ROLE RESULT

```
Verdict: PASS
Architecture disposition: DESIGN_ACCEPTABLE
Scope evaluated: Diagnostics MCP active-session extension (B1..B5); reuse of the
proven C# Validation.Host + Python command-113 transport; offline/active
separation; ownership/conflict; fail-closed safety; file/symbol change plan.
Evidence: tools/diagnostics/mcp_server/server.py, core.py, observe.py,
live_transport.py, observe_guard.py, slaveinfo_decoder.py,
cold_start_slaveinfo_capture.py; SiebwaldeApp.Koploper.Validation.Host
(ReadOnlyTelemetryValidationHost.cs, Program.cs, KoploperSessionCommandGuard.cs,
KoploperSessionCommandAllowlist.cs); docs/K8B-COMMUNICATION-RECOVERY.md,
docs/K6-LIVE-OBSERVE-VALIDATION.md, docs/SIEBWALDE-KNOWLEDGE-AND-DIAGNOSTICS-ARCHITECTURE.md.
Findings: recommended architecture is hybrid reuse (C# subprocess for B2/B3/B5,
existing Python 113 transport for B1/B4) with a protocol-free orchestrator; no new
independent PIC32 stack; ownership = OS bind (authoritative) + in-process registry
+ optional durable lock; authorization = consumable scoped grant; combined
allowlist {250,100,101,104,106,105,113} with 108/107/flash structurally unreachable.
Unverified: none beyond the explicitly-stated open questions (lock-file policy,
tool granularity, B3 mapping labels).
Recommended next workflow state: ANALYZE_DESIGN → present to Project Lead for PO
decision on open questions, then IMPLEMENT under a fresh scoped LIVE_HARDWARE grant.
```
