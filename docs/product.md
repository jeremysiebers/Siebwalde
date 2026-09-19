# Product

This document records the confirmed project purpose, user-confirmed requirements, priorities, and open questions. It separates verified facts from assumptions, ideas, and unresolved questions.

`docs/product-clarification.md` is a recorded clarification-session prompt. It is not the product specification. The raw structured input from the product owner is captured in `human_input.md`.

## Confirmed Project Purpose

Status: confirmed by the product owner during the clarification session (2026-09-11).

Siebwalde is a model railway control project for the Siebwalde model train yard. The PC application is intended to control the layout, with Koploper as the driving-behavior authority and microcontroller firmware handling hardware-level control.

## Confirmed System Roles

Status: confirmed by the product owner. Code behavior still requires revalidation where noted.

| Component | Confirmed role | Status |
| --- | --- | --- |
| `SiebwaldeApp` + `SiebwaldeApp.Core` + `SiebwaldeApp.EcosEmu` | The active PC application; one solution containing desktop UI, core, and ECoS emulator. | Active |
| `TrackController5/` | Embedded controller that drives the 50 ModBus amplifiers, translating C# Ethernet commands into ModBus/bootloader commands. Must also perform MMDC tasks (communication loss, autonomous reactions). | Active |
| `TrackAmplifier4.X/` | Application running on all 50 amplifiers. Currently "dumb"; must later execute downloaded parameters/setpoints from Koploper, perform MMDC (over-current, over-temperature), and report occupancy. | Active, under development |
| `TrackAmplifierBootLoader.X/` | Fixed bootloader programmed into every amplifier. | Active |
| `TrackBackplane2.X/` | Fixed ModBus code for the 5 backplanes (reset, init, operational CRC check, FW download, switch to ModBus). Static; no train-behavior logic needed for now. | Active, static |
| `TrackControllerPic18.X` (in `Backup projects/`) | Obsolete embedded-C predecessor. Its remnants must be removed from the C# project. | Obsolete |
| `FiddleYard/` | Embedded C of the Ethernet controller that the C# app currently initializes and controls. To be left as-is for now. | Active, deferred |
| `YardController_IOX.X` (in `Backup projects/`) | Embedded C for (1) part of the Faller Car System and (2) Yard control via handheld transmitter. Partly split into C# for flexibility/user configuration later. Not yet an expected init-page host. | Active, future split |
| Other top-level `*.X/` projects | Standalone microcontroller projects for model-railway control. | Standalone |
| `KiCad/` | PCB designs; only relevant for IO mapping. | Reference |
| `Common_Files/ModBusSlaveMasterMappings` | Start of a generic interface definition for ModBus registers. | Reference |

## Confirmed Requirements

Status: confirmed by the product owner (2026-09-11).

### C# application

- Clean up the project: get the intended core and separation of concerns (IoC) in order.
- Inventory old remnants inside the C# project and mark them for removal (for example TrackController-related code from earlier implementations where coding station behavior in C# was still the goal). The Fiddle Yard is excluded.
- Resolve the missing `IoC.Kernel`, missing station-domain symbols, initialization step order, and configuration authority.
- Apply modern programming standards, unit testing, and simulations.
- Leave the current Fiddle Yard logic, behavior, and visualization unchanged for now; it must keep working. IoC improvements may later be applied there, but behavior must be validated.

### Program startup and initialization

- On start, the `SiebwaldeInitPage` is loaded.
- The page automatically searches for expected hosts, determines presence with modern techniques, and displays the result dynamically.
- The page shows human-readable logging of logical steps and states; detailed logging goes to the associated log files.
- When a host is available, its functionality can be started with a start button (operator-dependent for now, because the project is in development).
- Expected hosts: FiddleYard, Ethernet ModBus master (TrackController5), Koploper, and later YardController.
- Detection for now: ping on host name for FiddleYard, the ModBus master, and YardController; a TCP connect probe for Koploper. Later, after C-code changes, one uniform detection layer will be built.

### Configuration and settings

- All hard-coded values must become editable and persisted through a new menu -> settings option.
- The settings must correspond with the current test/program files.
- Use `app.config` if that is an appropriate/usual method.
- These settings belong in the core, not in the visualization.
- A default button per entity is wanted, with the ability to undo (for example Ctrl-Z).

### Engineering standards and simulation

- Add unit tests for the existing window/program model and for new extensions. Fiddle Yard logic is deferred because it will be rewritten, including its visualization.
- Standard coding standards apply; existing code may be adjusted, with unit tests demonstrating the current existing functionality.
- When a host is not detected, it must be possible to start it in simulation:
  - FiddleYard already has a complete data-generator simulation; adapt later.
  - A Koploper simulator should keep Koploper satisfied and/or simulate that Koploper is present, with manual user control (how many/where trains are, commands to trains). This builds on the existing C# ECoS emulator, which will interface Koploper with the C# program.

### Koploper-driven train control (target capability)

- Koploper owns the driving behavior via the ECoS emulator.
- C# receives Koploper's direct per-encoder commands and translates them per locomotive into setpoints for the amplifier(s) driving that locomotive.
- Setpoint control must move along with the locomotive.
- Track amplifier occupancy goes via C# to Koploper; Koploper returns locomotive-location display information via a dedicated port; C# uses that to select the target amplifiers.
- When a block is free (no occupancy), C# must already command the next block's amplifier so the locomotive can cross smoothly (look-ahead).
- The block topology (for example block 1 = amplifier 1 -> block 2 = amplifier 2) is a user-definable configuration input in `app.config`.
- The exact Koploper protocol and port roles are derived from `Ecos ESU info`, the C# ECoS emulator code, and working test examples; they are not supplied by the product owner.
- Koploper initiates the connection via its own "make connection" button to the configured address.
- Switch-street commands from Koploper must be translated into Fiddle Yard TOP/BOTTOM shift commands.
- Signals currently implemented in hardware must move to software, because occupancy can no longer be hardwired to the Fiddle Yard controller once real trains run.
- Initialization must be clearer: verify present Ethernet targets and online checks, initialize amplifiers and check/download firmware, then establish the Koploper connection and operate transparently to Koploper.

### Firmware (later)

- TrackAmplifier4.X must execute downloaded parameters/setpoints and perform MMDC plus occupancy feedback.
- TrackController5 must perform MMDC and translate commands.
- TrackBackplane2.X stays static.
- YardController functionality will be split between embedded C and C# for flexibility.

### MMDC And Safety (responsibility split)

- **TrackAmplifier4.X:** hardware protection against over-current and over-temperature, plus MMDC values downloaded from C# (maximum current cutoff, maximum temperature cutoff, hiccup-mode auto restart, PWM setpoint monitoring). On error it must enter hiccup (bring temperature under control) and report to the master, then to C#.
- **TrackController5:** monitors communication with the PC and, on loss, performs an emergency stop via broadcast to all track amps. It also monitors communication with amplifiers; if an amplifier drops out, operation may continue but C# (and the visualization) must be informed and an occupancy report must be given to C#/Koploper.
- **C# layer:** monitors communication with the ModBus master and Koploper; communicates emergency stop upward/downward when one of them stops communicating; handles alarm and operator logging; provides a recovery strategy, possibly with manual override to set setpoints, switch streets, and recover trains.
- Koploper's emergency button is a software emergency-stop command that can be forwarded with the same broadcast to all track amps.
- How an amplifier error is communicated to Koploper must be determined; the product owner believes the ECoS ESU can send a stop command to Koploper on overload, to be verified against datasheets/internet.

### YardController Split And Handover

- The Yard is hand-operated and stays hand-operated.
- Handover main line -> Yard: the operator requests a locomotive; C# reads the request and passes it to Koploper (via a separate occupancy detector or action). Koploper shunts a freight train from the main line onto a dead-end track, the switch street is switched back, and the main line continues. The operator then switches control of that dead-end block to the hand controller via RC.
- Handover Yard -> main line: a locomotive is parked on a block and handed over via a switch command to the amplifier known by C#/Koploper. The operator indicates the locomotive in Koploper and starts it in software; Koploper plans and merges it into main-line traffic.
- Faller Car System: either driven from Koploper (experiment, to test configuration benefits) or a dedicated C# solution. Its IO is boolean only (stop/depart, switch-street selection), with limited hall-sensor detection.

### Shuttle Line (Pendelbaan)

- Besides the main line there is a shuttle line where a maximum of 4 and a minimum of 1 locomotive shuttles on a separate piece of rail between 3 stations: 2 end stations with a switch and 1 middle station with a fixed driving direction.
- There is no fixed coupling between the shuttle line and other layout elements.
- Two amplifiers are assigned to the shuttle line; in the middle station the switch street is switched between one and the other amplifier using switches and relays. These are ModBus amplifiers.
- Block boundaries and chaining exist on paper only and are not yet known in software.
- How the shuttle line is controlled (Koploper or C#) and how it relates to the main line and Yard handover is still open.

### UI And Visualization (Designer Scope)

- A designer agent is needed for UI (WPF/WinForms): visual design of the C# app, control and overview panels, and the Fiddle Yard visualization (currently WinForms, later WPF).
- Possibly a visualization of the whole layout to compare with Koploper data, serving as diagnostics and as a manual override/test tool to operate elements (switch streets) and a locomotive by hand, possibly with input from an external controller.
- An integrator/test agent is also considered necessary.
- Both agents were created on 2026-09-11 (`.opencode/agents/designer.md`, `.opencode/agents/integrator.md`); an OpenCode restart is required to load them.

### Test Layout

- The test layout uses 4 amplifiers. Their addresses/IDs may be recorded in a spreadsheet; candidate files are under `Backup projects/TrackControllerPic18.X/Doc/` (not yet parsed).
- There is no fixed coupling between specific locomotives and amplifiers; block topology is on paper only.

## Domain Context (Product Owner, Not Yet Code-Verified)

Koploper speed control distinguishes three things:

| Concept | Meaning | Stored in |
| --- | --- | --- |
| Decoder settings | Motor characteristic, speed curve, acceleration/deceleration | Locomotive decoder |
| Speed calibration | Which real scale speed belongs to which speed step | Koploper |
| Software acceleration/braking | How the requested speed changes over time | Koploper |

- Koploper supports mass simulation, train types, and "learning to brake".
- Calibration measures time over a known distance at several speed steps; it does not program a new decoder curve. Decoder changes can make calibration stale.
- CV4 affects both intermediate speed reductions and final stops, so a zero command does not necessarily stop immediately. ESU describes CV3/CV4 as time settings with speed-dependent distance (ESU LokPilot manual, section 10.1).
- For software braking a small decoder deceleration is wanted; there is no universal correct CV4 value.
- Koploper normally uses the calibration table and does not continuously measure actual speed; feedback sensors report position passages. Decoder load compensation is a separate control loop.
- The exact internal braking calculation could not be confirmed from the consulted documentation and remains uncertain.

## Priorities

Status: confirmed by the product owner (2026-09-11).

1. **First increment: C# cleanup, startup, and correctness.** Revalidate prior findings, restore the intended core/IoC separation, inventory and mark obsolete remnants for removal, and establish tests/simulation. Includes the `SiebwaldeInitPage` startup/initialization story (host detection, dynamic display, human-readable page logging), the menu -> settings option for hard-coded values, and a simulation fallback for undetected hosts. Fiddle Yard behavior stays unchanged. No firmware changes yet.
2. **Application guide.** Create `docs/application-guide.md` as the human-readable guide.
3. **Koploper translation path.** On the 4-amplifier test layout, implement the full chain from Koploper command to amplifier setpoints and occupancy feedback, including clearer initialization and Koploper transparency.
4. **Firmware development.** TrackAmplifier4.X parameters/setpoints/MMDC and TrackController5 MMDC, in bounded passes.
5. **Fiddle Yard, YardController, and shuttle line.** Defer Fiddle Yard; plan the YardController split and main-line/Yard handover later; define shuttle-line control later.

## Approved Increment 1 (2026-09-11)

Status: implemented and verified on 2026-09-11. See `docs/build-test.md` for build results.

Scope:

- Fix `SiebwaldeApp.Core.Host/Program.cs` logger setup to use the active Core IoC API.
- Fix the initialization step sequencing so `SetDefaultPwmSetpointsStep` runs and all step names resolve.

Exclusions:

- No Fiddle Yard changes.
- No test-project repair or archival decision.
- No obsolete-remnant removal.
- No configuration-authority refactor.

Acceptance criteria:

- `SiebwaldeApp.Core` and `SiebwaldeApp.Core.Host` build with 0 errors.
- Initialization order is `InitTrackamplifiers` -> `SetDefaultPwmSetpoints` -> `EnableTrackamplifiers`, and each returned next-step name matches a registered step name.
- No Fiddle Yard source file is modified.

Verification approach:

- `dotnet build` for both projects (Debug, no hardware, no test execution).
- Source inspection of the step names against the registered dictionary.

## Ideas And Proposals (Unapproved)

Status: not confirmed. Tracked for future discussion, not committed scope.

- Control the Faller Car System via Koploper by faking suitable data.
- Add a "designer" agent alongside architect and developer.
- Use `Common_Files/ModBusSlaveMasterMappings` as the basis for a generic ModBus register interface.

## Open Questions

- How should an amplifier error (over-temperature/over-current) be communicated to Koploper, and can ECoS overload/stop semantics be reused (to be researched)?
- How should the shuttle line be controlled, and how does it relate to the main line and Yard handover?
- How do we transfer the paper block topology and amplifier IDs into `app.config`, and are the amplifier IDs in the candidate spreadsheets?
- Which exact values must be editable in menu -> settings, and how far should the per-entity default/undo (Ctrl-Z) behavior extend?
- Which of the two Koploper ports is the connection port and which is the block/location info port (to be verified from `Ecos ESU info` and code)?

## Assumptions

- The application is operated on Windows.
- The runtime deployment path is `C:\Localdata\Siebwalde`, as confirmed by hard-coded paths in source and by runtime logs under `Logging/`.
- The prior .NET code-analysis findings are not yet revalidated against current source and are treated as requiring revalidation, not as confirmed true or false.
