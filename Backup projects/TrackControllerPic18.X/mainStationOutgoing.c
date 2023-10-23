#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "pathway.h"
#include "setocc.h"

STNTRACK *activeTrack;

int8_t MAINxSTATIONSxOUTGOING(STATION *self){
    
    if(self->stnTrack1.stnState == STN_OUTGOING){
        activeTrack = &self->stnTrack1;
    }
    else if(self->stnTrack2.stnState == STN_OUTGOING){
        activeTrack = &self->stnTrack2;
    }
    else if(self->stnTrack3.stnState == STN_OUTGOING){
        activeTrack = &self->stnTrack3;
    }
    else{
        return(done);
    }
    
    switch(activeTrack->stnSequence){
        case SEQ_IDLE:
            SETxSTATIONxPATHWAY(self, activeTrack->trackNr);
            activeTrack->stnNextState = SEQ_SET_OCC;
            activeTrack->stnSequence = SEQ_WAIT;
            break;
            
            case SEQ_WAIT
            
        default: break;
    }
    
    return(busy);
}
