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


/*----------------------------------------------------------------------------*/
#define NUMBER_OF_SLAVES    4                                                                                                  

static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES];    

/*----------------------------------------------------------------------------*/
unsigned char temp[4] =  {0, 0, 0, 1};
unsigned char temp2[4] = {0, 0, 0, 0};
unsigned char temp3[4] = {0, 0, 0, 7};
unsigned char temp4[9] = {0, 2, 0, 2, 4, 0x55, 0xAA, 0x50, 0xA0};
unsigned char state = 0;
unsigned int wait = 0, wait2 = 0;

void main(void) {
    // Initialize the device
    SYSTEM_Initialize();

    PORTDbits.RD2 = Off;                                                        // Set Slave micro controllers in reset.
    for(wait = 0xFFFF; wait > 0; wait--){
        for(wait2 = 0x4; wait2 > 0; wait2--);
    }
    PORTDbits.RD2 = On;                                                         // Release Slave micro controllers from reset.
    
    for(wait = 0xFFFF; wait > 0; wait--){
        for(wait2 = 0x4; wait2 > 0; wait2--);
    }
    PORTDbits.RD1 = Off;
        
    InitPetitModbus(SlaveInfo);                                                 // Pass address of array of struct for data storage
    
    while(1)
    { 
        //PORTDbits.RD1 = !PORTDbits.RD1;        
        ProcessPetitModbus();
            
        if(PIR1bits.TMR2IF)
        {
            switch(state){

                case 0 :if(SendPetitModbus(1, 3, temp3, 4)){
                            state += 1;
                        }
                break;

                case 1 :if(SlaveInfo[1].MbCommError == SLAVE_DATA_OK || SlaveInfo[1].MbCommError == SLAVE_DATA_TIMEOUT){
                            state += 1;
                        }
                        else if(SlaveInfo[1].MbCommError == SLAVE_DATA_NOK){
                            //HANG
                        }
                break;

                case 2 :if(SendPetitModbus(2, 3, temp3, 4)){
                            state += 1;
                        }
                break;

                case 3 :if(SlaveInfo[2].MbCommError == SLAVE_DATA_OK || SlaveInfo[1].MbCommError == SLAVE_DATA_TIMEOUT){
                            state += 1;
                        }
                        else if(SlaveInfo[1].MbCommError == SLAVE_DATA_NOK){
                            //HANG
                        }
                break;

                case 4 :if(SendPetitModbus(3, 3, temp3, 4)){
                            state += 1;
                        }
                break;

                case 5 :if(SlaveInfo[3].MbCommError == SLAVE_DATA_OK || SlaveInfo[1].MbCommError == SLAVE_DATA_TIMEOUT){
                            state = 0;
                        }
                        else if(SlaveInfo[1].MbCommError == SLAVE_DATA_NOK){
                            //HANG
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


/*
 case 0: if(SendPetitModbus(1, 6, temp, 4)){
                            state = 1;                        
                        }
                        break;
                    
                case 1: if(SlaveInfo[1].MbCommError == SLAVE_DATA_OK || SlaveInfo[1].MbCommError == SLAVE_DATA_TIMEOUT){
                            state = 12;
                        }
                        else if(SlaveInfo[1].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break;
                        
                case 12: if(SendPetitModbus(1, 3, temp3, 4)){
                            state = 13;
                        }
                        break;
                        
                case 13: if(SlaveInfo[1].MbCommError == SLAVE_DATA_OK || SlaveInfo[1].MbCommError == SLAVE_DATA_TIMEOUT){
                            state = 2;
                        }
                        else if(SlaveInfo[1].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break;  
                        
                case 14: if(SendPetitModbus(1, 16, temp4, 9)){
                            state = 15;
                        }
                        break;
                        
                case 15: if(SlaveInfo[1].MbCommError == SLAVE_DATA_OK){
                            state = 2;
                        }
                        else if(SlaveInfo[1].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break; 
                    
                case 2: if(SendPetitModbus(2, 6, temp, 4)){
                            state = 3;                             
                        }                 
                        break;
                    
                case 3: if(SlaveInfo[2].MbCommError == SLAVE_DATA_OK){
                            state = 4;
                        }
                        else if(SlaveInfo[2].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break;
                    
                case 4: if(SendPetitModbus(3, 6, temp, 4)){
                            state = 5;                             
                        }                 
                        break;
                    
                case 5: if(SlaveInfo[3].MbCommError == SLAVE_DATA_OK){
                            state = 6;
                        }
                        else if(SlaveInfo[3].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break;
                
                case 6: if(SendPetitModbus(1, 6, temp2, 4)){
                            state = 7;
                        }
                        break;
                    
                case 7: if(SlaveInfo[1].MbCommError == SLAVE_DATA_OK || SlaveInfo[1].MbCommError == SLAVE_DATA_TIMEOUT){
                            state = 16;
                        }
                        else if(SlaveInfo[1].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break;
                        
                case 16: if(SendPetitModbus(1, 3, temp3, 4)){
                            state = 17;
                        }
                        break;
                        
                case 17: if(SlaveInfo[1].MbCommError == SLAVE_DATA_OK || SlaveInfo[1].MbCommError == SLAVE_DATA_TIMEOUT){
                            state = 8;
                        }
                        else if(SlaveInfo[1].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break;  
                    
                case 8: if(SendPetitModbus(2, 6, temp2, 4)){
                            state = 9;                             
                        }                 
                        break;
                    
                case 9: if(SlaveInfo[2].MbCommError == SLAVE_DATA_OK){
                            state = 10;
                        }
                        else if(SlaveInfo[2].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break;
                    
                case 10: if(SendPetitModbus(3, 6, temp2, 4)){
                            state = 11;                             
                        }                 
                        break;
                    
                case 11: if(SlaveInfo[3].MbCommError == SLAVE_DATA_OK){
                            state = 0;
                        }
                        else if(SlaveInfo[3].MbCommError == SLAVE_DATA_NOK){
                            //PORTDbits.RD1 = On;
                        }
                        break;
 */