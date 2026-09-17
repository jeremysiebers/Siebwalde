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
- Switch commands forward to `IHardwareBackend.SetSwitch(decoderAddress, outputIndex, on)`.

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
| `IHardwareBackend` | `SetPower(bool)`, `SetLocoSpeed(address, ecosSpeed, direction)`, `SetSwitch(decoderAddress, outputIndex, on)` | Commands from Koploper. Replace/augment the simulator backend with a track-amplifier backend. |
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

## Trace Data (2026-09-11)

- No Koploper/ECoS trace data exists in `Logging/` or anywhere else in the repository. The emulator and the external-info client log to the console only (`[EXT]`, `[LOCO]`, `[ECOS]`, `[HW-FEEDBACK]`, `TX:`), and no console capture was kept.
- Therefore the open question "is the block field the current block or the next/reserved block?" cannot be answered from code or history.
- Added console-to-file tracing to `SiebwaldeApp.EcosEmu.Host` (`EcosEmuTrace`): it tees console output to `Logging\<dd-MM-yyyy>_EcosEmuTrace.txt`. Run a Koploper session (for example the simple circle) against the emulator to capture the records, then determine from the trace:
  - whether `[EXT] Loc X -> Block Y` reports the current block or the next/reserved block;
  - what the description field contains (the observed sample was `Route onbekend`);
  - whether `[LOCO] Address A is now in block B` and the `TX:` speed/sensor frames can be correlated with the position records.

## Implementation Status

- `SiebwaldeApp.Core.AmplifierSpeedMapper` - done (ECoS 0..127 + direction -> PWM).
- `SiebwaldeApp.Core.BlockTopology` - done (block -> amplifier mapping, parsed from configuration text).
- `SiebwaldeApp.Integration.TrackAmplifierHardwareBackend` - done: implements `IHardwareBackend`, resolves locomotive -> block via `IBlockPositionProvider`, block -> amplifiers via `BlockTopology`, speed -> PWM via `AmplifierSpeedMapper`, and queues writes through `TrackApplicationVariables.SetDesiredAmplifierControl`. `SetPower(false)` sets all mapped amplifiers to neutral. `SetSwitch` is not handled yet.
- New non-UI project `SiebwaldeApp.Integration` (`net8.0-windows7.0`) references Core + EcosEmu; all translation logic stays out of the WPF project.
- Still open: look-ahead (delta-sync and one-block-ahead fallback), occupancy feedback into `IHardwareFeedbackSink`, backend selection (real vs `TrackSimulatorBackend`), and the `app.config` topology setting plus settings-page editing.



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

