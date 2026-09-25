# Unapproved Follow-Up Work

This backlog records proposed improvements discovered during analysis. None of these items are approved for implementation yet.

## Confirmed Priority Order (from product clarification, 2026-09-11)

The product owner confirmed the following order. Items remain unapproved for implementation until explicitly selected.

1. **C# cleanup and correctness (first increment).** Revalidate prior .NET findings, restore intended core/IoC separation, remove obsolete remnants (`TrackControllerPic18.X` leftovers), establish tests/simulation. No firmware changes.
2. **Human-readable application guide** (`docs/application-guide.md`).
3. **Koploper translation path** on the 4-amplifier test layout, including clearer initialization.
4. **Firmware development**: TrackAmplifier4.X setpoints/parameters/MMDC and TrackController5 MMDC.
5. **Fiddle Yard and YardController** later.

## C# Cleanup And Revalidation (First Increment)

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Revalidate the missing `IoC.Kernel` finding. | DONE (2026-09-11): CONFIRMED. `SiebwaldeApp.Core.Host/Program.cs:25` uses `IoC.Kernel.Bind<ILogFactory>()`; `SiebwaldeApp.Core.IoC` has only `Logger`/`ConfigureLogger`. | Fix unapproved: use `IoC.ConfigureLogger(...)` or another approved API. |
| Revalidate the missing station-domain symbols in tests. | DONE (2026-09-11): CONFIRMED. Tests use `IoC.Kernel`/Ninject and reference undefined `StationTrack`, `TrainType`, `StationSide`, `TrackApplication`, `TrackMetadata`, `TrackRole`, `ITrackIn`, `ITrackOut`. | Test project either compiles or is explicitly archived with rationale. |
| Revalidate the initialization step order (`SetDefaultPwmSetpointsStep`). | DONE (2026-09-11): CONFIRMED. `InitTrackamplifiersStep` returns `Next("EnableTrackamplifiers")` (skips it); `SetDefaultPwmSetpointsStep` returns `Next("EnableTrackamplifiersStep")`, which does not match the registered name and would fail initialization. | Intended order is confirmed and implemented with matching `IInitializationStep.Name` values. |
| Establish configuration authority for endpoints and firmware path. | DONE (2026-09-11, Increment 4): added `CoreConfiguration` backed by `CoreSettings`; `TrckIpAddress` and `TrackAmplifierFwPath` settings added; track port defaults corrected to `10000`/`10001`; startup code no longer hard-codes values. | One authoritative configuration source is documented and used (ties into the settings UI item). |
| Add the `CoreSettings` section to the WPF `App.config`. | DONE (2026-09-11): the WPF `App.config` now declares and defines `SiebwaldeApp.Core.Properties.CoreSettings` with explicit values. | The WPF app.config carries explicit `SiebwaldeApp.Core.Properties.CoreSettings` values. |
| Decide the role of `SiebwaldeApp.Core.Host/Program - Copy.cs`. | DONE (2026-09-11): removed the stale copy and its `<Compile Remove>` csproj entry. | File is removed or documented as intentionally kept. |
| Remove obsolete `TrackControllerPic18.X` remnants from the C# project. | DONE (2026-09-11, Increment 3): removed the station-policy settings feature (`StationSettingsPage` xaml/cs, `StationSettingsPageViewModel`, `ApplicationPage.StationSettings`, converter case, menu command/button) and the commented Pic18 block in `App.xaml.cs`; fixed the stale `StartTrackApplication` XML doc. | Remnants are removed; active `TrackControllerCommands` (ModBus) is kept; build/tests remain green. |
| Decide the Page-Removed legacy XAML leftovers. | DONE (2026-09-11): removed `TrackAmplifierItemView.xaml`, `TrackAmplifierManualControlView.xaml(.cs)`, `TrackControlView.xaml(.cs)` and the empty `TrackControlViewModel`/`TrackAmplifierManualControlViewModel`, plus their `<None Include>`/`<Page Remove>` csproj entries. `SiebwaldeApp.sln` builds with 0 errors. | Resolved. |
| Define modern standards, unit testing, and simulation baseline. | Product owner requested modern programming standards, unit testing, and simulations. | Chosen frameworks/analyzers and a simulation approach are documented and applied. |
| Restore intended core separation and IoC structure. | Product owner requested the intended core and separation (IoC) be put in order. | Core/UI boundaries and IoC composition are documented and consistent. |
| Implement `SiebwaldeInitPage` host detection and dynamic display. | DONE (2026-09-11, Increment 5A): `HostDetection` service (ping for FiddleYard/TrackController, TCP connect for Koploper); page shows per-host status, a step/state log, and start buttons enabled only for detected hosts. | Expected hosts are probed and shown with present/absent state; page shows logical steps/states; start buttons enable detected hosts. |
| Add menu -> settings option for hard-coded values. | DONE (2026-09-11, Increment 5B): `SiebwaldeSettingsPage` edits and persists the core configuration values, with per-entity reset and undo (Ctrl-Z). Editable settings changed to User scope. | Settings service lives in core using `app.config`; UI edits and persists values; per-entity default button and undo work; no hard-coded endpoint/path remains in active startup code. |
| Build one uniform host detection layer (later). | Product owner wants a single detection layer after C-code changes. | Firmware exposes a stable detect/connect mechanism and the C# layer uses one uniform implementation. |
| Add simulation fallback for undetected hosts. | Product owner requires a simulation option per host. | Undetected hosts can be started in simulation; FiddleYard uses its existing generator; a Koploper simulator builds on the ECoS emulator. |
| Add unit tests for the window/program model. | PARTIAL (2026-09-11, Increment 7): `SiebwaldeApp.Core.Tests` added with 23 tests for `TrackApplicationVariables`, `TrackAmplifierInitializationServiceAsync`, and `SimpleEcosCommandParser`. Window/UI-model tests still to be added. | Existing window/program-model behavior is covered by tests before/while refactoring. |
| Cover the firmware-packet send path with tests. | `SendNextFwDataPacket.ExecuteAsync` now awaits `SendAsync`; not covered by tests yet. | A test asserts send ordering and failure propagation via a fake `ITrackCommClient`. |
| Cover `SimpleEcosBackend` and `JsonLocoRepository`. | Not covered yet; parser is covered. | Command handling and JSON persistence have unit tests. |
| Investigate `TrackApplicationVariables` HoldingReg aliasing. | All 56 `trackAmpItems` receive the same `HoldingReg` array instance from the constructor. | Confirmed whether this is intended; fix or document. |

## Koploper Translation Path (Later Increment)

Status: this is the agreed NEXT MAJOR step, referred to as **Increment 6**. The product owner will ask for it explicitly; do not start it before that. When it is requested, resume from this section.

Increment 6 outline:
1. Protocol reconnaissance (bounded): derive the exact ECoS per-encoder command and the two port roles (`5700` vs `15471`) from `Ecos ESU info` and `SiebwaldeApp.EcosEmu`; document the findings. - DONE (2026-09-11): documented in `docs/koploper-interface.md`. Port roles resolved: `15471` = ECoS command server (Koploper connects to C#), `5700` = Koploper position info (C# connects to Koploper). Per-encoder command = `set(id, speed[...])` / `set(id, dir[...])` -> `IHardwareBackend.SetLocoSpeed`.
2. Design the translation layer: Koploper command -> block -> amplifier setpoints; occupancy feedback back to Koploper; block-to-amplifier topology in `app.config`; look-ahead (pre-command the next block's amplifier).
3. Implement on the 4-amplifier test layout.


| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Document the per-encoder Koploper command format and the dedicated locomotive-location port/protocol. | Product owner Round 3: derive from `Ecos ESU info`, the C# ECoS emulator code, and working test examples. | Protocol and port roles are documented from source/data; which port is the connection port vs the info port is confirmed. |
| Implement locomotive-to-amplifier setpoint translation that follows the locomotive. | Product owner requirement. | On the 4-amplifier test layout, Koploper commands drive the correct amplifiers. |
| Add user-configurable block-to-amplifier topology in `app.config`. | Product owner Round 3: block 1 = amp 1 -> block 2 = amp 2 is user-definable. | Topology is editable and persisted; C# maps Koploper block/location data to amplifiers. |
| Implement look-ahead amplifier commanding. | Product owner Round 3: when a block is free, command the next block's amplifier. | Adjacent free block is pre-commanded so the locomotive crosses smoothly. |
| Feed amplifier occupancy back to Koploper. | Product owner requirement. | Occupancy reaches Koploper and influences the next control behavior. |
| Implement clearer initialization: Ethernet target/online checks, amplifier init, FW checks/downloads, then Koploper connection. | Product owner requirement. | Initialization surfaces predictable status and failures. |
| Translate Koploper switch-street commands into Fiddle Yard TOP/BOTTOM shift commands. | Product owner requirement. | Switch-street commands map to the Fiddle Yard shift protocol. |
| Move hardware signals to software. | Occupancy cannot remain hardwired to the Fiddle Yard controller once real trains run. | Signal handling is defined and implemented in software. |
| Add a block-adjacency / chain list in C#. | Needed to derive the next block for look-ahead (Koploper does not send the destination). | The chain is configurable and used to pre-command the next block's amplifier. |
| Add a switch mapping list (real <-> Koploper + default init state). | Real switch x corresponds to Koploper switch designation y, with a default init state (straight/diverging). | Mapping is configurable and documented; Koploper switch commands can be translated. |
| Feed amplifier occupancy to Koploper. | Real-system source of occupancy: amplifier -> C# (`IHardwareFeedbackSink.OnSensorChangedAsync`) -> ECoS event -> Koploper. | Amplifier occupancy reaches Koploper and drives block updates. |
| Add a divergence check with ECoS stop and diagnostics logging. | If C# and Koploper drift apart, C# should command Koploper to stop and log what diverged. | Divergence is detected, Koploper is stopped via ECoS, and the mismatch is logged with detail. |
| Consider a dedicated diagnostics agent. | Product owner suggestion for divergence/diagnostic analysis. | Role and outputs defined and approved before creation. |
| Operator diagnostics for occupancy mismatches. | When Koploper data does not match the measured amplifier occupancy (and vice versa), the operator needs a clear signal and detail. | Mismatches are detected and surfaced; a simple track-plan view with the data is available. |
| Add a Koploper-block to bezetmelder/amplifier mapping. | Koploper blocks are collections of occupancy detectors spanning several amplifier sections; terminology must stay sharp. | Koploper block -> bezetmelders -> amplifier sections is configurable and used by the translation layer. |
| Validate `locos.json` recreation and auto-sync. | `locos.json` was removed; it must be recreated when Koploper starts and be auto-synced because it is new. | Starting the emulator + Koploper recreates the file and populates it via the Koploper sync. |

## MMDC, Safety, And Yard (Later)

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Research ECoS overload/stop semantics for amplifier errors. | Product owner Round 4: how to communicate over-temp/over-current to Koploper is unknown; ECoS may send a stop on overload. | Findings documented from datasheets/internet; a chosen reporting path is specified. |
| Implement emergency-stop broadcast on PC communication loss. | Product owner Round 4: TrackController5 stops all track amps on loss. | Communication loss triggers a deterministic emergency-stop broadcast; Koploper emergency button forwarded the same way. |
| Implement C# communication monitoring, alarm, and recovery. | Product owner Round 4. | C# monitors ModBus master and Koploper, alarms/logs, and offers manual override recovery. |
| Implement Yard handover commands (main line <-> Yard). | Product owner Round 4. | Operator request reaches Koploper; return handover parks and hands over a locomotive. |
| Decide Faller Car control (Koploper experiment vs dedicated C#). | Product owner Round 4: experiment. | Experiment outcome documented; either Koploper-based config or a dedicated C# solution chosen. |
| Define shuttle line (pendelbaan) control. | Product owner Round 4: max 4, min 1 locomotive between 3 stations. | Control ownership (Koploper vs C#) and integration with the main line are documented and scheduled. |

## Build And Test Health

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Fix `SiebwaldeApp.Core.Host` logger setup. | DONE (2026-09-11, Increment 1): `Program.cs` now uses `IoC.ConfigureLogger(...)`; host builds with 0 errors. | Host builds and configures logging through an existing or approved API. |
| Reconcile `SiebwaldeApp.Tests` with active source. | DONE (2026-09-11, Increment 2): the obsolete test project was removed; design intent archived in `docs/project-knowledge.md`; recoverable from git commit `104c1e6`. | Resolved. |
| Add the test project to the appropriate solution if it is active. | CANCELLED (2026-09-11): the test project was obsolete and removed. | Not applicable. |

## Track Application

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Correct initialization step sequencing around `SetDefaultPwmSetpointsStep`. | DONE (2026-09-11, Increment 1): `InitTrackamplifiersStep` -> `SetDefaultPwmSetpoints` -> `EnableTrackamplifiers`; all names resolve. | Intended step order is verified and implemented with matching `IInitializationStep.Name` values. |
| Await firmware packet sends. | `SendNextFwDataPacket.Execute` calls `SendAsync(...).ConfigureAwait(false)` without awaiting. | Firmware send ordering and error handling are deterministic. |
| Decide settings source for track ports/IP. | CONFIRMED 2026-09-11: startup code hard-codes `192.168.1.193`, `10000`, `10001`, and the firmware path; `CoreSettings` has ports but no IP/path and is unused for the track transport. | One authoritative configuration source is documented and used (settings UI item). |
| Add bounded timeout/recovery behavior for initialization. | `TrackAmplifierInitializationServiceAsync` has a per-read timeout but no overall timeout. | Initialization failures surface predictably with logged reason and cancellation support. |

## Fiddle Yard

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Add cancellation/stop path for `NewReceiver`. | `NewReceiver` uses a blocking receive loop without a verified cancellation API. | Fiddle Yard shutdown can stop receiver tasks without process exit. |
| Stop swallowing connection errors silently in `NewSender.ConnectUdp`. | Code inspection found catch/ignore behavior. | Connection failures are logged and surfaced to caller. |
| Review TODO/TBD error paths in `FiddleYardApplication`. | Several state-machine branches have incomplete recovery behavior. | Known failure states have documented and tested behavior. |

## ECoS Emulator

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Clarify multi-client behavior. | `SimpleEcosBackend` stores one `_currentWriter`. | Multi-client policy is documented and enforced. |
| Harden ECoS command parsing. | `SimpleEcosCommandParser` uses simple comma splitting. | Parser behavior for quoted values and malformed commands is tested. |
| Add graceful stop for Koploper external info and simulator loops. | DONE (2026-09-23, PR #8, merge commit `5fb751552b368015ba17f796d86f41c1d7fa7c13`): `EcosEmulatorServer`, `KoploperExternalInfoClient` and `TrackSimulatorBackend` now have idempotent, bounded `StopAsync` (tracked+awaited tasks, disposed listener/connection, restartable) plus a non-blocking sync `Stop` for the in-process host. | Host shutdown cancels and awaits background tasks. |

## Workspace Hygiene

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Decide role of `SiebwaldeApp.EcosEmu_old`. | Not referenced by discovered solutions and targets `net9.0`. | Kept, archived, or removed by explicit user decision. |
| Decide role of `SiebwaldeApp/SiebwaldeApp.zip`. | Archive exists but was not inspected. | Archive is documented as reference, backup, or obsolete artifact. |
| Reconcile the superseded pre-migration copy. | `C:\Users\jerem\Downloads\Test` still exists with a subset of the workspace. | Kept, archived, or removed by explicit user decision. |
| Review generated MPLAB metadata with stale absolute paths. | `YardController.X/nbproject/private/private.xml` references `C:/Localdata/GIT/Siebwalde/...`; generated `.sdb`/`.cmf` files contain historical paths. | Stale generated metadata is regenerated or documented as non-authoritative. |
| Add source-control ignore rules for generated firmware output. | The repository is a Git repo; many MPLAB `dist/`, `build/`, and `nbproject/private/` outputs are untracked. | `.gitignore` policy for firmware build output is explicit. |

## Documentation Deliverables

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Create the consolidated human-readable application guide. | DONE (2026-09-11): `docs/application-guide.md` created with system overview, structure, workflows, endpoints, status, and glossary. | A coherent guide covers architecture, workflows, and verified usage with diagrams, and distinguishes verified facts from revalidation items. |
| Revalidate prior .NET code-analysis findings. | DONE (2026-09-11): revalidated by source inspection; the key claims are CONFIRMED (see `docs/build-test.md`). Fiddle Yard/emulator items remain not re-verified. | Remaining not-re-verified items are checked when those areas are touched. |
| Investigate additional source areas. | Firmware/hardware/Python areas are inventoried only. | Each area has at least a bounded inventory note; deep analysis is scheduled by priority. |

## YardController And Faller Car (Future)

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Plan the split of `YardController_IOX.X` between embedded C and C#. | Product owner wants more flexibility and user configuration. | A documented split lists which functions stay embedded and which move to C#. |
| Evaluate controlling the Faller Car System via Koploper. | Product owner idea, unapproved. | Feasibility is assessed and either scheduled or dropped. |

## Process And Agents

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Add a "designer" agent. | Product owner Round 5: needed for UI (WPF/WinForms) visual design, control/overview panels, Fiddle Yard visualization, and a possible whole-layout diagnostics/manual-override view. | DONE (2026-09-11): `.opencode/agents/designer.md` created; requires an OpenCode restart. |
| Add an integrator/test agent. | Product owner Round 5: considered necessary. | DONE (2026-09-11): `.opencode/agents/integrator.md` created; requires an OpenCode restart. |
| Define the layout/amplifier topology in `app.config`. | Product owner Round 5: topology is on paper only; amplifier IDs may be in candidate spreadsheets under `Backup projects/TrackControllerPic18.X/Doc/`. | Topology is transcribed into configuration and validated against the layout. |
| Build a whole-layout diagnostics and manual-override visualization. | Product owner Round 5: to compare with Koploper data and manually operate switch streets/locomotives. | A view shows layout state and allows manual element/loco control. |

## Workflow v1 Process-Improvement Candidates

Discovered during the Workflow v1 bootstrap. Not approved for implementation; recorded so they are not lost. Implement in a dedicated workflow/governance increment, not mixed into product work.

| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| `opencode.json` bash ask-guards are best-effort only. | The guards force push / hard reset / `clean` / history rewrite / interactive rebase / force branch delete, but pattern bypasses exist (for example `git -C`, aliases, refspec force) and `git clean -n` also triggers. Documented as a known limitation in `docs/current-state.md`. | Either accept and keep the documented limitation, or replace the guards with a more reliable mechanism. Workflow v1 remains the authority boundary either way. |
| Active State authority invalidation is representable but not auto-detected. | `.opencode/workflow/active-state-check.ps1` validates `pending_authority` structure but does not compare `bound_revision` to Git or auto-mark `INVALIDATED`. | A revision change can be detected and surfaced as authority invalidation, or the limitation is accepted explicitly. |
| Workflow v1 bootstrap merged to `master`. | Merged via PR #5 (`Introduce Siebwalde Development Workflow v1`), merge commit `a6b3467aca22e2f8be79c1133a5509f351c78135`; post-merge CI PASS. | DONE (2026-09-22). |

## Increment 6 Status (2026-09-19)

Done this session:

- Protocol reconnaissance, terminology, oval mapping, bezetmelder -> sensor mapping: `docs/koploper-interface.md`.
- `AmplifierSpeedMapper`, `BlockTopology` routing + no-look-ahead marker, `IOccupancyProvider`, `LookAheadPlanner`, `KoploperBlockMap`, `TrackAmplifierRegisters`, `TrackAmplifierOccupancyProvider`.
- `TrackAmplifierOccupancyBridge` (event-driven) and `TrackControlIntegration` (option A composition).
- `BlockTopologyConfig` + `KoploperBlockMapConfig` editable on the settings page, with undo and a reset-to-defaults button, and explicit defaults in both `App.config` files.
- `BlockTopology.Parse`/`KoploperBlockMap.Parse` accept line breaks as separators (the settings fields are multi-line), so an unprefixed section containing `>` is treated as routes instead of being silently dropped.
- `locos.json` recreation/auto-sync validated; `locos.json` no longer pre-seeded.

Still open (next steps): see **Open work** at the end of this file, which is the single authoritative and categorised list.

## Increment 6 App-Startup Wiring (2026-09-19, second task)

Done:

- `TrackControlMode` and `IEcosHostService` added to Core; `SiebwaldeApplicationModel` now takes the host and starts/stops it.
- `TrackControlHost` (Integration) composes and owns the ECoS host, the ECoS server (15471) and the Koploper external-info client (5700), for both real and simulator modes.
- Real mode starts automatically at the end of `StartTrackApplication()`; simulator mode is an explicit operator action on the init page.
- `EcosEmulatorServer` shutdown now tolerates cancellation and a disposed listener.
- Software-only validation: the host brought up 127.0.0.1:15471 in simulator mode, accepted a connection, and released the port on stop.

Completed from the previous "still open" list: **Finish the app startup wiring.**

Remaining concern: `CoreSettings` are User-scope, so an existing `user.config` can still hold an empty `BlockTopologyConfig` from before the defaults were added. **Resolved in the ECoS host hardening pass below.**

## Increment 6 ECoS Host Hardening (2026-09-19, third task)

Done:

- Explicit mode-transition semantics with a returned result: `AlreadyActive` (same mode), `Rejected` (simulator requested while real runs), `Transitioned` (real requested while simulator runs), plus `NotAvailable`/`Failed`. Real mode is authoritative.
- Invalid real requests are rejected before a running host is touched; a failed transition releases the port and the external-info client.
- Active mode exposed via `SiebwaldeApplicationModel.ActiveEcosMode` and shown on the init page (`EcosModeStatus`).
- `EcosEmulatorServer` sets `ReuseAddress` so 15471 rebinds immediately during a transition.
- Blank persisted mapping settings fall back to the declared default from the settings metadata; malformed non-empty values are still used as-is.

The "Still open (next steps)" list in the app-startup-wiring section above remains the authoritative next-steps list.

## Increment 6 Item 3: Switch Mapping (2026-09-19, fourth task)

Done:

- `SwitchMapping` (Core) parses `SwitchMapConfig`: `switches: <ecos>:<physical>[:inverted][:g|r|keep]`, recording invalid and duplicate entries instead of producing a plausible-but-wrong mapping.
- `SwitchController` (Integration) translates ECoS switch requests to physical drives, tracks logical (ECoS) and physical positions separately, and initializes configured defaults.
- `SwitchTranslatingHardwareBackend` applies the shared translation in front of whichever hardware backend runs, so real and simulator mode use one control path.
- `IHardwareBackend.SetSwitch` now returns `bool`; the ECoS backend sends no state event when the command reached no output.
- Proven route conditions shipped: `3>4@1:0, 3>5@1:1` (from `Logging\19-09-2026_EcosEmuTrace.txt`, 6 occurrences per route).
- Settings page field with save/reset/undo and blank-value fallback.

New items opened by this work:

| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| Real switch output path (accessory decoder). | `TrackAmplifierHardwareBackend.SetSwitch` returns false and only logs. | Mapped switches are actuated on the real layout. |
| Real-layout power-on switch positions. | `SwitchMapConfig` default is `keep` because the rest position is unknown. | Confirmed positions are configured as `g`/`r`. |
| Physical switch feedback. | `ISwitchOutput.SetPosition` is bool but no real feedback exists; `UnobservableSwitchObserver` reports "not observable". | A real observer exists and the ECoS state reflects confirmed hardware state. |
| Signals 51..55 as switches. | Koploper commands them via `switch[...]`; they are unmapped and ignored. | Signals are either mapped or deliberately documented as out of scope. |

## Increment 6 Item 4: Divergence, Safety Stop And Diagnostics (2026-09-19, fifth task)

Done:

- Core diagnostics model: `DiagnosticSeverity`, `DiagnosticCode`, `SafetyAction`, immutable `ControlDiagnostic` with loco/block/switch context, and `ControlDiagnostics` with a bounded history plus a separate latched unsafe state.
- Requested / Commanded / Observed kept separate; unavailable feedback is never reported as confirmation.
- `ControlSafetyGuard` with idempotent per-fault stop (no stop storm) and explicit-reset-only recovery.
- `DivergenceChecker` covering unmapped switch, route/switch mismatch, unknown switch state, command-not-applied, commanded/observed mismatch and occupancy mismatch, reusing `BlockTopology`, `SwitchController` and `IOccupancyProvider`.
- Safety stops reuse the existing paths: per-loco `SetLocoSpeed(address, 0, dir)`, unattributable `SetPower(false)`.
- Thin init-page surface: mode, control-path health, latest diagnostic, reset button.

New items opened by this work:

| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| Simulator occupancy provider. | Simulator occupancy arrives as ECoS sensor events, so `OccupancyAvailable` is false there. | An `IOccupancyProvider` over the simulator exists and occupancy divergence is checked in simulator mode. |
| Latched fault does not block new commands. | The guard prevents repeated stops but not a new Koploper command. | **RESOLVED 2026-09-19**: `ControlSafetyInterlockBackend` refuses non-zero movement while a StopRequired fault is latched, with reset revalidation. |

## Increment 6 Safety Movement Interlock (2026-09-19, sixth task)

Done:

- `ControlSafetyInterlockBackend` decorates the hardware backend in both modes, so every ECoS movement command passes one policy.
- Loco-scoped latch refuses non-zero movement for that loco only; layout latch refuses it for all locos and refuses power-on. Stops, power-off and switch commands always pass.
- `IHardwareBackend.SetPower`/`SetLocoSpeed` return `bool`; a refused movement is answered with `<END 8 (SAFETY_INTERLOCK)>` and produces no speed event.
- Rejections are reported once per loco per latch (`MovementRejectedBySafety`).
- `ControlSafetyGuard.Reset()` revalidates through `DivergenceChecker.IsResolved` and refuses while the condition persists (`ResetRefused`).

**Increment 6 is complete.** The open work below is categorised by what is actually needed to close it. Nothing here is a code gap in the implemented translation, safety or diagnostics path.

## Open work

### Production control trace (implemented 2026-09-20)

The supplemental Integrator review concluded `PRODUCTION TRACE INCOMPLETE`; the dedicated production trace is now implemented and software-verified (commits `4b14186` production trace + tests, `01414e1` harness). Independent review verdict `CONTROL TRACE REVIEW PASS`, and the physical post-fix validation is now **COMPLETE** (2026-09-21; see `docs/handoff.md`). The `FileLogger` hardening and correlation-id items below remain open as post-increment follow-ups.

| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| **Dedicated production Koploper/ECoS control trace implemented.** | `ControlTraceLogger`/`IControlTrace` (Core) write one dedicated daily file `{CoreSettings.LogDirectory}{dd-M-yyyy}_ControlTraceLog.txt` through the existing `ILogFactory`/`BaseLogFactory`/`FileLogger`, registered in `SiebwaldeApp/IoC/IoC.cs` (`ControlTraceLogging.Register`). Events: `CONTROL_TRACE_START`, `ECOS_COMMAND`, `SPEED_DECISION`, `BLOCK_TRANSITION`, `AMPLIFIER_COMMAND`, `AMPLIFIER_WRITE`, `TRACKER_ADD/REMOVE/TRANSFER`, `SAFETY_STOP`, `SAFETY_STOP_RESULT`, `SAFETY_ESCALATION`, `EMERGENCY_TARGET_SET`, `MANUAL_CONTROL`, `ABNORMAL`. | Done: production registration creates the file automatically; formatting/causal-sequence tests pass; offline parsing is practical (stable `EVENT=`/`key=value` payload, invariant numbers, deterministic lists). |
| **`FileLogger` limitations reused, not redesigned.** | `FileLogger` has no rotation/retention, is not thread-safe, uses `File.AppendAllText` and silently swallows write failures. `ControlTraceLogger` serializes only its own writes; the trace file is written only by the `ControlTraceLog` instance, so trace records cannot interleave. | Documented. A future hardening task (thread-safe/rotating writer) needs explicit approval and was deliberately not part of the trace task. |
| **Correlation-id decision: not added.** | No existing request/event identity propagates through `IHardwareBackend`/`IAmplifierNeutralizer`/`AmplifierCommandTracker`; adding one would require invasive signature changes through several layers. | Decision: stable millisecond timestamps plus loco/amp/event fields suffice for offline reconstruction; revisit only if parsing proves insufficient. |
| **Two previously identified low-cost safety tests added.** | `AmplifierClassificationAndSafetyDomainTests.EmptyGroupConfiguration_IsEquivalentToEmpty_AndNeverInfersMainRailway`; `AmplifierClassificationAndSafetyDomainTests.ConfiguredMountainRailway_IsIncludedByStrongestEmergency_ButNotByOrdinaryLayoutStop`. | Done. |

### Software follow-up

| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| **SAFETY REVIEW FAIL (HIGH PRIORITY) - the stop-reachability fallback treated backplane slaves 51..55 as track amplifiers. FIXED IN SOFTWARE 2026-09-20 (new commit on `feature/safety-stop-reachability`); independent software re-review PASS (device-class safety) and physically validated 2026-09-21 (`TEST B - BACKPLANE NON-TARGETING PASS`; physically proven for detected address 51).** | Independent Integrator review of the stop-reachability fix (`SOFTWARE REVIEW FAIL`): the amplifier-centric fallback derived targets with `SlaveNumber != 0 && SlaveDetected != 0` without restricting to the physical track-amplifier class, so detected backplane/configuration slaves 51..55 entered the target set. Writing the track-amplifier neutral value 399 (`0x018F`) to a backplane module's HoldingReg0 is invalid because `TrackBackplane2.X/main_proto_backplane.c` uses HR0 `ActValue` as a configuration/enable word. | Done: authoritative `TrackAmplifierAddress` (`1..50` track amplifiers, `51..55` backplane) enforced centrally in `GetKnownPhysicalAmplifiers`, `NeutralizeAmplifiers`, `TrackApplicationVariables.SetDesiredAmplifierControl`, `AmplifierCommandTracker`, and every topology/detected-amplifier iteration. Regression tests in `AmplifierClassificationAndSafetyDomainTests` (physical class, backplane exclusion with no HR0/PWM write, unmapped/spare amplifier, grouping independence). |
| **ARCHITECTURE DECISION PENDING (Product Owner) - complete track-amplifier group/domain configuration and cross-domain emergency policy.** | `TrackAmplifierGroups`/`TrackAmplifierGroupsConfig` now exist (main railway / mountain railway / spare) but are empty by default; the planned installation uses the same `1..50` hardware for main railway, ~2 mountain-railway amplifiers and possibly spares. | The maintainer can answer which addresses are legitimate track amplifiers, main railway, mountain railway, installed spares, and backplane/configuration devices. An explicit Product Owner decision defines the behaviour for loco stop / main layout stop / mountain stop / strongest emergency neutralization, and the treatment of `Spare`. The current strongest emergency (`StopLayout`) targets all legitimate track amplifiers the control path knows about; it must not be narrowed or expanded by inference. |
| **SAFETY ARCHITECTURE GAP (investigate) - startup/restart neutral guarantee is not established or observed by C#.** | `TrackAmplifier4.X/regulator.c` `REGULATORxINIT()` sets `PWM3_LoadDutyValue(399)` and `PetitHoldingRegisters[HR_PWM_COMMAND].ActValue \|= 399` at amplifier startup (a per-amplifier firmware default). C# initialization (`SetDefaultPwmSetpointsStep`) only sets the in-memory observed image to 400 and does not send neutral 399; C# never observes neutral before allowing movement. The master-reset physical effect is not fully source-verifiable from the committed C#/firmware. | A documented, tested guarantee that every configured track-amplifier operational group is physically neutral after startup/restart before movement is allowed, or an explicit accepted-risk statement. Do not modify firmware without authorization. |
| **SOFTWARE FOLLOW-UP - the manual `SetAmplifierControl` path is outside loco ownership.** | `SiebwaldeApplicationModel.SetAmplifierControl` queues an HR0 command directly through `TrackApplicationVariables.SetDesiredAmplifierControl`; it is not recorded in `AmplifierCommandTracker`, so the invariant "every successfully commanded non-neutral track amplifier remains represented until neutralized" does not hold for loco-scoped tracking. The path is runtime (the manual page) and can run while the safety architecture is active. | Confirmed as intentionally outside loco ownership and documented: the strongest physical neutralization (`StopLayout`) reaches a manually commanded, detected legitimate track amplifier, but a loco-scoped stop cannot cover it. A stronger guarantee (tracking manual commands under an explicit non-loco owner at the shared choke point) is a proposed follow-up that needs Project Lead approval. |
| **SAFETY DEFECT (HIGH PRIORITY) - physical stop reachability after locomotive block mapping is lost.** **PHYSICALLY CONFIRMED 2026-09-20** (`STOP REACHABILITY DEFECT CONFIRMED`; source classification `SOURCE-CONFIRMED STOP-REACHABILITY GAP`). Also `STOP FAILURE REPORTING DEFECT CONFIRMED`; layout stop fallback `LAYOUT STOP FALLBACK PASS`. **FIXED IN SOFTWARE 2026-09-20 (commit `66d75d0`/see `docs/handoff.md`); PHYSICALLY VALIDATED 2026-09-21: `TEST A - STOP REACHABILITY PASS` (physical motor stop), `TEST B - BACKPLANE NON-TARGETING PASS` (detected backplane address 51), `TEST C - UNMAPPED AMP6 EMERGENCY REACHABILITY PASS`, `PHYSICAL B/C BINARY-PROVENANCE VALIDATION PASS`. `ORIGINAL SAFETY DEFECT CLOSED: YES`. **MERGED / CLOSED 2026-09-21: PR #4 "Fix safety-stop reachability and add production control tracing", merge commit `3b275fa27c9197400ee40cbfa5759450443535d3` into `master`, post-merge CI PASS.** | Architect investigation 2026-09-20 (`feature/safety-stop-reachability`): `EcosHardwareStopSink.StopLoco` depends entirely on current block-to-amplifier resolution (`TrackAmplifierHardwareBackend.SetLocoSpeed:89-100`); when resolution fails it writes nothing yet is logged and treated as a successful stop (`EcosHardwareStopSink.cs:42-44`, and `ControlSafetyGuard.cs:183` discards the result). No retained per-loco or per-amplifier physical target is consulted by any stop path. Block transitions never neutralize the vacated amplifier (`SimpleEcosBackend.OnBlockEntered`), so a previously commanded amplifier can remain non-neutral; look-ahead can leave a second amplifier non-neutral when planning fails during a stop. The amplifier-centric layout stop `TrackAmplifierHardwareBackend.SetPower(false)` iterates all `BlockTopology` blocks and is mapping-independent, but it is not escalated to automatically and omits detected-but-unmapped amplifiers. `CommandNotApplied`/`BackendUnavailable` exist but are not raised by the stop path, so a failed stop was silent at the safety layer. **Live validation 2026-09-20 (harness commit `03f5221`, branch `feature/safety-stop-reachability`): after a real logical block 1 -> block 3 transition, amplifier 1 stayed at HR0 416 with no neutral write; the real loco-scoped safety stop (Stage 5) neutralized the resolved amp 3 to 399 but left amp 1 at 416, while `EcosHardwareStopSink.StopLoco` returned `True` and no failure diagnostic was emitted (`STOP REACHABILITY DEFECT CONFIRMED`, `STOP FAILURE REPORTING DEFECT CONFIRMED`). The layout stop `SetPower(false)` then neutralized amp 1 to 399 (`LAYOUT STOP FALLBACK PASS`); amp 6 remained untargeted. See `docs/handoff.md`.** | Answer: can the mapping actually disappear while an amplifier is still driving non-neutral PWM in normal runtime? does the amplifier retain its previous setpoint? can the stop path identify the last commanded physical amplifier by another path? if not, what state must be retained so an emergency/safety stop can always reach the last commanded actuator? Recommended (NOT yet approved): (1) minimal safety correction - honour the stop result, emit a failure diagnostic, and escalate to the amplifier-centric neutralization path; (2) broader cleanup - amplifier-centric commanded-state ownership with confirmed neutralization, vacated-block neutralization, look-ahead target retention, and commanded-vs-observed PWM confirmation. A controlled physical reproduction plan exists (amplifier 1, low PWM, operator-in-the-loop, harness substituting only the block source). Do not implement now. |
| **DOCUMENTATION/CODE-QUALITY FOLLOW-UP: `LocoState.Direction` XML documentation does not match the implementation convention.** | Software review 2026-09-20: the XML doc on `LocoState.Direction` (`SimpleEcosBackend.cs`) says `1 = forward` / `-1 = reverse`, while the implementation uses `0 = forward`, non-zero = reverse (`AmplifierSpeedMapper`). No functional defect demonstrated. | Correct the XML documentation to `0 = forward, non-zero = reverse`. Trivial cleanup; does not block the branch. |
| ~~**CONFIRMED PRODUCT DEFECT: `AmplifierSpeedMapper` assumes 127 speed steps, so DCC28 speed is under-scaled.**~~ **Fixed in software and live-validated 2026-09-20.** | Live validation 2026-09-19: the locomotive protocol is `DCC28` and Koploper/ECoS supplied steps `0..28`, but `ToPwm` scaled by 127. Live DCC28 step 24 produced only **~PWM 475** instead of approaching 799, so the motor never reached the top of the usable 400..799 range. The root cause was that `SimpleEcosBackend`'s `opt.StartsWith("speed")` branch also matched `speedstep[...]` and passed the raw protocol step downstream. | Done: `ProtocolSpeedNormalizer` normalizes `speedstep[...]` (DCC28 `0..28` -> `0..127`, round-half-up) at the ECoS boundary, `speed[...]` stays normalized, unknown protocols are refused explicitly, and the hardware layer stays protocol-independent. Regression tests added (`ProtocolSpeedNormalizerTests`, `SimpleEcosBackendSpeedNormalizationTests`). **Live 2026-09-20: full `speedstep[0..28]` ramp on amplifier 1, HR0 399..799 (step 24 -> 109 -> 742), motor responded, Koploper `<END 0 (OK)>`, no corrective traffic.** |
| **SOFTWARE FOLLOW-UP: `SimpleEcosBackend` command dispatch still depends on prefix ordering.** | Independent software review 2026-09-20 (commit `5042f70`): `speedstep[...]` is now checked before `speed[...]`, which is functionally correct, but correctness still relies on broad-prefix ordering rather than a structurally distinct match. `HandleCreateAsync` has the same class of maintainability risk (`addr` is checked before `addrext`). | Dispatch on the exact property name up to `[` (for example `speed` vs `speedstep`, `addr` vs `addrext`) so a future reorder cannot reintroduce the defect. Not a blocker for the current live validation; do not change during evidence collection unless live testing proves a concrete correctness defect. |
| **SOFTWARE FOLLOW-UP: C# TrackAmplifier info page does not follow live data; updates should be event-based.** | Live validation 2026-09-20 (Koploper hand-controller test): the C# TrackAmplifier info page only updated on user input; the built-in polling did not make it follow the live Koploper/ECoS/amplifier data. The existing 10 Hz comm timer and 2 s update were built for the manual TrackControl info page. | Investigate event-based updates across the chain Koploper -> EcosEmu -> C# amplifier info page and the comm path to the master (mailbox in master). Decide with an experienced dev/arch whether the 10 Hz/packetized master communication can stay or must become event-based. Not fixed during the live test. |
| ~~**CONFIRMED PRODUCT DEFECT: a direction command issued while a locomotive has no known block is lost, so the locomotive starts in the stale/default direction.**~~ **Fixed in software and live-regression-validated 2026-09-20.** | Targeted live reproduction 2026-09-20: with loco 2 (id 1001) on block 0 (unmapped), Koploper's `set(1001,dir[1],speedstep[0])` and `set(1001,dir[0],speedstep[0])` were both refused with `<END 8 (SAFETY_INTERLOCK)>` and emitted no `dir[...]` event; after placing loco 2 in block 1, `set(1001,speedstep[1])` wrote reverse-band PWM **382 (0x017E)** although Koploper showed forward (forward would be 416). Root cause: `SimpleEcosBackend` assigned `loco.Direction` only on `IHardwareBackend.SetLocoSpeed` success. | Done: `SimpleEcosBackend` now owns the requested/logical direction and speed; the `dir` branch always updates `loco.Direction` and emits the event, physical application is best effort, and a later movement uses the most recent requested direction. Regression tests in `SimpleEcosBackendDirectionStateTests`. **Live regression 2026-09-20: `LIVE DIRECTION REGRESSION PASS` - unmapped `dir[...]` now returns `<END 0 (OK)>` + `dir` event with no write; after mapping, `speedstep[1]` wrote forward 416 (0x01A0) and reverse 382 (0x017E) for the requested direction. Retained non-zero speed while unmapped caused no movement on mapping. See `docs/handoff.md`.** |
| Simulator occupancy through the production abstraction. | Simulator occupancy arrives as ECoS sensor events, so `OccupancyAvailable` is false in simulator mode and route occupancy checks do not run there. | An `IOccupancyProvider` over the simulator exists and occupancy divergence is checked in simulator mode. |
| Route checks are only wired into the real-mode look-ahead path. | `TrackAmplifierHardwareBackend.Divergence` is set in real mode; the simulator's checker is only reachable by explicit calls. | A route check also runs automatically in simulator mode. |
| ~~Watchdog for stale amplifier occupancy.~~ **Resolved.** | The comm client keeps republishing its cached container and never clears `SlaveDetected`, so cached data used to stay "valid" forever. `TrackAmplifierDataFreshness` now derives freshness from the frame timestamp. | Done: stale amplifier data is treated as unknown, never as clear. |
| Investigate `TrackApplicationVariables` HoldingReg aliasing. | All 56 `trackAmpItems` share one `HoldingReg` array instance. | Confirmed intended or fixed. |
| `EcosEmulatorServer` binds loopback only. | `IPAddress.Loopback` in `EcosEmulatorServer.Start`; correct while Koploper runs on the same PC. | Confirmed same-PC, or made configurable. |
| ~~`dir[...]` is refused during a loco safety latch even at speed 0.~~ **Resolved 2026-09-20.** | `SimpleEcosBackend` now treats `dir[...]` as requested/logical state: it always updates `loco.Direction` and emits the event, while a physical application is still blocked by the interlock. | Done: direction changes are logical and are never refused as movement; non-zero movement remains gated by the safety interlock. |
| ~~A refused movement always answers `END 8 (SAFETY_INTERLOCK)`, even when the real reason is "no known block".~~ **Resolved 2026-09-20.** | `SimpleEcosBackend` now distinguishes a real safety refusal from "no physical target" via the optional `IMovementSafetyGate` (implemented by `ControlSafetyInterlockBackend`). A no-target command is logically accepted (`<END 0 (OK)>`); only a latched safety interlock answers `<END 8 (SAFETY_INTERLOCK)>`. | Done: refusal reasons are distinguished in the ECoS reply; a no-target command no longer falsely claims a safety interlock. |
| Unit tests for the window/program model. | `SiebwaldeApp.Core.Tests` covers Core/Integration; UI-model tests are still absent. | Window/program-model behaviour is covered. |
| Tracked `.csproj.user` files with dangling entries. | `SiebwaldeApp/SiebwaldeApp/SiebwaldeApp.csproj.user` references XAML/VM files deleted in Increment 3; `.gitignore` has no `*.user` rule. | User-local files are untracked and ignored. |

### Firmware dependency

| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| ~~Real amplifier occupancy bit.~~ **Not a dependency.** | `TrackAmplifier4.X/processio.c` already sets `HR_STATUS` bit 10 from `g_occ = CMP1_GetOutputStatus()`. The `General.h` TODO comment is stale. Real mode now consumes it. | Done: real mode reads the existing amplifier occupancy. |
| Stale firmware comment. | `TrackAmplifier4.X/modbus/General.h` line 95 still says "(TODO: implement when occupancy source known)" while `processio.c` implements it. | Comment corrected in firmware (not part of this increment). |

### Physical hardware dependency
| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| Real physical switch output (accessory decoder). | `TrackAmplifierHardwareBackend.SetSwitch` returns false; the real switch sink drives nothing. | An accessory-decoder output path actuates mapped switches. |
| Physical switch feedback. | `UnobservableSwitchObserver` always reports "not observable". | A real observer exists and the ECoS state reflects confirmed hardware state. |

### Configuration / user-input dependency

| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| Real-layout power-on switch positions. | `SwitchMapConfig` default is `keep` because the rest position is unknown. | Confirmed positions are configured as `g`/`r`. |
| Real-layout topology, block map and switch addresses. | The shipped defaults describe the test oval. | The real layout values are entered on the settings page. |
| Signals 51..55 as switches. | Koploper commands them via `switch[...]`; they are unmapped and ignored. | Signals are either mapped or deliberately documented as out of scope. |

## Recovery & Maintenance System (future)

The ECoS emulator graceful-shutdown work (merged 2026-09-23, PR #8, merge commit `5fb751552b368015ba17f796d86f41c1d7fa7c13`) is recorded as a **completed foundation** for a future Recovery & Maintenance System.

The Product Owner has since issued a full assignment for this system. See the durable feature brief, architecture proposal, development roadmap and open Product Owner decisions in **`docs/recovery-maintenance-system.md`**. The first implementation increment (software-only, simulator mode: stop/start/restart of the C# track-control runtime from WPF) is described there; do not start implementation until the Product Owner approves it as a separate increment.

### Recovery & Maintenance — Increment 1 follow-ups (not implemented, deferred)

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| ~~No simulator `ITrackTransport`.~~ **DONE (2026-09-24):** a deterministic `DeterministicTrackTransport : ITrackTransport` (Core `TrackApplication.Simulator`) now emulates the PIC32 master protocol (control frames + SLAVEINFO) so the full track part (comm → 9-step init → `TrackControlMain` write loop) runs end-to-end software-only, proven by `DeterministicTrackTransportEndToEndTests`. | The real-mode track part is composed but cannot be executed in simulator mode because no simulator `ITrackTransport` exists (the legacy `EthernetTargetDataSimulator` feeds the old `TrackIOHandle` path). | A simulator transport + deterministic init path, so the full track part can run end-to-end without hardware. |
| `KoploperExternalInfoClient` lacks a `Faulted` event. | `EcosEmulatorServer` and `TrackSimulatorBackend` surface background-task faults; the external-info client catches broadly and retries, so a fault there is not surfaced to the runtime `Failed` state. | Either surface external-info faults the same way, or document that retry-forever is the intended behaviour. |
| `OnEcosHostFaulted` ignores faults during `Starting`. | The runtime coordinator only maps `Running → Failed` on a host fault; a fault raised between host start and the `Running` transition is dropped and the start still reports `Running`. | A fault during `Starting` also transitions to `Failed` (or the window is provably impossible). |
| WPF runtime surface not runtime-tested. | The init-page Start/Stop/Restart surface and `App.OnExit` graceful stop are build-verified (V1) only; the WPF app was not launched. | Manual/runtime smoke test of the WPF lifecycle surface (start/stop/restart buttons, state display, failure display). |
| Stop/telemetry retention policy. | After a stop the runtime nulls `TrackApplicationVariables`, so the amplifier page clears (no "last known state" shown). Retaining last-known state as "stale" would be a product decision. | Product Owner decides whether to retain/display last-known amplifier state after stop. |

### Recovery & Maintenance — observed-neutral increment follow-ups (deferred)

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| V3 integration test of the movement gate through a real `TrackControlHost`. | The gate + ECoS `SAFETY_INTERLOCK` surfacing is proven at unit level (`ControlSafetyInterlockBackendTests`, `TrackAmplifierHardwareBackendTests`) and end-to-end via the manual `SetAmplifierControl` funnel; a full `SetLocoSpeed` → `EXEC_MBUS_SLAVE_DATA_EXCH` → `SimpleEcosBackend` `END 8 (SAFETY_INTERLOCK)` run through a real `TrackControlHost` + `DeterministicTrackTransport` is not yet executed. | A software-only integration test drives `SetLocoSpeed` through the real host and asserts the ECoS refusal + the 108 write. |
| Empty safety domain is fail-closed (movement blocked). | `TrackAmplifierGroups.AllConfigured` is empty with the production default config, so the observed-neutral gate now keeps movement blocked with a fault until the amplifier group/domain config is populated. This is the safe consequence of open Product Owner decision 1 (group/domain). | Product Owner configures the amplifier groups; optionally a dedicated "no safety domain" operator surface. |

### Recovery & Maintenance — V4 physical-neutral prerequisites (deferred; see `docs/recovery-maintenance-system.md` §9)

| Item | Evidence | Suggested acceptance criteria |
| --- | --- | --- |
| Fix the PIC18 firmware build (blocking for V4). | `TrackAmplifier4.X/main.c` calls undefined `CheckModbusTimeout`/`Ramp_Update`/`ControlCore_Update`; `runtime_command` undefined; `REGULATORxUPDATE` only in commented-out code; `modbushooks.c/.h` untracked (define `OnHoldingRegisterWrite` + `last_modbus_activity_tick`); `Update_AmplifierTicks` type mismatch. The `dist/` artifacts are from an older `main.c`. | Committed source builds cleanly and matches the flashed image; the HR0→PWM apply path is linked. FIRMWARE_FLASH-gated. |
| Expose applied PWM in readback. | SLAVEINFO `HoldingReg[0]` is the command echo (`PetitHoldingRegisters[0]`), never the applied duty; no `CURRENT_PWM`/`TARGET_PWM` register exists. | A read-only register carries the duty actually loaded into PWM3, so the protocol can distinguish command-vs-applied. FIRMWARE_FLASH-gated. |
| Expose brake/enable line state. | `LM_BRAKE` is asserted at boot and never cleared in the committed tree (scenario S5). | A status register reflects LM_BRAKE/LM_PWM/LM_DIR so C# can see whether the brake line is held. FIRMWARE_FLASH-gated. |
| V3 integration test (software-first prerequisite). | `SetLocoSpeed` → `EXEC_MBUS_SLAVE_DATA_EXCH(108)` → `SimpleEcosBackend` `END 8 (SAFETY_INTERLOCK)` through a real `TrackControlHost` is provable software-only with `DeterministicTrackTransport`. | A deterministic test drives the full host path and asserts the refusal + 108 write. |
| V4 physical-neutral measurement matrix. | Protocol-observed HR0==399 is a command echo; physical neutral (PWM ~50%, motor stopped) is unproven. | Execute `docs/recovery-maintenance-system.md` §9.6 T1–T7 under LIVE_HARDWARE against the fixed firmware. |


