#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "rand.h"
#include "mountaintrack.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"
#include "mountainStationOutbound.h"
#include "mountainStationInbound.h"

static REL         *setT4ToAmpT6;
static REL         *setT5ToAmpT3;   
static bool        invert = true;

void INITxMOUNTAINxSTATION(void)
{
    INITxPATHWAYxMNTSTATION(&waldsee, &waldberg);
    setT4ToAmpT6                            = &REL_T4; /* default T4 to AmpT3 */
    setT5ToAmpT3                            = &REL_T5; /* default T5 to AmpT6 */
    
    /**************************************************************************/
    waldsee.name                            = WALDSEE;
    waldsee.AppState                        = INIT;
    waldsee.getTrainEnterSiebwaldeStn       = &HALL_BLK_T4;
    waldsee.setOccAmpOut                    = &OCC_TO_T3;
    waldsee.getOccAmpOut                    = &OCC_FR_T3;    
    waldsee.setPath                         = &WS_WALDSEE;
      
    waldsee.stnTrack1.trackNr               = T1;
    waldsee.stnTrack1.stnState              = STN_IDLE;
    waldsee.stnTrack1.stnSequence           = SEQ_IDLE;
    waldsee.stnTrack1.getTrainEnterStnTrack = &HALL_BLK_T1;
      
    waldsee.stnTrack2.trackNr               = T2;
    waldsee.stnTrack2.stnState              = STN_IDLE;    
    waldsee.stnTrack2.stnSequence           = SEQ_IDLE;
    waldsee.stnTrack2.getTrainEnterStnTrack = &HALL_BLK_T2;
    /**************************************************************************/
//    siebwalde.name                          = SIEBWALDE;
//    siebwalde.AppState                      = INIT;
//    siebwalde.hndlState                     = HNDL_IDLE;
//    siebwalde.getTrainEnterStationTrack1    = &HALL_BLK_T4;
//    siebwalde.getTrainEnterStationTrack2    = &HALL_BLK_T5;    
//
//    siebwalde.stnTrack1.trackNr          = T4;
//    siebwalde.stnTrack1.stnState         = STN_IDLE;
//    siebwalde.stnTrack1.stnSequence      = SEQ_IDLE;
//
//    siebwalde.stnTrack2.trackNr          = T5;
//    siebwalde.stnTrack2.stnState         = STN_IDLE;    
//    siebwalde.stnTrack2.stnSequence      = SEQ_IDLE;
    /**************************************************************************/
    waldberg.name                           = WALDBERG;
    waldberg.AppState                       = INIT;
    waldberg.getTrainEnterSiebwaldeStn      = &HALL_BLK_T5;
    waldberg.setOccAmpOut                   = &OCC_TO_T6;
    waldberg.getOccAmpOut                   = &OCC_FR_T6;
    waldberg.setPath                        = &WS_WALDBERG;
 
    waldberg.stnTrack1.trackNr              = T7;
    waldberg.stnTrack1.stnState             = STN_IDLE;
    waldberg.stnTrack1.stnSequence          = SEQ_IDLE;
    waldberg.stnTrack1.getTrainEnterStnTrack= &HALL_BLK_T7;
 
    waldberg.stnTrack2.trackNr              = T8;
    waldberg.stnTrack2.stnState             = STN_IDLE;    
    waldberg.stnTrack2.stnSequence          = SEQ_IDLE;
    waldberg.stnTrack2.getTrainEnterStnTrack= &HALL_BLK_T8;    
}

void UPDATExMOUNTAINxSTATION(MNTSTATION *self)
{
    switch(self->AppState)
    {    
        // <editor-fold defaultstate="collapsed">
        case INIT:
            
            /* Set all hall sensors debounced values to False */
            self->stnTrack1.getTrainEnterStnTrack->value    = false;
            self->stnTrack2.getTrainEnterStnTrack->value    = false;
            self->getTrainEnterSiebwaldeStn->value          = false;
            
            /* 
             * Set the occupied signals to all blocks to true to stop
             * all trains from driving. 
             */
            SETxOCC(self->setOccAmpOut, true);
                        
            /* 
             * Check Station/track occupancy, only the 2 outer stations
             * are used, assumed is that max 4 trains can be on the track.
             * Only Waldsee and Waldberg have stations to store trains.
             */
            
            if(self->getOccAmpOut->value){
                /* Check if assumed default switch state T1<->T3 and T7<->T6 */
                self->stnTrack1.stnOccupied = true;
                self->stnTrack1.stnState = STN_WAIT;
            }
            else{
                self->stnTrack1.stnState = STN_IDLE;
                self->stnTrack1.stnOccupied = false;
            }
            
            /* Switch to the other track of the station */
            SETxMNTSTATIONxPATHWAY(self, self->stnTrack2.trackNr);
            /* Setup a wait time for the switch */
            self->tCountTime = GETxMILLIS();
            self->tWaitTime  = tSwitchPointWaitTime;
            self->AppNextState = INIT2;
            self->AppState = WAIT;
            break;
            
        case INIT2:
            /* Check if other side of the station has a train */
            if(self->getOccAmpOut->value){
                /* Check if assumed switched state T3<->T2, T8<->T6 */
                self->stnTrack2.stnOccupied = true;
                self->stnTrack2.stnState = STN_WAIT;                    
            }
            else{
                self->stnTrack2.stnState = STN_IDLE;
                self->stnTrack2.stnOccupied = false;
            }
            
            if(self->stnTrack2.stnOccupied){
                self->AppState = STN_TO_SIEBWALDE; 
            }
            else if(self->stnTrack1.stnOccupied){                
                self->AppNextState = STN_TO_SIEBWALDE;
            }
            else{
                self->AppState = SIEBWALDE_SWITCHT4T5;
            }  
            break;
            
        // </editor-fold>    
            
        case STN_TO_SIEBWALDE:
            
            /* Handle the stnTrack x which is in Outbound mode */
            if(MOUNTAINxSTATIONxOUTBOUND(self) == done){
                /* 
                 * Do wait after outbound for the train to drive to other 
                 * station 
                 */
                self->AppNextState = SIEBWALDE_SWITCHT4T5;
                self->LastState    = STN_OUTBOUND;
                self->AppState     = WAIT;
                self->tCountTime   = GETxMILLIS();
                self->tWaitTime    = tTrainWaitTime + GETxRANDOMxNUMBER();
                
                /* Set the inbound to the other Station */
                if(self->name == WALDSEE){
                    if(waldberg.stnTrack1.stnOccupied){
                        waldberg.stnTrack2.stnState = STN_INBOUND;
                    }
                    else{
                        waldberg.stnTrack1.stnState = STN_INBOUND;
                    }
                }
                else if(self->name == WALDBERG){
                    if(waldsee.stnTrack1.stnOccupied){
                        waldsee.stnTrack2.stnState = STN_INBOUND;
                    }
                    else{
                        waldsee.stnTrack1.stnState = STN_INBOUND;
                    }
                }
            }
            break;
            
        case SIEBWALDE_TO_STN:
            
            /* Handle the stnTrack x which is in Outbound mode */
            if(MOUNTAINxSTATIONxINBOUND(self) == done){
                self->AppState  = SIEBWALDE_SWITCHT4T5;  
                self->LastState = STN_INBOUND;
                
                /* Set the outbound for each Station */
                if(self->name == WALDSEE){
                    if(self->LastInboundStn == T1 && self->stnTrack2.stnOccupied){
                        self->stnTrack2.stnState = STN_WAIT;
                    }
                    else if(self->stnTrack1.stnOccupied){
                        self->stnTrack1.stnState = STN_WAIT;
                    }
                    else{
                        self->stnTrack1.stnState = STN_IDLE;
                        self->stnTrack2.stnState = STN_IDLE;
                    }
                }
                else if(self->name == WALDBERG){
                    if(self->LastInboundStn == T7 && self->stnTrack2.stnOccupied){
                        self->stnTrack2.stnState = STN_WAIT;
                    }
                    else if(self->stnTrack1.stnOccupied){
                        self->stnTrack1.stnState = STN_WAIT;
                    }
                    else{
                        self->stnTrack1.stnState = STN_IDLE;
                        self->stnTrack2.stnState = STN_IDLE;
                    }
                }
            }
            break;
        
        /*
         * Verify in this state when both or in case of 1 train, both sides of
         * the mountain track are ready for switching the T4 and T5 track.
         */
        case SIEBWALDE_SWITCHT4T5:
            /* Only switch one time! */
            if(self->name == WALDSEE){
                if(waldsee.AppState == SIEBWALDE_SWITCHT4T5 &&
                        waldberg.AppState == SIEBWALDE_SWITCHT4T5){
                    SETxOCC(setT4ToAmpT6, invert);
                    SETxOCC(setT5ToAmpT3, invert);
                    invert = !invert;
                    
                    if(STN_INBOUND == self->LastState){
                        waldsee.AppState     = STN_TO_SIEBWALDE;
                        waldberg.AppState    = STN_TO_SIEBWALDE;
                    }
                    else{
                        waldsee.AppState     = SIEBWALDE_TO_STN;
                        waldberg.AppState    = SIEBWALDE_TO_STN;
                    } 
                }                
            }
            break;
        
        case WAIT:
            /* Wait until the switch has moved */
            if((GETxMILLIS() - self->tCountTime) > self->tWaitTime){
                self->AppState = self->AppNextState;
            }
            break;
            
        default: self->AppState = INIT;
            break;
    }
}


/*
 * During every xMiliseconds an interrupt will call this function to 
 * check if a train wait time is done and no other track is in outbound mode.
 * Otherwise add another random time of max 0 - 32 seconds
 */
void UPDATExMOUNTAINxTRAINxWAIT(MNTSTATION *self)
{
    /* Get one time the actual time */
    uint32_t millis = GETxMILLIS();
    
    if(STN_WAIT == self->stnTrack1.stnState){
        if((millis - self->stnTrack1.tCountTime) > self->stnTrack1.tWaitTime){
            if(STN_OUTBOUND != self->stnTrack2.stnState){
                self->stnTrack1.stnState = STN_OUTBOUND;
            }
            else{
                self->stnTrack1.tCountTime = millis;
                self->stnTrack1.tWaitTime = GETxRANDOMxNUMBER();
                self->stnTrack1.stnState = STN_WAIT;
            }
        }
    }
    
    if(STN_WAIT == self->stnTrack2.stnState){
        if((millis - self->stnTrack2.tCountTime) > self->stnTrack2.tWaitTime){
            if(STN_OUTBOUND != self->stnTrack1.stnState){
                self->stnTrack2.stnState = STN_OUTBOUND;
            }
            else{
                self->stnTrack2.tCountTime = millis;
                self->stnTrack2.tWaitTime = GETxRANDOMxNUMBER();
                self->stnTrack2.stnState = STN_WAIT;
            }
        }
    }
}