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
// local methods
void ProcessYardFunction(CHANNEL channel);

static uint8_t selector = 0;
static bool    idle = true;


void INITxYARDxFUNCTION(void){
   IOX_RESET_SetLow();
   IOX_RESET_SetHigh();
   MCP23017_Init(devices, 6); // Initialize 6 MCP23017 devices
}

void UPDATExYARDxFUNCTIONxSELECTION(void) 
{
    if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH3)){
        ProcessYardFunction(CH3);
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH4)){
        ProcessYardFunction(CH4);
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH5)){
        ProcessYardFunction(CH5);
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH6)){
        ProcessYardFunction(CH6);
    }
    else{
        ProcessYardFunction(NULL);
    }
    
    WRITExYARDxDEVICExVAR(yardLedArr, ARRAY_SIZE(yardLedArr),
                        LEDS, BVLED1, true);
}

void ProcessYardFunction(CHANNEL channel){
    // Show the last selected function and do nothing
    if(idle){
        WRITExYARDxDEVICExVAR(yardLedArr, ARRAY_SIZE(yardLedArr),
                        LEDS, yardLedArr[selector], true);
        idle = false;
        return;
    }
    // Check which button was pressed
    switch (channel){
        case CH3:
            break;
            
        case CH4:
            break;
            
        case CH5:
            break;
            
        case CH6:
            break;
            
        default:
            break;
    }
    
}





void WRITExYARDxDEVICExVAR(void *self, size_t arrSize, ARRAY_TYPE type, 
        int8_t index, bool value){
    
    if(index < 0 || index >= arrSize){
        return;
    }
    bool temp = value;
    YARDOUTPUT *yardOutput = NULL; // Declare and initialize
    YARDLED *yardLed = NULL;       // Declare and initialize
    
    switch(type){
        case OUTPUTS:
            yardOutput = (YARDOUTPUT *)self;
            if(yardOutput[index].invertedLevel){
                temp = !temp; // Invert the value
            }
            yardOutput[index].value = temp;
            yardOutput[index].valueUpdated = true;
            break;
            
        case LEDS:
            yardLed = (YARDLED *)self;
            yardLed[index].value = temp;
            yardLed[index].valueUpdated = true;
            break;
            
        default: break;
    }    
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