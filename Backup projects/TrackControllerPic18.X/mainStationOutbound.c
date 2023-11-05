#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"

STNTRACK *activeTrack;

int8_t MAINxSTATIONxOUTBOUND(STATION *self){
    
    if(self->stnTrack1.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack1;
    }
    else if(self->stnTrack2.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack2;
    }
    else if(self->stnTrack3.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack3;
    }
    else{
        return(nop);
    }
    
    switch(activeTrack->stnSequence){
        
        /* switch to the outgoing track, set the signals and wait */
        case SEQ_IDLE:            
            SETxSTATIONxPATHWAY(self, activeTrack->trackNr);
            activeTrack->tCountTime     = GETxMILLIS();
            activeTrack->tWaitTime      =  tSwitchPointWaitTime;
            activeTrack->stnNextState   = SEQ_SET_OCC;
            activeTrack->stnSequence    = SEQ_WAIT;
            break;
            
            /* Disable the occupied signal to let the train drive out*/
        case SEQ_SET_OCC:
            SETxOCC(activeTrack->setOccStn, false);
            activeTrack->stnSequence    = SEQ_CHK_TRAIN;
            break;
            
        /* Check when the occupied signal goes low */
        case SEQ_CHK_TRAIN:
            if(activeTrack->getOccStn->value){
                break;
            }
            else{
                activeTrack->stnOccupied = false;
                activeTrack->stnState    = STN_EMPTY;
                activeTrack->stnSequence = SEQ_IDLE;
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
