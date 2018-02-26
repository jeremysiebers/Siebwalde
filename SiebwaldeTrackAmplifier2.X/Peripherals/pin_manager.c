#include <xc.h>
#include "pin_manager.h"

void PIN_MANAGER_Initialize(void) {
    
    ADCON1bits.PCFG = 0b1111; // all digital
    
    TRISA = 0xFF;
    
    TRISB = 0xFF;
 
    TRISC = 0xFF;
    
    TRISCbits.TRISC1 = 0;//Led
    TRISCbits.TRISC3 = 0;//Led
    TRISCbits.TRISC2 = 0;//PWM
    TRISCbits.TRISC6 = 0;//TX

}
/**
 End of File
 */