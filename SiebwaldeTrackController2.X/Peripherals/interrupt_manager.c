#include "interrupt_manager.h"
#include "config.h"

void INTERRUPT_Initialize(void) {
    // If using interrupts in PIC18 High/Low Priority Mode you need to enable the Global High and Low Interrupts
    // If using interrupts in PIC Mid-Range Compatibility Mode you need to enable the Global and Peripheral Interrupts
    // Use the following macros to:

    // Enable the Global Interrupts
    INTERRUPT_GlobalInterruptEnable();

    // Enable the Peripheral Interrupts
    INTERRUPT_PeripheralInterruptEnable();

    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();  
}

void interrupt INTERRUPT_InterruptManager(void) {
    // interrupt handler  
    
//    if (INTCONbits.INT0IE == 1 && INTCONbits.INT0IF == 1){
//        INT0_ISR();
//    }
    
    if (INTCONbits.TMR0IE == 1 && INTCONbits.TMR0IF == 1){
        TMR0_ISR();
    }
    
    if (PIE1bits.RCIE == 1 && PIR1bits.RCIF == 1){
        ausart_Receive_ISR();
    }
    
    if (PIE1bits.TXIE == 1 && PIR1bits.TXIF == 1){
        ausart_Transmit_ISR();
    }
}

/**
 End of File
 */