# TRACK_AMPLIFIER_STATE_MACHINE.md

# Track Amplifier Runtime State Machine
Version: 1.0

Language rules:
- Conversation with user: Dutch
- Code + code comments: English only

---

# 1. Purpose

This document defines the runtime state machine for the Track Amplifier firmware.

It exists to make the following explicit:

- which runtime states exist
- which conditions cause transitions
- which outputs are allowed in each state
- how STOP, EMERGENCY STOP, FAULT, and COMMS LOST interact
- how legacy startup behavior coexists with safe runtime gating

This file is intended to prevent ambiguous or scattered safety logic.

---

# 2. Core principle

Final output behavior must be determined by one central state machine.

No normal runtime path may bypass this state machine.

The state machine is owned by the Control Core.

---

# 3. State list

Recommended runtime states:

- `STATE_NOT_INITIALIZED`
- `STATE_READY`
- `STATE_RUN`
- `STATE_STOPPING`
- `STATE_EMERGENCY_STOP`
- `STATE_FAULT`

Optional explicit state:

- `STATE_COMMS_LOST`

If `STATE_COMMS_LOST` is not implemented as a separate state, communication loss must still be represented as a latched control reason that forces a safe stop path.

---

# 4. State intent

## 4.1 STATE_NOT_INITIALIZED

Meaning:

- runtime configuration is not yet valid
- init/apply sequence is not completed
- amplifier must not provide traction output

Allowed behavior:

- safe output only
- hold stop/neutral duty
- accept config download
- accept APPLY command
- expose telemetry/status

Forbidden behavior:

- following normal run commands
- enabling traction output from speed command alone

Expected output:

- `target_pwm = PWM_STOP_DUTY`
- final output forced safe

---

## 4.2 STATE_READY

Meaning:

- valid runtime configuration is active
- initialization is complete
- amplifier is ready to run
- actual run output is not yet active unless command conditions permit

Allowed behavior:

- wait for run command
- accept stop command
- accept updated commands
- remain safe if enable/run condition not active

Expected output:

- normally safe stop unless a valid run command is active

---

## 4.3 STATE_RUN

Meaning:

- amplifier is allowed to follow a valid run command
- ramp engine drives `current_pwm -> target_pwm`

Allowed behavior:

- normal acceleration/deceleration
- dynamic target changes
- transition to stop on stop request
- immediate override by higher priority events

Expected output:

- controlled PWM according to active command and active config

---

## 4.4 STATE_STOPPING

Meaning:

- amplifier is executing an intentional stop transition
- normal target is overridden toward stop duty

Allowed behavior:

- ramp toward `PWM_STOP_DUTY`
- remain in stopping until neutral/stop is reached
- transition to READY when stop completed and no higher-priority issue exists

Expected output:

- deceleration toward stop/neutral

---

## 4.5 STATE_EMERGENCY_STOP

Meaning:

- emergency stop condition is active
- normal run logic is overridden

Allowed behavior:

- fast ramp down toward stop duty
- or hard disable/brake if defined by hardware/safety policy

Expected output:

- most aggressive safe stop policy available by design

Notes:

- emergency stop must always have higher priority than normal stop
- recovery from emergency stop must be explicit and deterministic

---

## 4.6 STATE_FAULT

Meaning:

- fault condition exists
- amplifier must not provide normal traction output

Examples:

- thermal fault
- overcurrent fault
- critical hardware fault
- invalid unsafe internal condition

Allowed behavior:

- safe output only
- publish fault telemetry
- optionally allow fault clear handling if safe

Expected output:

- safe stop and/or output disable

Notes:

- fault state overrides run and stop requests
- fault recovery rules must be explicit

---

## 4.7 STATE_COMMS_LOST (optional dedicated state)

Meaning:

- Modbus communication watchdog expired
- host commands can no longer be trusted as current

Allowed behavior:

- force safe stop policy
- publish comms lost telemetry
- optionally escalate to disable/brake after longer timeout

Expected output:

- fast ramp toward `PWM_STOP_DUTY`
- optional later hard disable

If this state is not explicit, equivalent behavior must still exist through state reason logic.

---

# 5. Required control inputs

The state machine should evaluate at least the following logical inputs:

- `init_done`
- `config_ok`
- `enable_cmd`
- `run_cmd_present`
- `stop_cmd`
- `emergency_stop_cmd`
- `fault_present`
- `comms_lost`
- `current_pwm`
- `target_pwm`
- `stop_reached`
- `apply_request`
- `clear_fault_request` (if supported)

Optional inputs:

- `legacy_startup_ready`
- `hardware_brake_available`
- `extended_timeout_elapsed`

---

# 6. State transition summary

## 6.1 Entry into STATE_NOT_INITIALIZED

Enter when:

- power-up
- reset
- configuration invalid
- apply not yet completed
- re-init required after severe condition

Exit when:

- valid `pending_config` is applied successfully
- `init_done == 1`
- no blocking fault condition exists

---

## 6.2 Entry into STATE_READY

Enter when:

- initialization completed successfully
- active config valid
- no stop transition in progress
- no fault
- no emergency stop active
- no communication loss handling active

Exit when:

- valid run command becomes active
- stop command asserted
- emergency stop asserted
- fault occurs
- communication loss occurs
- init becomes invalid

---

## 6.3 Entry into STATE_RUN

Enter when all are true:

- `init_done == 1`
- `config_ok == 1`
- `enable_cmd == 1`
- no fault
- no emergency stop
- no communication loss forced stop
- valid run command present

Exit when any occurs:

- stop command
- speed command requests stop
- emergency stop
- fault
- communication loss
- enable removed
- init invalidated

---

## 6.4 Entry into STATE_STOPPING

Enter when:

- stop command received
- speed target becomes zero-equivalent
- run permission removed while safe deceleration is desired
- communication loss triggers ramp stop policy

Exit when:

- `current_pwm == PWM_STOP_DUTY` or stop condition tolerance satisfied
- and no higher priority condition remains

Then transition typically to:

- `STATE_READY`
- or `STATE_COMMS_LOST`
- or remain blocked by fault/emergency state if present

---

## 6.5 Entry into STATE_EMERGENCY_STOP

Enter immediately when:

- emergency stop command asserted
- emergency stop register bit asserted
- emergency stop hardware condition asserted

Exit only when all are true:

- emergency stop condition cleared
- safe stop achieved
- firmware policy permits leaving emergency state
- no fault present
- init still valid or re-init policy satisfied

Typical next state:

- `STATE_READY`
- or `STATE_NOT_INITIALIZED` if policy requires re-init

---

## 6.6 Entry into STATE_FAULT

Enter immediately when:

- critical fault detected
- thermal/overcurrent/hardware fault asserted
- internal invariant violation triggers fault handling

Exit only when all are true:

- fault cleared
- system safe
- policy permits recovery
- initialization still valid or re-init completed

Typical next state:

- `STATE_READY`
- or `STATE_NOT_INITIALIZED`

---

## 6.7 Entry into STATE_COMMS_LOST

Enter when:

- communication watchdog timeout expires

Exit when:

- valid communication restored
- policy allows recovery
- safe stop achieved
- no fault/emergency stop present

Typical next state:

- `STATE_READY`
- or `STATE_NOT_INITIALIZED` if re-init is required by policy

---

# 7. Priority rules

Priority must be deterministic.

Recommended priority order, highest first:

1. `FAULT`
2. `EMERGENCY_STOP`
3. `COMMS_LOST`
4. `NOT_INITIALIZED`
5. `STOPPING`
6. `RUN`
7. `READY`

Interpretation:

- a higher priority condition always overrides a lower priority requested action
- normal run commands are only relevant if all higher-priority conditions are absent
- stop commands override run commands
- emergency stop overrides stop
- fault overrides all

---

# 8. Output rules per state

## STATE_NOT_INITIALIZED
- force safe output
- `target_pwm = PWM_STOP_DUTY`
- no traction permission

## STATE_READY
- safe output
- may hold stop duty
- ready to transition to RUN if command is valid

## STATE_RUN
- output follows ramped target
- traction allowed

## STATE_STOPPING
- output ramps toward `PWM_STOP_DUTY`
- no new lower-priority run command may interrupt unless explicitly allowed by policy

## STATE_EMERGENCY_STOP
- apply emergency stop policy
- fast ramp or hard stop
- no normal run allowed

## STATE_FAULT
- safe output only
- disable traction

## STATE_COMMS_LOST
- safe stop policy
- no normal run until communication recovery rules pass

---

# 9. Stop completion definition

A stop is considered complete when:

- `current_pwm == PWM_STOP_DUTY`

Or, if a tolerance is required:

- `abs(current_pwm - PWM_STOP_DUTY) <= STOP_TOLERANCE`

This rule must be explicit in code.

---

# 10. Communication watchdog behavior

Communication watchdog monitors relevant Modbus activity.

Watchdog kick sources should include:

- speed command writes
- control word writes
- init/config writes
- APPLY writes

When timeout expires:

- set `comms_lost = 1`
- trigger safe stop path
- enter `STATE_COMMS_LOST` or equivalent logic

Optional escalation:

- after extended timeout, disable output or brake harder

Communication recovery must be explicit and not implicit from stale state.

---

# 11. Initialization gating rules

Legacy startup/address assignment is not sufficient for runtime permission.

Runtime output may only be allowed if all are true:

- `init_done == 1`
- `config_ok == 1`
- `enable_cmd == 1`
- `fault_present == 0`
- `emergency_stop_cmd == 0`
- `comms_lost == 0`

This rule must be enforced centrally.

---

# 12. Legacy startup interaction

Legacy startup behavior may include:

- default address visibility
- hardware ID based configuration mode
- host assignment of final address
- early enable/configuration signaling

But none of these may directly grant runtime output permission.

Correct conceptual sequence:

1. power-up
2. legacy startup/address handling
3. host assigns identity/address
4. runtime config download
5. APPLY
6. `INIT_DONE = 1`
7. Control Core may permit RUN if all safety conditions are true

---

# 13. Recommended state evaluation pseudocode

```c
ControlState ControlCore_EvaluateState(const ControlInputs* in)
{
    if (in->fault_present)
    {
        return STATE_FAULT;
    }

    if (in->emergency_stop_cmd)
    {
        return STATE_EMERGENCY_STOP;
    }

    if (in->comms_lost)
    {
        return STATE_COMMS_LOST;
    }

    if (!in->init_done || !in->config_ok)
    {
        return STATE_NOT_INITIALIZED;
    }

    if (in->stop_cmd || in->target_is_stop)
    {
        return STATE_STOPPING;
    }

    if (in->enable_cmd && in->run_cmd_present)
    {
        return STATE_RUN;
    }

    return STATE_READY;
}

14. Recommended transition table
Current State	Condition	Next State
ANY	fault present	STATE_FAULT
ANY except FAULT	emergency stop asserted	STATE_EMERGENCY_STOP
ANY except FAULT/EMERGENCY_STOP	comms lost	STATE_COMMS_LOST or STATE_STOPPING
NOT_INITIALIZED	apply success and config valid	STATE_READY
READY	valid run command + enable	STATE_RUN
RUN	stop command	STATE_STOPPING
RUN	target becomes stop	STATE_STOPPING
RUN	enable removed	STATE_STOPPING or STATE_READY depending on policy
STOPPING	stop reached	STATE_READY
EMERGENCY_STOP	estop cleared and safe	STATE_READY or STATE_NOT_INITIALIZED
FAULT	fault cleared and safe	STATE_READY or STATE_NOT_INITIALIZED
COMMS_LOST	comms restored and safe	STATE_READY or STATE_NOT_INITIALIZED

15. Telemetry requirements

Telemetry should expose at least:

current state

current_pwm

target_pwm

fault flags

emergency stop active

comms lost active

init done

config ok

enable active

This allows host-side diagnosis of why output is blocked.

16. Latching policy recommendations

Recommended latching behavior:

FAULT should latch until explicit clear or safe recovery policy

EMERGENCY_STOP should latch at least until command clears and stop is achieved

COMMS_LOST may latch until valid communication recovery is confirmed

INIT_DONE remains valid until reset/re-init/invalidation event

Latch policy must be documented in code.

17. Safe defaults

At boot and after reset, assume:

state = STATE_NOT_INITIALIZED

current output safe

target_pwm = PWM_STOP_DUTY

communication not yet trusted for runtime

no traction permission

This must remain true even if legacy startup communication is active.

18. Non-negotiable invariants

The following must always hold:

no traction output before init complete

fault overrides everything

emergency stop overrides run/stop logic

communication loss forces safe stop

partial config must not drive outputs

final output authority belongs to Control Core

19. AI and development rules

Any future modification to runtime behavior must:

preserve this state model or explicitly document deviations

preserve deterministic priority handling

avoid direct PWM writes outside Control Core

keep stop/estop/fault/comms rules centralized

preserve compatibility with legacy startup behavior

If implementation differs from this document, the difference must be explicitly documented.