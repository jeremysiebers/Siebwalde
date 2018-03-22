#include "General.h"
#include <xc.h>

void PetitModbusIntHandler(void) 
{
    if (INTCONbits.TMR0IE == 1 && INTCONbits.TMR0IF == 1) 
    {        
        Timer1_Tick_Counter++;
        
        PetitModBus_TimerValues();
        
        TMR0        = 241; // 101 --> 1kHz/1ms tick, 241 --> 101us tick
        INTCONbits.TMR0IF      =0;
    }
    
    if( PIE1bits.RCIE == 1 && PIR1bits.RCIF == 1)
    {
        ReceiveInterrupt(RCREG);
        PIR1bits.RCIF = 0;        
    }
}
