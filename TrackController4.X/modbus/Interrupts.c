#include "../modbus/General.h"
#include <xc.h>
#include "../mcc_generated_files/mcc.h"
/* These are called via the MCC generated standard interrupt routines per peripheral,
 * the default interrupt handler is re-routed to the routines here. 
*/
void PetitModbusIntHandlerTMR(void){
    //PetitModBus_TimerValues();
    PetitModbusTimerValue = 3;                                                  // Between receive interrupts it took to long --> message done
    PIE4bits.TMR3IE = 0;
    PIR4bits.TMR3IF = 0;
}

void PetitModbusIntHandlerSlaveTimeOutTMR(void){
    SlaveAnswerTimeoutCounter   = 1;
    //LATBbits.LATB7              = 0;//--> led diag
    T1CONbits.TMR1ON            = 0;
    PIE4bits.TMR1IE             = 0;
    TMR1H                       = 0x00;
    TMR1L                       = 0x00;
    PIR4bits.TMR1IF             = 0;
}

void PetitModbusIntHandlerRC(void){     
    TMR3_Reload();
    PIR4bits.TMR3IF = 0;
    PIE4bits.TMR3IE = 1;
    ReceiveInterrupt(RCREG);  
    
    T1CONbits.TMR1ON  = 0;                                                      // Data received stop answer timeout timer
    PIE4bits.TMR1IE   = 0;        
    //LATBbits.LATB7    = 0;//--> led diag   
}

/* 
 void PetitModbusIntHandler(void) 
{
    if (PIE2bits.TMR3IE == 1 && PIR2bits.TMR3IF == 1)                           // Data receive timeout (minimum of 3.5 char)
    {        
        PetitModBus_TimerValues();
        LATBbits.LATB5  = 0;//--> led diag
        T3CONbits.TMR3ON= 0;
        PIE2bits.TMR3IE = 0;
        PIR2bits.TMR3IF = 0;
    }
    
    if (PIE3bits.TMR4IE == 1 && PIR3bits.TMR4IF == 1)                           // Slave answer timeout
    {        
        SlaveAnswerTimeoutCounter   = 1;
        LATBbits.LATB7              = 0;//--> led diag
        T4CONbits.TMR4ON            = 0;
        PIE3bits.TMR4IE             = 0;
        TMR4                        = 0x00;
        PIR3bits.TMR4IF             = 0;
    }
    
    if( PIE3bits.RC2IE == 1 && PIR3bits.RC2IF == 1)                             // UART2 for modbus, UART1 for diag
    {
        ReceiveInterrupt(RCREG2);
        PIR3bits.RC2IF    = 0;
        
        T3CONbits.TMR3ON  = 0;
        TMR3L             = 0xDA; // --> 225us tick
        TMR3H             = 0xF7; // --> 225us tick
        PIR2bits.TMR3IF   = 0;
        PIE2bits.TMR3IE   = 1;
        LATBbits.LATB5    = 1;//--> led diag
        T3CONbits.TMR3ON  = 1;
                        
        T4CONbits.TMR4ON  = 0;                                                  // Data received stop answer timeout timer
        PIE3bits.TMR4IE   = 0;        
        LATBbits.LATB7    = 0;//--> led diag        
    }
} 
 */