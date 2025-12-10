#include "regulator.h"
#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "modbus/General.h"
#include "processio.h"

static unsigned int PwmDutyCyclePrev = 0;

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
uint16_t REGULATORxUPDATE (){
    
    uint16_t Return_Val = false;
    
    /* EMO stop: kill PWM immediately if EMO bit is set in HR_PWM_COMMAND */
    if (PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_EMO_BIT){        // If EMO command active kill PWM
        LM_BRAKE_LAT = true;
        LED_ERR_LAT = true;
        Return_Val = true;
        return (Return_Val);
    }
    else{
        LM_BRAKE_LAT = false;
        LED_ERR_LAT = false;
    }

    //LM_BRAKE_LAT = PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_BRAKE_BIT;   // load brake from register
        
    if ((PwmDutyCyclePrev != (PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_SETPOINT_MASK))){
        
        if((PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_SETPOINT_MASK) == 0){
            PetitHoldingRegisters[HR_PWM_COMMAND].ActValue |= (HR_PWM_SETPOINT_MASK & 1);
        }
        PWM3_LoadDutyValue(PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_SETPOINT_MASK);     // load duty cycle from register
        PwmDutyCyclePrev = PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_SETPOINT_MASK;
        Return_Val = true;
    }        
    
    return (Return_Val);
}
