# Handoff

## Latest Session (2026-09-20, stop-reachability review FAIL corrected: physical device classification)

Branch `feature/safety-stop-reachability`, implementation commit `be05c37`, harness commit `15fcf7d`, on top of the accepted fix `66d75d0` and harness adaptation `c13d9fc`. The independent Integrator review verdict was `SOFTWARE REVIEW FAIL`: the core stop-reachability architecture was accepted, but the amplifier-centric fallback included backplane/configuration slaves `51..55`, which must never receive track-amplifier PWM semantics. **This session corrects that defect and the secondary safety weaknesses in software only. No live hardware was started and no physical validation is claimed.**

### Exact root cause of the 51..55 inclusion

`TrackAmplifierHardwareBackend.GetKnownPhysicalAmplifiers()` selected detected hardware with `amplifier.SlaveNumber != 0 && amplifier.SlaveDetected != 0`. The `trackAmpItems` container holds slaves `0..55`, so the backplane/configuration modules `51..55` (which are detected on the same bus) entered the safety target set. `NeutralizeAmplifiers` then queued the track-amplifier neutral value `399` (`0x018F`) to them. That is invalid: `TrackBackplane2.X/main_proto_backplane.c` `Set_Amplifier()` treats HoldingReg0 `ActValue` as a configuration/enable word for amplifier IDs, so writing `399` would alter configuration bits.

### Authoritative physical device classification

New `TrackAmplifierAddress` (Core) is the single definition: `IsTrackAmplifierAddress` = `1..50`; `IsBackplaneConfigurationSlave` = `51..55`. Verified against source: `TrackApplicationVariables.MaxAmplifiers`, `TrackAmplifierWriteData.SlaveNumber` doc, `TrackAmplifierPageViewModel` ("1..50 amplifiers", "51..55 backplane modules"), `FlashFwTrackamplifiersStep` and `EthernetTargetDataSimulator` (`< 51`), `TrackControlMain` ("data[0] = SlaveAddress (1..50)"), and `TrackBackplane2.X/main_proto_backplane.c` `Get_ID()` (`MODBUS_ADDRESS = 50 + ID pin`). The source confirmed the intended ranges; no contradiction was found.

### Exact correction and central enforcement

- `TrackApplicationVariables.SetDesiredAmplifierControl` — the lowest shared point through which every HR0 command passes (loco, look-ahead, safety, manual page) — refuses any non-track address. No track-amplifier PWM/neutral semantics can reach `51..55` through any path.
- `TrackAmplifierHardwareBackend.GetKnownPhysicalAmplifiers` returns only legitimate track amplifiers; `NeutralizeAmplifiers` rejects non-track addresses (returned as not commanded, no write); `SetLocoSpeed`, `ApplyLookAhead`, `SetPower(false)` and `InitializeDefaultPwmSetpoints` filter topology/detected amplifiers to the track class; `RecordCommand` never records a non-track address.
- `AmplifierCommandTracker` refuses to retain a non-track address (defense in depth).
- `EcosHardwareStopSink` re-validates every target against `TrackAmplifierAddress` before it enters a set or a write.

### Physical device type vs operational group vs block mapping

Three independent concepts are now explicit. (1) Physical device type (`TrackAmplifierAddress`) decides whether PWM/HR0 semantics are legal. (2) Operational group / safety domain (`TrackAmplifierGroups`, new; `MainRailway` / `MountainRailway` / `Spare`) decides the domain; a legitimate track amplifier with no configured group stays `Unassigned` and is never inferred as main railway from its address, its detection, or its `BlockTopology` membership. (3) Logical block mapping (`BlockTopology`) only routes blocks to amplifiers. **No operational group/domain model existed before this session; it is introduced now**, backed by the new `TrackAmplifierGroupsConfig` setting (default `main: ; mountain: ; spare: `, i.e. unconfigured) and `CoreConfiguration.BuildTrackAmplifierGroups()`. The settings-page editor for it is deliberately deferred (next architecture task).

### Proposed representation for MainRailway / MountainRailway / Spare

`TrackAmplifierGroups.Parse`/`Create` accept explicit address collections; `Classify(address)` returns the group; `AllConfigured` and `GetGroup(group)` are available. Backplane addresses, invalid addresses and duplicates are recorded in `Errors` and ignored. A `Spare` is still physically a `TrackAmplifier` (tested) and is never inferred from detection state.

### Which safety actions operate on which group/domain

- `EcosHardwareStopSink.StopLoco` — the loco's retained targets, across whatever domain those amplifiers belong to. It is not domain-scoped.
- `EcosHardwareStopSink.StopLayout` (strongest physical emergency, used by `ControlSafetyGuard` escalation) — every legitimate track amplifier the control path knows about (detected hardware plus configured topology) plus every retained target. It is not narrowed to one domain and not expanded to configured-but-undetected group members.
- Normal logical `SetPower(false)` — unchanged: topology-only, mapping-oriented.

### Pending Product Owner / architecture decision (NOT decided here)

The cross-domain emergency policy is **PENDING**: whether a main-railway failure (`SafetyAction.StopLayoutEscalated`) must also neutralize the mountain railway, and whether a configured `Spare` is included in normal layout stop / main fallback / mountain fallback / strongest emergency. The current strongest emergency already targets all known legitimate track amplifiers (pre-existing accepted behaviour); it was deliberately not changed into a domain-scoped action, and no new cross-domain behaviour was added.

### Amp 6 current classification / behaviour

Amp 6 is a detected, unmapped prototype. Physical class: legitimate `TrackAmplifier` (`1..50`). Operational group: `Unassigned` by default; it is never silently classified as `MainRailway` from detection. Because it is detected, the strongest physical neutralization still reaches it (existing behaviour). The harness dry-run configures it as `MountainRailway` only to demonstrate that the group is independent of the physical class and the block mapping.

### Stale / unavailable command semantics

`IAmplifierNeutralizer.GetAmplifierCommunicationState` exposes `Invalid` / `NeverSeen` / `Stale` / `Fresh` (detected **and** fresh via `TrackAmplifierDataFreshness`). A required target is only established when a neutral command was accepted for a legitimate track amplifier **and** its state is `Fresh`. A stale or never-seen required target is reported as unresolved, keeps its retained target, and makes `SafetyStopResult.Succeeded` false; the guard keeps the latch and emits `CommandNotApplied`. A topology-only amplifier that was never seen remains a best-effort extra (neutral is still queued) and is not a false failed target. No hardware acknowledgement is invented.

### Exact definition of neutralization success after correction

Commanded = a neutral setpoint was accepted at the pending-write queue boundary for a legitimate, fresh track amplifier. `Succeeded` = every required target is Commanded and the backend is available. Transmission and observed physical neutralization are not claimed (the protocol provides no acknowledgement). Requested / Commanded / Observed remain distinct.

### Tracker clearing semantics

A retained target is cleared only after a neutral command is accepted for a fresh legitimate track amplifier (`RecordCommand` requires `Fresh`). Moving block, losing the mapping, or a stale/queued neutral never clears it. `StopLayout` clears all outstanding only when every required target is established; otherwise it keeps the unestablished ones.

### Manual `SetAmplifierControl` conclusion

Investigated: `SiebwaldeApplicationModel.SetAmplifierControl` queues an HR0 command directly and is not recorded in `AmplifierCommandTracker`, so the invariant "every commanded non-neutral track amplifier remains represented until neutralized" does not hold for loco-scoped tracking. The manual page is runtime but is an operator/diagnostic path outside loco ownership; the backplane boxes in `TrackAmplifierPage.xaml` expose no PWM/EmoStop controls. Correction: documented as outside loco ownership; the strongest physical neutralization (`StopLayout`) reaches a manually commanded, detected legitimate track amplifier (tested), while a loco-scoped stop cannot cover it. Bringing the manual path under a non-loco tracker owner at the shared choke point is a proposed follow-up needing Project Lead approval (recorded in `docs/backlog.md`).

### Ownership-transfer status

Preserved. The tracker remains per-loco/per-amplifier; a later legitimate command transfers ownership; a stale owner cannot clear another loco's target. Added test `OwnershipTransfer_IsPerAmplifier_AndDoesNotCrossIntoAnotherGroup` proves a loco commanding a mountain-railway amplifier does not affect another loco's main-railway target merely because the hardware type is identical.

### Startup / restart / reset safety conclusion (reported, not hidden)

Source: `TrackAmplifier4.X/regulator.c` `REGULATORxINIT()` sets `PWM3_LoadDutyValue(399)` and `PetitHoldingRegisters[HR_PWM_COMMAND].ActValue |= 399` at amplifier startup — a per-amplifier firmware default. C# initialization (`SetDefaultPwmSetpointsStep`) only sets the in-memory observed image to `400` and never sends neutral `399`; C# never observes neutral before allowing movement. The master-reset physical effect is not fully source-verifiable from the committed C#/firmware (`ControlCore_Update`/`Ramp_Update` are referenced in `main.c` but not defined in the tracked tree). **Conclusion: there is a partial per-amplifier firmware guarantee but no C#-established or C#-observed startup neutral guarantee.** Reported as a separate safety architecture gap in `docs/backlog.md`; firmware was not modified.

### Files changed

- Added: `SiebwaldeApp.Core/Model/TrackApplication/Control/TrackAmplifierAddress.cs`, `.../TrackAmplifierGroups.cs`, `SiebwaldeApp.Core.Tests/AmplifierClassificationAndSafetyDomainTests.cs`.
- Modified: `AmplifierCommandTracker.cs`, `IAmplifierNeutralizer.cs`, `TrackApplicationVariables.cs`, `CoreConfiguration.cs`, `CoreSettings.settings`/`CoreSettings.Designer.cs`, `app.config`, `App.config`, `EcosHardwareStopSink.cs`, `TrackAmplifierHardwareBackend.cs`, `TrackControlHost.cs`, `TrackControlIntegration.cs`, `StopReachabilityTests.cs`, `TrackApplicationVariablesTests.cs`.
- Harness (separate revision `15fcf7d`): `SiebwaldeApp.StopReachabilityHarness/Program.cs`, `StopReachabilityHarness.cs`.

### Tests and checks (software only)

- `dotnet build SiebwaldeApp.sln -t:Rebuild` -> **0 errors, 175 warnings** (baseline 175; no new warnings).
- `dotnet test SiebwaldeApp.sln` -> **322/322 passed** (was 297; +25).
- `dotnet build SiebwaldeApp.sln -c Release -t:Rebuild` -> **0 errors, 175 warnings**.
- `dotnet test SiebwaldeApp.sln -c Release --no-build` -> **322/322 passed**.
- Harness: `dotnet build SiebwaldeApp.StopReachabilityHarness/SiebwaldeApp.StopReachabilityHarness.csproj` -> **0 errors, 0 warnings**.
- Harness dry run (`--dry-run --script`): Stage 3 still leaves amp 1 non-neutral (416, the defect precondition); Stage 5 reports `GUARD_ACTION=StopLoco SINK_RESULT=True` with `AMP1=399 AMP3=399`; the new classification check reports `slave 1/50 = track amplifier`, `slave 51/52/55 = backplane`, groups `MainRailway {1,3,4}` / `MountainRailway {6}` / `Spare {50}` / `Unassigned {51}`, `GetKnownPhysicalAmplifiers=[1,2,3,4,5,6,50]`, and `BACKPLANE SAFETY CHECK: PASS`. The harness does not perform the neutralization itself. Software-only evidence, not a physical result.

### New tests

`AmplifierClassificationAndSafetyDomainTests` (24 cases) covers the physical class (`1`/`50` in, `51`/`52`/`55`/`0`/`56`/`-1` out), backplane detection never becoming a target or producing an HR0 write, direct neutralizer refusal, a topology mapping a backplane slave being ignored, an unmapped track amplifier, a spare still classified as a track amplifier, operational grouping independent of class/mapping, no main-railway inference from `BlockTopology`, group parsing rejecting backplane/duplicates, never-seen and detected-but-stale required targets, a layout stop with one stale amplifier, the manual path, ownership transfer across groups, and startup device-class scoping. `TrackApplicationVariablesTests` adds a backplane-untouched startup test; `StopReachabilityTests` seeds detected/fresh amplifiers and keeps the accepted A->B, look-ahead, mapping-loss, failure-reporting and escalation coverage.

### Traceability

The historical physical-defect harness commit `03f5221` is untouched. The post-fix harness revision `15fcf7d` is the revision any future physical validation must cite together with the branch HEAD.

### Explicitly pending

- **Independent software re-review of `be05c37`/`15fcf7d` (Integrator).**
- **Physical post-fix verification on the layout (Integrator). The fix is not physically validated.**
- Product Owner decision on the complete group/domain configuration and the cross-domain emergency policy.
- Startup/restart neutral guarantee (separate safety architecture gap).
- Optional stronger guarantee for the manual `SetAmplifierControl` path.

### Uncertainty for the Integrator to check

- Confirm the classification test genuinely fails against `66d75d0` (it does: the reviewed `GetKnownPhysicalAmplifiers` returns `51`/`52`/`55`).
- Confirm the chosen required-target rule: a topology-only never-seen amplifier is a best-effort extra, while a detected-but-stale amplifier is a required, failed target. Decide whether an absent mapped amplifier should be a failure instead.
- Confirm that the default unconfigured grouping is acceptable until the Product Owner supplies the real MainRailway/MountainRailway/Spare assignment.

## Latest Session (2026-09-20, stop-reachability safety fix implemented and software-verified)

Branch `feature/safety-stop-reachability`, production implementation `66d75d0`, harness adaptation `c13d9fc`, on top of the defect confirmation `488ad45`. The confirmed physical safety defect is **fixed in software and software-verified**; **no live hardware was started and no physical validation is claimed**.

### Root cause (confirmed by the physical evidence)

`EcosHardwareStopSink.StopLoco` resolved its target purely from the locomotive's **current** block (`TrackAmplifierHardwareBackend.SetLocoSpeed`), discarded the backend return and always returned `true`; `ControlSafetyGuard` discarded the result. A normal A -> B transition never neutralizes the vacated amplifier, and look-ahead can command a second amplifier, so a previously commanded physical output became unreachable. Live evidence (harness `03f5221`): after block 1 -> block 3, amp 1 stayed at HR0 416; the loco stop neutralized only the resolved amp 3; the sink returned `True` with no failure diagnostic.

### The fix (selected architecture)

A retained **commanded-actuator ownership** model, not a single `lastAmplifier`:

- `AmplifierCommandTracker` (Core) keeps the **set** of physical amplifiers each locomotive last commanded non-neutral. Add on a concrete non-neutral physical command (the queued HR0 setpoint); remove only after a neutral command is successfully issued; never remove because the loco moved, the route changed or the mapping disappeared. Ownership transfers to the most recent commanding loco, so one loco's stop cannot clear another loco's outstanding target; a deliberate global neutralization clears across locos.
- `TrackAmplifierHardwareBackend` records current-block **and** look-ahead commands in the tracker and implements the new `IAmplifierNeutralizer` (`NeutralizeAmplifiers`, `GetKnownPhysicalAmplifiers`), independent of the block mapping and topology.
- `EcosHardwareStopSink.StopLoco` runs the existing loco path (`SetLocoSpeed(address, 0, 0)`) **and then** neutralizes every retained outstanding target. It returns a `SafetyStopResult`; `ISafetyStopSink` now returns that result for both stop methods.
- Stop success = **all required neutral commands were accepted for concrete physical amplifiers**; it never claims observed neutralization (observed state stays separate).
- `ControlSafetyGuard` inspects the result: on an incomplete loco stop it emits `CommandNotApplied` (or `BackendUnavailable`), keeps the latch, and escalates to amplifier-centric layout neutralization (`SafetyAction.StopLayoutEscalated`); a failed escalation emits a second diagnostic and the latch remains. The latch is never cleared because a stop call returned.
- Detected-but-unmapped policy: the fallback target set is detected hardware (`SlaveDetected`) **plus** configured topology **plus** all retained targets, so installed amp 6 is included. `SetPower(false)` keeps its topology-only logical semantics; the new primitive is used explicitly by the safety escalation.

### Files changed

- Added: `SiebwaldeApp.Core/Model/TrackApplication/Control/AmplifierCommandTracker.cs`, `.../Control/IAmplifierNeutralizer.cs`, `.../Diagnostics/SafetyStopResult.cs`, `SiebwaldeApp.Core.Tests/StopReachabilityTests.cs`.
- Modified: `Diagnostics/ISafetyStopSink.cs`, `Diagnostics/DiagnosticTypes.cs` (+`SafetyAction.StopLayoutEscalated`), `Integration/ControlSafetyGuard.cs`, `Integration/EcosHardwareStopSink.cs`, `Integration/TrackAmplifierHardwareBackend.cs`, `Integration/TrackControlHost.cs`, `Integration/TrackControlIntegration.cs`, and the test fakes in `DivergenceAndSafetyTests.cs`, `SimpleEcosBackendDirectionStateTests.cs`, `SimpleEcosBackendSpeedNormalizationTests.cs`.
- Harness (separate post-fix revision `c13d9fc`): `SiebwaldeApp.StopReachabilityHarness/HarnessSupport.cs`, `StopReachabilityHarness.cs`.

### Tests and checks

- `dotnet build SiebwaldeApp.sln -t:Rebuild` -> **0 errors, 175 warnings** (baseline 175; no new warnings).
- `dotnet test SiebwaldeApp.sln` -> **297/297 passed** (was 280; +17 `StopReachabilityTests`).
- `dotnet build SiebwaldeApp.sln -c Release` -> **0 errors, 175 warnings**.
- `dotnet test SiebwaldeApp.sln -c Release --no-build` -> **297/297 passed**.
- Harness: `dotnet build SiebwaldeApp.StopReachabilityHarness/SiebwaldeApp.StopReachabilityHarness.csproj` -> **0 errors, 0 warnings**.
- Harness dry run (`--dry-run --script`, recording comm client, no socket, no hardware): Stage 3 still leaves amp 1 non-neutral (416, the defect precondition), and Stage 5 now reports `GUARD_ACTION=StopLoco SINK_RESULT=True` with `AMP1=399 AMP3=399` (amp 1 was reached through the retained target). This is software-only evidence, not a physical result.
- New tests cover: A -> B orphaned actuator (amp 1 + amp 3 neutralized), mapping loss (unmapped and null block), look-ahead two outstanding targets, detected-but-unmapped amp 6 in the layout fallback, incomplete loco stop not reported as success, missing backend, guard escalation and failure diagnostics, latch retention, multiple-loco isolation, ownership transfer, and the normal stop.

### Explicitly pending

- **Physical post-fix verification on the layout (Integrator). The fix is not physically validated.** The historical defect reproduction with harness `03f5221` remains valid and untouched; the adapted harness `c13d9fc` is the post-fix validation revision.
- Asynchronous send failure and observed physical neutralization are still not synchronously observable; the tracker tracks commanded state, not confirmed physical state.

### Uncertainty for the Integrator to check

- With the new contract a loco-scoped stop that cannot resolve any physical target now escalates to the amplifier-centric layout neutralization and reports `SafetyAction.StopLayoutEscalated`. Verify this is the intended operator-visible behaviour on the layout.
- Confirm that commanding neutral to a detected-but-unmapped amplifier (for example amp 6) is accepted by the master/amplifier chain.
- The tracker is process-lifetime state; verify the interaction with a Koploper `set(1,stop)` (which clears mapped targets through `SetPower(false)`) and with a master reset/reinitialization.

## Latest Session (2026-09-20, live stop-reachability validation - DEFECT CONFIRMED)

Branch `feature/safety-stop-reachability`, live-test HEAD `a614efc`, harness commit `03f5221`. Targeted operator-in-the-loop live validation using the committed harness `SiebwaldeApp/SiebwaldeApp.StopReachabilityHarness/`. **No production code, firmware or configuration was changed; tracked tree clean throughout.**

### Traceability
`Harness commit 03f5221 was used for the live physical validation.` Branch `feature/safety-stop-reachability`, live-test HEAD `a614efc`, executable `SiebwaldeApp\SiebwaldeApp.StopReachabilityHarness\bin\Debug\net8.0-windows7.0\SiebwaldeApp.StopReachabilityHarness.exe`, command `--live --ecos-id 1001 --address 2 --protocol DCC28 --from 1 --to 3`. Evidence: `Logging\stopreach-live-20260920-run2.*`. Harness PID 21964 (run 2), driver 10400, window `SIEBWALDE STOP-REACHABILITY LIVE`. (Run 1 hung at init because the master ignored the one-shot `CLIENT_CONNECTION_REQUEST`; after an operator master reset, the full production init pipeline completed.)

### Results (amplifier 1 = motor, amplifier 3 = no motor)
| Stage | Logical block | Amp 1 commanded/observed | Amp 3 commanded/observed | Physical |
| --- | --- | --- | --- | --- |
| Stage 0 baseline | - | -/399 | -/399 | stopped |
| Stage 2 `speedstep[1]` | 1 | 416/416 | none/399 | motor slow forward |
| Stage 3 transition 1->3 (no command) | 3 | none/**416** | none/399 | motor still forward |
| Stage 4 `speedstep[1]` | 3 | none/**416** | 416/416 | motor still forward |
| Stage 5 real loco-scoped safety stop | 3 | **none/416** | 399/399 | motor still forward |
| Layout stop fallback | - | 399/399 | none/399 | motor stopped |

- Stage 3 produced **no `[WRITE]`**: the logical A->B transition never neutralized the vacated amplifier.
- Stage 5 invoked the REAL `ControlSafetyGuard.Apply` -> `EcosHardwareStopSink.StopLoco` -> real backend. It neutralized the currently resolved amp 3 only; amp 1 received no neutral write and stayed at HR0 416. The sink returned `True`, the guard reported `StopLoco`, and **no failure diagnostic** was raised.
- Layout stop `SetPower(false)` neutralized amp 1 (and mapped amps 1/2/3/4/5); **amp 6 (installed, unmapped) was not targeted**.

### Classifications
- `STOP REACHABILITY DEFECT CONFIRMED`
- `LAYOUT STOP FALLBACK PASS`
- `STOP FAILURE REPORTING DEFECT CONFIRMED`

### Cleanup
`resetsafety` applied (no movement command); all observed HR0 = 399; harness 21964 + driver 10400 terminated; UDP 10001 released; TCP 15471 free; tracked tree clean.

### Next
The physical evidence justifies a Developer safety-fix task (not started): (a) the stop path must reach the last commanded physical amplifier, not only the currently resolved one; (b) honour the stop result and raise a diagnostic on failed/unconfirmed neutralization; (c) escalate to the amplifier-centric neutralization path and include detected-but-unmapped amplifiers. Harness disposition still undecided.

## Latest Session (2026-09-20, safety stop-reachability architecture investigation)

Branch `feature/safety-stop-reachability` (new, from merged `master` at `0ee1b40`), HEAD `0ee1b40`. Architect source investigation only; **no code, firmware, or hardware was changed**.

### Classification
`SOURCE-CONFIRMED STOP-REACHABILITY GAP` (source architecture only; not a physically confirmed product defect).

### Key findings (source)
- `EcosHardwareStopSink.StopLoco` resolves the target purely from the loco's current block (`TrackAmplifierHardwareBackend.SetLocoSpeed:89-100`); when no amplifier mapping resolves it writes nothing, but the sink logs success and returns true (`EcosHardwareStopSink.cs:42-44`) and `ControlSafetyGuard` discards the result (`ControlSafetyGuard.cs:183`). A failed per-loco stop is silent.
- No retained per-loco or per-amplifier physical target is consulted by any stop path.
- Block transitions never neutralize the vacated amplifier (`SimpleEcosBackend.OnBlockEntered`), so a previously commanded amplifier can remain non-neutral; look-ahead can leave a second amplifier non-neutral when planning fails during a stop.
- The layout stop `TrackAmplifierHardwareBackend.SetPower(false)` is amplifier-centric and mapping-independent (iterates all `BlockTopology` blocks), but is not auto-escalated to and omits detected-but-unmapped amplifiers.
- `CommandNotApplied`/`BackendUnavailable` exist but are not raised by the stop path.
- Requested (`LocoState.Speed`/`Direction`) / Commanded (`PendingWrites[amp].Hr0Value`, never compared to observed) / Observed (`TrackAmplifierItem.HoldingReg[0]`) are distinct; `SetLocoSpeed(...,0) == true` does not mean physically neutral.

### Scenarios
1. Per-loco stop after the loco's current block has no amplifier mapping -> no write, reported success.
2. Layout stop -> reaches mapped amplifiers regardless of loco mapping (control that succeeds).
3. Remap A -> B while A still holds a non-neutral setpoint -> A can be left energized (vacated blocks never neutralized).
4. Communication/freshness unknown -> no stop-path gating, but no delivery confirmation, so an unobservable failure is possible.

### Recommended next step (NOT approved/implemented)
1. Minimal safety correction: honour the stop result, emit a failure diagnostic, escalate to the amplifier-centric neutralization path.
2. Broader cleanup: amplifier-centric commanded-state ownership with confirmed neutralization, vacated-block neutralization, look-ahead target retention, commanded-vs-observed PWM confirmation.
3. Optional diagnostics: surface unconfirmed neutralizations; wire `ReportBackendUnavailable`; fix misleading stop-sink log/XML.

### Physical reproduction
A minimal controlled plan exists (amplifier 1, lowest non-neutral PWM, operator-in-the-loop, or a harness substituting only the block source) with explicit abort/recovery criteria (process exit is NOT neutralization; amplifier power-cycle as final fallback). Not executed.

### Not done
No Developer, Integrator live test, implementation, hardware process, or PR.

### Next
Await Product Owner decision on the minimal safety correction and/or the physical reproduction.

### Refined test plan (Integrator, 2026-09-20; NOT executed)

`StopLoco` does not remove the mapping; the mapping *changes* through a normal block transition. Scenario classification:

- **Scenario A (Koploper unplaces/block 0):** operator/admin edge case; the non-neutral precondition is prevented by Koploper's speed-0-first rule. Not the primary scenario.
- **Scenario B (normal A -> B transition):** NORMAL running scenario; `OnBlockEntered` only updates logical state and never neutralizes the vacated amplifier, so amplifier A can stay non-neutral while the current mapping points to B.
- **Scenario C (look-ahead multi-target):** NORMAL but conditional; `ApplyLookAhead` can hold two amplifiers non-neutral. Deferred (needs two simultaneous targets).

**Recommended PRIMARY TEST = A->B TRANSITION, block 1 -> block 3** (lowest DCC28 step). In the current topology look-ahead is deterministic and absent here (amp 2 not installed blocks `1>2`; unknown switch 1 blocks `3>4`/`3>5`), so amp 1 is the only non-neutral target before the transition. Sequence: loco in block 1 -> low speed (amp 1 = 416) -> reassign loco to block 3 -> no write to amp 1 (stays 416) -> low speed again (amp 3 = 416) -> invoke the real per-loco safety stop -> amp 3 = 399, **amp 1 stays 416**.

**Safety-stop trigger:** no normal real-mode condition deterministically reaches `ControlSafetyGuard`, so the narrowest controlled invocation is `ControlSafetyGuard.Apply(<loco-scoped StopRequired diagnostic>)` in a harness that reuses the real `ControlSafetyGuard`, `EcosHardwareStopSink`, `TrackAmplifierHardwareBackend`, `TrackControlMain` and amplifier comm, substituting only the trigger (and the block source if Koploper refuses an at-speed reassignment). A Koploper speed-0 command is only a normal-path control, not the safety-sink test.

**Evidence per amplifier:** Requested (`LocoState.Speed`/echo), Commanded (`PendingWrites` + `[WRITE]` log), Observed (`HoldingReg[0]`/`PwmFeedback`). `StopLoco returned true` is not proof of a physical stop. Capture backend return, sink result, guard behaviour, diagnostics (expected: none) and actual HR0 to prove both the reachability failure and the failure-reporting failure.

**Secondary test:** controlled no-mapping stop (block 0/unmapped) to isolate the failure-reporting failure (backend returns false, sink still returns true and logs success, no write).

**Layout stop:** `SetPower(false)` neutralizes amps 1/3/4 (verified by observed HR0) but **not** the installed-but-unmapped amp 6.

**Recovery (independent of the per-loco stop):** layout stop verified by observed HR0 -> master software reset -> physical amplifier/backplane power removal. A process exit is not neutralization. The operator must know the exact recovery action before non-zero PWM is applied.

**Open live prerequisite:** does Koploper allow reassigning a loco's block while speed is non-zero? If not, the harness block source produces the transition.

### Harness disposition
`SiebwaldeApp/SiebwaldeApp.StopReachabilityHarness/` is committed validation tooling on `feature/safety-stop-reachability` (commit `03f5221`), deliberately not in `SiebwaldeApp.sln`. Disposition: **committed validation tooling; final merge disposition undecided** (retain as reusable tooling, convert to automated regression tests, or remove before the final PR). Physical safety evidence must be tied to the exact committed harness revision.

### Next
Await Product Owner decision on the refined primary test and/or the minimal safety correction.

## Latest Session (2026-09-20, live direction regression + safety reachability attempt)

Branch `feature/live-koploper-occupancy-validation`, HEAD `a2125a7` (plus this documentation commit). Targeted operator-in-the-loop live regression of the direction fix and a safety-reachability investigation. **No production code was changed.**

### Live process
- WPF app (real mode) PID 12716, window `Siebwalde Application`; port 15471; Koploper reconnected by the operator.
- Evidence: `Logging\live-validation-20260920-144552.stdout.txt`, `Logging\20-9-2026_TrackAppLog.txt`.
- Cleanup completed: speed 0, last `[WRITE] slave=1 HR0=0x018F` (399), `Stop-Process -Id 12716`, PID terminated, port 15471 released. Motor disconnected (operator-confirmed).

### Test A - direction defect regression: `LIVE DIRECTION REGRESSION PASS`
- Forward: loco 2 (id 1001) unmapped, `set(1001,dir[0],speedstep[0])` -> `<END 0 (OK)>` + `dir[0]` event, no write; placed in block 1 (no movement on mapping); `set(1001,speedstep[1])` -> `speed[5]` -> `[WRITE] slave=1 HR0=0x01A0` = **416 forward**.
- Reverse: unmapped `dir[1]` -> `<END 0 (OK)>` + event; re-mapped; `speedstep[1]` -> `HR0=0x017E` = **382 reverse**.
- Both returns to 0 wrote 399.

### Test B - retained non-zero speed while unmapped
- B1 PASS: unmapped `speedstep[1]` -> logical `speed[5]`, no `[WRITE]`.
- B2 PASS: re-mapping to block 1 caused **no** physical movement; slave 1 stayed 399.
- B3 NOT REACHABLE via Koploper UI: Koploper requires speed 0 before a direction change, so the "direction-only command re-applies a retained non-zero speed" source-level nuance could not be exercised live. Not a product defect.

### Test C/D - direction during a real safety latch, stop during latch: `NOT EXECUTED`
- Real-mode switch-condition and commanded/observed latches are unreachable (no switch feedback; positions stay unknown -> `StateUnknown`).
- The only theoretically live-reachable latch is `OccupancyMismatch` on a switch-less route, which would require additional physical state not present in this targeted setup.
- No operator-accessible latch-injection control exists and fault injection is out of scope, so these were not executed and no hardware behaviour was simulated.

### Test E - mapping-loss stop reachability: `INCONCLUSIVE / CONDITION NOT SAFELY REPRODUCIBLE`
- Koploper blocks removing a loco from a block while it is driving (speed must be 0 first), so the precondition (mapping loss while the amplifier is non-neutral) could not be created through the normal UI.
- The source-level safety reachability gap remains open as a HIGH PRIORITY backlog investigation.

### Koploper UI behaviours observed
- A direction change requires speed 0 first; Koploper then bundles the direction with `speedstep[0]` (e.g. `set(1001,dir[0],speedstep[0])`).
- Removing a loco from a block requires speed 0 first.
- No unexpected corrective/oscillating traffic; all replies `<END 0 (OK)>`; `END 8` never occurred.

### Not verifiable / residual
- Physical amplifier PWM is inferred from the write log (no hardware register read).
- Motor-connected state is operator-reported.
- The `IMovementSafetyGate` "future decorator outside the interlock" fragility remains a maintainability note (software review).

### Next steps
- Decide whether to pursue the HIGH PRIORITY safety reachability investigation with a controlled physical setup.
- PR still not created.

## Latest Session (2026-09-20, direction-before-known-block fix implemented and software-verified)

Branch `feature/live-koploper-occupancy-validation`, based on HEAD `a223d7e`. The confirmed direction-before-known-block defect is **fixed in software and software-verified**; **no live/hardware validation was performed** in this task and none is claimed.

### Root cause (confirmed)

`SimpleEcosBackend.HandleSetAsync` assigned `loco.Direction` only when `IHardwareBackend.SetLocoSpeed` returned true. With no known block, `TrackAmplifierHardwareBackend.SetLocoSpeed` returns false, so the requested direction was discarded and a later speed command reused the stale default `Direction = 1`. The same single boolean also conflated "no physical target" with a latched safety refusal, so the reply was always `<END 8 (SAFETY_INTERLOCK)>`.

### The fix

- Requested/logical direction and speed are now owned by the ECoS model (`SimpleEcosBackend`). The `dir[...]` branch always updates `loco.Direction` and emits `id dir[n]`; physical application is best effort and its result no longer gates the logical state.
- Speed application is factored into `SimpleEcosBackend.ApplyNormalizedLocoSpeed`. A stop (0) is always logically accepted; a non-zero speed that cannot reach a target retains the requested logical value; only a **real safety refusal** leaves the logical state unchanged and answers `<END 8 (SAFETY_INTERLOCK)>`.
- A real safety refusal is distinguished from "no target" through the new optional capability `IMovementSafetyGate` (in `SiebwaldeApp.EcosEmu`, implemented by `ControlSafetyInterlockBackend`). `IHardwareBackend` signatures were **not** changed.
- Queued events are emitted after any reply, so a logically-accepted direction is reported even if another option in the same command was refused. Events describe logical state, not a physical amplifier change.
- DCC28 normalization (`ProtocolSpeedNormalizer`) and `AmplifierSpeedMapper` were not touched. The movement interlock (loco/layout latch, stop always allowed, switch commands pass) is unchanged.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**, **175 warnings** (baseline 175; no new warnings).
- `dotnet test SiebwaldeApp.sln` -> **280/280 passed** (was 267; +13).
- `dotnet build SiebwaldeApp.sln -c Release` -> **0 errors**, **175 warnings**.
- `dotnet test SiebwaldeApp.sln -c Release --no-build` -> **280/280 passed**.
- New tests: `SimpleEcosBackendDirectionStateTests` (forward/reverse while unmapped, forward->reverse and reverse->forward most-recent-wins, live command order `dir[...]` + `speedstep[0]` -> block -> `speedstep[1]`, known-block direction handling, per-loco independence, unmapped stop, no false `SAFETY_INTERLOCK` for no-target, safety latch still rejects non-zero movement, retained direction used after reset).
- No hardware was connected or controlled; no hardware-driving process was started.

### Explicitly still pending

- Physical regression validation of the direction fix on the layout (Integrator). The fix is **not** live-validated.
- PR for `feature/live-koploper-occupancy-validation` still not created.

### Uncertainty for the Integrator to check

- The ECoS model now retains a requested non-zero speed when no physical target exists. No physical movement is produced; a later command consumes the retained value once a block is known. Live behaviour of Koploper when it receives a `dir[...]`/`speed[...]` event for a locomotive that is not physically addressable is not verified.
- A `dir[...]` command is now logically accepted (`<END 0 (OK)>`) even while a safety latch blocks the physical application; the physical movement is still blocked by `ControlSafetyInterlockBackend`.

## Latest Session (2026-09-20, direction-state defect confirmed by targeted reproduction)

Branch `feature/live-koploper-occupancy-validation`, HEAD `953360f` (plus this documentation commit). A targeted, operator-in-the-loop reproduction confirmed a product defect: a direction command issued while a locomotive has no known block is discarded, and the stale default direction is used once the locomotive later gets a block. **No production code was changed.**

### Reproduction

- App restarted (real mode, PID 10716); Koploper reconnected to 15471.
- loco 2 (ecosId 1001, address 2) placed on block 0 (no amplifier mapping). Koploper's UI showed forward.
- `set(1001,dir[1],speedstep[0])` and `set(1001,dir[0],speedstep[0])` -> both `<END 8 (SAFETY_INTERLOCK)>`, no `dir[...]` event; the logical direction stayed at the default `1` (reverse).
- loco 2 then placed in block 1 (`[EXT] Loc 2 -> Block 1`); `set(1001,speedstep[1])` -> `<END 0 (OK)>`, `1001 speed[5]`, runtime write `slave=1 HR0=0x017E` = **382 (reverse band)**. Forward would have been 416 (0x01A0).
- Return to 0: `slave=1 HR0=0x018F` (399).

### Root cause (confirmed in source)

`SimpleEcosBackend.cs:438-454` assigns `loco.Direction` only when `IHardwareBackend.SetLocoSpeed` returns true. With no known block, `TrackAmplifierHardwareBackend.SetLocoSpeed` returns false (`TrackAmplifierHardwareBackend.cs:89-100`), so the requested direction is discarded. `LocoState.Direction` stays at its default `1` (reverse, `SimpleEcosBackend.cs:1548`) and the later speed command reuses it (`SimpleEcosBackend.cs:423`). `<END 8 (SAFETY_INTERLOCK)>` is emitted for any hardware false return, not only a latched fault.

### Classification and follow-up

- Verdict: `CONFIRMED PRODUCT DEFECT` (recorded in `docs/backlog.md`).
- A Developer task is warranted but has **not** been started: retain the requested logical direction independently of hardware acceptance (or re-synchronise once a block is known), preserving the movement interlock for non-zero movement, with a focused unit test.
- Coverage gap: the only existing `dir[...]` test covers the success path; there is no test for `dir` with a null block provider / false backend return.

### Cleanup

- Return to 0 verified (399); motor disconnected (operator-confirmed); `Stop-Process -Id 10716`; PID terminated; port 15471 released. No process restarted.

### Next steps

- Product Owner to approve a Developer task for the direction-retention fix.
- PR still not created.

## Latest Session (2026-09-20, live DCC28 + occupancy bridge validation on real hardware)

Branch `feature/live-koploper-occupancy-validation`, HEAD `3be34891e5aa52233151e75824b4727ad2eb0987` at the end of the live run (implementation `5042f70`, docs `0b4f2b6`, context/agent policy `4c394d7`, `c7c0b7d`, `dde29cf`, `3be3489`). The DCC28 normalization and the occupancy bridge were validated live on the real amplifier setup. **No production code or firmware was changed during the live run.**

### Live process and cleanup

- WPF app (real mode) `SiebwaldeApp.exe`, PID 13384, window `Siebwalde Application`, working directory the Debug exe folder; started 12:35:30. The operator performed the normal production lifecycle (host detection + Start TrackController); the app started the track application and the real ECoS host.
- Port 15471 owned by PID 13384, Koploper connected; port 5700 = Koploper.
- Evidence: `Logging\live-validation-20260920-123530.stdout.txt`, runtime write log `Logging\20-9-2026_TrackAppLog.txt`, Core log.
- Cleanup completed: locomotive speed 0, last runtime writes neutral `HR0=0x018F` (399) on slaves 1, 3, 4 (slave 6 never written), `Stop-Process -Id 13384` issued, PID terminated, port 15471 released. Operator confirmed the motor was disconnected from all amplifiers.

### DCC28 live result (amplifier 1) - `LIVE DCC28 VALIDATION PASS`

| DCC28 step | Normalized | PWM (HR0, slave 1) | Physical |
| --- | --- | --- | --- |
| 0 | 0 | 399 (0x018F) | motor stopped |
| 1..27 | `(n*127+14)/28` | 400..~790 | motor ramps |
| 24 | 109 | 742 (previously 475) | motor at high speed |
| 28 | 127 | 799 (0x031F) | full throttle |
| 0 | 0 | 399 (0x018F) | motor stopped |

Wire: `set(1000,speedstep[n])` only, protocol `DCC28`; every reply `<END 0 (OK)>`; normalized `speed[...]` echo; no corrective/repeated traffic. "140 km/h" on Koploper's display = step 28; it never appears on the ECoS wire and no PWM above 799 was produced.

### Block routing live result - PASS

- block 1 -> amplifier 1 (slave 1).
- block 3 -> amplifier 3 (slave 3); loc2.
- block 4 -> amplifier 4 (slave 4); look-ahead (`4>1`, unconditional) also wrote slave 1 with the same PWM; no motor on amplifier 1.
- The observed direction band matched the mapper (`dir[0]` forward, `dir[1]` reverse).

### Occupancy bridge live result - `LIVE OCCUPANCY BRIDGE PASS` (blocks 1, 3, 4)

| Block | Amplifier | Bezetmelders | Sensors | Module-100 bits | Occupy | Clear |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 1 | 1.01, 1.02 | 1, 2 | 0, 1 | `0x00->0x01->0x03` | `0x03->0x02->0x00` |
| 3 | 3 | 1.05, 1.06 | 5, 6 | 4, 5 | `0x00->0x10->0x30` | `0x30->0x20->0x00` |
| 4 | 4 | 1.07, 1.08 | 7, 8 | 6, 7 | `0x00->0x40->0xC0` | `0xC0->0x80->0x00` |

- Each change was pushed to the live Koploper client as `<EVENT 100>` + `100 state[...]` + `<END 0 (OK)>`, with no corrective/extra traffic.
- `sensorId - 1` indexing confirmed across bits 0/1, 4/5, 6/7.
- Amplifier 6 is installed but unmapped (not in `BlockTopologyConfig`/`KoploperBlockMapConfig`), so it is correctly absent from module 100 and Koploper; no temporary mapping was added.
- An initial block-3 attempt reported block 4 because amplifier 3's switch had bad contact and amplifier 4's switch was engaged. After the operator re-established amplifier 3, block 3 reported correctly. **This was a physical test-setup issue, not a product defect.**

### Not directly verified (inferred or operator-side)

- Raw per-amplifier `HR_STATUS` (HoldingReg2) bit 10 is not written to any log; the amplifier-side comparator -> bit-10 step is inferred from `processio.c` plus the observed app-side events.
- Koploper's UI rendering of the occupancy/bezetmelder and of the normalized `speed[...]` echo is operator-side; the wire events were proven delivered.
- Precise occupancy transition latency was not measured (no per-frame timestamps in the logs).

### New observations recorded as software follow-ups (not fixed)

- `docs/backlog.md`: C# TrackAmplifier info page does not follow live data; updates should be event-based (the 10 Hz comm timer / 2 s update was built for the manual info page).
- `docs/backlog.md`: **confirmed product defect** (targeted reproduction 2026-09-20): a direction command issued while a locomotive has no known block is lost, so the locomotive starts in the stale/default direction. See the direction-state session above.
- `docs/backlog.md`: `SimpleEcosBackend` dispatch still depends on prefix ordering (`speed`/`speedstep`, `addr`/`addrext`).

### Next steps

- Decide on the software follow-ups above.
- PR for `feature/live-koploper-occupancy-validation` has not been created.

## Latest Session (2026-09-20, DCC28 speed normalization implemented)

Branch `feature/live-koploper-occupancy-validation`, implementation commit `5042f70`, with this documentation commit on top. The confirmed DCC28 scaling defect is fixed in software; **physical verification is still pending** and must be performed by the Integrator, not the Developer.

### Root cause (confirmed from source)

`SimpleEcosBackend.HandleSetAsync` tested `opt.StartsWith("speed", ...)` before the `speedstep[...]` case. Because `"speedstep[...]"` also starts with `"speed"`, the protocol-specific step from Koploper (`set(id,speedstep[n])`, DCC28 `0..28`) was parsed by the `speed` branch and passed downstream unchanged as if it were the normalized `0..127` ECoS speed. The later `speedstep` branch was therefore dead code. This matches the live trace (`Logging\19-09-2026_EcosEmuTrace.txt`: `set(1000,speedstep[24])` -> `[SIM-HW] ... speed=24`) and the measured ~PWM 475.

### The fix

- New `SiebwaldeApp.EcosEmu.ProtocolSpeedNormalizer` (ECoS/protocol layer): `speedstep` -> normalized `0..127`. DCC28 `0..28` is scaled with round-half-up integer arithmetic `(step*127+14)/28`; DCC128 is passed through (already normalized); `speed[...]` is never scaled twice; step 0 is always 0; a non-zero step for an unknown protocol is refused explicitly (`END 1 UNSUPPORTED_PROTOCOL`).
- `SimpleEcosBackend` handles `speedstep` before `speed`, stores the normalized value as `loco.Speed`, and emits the normalized `speed[...]` event. Direction handling (`dir[...]`) reuses the stored normalized speed, so direction changes do not alter normalization.
- The hardware backend and `AmplifierSpeedMapper` were **not** changed: they still operate on normalized `0..127` and stay protocol-independent. No DCC28 knowledge was added to `TrackAmplifierHardwareBackend`, `AmplifierSpeedMapper`, or the physical amplifier model.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**, **175 warnings** (unchanged from baseline).
- `dotnet test SiebwaldeApp.sln` -> **267/267 passed** (was 227; +40).
- `dotnet build SiebwaldeApp.sln -c Release` -> **0 errors**, **175 warnings**.
- `dotnet test SiebwaldeApp.sln -c Release --no-build` -> **267/267 passed**.
- New tests: `ProtocolSpeedNormalizerTests`, `SimpleEcosBackendSpeedNormalizationTests`.
- No hardware was connected or controlled; no hardware-driving process was started.

### Explicitly still pending at the time

- ~~live DCC28 full-range verification through the amplifier (Integrator);~~ **done 2026-09-20; see the live-validation session above.**
- ~~live occupancy bridge validation (`TrackAmplifierOccupancyBridge` -> module 100 -> Koploper bezetmelder);~~ **done 2026-09-20; see the live-validation session above.**
- PR for `feature/live-koploper-occupancy-validation` still not created.

### Uncertainty for the Integrator to check

The emulator now echoes `speed[<normalized>]` for a `speedstep[<n>]` command (for DCC28 step 14 it echoes `speed[64]`, not `speed[14]`). Live validation 2026-09-20: Koploper accepted the normalized echo with `<END 0 (OK)>` and sent no corrective traffic; the UI/throttle rendering itself remains operator-side. The motor-side effect is unambiguous (normalized domain).

### Safety note

This session performed no live/hardware work. The amplifier-1 residual PWM 475 from the earlier live session was subsequently cleared during the 2026-09-20 live validation, which ended with all commanded outputs neutral and the process stopped.

## Latest Session (2026-09-19, live Koploper setpoint validation + agent policy)

Branch `feature/live-koploper-occupancy-validation`, HEAD `b09c697`. **No production code was changed.** One product defect was found and is recorded, not fixed.

### Repository state

- branch: `feature/live-koploper-occupancy-validation`
- HEAD: `b09c697`
- local branch matches `origin/feature/live-koploper-occupancy-validation`
- tracked working tree clean
- commits created this session: `b09c697` (agent policy only)

### Verified live facts

**Validation-harness limitation (not a product defect).** The initial standalone harness did not start `TrackControlMain.StartRuntime`, so setpoints stayed in `PendingWrites` and nothing reached the amplifiers. Once the production runtime loop was started, the real command path worked.

**Proven physical command path:**

```
Koploper hand controller -> ECoS port 15471 -> SimpleEcosBackend
  -> block/amplifier translation -> hardware backend -> runtime write loop
  -> PIC32 master -> amplifier 1 -> physical motor
```

The motor physically responded to the Koploper hand controller. Observed C# -> amplifier response: **~120 ms** (consistent with the 10 Hz runtime loop plus a ~40 ms frame round-trip). No firmware was modified or flashed, no switch/accessory output was used, and movement happened only through the operator's hand controller on a free-running motor.

**Confirmed product defect - DCC28 scaling.** Live evidence:

| Item | Value |
| --- | --- |
| Locomotive protocol | `DCC28` |
| Speed steps supplied by Koploper/ECoS | `0..28` |
| Backend/hardware speed contract | normalized `0..127` |
| `AmplifierSpeedMapper` domain | normalized `0..127` |
| Live DCC28 step 24 | ~PWM 475 |

DCC28 speed is therefore **under-scaled**: a full-throttle command reaches only roughly PWM 475 instead of approaching 799, so only part of the usable range is used.

**Preferred correction (NOT implemented):**

```
protocol-specific ECoS speed -> normalize at the ECoS/protocol boundary
  -> normalized 0..127 -> existing IHardwareBackend
  -> existing AmplifierSpeedMapper -> amplifier PWM
```

Normalization belongs at the ECoS/protocol boundary; the hardware/backend layer stays protocol-independent.

### Agent policy (committed)

`b09c697` adds the live-hardware working policy to `.opencode/agents/project-lead.md` (+128) and `.opencode/agents/integrator.md` (+77). The single-agent default is unchanged; live hardware is now the explicit exception that delegates to the Integrator. `developer.md`, `architect.md` and `designer.md` were **not** modified.

### NOT completed (do not assume otherwise)

- ~~Developer implementation of DCC28 normalization;~~ **done 2026-09-20, commit `5042f70`**
- ~~regression tests for the DCC28 scaling defect;~~ **done 2026-09-20, commit `5042f70`**
- independent Integrator review of that fix;
- live DCC28 full-range verification;
- live occupancy bridge validation through `TrackAmplifierOccupancyBridge` -> ECoS module 100 -> Koploper bezetmelder;
- PR for `feature/live-koploper-occupancy-validation`.

### Agent workflow for the next session

```
Project Lead
-> Developer implements and software-verifies the DCC28 fix
-> Integrator independently reviews
-> Integrator performs the later live validation
-> Project Lead records results
```

The Developer must not perform the independent live validation of its own fix. Do not invoke Architect or Designer unless a concrete need arises.

### Exact next task

1. read the durable project documents;
2. verify commit `b09c697`;
3. confirm current branch/HEAD;
4. use the Developer subagent to implement DCC28 normalization at the ECoS protocol boundary;
5. add regression tests;
6. run Debug and Release build/tests;
7. commit and push the implementation;
8. return to the Project Lead before any Integrator or live-validation step begins.

### Safety note for the next live session

When the last harness stopped it left **amplifier 1 at PWM 475** (HR0 `0x01DB`) instead of neutral; amplifiers 3, 4 and 6 were at 399. The harness did not command neutral on shutdown - exactly the gap the new cleanup policy addresses. Confirm amplifier 1 is safe (power-cycle, master reset, or an explicitly announced neutral command) before any further live work.

## Latest Session (2026-09-19, real-hardware occupancy validation)

Branch `feature/real-occupancy-integration`. The existing occupancy path was validated on the real amplifier setup. **No production-code change was required**; this session is documentation only.

### What was validated

```
PIC18 CMP1 -> HR_STATUS bit 10 -> PIC32/master transport -> TrackCommClientAsync
  -> TrackAmplifierItem -> TrackAmplifierOccupancyProvider -> observability/freshness
```

Tested amplifiers: **1, 3, 4, 6** (four proto amplifiers), all `SlaveDetected = 1`, all `HR11 = 0x251F` (flash step found 0 slaves to flash).

- Healthy frame intervals per amplifier: median **40-42 ms**, maximum **66-70 ms** -> the 2 s freshness timeout has roughly **30x margin**; no adjustment needed.
- `HR_STATUS` bit 10 changed exactly with physical occupancy: amplifier 1 occupied `0x2E02` (bit 10 set), clear `0x2A01` (bit 10 clear). Only bit 10 was under test.
- Provider once valid data flowed: blocks backed by detected amplifiers (sections 1, 3, 4) reported `known = true`; blocks whose sections do not exist (2, 5) stayed `known = false`. `OccupancyAvailable = true`. Non-existing sections were never falsely clear.

### Freshness invariant physically confirmed

The master stopped delivering fresh frames while the 100 ms C# republish kept firing (median interval rose ~41 ms -> ~94 ms). Old `HoldingReg` values stayed present, `LastDataReceivedUtc` went stale, `OccupancyAvailable` became false and every block became unknown. The old clear values were **not** treated as known-clear. This confirms `stale != clear` on real hardware and that `AmplifierDataReceived` alone is not proof of fresh data.

### Not yet validated (not defects)

- physical occupancy transition latency was not timestamped;
- WPF occupancy indication was not compared during this validation;
- the ECoS/Koploper occupancy bridge was not started, because Koploper was running and autonomous movement was intentionally avoided;
- occupancy-driven live safety/look-ahead behaviour with a real train is a separate controlled test.

### Validation-environment limitation (not a product defect)

During the standalone hardware validation the temporary checker was able to leave the master communication session in a state that required reinitialization. This was **not** reproduced through the normal application lifecycle, where master/amplifier communication continues running continuously, load/amplifier disconnects are already detected and reported by the existing system, and a software reset/reinitialization path already exists. It is therefore recorded as a **test-harness limitation** of running a standalone checker outside the normal application lifecycle and communication ownership, not as a demonstrated production defect, and it does not warrant a backlog item.

The production result from that event remains valid and is the relevant occupancy-validation outcome: when fresh SLAVEINFO data stopped arriving, old register values remained cached, and the new C# freshness logic correctly changed occupancy to **unknown** instead of continuing to report a stale clear state.

### Files changed

Documentation only: `docs/koploper-interface.md`, `docs/handoff.md`, `docs/backlog.md`, `docs/analysis-coverage.md`.

### Best next step

Start the ECoS/Koploper occupancy bridge in a controlled run (Koploper stopped or with autonomous movement prevented) to validate the bridge end-to-end, then the occupancy-driven safety/look-ahead behaviour with a real train.

## Latest Session (2026-09-19, occupancy freshness review)

Branch `feature/real-occupancy-integration`. Reviewed whether `SlaveDetected != 0` proves *current* data. **It does not.** A genuine freshness defect existed and is fixed; no firmware was changed.

### What the existing state actually proves

| Signal | Semantics |
| --- | --- |
| `SlaveDetected` | Written once per parsed frame (`TrackCommClientAsync:180`) and **never cleared**. Proves only that a frame was seen at some point. |
| `HoldingReg` | Keeps its last received values indefinitely; nothing resets it after a loss. |
| `AmplifierDataReceived` | Republished every 100 ms by `_publishTimer` for every `SlaveDetected != 0` amplifier, **regardless of new data**. Not a freshness signal. |
| `MbReceiveCounter` | Read from the frame (not incremented by C#); its increment semantics cannot be verified without firmware. |
| `ITrackTransport` | Exposes only `Open/Close/Send/Receive`; no connection-loss or health signal. |

So no existing state provided freshness, and the previous `SlaveDetected != 0` check could leave a block "known clear" forever after communication stopped.

### The fix

- `TrackAmplifierItem.LastDataReceivedUtc` is stamped in `TrackCommClientAsync` in the same step that stores `HoldingReg` and `SlaveDetected` - one line, single source of truth, no new timer.
- `TrackAmplifierDataFreshness` is the single policy: default 2 s, well above the 10 Hz republish cycle. `IsCurrentData` = detected **and** fresh.
- `TrackAmplifierOccupancyProvider` and `AmplifierOccupancyObservability` use it. Staleness is evaluated when a consumer already runs, so no polling loop was added.
- A fresh occupied section still proves a block occupied even when another section is silent; a **stale** occupied reading is not promoted to definite occupancy.

### Final semantics per amplifier section

| Section state | Meaning |
| --- | --- |
| fresh + occupied | occupied |
| fresh + clear | clear |
| stale (either value) | unknown |
| never received | unknown |
| not detected | unknown |

Block: occupied if any covering section is fresh+occupied (even if another is unknown); known clear only if **every** covering section is fresh+clear; otherwise unknown.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**, **175 warnings** (unchanged; no new warnings introduced).
- `dotnet test` -> **227/227 passed** (was 218; +9).

### Remaining concerns

1. The 2 s freshness window is a policy value, not derived from the master's cycle. It has since been measured on real hardware (~30x margin), so no tuning is needed; see the hardware-validation session above.
2. Real occupancy still needs a live hardware run to confirm end-to-end behaviour.
3. The stale `General.h` comment remains (deliberately not touched in this task).

### Best next step

Controlled real-hardware occupancy test on the amplifier oval.

## Latest Session (2026-09-19, real occupancy integration)

Branch `feature/real-occupancy-integration` (from `master` at `f778a32`). Connects the existing real amplifier occupancy data to the new C# occupancy/divergence/safety architecture. **No firmware was changed.**

### The existing occupancy path (verified)

```
PIC18 processio.c: g_occ = CMP1_GetOutputStatus() -> HR_STATUS (HoldingReg2) bit 10
  -> PIC32 master SLAVEINFO frame
  -> TrackCommClientAsync.HandleNewDataAsync: writes trackAmpItems[n].HoldingReg + SlaveDetected,
     raises AmplifierDataReceived
  -> TrackAmplifierItem.HoldingReg[2]
```

Both consumers read the same value: the track-amplifier page (`hr2 & TrackAmplifierRegisters.OccupiedBit`) and `TrackAmplifierOccupancyProvider` (`TrackAmplifierRegisters.IsOccupied(HoldingReg)`).

### Why real mode previously reported occupancy unavailable

`TrackControlHost` set `ModeObservability { OccupancyAvailable = false }` for real mode, based on the `General.h` "(TODO: implement when occupancy source known)" comment. That comment is **stale**: `processio.c` already sets the bit from a real comparator input.

### What changed

- `IOccupancyProvider` gained `IsBlockOccupancyKnown`; unknown is now distinct from clear.
- `TrackAmplifierOccupancyProvider` reads `TrackAmplifierItem` (registers plus detection) and reports a block known only when every covering section has valid data.
- `AmplifierOccupancyObservability` (Integration) replaces the hard-coded `false`: availability follows `SlaveDetected`, the existing "a frame was parsed" signal.
- `DivergenceChecker`: occupied -> `OccupancyMismatch` (StopRequired); not occupied but unknown -> `StateUnknown` (Rejected, no stop).
- `LookAheadPlanner` no longer pre-commands into a block whose occupancy is unknown.
- `TrackAmplifierOccupancyBridge` skips unknown blocks instead of reporting them free, and leaves them out of change tracking.
- `ControlSafetyGuard.Reset()` requires an `OccupancyMismatch` block to be known clear.

### Files changed

- Modified: `Core/Model/TrackApplication/Control/IOccupancyProvider.cs`, `TrackAmplifierOccupancyProvider.cs`, `LookAheadPlanner.cs`, `Integration/DivergenceChecker.cs`, `TrackAmplifierOccupancyBridge.cs`, `TrackControlIntegration.cs`, `TrackControlHost.cs`, `Observability.cs`, and the tests `TrackAmplifierOccupancyProviderTests.cs`, `TrackControlIntegrationTests.cs`, `DivergenceAndSafetyTests.cs`, `LookAheadPlannerTests.cs`, `TrackAmplifierHardwareBackendLookAheadTests.cs`, `TrackAmplifierOccupancyBridgeTests.cs`.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**.
- `dotnet test` -> **218/218 passed** (was 204; +14).

### Remaining concerns

1. Real occupancy has since been physically validated on the actual amplifiers; see the hardware-validation session above.
2. Simulator mode still delivers occupancy as ECoS sensor events, so `OccupancyAvailable` stays false there (unchanged, out of scope).
3. The stale `General.h` comment remains in the firmware; correcting it is a separate firmware change.

### Best next step

Validate real occupancy on the hardware oval with the amplifier firmware running, then consider the simulator occupancy provider.

## Latest Session (2026-09-19, safety movement interlock)

Closes the gap that a latched `StopRequired` fault prevented repeated stops but not a later Koploper command from moving the affected locomotive again.

### Where the interlock lives

`SiebwaldeApp.Integration.ControlSafetyInterlockBackend` is a decorator over the hardware backend that is already in use. It is inserted between the ECoS backend and the hardware backend in both modes (simulator in `TrackControlHost`, real inside `TrackControlIntegration`), so every ECoS movement command passes through the same policy. No second control path was introduced, and no safety logic sits in WPF.

### Blocking rules while a StopRequired fault is latched

| Latch scope | Non-zero movement | Stop (speed 0) | Power off | Power on | Switch commands |
| --- | --- | --- | --- | --- | --- |
| Loco N | refused for N only | allowed | allowed | allowed | allowed |
| Layout (unattributable fault) | refused for every loco | allowed | allowed | **refused** | allowed |

A layout latch escalates over loco latches. Switch commands always pass, because a corrective switch change is often the only way to resolve the divergence, and a corrective command does not unlatch anything.

### How a rejected movement is represented

`IHardwareBackend.SetPower` and `SetLocoSpeed` now return `bool`, in the same spirit as the earlier `SetSwitch` fix. `SimpleEcosBackend` only updates its logical loco state and only emits a `speed[...]`/`dir[...]` event when the backend reports the command was applied; otherwise it replies `<END 8 (SAFETY_INTERLOCK)>` and reports nothing. Koploper is therefore never told a refused movement succeeded. `TrackAmplifierHardwareBackend.SetLocoSpeed` also returns false for a locomotive with no known block, so that case is no longer falsely acknowledged either.

A rejection is reported once per affected locomotive per latch (`MovementRejectedBySafety`), so repeated commands cannot flood the diagnostics.

### Reset and recovery

`ControlSafetyGuard.Reset()` now **revalidates** every latched fault before clearing. `DivergenceChecker.IsResolved` decides: a `RouteSwitchMismatch` is resolved when the switch's known logical position now matches the required position; `StateUnknown` when the position is known; `OccupancyMismatch` when the block is no longer occupied. A refused reset reports `ResetRefused` and leaves the latch and the movement interlock in place. Reset never issues movement, and a corrective switch command does not unlatch by itself.

### Files changed

- Added: `SiebwaldeApp.Integration/ControlSafetyInterlockBackend.cs`.
- Modified: `EcosEmu/Hardware/IHardwareBackend.cs` (SetPower/SetLocoSpeed return bool), `EcosEmu/Hardware/TrackSimulatorBackend.cs`, `EcosEmu/Hardware/DummyHardwareBackend.cs`, `EcosEmu/Backend/SimpleEcosBackend.cs`, `Integration/SwitchTranslatingHardwareBackend.cs`, `Integration/TrackAmplifierHardwareBackend.cs`, `Integration/ControlSafetyGuard.cs`, `Integration/DivergenceChecker.cs`, `Integration/TrackControlHost.cs`, `Integration/TrackControlIntegration.cs`, `Core/Model/TrackApplication/Diagnostics/DiagnosticTypes.cs` (+MovementRejectedBySafety, +ResetRefused), `Core/Model/TrackApplication/Diagnostics/ControlDiagnostic.cs` (+RequiredSwitchPosition), `Core/Model/TrackApplication/Control/IEcosHostService.cs`, `Core/Model/SiebwaldeApplicationModel.cs`, `SiebwaldeApp/ViewModel/SiebwaldeViewModels/SiebwaldeInitPageViewModel.cs`, `Core.Tests/DivergenceAndSafetyTests.cs`, `Core.Tests/SwitchControllerTests.cs`.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**.
- `dotnet test` -> **204/204 passed** (was 193; +11 interlock tests, including an integration-style test through `SimpleEcosBackend`).
- Software-only scenario on port 15471 (temporary console host, since removed): divergence detected and loco 1 stopped; `set(1000,speed[40])` answered `<END 8 (SAFETY_INTERLOCK)>` with no speed event; four repeats produced one rejection diagnostic; `set(1000,speed[0])` answered `<END 0 (OK)>`; a corrective switch command was accepted and did not unlatch; reset while still wrong was refused; after correcting the switch the reset succeeded and `speed[40]` was accepted again.
- No physical hardware was touched.

### Remaining concerns

1. Real mode still has no switch feedback and no reliable occupancy, so route checks cannot confirm anything there.
2. The interlock blocks commands; it does not forcibly re-issue stops, which is correct but means a latched fault relies on the earlier safety stop having landed.
3. `dir[...]` is treated as a movement command and goes through the same path, so a direction change is refused while the locomotive is latched even at speed 0. That is conservative and safe.

### Increment 6 completion assessment

**Increment 6 can be declared complete.** The translation path (speed/PWM, routing and look-ahead, occupancy, switch mapping), the ECoS host lifecycle with explicit mode semantics, the configurable mappings, and the divergence/safety/diagnostics layer including the movement interlock are implemented, tested and documented. What remains is deliberately out of scope and recorded in `docs/backlog.md`: the real amplifier occupancy firmware bit, the real accessory-decoder switch output, physical switch feedback, and simulator occupancy through the production occupancy abstraction. None of these block a first integration/PR; they are hardware and firmware dependencies.

## Latest Session (2026-09-19, item 4: divergence, safety stop, diagnostics)

Increment 6 item 4 is implemented: a diagnostic/safety layer around the Koploper/ECoS translation path.

### Diagnostic model (Core)

`SiebwaldeApp.Core` gained `DiagnosticSeverity` (Info, Warning, Rejected, StopRequired), `DiagnosticCode` (UnmappedAddress, InvalidConfiguration, RouteSwitchMismatch, CommandNotApplied, CommandedObservedMismatch, OccupancyMismatch, BackendUnavailable, StateUnknown), `SafetyAction` (None, StopLoco, StopLayout), the immutable `ControlDiagnostic` (code, severity, subject, detail, loco/block/switch context, timestamp, safety action taken) and `ControlDiagnostics` (bounded 100-entry history plus a separate latched unsafe state). The model carries no protocol or UI wording.

### Requested / Commanded / Observed

Kept explicitly apart. `SwitchController` records the requested (logical ECoS) position and the commanded (physical) position separately, and consults an `ISwitchObserver` only when `IObservability.SwitchFeedbackAvailable` is true. In real mode switch feedback does not exist, so an `UnobservableSwitchObserver` reports "not observable" and the command is reported as `StateUnknown`/Warning - never as a confirmation. `ISwitchOutput` exposes `IsAvailable` so a not-yet-wired output is a known limitation, not a fault.

### What counts as divergence

- unmapped switch address (Warning, command ignored);
- route needs a switch position that differs from the known logical position (StopRequired);
- route needs a switch whose position is unknown (Rejected);
- route needs a switch that is not mapped at all (Rejected);
- backend did not apply a commanded position (Rejected);
- commanded vs observed mismatch, only where observation exists (StopRequired);
- target block occupied, only when occupancy is observable (StopRequired);
- backend/communication unavailable (StopRequired, layout-scoped);
- invalid or duplicate switch mapping (Rejected, reported at controller construction).

### Safety reaction and stop mechanism

`ControlSafetyGuard` applies the reaction and is idempotent per fault key (`code|subject`): the first StopRequired for a fault issues a stop, repeats do nothing, a different fault still acts. A loco-scoped fault calls the existing per-loco path (`IHardwareBackend.SetLocoSpeed(address, 0, dir)` -> neutral PWM), so unrelated trains keep running; an unattributable fault calls the existing central path (`IHardwareBackend.SetPower(false)`, the same mechanism Koploper's `set(1,stop)` uses). No second locomotive-control path was introduced; `EcosHardwareStopSink` wraps the backend already in use.

### Latching and recovery

The first StopRequired latches and stays latched; lower-severity diagnostics and later commands never clear it. Recovery is only an explicit `ResetSafety()` (host -> guard -> diagnostics). Warnings are transient and do not latch.

### Operator visibility

`IEcosHostService`/`SiebwaldeApplicationModel` expose `Diagnostics`, `IsUnsafe` and `ResetSafety()`. The init page shows the active mode, the control-path health (`Control path: UNSAFE (latched)` or the current severity), the latest diagnostic text and a "Reset control safety" button. The view model only displays and requests; the decision stays in the host.

### Files changed

- Added: `Core/Model/TrackApplication/Diagnostics/DiagnosticTypes.cs`, `Diagnostics/ControlDiagnostic.cs`, `Diagnostics/ControlDiagnostics.cs`, `Diagnostics/ISafetyStopSink.cs`, `Integration/ControlSafetyGuard.cs`, `Integration/DivergenceChecker.cs`, `Integration/EcosHardwareStopSink.cs`, `Integration/Observability.cs`, `Core.Tests/DivergenceAndSafetyTests.cs`.
- Modified: `Core/Model/TrackApplication/Control/ISwitchOutput.cs` (IsAvailable + bool return), `Core/Model/TrackApplication/Control/IEcosHostService.cs`, `Core/Model/SiebwaldeApplicationModel.cs`, `Integration/SwitchController.cs`, `Integration/DelegateSwitchOutput.cs`, `Integration/TrackAmplifierHardwareBackend.cs`, `Integration/TrackControlHost.cs`, `Integration/TrackControlIntegration.cs`, `EcosEmu/Hardware/TrackSimulatorBackend.cs`, `SiebwaldeApp/Pages/SiebwaldePages/SiebwaldeInitPage.xaml`, `SiebwaldeApp/ViewModel/SiebwaldeViewModels/SiebwaldeInitPageViewModel.cs`, `Core.Tests/SwitchControllerTests.cs`.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**.
- `dotnet test` -> **193/193 passed** (was 165; +28 in `DivergenceAndSafetyTests`).
- Software-only scenario through the real ECoS path on 15471 (temporary console host, since removed): a valid route stayed healthy; setting switch 1 to diverging made `CheckTransition(3 -> 4)` return `RouteSwitchMismatch`/StopRequired with loco=1, block=3, switch=1 and action=StopLoco; five repeat evaluations produced five reports but only **one** stop; an unmapped `switch[9g]` produced a Warning; an explicit reset cleared the latch and a re-check stopped again for the still-present fault; only the targeted loco was stopped.
- No physical hardware was touched.

### Remaining concerns

1. Route checks run from the look-ahead path (`TrackAmplifierHardwareBackend.Divergence`), so a route is only checked when a locomotive is about to be pre-commanded into the next block.
2. Simulator occupancy is delivered as ECoS sensor events rather than through an `IOccupancyProvider`, so `OccupancyAvailable` is false there; occupancy divergence is covered by tests with fakes.
3. Real mode reports both switch feedback and occupancy as unavailable, so no route check can currently confirm anything on real hardware.
4. A latched fault stops a locomotive but does not prevent Koploper from commanding again; the guard only prevents repeated stops for the same fault.

### Best next step

Item 4 is the last planned Increment 6 item. Next candidates: the real accessory-decoder switch output path, real amplifier occupancy (firmware bit), or the first end-to-end integration with Koploper on the hardware oval.

## Latest Session (2026-09-19, item 3: switch mapping)

Increment 6 item 3 is implemented: Koploper can control switches through the ECoS emulator, with one shared translation path for real and simulator mode.

### Switch-command data flow

```
Koploper -> EcosEmulatorServer (15471) -> SimpleEcosBackend.HandleSwitchCommand
        -> IHardwareBackend.SetSwitch(decoderAddress, outputIndex, on)   [now returns bool]
        -> SwitchTranslatingHardwareBackend   (shared translation)
        -> SwitchController.TryApply          (SwitchMapping lookup + invert)
        -> ISwitchOutput.SetPosition(physicalAddress, position)
             simulator: TrackSimulatorBackend switch store
             real:      deliberate no-op that logs (accessory path not wired yet)
```

The ECoS backend only emits a `state[...]` event when `SetSwitch` returns true, so an unmapped address can no longer make the logical ECoS state disagree with the layout.

### Proven mapping (not guessed)

The `3 -> 4` versus `3 -> 5` question is now answered from the live trace `Logging\19-09-2026_EcosEmuTrace.txt`, six occurrences per route:

- Loc 1 (goes to block 4): `set(11,switch[1g])` + `set(11,switch[2r])`
- Loc 2 (goes to block 5): `set(11,switch[1r])` + `set(11,switch[2g])`

Both switches are always commanded as a complementary pair, so conditioning the route on switch 1 alone is correct and sufficient. The shipped `BlockTopologyConfig` default now uses `routes: 1>2,2>3,3>4@1:0,3>5@1:1,4>1,5>1` (previously provisional and unconditional).

### Configuration

`SwitchMapConfig`, same style as the other mapping settings:

```
switches: <ecosAddress>:<physicalAddress>[:inverted][:g|r|keep], ...
```

Shipped default `switches: 1:1:keep, 2:2:keep`. Editable on the settings page with save/reset/undo, with the same blank-value fallback to the declared default as the other mappings. Invalid entries and duplicate ECoS addresses are recorded in `SwitchMapping.Errors`, logged, and never turned into a plausible-but-wrong mapping.

### Startup state

`SwitchController.Initialize()` runs during host start (before the server listens). Entries with `g`/`r` are driven and recorded as both logical and physical state; `keep` entries are left untouched and are deliberately **not** reported as known, so the logical and physical state cannot disagree silently. Real mode drives nothing because the accessory-decoder path does not exist yet, and it runs only after `StartTrackApplication()` has completed its initialization, so nothing can actuate early.

### Files changed

- Added: `SiebwaldeApp.Core/Model/TrackApplication/Control/ISwitchOutput.cs`, `SiebwaldeApp.Core/Model/TrackApplication/Control/SwitchMapping.cs`, `SiebwaldeApp.Integration/SwitchController.cs`, `SiebwaldeApp.Integration/SwitchTranslatingHardwareBackend.cs`, `SiebwaldeApp.Integration/DelegateSwitchOutput.cs`, `SiebwaldeApp.Core.Tests/SwitchMappingTests.cs`, `SiebwaldeApp.Core.Tests/SwitchControllerTests.cs`.
- Modified: `SiebwaldeApp.EcosEmu/Hardware/IHardwareBackend.cs` (SetSwitch returns bool), `SiebwaldeApp.EcosEmu/Hardware/TrackSimulatorBackend.cs`, `SiebwaldeApp.EcosEmu/Hardware/DummyHardwareBackend.cs`, `SiebwaldeApp.EcosEmu/Backend/SimpleEcosBackend.cs`, `SiebwaldeApp.Integration/TrackAmplifierHardwareBackend.cs`, `SiebwaldeApp.Integration/TrackControlHost.cs`, `SiebwaldeApp.Integration/TrackControlIntegration.cs`, `SiebwaldeApp.Core/Configuration/CoreConfiguration.cs`, `SiebwaldeApp.Core/Properties/CoreSettings.settings`, `SiebwaldeApp.Core/Properties/CoreSettings.Designer.cs`, `SiebwaldeApp.Core/app.config`, `SiebwaldeApp/App.config`, `SiebwaldeApp/Pages/SiebwaldePages/SiebwaldeSettingsPage.xaml`, `SiebwaldeApp/ViewModel/SiebwaldeViewModels/SiebwaldeSettingsPageViewModel.cs`.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**.
- `dotnet test` -> **165/165 passed** (was 124; +41 across `SwitchMappingTests` and `SwitchControllerTests`).
- Software-only integration test through the real ECoS port (temporary console host, since removed): started the host in simulator mode on 15471, sent `set(11,switch[1g])`, `[1r]`, `[2g]`, `[2r]` over TCP, and confirmed all four reached the expected logical and physical state; an unmapped `switch[9g]` drove nothing and sent no state event; the port was released on stop.
- No physical hardware was touched.

### Remaining concerns

1. The real accessory-decoder output path does not exist, so real-mode switch commands are logged and not actuated (`TrackAmplifierHardwareBackend.SetSwitch` returns false).
2. The real layout's power-on switch positions are unknown, hence the `keep` default.
3. Signals 51..55 are commanded by Koploper as switches but are deliberately unmapped; they are ignored and no state is reported for them.
4. A physical drive failure cannot be signalled back through `ISwitchOutput` (void), so the ECoS state reflects "commanded" rather than "confirmed at the layout". Real feedback would need an async/result path.

### Best next step

**Item 4: divergence check** (compare Koploper/ECoS state with the physical/simulated state, stop Koploper via ECoS on mismatch, and surface operator diagnostics).

## Latest Session (2026-09-19, ECoS host hardening)

Two hardening changes on top of the app-startup wiring.

### Mode-transition semantics

Real mode is authoritative. `IEcosHostService.StartAsync` now returns `Task<EcosHostStartResult>` (`Started`, `AlreadyActive`, `Transitioned`, `Rejected`, `NotAvailable`, `Failed`) instead of silently ignoring a start request:

- requested mode already active -> `AlreadyActive` (idempotent);
- `Simulator` requested while `Real` runs -> `Rejected`; the real host is left untouched;
- `Real` requested while `Simulator` runs -> the simulator is stopped cleanly and real mode starts (`Transitioned`), so a successfully started real track application is never left hidden behind a simulator;
- `Real` requested without the track client/variables -> `ArgumentException` **before** the running host is touched;
- if the transition itself fails, the host releases the port and the external-info client before rethrowing.

The active mode is exposed through `SiebwaldeApplicationModel.ActiveEcosMode` and shown on the init page as `EcosModeStatus` ("ECoS host: Real mode active"), so the operator never has to assume a start succeeded. `EcosEmulatorServer` sets `ReuseAddress` so port 15471 can be rebound immediately during a transition.

### Blank mapping settings

`CoreConfiguration.BlockTopologyConfig`/`KoploperBlockMapConfig` now fall back to the setting's declared default when the persisted user value is null/empty/whitespace, read from `CoreSettings.Default.Properties[name].DefaultValue` (the Designer stays the single source of truth). A non-empty value is used as-is, even when malformed, so configuration errors stay diagnosable instead of being masked. The settings page reads the same resolved values, so the page and the runtime cannot disagree.

### Files changed

- Added: `SiebwaldeApp.Core/Model/TrackApplication/Control/EcosHostStartResult.cs`, `SiebwaldeApp.Core.Tests/CoreConfigurationTests.cs`.
- Modified: `SiebwaldeApp.Core/Model/TrackApplication/Control/IEcosHostService.cs`, `SiebwaldeApp.Core/Model/SiebwaldeApplicationModel.cs`, `SiebwaldeApp.Core/Configuration/CoreConfiguration.cs`, `SiebwaldeApp.Integration/TrackControlHost.cs`, `SiebwaldeApp.EcosEmu/Server/EcosEmulatorServer.cs`, `SiebwaldeApp/ViewModel/SiebwaldeViewModels/SiebwaldeInitPageViewModel.cs`, `SiebwaldeApp/ViewModel/SiebwaldeViewModels/SiebwaldeSettingsPageViewModel.cs`, `SiebwaldeApp/Pages/SiebwaldePages/SiebwaldeInitPage.xaml`, `SiebwaldeApp.Core.Tests/TrackControlHostTests.cs`.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**.
- `dotnet test` -> **124/124 passed** (was 107; +9 mode-transition/lifecycle tests, +8 settings-fallback tests). The transition test proves the port stays served across Simulator -> Real and is released on stop.

### Remaining concerns

1. The ECoS host starts in real mode before the firmware occupancy flag is populated, so Koploper sees no occupancy feedback on real hardware yet (firmware TODO).
2. `EcosEmulatorServer` binds loopback only; correct while Koploper is on the same PC.
3. `ReuseAddress` means another local process could bind 15471 while the host restarts; acceptable for this single-purpose host but worth remembering.
4. Mode switching is a stop-and-start of the ECoS server, so Koploper sees a brief disconnect during a Simulator -> Real transition.

### Best next step

**Item 3: switch mapping** (real <-> Koploper designation plus the default initial position), followed by item 4 (divergence check with ECoS stop and operator diagnostics).

## Latest Session (2026-09-19, app-startup wiring)

Completed on `feature/csharp-cleanup-startup`: the ECoS host is now composed, started and stopped by the application.

### Architecture chosen

Core defines `TrackControlMode` (Simulator/Real) and `IEcosHostService`. `SiebwaldeApp.Integration.TrackControlHost` implements it and owns the composition and lifetime: it creates the Koploper external-info client (5700), the loco repository, the mode-specific hardware backend, the ECoS backend and the `EcosEmulatorServer` (15471), and releases them all on `Stop()`.

Core cannot reference Integration (Integration references Core, and Core has no project references at all), so `SiebwaldeApplicationModel` takes the host through its constructor. WPF only builds it in `IoC.Setup()` via `TrackControlHost.FromConfiguration()` and calls a start/stop command; it holds no control logic.

### Mode selection

- **Real**: started automatically at the end of `StartTrackApplication()`, so the established initialization sequencing is untouched. It needs the track communication client and shared variables, which only exist once the track application has started.
- **Simulator**: explicit operator action, "ECoS simulator" on the init page (`InitEcosSimulator` -> `SiebwaldeApplicationModel.StartEcosHostSimulatorAsync()`). Software-only, no hardware.

Starting an already running host is ignored, so the simulator button cannot take a running real host down under Koploper.

### Ownership and lifetime

`TrackControlHost` owns everything it creates. `SiebwaldeApplicationModel.StopEcosHost()` stops it, and `StopTrackApplication()` now calls that first, before its `_trackControlMain == null` early return, so simulator mode is also shut down cleanly.

### Files changed

- Added: `SiebwaldeApp.Core/Model/TrackApplication/Control/TrackControlMode.cs`, `SiebwaldeApp.Core/Model/TrackApplication/Control/IEcosHostService.cs`, `SiebwaldeApp.Integration/TrackControlHost.cs`, `SiebwaldeApp.Core.Tests/TrackControlHostTests.cs`.
- Modified: `SiebwaldeApp.Core/Model/SiebwaldeApplicationModel.cs`, `SiebwaldeApp.EcosEmu/Server/EcosEmulatorServer.cs`, `SiebwaldeApp/IoC/IoC.cs`, `SiebwaldeApp/ViewModel/SiebwaldeViewModels/SiebwaldeInitPageViewModel.cs`, `SiebwaldeApp/Pages/SiebwaldePages/SiebwaldeInitPage.xaml`.

`EcosEmulatorServer` now tolerates `OperationCanceledException` (shutdown while Koploper is connected) and `ObjectDisposedException` (listener closed with a pending accept); both previously produced unobserved task exceptions on shutdown.

### Tests and checks

- `dotnet build SiebwaldeApp.sln` -> **0 errors**.
- `dotnet test` -> **107/107 passed** (was 100; +7 `TrackControlHostTests` covering simulator start, port release, empty loco repository creation, the real-mode guard, idempotent start, safe stop-when-never-started and the constructor guard).
- Software-only startup validation with a temporary console host (outside the repository, since removed): started the host with the real configuration in simulator mode, confirmed `127.0.0.1:15471` listened and accepted a TCP connection, confirmed a clean stop released the port (`ConnectionRefused`), and confirmed the new settings were read (5 topology blocks, 5 block-map blocks).
- During that validation Koploper was running on this PC and connected to the temporary listener; the external-info direction (C# -> 5700) also connected and reported `[EXT] Loc 1 -> Block 1`. No physical hardware was touched.

### Remaining concerns

1. `CoreSettings` are User-scope: an existing `user.config` can still hold an empty `BlockTopologyConfig` from before the defaults were added. If `CoreConfiguration.BuildBlockTopology()` returns no blocks, use Reload/Reset on the settings page. Real mode would then have no amplifier mapping.
2. The ECoS host starts in real mode even before the firmware occupancy flag is populated, so Koploper sees no occupancy feedback on real hardware yet (firmware TODO).
3. `EcosEmulatorServer` binds loopback only; correct while Koploper is on the same PC.
4. `IoC.Setup()` now constructs the host, so a misconfigured `locos.json` path or an occupied port surfaces at startup; `StartEcosHostAsync` logs and continues rather than failing the app.

### Best next step

**Item 3: switch mapping** (real <-> Koploper designation plus the default initial position), followed by item 4 (divergence check with ECoS stop and operator diagnostics).

## Latest Session (2026-09-19)

Completed on `feature/csharp-cleanup-startup` (all committed and pushed; `git status` clean for tracked files):

- **Increment 6 step 1** (protocol reconnaissance) and **step 2-3**: documented in `docs/koploper-interface.md`.
- **Speed -> PWM**: `AmplifierSpeedMapper` (ECoS 0..127 + direction -> neutral 399, forward 400..799, reverse 398..1, never 0).
- **Routing + look-ahead**: `BlockTopology` extended with switch-conditioned transitions and a no-look-ahead marker (`!`) for station departures; `IOccupancyProvider`; `LookAheadPlanner`; look-ahead wired into `TrackAmplifierHardwareBackend`.
- **Koploper block mapping**: `KoploperBlockMap` (Koploper block -> bezetmelders -> amplifier sections, with reverse lookups). The authoritative oval mapping was extracted from the Koploper HTML export in `Logging\Ovaaltje\`.
- **Occupancy path**: `TrackAmplifierRegisters` (mirrors `TrackAmplifier4.X/modbus/General.h`; HR_STATUS = HoldingReg2, bit 10 = occupied) and `TrackAmplifierOccupancyProvider` (block occupied when any covered section is occupied). `TrackAmplifierOccupancyBridge` forwards occupancy changes to Koploper as ECoS sensor events (event-driven).
- **Composition (option A)**: `TrackControlIntegration` builds the occupancy provider, the real backend, the in-process `SimpleEcosBackend` (when a loco repository is supplied) and the bridge; `Attach()`/`Detach()` subscribe to `ITrackCommClient.AmplifierDataReceived`. The WPF app now references `SiebwaldeApp.Integration`.
- **Editable mapping settings** (Increment 6 item 5, completed): `BlockTopologyConfig` and `KoploperBlockMapConfig` user settings (oval defaults), exposed via `CoreConfiguration.BuildBlockTopology()`/`BuildKoploperBlockMap()`, editable on the settings page with undo and a reset-to-defaults button, and with explicit defaults in both `App.config` files. `BlockTopology.Parse`/`KoploperBlockMap.Parse` now also accept line breaks as separators, so the multi-line settings fields cannot silently produce an empty configuration. Commit `bf2dd81`.
- **Live Koploper session**: emulator trace capture added to the emulator host; verified `create`, `set(id, speedstep[n])`, occupancy events, `[EXT]` position records, the loco sync, and the bezetmelder -> sensor/bit mapping.

Tests: `dotnet test` **100/100 passed**. `SiebwaldeApp.sln` builds with 0 errors.

### Files changed this session

Core (`SiebwaldeApp/SiebwaldeApp.Core`):
- Added: `Model/TrackApplication/Control/IOccupancyProvider.cs`, `KoploperBlockMap.cs`, `LookAheadPlanner.cs`, `TrackAmplifierOccupancyProvider.cs`, `TrackAmplifierRegisters.cs`.
- Modified: `Model/TrackApplication/Control/BlockTopology.cs`, `Model/TrackApplication/Control/KoploperBlockMap.cs`, `Configuration/CoreConfiguration.cs`, `Properties/CoreSettings.settings`, `Properties/CoreSettings.Designer.cs`, `app.config`.

Integration (`SiebwaldeApp/SiebwaldeApp.Integration`):
- Added: `TrackAmplifierOccupancyBridge.cs`, `TrackControlIntegration.cs`.
- Modified: `TrackAmplifierHardwareBackend.cs`.

WPF app (`SiebwaldeApp/SiebwaldeApp`):
- Modified: `SiebwaldeApp.csproj` (added Integration reference), `Pages/SiebwaldePages/SiebwaldeSettingsPage.xaml`, `ViewModel/SiebwaldeViewModels/SiebwaldeSettingsPageViewModel.cs`, `ViewModel/TrackViewModels/TrackAmplifierPageViewModel.cs` (now uses the Core register constants).

Emulator host:
- Modified: `SiebwaldeApp.EcosEmu/SiebwaldeApp.EcosEmu.Host/Program.cs` (console trace capture) - committed earlier in the session.

Tests (`SiebwaldeApp/SiebwaldeApp.Core.Tests`):
- Added: `BlockTopologyRoutingTests.cs`, `KoploperBlockMapTests.cs`, `LookAheadPlannerTests.cs`, `TrackAmplifierHardwareBackendLookAheadTests.cs`, `TrackAmplifierOccupancyBridgeTests.cs`, `TrackAmplifierOccupancyProviderTests.cs`, `TrackControlIntegrationTests.cs`.

Docs:
- Modified: `docs/koploper-interface.md` (large additions), `docs/backlog.md`, `docs/handoff.md`.

### Decisions and assumptions

- **PWM mapping**: neutral 399, forward 400..799, reverse 398..1, never 0 (confirmed by the product owner). Speed steps are 0..127 (from the `ecos-master` library).
- **A Koploper block can have one or more bezetmelders**, not always two; the physical minimum of two applies where precise stopping is required.
- **Bezetmelder = sensor**: bezetmelder `module.point` maps to ECoS sensor id `(module-1)*16 + point`, bit = sensorId-1 in feedback module 100. Verified against the live trace.
- **Occupancy is event-driven, not polled**: the bridge evaluates on `AmplifierDataReceived` and only emits events on change.
- **Do not pre-seed `locos.json`**: Koploper syncs its locos to the central; pre-seeding creates duplicates.
- **Option A**: the WPF app is the composition root and hosts the ECoS backend in-process with the real hardware backend.
- **Assumption**: the current firmware reports a single occupied flag per amplifier section, so all bezetmelders of a Koploper block follow that flag until entry/exit can be distinguished.
- **Assumption**: `TrackAmplifierRegisters` is the single source of truth for the amplifier register layout; the register description is provisional and may change with new firmware.

### Tests and checks performed

- `dotnet test "SiebwaldeApp\SiebwaldeApp.Core.Tests\SiebwaldeApp.Core.Tests.csproj" -c Debug` -> **98 passed, 0 failed**.
- `dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug` -> **0 errors**.
- Live Koploper session against the emulator: loco sync, driving (`set(id, speedstep[n])`), occupancy events, position records - all verified from the trace.
- No hardware was connected or controlled.

### Incomplete / uncertain

1. ~~**App startup wiring (rest of "2-rest")**~~ - **completed in the app-startup wiring session above** (`TrackControlHost`, real/simulator mode, clean shutdown).
2. **Switch mapping** (real <-> Koploper + default init state) not done; switch addresses for the oval are known (1 and 2) but the branch selection (`3>4` vs `3>5`) is still provisional in the topology config.
3. **Divergence check + ECoS stop + operator diagnostics** not started.
4. The firmware occupied flag has a TODO; live occupancy depends on firmware that populates it (the product owner states the real test firmware already returns it).
5. `TrackApplicationVariables` gives all 56 items the same `HoldingReg` array instance (aliasing) - recorded in the backlog.
6. `Logging/` (runtime logs, traces, `locos.json`, the Koploper HTML exports) is untracked and intentionally not committed.

### Best next step

**Finish the app startup wiring**: in `SiebwaldeApplicationModel` (or a new `EcosEmulatorService`) create `TrackControlIntegration` from `CoreConfiguration.BuildBlockTopology()`/`BuildKoploperBlockMap()` plus the loco repository and `KoploperExternalInfoClient`, start the `EcosEmulatorServer`, call `Attach()`, and add a real-vs-simulator mode selection. Then add the two new settings to `App.config`.

## Latest Session (2026-09-17)

Completed today (all on `feature/csharp-cleanup-startup`, pushed):

- Increments 1-7: host logging + init sequencing fix, obsolete test project removed, Pic18/station remnants removed, configuration centralized (`CoreConfiguration`), editable settings page, host detection + init page UI (status dots, larger log, 10 s re-detection), core unit tests.
- Increment 6 step 1-2c: Koploper/ECoS reconnaissance (`docs/koploper-interface.md`), `AmplifierSpeedMapper` (neutral 399, forward 400..799, reverse 398..1, never 0), `BlockTopology`, and the new non-UI `SiebwaldeApp.Integration` project with `TrackAmplifierHardwareBackend`.
- Live Koploper session captured; emulator host now tees console output to `Logging\<date>_EcosEmuTrace.txt`.

Verified live against Koploper:

- `create(10,name["..."],protocol[DCC28],addr[N],append)` -> emulator assigns ids 1002, 1003, ...
- Driving: `set(<ecosId>, speedstep[<n>])` ramping over time; direction via `set(<ecosId>, dir[...])`.
- Occupancy to Koploper: `TX: <EVENT 100>` + `100 state[0x...]` (module 100, 16 inputs, bitmask).
- `[EXT] Loc N -> Block M` is the **current** block; no destination/route is transmitted (`desc="Route onbekend"`).
- Startup dependency: with an empty loco list Koploper reports 0 locos, sends no `create`, and drives nothing.
- Causality: in the simulator occupancy is derived from Koploper position (inverted); on real hardware the amplifier occupancy is the source.

Known issues / next steps:

1. Duplicate locos exist (seeded 1000/1001 + Koploper-created 1002/1003 for addresses 1/2). Seeding was a temporary workaround; the correct route is to let Koploper create them.
2. Add a block-adjacency / chain list to `BlockTopology` (Koploper sends no destination).
3. Add a switch mapping list (real switch <-> Koploper designation + default init state).
4. Implement the look-ahead fallback (one block ahead) in `TrackAmplifierHardwareBackend`.
5. Feed amplifier occupancy to `IHardwareFeedbackSink.OnSensorChangedAsync` (real-system source).
6. Backend selection: real (`TrackAmplifierHardwareBackend`) vs `TrackSimulatorBackend`.
7. Divergence check with ECoS stop command and diagnostics logging (maybe a dedicated diagnostics agent).
8. Settings-page extension for the mapping tables (product owner will supply a Koploper screenshot).

Tests: `dotnet test` 53/53 passing. `Logging/` (16.4 MB of runtime logs, including `locos.json`) is untracked and intentionally not committed. No pull request has been created yet.

## Current Session Status

This session completed a documentation-only workspace migration check and started the product clarification phase for the Siebwalde repository at `C:\Localdata\Siebwalde` (Git repository, `origin https://github.com/jeremysiebers/Siebwalde.git`).

Completed:

- Verified repository boundaries and top-level structure; all `.sln` files and `ProjectReference` entries resolve.
- Corrected documentation references to the pre-migration directory.
- Created `docs/product.md` and captured the product owner's clarification answers in `human_input.md`.
- Recorded confirmed component roles, requirements, and priorities, plus open questions and assumptions.
- Marked prior .NET code-analysis findings as requiring revalidation.

No source code, generated files, dependencies, or Git history were modified. No builds were run and no hardware was connected.

## Files Updated

Migration check:

- `AGENTS.md`, `docs/README.md`, `docs/inventory.md`, `docs/project-knowledge.md`, `docs/analysis-coverage.md`, `docs/build-test.md`, `docs/architecture.md`, `docs/decisions.md`, `docs/backlog.md`, `docs/handoff.md`.

Product clarification:

- `human_input.md` - cleaned and structured product-owner input plus follow-up questions.
- `docs/product.md` - confirmed purpose, roles, requirements, priorities, open questions.
- `docs/decisions.md` - Koploper authority, TrackControllerPic18.X obsolete, Fiddle Yard deferred, C# cleanup first, iterative architecture.
- `docs/backlog.md` - confirmed priority order and new work items.
- `docs/analysis-coverage.md`, `docs/project-knowledge.md`, `docs/architecture.md` - confirmed component roles.

## Confirmed Component Roles

- Active PC application: `SiebwaldeApp` + `SiebwaldeApp.Core` + `SiebwaldeApp.EcosEmu`.
- `TrackController5` drives the 50 ModBus amplifiers and performs MMDC.
- All 50 amplifiers run `TrackAmplifier4.X` with `TrackAmplifierBootLoader.X`.
- `TrackBackplane2.X` is static backplane ModBus code.
- `TrackControllerPic18.X` is obsolete; C# remnants must be removed.
- `FiddleYard` C# control is deferred; embedded C is in `FiddleYard/`.
- `YardController_IOX.X` will be partly split into C# later.
- Koploper owns driving behavior via the ECoS emulator; C# translates commands and returns occupancy.

## Confirmed Priorities

1. C# cleanup and correctness (first increment): revalidate findings, restore core/IoC separation, remove obsolete remnants, tests/simulation. No firmware changes.
2. Create `docs/application-guide.md`.
3. Koploper translation path on the 4-amplifier test layout, with clearer initialization.
4. Firmware: TrackAmplifier4.X setpoints/parameters/MMDC and TrackController5 MMDC.
5. Fiddle Yard and YardController later.

## Product Clarification Progress

Phase 0 is in progress. Round 1 (2026-09-11) confirmed:

- Refined first-increment scope: C# cleanup and correctness plus the program startup/initialization story; Fiddle Yard stays functional but unchanged; obsolete remnants are inventoried and marked (not yet removed).
- `SiebwaldeInitPage` as the startup page with automatic host detection (FiddleYard, Ethernet ModBus master, Koploper, later YardController), dynamic display, human-readable page logging, and start buttons.
- All hard-coded values become editable/persisted via a new menu -> settings option.
- Engineering standards: unit tests for the window/program model, standard coding standards, and a simulation fallback for undetected hosts (FiddleYard generator now; Koploper simulator from the existing ECoS emulator).
- Fiddle Yard logic/behavior/visualization deferred but must keep working.

Round 2 (2026-09-11) confirmed:

- Host detection for now: ping on host name (FiddleYard, ModBus master, YardController) and TCP connect probe (Koploper). A single uniform detection layer is planned later after C-code changes.
- Settings: belong in the core, use `app.config` if appropriate, with a default button per entity and undo (Ctrl-Z).

Round 3 (2026-09-11) confirmed:

- The Koploper/ECoS protocol and port roles are derived from `Ecos ESU info`, the `SiebwaldeApp.EcosEmu` source, and working test examples, not from the product owner. Koploper connects via its own "make connection" button.
- Locomotive location comes from Koploper via the dedicated port. The block-to-amplifier topology is a user-definable `app.config` input.
- C# must pre-command the next block's amplifier when the current block is free (look-ahead).

Round 4 (2026-09-11) confirmed:

- MMDC split: TrackAmplifier4.X does hardware protection and hiccup; TrackController5 monitors PC/amplifier communication and broadcasts emergency stop; C# handles communication monitoring, alarm/logging, and recovery (possibly manual override).
- Koploper emergency button is a software emergency stop forwarded by broadcast. How amplifier errors reach Koploper still needs ECoS datasheet research.
- The Yard stays hand-operated with main-line <-> Yard handover commands. Faller Car via Koploper is an experiment, otherwise a dedicated C# solution.
- A shuttle line (pendelbaan) exists (max 4, min 1 locomotive between 3 stations); its control ownership is undecided.
- YardController is not yet an init-page host.

Round 5 (2026-09-11) confirmed:

- Test layout: 4 amplifiers; IDs may be in a spreadsheet (candidates under `Backup projects/TrackControllerPic18.X/Doc/`, not yet parsed); no fixed loco coupling; block topology is on paper only.
- Shuttle line: no fixed coupling to other elements; two ModBus amplifiers, with the middle station switching the switch street between them via switches/relays.
- A designer agent (UI WPF/WinForms, panels, Fiddle Yard visu, layout diagnostics/manual override) and an integrator/test agent are needed. Creation is deferred until the clarification phase is closed.

Clarification rounds 1-5 are complete and the phase is closed. The requested agents were created:

- `.opencode/agents/designer.md` (subagent).
- `.opencode/agents/integrator.md` (subagent).
- `project-lead.md` delegation section updated.

Development happens on branch `feature/csharp-cleanup-startup`; a pull request to the default branch happens only after a first successful integration with the agents. Restart OpenCode to load the new agents.

## Phase 1 Revalidation Results (2026-09-11)

Prior .NET findings were revalidated against current source by inspection (no build). All confirmed:

- CONFIRMED: `SiebwaldeApp.Core.Host/Program.cs:25` uses `IoC.Kernel`; `SiebwaldeApp.Core.IoC` has only `Logger`/`ConfigureLogger`. Compile error.
- CONFIRMED: `SiebwaldeApp.Tests` uses `IoC.Kernel`/Ninject and references undefined station-domain symbols; it cannot compile.
- CONFIRMED: `SetDefaultPwmSetpointsStep` is skipped by `InitTrackamplifiersStep` and returns `Next("EnableTrackamplifiersStep")`, which does not match the registered `EnableTrackamplifiers` and would fail initialization.
- CONFIRMED: endpoints (`192.168.1.193`, `10000`, `10001`) and the firmware path are hard-coded in both `SiebwaldeApplicationModel.cs` and `Core.Host/Program.cs`; `CoreSettings` is not authoritative for the track transport.
- CONFIRMED: Pic18-era commented remnants in `App.xaml.cs` and station-era UI remnants (`StationSettingsPage`, `StationSettingsPageViewModel`, `ApplicationPage.StationSettings`, `SideMenuViewModel.StationSettingsPage`). Active `TrackControllerCommands` (ModBus) is not a remnant.

Not re-verified: Fiddle Yard error paths, `SendNextFwDataPacket` await behavior, `TrackClientAsync` publish-interval comment, ECoS multi-client behavior. No build/test was executed.

## Increment 1 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- `SiebwaldeApp.Core.Host/Program.cs`: replaced `IoC.Kernel.Bind<ILogFactory>()` with `IoC.ConfigureLogger(...)`.
- `InitTrackamplifiersStep`: now returns `Next("SetDefaultPwmSetpoints")`.
- `SetDefaultPwmSetpointsStep`: now returns `Next("EnableTrackamplifiers")`.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.Core`: 0 errors.
- `SiebwaldeApp.Core.Host`: 1 error before, 0 errors after.
- `SiebwaldeApp.sln`: 0 errors.
- `SiebwaldeApp.EcosEmu.sln`: 0 errors.

Step-name chain verified by source inspection. No Fiddle Yard source changed. `SiebwaldeApp.Tests` was not built (it was obsolete and has since been removed in Increment 2).

## Increment 2 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Removed `SiebwaldeApp/SiebwaldeApp.Tests` (8 tracked files) as an obsolete remnant of the abandoned station-in-C# approach, per product-owner option 1 (archive/remove).
- Removed the leftover generated `bin/`/`obj/` folder of the deleted project.
- Archived the encoded station design intent in `docs/project-knowledge.md`; source recoverable from git commit `104c1e6`.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.sln`: 0 errors after removal.
- `SiebwaldeApp.Core.Host`: 0 errors after removal.

No active project referenced the test project, so builds were unaffected.

## Increment 3 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Removed the dead station-policy settings feature: `StationSettingsPage.xaml(.cs)`, `StationSettingsPageViewModel.cs`, `ApplicationPage.StationSettings`, the `ApplicationPageValueConverter` case, the `SideMenuViewModel.StationSettingsPage` command, and the `TrackMenu.xaml` menu button.
- Removed the commented Pic18-era block from `App.xaml.cs` (and the two fully-commented amplifier view files `TrackAmplifierItemView.xaml.cs` and `TrackAmplifierItemViewModel.cs`).
- Fixed the stale `StartTrackApplication` XML doc.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.sln`: 0 errors.
- No remaining references to `StationSettingsPage`, `ApplicationPage.StationSettings`, `StationPolicy`, `TrackPic18UdpAdapter`, `YardPic18UdpAdapter`, or `TrackAmplifierItemViewModel` in the WPF app source.

Fiddle Yard and the active ModBus `TrackControllerCommands` were not touched. The Page-Removed legacy XAML leftovers (`TrackAmplifierItemView.xaml`, `TrackAmplifierManualControlView.xaml`, `TrackControlView.xaml`) and their empty ViewModels were also removed afterwards, together with their csproj entries.

## Application Guide Created (2026-09-11)

`docs/application-guide.md` now exists and is indexed in `docs/README.md`. It covers the system overview, C# project structure, entry points, IoC, startup, track control, Fiddle Yard, ECoS emulator, the planned Koploper control loop, configuration/endpoints, build/run, current status, and a glossary. It marks planned versus verified behavior.

## Increment 4 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Added `SiebwaldeApp.Core.CoreConfiguration` as the single source of truth for core configuration values.
- Added `CoreSettings` entries `TrckIpAddress` (`192.168.1.193`) and `TrackAmplifierFwPath`, and corrected `TrckSendingPort`/`TrckReceivingPort` defaults from `60000` to `10000`/`10001`.
- `SiebwaldeApplicationModel` and `SiebwaldeApp.Core.Host/Program.cs` now use `CoreConfiguration` instead of hard-coded values.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.Core.Host`: 0 errors.
- `SiebwaldeApp.sln`: 0 errors.
- No hard-coded track IP or firmware path remains in active startup code.

Note: the WPF `App.config` does not yet carry the `CoreSettings` section; the WPF app relies on the Designer defaults (behavior unchanged). The settings UI (edit/default/undo) is still planned.

## Increment 5A Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Added `SiebwaldeApp.Core/Diagnostics/HostDetection.cs`: ping for FiddleYard (`FIDDLEYARD`) and the TrackController (`CoreConfiguration.TrackControllerIpAddress`), TCP connect for Koploper (`127.0.0.1:5700`).
- Reworked `SiebwaldeInitPageViewModel`: per-host status, human-readable log, `DetectHosts`, `InitAllControllers`, `InitTrackController`, `InitFiddleYardController`, and `InitFiddleYardSimulator` commands; start buttons guarded by detection.
- Reworked `SiebwaldeInitPage.xaml`: Detect button, TrackController/FiddleYard/Koploper status rows, start buttons, and a FiddleYard simulator button.
- `FiddleYardController.StartFiddleYardControllerAsync(bool forceSimulator = false)` and `SiebwaldeApplicationModel.StartFYController(bool forceSimulator = false)` support the operator-activated simulator.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.sln`: 0 errors.
- `SiebwaldeApp.Core.Host`: 0 errors.

Only FiddleYard has a simulator option; Koploper and TrackController do not (per product owner). Host detection was not run against live hosts.

## Increment 5B Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Implemented `SiebwaldeSettingsPage` + `SiebwaldeSettingsPageViewModel`: editable core configuration values, `Save`, `Reload`, per-entity reset (`ResetTrack`, `ResetFiddleYard`, `ResetLogging`), and `Undo` bound to Ctrl-Z.
- Changed the editable settings to User scope with setters (`LogDirectory`, `FYSendingport`, `FYReceivingport`, `TrckSendingPort`, `TrckReceivingPort`, `TrckIpAddress`, `TrackAmplifierFwPath`) and moved their config entries to the `userSettings` sections in the WPF `App.config` and the Core `app.config`.

Verified by `dotnet build` (Debug, no hardware):

- `SiebwaldeApp.sln`: 0 errors.
- `SiebwaldeApp.Core.Host`: 0 errors.

Settings persistence and page behavior are code-inspected only; not runtime-verified.

## Init Page UI Improvements (Designer, 2026-09-11)

Delegated to the Designer agent and verified by the Project Lead:

- Larger logging text (`FontSizeLarge`) in the init page log.
- Colored host status dots (`Styles/Indicators.xaml` + `ValueConverters/HostStatusBrushConverter.cs`): amber while checking, green when present, gray when absent.
- Live "Detecting hosts..." indicator using the existing `SpinningText` style.
- Automatic host re-detection every 10 seconds (`DispatcherTimer`), overlap-guarded, with change-driven logging; timer stopped on page `Unloaded` to avoid leaking the transient view model.
- `App.xaml` merges `Styles/Indicators.xaml`.

Verified by `dotnet build "SiebwaldeApp\SiebwaldeApp.sln" -c Debug`: 0 errors. All referenced resources exist. Visual result not runtime-verified.

## App Run Check And Visual Verification (2026-09-11)

`SiebwaldeApp.exe` (Debug) was launched several times: process `SiebwaldeApp` started, window title `Siebwalde Application`, and `Logging\17-09-2026_SiebwaldeApp.CoreLog.txt` recorded a clean startup (PC MAC `18C04D94A26D`, PC IP `192.168.1.13`). No exceptions.

Verified at runtime:
- Host detection works: TrackController `192.168.1.193` reported Present (ping OK); FiddleYard and Koploper Absent in this environment.
- The TrackController page can manually drive PWM on the 4 detected amplifiers and shows the master/amplifier communication overview.
- The init page was captured as a screenshot (PowerShell window capture) and inspected. The first implementation clipped the button text ("etect hosts", "TrackContr", "art FiddleYa", "leYard simu") because the buttons and status columns were too narrow. Fixed by switching the rows to `Auto`/`*` columns, `MinWidth` buttons with `FontSizeSmall`, and wrapping status text; re-verified visually.

Process note: the Designer agent had no screenshot or visual feedback, which is why it did not catch the clipping. A screenshot workflow (PowerShell `CopyFromScreen` on the app window, then reading the PNG) is available for future UI work.

The app process was stopped afterwards.

## Increment 7 Implementation Results (2026-09-11)

Implemented on `feature/csharp-cleanup-startup`:

- Added `SiebwaldeApp/SiebwaldeApp.Core.Tests` (xUnit, `net8.0-windows7.0`), referencing `SiebwaldeApp.Core` and `SiebwaldeApp.EcosEmu`, and added it to `SiebwaldeApp.sln`.
- 23 tests: `TrackApplicationVariables` (PWM clamp 0..799, EmoStop bit 15, slave 0 ignored, pending-write semantics, default PWM setpoints), `TrackAmplifierInitializationServiceAsync` (step chaining, unknown initial/next step, error, Continue-then-Completed), `SimpleEcosCommandParser` (id/options, malformed input, quoted-comma limitation).

Verified: `dotnet test` -> 23/23 passed; `SiebwaldeApp.sln` builds with 0 errors.

Findings recorded while testing:
- A fresh `TrackAmplifierWriteData` starts at `Hr0Value` 0, so requesting PWM 0 on a fresh amplifier is treated as "no change" and is not queued (documented by a test).
- `TrackApplicationVariables` assigns the same `HoldingReg` array instance to all 56 `trackAmpItems` (aliasing); recorded in `docs/backlog.md` for investigation.

Still open: window/UI-model tests, `SendNextFwDataPacket` send-path tests, `SimpleEcosBackend`/`JsonLocoRepository` tests.

## Increment 6 Step 1: Koploper Protocol Reconnaissance (2026-09-11)

Documented in `docs/koploper-interface.md`:

- Port roles resolved: `15471` = `EcosEmulatorServer` (Koploper connects TO C# with ECoS commands); `5700` = Koploper external info (C# connects TO Koploper for locomotive-to-block positions).
- ECoS commands handled: `set`, `get`, `queryObjects`, `request`, `release`, `create`, `delete`. Locomotive control arrives as `set(id, speed[...])` / `set(id, dir[...])` / `set(id, func[i,v])` and forwards to `IHardwareBackend.SetLocoSpeed(address, speed, direction)`.
- Position records: `0x1B`-separated, 5 fields (`&loco`, block, modelTime, pcTime, description).
- Seams: `IHardwareBackend`, `IHardwareFeedbackSink`, `IBlockPositionProvider`.
- Proposed design and open questions recorded (speed range, sensor-id mapping, look-ahead rules, backend replacement).

Next: step 2 (translation-layer design/implementation) and step 3 (4-amplifier test layout). No Koploper runtime test performed yet.

## Resume Instructions

1. Restart OpenCode from `C:\Localdata\Siebwalde` and select the `project-lead` agent.
2. Continue from `docs/product.md`, `human_input.md`, and `docs/analysis-coverage.md`.
3. Increments 1-3 are implemented and verified; propose the next increment (configuration authority/settings UI, or the application guide) for approval.
4. `docs/application-guide.md`. - DONE (2026-09-11): created.
5. Product clarification rounds 1-5 are complete; remaining items are research tasks (Koploper protocol, ECoS overload semantics, topology/spreadsheet).
6. Keep communicating with the user in Dutch; keep documentation and agent instructions in English.

## Constraints To Preserve

- Current authorization covers analysis and documentation only.
- Do not modify application source code, dependency files, generated files, or runtime configuration.
- Do not build, flash, or connect to hardware without explicit user authorization.
- Do not modify Git history.
- Record improvements separately as unapproved follow-up work.
- Support durable knowledge with repository-relative paths and exact symbol names.
- Distinguish verified facts, assumptions, proposals, and open questions.

## Documentation Ownership Plan

- Project Lead owns `AGENTS.md`, `docs/README.md`, `docs/product.md`, `human_input.md`, `docs/analysis-coverage.md`, `docs/inventory.md`, `docs/project-knowledge.md`, `docs/decisions.md`, `docs/backlog.md`, and `docs/handoff.md`.
- Architect owns `docs/architecture.md` and reviews implementation summaries for architectural consistency.
- Developer owns `docs/implementation.md` and `docs/build-test.md` and verifies important architectural claims against code.
- Designer owns UI design documentation and (when authorized) WPF/WinForms implementation files.
- Integrator owns test/simulation strategy documentation and (when authorized) test projects and harnesses.

## Remaining Work

1. Create `docs/application-guide.md` (still missing). - DONE (2026-09-11): guide created and indexed.
2. Revalidate prior .NET code-analysis findings against current source. - DONE (2026-09-11): confirmed; see Phase 1 Revalidation Results.
3. Product clarification rounds 1-5. - DONE (2026-09-11): remaining items are research tasks (Koploper protocol, ECoS overload semantics, topology/spreadsheet).
4. Design and implement the Koploper translation path (later increment).
5. Investigate the additional firmware/hardware/Python source areas in bounded passes.
6. Designer and integrator agents. - DONE (2026-09-11): created.
7. Propose the first fix increment for product-owner approval (Core.Host logger, init sequencing, remnant cleanup, test-project decision). - DONE (2026-09-11): Increment 1 (host logger + init sequencing) implemented and verified.
8. Remove obsolete test project. - DONE (2026-09-11): Increment 2 removed `SiebwaldeApp.Tests`; design intent archived.
9. Increment 3 (proposed): remove confirmed Pic18-era and station-era remnants from the WPF app. - DONE (2026-09-11): station-policy feature and commented Pic18 code removed; build verified.
10. Increment 4 (next): configuration authority / settings UI. The application guide is done.11. **Increment 6 (next major, on request): the Koploper translation path.** Protocol reconnaissance -> translation-layer design -> implement on the 4-amplifier test layout. See `docs/backlog.md`, section "Koploper Translation Path". Do not start before the product owner asks.
12. Treat all remaining `docs/backlog.md` items as unapproved until the user selects implementation work.
