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
    //modbus_sync_LAT = 1;
    SlaveAnswerTimeoutCounter   = 1;
    T1CONbits.TMR1ON            = 0;
    PIE4bits.TMR1IE             = 0;
    PIR4bits.TMR1IF             = 0;
}

void PetitModbusIntHandlerRC(void){  
    TMR3_Reload();
    PIR4bits.TMR3IF   = 0;
    PIE4bits.TMR3IE   = 1;
    T1CONbits.TMR1ON  = 0;                                                      // Data received stop answer timeout timer
    PIE4bits.TMR1IE   = 0;        
    PIR4bits.TMR1IF   = 0;
    ReceiveInterrupt(RCREG);  
}