#include "General.h"
#include "Init.h"
#include "../mcc_generated_files/mcc.h"

// #define DIVIDER ((int)(FOSC/(16UL * BAUD_RATE) -1)) --> set in MCC

/* 
 * Peripherals are initialized via MCC generated files/settings.
 * The default interrupt handlers of the peripherals are re-routed here. 
*/

void InitUART(void)
{
    EUSART_SetRxInterruptHandler(PetitModbusIntHandlerRC);
}

void InitTMR(void)
{    
    TMR3_SetInterruptHandler(PetitModbusIntHandlerTMR);                         // MessageTimeoutTimer(e.g. ~4.5 char timeout)
    TMR1_SetInterruptHandler(PetitModbusIntHandlerSlaveTimeOutTMR);             // SlaveAnswerTimeoutTimer (Master required timer)
}