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

unsigned int MODBUS_ADDRESS = 0;
unsigned int result = 0;

void main(void) {
    
    SYSTEM_Initialize();
    TMR1_StopTimer();
    __delay_ms(1000);
    
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    
    LED_RUN_LAT = 0;
    LED_WAR_LAT = 0;
    LED_ERR_LAT = 1;    
    LED_TX_LAT = 0;
    LED_RX_LAT = 0;
    
    Get_ID_From_AD();
    
    InitPetitModbus(MODBUS_ADDRESS);
            
    while(1){
        ProcessPetitModbus();
        PetitHoldingRegisters[0].ActValue = 0;
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
void Get_ID_From_AD(){
    
    result = ADCC_GetSingleConversion(0);
    
    if(result > 80 && result < 96){
        MODBUS_ADDRESS = 1;
        LED_RUN_LAT = 1;
        LED_ERR_LAT = 0;
    }
    else if(result > 95 && result < 104){
        MODBUS_ADDRESS = 2;
        LED_RUN_LAT = 1;
        LED_ERR_LAT = 0;
    }
    else if(result > 104 && result < 112){
        MODBUS_ADDRESS = 3;
        LED_RUN_LAT = 1;
        LED_ERR_LAT = 0;
    }
    else{
        MODBUS_ADDRESS = 0;
        LED_ERR_LAT = 1;
        while(1){};
    }
}