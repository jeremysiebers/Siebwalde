/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 */


#include <xc.h>
#include <stdint.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "modbus/PetitModbus.h"
#include "processio.h"
#include "regulator.h"

static unsigned int MODBUS_ADDRESS = 0;
static unsigned int LED_TX_prev, LED_RX_prev, LED_ERR_prev, LED_WAR_prev = 0;
static unsigned int LED_TX_STATE, LED_RX_STATE, LED_ERR_STATE, LED_WAR_STATE = 0;
static unsigned int LED_ERR, LED_WAR = 0;
static unsigned int Config = 1;
static unsigned int Startup_Machine = 0;
unsigned int Update_Amplifier = 0;
static unsigned int Sequencer = 0;

static uint8_t test[1000];

/*----------------------------------------------------------------------------*/
void main(void) {
    
    unsigned int result = 0;
    
    SYSTEM_Initialize();
    // If using interrupts in PIC18 High/Low Priority Mode you need to enable the Global High and Low Interrupts
    // If using interrupts in PIC Mid-Range Compatibility Mode you need to enable the Global and Peripheral Interrupts
    // Use the following macros to:
    
    PetitHoldingRegisters[11].ActValue = ReadFlashChecksum();

    // Enable the Global Interrupts
    INTERRUPT_GlobalInterruptEnable();

    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Enable the Peripheral Interrupts
    INTERRUPT_PeripheralInterruptEnable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();
    
    /* Test the onboard Led's */
    //while(Led_Disco() == false);
    
    LED_RUN_LAT     = 0;
    LED_ERR_LAT     = 1;    
    LM_DIR_LAT      = 0;
    LM_PWM_LAT      = 0;
    LM_BRAKE_LAT    = 1;
    
    MODBUS_ADDRESS = 0xAA;                                                      // Address to listen to get configured when ID pin is pulled low 170 dec.    
    InitPetitModbus(MODBUS_ADDRESS);
/*----------------------------------------------------------------------------*/
    
    while(Config){
        
        test[0] = 1;
        
        switch(Startup_Machine){
            case 0 :
                if (ID_PORT == 0){                                              // When ID_PORT is pulled low the amplifier will be configured
                    Startup_Machine = 1;
                }
				LED_ERR_LAT     = 1;
                break;
                
            case 1 :
                result = ProcessPetitModbus();
                LED_ERR++;                
                Led_Blink();
                if (ID_PORT){                                                   // When the ID_PORT is released, check if a new ID is set, otherwise go back to initial state
                    if((PetitHoldingRegisters[2].ActValue & 0x3F)!= 0 ){
                        LED_ERR         = 0; 
                        LED_ERR_LAT     = 0;                        
                        MODBUS_ADDRESS = (PetitHoldingRegisters[2].ActValue & 0x3F);
                        InitPetitModbus(MODBUS_ADDRESS);
                        Startup_Machine = 5;
                   }  
                    else{
                        Startup_Machine = 0;
                        Config          = 1;
                        LED_ERR         = 0;
                        LED_ERR_LAT     = 1;
                        LED_RUN_LAT     = 0;
                        LED_WAR_LAT     = 0;
                        LED_TX_LAT      = 0;
                        LED_RX_LAT      = 0;
                    }
                }
                if(PetitHoldingRegisters[11].ActValue == 0){
                    Startup_Machine = 2;
                    //RESET();                                                    // Called for bootloader invoking
                }
                break;
                
            case 2 :
            {
                if(ProcessPetitModbus() == 2){
                    Startup_Machine = 3;
                }
                break;
            }
            
            case 3 :
            {
                if(ProcessPetitModbus() == 1){
                    RESET();                                                    // Called for bootloader invoking
                }
                break;
            }
                
            case 5 :
                ProcessPetitModbus();
                LED_WAR++;
                Led_Blink();
                if ((PetitHoldingRegisters[1].ActValue & 0x8000)!= 0){          // Wait until the amplifier is enabled by the Master
                    Startup_Machine = 0;
                    Config          = 0;
                    LED_WAR         = 0;
                    LED_WAR_LAT     = 0;
                    LED_RUN_LAT     = 1;
                }
                break;
                
            default: 
                break;
        }
    }
        
    REGULATORxINIT();
/*----------------------------------------------------------------------------*/
    
    while(1){
    
        ProcessPetitModbus();
        Led_Blink();
        
//        test[0]++;
//        
//        if(test[0] > 50){
//            test[0] = 0;
//        }
        
        
        if (Update_Amplifier){
            
            switch(Sequencer){
                case 0:
                    if(MEASURExBMF() == true){
                        Sequencer++;
                        Update_Amplifier = false;  
                    }
                    break;
                    
                case 1:
                    if(REGULATORxUPDATE() == true){
                        Sequencer++;
                        Update_Amplifier = false;
                    }
                    break;
                    
                case 2:
                    if(ADCxIO() == true){
                        Sequencer++;
                        Update_Amplifier = false;
                    }
                    break;
                    
                case 3:
                    Sequencer = 0;
                    Update_Amplifier = false;
                    break;
                    
                default:
                    Sequencer = 0;
                    Update_Amplifier = false;
                    break;
            }            
            
        }
        
    }
}

/*----------------------------------------------------------------------------*/
void Led_Blink (){
    if(PIR4bits.TMR6IF){
        PIR4bits.TMR6IF = 0;
        
        switch(LED_WAR_STATE){
            case 0 : 
                if (LED_WAR > 0){
                    LED_WAR_LAT = 1;
                    LED_WAR_prev = LED_WAR;
                    LED_WAR_STATE = 1;                        
                }
                break;

            case 1 :
                if (LED_WAR == LED_WAR_prev || LED_WAR != LED_WAR_prev){
                    LED_WAR_LAT = 0;
                    LED_WAR_prev = 0;
                    LED_WAR = 0;
                    LED_WAR_STATE = 0;                        
                }
                break;

            default :
                LED_WAR_STATE = 0;
                break;                       
        }
        
        switch(LED_ERR_STATE){
            case 0 : 
                if (LED_ERR > 0){
                    LED_ERR_LAT = 1;
                    LED_ERR_prev = LED_ERR;
                    LED_ERR_STATE = 1;                        
                }
                break;

            case 1 :
                if (LED_ERR == LED_ERR_prev || LED_ERR != LED_ERR_prev){
                    LED_ERR_LAT = 0;
                    LED_ERR_prev = 0;
                    LED_ERR = 0;
                    LED_ERR_STATE = 0;                        
                }
                break;

            default :
                LED_ERR_STATE = 0;
                break;                       
        }
            
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
        //TMR0_Reload();
    }
}
/******************************************************************************
 * Function: Led_Disco()   
 *
 * PreCondition:    
 *
 * Input:           
 *
 * Output:          
 *
 * Side Effects:    
 *
 * Overview:       Test led's onboard 
 *****************************************************************************/
uint8_t     Disco   = 0;
uint8_t     Count   = 0;
uint16_t    Out     = 0;
uint8_t     Loop    = 0;
uint8_t Led_Disco (){
    
    uint8_t Return_Val = false;
    
    if(PIR4bits.TMR6IF){
        PIR4bits.TMR6IF = 0;
        
        Count++;
        if(Count > 5){
           Disco++; 
           Count    = 0;
           Out      = 5;
        }
        
        switch (Disco){
            case 0:
                Led_Convert(Out);
                Out++;
                break;

            case 1:
                Led_Convert(Out);
                Out--;
                break;
                
            case 2:
                Loop++;
                if(Loop > 1){
                    Return_Val = true;
                }
                else{
                    Disco   = 0;
                    Out     = 1;
                    Count   = 0;
                }
                break;

            default :
                break;
        }
    }
    return(Return_Val);
}

/******************************************************************************
 * Function: Led_Convert(uint8_t Number)
 *
 * PreCondition:    
 *
 * Input:           
 *
 * Output:          
 *
 * Side Effects:    
 *
 * Overview:       Test led's onboard 
 *****************************************************************************/
void Led_Convert(uint8_t Number){
    switch (Number){
        case 1:
            LED_RUN_LAT     = 1;
            LED_WAR_LAT     = 0;
            LED_ERR_LAT     = 0; 
            LED_RX_LAT      = 0; 
            LED_TX_LAT      = 0;
            LED_OCC_LAT     = 0;
            break;
        case 2:
            LED_RUN_LAT     = 0;
            LED_WAR_LAT     = 1;
            LED_ERR_LAT     = 0; 
            LED_RX_LAT      = 0; 
            LED_TX_LAT      = 0;
            LED_OCC_LAT     = 0;
            break;
        case 3:
            LED_RUN_LAT     = 0;
            LED_WAR_LAT     = 0;
            LED_ERR_LAT     = 1; 
            LED_RX_LAT      = 0; 
            LED_TX_LAT      = 0;
            LED_OCC_LAT     = 0;
            break;
        case 4:
            LED_RUN_LAT     = 0;
            LED_WAR_LAT     = 0;
            LED_ERR_LAT     = 0; 
            LED_RX_LAT      = 1; 
            LED_TX_LAT      = 0;
            LED_OCC_LAT     = 0;
            break;
        case 5:
            LED_RUN_LAT     = 0;
            LED_WAR_LAT     = 0;
            LED_ERR_LAT     = 0; 
            LED_RX_LAT      = 0; 
            LED_TX_LAT      = 1;
            LED_OCC_LAT     = 1;
            break;
        default:            
            break;
    }
}

/******************************************************************************
 * Function: uint16_t ReadFlashChecksum(){
 *
 * PreCondition:    
 *
 * Input:           
 *
 * Output: read 16 bit flash stored checksum                
 *
 * Side Effects:    
 *
 * Overview:
 *****************************************************************************/
uint16_t  Stored_Checksum;

uint16_t ReadFlashChecksum(){
    TBLPTR = (0x008000 - 2);
    asm("TBLRD *+");
    Stored_Checksum = TABLAT;
    asm("TBLRD *+");
    Stored_Checksum += ((uint16_t)TABLAT) << 8;
    return (Stored_Checksum);
}
/**
 End of File
*/