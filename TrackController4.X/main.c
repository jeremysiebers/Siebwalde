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
 *------------------------------------------------------------------------------
 * Master communication scheme during OP:
 * 
 * 
 *     |Critical|    |Critical|    |INFO    |     
 * ----|MESSAGE1|----|MESSAGE2|----|MESSAGE3|-------
 *     |Reg1 R  |    |Reg1 W  |	   |Reg3 R/W|	
 *     |Reg2 R  |    |Reg4 W  |    |Reg5 R/W| 
 *     |Reg4 R  |				   |Reg6 R/W|	
 *     				   			   |Reg7 R/W|	
 *                                 |Reg8 R/W|	
 *                                 |Reg9 R/W|
 *     
 * Message 3 consists out of a mailbox were info can be R/W of Reg 3,5,6 - 9 or 
 * NOP, a maximum amount of 3 registers should be addressed at once.
 * 
 *------------------------------------------------------------------------------ 
 * 
 * Modbus Track Slave Data Register mapping:
 * 
 * Data register, bit(s)        Function            	R/W     Critical
 * 
 * -----------------------------RUNNING PARAMETERS------------------------------
 * 
 * Reg1, 0 - 7                  PWM set point       	R/W     No/Yes			// new PWM setpoint (speed in 0 - 255 km/h)
 * Reg1, 8						PWM direction       	R/W     No/Yes          // Forward / Backward
 * Reg1, 9                      PWM enable          	R/W     No/Yes          // enabling of the H-bridge
 * Reg1, 10                     
 * Reg1, 11                     
 * Reg1, 12                                         	
 * Reg1, 13                                         	
 * Reg1, 14                                         	
 * Reg1, 15                     Emo Stop            	R/W     No/Yes			// Slave takes action to stop train as fast as possible
 *                                                  	
 * Reg2, 0 - 9                  Back EMF            	R/-     Yes/-           // Read of back EMF train motor                      
 * Reg2, 10                     Occupied				R/-     Yes/-          
 * Reg2, 11                     ThermalFlag				R/-     Yes/-			// H-bridge thermal flag output              
 * Reg2, 12                     H-bridge over current 	R/-     Yes/-			// When over current is detected
 * Reg2, 13 - 15                Led Status           	R/-     Yes/-			// Run / Warning / Error Led
 *                                                  	
 * Reg3, 0 - 4                  H-bridge fuse status	R/-	    No/-            // Voltage H-bridge fuse 0 - 31V
 * Reg3, 5 
 * Reg3, 6 
 * Reg3, 7
 * Reg3, 8 - 15					H-bridge temperature	R/-		No/-			// H-bridge temperature 0 - 255 degrees Celsius
 *                                                  	
 * Reg4, 0 - 9                  Set BEMF speed      	R/W     No/No           // Set value of BEMF, this to allow constant speed regulation
 * Reg4, 10                     Set CSReg             	R/W     No/No           // Enable constant speed regulation
 * Reg4, 11 					Clear Amp status		R/W 	No/No			// Clear amplifier status
 * Reg4, 12
 * Reg4, 13
 * Reg4, 14
 * Reg4, 15						Reset Amplifier			-/W		No/-			// Execute an Amplifier reset().
 * 
 * Reg5, 0 - 15                 Amplifier Status        R/-     No/No           // Amplifier status list + internall temp?
 *
 * 
 * -----------------------------CONFIG PARAMETERS------------------------------- 
 * 
 * Reg6, 0 - 5					Amplifier ID			R/W		No/No           // Amplifier ID for Track amp 1 to 50. Backplane config modules have address 51 to 55 
 * Reg6, 6						Single/Double PWM		R/W		No/No			// used in single or double sided PWM operation 0 is dual sided PWM, 1 is single sided PWM
 * Reg6, 7
 * Reg6, 8
 * Reg6, 9
 * Reg6, 10
 * Reg6, 11
 * Reg6, 12
 * Reg6, 13
 * Reg6, 14
 * Reg6, 15
 *
 * Reg7, 0 - 7                  Acceleration par    	R/W     No/No			// Acceleration number 0 - 255
 * Reg7, 8 - 15                 Deceleration par    	R/W     No/No			// Deceleration number 0 - 255
 *                      
 * Reg8, 0 - 15					Messages Received		R/-		No/-			// Slave register of messages Received to Master
 * Reg9, 0 - 15					Messages Sent			R/-		No/-			// Slave register of messages sent to Master
 *
 *
 *
 *
 *------------------------------------------------------------------------------
 *
 *
 *
 * Modbus Backplane Slave Data Register mapping:
 * 
 * Data register, bit(s)        Function            	R/W     Critical
 * 
 * -----------------------------RUNNING PARAMETERS------------------------------
 * 
 * Reg1, 0                  	Slave 1  enable       	R/W     No/Yes			// Enable Slave 1  for configuring
 * Reg1, 1                  	Slave 2  enable       	R/W     No/Yes			// Enable Slave 2  for configuring 
 * Reg1, 2                  	Slave 3  enable       	R/W     No/Yes			// Enable Slave 3  for configuring 
 * Reg1, 3                  	Slave 4  enable       	R/W     No/Yes			// Enable Slave 4  for configuring 
 * Reg1, 4                  	Slave 5  enable       	R/W     No/Yes			// Enable Slave 5  for configuring 
 * Reg1, 5                  	Slave 6  enable       	R/W     No/Yes			// Enable Slave 6  for configuring 
 * Reg1, 6                  	Slave 7  enable       	R/W     No/Yes			// Enable Slave 7  for configuring 
 * Reg1, 7                  	Slave 8  enable       	R/W     No/Yes			// Enable Slave 8  for configuring 
 * Reg1, 8						Slave 9  enable       	R/W     No/Yes          // Enable Slave 9  for configuring 
 * Reg1, 9                      Slave 10 enable       	R/W     No/Yes          // Enable Slave 10 for configuring 
 * 
 * Reg2, 13 - 15                Led Status           	R/-     Yes/-			// Run / Warning / Error Led
 *
 * Reg4, 11 					Clear ConfigSlave		R/W 	No/No			// Clear amplifier status
 * Reg4, 15						Reset Amplifier			W/-		No/-			// Execute an Amplifier reset().
 *
 * Reg5, 0 - 15                 ConfigSlave Status      R/-     No/No           // ConfigSlave status list + internall temp?
 *
 * Reg6, 0 - 5					ConfigSlave ID			R/-		No/-            // ConfigSlave ID for backplane config modules have address 51 to 55 
 *
 * Reg8, 0 - 15					Messages Received		R/-		No/-			// Slave register of messages Received to Master
 * Reg9, 0 - 15					Messages Sent			R/-		No/-			// Slave register of messages sent to Master
 *
 */
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 