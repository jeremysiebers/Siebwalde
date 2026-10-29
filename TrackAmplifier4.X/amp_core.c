#include "amp_core.h"
#include <string.h>
AmpModel amp_model;

bool AmpConfig_Validate(const AmpConfig *c)
{
    /* The frozen contract specifies independent steps, no ordering relation. */
    return c->accel_step && c->decel_step && c->fast_stop_step &&
           c->watchdog_ms >= 50u && c->watchdog_ms <= 60000u;
}

void AmpCore_Init(uint16_t checksum)
{
    memset(&amp_model, 0, sizeof(amp_model));
    amp_model.checksum = checksum;
    amp_model.assign_window = true;
    amp_model.command.pwm = AMP_NEUTRAL;
    amp_model.command_echo = AMP_NEUTRAL;
    amp_model.ramp_current = AMP_NEUTRAL;
    amp_model.status.requested_pwm = AMP_NEUTRAL;
    amp_model.status.target_pwm = AMP_NEUTRAL;
    amp_model.status.applied_pwm = AMP_NEUTRAL;
    amp_model.status.brake_active = true;
    amp_model.lease_ticks = AMP_LEASE_TICKS;
    amp_model.fault.causes = AMP_FAULT_CONFIG;
    amp_model.fault.validity = 0x55u; /* expected unavailable until sampled */
}

void AmpCore_Inhibit(void)
{
    if (!amp_model.status.inhibited) {
        amp_model.status.inhibited = true;
        amp_model.renewal_after_inhibit = false;
    }
    amp_model.command.pwm = AMP_NEUTRAL; /* never replay retained movement */
    amp_model.fast_stopping = true;
    /* F5: any safety inhibit (lease expiry / comm loss / hard fault / EMO /
     * STOP_REQUEST / DISABLE) also drops the track-power request, so a transient
     * fault that later clears can never re-energize the H-bridge at neutral
     * without a fresh operator HR_TRACK_POWER=1 write. */
    amp_model.status.track_power_requested = false;
    /* Group-ready is a C#-issued global decision; a local safety inhibit means the
     * amplifier is no longer "ready", so drop group_ready too and return to STANDBY.
     * C# must re-issue GROUP_READY after recovery. */
    amp_model.status.group_ready = false;
}

void AmpLed_Modes(bool pwm3en, AmpLedMode *run_mode, AmpLedMode *war_mode, AmpLedMode *err_mode)
{
    const AmpModel *m = &amp_model;
    bool fault_display = m->status.state == AMP_ST_FAULT || m->command.emo ||
                         (m->fault.causes & AMP_FAULT_COMMS) != 0;

    /* ERR LED: continuous on for a hard fault / EMO / comm loss (existing priority). */
    *err_mode = fault_display ? AMP_LED_ON : AMP_LED_OFF;

    if (pwm3en && !m->status.brake_active) {
        /* Energized (RUN, or a fast-stop/stop ramp still switching): RUN continuous, WAR off. */
        *run_mode = AMP_LED_ON;
        *war_mode = AMP_LED_OFF;
    } else if (m->status.state == AMP_ST_READY) {
        /* Group-ready but not energized: RUN slow blink, WAR off. */
        *run_mode = AMP_LED_BLINK;
        *war_mode = AMP_LED_OFF;
    } else {
        /* STANDBY / not-init / fault / lease / stopped: RUN off, WAR slow blink. */
        *run_mode = AMP_LED_OFF;
        *war_mode = AMP_LED_BLINK;
    }
}

uint16_t AmpCore_StepRamp(uint16_t current, uint16_t target, uint8_t step)
{
    if (current < 1u) current = 1u;
    if (current > 799u) current = 799u;
    if (target < 1u) target = 1u;
    if (target > 799u) target = 799u;
    /* Reversal must visit exactly neutral, even when step straddles it. */
    if ((current < AMP_NEUTRAL && target > AMP_NEUTRAL) ||
        (current > AMP_NEUTRAL && target < AMP_NEUTRAL)) target = AMP_NEUTRAL;
    if (current < target) {
        if (target - current <= step) return target;
        return current + step;
    }
    if (current - target <= step) return target;
    return current - step;
}

void AmpCore_Update(void)
{
    AmpModel *m = &amp_model;
    uint16_t goal = AMP_NEUTRAL;
    uint8_t step;
    bool hard, comm, lease;
    if (m->lease_ticks < AMP_LEASE_TICKS) m->lease_ticks++;
    if (m->comm_watchdog_ticks < 0xFFFFFFFFul) m->comm_watchdog_ticks++;
    m->status.lease_fresh = m->lease_ticks < AMP_LEASE_TICKS;
    if (!m->status.lease_fresh && !(m->fault.causes & AMP_FAULT_LEASE)) {
        AmpCore_Inhibit();
        m->fault.causes |= AMP_FAULT_LEASE;
    }
    if (m->command.enable && m->status.config_ok &&
        m->comm_watchdog_ticks >= m->comm_timeout_ticks &&
        !(m->fault.causes & AMP_FAULT_COMMS)) {
        AmpCore_Inhibit();
        m->fault.causes |= AMP_FAULT_COMMS;
    }
    hard = (m->fault.causes & AMP_FAULT_HARD) != 0;
    comm = (m->fault.causes & AMP_FAULT_COMMS) != 0;
    lease = (m->fault.causes & AMP_FAULT_LEASE) != 0;
    if (hard || m->command.emo || m->command.stop_request) AmpCore_Inhibit();
    if (hard) m->status.state = AMP_ST_FAULT;
    else if (m->command.emo) m->status.state = AMP_ST_ESTOP;
    else if (comm) m->status.state = AMP_ST_COMMS_LOST;
    else if (lease) m->status.state = AMP_ST_LEASE_EXPIRED;
    else if (!m->status.init_done || !m->status.config_ok) m->status.state = AMP_ST_NOT_INITIALIZED;
    else if (m->status.config_ok && !m->status.group_ready) m->status.state = AMP_ST_STANDBY;
    else if (m->fast_stopping || (!m->command.enable && m->ramp_current != AMP_NEUTRAL) ||
             (m->command.pwm == AMP_NEUTRAL && m->ramp_current != AMP_NEUTRAL)) m->status.state = AMP_ST_STOPPING;
    else if (m->command.enable && !m->status.inhibited && m->status.lease_fresh &&
             m->command.pwm != AMP_NEUTRAL) m->status.state = AMP_ST_RUN;
    else m->status.state = AMP_ST_READY;

    if (m->status.state == AMP_ST_RUN) goal = m->command.pwm;
    step = m->active.decel_step;
    if (m->fast_stopping || hard || comm || lease || m->command.emo)
        step = m->active.fast_stop_step ? m->active.fast_stop_step : 8u;
    else if ((goal > AMP_NEUTRAL && goal > m->ramp_current && m->ramp_current >= AMP_NEUTRAL) ||
             (goal < AMP_NEUTRAL && goal < m->ramp_current && m->ramp_current <= AMP_NEUTRAL))
        step = m->active.accel_step;
    if (!m->status.config_ok) m->ramp_current = AMP_NEUTRAL;
    else m->ramp_current = AmpCore_StepRamp(m->ramp_current, goal, step);
    m->status.target_pwm = m->ramp_current;
    m->status.enable_active = m->command.enable;
    m->status.stopping_active = m->ramp_current != AMP_NEUTRAL && goal == AMP_NEUTRAL;
    m->status.brake_active = hard || m->command.emo || m->command.brake || !m->status.config_ok;
    AmpHw_SetBrake(m->status.brake_active);
    /* Retained EMO semantics: latch brake immediately, do not load PWM. */
    if (!m->command.emo) AmpHw_SetDuty(m->ramp_current);
    m->status.dir_active = m->status.applied_pwm > AMP_NEUTRAL;
    /* Track-power energize (HR_TRACK_POWER). A dedicated, safety-gated clear of the
     * inhibit latch at NEUTRAL PWM only, for occupancy detection: it never supplies a
     * non-neutral command and never bypasses a safety override. It runs AFTER the
     * fault/inhibit/state logic above, so hard / EMO / STOP_REQUEST / lease-expiry /
     * comm-loss / DISABLE have already inhibited; the guard below must therefore also
     * exclude EMO and STOP_REQUEST explicitly (they do not set a fault cause). */
    if (m->status.track_power_requested && m->command.enable &&
        m->status.config_ok && !m->fault.causes && m->status.lease_fresh &&
        !m->command.emo && !m->command.stop_request &&
        m->ramp_current == AMP_NEUTRAL && m->status.applied_pwm == AMP_NEUTRAL) {
        m->status.inhibited = false;
    }
    /* De-energize the bridge once neutral is reached after any inhibit
     * (comm-loss / lease-expiry / STOP_REQUEST / DISABLE): keep the PWM stage
     * switching only while actively ramping down, then drop PWM3EN.
     * hard faults still cut immediately (!hard). */
    AmpHw_Enable(m->status.config_ok &&
                 ((m->command.enable && !m->status.inhibited) || m->status.stopping_active) &&
                 !hard);
    if (m->ramp_current == AMP_NEUTRAL && !m->command.emo) {
        m->fast_stopping = false;
        m->command.stop_request = false;
        m->status.stopping_active = false;
        if (m->status.state == AMP_ST_STOPPING) m->status.state = AMP_ST_READY;
    }
}
