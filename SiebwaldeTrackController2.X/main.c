/*
 * File:   main.c
 * Author: Jeremy
 *
 * Created on November 24, 2015, 9:46 PM
 */
#include <xc.h>
#include "Peripherals/config.h"


void main(void) {
    // Initialize the device
    SYSTEM_Initialize();

    
    PORTDbits.RD0 = On;                                                         // Release Slave micro controllers from reset.
    
//    PWM_LoadDutyValue(208);      //416 = 100%
    
    while(1)
    {
        
    }
}
