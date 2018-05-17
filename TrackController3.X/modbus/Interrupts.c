#include "General.h"
#include <p18f97j60.h>			// uProc lib

void PetitModbusIntHandler(void) 
{
    if (PIE2bits.TMR3IE == 1 && PIR2bits.TMR3IF == 1)                           // Data receive timeout (minimum of 3.5 char)
    {        
        PetitModBus_TimerValues();
        PORTBbits.RB5   = 0;//--> led diag
        PIE2bits.TMR3IE = 0;
        PIR2bits.TMR3IF = 0;
    }
    
    if (PIE3bits.TMR4IE == 1 && PIR3bits.TMR4IF == 1)                           // Slave answer timeout
    {        
        SlaveAnswerTimeoutCounter   = 1;
        PORTBbits.RB7               = 0;//--> led diag
        T4CONbits.TMR4ON            = 0;
        PIE3bits.TMR4IE             = 0;
        TMR4                        = 0x00;
        PIR3bits.TMR4IF             = 0;
    }
    
    if( PIE3bits.RC2IE == 1 && PIR3bits.RC2IF == 1)                             // UART2 for modbus, UART1 for diag
    {
        TMR3L             = 0xDA; // --> 225us tick
        TMR3H             = 0xF7; // --> 225us tick
        PIR2bits.TMR3IF   = 0;
        PIE2bits.TMR3IE   = 1;
        PORTBbits.RB5     = 1;//--> led diag
        
        ReceiveInterrupt(RCREG);
        //PIR1bits.RCIF = 0;  
        
        T4CONbits.TMR4ON  = 0;                                                  // Data received stop answer timeout timer
        PIE3bits.TMR4IE   = 0;        
        PORTBbits.RB7     = 0;//--> led diag
    }
}
