#include <xc.h>
#include <stdbool.h>
#include "enums.h"
#include "rand.h"
#include "mainstation.h"
#include "communication.h"
#include "pathway.h"
#include "tracksignal.h"
#include "setocc.h"
#include "milisecond_counter.h"
#include "mainStationOutbound.h"
#include "mainStationInbound.h"
#include "mainStationPassing.h"

void INITxSTATION(void)
{
    INITxPATHWAYxSTATION(&top, &bot);
    top.name                    = MAIN_STATION_TOP;
    top.AppState                = INIT;
    top.getFreightLeaveStation  = &HALL_BLK_13;
    top.getFreightEnterStation  = &HALL_BLK_9B;
    top.setOccBlkIn             = &OCC_TO_9B;
    top.getOccBlkIn             = &OCC_FR_9B;    
    top.getOccBlkOut            = &OCC_FR_BLK13;
    top.setPath                 = &WS_TOP;
    top.prevPath                = 0;
    top.newPath                 = 0;
    top.setSignal               = &SIG_TOP; 
    top.setSignalTime           = 0;
    
    top.stnTrack1.stnName       = STNTRACK1;
    top.stnTrack1.trackNr       = TRACK10;
    top.stnTrack1.stnState      = STN_IDLE;
    top.stnTrack1.stnSequence   = SEQ_IDLE;
    top.stnTrack1.setOccStn     = &OCC_TO_STN_10;
    top.stnTrack1.getOccStn     = &OCC_FR_STN_10;
    
    top.stnTrack2.stnName       = STNTRACK2;
    top.stnTrack2.trackNr       = TRACK11;
    top.stnTrack2.stnState      = STN_IDLE;    
    top.stnTrack2.stnSequence   = SEQ_IDLE;
    top.stnTrack2.setOccStn     = &OCC_TO_STN_11;
    top.stnTrack2.getOccStn     = &OCC_FR_STN_11;
    
    top.stnTrack3.stnName       = STNTRACK3;
    top.stnTrack3.trackNr       = TRACK12;
    top.stnTrack3.stnState      = STN_IDLE;
    top.stnTrack3.stnSequence   = SEQ_IDLE;
    top.stnTrack3.setOccStn     = &OCC_TO_STN_12;
    top.stnTrack3.getOccStn     = &OCC_FR_STN_12;
    
    bot.name                    = MAIN_STATION_BOT;
    bot.AppState                = INIT;
    bot.getFreightLeaveStation  = &HALL_BLK_4A;
    bot.getFreightEnterStation  = &HALL_BLK_21A;
    bot.setOccBlkIn             = &OCC_TO_21B;
    bot.getOccBlkIn             = &OCC_FR_21B;    
    bot.getOccBlkOut            = &OCC_FR_BLK4;
    bot.setPath                 = &WS_BOT;
    bot.prevPath                = 0;
    bot.newPath                 = 0;
    bot.setSignal               = &SIG_BOT; 
    bot.setSignalTime           = 0;
    
    bot.stnTrack1.stnName       = STNTRACK1;
    bot.stnTrack1.trackNr       = TRACK1;
    bot.stnTrack1.stnState      = STN_IDLE;
    bot.stnTrack1.stnSequence   = SEQ_IDLE;
    bot.stnTrack1.setOccStn     = &OCC_TO_STN_1;
    bot.stnTrack1.getOccStn     = &OCC_FR_STN_1;
    
    bot.stnTrack2.stnName       = STNTRACK2;
    bot.stnTrack2.trackNr       = TRACK2;
    bot.stnTrack2.stnState      = STN_IDLE;
    bot.stnTrack2.stnSequence   = SEQ_IDLE;
    bot.stnTrack2.setOccStn     = &OCC_TO_STN_2;
    bot.stnTrack2.getOccStn     = &OCC_FR_STN_2;
    
    bot.stnTrack3.stnName       = STNTRACK3;
    bot.stnTrack3.trackNr       = TRACK3;
    bot.stnTrack3.stnState      = STN_IDLE;
    bot.stnTrack3.stnSequence   = SEQ_IDLE;
    bot.stnTrack3.setOccStn     = &OCC_TO_STN_3;
    bot.stnTrack3.getOccStn     = &OCC_FR_STN_3;
    
}

void UPDATExSTATION(STATION *self) 
{    
    switch(self->AppState)
    {    
        // <editor-fold defaultstate="collapsed">
        case INIT:
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                       0, 
                                       self->AppState, 
                                       NONE);
            /* Set both hall sensors debounced values to False */
            self->getFreightLeaveStation->value  = false;
            self->getFreightEnterStation->value = false;
            
            /* 
             * Set the occupied signals to all blocks to true to stop
             * all trains from driving. 
             */
            SETxOCC(self->setOccBlkIn, true);
            SETxOCC(self->stnTrack1.setOccStn , true);
            SETxOCC(self->stnTrack2.setOccStn , true);
            SETxOCC(self->stnTrack3.setOccStn , true);
            
            /*
             * Set all the track signals to red
             */
            if(self->name == MAIN_STATION_TOP){
                SETxSIGNAL(self, 10, SIG_RED);
                SETxSIGNAL(self, 11, SIG_RED);
                SETxSIGNAL(self, 12, SIG_RED);
            }
            else if(self->name == MAIN_STATION_BOT){
                SETxSIGNAL(self, 1,  SIG_RED);
                SETxSIGNAL(self, 2,  SIG_RED);
                SETxSIGNAL(self, 3,  SIG_RED);
            }
            
            /* Check if Station1 is occupied */
            if(self->stnTrack1.getOccStn->value){
                /*
                 * Start station wait random timers set STN1 to occupied
                 */                
                self->stnTrack1.stnOccupied = true;
                self->stnTrack1.stnState    = STN_WAIT;
                self->stnTrack1.tWaitTime   = tTrainWaitTime + GETxRANDOMxNUMBER();
                
            }
            else{
                self->stnTrack1.stnOccupied = false;
                self->stnTrack1.stnState = STN_IDLE;
            }
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                       self->stnTrack1.stnName, 
                                       self->stnTrack1.stnState, 
                                       NONE);
            
            /* Check if Station2 is occupied */
            if(self->stnTrack2.getOccStn->value){
                /*
                 * Start station wait random timers set STN2 to occupied
                 */
                self->stnTrack2.stnOccupied = true;
                self->stnTrack2.stnState    = STN_WAIT;
                self->stnTrack2.tWaitTime   = tTrainWaitTime + GETxRANDOMxNUMBER();
            }
            else{
                self->stnTrack2.stnOccupied = false;
                self->stnTrack2.stnState = STN_IDLE;
            }
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                       self->stnTrack2.stnName, 
                                       self->stnTrack2.stnState, 
                                       NONE);
            
            /* Check if Station3 is occupied */
            if(self->stnTrack3.getOccStn->value){
                
                /* Set station 3 to occupied */
                self->stnTrack3.stnOccupied = true;
                self->stnTrack3.stnState = STN_OUTBOUND;                                
            }
            /* When STN3 is free check if there is an incoming train */
            else if(self->getOccBlkIn->value){
//                /* Check for a free track */
//                if(false == self->stnTrack1.stnOccupied){
//                    self->stnTrack1.stnState = STN_INBOUND;
//                }
//                else if(false == self->stnTrack2.stnOccupied){
//                    self->stnTrack2.stnState = STN_INBOUND;
//                }
                if(self->getOccBlkOut->value){
                    /*
                     * Go to: Inbound on Station track 3
                     * continue with incoming train as freight
                     */
                    self->stnTrack3.stnState = STN_INBOUND;
                }
                /* Outgoing block is free */
                else{
                    /*
                     * Go to: let the incoming train pass as freight  
                     */
                    self->stnTrack3.stnState = STN_PASSING;
                }                
            }
            else{
                self->stnTrack3.stnOccupied = false;
                self->stnTrack3.stnState = STN_IDLE;
            }
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                       self->stnTrack3.stnName, 
                                       self->stnTrack3.stnState, 
                                       NONE);
            
            /* Start executing main state machine */
            self->AppState = RUN;
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                       0, 
                                       self->AppState, 
                                       NONE);
            break;
            
        // </editor-fold>
            
        case RUN:
            /*
             * INBOUND:
             * When inbound and a station is empty, 
             * while no INBOUND or PASSING is in progress,
             * take the train to the empty station track
             */
            if(true == self->getOccBlkIn->value){
                /* Check Track 1 and other states */
                if(false == self->getFreightEnterStation->value){
                    if(false == self->stnTrack1.stnOccupied &&
                            STN_INBOUND != self->stnTrack2.stnState &&
                            STN_INBOUND != self->stnTrack3.stnState &&
                            STN_PASSING != self->stnTrack2.stnState &&
                            STN_PASSING != self->stnTrack3.stnState
                            ){
                        self->stnTrack1.stnState = STN_INBOUND;
                        CREATExTASKxSTATUSxMESSAGE(self->name, 
                                                   self->stnTrack1.stnName, 
                                                   self->stnTrack1.stnState, 
                                                   NONE);
                    }
                    /* Check Track 2 and other states */
                    else if(false == self->stnTrack2.stnOccupied &&
                            STN_INBOUND != self->stnTrack1.stnState &&
                            STN_INBOUND != self->stnTrack3.stnState &&
                            STN_PASSING != self->stnTrack1.stnState &&
                            STN_PASSING != self->stnTrack3.stnState
                            ){
                        self->stnTrack2.stnState = STN_INBOUND;
                        CREATExTASKxSTATUSxMESSAGE(self->name, 
                                                   self->stnTrack2.stnName, 
                                                   self->stnTrack2.stnState, 
                                                   NONE);
                    }
//                    else if(false == self->stnTrack3.stnOccupied &&
//                            STN_INBOUND != self->stnTrack1.stnState &&
//                            STN_INBOUND != self->stnTrack2.stnState &&
//                            STN_PASSING != self->stnTrack1.stnState &&
//                            STN_PASSING != self->stnTrack2.stnState
//                            ){
//                        self->stnTrack3.stnState = STN_INBOUND;
//                    }
                }                                        
                /*
                 * INBOUND TRACK3:
                 * When inbound and only station3 is empty and the outbound
                 * track is free(false) or occupied (true) then let the train 
                 * pass the station or store it in station track 3
                 * There will be no train storage on track 3 normally.
                 */
                else if(false == self->getOccBlkOut->value &&
                        false        == self->stnTrack3.stnOccupied &&
                        STN_INBOUND  != self->stnTrack1.stnState &&
                        STN_INBOUND  != self->stnTrack2.stnState &&
                        STN_PASSING  != self->stnTrack1.stnState &&
                        STN_PASSING  != self->stnTrack2.stnState &&
                        STN_OUTBOUND != self->stnTrack1.stnState &&
                        STN_OUTBOUND != self->stnTrack2.stnState
                        ){
                    self->stnTrack3.stnState = STN_PASSING;
                    if(true == self->getFreightEnterStation->value){
                        self->getFreightEnterStation->value = false;
                    }
                    CREATExTASKxSTATUSxMESSAGE(self->name, 
                                                   self->stnTrack3.stnName, 
                                                   self->stnTrack3.stnState, 
                                                   NONE);
                }
                /* 
                 * Station 1 and 2 are allowed to do outbound while stnTrack3 
                 * has an Inbound 
                 */
                else if(true == self->getOccBlkOut->value){
                    if(false == self->stnTrack3.stnOccupied &&
                            STN_INBOUND != self->stnTrack1.stnState &&
                            STN_INBOUND != self->stnTrack2.stnState &&
                            STN_PASSING != self->stnTrack1.stnState &&
                            STN_PASSING != self->stnTrack2.stnState
                            ){
                        self->stnTrack3.stnState = STN_INBOUND;
                        CREATExTASKxSTATUSxMESSAGE(self->name, 
                                                   self->stnTrack3.stnName, 
                                                   self->stnTrack3.stnState, 
                                                   NONE);
    //                    if(true == self->getFreightEnterStation->value){
    //                        self->getFreightEnterStation->value = false;
    //                    }
                    }
                    /* 
                     * Only store freight in stnTrack 1 or 2 when stnTrack3
                     * is full and getOccBlkOut is true, set freight wait time
                     * on stnTrack 1 or 2!
                     */
//                    else if(false == self->stnTrack1.stnOccupied &&
//                            STN_INBOUND != self->stnTrack2.stnState &&
//                            STN_INBOUND != self->stnTrack3.stnState &&
//                            STN_PASSING != self->stnTrack2.stnState &&
//                            STN_PASSING != self->stnTrack3.stnState
//                            ){
//                        self->stnTrack1.stnState = STN_INBOUND;
//                    }
//                    /* Check Track 2 and other states */
//                    else if(false == self->stnTrack2.stnOccupied &&
//                            STN_INBOUND != self->stnTrack1.stnState &&
//                            STN_INBOUND != self->stnTrack3.stnState &&
//                            STN_PASSING != self->stnTrack1.stnState &&
//                            STN_PASSING != self->stnTrack3.stnState
//                            ){
//                        self->stnTrack2.stnState = STN_INBOUND;
//                    }
                }                
            }            
            /* Handle the stnTrack x which is in Inbound mode */
            MAINxSTATIONxINBOUND(self);
            /* Handle the stnTrack x which is in Passing mode */
            MAINxSTATIONxPASSING(self);
            /* Handle the stnTrack x which is in Outbound mode */
            MAINxSTATIONxOUTBOUND(self);
            break;
        
        default:
            self->AppState = INIT;
            break;
    }
}

/*
 * During every xMiliseconds an interrupt will call this function to 
 * check if a train wait time is done and no other track is in outbound mode.
 * Otherwise add another random time of max 0 - 32 seconds
 */
void UPDATExSTATIONxTRAINxWAIT(STATION *self)
{
    /* Get one time the actual time */
    uint32_t millis = GETxMILLIS();
    
    if(STN_WAIT == self->stnTrack1.stnState){
        if((millis - self->stnTrack1.tCountTime) > self->stnTrack1.tWaitTime){
            if(STN_OUTBOUND != self->stnTrack2.stnState &&
                    STN_OUTBOUND != self->stnTrack3.stnState &&
                    STN_PASSING  != self->stnTrack2.stnState &&
                    STN_PASSING  != self->stnTrack3.stnState){
                self->stnTrack1.stnState = STN_OUTBOUND;                
            }
            else{
                self->stnTrack1.tCountTime = millis;
                self->stnTrack1.tWaitTime = GETxRANDOMxNUMBER();
                self->stnTrack1.stnState = STN_WAIT;
            }
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->stnTrack1.stnName, 
                                           self->stnTrack1.stnState, 
                                           NONE);
        }
    }
    if(STN_WAIT == self->stnTrack2.stnState){
        if((millis - self->stnTrack2.tCountTime) > self->stnTrack2.tWaitTime){
            if(STN_OUTBOUND != self->stnTrack1.stnState &&
                    STN_OUTBOUND != self->stnTrack3.stnState &&
                    STN_PASSING  != self->stnTrack1.stnState &&
                    STN_PASSING  != self->stnTrack3.stnState){
                self->stnTrack2.stnState = STN_OUTBOUND;
            }
            else{
                self->stnTrack2.tCountTime = millis;
                self->stnTrack2.tWaitTime = GETxRANDOMxNUMBER();
                self->stnTrack2.stnState = STN_WAIT;
            }
        }
        CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->stnTrack2.stnName, 
                                           self->stnTrack2.stnState, 
                                           NONE);
    }
    if(STN_WAIT == self->stnTrack3.stnState){
        if((millis - self->stnTrack3.tCountTime) > self->stnTrack3.tWaitTime){
            if(STN_OUTBOUND != self->stnTrack1.stnState &&
                    STN_OUTBOUND != self->stnTrack2.stnState &&
                    STN_PASSING  != self->stnTrack1.stnState &&
                    STN_PASSING  != self->stnTrack2.stnState){
                self->stnTrack3.stnState = STN_OUTBOUND;
            }
            else{
                self->stnTrack3.tCountTime = millis;
                self->stnTrack3.tWaitTime = tFreightTrainWaitTime;//GETxRANDOMxNUMBER();
                self->stnTrack3.stnState = STN_WAIT;
            }
        }
        CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->stnTrack3.stnName, 
                                           self->stnTrack3.stnState, 
                                           NONE);
    }
}