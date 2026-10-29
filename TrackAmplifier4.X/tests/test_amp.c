/* Runs unchanged production core/adapter with a mocked PWM peripheral. */
#include "amp_adapter.h"
#include "amp_core.h"
#include "modbus/General.h"
#include "petit_shim.h"
PetitRegStructure PetitHoldingRegisters[24];
static uint16_t peripheral_duty;
static bool peripheral_brake, peripheral_enable;
static unsigned checks;
int test_transport(void);
__declspec(dllimport) void __stdcall ExitProcess(unsigned int code);
__declspec(dllimport) void * __stdcall GetStdHandle(unsigned int handle);
__declspec(dllimport) int __stdcall WriteFile(void *, const void *, unsigned int, unsigned int *, void *);
static void print(const char *s) {
    unsigned int n = 0, written;
    while (s[n]) n++;
    WriteFile(GetStdHandle((unsigned int)-11), s, n, &written, 0);
}
#define CHECK(e) do { checks++; if (!(e)) { print("FAIL: " #e "\n"); ExitProcess(1); } } while (0)
void *memset(void *p, int value, unsigned long long size) {
    unsigned char *b = p;
    while (size--) *b++ = (unsigned char)value;
    return p;
}
/* AmpHw_SetDuty below is extracted verbatim from regulator.c by the runner. */
void PWM3_LoadDutyValue(uint16_t duty) { peripheral_duty = duty; }
void AmpHw_SetBrake(bool on) { peripheral_brake = on; }
void AmpHw_Enable(bool on) { peripheral_enable = on; }
/* LED_RUN contract (production main.c): PWM3EN && !amp_model.status.brake_active,
 * observed here as the actually-applied drive (peripheral_enable && !peripheral_brake). */
static bool led_run(void) { return peripheral_enable && !peripheral_brake; }
static uint8_t write_reg(uint16_t addr, uint16_t v) {
    return AmpAdapter_OnWrite(addr, v, false);
}
static void tick(void) { AmpCore_Update(); AmpAdapter_Refresh(); }
static void reset(void) {
    AmpCore_Init(0x1234u); AmpHw_SetDuty(399u); AmpAdapter_Init();
}
static void configure(void) {
    CHECK(write_reg(HR_CFG_RAMP, 0x0202u) == 0);
    CHECK(write_reg(HR_CFG_FAST_STOP, 8u) == 0);
    CHECK(write_reg(HR_CFG_WATCHDOG, 250u) == 0);
    CHECK(write_reg(HR_CFG_TAG, 55u) == 0);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_APPLY) == 0);
    CHECK(amp_model.status.init_done && amp_model.status.config_ok);
}
/* Enable + lease + GROUP_READY: the amp leaves STANDBY and becomes READY. */
static void group_ready_on(void) {
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE | HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == 0);
    CHECK(amp_model.status.group_ready);
}
static void run(uint16_t pwm) {
    group_ready_on();
    CHECK(write_reg(HR_COMMAND, pwm) == 0);
    tick();
    CHECK(amp_model.status.state == AMP_ST_RUN);
}
/* Establish the normal startup end-state the track-power mechanism targets:
 * configured + enabled, lease expired -> inhibit latched, ramp at neutral,
 * bridge de-energized, lease fault present. */
static void inhibit_at_neutral(void) {
    unsigned i;
    reset(); configure(); run(799u);
    for (i = 0u; i < 200u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    for (i = 0u; i < 100u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    for (i = 0u; i < 50u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(amp_model.status.inhibited);
    CHECK(amp_model.fault.causes == AMP_FAULT_LEASE);
    CHECK(peripheral_duty == 399u && !amp_model.status.stopping_active);
    CHECK(!peripheral_enable);
}
/* From inhibited-at-neutral, renew the lease (clears the lease fault) and request
 * track power, then advance one tick: the bridge must energize at neutral. */
static void track_power_on(void) {
    inhibit_at_neutral();
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_TRACK_POWER, TRACK_POWER_ENABLE) == 0);
    tick();
    CHECK(!amp_model.status.inhibited);
    CHECK(peripheral_enable);
    CHECK(peripheral_duty == 399u);
}
int main(void)
{
    unsigned i, j;
    uint16_t previous;
    AmpConfig saved;
    reset();
    CHECK(amp_model.status.state == AMP_ST_NOT_INITIALIZED);
    CHECK(peripheral_duty == 399u && amp_model.status.applied_pwm == 399u);
    CHECK(PetitHoldingRegisters[HR_IDENTITY].ActValue == 0xF101u);
    CHECK(PetitHoldingRegisters[HR_FW_VERSION].ActValue == 0x0100u);
    CHECK(PetitHoldingRegisters[HR_FW_VERSION_PATCH_PROTO].ActValue == 0x0202u);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE | HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_COMMAND, 799u) == 0);
    tick();
    CHECK(peripheral_duty == 399u && peripheral_brake && !peripheral_enable);
    CHECK(amp_model.status.state == AMP_ST_NOT_INITIALIZED);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_APPLY) == AMP_WRITE_RANGE);
    CHECK(amp_model.fault.causes & AMP_FAULT_CONFIG);
    CHECK(!amp_model.status.config_ok);
    CHECK(write_reg(HR_CONTROL, 0u) == 0);
    configure();
    saved = amp_model.active;
    CHECK(write_reg(HR_CFG_RAMP, 0x0002u) == AMP_WRITE_RANGE);
    CHECK(write_reg(HR_CFG_FAST_STOP, 0x0108u) == AMP_WRITE_RANGE);
    CHECK(write_reg(HR_CFG_WATCHDOG, 49u) == AMP_WRITE_RANGE);
    CHECK(write_reg(HR_CFG_WATCHDOG, 60001u) == AMP_WRITE_RANGE);
    CHECK(write_reg(HR_CFG_WATCHDOG, 60000u) == 0);
    CHECK(amp_model.active.watchdog_ms == saved.watchdog_ms);
    /* Invalid pending set tests APPLY's whole-set rejection, not just wire validation. */
    amp_model.pending.accel_step = 0;
    CHECK(write_reg(HR_CONTROL, 0u) == 0);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_APPLY) == AMP_WRITE_RANGE);
    CHECK(amp_model.active.watchdog_ms == saved.watchdog_ms && amp_model.active.accel_step == 2u);
    CHECK(amp_model.pending.watchdog_ms == saved.watchdog_ms && amp_model.pending.accel_step == 2u);
    CHECK(!(amp_model.fault.causes & AMP_FAULT_CONFIG));
    CHECK(PetitHoldingRegisters[HR_WRITE_RESULT].ActValue == 0x8101u);
    for (i = 7u; i < 23u; i++) {
        previous = PetitHoldingRegisters[i].ActValue;
        CHECK(write_reg((uint16_t)i, 0u) == AMP_WRITE_READ_ONLY);
        if (i != HR_WRITE_RESULT) CHECK(PetitHoldingRegisters[i].ActValue == previous);
    }
    CHECK(write_reg(65535u, 0u) == AMP_WRITE_RANGE);
    CHECK(write_reg(HR_COMMAND, 800u) == AMP_WRITE_RANGE);
    CHECK(write_reg(HR_COMMAND, 1023u) == AMP_WRITE_RANGE);
    CHECK(write_reg(HR_COMMAND, 0x2000u | 399u) == AMP_WRITE_RANGE);
    CHECK(write_reg(HR_ADDRESS, 1u) == 0);
    AmpAdapter_AddressAssigned();
    CHECK(write_reg(HR_ADDRESS, 2u) == AMP_WRITE_RELATIONSHIP);

    /* HR1 truth table, edge retries, rejected writes don't re-arm. */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE) == 0);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    CHECK(amp_model.command.enable);
    amp_model.msgs_rx = 4; amp_model.msgs_tx = 5;
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_RESET) == 0);
    CHECK(!amp_model.msgs_rx && !amp_model.msgs_tx);
    amp_model.msgs_rx = 6;
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_RESET) == 0);
    CHECK(amp_model.msgs_rx == 6u);
    CHECK(write_reg(HR_CONTROL, 1u) == AMP_WRITE_RANGE);
    CHECK(amp_model.prev_control_word == HR_CONTROL_RESET);
    CHECK(write_reg(HR_CONTROL, 0x8400u) == AMP_WRITE_RELATIONSHIP);
    CHECK(amp_model.command.enable && amp_model.prev_control_word == HR_CONTROL_RESET);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_RESET) == 0);
    CHECK(amp_model.msgs_rx == 6u);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    CHECK(amp_model.msgs_rx == 6u && amp_model.command.enable);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_RESET) == 0);
    CHECK(!amp_model.msgs_rx);
    CHECK(write_reg(HR_CFG_FAST_STOP, 9u) == 0);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_APPLY) == 0);
    CHECK(amp_model.active.fast_stop_step == 9u);
    CHECK(write_reg(HR_CFG_FAST_STOP, 10u) == 0);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_APPLY) == 0);
    CHECK(amp_model.active.fast_stop_step == 9u);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    CHECK(amp_model.active.fast_stop_step == 9u);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_APPLY) == 0);
    CHECK(amp_model.active.fast_stop_step == 10u);
    amp_model.fault.causes |= AMP_FAULT_COMMS;
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    CHECK(amp_model.fault.causes & AMP_FAULT_COMMS); /* renewal never fires CLEAR */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_CLEAR) == 0);
    CHECK(!(amp_model.fault.causes & AMP_FAULT_COMMS));
    amp_model.fault.causes |= AMP_FAULT_COMMS;
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_CLEAR) == 0);
    CHECK(amp_model.fault.causes & AMP_FAULT_COMMS); /* repeated strobe doesn't fire */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_CLEAR) == 0);
    CHECK(!(amp_model.fault.causes & AMP_FAULT_COMMS));

    /* Exhaustive range/step checks, neutral snap and reversal. */
    for (i = 0u; i <= 1023u; i++) for (j = 1u; j <= 255u; j++) {
        uint16_t duty = AmpCore_StepRamp((uint16_t)i, 399u, (uint8_t)j);
        CHECK(duty >= 1u && duty <= 799u);
        if (i >= 1u && i <= 799u && (i > 399u ? i-399u : 399u-i) <= j) CHECK(duty == 399u);
    }
    CHECK(AmpCore_StepRamp(405u, 1u, 8u) == 399u);
    CHECK(AmpCore_StepRamp(393u, 799u, 8u) == 399u);
    CHECK(AmpCore_StepRamp(399u, 0u, 255u) == 144u);
    CHECK(AmpCore_StepRamp(1u, 0u, 255u) == 1u);
    CHECK(AmpCore_StepRamp(799u, 1023u, 255u) == 799u);
    reset(); configure();
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE | HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == 0);
    CHECK(write_reg(HR_COMMAND, 0u) == AMP_WRITE_SATURATED);
    for (i = 0; i < 200u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 1u && amp_model.status.requested_pwm == 0u);
    CHECK(PetitHoldingRegisters[HR_COMMAND].ActValue == 0u);
    CHECK(write_reg(HR_COMMAND, 399u) == 0);
    tick(); CHECK(peripheral_duty == 3u); /* normal deceleration, not fast-stop */
    CHECK(led_run()); /* normal decel: bridge still switching -> LED_RUN ON */
    print("PASS: reset/config/validation/HR1/ramps\n");

    reset(); configure(); run(799u);
    CHECK(peripheral_duty == 401u);
    CHECK(peripheral_enable); /* normal enabled run: bridge ON */
    CHECK(PetitHoldingRegisters[HR_COMMAND].ActValue == 799u);
    CHECK(PetitHoldingRegisters[HR_TARGET_PWM].ActValue == 401u);
    CHECK((PetitHoldingRegisters[HR_APPLIED_PWM].ActValue & 0x3FFu) == peripheral_duty);
    for (i = 0u; i < 200u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 799u);
    CHECK(amp_model.status.lease_fresh);
    for (i = 0u; i < 100u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(amp_model.status.state == AMP_ST_LEASE_EXPIRED);
    CHECK(amp_model.status.inhibited && amp_model.fault.causes == AMP_FAULT_LEASE);
    CHECK(led_run()); /* lease fast-stop ramp in progress: bridge switching -> LED_RUN ON */
    CHECK(write_reg(HR_COMMAND, 799u) == AMP_WRITE_RELATIONSHIP);
    for (i = 0u; i < 50u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 399u && !amp_model.status.stopping_active);
    CHECK(!peripheral_enable); /* lease fast-stop reached neutral: bridge de-energized */
    CHECK(!led_run()); /* neutral reached: LED_RUN OFF */
    CHECK(write_reg(HR_COMMAND, 799u) == 0); tick();
    CHECK(peripheral_duty == 399u); /* no renewal, command cannot release */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0); tick();
    CHECK(amp_model.status.inhibited && peripheral_duty == 399u);
    CHECK(!peripheral_enable); /* still inhibited at neutral: bridge stays de-energized */
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == 0); /* re-declare ready after lease recovery */
    CHECK(write_reg(HR_COMMAND, 799u) == 0); tick();
    CHECK(!amp_model.status.inhibited && peripheral_duty == 401u);
    /* Default-config bound measured in logical ticks from the last renewal. */
    reset(); configure(); run(799u);
    for (i = 0u; i < 200u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 799u);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    for (i = 0u; i < AMP_LEASE_TICKS - 1u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(amp_model.status.lease_fresh && peripheral_duty == 799u);
    AmpAdapter_OnFrame(1u, 3u); tick();
    CHECK(amp_model.status.state == AMP_ST_LEASE_EXPIRED && peripheral_duty == 791u);
    CHECK(peripheral_enable); /* fast-stop ramp in progress: bridge still switching */
    for (i = 0u; i < 49u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 399u && amp_model.status.inhibited && !amp_model.status.stopping_active);
    CHECK(!peripheral_enable); /* neutral reached: bridge de-energized */
    /* 343 ticks = 584.472 ms; contract allows <=2 additional scheduling ticks. */
    print("PASS: lease expires despite FC03 / fast-stop / inhibit / fresh release\n");

    reset(); configure(); run(799u);
    for (i = 1u; i < 147u; i++) tick();
    CHECK(amp_model.status.state == AMP_ST_COMMS_LOST);
    CHECK(amp_model.status.inhibited && amp_model.fault.causes & AMP_FAULT_COMMS);
    CHECK(led_run()); /* comm-loss fast-stop ramp in progress: bridge switching -> LED_RUN ON */
    for (i = 0u; i < 50u; i++) tick();
    CHECK(peripheral_duty == 399u);
    CHECK(!peripheral_enable); /* comm-loss fast-stop reached neutral: bridge de-energized */
    CHECK(!led_run()); /* neutral reached: LED_RUN OFF */
    AmpAdapter_OnFrame(1u, 3u); tick();
    CHECK(amp_model.fault.causes & AMP_FAULT_COMMS); /* communication alone doesn't clear latch */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_CLEAR | HR_CONTROL_LEASE) == 0);
    CHECK(!amp_model.fault.causes && amp_model.status.inhibited);
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == 0); /* re-declare ready after comm-loss recovery */
    CHECK(write_reg(HR_COMMAND, 700u) == 0); tick();
    CHECK(peripheral_duty == 401u);
    CHECK(write_reg(HR_COMMAND, 399u | HR_PWM_STOP_BIT) == 0); tick();
    CHECK(peripheral_duty == 399u && amp_model.status.inhibited);
    CHECK(write_reg(HR_COMMAND, 700u) == 0); tick();
    CHECK(peripheral_duty == 399u); /* renewal must follow stop latch */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == 0); /* re-declare ready after stop latch */
    CHECK(write_reg(HR_COMMAND, 700u) == 0); tick();
    CHECK(peripheral_duty == 401u);
    CHECK(write_reg(HR_COMMAND, HR_PWM_EMO_BIT) == 0); tick();
    CHECK(amp_model.status.state == AMP_ST_ESTOP && peripheral_brake && peripheral_duty == 401u);
    CHECK(!led_run()); /* EMO -> brake active -> LED_RUN OFF */
    CHECK((PetitHoldingRegisters[HR_APPLIED_PWM].ActValue & 0x3FFu) == 401u);
    CHECK(write_reg(HR_COMMAND, 399u) == 0); tick();
    CHECK(peripheral_duty == 399u && amp_model.status.inhibited);
    AmpHw_SetDuty(0u); CHECK(peripheral_duty == 1u && amp_model.status.applied_pwm == 1u);
    AmpHw_SetDuty(1023u); CHECK(peripheral_duty == 799u && amp_model.status.applied_pwm == 799u);
    print("PASS: comm-watchdog/latch/STOP/EMO/applied PWM tracking\n");

    /* LED_RUN contract: effective H-bridge drive = PWM3EN && !brake_active.
     * The harness observes the actually-applied drive as led_run() =
     * (peripheral_enable && !peripheral_brake). */
    reset(); configure();
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE | HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == 0);
    CHECK(write_reg(HR_COMMAND, 399u) == 0);
    tick();
    CHECK(led_run()); /* enable + neutral -> ON (bridge actively switching) */
    CHECK(write_reg(HR_COMMAND, 799u) == 0);
    tick();
    CHECK(led_run()); /* enable + non-neutral -> ON */
    for (i = 0u; i < 200u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 799u);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_DISABLE) == 0);
    tick();
    CHECK(led_run()); /* DISABLE ramp in progress -> ON */
    for (i = 0u; i < 60u; i++) tick();
    CHECK(!amp_model.status.stopping_active && peripheral_duty == 399u);
    CHECK(!led_run()); /* DISABLE ramp reached neutral -> OFF */
    reset(); configure(); run(799u);
    CHECK(led_run());
    CHECK(write_reg(HR_COMMAND, HR_PWM_BRAKE_BIT | 799u) == 0);
    tick();
    CHECK(!led_run()); /* brake command -> brake_active -> OFF */
    reset(); configure(); run(799u);
    CHECK(led_run());
    amp_model.fault.causes |= AMP_FAULT_HARD;
    tick();
    CHECK(!led_run()); /* hard fault -> brake active + bridge cut -> OFF */
    print("PASS: LED_RUN bridge-drive truth table\n");

    /* Fault-B regression: a DISABLE control write must reflect enable-active
     * cleared + inhibited set in the HR8 mirror immediately, without waiting for
     * the regulator tick. Before the fix, enable-active stayed 1 in the mirror
     * until the next AmpCore_Update(), so the C# DISABLE verify read a stale
     * enable-active=1 (HR8=0xD100) and failed. */
    reset(); configure(); run(799u);
    CHECK(peripheral_enable);
    CHECK((PetitHoldingRegisters[HR_STATE].ActValue & 0x0100u) != 0); /* enable-active set after ENABLE */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_DISABLE) == 0);
    /* No tick() here: the write itself must already clear the mirror bit. */
    CHECK(!amp_model.command.enable);
    CHECK((PetitHoldingRegisters[HR_STATE].ActValue & 0x0100u) == 0); /* enable-active cleared synchronously */
    CHECK((PetitHoldingRegisters[HR_STATE].ActValue & 0x8000u) != 0); /* inhibited set synchronously */
    print("PASS: fault-B DISABLE reflects enable-active/inhibited synchronously\n");

    /* ---- HR_TRACK_POWER: dedicated, safety-gated neutral energize ---- */
    inhibit_at_neutral();
    /* Without track power, a plain lease renewal must NOT energize at neutral. */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    tick();
    CHECK(amp_model.status.inhibited && !peripheral_enable);

    /* (a) track-power request energizes at neutral only once every gate is met. */
    track_power_on();
    CHECK((PetitHoldingRegisters[HR_STATE].ActValue & 0x8000u) == 0u); /* inhibited==0 */
    CHECK(PetitHoldingRegisters[HR_TRACK_POWER].ActValue == TRACK_POWER_ENABLE);
    CHECK(led_run()); /* enable + neutral + !brake -> bridge switching */

    /* Validation: only bits 0..1 allowed; rejected writes do not change the request. */
    CHECK(write_reg(HR_TRACK_POWER, 0x0004u) == AMP_WRITE_RANGE); /* bit 2 reserved */
    CHECK(write_reg(HR_TRACK_POWER, 0x8000u) == AMP_WRITE_RANGE); /* bit 15 reserved */
    CHECK(write_reg(HR_TRACK_POWER, 0xFFFCu) == AMP_WRITE_RANGE); /* bits 2..15 reserved */
    CHECK(amp_model.status.track_power_requested);

    /* (c) clearing the bit de-energizes (re-latch inhibit). */
    CHECK(write_reg(HR_TRACK_POWER, 0u) == 0);
    CHECK(!amp_model.status.track_power_requested);
    CHECK(amp_model.status.inhibited); /* synchronous re-latch in OnWrite */
    tick();
    CHECK(amp_model.status.inhibited && !peripheral_enable);
    CHECK(PetitHoldingRegisters[HR_TRACK_POWER].ActValue == 0u);

    /* Not ready: no APPLY -> track power cannot energize (config_ok gate). */
    reset();
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE | HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_TRACK_POWER, TRACK_POWER_ENABLE) == 0);
    tick();
    CHECK(!peripheral_enable);

    /* (b) hard fault still inhibits even when track power requested. */
    track_power_on();
    amp_model.fault.causes |= AMP_FAULT_HARD;
    tick();
    CHECK(amp_model.status.inhibited && !peripheral_enable);

    /* (b) EMO still inhibits even when track power requested. */
    track_power_on();
    CHECK(write_reg(HR_COMMAND, HR_PWM_EMO_BIT) == 0);
    tick();
    CHECK(amp_model.status.state == AMP_ST_ESTOP);
    CHECK(amp_model.status.inhibited && !peripheral_enable);

    /* (b) DISABLE still inhibits even when track power requested. */
    track_power_on();
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_DISABLE) == 0);
    CHECK(!amp_model.command.enable && amp_model.status.inhibited);
    tick();
    CHECK(amp_model.status.inhibited && !peripheral_enable);

    /* (b) comm loss still inhibits even when track power requested. */
    track_power_on();
    for (i = 0u; i < 147u; i++) tick();
    CHECK(amp_model.status.inhibited && (amp_model.fault.causes & AMP_FAULT_COMMS));
    CHECK(!peripheral_enable);

    /* (b) lease expiry still inhibits even when track power requested. */
    track_power_on();
    for (i = 0u; i < AMP_LEASE_TICKS + 2u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(amp_model.status.inhibited && (amp_model.fault.causes & AMP_FAULT_LEASE));
    CHECK(!peripheral_enable);
    print("PASS: HR_TRACK_POWER neutral energize / safety overrides / de-energize\n");

    /* F5: a safety inhibit drops the track-power request; recovery does NOT
     * re-energize the bridge until a fresh HR_TRACK_POWER=1 write. */
    track_power_on(); /* energized at neutral, track_power_requested == true */
    CHECK(amp_model.status.track_power_requested);
    /* EMO inhibits and drops the request. */
    CHECK(write_reg(HR_COMMAND, HR_PWM_EMO_BIT) == 0);
    tick();
    CHECK(amp_model.status.state == AMP_ST_ESTOP);
    CHECK(amp_model.status.inhibited && !peripheral_enable);
    CHECK(!amp_model.status.track_power_requested); /* F5: request dropped */
    /* Release EMO and renew the lease: the bridge must STAY de-energized. */
    CHECK(write_reg(HR_COMMAND, 399u) == 0);
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    tick();
    CHECK(!amp_model.command.emo);
    CHECK(amp_model.status.inhibited && !peripheral_enable);
    CHECK(!amp_model.status.track_power_requested); /* still dropped after recovery */
    /* A fresh HR_TRACK_POWER=1 write re-energizes. */
    CHECK(write_reg(HR_TRACK_POWER, TRACK_POWER_ENABLE) == 0);
    tick();
    CHECK(amp_model.status.track_power_requested);
    CHECK(!amp_model.status.inhibited && peripheral_enable);
    CHECK(peripheral_duty == 399u);
    print("PASS: HR_TRACK_POWER auto-clear on safety inhibit (F5)\n");

    /* ---- GROUP_READY: C#-issued STANDBY -> READY transition + LED modes ---- */

    /* GROUP_READY reject: config not ok. */
    reset();
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE | HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == AMP_WRITE_RELATIONSHIP);
    CHECK(!amp_model.status.group_ready);

    /* GROUP_READY reject: fault present. */
    reset(); configure();
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE | HR_CONTROL_LEASE) == 0);
    amp_model.fault.causes |= AMP_FAULT_HARD;
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == AMP_WRITE_RELATIONSHIP);
    CHECK(!amp_model.status.group_ready);

    /* STANDBY (configured, inhibited by lease expiry, not group-ready): de-energized,
     * WAR blink, RUN off. */
    inhibit_at_neutral();
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    tick();
    CHECK(amp_model.status.state == AMP_ST_STANDBY);
    CHECK(!amp_model.status.group_ready && !peripheral_enable);
    {
        AmpLedMode rm, wm, em;
        AmpLed_Modes(peripheral_enable, &rm, &wm, &em);
        CHECK(rm == AMP_LED_OFF && wm == AMP_LED_BLINK && em == AMP_LED_OFF);
    }

    /* GROUP_READY accepted -> READY (still de-energized; GROUP_READY never energizes). */
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY) == 0);
    CHECK(amp_model.status.group_ready);
    tick();
    CHECK(amp_model.status.state == AMP_ST_READY);
    CHECK(!peripheral_enable);
    {
        AmpLedMode rm, wm, em;
        AmpLed_Modes(peripheral_enable, &rm, &wm, &em);
        CHECK(rm == AMP_LED_BLINK && wm == AMP_LED_OFF && em == AMP_LED_OFF); /* READY: RUN slow blink */
    }

    /* Clearing GROUP_READY returns to STANDBY. */
    CHECK(write_reg(HR_TRACK_POWER, 0u) == 0);
    CHECK(!amp_model.status.group_ready);
    tick();
    CHECK(amp_model.status.state == AMP_ST_STANDBY);

    /* Energize (track power + keep group ready) -> RUN LED continuous. */
    CHECK(write_reg(HR_TRACK_POWER, GROUP_READY | TRACK_POWER_ENABLE) == 0);
    tick();
    CHECK(peripheral_enable);
    {
        AmpLedMode rm, wm, em;
        AmpLed_Modes(peripheral_enable, &rm, &wm, &em);
        CHECK(rm == AMP_LED_ON && wm == AMP_LED_OFF && em == AMP_LED_OFF);
    }

    /* Fault overrides the normal LED (ERR on, RUN off, WAR blink). */
    amp_model.fault.causes |= AMP_FAULT_HARD;
    tick();
    {
        AmpLedMode rm, wm, em;
        AmpLed_Modes(peripheral_enable, &rm, &wm, &em);
        CHECK(rm == AMP_LED_OFF && wm == AMP_LED_BLINK && em == AMP_LED_ON);
    }

    print("PASS: GROUP_READY STANDBY/READY/RUN + LED modes\n");

    CHECK(test_transport() == 0);
    print("PASS: production FC03/06/16 handlers, broadcast/range/readonly/malformed frames\n");
    CHECK(checks > 260000u);
    ExitProcess(0);
    return 0;
}
