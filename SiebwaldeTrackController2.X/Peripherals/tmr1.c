#include <xc.h>
#include "tmr1.h"

/**
  Section: TMR1 APIs
 */

void TMR1_Initialize(void) {
    TMR1_StopTimer();
    // Set TMR1 to the options selected in the User Interface

    // TMR1ON off; T2CKPS 1:16; T2OUTPS 1:8; 
    
    T1CONbits.T1CKPS = 0b00;

    // TMR1 0x0; 
    TMR1 = 0x00;

    // Clearing IF flag.
    PIR1bits.TMR1IF = 0;
    PIE1bits.TMR1IE = 0;

    // Stop TMR1
    TMR1_StartTimer();
}

void TMR1_StartTimer(void) {
    // Start the Timer by writing to TMRxON bit
    T1CONbits.TMR1ON = 1;
}

void TMR1_StopTimer(void) {
    // Stop the Timer by writing to TMRxON bit
    T1CONbits.TMR1ON = 0;
}

uint8_t TMR1_ReadTimer(void) {
    uint8_t readVal;

    readVal = TMR1;

    return readVal;
}

void TMR1_WriteTimer(uint8_t timerVal) {
    // Write to the Timer2 register
    TMR1 = timerVal;
}

bool TMR1_HasOverflowOccured(void) {
    // check if  overflow has occurred by checking the TMRIF bit
    bool status = PIR1bits.TMR1IF;
    if (status) {
        // Clearing IF flag.
        PIR1bits.TMR1IF = 0;
    }
    return status;
}
/**
  End of File
 */