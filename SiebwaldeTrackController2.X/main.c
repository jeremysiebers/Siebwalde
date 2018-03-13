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

unsigned char temp[4] = {0, 0, 0, 1};
unsigned char temp2[4] = {0, 0, 0, 0};
unsigned char state = 0;

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();

    PORTDbits.RD0 = Off;                                                        // Set Slave micro controllers in reset.
    PORTDbits.RD0 = On;                                                         // Release Slave micro controllers from reset.
        
    InitPetitModbus();
    
    while(1)
    {        
        ProcessPetitModbus();
        
        if(Timer1_Tick_Counter>300)
        {
            switch(state){
                case 0 : if (SendPetitModbusBusyCheck()){
                    SendPetitModbus(1, 6, temp, 4);
                    state = 1;
                    PORTDbits.RD1 = On;
                }
                    break;
                    
                case 1: if (SendPetitModbusBusyCheck()){
                    SendPetitModbus(2, 6, temp, 4);
                    state = 2;
                }
                    break;
                    
                case 2: if (SendPetitModbusBusyCheck()){
                    SendPetitModbus(3, 6, temp, 4);
                    state = 3;                    
                }
                    break;
                    
                case 3 : if (SendPetitModbusBusyCheck()){
                    SendPetitModbus(1, 6, temp2, 4);
                    state = 4;
                    PORTDbits.RD1 = Off;
                }
                    break;
                    
                case 4: if (SendPetitModbusBusyCheck()){
                    SendPetitModbus(2, 6, temp2, 4);
                    state = 5;
                }
                    break;
                    
                case 5: if (SendPetitModbusBusyCheck()){
                    SendPetitModbus(3, 6, temp2, 4);
                    state = 0;                    
                }
                    break;
                    
                default : state = 0;
                    break;
            }  
            Timer1_Tick_Counter=0;
        }
    }
}

