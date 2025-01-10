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
   //MCP23017_Init(devices, 6); // Initialize 6 MCP23017 devices
}

void UPDATExYARDxFUNCTIONxSELECTION(void) 
{
    // Set the default channel state
    CHANNEL channel = NOF;
    // Set the current selected Led(function)
    YARDLED *led = &yardLedArr[selector];
    /* Get one time the actual time */
    uint32_t millis = GETxMILLIS();
    // put the time in an universal holder since only 1 led will be blinking, handle this not in a time count in every led!
    
    if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH3)){
        //ProcessYardFunction(CH3);
        channel = CH3;
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH4)){
        //ProcessYardFunction(CH4);
        channel = CH4;
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH5)){
        //ProcessYardFunction(CH5);
        channel = CH5;
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH6)){
        //ProcessYardFunction(CH6);
        channel = CH6;
    }
//    else{
//        //ProcessYardFunction(NOF);
//        
//    }
//}

//void ProcessYardFunction(CHANNEL channel){
    // Show the last selected function and do nothing
    if(NOF != channel && idle){        
        led->enLed = true;
        idle = false;
    }
    else{
        // Check if a "next" button was pressed, turn off the current selected function led
        if(NOF != channel && CH6 != channel){
            led->enLed = false;
        }
        // Check which button was pressed and jump or activate new function
        switch (channel){
            case CH3:
            {
                if(ARR_MAX_ELEM(yardLedArr) == selector){
                   selector = 0; 
                }
                else{
                    selector++;            
                }
                *led = yardLedArr[selector];
                led->enLed = true;
            }
                break;

            case CH4:
            {
                if(0 == selector){
                    selector = ARR_MAX_ELEM(yardLedArr);
                }
                else{
                    selector--;
                }
                *led = yardLedArr[selector];
                led->enLed = true;
            }
                break;

            case CH5:
            {
                if((selector + 10) >(ARR_MAX_ELEM(yardLedArr))){
                    uint8_t delta = ARR_MAX_ELEM(yardLedArr) - selector;
                    selector = 10 - delta;
                }
                else{
                    selector+=10;
                }
                *led = yardLedArr[selector]
                led->enLed = true;
            }
                break;

            case CH6:
            {
                // turn off the led
                led->enLed = false;
                idle = true;
                //ExecSelectedFunction(selector);
            }
                break;

            default:
                break;
        }
    }
    
    // check if the function is on or off, when off the led needs to blink
    if(true == led->enLed && BLK == led->state){
        
    }
    
}





void WRITExYARDxDEVICExVAR(void *self, size_t arrSize, ARRAY_TYPE type, 
        int8_t index, void *value){
    
    if(index < 0 || index >= arrSize){
        return;
    }
    
    //YARDOUTPUT *yardOutput = NULL; // Declare and initialize
    //YARDLED *yardLed = NULL;       // Declare and initialize
    
    switch(type){
        case OUTPUTS:
        {
            YARDOUTPUT *yardOutput = (YARDOUTPUT *)self;
            YARDOUTPUT *yardOutputUpdate = (YARDOUTPUT *)value;
            if(yardOutput[index].invertedLevel){
                //temp = !temp; // Invert the value
            }
            //yardOutput[index].value = temp;
            yardOutput[index].valueUpdated = true;
        }
            break;
            
        case LEDS:
        {
            YARDLED *yardLed = (YARDLED *)self;
            YARDLED *yardLedUpdate = (YARDLED *)value;
            //yardLed[index].value = temp;
            //yardLed[index].valueUpdated = true;
        }
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