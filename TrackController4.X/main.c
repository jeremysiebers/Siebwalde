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
#define NUMBER_OF_SLAVES    4                                                   // 0 is for the master self. 5 backplane slaves + 50 Track slaves = 56

static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES];                          // Array of structs holding the data of all the slaves connected  

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
    TX_ENA_LAT = 1;                                                             // Enable TX, for master TX is always enabled.
    //SPI1_setExchangeHandler();
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    while(1)
    { 
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

/*
 * 
 * Data register, bit(s)        Function            	R/W     Critical
 * 
 * -----------------------------RUNNING PARAMETERS------------------------------
 * 
 * Reg1, 0 - 9                  PWM set point       	W       Yes				// new PWM setpoint (speed in 0 - 255 km/h)
 * Reg1, 10                     PWM direction       	W       Yes             // Forward / Backward
 * Reg1, 11                     PWM enable          	W       Yes             // enabling of the H-bridge
 * Reg1, 12                                         	W       Yes
 * Reg1, 13                                         	W       Yes
 * Reg1, 14                                         	W       Yes
 * Reg1, 15                     Emo Stop            	W       Yes				// Slave takes action to stop train as fast as possible
 *                                                  	
 * Reg2, 0                      H-bridge over current 	R       Yes				// When over current is detected
 * Reg2, 10                     Occupied				R       Yes             
 * Reg2, 11                     ThermalFlag				R       Yes   			// H-bridge thermal flag output              
 * Reg2, 12 - 13                Status              	R       Yes				// Run / Warning / Error of the amplifier
 * Reg2, 14                                         	R       Yes
 * Reg2, 15                     		            	R       Yes
 *                                                  	
 * Reg3, 0 - 5                  H-bridge fuse status	R       No              // Voltage over the H-bridge fuse 0 - 63V
 * Reg3, 6 - 14					H-bridge temperature	R		No				// H-bridge temperature 0 - 255 degrees selcius
 * Reg3, 15 
 *                                                  	
 * Reg4, 0 - 9                  Back EMF            	R       No              // Read of back EMF train motor
 * Reg4, 10
 * Reg4, 11
 * Reg4, 12
 * Reg4, 13
 * Reg4, 14
 * Reg4, 15
 *                                                  	
 * Reg5, 0 - 9                  Set BEMF speed      	W       No              // Set value of BEMF, this to allow constant speed regulation
 * Reg5, 10                     Set CSReg             	W       No              // Enable constant speed regulation
 * Reg5, 11 					Clear Amp status		W		No				// Clear amplifier status
 * Reg5, 12
 * Reg5, 13
 * Reg5, 14
 * Reg5, 15
 * 
 * Reg6, 0 - 15                 Amplifier Status        R       No              // Amplifier status list
 *
 * 
 * -----------------------------CONFIG PARAMETERS------------------------------- 
 * 
 * Reg7, 0 - 5					Amplifier ID			W		No				// Amplifier ID for Track amp 1 to 50. Backplane config modules have address 51 to 55 
 * Reg7, 6						Single/Double PWM		W		No				// used in single or double sided PWM operation 0 is dual sided PWM, 1 is single sided PWM
 * Reg7, 7
 * Reg7, 8
 * Reg7, 9
 * Reg7, 10
 * Reg7, 11
 * Reg7, 12
 * Reg7, 13
 * Reg7, 14
 * Reg7, 15
 *
 * Reg8, 0 - 7                  Acceleration par    	W       No				// Acceleration number 0 - 255
 * Reg8, 8 - 15                 Deceleration par    	W       No				// Deceleration number 0 - 255
 *                      
 * 
 */
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 