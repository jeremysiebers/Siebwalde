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
#include "modbus/PetitModbus.h"
#include "modbus/PetitModbusPort.h"

unsigned char temp[4] = {0, 0, 0, 1};
unsigned char temp2[4] = {0, 0, 0, 0};
unsigned char state = 0;
unsigned char slave = 2;
unsigned int wait = 0, wait2 = 0;



void main(void) {
    // Initialize the device
    SYSTEM_Initialize();

    PORTDbits.RD2 = Off;                                                        // Set Slave micro controllers in reset.
    for(wait = 0xFFFF; wait > 0; wait--){
        for(wait2 = 0x4; wait2 > 0; wait2--);
    }
    PORTDbits.RD2 = On;                                                         // Release Slave micro controllers from reset.
        
    InitPetitModbus();
    
    while(1)
    { 
        //PORTDbits.RD1 = !PORTDbits.RD1;
        ProcessPetitModbus();

        if(PIR1bits.TMR2IF)
        {
            switch(state){
                case 0: if(SendPetitModbus(1, 6, temp, 4)){
                            state = 1;
                            //PORTDbits.RD1 = On;
                        }
                        break;
                    
                case 1: if(SendPetitModbus(slave, 6, temp, 4)){
                            if (slave > 48){
                                slave = 2;
                                state = 2;
                            }
                            else{
                                slave++;
                            }
                            
                        }
                        break;
                    
                case 2: if(SendPetitModbus(50, 6, temp, 4)){
                            state = 3;                             
                        }                 
                        break;
                    
                case 3: if(SendPetitModbus(1, 6, temp2, 4)){
                            state = 4;
                            //PORTDbits.RD1 = Off;
                        }
                        break;
                    
                case 4: if(SendPetitModbus(slave, 6, temp2, 4)){
                            if (slave > 48){
                                slave = 2;
                                state = 5;
                            }
                            else{
                                slave++;
                            }
                            
                        }
                        break;
                    
                case 5: if(SendPetitModbus(50, 6, temp2, 4)){
                            state = 0;  
                        }                  
                        break;
                    
                default : state = 0;
                    break;
            }
            
            PIR1bits.TMR2IF = 0;  // reset IF flag if state machine takes too long
            TMR2 = 0;
        }
    }
}