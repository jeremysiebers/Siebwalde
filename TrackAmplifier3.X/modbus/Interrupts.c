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

void PetitModbusIntHandlerRC(void){ 
    
    TMR3_Reload();
    PIR4bits.TMR3IF = 0;
    PIE4bits.TMR3IE = 1;
    ReceiveInterrupt(RCREG);    
}

/*
 
 * if (PIE4bits.TMR3IE == 1 && PIR4bits.TMR3IF == 1)                           // Data receive timeout (minimum of 3.5 char)
    {        
        PetitModBus_TimerValues();
        PORTBbits.RB5   = 0;//--> led diag
        PIE4bits.TMR3IE = 0;
        PIR4bits.TMR3IF = 0;
    }
    
    if (PIE3bits.RCIE == 1 && PIR3bits.RCIF == 1)                               // UART1 for modbus
    {        
        TMR3_Reload();
        PIR4bits.TMR3IF   = 0;
        PIE4bits.TMR3IE   = 1;
        PORTBbits.RB5     = 1;//--> led diag
        
        ReceiveInterrupt(RCREG);
        //PIR1bits.RCIF = 0;  
    }
 
 
 */