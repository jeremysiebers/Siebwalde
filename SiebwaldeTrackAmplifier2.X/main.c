/*
 * File:   main.c
 * Author: Jeremy
 *
 * Created on November 24, 2015, 9:46 PM
 */
#include <xc.h>
#include "Peripherals/config.h"
#include <stdlib.h>
#include <stdio.h>

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();

    PORTCbits.RC1 = 0;
    PORTCbits.RC3 = 0;
    
    PWM_LoadDutyValue(208);      //416 = 100%
    
    while(1)
    {
        PORTCbits.RC3 = !PORTCbits.RC3;
    }
}
