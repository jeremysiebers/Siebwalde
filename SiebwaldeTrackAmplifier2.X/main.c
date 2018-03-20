/*
 * File:   main.c
 * Author: Jeremy
 *
 * Created on November 24, 2015, 9:46 PM
 */
#include <xc.h>
#include <stdlib.h>
#include <stdio.h>

#include "main.h"
#include "Peripherals/config.h"
#include "modbus/General.h"


#define ADDRESS 1

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();
    
    InitPetitModbus(ADDRESS);
    
    PWM_LoadDutyValue(208);      //416 = 100%
    
    
    while(1)
    {
        PORTCbits.RC3 = PetitRegisters[0].ActValue;
        
        ProcessPetitModbus();
            
        
//        if(Timer1_Tick_Counter>1000)
//        {
//            Timer1_Tick_Counter=0;
//            
//            PetitRegisters[5].ActValue++;
//        }
        
        if (RCSTAbits.OERR && RCSTAbits.CREN){
            RCSTAbits.CREN = 0;
            PORTCbits.RC4 = 1;
        }
        
    }
}