#include "General.h"
#include "Init.h"
#include "../mcc_generated_files/mcc.h"

#define DIVIDER ((int)(FOSC/(16UL * BAUD_RATE) -1))

void InitUART(void)
{
    EUSART_SetRxInterruptHandler(PetitModbusIntHandlerRC);
}

void InitTMR(void)
{    
    TMR3_SetInterruptHandler(PetitModbusIntHandlerTMR);                         // MessageTimeoutTimer(e.g. ~4.5 char timeout)
}