# Simple Loop — Physical Reference Baseline (Step 2)

**Status:** Physical validation of the Simple Loop oval (four prototype amplifiers, existing firmware) — software/protocol-level results recorded 2026-10-02. The missing "carry locomotive properties" mechanism is recorded as the next increment; see `docs/backlog.md`.

**Scope note:** this is a **software/protocol + operator-observed** baseline. Physical PWM behaviour (duty ~50% at HR0=399, motor stopped) was verified during the earlier **hardware development** (Product Owner-confirmed), **not re-measured in Step 2**. This step claims **no new V4 measurement evidence**. Three categories are kept distinct: (a) historical hardware-development verification, (b) current protocol observations (this step), (c) physical behaviour actually re-measured in this step (none). A new scope/multimeter measurement is only proposed when a concrete technical finding warrants it.

## 1. Reference environment

| Item | Value |
|---|---|
| C# revision | `feature/simple-loop-physical-validation` @ `54fc1a1` (on top of master `e6e49d8`) |
| Layout profile | `simple-loop.json` (4 sections, no switches, one bezetmelder/block 1.01–1.04) |
| Physical binding | section 1→amp 1, 2→amp 3, 3→amp 4, 4→amp 6 |
| Locomotives | DCC decoder addresses 1 (block 1) and 2 (block 3) |
| Amplifier firmware | PIC18 `TrackAmplifier4.X`, baseline `928ea7c` (F1), checksum **`0x251F`** (HR11 readback confirmed: "0 slaves require flashing") |
| PIC32 master | `TrackController5`, RS-422/9-bit ModBus, Ethernet 192.168.1.193:10000/10001 |
| Koploper | connects to C# on 15471 (ECoS); C# connects to Koploper on 5700 (external info) |

## 2. Verified behaviour (software/protocol + operator)

| Aspect | Result |
|---|---|
| Slave detection | Slave **1, 3, 4, 6** detected (+ backplane Slave 51, correctly non-track-amplifier) |
| Firmware identity | HR11 checksum `0x251F` on all four amplifiers; 0 slaves require flashing |
| Init | `Completed` (ConnectToEthernetTarget → DetectSlaves → FlashFwTrackamplifiers → EnableTrackamplifiers) |
| Observed neutral | movement permission granted on domain `{1,3,4,6}` (genuine protocol readback, no default-399-as-observation) |
| HR0 command path | `EXEC_MBUS_SLAVE_DATA_EXCH` writes to slaves 1/3/4/6; no write to logical amplifier 2 |
| Direction | **Fixed physically** (wire swap): PWM > 399 = clockwise (rechtsom), matching forward |
| Occupancy | amplifier HR_STATUS bit 10 → C# → EcosEmu module 100 → Koploper, per correct block (after operator fixed Koploper bezetmelder placement) |
| Full loop (1 loc) | yes; Koploper reports block sequence 4→1→2→3→4→…; all four amplifiers commanded via look-ahead |
| 2 locs | both commanded independently (objects 1000 + 1001); stalls at each block transition (see §4) |

## 3. FullSimulation vs Real

| Property | FullSimulation | Real |
|---|---|---|
| Block sequence | 1→2→3→4→1 | 4→1→2→3→4→… (same closed loop) |
| Section→amplifier | logical 1..4 | physical 1/3/4/6 |
| Direction (forward) | abstract | PWM > 399 = rechtsom (after wire swap) |
| Occupancy source | deterministic simulator | amplifier HR_STATUS bit 10 (CMP1) |
| Block transition | movement simulator drives it | Koploper position (5700) + speedstep; **no block-transition-driven re-command** (gap) |

## 4. Findings

1. **Direction polarity (FIXED):** PWM > 399 physically drove counter-clockwise; operator swapped track/motor wires. Not a software change.
2. **Communication hang / no watchdog:** amplifier→master comm stopped during the session; software re-init hung at `DetectSlaves`; only a power-cycle recovered it. Baseline firmware has no watchdog/comms-watchdog/auto-recovery (deferred Control Core). Relevant to Step 3/4.
3. **Occupancy "nothing in Koploper" was a Koploper config error:** the C# occupancy path was correct; the earlier JSIF_SimpleLoop had bezetmeldpunten on the wrong graphical line. Operator fixed it.
4. **Speed-command-driven mapping:** the block→amplifier mapping is driven by Koploper's `speedstep`, not by the block position. `BLOCK_TRANSITION` (Koploper position) does not re-command by itself.
5. **Missing "carry locomotive properties" (NEXT INCREMENT):** per-loc duty must run continuously and be carried across blocks (look-ahead set the next block before the transition; neutralise the vacated block to 399; keep per-loc duty separate). At a section transition, mismatched duty causes a current-limited equalisation/short-circuit current and the loc stalls.
6. **Safety requirement (within #5):** a vacated block must be neutralised to 399; a powered rail with stale non-neutral duty is a hazard.

## 5. Current firmware limitations (baseline)

- No MMDC, no over-current detection (HR_STATUS bit 12 never set), no watchdog/ramp, no comms recovery.
- HR0 is a command echo; there is no applied-PWM readback. Telemetry = HR4 (fuse voltage), HR5 (temperature), HR6 (current), HR_STATUS (occupancy bit 10, thermal bit 11, BEMF bits 0-9).

## 6. Evidence boundary (preserved)

`Requested ≠ Commanded ≠ Transmitted ≠ Protocol-observed ≠ Physical-observed`. Protocol-observed HR0 == 399 is the PIC18 holding-register echo, not a physical PWM/neutral measurement. Physical PWM duty was verified during hardware development (Product Owner-confirmed) but is **not re-measured here**; no new V4 measurement evidence is claimed.

## 7. Next increment

The "carry locomotive properties across the track" mechanism (with the control-source model decision A/B) — see `docs/backlog.md`. Not started.
