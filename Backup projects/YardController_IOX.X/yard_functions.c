/*
 * File:   yard_functions.c
 * Author: Jeremy Siebers
 *
 * Created on January 5, 2025, 14:44
 */
#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "enums.h"
#include "rand.h"
#include "milisecond_counter.h"
#include "communication.h"
#include "mcp23017.h"
#include "yard_functions.h"

static uint8_t selector = 0;
static bool    idle = true;

void INITxYARDxFUNCTION(void){
   IOX_RESET_SetLow();
   IOX_RESET_SetHigh();
   MCP23017_Init(devices, 6); // Initialize 25 MCP23017 devices
   
   yardOutputArr[0].value = true;
   yardOutputArr[0].valueUpdated = true;
}

void UPDATExYARDxFUNCTIONxSELECTION(void) 
{
    if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH3)){
        
    }
    if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH4)){
        
    }
    if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH5)){
        
    }
    if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH6)){
        
    }
}

void WRITExYARDxDEVICExVAR(YARDOUTPUT self[], size_t size, YARD_FUNCTIONS function, bool value){
    bool temp = value;
    if(self[function].invertedLevel){
        temp = !temp; // Invert the value
    } 
    self[function].value = temp;
    self[function].valueUpdated = true;
}

//void MCP23017_Init(void) {
//    // init the IO Expander ports all outputs
//    for(uint8_t i = 0x21; i<0x27; i++){
//        I2C2_Write1ByteRegister(i, IOCON,  0b00100000);
//        I2C2_Write1ByteRegister(i, IODIRA, 0x00);
//        I2C2_Write1ByteRegister(i, IODIRB, 0x00);    
//        uint8_t data1[2] = {OLATA, 0xFF};
//        I2C2_WriteNBytes(i, data1, 2);
//        uint8_t data2[2] = {OLATB, 0xFF};
//        I2C2_WriteNBytes(i, data2, 2);
//    }    
    
//    I2C2_Write1ByteRegister(MCP23017_ADDR2, IOCON,  0b00100000);
//    I2C2_Write1ByteRegister(MCP23017_ADDR2, IODIRA, 0x00);
//    I2C2_Write1ByteRegister(MCP23017_ADDR2, IODIRB, 0x00);    
//    I2C2_WriteNBytes(MCP23017_ADDR2, data1, 2);
//    I2C2_WriteNBytes(MCP23017_ADDR2, data2, 2);
//    
//    I2C2_Write1ByteRegister(MCP23017_ADDR3, IOCON,  0b00100000);
//    I2C2_Write1ByteRegister(MCP23017_ADDR3, IODIRA, 0x00);
//    I2C2_Write1ByteRegister(MCP23017_ADDR3, IODIRB, 0x00);    
//    I2C2_WriteNBytes(MCP23017_ADDR3, data1, 2);
//    I2C2_WriteNBytes(MCP23017_ADDR3, data2, 2);
//}

//void MCP23017_ToggleOutputs(void) {
//    TP4_SetHigh();
//    if(toggle == true){
//        //uint8_t data1[2] = {OLATA, 0xFF};
//        //uint8_t data2[2] = {OLATB, 0xFF};
//        uint8_t data1[3] = {OLATA, 0xAA, 0xAA};
//        //I2C2_Write1ByteRegister(MCP23017_ADDR, OLATA, 0);
////        I2C2_WriteNBytes(MCP23017_ADDR1, data1, 3);
////        I2C2_WriteNBytes(MCP23017_ADDR2, data1, 3);
////        I2C2_WriteNBytes(MCP23017_ADDR3, data1, 3);
//        //I2C2_WriteNBytes(MCP23017_ADDR, data2, 2);
//        //I2C2_Write1ByteRegister(MCP23017_ADDR, OLATB, 0x00);
//        toggle = false;
//        
//        for(uint8_t i = 0x21; i<0x27; i++){
//            I2C2_WriteNBytes(i, data1, 3);
//        }
//        
//    }
//    else{
//        //uint8_t data1[2] = {OLATA, 0x00};
//        //uint8_t data2[2] = {OLATB, 0x00};
//        uint8_t data1[3] = {OLATA, 0x55, 0x55};
//        //I2C2_Write1ByteRegister(MCP23017_ADDR, OLATA, 0);
////        I2C2_WriteNBytes(MCP23017_ADDR1, data1, 3);
////        I2C2_WriteNBytes(MCP23017_ADDR2, data1, 3);
////        I2C2_WriteNBytes(MCP23017_ADDR3, data1, 3);
//        //I2C2_WriteNBytes(MCP23017_ADDR, data2, 2);
//        //I2C2_Write1ByteRegister(MCP23017_ADDR, OLATB, 0xFF);
//        toggle = true;
//        
//        for(uint8_t i = 0x21; i<0x27; i++){
//            I2C2_WriteNBytes(i, data1, 3);
//        }
//    }
//    TP4_SetLow();
//}