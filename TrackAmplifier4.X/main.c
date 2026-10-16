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
#include "modbus/General.h"
#include "processio.h"
#include "regulator.h"
#include "amp_core.h"
#include "amp_adapter.h"

static uint8_t MODBUS_ADDRESS = 0;
static uint8_t LED_TX_prev, LED_RX_prev, LED_ERR_prev, LED_WAR_prev = 0;
static uint8_t LED_TX_STATE, LED_RX_STATE, LED_ERR_STATE, LED_WAR_STATE = 0;
static uint8_t LED_ERR, LED_WAR = 0;
static uint8_t Config = 1;
static uint8_t Startup_Machine = 0;
static uint8_t Sequencer = 0;
volatile uint8_t Update_AmplifierTicks = 0;
static uint8_t LastAmplifierTick = 0;
static uint8_t MeasurementTick = 0;
static bool MeasurementPending = false;

static void Amplifier_ServiceTick(void)
{
    /* Atomic byte read, modulo-256 counter retained. Never clear the ISR counter. */
    uint8_t now = Update_AmplifierTicks;
    if (now != LastAmplifierTick) {
        LastAmplifierTick++; /* bounded catch-up, one tick per super-loop pass */
        REGULATORxUPDATE();
        AmpAdapter_Refresh();
        LED_RUN_LAT = amp_model.status.state == AMP_ST_RUN;
        LED_ERR_LAT = amp_model.status.state == AMP_ST_FAULT || amp_model.command.emo ||
            (amp_model.fault.causes & AMP_FAULT_COMMS) != 0;
    }
}

//static uint8_t test = 0xFE;

/*----------------------------------------------------------------------------*/
void main(void) {
    
    uint8_t result = 0;
    
    SYSTEM_Initialize();
    
    /* Store firmware checksum to HoldingReg11 (HR_SW_CHECKSUM) for flash checking */
    AmpCore_Init(ReadFlashChecksum());
    AmpAdapter_Init();

    /* Enable the Global Interrupts */
    INTERRUPT_GlobalInterruptEnable();

    /* Enable the Peripheral Interrupts */
    INTERRUPT_PeripheralInterruptEnable();
 
    /* Initialise LEDs and Modbus address etc. */
    LED_RUN_LAT     = 0;
    LED_ERR_LAT     = 0;
    LED_WAR_LAT     = 0;
    LED_TX_LAT      = 0;
    LED_RX_LAT      = 0;
    
    LM_PWM_LAT      = 0;
    LM_DIR_LAT      = 0;
    LM_BRAKE_LAT    = 1;
    
    MODBUS_ADDRESS = 0xAA;                                  //...listen to get configured when ID pin is pulled low 170 dec.    
    InitPetitModbus(MODBUS_ADDRESS);
    REGULATORxINIT();
/*----------------------------------------------------------------------------*/
//<editor-fold defaultstate="collapsed" desc="CONFIG SECTION">
        while(Config){
            Amplifier_ServiceTick();
            
            switch(Startup_Machine){
                case 0 :
                    if (ID_PORT == 0){                         // ... // When ID_PORT is pulled low the amplifier will be configured
                        Startup_Machine = 1;
                    }
					LED_ERR_LAT     = 1;
                    break;
                    
                case 1 :
                    result = ProcessPetitModbus();
                    LED_ERR++;                
                    Led_Blink();
                    if (ID_PORT){                               //...d, check if a new ID is set, otherwise go back to initial state
                        if(amp_model.address != 0u){
                            LED_ERR         = 0; 
                            LED_ERR_LAT     = 0;                        
                            MODBUS_ADDRESS  = amp_model.address;
                            /* Mark that the ID has been programmed by the master */
                            AmpAdapter_AddressAssigned();
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
                    /* If checksum is zero, go into bootloader invoke sequence */
                    if(amp_model.checksum == 0){
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
                    /* Addressing complete: enter monitoring even before enable/config. */
                    if (amp_model.status.id_set){
                        Startup_Machine = 0;
                        Config          = 0;
                        LED_WAR         = 0;
                        LED_WAR_LAT     = 0;
                        LED_RUN_LAT     = 0;
                    }
                    break;
                    
                default: 
                    break;
            }
        }
    
//</editor-fold>
        
        
//<editor-fold defaultstate="collapsed" desc="RUN SECTION">
        
        while(1){
            Amplifier_ServiceTick();
            
            //PT2_SetHigh();
            ProcessPetitModbus();
            //PT2_SetLow();
            
            Led_Blink();
            
            if (MeasurementPending || Update_AmplifierTicks != MeasurementTick){
                MeasurementPending = true;
                                
                LED_OCC_LAT = CMP1_GetOutputStatus();
                
                switch(Sequencer){
                    case 0:
                        if(MEASURExBMF() == true){
                            Sequencer = 1;
                            MeasurementPending = false;
                            MeasurementTick = Update_AmplifierTicks;
                        }
                        break;
                        
                    case 1:
                        { /* PWM/control runs at loop top, independent of ADC/BEMF progress. */
                            Sequencer = 2;
                            MeasurementPending = false;
                            MeasurementTick = Update_AmplifierTicks;
                        }
                        break;
                        
                    case 2:
                        if(ADCxIO() == true){
                            Sequencer = 0;
                            MeasurementPending = false;
                            MeasurementTick = Update_AmplifierTicks;
                        }
                        break;
                        
                    case 3:
                        Sequencer = 0;
                        MeasurementPending = false;
                        MeasurementTick = Update_AmplifierTicks;
                        break;
                        
                    default:
                        Sequencer = 0;
                        MeasurementPending = false;
                        MeasurementTick = Update_AmplifierTicks;
                        break;
                }            
                
            }
        }
//</editor-fold>
}
/*----------------------------------------------------------------------------*/
//<editor-fold defaultstate="collapsed" desc="LED SECTION">
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
//</editor-fold>

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
