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
void REGULATORxINIT(){
    LM_DIR_LAT      = 0;
    LM_PWM_LAT      = 1;
    LM_BRAKE_LAT    = 0;
    PWM3CON         = 0x80;
    TRISCbits.TRISC4 = 0;
    TRISCbits.TRISC5 = 0;
    TRISCbits.TRISC6 = 0;
    
    PwmDutyCyclePrev = ((unsigned int)(PWM3DCH << 2)) + 
                       ((unsigned int)(PWM3DCL >> 6));    
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

unsigned int REGULATORxUPDATE(){
    
    unsigned int Return_Val = true;
    
    /*
    if (PetitHoldingRegisters[0].ActValue & 0x8000){                            // If EMO command active kill PWM
        PWM3CON         = 0x00;
        LM_DIR_LAT      = 0;
        LM_PWM_LAT      = 0;
        LM_BRAKE_LAT    = 1;        
    }
    else if (0 == PWM3CON){                                                     // When no EMO, check if PWM is initialized
        REGULATORxINIT();                                                       // Init PWM if not initialized
    }
    else{
    
        //LM_DIR_LAT =  PetitHoldingRegisters[0].ActValue & 0x0400;               // load direction from register only when single sided PWM
        LM_BRAKE_LAT = PetitHoldingRegisters[0].ActValue & 0x0800;              // load brake from register
        
        if ((PwmDutyCyclePrev != PetitHoldingRegisters[0].ActValue & 0x03FF)){
            PWM3_LoadDutyValue(PetitHoldingRegisters[0].ActValue & 0x03FF);     // load duty cycle from register
            PwmDutyCyclePrev = PetitHoldingRegisters[0].ActValue & 0x03FF;
        }        
    }*/
    
    
    
    
    return (Return_Val);
}