#include "regulator.h"
#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "modbus/General.h"
#include "processio.h"

static uint16_t PwmDutyCyclePrev = 0;

/*#--------------------------------------------------------------------------#*/
/*  Description: REGULATORxINIT()
 *
 *  Input(s)   : 
 *
 *  Output(s)  :
 *
 *  Returns    :
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : 
 */
/*#--------------------------------------------------------------------------#*/
uint16_t REGULATORxINIT (){
    
    uint16_t Return_Val = false;
    
    PwmDutyCyclePrev = 399;
    
    //PWM3_Initialize();
    PWM3_LoadDutyValue(399);
    
    TRISCbits.TRISC4 = 0;
    TRISCbits.TRISC5 = 0;
    TRISCbits.TRISC6 = 0;
    
    PWM3EN = true;
    LM_PWM_LAT = true;
    PetitHoldingRegisters[HR_PWM_COMMAND].ActValue |= (HR_PWM_SETPOINT_MASK & 399);
    
    return (Return_Val);
}

/*#--------------------------------------------------------------------------#*/
/*  Description: REGULATORxUPDATE()
 *
 *  Input(s)   : 
 *
 *  Output(s)  :
 *
 *  Returns    :
 *
 *  Pre.Cond.  :
 *
 *  Post.Cond. :
 *
 *  Notes      : 
 */
/*#--------------------------------------------------------------------------#*/
uint16_t REGULATORxUPDATE(void)
{
    uint16_t Return_Val = false;

    // Read command once (avoid multiple reads of a shared 16-bit value)
    uint16_t cmd = PetitHoldingRegisters[HR_PWM_COMMAND].ActValue;

    /* EMO stop: kill PWM immediately if EMO bit is set */
    if (cmd & HR_PWM_EMO_BIT) {
        LM_BRAKE_LAT = true;
        LED_ERR_LAT  = true;
        return true;
    }

    LM_BRAKE_LAT = false;
    LED_ERR_LAT  = false;

    uint16_t duty = cmd & HR_PWM_SETPOINT_MASK;

    /* Due to remote timer2 reset pulse (PWM clock sync) value 0 cannot be reached */
    if (duty == 0) duty = 1;              // clamp locally, do NOT modify HR_PWM_COMMAND

    if (PwmDutyCyclePrev != duty) {
        PWM3_LoadDutyValue(duty);
        PwmDutyCyclePrev = duty;
    }

    return true;
}
