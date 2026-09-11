# TrackAmplifier Agent Instruction Set
Version: 2.0

Language rules:
- Conversation: Dutch
- Code + code comments: English only

---

# 1. System Overview

Architecture chain:

Koploper  
→ C# Application  
→ Ethernet Modbus Master  
→ Track Amplifier Firmware

Responsibilities:

C# application:
- single source of truth
- locomotive behaviour
- track amplifier configuration
- initialization download
- runtime commands

Track amplifier firmware:
- deterministic command execution
- safe PWM control
- emergency stop handling
- communication watchdog
- telemetry

---

# 2. Configuration Ownership

The amplifier does NOT store CV-like parameters persistently.

All configuration is downloaded from the C# application during initialization.

Rules:

- configuration may exist only in RAM
- amplifier replacement must not require manual reconfiguration
- amplifier must boot into a safe state
- C# performs full initialization download

Allowed persistent data:

- hardware identity
- board revision
- fixed hardware safety constants

Not allowed:

- locomotive specific behaviour
- persistent CV storage
- hidden runtime state that survives replacement

---

# 3. Legacy Startup Behaviour

Existing firmware contains legacy startup communication behaviour.

Important elements:

- temporary Modbus address during startup
- hardware ID input for configuration mode
- host assigns final Modbus address
- amplifier becomes visible to master before runtime initialization

Important rule:

Legacy configuration visibility does NOT mean the amplifier is ready to drive.

Runtime traction permission must be gated by:

INIT_DONE == 1  
AND  
ENABLE == 1

Legacy startup must remain compatible.

---

# 4. Control Core Architecture

All PWM output must be owned by one module.
control_core.c

Responsibilities:

- state machine
- command arbitration
- ramp control
- stop behaviour
- emergency stop handling
- communication watchdog handling
- final PWM ownership

Rules:

- no other module may write PWM directly during normal operation
- legacy PWM paths should be migrated to Control Core
- Control Core enforces safety invariants

---

# 5. Runtime State Model

Recommended states:

STATE_NOT_INITIALIZED  
STATE_READY  
STATE_RUN  
STATE_STOPPING  
STATE_EMERGENCY_STOP  
STATE_FAULT  

Optional:

STATE_COMMS_LOST

Output permission requires:

INIT_DONE == 1  
ENABLE == 1  
NO_FAULT  
NO_COMMS_LOST

---

# 6. Modbus Write Callback

All holding register writes must trigger a firmware callback.

Applicable to:

Function Code 06  
Function Code 16

Concept:

OnHoldingRegisterWrite(addr, value, broadcast)

Used to:

- update pending configuration
- update commands
- detect STOP / ESTOP
- kick communication watchdog

Both FC06 and FC16 must route through this logic.

---

# 7. Initialization Flow (2-Phase Commit)

Firmware must separate pending and active configuration.

Flow:

1. C# writes configuration registers
2. firmware stores values in pending_config
3. C# writes CONTROL.APPLY
4. firmware validates configuration
5. pending_config → active_config
6. firmware sets:

STATUS.INIT_DONE = 1  
STATUS.CONFIG_OK = 1

Traction output only allowed after initialization.

---

# 8. PWM Behaviour

Stop duty:

PWM_STOP_DUTY = 399

Rules:

- STOP ramps toward PWM_STOP_DUTY
- speed=0 ramps toward PWM_STOP_DUTY
- if INIT not completed → force stop duty
- duty value 0 must not be used as neutral output

---

# 9. Ramp Behaviour

Amplifier provides generic ramping only.

Transition model:

current_pwm → target_pwm

Ramp parameters:

accel_step_per_tick  
decel_step_per_tick

Ramp tick must be deterministic.

Emergency stop may:

- use faster deceleration
- or hard stop depending on hardware policy

---

# 10. Communication Watchdog

Firmware must detect loss of Modbus communication.

If no relevant writes occur within TIMEOUT_MS:

state → COMMS_LOST

Default reaction:

fast ramp toward PWM_STOP_DUTY

Optional escalation:

disable/brake after extended timeout.

Watchdog reset occurs on:

- command writes
- control writes
- config writes
- APPLY command

---

# 11. Command Priority

Highest priority first:

1. hardware fault
2. emergency stop
3. communication loss
4. not initialized
5. stop command
6. normal run command

Lower priority commands may never override higher priority conditions.

---

# 12. Telemetry

Firmware should report:

current_pwm  
target_pwm  
state  
fault flags  
comms lost flag  
voltage/current/temperature

Telemetry must represent the Control Core state.

---

# 13. Known Firmware Constraints

Existing behaviour assumptions:

Stop duty:
399

Zero duty:
not used as neutral

Legacy code may still perform direct PWM writes and must be refactored carefully.

Periodic timing already exists via timer interrupts and should be reused.

---

# 14. Key Firmware Files

Important integration points:

main.c  
startup logic and sequencing

PetitModbus.c  
register write handlers

regulator.c  
current PWM output logic

interrupt_manager.c  
timer interrupt and periodic tick

Register definitions must remain compatible.

---

# 15. Safety Invariants

The following rules are mandatory:

traction output only allowed if INIT_DONE and ENABLE

fault state forces safe output

communication loss forces safe stop

emergency stop overrides all commands

partial configuration must never drive outputs

Future code must preserve these invariants.

---

# 16. Architecture Boundary

Legacy startup layer handles:

address assignment  
startup discovery  
configuration visibility

Control Core handles:

runtime output permission  
PWM generation  
ramp behaviour  
safety logic  
stop logic  
comms watchdog

Legacy behaviour must not bypass Control Core.

---

# 17. Pending vs Active Configuration

Configuration registers update:

pending_config

Runtime logic uses:

active_config

APPLY command performs:

pending_config → active_config

Partial configuration must never affect runtime behaviour.

---

# 18. Future Extension Guidelines

Safe extension areas:

additional telemetry  
configuration CRC/version  
improved ramp models  
diagnostics

Avoid:

persistent amplifier CV storage  
locomotive intelligence in amplifier  
PWM writes outside Control Core

---

# 19. AI Development Rules

Future AI sessions must:

respect this document as architecture contract  
preserve legacy startup compatibility  
avoid direct PWM writes outside Control Core  
avoid locomotive behaviour in amplifier  
avoid persistent CV storage assumptions  
prefer minimal deterministic changes

Code must remain embedded-friendly and deterministic.

# 20. Periodic Tick Handling

The firmware uses `Update_AmplifierTicks` as the central periodic timing source.

Rules:

- `Update_AmplifierTicks` must be incremented from the timer interrupt only
- `Update_AmplifierTicks` must be declared as:
  - `volatile uint32_t`
- main loop logic must detect tick changes and execute periodic amplifier logic once per new tick
- do not scatter direct tick comparisons across unrelated modules
- use one central tick gate/helper for deterministic periodic execution

Recommended pattern:

- ISR increments:
  - `Update_AmplifierTicks++`
- main loop checks whether a new tick occurred
- if yes:
  - execute periodic amplifier tasks exactly once for that tick

Recommended helper concept:

```c
bool AmplifierTick_Elapsed(void);