/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 */
#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "modbus/General.h"

/*----------------------------------------------------------------------------*/
#define NUMBER_OF_SLAVES    4                                                   // 0 is not used or for the master self                                                                                      

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
    InitPetitModbus(SlaveInfo);                                                 // Pass address of array of struct for data storage
    
    while(1)
    { 
        //PORTDbits.RD1 = !PORTDbits.RD1;        
        ProcessPetitModbus();
            
        if(PIR4bits.TMR2IF)
        {
            switch (state){
                case 0: if(SendPetitModbus(1, 3, temp3, 4) == False){
                            //printf("SendPetitModbus slave 1 was NOK!!\n\r");
                        }
                        else{
                            //printf("SendPetitModbus slave 1 was OK!!\n\r");
                        }
                        //Led1 ^= 1;
                        state = 1;
                break;
                
                case 1: if(SendPetitModbus(2, 3, temp3, 4) == False){
                            //printf("SendPetitModbus slave 2 was NOK!!\n\r");
                        }
                        else{
                            //printf("SendPetitModbus slave 2 was OK!!\n\r");
                        }
                        state = 2;
                break;
                
                case 2: if(SendPetitModbus(3, 3, temp3, 4) == False){
                            //printf("SendPetitModbus slave 3 was NOK!!\n\r");
                        }
                        else{
                            //printf("SendPetitModbus slave 3 was OK!!\n\r");
                        }
                        state = 0;
                break;
                
                default : state = 0;
                break;                
                
            }

            PIR4bits.TMR2IF = 0;  // reset IF flag if state machine takes too long
            TMR2 = 0;
        }
    }
}