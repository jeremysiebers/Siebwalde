#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"

STNTRACK *activeTrack;

int8_t MAINxSTATIONxPASSING(STATION *self){
    
    activeTrack = &self->stnTrack3;
    
    switch(activeTrack->stnSequence){
        
        /* switch to the passing track3, set the signals and wait */
        case SEQ_IDLE:            
            SETxSTATIONxPATHWAY(self, activeTrack->trackNr);
            activeTrack->tCountTime     = GETxMILLIS();
            activeTrack->tWaitTime      =  tSwitchPointWaitTime;
            activeTrack->stnNextState   = SEQ_SET_OCC;
            activeTrack->stnSequence    = SEQ_WAIT;
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
            else{
                /* Wait until train is passing the station */
                break;
            }
            break;
        
        /* Check if the train has left the passing track */
        case SEQ_CHK_PASSED:
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
