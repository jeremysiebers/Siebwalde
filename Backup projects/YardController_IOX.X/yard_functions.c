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

#define jumpSize 4
#define unusedBV 2U /* Unused outputs */

// local methods
bool ResetHelperFunction(void);
void setLed(const YARDLED *self, LEDSTATE state);
void setOut(const YARDOUTPUT *self, bool state);
uint8_t computeChecksum(const YARDLED *array, size_t size);
void updateIOX(void);
void ExecSelectedFunction(const YARDLED *self, YARDOUTPUT *output);
void executeRules(YARDOUTPUT *output, YARD_LEDS led);

static uint8_t  selector        = 0;
static BLINKOUT blinkout;
static IOXDATA  prevIOX[6];
static uint32_t tStartIOXTime;
static uint32_t tWaitIOXTime;
static uint8_t  IOXupdater      = 0;
static uint8_t  resetSequencer  = 0;

void INITxYARDxFUNCTION(void){
    while(ResetHelperFunction());
   #ifndef DEBUG
   MCP23017xInit(devices, 6); // Initialize 6 MCP23017 devices
   #endif
   
   // Init the blinkout struct (static) hence cannot use compile time values
   blinkout.idle = true;
   blinkout.tStartBlinkTime = GETxMILLIS();
   blinkout.tStartIdleTime  = GETxMILLIS();
   blinkout.tStartResetTime = GETxMILLIS();
   blinkout.tWaitBlinkTime  = tLedBlinkTime;
   blinkout.tWaitIdleTime   = tIdleTimeOut;
   blinkout.tWaitResetTime  = tIOXReset;
   
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

/*
 * Reset the IOX devices for first init or reset to recover from I2C issue
 */

bool ResetHelperFunction(void){
    bool busy = true;
    
    switch(resetSequencer){
        case 0:{
            /* Store the latest data to go out */
            IOX_RESET_SetLow();
            blinkout.tStartResetTime = GETxMILLIS();
            resetSequencer++;
        }
        break;
        
        case 1:{
            if((GETxMILLIS() - blinkout.tStartResetTime) > blinkout.tWaitResetTime){
                IOX_RESET_SetHigh();
                resetSequencer++;
            }
        }
        break;
                
        case 2:{
            #ifndef DEBUG
            MCP23017xInit(devices, 6); // Initialize 6 MCP23017 devices
            #endif
            
            /* Reset the previous data so outputs are re-written */
            for(uint8_t i=0; i<6; i++){
                prevIOX[i].IOXRA = 0;
                prevIOX[i].IOXRB = 0;
            }            
            resetSequencer = 0;
            busy = false;
        }
        break;
                
        default: break;
        
    }
    return(busy);
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
        channel = ASSERT;
    }
    else if(HOTRC_CH6.value){ /* Channel pulses one 1 press 0-1-0 */
        HOTRC_CH6.value = false; /* Reset the value for next press */
        channel = JMP;
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH2L)){ /* Rotate disk */
        if(HOTRC_CH2L.value){
            setOut(&yardOutputArr[DISKL], false);
            setOut(&yardOutputArr[DISKR], false);
        }
        setOut(&yardOutputArr[DISKC], HOTRC_CH2L.value);
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH2R)){ /* Rotate disk */
        if(HOTRC_CH2R.value){
            setOut(&yardOutputArr[DISKL], true);
            setOut(&yardOutputArr[DISKR], true);
        }
        setOut(&yardOutputArr[DISKC], HOTRC_CH2R.value);
    }
    else if(RESET_IOX.value){ /* Check if a reset sequence is required for IOX I2C recovery */
        RESET_IOX.value = ResetHelperFunction();
        return;
    }

    /* Check if button is pressed */
    if(NOF != channel){
        // re-start the idle timer when button is pressed
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
        // Set the led on(true) if state is BLINK it will blink automatically
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
        }
        // Check which button was pressed and jump or activate new function
        switch (channel){
            case NEXT:
            {
                if((ARR_MAX_ELEM(yardLedArr) - unusedBV) == selector){
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
                    selector = (ARR_MAX_ELEM(yardLedArr) - unusedBV);
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
                if((selector + jumpSize) >(ARR_MAX_ELEM(yardLedArr) - unusedBV)){
                    uint8_t delta = (ARR_MAX_ELEM(yardLedArr) - unusedBV) - selector;
                    selector = jumpSize - delta;
                }
                else{
                    selector += jumpSize;
                }
                // Jump JUMPSIZE functions
                led = &yardLedArr[selector];
            }
                break;

            case ASSERT:
            {
                led->funcActivated = !led->funcActivated;
                // led shall show the state
                led->state = (led->funcActivated) ? ON : BLINK;
                blinkout.tStartIdleTime = millis;
                ExecSelectedFunction(led, yardOutputArr);
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

#pragma optimize( "", off )
void setLed(const YARDLED *self, LEDSTATE state)
{
    if (TOGGLE == state){
        state = (*self->portx_ptr & self->pin_mask) ? OFF : ON; // Determine new state
//        if((*self->portx_ptr & self->pin_mask) != 0){
//            state = OFF;
//        }
//        else{
//            state = ON;
//        }
    }
    if(ON == state){
        *self->portx_ptr |= self->pin_mask;
    }
    else if(OFF == state){
        *self->portx_ptr &= !self->pin_mask;
    }
}

void setOut(const YARDOUTPUT *self, bool state)
{
    if (self->invertedLevel) {
        state = !state; // Invert the state if the level is inverted
    }

    if (state) {
        *self->portx_ptr |= self->pin_mask; // Set the bit
    } else {
        *self->portx_ptr &= ~self->pin_mask; // Clear the bit
    }
}
#pragma optimize( "", on )


/*
 * updateIOX()
 * Wrtite to the IO Expander chips 1 at a time
 */
void updateIOX(void){
    uint8_t updatePrev = 0;
    
    if(prevIOX[IOXupdater].IOXRAupdate){
        MCP23017xWritePort(&devices[IOXupdater], 0xA, devices[IOXupdater].byteView.IOXRA);
        prevIOX[IOXupdater].IOXRAupdate = false;
        updatePrev = 1;
    }
    if(prevIOX[IOXupdater].IOXRBupdate){
        MCP23017xWritePort(&devices[IOXupdater], 0xB, devices[IOXupdater].byteView.IOXRB);
        prevIOX[IOXupdater].IOXRBupdate = false;
        updatePrev = 2;
    }    
    
    if(updatePrev == 1){
        /* Store the current state of the device IOX var that was updated */
        prevIOX[IOXupdater].IOXRA = devices[IOXupdater].byteView.IOXRA;
    }
    else if(updatePrev == 2){
        /* Store the current state of the device IOX var that was updated */
        prevIOX[IOXupdater].IOXRB = devices[IOXupdater].byteView.IOXRB;
    }
    
    IOXupdater = (IOXupdater == 5) ? 0 : IOXupdater + 1;
}

/*
 * ExecSelectedFunction(const YARDLED *led, YARDOUTPUT *output)
 * Execute the selected function BV.
 */
void ExecSelectedFunction(const YARDLED *led, YARDOUTPUT *output){
    
    /* When a function is deactivated, only the rail power needs to be
     * disabled. This assumes the first 29 leds to map to the first
     * 29 BV's!!! Filter out those who should be handled diff!!
     */
    if(false == led->funcActivated){
        if(BVLED27 == led->nled){
            // Crane function, stop function, invert crane direction
            setOut(&output[CRENA], false);
//            if(false == output[CRDIR].value){
//                output[CRDIR].value = true;
//            }
            
            output[CRDIR].value = !output[CRDIR].value;
            setOut(&output[CRDIR], output[CRDIR].value);
        }
        else{
            // BV functions
            setOut(&output[led->nled], false);
        }
        return;
    }
    /* Activate function */
    /* Disable all other leds when switching from 1 activated BVLED to another 
     * Filter out those who should not be touched
     */
    for (uint8_t i = 0; i < (ARR_SIZE(yardLedArr)); i++){
        if(BVLED27 == yardLedArr[i].nled){
            // Crane function do not touch!            
        }
        else if(yardLedArr[i].nled != led->nled){
            yardLedArr[i].state = BLINK;
        }
    }    
    // turn off all BV's since a new one is going to be activated
    executeRules(output, BVLEDZERO);
    /* Call the rule table and execute the rules */
    executeRules(output, led->nled);
}

void executeRules(YARDOUTPUT *output, YARD_LEDS led) {
    if (led >= sizeof(ruleTable) / sizeof(ruleTable[0]) || ruleTable[led].rules == NULL) {
        return; // Invalid or undefined LED
    }

    const RuleSet *ruleSet = &ruleTable[led];
    for (size_t i = 0; i < ruleSet->ruleCount; i++) {
        setOut(&output[ruleSet->rules[i].outputIndex], ruleSet->rules[i].state);
    }
}