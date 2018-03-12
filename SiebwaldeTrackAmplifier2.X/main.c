/*
 * File:   main.c
 * Author: Jeremy
 *
 * Created on November 24, 2015, 9:46 PM
 */
#include <xc.h>
#include "Peripherals/config.h"
#include "modbus/General.h"
#include <stdlib.h>
#include <stdio.h>


void main(void) {
    // Initialize the device
    SYSTEM_Initialize();
    
    InitPetitModbus(2);
    
    PWM_LoadDutyValue(208);      //416 = 100%
    
    
    while(1)
    {
        PORTCbits.RC1 = !PORTCbits.RC1;
        
        ProcessPetitModbus();
            
        
        if(Timer1_Tick_Counter>1000)
        {
            Timer1_Tick_Counter=0;
            
            PetitRegisters[5].ActValue++;
        }
        
        PetitRegisters[10].ActValue = 12345;
        
    }
}