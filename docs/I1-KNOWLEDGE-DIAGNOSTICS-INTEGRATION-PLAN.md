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
| Firmware flash/recovery reference + amp-1 evidence docs | `69a501f2`, `d7167e85` | passive docs |
| PicoScope MCP PICO-1..4T | `d24daef1`…`80694e9b` (13) | accepted instrumentation |
| PICO-5B docs | `c95ea616`, `e440605a`, `1f89c0b8` | passive docs |

K-series 17: K0/K1 (4), K2 (2), K3 (3), K4 (3), K5 (5).

## B. Integration strategy (binary; recommended = Option A)

The HR22 gate (`128a0d81`) is the **direct child of master** `376c56e`; all 21
other pico-5b-plan commits descend from it. Therefore "defer the gate" and
"preserve commit identities" are **topologically mutually exclusive** — the
choice is binary:

- **Option A — layered, preserve identities** (only if the gate is GO): merge
  `feature/pico-5b-plan` (`1f89c0b`) into master as the prerequisite layer
  (fast-forward while master stays at `376c56e`), then the K-series
  (`a6e9dde..c75094f`) fast-forwards cleanly — **no history rewrite**; all commit
  identities AND all 67 `source_revision:"1f89c0b"` provenance references stay valid.
- **Option B — curated, drop the gate** (if the gate is NO-GO/deferred): drop
  `128a0d81` and re-stage the picoscope + baseline-0 + docs + K-series as new
  curated commits. This is a history rewrite (new identities) that also
  **invalidates 67 seed `source_revision:"1f89c0b"` references** (plus `9ff8e39` /
  `8fa3a6e` / `e880fde` / `26a7bf8`) — requiring a provenance re-pinning pass.
  Requires explicit PO approval for the rewrite.

**Recommended: Option A**, because the HR22 gate is safe (fail-closed, behind
HR7, legacy-unaffected) and integrating it as the prerequisite layer preserves
both commit identities and knowledge provenance, subject to its independent C#
review (Section C). Note: master must still be at `376c56e` at integration time
for the fast-forward to hold.

## C. Production-impact assessment

### C# HR22 protocol gate (`128a0d81`) — **GO conditional (integrate as prerequisite, after independent C# review)**

- A fail-closed Phase-1 protocol check (HR22 == 0x01) **strictly behind** the
  existing HR7 identity check (verified in `AmplifierPhase1Safety.cs:17-49` and
  `TrackAmplifierRegisters.cs:55-62`). `0x00` (absent/legacy/exception) and
  `0x02` → deny ("never compatible by default"); legacy 0x251F / PIC32-L1 fail
  the HR7/register-count check first, so legacy behavior is unchanged.
- **Recommendation: GO as part of the prerequisite layer (Option A)**, gated on
  an independent C# review (.NET build + 482 baseline + the 5 gate tests), which
  the K-series chain did not perform. Do not treat a feature-branch test-pass as
  integration approval.
- **Separate GO/NO-GO: GO-conditional** — if the C# review passes, integrate; if
  it cannot be reviewed now, defer (which forces Option B + the 67-reference
  provenance re-pinning cost). Note `feature/protocol-version-gate` also carries
  `5c916522` (the Phase-1 preflight doc) — both travel together.

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

1. **HR22 gate**: GO-conditional (integrate as the prerequisite layer after an
   independent C# review) — the gate is safe/fail-closed/legacy-unaffected and is
   the single production-C# change in the entanglement. If it cannot be reviewed
   now, defer (which forces Option B + the provenance re-pinning cost).
2. **Integration strategy**: binary — Option A (layered, preserve identities +
   provenance, requires the gate GO) vs Option B (curated, drop the gate, history
   rewrite + re-pin 67 `source_revision` references, requires PO approval for the
   rewrite).
3. **Comm.py operational change**: isolate/revert the hardcoded-IP change vs
   accept it as a diagnostic-only modification (the parse-loop fix is a separate
   compatibility correction).

## G. Next increment

K6 = first physical OBSERVE (PIC32 `GET_CONTROLLER_VERSION` 113), gated by a new
scoped PO `LIVE_HARDWARE` authorization + offline transport validation first.
Final master integration is a separate, later PO authority gate.
