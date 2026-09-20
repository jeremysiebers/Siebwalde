# Koploper / ECoS Interface (Reconnaissance)

Status: Increment 6, step 1 (protocol reconnaissance). Verified from source code unless marked otherwise. No runtime test with Koploper has been performed in this session.

References: `Ecos ESU info/ecos_pc_protocole.pdf`, `Ecos ESU info/ecos_pc_interface2.pdf`, `Ecos ESU info/ecos_pc_interface3.pdf`, `Ecos ESU info/ecos-master.zip`.

## Port Roles (resolved)

| Port | Owner | Direction | Purpose |
| --- | --- | --- | --- |
| `15471` | `EcosEmulatorServer` (C#) | Koploper connects TO C# | ECoS command-station protocol: Koploper sends `set/get/...`; C# replies and emits events. |
| `5700` | Koploper (external info) | C# connects TO Koploper | Koploper reports locomotive-to-block position records. |

So: `15471` is the command interface, `5700` is the position/telemetry interface.

## ECoS Commands Handled By The Emulator

`SimpleEcosBackend.HandleAsync` dispatches: `set`, `get`, `queryObjects`, `request`, `release`, `create`, `delete`.

Locomotive control arrives as `set(<objectId>, speed[<value>])`, `set(<objectId>, dir[<value>])`, and `set(<objectId>, func[<index>,<value>])`.

- `speed` and `dir` are applied to the locomotive and forwarded to `IHardwareBackend.SetLocoSpeed(loco.Address, loco.Speed, loco.Direction)`.
- `func` updates `loco.Functions[index]`.
- Switch commands forward to `IHardwareBackend.SetSwitch(decoderAddress, outputIndex, on)`, which returns whether the command reached a physical output. When it returns false (an unmapped address), the ECoS backend sends no state event, so the logical state cannot silently disagree with the layout.

This `set(id, speed[...])` / `set(id, dir[...])` traffic is the "per-encoder command" described by the product owner.

## Position Records From Koploper (port 5700)

`KoploperExternalInfoClient` parses `0x1B`-separated records with 5 fields:

| Field | Example | Meaning |
| --- | --- | --- |
| 0 | `&4` | Header containing the locomotive number (strip leading `&`). |
| 1 | `4` | Block number. |
| 2 | `11:26:51` | Model time. |
| 3 | `23:37:16` | PC time. |
| 4 | `Route onbekend` | Description. |

`0x00` is ignored; the record ends after the 5th field separator. The client stores `_locToBlock[loco] = block`, raises `BlockEntered(loco, block)`, and exposes `TryGetBlockForLoc(loco)`.

## Seams To Build On

| Interface | Members | Role in the translation layer |
| --- | --- | --- |
| `IHardwareBackend` | `SetPower(bool)`, `SetLocoSpeed(address, ecosSpeed, direction)`, `SetSwitch(decoderAddress, outputIndex, on) -> bool` | Commands from Koploper. The bool return tells the ECoS backend whether the command reached a physical output. |
| `IHardwareFeedbackSink` | `OnSwitchChangedAsync(ecosId, decoderAddress, outputIndex)`, `OnSensorChangedAsync(sensorId, occupied)` | Occupancy/switch feedback to Koploper. |
| `IBlockPositionProvider` | `TryGetBlockForLoc(loc)`, `BlockEntered(loco, block)` | Locomotive-to-block position from Koploper. |

## Proposed Translation Layer (step 2 design)

1. **Track-amplifier hardware backend**: implement `IHardwareBackend` so that `SetLocoSpeed(address, ecosSpeed, direction)`:
   - resolves the block via `IBlockPositionProvider.TryGetBlockForLoc(address)`;
   - resolves the amplifier(s) from the configured block topology;
   - converts `ecosSpeed` to a PWM setpoint;
   - calls `TrackApplicationVariables.SetDesiredAmplifierControl(slaveNumber, pwm, emoStop)`.
2. **Look-ahead**: when the current block is free, also command the next block's amplifier so the locomotive crosses smoothly.
3. **Occupancy feedback**: amplifier occupancy (from `TrackCommClientAsync` amplifier frames) must reach `IHardwareFeedbackSink.OnSensorChangedAsync(sensorId, occupied)` so Koploper receives it as an ECoS sensor event.
4. **Topology configuration**: block-to-amplifier chaining (for example `block1=amp1, block2=amp2`) as a user-editable `app.config` setting.
5. **Speed mapping**: convert the ECoS speed value Koploper sends into the amplifier PWM range (0..799).

## Verified From A Live Session (2026-09-17)

A real Koploper session against the emulator was captured in `Logging\17-09-2026_EcosEmuTrace.txt`. Confirmed behaviour:

### Locomotive creation

Koploper can **synchronize** its locomotive data with the digital central (the ECoS emulator). This sync makes the placed trains known to the central. When the central's loco list (our `locos.json`) is empty, Koploper reports that a sync is required; the product owner has tested this and it works.

Consequence: do **not** pre-seed `locos.json`. Let Koploper create/sync the locomotives. Pre-seeding causes duplicate locos for the same decoder address (seeded 1000/1001 plus Koploper-created 1002/1003).

Koploper creates locomotives on the command station with:

```
create(10,name["Loco1"],protocol[DCC28],addr[1],append)
create(10,name["Loco2"],protocol[DCC28],addr[2],append)
```

The emulator assigns the next object id (1002, 1003, ...) and persists them. Note Koploper used protocol `DCC28` here, not `DCC128`.

### Driving

Koploper drives a locomotive with `set(<ecosId>, speedstep[<n>])`, ramping the step over time (observed 1,2,3,...,7 in quick succession). Direction uses `set(<ecosId>, dir[...])`. The emulator echoes `TX: <ecosId> speed[<n>]`.

Important: Koploper drives the object id it created (for example 1002), not a pre-seeded one for the same decoder address. Pre-seeding `locos.json` with the same address but a different id therefore creates duplicates (1000/1001 seeded + 1002/1003 created).

### Occupancy feedback

The simulator reports occupancy to Koploper as ECoS sensor events:

```
[HW-FEEDBACK] 100 state[0x110]
TX: <EVENT 100>
TX: 100 state[0x110]
```

Feedback module `100` carries the 16 occupancy inputs; the state is a bitmask.

### Position records

`[EXT] Loc <n> -> Block <m>` is the **current** block of the locomotive (observed as the train advanced: block 4 -> block 3). The description was `Route onbekend` in every observed record; no destination/route was transmitted.

### Startup dependency

If the emulator has no locomotives when Koploper connects, Koploper reports 0 locos, sends no `create` until the operator adds them in Koploper, and drives nothing. The loco list must exist (or be created by the operator) before driving.

## Real-System Control Flow (design, confirmed with product owner 2026-09-17)

This is the intended flow on real hardware. It supersedes the earlier "delta-sync" idea; the practical look-ahead is one block ahead.

1. **Start position.** The operator drags a locomotive onto the block where it physically stands in Koploper. Koploper transmits this start position (before driving starts).
2. **Driving decision.** Koploper decides to drive: it checks the occupancy of the next block; if free it reserves the route internally (Koploper's driving-schedule map) and commands the locomotive with setpoints (`set(<ecosId>, speedstep[<n>])`).
3. **Translation in C#.** C# maps the locomotive's start position (block) through the block-to-amplifier table and forwards the setpoint to the correct amplifier(s).
4. **Topology and switches.** C# needs a block-adjacency list (how blocks are chained) to derive the next block, plus a switch mapping list: real switch <-> Koploper switch designation, and the default init state after Koploper switches (straight or diverging). The block and switch list in C# must know this plan/order so the next block can be derived.
5. **Occupancy.** When the locomotive enters the next block, an occupancy report is generated; C# forwards it to Koploper and it is also visible on the position port (5700).
6. **Divergence check (optional, wanted).** If things drift apart, C# commands Koploper to stop (an ECoS stop command can be sent to Koploper) and logs that something diverged and what (diagnostics). A dedicated diagnostics agent may be added later.

### C# data required

| Data | Purpose |
| --- | --- |
| Block -> amplifier table | Which amplifier(s) drive a block. |
| Block adjacency / chain | Derive the next block for look-ahead. |
| Switch mapping | Real switch <-> Koploper switch designation. |
| Switch default init state | Straight or diverging after Koploper switches. |
| Loco position (from `[EXT]`) | Which block the locomotive currently occupies. |
| Amplifier occupancy | Source of occupancy for the real system (amplifier -> C# -> Koploper). |

### Causality note

In the current simulator the causality is inverted (Koploper position -> simulator derives occupancy -> echoed back), because the simulator has no independent sensor model. On real hardware the amplifier occupancy is the source: amplifier -> C# -> Koploper -> `[EXT]` position confirmation.

## Terminology (important - Koploper vs amplifier sections)

Koploper and our C# model use different meanings for "block". Keep these apart:

| Term | Meaning |
| --- | --- |
| **Bezetmelder** (occupancy detector) | A single detection point/section of arbitrary track length. Koploper numbers them per block, for example `1.03` (first point) and `1.04` (last point). |
| **Koploper block** | A *collection* of occupancy detectors placed strategically. One Koploper block can cover several physical amplifier sections. Koploper requires at least 2 detectors per block (an entry detector to slow the locomotive down and a stop detector to stop it), so a locomotive can stop precisely in a station. |
| **Amplifier section** | The traditional physical division of the layout where each section has its own track amplifier. This is what `BlockTopology` currently models (called "block" in the code). |

Consequence: our `BlockTopology` "block" numbers are **amplifier sections**, not Koploper blocks. A separate mapping is required:

- Koploper block -> set of bezetmelders -> set of amplifier sections.
- Amplifier section -> its amplifier(s) (already in `BlockTopology`).

### Koploper oval (reference)

The product owner's current Koploper oval was shared as screenshots:

- **Layout overview**: oval with Koploper blocks 1..5, occupancy detectors (red), switches (cyan), and a `V`/`N` marker.
- **Digital components**: `Bezetmelder` type with HSI-88 counts; `Wisseldecoders 0 => nrs 1 t/m 2048`.
- **Blocks maintenance**: block graph (1-2-3-4-5).
- **Switch routes ("wisselstraten")**: `Van: 1 naar 2` with a route sequence number; defines the route numbering.
- **"Waar in blokken"**: `Van: 1 naar: 2` -> "Welke bezetmeldpunten komen er": `1e punt [=1.03]`, `Laatste punt [=1.04]`.

This confirms: a Koploper block (for example block 1) is described by a start and end bezetmelder, and one such block spans multiple amplifier sections.

## Test / Simulation Oval (reference)

This is the small oval used for simulation and interface testing. The real layout is much larger, and the hardware oval currently in use has **no switches** at the moment.

Derived from a live Koploper session (trace `Logging\19-09-2026_EcosEmuTrace.txt`):

- **Block chain**: `1 -> 2 -> 3 -> (4 or 5) -> 1`. Blocks 4 and 5 are the two parallel top tracks (the passing loop). In the observed session locomotive 1 used block 4 and locomotive 2 used block 5.
- **Switches**: Koploper commands `set(11, switch[<address><g|r>])` for addresses **1, 2, 51, 52, 53, 54, 55** (`g` = straight, `r` = diverging).
- **Occupancy**: the simulator reports a 16-bit mask on feedback module `100`; the values cluster per locomotive (bases `0xC0..` and `0x300..`), consistent with **2 sensors per block** (the minimum Koploper requires for brake + stop detection).
- **Limitation**: the exact bezetmelder numbering (`block.point`, for example `1.03` / `1.04`) cannot be derived reliably from the emulator trace, because the simulator uses its own encoding and derives occupancy from the Koploper position. The authoritative source is Koploper's own configuration or the "Waar in blokken" data.

Example topology configuration for this oval:

```
amps:   1:1, 2:2, 3:3, 4:4, 5:5
routes: 1>2, 2>3, 3>4@<switch>:<position>, 3>5@<switch>:<position>, 4>1, 5>1
```

## Test / Simulation Oval - Authoritative Mapping (from Koploper export 2026-09-19)

Source: `Logging\Ovaaltje\BaanOverzicht_20260919_1222_0001.html` (blocks) and `..._0002.html` (decoder outputs, bezetmelders, loco addresses).

### Blocks and bezetmelders

| Koploper block | Bezetmelders | Amplifier section |
| --- | --- | --- |
| 1 | 1.01, 1.02 | 1 |
| 2 | 1.03, 1.04 | 2 |
| 3 | 1.05, 1.06 | 3 |
| 4 | 1.07, 1.08 | 4 |
| 5 | 1.09, 1.10 | 5 |

Each Koploper block has exactly 2 bezetmelders (entry + stop), as required by Koploper.

### Decoder outputs

| Address | Type | Used in block |
| --- | --- | --- |
| 1 | Wissel (switch) | - |
| 2 | Wissel (switch) | - |
| 51 | Sein (signal) | 1 |
| 52 | Sein | 2 |
| 53 | Sein | 3 |
| 54 | Sein | 4 |
| 55 | Sein | 5 |

### Block routing

- Block 1 is reached from 4 and from 5.
- 1 -> 2 -> 3 -> (4 or 5) -> 1.

The branch `3 -> 4` versus `3 -> 5` is controlled by the two switches (addresses 1 and 2).

### Bezetmelder usage

| Bezetmelder | Used for | Occupies block |
| --- | --- | --- |
| 1.01 | Van 4 naar 1, Van 5 naar 1 | 1 |
| 1.02 | Van 4 naar 1, Van 5 naar 1 | 1 |
| 1.03 | Van 1 naar 2 | 2 |
| 1.04 | Van 1 naar 2 | 2 |
| 1.05 | Van 2 naar 3 | 3 |
| 1.06 | Van 2 naar 3 | 3 |
| 1.07 | Van 3 naar 4 | 4 |
| 1.08 | Van 3 naar 4 | 4 |
| 1.09 | Van 3 naar 5 | 5 |
| 1.10 | Van 3 naar 5 | 5 |

### Locomotives

| Decoder address | Description |
| --- | --- |
| 1 | Loc1 |
| 2 | Loc2 |

### Concrete topology configuration for this oval

```
amps:   1:1, 2:2, 3:3, 4:4, 5:5
routes: 1>2, 2>3, 3>4@1:0, 3>5@1:1, 4>1, 5>1
```

### Switch state that selects 3 -> 4 versus 3 -> 5 (PROVEN)

Earlier revisions marked this as provisional. It is now proven from the live trace
`Logging\19-09-2026_EcosEmuTrace.txt`, which repeats the pattern six times per route.

Correlating every switch command for addresses 1 and 2 with the block Koploper last
reported for the moving locomotive:

| Locomotive | Route taken | Commands Koploper sends while in block 3 | Occurrences |
| --- | --- | --- | --- |
| Loc 1 | 3 -> 4 | `set(11,switch[1g])` then `set(11,switch[2r])` | 6 |
| Loc 2 | 3 -> 5 | `set(11,switch[1r])` then `set(11,switch[2g])` | 6 |

The block sequence confirms the destinations: loco 1 reaches block 4 (trace lines 512,
1616, 2692, ...) and loco 2 reaches block 5 (lines 1074, 2158, ...).

So **both** switches take part in selecting the branch, and they are always commanded as a
complementary pair:

| Route | Switch 1 | Switch 2 |
| --- | --- | --- |
| 3 -> 4 | `g` (straight, output 0) | `r` (diverging, output 1) |
| 3 -> 5 | `r` (diverging, output 1) | `g` (straight, output 0) |

Because the pair is complementary, the routing model can condition the transition on switch 1
alone, which is what `routes: 3>4@1:0, 3>5@1:1` expresses. The `@<id>:<position>` id is the
**ECoS/Koploper switch address** and the position uses ECoS semantics (0 = `g`/straight,
1 = `r`/diverging).

### Switch mapping configuration

Switches are translated from ECoS/Koploper addresses to physical switch outputs by
`SwitchMapConfig`, in the same style as the other mapping settings:

```
switches: <ecosAddress>:<physicalAddress>[:inverted][:g|r|keep], ...
```

- `ecosAddress` - the address in `set(<id>,switch[<addr>g|r])`.
- `physicalAddress` - the accessory output address driven on the layout.
- `inverted` - swap `g`/`r` between the ECoS request and the physical output.
- `g` / `r` / `keep` - the position to drive to during initialization; `keep` (also the
  default when omitted) leaves the output untouched.

Shipped default: `switches: 1:1:keep, 2:2:keep` (identity mapping for the two oval switches).

**Unresolved:** the power-on/rest position of the real layout switches is not known, so the
shipped default is `keep`: no output is driven and no switch state is reported to Koploper,
rather than guessing a position that could disagree with the layout. Set `g` or `r` per switch
once the real rest position is confirmed.

Note: addresses 51..55 are signals, not turnouts. They are commanded by Koploper in the same
`switch[...]` form but are deliberately not mapped, so their commands are ignored and no
switch state is reported for them.

### Bezetmelder to sensor/bit mapping (verified)

The simulator's sensor numbering matches Koploper's bezetmelder numbering exactly:

- `SimpleEcosBackend.OnSensorChangedAsync(sensorId, occupied)` sets `bit = sensorId - 1` in feedback module `100`.
- `TrackSimulatorBackend.InitOvalWithLadder()` assigns block 1 -> sensors 1,2; block 2 -> 3,4; block 3 -> 5,6; block 4 -> 7,8; block 5 -> 9,10 (enter, exit).
- Koploper bezetmelders: block 1 -> 1.01, 1.02; block 2 -> 1.03, 1.04; ... block 5 -> 1.09, 1.10.

| Koploper bezetmelder | Sensor id | Bit in module 100 |
| --- | --- | --- |
| 1.01 | 1 | 0 |
| 1.02 | 2 | 1 |
| 1.03 | 3 | 2 |
| 1.04 | 4 | 3 |
| 1.05 | 5 | 4 |
| 1.06 | 6 | 5 |
| 1.07 | 7 | 6 |
| 1.08 | 8 | 7 |
| 1.09 | 9 | 8 |
| 1.10 | 10 | 9 |

Verified against the live trace: loc1 in block 2 + loc2 in block 5 gives `0x104` (bits 2+8); block 3 + block 5 gives `0x110` (bits 4+8); block 4 + block 5 gives `0x140` (bits 6+8). Transient values appear during a transition, which explains earlier confusion.

Consequence: for real hardware, amplifier occupancy must be reported as sensor ids 1..10 (bit = id - 1) so Koploper sees the correct bezetmelders.

## Test Setups And Detector Count

- **Real hardware (current, near-term target)**: 4 proto amplifiers and an oval **without switches**. Koploper block = amplifier section 1-1. So 4 amplifiers, 4 blocks, maximum 2 trains running without collision. This is what we will test with Koploper next.
- **Simulation (current Koploper oval)**: 5 blocks, 2 bezetmelders each, 2 switches, with a passing loop.
- A Koploper block does **not** always have exactly 2 bezetmelders; it can have one or more. The physical minimum of 2 applies where precise stopping is required (for example a station: a brake detector plus a stop detector).
- **Firmware status**: the test firmware (bootloader + communication layer) accepts setpoints and returns the occupancy signal. MMDC and further behaviour will be added later.

## Trace Data (2026-09-11)

- No Koploper/ECoS trace data exists in `Logging/` or anywhere else in the repository. The emulator and the external-info client log to the console only (`[EXT]`, `[LOCO]`, `[ECOS]`, `[HW-FEEDBACK]`, `TX:`), and no console capture was kept.
- Therefore the open question "is the block field the current block or the next/reserved block?" cannot be answered from code or history.
- Added console-to-file tracing to `SiebwaldeApp.EcosEmu.Host` (`EcosEmuTrace`): it tees console output to `Logging\<dd-MM-yyyy>_EcosEmuTrace.txt`. Run a Koploper session (for example the simple circle) against the emulator to capture the records, then determine from the trace:
  - whether `[EXT] Loc X -> Block Y` reports the current block or the next/reserved block;
  - what the description field contains (the observed sample was `Route onbekend`);
  - whether `[LOCO] Address A is now in block B` and the `TX:` speed/sensor frames can be correlated with the position records.

## Implementation Status

- `SiebwaldeApp.Core.AmplifierSpeedMapper` - done (ECoS 0..127 + direction -> PWM).
- `SiebwaldeApp.Core.BlockTopology` - done: block -> amplifier mapping plus a routing model. Configuration sections: `amps: block:amp[+amp]` and `routes: from>to[@switchId:position][!]`; `!` forbids look-ahead (for example a station departure block).
- `SiebwaldeApp.Core.IOccupancyProvider` - done: block occupancy abstraction, with an explicit `IsBlockOccupancyKnown` so "unknown" is never read as "clear". The real implementation (`TrackAmplifierOccupancyProvider`) reads the existing amplifier holding registers, the same value the track-amplifier page decodes.
- `SiebwaldeApp.Core.LookAheadPlanner` - done: picks the next block to pre-command, filtered by switch position, excluding no-look-ahead transitions and occupied targets.
- `SiebwaldeApp.Integration.TrackAmplifierHardwareBackend` - done: implements `IHardwareBackend`; resolves locomotive -> block, block -> amplifiers, speed -> PWM, queues writes, and (when a planner and occupancy provider are supplied) also commands the next block. `SetPower(false)` sets all mapped amplifiers to neutral. `SetSwitch` returns false and drives nothing, because switches are driven by accessory decoders and that real path is not wired yet.
- `SiebwaldeApp.Core.SwitchMapping` - done: parses `SwitchMapConfig` (`ecosAddress:physicalAddress[:inverted][:g|r|keep]`), records invalid and duplicate entries in `Errors` instead of turning them into a plausible mapping.
- `SiebwaldeApp.Integration.SwitchController` - done: translates an ECoS switch request into a physical drive, tracks the logical (ECoS) position for routing/look-ahead and the physical position for diagnostics, and initializes configured defaults.
- `SiebwaldeApp.Integration.SwitchTranslatingHardwareBackend` - done: applies the shared switch translation in front of whichever hardware backend is active, so real and simulator mode use one control path.
- `SiebwaldeApp.Integration.ControlSafetyInterlockBackend` - done: refuses non-zero movement while a safety fault is latched (per loco, or layout-wide for an unattributable fault) while stops, power-off and switch commands stay allowed.
- `IHardwareBackend.SetSwitch`, `SetPower` and `SetLocoSpeed` return `bool`: the ECoS backend never reports a state change or a movement that did not actually reach the hardware.
- New non-UI project `SiebwaldeApp.Integration` (`net8.0-windows7.0`) references Core + EcosEmu; all translation logic stays out of the WPF project.
- Tests: 204/204 passing in `SiebwaldeApp.Core.Tests`.
- Still open: the real switch-output path (accessory decoder), physical switch feedback, the real-layout power-on switch positions, the amplifier occupancy firmware bit, simulator occupancy through the production abstraction, and the watchdog for stale occupancy. See `docs/backlog.md`.

### Divergence, safety stop and diagnostics

`SiebwaldeApp.Core` models the control path's health without protocol or UI wording: `DiagnosticSeverity` (Info/Warning/Rejected/StopRequired), `DiagnosticCode` (UnmappedAddress, InvalidConfiguration, RouteSwitchMismatch, CommandNotApplied, CommandedObservedMismatch, OccupancyMismatch, BackendUnavailable, StateUnknown), `SafetyAction` (None/StopLoco/StopLayout) and the immutable `ControlDiagnostic` (code, severity, subject, detail, loco/block/switch context, timestamp, action taken). `ControlDiagnostics` keeps a bounded history and a separate latched unsafe state.

Requested, commanded and observed stay separate: `SwitchController` records the requested (logical ECoS) position and the commanded (physical) position, and only compares against an observed position when switch feedback actually exists. Real mode has none, so its state is `StateUnknown`, never a confirmation.

`DivergenceChecker` checks a route before the look-ahead pre-commands the next block. `ControlSafetyGuard` applies the reaction once per fault key (no stop storm); a loco-scoped fault stops that locomotive through the existing `SetLocoSpeed(address, 0, dir)` path, and an unattributable fault stops the layout through the existing `SetPower(false)` path (the same central stop as `set(1,stop)`). The first StopRequired latches until an explicit reset.

### Safety movement interlock

`ControlSafetyInterlockBackend` sits between the ECoS backend and the hardware backend, so every movement command passes one policy while a fault is latched: non-zero movement is refused for the affected locomotive (or for all locomotives when the fault is layout-wide), power-on is refused for a layout-wide fault, and stops, power-off and switch commands always pass. A corrective switch command never unlatches anything.

`IHardwareBackend.SetPower` and `SetLocoSpeed` return `bool`. When a movement is refused, `SimpleEcosBackend` replies `<END 8 (SAFETY_INTERLOCK)>`, keeps its logical speed unchanged and sends no `speed[...]`/`dir[...]` event, so Koploper is never told a refused movement succeeded. Rejections are reported once per locomotive per latch (`MovementRejectedBySafety`).

Recovery is explicit: `ControlSafetyGuard.Reset()` revalidates every latched fault through `DivergenceChecker.IsResolved` and is refused with `ResetRefused` while the condition persists. Only after the correction plus a successful reset does movement become possible again.

### Real occupancy path (verified, already working)

The amplifier occupancy travels over the existing embedded transport; no firmware change is required.

```
PIC18 amplifier
  processio.c: g_occ = CMP1_GetOutputStatus()
  processio.c: HR_STATUS (HoldingReg2) bit 10 = g_occ
  -> PIC32 master SLAVEINFO frame (12 holding registers + counters)
  -> TrackCommClientAsync.HandleNewDataAsync
       parses HEADER + SLAVEINFO, writes trackAmpItems[slaveNumber].HoldingReg
       and sets SlaveDetected, then raises AmplifierDataReceived
  -> TrackAmplifierItem.HoldingReg[2]
```

Both consumers read the same value:

- the track-amplifier page decodes `IsOccupied = (hr2 & TrackAmplifierRegisters.OccupiedBit) != 0`;
- `TrackAmplifierOccupancyProvider` uses `TrackAmplifierRegisters.IsOccupied(HoldingReg)` for the amplifier sections of a Koploper block, and therefore reads the identical bit from the identical array.

**Stale comment:** `TrackAmplifier4.X/modbus/General.h` still annotates `HR_STATUS_OCCUPIED_BIT` with "(TODO: implement when occupancy source known)", but `processio.c` already implements it. The comment is wrong, not the firmware.

**Known versus unknown.** `TrackAmplifierItem.LastDataReceivedUtc` is stamped when a frame is parsed, and `TrackAmplifierDataFreshness` decides whether that is recent enough (default 2 s, a policy value well above the comm client's 10 Hz republish cycle). A section counts as current only when it is detected **and** its data is fresh. `TrackAmplifierOccupancyProvider.IsBlockOccupancyKnown` requires every amplifier section that covers a block to be current; one silent section makes the whole block unknown. Unknown is never reported to Koploper as clear, is never treated as a free block by look-ahead, and produces `StateUnknown` (Rejected) rather than `OccupancyMismatch` (StopRequired) during a route check.

**Why freshness is derived and not read from existing state.** `SlaveDetected` is written once per parsed frame and is never cleared, `HoldingReg` keeps its last values indefinitely, and `TrackCommClientAsync._publishTimer` republishes `AmplifierDataReceived` every 100 ms for every amplifier with `SlaveDetected != 0` regardless of whether new data arrived. `ITrackTransport` exposes no connection-loss or health signal. So none of the existing state proves that data is current; the frame timestamp is the only reliable signal, and it is stamped in the same place the registers are stored.

### Physical validation (2026-09-19, real hardware)

The whole path has now been validated on the real amplifier setup, not only from code:

```
PIC18 CMP1 -> HR_STATUS bit 10 -> PIC32/master transport -> TrackCommClientAsync
  -> TrackAmplifierItem -> TrackAmplifierOccupancyProvider -> observability/freshness
```

Tested physical amplifier addresses: **1, 3, 4, 6** (the four proto amplifiers). All reported `SlaveDetected = 1` and the same firmware checksum `HR11 = 0x251F`, consistent with the CRC confirmation, so the initialization pipeline found 0 slaves to flash.

**Frame intervals (healthy operation, per amplifier):** median **40-42 ms**, maximum **66-70 ms**. The 2-second freshness timeout therefore has roughly **30x margin** under normal operation, and no timeout adjustment is required. An aggregate view over all slaves that produced frames showed a worst interval of 112 ms (~18x margin).

**Occupancy bit:** physical occupancy changed `HR_STATUS` bit 10 exactly as expected.

| Amplifier 1 | `HR_STATUS` | bit 10 |
| --- | --- | --- |
| physically occupied | `0x2E02` | set |
| clear (load removed) | `0x2A01` | clear |

Only bit 10 was under test. The other `HR_STATUS` bits differ between these two samples and between amplifiers; no meaning is claimed for them here.

**Provider result once valid data was flowing:**

| Block (sections) | Backing | Result |
| --- | --- | --- |
| 1 (1), 3 (3), 4 (4) | detected physical amplifiers | `known = true`, `occupied = false` |
| 2 (2), 5 (5) | sections that do not exist | `known = false` |

`OccupancyAvailable = true` while fresh frames arrived. Missing or unavailable sections stayed **unknown** and were never falsely reported as clear.

### Physical validation of the freshness invariant

During the test the master stopped delivering fresh amplifier frames. This was observed for real, not simulated:

- the existing 100 ms C# republish mechanism kept firing;
- old `HoldingReg` values remained present in the container;
- the tell-tale sign was the median frame interval rising from ~41 ms to ~94 ms, i.e. dominated by the republish timer instead of real frames;
- `LastDataReceivedUtc` correctly became stale;
- `OccupancyAvailable` became **false**;
- every block became **unknown**;
- the old clear values were **not** treated as known-clear.

This physically confirms the safety invariant **`stale != clear`**, and confirms that `AmplifierDataReceived` on its own is not proof of fresh hardware data.

### Live command path validation (2026-09-19)

The outbound setpoint path was validated live with a free-running DC motor on amplifier 1, driven manually from Koploper's hand controller:

```
Koploper hand controller -> ECoS port 15471 -> SimpleEcosBackend
  -> block/amplifier translation -> hardware backend -> runtime write loop
  -> PIC32 master -> amplifier 1 -> physical motor
```

The motor responded to the hand controller. Observed C# -> amplifier response: **~120 ms** (the 10 Hz runtime write loop plus a ~40 ms frame round-trip). No firmware was changed or flashed, and no switch/accessory output was used.

**Harness lesson (not a product defect):** a standalone harness must start `TrackControlMain.StartRuntime`. Without the production runtime loop the setpoints stay in `PendingWrites` and never reach the amplifiers.

**Confirmed defect - DCC28 speed scaling.** The locomotive protocol is `DCC28` and Koploper/ECoS supplied steps `0..28`, but `AmplifierSpeedMapper.ToPwm` scales by 127. Live DCC28 step 24 produced only **~PWM 475** instead of approaching 799, so only part of the usable 400..799 range is used.

Preferred correction (not implemented): normalize protocol-specific speed at the ECoS/protocol boundary into the existing normalized `0..127` contract, so `IHardwareBackend` and `AmplifierSpeedMapper` stay protocol-independent.

**Validation-environment note.** The standalone checker used for this validation runs outside the normal application lifecycle and communication ownership. During the session it was able to leave the master communication session in a state that required reinitialization. This was not reproduced through the normal application lifecycle - where master/amplifier communication runs continuously, load/amplifier disconnects are already detected by the existing system, and a software reset path exists - so it is treated as a test-harness limitation rather than a production defect. The freshness result above is unaffected: it is about what the C# side does when fresh data stops arriving.



## Open Questions

- Which ECoS speed range does Koploper send (0..126 or 0..28)? Needed for the speed-to-PWM mapping. - RESOLVED: **0..127 (128 steps)**. The `ecos-master` C# library (`Ecos ESU info/ecos-master.zip`, `ECoSEntities/Locomotive.cs`) returns `128` from `GetNumberOfSpeedsteps()` for MM128/DCC128, and sends `set(<id>, speedstep[<step>])`. The emulator's `opt.StartsWith("speed")` also matches `speedstep[...]`.
- Do Koploper block numbers map 1:1 to ECoS sensor ids used for occupancy feedback? - Product owner: a mapping must be created in Koploper; a screenshot will follow, and the settings page will be extended so the mapping can be created and edited.
- What are the exact look-ahead rules? - Product owner: Koploper reserves a route ahead internally from occupancy data and may report "loc x to block 4" while C# still has the loc in block 1; then C# can compute the delta and drive multiple amplifiers in sync. If Koploper does not provide this, C# checks whether the next block is free and pre-sets the same setpoint one block ahead. Testable with a Koploper test design, unit tests, or a simulator.
- Should the track-amplifier backend replace or complement `TrackSimulatorBackend`? - RESOLVED: keep `TrackSimulatorBackend` as the simulation backend (it simulates blocks/trains/occupancy and reports sensors via `IHardwareFeedbackSink`) and add the real track-amplifier backend as an alternative, selectable like the Fiddle Yard real/simulator choice. `DummyHardwareBackend` is a minimal mock and can be kept for simple tests.

## Amplifier PWM Mapping (confirmed)

- ECoS sends speed steps `0..127`.
- The amplifier PWM is bidirectional: **neutral = 399**, **forward = 400..799**, **reverse = 398..1**.
- PWM `0` must never be used: it produces a clipping artefact.
- On the shuttle line (pendelbaan), after a locomotive change at the middle station and the relay switch-over, both amplifiers' driving direction must be reversed.

Implemented in `SiebwaldeApp.Core.AmplifierSpeedMapper`:

| Condition | PWM |
| --- | --- |
| speed 0 (stop) | 399 |
| forward, speed 1..127 | `400 + round((799 - 400) * speed / 127)` |
| reverse, speed 1..127 | `398 - round((398 - 1) * speed / 127)` |

Because of the linear interpolation, speed step 1 does not land exactly on the end point (forward speed 1 -> 403, reverse speed 1 -> 395); the end points are reached at speed 127.

