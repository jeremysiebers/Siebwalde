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
void setLed(const YARDLED *self, LEDSTATE state);
uint8_t computeChecksum(const YARDLED *array, size_t size);
void updateIOX(void);

static uint8_t  selector = 0;
static BLINKOUT blinkout;
static IOXDATA  prevIOX[6];
static uint32_t tStartIOXTime;
static uint32_t tWaitIOXTime;

void INITxYARDxFUNCTION(void){
   IOX_RESET_SetLow();
   IOX_RESET_SetHigh();
   //MCP23017_Init(devices, 6); // Initialize 6 MCP23017 devices
   
   // Init the blinkout struct (static) hence cannot use compile time values
   blinkout.idle = true;
   blinkout.tStartBlinkTime = GETxMILLIS();
   blinkout.tStartIdleTime  = GETxMILLIS();
   blinkout.tWaitBlinkTime  = tLedBlinkTime;
   blinkout.tWaitIdleTime   = tIdleTimeOut;
   
   // Init the IOX update time
   tStartIOXTime = GETxMILLIS();
   tWaitIOXTime = tIOXTimeOut;
   
   /* Store the current state of the device IOX var */
    {
        for(uint8_t i=0; i<6; i++){
            prevIOX[i].IOXRA = devices[i].byteView.IOXRA;
            prevIOX[i].IOXRB = devices[i].byteView.IOXRB;
        }
    }
}

void UPDATExYARDxFUNCTIONxSELECTION(void) 
{
    // Set the default channel state
    CHANNEL channel = NOF;
    // Set the current selected Led(function)
    YARDLED* led = &yardLedArr[selector];
    /* Get one time the actual time */
    uint32_t millis = GETxMILLIS();
        
    // Check if a button was pressed
    if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH3)){
        channel = NEXT;        
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH4)){
        channel = PREV;
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH5)){
        channel = JMP;
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH6)){
        channel = ASSERT;
    }    
    if(NOF != channel){
        // re-start the idle timer after a button press
        blinkout.tStartIdleTime = millis;
    }

    // Show the last selected function and do nothing
    if(NOF != channel && blinkout.idle){ 
        if(led->funcActivated){
            led->state = ON;
        }
        else{
            led->state = BLINK;
        }
        //led->enLed = true;
        setLed(led, ON);
        // We are not idle anymore
        blinkout.idle           = false;
        // Start the idle countdown timer
        blinkout.tStartIdleTime = millis;
    }
    else{
        /* Check if a "next" button was pressed, turn off the current selected 
         * function led to lid the next one
         */
        if(NOF != channel && ASSERT != channel){
            setLed(led, OFF);
            //led->enLed = false;
        }
        // Check which button was pressed and jump or activate new function
        switch (channel){
            case NEXT:
            {
                if(ARR_MAX_ELEM(yardLedArr) == selector){
                   selector = 0; 
                }
                else{
                    selector++;            
                }
                // Point to next function
                led = &yardLedArr[selector];
            }
                break;

            case PREV:
            {
                if(0 == selector){
                    selector = ARR_MAX_ELEM(yardLedArr);
                }
                else{
                    selector--;
                }
                // Point to previous function
                led = &yardLedArr[selector];
            }
                break;

            case JMP:
            {
                if((selector + 10) >(ARR_MAX_ELEM(yardLedArr))){
                    uint8_t delta = ARR_MAX_ELEM(yardLedArr) - selector;
                    selector = 10 - delta;
                }
                else{
                    selector+=10;
                }
                // Jump 10 functions
                led = &yardLedArr[selector];
            }
                break;

            case ASSERT:
            {
                led->funcActivated = !led->funcActivated;
                // led shall show the state
                led->state = (led->funcActivated) ? ON : BLINK;
                blinkout.tStartIdleTime = millis;
                //ExecSelectedFunction(selector);
            }
                break;

            default:
                break;
        }
    }
    
    // check if the function is on or off, when off the led needs to blink
    if(BLINK == led->state){
        if((millis - blinkout.tStartBlinkTime) > blinkout.tWaitBlinkTime){
            blinkout.tStartBlinkTime = millis;
            setLed(led, TOGGLE);
        }
    }
    else if(ON == led->state){
        setLed(led, ON);
    }
    else{
        setLed(led, OFF);
    }
    
    // Handle the idle timeout when not idle
    if((millis - blinkout.tStartIdleTime) > blinkout.tWaitIdleTime && !blinkout.idle){
            led->state = OFF;
            blinkout.idle = true;
    }
    
    
    /* Check the current state of the device IOX var to the previous */
    for(uint8_t i=0; i<6; i++){
        //led->state = (led->funcActivated) ? ON : BLINK;
        prevIOX[i].IOXRAupdate = 
                (prevIOX[i].IOXRA == devices[i].byteView.IOXRA) 
                ? false : true;

        prevIOX[i].IOXRBupdate = 
                (prevIOX[i].IOXRB == devices[i].byteView.IOXRB) 
                ? false : true;
    }
    
    /* Activate only 1 IOX port per tWaitIOXTime */
    if((millis - tStartIOXTime) > tWaitIOXTime){
        /* Check if an update to a IOX is needed */
        updateIOX();
        /* start the timer for next check */
        tStartIOXTime = millis;
    }
}

//#pragma optimize( "", off )
void setLed(const YARDLED *self, LEDSTATE state)
{
    if (TOGGLE == state){
        if((*self->portx_ptr & self->pin_mask) != 0){
            state = OFF;
        }
        else{
            state = ON;
        }
    }
    if(ON == state){
        *self->portx_ptr |= self->pin_mask;
    }
    else if(OFF == state){
        *self->portx_ptr &= !self->pin_mask;
    }
}
//#pragma optimize( "", on )


static uint8_t IOXupdater = 0;

void updateIOX(void){
    bool updatePrev = false;
    
    if(prevIOX[IOXupdater].IOXRAupdate){
        //MCP23017xWritePort(&devices[IOXupdater], 0xA, devices[IOXupdater].byteView.IOXRA);
        prevIOX[IOXupdater].IOXRAupdate = false;
        updatePrev = true;
    }
    if(prevIOX[IOXupdater].IOXRBupdate){
        //MCP23017xWritePort(&devices[IOXupdater], 0xB, devices[IOXupdater].byteView.IOXRB);
        prevIOX[IOXupdater].IOXRBupdate = false;
        updatePrev = true;
    }    
    
    if(updatePrev){
        /* Store the current state of the device IOX var */
        for(uint8_t i=0; i<6; i++){
            prevIOX[i].IOXRA = devices[i].byteView.IOXRA;
            prevIOX[i].IOXRB = devices[i].byteView.IOXRB;
        }
    }
    
    IOXupdater = (IOXupdater == 5) ? 0 : IOXupdater + 1;
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