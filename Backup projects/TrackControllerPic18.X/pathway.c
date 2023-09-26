#include <xc.h>
#include "pathway.h"
#include "main.h"
#include "milisecond_counter.h"

STATION *refTOP;
STATION *refBOT;

void INITxPATHWAY(STATION *reftop, STATION *refbot)
{
    refTOP = reftop;
    refBOT = refbot;
}

void SETxSTATIONxPATHWAY(STATION *instance, uint8_t path)
{
    switch(path){
        case 1:
            *instance->setPath->port1_ptr |=  instance->setPath->pin1_mask;
            *instance->setPath->port2_ptr &= ~instance->setPath->pin2_mask;
            *instance->setPath->port3_ptr |=  instance->setPath->pin3_mask;
            *instance->setPath->port4_ptr &= ~instance->setPath->pin4_mask; 
            break;
            
        case 2:
            *instance->setPath->port1_ptr &= ~instance->setPath->pin1_mask;
            *instance->setPath->port2_ptr |=  instance->setPath->pin2_mask;
            *instance->setPath->port3_ptr &= ~instance->setPath->pin3_mask;
            *instance->setPath->port4_ptr |=  instance->setPath->pin4_mask;
            break;
            
        case 3:
            *instance->setPath->port1_ptr &= ~instance->setPath->pin1_mask;
            *instance->setPath->port2_ptr &= ~instance->setPath->pin2_mask;
            *instance->setPath->port3_ptr &= ~instance->setPath->pin3_mask;
            *instance->setPath->port4_ptr &= ~instance->setPath->pin4_mask;
            break;
 
        default:break;
    }
    
    if(instance->prevPath != path)
    {        
        /* Store new path in the previous path and set path update in newPath */
        instance->prevPath = path;
        instance->newPath = path;
        /* Path has changed, set all signals to red*/
        *instance->setSignal->port1_ptr &= ~instance->setSignal->pin1_mask; // Green
        *instance->setSignal->port2_ptr |=  instance->setSignal->pin2_mask; // Red
        *instance->setSignal->port3_ptr &= ~instance->setSignal->pin3_mask;
        *instance->setSignal->port4_ptr |=  instance->setSignal->pin4_mask;
        *instance->setSignal->port5_ptr &= ~instance->setSignal->pin5_mask;
        *instance->setSignal->port6_ptr |=  instance->setSignal->pin6_mask;
        instance->setSignalTime = GETxMILLIS();
    }
}

/*
 * During every xMiliseconds an interrupt will call this function to check
 * if there are newPaths defined within the 2 stations.
 */
void UPDATExSIGNAL()
{
    /* Check of TOP station 1-3 */
    if(refTOP->newPath == 0){
        return;
    }
    else{
        /* If a new path is defined then check if enough time has elapsed to
         swith the signal */
        if((GETxMILLIS() - refTOP->setSignalTime) > 50){
            setSignal(refTOP);
            refTOP->newPath = 0;
        }
    }
    
    /* Check of BOT station 10-12 */
    if(refBOT->newPath == 0){
        return;
    }
    else{
        /* If a new path is defined then check if enough time has elapsed to
         swith the signal */
        if((GETxMILLIS() - refBOT->setSignalTime) > 50){
            setSignal(refBOT);
            refBOT->newPath = 0;
        }
    }
}

/*
 * This function takes a reference to a Station and changes the signal status
 * related to the desired newPath
 */
void setSignal(STATION *instance)
{
    switch(instance->newPath)
    {
        case 1:
            /* Signal 1B / 10B */
            *instance->setSignal->port1_ptr |=  instance->setSignal->pin1_mask; // Green
            *instance->setSignal->port2_ptr &= ~instance->setSignal->pin2_mask; // Red
            break;
            
        case 2:
            /* Signal 2B / 11B */
            *instance->setSignal->port3_ptr |=  instance->setSignal->pin3_mask;
            *instance->setSignal->port4_ptr &= ~instance->setSignal->pin4_mask;
            break;
            
        case 3:
            /* Signal 3B / 12B */
            *instance->setSignal->port5_ptr |=  instance->setSignal->pin5_mask;
            *instance->setSignal->port6_ptr &= ~instance->setSignal->pin6_mask;
            break;
            
        default: break;
    }
}