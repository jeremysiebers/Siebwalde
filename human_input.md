# Human Input - Product Clarification Session

This file records the product owner's answers during the product clarification phase, cleaned up and structured. It also lists open follow-up questions from the Project Lead. It is input material; the durable specification lives in `docs/product.md`.

Session date: 2026-09-11
Source: raw input provided by the product owner, transcribed and clarified.

---

## Question 1 - Applications And Firmware Actually Used

### PC application

- The PC application consists of three modules that belong to one solution: `SiebwaldeApp`, `SiebwaldeApp.Core`, and `SiebwaldeApp.EcosEmu`.
- They are grouped in the `SiebwaldeApp` project/solution file.

### TrackController5 (active)

- Embedded microcontroller application responsible for driving the 50 ModBus amplifiers.
- The C# application commands it over Ethernet; TrackController5 translates those commands into ModBus commands or bootloader commands.
- TrackController5 must also perform MMDC ("Material Machine Damage Control") tasks, such as handling loss of communication and possibly reacting autonomously to certain situations.

### TrackAmplifier4.X + TrackAmplifierBootLoader.X (active, 50 units)

- All 50 amplifiers run the same application: `TrackAmplifier4.X`, with a fixed bootloader `TrackAmplifierBootLoader.X` programmed into the amplifiers.
- Current state: the amplifiers are still "dumb". It was already possible to show active amplifier instances and to adjust the PWM signal live with a slider.
- Intended state: the amplifiers execute downloaded parameters and setpoints (translated from Koploper), perform MMDC tasks (over-current, over-temperature, etc.), and report whether a train is present on their track section.

### TrackBackplane2.X (active, static)

- Fixed ModBus code for the 5 backplanes.
- C# and the ModBus master can reset, initialize, perform operational CRC checks, download firmware, and then switch over to normal ModBus communication.
- These backplanes are static and, for now, need no code that depends on functional train behavior.

### TrackControllerPic18.X (obsolete predecessor)

- An earlier embedded-C predecessor, originally intended to be converted to C# to code the first station driving behavior in C#. Remnants are still present in the project.
- After an AI-assisted test period, an ECoS/ESU emulator was built that fools Koploper and allows the complete driving behavior to be configured in Koploper.
- Via the emulator, C# receives Koploper's direct commands per "encoder". C# must translate these per locomotive (Koploper knows which locomotive is where) into setpoints for the amplifier(s) that drive that locomotive. Setpoint control must move along with the locomotive.
- Occupancy reported by the track amplifiers goes via C# to Koploper. Koploper replies via a dedicated port with display-type information about which locomotive is located where. With this information C# determines which amplifiers receive the commands.
- Conclusion: `TrackControllerPic18.X` is obsolete, and its remnants must be removed from the C# project.

### YardController_IOX.X (active embedded C, future split)

Two tasks today:

1. Controlling part of the Faller Car System: behavior of currently 3 battery-powered vehicles, using servos under the road surface to set switches or stop vehicles.
2. Translating and controlling the Yard itself using a handheld transmitter. Actions at physical locations can be switched on/off, for example: enabling a shunting track (coupled to the PWM controller driven by the RC remote), setting a switch street correctly, selecting other switch streets, controlling a turntable, and controlling a crane that moves up and down.

Intention: part of this remains in the embedded C application (certain behavior, actuation, or dynamic configuration), while other parts move to the C# program for more flexibility and user configuration. Possibly the Faller Car System could also be controlled by Koploper if the right data can be faked.

### Other areas

- All other directories are standalone microcontroller projects for model-railway control possibilities.
- `KiCad` contains PCB designs; not interesting except possibly for IO mapping.
- `Common_Files/ModBusSlaveMasterMappings` contains a small start of a generic interface definition for ModBus registers.

---

## Question 2 - First Concrete Capability To Develop

- The current C# application can initialize and control the Fiddle Yard. This should not be converted or optimized for now; the embedded C of the Ethernet controller lives in `Siebwalde\FiddleYard` and comes later.
- The first problem we will eventually run into: once Koploper takes control (releasing blocks and commanding regulators), switch-street commands must be translated into Fiddle Yard TOP/BOTTOM shift commands. Signals currently implemented in hardware must move to software, because when real trains run on real hardware, occupancy can no longer be hardwired to the Fiddle Yard controller that currently performs MMDC.
- The product owner wants to clean up the project: get the intended core and separation (IoC) in order, and remove old remnants (except the Fiddle Yard for now).
- Then, on the test layout with 4 amplifiers, implement the full command-translation chain: Koploper command -> C# -> track amplifiers driving the train; occupancy feedback -> C# -> Koploper -> next control behavior.
- A clearer initialization behavior is required: verification of present Ethernet targets and online checks, amplifier init and firmware code checks/downloads, then establishing the Koploper connection, and then being "transparent" to Koploper while controlling the layout.
- The track amplifier embedded code is where the product owner currently is, but the C# side can be brought in order first (including modern programming standards, unit testing, and simulations).

---

## Question 3 - Working Method

- Architecture must be developed through interaction between AI and human, with questions and iteration, and improved continuously.
- Old parts must be removed, and the missing `IoC.Kernel`, missing station-domain symbols, initialization step order, and configuration authority must be resolved.
- This story must reach the required Markdown files via the architect, designer, project lead, and any other agents we may add.

---

## Round 1 Answers - Scope, Configuration, Engineering Standards (2026-09-11)

### Increment scope (refined)

- Goal: clean up and correct the C# application; restore the intended core/IoC separation; inventory old remnants inside the C# project and mark them for removal. These remnants relate to earlier implementations, for example where coding station behavior in C# was still the goal.
- Revalidate the earlier findings against the current source.
- Layout driving behavior will be determined by Koploper. The C# layer interprets Koploper commands and data and determines which amplifiers to control.
- In addition, there is a whole init/program-startup story that the product owner wants to improve and clean up together.
- The Fiddle Yard part (logic, behavior, visualization) comes later. It must remain functional as it is now. When IoC improvements are applied, they may also be applied there, but the behavior must be validated to keep working.

### Program startup and initialization (in scope)

- On start of the Siebwalde app, the `SiebwaldeInitPage` is loaded.
- This page must automatically search for expected hosts and determine, using modern techniques, whether a host is present or not, and display this dynamically.
- The page must also show logging (human-readable logical steps and states); all other detailed logging goes to the associated log files.
- When a host is available, the relevant functionality can be started with a start button. This remains operator-dependent for now because we are still in the development phase.
- Expected hosts: FiddleYard, Ethernet ModBus master, Koploper, and later YardController.

### Configuration (answer to question 2)

- A to-be-built option via the menu (menu -> settings) must allow all hard-coded values to be edited and saved.
- The settings must correspond with the current test/program files.

### Engineering standards (answer to question 3)

- Add unit tests for the existing "window/program model" and for the extensions we are going to make.
- Fiddle Yard logic comes later because it must be completely rewritten, including the visualization.
- When a host is not detected, there must be an option to start it in simulation:
  - FiddleYard already has its own complete data-generator simulation; it will be adapted later.
  - For the Koploper interface part in C#, a simulator is useful: on one hand to keep Koploper satisfied, on the other hand to simulate that Koploper is present. The user could then manually set things such as how many trains there are and where, and give commands to trains. This builds on everything already programmed and tested in the current C# ECoS emulator, which will be used to interface Koploper with the C# program.
- Code must follow normal standard coding standards. Existing code may be adjusted, with unit tests demonstrating the current existing functionality.

## Round 2 Answers - Host Detection And Settings (2026-09-11)

### Host detection (answer to question 4)

- For now: ping on host name, and a TCP connect to Koploper.
- YardController: also a ping.
- Later, when we are further along, the C code can be adapted to build a proper detect-and-connect. Later we will make one uniform detection layer.

### Settings (answer to question 5)

- If `app.config` is a usual/appropriate method, use it.
- The product owner assumes this kind of setting belongs in the core, since it has nothing to do with the visualization.
- A default button per entity is useful, and it must be possible to undo (for example with Ctrl-Z).

## Round 3 Answers - Koploper Interface And Locomotive Mapping (2026-09-11)

### Koploper interface (answer to question 6)

- The protocol can be derived from the code and data files, not from the product owner.
- The ECoS emulator emulates the ECoS ESU that Koploper drives. Data files are available in `C:\Localdata\Siebwalde\Ecos ESU info` and in the C# code; this includes the protocol and test examples that actually worked between Koploper and the ECoS ESU emulator.
- Koploper has its own "make connection" button and connects to the configured address. That address is probably on one of the two local ports.
- The other port, possibly `15471`, is where Koploper offers an interface reporting on which block a train is located. This must be translated by C# into which amplifier to command.
- Additionally, when a block is free (no occupancy), C# must already command the next block's amplifier so the locomotive can cross smoothly.

### Locomotive-to-amplifier mapping (answer to question 7)

- The best source is Koploper via that separate port (for example "train x is on block 1").
- The open point is how to determine how many amplifier blocks exist and how they are chained (block 1 (amp 1) -> block 2 (amp 2)). This could be derived by reading with MCP and screenshots, or it can be a user-definable input field in the configuration (`app.config`).
- Koploper couples the locomotive to the track via the occupancy reports from the amplifiers. Koploper then follows the locomotive's route and guards that it follows the expected path. There are various error states and actions Koploper can take.

## Round 4 Answers - MMDC, Safety, And YardController Split (2026-09-11)

### Koploper speed control and calibration (domain context)

- Koploper controls the speed profile and stores calibration data on the PC.
- Three distinct things must not be confused:
  - Decoder settings (motor characteristic, speed curve, acceleration/deceleration) - stored in the locomotive decoder.
  - Speed calibration (which real scale speed belongs to which speed step) - stored in Koploper.
  - Software acceleration/braking (how the requested speed changes over time) - in Koploper.
- Koploper supports mass simulation, train types, and "learning to brake".
- Calibration: Koploper lets the locomotive cover a known distance at several speed steps and measures the time, producing a table (for example step 8 = 25 km/u, step 16 = 60 km/u). This does not program a new speed curve into the decoder; the decoder is configured first, then Koploper learns the result. If the decoder characteristic changes, the calibration can become stale.
- Station stop (usual brake-detector + stop-detector approach): the train reaches the brake detector; Koploper gradually lowers the requested speed by sending lower speed steps; the train approaches the stop detector at low speed; at the stop point speed becomes zero, possibly after a configured run-on. "Learning to brake" adapts braking to the block. The exact internal calculation method could not be confirmed from the consulted documentation.
- CV4 sets deceleration for common DCC decoders and affects both intermediate reductions (step 20 -> 10) and final stops (step 3 -> 0). A normal zero command therefore does not necessarily stop immediately. ESU describes CV3/CV4 as time settings where the distance covered is speed-dependent (ESU LokPilot manual, section 10.1).
- For software braking, a small decoder deceleration is wanted: enough to smooth transitions, but not so much that the locomotive lags behind Koploper's commands. There is no universal correct CV4 value; it depends on the decoder type.
- Koploper normally knows the expected speed from the calibration table; it does not continuously measure actual speed. Feedback sensors report position passages. The decoder can regulate motor speed internally via load compensation, but that is a different control loop from the PC-side speed planning.

### TrackAmplifier4.X (answer to question 8)

- Gets parameters downloaded from C#. These parameters are determined by the user, or by future calculations/calibration software. They include MMDC values: maximum current cutoff, maximum temperature cutoff, hiccup mode auto restart, and PWM setpoint monitoring.
- Setpoints (driving speed) come in from Koploper via C#.
- On error (overtemperature, overcurrent) the amplifier must go into hiccup (bring temperature under control) and report this to the master, then to C#, and we must find out how to communicate this to Koploper.

### TrackController5

- Must monitor communication with the PC and, on loss, stop everything; in this case with an emergency stop broadcast to all track amps.
- When communication with an amplifier is lost, operation could continue, but C# (and the visualization) must be told what is wrong, and an occupancy report must be given to C#/Koploper so it can respond.
- Koploper's emergency button is a software emergency-stop command that can also be forwarded with the same broadcast to all track amps.

### C# layer

- MMDC logic in the application layer: monitor communication with the ModBus master and Koploper; communicate emergency stop upward and/or downward when one of the two no longer communicates; alarm and logging to the operator.
- A recovery strategy is needed, possibly with manual override from C# to set setpoints manually to recover trains, switch streets, etc.

### Safety responsibility

- Hardware protection against overtemperature/current in the track amp, with communication to the C# layer for logging and alarm, and a stop/occupancy report to Koploper. The product owner thinks the ECoS ESU can also send a stop command to Koploper on overload; this must be verified against datasheets or the internet.
- The C# layer is responsible for communication checks, commanding when communication fails, logging, alarm, and recovery handling.

### YardController split (answer to question 9)

- The YardController has two tasks.
- Faller Car System: either coded in C# (later) or driven from Koploper. Its IO consists only of boolean variables (stop/depart cars and switch-street selection). There are hall sensors that can detect the cars and distinguish them to a limited extent.
- Hand-operated side of the Yard stays hand-operated. However, a command can be set where the operator indicates to the main line that he wants to receive a locomotive. C# can read this request and pass it to Koploper via a separate occupancy detector or action. Koploper then ensures a freight train is shunted from the main line, placed on a dead-end track, the switch street is switched back, and the main line continues. The yard operator can then, via RC remote, switch control of that dead-end block to the hand controller and drive/shunt the locomotive.
- The reverse must also be possible: a locomotive is parked on a block by the operator and handed over via a switch command to the amplifier known by C#/Koploper. It must be made clear to Koploper that a train is present; the operator probably has to indicate manually in Koploper which locomotive it is, and then start it in software. Koploper then plans it again and merges it into main-line traffic.
- Faller Car via Koploper: an experiment to see whether it offers benefits for configuring behavior easily in Koploper. If not, a dedicated C# solution is needed.
- Besides the main line there is a shuttle line (pendelbaan): maximum 4 locomotives and minimum 1 locomotive can shuttle up and down on a separate piece of rail between 3 stations (2 end stations with a switch and 1 middle station with a fixed driving direction).
- The YardController does not need to be an expected host on the init page yet; that comes later.

## Round 5 Answers - Test Layout, Shuttle Line, And Agents (2026-09-11)

### Test layout and shuttle line (answer to question 10)

- The 4 amplifier addresses/IDs should be looked up in one of the "excel" files. The product owner thinks they may not be there yet, because it currently runs on old hardware that is not digital.
- Candidate spreadsheet files found in the repository: `Backup projects/TrackControllerPic18.X/Doc/` (`Poorten PCB-420-421-422-423-424.ods`, `Connectorprinten-2018.ods`, `Hoodkabels A-B-C-2018-rev 4/5.ods`, `Kabel kleur codering.ods`) and `YardController_IOX.X/Doc/Car System-2 .ods`. These have not been opened or parsed yet.
- There is no fixed coupling between the shuttle line and other layout elements.
- Two amplifiers are assigned to the shuttle line. In the middle station, the switch street is switched between one and the other amplifier using switches and relays. They are ModBus amplifiers.
- Block boundaries and chaining are not yet known in software, but they exist on paper.

### Designer and integrator/test agents (answer to question 11)

- A designer agent is needed for the UI (WPF/WinForms): visual design of the C# app, control and overview panels, and the Fiddle Yard visualization (currently WinForms, later WPF).
- Possibly also a visualization of the whole layout to compare with Koploper data. This would also serve as diagnostics and as a manual override/test article to operate elements (switch streets) and a locomotive by hand, possibly with input from an external controller.
- An integrator/test agent is also considered necessary.
- Note: per `docs/product-clarification.md`, agent definitions must not be changed during the clarification phase. Creation is deferred until the phase is closed.

## Follow-Up Questions From The Project Lead

These are open and should be answered in a later clarification round. They do not block starting the C# cleanup.

1. **Koploper interface details** - ANSWERED (Round 3): derive the exact protocol/ports from `Ecos ESU info`, the C# ECoS emulator code, and working test examples; do not rely on the product owner. Connection is initiated by Koploper's own "make connection" button to the configured address.
2. **Locomotive-to-amplifier mapping** - ANSWERED (Round 3): derive block occupancy/location from Koploper via the separate port. Block topology (block -> amplifier chaining) is a user-definable configuration input. Koploper couples locomotive to track via amplifier occupancy and guards the expected path.
3. **MMDC split** - ANSWERED (Round 4): TrackAmplifier4.X does HW protection (current/temp cutoff, hiccup, PWM setpoint monitoring) and reports; TrackController5 monitors PC comm (emergency-stop broadcast) and amplifier comm; C# does comm monitoring, alarm/logging, and recovery.
4. **Test layout** - ANSWERED (Round 5): amplifier IDs may be in a spreadsheet (candidates under `Backup projects/TrackControllerPic18.X/Doc/`); no fixed loco coupling; block boundaries/chaining are on paper only, not yet in software.
5. **YardController split** - ANSWERED (Round 4): hand-operated Yard stays hand-operated with C#/Koploper handover commands; Faller Car via Koploper is an experiment, otherwise a dedicated C# solution; YardController is not yet an expected init host.
6. **Configuration authority** - ANSWERED (Round 1): all hard-coded values become editable and persisted via a new menu -> settings option, aligned with the current test/program files.
7. **Engineering standards** - ANSWERED (Round 1): unit tests for the window/program model; standard coding standards; simulation fallback for undetected hosts; Fiddle Yard rewrite later.
8. **Process/agents** - ANSWERED (Round 5): a designer agent is needed (UI WPF/WinForms, panels, Fiddle Yard visu, layout visualization/diagnostics/manual override); an integrator/test agent is also considered necessary. Creation is deferred until the clarification phase is closed.
9. **Increment confirmation** - ANSWERED (Round 1): scope refined to include the init/program-startup story, host detection, settings, and simulation fallback; Fiddle Yard stays functional but unchanged.

### New Questions From Round 1

10. **Host detection technique** - ANSWERED (Round 2): ping on host name, TCP connect to Koploper, ping for YardController; one uniform detection layer later after C-code changes.
11. **Settings scope** - ANSWERED (Round 2): use `app.config` if appropriate; settings belong in core, not the visualization; default button per entity plus undo (Ctrl-Z). Exact value list and undo scope still to be detailed.

### New Questions From Round 4

12. **Amplifier error to Koploper**: How should an amplifier error (overtemperature/overcurrent) be reported to Koploper? Is there an ECoS overload/stop semantics to reuse (to be researched in datasheets/internet)?
13. **Shuttle line (pendelbaan)**: How should the shuttle line (max 4, min 1 locomotive between 3 stations) be controlled - by Koploper or by C# - and how does it relate to the main line and the Yard handover?
14. **Test layout** (still open from earlier): Which 4 amplifier addresses/IDs are on the test layout, and is there a fixed wiring/locomotive assignment? - ANSWERED (Round 5): look up in the spreadsheet candidates; block topology is on paper only.
