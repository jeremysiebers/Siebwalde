#include "General.h"
#include <xc.h>

void PetitModbusIntHandler(void) 
{
    if (INTCONbits.TMR0IE == 1 && INTCONbits.TMR0IF == 1) 
    {
        INTCONbits.TMR0IF      =0;        
        TMR0        =0x65;
        
        Timer1_Tick_Counter++;
        
        PetitModBus_TimerValues();
        
        //PORTDbits.RD1 ^= 0x01;
    }
    
    if( PIE1bits.RCIE == 1 && PIR1bits.RCIF == 1)
    {
        ReceiveInterrupt(RCREG);
        PIR1bits.RCIF = 0;
    }
}
