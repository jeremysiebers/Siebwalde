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

unsigned int MODBUS_ADDRESS = 50;
unsigned int result = 0;

unsigned int LED_TX_prev, LED_RX_prev = 0;
unsigned int LED_TX_STATE, LED_RX_STATE = 0;

void main(void) {
    
    SYSTEM_Initialize();
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
    
    
}