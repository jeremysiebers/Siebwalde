#include "amp_adapter.h"
#include "amp_core.h"
#include "modbus/General.h"
#ifdef AMP_HOST_TEST
#include "tests/petit_shim.h"
#else
#include "modbus/PetitModbus.h"
#endif

static void AmpAdapter_EncodeWritable(void)
{
    AmpModel *m = &amp_model;
    PetitHoldingRegisters[HR_COMMAND].ActValue = m->command_echo;
    PetitHoldingRegisters[HR_CONTROL].ActValue = (m->command.enable ? HR_CONTROL_ENABLE : 0u) |
        (m->status.lease_fresh ? HR_CONTROL_LEASE : 0u);
    PetitHoldingRegisters[HR_ADDRESS].ActValue = m->address | (m->status.id_set ? 0x2000u : 0u);
    PetitHoldingRegisters[HR_CFG_RAMP].ActValue = m->pending.accel_step | ((uint16_t)m->pending.decel_step << 8);
    PetitHoldingRegisters[HR_CFG_FAST_STOP].ActValue = m->pending.fast_stop_step;
    PetitHoldingRegisters[HR_CFG_WATCHDOG].ActValue = m->pending.watchdog_ms;
    PetitHoldingRegisters[HR_CFG_TAG].ActValue = m->pending.tag;
    PetitHoldingRegisters[HR_TRACK_POWER].ActValue =
        m->status.track_power_requested ? TRACK_POWER_ENABLE : 0u;
}

static uint8_t AmpAdapter_Result(uint16_t addr, uint8_t result)
{
    amp_model.write_result = (addr & 0xFFu) | ((uint16_t)result << 8) |
        (result && result != AMP_WRITE_SATURATED ? 0x8000u : 0u);
    PetitHoldingRegisters[HR_WRITE_RESULT].ActValue = amp_model.write_result;
    return result;
}

void AmpAdapter_Init(void)
{
    PetitHoldingRegisters[HR_IDENTITY].ActValue = AMP_IDENTITY;
    PetitHoldingRegisters[HR_FW_VERSION].ActValue = (AMP_FW_MAJOR << 8) | AMP_FW_MINOR;
    PetitHoldingRegisters[HR_FW_VERSION_PATCH_PROTO].ActValue = (AMP_FW_PATCH << 8) | AMP_PROTOCOL_VERSION;
    PetitHoldingRegisters[HR_WRITE_RESULT].ActValue = amp_model.write_result;
    AmpAdapter_EncodeWritable();
    AmpAdapter_Refresh();
}

void AmpAdapter_OnFrame(uint8_t addr, uint8_t fn)
{
    (void)fn;
    amp_model.comm_watchdog_ticks = 0; /* any CRC-valid addressed frame, never a lease renewal */
    if (addr != 0u) amp_model.msgs_rx++;
}
void AmpAdapter_OnTransmit(void) { amp_model.msgs_tx++; }
void AmpAdapter_AddressAssigned(void)
{
    amp_model.status.id_set = true;
    amp_model.assign_window = false;
    AmpAdapter_EncodeWritable();
}

uint8_t AmpAdapter_ValidateWrite(uint16_t addr, uint16_t v)
{
    if (addr >= 24u) return AMP_WRITE_RANGE;
    if (addr >= HR_IDENTITY && addr != HR_TRACK_POWER) return AMP_WRITE_READ_ONLY;
    switch (addr) {
    case HR_COMMAND:
        if ((v & 0x6000u) || (v & HR_PWM_SETPOINT_MASK) > 799u) return AMP_WRITE_RANGE;
        if (!(v & (HR_PWM_STOP_BIT | HR_PWM_EMO_BIT)) &&
            (amp_model.fast_stopping || amp_model.status.stopping_active) &&
            (v & HR_PWM_SETPOINT_MASK) != AMP_NEUTRAL) return AMP_WRITE_RELATIONSHIP;
        break;
    case HR_CONTROL:
        if (v & 0x03FFu) return AMP_WRITE_RANGE;
        if ((v & 0x8400u) == 0x8400u) return AMP_WRITE_RELATIONSHIP;
        break;
    case HR_TRACK_POWER:
        if (v & 0xFFFEu) return AMP_WRITE_RANGE;
        break;
    case HR_ADDRESS:
        if ((v & 0xFFC0u) || !(v & 0x003Fu)) return AMP_WRITE_RANGE;
        if (!amp_model.assign_window) return AMP_WRITE_RELATIONSHIP;
        break;
    case HR_CFG_RAMP:
        if (!(v & 0xFFu) || !(v >> 8)) return AMP_WRITE_RANGE;
        break;
    case HR_CFG_FAST_STOP:
        if (!v || v > 255u) return AMP_WRITE_RANGE;
        break;
    case HR_CFG_WATCHDOG:
        if (v < 50u || v > 60000u) return AMP_WRITE_RANGE;
        break;
    default: break;
    }
    return AMP_WRITE_OK;
}

uint8_t AmpAdapter_OnWrite(uint16_t addr, uint16_t v, bool broadcast)
{
    AmpModel *m = &amp_model;
    uint16_t edges;
    uint8_t result = AmpAdapter_ValidateWrite(addr, v);
    (void)broadcast; /* identical validation; transport suppresses broadcast responses */
    if (result) return AmpAdapter_Result(addr, result);
    switch (addr) {
    case HR_COMMAND:
        m->command_echo = v;
        m->status.requested_pwm = v & HR_PWM_SETPOINT_MASK;
        m->command.dir = (v & HR_PWM_DIR_BIT) != 0;
        m->command.brake = (v & HR_PWM_BRAKE_BIT) != 0;
        m->command.emo = (v & HR_PWM_EMO_BIT) != 0;
        m->command.stop_request = (v & HR_PWM_STOP_BIT) != 0;
        if (m->command.emo || m->command.stop_request) AmpCore_Inhibit();
        else if (m->status.inhibited) {
            if (m->renewal_after_inhibit && m->lease_ticks < AMP_LEASE_TICKS &&
                m->command.enable && m->status.config_ok && !m->fault.causes &&
                m->ramp_current == AMP_NEUTRAL && m->status.applied_pwm == AMP_NEUTRAL &&
                m->status.requested_pwm != AMP_NEUTRAL) {
                m->status.inhibited = false;
                m->command.pwm = m->status.requested_pwm;
            }
        } else m->command.pwm = m->status.requested_pwm;
        if (m->status.requested_pwm == 0u && !m->command.emo && !m->command.stop_request)
            result = AMP_WRITE_SATURATED;
        break;
    case HR_CONTROL:
        edges = v & (uint16_t)~m->prev_control_word;
        m->prev_control_word = v;
        if (v & HR_CONTROL_ENABLE) m->command.enable = true;
        if (v & HR_CONTROL_DISABLE) {
            m->command.enable = false;
            AmpCore_Inhibit();
        }
        if (v & HR_CONTROL_LEASE) {
            m->lease_ticks = 0;
            m->status.lease_fresh = true;
            m->renewal_after_inhibit = m->status.inhibited;
            m->fault.causes &= (0xFFFFu ^ AMP_FAULT_LEASE);
        }
        if (edges & HR_CONTROL_APPLY) {
            if (AmpConfig_Validate(&m->pending)) {
                m->active = m->pending;
                m->comm_timeout_ticks = ((uint32_t)m->active.watchdog_ms * 1000ul + 1703ul) / 1704ul;
                m->status.init_done = m->status.config_ok = true;
                m->fault.causes &= (0xFFFFu ^ AMP_FAULT_CONFIG);
            } else {
                m->pending = m->active;
                result = AMP_WRITE_RANGE;
            }
        }
        if ((edges & HR_CONTROL_CLEAR) && m->comm_watchdog_ticks < m->comm_timeout_ticks)
            m->fault.causes &= (0xFFFFu ^ AMP_FAULT_COMMS);
        /* Phase-2 hard causes cannot be cleared without live-cause qualification. */
        if (edges & HR_CONTROL_RESET) m->msgs_rx = m->msgs_tx = 0;
        break;
    case HR_ADDRESS: m->address = (uint8_t)v; break;
    case HR_TRACK_POWER:
        m->status.track_power_requested = (v & TRACK_POWER_ENABLE) != 0;
        /* Clearing the request always re-latches inhibit, so a de-energize is
         * never silently skipped even if the neutral energize was pending. */
        if (!m->status.track_power_requested) AmpCore_Inhibit();
        break;
    case HR_CFG_RAMP: m->pending.accel_step = (uint8_t)v; m->pending.decel_step = (uint8_t)(v >> 8); break;
    case HR_CFG_FAST_STOP: m->pending.fast_stop_step = (uint8_t)v; break;
    case HR_CFG_WATCHDOG: m->pending.watchdog_ms = v; break;
    case HR_CFG_TAG: m->pending.tag = v; break;
    default: break;
    }
    /* Fault B: keep the derived enable-active status and the HR8 state mirror in
     * lockstep with the write. `command.enable` flips synchronously here, but
     * `status.enable_active` is otherwise only recomputed by AmpCore_Update() at
     * the ~1.7 ms regulator tick; a post-write FC03 read could therefore return a
     * stale enable-active=1 and fail the C# DISABLE verify. Rebuild the mirror now
     * so the write is visible immediately (state-enum low bits self-correct at the
     * next tick). */
    m->status.enable_active = m->command.enable;
    AmpAdapter_Refresh();
    return AmpAdapter_Result(addr, result);
}

void AmpAdapter_Refresh(void)
{
    /* Main loop only. No ISR writes the model/mirror; FC03 cannot interleave. */
    const AmpModel *m = &amp_model;
    uint16_t state = (uint16_t)m->status.state;
    if (m->status.enable_active) state |= 0x0100u;
    if (m->status.init_done) state |= 0x0200u;
    if (m->status.config_ok) state |= 0x0400u;
    if (m->status.stopping_active) state |= 0x0800u;
    if (m->status.brake_active) state |= 0x1000u;
    if (m->status.dir_active) state |= 0x2000u;
    if (m->status.id_set) state |= 0x4000u;
    if (m->status.inhibited) state |= 0x8000u;
    AmpAdapter_EncodeWritable();
    PetitHoldingRegisters[HR_STATE].ActValue = state;
    PetitHoldingRegisters[HR_FAULT].ActValue = m->fault.causes;
    PetitHoldingRegisters[HR_VALIDITY].ActValue = m->fault.validity;
    PetitHoldingRegisters[HR_SW_CHECKSUM].ActValue = m->checksum;
    PetitHoldingRegisters[HR_REQUESTED_PWM].ActValue = m->status.requested_pwm;
    PetitHoldingRegisters[HR_TARGET_PWM].ActValue = m->status.target_pwm;
    PetitHoldingRegisters[HR_APPLIED_PWM].ActValue = m->status.applied_pwm |
        (m->status.brake_active ? 0x0400u : 0u) | (m->status.dir_active ? 0x0800u : 0u);
    PetitHoldingRegisters[HR_MEAS_VOLTAGE].ActValue = m->voltage;
    PetitHoldingRegisters[HR_MEAS_TEMPERATURE].ActValue = m->temperature;
    PetitHoldingRegisters[HR_MEAS_CURRENT].ActValue = m->current;
    PetitHoldingRegisters[HR_MSGS_RX].ActValue = m->msgs_rx;
    PetitHoldingRegisters[HR_MSGS_TX].ActValue = m->msgs_tx;
}
