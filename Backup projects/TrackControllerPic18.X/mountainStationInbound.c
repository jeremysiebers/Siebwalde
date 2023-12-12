#include <xc.h>
#include <stdbool.h>
#include "enums.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"
#include "rand.h"

static MNTSTNTRACK *activeTrack;

int8_t MOUNTAINxSTATIONxINBOUND(MNTSTATION *self){
    
    if(self->stnTrack1.stnState == STN_INBOUND){
        activeTrack = &self->stnTrack1;
    }
    else if(self->stnTrack2.stnState == STN_INBOUND){
        activeTrack = &self->stnTrack2;
    }
    else{
        return(done);
    }
    
    switch(activeTrack->stnSequence){
        
        /* switch to the inbound track, set the signals and wait.
         * Inbound track does not need to check if the outgoing block is free
         */
        case SEQ_IDLE:            
            SETxMNTSTATIONxPATHWAY(self, activeTrack->trackNr);
            activeTrack->tCountTime     = GETxMILLIS();
            activeTrack->tWaitTime      = tSwitchPointWaitTime;
            activeTrack->stnNextState   = SEQ_SET_OCC;
            activeTrack->stnSequence    = SEQ_WAIT;
            break;
            
            /* Disable the occupied signal to let the train drive in */
        case SEQ_SET_OCC:
            SETxOCC(self->setOccAmpOut, false);
            activeTrack->stnSequence    = SEQ_CHK_TRAIN;
            break;
            
        /* Check when the occupied signal goes low */
        case SEQ_CHK_TRAIN:
            if(activeTrack->getTrainEnterStnTrack->value){
                SETxOCC(self->setOccAmpOut, true); // Stop train from driving
                activeTrack->stnOccupied = true;
                activeTrack->stnSequence = SEQ_IDLE;
                activeTrack->stnState    = STN_WAIT; // Set to wait state for next outbound event
                activeTrack->tCountTime  = GETxMILLIS();
                activeTrack->tWaitTime   = (tTrainWaitTime + GETxRANDOMxNUMBER());
                activeTrack->getTrainEnterStnTrack->value = false;
                self->getTrainEnterSiebwaldeStn->value = false;
                self->LastInboundStn = activeTrack->trackNr;
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
