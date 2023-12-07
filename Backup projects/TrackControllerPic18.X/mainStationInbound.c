#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"
#include "rand.h"

STNTRACK *activeTrack;

int8_t MAINxSTATIONxINBOUND(STATION *self){
    
    if(self->stnTrack1.stnState == STN_INBOUND){
        activeTrack = &self->stnTrack1;
    }
    else if(self->stnTrack2.stnState == STN_INBOUND){
        activeTrack = &self->stnTrack2;
    }
    else if(self->stnTrack3.stnState == STN_INBOUND){
        activeTrack = &self->stnTrack3;
    }
    else{
        return(nop);
    }
    
    switch(activeTrack->stnSequence){
        
        /* switch to the inbound track, set the signals and wait.
         * Inbound track does not need to check if the outgoing block is free
         */
        case SEQ_IDLE:            
            SETxSTATIONxPATHWAY(self, activeTrack->trackNr, STN_INBOUND);
            activeTrack->tCountTime     = GETxMILLIS();
            activeTrack->tWaitTime      = tSwitchPointWaitTime;
            activeTrack->stnNextState   = SEQ_SET_OCC;
            activeTrack->stnSequence    = SEQ_WAIT;
            break;
            
            /* Disable the occupied signal to let the train drive in */
        case SEQ_SET_OCC:
            SETxOCC(activeTrack->setOccStn, false);
            SETxOCC(self->setOccBlkIn, false);
            activeTrack->stnSequence    = SEQ_CHK_TRAIN;
            break;
            
        /* Check when the occupied signal goes low */
        case SEQ_CHK_TRAIN:
            if(activeTrack->getOccStn->value){
                SETxOCC(activeTrack->setOccStn, true); // Stop train from driving
                SETxOCC(self->setOccBlkIn, true); // Stop other trains from driving in
                activeTrack->stnOccupied = true;
                activeTrack->stnSequence = SEQ_IDLE;
                activeTrack->stnState    = STN_WAIT; // Set to wait state for next outbound event
                activeTrack->tCountTime  = GETxMILLIS();
                /* Set the minimum wait time + a random time */
                activeTrack->tWaitTime   = tTrainWaitTime + GETxRANDOMxNUMBER();
                activeTrack = 0;
                return(done);
            }
            break;
        
        /* Wait time counter using actual millisecond counter */
        case SEQ_WAIT:
            if((GETxMILLIS() - activeTrack->tCountTime) > activeTrack->tWaitTime){
                activeTrack->stnSequence = activeTrack->stnNextState;
            }
            break;
            
        default: break;
    }
    
    return(busy);
}
