# Simple Loop — Gate A: Physical Readiness & Preflight Checklist

**Step 2 of the Simple Loop program** (`docs/simple-loop-validation-roadmap.md` §7).
**Status:** Gate A authorized for preparation + guided, voltage-free physical inspection only.
No active Real-mode start, no track power, no locomotive movement, no firmware flash.

**Authority:** This checklist is voltage-free. `LIVE_HARDWARE` for Gate B (active connection,
neutral observation) is a separate, not-yet-granted authority.

**Evidence rule:** physical facts are confirmed by the operator; nothing is assumed. Missing or
unknown data is recorded as `UNKNOWN`, never filled in by invention.

## The physical Simple Loop (reference)

| Logical section | Physical amplifier (ModBus) | Koploper bezetmelder |
|---|---:|---|
| 1 | 1 | 1.01 |
| 2 | 3 | 1.02 |
| 3 | 4 | 1.03 |
| 4 | 6 | 1.04 |

No switches. Same logical topology as FullSimulation; only the physical binding differs.

## Known firmware/hardware facts (from `docs/firmware-toolchain-readiness.md` F1)

- Amplifier = `TrackAmplifier4.X` (PIC18F25K40), baseline source `928ea7c` restored.
- Reference image = the **Offset** configuration; C# host checksum **`0x251F`**; HEX SHA-256
  `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D`.
- Firmware identity readback = `HR_SW_CHECKSUM` (HoldingReg11), read from flash `0x7FFE/0x7FFF`.
- Amplifier address (1/3/4/6) is **assigned dynamically by the PIC32 master** during the config
  sequence (boot at `0xAA` "unconfigured"; `ID_PORT` low → config mode; master writes
  `HR_STATUS_CONFIG_ID_MASK`; ID released → latch + `HR_STATUS_ID_SET_BIT`). Verified in Gate B
  via slave detection, not a DIP switch.
- Occupancy = `HR_STATUS` (HoldingReg2) bit 10 (`CMP1_GetOutputStatus()`), per-section.
- PWM: `HR0` bits 0–9 = setpoint (mask `0x03FF`), bit 15 = EMO; neutral **399** (~50% duty);
  forward 400–799; reverse 1–398. `LM_BRAKE` (RC5) asserted at boot.
- Telemetry: HR4 fuse voltage, HR5 H-bridge temperature, HR6 H-bridge current, HR_STATUS
  (occupancy bit 10, thermal bit 11, BEMF bits 0–9). **No overcurrent bit (bit 12 never set), no
  MMDC, no watchdog/ramp** in the baseline firmware.

## Checklist

### A1 — Amplifier presence & identification
- [ ] Four `TrackAmplifier4.X` prototype amplifiers physically present and connected.
- [ ] Board labels / revisions noted (any identifying marking).

### A2 — Address setting 1/3/4/6
- [ ] Confirm the four amplifiers are wired for dynamic address assignment (backplane slots /
      ID-pin configuration). Actual addresses are verified in Gate B via slave detection.

### A3 — PIC32 master + RS-422
- [ ] `TrackController5` PIC32 master present and powered-off during this inspection.
- [ ] RS-422 / 9-bit ModBus wiring between PIC32 and amplifiers/backplane.
- [ ] Ethernet between the PC (C#) and the PIC32 master (reference `CoreSettings`: IP
      `192.168.1.193`, send port `10000`, receive port `10001`).

### A4 — Oval connection
- [ ] Four track sections wired to the four amplifiers (section 1→amp1, 2→amp3, 3→amp4, 4→amp6).

### A5 — Power & current limiting
- [ ] Power supply present; rated voltage/current noted.
- [ ] Current limiting / fuses / breakers present and rated.

### A6 — Stop / EMO
- [ ] Physical Stop/EMO reachable and functional (hardware emergency-stop path, independent of C#).

### A7 — Measuring instruments
- [ ] Oscilloscope and/or multimeter available for PWM/output validation (Gate B).

### A8 — Occupancy support (firmware + hardware)
- [ ] Comparator input (`CMP1`) wired to the track for per-section occupancy detection
      (this is what the baseline firmware reports as `HR_STATUS` bit 10).

### A9 — Firmware / checksum reference
- [ ] Reference checksum `0x251F` recorded; Gate B reads `HR11` (`HR_SW_CHECKSUM`) to establish
      the actually-flashed image identity (and reports any mismatch, not just `0x251F`).

### A10 — C# Real-mode configuration
- [ ] `simple-loop.json`: physical mapping 1/3/4/6, one bezetmelder per block (1.01–1.04),
      DCC decoder addresses 1/2.
- [ ] `CoreConfiguration` track-controller IP/ports match the actual PIC32 master.

## Observations log (filled during the guided inspection)

| # | Item | Operator observation | Status |
|---|---|---|---|
| A1 | Amplifier presence/identification | 4 amplifiers present, connected to slots 1/3/4/6; universal boards (no individual ID) | confirmed |
| A2 | Address setting 1/3/4/6 | Address assigned by the PIC32 master via backplane pin-switching (slot select) during the ModBus init sequence; slot 1→addr 1, 3→3, 4→4, 6→6 | confirmed (mechanism); actual addresses verified in Gate B |
| A3 | PIC32 master + RS-422 | PIC32 present and powered off; RS-422/9-bit ModBus present; Ethernet PC↔PIC32 present (reference 192.168.1.193:10000/10001) | confirmed |
| A4 | Oval connection | Closed loop of 4 sections, no switches; section 1→amp1, 2→amp3, 3→amp4, 4→amp6, wired by hand per Koploper block allocation | confirmed |
| A5 | Power & current limiting | Operator: "ready"; specific voltage/current ratings + limit values not individually stated | confirmed-ready; specific values to be captured at Gate B preflight |
| A6 | Stop/EMO | Operator: "ready"; physical emergency-stop reachable (type not stated) | confirmed-ready; re-confirm reachability at Gate B preflight |
| A7 | Measuring instruments | Operator: "ready"; scope/multimeter available (type not stated) | confirmed-ready; instrument details at Gate B |
| A8 | Occupancy comparator (CMP1) | Not individually stated by operator; baseline firmware reports CMP1 → HR_STATUS bit 10 | UNKNOWN (to be confirmed at Gate B via readback) |
| A9 | Firmware/checksum reference | Reference checksum 0x251F (Offset HEX SHA-256 B848EF040DBA538D...); actual flashed identity UNPROVEN until HR11 readback | recorded from repo; verified in Gate B |
| A10 | C# Real-mode config | simple-loop.json physical 1/3/4/6, one bezetmelder/block, decoder 1/2; CoreConfiguration IP/ports | recorded from repo |
