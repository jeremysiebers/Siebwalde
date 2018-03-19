#include <xc.h>
#include "pin_manager.h"

void PIN_MANAGER_Initialize(void) {
    
    ADCON1bits.PCFG = 0b1111; // all digital
    
    TRISA = 0xFF;
    
    TRISB = 0xFF;
 
    TRISC = 0xFF;
    
    TRISD = 0xFF;
    
    TRISDbits.TRISD0 = 0;//Amplifiers reset
    TRISDbits.TRISD1 = 0;//Led
    TRISDbits.TRISD2 = 0;//Led
    
    TRISCbits.TRISC6 = 0;//TX

}
/**
 End of File
 */