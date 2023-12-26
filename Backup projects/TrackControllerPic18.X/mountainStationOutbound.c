#include <xc.h>
#include <stdbool.h>
#include "enums.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"
#include "communication.h"

static MNTSTNTRACK *activeTrack;

int8_t MOUNTAINxSTATIONxOUTBOUND(MNTSTATION *self){
    
    if(self->stnTrack1.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack1;
    }
    else if(self->stnTrack2.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack2;
    }
    /* when only 1 train, and the outbound program is started when self
     * has no trains in the station, skip outbound program with nop */
    else if(false == self->stnTrack1.stnOccupied && 
            false == self->stnTrack2.stnOccupied){
        return(nop);
    }
    /* Occupied on one track wait for WAIT state to go to OUTBOUND */
    else{
        return(busy);
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
                CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           activeTrack->stnName, 
                                           activeTrack->stnState, 
                                           activeTrack->stnSequence);
            break;
            
            /* Disable the occupied signal to let the train drive out*/
        case SEQ_SET_OCC:
            SETxOCC(self->setOccAmpOut, false);
            activeTrack->stnSequence = SEQ_CHK_TRAIN;
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           activeTrack->stnName, 
                                           activeTrack->stnState, 
                                           activeTrack->stnSequence);
            break;
            
        /* 
         * Check when the occupied signal goes low AND train is in the 
         * block out! 
         */
        case SEQ_CHK_TRAIN:
            if(true == self->getTrainEnterSiebwaldeStn->value ){
                SETxOCC(self->setOccAmpOut, true);
                activeTrack->stnOccupied = false;
                /* Reset the hall sensors */
                activeTrack->getTrainEnterStnTrack->value = false;
                self->getTrainEnterSiebwaldeStn->value = false;
                
                activeTrack->stnNextState   = SEQ_OUTBOUND_BRAKE_TIME;
                activeTrack->stnSequence    = SEQ_WAIT;
                activeTrack->tCountTime     = GETxMILLIS();
                activeTrack->tWaitTime      =  tInOutboundStopWaitTime;
                
                CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           activeTrack->stnName, 
                                           activeTrack->stnState, 
                                           activeTrack->stnSequence);               
            }
            break;
            
        case SEQ_OUTBOUND_BRAKE_TIME:
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
