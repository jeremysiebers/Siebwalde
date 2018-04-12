#include "General.h"
#include <xc.h>

void PetitModbusIntHandler(void) 
{
    if (INTCONbits.TMR0IE == 1 && INTCONbits.TMR0IF == 1) 
    {        
        //Timer1_Tick_Counter++;
        
        PetitModBus_TimerValues();
        
        INTCONbits.TMR0IE = 0;
        INTCONbits.TMR0IF = 0; 
        
        SlaveAnswerTimeoutCounter = 0;
        
        //TMR0        = 241; // 101 --> 1kHz/1ms tick, 241 --> 101us tick
        //INTCONbits.TMR0IF      =0;
    }
    
    if( PIE1bits.RCIE == 1 && PIR1bits.RCIF == 1)
    {
        TMR0        = 190; // 190 --> 225us tick
        INTCONbits.TMR0IF = 0;
        INTCONbits.TMR0IE = 1;
        
        ReceiveInterrupt(RCREG);
        //PIR1bits.RCIF = 0;     
        SlaveAnswerTimeoutCounter = 0;
    }
}
