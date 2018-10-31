/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 */
#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "modbus/Interrupts.h"
#include "modbus/PetitModbus.h"
#include "commhandler.h"

#define ALLxSLAVESxDATA ((unsigned int)((NUMBER_OF_SLAVES-1)*3) * 4)

typedef enum
{
    OK          = 0x02, 
    NOK         = 0x04, 
    BUSY        = 0x01,
    IDLE        = 0x00,
    MODE        = 0x01,
    WRITE       = 0x02,
    HOLDINGREG  = 0x04,
    INPUTREG    = 0x08,
    DIAGREG     = 0x10,
    EXEC        = 0x20
};

/*----------------------------------------------------------------------------*/

static SLAVE_INFO         SlaveInfo[NUMBER_OF_SLAVES];                          // Array of structs holding the data of all the slaves connected  

/*----------------------------------------------------------------------------*/

unsigned int LED_TX_prev, LED_RX_prev = 0;
unsigned int LED_TX_STATE, LED_RX_STATE = 0;
unsigned int UpdateNextSlave = false;
unsigned int AllSlavesReadAllDataCounter = 1;

unsigned int CommandMachine = 0;
unsigned char HoldingRegisterWrite[7] = {0, 0, 0, 1, 2, 0, 0};                  // {start address High, start address Low, 
                                                                                // number of registers High, number of registers Low, 
                                                                                // byte count, Register Value Hi, Register Value Lo} 

void main(void) {
    // Initialize the SLAVE_INFO struct with slave numbers
    for (char i = 0; i <NUMBER_OF_SLAVES; i++){
        SlaveInfo[i].SlaveNumber = i;
        SlaveInfo[i].Header = 0xAA;
        SlaveInfo[i].Footer = 0x55;
    }
    
    // Initialize the device
    SYSTEM_Initialize();
    TMR1_StopTimer();                                                           // prevent timer1 from setting slave timeout to 1.
    
    LED_RUN_LAT = 0;
    LED_WAR_LAT = 0;
    LED_ERR_LAT = 1;    
    LED_TX_LAT = 0;
    LED_RX_LAT = 0;
    
    __delay_ms(50);                                                            // Wait longer then the slaves (1000ms)
    
    TMR0_StartTimer();                                                          // TX/RX led timer
    
    InitPetitModbus(SlaveInfo);                                                 // Pass address of array of struct for data storage
    InitSlaveCommunication(SlaveInfo);                                          // Pass address of array of struct for data storage
    TX_ENA_LAT = 1;                                                             // Enable TX, master TX is always enabled.
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    LED_RUN_LAT = 1;
    LED_WAR_LAT = 0;
    LED_ERR_LAT = 0;
    
    modbus_sync_LAT = 0;
    
    while(1)
    {
        switch((SlaveInfo[0].HoldingReg[0] & 0x01)){
            case 0:                
                switch(CommandMachine){
                    case 0:
                        if (SlaveInfo[0].HoldingReg[0] & EXEC){                 // if execute is set
                                                        
                            if((SlaveInfo[0].HoldingReg[0] & 0x1C) == HOLDINGREG){ 
                                if(SlaveInfo[0].HoldingReg[0] & WRITE){          // if write is set
                                    SlaveInfo[0].InputReg[0] = BUSY;
                                    CommandMachine = 10;
                                }
                                else{
                                    SlaveInfo[0].InputReg[0] = NOK;   
                                    CommandMachine = 50;
                                }                                
                            }
                            else if((SlaveInfo[0].HoldingReg[0] & 0x1C) == INPUTREG){
                                if(SlaveInfo[0].HoldingReg[0] & WRITE){          // if write is set
                                    SlaveInfo[0].InputReg[0] = NOK;
                                    CommandMachine = 50;
                                }
                                else{
                                    SlaveInfo[0].InputReg[0] = BUSY;
                                    CommandMachine = 20;
                                }
                            }
                            else if((SlaveInfo[0].HoldingReg[0] & 0x1C) == DIAGREG){
                                if(SlaveInfo[0].HoldingReg[0] & WRITE){          // if write is set
                                    SlaveInfo[0].InputReg[0] = NOK;
                                    CommandMachine = 50;
                                }
                                else{
                                    SlaveInfo[0].InputReg[0] = BUSY;
                                    CommandMachine = 30;
                                }
                            }                            
                        }
                        break;
 //----------------------------------------------------------------------------------------------------------------------//                              
                    case 10:
                        HoldingRegisterWrite[1] = SlaveInfo[0].HoldingReg[3];      // Register address to write to
                        HoldingRegisterWrite[6] = SlaveInfo[0].HoldingReg[2];      // low char data
                        HoldingRegisterWrite[5] = SlaveInfo[0].HoldingReg[2]>> 8;  // high char data
                        SendPetitModbus(SlaveInfo[0].HoldingReg[1], PETITMODBUS_WRITE_MULTIPLE_REGISTERS, HoldingRegisterWrite, 7);
                        CommandMachine = 40;
                        break;
                        
                    
                        
//----------------------------------------------------------------------------------------------------------------------//

//----------------------------------------------------------------------------------------------------------------------//

//----------------------------------------------------------------------------------------------------------------------//                        
                    case 40:
                        SlaveInfo[0].InputReg[0] = OK;
                        CommandMachine = 50;                        
                        break;
                        
                    case 50:
                        if ((SlaveInfo[0].HoldingReg[0] & EXEC) == 0){          // Remove execute command before returning
                            CommandMachine = 0; 
                            SlaveInfo[0].InputReg[0] = IDLE;                    // reset status register for readback of execution towards ethernet target
                        }
                        break;
                        
                    default :
                        break;
                }
                break;
                
 //----------------------------------------------------------------------------------------------------------------------//
                
            case 1:
                if (UpdateNextSlave == true){
                    ProcessNextSlave();
                    UpdateNextSlave = false;
                    AllSlavesReadAllDataCounter++;
                    if (AllSlavesReadAllDataCounter > ALLxSLAVESxDATA){
                        AllSlavesReadAllDataCounter = 1;
                    }
                }
                break;
                
            default :
                break;
        }
        
        ProcessPetitModbus();
        
        if (ProcessSlaveCommunication() == true){                       // check if modbus communication is running/finished/failed
            //modbus_sync_LAT = 1;                        
        }
         
        Led_Blink();
    }
}

void Led_Blink (){
    if(PIR0bits.TMR0IF){
            
        switch(LED_TX_STATE){
            case 0 : 
                if (LED_TX > 0){
                    LED_TX_LAT = 1;
                    LED_TX_prev = LED_TX;
                    LED_TX_STATE = 1;
                }
                break;

            case 1 :
                if (LED_TX == LED_TX_prev || LED_TX != LED_TX_prev){
                    LED_TX_LAT = 0;
                    LED_TX_prev = 0;
                    LED_TX = 0;
                    LED_TX_STATE = 0;
                }
                break;

            default :
                LED_TX_STATE = 0;
                break;                       
        }

        switch(LED_RX_STATE){
            case 0 : 
                if (LED_RX > 0){
                    LED_RX_LAT = 1;
                    LED_RX_prev = LED_RX;
                    LED_RX_STATE = 1;
                }
                break;

            case 1 :
                if (LED_RX == LED_RX_prev || LED_RX != LED_RX_prev){
                    LED_RX_LAT = 0;
                    LED_RX_prev = 0;
                    LED_RX = 0;
                    LED_RX_STATE = 0;
                }
                break;

            default :
                LED_RX_STATE = 0;
                break;                       
        }
        PIR0bits.TMR0IF = 0;
        TMR0_Reload();
    }
}