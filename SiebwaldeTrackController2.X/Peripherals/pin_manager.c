#include <xc.h>
#include "pin_manager.h"

void PIN_MANAGER_Initialize(void) {
    
    ADCON1bits.PCFG = 0b1111; // all digital
    
    TRISA = 0xFF;
    
    TRISB = 0xFF;
 
    TRISC = 0xFF;
    
    TRISD = 0x00;

}
/**
 End of File
 */