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
| Establish configuration authority for endpoints and firmware path. | DONE (2026-09-11): CONFIRMED. `SiebwaldeApplicationModel.cs:23,140-142` and `Core.Host/Program.cs:9,59-61` hard-code values; `CoreSettings` has ports but no IP/firmware path and is not used for the track transport. | One authoritative configuration source is documented and used (ties into the settings UI item). |
| Remove obsolete `TrackControllerPic18.X` remnants from the C# project. | CONFIRMED (2026-09-11): commented `TrackPic18UdpAdapter`/`YardPic18UdpAdapter` code in `App.xaml.cs`; station-era UI remnants (`StationSettingsPage`, `StationSettingsPageViewModel`, `ApplicationPage.StationSettings`, `SideMenuViewModel.StationSettingsPage`); stale doc comment on `StartTrackApplication`. | Remnants are removed; active `TrackControllerCommands` (ModBus) is kept; build/tests remain green. |
| Define modern standards, unit testing, and simulation baseline. | Product owner requested modern programming standards, unit testing, and simulations. | Chosen frameworks/analyzers and a simulation approach are documented and applied. |
| Restore intended core separation and IoC structure. | Product owner requested the intended core and separation (IoC) be put in order. | Core/UI boundaries and IoC composition are documented and consistent. |
| Implement `SiebwaldeInitPage` host detection and dynamic display. | Product owner requires automatic detection of FiddleYard, ModBus master, Koploper, and later YardController, with human-readable page logging. | Expected hosts are probed (ping on host name; TCP connect for Koploper) and shown with present/absent state; page shows logical steps/states; start buttons enable detected hosts. |
| Add menu -> settings option for hard-coded values. | Product owner requires all hard-coded values to be editable and persisted, matching current test/program files; settings belong in core; default per entity plus undo (Ctrl-Z). | Settings service lives in core using `app.config`; UI edits and persists values; per-entity default button and undo work; no hard-coded endpoint/path remains in active startup code. |
| Build one uniform host detection layer (later). | Product owner wants a single detection layer after C-code changes. | Firmware exposes a stable detect/connect mechanism and the C# layer uses one uniform implementation. |
| Add simulation fallback for undetected hosts. | Product owner requires a simulation option per host. | Undetected hosts can be started in simulation; FiddleYard uses its existing generator; a Koploper simulator builds on the ECoS emulator. |
| Add unit tests for the window/program model. | Product owner requested unit tests for the existing model and extensions. | Existing window/program-model behavior is covered by tests before/while refactoring. |

## Koploper Translation Path (Later Increment)

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
| Reconcile `SiebwaldeApp.Tests` with active source. | CONFIRMED 2026-09-11: tests reference missing station-domain symbols and `IoC.Kernel`/Ninject. | Test project builds or is explicitly archived with rationale. |
| Add the test project to the appropriate solution if it is active. | `SiebwaldeApp/SiebwaldeApp.Tests/SiebwaldeApp.Tests.csproj` exists but is not in `SiebwaldeApp.sln`. | Chosen solution includes active test project, or documentation explains why it is separate. |

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
| Create the consolidated human-readable application guide. | `docs/application-guide.md` does not exist; `docs/product.md` now captures purpose and scope only. | A coherent guide covers architecture, workflows, and verified usage with diagrams, and distinguishes verified facts from revalidation items. |
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
