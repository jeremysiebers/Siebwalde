#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "pathway.h"
#include "tracksignal.h"
#include "setocc.h"
#include "milisecond_counter.h"

static STNTRACK *activeTrack;

int8_t MAINxSTATIONxPASSING(STATION *self){
    
    if(self->stnTrack3.stnState == STN_PASSING){
        activeTrack = &self->stnTrack3;
    }
    else if(self->stnTrack1.stnState == STN_PASSING){
        activeTrack = &self->stnTrack1;
    }
    else if(self->stnTrack2.stnState == STN_PASSING){
        activeTrack = &self->stnTrack2;
    }
    else{
        return(nop);
    }
    
    switch(activeTrack->stnSequence){
        
        /* switch to the passing track3, set the signals and wait 
         * when the outgoing block is free
         */
        case SEQ_IDLE:
            if(false == self->getOccBlkOut->value){
                SETxSTATIONxPATHWAY(self, activeTrack->trackNr, STN_PASSING);
                SETxSIGNAL(self, activeTrack->trackNr, SIG_GREEN);
                activeTrack->tCountTime     = GETxMILLIS();
                activeTrack->tWaitTime      =  tSwitchPointWaitTime;
                activeTrack->stnNextState   = SEQ_SET_OCC;
                activeTrack->stnSequence    = SEQ_WAIT;
            }                
            break;
            
            /* Disable the occupied signal to let the train drive in */
        case SEQ_SET_OCC:
            SETxOCC(activeTrack->setOccStn, false);
            SETxOCC(self->setOccBlkIn, false);
            activeTrack->stnSequence    = SEQ_CHK_TRAIN;
            break;
            
        /* Check if the train is passing in the station */
        case SEQ_CHK_TRAIN:
            if(activeTrack->getOccStn->value){
                SETxOCC(self->setOccBlkIn, true); // Stop other trains from driving in
                activeTrack->stnSequence = SEQ_CHK_PASSED;                
            }
            break;
        
        /* 
         * Check if the train has left the passing track AND is in the
         * block out. 
         */
        case SEQ_CHK_PASSED:
            if(false == activeTrack->getOccStn->value &&
                    true == self->getOccBlkOut->value){                
                if(true == self->getFreightEnterStation->value){
                    self->getFreightEnterStation->value = false;
                }
                activeTrack->tCountTime     = GETxMILLIS();
                activeTrack->tWaitTime      = tOutboundWaitTime;
                activeTrack->stnNextState   = SEQ_OUTBOUND_LEFT_STATTION;
                activeTrack->stnSequence    = SEQ_WAIT;
            }
            break;
            
        case SEQ_OUTBOUND_LEFT_STATTION:
            SETxSIGNAL(self, activeTrack->trackNr, SIG_RED);
            activeTrack->stnOccupied = false;
            activeTrack->stnState    = STN_IDLE;
            activeTrack->stnSequence = SEQ_IDLE;
            activeTrack = 0;
            return(done);
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
