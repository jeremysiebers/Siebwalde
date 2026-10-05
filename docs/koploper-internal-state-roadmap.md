# Koploper Internal State → Siebwalde Integration Roadmap (KIS)

**Status:** Product Owner program (2026-10-05). Authoritative technical source: `docs/koploper-internal-state-integration.md` (master handoff PoC 01–06). This roadmap supersedes the earlier Carrier Increment position-derivation plan; the functional Carrier requirements (per-loc duty, timely next-section, neutralise vacated section to 399, two independent locs) remain in force.

**Current authorization:** KIS-01 + KIS-02 only (software-only, `PR_EXECUTION`, no merge). KIS-03 and later start only after KIS-01/02 are cleanly merged/closed and the Product Owner confirms the transition.

## Hard architecture decision

- **Koploper owns:** planning, route choice, block reservation, loco administration, brake/stop planning, switch-street logic.
- **Siebwalde owns:** track-amplifier assignment, PWM/setpoint distribution, direction, physical occupancy, freshness, safety/interlocks, hardware diagnostics, actual hardware-output authorization.
- **Hard rule:** `Observed Koploper state != Authorized physical output`. A reservation may cause logical ownership, never a direct PWM write. Existing occupancy/freshness/movement-permission/safety layers stay independently decisive.

## Reliability model (from the handoff)

- **High confidence (production read-only):** root RVA `0x3259B0`; block list `root+0x5AC`; loco list `root+0x5C8`; `TBlok+0x14C` internal block ID; `TBlok+0x1AC` owner; `TBlok+0x1ED` state (`0` Free, `1` Reserved, `2` Occupied, `9` Transition); `Loc+0x1A8` internal loc ID.
- **Strong but not safety-authoritative:** `Loc+0x58` current-block candidate (cross-check only).
- **Not a primary source (do not reintroduce without new evidence):** `TBlok+0x16C`, `TBlok+0x24C`, `TBlok+0x258`, `TBlok+0x25C`, `Loc+0x4D8`, TCP/5000 reservation API, 1500 logical actions.
- **Binary gating:** only Koploper `9.4.0.9` (SHA-256 `645B4681C14975F3619EB44968C74F3B7925CB914C5A23302932918D73079D2E`). Mismatch → `UnsupportedVersion`/`Unknown`, no decode, no authority. Absolute runtime pointers never hard-coded (ASLR via `moduleBase + RVA`).
- **Read-only:** only `PROCESS_QUERY_LIMITED_INFORMATION` + `PROCESS_VM_READ`. No `WriteProcessMemory`/injection/hooking/patching.
- **Coherent full snapshot is authoritative:** bounded double-read/retry; torn/mutating/invalid/stale → retry then `Degraded`/`Unknown`; never publish a partial old/new mix; no stale reservation inheritance.
- **ID separation:** internal block ID ≠ display block number ≠ physical amplifier address. Mapping mismatch = configuration error / fail-safe.

## KIS stages (each: Increment Contract → implementation → tests → independent review → PR → MERGE_READY → revision-bound merge → post-merge closure)

| Stage | Scope | Output authority |
|---|---|---|
| **KIS-01** | Read-only Windows process foundation: `IKoploperProcessLocator`, `IKoploperExecutableVerifier`, `IKoploperMemoryReader` (P/Invoke), 32-bit pointer helpers, no-write verification | none (foundation) |
| **KIS-02** | Koploper 9.4 objectgraph decoder: versioned `Koploper94MemoryLayout`, root resolver, TList decoding, raw block/loco registries, sanity checks | none |
| **KIS-03** | Typed block-state decoder (`Unknown/Free/Reserved/Occupied/Transition`) + owner validation | none |
| **KIS-04** | Coherent snapshots + freshness (double-read/retry, sequence, generation, health, age, restart invalidation) | none |
| **KIS-05** | Reservation Observer (`occupied block` + `reserved blocks` per loc; full snapshot authoritative) | none |
| **KIS-06** | Live simulator cross-validation (Koploper 9.4 GUI + observer + optional 5700) | none |
| **KIS-07** | Siebwalde mapping (`Koploper block → logical section → physical amplifier`) — SHADOW only, no writes | none |
| **KIS-08** | Reconciliation + safety integration (observed planning vs desired vs reconciled vs authorized) — still SHADOW | none |
| **KIS-09** | Controlled physical validation (one loc → full loop → two locs) | `LIVE_HARDWARE` (separate) |

**Note:** KIS-01 + KIS-02 may be combined into the first PR (per the technical handoff). No scope creep into state semantics or TrackControl integration.

## Open acceptance items (remain open)

- Explicit reservation cancellation (`state 1 → gone` without `state 2`) — not yet reproduced; validated before live output authority.
- Definitive display-block mapping (`TBlok+0x15C`) for the real Siebwalde database.
- Route ordering (state-1 gives a set, not a guaranteed ordered route).
- Performance/freshness budget for live physical control (not the ~300 ms PowerShell PoC value).

## Diagnostics (minimum)

`KOPLOPER_PROCESS_NOT_FOUND`, `KOPLOPER_UNSUPPORTED_BINARY`, `KOPLOPER_ACCESS_DENIED`, `KOPLOPER_ROOT_INVALID`, `KOPLOPER_LIST_INVALID`, `KOPLOPER_POINTER_INVALID`, `KOPLOPER_SNAPSHOT_INCONSISTENT`, `KOPLOPER_UNKNOWN_BLOCK_STATE`, `KOPLOPER_OWNER_NOT_FOUND`, `KOPLOPER_STALE`, `KOPLOPER_RESTARTED`, `KOPLOPER_BLOCK_MAPPING_MISSING`, `KOPLOPER_BLOCK_MAPPING_CONFLICT`.

## Suggested repository placement (Core, not EcosEmu)

```
SiebwaldeApp.Core/
  Koploper/
    Process/
    Memory/
    Model/
    Observation/
    Diagnostics/
SiebwaldeApp.Core.Tests/
  Koploper/
```
