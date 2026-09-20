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
| Add graceful stop for Koploper external info and simulator loops. | Background loops exist in `KoploperExternalInfoClient` and `TrackSimulatorBackend`. | Host shutdown cancels and awaits background tasks. |

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

### Software follow-up

| Item | Evidence | Acceptance criteria |
| --- | --- | --- |
| ~~**CONFIRMED PRODUCT DEFECT: `AmplifierSpeedMapper` assumes 127 speed steps, so DCC28 speed is under-scaled.**~~ **Fixed in software 2026-09-20.** | Live validation 2026-09-19: the locomotive protocol is `DCC28` and Koploper/ECoS supplied steps `0..28`, but `ToPwm` scaled by 127. Live DCC28 step 24 produced only **~PWM 475** instead of approaching 799, so the motor never reached the top of the usable 400..799 range. The root cause was that `SimpleEcosBackend`'s `opt.StartsWith("speed")` branch also matched `speedstep[...]` and passed the raw protocol step downstream. | Done: `ProtocolSpeedNormalizer` normalizes `speedstep[...]` (DCC28 `0..28` -> `0..127`, round-half-up) at the ECoS boundary, `speed[...]` stays normalized, unknown protocols are refused explicitly, and the hardware layer stays protocol-independent. Regression tests added (`ProtocolSpeedNormalizerTests`, `SimpleEcosBackendSpeedNormalizationTests`). **Live DCC28 full-range verification still pending (Integrator).** |
| **SOFTWARE FOLLOW-UP: `SimpleEcosBackend` command dispatch still depends on prefix ordering.** | Independent software review 2026-09-20 (commit `5042f70`): `speedstep[...]` is now checked before `speed[...]`, which is functionally correct, but correctness still relies on broad-prefix ordering rather than a structurally distinct match. `HandleCreateAsync` has the same class of maintainability risk (`addr` is checked before `addrext`). | Dispatch on the exact property name up to `[` (for example `speed` vs `speedstep`, `addr` vs `addrext`) so a future reorder cannot reintroduce the defect. Not a blocker for the current live validation; do not change during evidence collection unless live testing proves a concrete correctness defect. |
| **SOFTWARE FOLLOW-UP: C# TrackAmplifier info page does not follow live data; updates should be event-based.** | Live validation 2026-09-20 (Koploper hand-controller test): the C# TrackAmplifier info page only updated on user input; the built-in polling did not make it follow the live Koploper/ECoS/amplifier data. The existing 10 Hz comm timer and 2 s update were built for the manual TrackControl info page. | Investigate event-based updates across the chain Koploper -> EcosEmu -> C# amplifier info page and the comm path to the master (mailbox in master). Decide with an experienced dev/arch whether the 10 Hz/packetized master communication can stay or must become event-based. Not fixed during the live test. |
| **SOFTWARE FOLLOW-UP: a direction command issued while a locomotive has no known block is lost, so the locomotive can start in the wrong direction.** | Live validation 2026-09-20: loc2's `set(1001,dir[0])` at 12:37:30 was refused with `<END 8 (SAFETY_INTERLOCK)>` because the locomotive had no known block; `Direction` stayed at its default `1` (reverse). When loc2 was later placed in block 3 and driven, it ran in reverse while Koploper showed forward, and toggling direction corrected it (operator observation, to be confirmed). | Decide whether direction should be applied or re-synchronised once the locomotive has a known block, and whether a direction-only command should be refused while unplaced. Not fixed during the live test. |
| Simulator occupancy through the production abstraction. | Simulator occupancy arrives as ECoS sensor events, so `OccupancyAvailable` is false in simulator mode and route occupancy checks do not run there. | An `IOccupancyProvider` over the simulator exists and occupancy divergence is checked in simulator mode. |
| Route checks are only wired into the real-mode look-ahead path. | `TrackAmplifierHardwareBackend.Divergence` is set in real mode; the simulator's checker is only reachable by explicit calls. | A route check also runs automatically in simulator mode. |
| ~~Watchdog for stale amplifier occupancy.~~ **Resolved.** | The comm client keeps republishing its cached container and never clears `SlaveDetected`, so cached data used to stay "valid" forever. `TrackAmplifierDataFreshness` now derives freshness from the frame timestamp. | Done: stale amplifier data is treated as unknown, never as clear. |
| Investigate `TrackApplicationVariables` HoldingReg aliasing. | All 56 `trackAmpItems` share one `HoldingReg` array instance. | Confirmed intended or fixed. |
| `EcosEmulatorServer` binds loopback only. | `IPAddress.Loopback` in `EcosEmulatorServer.Start`; correct while Koploper runs on the same PC. | Confirmed same-PC, or made configurable. |
| `dir[...]` is refused during a loco safety latch even at speed 0. | `SimpleEcosBackend` routes `dir` through `SetLocoSpeed`, so the interlock treats it as movement. Conservative and safe. | Confirmed acceptable, or refined to allow a direction change at speed 0. |
| A refused movement always answers `END 8 (SAFETY_INTERLOCK)`, even when the real reason is "no known block". | `SimpleEcosBackend` uses one refusal code; the structured diagnostic carries the true reason. | Refusal reasons are distinguishable in the ECoS reply. |
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


