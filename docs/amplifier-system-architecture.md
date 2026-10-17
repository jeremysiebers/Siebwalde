# Amplifier System — Cross-Layer Architecture and Phased Implementation Roadmap (Revision 2)

**Status:** Architecture / design proposal. Read-only investigation + design; no product code, firmware, or hardware was changed.
**Authority:** Translates the consolidated Product Owner requirements in `docs/amplifier-system-requirements.md` and the Product Owner decisions dated 2026-10-15 into a software-only cross-layer design. This document **replaces** Revision 1 (the 12→16 / HR9-identity approach). It does **not** authorize behavioral implementation, flashing, or live hardware.
**Prepared by:** Architect (Siebwalde, Workflow v1), under `ANALYZE_DESIGN`.
**Evidence conventions:** *Verified* = read from committed source at `master @ 11ba5e3`; *Inference* = derived design; *Assumption* = stated position; *PROPOSED* = new interface allocation or numeric value, **not** approved; `LIKE_THIS` = named value, no number selected. Requirement tags `[Agreed]`/`[To verify]`/`[Research]`/`[Deferred]` are reproduced from the requirements document.

---

## 0. Summary

The amplifier system spans three layers. The design **decouples** the Modbus register representation from the internal control/protection model, and replaces the 12-register legacy map with a **clean-sheet, versioned holding-register map** (21 registers, `MAP_VERSION = 0x01`). There is **no backward compatibility** with the 12-register map and **no reuse of HR9 as an identity register**.

1. **PIC18 amplifier** (`TrackAmplifier4.X`, PIC18F25K40) — owns *immediate* electrical protection (Phase 2), *applied* PWM output, *local* ramp/state/comm-watchdog. Reorganized internally into five modules (transport → adapter → model → core → hardware access). The business logic reads/writes an internal model, never `PetitHoldingRegisters[]` directly.
2. **PIC32 master** (`TrackController5`) — Modbus/bootloader master, cyclic slave polling, global-stop latch + host-authority heartbeat, firmware download. Changes are *additive*: extend the poll rotation to read the new registers and the identity register; the `SLAVE_INFO` frame grows in lockstep with the register count.
3. **C# host** (`SiebwaldeApp`) — parameter ownership, domain (main/mountain) knowledge, soft thresholds, recovery orchestration, Koploper authority, and the single HR0 movement gate.

Three distinctions are preserved exactly as agreed: **immediate electrical protection is local (PIC18)**; **soft thresholds + recovery orchestration live in Siebwalde (C#)**; **the global running prohibition stops main+mountain traction without collapsing their independent enable/config/recovery boundaries**. The **five output-evidence meanings** (`Requested` ≠ `Target` ≠ `Applied` ≠ `Peripheral` ≠ `Observed`) are made explicit and no command echo is ever treated as physical evidence.

The first PIC18 increment ("Control Core-lite") is a **complete minimum path**: firmware identity, compatible master/host access, valid parameter initialization, state transitions, output control, and useful status/error reporting — no essential parameter-ownership semantics deferred to a later phase while Phase 1 depends on them.

---

## A. Module architecture (the required decoupling)

### A.1 Five module boundaries

```
 ┌────────────────────────────────────────────────────────────────────────┐
 │ (a) transport/protocol        PetitModbus.c/.h, PetitModbusPort.c/.h,   │
 │     interrupt_manager.c       9-bit RX / CRC / FC03/06/16 / RX-TX FSM   │
 │                               (ISR context)                            │
 └───────────────┬────────────────────────────────────────────────────────┘
                 │ frame bytes / register read-write requests (main loop)
 ┌───────────────▼────────────────────────────────────────────────────────┐
 │ (b) application-protocol adapter   amp_adapter.c/.h   (NEW)             │
 │     decode → validate → internal operation; encode internal status →    │
 │     read-only registers. Owns the wire-image ←→ model boundary.         │
 │     (main-loop context; called from ProcessPetitModbus + per-tick)      │
 └───────────────┬────────────────────────────────────────────────────────┘
                 │ AmpCommand / AmpConfig / AmpStatus / AmpFault
 ┌───────────────▼────────────────────────────────────────────────────────┐
 │ (c) internal commands/config/status   amp_model.h   (NEW, data only)    │
 │     structs + pure validation helpers. No execution context.            │
 └───────────────┬────────────────────────────────────────────────────────┘
                 │ AmpCore_Update() reads model, writes model + HW calls
 ┌───────────────▼────────────────────────────────────────────────────────┐
 │ (d) control/protection state machines   amp_core.c/.h   (NEW)           │
 │     state eval + ramp + comm-watchdog + applied-readback + fault latch  │
 │     (main-loop, once per TMR4 tick, O(1), non-blocking)                 │
 └───────────────┬────────────────────────────────────────────────────────┘
                 │ AmpHw_SetDuty / AmpHw_SetBrake / AmpHw_ReadAdc
 ┌───────────────▼────────────────────────────────────────────────────────┐
 │ (e) hardware access   regulator.c (PWM), processio.c (ADC/CMP1), MCC    │
 │     (retained; call sites narrowed to thin accessors)                   │
 └─────────────────────────────────────────────────────────────────────────┘
```

**Ownership rule (hard invariant):** business behavior depends only on the internal model (c). `PetitHoldingRegisters[]` becomes a **pure protocol mirror** — written only by (b) from validated frames, read only by (b) when encoding responses. The control/protection core (d) never indexes `PetitHoldingRegisters[]`.

### A.2 Concrete data model (c) — `amp_model.h`

```c
typedef enum {
    AMP_ST_NOT_INITIALIZED = 0, AMP_ST_READY = 1, AMP_ST_RUN = 2,
    AMP_ST_STOPPING = 3, AMP_ST_FAULT = 4, AMP_ST_ESTOP = 5,
    AMP_ST_COMMS_LOST = 6, AMP_ST_LEASE_EXPIRED = 7
} AmpState;

typedef struct {                 /* pending == active on APPLY; validated as a set */
    uint8_t  accel_step;         /* PWM counts per 1.704 ms tick, 1..255           */
    uint8_t  decel_step;         /* PWM counts per tick, 1..255                    */
    uint8_t  fast_stop_step;     /* PWM counts per tick, 1..255                    */
    uint16_t watchdog_ms;        /* comm-watchdog timeout, 50..60000               */
    uint16_t tag;                /* config version/CRC echoed on APPLY             */
} AmpConfig;

typedef struct {
    bool     enable;             /* level: traction permission                     */
    uint16_t pwm;                /* requested setpoint; wire 10-bit, valid duty 1..799, neutral 399 */
    bool     dir;                /* direction bit (defined; Phase 1 scalar-PWM)    */
    bool     brake;              /* brake request bit                              */
    bool     stop_request;       /* controlled fast-stop ramp                      */
    bool     emo;                /* immediate brake-latch                         */
} AmpCommand;

typedef struct {
    AmpState state;
    bool     init_done, config_ok, id_set, enable_active, brake_active,
             dir_active, stopping_active;
    uint16_t requested_pwm;      /* last valid requested setpoint                  */
    uint16_t target_pwm;         /* ramped/corrected setpoint                      */
    uint16_t applied_pwm;        /* value last written to PWM3                     */
    bool     inhibited;          /* latched movement inhibit (safety stop)         */
    bool     lease_fresh;        /* movement-permission lease not expired          */
} AmpStatus;

typedef struct {                 /* fault-cause bitmask (see §C.6)                 */
    uint16_t causes;             /* bit0 OC, bit1 hard-thermal, bit2/3 vBus,       */
                                 /* bit4 config-invalid, bit5 comm-lost, ...       */
    uint8_t  validity;           /* 2-bit per signal (voltage/temp/current/occ)    */
} AmpFault;

typedef struct {
    AmpConfig pending, active;
    AmpCommand command;
    AmpStatus status;
    AmpFault  fault;
    uint32_t  comm_watchdog_ticks;      /* main-loop counter                       */
    uint32_t  lease_ticks;              /* ticks since last lease renewal          */
    uint16_t  ramp_current;             /* scalar current PWM                       */
} AmpModel;
```

### A.3 Adapter (b) — `amp_adapter.c/.h` (main-loop context)

```c
/* Replaces the untracked modbushooks.c seed; commits/recover it first (§G). */
void AmpAdapter_OnFrame(uint8_t addr, uint8_t fn);   /* kick watchdog on any addressed frame */
void AmpAdapter_OnWrite(uint8_t addr, uint16_t value, bool broadcast);
/* - addr out of range            -> Modbus exception 02 + write-result
 * - read-only register (addr>=HR_RW_END) -> exception 02 + write-result
 * - reserved bits set / value out of range -> reject, write-result
 * - command registers (HR_COMMAND/HR_CONTROL) -> AmpCommand (level/edge)
 * - config registers -> pending AmpConfig fields
 * - APPLY edge (HR_CONTROL bit13) -> AmpConfig_Validate + AmpConfig_Apply  */

void AmpAdapter_Refresh(void);
/* Called once per tick AFTER AmpCore_Update(): encodes AmpModel ->
 * read-only holding registers (HR_IDENTITY..HR_MSGS_TX). The identity
 * register is encoded once at init, not every tick. */
```

All adapter work is main-loop, bounded, and O(1): a write touches at most one register and (on APPLY) validates a fixed 5-field struct. No ISR-side adapter logic.

### A.4 Core (d) — `amp_core.c/.h` (main-loop, once per TMR4 tick)

```c
void AmpCore_Update(void) {   /* ~1.704 ms; O(1); never blocks              */
    AmpCore_EvalState(&model);            /* priority-ordered state test     */
    AmpCore_StepCommWatchdog(&model);     /* expiry -> comm-lost -> STOPPING */
    AmpCore_StepRamp(&model);             /* one step toward target per tick */
    AmpCore_CommitOutput(&model);         /* AmpHw_SetDuty/SetBrake + applied*/
}
```

Execution budget: a state test + one `uint16_t` ramp step + one `PWM3_LoadDutyValue` + a few register writes = a handful of µs per 1.704 ms tick, far inside budget. The heavy BEMF/ADC work stays in the existing sequencer and is unchanged.

### A.5 Hardware access (e) — thin accessors (retained logic, narrowed call site)

```c
void AmpHw_SetDuty(uint16_t duty)   { PWM3_LoadDutyValue(duty); }   /* wrapper */
void AmpHw_SetBrake(bool on)        { LM_BRAKE_LAT = on; }
void AmpHw_Enable(bool on)          { PWM3EN = on; }
```

The existing `REGULATORxUPDATE()` direct `HR_PWM_COMMAND → PWM3` body is **replaced** by `AmpCore_CommitOutput()`; the PWM init (`REGULATORxINIT` neutral 399 + brake-at-boot) is **retained**. `processio.c` (`MEASURExBMF`, `ADCxIO`) is **retained** unchanged (Phase 1).

### A.6 Shared ISR/main-loop data (explicit, no new hazards)

| Variable | Written by | Read by | Notes |
|---|---|---|---|
| `PetitReceiveBuffer` / `PetitReceiveCounter` | RX ISR | `Petit_RxRTU` (main) | existing; unchanged |
| `PetitModbusTimerValue` | ISR (TMR3) | `Petit_CheckRxTimeout` (main) | existing; unchanged |
| `Update_AmplifierTicks` | TMR4 ISR | main loop | existing `volatile uint8_t`; single-byte access is atomic on PIC18; do **not** widen to `uint32_t` (the untracked `modbushooks.c` declares it `uint32_t` — that mismatch is a defect to be resolved, see §G) |
| `last_modbus_activity_tick` | main (adapter) | main (core) | watchdog kick; must be main-loop only, not ISR |

No new ISR-side logic is added; no critical-section change is required.

---

## B. Cooperative execution and timing (RETAINED / MODIFIED ledger)

### B.1 What is RETAINED (working timing/protocol — do not casually alter)

| Mechanism | Evidence | Disposition |
|---|---|---|
| Modbus RTU framing, HW CRC (`CRC_HW`), FC03/FC06/FC16, FC04/FC08 disabled | `PetitModbus.c/.h` | **RETAINED** |
| UART 9-bit RX address-detect (`ADDEN`), 8-bit TX, RS485 `TX_ENA` | `PetitModbusPort.c` | **RETAINED** |
| Single ISR: UART RX → TMR3 (inter-char) → TMR4 (1.704 ms tick); `IPEN=0` (no priority levels) | `interrupt_manager.c` | **RETAINED** |
| TMR1 (100 µs BEMF), TMR2 (20 kHz PWM3 timebase), TMR6 (LED) | MCC drivers | **RETAINED** |
| Cooperative main loop (`ProcessPetitModbus → Led_Blink → sequencer`) | `main.c` | **RETAINED** (one hook added) |
| ID-pin → runtime-address startup (states 0/1), ID-set latch | `main.c:60-100` | **RETAINED** |
| `HR_SW_CHECKSUM == 0` bootloader-entry handshake; `ReadFlashChecksum()` | `main.c:37,96` | **RETAINED** |
| Bootloader firmware/config download via PIC32, checksum compare on HR11, version read `CMD_READ_VERSION` | `slavestartup.c`, `slavefwhandler.c`, `PublicEnums.cs` | **RETAINED** |
| Backplane slot-select = backplane slave (51–55) HR0 write; amplifier detect = HR11 read at `0xAA`; address assign = HR2 write at `0xAA` | `slavestartup.c` | **RETAINED** |
| Broadcast enable = HR1 `0x8000` at the ENABLE step | `slavestartup.c:584-629` | **RETAINED** (semantics widened to re-assertable level, below) |
| C# single movement gate + global stop + observed-neutral gate | PR #11/#40/#41 | **RETAINED** |

### B.2 What is MODIFIED (bounded, coordinated)

| Mechanism | Change | Why |
|---|---|---|
| `NUMBER_OF_HOLDING_PETITREGISTERS` 12 → **21** | bump in **both** PIC18 and PIC32 `PetitModbus.h` | clean-sheet map |
| FC06/FC16 write handlers | route through `AmpAdapter_OnWrite` (and kick the watchdog on any addressed frame via `AmpAdapter_OnFrame`) instead of direct `ActValue=` | per-register validation + decoupling + correct watchdog kick |
| `SLAVE_INFO` frame | grows automatically via `HoldingReg[NUMBER_OF_HOLDING_PETITREGISTERS]` + C# `UInt16[12]` → `[21]`, `DeterministicTrackTransport.HoldingRegisterCount` | frame is register-count-bound |
| PIC32 poll rotation | add MESSAGE cases to read HR0–HR20 (incl. `HR_IDENTITY`) | read the new map |
| `REGULATORxUPDATE` body | replaced by `AmpCore_Update` via hardware accessors | ramp + applied readback + watchdog |
| Enable (HR1 bit15) | one-way latch → re-assertable level | state machine requires `ENABLE ∧ …`; clearing enable removes traction (ramp to neutral) |

### B.3 What is REPLACED (deliberate, clean-sheet)

| Mechanism | New meaning | Why |
|---|---|---|
| Legacy 12-register map (HR3/9/10 unused; HR2 overloaded with address+BEMF+occupancy/thermal) | 21-register versioned map (§C) | clean-sheet interface |
| HR9 `HR_CONFIG_ID_PWM` | reassigned to `HR_FAULT` (fault cause); **not** identity | PO decision: HR9 already has named address/PWM-mode/reset responsibilities; identity goes to a fresh register |
| No per-register validation | full write validation + write-result | `[Agreed]` parameter validation |
| No ramp / no watchdog / no applied readback | ramp engine + comm watchdog + `HR_APPLIED_PWM` | `[Agreed]` stop/ramp/evidence |

### B.4 Non-blocking guarantee

- Ramps: **one** scalar PWM step per 1.704 ms tick (`AmpCore_StepRamp`), O(1).
- Retries: **none in the PIC18**. Polling/retry/backoff live in the PIC32 and C#; the slave only responds. Config download is a short FC06/FC16 sequence (small fixed set), and APPLY validation is a fixed 5-field check.
- Config processing: APPLY is O(1) validation + copy; never blocks the loop.

---

## C. Interface contract (clean-sheet, versioned — all PROPOSED)

### C.1 Version detection, fail-closed, deployment, rollback

- **Anchor:** `HR_IDENTITY` (`HR7`), read-only, present in every map version, readable by FC03 (the master already polls holding registers). The master/C# reads `HR_IDENTITY` **before** any config download or enable.
- **Detection:** `HR_IDENTITY` bits 0–7 = `MAP_VERSION` (Phase 1 = `0x01`). Bits 8–11 = `DEVICE_CLASS` (1 = track amplifier). Bits 12–15 = capability flags (Phase 1 non-zero). **Legacy disambiguation is mandatory:** legacy HR7 is the `HR_MESSAGES_RECEIVED` counter (verified `General.h:44`, incremented on every addressed frame per `PetitModbus.c:757-758`), which can transiently mimic identity bits. Require (i) `DEVICE_CLASS == 1` **and** a non-zero capability bitmap, and (ii) `HR_IDENTITY` stable across two consecutive reads. Check (i) alone does NOT disambiguate — `DEVICE_CLASS` occupies bits 8–11 (not 12–15), so a legacy counter value of `0x1101` (= 4353) decodes as version `0x01` + class `1` + non-zero capability and would pass check (i). The decisive fail-closed defense is check (ii): a legacy counter increments on every addressed read and can never be stable across two consecutive reads, while a fixed identity is. **Check (ii) is valid only when the two observations are two distinct fresh FC03 reads of HR7 performed at the PIC32** (each updating `HoldingReg[7]` + `MbCommError=SLAVE_DATA_OK` via `HandlePetitModbusReadHoldingRegistersSlaveReadback`), **not** two cached `SLAVEINFO` uploads (`mbus.c` `UPLOAD_STATE_ALL` re-sends the cached `SlaveInfo` struct on the timer callback, independent of Modbus success — see R5‑B). C# must consume the PIC32's two-fresh-read result and must not derive identity from the SLAVEINFO stream.
- **Fail-closed rejection:** if `HR_IDENTITY` reads back `0x0000`, a non-`0x01` version, or an unexpected device class, the master/C# **blocks only that amplifier**: no config download, no enable, no traction; it surfaces "incompatible firmware" per amplifier. A missing capability bit blocks only the dependent feature (e.g., no applied-readback ⇒ do not rely on `HR_APPLIED_PWM` for the neutral gate).
- **Mixed-version handling:** because register *meanings* differ, mixed firmware **cannot safely interoperate**. The identity check makes mixed versions fail-closed per amplifier. This is explicit, not hidden.
- **Coordinated deployment:** flash new firmware to all amplifiers first (existing bootloader path), then update PIC32 + C#; verify `HR_IDENTITY == 0x01` on every amplifier before enable.
- **Rollback:** reflash prior firmware + revert PIC32/C#; the `MAP_VERSION` check re-detects the downgrade and re-handles it (fail-closed on the old map until the matching host code is restored). The bootloader checksum (`HR_SW_CHECKSUM`) and version read remain the downgrade-verification mechanism.

### C.2 Register map (21 holding registers, `HR0`–`HR20`) — PROPOSED

All values/allocations are **PROPOSED**, not approved. `R/W` = writable (validated); `R` = read-only (write ⇒ Modbus exception 02 + write-result).

| HR | Name | Acc | Content (bits) |
|---|---|---|---|
| 0 | `HR_COMMAND` | R/W | 0–9 PWM setpoint; 10 DIR; 11 BRAKE; 12 STOP_REQUEST; 15 EMO. Readback = last-received command echo, **not** applied. **Anchor** (write path). |
| 1 | `HR_CONTROL` | R/W | 15 ENABLE (set-only); 10 DISABLE (set-only); 14 MOTION_PERMISSION (set-only lease renewal); 13 APPLY_CONFIG (edge); 12 CLEAR_FAULT (edge); 11 RESET_COUNTERS (edge); 9..0 reserved=0. **Anchor** (broadcast enable + lease renewal). All bits *ones-act, zeros-ignored* (see R7‑1). |
| 2 | `HR_ADDRESS` | R/W | 0–5 runtime address; 13 ID_SET (firmware-latched, R). **Anchor** (address assignment). |
| 3 | `HR_CFG_RAMP` | R/W | 0–7 `ACCEL_STEP`; 8–15 `DECEL_STEP`. Pending until APPLY. |
| 4 | `HR_CFG_FAST_STOP` | R/W | 0–7 `FAST_STOP_STEP`; 8–15 reserved=0. Pending until APPLY. |
| 5 | `HR_CFG_WATCHDOG` | R/W | 0–15 `WATCHDOG_MS` (50–60000). Pending until APPLY. |
| 6 | `HR_CFG_TAG` | R/W | 0–15 config version/CRC, echoed on APPLY. Pending until APPLY. |
| 7 | `HR_IDENTITY` | R | 0–7 `MAP_VERSION`=0x01; 8–11 `DEVICE_CLASS`=1; 12–15 capability (12 applied-readback, 13 ramp, 14 comm-watchdog, 15 two-phase-config). |
| 8 | `HR_STATE` | R | 0–3 state enum (7 = LEASE_EXPIRED); 8 ENABLE_ACTIVE; 9 INIT_DONE; 10 CONFIG_OK; 11 STOPPING_ACTIVE; 12 BRAKE_ACTIVE; 13 DIR_ACTIVE; 14 ID_SET; 15 INHIBITED (latched movement inhibit). |
| 9 | `HR_FAULT` | R | 0 overcurrent; 1 hard-thermal; 2 vBus-high; 3 vBus-low; 4 config-invalid; 5 comm-lost; 6 internal-invariant; 7 lease-expired (movement-permission timeout). |
| 10 | `HR_VALIDITY` | R | 0–1 voltage; 2–3 temperature; 4–5 current; 6–7 occupancy (00 valid / 01 expected-unavailable / 10 invalid / 11 reserved); 8 occupancy-live; 9 thermal-live; 10 overcurrent-live. |
| 11 | `HR_SW_CHECKSUM` | R | firmware checksum; `0` = bootloader handshake. **Anchor** (bootloader). Must not be repurposed. |
| 12 | `HR_REQUESTED_PWM` | R | received/requested setpoint (last valid command; wire 10-bit, valid duty 1..799, neutral 399). |
| 13 | `HR_TARGET_PWM` | R | internally ramped/corrected setpoint. |
| 14 | `HR_APPLIED_PWM` | R | value last written to the PWM3 duty register (valid duty 1..799; never 0, never 800..1023) + bit10 BRAKE_ACTIVE, bit11 DIR_ACTIVE. |
| 15 | `HR_WRITE_RESULT` | R | 0–7 last-written address; 8–10 result (0 ok / 1 range / 2 read-only / 3 relationship / 4 saturated); 15 retained (active config unchanged). |
| 16 | `HR_MEAS_VOLTAGE` | R | H-bridge/fuse voltage (raw ADC). |
| 17 | `HR_MEAS_TEMPERATURE` | R | H-bridge temperature (raw ADC). |
| 18 | `HR_MEAS_CURRENT` | R | H-bridge current (raw ADC). |
| 19 | `HR_MSGS_RX` | R | messages-received counter. |
| 20 | `HR_MSGS_TX` | R | messages-sent counter. |

**Capacity/extensibility:** 21 registers; ~11 reserved bit-fields across the map plus contiguous growth space beyond HR20 (buffer sizes are register-count-derived, so growth is a count bump + poll-rotation add). The Input/Diagnostic (FC04/FC08) split is a **later cleanup**, not Phase 1 (§K.5). Reserved bits must be written 0 and are ignored on read.

### C.3 Why these anchors, and what moved

- **HR0/HR1/HR2/HR11 retained at their addresses** because they are tied to *working protocol/startup/bootloader timing* (write path keyed on HR0; broadcast enable on HR1; address assign on HR2; bootloader checksum on HR11), not for backward compatibility. Everything else is redesignable and is redesigned.
- **HR9 is NOT identity.** Its prior "Amplifier ID / PWM mode / reset" responsibilities are disposed of: address assignment stays at `HR_ADDRESS` (HR2); PWM mode and reset had **no active application use** (verified) and are not carried forward. Identity/version lives at **HR7** (fresh address).

### C.4 Message-level contract (C# ↔ PIC32, unchanged transport)

`EXEC_MBUS_SLAVE_DATA_EXCH` (108) already carries 1–2 consecutive register writes (little-endian, `0xAA` write / `0x55` read). Config download = a short FC06/FC16 sequence over `HR_CFG_*` then `APPLY`. The `SLAVEINFO` (`0xFF`) frame grows from 12 to 21 `HoldingReg` entries; C# parser + `SLAVE_INFO` struct + `TrackAmplifier.HoldingReg` + `DeterministicTrackTransport` change in lockstep. **Readback-freshness caveat preserved:** `TrackAmplifierDataFreshness` measures PIC32 SLAVEINFO receipt, not a fresh PIC18 transaction; it is acceptable for identity/status/applied-PWM observation but is not real-time.

### C.5 Init / enable / standby sequence (v2)

1. Power-up → `NOT_INITIALIZED`; red LED awaiting addressing (retained legacy states 0/1).
2. Address assigned → ID-set, red off (retained).
3. Master/C# reads `HR_IDENTITY`; on `0x01` proceeds, else blocks this amplifier.
4. C# downloads config set (`HR_CFG_*` + tag) and writes `APPLY` (HR_CONTROL bit13).
5. Firmware validates the whole set; success ⇒ `pending→active`, `INIT_DONE`+`CONFIG_OK`; failure ⇒ write-result + previous active config retained.
6. Standby → orange flashing, monitoring active, bridge floating.
7. C# writes `ENABLE` (HR_CONTROL bit15) after system readiness + observed neutral.
8. `ENABLE ∧ INIT_DONE ∧ CONFIG_OK ∧ ¬FAULT ∧ ¬COMMS_LOST` ⇒ `RUN` on a valid command.

---

## D. State / ownership / timing model

### D.1 PIC18 runtime state (full target; Phase 1 subset bolded)

```
NOT_INITIALIZED → READY ⇄ RUN → STOPPING
      ↑             ↑       ↓ (stop/comm-loss/disable)
      └─ reset / invalid config
      FAULT ← (fault / estop / comm-loss-latched)
```

Priority (highest first, inherited from the deferred state machine, hard invariant): **FAULT > ESTOP > COMMS_LOST > LEASE_EXPIRED > NOT_INITIALIZED > STOPPING > RUN > READY**.

- `NOT_INITIALIZED` — after reset; no valid config; neutral 399; bridge safe.
- `READY` — `INIT_DONE ∧ CONFIG_OK`; monitoring active; neutral; awaiting valid command + enable.
- `RUN` — enabled, following a valid command; ramp drives `ramp_current → target_pwm`.
- `STOPPING` — stop request / zero target / disable / comm-loss; ramping to neutral; normal commands cannot interrupt.
- `LEASE_EXPIRED` — movement-permission lease timed out (no `MOTION_PERMISSION` renewal within `MOTION_LEASE_TIMEOUT_MS`); latched; fast ramp to neutral; `INHIBITED` set; exit only via fresh lease renewal + fresh valid command (no auto-restore of old PWM).
- `FAULT` — a latched fault (Phase 1: config-invalid or comm-lost; Phase 2 adds electrical faults); output floating/braked; exit only via clear/re-init.
- `ESTOP` — EMO asserted; immediate brake-latch (retained); exit only when EMO cleared and stop reached.

**Phase 1 implements:** `NOT_INITIALIZED`, `READY`, `RUN`, `STOPPING`, `FAULT` (config-invalid + comm-lost causes), plus EMO as the retained brake-latch. The electrical fault causes (overcurrent/thermal/vBus) arrive in Phase 2.

### D.2 Ownership matrix (who knows what)

| Concern | PIC18 | PIC32 | C# |
|---|---|---|---|
| Electrical protection (hard) | **Owner** (Phase 2) | none | none (observes) |
| Soft thresholds / recovery budget | none | none | **Owner** |
| Ramp / comm-watchdog / applied readback | **Owner** | transparent | consumes |
| Config validation (values + relationships) | **Owner** | transparent carrier | supplies values |
| **Parameter set per position** | holds active copy | transparent | **Owner** (per physical position) |
| **Domain (main/mountain/spare/unassigned)** | **none** | **none** | **Owner** |
| Global stop latch / host heartbeat | executes | **Owner** | originates/releases |

### D.3 Domain separation (main vs mountain) — explicit

- The PIC18 and PIC32 have **no domain concept**. A slave does not need to know "am I main or mountain"; it executes per-position config, enable, commands, and stop commands identically. No per-slave knowledge of the whole layout is added.
- **All domain knowledge lives in C#**: the position→domain mapping (main / mountain / spare / unassigned), the per-domain enable/config/recovery state, and the per-domain manual ownership.
- The **global running prohibition** is expressed as a *command* (STOP_REQUEST / EMO / fast-stop) delivered to every configured amplifier in both domains. It stops main+mountain traction but does **not** merge their enable/config/recovery boundaries: each amplifier retains its own config set, enable, fault latch, and recovery; releasing the global stop does not clear a local fault (§`[Agreed]`).
- Permitted switch/FY recovery (J/K) is **not** disabled by the global prohibition; it lives in C# interlocks, not in the amplifier.

### D.4 Timing model (shared)

- Scheduler tick = TMR4, 1.704 ms (`T4CLKCON=0x01` FOSC/4 = 16 MHz; `T4CKPS` 1:128; `PR4=0xD4`=212 ⇒ 213/125 kHz = 1.704 ms). `AmpCore_Update()` runs once per tick, before the existing measurement sequencer, so protection/watchdog are never gated behind a stalled ADC/BEMF step.
- Comm watchdog: kicked on **any** addressed frame (read **or** write) via the adapter hook — the PIC32 idle poll is FC03 reads, so a write-only kick would false-trip an idle amplifier; expiry ⇒ `COMMS_LOST` ⇒ `STOPPING` (fast ramp). Evaluated at loop top, independent of the master/C# surviving.
- Movement-permission lease: renewed by any `MOTION_PERMISSION` write (HR1 bit 14), **never** by FC03 reads; expiry ⇒ `LEASE_EXPIRED` ⇒ local fast ramp to neutral (`INHIBITED`). Evaluated at loop top like the comm-watchdog; the lease is the PIC18-local backstop for "master polling but permission revoked", which the comm-watchdog (any-addressed-frame) cannot detect.
- Reset ⇒ safe by construction: `NOT_INITIALIZED`, neutral 399, brake-at-boot retained.

---

## E. Neutral / output evidence model (the five meanings)

The following are **distinct** and none implies the next physically:

| Boundary | Register / source | Meaning | Evidence level |
|---|---|---|---|
| 1. Received/requested setpoint | `HR_REQUESTED_PWM` (HR12) | last valid command setpoint | protocol |
| 2. Internally ramped/corrected setpoint | `HR_TARGET_PWM` (HR13) | ramp engine's current target (accel/decel/fast-stop) | firmware |
| 3. Applied value (written to PWM peripheral) | `HR_APPLIED_PWM` (HR14) | the duty **last written to PWM3** (`PWM3_LoadDutyValue`), with brake/dir flags | firmware truth |
| 4. Peripheral readback (if any) | PWM3 duty register readback | what the PWM peripheral currently holds; double-buffered on PIC18 — **verify a meaningful readback exists** before exposing | hardware-register |
| 5. Independently measured physical output | scope/voltmeter on RC6 | actual electrical output | **V4 only** |

**Explicit statements:**

- `HR_APPLIED_PWM` is defined precisely as "the value the firmware last wrote to the PWM3 duty register", **not** a command echo, **not** a physical measurement, and **not** the peripheral's own register readback (which is boundary 4, and only if the hardware supports a meaningful read).
- Reading back `HR_COMMAND` is a **command echo** and is **never** treated as physical evidence. The previous "protocol-observed HR0 == 399" bar is insufficient for physical restart safety (§9 of `recovery-maintenance-system.md`); the applied-PWM register closes the "applied" gap at the protocol level, but the physical "observed" gap remains V4.
- Boundary 4 is optional and marked `[To verify]`; if it is not reliable, `HR_APPLIED_PWM` (boundary 3) is the strongest firmware-side evidence available.

---

## F. Required values (Phase 1) — PROPOSED unless PO-selected

### F.1 Hard electrical/thermal limits (a) — PO-selected, NOT invented

Named unresolved; **Phase 2 gate**. Do not proceed with electrical protection until selected:
`OVERCURRENT_HARD_THRESHOLD`, `THERMAL_HARD_TRIP_LEVEL`, `VBUS_HARD_HIGH`, `VBUS_HARD_LOW`. These are firmware constants (permanent; new firmware to change). **Phase 1 does not implement hard electrical protection** (see §H "absent").

### F.2 Watchdog timing (b) — config parameter, firmware-defaulted, PROPOSED default

- `COMM_WATCHDOG_TIMEOUT_MS_DEFAULT = 250` ms (range 50–60000). *Rationale:* the PIC32 poll cycle is tens of ms with inter-message gaps and init pauses; 250 ms tolerates several missed polls without a brief stall going unnoticed, and is re-tunable per position via `HR_CFG_WATCHDOG`. It is a normal config value, **not** a hard electrical limit.

### F.3 Ramp parameters (c) — config parameters, PROPOSED defaults

Units = PWM counts per 1.704 ms scheduler tick. Valid duty is 1..799, neutral 399. Full travel is asymmetric: −399 counts reverse nominal (399→0; applied reverse extreme is 1 because duty 0 is clamped to 1) and +400 counts forward (399→799). The wire command field is 10-bit (0..0x3FF) but 800..1023 are rejected as invalid peripheral duty.

| Name | Default | Rationale |
|---|---|---|
| `ACCEL_STEP_DEFAULT` | 2 | ≈ 400/2 = 200 ticks ≈ 341 ms forward full-swing (399→799); reverse ≈ 398/2 ≈ 339 ms (399→1) |
| `DECEL_STEP_DEFAULT` | 2 | ≈ 340 ms normal stop; matches accel feel |
| `FAST_STOP_STEP_DEFAULT` | 8 | ≈ 400/8 = 50 ticks ≈ 85 ms forward fast-stop (399→799); reverse ≈ 85 ms (399→1) |

Neutral = **399** (verified baseline `AmplifierSpeedMapper.NeutralPwm`, `REGULATORxINIT` duty 399). Direction-through-neutral is automatic with the scalar-PWM model (399 midpoint). All ramp defaults are **PROPOSED** and tunable per position; the PO must confirm or adjust.

### F.4 Software warning/recovery policy (d) — C#, Phase 3, deferred

`COOLDOWN_*`, recovery attempt/time budgets, `STABLE_SUCCESS_INTERVAL`, soft vBus/temp thresholds — all C#-owned, Phase 3. Not Phase 1 values; none are invented here.

---

## G. Configuration transaction (minimal two-phase) — PROPOSED

Adopted because it satisfies the `[Agreed]` "no partial config drives output" requirement. Minimal implementation, no generic framework:

- **Staging:** `HR_CFG_*` writes go to `AmpConfig pending` only; the control loop reads `active` exclusively.
- **APPLY:** `HR_CONTROL` bit13 (edge) triggers `AmpConfig_Validate(&pending)` then copy to `active` on success.
- **Single-param change during operation:** writing one `HR_CFG_*` then APPLY validates the **whole** resulting set and activates it. Works without re-downloading the others.
- **Complete-set validation:** APPLY validates all five fields together (each in range; reserved bits zero; relationships consistent).
- **Rejection preserves active config:** on failure, `active` is untouched; `HR_WRITE_RESULT` = rejected + address + reason + retained flag.
- **Saturation explicit/readable:** a permitted clamp (e.g., fast-stop step capped) reports result code 4 (`saturated`) — never silent.
- **Partial transfer cannot activate:** only APPLY activates; partial pending writes are never used by the core.
- **Results identify success/failure + affected params:** `HR_WRITE_RESULT` carries last-written address + result code + retained flag. A rejected APPLY does **not** set `HR_FAULT` bit4 while the previously-active config remains valid (a rejected runtime update is a write-result condition, not a drive-blocking fault); `HR_FAULT` bit4 is set only when the **active** config is invalid or absent (initial config failure), which blocks traction.
- **Initialization blocked until valid config:** `INIT_DONE`/`CONFIG_OK` set only after a successful APPLY; no traction before both.

---

## H. Protections: preserved / new / absent (Phase 1)

**Preserved from baseline (RETAINED):** EMO brake-latch (HR0 bit15 → `LM_BRAKE`); neutral-at-boot (399) + brake-at-boot; ID-pin/address-assignment startup; bootloader checksum handshake; 9-bit UART framing + HW CRC; cooperative main loop + single ISR; C# single movement gate + global stop (PR #40) + observed-neutral gate (PR #11).

**New in Phase 1:** per-register write validation + write-result; two-phase config (no partial config drives output); `INIT_DONE`/`CONFIG_OK` gating (no traction before valid config); comm watchdog (local fast-stop on comm loss); movement-permission lease (local fast-stop on lease expiry, independent of FC03 polling) + latched `INHIBITED` stop status + `lease-expired` fault cause; ramp engine (accel/decel/fast-stop, direction-through-neutral); applied-PWM readback; identity/capability + version detection + fail-closed; status/fault/validity reporting.

**Absent in Phase 1 (explicit):** hard electrical overcurrent/thermal/vBus trips (Phase 2); soft thresholds + recovery orchestration (Phase 3); per-signal analog qualification/debounce + hysteresis (Phase 2/3); test overrides (Phase 4); BEMF control (later); physical "observed" output (V4).

**Safe handling of unsupported/partially-deployed versions:** fail-closed identity check; mixed versions block only the affected amplifier; coordinated flash-then-host deployment; rollback via reflash + version re-detection.

**Verifiable offline vs hardware-only:**

| Offline (V1/V2/V3) | Hardware-only (V4) |
|---|---|
| Build under XC8 (F1 harness) | PWM duty physically applied (~50% at neutral) |
| Host-shim pure-logic tests (state transition, ramp step, config validation, watchdog) | motor stopped / brake line asserted |
| C# simulator protocol tests (map, identity, download/apply/write-result, applied readback) | hard fault trips + timing |
| V3 integration (download/readback path) | — |

---

## Phasing

Each increment is independently reviewable and, where possible, software-proven.

- **Phase 0 — Interface & state-model agreement (this document).** Deliverable: §C map, §A modules, §D state, §F values. **Gate:** PO approval of the clean-sheet map + values.
- **Phase 1 — PIC18 "Control Core-lite" + thin PIC32/C# support** (contract below). Complete minimum path.
- **Phase 2 — PIC18 local hard electrical protection** (overcurrent/thermal/vBus hard trips, PO-selected thresholds; validity flags; fault latches). Depends on Phase 1 fault/validity registers + hardware-source characterization.
- **Phase 3 — C# soft thresholds + recovery orchestration.**
- **Phase 4 — C# domain/parameter ownership + test overrides.**
- **Phase 5 — status/diagnostic integration.**
- **Later:** BEMF mountain control (G), mountain lighting (G), FY (J), yard/transfer (K), main/mountain manual operation (I), coordinated shutdown (L), control-source arbiter/hand-back.

**Explicitly unchanged limitations until resolved:** PR #40 global stop is abrupt neutral 399 (no fast ramp) until Phase 1 lands; no proven maximum wall-clock host-loss stopping time; no slave receipt/ack; no per-amplifier recovery/arbiter. These remain visible, not silently "fixed".

---

## First-increment proposal (Phase 1 — PIC18 "Control Core-lite")

### Objective
Give the PIC18 the minimal software-testable control/safety foundation the `[Agreed]` requirements demand, **as a complete minimum path**: firmware identity, compatible master/host access, valid parameter initialization, state transitions, output control, useful status/error reporting.

### Scope (PIC18, `TrackAmplifier4.X`)
1. Recover/commit `modbushooks.c/.h`; resolve the `Update_AmplifierTicks` `uint8_t` vs `uint32_t` mismatch; establish a clean build (F1 harness gate).
2. New 21-register map + `HR_IDENTITY` (version/capability).
3. Adapter (`amp_adapter.c/.h`): per-register write validation + write-result + wire↔model boundary.
4. Model (`amp_model.h`): internal structs (config/command/status/fault).
5. Core (`amp_core.c/.h`): state eval + ramp + comm watchdog + applied readback; replaces `REGULATORxUPDATE` body.
6. Two-phase config with APPLY; `INIT_DONE`/`CONFIG_OK` gating.
7. Hardware accessors (thin wrappers) over retained `regulator.c`/MCC.

### Scope (PIC32, `TrackController5`) — thin, additive
- Bump `NUMBER_OF_HOLDING_PETITREGISTERS` 12→21; extend poll rotation to read HR0–HR20; read `HR_IDENTITY` during init; grow `SLAVE_INFO` frame (automatic). No new commands, no global-stop change.

### Scope (C#, `SiebwaldeApp`)
- Extend `TrackAmplifierRegisters`, `TrackAmplifier.HoldingReg`, SLAVEINFO parser, `DeterministicTrackTransport` to 21 registers; add `HR_IDENTITY` compatibility/version check (fail-closed); implement init-time config download + readback verification; consume status/fault/validity/applied/write-result.

### Exclusions
Local hard electrical protection (Phase 2); soft thresholds/recovery (Phase 3); test overrides/domain store (Phase 4); BEMF; trace/storage; UI; FY/yard; DIR-bit-based direction (scalar-PWM direction retained).

### Acceptance criteria (draft)
- AC1: firmware builds reproducibly via the F1 harness (0 errors), `Offset` config.
- AC2: invalid/read-only/reserved write rejected; previous active config retained; `HR_WRITE_RESULT` reports address + reason; saturation explicit.
- AC3: no traction before `INIT_DONE ∧ CONFIG_OK ∧ ENABLE`; reset ⇒ neutral/`NOT_INITIALIZED`.
- AC4: `HR_APPLIED_PWM` tracks the ramped PWM3 duty, distinct from the `HR_COMMAND` echo and from `HR_TARGET_PWM`.
- AC5: comm-loss ⇒ local fast ramp to neutral within the watchdog timeout; no host involved.
- AC6: C# detects `MAP_VERSION` via `HR_IDENTITY`, rejects incompatible amplifiers (fail-closed), and completes config download + readback verification.
- AC7: existing C# baseline stays green; no regression to global stop / observed-neutral gate / KIS carrier.
- AC8: movement-permission lease expiry ⇒ local fast ramp to neutral within `MOTION_LEASE_TIMEOUT_MS` + tick + ramp (≈ 588 ms), even while FC03 polling continues; `INHIBITED` latched; no auto-restore of old PWM; release requires fresh lease renewal + fresh valid command.
- AC9: HR1 per-bit decode (R7‑1) — broadcast renewal `0x4000` neither clears ENABLE nor triggers APPLY/CLEAR_FAULT/RESET_COUNTERS; reserved-bit writes rejected; ENABLE+DISABLE together rejected.

### Build / test / review plan
- Build: F1 harness (pinned XC8 2.31 / XC32 2.50); C# Debug/Release suites.
- Test: host-shim C tests (ramp/validation/state/watchdog); `DeterministicTrackTransport` extended to the new map; V3 integration for the download/readback path.
- Review: R2 (cross-component protocol/interface + lifecycle + safety-relevant firmware); validation V1/V2 now, V4 deferred. Segregation: Developer implements; Architect consulted; Integrator independently verifies.

---

## Remaining Product Owner decisions (recommendation + consequence)

1. **Approve the clean-sheet register map + identity/version mechanism (§C).** *Recommendation:* approve the 21-register holding map with `HR_IDENTITY` at HR7 (`MAP_VERSION 0x01`), version detection + fail-closed + coordinated deployment + rollback. *Consequence:* this is the interface freeze that unblocks Phase 1 across all three layers; without it, no implementation can start.
2. **Confirm the numeric defaults (§F.2/F.3).** *Recommendation:* `WATCHDOG_MS=250`, `ACCEL_STEP=2`, `DECEL_STEP=2`, `FAST_STOP_STEP=8` (all tunable per position). *Consequence:* these are non-electrical tuning values; Phase 1 can proceed with them as placeholders, but a PO confirmation avoids a re-tune later.
3. **Hard electrical limit values (§F.1).** *Recommendation:* select `OVERCURRENT_HARD_THRESHOLD`, `THERMAL_HARD_TRIP_LEVEL`, `VBUS_HARD_HIGH/LOW` before Phase 2. *Consequence:* Phase 1 does not implement electrical protection (explicitly absent); Phase 2 is blocked until selected.
4. **Domain config for Phase 1.** *Recommendation:* treat all configured legitimate track amplifiers (1..50) as one restart-safety domain for Phase 1; defer `MainRailway/MountainRailway/Spare/Unassigned` to Phase 4. *Consequence:* until decided, the observed-neutral gate is fail-closed (movement blocked) with the empty default.
5. **Input/Diagnostic (FC04/FC08) split timing.** *Recommendation:* defer to a later cleanup; keep read-only telemetry as validated read-only holding registers in Phase 1. *Consequence:* adding FC04/FC08 would add a second poll function + frame growth now, with no `[Agreed]` requirement driving it — deferring keeps Phase 1 minimal.

---

**Evidence base (verified at `master @ 11ba5e3`):** `TrackAmplifier4.X/main.c`, `regulator.c`, `modbus/General.h`, `modbus/PetitModbus.c/.h`, `modbus/PetitModbusPort.c`, `mcc_generated_files/interrupt_manager.c`, `tmr3.c`, `tmr4.c`, `processio.c`; `TrackController5/firmware/src/slavestartup.c`, `slavehandler.c`, `enums.h`, `modbus/PetitModbus.h`; `SiebwaldeApp/.../TrackAmplifierRegisters.cs`, `NewSenActCmdMes.cs`, `PublicEnums.cs`, `DeterministicTrackTransport.cs`; `docs/recovery-maintenance-system.md`, `TrackAmplifier4.X/MODBUS_TRACK_AMPLIFIER_MAPPING.md`, `TRACK_AMPLIFIER_STATE_MACHINE.md`.

---

## Revision 3 — Design Closure Addendum (2026-10-15)

> All values/allocations below remain **PROPOSED** unless marked *verified* or *PO-reserved*. *Verified* = read from committed source at `master @ 11ba5e3`. This addendum resolves the Product Owner dispositions F1–F5 and the four targeted architecture checks; it does not restart the design.

### R3‑1. F2 — timing-default substantiation (PROPOSED, not operational defaults)

Verified tick basis: PIC18 `TMR4` = **1.704 ms** (`mcc.c` `OSCFRQ=0x08`=64 MHz → `T4CLKCON=0x01`=FOSC/4=16 MHz; `T4CKPS`=1:128; `T4PR=0xD4`=212 ⇒ 213×128/16 MHz). Full PWM travel is asymmetric about neutral `399`: +400 forward (399→799), −399 reverse nominal (399→0; applied reverse extreme is 1, duty 0 clamped) (*verified* `AmplifierSpeedMapper`: `MinPwm=1, MaxPwm=799, NeutralPwm=399`). Wire command field is 10-bit (0..0x3FF) but valid peripheral duty is 1..799; 800..1023 rejected.

| Default | Scheduling/tick basis | Comm-cycle / latency basis | Fault-handling interaction | Expected output behaviour | Safe placeholder? |
|---|---|---|---|---|---|
| `WATCHDOG_MS=250` | 250 ms ÷ 1.704 ms ≈ **147 ticks** | PIC32 cyclic poll advances one slave per comm-cycle alarm (`mbus.c` `ModbusCommCycleCallBack`, `divider=actualFrequency/832`); C# publishes cached container at 10 Hz (`TrackAmplifierDataFreshness`, `DefaultStaleAfter=2 s`); PIC32 host-loss latch `HOST_AUTHORITY_WATCHDOG_MS=2000` (*verified* `enums.h:88`) | Expiry ⇒ `COMMS_LOST` ⇒ local fast ramp to neutral (STOPPING), independent of host; the 250 ms layer is **distinct from** the PIC32 2 s host-loss latch and the C# 2 s freshness — do not conflate them | Master presence lost > 250 ms ⇒ self-neutralize | **Yes** — normal config value (range 50–60000, per-position via `HR_CFG_WATCHDOG`), not a hard electrical limit |
| `ACCEL_STEP=2` | 400 ÷ 2 = 200 ticks ≈ **341 ms** forward full-swing (399→799) | none (local) | none (ramp only) | smooth accel, symmetric with decel | **Yes** |
| `DECEL_STEP=2` | 200 ticks ≈ **341 ms** normal stop | none (local) | zero command / direction-reversal passes neutral | controlled normal stop | **Yes** |
| `FAST_STOP_STEP=8` | 400 ÷ 8 = 50 ticks ≈ **85 ms** fast stop (399→799; reverse 399→1) | none (local) | used by STOP_REQUEST and comm-loss ramp | clearly faster than normal, still controlled | **Yes** |

**Required correction (check‑3 consequence):** the comm watchdog **kick condition must be "any addressed frame"**, not "FC06/FC16 write". *Verified*: the PIC32 idle poll is **FC03 reads only** (`slavehandler.c:219` "normale cyclic slave polling" issues `Data.Direction = READ` on `HOLDINGREG0/2/4/6/8/10`, two registers at a time); `ProcessPetitModbus` counts `HR_MESSAGES_RECEIVED` on addressed frames regardless of function. If the watchdog only kicked on writes, an idle (no-movement) amplifier would false-trip every ~250 ms. Kick at the point where an addressed frame is confirmed (read **or** write), e.g. in the adapter hook invoked from `ProcessPetitModbus` on address match — rename `AmpAdapter_OnWrite` to `AmpAdapter_OnFrame(addr, fn)`.

### R3‑2. F4 — revised C# domain model (no new config mechanism)

**Reuse what already exists.** The three-way separation the disposition requires is already implemented in the C# core; Phase‑1 needs no new mechanism:

- **Physical amplifier addressing** → `TrackAmplifierAddress` (*verified* `SiebwaldeApp.Core/Model/TrackApplication/Control/TrackAmplifierAddress.cs`): `1..50` = legitimate PWM targets; `51..55` = backplane/configuration devices that must **never** receive a PWM/neutral write.
- **Operational domain** → `TrackAmplifierOperationalGroup` (`Unassigned=0, MainRailway=1, MountainRailway=2, Spare=3`) + `TrackAmplifierGroups` (*verified* `.../Control/TrackAmplifierGroups.cs`): parses `"main: 1,3,4,5 ; mountain: 6,7 ; spare: 8"`, rejects non-amplifier/duplicate addresses into `Errors`, provides `Classify(ushort)` / `GetGroup()` / `AllConfigured`. An address with no group is `Unassigned` and is **never inferred as MainRailway**.
- **Global-stop scope** → `EcosHardwareStopSink.StopLayout()` (*verified* `SiebwaldeApp.Integration/EcosHardwareStopSink.cs:173-207`): the strongest emergency neutralization deliberately crosses **all** groups because unresolved traction is a physical-safety condition; the target set is filtered by `TrackAmplifierAddress.IsTrackAmplifierAddress` (physical device class), never by domain; backplane 51–55 are excluded as a *different device class*, not because of domain.

**Consequence for PIC18/PIC32:** none — the firmware has no domain concept. Per-position config, enable, command and stop delivery remain address-keyed; domain mapping, per-domain enable/config/recovery, and per-domain manual ownership stay C#-owned, keyed through `TrackAmplifierGroups`. Physical binding already flows via `LayoutProfile.ToPhysicalDomain()` and `TrackApplicationRuntimeHost.EffectiveDomain()` (fail-closed on empty domain).

### R3‑3. F3 — electrical-limit parameter definitions (values PO-reserved)

All values **PO-reserved**; no number selected. Permanent firmware constants (new firmware to change), consumed in Phase 2 only.

| Parameter | Source signal / register | Units | Validation rule | Enforcement point | Consequence of missing/absent |
|---|---|---|---|---|---|
| `OVERCURRENT_HARD_THRESHOLD` | `HR_MEAS_CURRENT` (ADC, `LM_CURR`) | raw ADC (later calibrated A) | monotonic sanity; must exceed known inrush/noise floor; cross-checked against `LM_THFLG` digital trip if available | PIC18 `amp_core` (Phase 2), immediate float + `HR_FAULT` bit0 | Phase 1 = no hard OC protection (explicitly absent); a "missing/unset" limit must be treated as **no protection ⇒ refuse drive permission / float** (fail-safe), never as "protection disabled silently" |
| `THERMAL_HARD_TRIP_LEVEL` | analog `HR_MEAS_TEMPERATURE` (ADC, `LM_TEMP`); the **digital** H-bridge trip is a hardware signal `LM_THFLG`, not a firmware constant | raw ADC / °C | must sit below the hardware digital-trip ceiling; hysteresis defined | PIC18 `amp_core` (Phase 2) | digital trip is hardware and always effective; firmware analog hard trip is additive. Missing ⇒ rely on digital trip only |
| `VBUS_HARD_HIGH` | `HR_MEAS_VOLTAGE` (ADC, `LM_V`) | raw ADC / V | `VBUS_HARD_LOW < normal < VBUS_HARD_HIGH`; five-region model (`[Agreed]` E) | PIC18 `amp_core` (Phase 2), immediate float **without ramp** (`[Agreed]` E) | missing ⇒ no hard vBus protection; fail-safe on unset |
| `VBUS_HARD_LOW` | `HR_MEAS_VOLTAGE` | raw ADC / V | as above | PIC18 `amp_core` (Phase 2), immediate float without ramp | missing ⇒ no hard vBus protection; fail-safe on unset |

No electrical activation proceeds until these are selected (authorization gate `FIRMWARE_FLASH`/`LIVE_HARDWARE`).

### R3‑4. F5 — read-only holding-telemetry semantics (FC04/FC08 deferred)

- **Function codes:** Phase‑1 read-only telemetry is served **only** by FC03 over the holding-register map (HR7–HR20). FC04 (input) and FC08 (diagnostic) remain disabled (`PETITMODBUS_READ_INPUT_REGISTERS_ENABLED=0`, `PETITMODBUS_DIAGNOSTIC_REGISTERS_ENABLED=0` — *verified* `PetitModbus.h`).
- **Write protection:** a FC06/FC16 write to a read-only register (HR7–HR20 and `HR_SW_CHECKSUM` HR11) ⇒ **Modbus exception 02** plus `HR_WRITE_RESULT` = `(addr, result=read-only, retained=1)`. For a broadcast write to a read-only register there is **no exception frame possible**, so the adapter must still (a) not modify the register and (b) update `HR_WRITE_RESULT`. *Verified note:* the current FC06/FC16 handlers return exception **03** for out-of-range address (`PetitModbus.c:350,445`) and have **no read-only distinction** — the adapter introduces the 02-for-read-only rule and the bound check.
- **Snapshot consistency:** all read-only registers are encoded **once per tick** by `AmpAdapter_Refresh()` from one coherent `AmpModel` snapshot (after `AmpCore_Update()`); `HR_IDENTITY` encoded once at init. Because `Refresh()` and the FC03 handler both run in the cooperative main loop (never preempted by each other; no ISR writes `PetitHoldingRegisters[]`), a multi-register FC03 read cannot tear across a Refresh.
- **Protocol behaviour:** FC03 read of HR7–HR20 returns the last snapshot; reserved bits read 0; out-of-range start/count ⇒ exception 02 (existing). `HR_WRITE_RESULT` is updated only on the write path, not at Refresh.

### R3‑5. The four checks, resolved with C-level detail

**Check 1 — Output-value semantics.** `HR_APPLIED_PWM` ≡ "the value the firmware last passed to `PWM3_LoadDutyValue()`" — this **is** the "last duty written to PWM3"; there is no residual ambiguity, the two are one definition. Source it from the same variable that drives `PWM3_LoadDutyValue` (today `PwmDutyCyclePrev` in `regulator.c:7,83-86`; becomes `AmpStatus.applied_pwm` written in `AmpCore_CommitOutput`). Five distinct levels: **Requested** = last valid command setpoint (`HR_REQUESTED_PWM`, echo-level) · **Target** = ramp-engine `ramp_current` (`HR_TARGET_PWM`, internal) · **Applied** = firmware commit (`HR_APPLIED_PWM`, firmware truth) · **Peripheral** = `PWM3DCH/PWM3DCL` readback (`[To verify]` meaningful readback; double-buffered) · **Observed** = scope/voltmeter (V4 only). Cautions: (a) `duty==0` is clamped to 1 (`regulator.c:81`) so applied is never 0 (matches `MinPwm=1`); (b) `REGULATORxINIT` seeds `HR_PWM_COMMAND` echo to 399 (`regulator.c:40`) — keep, but never treat echo as applied; (c) `MEASURExBMF` disables the H-bridge ~every 10 ms for BEMF sampling (`processio.c:43-56`), so "applied" may momentarily not equal physical during sampling — applied ≠ physical there.

**Check 2 — Config + APPLY lifecycle.** Add two explicit rules to §G: (i) **pending initialization** — `AmpConfig pending` is initialized = `active` at first download (and re-seeded = `active` after any rejected APPLY), so unchanged parameters are preserved and a rejected field cannot leak into a later APPLY; (ii) **rejection resets `pending ← active`** and sets `HR_WRITE_RESULT`(rejected+addr+reason+retained); it does **not** set `HR_FAULT` bit4 when the active config remains valid — a rejected runtime single-param update must not latch a drive-blocking config fault. `HR_FAULT` bit4 is reserved for an invalid/absent **active** config (initial config failure), which blocks traction. APPLY is O(1) validate-then-copy in the main loop (no preemption between validate and copy), so an APPLY edge can never activate a partially-written set; partial `HR_CFG_*` writes before the APPLY edge are inert (core reads `active` only). Restart ⇒ `NOT_INITIALIZED`, RAM-only `active` cleared (no persistence — `[Agreed]`), C# re-downloads. **Invalid configuration remains SAFE** (no traction: `INIT_DONE∧CONFIG_OK` never set; neutral 399 + brake-at-boot), **identifiable** (`HR_IDENTITY`/`HR_STATE` still served by FC03), **diagnosable** (`HR_FAULT` bit4 + `HR_WRITE_RESULT`), **provisionable** (still accepts `HR_CFG_*`+APPLY and `HR_ADDRESS`). *Additional:* reject `HR_ADDRESS` writes outside the NOT_INITIALIZED/assign window (post-enable re-addressing is a mid-run hazard).

**Check 3 — Protocol/timing ledger.** Independently re-verified against source: **RETAINED** — Modbus framing + FC03/06/16 (`PetitModbus.c`), 9-bit RX address-detect `ADDEN`/8-bit TX (`PetitModbusPort.c:66-104`, `interrupt_manager.c:77`), single ISR `IPEN=0`, TMR3 inter-char + TMR4 1.704 ms tick, cooperative loop, ID-pin startup, `HR_SW_CHECKSUM==0` bootloader handshake + `ReadFlashChecksum()` (`main.c:37,96`), backplane slot-select (backplane HR0), amplifier detect (HR11 read @`0xAA`), address assign (HR2 write @`0xAA`), broadcast enable (HR1 `0x8000`) — all confirmed (`slavestartup.c:124-629`). **Corrections required (folded in here):** (a) watchdog kick not wired — `OnHoldingRegisterWrite` exists in untracked `modbushooks.c` but is **never called** from the FC06/FC16 handlers; (b) `Update_AmplifierTicks` is `uint8_t` (`main.c:25`, `main.h:18`, `interrupt_manager.c:107`) but `modbushooks.c:8` declares `extern volatile uint32_t` — mismatch; (c) broadcast FC06 write lacks a bounds check (`PetitModbus.c:345-349`) — latent OOB, fixed by per-register validation; (d) **watchdog kick = any addressed frame** (see R3‑1); (e) CRC asymmetry `[To verify]`: PIC18 = `CRC_HW` **non-reversed**, PIC32 = `CRC_LOOKUP` **+ `CRC_HW_REVERSE`** — internal interop relies on this working today; the redesign must not touch it, but the consistency review should confirm the two remain byte-compatible; (f) **`processio.c` writes `PetitHoldingRegisters[]` directly** (`HR_STATUS`, `HR_FUSE_VOLTAGE`, `HR_HBRIDGE_TEMPERATURE`, `HR_HBRIDGE_CURRENT`) — this breaks the "mirror written only by adapter" invariant; the ADC/BEMF *sequencing* is retained but its register targets must move to `HR_VALIDITY`/`HR_MEAS_*` via the model (add a MODIFIED ledger row). Register-count/wire implications: `NUMBER_OF_HOLDING_PETITREGISTERS` 12→21 in **both** `PetitModbus.h`, `SLAVE_INFO.HoldingReg[12]→[21]` (auto), C# `TrackAmplifier.HoldingReg[12]→[21]` (`NewSenActCmdMes.cs:502`), `DeterministicTrackTransport.HoldingRegisterCount=12→21` (`DeterministicTrackTransport.cs:87`), and `EthernetTargetDataSimulator.HoldingRegInit` (`EthernetTargetDataSimulator.cs` — a second 12-register simulator site). The redesign does **not** authorize changing framing/CRC/9-bit/addressing/bootloader.

**Check 4 — Phase-1 completeness.** Internally coherent and offline-testable: identity (HR7) → config download+APPLY (HR3–6, HR1 bit13) → enable (HR1 bit15) → RUN gated on `INIT_DONE∧CONFIG_OK∧ENABLE∧¬FAULT∧¬COMMS_LOST` → command (HR0) → applied readback (HR14) → write-result (HR15) → fault/validity (HR9/10) → msg counters (HR19/20). Safe transitions: reset⇒neutral/NOT_INITIALIZED; comm-loss⇒fast ramp; EMO⇒brake-latch. Nothing essential is deferred silently: electrical protection = Phase 2 **absent** (explicit), soft thresholds/recovery = Phase 3 (explicit); parameter ownership stays C# (per position). BEMF sequencer (`MEASURExBMF`/`ADC_BMF`) retained in Phase 1 (register targets only move). Main/mountain behaviour, protection, BEMF, manual operation, logging, and yard requirements remain in `docs/amplifier-system-requirements.md` and are not dropped; Siebwalde stays the Koploper gateway (no locomotive-following administration). Offline test seams: host-shim C tests for `AmpCore_StepRamp` (one step/tick), `AmpConfig_Validate` (5-field), watchdog tick counting, applied-pwm tracking; `DeterministicTrackTransport` map tests; V3 download/readback path.

### R3‑6. Interface-contract status

- **Frozen-pending-review** (accepted direction; must still pass architectural + independent consistency review (R2) before the wire contract is "frozen"): the clean-sheet 21-register holding map with `HR_IDENTITY` at **HR7** (`MAP_VERSION=0x01`, `DEVICE_CLASS=1`, capability bits), the retained anchors **HR0/HR1/HR2/HR11**, version detection + fail-closed + coordinated flash-then-host deployment + rollback, FC04/FC08 deferral (read-only telemetry via FC03), and the five-module decoupling (transport/adapter/model/core/hardware).
- **Still PROPOSED** (not frozen): exact bit allocations in HR3–6 and HR8–HR20 (candidates pending consistency review); numeric defaults `WATCHDOG_MS=250`, ramp `2/2/8`; `HR_WRITE_RESULT` result-code encoding; the watchdog-kick condition (any addressed frame — new resolution); the read-only-write exception code (02).

### R3‑7. Remaining genuinely-necessary PO decisions

1. **F3 electrical-limit values only** — `OVERCURRENT_HARD_THRESHOLD`, `THERMAL_HARD_TRIP_LEVEL` (or confirm digital-trip-only), `VBUS_HARD_HIGH`, `VBUS_HARD_LOW`. **Phase 2 gate**; no hardware activation without them.
2. No new PO decisions. Register-allocation detail, the watchdog-kick condition, and the read-only exception code are ordinary engineering detail and are resolved here.

---

## Revision 4 — PO hardware clarification + final consistency pass (2026-10-15)

> All values remain **PROPOSED** unless marked *verified*. *Verified* = read from committed source at `master @ 11ba5e3`. This note corrects the Rev 2 body where it still contradicted the confirmed PO hardware facts, and resolves the eight bounded consistency checks. It does not restart the design and does not reopen the established decisions (clean-sheet 21-register map, `HR_IDENTITY`@HR7, anchors HR0/1/2/11, transport→adapter→model→core→hardware decoupling, main/mountain in C# only, requested/target/applied/peripheral/observed distinct, pending/active APPLY, FC04/FC08 deferred, Siebwalde = Koploper gateway).

### R4‑1. PWM range and representation (corrected, not changed)

- **Wire encoding** remains a 10-bit command field, `HR_PWM_SETPOINT_MASK = 0x03FF` (*verified* `General.h:63`). Bits 0–9 carry the duty; bit 10 `DIR`, bit 11 `BRAKE`, bit 15 `EMO` are unchanged (*verified* `General.h:66-69`).
- **Valid peripheral duty is `1..799`, neutral is `399`.** The PO-confirmed hardware PWM duty is minimum 0 / neutral 399 / maximum 799, **not** a 0..1023 usable range. `0` is **not** zero-speed; **399 is zero-speed**.
- **`0` is clamped to `1` locally** in `REGULATORxUPDATE` (`regulator.c:81`, `if (duty == 0) duty = 1;`) without modifying `HR_PWM_COMMAND`; therefore the *applied* duty is **never 0** (matches `AmplifierSpeedMapper.MinPwm = 1`, *verified*).
- **`800..1023` must never become a valid peripheral duty.** The adapter (`AmpAdapter_OnWrite`) must **reject** (write-result `range`, no write) any HR0 command whose bits 0–9 are in `800..1023`, rather than clamping them; the ramp engine (`AmpCore_StepRamp`) clamps its own output to `1..799` and never steps outside that band. (The **current** `REGULATORxUPDATE` has no such rejection — it would load any duty `0..1023`, clamping only `0→1`; the rejection is a Phase-1 addition.)
- **Requested vs applied zero.** The PO-documented hardware range is **min 0 / neutral 399 / max 799** and is unchanged. Requested `0` is a **legal wire value** (reverse extreme) and is clamped to applied `1` in `REGULATORxUPDATE` (`regulator.c:81`) without modifying `HR_PWM_COMMAND`; therefore `HR_REQUESTED_PWM` (HR12) may read `0`, while `HR_APPLIED_PWM` (HR14) is **never 0** (min applied `1`). This is not a redefinition of the hardware range — it is the intended local clamp. C# `AmplifierSpeedMapper.ToPwm` never emits `0` or `>799`.
- **Ranges:** forward `400..799`, reverse `398..1` (applied; `ReverseMaxPwm = 398`, *verified* `AmplifierSpeedMapper`), neutral `399`. Full travel is **asymmetric**: +400 counts forward (`399→799`), −399 counts reverse nominal (`399→0`) but −398 counts applied (`399→1`, since 0 is clamped).
- **Scalar-PWM + DIR consistency:** direction is encoded *in the duty value itself* (399 neutral; >399 forward; <399 reverse). The DIR bit (bit 10) is defined but not used to select a separate field in Phase 1 (scalar-PWM). Reversal is a scalar ramp **through neutral 399**; because the ramp clamps at `799` (forward) and `1` (reverse), it **cannot wrap or overflow into `800..1023`**. Neutral `399` is unchanged.

### R4‑2. Watchdog / liveness separation (four distinct mechanisms)

- **(A) C#↔PIC32 host heartbeat freshness** — PIC32 `HOST_AUTHORITY_WATCHDOG_MS = 2000` ms, refreshed by every `HOST_HEARTBEAT` (110) frame, expiry → `LATCHxGLOBALxSTOP(reason=1, GLOBAL_STOP_NEUTRAL_HR0=399)` (*verified* `controller.c:114-121`, `enums.h:87-90`). This is the **host-loss** path.
- **(B) PIC32↔PIC18 comm liveness** — the new PIC18 comm-watchdog, kicked on **any addressed frame (read or write)**. This is the **"master stopped talking"** detector; expiry → `COMMS_LOST` → local fast ramp to neutral.
- **(C) movement-command freshness/validity** — no PIC18 mechanism today; the last command is held indefinitely. Correct for a gateway (Koploper holds speed): "C# gone" is covered by (A)+(D); "PIC32 polls but no new commands" is desired hold, not a fault.
- **(D) global-stop latch + stop delivery** — PIC32 latch → `ENQUEUExGLOBALxSTOP(hr0)` writes HR0 to every detected amplifier (*verified* `controller.c:105-112`, `slavehandler.c:432-450`).
- **(E) movement-permission lease (NEW)** — PIC32 broadcast renewal of `MOTION_PERMISSION` (HR1 bit 14), gated on `!ISxGLOBALxSTOPxLATCHED()`; PIC18 lease timer; expiry → local fast-stop + `LEASE_EXPIRED` + `INHIBITED`. This is the **guaranteed** stop backstop that closes the R5‑A gap: it stops the amplifier even when a STOP_REQUEST write is lost and FC03 polling continues (which feeds (B) but not (E)).

**Scenario matrix:**
1. *C# gone while PIC32 polls* → (A) expires (2 s) → (D) neutralizes all **only if every per-slave stop write is ACKed and executed** (see R5‑A). A lost stop write for one amplifier + continued FC03 polling keeps that amplifier running while (B) stays fed (liveness) — the comm-watchdog does **not** catch this. (B) is **not** tripped (PIC32 still polls) — correct for liveness, but the safe-stop guarantee comes from (D)+R5‑A confirmation, not from (B).
2. *PIC32 polls but no new commands* → last command retained (gateway hold). (B) stays fed. Correct; no PIC18 action.
3. *PIC32 polling stops* → (B) expires → local fast-stop. Correct.
4. *Unrelated frame refreshes (B)* → "any addressed frame" kicks (B). Correct for liveness; it does not (and must not) affect command freshness, which is a PIC32/C# concern, not PIC18.
5. *Corrupted/incomplete/misaddressed frame* → CRC/9-bit address-detect reject it; it is **not** counted as addressed, so (B) is not kicked. Correct.
6. *PIC32 busy init/firmware* → polling stops → (B) would expire. Harmless while not enabled (amplifier in `NOT_INITIALIZED`/`READY`, no traction); during firmware flash the bootloader checksum path takes over. Rule: (B) may only latch `COMMS_LOST → STOPPING` while traction is enabled; otherwise it is a status, not a safety trip.
7. *Full amplifier count* → **250 ms is NOT proven safe** (below).

**250 ms at full complement — the one open decision.** The per-amplifier addressed-frame interval scales with (detected amplifiers × reads-per-amplifier × Modbus transaction time). At `BAUD_RATE = 125000` (*verified* `Init.h:8`), a 2-register FC03 exchange ≈ 17 bytes ≈ 1.4–1.7 ms + inter-frame gap; the PIC32 reads each amplifier across 3 message types (`slavehandler.c:226-275`) = **3 FC03 transactions per rotation** (M1 HR0‑1, M2 HR2‑3, M3 one of HR4‑5/6‑7/8‑9/10‑11 cycling SIP1→SIP4 over 4 rotations). With 50 amplifiers a full rotation ≈ **150 transactions ≈ ~240 ms nominal**. The three reads per amplifier are **interleaved** across the rotation (`ProcessSlave` increments per transaction, `Message` on wrap — `slavehandler.c:125,129-140`), so each amplifier is addressed roughly every ~50 transactions ≈ **~80 ms nominal** (max inter-addressed-frame gap). This ~3× nominal margin at 250 ms is **not a safety proof**: the gap is **not bounded in source** and is degraded by init pauses, mailbox bursts (a 50-entry global-stop fan-out ≈ 80 ms preempts polling; a 99-slot FIFO can starve polling under sustained C# traffic), and per-slave receive timeouts. Therefore `250 ms` is a **prototype placeholder (4-amp), not a proven production default**: before the interface is frozen, the worst-case gap must be **measured** and `WATCHDOG_MS` set ≥ **K × measured_worst_case** with **K ≥ 2** (see R5‑C policy). No analytical value may be substituted for the measurement.

**Command-freshness mechanism needed?** **No** (the gateway holds the last command; "C# gone" is covered by (A)+(D)). **Stop-delivery confirmation is a separate, required addition** — see R5‑A. (D) is fire-and-forget today: the fan-out queues 50 writes with no ACK check, no retry, and no `HR_STATE`/`HR_APPLIED_PWM` readback, so a lost stop write + continued FC03 polling leaves that amplifier running. The comm-watchdog (B) is a **liveness** detector and must **never** be relied on to close the stop-delivery gap. The smallest coherent addition is therefore **not** "none": it is the bounded per-slave delivery-confirmation mechanism of R5‑A (ACK check + bounded retry + `HR_STATE`/`HR_APPLIED_PWM` readback + explicit per-amplifier report), reusing `ENQUEUExGLOBALxSTOP`, `MbCommError`, and the existing poll/read rotation.

### R4‑3. Global-stop → fast-stop dispatch (minimum coordinated semantics)

- The current PIC32 global-stop writes HR0 = `399` (plain neutral) or `0x8000` (EMO) (*verified* `enums.h:89-90`, `slavehandler.c:439-445`). In the new HR0 layout a **plain `399` write (bits 0–9 = 399, all flags clear) must be interpreted by the PIC18 as a NORMAL command (normal decel), never as fast-stop**.
- To make a global stop enter the PIC18 **FAST-STOP** ramp (not normal decel), the PIC32 global-stop write must set an explicit flag. **Redefine `GLOBAL_STOP_NEUTRAL_HR0 = 399 | STOP_REQUEST_BIT (0x1000)`** so the PIC18 sees `STOP_REQUEST` and ramps fast to neutral. **Keep `GLOBAL_STOP_EMO_HR0 = 0x8000`** for the physical-estop variant (immediate brake-latch, retained `regulator.c:69-73` semantics).
- **A neutral-399 write alone cannot accidentally select normal decel as a safety substitute, and a fast-stop flag cannot be mistaken for a normal command:** the adapter distinguishes the two by the presence/absence of `STOP_REQUEST`/`EMO` bits, never by the duty value alone.
- **Normal commands cannot interrupt an active fast-stop:** state priority `FAULT > ESTOP > COMMS_LOST > … > STOPPING > RUN` (D.1) means new normal HR0 commands are rejected/ignored while `STOPPING`/`ESTOP` is active.
- **Release does not autonomously restore old PWM:** after fast-stop completes (neutral reached) the PIC18 holds neutral; a fresh valid command is required. The prior PWM is not retained or resumed.
- **PIC32 filter update (required):** `ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX` currently rejects any HR0 write that is not exactly `399` while latched (*verified* `slavehandler.c:388-392`). With the new layout this filter must accept only the fast-stop/EMO encoding (`399|STOP_REQUEST` or `0x8000`) and reject every normal movement HR0 (including a plain `399`, which must not be used to bypass the latch).

### R4‑4. Existing-protection preservation (non-regression contract)

**Proven in source (RETAINED by Phase 1):** EMO brake-latch (`regulator.c:69-73`: `HR_PWM_EMO_BIT` → `LM_BRAKE_LAT = true`, `LED_ERR_LAT = true`, early return, no PWM load) preserved as `ESTOP`; neutral-at-boot + brake-at-boot (`main.c:52-54`, `regulator.c:29-40`) preserved; duty `0→1` local clamp (`regulator.c:81`, never modifies `HR_PWM_COMMAND`) preserved; BEMF/ADC H-bridge sampling disable (~every 10 ms, `processio.c:43-56`) + ADC mux `LM_V`/`LM_TEMP`/`LM_CURR` → HR4/5/6 (`processio.c:219-259`) — **sequencing RETAINED; register targets move to `HR_MEAS_*`/`HR_VALIDITY` via the model**; thermal digital flag `LM_THFLG` → `HR_STATUS_THERMAL_BIT` (`processio.c:189`) — status-only (does not brake today), retained as reported signal.

**Needing hardware verification (not provable from source):** whether `LM_THFLG`/the H-bridge power stage has any autonomous (hardware) brake/trip beyond the software status bit; whether `HR_STATUS_OVERCURRENT_BIT` (`General.h:97`, "TODO") has any real hardware source (no software enforcement in tree); vBus high/low — **no hard protection in source** (`HR_FUSE_VOLTAGE` reported only).

**Missing Phase-2 features (explicit, not silently deferred):** hard overcurrent/thermal/vBus firmware trips (immediate float). These are **not a regression** — they are not implemented in committed source (TODOs / status-only), so nothing proven is dropped. **Deployment boundary (do not collapse):** Phase-1 = offline software validation only (no electrical protection implemented or exercised); Phase-2 = protection implementation (PO-selected thresholds, gated firmware change — selecting threshold numbers alone implements nothing); future hardware deployment + V4 = gated on the **actual implemented protection state** plus an **explicitly reviewed physical validation plan**. See R5‑E.

**Behaviors affected by replacing `REGULATORxUPDATE`:** EMO brake-latch, the non-EMO brake/dir-latch release (`LM_BRAKE_LAT`/`LED_ERR_LAT` clear, `regulator.c:75-76`), and the 0→1-clamped duty load — all preserved by `AmpCore_CommitOutput`. **No hard protection is lost. No BLOCKING finding.**

### R4‑5. Config / APPLY fault semantics (four distinct cases)

`HR_FAULT` bit4 (`config-invalid`) means **the *active* config is invalid or absent** — a genuine drive-blocking condition; it is **not** a generic "last APPLY was rejected" flag.

1. **Invalid pending txn (rejected runtime APPLY, active config still valid):** report `HR_WRITE_RESULT` = (addr, `range`/`relationship`, retained=1); **do not** set `HR_FAULT` bit4; **do not** change `active`; **do not** block traction.
2. **Invalid active config (init-time rejection / never valid):** `CONFIG_OK`/`INIT_DONE` stay clear → no traction (neutral 399 + brake-at-boot); `HR_FAULT` bit4 **set**; per-amp failure reported.
3. **Initial config failure (no valid config ever downloaded):** `NOT_INITIALIZED`, no drive, reported per amplifier; still FC03-readable/provisionable.
4. **Runtime protection fault (overcurrent/thermal/vBus, Phase 2):** `HR_FAULT` bits 0–3, drive blocked, latched; distinct from config faults.

`AmpConfig pending` is initialized = `active` and re-seeded `pending ← active` after any rejected APPLY (no rejected field leaks). `HR_WRITE_RESULT` reports the **param (last-written address) + reason** for every failure. Re-init invalidates stale drive permission (RAM-only `active` cleared; no persistence).

### R4‑6. Identity / deployment (legacy-misclassification fix)

Legacy HR7 is `HR_MESSAGES_RECEIVED` (*verified* `General.h:44`), a 16-bit counter incremented on **every addressed frame** including FC03 reads (*verified* `PetitModbus.c:757-758`). A single read of a legacy amplifier's HR7 can therefore **transiently** equal a plausible `HR_IDENTITY` value (e.g. counter 257 → `0x0101` = "version 1, class 1, capabilities 0"). Detection by `MAP_VERSION`+`DEVICE_CLASS` alone **can misclassify a legacy amplifier as compatible**.

**Mandatory disambiguation (both required, fail-closed):** (1) require `DEVICE_CLASS == 1` **and** a **non-zero** capability bitmap (bits 12–15 ≠ 0); (2) confirm `HR_IDENTITY` is **stable across two consecutive, distinct, fresh FC03 reads of HR7 issued back-to-back by the PIC32** (a legacy counter increments on every fresh read; a fixed identity does not). **The two observations MUST be two fresh FC03 readbacks** — a repeated cached `SLAVEINFO` upload does **not** qualify, because `UPLOAD_STATE_ALL` re-sends the cached `SlaveInfo` struct on the timer callback independent of Modbus success (`mbus.c:415-433`), so two cached uploads are the same value for both legacy and new firmware and make check (2) vacuous (R5‑B). **Check (1) alone is not sufficient**: `DEVICE_CLASS` occupies bits 8–11 (not 12–15), so a legacy counter value of `0x1101` (= 4353) decodes as version `0x01` + class `1` + non-zero capability and would pass check (1) on a single read. The decisive fail-closed defense is check (2) performed on two fresh FC03 reads — a legacy counter can never be stable across two fresh reads. No probe writes against legacy firmware are proposed.

Mixed-version and legacy amplifiers fail-closed per amplifier. Coordinated deployment: flash new firmware to all amplifiers first, then update PIC32 + C#; verify `HR_IDENTITY` (stable + class + non-zero capability + version 0x01) before enable; rollback re-detects via `MAP_VERSION` + bootloader checksum. **Preserve unchanged:** 9-bit UART RX/8-bit TX, HW CRC, backplane slot-select (HR0 @51–55), runtime address (ID-pin → HR2), bootloader (`HR_SW_CHECKSUM == 0` handshake, `ReadFlashChecksum()`).

### R4‑7. Domain / Phase-1 (confirmed, no new mechanism)

Phase 1 relies on the existing C# operational-group config: `TrackAmplifierAddress` (1..50 = PWM targets, 51..55 = never-write backplane), `TrackAmplifierOperationalGroup` (`Unassigned/MainRailway/MountainRailway/Spare`) + `TrackAmplifierGroups` (parse, classify, reject duplicates, `Unassigned` never inferred as MainRailway). Per-amp config/enable and per-domain manual/auto boundaries stay C#-owned; PIC18/PIC32 have no domain concept. Global-stop scope crosses all groups (`EcosHardwareStopSink.StopLayout()` filters by physical device class 1..50, never by domain). Fail-closed on missing/invalid domain via `EffectiveDomain()`. **No essential Phase-1 requirement is deferred to Phase 4.**

### R4‑8. Interface-contract consistency (one contract, confirmed with additions)

The 21-register map is a single contract across PIC18 / PIC32 / C# / simulator. Count propagation is register-count-derived but the C# side hardcodes `12` in more files than the Rev 2 body lists — all must move to 21 in lockstep: PIC18 `PetitModbus.h:11` and PIC32 `PetitModbus.h:15` (`NUMBER_OF_HOLDING_PETITREGISTERS` 12→21), PIC32 `PetitModbus.h:72` (`SLAVE_INFO.HoldingReg[N]` auto); C# `NewSenActCmdMes.cs:502`, `TrackAmplifierItem.cs:17` (`new ushort[12]`), `TrackCommClientAsync.cs:164` (`new ushort[12]`), `DeterministicTrackTransport.cs:87` (`HoldingRegisterCount = 12`), `EthernetTargetDataSimulator.cs` (HoldingReg init), and `TrackAmplifierRegisters.cs` (add HR12–20 constants).

Retained-behavior evidence all confirmed and consistent: framing + FC03/06/16, 9-bit RX `ADDEN`/8-bit TX, single ISR `IPEN=0`, TMR3 inter-char + TMR4 1.704 ms, cooperative loop, ID-pin startup, `HR_SW_CHECKSUM==0` bootloader handshake, backplane/address/broadcast-enable startup, broadcast FC06/FC16 lack of bounds check (`PetitModbus.c:345-349` — latent OOB fixed by per-register validation), `processio.c` direct `PetitHoldingRegisters[]` writers (must move via the model). CRC asymmetry (PIC18 `CRC_HW` non-reversed vs PIC32 `CRC_LOOKUP`+`CRC_HW_REVERSE`) remains `[To verify]` for byte-compatibility and must not be disturbed.

---

## R4 findings — updated remaining Product Owner decisions

1. **Comm-watchdog default timing at full complement (NEW, required before wire-contract freeze).** `WATCHDOG_MS = 250` is a prototype (4-amp) placeholder, not proven safe for a 50-amplifier layout (nominal per-amplifier poll interval ≈ 80 ms interleaved, full rotation ≈ 240 ms; but unbounded under init/burst/fan-out/stuck-slave, unmeasured — see R5‑C). Options: (a) accept 250 ms as prototype-only with an explicit full-complement re-derivation gate, or (b) provide a measured worst-case poll schedule / select a conservative default (≥ K× with K≥2).
2. **F3 electrical-limit values (unchanged, Phase 2 gate).** `OVERCURRENT_HARD_THRESHOLD`, `THERMAL_HARD_TRIP_LEVEL` (or digital-trip-only), `VBUS_HARD_HIGH`, `VBUS_HARD_LOW`.

No other PO decisions. Register-bit allocation, exception codes, the watchdog-kick condition, config-fault semantics, and identity disambiguation are ordinary engineering detail and are resolved in this document.

---

## Revision 5 — Freeze-gate resolution note (2026-10-15)

> Read-only investigation + documentation-only correction. All *verified* references are read from committed source at `master @ 11ba5e3`. This note resolves the five freeze-gate concerns (A–E). It does not restart the design, does not reopen the established decisions, and grants **no** implementation/flash/hardware authority.

### R5‑A. STOP DELIVERY / WATCHDOG SAFETY (the critical one)

**Question answered.** The current contract does **NOT** guarantee that an amplifier enters a safe stopping state when a global-stop write is lost. It guarantees only *queued* and *best-effort transmitted*; it guarantees neither *slave ACK*, nor *PIC18 accepted-stop-state*, nor *internally applied PWM*.

**Verified mechanics (why the gap is real):** C# heartbeat expiry → PIC32 `CHECKxHOSTxAUTHORITY()` → `LATCHxGLOBALxSTOP(1, GLOBAL_STOP_NEUTRAL_HR0=399)` (*verified* `controller.c:114-121`); the latch is **one-shot** (`controller.c:105-112`). `ENQUEUExGLOBALxSTOP(hr0)` (*verified* `slavehandler.c:432-450`) fans out one FC16 write to `HOLDINGREG0` per detected amplifier with **no ACK and no retry**. `SendNextMessage()` (`slavehandler.c:172-216`) consumes each mailbox entry exactly once regardless of whether the slave answered; `PROCESSxSLAVExCOMMUNICATION` (`slavehandler.c:319-366`) maps OK/NOK/TIMEOUT/EXCEPTION all to "return true" (no re-send). The PIC32 continues the cyclic FC03 rotation while latched (`mbus.c:204-210`), and the PIC18 comm-watchdog is kicked on **any addressed frame including FC03** (PIC18 `PetitModbus.c:755-759`). A lost stop write + continued FC03 polling therefore keeps that amplifier running; the liveness-based watchdog cannot detect the lost write.

**Six delivery levels (do not equate them):** (1) command queued; (2) transmitted; (3) slave ACK (`MbCommError == SLAVE_DATA_OK` — proves the transport accepted the value, not that the core stopped); (4) PIC18 accepted-stop-state (`HR_STATE` `STOPPING_ACTIVE`); (5) internally applied PWM (`HR_APPLIED_PWM` neutral `399`); (6) physical output — **V4 only**. Current coverage: levels 1–2 only, best-effort.

**Smallest necessary mechanism (reuses existing PIC32 mechanisms; no new framework):**
- **PIC32 O2 — ACK tracking:** while latched, track per-detected-amplifier stop state {queued, transmitted, acked, confirmed, failed}, recording `MbCommError` after each stop-write echo (reuses `CHECKxMODBUSxCOMMxSTATUS`).
- **PIC32 O3 — bounded retry:** on TIMEOUT/NOK/EXCEPTION (or no echo) re-enqueue that one amplifier's `HR0` stop write, bounded (≤ 3 delivery attempts). Do not alter the one-shot latch; retry delivery only.
- **PIC32 O4 — execution confirmation:** after ACK, issue a **fresh** FC03 read of `HR_STATE` (HR8) and `HR_APPLIED_PWM` (HR14) and require `STOPPING_ACTIVE` (or state ∈ STOPPING/ESTOP) **and** applied duty `== 399` before marking "confirmed".
- **PIC32 O5 — explicit report:** extend `GLOBAL_STOP_STATE` (or a companion per-slave status) to report {confirmed, pending, failed} per amplifier.
- **PIC32 O6 — authority (already present):** retain the `ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX` filter rejecting normal movement HR0 while latched (`slavehandler.c:385-392`).
- **C# O7:** consume the per-amplifier stop-confirmation status; block normal release/exit on any non-confirmed amplifier and raise an alarm (requirement L); do not clear the global stop until all amplifiers are confirmed, or operator-forced with an explicit "unknown states" report.
- **PIC18 O9–O11 (state-readback requirement):** `HR_STATE` (HR8) exposes `STOPPING_ACTIVE` (bit 11) + state enum, latched until applied neutral, FC03-readable, distinct from the command echo; `HR_APPLIED_PWM` (HR14) = the value actually written to `PWM3` (neutral `399` after a completed fast-stop), FC03-readable, never `0`, never `800..1023`. After a `STOP_REQUEST` fast-stop completes, hold neutral; no auto-resume; a fresh valid command is required.

**Consequence.** "C# gone → global-stop neutralizes all" is only true **if** O2–O5 are implemented; the comm-watchdog is a liveness detector and must **never** be relied on to close this gap.

### R5‑B. LEGACY FIRMWARE IDENTITY (freshness of the two-read test)

- **(i) Legacy counter increment — verified.** Legacy HR7 is `HR_MESSAGES_RECEIVED` (*verified* `General.h:44`), incremented on **every addressed (non-broadcast) frame regardless of function**, before the function switch, hence **including FC03 reads** (*verified* PIC18 `PetitModbus.c:757-758`).
- **(ii) Transaction-freshness requirement — verified.** The two-read stability test is valid **only** when the two observations are **two distinct fresh FC03 reads of HR7**, each updating the PIC32 cached `HoldingReg[7]` through `HandlePetitModbusReadHoldingRegistersSlaveReadback` (*verified* PIC32 `PetitModbus.c:620-686`). A repeated cached `SLAVEINFO` frame does **not** qualify: `UPLOAD_STATE_ALL` re-uploads the cached `SlaveInfo` struct on the timer callback (`mbus.c:415-433`, `mbus.c:125-129`), independent of Modbus success — two cached uploads are the same value for both legacy and new firmware, making the stability test vacuous.
- **Requirement (no probe writes):** identity disambiguation MUST be an explicit **PIC32 init-time sequence** of two dedicated back-to-back FC03 reads of HR7, each confirmed fresh (`MbCommError == SLAVE_DATA_OK` + `HoldingReg[7]` updated); C# consumes the two-fresh-read result and must not derive identity from the SLAVEINFO stream.

### R5‑C. FULL-SYSTEM WATCHDOG BUDGET (keep 250 ms provisional)

- **Analytical bounds (from source constants, not measurement):** 2-reg FC03 = 8 B request + 9 B response = **17 bytes**; at `BAUD_RATE = 125000` (*verified* `Init.h:8`), 10 bits/char ⇒ ≈ 1.36 ms + inter-frame gap ≈ **1.4–1.7 ms per transaction**. Rotation (*verified* `slavehandler.c:224-284`): **3 FC03 transactions per amplifier per rotation** (M1 HR0‑1, M2 HR2‑3, M3 one of HR4‑5/6‑7/8‑9/10‑11 cycling SIP1→SIP4 over 4 rotations). Full rotation (50 amps) = 150 transactions ≈ **~240 ms nominal**. The three reads per amplifier are **interleaved** (`ProcessSlave` increments per transaction, `Message` on wrap — `slavehandler.c:125,129-140`), so each amplifier is addressed roughly every ~50 transactions ≈ **~80 ms nominal** (max inter-addressed-frame gap) — ~3× nominal margin at 250 ms, which is **not a safety proof** because the gap is unbounded under mailbox preemption, a stuck-slave receive timeout, and init/bootloader pauses.
- **Assumptions (unverified):** exact `DRV_TMR` counter frequencies for comm-cycle (`/832`), receive-timeout (`/4002`), character-timeout (`/41015`) alarms; the ~1.5 ms transaction figure.
- **Unbounded/degrading cases:** mailbox preemption (`SendNextMessage` serves `Rcd[]` first; a 50-entry global-stop fan-out ≈ 80 ms preempts polling; a 99-slot FIFO can starve polling under sustained C# traffic); a stuck slave consumes its slot with a receive timeout each rotation; init/bootloader/enable pauses (`WAIT_TIME1=15 ms`, `WAIT_TIME2=1000 ms`; `MBUS_STATE_SLAVES_BOOT_WAIT`, `MBUS_STATE_RESET_WAIT`). **Measured evidence: none.**
- **Policy (before full-system deployment):** (1) measure the worst-case inter-addressed-frame gap per amplifier across a full rotation under full 50-amp complement + normal mailbox traffic + one unresponsive slave + an init/bootloader/global-stop sequence; (2) set `WATCHDOG_MS ≥ K × measured_worst_case` with **K ≥ 2** (justified in record); (3) do not deploy the full installation or freeze the contract until the measurement exists (keep 250 ms as the 4-amp placeholder); (4) prefer no-false-trips (higher K) over fast trip because the PIC18 watchdog is a **second** layer behind (A) host-loss + (D) global-stop, and R5‑A delivery-confirmation is required and must never be delegated to the watchdog.

### R5‑D. PWM ZERO SEMANTICS (confirmed precisely)

- Wire field 10-bit (`HR_PWM_SETPOINT_MASK = 0x03FF`, `General.h:63`); DIR bit 10, BRAKE bit 11, EMO bit 15 (`General.h:66-69`). PO-documented hardware duty range **min 0 / neutral 399 / max 799** (unchanged).
- Firmware clamp: requested `0` → applied `1` (`regulator.c:81`), without modifying `HR_PWM_COMMAND`. Requested `0` is a **legal wire value** (reverse extreme); applied duty is **never 0** (min applied `1`). Not a contradiction — the intended clamp (`AmplifierSpeedMapper.MinPwm = 1`).
- Neutral `399`; forward `400..799`; reverse applied `398..1` (`AmplifierSpeedMapper.cs:16-28`).
- `800..1023` rejected by the adapter (Phase-1 addition; current firmware has no such rejection). Requested vs applied are distinct: `HR_REQUESTED_PWM` (HR12) = last valid command setpoint (echo-level); `HR_APPLIED_PWM` (HR14) = last value written to `PWM3` (firmware truth). C# `ToPwm` never emits 0 or >799.

### R5‑E. HARD-PROTECTION DEPLOYMENT BOUNDARY (preserve accurately)

Firmware-based hard overcurrent/thermal/vBus protections are **NOT active** in the baseline (`HR_STATUS_OVERCURRENT_BIT` TODO-only `General.h:97`; `HR_STATUS_THERMAL_BIT` status-only from `LM_THFLG`, no brake; no vBus hard trip — `HR_FUSE_VOLTAGE` reported only). **Selecting Phase-2 threshold numbers alone implements nothing.** Three distinct boundaries, none collapsed: **Phase-1** = offline software validation (V1/V2/V3, no electrical protection); **Phase-2** = protection implementation (PO-selected thresholds, gated firmware change); **future hardware deployment + V4** = gated on the **actual implemented protection state** plus an **explicitly reviewed physical validation plan** — not inferable from software alone.

**R5 findings: No BLOCKING. No genuinely-necessary new PO decision.** The stop-delivery gap (A) is closed by the smallest R5‑A mechanism (no new framework); the watchdog default is already PO-disposed as a prototype placeholder (R5‑C supplies the measurement-gated policy); the Phase-2 electrical thresholds are already PO-reserved (out of scope for this task).

---

## Revision 6 — Stop-safety contract closure: movement-permission lease (2026-10-15)

> Read-only investigation + documentation-only correction. All *verified* references are read from committed source at `master @ 11ba5e3`. This note closes the R5‑A residual gap (detection ≠ guaranteed stopping) with a minimal PIC18 **movement-permission lease**. It does not restart the design, does not reopen the established decisions, and grants **no** implementation/flash/hardware authority.

### R6‑1. The residual gap (precise)

R5‑A guarantees *queued → transmitted → acked → confirmed* detection, but **detection does not guarantee stopping**. Verified: `ENQUEUExGLOBALxSTOP` (`slavehandler.c:432-450`) is fire-and-forget; `SendNextMessage` consumes each mailbox entry once regardless of the slave answer (`slavehandler.c:172-216`); the PIC18 comm-watchdog is kicked on **any addressed frame including FC03** (`PetitModbus.c:755-759`). A lost STOP_REQUEST (never reaches/executes the PIC18) + continued FC03 polling keeps the comm-watchdog fed, so the amplifier **keeps driving** while the PIC32/C# correctly reports "failed" and blocks release/exit. The failure is *reported* but not *safe*.

### R6‑2. Selected mechanism: movement-permission lease (rationale + rejected alternatives)

The lease inverts the failure mode: safety comes from **expiry** (absence of renewal), not from any specific write reaching the PIC18. The PIC32 periodically renews a dedicated `MOTION_PERMISSION` signal; global stop **inhibits renewal**; the PIC18 fast-stops locally when renewal ceases, even while FC03 polling continues.

Rejected alternatives: (1) R5‑A alone + host fail-closed — detection only; (2) physical stop wire / per-amplifier EMO — hardware change, Phase-2 territory, not minimal; (3) make ENABLE itself a re-asserted heartbeat — conflates static enable with dynamic permission and risks accidental re-enable on broadcast; (4) nonce/generation-token lease — anti-replay strength not needed on a single-master backplane, adds a framework.

### R6‑3. Safety invariant (one sentence)

> A track amplifier may apply a non-neutral PWM only while its movement-permission lease is fresh (explicitly renewed within `MOTION_LEASE_TIMEOUT_MS`); the moment that permission is no longer renewed, the amplifier locally fast-stops to neutral and latches an inhibit, within a bounded firmware-side budget (`MOTION_LEASE_TIMEOUT_MS` + one scheduler tick + the fast-stop ramp), independent of continued FC03 polling, lost STOP_REQUEST writes, or stale queued commands.

### R6‑4. Stop/release state transitions (lease integrated)

- **Additions:** `AMP_ST_LEASE_EXPIRED = 7`; `HR_STATE` bit 15 = `INHIBITED`; `HR_FAULT` bit 7 = `lease-expired`.
- **Priority:** `FAULT > ESTOP > COMMS_LOST > LEASE_EXPIRED > NOT_INITIALIZED > STOPPING > RUN > READY`.
- **RUN gate (updated):** `ENABLE ∧ INIT_DONE ∧ CONFIG_OK ∧ ¬FAULT ∧ ¬COMMS_LOST ∧ LEASE_FRESH`. `LEASE_FRESH := lease_ticks < LEASE_TIMEOUT_TICKS`; `lease_ticks` increments every 1.704 ms tick, reset on each `MOTION_PERMISSION` write (HR1 bit 14 = 1). FC03 reads never renew. Reset starts the lease expired (restart requires fresh renewal).
- **Stop edges (all set `INHIBITED`; none auto-restore old PWM):** STOP_REQUEST → `STOPPING` (fast) → neutral → `READY`+`INHIBITED`; lease-expiry → `LEASE_EXPIRED` → fast ramp → neutral, `INHIBITED`; comm-lost → `COMMS_LOST` → fast ramp → neutral, `INHIBITED`; EMO → `ESTOP` (brake-latch), `INHIBITED`; FAULT → `FAULT`, `INHIBITED`. **normal-zero** (duty 399, no flags) → `STOPPING` (normal decel) → `READY`, **no** `INHIBITED`, **no** latch.
- **Release:** any latched/inhibited state → `RUN` requires stop cause cleared, `ENABLE`, `LEASE_FRESH` (renewal received **after** the inhibit was latched), `¬FAULT`, `¬COMMS_LOST`, **and** a fresh valid non-neutral command. Old PWM is never resumed.

**Scenario matrix:** normal operation — lease renewed every 100 ms (fresh). Delays/dropped writes — 5× margin (≈4 lost renewals) tolerated; lost STOP_REQUEST is irrelevant (expiry is the backstop). Queued stale commands — mailbox reset on latch (`slavehandler.c:438`) + latch filter (`slavehandler.c:388-392`) + "normal commands cannot interrupt STOPPING" + `INHIBITED` block; only one in-flight frame (~1.5 ms) survives and is rejected during `STOPPING`. PIC32 host-loss — heartbeat expiry (2 s) latches and stops renewal; lease expires ≤ 500 ms later even if the fan-out is lost. Stop/release races — a renewal in-flight at the latch only resets the timer (RUN still needs a valid command, none available while latched); release is unambiguous (renewal resumes only when not latched, then a fresh command).

**PIC32 hard invariant:** lease renewal is gated on `!ISxGLOBALxSTOPxLATCHED()`; renewal is a **broadcast** (address 0, WRITE, HOLDINGREG1, `0x4000`) every `MOTION_LEASE_RENEWAL_MS`, so it is **1 transaction rather than 50**, but it is **not** insulated from the single Modbus bus: it is serialized behind in-flight polls, the global-stop fan-out and mailbox bursts (the transport is busy-rejecting — `PetitModbus.c:554-564`). The exact renewal scheduling path (mailbox vs direct-send, which timer/task drives it) is an implementation detail to be specified, and the 100/500 (5×) margin remains subject to the R5‑C measurement gate.

### R6‑5. Timeout / latency budget (named default)

`MOTION_LEASE_TIMEOUT_MS = 500` (PIC18) · `MOTION_LEASE_RENEWAL_MS = 100` (PIC32) — tunable firmware constants, **not** PO-gated. Budget = lease timeout (500 ms) + scheduling (≤ 1 tick ≈ 2 ms; `AmpCore_Update` at loop top, not gated behind the sequencer) + fast-stop ramp (400 counts ÷ `FAST_STOP_STEP=8` = 50 ticks ≈ 85 ms) = **≈ 587 ms worst-case firmware-side** stop response. This is **separate** from eventual physical stopping (V4). Margin 500/100 = 5× (≥ 2, per R5‑C policy); the R5‑C measurement gate still applies to the PIC32 broadcast schedule.

### R6‑6. Interface-contract changes (minimal)

No new registers, no map-size change. Four allocations: HR1 bit 14 = `MOTION_PERMISSION` (lease-renewal write); HR8 bits 0–3 add enum value 7 = `LEASE_EXPIRED`; HR8 bit 15 = `INHIBITED` (latched movement inhibit); HR9 bit 7 = `lease-expired`. PIC32: additive periodic broadcast lease renewal (`HR1=0x4000`), gated on `!ISxGLOBALxSTOPxLATCHED()`. C#: additive consumption of `INHIBITED` + `HR_FAULT` bit 7.

### R6‑7. Stable stop-completion observation (latched + freshness)

`HR_STATE == STOPPING ∧ HR_APPLIED_PWM == 399` is **not stable** (STOPPING is a ~85 ms transient; `STOPPING → READY` at neutral makes a delayed readback miss it). Replace with the latched condition: **`HR_STATE.INHIBITED == 1 ∧ HR_STATE.STOPPING_ACTIVE == 0 ∧ HR_APPLIED_PWM == 399`**, read via fresh FC03 (HR8 + HR14, two distinct fresh reads per R5‑B). Once neutral is reached this is latched and stable (INHIBITED stays set, STOPPING_ACTIVE stays clear, applied stays 399 — no auto-restore). EMO: `INHIBITED == 1 ∧ BRAKE_ACTIVE == 1`. Never conflate stop-requested / write-acked / stop-state-entered / output-applied-neutral / physical-neutral-verified.

### R6‑8. Remaining genuinely-necessary PO decisions (unchanged)

1. Comm-watchdog full-complement default (operational gate; R5‑C measurement, K≥2).
2. F3 electrical-limit values (Phase 2 gate).

No new PO decisions.

### R6 findings

No BLOCKING. No new genuinely-necessary PO decision. The stop-safety gap is closed by the lease; the wire-contract-freeze disposition is **DESIGN_ACCEPTABLE** (design disposition only, not behavioral acceptance).

### R6 errata — independent Integrator findings (recorded, not yet resolved)

The Integrator independently challenged the lease and confirmed the **stop half is a proven design-level guarantee** (eventual firmware-side neutralization), with two MAJOR items to record:

- **M1 — Release/re-arm path is absent (required dependency, specified in R7‑3, not implemented).** The lease stop semantics are sound, but the R6‑4 "Release" edge depends on an un-latch that does not exist: `REARM_GLOBAL_STOP` (command 112) is a reserved no-op (`controller.c:155-156`) and `GlobalStopLatch` is one-shot and never cleared. Requirement `[Agreed]` B ("release may originate from the agreed controls when physical stop inputs are clear") is therefore an **unresolved dependency**, not something the lease resolves. The global-stop re-arm mechanism must be designed/implemented as a separate increment; until then, once latched, renewal never resumes and amplifiers cannot re-enter RUN without a PIC32 reset. This is an explicit open item, not a defect in the stop-safety contract.
- **M2 — Renewal scheduling claim corrected.** The R6‑4 "hard invariant" wording was corrected in-body: broadcast renewal is 1 transaction vs 50, but is serialized on the single busy-rejecting Modbus bus behind in-flight polls, the global-stop fan-out and mailbox bursts; the exact scheduling path is unspecified and the 100/500 margin remains subject to the R5‑C measurement gate. (Availability/false-trip risk, not stop-safety risk.)

**Minor (implementation-discipline, fold into acceptance criteria):** m1 — lease reset must be keyed to the WRITE function (`fn ∈ {FC06,FC16} ∧ HR1 ∧ bit14`), not the any-frame/`OnFrame` hook; m2 — `LEASE_EXPIRED` outranks `READY` so a latch sets `INHIBITED` on ALL amplifiers including never-enabled/neutral ones (fail-safe but over-broad; every amplifier then needs a fresh renewal + fresh command); m3 — the ≈587 ms budget is a **default-config** bound (depends on `FAST_STOP_STEP=8`, which is per-position tunable); m4 — the ramp must snap to **exactly 399** on the final step or the `HR_APPLIED_PWM == 399` completion condition can never be observed.

The stop-safety contract (R6) is **DESIGN_ACCEPTABLE**; the release/re-arm mechanism (M1) is an explicitly deferred dependency and must be a named future increment before release semantics can be exercised on hardware.

---

## Revision 7 — Freeze-Preparation Note (2026-10-16)

> Read-only investigation + documentation-only design. All *verified* references are read from committed source at `master @ 11ba5e3`. This note resolves the HR1 CONTROL shared-register conflict, fixes the lease budget and stale-renewal semantics, specifies the release/re-arm dependency, locks the stop-observability evidence, and proposes the Phase-1 increment contract. It does not restart the design, does not reopen the established decisions, and grants **no** implementation/flash/hardware authority.

### R7‑1. HR1 CONTROL semantics — per-bit decode (resolves the shared-register conflict)

**Problem (verified):** HR1 packs a LEVEL (`ENABLE`, bit 15) with SET-ONLY/EDGE bits. The transport is a **full-register overwrite**: a broadcast FC06/FC16 write of `HR1 = 0x4000` (lease renewal, bit 14 only) sets `ActValue = 0x4000`, which a naive "level-from-value" decode would read as `ENABLE=0` (disable). A periodic broadcast renewal must not be able to disable the amplifier.

**Resolution — every HR1 bit becomes *ones-act, zeros-ignored*; `ENABLE` becomes a set/clear bit-pair.** No new register, no map-size change (bit 10 was reserved). The adapter owns the decode; the mirror is never interpreted as control state.

**HR1 (`HR_CONTROL`) decode table** — for a written value `V`:

| Bit | Name | Type | Decode of `V` |
|---|---|---|---|
| 15 | `ENABLE` | set-only (latch) | `V.15==1` → `enable_active = true`. `V.15==0` → no effect. |
| 14 | `MOTION_PERMISSION` | set-only (renewal) | `V.14==1` → `lease_ticks = 0`, `lease_fresh = true`. `V.14==0` → no effect. |
| 13 | `APPLY_CONFIG` | edge (0→1) | rising edge → validate + apply. `V.13==0` → re-arm only. |
| 12 | `CLEAR_FAULT` | edge (0→1) | rising edge → clear latched fault if cause gone. |
| 11 | `RESET_COUNTERS` | edge (0→1) | rising edge → zero msg counters. |
| 10 | `DISABLE` | set-only (latch) | `V.10==1` → `enable_active = false` → ramp to neutral. |
| 9..0 | reserved | — | must be 0; any set ⇒ reject the write (write-result range). |

**Invariants:** `ENABLE` is never touched by a renewal (`0x4000` sets only bit 14; `0x8000` only bit 15; `0x0400` only bit 10). A write setting both ENABLE and DISABLE (`0x8400`) is rejected (relationship). Edge bits fire only on 0→1 (adapter keeps `prev_control_word`). The adapter owns the decode; the HR1 readback (if any) is the model-derived control echo, not the raw last write. `processio.c` must stop writing `PetitHoldingRegisters[]` directly (its register targets move to `HR_VALIDITY`/`HR_MEAS_*` via the model).

**Rejected:** move `MOTION_PERMISSION` to a separate register (map 21→22, and does not fix ENABLE needing explicit set/clear); ENABLE as read-modify-write level (fragile for a safety bit); `0x0000 = DISABLE` (ambiguous with no-op).

### R7‑2. Lease expiry + fast-stop budget (and stale-renewal non-restoration)

- Renewal keyed to WRITE only: `lease_ticks = 0` on `fn ∈ {FC06,FC16} ∧ HR1 ∧ bit14`. FC03 reads never renew; the any-frame hook never renews.
- Expiry: `lease_ticks` increments per 1.704 ms tick; `LEASE_FRESH := lease_ticks < LEASE_TIMEOUT_TICKS` (500 ms ≈ 294 ticks). On expiry → `LEASE_EXPIRED` → fast ramp → neutral → `INHIBITED` latched.
- **Stopping budget relative to the last accepted renewal** (default config): 500 ms lease + ≤ 2 ticks scheduling (≈ 3.4 ms) + fast-stop ramp (85 ms) = **≈ 588 ms**. Qualifiers: default-config bound (depends on `FAST_STOP_STEP=8`, tunable); the ramp must snap to exactly 399 on the final step.
- **Distinct from the FULL host-loss budget:** C# 2 s heartbeat → PIC32 latch → fan-out (50 writes ≈ 80 ms nominal, unbounded — R5‑C) → delivery confirmation (R5‑A) → PIC18 lease expiry (≤ 500 ms) → ramp (85 ms). Only the last ≈ 588 ms segment is bounded; the 2 s + fan-out segment has no proven max wall-clock bound.
- **Stale/queued renewal cannot restore permission after global stop:** (1) renewal broadcast gated on `!ISxGLOBALxSTOPxLATCHED()`; (2) latch resets the mailbox (drops queued renewals); (3) an in-flight renewal arriving after the latch only resets `lease_ticks` (does not set ENABLE, clear INHIBITED, or provide a command); RUN needs `ENABLE ∧ … ∧ LEASE_FRESH ∧ fresh valid command`; (4) release requires a renewal received after the inhibit was latched — which cannot occur while latched (PIC32 stops renewing).

### R7‑3. Release / re-arm — required dependency (specified, not implemented)

Status: **REQUIRED for an operationally complete Phase 1** (may be a separate bounded increment, but must not be silently deferred past integrated operational acceptance). `REARM_GLOBAL_STOP` (112) is a no-op (`controller.c:155-156`); `GlobalStopLatch` is one-shot.

**Design (design-only):** PIC32 — implement `REARM_GLOBAL_STOP`: `LATCHED → ARMED` (not directly to DISARMED; authority re-arms on the next `HOST_HEARTBEAT`, preserving the DISARMED→ARMED contract), resume lease renewal. C# — own release authority: confirm physical stop inputs clear before emitting 112; after re-arm emit a fresh valid movement command. PIC18 — unchanged stop/release semantics (exit INHIBITED → RUN needs stop cause cleared + ENABLE + LEASE_FRESH + ¬FAULT + ¬COMMS_LOST + fresh valid non-neutral command). Preserved: physical stop inputs clear; explicit release authority; local faults independent (re-arm does not clear a PIC18 FAULT); no auto-restore of old PWM; no stale command/lease replay; main/mountain boundaries preserved.

### R7‑4. Stop observability (locked)

Stable completion evidence: **`HR_STATE.INHIBITED == 1 ∧ HR_STATE.STOPPING_ACTIVE == 0 ∧ HR_APPLIED_PWM == 399`**, via **two distinct fresh FC03 reads** of HR8 + HR14 (not a cached SLAVEINFO upload). Fresh and coherent across PIC32 polling cycles. Never claim physical stop from firmware register readback (V4 only).

### R7‑5. Phase-1 increment contract (smallest coherent decomposition)

- **A. Interface & state-model freeze** (this Rev 7). Gate: PO/Lead approval.
- **Parallel after A** (isolated branches/worktrees, share only the frozen contract): **B. PIC18 Control Core-lite** (commit modbushooks, fix Update_AmplifierTicks mismatch, clean F1 build; 21-register map + HR_IDENTITY; model → adapter (HR1 decode, validation, write-result) → core (state/ramp/watchdog/lease/applied) → hardware; move processio register targets to model); **C. PIC32 master** (12→21, poll rotation HR0–HR20, HR_IDENTITY init read, lease-renewal broadcast, global-stop delivery+verification, REARM); **D. C# host** (21-register parser/simulator, HR_IDENTITY fail-closed, config download+verify, consume status/fault/applied/write-result/INHIBITED, release authority); **F. Simulator + host-shim tests** (HR1 decode, ramp, config validate, watchdog/lease tick, applied tracking; map/protocol tests).
- **Sequential after B+C+D:** **E. Release/re-arm integration** (must complete before integrated operational acceptance); **G. Independent R2 review**; **H. V3 integration**.
- **Deferred (Phase 2/hardware/operational):** hard electrical protection (F3 gate); soft thresholds (Phase 3); test overrides (Phase 4); BEMF; physical output (V4); full-complement watchdog measurement (R5‑C).

### R7‑6. Wire-contract status

- **Frozen-pending-PO** (final freeze after R2): clean-sheet 21-register map, `HR_IDENTITY`@HR7, anchors HR0/1/2/11, five-module decoupling, version detection + fail-closed + deploy + rollback, FC04/FC08 deferral.
- **Resolved → frozen-pending-PO:** HR1 per-bit semantics (ENABLE/DISABLE pair + MOTION_PERMISSION + edge bits); lease 500/100; lease budget ≈ 588 ms; stop-observability; release/re-arm design; watchdog-kick any-addressed-frame; read-only exception 02.
- **Still PROPOSED:** `WATCHDOG_MS=250` (placeholder), ramp `2/2/8`, `HR_WRITE_RESULT` encoding, exact HR3–6/HR8–20 bit allocations.
- **Two existing PO gates (unchanged):** (1) comm-watchdog full-complement default (R5‑C); (2) F3 electrical limits (Phase 2).

### R7‑7. Findings

No BLOCKING. No new PO decision. Release/re-arm is a named required increment (not a freeze blocker); the stop-safety half is complete regardless of when re-arm is implemented.

---

## Revision 8 — Final Ratification (2026-10-16)

> **This note is the FINAL authority for the wire contract.** §8.4 below supersedes all prior revision text and addenda (Rev 2 through Rev 7) wherever they conflict. Read-only investigation + documentation-only design; no implementation/flash/hardware authority. Revision 7 was accepted; this note does not open a new architecture revision.

### R8‑1. HR1 command-strobe semantics (edge bits)

Edge bits `APPLY_CONFIG`(13), `CLEAR_FAULT`(12), `RESET_COUNTERS`(11) fire on a wire **0→1 transition**; the adapter keeps `prev_control_word` (init `0x0000`):

```
edge_fire(bit) := (V & (1<<bit)) != 0  AND  (prev & (1<<bit)) == 0
```

After any accepted write, `prev_control_word := V`. A repeated command requires the master to write the bit `0` (re-arm) then `1` (fire). A rejected write (reserved bits set, or ENABLE+DISABLE together) neither fires nor updates `prev_control_word`. The lease-renewal broadcast `0x4000` (bits 13/12/11 = 0) **re-arms** all three edges (never fires, never double-fires) and does not touch ENABLE/DISABLE.

Side-effect / idempotency: `APPLY_CONFIG` re-validates the (unchanged, re-seeded-on-rejection) pending set → same deterministic result, no partial activation; `CLEAR_FAULT` clears only if the cause is gone; `RESET_COUNTERS` zeroes counters. Retry is harmless. Acknowledgment via `HR_WRITE_RESULT`.

`ENABLE`(15)/`DISABLE`(10)/`MOTION_PERMISSION`(14) are set-only level bits, never strobed, independent of `prev_control_word`. `ENABLE`+`DISABLE` together (`0x8400`) ⇒ reject.

### R8‑2. Complete release / re-arm path (normative sequence)

1. Physical stop inputs clear (operator). 2. C# confirms clear. 3. C# emits `REARM_GLOBAL_STOP` (112). 4. PIC32 `LATCHED → ARMED` (not DISARMED; authority re-arms on the next `HOST_HEARTBEAT`). 5. C# resumes heartbeat → lease-renewal broadcast resumes. 6. PIC18 receives renewed `MOTION_PERMISSION` → `LEASE_FRESH` restored, but `INHIBITED` stays latched. 7. C# emits a fresh valid non-neutral HR0 command. 8. PIC18 satisfies the full RUN gate → exits INHIBITED → ramps from neutral.

**Non-resumption:** host heartbeats (C#↔PIC32) never reach the PIC18 and carry no command; lease renewals only reset `lease_ticks` (no ENABLE, no INHIBITED clear, no command). RUN requires a fresh valid non-neutral command. Local faults independent (re-arm clears only the PIC32 latch, not any PIC18 FAULT). Main/mountain boundaries preserved.

### R8‑3. Host-loss stop budget (six stages, clarified)

(a) PIC32 detects expired host authority (2 s heartbeat) → (b) inhibits MPL renewals (latch, gate `!ISxGLOBALxSTOPxLATCHED()`) → (c) PIC18 lease expiry (≤ 500 ms after last renewal) → (d) local fast-stop ramp (≈ 85 ms) → (e) stop delivery/readback confirmation (R5‑A, best-effort/unbounded) → (f) V4 physical evidence.

**The unbounded fan-out (e) is NOT a necessary component of the MPL safety budget:** the lease-expiry mechanism guarantees (c) + ≤2 ticks scheduling + (d) = **≈ 588 ms** independently of whether any STOP_REQUEST or the fan-out succeeds. Overall wall-clock = 2 s (detection) + ≈588 ms (bounded) + fan-out (unbounded, not required for safety) + V4 (later).

**Scheduling assumptions:** cooperative super-loop; `AmpCore_Update()` at loop top not gated behind the sequencer (≤1 tick); lease renewal = single broadcast transaction (100 ms); lease tick increments on TMR4 (1.704 ms).

### R8‑4. Final consolidated wire contract (SUPERSEDES ALL PRIOR REVISIONS)

**Register map (23 holding registers).** `R/W` = writable/validated; `R` = read-only (write ⇒ exception 02 + write-result `read-only`). Reserved bits must be 0. HR0–HR20 are the frozen Phase-1 map; HR21/HR22 are an **additive** firmware-versioning extension (`MAP_VERSION` stays `0x01`).

| HR | Name | Acc | Final bit allocation |
|---|---|---|---|
| 0 | `HR_COMMAND` | R/W | 0–9 PWM; 10 DIR; 11 BRAKE; 12 STOP_REQUEST `0x1000`; 15 EMO `0x8000`. Readback = command echo (not applied). |
| 1 | `HR_CONTROL` | R/W | 15 ENABLE (set-only); 14 MOTION_PERMISSION (set-only); 13 APPLY (edge); 12 CLEAR_FAULT (edge); 11 RESET_COUNTERS (edge); 10 DISABLE (set-only); 9..0 reserved=0. Ones-act, zeros-ignored. |
| 2 | `HR_ADDRESS` | R/W | 0–5 address; 13 ID_SET (R). Reject outside assign window. |
| 3 | `HR_CFG_RAMP` | R/W | 0–7 ACCEL_STEP; 8–15 DECEL_STEP. Pending. |
| 4 | `HR_CFG_FAST_STOP` | R/W | 0–7 FAST_STOP_STEP; 8–15 reserved. Pending. |
| 5 | `HR_CFG_WATCHDOG` | R/W | 0–15 WATCHDOG_MS (50–60000). Pending. |
| 6 | `HR_CFG_TAG` | R/W | 0–15 config version/CRC. Pending. |
| 7 | `HR_IDENTITY` | R | 0–7 MAP_VERSION `0x01`; 8–11 DEVICE_CLASS `1`; 12–15 capability (12 applied-readback, 13 ramp, 14 comm-watchdog, 15 two-phase-config). |
| 8 | `HR_STATE` | R | 0–3 state (0..7, 7=LEASE_EXPIRED); 8 ENABLE_ACTIVE; 9 INIT_DONE; 10 CONFIG_OK; 11 STOPPING_ACTIVE; 12 BRAKE_ACTIVE; 13 DIR_ACTIVE; 14 ID_SET; 15 INHIBITED. |
| 9 | `HR_FAULT` | R | 0 OC; 1 thermal; 2 vBus-high; 3 vBus-low; 4 config-invalid; 5 comm-lost; 6 invariant; 7 lease-expired. |
| 10 | `HR_VALIDITY` | R | 0–1 V; 2–3 T; 4–5 I; 6–7 occ (00/01/10/11); 8 occ-live; 9 thermal-live; 10 OC-live. |
| 11 | `HR_SW_CHECKSUM` | R | firmware checksum; 0 = bootloader handshake. |
| 12 | `HR_REQUESTED_PWM` | R | last valid command setpoint (echo). |
| 13 | `HR_TARGET_PWM` | R | ramped/corrected setpoint. |
| 14 | `HR_APPLIED_PWM` | R | last written to PWM3 (1..799, never 0/800..1023) + bit10 BRAKE_ACTIVE, bit11 DIR_ACTIVE. |
| 15 | `HR_WRITE_RESULT` | R | 0–7 addr; 8–10 result (0/1/2/3/4); 15 retained. |
| 16–18 | `HR_MEAS_VOLTAGE/TEMP/CURRENT` | R | raw ADC. |
| 19–20 | `HR_MSGS_RX/TX` | R | counters. |
| 21 | `HR_FW_VERSION` | R | firmware semantic version `(MAJOR<<8)&#124;MINOR` (1.0 → `0x0100`). |
| 22 | `HR_FW_VERSION_PATCH_PROTO` | R | `(PATCH<<8)&#124;PROTOCOL_VERSION` (patch 0, protocol `0x01` → `0x0001`). |

**PWM range:** duty 0..799, neutral 399, forward 400..799, reverse 398..1 (applied); wire 10-bit `0x03FF`; **800..1023 rejected**; requested 0 legal (clamped to applied 1). Direction in the duty value.

**Identity:** HR7 version/class/capability; fail-closed; two-fresh-read disambiguation (class==1 AND non-zero capability AND stable across two fresh FC03 reads — legacy counter can't satisfy).

**Config/APPLY:** two-phase pending/active; APPLY validates whole set; rejection preserves active + write-result (no HR_FAULT bit4 while active valid); INIT_DONE/CONFIG_OK only after successful APPLY.

**Lease:** HR1 bit14 renews (WRITE only, never FC03); 500/100; expiry → LEASE_EXPIRED → fast ramp → INHIBITED; renewal gated on `!ISxGLOBALxSTOPxLATCHED()`; budget ≈588 ms independent of STOP_REQUEST delivery.

**Stop/inhibit/re-arm:** STOP_REQUEST `399|0x1000` = fast-stop; EMO `0x8000` = brake; INHIBITED latched on every stop edge, cleared only by RUN re-entry (fresh command) or reset; REARM 112 = PIC32 LATCHED→ARMED; completion evidence `INHIBITED ∧ !STOPPING_ACTIVE ∧ applied==399` (two fresh FC03); priority FAULT>ESTOP>COMMS_LOST>LEASE_EXPIRED>NOT_INITIALIZED>STOPPING>RUN>READY.

**Status/error:** HR_STATE/HR_FAULT/HR_VALIDITY/HR_WRITE_RESULT/counters/applied; read-only via FC03 (FC04/FC08 deferred); snapshot-consistent via `AmpAdapter_Refresh()`.

### R8‑5. Phase-1 execution readiness (finalized)

Dependency: **A (freeze)** → **B (PIC18) / C (PIC32) / D (C#) / F (tests)** parallel (isolated worktrees, disjoint file ownership) → **E (release/re-arm integration)** → **G (R2 review)** → **H (V3 integration)**. No V4/hardware/flash.

Increment Contract (final): objective = PIC18 Control Core-lite + thin PIC32/C# support as a complete minimum path. AC1–AC9 (AC8 lease ≈588 ms; AC9 HR1 per-bit decode). Explicit exclusions: hard electrical protection (F3), soft thresholds (Phase 3), test overrides (Phase 4), BEMF, trace/UI, FY/yard, V4, watchdog measurement (R5‑C). Review R2; validation V1/V2/V3.

### R8 findings

No BLOCKING. No new PO decision. The two existing gates remain and neither blocks Phase 1: (1) comm-watchdog full-complement default (R5‑C, operational/availability); (2) F3 electrical-limit values (Phase 2). Release/re-arm (E) is a named required increment, not a freeze blocker.

---

## CONTRACT FREEZE RECORD

**Frozen:** 2026-10-16 (Product Owner authorization). **Normative contract:** §R8‑4 (Final consolidated wire contract) — this is the single frozen authority for all wire-visible addresses, bit allocations, encodings, command/status semantics, and the movement-permission lease. §R8‑1 (HR1 command-strobe), §R8‑2 (release/re-arm), §R8‑3 (host-loss budget) are frozen normative semantics. All earlier revision text/addenda (Rev 2–7) are historical and must not override the frozen §R8‑4.

**Frozen (wire-visible):** 21-register map (HR0–HR20) with all bit allocations; PWM 0..799/neutral 399/800..1023-rejected; HR7 identity (MAP_VERSION 0x01, DEVICE_CLASS 1, capability bits) + two-fresh-read disambiguation; HR1 per-bit semantics (ENABLE/DISABLE/MOTION_PERMISSION set-only, APPLY/CLEAR_FAULT/RESET_COUNTERS edge 0→1); two-phase pending/active APPLY; movement-permission lease (HR1 bit14, 500/100, LEASE_EXPIRED, INHIBITED); STOP_REQUEST `399|0x1000`; EMO `0x8000`; REARM 112; state priority; stop-completion evidence; status/fault/validity/write-result encoding.

**Additive firmware-versioning extension (semantic versioning, `MAP_VERSION` unchanged at `0x01`):** HR21 `HR_FW_VERSION` = `(MAJOR<<8)|MINOR` (first release `1.0.0` → `0x0100`), HR22 `HR_FW_VERSION_PATCH_PROTO` = `(PATCH<<8)|PROTOCOL_VERSION` (`0x0001`), read-only, served by FC03. PIC32 also gains command 113 `GET_CONTROLLER_VERSION` (reply: `data[0]`=echo 113, `data[1..3]`=MAJOR/MINOR/PATCH, `data[4]`=PROTOCOL_VERSION, `data[5]`=0, `data[6..N]`=build-date string). This does not change any HR0–HR20 meaning.

**NOT frozen (tunable, operational — separate from wire format):** `WATCHDOG_MS` (250 ms prototype placeholder; full-complement value = R5‑C measurement gate); ramp steps `2/2/8`; `MOTION_LEASE_TIMEOUT_MS=500` / `MOTION_LEASE_RENEWAL_MS=100` (tunable firmware constants); F3 electrical-limit values (Phase 2).

**Baseline:** contract authored at working-tree `master @ 11ba5e3`; implementation begins on branch `feature/amplifier-phase1`.

---

## Phase-1 Contract Amendments (2026-10-16, during implementation)

These two clarifications resolve cross-layer gaps found during Phase-1 implementation. They refine, not contradict, the frozen §R8‑4.

### Amendment A1 — HR11 access (bootloader entry preserved)

§R8‑4 lists `HR_SW_CHECKSUM` (HR11) as `R` (read-only). Clarification: HR11 is read-only **at the runtime address (1–50) after configuration**. The bootloader-entry write (`HR11 = 0` at the **boot address `0xAA` during startup**, handled by the existing startup state machine state 1 — NOT the runtime adapter) is a pre-runtime mechanism and is preserved unchanged. The PIC32 `FlashSequencer` `HR11=0` bootloader-entry write remains valid. The runtime adapter must NOT reject the boot-address `HR11=0` write during startup; its "read-only" rule for HR11 applies only after the amplifier is configured to a runtime address.

### Amendment A2 — PIC32→C# result encodings (identity + stop confirmation)

§R8‑4 froze the identity and stop-confirmation *semantics* but did not allocate their PIC32→C# *encodings*. Define:

- **Identity (per amplifier):** the PIC32's two-fresh-read HR7 check gates per-amplifier ENABLE — an incompatible amplifier is **not enabled** and remains `NOT_INITIALIZED`. C# reads `HR7` (MAP_VERSION/DEVICE_CLASS/capability) and `HR_STATE` (INIT_DONE/CONFIG_OK) from the 21-register SLAVEINFO frame and applies its own fail-closed compatibility check. **No new wire encoding is required** — the identity result is observable via the already-carried HR7/HR_STATE registers.
- **Stop confirmation (per amplifier):** the PIC32's per-amplifier stop-confirmation {confirmed, pending, failed} is reported to C# via the existing `GLOBAL_STOP_STATE` (command 111) status, extended with a per-amplifier confirmation payload: for each detected amplifier, a 2-bit field encodes {0=confirmed, 1=pending, 2=failed} (little-endian packed over the detected-amplifier list). C# consumes this to block release/exit while any amplifier is non-confirmed.
