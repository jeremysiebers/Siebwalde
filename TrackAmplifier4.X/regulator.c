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
    
    PwmDutyCyclePrev = 0;
    
    PWM3_Initialize();
    PWM3_LoadDutyValue(0);
    
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
        PWM3_LoadDutyValue(0);
        PwmDutyCyclePrev = 0;
        Return_Val = true;
        return (Return_Val);
    }
    
    /* Normal PWM control */
    /*
        // Direction (commented in original code)
        //LM_DIR_LAT =  PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_DIR_BIT;      
        // load direction from register only when single sided PWM
    */
    LM_BRAKE_LAT = PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_BRAKE_BIT;   // load brake from register
        
    if ((PwmDutyCyclePrev != (PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_SETPOINT_MASK))){
        
        PWM3_LoadDutyValue(PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_SETPOINT_MASK);     // load duty cycle from register
        PwmDutyCyclePrev = PetitHoldingRegisters[HR_PWM_COMMAND].ActValue & HR_PWM_SETPOINT_MASK;
        Return_Val = true;
    }        
    
    return (Return_Val);
}
