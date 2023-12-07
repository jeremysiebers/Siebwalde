#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "mountaintrack.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"

REL         *setT4ToAmpT3;
REL         *setT5ToAmpT3;

void INITxMOUNTAINxSTATION(void)
{
    INITxPATHWAYxMNTSTATION(&waldsee, &waldberg);
    setT4ToAmpT3                            = &REL_T4;
    setT5ToAmpT3                            = &REL_T5;
    /**************************************************************************/
    waldsee.name                            = WALDSEE;
    waldsee.AppState                        = INIT;
    waldsee.hndlState                       = HNDL_IDLE;
    waldsee.getTrainEnterStationTrack1      = &HALL_BLK_T1;
    waldsee.getTrainEnterStationTrack2      = &HALL_BLK_T2;
    waldsee.setOccAmpOut                    = &OCC_TO_T3;
    waldsee.getOccAmpOut                    = &OCC_FR_T3;
    waldsee.setOccAmpIn                     = &OCC_TO_T6;
    waldsee.getOccAmpIn                     = &OCC_FR_T6;    
    waldsee.setPath                         = &WS_WALDSEE;
      
    waldsee.mntStnTrack1.trackNr            = T1;
    waldsee.mntStnTrack1.stnState           = STN_IDLE;
    waldsee.mntStnTrack1.stnSequence        = SEQ_IDLE;
      
    waldsee.mntStnTrack2.trackNr            = T2;
    waldsee.mntStnTrack2.stnState           = STN_IDLE;    
    waldsee.mntStnTrack2.stnSequence        = SEQ_IDLE;
    /**************************************************************************/
    siebwalde.name                          = SIEBWALDE;
    siebwalde.AppState                      = INIT;
    siebwalde.hndlState                     = HNDL_IDLE;
    siebwalde.getTrainEnterStationTrack1    = &HALL_BLK_T4;
    siebwalde.getTrainEnterStationTrack2    = &HALL_BLK_T5;    

    siebwalde.mntStnTrack1.trackNr          = T4;
    siebwalde.mntStnTrack1.stnState         = STN_IDLE;
    siebwalde.mntStnTrack1.stnSequence      = SEQ_IDLE;

    siebwalde.mntStnTrack2.trackNr          = T5;
    siebwalde.mntStnTrack2.stnState         = STN_IDLE;    
    siebwalde.mntStnTrack2.stnSequence      = SEQ_IDLE;
    /**************************************************************************/
    waldberg.name                           = WALDBERG;
    waldberg.AppState                       = INIT;
    waldberg.hndlState                      = HNDL_IDLE;
    waldberg.getTrainEnterStationTrack1     = &HALL_BLK_T7;
    waldberg.getTrainEnterStationTrack2     = &HALL_BLK_T8;
    waldsee.setOccAmpOut                    = &OCC_TO_T6;
    waldsee.getOccAmpOut                    = &OCC_FR_T6;
    waldsee.setOccAmpIn                     = &OCC_TO_T3;
    waldsee.getOccAmpIn                     = &OCC_FR_T3;
    waldberg.setPath                        = &WS_WALDBERG;
 
    waldberg.mntStnTrack1.trackNr           = T7;
    waldberg.mntStnTrack1.stnState          = STN_IDLE;
    waldberg.mntStnTrack1.stnSequence       = SEQ_IDLE;
 
    waldberg.mntStnTrack2.trackNr           = T8;
    waldberg.mntStnTrack2.stnState          = STN_IDLE;    
    waldberg.mntStnTrack2.stnSequence       = SEQ_IDLE;    
}

void UPDATExMOUNTAINxSTATION(MNTSTATION *self)
{
    switch(self->AppState)
    {    
        // <editor-fold defaultstate="collapsed">
        case INIT:
            
            /* Set all hall sensors debounced values to False */
            self->getTrainEnterStationTrack1->value  = false;
            self->getTrainEnterStationTrack2->value  = false;
            
            /* 
             * Set the occupied signals to all blocks to true to stop
             * all trains from driving. 
             */
            if(self->name != SIEBWALDE){
                SETxOCC(self->setOccAmpOut, true);
            }
            
            /* When the other stations are done intializing */
            if(self->name == SIEBWALDE){
                if(waldsee.AppState == RUN && waldberg.AppState == RUN){
                    siebwalde.AppState = RUN;
                }
            }
            /* 
             * Check Station/track occupancy, only the 2 outer stations
             * are used, assumed is that max 4 trains can be on the track.
             * Only Waldsee and Waldberg have stations to store trains.
             */
            else{
                if(self->getOccAmpOut->value){
                    /* Check if assumed default switch state<->Tx is ok? */
                    self->mntStnTrack1.stnOccupied = true;
                    self->mntStnTrack1.stnState = STN_WAIT;
                    /* Switch to the other track of the station */
                    SETxMNTSTATIONxPATHWAY(self, self->mntStnTrack2.trackNr);
                    /* Setup a wait time for the switch */
                    self->tCountTime = GETxMILLIS();
                    self->tWaitTime  = tSwitchPointWaitTime;
                    self->AppState = INIT2;
                }
            }
            break;
            
        case INIT2:
            /* Wait until the switch has moved */
            if((GETxMILLIS() - self->tCountTime) > self->tWaitTime){
                self->AppState = INIT3;
            }
            break;
            
        case INIT3:
            if(self->name != SIEBWALDE){
                if(self->getOccAmpOut->value){
                    /* Check if assumed default switch state<->Tx is ok? */
                    self->mntStnTrack2.stnOccupied = true;
                    self->mntStnTrack2.stnState = STN_WAIT;                    
                }
                self->AppState = RUN;
            }
            break;
            
        // </editor-fold>    
            
        case RUN:
            
            
            
            break;
            
        default: self->AppState = INIT;
            break;
    }
}

void UPDATExMOUNTAINxTRAINxWAIT()
{
    uint32_t millis = GETxMILLIS();
    
    if(STN_WAIT == waldsee.mntStnTrack1.stnState){
        if((millis - waldsee.mntStnTrack1.tCountTime) > waldsee.mntStnTrack1.tWaitTime){
                waldsee.mntStnTrack1.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == waldsee.mntStnTrack2.stnState){
        if((millis - waldsee.mntStnTrack2.tCountTime) > waldsee.mntStnTrack2.tWaitTime){
                waldsee.mntStnTrack2.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == siebwalde.mntStnTrack1.stnState){
        if((millis - siebwalde.mntStnTrack1.tCountTime) > siebwalde.mntStnTrack1.tWaitTime){
                siebwalde.mntStnTrack1.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == siebwalde.mntStnTrack2.stnState){
        if((millis - siebwalde.mntStnTrack2.tCountTime) > siebwalde.mntStnTrack2.tWaitTime){
                siebwalde.mntStnTrack2.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == waldberg.mntStnTrack1.stnState){
        if((millis - waldberg.mntStnTrack1.tCountTime) > waldberg.mntStnTrack1.tWaitTime){
                waldberg.mntStnTrack1.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == waldberg.mntStnTrack2.stnState){
        if((millis - waldberg.mntStnTrack2.tCountTime) > waldberg.mntStnTrack2.tWaitTime){
                waldberg.mntStnTrack2.stnState = STN_OUTBOUND;
            }
    }
}