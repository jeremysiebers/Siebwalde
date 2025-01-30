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

//#define jumpSize 4
//#define unusedBV 2U /* Unused outputs */

// local methods
bool ResetHelperFunction(void);
void setLed(const YARDLED *self, LEDSTATE state);
void setOut(const YARDOUTPUT *self, bool state);
uint8_t computeChecksum(const YARDLED *array, size_t size);
void updateIOX(void);
void ExecSelectedFunction(const YARDLED *self, YARDOUTPUT *output);
void executeRules(YARDOUTPUT *output, YARD_LEDS led);

static uint8_t  selector        = 0;
static jmpGroups  jmpSelector   = jmpGrp1;
static TIMERS   timers;
static IOXDATA  prevIOX[6];
static uint8_t  IOXupdater      = 0;
static uint8_t  resetSequencer  = 0;

/*
 * INITxYARDxFUNCTION()
 * The main Yard updater init function called by main program
 */
void INITxYARDxFUNCTION(void){
    /* Call the reset function */
    while(ResetHelperFunction());
   
    /* Get one time the actual time */
    uint32_t millis = GETxMILLIS();
    
   // Init the blinkout struct (static) hence cannot use compile time values
   timers.idle = true;
   timers.tStartBlinkTime = millis;
   timers.tStartIdleTime  = millis;
   timers.tStartResetTime = millis;
   timers.tStartIOXTime   = millis;
   timers.tWaitBlinkTime  = tLedBlinkTime;
   timers.tWaitIdleTime   = tIdleTimeOut;
   timers.tWaitResetTime  = tIOXReset;
   timers.tWaitIOXTime    = tIOXTimeOut;
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
            timers.tStartResetTime = GETxMILLIS();
            resetSequencer = 1;
        }
        break;
        
        case 1:{
            if((GETxMILLIS() - timers.tStartResetTime) > timers.tWaitResetTime){
                IOX_RESET_SetHigh();
                resetSequencer = 2;
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
            
            /* 
             * Initialize functions that need to be enabled and active from the 
             * beginning.
             */
            YARDLED* tmp = &yardLedArr[BVLED26];
            tmp->funcActivated = true;
            tmp->state = ON;
            //setLed(tmp, ON); do not set the actual port output on!
            executeRules(yardOutputArr, BVLEDINIT); 
            
            resetSequencer = 0;
            busy = false;
        }
        break;
                
        default: break;        
    }
    return(busy);
}
/*
 * UPDATExYARDxFUNCTIONxSELECTION()
 * The main Yard updater function called by main program
 */
void UPDATExYARDxFUNCTIONxSELECTION(void) 
{
    // Set the default channel state
    CHANNEL channel = NOF;    
    /* Get one time the actual time */
    uint32_t millis = GETxMILLIS();
    /* Create const pointer to the group table */
    const GroupSet *groupSet = &groupTable[jmpSelector];
    // Set the current selected Led(function)
    YARDLED* led = &yardLedArr[groupSet->groups[selector].groupIndex];    
        
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
        /* If activated by RC controller */
        if(HOTRC_CH2L.value){
            /* Let the motor turn anti-clock wise */
            setOut(&yardOutputArr[DISKL], false);
            setOut(&yardOutputArr[DISKR], false);
        }
        /* Activate or de-activate the coil depending on the RC controller
         * letting the stick go will make the coil go back(off) and stop the 
         * motor on the next track at the rotate disk.
         */
        setOut(&yardOutputArr[DISKC], HOTRC_CH2L.value);
    }
    else if(DEBOUNCExGETxVALUExUPDATEDxSTATE(&HOTRC_CH2R)){ /* Rotate disk */
        /* If activated by RC controller */
        if(HOTRC_CH2R.value){
            /* Let the motor turn clock wise */
            setOut(&yardOutputArr[DISKL], true);
            setOut(&yardOutputArr[DISKR], true);
        }
        /* Activate or de-activate the coil depending on the RC controller
         * letting the stick go will make the coil go back(off) and stop the 
         * motor on the next track at the rotate disk.
         */
        setOut(&yardOutputArr[DISKC], HOTRC_CH2R.value);
    }
    /* Check if a reset sequence is required for manual IOX I2C recovery */
    else if(RESET_IOX.value){ 
        /* Keep calling the reset helper function until done */
        RESET_IOX.value = ResetHelperFunction();
        /* go back to main as long as the reset is not done */
        return;
    }

    /* Check if button is pressed */
    if(NOF != channel){
        // re-start the idle timer when button is pressed
        timers.tStartIdleTime = millis;
    }

    // Show the last selected function and do nothing (wakeup from "sleep")
    if(NOF != channel && timers.idle){ 
        if(true == led->funcActivated){
            led->state = ON;
        }
        else{
            led->state = BLINK;
        }
        // Set the led on(true) if state is BLINK it will blink automatically
        setLed(led, ON);
        // We are not idle anymore
        timers.idle           = false;
        // Start the idle countdown timer
        timers.tStartIdleTime = millis;
    }
    else{
        /* Check if a "next" button was pressed, turn off the current selected 
         * function led to lid the next one
         */
        if(NOF != channel && ASSERT != channel){
            setLed(led, OFF);
        }        
        // Check which button was pressed, jump or activate new function
        switch (channel){
            case NEXT:
            {     
                // <editor-fold defaultstate="collapsed">
//                if((ARR_MAX_ELEM(yardLedArr) - unusedBV) == selector){
//                   selector = 0; 
//                }
//                else{ selector++; }
//                // Point to next function
//                led = &yardLedArr[selector];// </editor-fold>
                /* Increment the selector */
                selector++;
                /* Check if the selector still fits in the active group table */
                if(selector < groupSet->groupCount){
                    /* Select the next BV from the active group table */
                    led = &yardLedArr[groupSet->groups[selector].groupIndex];
                }
                else{
                    /* Reset selector to beginning */
                    selector = 0;
                    /* Select the first BV within the active group table */
                    led = &yardLedArr[groupSet->groups[selector].groupIndex];
                }
            }
                break;

            case PREV:
            {
                // <editor-fold defaultstate="collapsed">
//                if(0 == selector){
//                    selector = (ARR_MAX_ELEM(yardLedArr) - unusedBV);
//                }
//                else{ selector--; }
//                // Point to previous function
//                led = &yardLedArr[selector];// 
                //</editor-fold>
                /* Check if the selector is already 0 */
                if(0 == selector){
                    /* set the selector to the last BV in the active group table */
                    selector = (uint8_t)(groupSet->groupCount - 1);
                    /* Select the last BV from the active group table */
                    led = &yardLedArr[groupSet->groups[selector].groupIndex];                    
                }
                else{
                    /* Decrement the selector */
                    selector--;
                    /* Select the previous BV from the active group table */
                    led = &yardLedArr[groupSet->groups[selector].groupIndex];
                }                
            }
                break;

            case JMP:
            {
                // <editor-fold defaultstate="collapsed">
//                if((selector + jumpSize) >(ARR_MAX_ELEM(yardLedArr) - unusedBV)){
//                    uint8_t delta = (ARR_MAX_ELEM(yardLedArr) - unusedBV) - selector;
//                    selector = jumpSize - delta;
//                }
//                else{
//                    selector += jumpSize;
//                }                
                // Jump JUMPSIZE functions
                
                // Jump fixed groups
                
//                led = &yardLedArr[jmpSelector];
//                if(3 == jmpSelector){
//                    jmpSelector = 11;
//                }
//                else if(11 == jmpSelector){
//                    jmpSelector = 22;
//                }
//                else if(22 == jmpSelector){
//                    jmpSelector = 3;
//                }
//                for (size_t i = 0; i < ruleSet->ruleCount; i++) {
//                    setOut(&output[ruleSet->rules[i].outputIndex], ruleSet->rules[i].state);
//                }
                // </editor-fold>
                /* Move to the next group, wrap around automatically */
                jmpSelector = (jmpSelector + 1) % (sizeof(groupTable) / sizeof(groupTable[0]));
                /* 
                 * %(modulus) total_groups ensures that when jmpSelector reaches 
                 * the last index, it wraps around to 0 (3 % 3 = 0)
                 */
                
                /* Update the const pointer to the new pointed group table */
                groupSet = &groupTable[jmpSelector];
                /* Reset the selector */
                selector = 0;
                /* Fetch the first BV within the active group table */
                led = &yardLedArr[groupSet->groups[selector].groupIndex];               
            }
                break;

            case ASSERT:
            {
                // Toggle the function activated state
                led->funcActivated = !led->funcActivated;
                // led shall show the state
                led->state = (led->funcActivated) ? ON : BLINK;
                // restart the idle timeout counter
                timers.tStartIdleTime = millis;
                // Execute the selected led function
                ExecSelectedFunction(led, yardOutputArr);
            }
                break;
            default:
                break;
        }
    }
    
    // check if the function is on, off or blink
    if(BLINK == led->state){
        if((millis - timers.tStartBlinkTime) > timers.tWaitBlinkTime){
            timers.tStartBlinkTime = millis;
            setLed(led, TOGGLE);
        }
    }
    else if(ON == led->state){ setLed(led, ON);}
    else if(OFF == led->state){ setLed(led, OFF);}
    
    // Handle the idle timeout when not idle
    if((millis - timers.tStartIdleTime) > timers.tWaitIdleTime && !timers.idle){
            led->state = OFF;
            timers.idle = true;
    }    
    
    /* 
     * Check the current state of the device IOX var to the previous
     */
    for(uint8_t i=0; i<6; i++){
        prevIOX[i].IOXRAupdate = 
                (prevIOX[i].IOXRA == devices[i].byteView.IOXRA) 
                ? false : true;

        prevIOX[i].IOXRBupdate = 
                (prevIOX[i].IOXRB == devices[i].byteView.IOXRB) 
                ? false : true;
    }
    
    /* Update only 1 IOX chip per tWaitIOXTime */
    if((millis - timers.tStartIOXTime) > timers.tWaitIOXTime){
        /* Check if an update to a IOX is needed */
        updateIOX();
        /* start the timer for next check */
        timers.tStartIOXTime = millis;
    }
}

#pragma optimize( "", off )
/*
 * setLed(const YARDLED *self, LEDSTATE state)
 * Sets the led state of the passed led to ON or Off
 * Write the IOX var that will be send by the updateIOX()
 */
void setLed(const YARDLED *self, LEDSTATE state)
{
    if (TOGGLE == state){
        state = (*self->portx_ptr & self->pin_mask) ? OFF : ON;
    }
    if(ON == state){
        *self->portx_ptr |= self->pin_mask;
    }
    else if(OFF == state){
        *self->portx_ptr &= !self->pin_mask;
    }
}
/*
 * setOut(const YARDOUTPUT *self, bool state)
 * Sets the output state of the passed output to ON or Off
 * taking into account if the output is active high or low
 * Write the IOX var that will be send by the updateIOX()
 */
void setOut(const YARDOUTPUT *self, bool state)
{
    if (self->invertedLevel) {
        state = !state; // Invert the state if the level is inverted
    }

    if (state) {
        *self->portx_ptr |= self->pin_mask; // Set the bit in the devices mem
    } else {
        *self->portx_ptr &= ~self->pin_mask; // Clear the bit in the devices mem
    }
}
#pragma optimize( "", on )


/*
 * updateIOX()
 * Write to the IO Expander chip, 1 at a time
 */
void updateIOX(void){
    if(prevIOX[IOXupdater].IOXRAupdate){
        #ifndef DEBUG
        MCP23017xWritePort(&devices[IOXupdater], 0xA, devices[IOXupdater].byteView.IOXRA);
        #endif
        prevIOX[IOXupdater].IOXRAupdate = false;
        prevIOX[IOXupdater].IOXRA = devices[IOXupdater].byteView.IOXRA;
    }
    if(prevIOX[IOXupdater].IOXRBupdate){
        #ifndef DEBUG
        MCP23017xWritePort(&devices[IOXupdater], 0xB, devices[IOXupdater].byteView.IOXRB);
        #endif
        prevIOX[IOXupdater].IOXRBupdate = false;
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
     * 29 BV's!!! Filter out those who should be handled differently!!
     */
    if(false == led->funcActivated){
        if(BVLED27 == led->nled){
            // Crane function, stop function, invert crane direction
            setOut(&output[CRENA], false);
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
        if(     BVLED27 == yardLedArr[i].nled ||    // Crane function                
                BVLED26 == yardLedArr[i].nled ){    // BV26 Rotate disk power
                // functions not to touch!            
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

/*
 * executeRules(YARDOUTPUT *output, YARD_LEDS led)
 * Execute the selected function by reading the rules lines per function.
 */
void executeRules(YARDOUTPUT *output, YARD_LEDS led) {
    if (led >= sizeof(ruleTable) / sizeof(ruleTable[0]) || ruleTable[led].rules == NULL) {
        return; // Invalid or undefined LED
    }

    const RuleSet *ruleSet = &ruleTable[led];
    for (size_t i = 0; i < ruleSet->ruleCount; i++) {
        setOut(&output[ruleSet->rules[i].outputIndex], ruleSet->rules[i].state);
    }
}