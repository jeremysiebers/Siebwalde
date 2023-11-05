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

void SETxSTATIONxPATHWAY(STATION *self, uint8_t path)
{
    switch(path){
        case 1:
            *self->setPath->port1_ptr |=  self->setPath->pin1_mask;
            *self->setPath->port2_ptr &= ~self->setPath->pin2_mask;
            *self->setPath->port3_ptr |=  self->setPath->pin3_mask;
            *self->setPath->port4_ptr &= ~self->setPath->pin4_mask; 
            break;
            
        case 2:
            *self->setPath->port1_ptr &= ~self->setPath->pin1_mask;
            *self->setPath->port2_ptr |=  self->setPath->pin2_mask;
            *self->setPath->port3_ptr &= ~self->setPath->pin3_mask;
            *self->setPath->port4_ptr |=  self->setPath->pin4_mask;
            break;
            
        case 3:
            *self->setPath->port1_ptr &= ~self->setPath->pin1_mask;
            *self->setPath->port2_ptr &= ~self->setPath->pin2_mask;
            *self->setPath->port3_ptr &= ~self->setPath->pin3_mask;
            *self->setPath->port4_ptr &= ~self->setPath->pin4_mask;
            break;
 
        default:break;
    }
    
    if(self->prevPath != path)
    {        
        /* Store new path in the previous path and set path update in newPath */
        self->prevPath = path;
        self->newPath = path;
        /* Path has changed, set all signals to red*/
        *self->setSignal->port1_ptr &= ~self->setSignal->pin1_mask; // Green
        *self->setSignal->port2_ptr |=  self->setSignal->pin2_mask; // Red
        *self->setSignal->port3_ptr &= ~self->setSignal->pin3_mask;
        *self->setSignal->port4_ptr |=  self->setSignal->pin4_mask;
        *self->setSignal->port5_ptr &= ~self->setSignal->pin5_mask;
        *self->setSignal->port6_ptr |=  self->setSignal->pin6_mask;
        self->setSignalTime = GETxMILLIS();
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
        if((GETxMILLIS() - refTOP->setSignalTime) > (tSignalSwitchWaitTime)){
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
        if((GETxMILLIS() - refBOT->setSignalTime) > (tSignalSwitchWaitTime)){
            setSignal(refBOT);
            refBOT->newPath = 0;
        }
    }
}

/*
 * This function takes a reference to a Station and changes the signal status
 * related to the desired newPath
 */
void setSignal(STATION *self)
{
    switch(self->newPath)
    {
        case 1:
            /* Signal 1B / 10B */
            *self->setSignal->port1_ptr |=  self->setSignal->pin1_mask; // Green
            *self->setSignal->port2_ptr &= ~self->setSignal->pin2_mask; // Red
            break;
            
        case 2:
            /* Signal 2B / 11B */
            *self->setSignal->port3_ptr |=  self->setSignal->pin3_mask;
            *self->setSignal->port4_ptr &= ~self->setSignal->pin4_mask;
            break;
            
        case 3:
            /* Signal 3B / 12B */
            *self->setSignal->port5_ptr |=  self->setSignal->pin5_mask;
            *self->setSignal->port6_ptr &= ~self->setSignal->pin6_mask;
            break;
            
        default: break;
    }
}