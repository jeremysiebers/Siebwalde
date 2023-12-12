#include <xc.h>
#include <stdbool.h>
#include "enums.h"
#include "pathway.h"
#include "tracksignal.h"
#include "setocc.h"
#include "milisecond_counter.h"
#include "communication.h"

static STNTRACK *activeTrack;

int8_t MAINxSTATIONxOUTBOUND(STATION *self){
    
    if(self->stnTrack3.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack3;
    }
    else if(self->stnTrack1.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack1;
    }
    else if(self->stnTrack2.stnState == STN_OUTBOUND){
        activeTrack = &self->stnTrack2;
    }
    else{
        return(nop);
    }
    
    switch(activeTrack->stnSequence){
        
        /* switch to the outgoing track, set the signals and wait 
         * when the outgoing block is free
         */
        case SEQ_IDLE:
            if(false == self->getOccBlkOut->value ){
                SETxSTATIONxPATHWAY(self, activeTrack->trackNr, STN_OUTBOUND);
                SETxSIGNAL(self, activeTrack->trackNr, SIG_GREEN);
                activeTrack->tCountTime     = GETxMILLIS();
                activeTrack->tWaitTime      =  tSwitchPointWaitTime;
                activeTrack->stnNextState   = SEQ_SET_OCC;
                activeTrack->stnSequence    = SEQ_WAIT;
                CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           activeTrack->stnName, 
                                           activeTrack->stnState, 
                                           activeTrack->stnSequence);
            }                
            break;
            
            /* Disable the occupied signal to let the train drive out*/
        case SEQ_SET_OCC:
            SETxOCC(activeTrack->setOccStn, false);
            activeTrack->stnSequence    = SEQ_CHK_TRAIN;
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
            if(false == activeTrack->getOccStn->value && 
                    true == self->getOccBlkOut->value){
                activeTrack->tCountTime     = GETxMILLIS();
                activeTrack->tWaitTime      = tOutboundWaitTime;
                activeTrack->stnNextState   = SEQ_OUTBOUND_LEFT_STATTION;
                activeTrack->stnSequence    = SEQ_WAIT;
                CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           activeTrack->stnName, 
                                           activeTrack->stnState, 
                                           activeTrack->stnSequence);
            }
            break;
            
        case SEQ_OUTBOUND_LEFT_STATTION:
            SETxSIGNAL(self, activeTrack->trackNr, SIG_RED);
            activeTrack->stnOccupied = false;
            activeTrack->stnState    = STN_IDLE;
            activeTrack->stnSequence = SEQ_IDLE;
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           activeTrack->stnName, 
                                           activeTrack->stnState, 
                                           activeTrack->stnSequence);
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
