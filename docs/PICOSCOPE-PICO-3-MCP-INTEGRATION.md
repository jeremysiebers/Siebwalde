# PICOSCOPE PICO-3 — MCP Integration (Layer C)

Date: 2026-10-22. OFFLINE implementation of the Layer C FastMCP stdio adapter over
the validated `tools/picoscope/` backend. No global OpenCode config change, no
hardware acquisition (the real-device operator gate stays closed). No Siebwalde
C#/firmware change.

## 1. Branch + scope

- Branch: `feature/picoscope-pico-3` (branched from `feature/picoscope-pico-2` @ `937c2ec`).
- Adds `tools/picoscope/mcp/` (Layer C) + `tools/picoscope/tests_mcp/` (20 offline
  MCP test categories) + this document. Reuses the validated Layer A backend and
  the deterministic simulator unchanged.

## 2. SDK / OpenCode inspection (Step 1 results)

- **OpenCode CLI:** `1.18.35` (`opencode.cmd --version`). MCP config lives in
  `opencode.json` under the `mcp` key (local servers use `type: "local"` +
  `command` array; see §6). The active project `opencode.json` was **not modified**.
- **MCP SDK availability:** neither `mcp` nor `fastmcp` was pre-installed; both
  are installed **project-locally** into `tools/picoscope/.venv`.
- **Selected + pinned SDK:** **FastMCP `4.1.3`** (high-level stdio framework),
  which depends on the official **`mcp` `2.3.0`** SDK (the v2 API). Both are pinned
  in `tools/picoscope/requirements-mcp.txt`; the v1/v2 SDK surface is never mixed.
  - Local stdio transport API (FastMCP 4.1.3): `FastMCP(name)`,
    `@mcp.tool()` on plain functions, `mcp.run(transport="stdio")`.
  - Real MCP test client (official `mcp` 2.3.0): `mcp.client.stdio.stdio_client(
    StdioServerParameters(command, args, cwd, env))` + `ClientSession(read, write)`
    → `await session.initialize()`, `session.list_tools()`, `session.call_tool(...)`.
- Base `requirements.txt` (numpy only) is unchanged; the MCP layer adds
  `requirements-mcp.txt`.

## 3. The four tool schemas

Server name: `picoscope` (OpenCode prefixes tool names as `picoscope_<tool>`).

### 3.1 `scope_info`

- **Input:** `{ "backend": "simulator" | "ps3000a" | "picosdk" | "real" }` (default `"simulator"`).
- **Output (compact):**
  ```json
  {
    "model": "3406D MSO", "backend": "simulator", "serial": "SIM0001",
    "variant": "SIMULATOR", "sdk_driver": "simulator-1.0.0",
    "capabilities": {"analog_channels": 4, "digital_ports": 2, "digital_lines": 16,
                     "block_capture": true, "measurement_only": true},
    "ownership": "available", "simulated": true, "real_backend_gate": "closed",
    "limits": { ... }
  }
  ```
- Non-owning discovery only. `ownership` ∈ `available | busy | gated | none`.
  A real-backend request with the gate closed returns `ownership: "gated"` and
  `real_backend_gate: "closed"` **without touching the driver**.

### 3.2 `scope_capture_block(acquisition_profile)`

- **Input `acquisition_profile`:**
  ```json
  {
    "backend": "simulator",
    "device_id": null,
    "analog_channels": [{"channel": "A", "enabled": true, "range_v": 5.0, "coupling": "DC"}],
    "digital_ports":    [{"port": 0, "enabled": true, "threshold_mv": 1500}],
    "trigger": {"source": "A", "threshold": 0.0, "edge": "rising",
                "pre_samples": 200, "post_samples": 800},
    "pre_samples": 0, "post_samples": 1000, "timebase": 10,
    "trigger_wait_ms": 5000, "notes": null
  }
  ```
- **Output (compact; no raw arrays):**
  ```json
  {
    "capture_id": "<32-hex uuid>", "sample_interval_ns": 2048, "sample_count": 1000,
    "trigger_status": "unused" | "triggered" | "unknown",
    "channel_mapping": {"analog": {...}, "digital": {...}},
    "artifact_paths": {"npz": "<evidence>/<id>.npz", "json": "<evidence>/<id>.json"},
    "warnings": [], "result": "ok", "simulated": true, "backend": "simulator"
  }
  ```
- Validation order: interval-independent limits → backend domain rules → (simulator)
  duration/total-timeout → only then cross-process lock + device open. Invalid
  profiles are rejected **before any SDK call**. `trigger_wait_ms` is validated
  against the limit; the real backend's readiness wait is Layer A's fixed 5 s bound.

### 3.3 `scope_get_capture_metadata(capture_id)`

- **Input:** `{ "capture_id": "<32-hex uuid>" }`.
- **Output:** compact sidecar metadata (identity, ranges, thresholds, trigger,
  sample count/interval, `channel_mapping`, `samples` dtype/shape descriptor only,
  `provenance`) + `artifact_paths`. Reads the JSON sidecar only — **no device, no
  `.npz` load, no raw arrays**.

### 3.4 `scope_export_capture(capture_id)`

- **Input:** `{ "capture_id": "<32-hex uuid>" }`.
- **Output:** `{ "capture_id", "artifact_paths": {npz, json}, "metadata": {...compact},
  "simulated", "backend", "result": "ok" }`. Returns validated artifact references
  only — never streams sample arrays.

## 4. Ownership handling

- **In-process guard (reused):** `backend/ownership.py` (`acquire`/`release`,
  token-scoped). Opening an owned device raises `DeviceBusyError`; release is
  deterministic on `close_device()` and on every exception path.
- **Cross-process guard (added):** `tools/picoscope/mcp/lock.py` — a narrow,
  removable, non-blocking OS file lock (`msvcrt.locking` on Windows, `fcntl.flock`
  fallback) per device id, inside the evidence dir. Two MCP server processes, or a
  second process vs PicoScope 7, cannot own the same device: the second acquire
  raises `DeviceBusyError` with a clear message. It **never kills any process**,
  and removing the small `.lock-<device>` file is the manual override.
- **Never simulator-fallback on a real-device failure:** the MCP layer maps
  `real`/`ps3000a`/`picosdk` directly to the `ps3000a` backend; a real request that
  fails raises (gate-closed error, `BackendUnavailableError`, `DeviceNotFoundError`,
  or `DeviceError`) — it never silently switches to the simulator.
- **Operator gate:** real-device acquisition requires `PICOSCOPE_MCP_ALLOW_REAL=1`
  (default off). In this increment it stays closed; no hardware was touched.
- **Deterministic close on exception/cancel:** `lock.release()` and
  `scope.close_device()` run in `finally` on every path (see the timeout-cleanup test).

## 5. Finite, configurable limits (sensible defaults)

Enforced by `tools/picoscope/mcp/config.py`, overridable via environment variables:

| Limit | Default | Env override |
|---|---|---|
| `max_sample_count` (pre+post) | 1,000,000 | `PICOSCOPE_MCP_MAX_SAMPLE_COUNT` |
| `max_analog_channels` | 4 | `PICOSCOPE_MCP_MAX_ANALOG_CHANNELS` |
| `max_digital_ports` | 2 | `PICOSCOPE_MCP_MAX_DIGITAL_PORTS` |
| `max_duration_s` | 10.0 | `PICOSCOPE_MCP_MAX_DURATION_S` |
| `max_trigger_wait_ms` | 60,000 | `PICOSCOPE_MCP_MAX_TRIGGER_WAIT_MS` |
| `total_acquisition_timeout_s` | 60.0 | `PICOSCOPE_MCP_TOTAL_ACQUISITION_TIMEOUT_S` |
| `max_memory_bytes` | 64 MiB | `PICOSCOPE_MCP_MAX_MEMORY_BYTES` |
| `max_capture_file_bytes` | 64 MiB | `PICOSCOPE_MCP_MAX_CAPTURE_FILE_BYTES` |
| `max_metadata_response_bytes` | 256 KiB | `PICOSCOPE_MCP_MAX_METADATA_RESPONSE_BYTES` |

The memory/file budget is derived from `sample_count × (8·analog + 1·digital_lines)`
bytes. The duration/total-timeout limits are enforced against the resolved sample
interval for the simulator (before any SDK call); for the real backend they are
checked against the interval returned by `ps3000aGetTimebase2`.

## 6. Project-scoped OpenCode registration (documented — NOT applied)

The following snippet is documentation only. **Do not apply it to the active
`opencode.json` yet** — the Product Owner / Project Lead applies it after the
operator gate is opened. To use it, merge the `mcp`/`tools`/`agent` blocks into
the project `C:\Localdata\Siebwalde\opencode.json` (a reversible, project-local edit;
no global `~/.config/opencode` change).

```jsonc
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "picoscope": {
      "type": "local",
      "command": [
        "C:\\Localdata\\Siebwalde\\tools\\picoscope\\.venv\\Scripts\\python.exe",
        "-m", "tools.picoscope.mcp"
      ],
      "cwd": "C:\\Localdata\\Siebwalde",
      "enabled": true,
      "environment": {
        "PICOSCOPE_EVIDENCE_DIR": "C:\\Localdata\\Siebwalde\\tools\\picoscope\\evidence\\mcp"
      },
      "timeout": 5000
    }
  },
  // Optional: expose the scope tools only to the measurement agent.
  "tools": { "picoscope_*": false },
  "agent": {
    "measurement": { "tools": { "picoscope_*": true } }
  }
}
```

- **Enable:** set `mcp.picoscope.enabled: true` (and, if per-agent scoped, enable
  `picoscope_*` only in the target agent). Restart OpenCode.
- **Disable:** set `mcp.picoscope.enabled: false` (or remove the `mcp.picoscope`
  block). Restart OpenCode.
- **Operator gate for real acquisition:** set `PICOSCOPE_MCP_ALLOW_REAL=1` in the
  `environment` block only when a physical 3406D MSO capture is authorized.
- Tool names exposed to OpenCode: `picoscope_scope_info`, `picoscope_scope_capture_block`,
  `picoscope_scope_get_capture_metadata`, `picoscope_scope_export_capture`.
- There is no `measurement` agent registered yet; the snippet uses it as the
  intended target — the PO/PL selects the actual agent name.

## 7. Measurement-only allowlist + security boundaries

- The MCP layer calls only these Layer A functions: `discover_devices`,
  `open_device`, `get_device_info`, `configure_analog_channel`,
  `configure_digital_port`, `configure_trigger`, `capture_block`, `save_capture`,
  `close_device`. No AWG / signal generation, no arbitrary PicoSDK command, no
  flash / reset / motion / Siebwalde state, no shell command. Enforced by a static
  test (`test_18_no_unsafe_output_op.py`).
- **Evidence security:** a single dedicated evidence dir (default
  `tools/picoscope/evidence/mcp`, override via `PICOSCOPE_EVIDENCE_DIR`). Capture ids
  are server-generated UUID hex; the only accepted id format is `^[0-9a-f]{32}$`
  (choke point that rules out `..`, `/`, `\` traversal). Artifact paths are resolved
  and checked with `os.path.realpath` + `os.path.commonpath` against the evidence dir
  (defends against symlink escape). No client-supplied paths exist.
- **Simulated vs real marking:** every capture sidecar carries `provenance.simulated`
  and `provenance.evidence_kind` (`"simulated"` | `"real"`), and every capture result
  carries `simulated` + `backend`; simulator evidence can never be mistaken for real.
- **No raw arrays in responses:** responses contain ids/paths/metadata only; raw
  samples live exclusively in the `.npz` artifact. Verified by `test_10`.

## 8. Offline MCP tests (20 categories, real MCP client)

Run (from the repo root):

```
tools\picoscope\.venv\Scripts\python.exe -m pytest tools\picoscope\tests_mcp -q
tools\picoscope\.venv\Scripts\python.exe -m pytest tools\picoscope\tests -q
```

Result: **48 passed** across 20 category files (see below); the PICO-1/PICO-2
regression remains **123 passed**.

| # | Category | File |
|---|---|---|
| 1 | server start/stop | `test_01_server_start_stop.py` |
| 2 | stdio handshake | `test_02_stdio_handshake.py` |
| 3 | tool discovery | `test_03_tool_discovery.py` |
| 4 | schema validation | `test_04_schema_validation.py` |
| 5 | explicit simulator selection | `test_05_explicit_simulator_selection.py` |
| 6 | `scope_info` simulated identity | `test_06_scope_info_simulated_identity.py` |
| 7 | MCP-client-requested capture | `test_07_mcp_client_capture.py` |
| 8 | NPZ + JSON generation | `test_08_npz_json_generation.py` |
| 9 | metadata-by-id | `test_09_metadata_by_id.py` |
| 10 | no raw arrays in responses | `test_10_no_raw_arrays.py` |
| 11 | invalid-request rejection | `test_11_invalid_request_rejection.py` |
| 12 | sample/timeout limits | `test_12_sample_timeout_limits.py` |
| 13 | concurrent-acquisition rejection | `test_13_concurrent_acquisition_rejection.py` |
| 14 | simulated device-error propagation | `test_14_simulated_device_error_propagation.py` |
| 15 | cancel/timeout cleanup | `test_15_cancel_timeout_cleanup.py` |
| 16 | unknown capture-id fail-safe | `test_16_unknown_capture_id_failsafe.py` |
| 17 | files stay inside evidence dir | `test_17_files_stay_inside_evidence_dir.py` |
| 18 | no unsafe output op | `test_18_no_unsafe_output_op.py` |
| 19 | metadata response size limit | `test_19_metadata_response_size_limit.py` |
| 20 | PICO-1/PICO-2 regression (123) | `test_20_pico1_pico2_regression.py` |

The tests use the official `mcp` 2.3.0 stdio client against the simulator only;
they never open the real `ps3000a` driver and never touch hardware.

## 9. GO/NO-GO

**GO for PICO-4.** The Layer C FastMCP stdio adapter is implemented offline and
validated by a real MCP client across all 20 categories; the PICO-1/PICO-2
regression is unaffected (123/123). Real-device capture remains gated behind the
operator gate (`PICOSCOPE_MCP_ALLOW_REAL=1`) and separate `LIVE_HARDWARE`
authorization — this increment performed no hardware acquisition.
