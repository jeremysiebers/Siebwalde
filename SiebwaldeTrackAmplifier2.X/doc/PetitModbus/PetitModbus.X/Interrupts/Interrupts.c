#include "General.h"

void interrupt tc_int(void) 
{
    if (TMR1IE && TMR1IF) 
    {
        TMR1IF      =0;
        TMR1H       =0xFD;
        TMR1L       =0x8F;
        
        Timer1_Tick_Counter++;
        
        PetitModBus_TimerValues();
    }
    
    if( RCIE && RCIF)
    {
        ReceiveInterrupt(RCREG);
        RCIF=0;
    }
}
