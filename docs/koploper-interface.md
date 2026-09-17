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

## Open Questions

- Which ECoS speed range does Koploper send (0..126 or 0..28)? Needed for the speed-to-PWM mapping. - RESOLVED: **0..127 (128 steps)**. The `ecos-master` C# library (`Ecos ESU info/ecos-master.zip`, `ECoSEntities/Locomotive.cs`) returns `128` from `GetNumberOfSpeedsteps()` for MM128/DCC128, and sends `set(<id>, speedstep[<step>])`. The emulator's `opt.StartsWith("speed")` also matches `speedstep[...]`.
- Do Koploper block numbers map 1:1 to ECoS sensor ids used for occupancy feedback? - Product owner: a mapping must be created in Koploper; a screenshot will follow, and the settings page will be extended so the mapping can be created and edited.
- What are the exact look-ahead rules? - Product owner: Koploper reserves a route ahead internally from occupancy data and may report "loc x to block 4" while C# still has the loc in block 1; then C# can compute the delta and drive multiple amplifiers in sync. If Koploper does not provide this, C# checks whether the next block is free and pre-sets the same setpoint one block ahead. Testable with a Koploper test design, unit tests, or a simulator.
- Should the track-amplifier backend replace or complement `TrackSimulatorBackend`? - RESOLVED: keep `TrackSimulatorBackend` as the simulation backend (it simulates blocks/trains/occupancy and reports sensors via `IHardwareFeedbackSink`) and add the real track-amplifier backend as an alternative, selectable like the Fiddle Yard real/simulator choice. `DummyHardwareBackend` is a minimal mock and can be kept for simple tests.

## Amplifier PWM Mapping (product owner specification)

- ECoS sends speed steps `0..127`.
- The amplifier PWM is bidirectional with a neutral point near the middle of `1..799`.
- Forward: roughly `400..799` (the product owner wrote "399 - 800", but 799 is the maximum).
- Reverse: roughly `1..399`.
- PWM `0` must never be used: it produces a clipping artefact.
- On the shuttle line (pendelbaan), after a locomotive change at the middle station and the relay switch-over, both amplifiers' driving direction must be reversed.

Exact neutral value and end points must be confirmed before finalizing the mapping. Proposed mapping (to be confirmed):

| Condition | PWM |
| --- | --- |
| speed 0 (stop) | neutral (400) |
| forward, speed 1..127 | 400 + speed * (799 - 400) / 127 |
| reverse, speed 1..127 | 399 - speed * (399 - 1) / 127 |

