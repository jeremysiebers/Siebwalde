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
#include "modbus/PetitModbus.h"
#include "modbus/PetitModbusPort.h"

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();

    PORTDbits.RD0 = On;                                                         // Release Slave micro controllers from reset.
        
    InitPetitModbus();
    
    while(1)
    {        
        ProcessPetitModbus();
        
        if(Timer1_Tick_Counter>1000)
        {
            Timer1_Tick_Counter=0;
            
            SendPetitModbus(1, 6, "1", 1);
            
        }
    }
}

