# MODBUS_TRACK_AMPLIFIER_MAPPING.md
Version: 1.0

Language rules:
- Conversation with user: Dutch
- Code + code comments: English only

---

# 1. Purpose

This document defines the Modbus register mapping for the Track Amplifier firmware.

Goals:

- separate command/config data from measured/status data
- use Holding Registers for writable control/configuration
- use Input Registers for read-only amplifier data
- preserve legacy startup/address assignment support
- support future Control Core architecture
- keep reserved bits explicit and deterministic

---

# 2. Register Type Policy

## 2.1 Holding Registers

Use Holding Registers for:

- runtime commands
- initialization/configuration download
- apply/reset/clear commands
- writable parameters

## 2.2 Input Registers

Use Input Registers for:

- read-only measured values
- read-only state/status flags
- amplifier telemetry
- fault flags
- back-EMF and occupancy information

## 2.3 Diagnostic Registers

Use Diagnostic Registers only for:

- communication statistics
- message counters
- optional debug/diagnostic mailbox data

---

# 3. General Bit Rules

Unless explicitly stated otherwise:

- reserved bits must be written as `0`
- reserved bits are ignored on read
- firmware must not assign undocumented meaning to reserved bits
- host software must not rely on reserved bits

For any 10-bit numeric field:

- bits `0..9` contain the value
- bits `10..15` are reserved unless explicitly assigned

This applies to PWM, BEMF setpoint, voltage/current/temperature fields where relevant.

---

# 4. Track Amplifier Slave Register Map

---

## 4.1 Holding Registers

### HoldingReg0 - Runtime Control Word
Access: R/W  
Purpose: runtime drive command

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..9 | PWM_COMMAND | R/W | Yes | 10-bit PWM/speed command |
| 10 | PWM_DIRECTION | R/W | Yes | 0 = reverse, 1 = forward |
| 11 | BRAKE_REQUEST | R/W | Yes | Request brake/output stop path |
| 12 | STOP_REQUEST | R/W | Yes | Controlled stop request, ramps to neutral stop |
| 13 | RESERVED | R/W | No | Must be written as 0 |
| 14 | RESERVED | R/W | No | Must be written as 0 |
| 15 | EMERGENCY_STOP | R/W | Yes | Emergency stop request |

Reset value:
- `0x0000`

Notes:
- `PWM_COMMAND` uses only 10 bits
- `PWM_COMMAND = 0` does not imply neutral stop by itself
- stable neutral stop is defined by `PWM_STOP_DUTY = 399`

---

### HoldingReg1 - Runtime Feature Control
Access: R/W  
Purpose: runtime optional control and maintenance commands

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..9 | BEMF_SETPOINT | R/W | No | 10-bit back-EMF target for constant speed regulation |
| 10 | CSREG_ENABLE | R/W | No | Enable constant speed regulation |
| 11 | CLEAR_STATUS | R/W | No | Clear latched amplifier status where allowed |
| 12 | CLEAR_MESSAGE_BUFFER | R/W | No | Clear communication/message buffer counters |
| 13 | APPLY_PENDING_CONFIG | R/W | Yes | Apply pending configuration to active configuration |
| 14 | RESERVED | R/W | No | Must be written as 0 |
| 15 | ENABLE_AMPLIFIER | R/W | Yes | Enable runtime amplifier operation |

Reset value:
- `0x0000`

Notes:
- `ENABLE_AMPLIFIER` alone must never bypass `INIT_DONE`
- `APPLY_PENDING_CONFIG` is edge/command semantic and may be auto-cleared by firmware

---

### HoldingReg2 - Reserved for Future Runtime Control
Access: R/W  
Purpose: reserved for future runtime control expansion

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | RESERVED | R/W | No | Must be written as 0 |

Reset value:
- `0x0000`

Notes:
- formerly used as mixed read-only state in legacy mapping
- new design moves read-only data to Input Registers

---

### HoldingReg3 - Reserved for Future Runtime Control / Mailbox
Access: R/W  
Purpose: optional mailbox / future host command extension

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | RESERVED | R/W | No | Reserved for future use |

Reset value:
- `0x0000`

Notes:
- keep free for future extension if mailbox use returns

---

### HoldingReg4 - Reserved
Access: R/W  
Purpose: reserved

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | RESERVED | R/W | No | Must be written as 0 |

---

### HoldingReg5 - Reserved
Access: R/W  
Purpose: reserved

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | RESERVED | R/W | No | Must be written as 0 |

---

### HoldingReg6 - Reserved
Access: R/W  
Purpose: reserved

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | RESERVED | R/W | No | Must be written as 0 |

---

### HoldingReg7 - Reserved
Access: R/W  
Purpose: reserved

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | RESERVED | R/W | No | Must be written as 0 |

---

### HoldingReg8 - Reserved
Access: R/W  
Purpose: reserved

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | RESERVED | R/W | No | Must be written as 0 |

---

### HoldingReg9 - Configuration Word 0
Access: R/W  
Purpose: static/runtime configuration downloaded from C#

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..5 | AMPLIFIER_ID | R/W | Yes | Amplifier Modbus ID, valid range 1..55 depending on device class |
| 6 | PWM_MODE_SINGLE_SIDED | R/W | No | 0 = dual-sided PWM, 1 = single-sided PWM |
| 7..14 | RESERVED | R/W | No | Must be written as 0 |
| 15 | RESET_SLAVE | R/W | Yes | Software reset / invoke bootloader policy if supported |

Reset value:
- implementation dependent

Notes:
- preserve legacy address assignment compatibility
- host must not assume write to ID instantly means runtime init complete

---

### HoldingReg10 - Ramp Parameters
Access: R/W  
Purpose: active acceleration/deceleration parameters

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..7 | ACCEL_STEP | R/W | No | Acceleration step parameter |
| 8..15 | DECEL_STEP | R/W | No | Deceleration step parameter |

Reset value:
- `0x0000`

Notes:
- downloaded from C# during init
- copied from pending to active on APPLY

---

### HoldingReg11 - Firmware / Configuration Check
Access: R/W or R/- depending on policy  
Purpose: version/check/CRC support

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | CONFIG_VERSION_OR_CHECKSUM | R/W | No | Optional configuration version or checksum field |

Reset value:
- implementation dependent

Notes:
- recommended use: configuration CRC/version written by C#
- firmware may validate this during APPLY
- if not used yet, reserve and document as such

---

## 4.2 Input Registers

### InputReg0 - Read-Only Runtime State Word
Access: R/-  
Purpose: amplifier state and immediate read-only flags

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..9 | BACK_EMF_VALUE | R/- | Yes | Measured back-EMF value |
| 10 | TRACK_OCCUPIED | R/- | Yes | Track occupancy detected |
| 11 | THERMAL_FLAG | R/- | Yes | H-bridge thermal warning/fault indicator |
| 12 | OVERCURRENT_FLAG | R/- | Yes | H-bridge overcurrent detected |
| 13 | AMPLIFIER_ID_SET | R/- | No | Amplifier ID set by master/legacy startup |
| 14 | INIT_DONE | R/- | Yes | Runtime initialization completed |
| 15 | CONFIG_OK | R/- | Yes | Active configuration validated |

Reset value:
- implementation dependent

---

### InputReg1 - Amplifier Status Word
Access: R/-  
Purpose: latched and runtime status flags

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0 | ENABLE_ACTIVE | R/- | Yes | Amplifier enable active internally |
| 1 | STOPPING_ACTIVE | R/- | Yes | Controlled stop in progress |
| 2 | EMERGENCY_STOP_ACTIVE | R/- | Yes | Emergency stop handling active |
| 3 | COMMS_LOST | R/- | Yes | Communication watchdog timeout active |
| 4 | FAULT_PRESENT | R/- | Yes | One or more faults active |
| 5 | CSREG_ACTIVE | R/- | No | Constant speed regulation active |
| 6 | BRAKE_ACTIVE | R/- | No | Brake/output stop path active |
| 7 | DIRECTION_ACTIVE | R/- | No | Current effective direction |
| 8..15 | RESERVED | R/- | No | Reserved for future status flags |

Reset value:
- implementation dependent

---

### InputReg2 - H-Bridge Supply / Fuse Voltage
Access: R/-  
Purpose: measured amplifier supply/fuse voltage

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..9 | HBRIDGE_VOLTAGE | R/- | No | Measured voltage |
| 10..15 | RESERVED | R/- | No | Reserved |

Notes:
- moved from legacy HoldingReg4

---

### InputReg3 - H-Bridge Temperature
Access: R/-  
Purpose: measured amplifier temperature

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..9 | HBRIDGE_TEMPERATURE | R/- | No | Measured temperature |
| 10..15 | RESERVED | R/- | No | Reserved |

Notes:
- moved from legacy HoldingReg5

---

### InputReg4 - H-Bridge Current
Access: R/-  
Purpose: measured amplifier current

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..9 | HBRIDGE_CURRENT | R/- | No | Measured current |
| 10..15 | RESERVED | R/- | No | Reserved |

Notes:
- moved from legacy HoldingReg6

---

### InputReg5 - Current PWM Output
Access: R/-  
Purpose: actual applied PWM command after Control Core and ramping

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..9 | CURRENT_PWM | R/- | No | Actual applied PWM value |
| 10..15 | RESERVED | R/- | No | Reserved |

Notes:
- useful for diagnostics and host visibility

---

### InputReg6 - Target PWM Output
Access: R/-  
Purpose: active target PWM after command interpretation

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..9 | TARGET_PWM | R/- | No | Current target PWM |
| 10..15 | RESERVED | R/- | No | Reserved |

---

### InputReg7 - Active Control State
Access: R/-  
Purpose: compact state export for host diagnostics

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..3 | CONTROL_STATE | R/- | No | Encoded state: NOT_INITIALIZED/READY/RUN/STOPPING/EMERGENCY_STOP/FAULT/COMMS_LOST |
| 4..15 | RESERVED | R/- | No | Reserved |

---

## 4.3 Diagnostic Registers

### DiagnosticReg0 - Messages Received Counter
Access: R/-  
Purpose: communication diagnostics

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | MESSAGES_RECEIVED | R/- | No | Messages received from master |

Notes:
- moved from legacy HoldingReg7

---

### DiagnosticReg1 - Messages Sent Counter
Access: R/-  
Purpose: communication diagnostics

| Bits | Name | Access | Critical | Description |
|---|---|---:|---:|---|
| 0..15 | MESSAGES_SENT | R/- | No | Messages sent to master |

Notes:
- moved from legacy HoldingReg8

---

# 5. Legacy Compatibility Notes

The previous mapping used several Holding Registers for read-only state and measurement data, including:

- read back EMF / occupied / thermal / overcurrent / ID set
- amplifier status
- fuse voltage
- temperature
- current
- message counters

The new mapping relocates these to Input Registers and Diagnostic Registers.

Compatibility strategy options:

## Option A - Clean Break
- host software switches fully to the new mapping
- old read-only Holding Register usage is removed

## Option B - Transitional Compatibility Layer
- firmware keeps legacy read-only Holding Registers mirrored temporarily
- Input Registers become the preferred interface
- host software migrates first, firmware compatibility can later be removed

Recommended:
- use Option B during migration

---

# 6. Initialization / Apply Semantics

Runtime-safe behavior requires more than legacy address assignment.

Recommended meanings:

- `AMPLIFIER_ID_SET` indicates legacy/programming identity is set
- `INIT_DONE` indicates runtime init/apply completed
- `CONFIG_OK` indicates active config accepted as valid
- `ENABLE_AMPLIFIER` alone must not allow traction unless `INIT_DONE == 1`

Recommended sequence:

1. power-up
2. legacy startup/address assignment
3. C# downloads pending configuration
4. C# writes APPLY
5. firmware validates and activates config
6. firmware sets `INIT_DONE = 1` and `CONFIG_OK = 1`
7. runtime output allowed only if enable and all safety conditions are satisfied

---

# 7. Safety Rules

Non-negotiable rules:

- no traction output before init completed
- emergency stop overrides normal commands
- communication loss forces safe stop behavior
- fault forces safe output
- reserved bits must not be used implicitly

---

# 8. Recommended Naming Conventions

Use consistent symbolic names in firmware:

- `HR_RUNTIME_CONTROL`
- `HR_RUNTIME_FEATURE_CONTROL`
- `HR_CONFIG_WORD0`
- `HR_RAMP_PARAMS`
- `IR_RUNTIME_STATE`
- `IR_AMPLIFIER_STATUS`
- `IR_HBRIDGE_VOLTAGE`
- `IR_HBRIDGE_TEMPERATURE`
- `IR_HBRIDGE_CURRENT`
- `IR_CURRENT_PWM`
- `IR_TARGET_PWM`
- `IR_CONTROL_STATE`
- `DR_MESSAGES_RECEIVED`
- `DR_MESSAGES_SENT`

---

# 9. Future Extension Guidance

Safe future additions:

- configuration CRC
- configuration version
- additional fault details
- richer telemetry
- more explicit command/state separation

Avoid:

- reintroducing read-only telemetry into Holding Registers
- using reserved bits without documentation
- distributing safety-critical commands across undocumented registers