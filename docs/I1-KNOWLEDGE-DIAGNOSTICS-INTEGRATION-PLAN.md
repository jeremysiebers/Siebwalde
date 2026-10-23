# I1 — Knowledge/Diagnostics Integration Plan (dependency disentanglement)

Date: 2026-10-24. OFFLINE preparation only. No master merge.

## A. Git dependency map

- master: `376c56e`. Accepted chain: K0/K1 `5d3969f` → K2 `8fa3a6e` → K3
  `b6ee814` → K4 `26a7bf8` → K5 `c75094f` (linear, verified).
- The K-series base is `feature/pico-5b-plan` @ `1f89c0b` (NOT master). Cumulative
  `376c56e..c75094f` = **39 commits = 22 inherited + 17 K-series**.

Inherited 22 (pico-5b-plan entanglement), grouped:

| Group | Commits | Integration eligibility |
|---|---|---|
| C# HR22 protocol gate | `128a0d81` | **DEFER** (separate `feature/protocol-version-gate`) |
| Phase-1 preflight doc | `5c916522` | passive doc |
| Baseline-0 firmware archive + readback | `78a4a6a5`, `667a490f` | immutable artifacts |
| Firmware flash/recovery reference + amp-1 evidence docs | `69a501f2`, `7167e85` | passive docs |
| PicoScope MCP PICO-1..4T | `d24daef1`…`80694e9b` (14) | accepted instrumentation |
| PICO-5B docs | `c95ea616`, `e440605a`, `1f89c0b8` | passive docs |

K-series 17: K0/K1 (4), K2 (2), K3 (3), K4 (3), K5 (5).

## B. Integration strategy (recommended)

**Recommendation: Option A — layered, preserve identities**, conditional on the
HR22 gate decision (below). Integrate `feature/pico-5b-plan` (`1f89c0b`) into
master as the prerequisite layer (it is already reviewed/instrumentation +
immutable artifacts + docs), then the K-series (`a6e9dde..c75094f`) merges as a
fast-forward — **no history rewrite, all commit identities preserved**.

**Fallback: Option B — curated** (only if the HR22 gate is deferred): drop
`128a0d81` and re-stage the picoscope + baseline-0 + docs + K-series as new
curated commits. This is a history rewrite (new identities) and requires
explicit PO approval before execution.

The decisive input is the HR22 gate GO/NO-GO (Section C). Do not select a
strategy on mechanical speed alone.

## C. Production-impact assessment

### C# HR22 protocol gate (`128a0d81`) — **DEFER to independent review**

- A fail-closed Phase-1 protocol check (HR22 == 0x01) **strictly behind** the
  existing HR7 identity check. `AmplifierPhase1Safety.Compatible` now requires
  identity AND protocol; `IncompatibilityReason` distinguishes firmware vs
  protocol. 5 new tests + simulator protocol overrides.
- For the **currently deployed legacy firmware (0x251F) + PIC32 L1**, the HR7
  identity check fails first, so the HR22 check never applies — legacy behavior
  is unchanged. It only refines Phase-1 amplifier compatibility.
- **Recommendation: keep on `feature/protocol-version-gate`; defer from the
  K-series integration.** It is a production C# behavior change, not required by
  the knowledge/diagnostics/tooling integration, and needs an independent .NET
  build + test review (482 baseline + the 5 gate tests) plus its own PO decision.
  A separate GO/NO-GO: **NO-GO to integrate now** (defer), because it is a
  Phase-1-specific change unverified against the current legacy deployment.

### K3 `TrackController5/Python/Comm.py` — **isolate the operational change**

- Changes to the existing `DataAquisition` transport (used by `TrackController.py`
  / `StateMachines.py` and the new `flash_one.py`): (a) replaces `TRACKCONTROL`
  DNS / `.100` fallback with hardcoded `192.168.1.193`; (b) bounds the receive
  parse loop.
- (a) hardcodes a machine-local IP into a shared module — not appropriate for a
  reusable tool and previously semi-configurable via DNS/fallback. (b) is an
  independent correctness fix.
- **Recommendation:** isolate the `Comm.py` operational change from the passive
  K-series integration; keep the hardcoded-IP change on the diagnostic path only
  (or make it explicit CLI/config), and flag the parse-loop fix as a separately
  reviewed compatibility correction. No live behavior change in I1.

### Firmware/bootloader tooling boundary

`flash_one.py` + `test_flash_one.py` are additive single-target diagnostics
(fail-closed, K3). The bootloader firmware itself is unchanged.

## D. Knowledge corrections (applied)

- `device:amp-1` now **HISTORICAL** 0xE906 (2026-10-19 round-trip), `RUNS_FIRMWARE`
  edge `superseded: true`, summary notes a later 0x251F (23-10 C# log) implying a
  rollback, current state **UNKNOWN** (no fresh independent HR11 readback).
- Dangling provenance: the untracked L2 doc (`0x195A`) and the external diagnostic
  HEX (`0x2D00`, in the `Siebwalde-pico5b-checksum` worktree) are marked not-durable
  with the SHA-256 retained as the durable reference. Rebuild idempotent (141/83).

## E. Validation

knowledge 22 PASS, diagnostics 55 PASS, driver 43 OK, preservation guard exit 0,
rebuild idempotent, MCPs register offline.

## F. PO decisions required

1. **HR22 gate**: defer (recommended) vs integrate independently — the gate is a
   Phase-1 C# behavior change unverified against the current legacy deployment.
2. **Integration strategy**: Option A (layered, preserve identities, integrates
   the full pico-5b-plan including the gate) vs Option B (curated, requires
   history rewrite + PO approval to drop the gate).
3. **Comm.py operational change**: isolate/revert the hardcoded-IP change vs
   accept it as a diagnostic-only modification.

## G. Next increment

K6 = first physical OBSERVE (PIC32 `GET_CONTROLLER_VERSION` 113), gated by a new
scoped PO `LIVE_HARDWARE` authorization + offline transport validation first.
Final master integration is a separate, later PO authority gate.
