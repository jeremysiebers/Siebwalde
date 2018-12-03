/*
 * File:   newmain.c
 * Author: Jeremy Siebers
 *
 * Created on April 16, 2018, 10:06 PM
 * 
 * orange = TX
 * yellow = RX
 * black = gnd
 */


#include <xc.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "modbus/General.h"

unsigned int MODBUS_ADDRESS = 0;
unsigned int result = 0;

unsigned int LED_TX_prev, LED_RX_prev = 0;
unsigned int LED_TX_STATE, LED_RX_STATE = 0;

unsigned int ActValue_prev = 0;

void main(void) {
    
    SYSTEM_Initialize();
    // If using interrupts in PIC18 High/Low Priority Mode you need to enable the Global High and Low Interrupts
    // If using interrupts in PIC Mid-Range Compatibility Mode you need to enable the Global and Peripheral Interrupts
    // Use the following macros to:

    // Enable the Global Interrupts
    INTERRUPT_GlobalInterruptEnable();

    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Enable the Peripheral Interrupts
    INTERRUPT_PeripheralInterruptEnable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();
    TMR1_StopTimer();
    LED_RUN_LAT     = 0;
    LED_WAR_LAT     = 0;
    LED_ERR_LAT     = 1;    
    LED_TX_LAT      = 0;
    LED_RX_LAT      = 0; 
    
    __delay_ms(10);
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    Get_ID();
    
    InitPetitModbus(MODBUS_ADDRESS);
            
    while(1){
        ProcessPetitModbus();
        
        Set_Amplifier();
        
        Led_Blink();
    }
}

/******************************************************************************
 * Function:        
 *
 * PreCondition:    
 *
 * Input:           
 *
 * Output:          
 *
 * Side Effects:    
 *
 * Overview:        
 *****************************************************************************/
void Get_ID(){
    
    MODBUS_ADDRESS = (unsigned int)50 + (unsigned int)((PORTA & 0xE0)>> 5);
    
    if(MODBUS_ADDRESS > 50 && MODBUS_ADDRESS < 60){
        LED_RUN_LAT = 1;
        LED_ERR_LAT = 0;
    }
    else{
        LED_RUN_LAT = 0;
        LED_ERR_LAT = 1;
        while(1);
    }
}

/******************************************************************************
 * Function:        
 *
 * PreCondition:    
 *
 * Input:           
 *
 * Output:          
 *
 * Side Effects:    
 *
 * Overview:        
 *****************************************************************************/
void Set_Amplifier(){
    
    if (ActValue_prev != PetitHoldingRegisters[0].ActValue){
        ActValue_prev = PetitHoldingRegisters[0].ActValue;
    }
    
    switch(PetitHoldingRegisters[0].ActValue){
        case 0x1   : AMP_ID1_SET_LAT  = 0; break;
        case 0x2   : AMP_ID2_SET_LAT  = 0; break;
        case 0x4   : AMP_ID3_SET_LAT  = 0; break;
        case 0x8   : AMP_ID4_SET_LAT  = 0; break;
        case 0x10  : AMP_ID5_SET_LAT  = 0; break;
        case 0x20  : AMP_ID6_SET_LAT  = 0; break;
        case 0x40  : AMP_ID7_SET_LAT  = 0; break;
        case 0x80  : AMP_ID8_SET_LAT  = 0; break;
        case 0x100 : AMP_ID9_SET_LAT  = 0; break;
        case 0x200 : AMP_ID10_SET_LAT = 0; break;
        default    : AMP_ID1_SET_LAT = AMP_ID2_SET_LAT = AMP_ID3_SET_LAT =
                     AMP_ID4_SET_LAT = AMP_ID5_SET_LAT = AMP_ID6_SET_LAT =
                     AMP_ID7_SET_LAT = AMP_ID8_SET_LAT = AMP_ID9_SET_LAT =
                     AMP_ID10_SET_LAT = 1;
        break;        
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