# TrackAmplifier Agent Instruction Set (Compact)

Language:
Conversation = Dutch  
Code/comments = English

---

# System Architecture

Koploper  
→ C# Application  
→ Ethernet Modbus Master  
→ Track Amplifier Firmware

C# owns:

- locomotive behaviour
- configuration
- runtime commands

Firmware owns:

- safe PWM execution
- watchdog behaviour
- emergency stop
- telemetry

---

# Configuration Model

No persistent CV storage.

All config downloaded from C# during init.

Amplifier boots in safe state.

Config exists in RAM only.

Allowed persistent data:

- hardware identity
- board revision

---

# Legacy Startup

Existing firmware supports startup address assignment.

Important rule:

Legacy configuration visibility ≠ runtime initialization.

Output allowed only if:

INIT_DONE == 1  
ENABLE == 1

Legacy behaviour must remain compatible.

---

# Control Core

Only one module owns PWM output:

control_core.c

Responsibilities:

- state machine
- ramp control
- safety gating
- stop / estop
- communication watchdog
- final PWM output

No direct PWM writes elsewhere.

---

# Runtime States

STATE_NOT_INITIALIZED  
STATE_READY  
STATE_RUN  
STATE_STOPPING  
STATE_EMERGENCY_STOP  
STATE_FAULT  
STATE_COMMS_LOST (optional)

Output allowed only if:

INIT_DONE  
ENABLE  
NO_FAULT  
NO_COMMS_LOST

---

# Modbus Integration

All register writes trigger callback:

OnHoldingRegisterWrite()

Used for:

- command handling
- config update
- watchdog reset

Applies to:

FC06  
FC16

---

# Initialization

Two-phase configuration.

Flow:

C# writes config  
→ pending_config

C# writes APPLY  
→ firmware validates

pending_config → active_config

Set:

INIT_DONE  
CONFIG_OK

Only then runtime commands are allowed.

---

# PWM Behaviour

Stop duty:

PWM_STOP_DUTY = 399

Rules:

speed=0 → ramp to stop duty  
STOP → ramp to stop duty  
INIT incomplete → force stop duty  
duty 0 not used as neutral

---

# Ramp Behaviour

Transition:

current_pwm → target_pwm

Parameters:

accel_step_per_tick  
decel_step_per_tick

Tick must be deterministic.

Emergency stop may use faster deceleration or hard stop.

---

# Communication Watchdog

If no commands within TIMEOUT_MS:

state → COMMS_LOST

Action:

fast ramp to stop duty.

Watchdog reset by command or config writes.

---

# Command Priority

1. fault
2. emergency stop
3. comms lost
4. not initialized
5. stop command
6. run command

Higher priority always wins.

---

# Telemetry

Expose:

current_pwm  
target_pwm  
state  
fault flags  
comms status  
voltage/current/temperature

---

# Firmware Constraints

Stop duty = 399  
Duty 0 not neutral  
Timer tick already exists  
Legacy PWM writes exist

Refactor carefully.

---

# Key Files

main.c  
PetitModbus.c  
regulator.c  
interrupt_manager.c

---

# Safety Rules

Never drive output unless:

INIT_DONE  
ENABLE  
NO_FAULT  
NO_COMMS_LOST

Emergency stop overrides everything.

Partial configuration must never drive outputs.

---

# AI Rules

Follow this document.

Do not:

- add locomotive logic
- store persistent CVs
- write PWM outside Control Core
- break legacy startup compatibility

