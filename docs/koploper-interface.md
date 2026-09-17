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

- Which ECoS speed range does Koploper send (0..126 or 0..28)? Needed for the speed-to-PWM mapping.
- Do Koploper block numbers map 1:1 to ECoS sensor ids used for occupancy feedback?
- What are the exact look-ahead rules (one block ahead, or until occupied)?
- Should the track-amplifier backend replace or complement `TrackSimulatorBackend`?
