#include <xc.h>
#include "tmr0.h"
#include "../main.h"

/**
  Section: TMR0 APIs
 */

volatile unsigned int count = 0;

void TMR0_Initialize(void) {
    // Set TMR0 to the options selected in the User Interface
    
    // TMR0 Prescaler (default assigned)
    OPTION_REGbits.PS = 0b111; //1:256
    
    OPTION_REGbits.T0CS = 0;
    
    OPTION_REGbits.T0SE = 0;
    
    OPTION_REGbits.PSA = 0;
    
    // TMR0 0x0; 
    TMR0 = 0x00;

    // Clearing IF flag.
    INTCONbits.TMR0IF = 0;
    
    // Enable interrupt
    INTCONbits.TMR0IE = 1;
    
}

uint8_t TMR0_ReadTimer(void) {
    uint8_t readVal;

    readVal = TMR0;

    return readVal;
}

void TMR0_WriteTimer(uint8_t timerVal) {
    // Write to the Timer2 register
    TMR0 = timerVal;
}

void TMR0_ISR(){
    
    if (count > 50){
        count = 0;
        PORTDbits.RD1 = !PORTDbits.RD1;
    }    
    count++;
    
    Update = 1;
    
    INTCONbits.TMR0IF = 0;
}
/**
  End of File
 */