# Amplifier System Requirements

**Status:** Durable requirements record, consolidated from the Product Owner interview of 2026-10-15. This is a **requirements and context document**, not a specification, not an architecture design, and **not an implementation increment**. It does not authorize behavioral implementation, firmware changes, flashing, hardware communication, or live tests.

**Authority:** Product Owner decisions captured here supersede any earlier interview proposals or example numbers. Where this document and an older proposal conflict, the text below wins.

**Scope warning:** This requirements set spans the C# host, PIC32 master and PIC18 amplifier firmware. It is **not** one authorized implementation increment. Future fiddle-yard, yard and BEMF requirements are retained here deliberately and must not be silently dropped because they fall outside the first amplifier increment.

## Reading conventions

Every bullet is tagged with one of four status markers. Do not collapse them.

| Marker | Meaning |
| --- | --- |
| `[Agreed]` | Agreed requirement / decision. |
| `[To verify]` | Existing or operator-described behavior that must still be verified against source and/or hardware before it is treated as fact. |
| `[Research]` | Open research question or unresolved implementation choice; no decision made yet. |
| `[Deferred]` | Deferred functionality; explicitly out of scope for now. |

Where an exact value (time, threshold, percentage, address, coefficient) appears in this document it is an **unverified example or operator-described context**, not a verified fact. Exact limits and implementation sequencing remain to be established and are recorded under "Open design work".

## Cross-references

- Architecture/boundaries/lifecycle: `docs/architecture.md`, `docs/implementation.md`.
- Firmware toolchain/build/flash baseline: `docs/firmware-toolchain-readiness.md`.
- Recovery & Maintenance System (operator/software/hardware recovery + control transfer, open PO decisions 1–5): `docs/recovery-maintenance-system.md`.
- Koploper/ECoS interface: `docs/koploper-interface.md`, `docs/koploper-internal-state-integration.md`.
- PIC18 amplifier firmware guidance: `TrackAmplifier4.X/AGENT_TRACK_AMPLIFIER.md`, `TrackAmplifier4.X/AGENT_TRACK_AMPLIFIER_COMPACT.md`, `TrackAmplifier4.X/MODBUS_TRACK_AMPLIFIER_MAPPING.md`, `TrackAmplifier4.X/TRACK_AMPLIFIER_STATE_MACHINE.md`.
- Product purpose/scope: `docs/product.md`.

---

## A. Architecture and existing firmware context

- `[Agreed]` One repository and one Project Lead coordinate C#, PIC32 master and PIC18 amplifier work. Existing roles may be instantiated for bounded parallel tasks.
- `[Agreed]` Siebwalde remains a gateway for Koploper, not an independent locomotive-tracking or traffic-management system. It may consume reliable Koploper identity/authority information for integration and diagnosis.
- `[Agreed]` Firmware uses cooperative multitasking. New ramps, retries and state machines must not block Modbus processing.
- `[Agreed]` Existing timers, interrupts, ADC/BEMF/PWM scheduling and execution budgets must be inventoried and understood before changing them (see `docs/firmware-toolchain-readiness.md`).
- `[To verify]` The described Amplifier4 configurations must be verified against source and preserved:
  - **Standalone** — for direct programming/debug.
  - **Combined** — bootloader plus application.
  - **Offset** — application updates through the existing initialization/bootloader path.
- `[To verify]` Backplane selection, runtime address assignment, default address, reset/bootloader entry, checksum identification and startup sequencing are operator-described context that must be verified against source (see `TrackAmplifier4.X/MODBUS_TRACK_AMPLIFIER_MAPPING.md`, `TRACK_AMPLIFIER_STATE_MACHINE.md`).

## B. Global stop and drive behavior

- `[Agreed]` Physical stop controls, Koploper stop and WPF stop represent the same global running prohibition.
- `[Agreed]` It stops the main layout and mountain layout, including manually driven digital amplifiers.
- `[Agreed]` Amplifiers execute a local fast controlled ramp to neutral; normal commands cannot interrupt it.
- `[Agreed]` Release may originate from the agreed controls when physical stop inputs are clear. Releasing a physical button alone must not resume movement.
- `[Agreed]` Local amplifier faults are not cleared by global release.
- `[Agreed]` After release, do not autonomously restore old PWM; follow a fresh valid movement command.
- `[To verify]` Actual Koploper zero/resume behavior after release requires verification.
- `[Agreed]` Three independent per-position parameters: acceleration ramp, normal deceleration ramp and fast-stop ramp.
- `[Agreed]` Normal zero commands use normal deceleration. Direction reversal passes through neutral.
- `[Agreed]` Switch functions and FY shifting remain available during a running prohibition when their own interlocks permit them.
- `[Agreed]` RC yard traction remains outside the main/mountain global stop. Digital-owned transfer tracks remain subject to it.
- `[Agreed]` Distinguish a running prohibition from protections that must stop a mechanical movement itself.

## C. Parameter ownership, storage and test overrides

- `[Agreed]` Amplifier settings belong to a physical position/section; replacement hardware receives those settings.
- `[Agreed]` Loc-specific BEMF settings belong to the locomotive.
- `[Agreed]` Normal operating parameters are downloaded into RAM and read back at initialization. No persistent parameter storage in the amplifier is required.
- `[Agreed]` No drive permission without a valid parameter configuration.
- `[Agreed]` Support complete parameter sets and single-parameter changes during operation.
- `[Agreed]` The amplifier validates values and relationships. Rejection preserves previous values. Any permitted saturation must be explicit and readable.
- `[Research]` Write-result/error information identifying affected parameters and reasons; architecture/register details are to be designed.
- `[Agreed]` Operator-originated failures are reported immediately. Initialization download failures are explicitly logged/reported per amplifier and block its drive permission.
- `[Agreed]` Hard protection defaults are firmware-defined values selected by the Product Owner.
- `[Agreed]` Temporary RAM test overrides are accessible only through an advanced/test facility, not normal operation.
- `[Agreed]` Test overrides may make protection stricter, not relax absolute hardware protection. They are visible/logged and disappear on re-init/power cycle.
- `[Agreed]` Permanent default changes require new firmware.
- `[Agreed]` Distinguish this protection-limit override from the separate, explicitly permitted Siebwalde soft-temperature test override (see E).

## D. Firmware identity and initialization

- `[Agreed]` Assigned firmware normally governs, including downgrade when necessary.
- `[Agreed]` A persistent per-position test setting may skip automatic firmware replacement.
- `[Agreed]` Identification and parameter/capability compatibility checks remain required where technically achievable. Incompatibility blocks only the affected amplifier.
- `[Research]` Design how compatibility is identified; do not assume an existing capability mechanism.
- `[To verify]` Operator-described startup sequence (verify against current firmware and preserve relevant behavior):
  - reset released → red/error while awaiting addressing;
  - address assigned → red off;
  - parameter download → orange steady;
  - standby → orange flashing, all monitoring/communication active, bridge floating;
  - enable after Siebwalde system readiness → green, initially neutral.
- `[Agreed]` Healthy amplifiers may proceed while a failed section remains blocked, if Siebwalde determines remaining operation is permissible.
- `[Agreed]` Warning levels show orange; enabled operation may retain green.
- `[Agreed]` Active trip/error shows red and green off.
- `[Agreed]` Status LEDs and software state must agree. Separate Tx/Rx indicators retain their existing message-rate-based behavior.
- `[Agreed]` Distinguish current fault, acknowledged fault, standby awaiting release and retained fault history.

## E. Local protection, measurement validity and recovery

- `[Agreed]` Immediate electrical protection stays local; recovery supervision/timing belongs primarily in Siebwalde.
- `[Agreed]` Normally attempt automatic recovery, bounded by both a maximum attempt count and a maximum elapsed recovery time; the first limit reached escalates to operator recovery.
- `[Agreed]` Require a configurable stable-success interval. Retrips during that interval remain in the same recovery budget.
- `[Agreed]` Preserve actual measured occupancy separately from fault-forced/effective occupancy.
- `[Agreed]` Floating means effective occupied, because the current voltage-drop occupancy method cannot operate without output voltage.
- `[Agreed]` Remove forced occupancy only when valid detection and all applicable release conditions permit it.
- `[Agreed]` Request a targeted Koploper locomotive stop where a reliable identity exists. Never invent identity.
- `[Research]` Investigate whether that stop can remain effective against subsequent commands. Reassert as necessary while blocked, without message storms.
- `[Agreed]` Unknown authority means stay neutral/blocked and occupied.
- `[Agreed]` TrackControl diagnostics provide cause, amplifier and Siebwalde states, recovery progress, confirmation, test and release actions.
- `[Agreed]` Speech notifications are supplementary and rate-limited.

**Hard overcurrent (current behavior):**
- `[To verify]` Hard overcurrent immediately floats the bridge and reports the fault.
- `[To verify]` The amplifier remains off until an explicit recovery/test command; Siebwalde schedules bounded attempts.
- `[To verify]` After escalation, operator confirmation can acknowledge the fault and show orange flashing while still floating. Start/test is a separate action.
- `[Agreed]` Active hard protections remain unoverrideable.

**Temperature:**
- `[Agreed]` Separate external analog temperature measurement from the H-bridge digital temperature-trip signal.
- `[Agreed]` Analog soft thresholds, warning/stop policy, cooldown, automatic recovery and temporary operator test override reside in Siebwalde.
- `[Agreed]` A bounded, visible/logged soft-temperature override may allow an earlier manual test while hard protections remain effective.
- `[Agreed]` A digital H-bridge temperature trip requires operator handling during the running session, after sufficient cooldown. No override while the hard block is active.
- `[Research]` Define cooldown thresholds for manual testing versus automatic operation.
- `[Agreed]` A full successful re-initialization may clear the historical latch if the trip is absent, temperature is sufficiently low and all checks pass. History remains; old commands do not resume.

**vBus voltage:**
- `[Agreed]` Five regions: too high, high, normal, low, too low.
- `[Agreed]` Hard upper/lower violations immediately float the bridge, without a ramp.
- `[Agreed]` Hard boundaries are local firmware values selected by the Product Owner.
- `[Agreed]` Soft warnings/earlier intervention belong in Siebwalde.
- `[Agreed]` Severe high voltage causes a prominent alarm including speech.
- `[To verify]` Verify regulator supply and brown-out configuration/behavior; reset does not prove a completed ramp.

**Measurement validity:**
- `[Agreed]` Value and validity are separate; zero, near-zero or negative is not automatically invalid.
- `[Agreed]` Distinguish valid, expected temporarily unavailable and invalid due to measurement failure.
- `[Agreed]` Invalid required measurement → float, block drive and force occupied.
- `[Agreed]` Apply suitable digital qualification/debounce and analog filtering/hysteresis.
- `[Agreed]` Define latency per signal; immediate overcurrent/hard-temperature protection must not be delayed beyond its required response. Exact values remain to be established.

## F. Communication recovery and restart

- `[Agreed]` First retry direct communication at the existing address.
- `[Agreed]` On response, verify identity/status and parameters; reload when appropriate.
- `[Agreed]` If unsuccessful, select only that backplane position, assign its address and initialize that amplifier.
- `[Agreed]` Bound attempts; visualize and log loss, retries, stages, results and durations.
- `[To verify]` Do not assume hardware supports hot-swap without verification.
- `[Agreed]` Automatic movement resumption requires reliable current authority/identity and a usable current command.
- `[Research]` Investigate requesting/triggering a fresh Koploper speed value, since unchanged speeds may not be resent.
- `[Agreed]` Retained speed alone is insufficient in normal mode.
- `[Agreed]` If validity cannot be established, remain blocked/occupied and require Koploper/operator recovery.
- `[Agreed]` Provide an advanced integration-test policy permitting last-known-speed ramp-up after otherwise successful local recovery. It cannot override global stop, active protection or mandatory operator blocks.
- `[Agreed]` This advanced policy resets to the safe default at every Siebwalde restart.
- `[Agreed]` Restart retains settings/history, not drive permission or manual speed.
- `[Agreed]` Re-initialization reassesses current conditions; historical faults alone do not prevent startup after conditions have genuinely recovered.

## G. BEMF — mountain layout only initially

- `[Agreed]` Main layout remains ordinary PWM for now.
- `[Agreed]` Mountain layout uses per-loc BEMF properties in Siebwalde, initially sharing common defaults.
- `[Agreed]` Preferred behavior: Koploper continuously controls desired speed; local BEMF corrects load from startup through braking where stable.
- `[Agreed]` Separate externally requested setpoint and internally corrected PWM; expose both.
- `[Research]` Investigate a bounded PWM startup followed by bumpless BEMF transfer if needed.
- `[Agreed]` Maintain BEMF during normal braking where usable; smoothly fall back to PWM at low/unusable measurement conditions.
- `[Research]` Fallback design if full-range operation is unstable: capture/hold one cruising BEMF target after startup. This remains a research option, not the selected implementation.
- `[Agreed]` Recover the prior Python PI/PID experiment and inspect measurement sign/scaling, saturation and control behavior.
- `[Agreed]` Bound startup assistance in magnitude/time; do not increase PWM indefinitely when BEMF is absent.
- `[Agreed]` After assistance expires, retain the current Koploper-requested PWM without extra boost.
- `[Agreed]` If startup still fails and Koploper does not stop, Siebwalde intervenes after the verified Koploper waiting time plus configurable margin. (The discussed ~60 s and ~1 s are unverified examples.)
- `[Agreed]` During running, loss of usable BEMF causes fallback to requested PWM with diagnostic/logging, not automatic stop solely for this condition.
- `[Agreed]` Restore BEMF automatically after a stability interval without PWM discontinuity.
- `[Agreed]` Distinguish missing usable BEMF from an actual invalid required measurement.
- `[Agreed]` Each mountain amplifier serves its own side. At the middle passing station, relay transfer occurs only when both trains are in position, stationary and relevant outputs floating.
- `[Agreed]` Rebind loc parameters and clear stale controller history on locomotive reassignment.
- `[Deferred]` Detailed route exceptions and relay sequencing remain later work.

**Mountain lighting:**
- `[Research]` Prefer Koploper light commands and timing; investigate actual support.
- `[Agreed]` Neutral PWM may keep lamps on while stationary; floating extinguishes them.
- `[Agreed]` Light-on while stopped requests neutral output. Light-off floats only while stopped; it does not alter traction during running.
- `[Agreed]` A valid departure command re-enables output, subject to all blocks.
- `[Agreed]` Add a Siebwalde station timer only if Koploper cannot provide the intended behavior.

## H. Trace and diagnostics storage

- `[Agreed]` Separate telemetry/events from component logging, but correlate timestamps/identifiers.
- `[Agreed]` Record values with validity, requested/corrected output, state, faults, authority/ownership changes, configuration writes, recovery and operator actions.
- `[Agreed]` Continuous compact history, automatic pre/post-fault windows, and manually enabled full-detail sessions for fixed duration or until stopped.
- `[Research]` Storage format (possibly SQLite) is an architecture choice.
- `[Agreed]` Do not block control/protection on logging.
- `[Agreed]` Log/storage failure never stops the layout; report incomplete recording.
- `[Agreed]` Configurable retention, storage budget and free-space limits.
- `[Agreed]` Automatically clean only eligible records; traces marked "keep" must never be automatically deleted.
- `[Agreed]` If protected traces exhaust space, stop recording with an alarm rather than deleting them or stopping trains.

## I. Main/mountain manual operation

- `[Agreed]` Independently selectable manual/automatic mode per main and mountain layout.
- `[Agreed]` Manual takeover only from standstill, with automatic commands inhibited; it must work without Koploper.
- `[Agreed]` Select one or multiple sections/amplifiers, not autonomous loc-following.
- `[Agreed]` Selection change resets the visible hand throttle to zero; deselected outputs neutral then floating; new selection requires a fresh command.
- `[Agreed]` Manual control survives page changes for parallel FY work. Show active ownership/selection/setpoint.
- `[Agreed]` Unexpected Koploper loss stops automatically controlled areas; already manual areas remain usable.
- `[Agreed]` Return to automatic from standstill, valid Koploper connection and explicit operator confirmation of loc/block alignment. No automatic return.

## J. Fiddle yard

`[Agreed]` Two mutually exclusive implementations:
1. The existing FY implementation, including its manual and autonomous storage/dispatch modes, used only with the main layout in Siebwalde manual mode.
2. A separate Koploper-driven implementation: Koploper owns traffic; Siebwalde translates virtual switch routes to FY track positions, handles I/O, interlocks and visualization. Temporary manual operation stays within this new implementation.

**Existing mode:**
- `[Agreed]` Preserve intake priority over departure.
- `[Agreed]` Bridge digital amplifier occupancy into existing FY input logic, OR with relevant physical target inputs.
- `[Agreed]` Bridge FY occupancy overrides into Siebwalde amplifier effective occupancy.
- `[Agreed]` Preserve raw sources and prevent self-sustaining feedback loops.
- `[Agreed]` Unknown required occupancy source means occupied/blocked.
- `[Agreed]` No normal forced-free override is needed.
- `[Agreed]` Use this implementation as a reference for digital occupancy verification.

**Koploper mode:**
- `[Agreed]` Separate top/bottom route targets.
- `[Research]` Convert virtual turnout combinations into a valid requested FY track; exact count/mapping requires inspection.
- `[Agreed]` Local traction interlock requires requested position reached, motion stopped and electrical coupling confirmed.
- `[Research]` Investigate Koploper readiness gating; a travel-time delay alone is not alignment proof.
- `[Agreed]` A started movement completes before a later requested movement, except safety intervention.
- `[Agreed]` Keep only the latest pending valid target per layer.
- `[To verify]` Startup initializes/validates actual position (reportedly track 1); no drive until position and selected route agree.
- `[Agreed]` Treat Koploper turnout initialization explicitly; partial commands must not unintentionally trigger arbitrary moves.
- `[To verify]` Optical sensor around 70% provides qualified stop feedback; no brake-learning is preferred here. (The 70% figure is operator-described, not measured.)
- `[Research]` Determine how the desired fast stopping differs from an ordinary normal-ramp zero command.
- `[Agreed]` End overshoot sensor may be ORed with following-section occupancy toward Koploper; preserve distinct diagnostics.
- `[Agreed]` Koploper, not the new FY logic, handles an overrun train.
- `[Agreed]` Entry/exit barrier sensors prevent movement when a train bridges the boundary.
- `[To verify]` Verify hardware/PIC18F-target/C# latches and preserve their protection in the new implementation.

**FY recovery:**
- `[Agreed]` One shared Ethernet target serves both layers; connection loss blocks both.
- `[Agreed]` Rail traction is controlled through the track amplifiers, not the FY target itself.
- `[Research]` Investigate local behavior of an already moving FY on link loss; later target-firmware improvement is expected.
- `[Agreed]` After communication recovery, automatic re-init/reposition is allowed if boundaries are clear and all safety conditions hold.
- `[Agreed]` Restore the last valid requested route; absence of a known route requires blocking, diagnostics and operator synchronization.
- `[Agreed]` Recovery and release are assessed independently per layer.
- `[Agreed]` Safety intervention during motion requires operator confirmation, clear boundary sensors and re-init; restore the current valid target, then explicit operator confirmation of Koploper synchronization.
- `[Agreed]` FY implementation switching must work without restarting Siebwalde, TrackController or future YardController.
- `[Agreed]` Safely stop and dispose the old implementation's tasks, connection and subscriptions before starting the other. Garbage collection alone is insufficient.
- `[Agreed]` Failed teardown blocks switching.

## K. Yard and main-layout train transfer

- `[Agreed]` YardController remains standalone RC-operated, with shared WPF control for existing route/track-power functions.
- `[Agreed]` Do not add autonomous yard traffic management or a new automatic route queue policy.
- `[Agreed]` Yard RC traction remains outside the main/mountain running prohibition; switches remain operable for recovery.
- `[Agreed]` Separate intake and export tracks allow simultaneous independent transfers.
- `[Agreed]` Operator requests a freight train via RC; WPF shows the request. Operator performs the Koploper train-selection/routing steps manually.
- `[Agreed]` After Koploper stops an arriving train and completes extraction, operator explicitly transfers electrical ownership to RC via RC/WPF. Route completion alone does not switch power.
- `[Agreed]` For export, operator places/stops the train, explicitly hands the track to the digital amplifier, then identifies/releases it in Koploper.
- `[Agreed]` Digital amplifier must be neutral at electrical handover. No RC-throttle-zero interlock was requested.
- `[To verify]` Relay hardware already prevents cross-connection; verify actual implementation.
- `[Agreed]` Active transfer ownership cannot be reclaimed by ordinary RC toggling.
- `[Agreed]` Deactivating a request requests cancellation, not immediate relay switching.
- `[Agreed]` Unstarted requests may cancel safely; active transfers require controlled cancellation.
- `[Agreed]` Export may return to RC through WPF while not taken away by Koploper.
- `[Agreed]` If a loc ID is assigned, operator first removes the train from Koploper; confirmed absence of assignment/authority and standstill permit return.
- `[Agreed]` WPF provides "Transfer to main layout" and "Take back to yard", actual owner, transition state and block reason.
- `[Agreed]` Digital-owned track displays normal amplifier diagnostics; RC ownership has explicit labeling, not color alone.
- `[Agreed]` Yard continues standalone without Siebwalde heartbeat, but ownership changes are blocked. Existing owner is retained on connection loss.
- `[Agreed]` On reconnect, reconcile state; incomplete transfers require fresh operator confirmation.
- `[To verify]` Reset enforces a fixed relay default; the desired future hardware direction is de-energized = digital/Siebwalde-Koploper owner. Verify current wiring.
- `[Agreed]` Digital default does not grant movement.
- `[Research]` Investigate reset during RC/digital operation before claiming safe handover.
- `[Agreed]` Ownership discrepancies with Koploper block new transfers pending operator reconciliation.

## L. Coordinated Siebwalde shutdown

- `[Agreed]` Block new commands.
- `[Agreed]` Stop digital train traction and safely stop FY/yard mechanisms as applicable.
- `[Agreed]` Verify stop, then reset amplifiers and close communication/logging.
- `[Agreed]` Respect the standalone RC-yard boundary; document which shutdown actions are technically available.
- `[Agreed]` Missing stop confirmation blocks normal exit, raises an alarm and triggers available safe fallback.
- `[Agreed]` Forced exit requires explicit operator action and reports unknown hardware states.
- `[Agreed]` Process exit does not prove physical neutralization.

---

## Open design work / unresolved items

The following remain to be established (they are deliberately left open, not treated as decided):

- Actual Koploper APIs and behavior (light commands/timing, speed re-send on unchanged speed, stop persistence against later commands, turnout initialization and readiness gating).
- Hardware facts (backplane/address/reset/bootloader/checksum sequencing, hot-swap capability, relay wiring and default state, barrier/overshoot sensor placement and thresholds, regulator/brown-out behavior).
- Exact time and threshold values (ramp rates, cooldown thresholds, vBus hard/soft boundaries, recovery attempt/time budgets, stable-success and stability intervals, Koploper waiting time and margin, message-rate and debounce/filter/latency budgets).
- Reset/heartbeat timing across Siebwalde, TrackController, YardController and FY targets.
- BEMF validation (measurement sign/scaling, saturation, control stability) and the recovered PI/PID experiment.
- Interface/register design (parameter set layout, write-result/error reporting, compatibility/identity mechanism, trace storage format).
- Ownership boundaries and state reconciliation between Siebwalde, Koploper, RC yard and FY.
- Implementation phasing and sequencing across the first amplifier increment and the deferred FY/yard/BEMF increments.

## Residual ambiguities (explicit, not re-interviewed)

- Exact count and mapping of virtual turnout combinations to a valid FY track (J, Koploper mode).
- The precise semantics of "fast stopping" vs "ordinary normal-ramp zero" for the FY optical-sensor stop (J).
- Whether the optical sensor ~70% figure is a fixed spec or a tuning starting point (J).
- The concrete cooldown thresholds separating manual test from automatic operation (E).
- The boundary between "missing usable BEMF" and "invalid required measurement" in edge conditions (G).

## Explicitly not implied

This document does not authorize an implementation increment, does not reopen closed increments (KIS-09/KIS-10, estop-commloss), and does not silently drop future fiddle-yard, yard, or BEMF requirements because they sit outside the first amplifier increment.
