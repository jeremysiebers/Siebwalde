/* Runs unchanged production core/adapter with a mocked PWM peripheral. */
#include "amp_adapter.h"
#include "amp_core.h"
#include "modbus/General.h"
#include "petit_shim.h"
PetitRegStructure PetitHoldingRegisters[23];
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
static void run(uint16_t pwm) {
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_ENABLE | HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_COMMAND, pwm) == 0);
    tick();
    CHECK(amp_model.status.state == AMP_ST_RUN);
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
    CHECK(PetitHoldingRegisters[HR_FW_VERSION_PATCH_PROTO].ActValue == 0x0001u);
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
    CHECK(write_reg(HR_COMMAND, 0u) == AMP_WRITE_SATURATED);
    for (i = 0; i < 200u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 1u && amp_model.status.requested_pwm == 0u);
    CHECK(PetitHoldingRegisters[HR_COMMAND].ActValue == 0u);
    CHECK(write_reg(HR_COMMAND, 399u) == 0);
    tick(); CHECK(peripheral_duty == 3u); /* normal deceleration, not fast-stop */
    print("PASS: reset/config/validation/HR1/ramps\n");

    reset(); configure(); run(799u);
    CHECK(peripheral_duty == 401u);
    CHECK(PetitHoldingRegisters[HR_COMMAND].ActValue == 799u);
    CHECK(PetitHoldingRegisters[HR_TARGET_PWM].ActValue == 401u);
    CHECK((PetitHoldingRegisters[HR_APPLIED_PWM].ActValue & 0x3FFu) == peripheral_duty);
    for (i = 0u; i < 200u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 799u);
    CHECK(amp_model.status.lease_fresh);
    for (i = 0u; i < 100u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(amp_model.status.state == AMP_ST_LEASE_EXPIRED);
    CHECK(amp_model.status.inhibited && amp_model.fault.causes == AMP_FAULT_LEASE);
    CHECK(write_reg(HR_COMMAND, 799u) == AMP_WRITE_RELATIONSHIP);
    for (i = 0u; i < 50u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 399u && !amp_model.status.stopping_active);
    CHECK(write_reg(HR_COMMAND, 799u) == 0); tick();
    CHECK(peripheral_duty == 399u); /* no renewal, command cannot release */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0); tick();
    CHECK(amp_model.status.inhibited && peripheral_duty == 399u);
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
    for (i = 0u; i < 49u; i++) { AmpAdapter_OnFrame(1u, 3u); tick(); }
    CHECK(peripheral_duty == 399u && amp_model.status.inhibited && !amp_model.status.stopping_active);
    /* 343 ticks = 584.472 ms; contract allows <=2 additional scheduling ticks. */
    print("PASS: lease expires despite FC03 / fast-stop / inhibit / fresh release\n");

    reset(); configure(); run(799u);
    for (i = 1u; i < 147u; i++) tick();
    CHECK(amp_model.status.state == AMP_ST_COMMS_LOST);
    CHECK(amp_model.status.inhibited && amp_model.fault.causes & AMP_FAULT_COMMS);
    for (i = 0u; i < 50u; i++) tick();
    CHECK(peripheral_duty == 399u);
    AmpAdapter_OnFrame(1u, 3u); tick();
    CHECK(amp_model.fault.causes & AMP_FAULT_COMMS); /* communication alone doesn't clear latch */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_CLEAR | HR_CONTROL_LEASE) == 0);
    CHECK(!amp_model.fault.causes && amp_model.status.inhibited);
    CHECK(write_reg(HR_COMMAND, 700u) == 0); tick();
    CHECK(peripheral_duty == 401u);
    CHECK(write_reg(HR_COMMAND, 399u | HR_PWM_STOP_BIT) == 0); tick();
    CHECK(peripheral_duty == 399u && amp_model.status.inhibited);
    CHECK(write_reg(HR_COMMAND, 700u) == 0); tick();
    CHECK(peripheral_duty == 399u); /* renewal must follow stop latch */
    CHECK(write_reg(HR_CONTROL, HR_CONTROL_LEASE) == 0);
    CHECK(write_reg(HR_COMMAND, 700u) == 0); tick();
    CHECK(peripheral_duty == 401u);
    CHECK(write_reg(HR_COMMAND, HR_PWM_EMO_BIT) == 0); tick();
    CHECK(amp_model.status.state == AMP_ST_ESTOP && peripheral_brake && peripheral_duty == 401u);
    CHECK((PetitHoldingRegisters[HR_APPLIED_PWM].ActValue & 0x3FFu) == 401u);
    CHECK(write_reg(HR_COMMAND, 399u) == 0); tick();
    CHECK(peripheral_duty == 399u && amp_model.status.inhibited);
    AmpHw_SetDuty(0u); CHECK(peripheral_duty == 1u && amp_model.status.applied_pwm == 1u);
    AmpHw_SetDuty(1023u); CHECK(peripheral_duty == 799u && amp_model.status.applied_pwm == 799u);
    print("PASS: comm-watchdog/latch/STOP/EMO/applied PWM tracking\n");
    CHECK(test_transport() == 0);
    print("PASS: production FC03/06/16 handlers, broadcast/range/readonly/malformed frames\n");
    CHECK(checks > 260000u);
    ExitProcess(0);
    return 0;
}
