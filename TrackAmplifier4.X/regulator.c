#include "regulator.h"
#include "mcc_generated_files/mcc.h"
#include "amp_core.h"

void AmpHw_SetDuty(uint16_t duty)
{
    if (duty == 0u) duty = 1u;
    if (duty > 799u) duty = 799u;
    PWM3_LoadDutyValue(duty);
    amp_model.status.applied_pwm = duty; /* same value, same hardware commit */
}
void AmpHw_SetBrake(bool on) { LM_BRAKE_LAT = on; }
void AmpHw_Enable(bool on) { PWM3EN = on; }

uint16_t REGULATORxINIT(void)
{
    AmpHw_SetDuty(399u);
    AmpHw_SetBrake(true);
    TRISCbits.TRISC4 = 0;
    TRISCbits.TRISC5 = 0;
    TRISCbits.TRISC6 = 0;
    AmpHw_Enable(true);
    LM_PWM_LAT = true;
    return false;
}

/* Compatibility entry point; the central tick gate calls this, never the ADC sequencer. */
uint16_t REGULATORxUPDATE(void)
{
    AmpCore_Update();
    return true;
}
