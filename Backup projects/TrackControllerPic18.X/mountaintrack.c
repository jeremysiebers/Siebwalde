#include <xc.h>
#include <stdbool.h>
#include "enums.h"
#include "rand.h"
#include "mountaintrack.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"
#include "mountainStationOutbound.h"
#include "mountainStationInbound.h"
#include "communication.h"

static REL          *setT4ToAmpT6;
//static REL          *setT5ToAmpT3; //soldered to REL_T4_T5  
static bool         invert = true;
static bool         invertExecuted = false;

void INITxMOUNTAINxSTATION(void)
{
    INITxPATHWAYxMNTSTATION(&waldsee, &waldberg);
    setT4ToAmpT6                            = &REL_T4_T5; /* default T4 to AmpT3 */
    //setT5ToAmpT3                            = &REL_T5; /* default T5 to AmpT6 soldered to REL_T4_T5 */
    
    /**************************************************************************/
    waldsee.name                            = WALDSEE;
    waldsee.AppState                        = INIT;
    waldsee.LastState                       = STN_IDLE;
    waldsee.getTrainEnterSiebwaldeStn       = &HALL_BLK_T4;
    waldsee.setOccAmpOut                    = &OCC_TO_T3;
    waldsee.getOccAmpOut                    = &OCC_FR_T3;    
    waldsee.setPath                         = &WS_WALDSEE;
    waldsee.LastInboundStn                  = T1;
    
    waldsee.stnTrack1.stnName               = MTNTRACK1;
    waldsee.stnTrack1.trackNr               = T1;
    waldsee.stnTrack1.stnState              = STN_IDLE;
    waldsee.stnTrack1.stnSequence           = SEQ_IDLE;
    waldsee.stnTrack1.getTrainEnterStnTrack = &HALL_BLK_T1;
    
    waldsee.stnTrack2.stnName               = MTNTRACK2;
    waldsee.stnTrack2.trackNr               = T2;
    waldsee.stnTrack2.stnState              = STN_IDLE;    
    waldsee.stnTrack2.stnSequence           = SEQ_IDLE;
    waldsee.stnTrack2.getTrainEnterStnTrack = &HALL_BLK_T2;
    /**************************************************************************/
    waldberg.name                           = WALDBERG;
    waldberg.AppState                       = INIT;
    waldberg.LastState                      = STN_IDLE;
    waldberg.getTrainEnterSiebwaldeStn      = &HALL_BLK_T5;
    waldberg.setOccAmpOut                   = &OCC_TO_T6;
    waldberg.getOccAmpOut                   = &OCC_FR_T6;
    waldberg.setPath                        = &WS_WALDBERG;
    waldberg.LastInboundStn                 = T8;
    
    waldberg.stnTrack1.stnName              = MTNTRACK7;
    waldberg.stnTrack1.trackNr              = T7;
    waldberg.stnTrack1.stnState             = STN_IDLE;
    waldberg.stnTrack1.stnSequence          = SEQ_IDLE;
    waldberg.stnTrack1.getTrainEnterStnTrack= &HALL_BLK_T7;
 
    waldberg.stnTrack2.stnName              = MTNTRACK8;
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
                /* Default switch state T3<->T2 and T6<->T8 */
                self->stnTrack2.stnOccupied = true;
                self->stnTrack2.stnState = STN_WAIT;
                self->AppState = STN_TO_SIEBWALDE;
            }
            else{
                self->stnTrack2.stnState = STN_IDLE;
                self->stnTrack2.stnOccupied = false;
                
                /* Switch to the other track of the station when train not 
                 * detected (only 2 trains drive). 
                 */
                SETxMNTSTATIONxPATHWAY(self, self->stnTrack1.trackNr);
                /* Setup a wait time for the switch */
                self->tCountTime = GETxMILLIS();
                self->tWaitTime  = tSwitchPointWaitTime;
                self->AppNextState = INIT2;
                self->AppState = WAIT;
            }
            CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->stnTrack2.stnName, 
                    self->stnTrack2.stnState, 
                    NONE);            
            
            break;
            
        case INIT2:
            /* Check if other side of the station has a train */
            if(self->getOccAmpOut->value){
                /* Check if assumed switched state T3<->T2, T8<->T6 */
                self->stnTrack1.stnOccupied = true;
                self->stnTrack1.stnState = STN_WAIT;
            }
            else{
                self->stnTrack1.stnState = STN_IDLE;
                self->stnTrack1.stnOccupied = false;
            }
            CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->stnTrack2.stnName, 
                    self->stnTrack2.stnState, 
                    NONE);
            
            if(self->stnTrack2.stnOccupied){
                self->AppState = STN_TO_SIEBWALDE; 
            }
            else if(self->stnTrack1.stnOccupied){                
                self->AppState = STN_TO_SIEBWALDE;
            }
            else{
                self->AppState = SIEBWALDE_SWITCHT4T5;
            }
            CREATExTASKxSTATUSxMESSAGE(self->name,
                    NONE, 
                    self->AppState, 
                    NONE);
            
            break;
            
        // </editor-fold>    
            
        case STN_TO_SIEBWALDE:
            
            /* Handle the stnTrack x which is in Outbound mode */
            self->Return = (TASK_STATE)MOUNTAINxSTATIONxOUTBOUND(self);
            if(done == self->Return || nop == self->Return){
                /* 
                 * Do wait after outbound for the train to drive to other 
                 * station 
                 */
                self->AppNextState = SIEBWALDE_SWITCHT4T5;
                self->LastState    = STN_OUTBOUND;
                /*
                 * SIEBWALDE_SWITCHT4T5; / WAIT;
                 * changed to directly depend on wait time of the train in siebwalde iso waiting longer
                 */
                self->AppState     = WAIT;
                self->tCountTime   = GETxMILLIS();
                self->tWaitTime    = tMountainTrainWaitTime + (GETxRANDOMxNUMBER() << tMountainRandomShift);
                CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->AppNextState, 
                    TIME, 
                    ((uint8_t)(self->tWaitTime/1000)));
                
                
                /* Set the inbound to the other Station to inbound when the
                 * return function is done (a train has moved).
                 */
                if(WALDSEE == self->name && done == self->Return){
                    if(false == waldberg.stnTrack1.stnOccupied){
                        waldberg.stnTrack1.stnState = STN_INBOUND;
                    }
                    else if(false == waldberg.stnTrack2.stnOccupied){
                        waldberg.stnTrack2.stnState = STN_INBOUND;
                    }
                }
                else if(WALDBERG == self->name && done == self->Return){
                    if(false == waldsee.stnTrack2.stnOccupied){
                        waldsee.stnTrack2.stnState = STN_INBOUND;
                    }
                    else if(false == waldsee.stnTrack1.stnOccupied){
                        waldsee.stnTrack1.stnState = STN_INBOUND;
                    }
                }
                /* messages */
                if(STN_INBOUND == self->stnTrack1.stnState){
                    CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->stnTrack1.stnName, 
                    self->stnTrack1.stnState, 
                    NONE);
                }
                else if(STN_INBOUND == self->stnTrack2.stnState){
                    CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->stnTrack2.stnName, 
                    self->stnTrack2.stnState, 
                    NONE);
                }
            }
            break;
            
        case SIEBWALDE_TO_STN:
            
            /* Handle the stnTrack x which is in Outbound mode */
            if(MOUNTAINxSTATIONxINBOUND(self) == done){
                self->AppState  = SIEBWALDE_SWITCHT4T5;  
                self->LastState = STN_INBOUND;                
                CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->AppState, 
                    NONE, 
                    NONE);                
            }
            break;
        
        /*
         * Verify in this state when both or in case of 1 train, both sides of
         * the mountain track are ready for switching the T4 and T5 track.
         */
        case SIEBWALDE_SWITCHT4T5:
            /* When both instances are done with a train move */
            if((waldsee.AppState   == SIEBWALDE_SWITCHT4T5 || 
                waldsee.AppState   == WAIT) &&
               (waldberg.AppState  == SIEBWALDE_SWITCHT4T5 ||
                waldberg.AppState  == WAIT) && 
               (waldberg.LastState == waldsee.LastState ||
                waldberg.LastState == STN_IDLE || /* Take fresh init also along for 1 train: */
                waldsee.LastState  == STN_IDLE)){
                
                /* Invert the drive direction */
                SETxOCC(setT4ToAmpT6, invert);
                //SETxOCC(setT5ToAmpT3, invert); soldered to REL_T4_T5
                
                CREATExTASKxSTATUSxMESSAGE(self->name,
                NONE,
                INVERT, 
                NONE);

                invert = !invert;
                
                /* only after an invert, an instance is allowed to proceed */
                if(SIEBWALDE_SWITCHT4T5 == self->AppState){
                    self->AppState = SIEWALDE_SET_NEXT;
                }
                else{
                    self->AppNextState = SIEWALDE_SET_NEXT;
                }
                
                /* Set the app state for the other instance */
                if(self->name == WALDSEE){
                    if(waldberg.AppState == SIEBWALDE_SWITCHT4T5){
                        waldberg.AppState = SIEWALDE_SET_NEXT;
                    }
                    else if(waldberg.AppState == WAIT){
                        waldberg.AppNextState = SIEWALDE_SET_NEXT;
                    }
                }
                else if(self->name == WALDBERG){
                    if(waldsee.AppState == SIEBWALDE_SWITCHT4T5){
                        waldsee.AppState = SIEWALDE_SET_NEXT;
                    }
                    else if(waldsee.AppState == WAIT){
                        waldsee.AppNextState = SIEWALDE_SET_NEXT;
                    }
                }
            }
            break;
            
        case SIEWALDE_SET_NEXT:            
            /* Set the next opposite action */
            if(STN_INBOUND == self->LastState){
                self->AppState     = STN_TO_SIEBWALDE;
            }
            else{
                self->AppState     = SIEBWALDE_TO_STN;
            }

            CREATExTASKxSTATUSxMESSAGE(self->name,
            NONE,
            self->AppState, 
            NONE);
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
                CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->stnTrack1.stnName, 
                    self->stnTrack1.stnState, 
                    NONE);
            }
            else{
                self->stnTrack1.tCountTime = millis;
                self->stnTrack1.tWaitTime = (GETxRANDOMxNUMBER() << tRandomShift);
                self->stnTrack1.stnState = STN_WAIT;
                CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->stnTrack1.stnName, 
                    TIME, 
                    ((uint8_t)(self->stnTrack1.tWaitTime/1000)));
            }
        }
    }
    
    if(STN_WAIT == self->stnTrack2.stnState){
        if((millis - self->stnTrack2.tCountTime) > self->stnTrack2.tWaitTime){
            if(STN_OUTBOUND != self->stnTrack1.stnState){
                self->stnTrack2.stnState = STN_OUTBOUND;
                CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->stnTrack2.stnName, 
                    self->stnTrack2.stnState, 
                    NONE);
            }
            else{
                self->stnTrack2.tCountTime = millis;
                self->stnTrack2.tWaitTime = (GETxRANDOMxNUMBER() << tMountainRandomShift);
                self->stnTrack2.stnState = STN_WAIT;
                CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->stnTrack2.stnName, 
                    TIME, 
                    ((uint8_t)(self->stnTrack2.tWaitTime/1000)));
            }
        }
    }
}