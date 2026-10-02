# Simple Loop — Physical Integration & Firmware Evolution Program

**Status:** Overarching Product Owner program (2026-10-01). **Step 1 is DONE / MERGED** (PR #18, merge commit `ccfd119`, post-merge CI PASS, 482/482). Steps 2–4 are planned and architecturally prepared but NOT executed until the preceding step is closed and the Product Owner authorizes the transition.

**Audience:** Product Owner (authority), Project Lead, Architect, Developer, Integrator.

This document is the durable four-step roadmap. The Active State Manifest (`.opencode/workflow/active-state.json`) describes only the step that is actually executing; `docs/current-state.md` reflects durable reality.

---

## 1. Program goal

Continue from the working Full Software Simulation (including live Koploper integration) toward a physically driving test oval, then firmware evolution (MMDC) and controlled fault/recovery validation.

Four sequential development steps:

1. **Simple Loop — C# / Real-mode Readiness** (software-only; no physical actuation).
2. **Simple Loop — Physical Validation with Existing Firmware** (build the physical reference baseline; no firmware change).
3. **Amplifier Firmware Evolution — MMDC & Observability** (PIC18 firmware, on the same physical Simple Loop).
4. **Fault Injection, Protection & Recovery Validation** (systematic fault/recovery evidence).

Each step is an independent, verifiable Workflow v1 increment with its own stage-gates.

---

## 2. One Simple Loop, two execution environments

The logical Simple Loop topology stays identical between FullSimulation and Real mode:

```text
                  Simple Loop
                  LayoutProfile
                       |
                Siebwalde C# Core
                       |
               Movement Safety Gate
                       |
                TrackControlMain
                       |
            +----------+----------+
            |                     |
      FullSimulation             Real
            |                     |
      Deterministic          PIC32 Master
      TrackTransport              |
            |                RS-422 / PIC18
      Simulated amps              |
            |                4 proto amps
            |                 1 / 3 / 4 / 6
            |                     |
            +----------+----------+
                       |
              Vergelijkbare tests
```

Additional environment/hardware bindings are allowed; a second, different definition of the same logical layout is not. Execution mode must not leak into the control logic above the movement-safety gate.

---

## 3. Stage-gates (every step)

1. Intake and baseline verification.
2. Increment Contract with acceptance criteria.
3. Analysis and any architecture decisions.
4. Implementation.
5. Automatic verification and relevant integration tests.
6. Independent review (Workflow v1).
7. Corrective loops as required.
8. `MERGE_READY` / `WAITING_AUTHORITY`.
9. Separate revision-bound human merge authorization.
10. Merge, post-merge verification, closure.

A step may begin only after the previous step is formally closed AND the Product Owner has explicitly authorized the transition. A green CI run is not transition authority; a merge is not closure until post-merge verification passes. Safety/config/hardware blockers keep the step open or `BLOCKED`.

---

## 4. Evidence boundary (applies to all steps)

```text
Requested != Commanded != Transmitted != Protocol-observed != Physical-observed
```

- An in-memory default is never protocol evidence.
- Simulated occupancy is not physical occupancy.
- An HR0 command echo is not an independent measurement of applied PWM.
- Protocol-observed HR0 == 399 does not prove physical neutral PWM or a stopped locomotive.
- `Requested/Commanded/Transmitted/Protocol-observed/Physical-observed` stay distinct in every claim and every test.

---

## 5. Step summaries and authority

| Step | Scope | Physical action | Authority required |
| --- | --- | --- | --- |
| 1 — C#/Real-mode Readiness | Software-only: physical binding, Real-mode composition, safety/startup, WPF awareness, tests | None | PR_EXECUTION (no merge) |
| 2 — Physical validation, existing firmware | Bring-up + reference baseline with current F1 firmware | `LIVE_HARDWARE` per phase; operator in the loop | Separate per-phase `LIVE_HARDWARE`; `FIRMWARE_FLASH` only if flashing becomes necessary |
| 3 — Firmware evolution (MMDC) | PIC18 MMDC + observability | Firmware flash + physical new-feature tests | `FIRMWARE_FLASH` + `LIVE_HARDWARE` (separate) |
| 4 — Fault injection / recovery | Fault simulation first, then bounded physical faults | Non-destructive emulation, then risk-assessed physical faults | `LIVE_HARDWARE` + per-test operator/product authorization |

This roadmap grants NO live-hardware, firmware-flash or fault-injection authority by itself.

---

## 6. Step 1 — Simple Loop: C# / Real-mode Readiness (DONE — MERGED, PR #18)

**Objective.** Prepare the existing C# application so the same Simple Loop that works in FullSimulation can be consciously selected and used in Real mode. No physical hardware is driven; no firmware change.

**Scope pillars.**

- **1.1 LayoutProfile & hardware binding** — separate logical section-id / block-id / amplifier-slave / occupancy-feedback mapping / simulator binding / physical hardware binding; add the `logical section -> physical amplifier` mapping (prototype amplifiers **1, 3, 4, 6**); validate duplicates/missing/contradictions; confirm which feedback the current firmware actually supports; do not silently simulate missing physical functionality in Real mode.
- **1.2 Real-mode composition** — the real production chain (`TrackControlHost`, `TrackControlMain`, `TrackCommClientAsync`, real transport, amplifier config, movement-safety gate, lifecycle, diagnostics/feedback) and profile-driven startup; no Simple-Loop-only bypasses.
- **1.3 Safety & startup** — corrected observed-neutral semantics (PR #17): movement permission starts `NotGranted`; the effective safety domain is explicitly known and never silently empty; only genuine protocol readback counts; partial/stale/non-neutral never grants; Stop/Restart/comm-error behavior verified.
- **1.4 Simple Loop from WPF** — conscious selection + visible execution mode; Real-mode startup never implied by selecting a profile.
- **1.5 Automated tests** — load/validate, topology, 1/3/4/6 mapping, shared topology, faulty bindings, safe init, observed-neutral grant, Start/Stop/Restart, no stale state, simulated-vs-real observation separation.

**Acceptance.** C# software-ready for the physical Simple Loop; mapping proven correct; FullSimulation still works; Real mode correctly prepared; tests + CI green; independent review done; a concrete Step 2 bring-up procedure produced. Stop at `MERGE_READY` / `WAITING_AUTHORITY`.

**Koploper reference data (2026-10-01).** The Product Owner supplied authoritative Koploper JSIF reference data, ingested under `docs/koploper-interop/` (provenance + SHA-256, original bytes preserved). The four-block Simple Loop (no switches, **one bezetmelder per block** `1.01..1.04`, DCC decoder addresses **1/2** ↔ ECoS object IDs **1000/1001**, physical binding 1/3/4/6) was actually loaded by Koploper and visually checked. Step 1 aligns `simple-loop.json` with this mapping and keeps the identifier distinctions explicit (logical section-id ≠ Koploper block ≠ bezetmelder name ≠ Koploper-internal feedback-id ≠ decoder address ≠ ECoS object-id ≠ physical amplifier).

**Current workflow state.** Tracked in the Active State Manifest.

---

## 7. Step 2 — Physical Simple Loop with Existing Firmware (IN PROGRESS — reference baseline recorded)

Use the existing F1 firmware baseline to make the physical Simple Loop run; establish the physical reference baseline. No automatic firmware change; flash only with separate `FIRMWARE_FLASH` authority if unavoidable. Phased: Gate A (physical readiness, no movement), Gate B (communication + neutral validation, with V4 measurements, explicit protocol-echo vs applied-PWM distinction), Gate C (controlled single-locomotive movement). Document firmware identity/checksum (`0x251F` is a known reference, not proof), mapping, measured behavior, observed-neutral results, working vs missing feedback. Distinguish software-proven / protocol-proven / physically-measured / not-yet-proven; never administratively PASS an unexecuted physical test.

**Progress (2026-10-02):** Gate A done; Gate B done at protocol level (detection 1/3/4/6, checksum `0x251F` confirmed, observed neutral; physical PWM measurement still pending); Gate C done for one loc (full loop) + two locs (with findings). Reference baseline recorded in `docs/simple-loop-reference-baseline.md`. Key findings: direction polarity fixed physically; communication hang / no watchdog; occupancy root cause was a Koploper config error; and a material architecture gap — **carry locomotive properties across the track** (PWM duty as a per-loc variable, neutralise vacated block, section-boundary duty match), which is the next increment.

---

## 8. Step 3 — Amplifier Firmware Evolution: MMDC & Observability (planned)

After Step 2's reference baseline: evolve the PIC18 amplifier firmware for MMDC and observability. Establish the MMDC functional contract (designed vs missing vs interfaces vs new product functionality); extend telemetry (requested PWM vs stored HR0 vs applied PWM vs brake/enable/EMO vs regulator/status/fault vs build-id) from the existing PIC18/PIC32 protocol architecture; keep the reproducible F1 build; keep bootloader separate; consistent PIC32/C#/simulator contracts. Compare against the Step 2 baseline (unchanged / intended new / new diagnostics / regressions). Flashing and physical new-feature tests require separate authority.

---

## 9. Step 4 — Fault Injection, Protection & Recovery Validation (planned)

Repeatable fault matrix (normal load, overload, short-circuit, amplifier comm loss/return, PIC32 loss, C# restart, Koploper Stop/EMO, firmware reset, bad/missing feedback) with per-layer safety ownership (hardware / PIC18 / PIC32 / C# / Koploper). Fault simulation first (deterministic, reusing the production chain and safety mechanisms, no forced-outcome simulator); then risk-assessed, bounded physical faults (current-limited, measured, predefined stop criteria, explicit authority; a real short-circuit test is never an automatic follow-up to a successful simulation). Validate recovery (auto-recovery allowed? movement permission stays NotGranted? individual amp reset? host restart? Koploper resync? when may the loco move again?) with the PR #16 restart/resync boundary. Report per scenario: software-tested / hardware-tested / PASS-FAIL / partially proven / open / future improvement.

---

## 10. Overarching quality rules

- No parallel product implementation across steps (architectural preparation only, until closed + authorized).
- Evidence separation (§4) always.
- Project Lead is orchestration owner; specialized agents (Architect/Developer/Integrator/Designer) used per Workflow v1 with the smallest sufficient role set.
- No autonomous merge to master; no force-push / history rewrite / evidence-branch deletion; no firmware flash or physical actuation on the strength of this roadmap alone.
- Post-merge CI is a real acceptance gate; a failing post-merge CI keeps the step open via the normal corrective loop.

## 11. Expected end state (all four steps done)

1. One shared Simple Loop topology usable in FullSimulation and Real mode.
2. A physically driving oval on the four prototype amplifiers.
3. A documented existing-firmware baseline.
4. New validated MMDC/observability firmware.
5. Reproducible tests comparing software and hardware behavior.
6. A fault/recovery test environment for future firmware and C# changes.
7. A proven development path scalable to the full Siebwalde layout.
