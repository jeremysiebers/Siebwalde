#include <xc.h>
#include <stdbool.h>
#include "enums.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"

static MNTSTNTRACK *activeTrack;

int8_t MOUNTAINxSTATIONxOUTBOUND(MNTSTATION *self){
    
    if(self->stnTrack1.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack1;
    }
    else if(self->stnTrack2.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack2;
    }
    else{
        return(done);
    }
    
    switch(activeTrack->stnSequence){
        
        /* switch to the outgoing track, set the signals and wait 
         * when the outgoing block is free
         */
        case SEQ_IDLE:
                SETxMNTSTATIONxPATHWAY(self, activeTrack->trackNr);
                activeTrack->tCountTime     = GETxMILLIS();
                activeTrack->tWaitTime      =  tSwitchPointWaitTime;
                activeTrack->stnNextState   = SEQ_SET_OCC;
                activeTrack->stnSequence    = SEQ_WAIT;              
            break;
            
            /* Disable the occupied signal to let the train drive out*/
        case SEQ_SET_OCC:
            SETxOCC(self->setOccAmpOut, false);
            activeTrack->stnSequence = SEQ_CHK_TRAIN;
            break;
            
        /* 
         * Check when the occupied signal goes low AND train is in the 
         * block out! 
         */
        case SEQ_CHK_TRAIN:
            if(true == self->getTrainEnterSiebwaldeStn->value ){
                SETxOCC(self->setOccAmpOut, true);
                activeTrack->stnOccupied = false;
                activeTrack->getTrainEnterStnTrack = false;
                activeTrack->stnState    = STN_IDLE;
                activeTrack->stnSequence = SEQ_IDLE;
                activeTrack = 0;
                self->getTrainEnterSiebwaldeStn->value = false;
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
