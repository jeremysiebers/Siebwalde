#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "mainstation.h"
#include "pathway.h"
#include "setocc.h"
#include "milisecond_counter.h"
#include "mainStationOutbound.h"
#include "mainStationInbound.h"
#include "mainStationPassing.h"

void INITxSTATION(void)
{
    INITxPATHWAY(&top, &bot);
    
    top.AppState                = INIT;
    top.hndlState               = HNDL_IDLE;
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
    
    top.stnTrack1.trackNr       = 10;
    top.stnTrack1.stnState      = STN_EMPTY;
    top.stnTrack1.stnSequence   = SEQ_IDLE;
    top.stnTrack1.setOccStn     = &OCC_TO_STN_10;
    top.stnTrack1.getOccStn     = &OCC_FR_STN_10;
    
    top.stnTrack2.trackNr       = 11;
    top.stnTrack2.stnState      = STN_EMPTY;    
    top.stnTrack2.stnSequence   = SEQ_IDLE;
    top.stnTrack2.setOccStn     = &OCC_TO_STN_11;
    top.stnTrack2.getOccStn     = &OCC_FR_STN_11;
    
    top.stnTrack3.trackNr       = 12;
    top.stnTrack3.stnState      = STN_EMPTY;
    top.stnTrack3.stnSequence   = SEQ_IDLE;
    top.stnTrack3.setOccStn     = &OCC_TO_STN_12;
    top.stnTrack3.getOccStn     = &OCC_FR_STN_12;
    
    bot.AppState                = INIT;
    bot.hndlState               = HNDL_IDLE;
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
    
    bot.stnTrack1.trackNr       = 1;
    bot.stnTrack1.stnState      = STN_EMPTY;
    bot.stnTrack1.stnSequence   = SEQ_IDLE;
    bot.stnTrack1.setOccStn     = &OCC_TO_STN_10;
    bot.stnTrack1.getOccStn     = &OCC_FR_STN_10;
    
    bot.stnTrack2.trackNr       = 2;
    bot.stnTrack2.stnState      = STN_EMPTY;
    bot.stnTrack2.stnSequence   = SEQ_IDLE;
    bot.stnTrack2.setOccStn     = &OCC_TO_STN_2;
    bot.stnTrack2.getOccStn     = &OCC_FR_STN_2;
    
    bot.stnTrack3.trackNr       = 3;
    bot.stnTrack3.stnState      = STN_EMPTY;
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
            /* Set both hall sensors debounced values to False */
            self->getFreightLeaveStation->value  = false;
            self->getFreightEnterStation->value = false;
            
            /* Set the occupied signals to all blocks to true to stop
             * all trains from driving. */
            SETxOCC(self->setOccBlkIn, true);
            SETxOCC(self->stnTrack1.setOccStn , true);
            SETxOCC(self->stnTrack2.setOccStn , true);
            SETxOCC(self->stnTrack3.setOccStn , true);
            
            /* Set freight passing path */
            //SETxSTATIONxPATHWAY(self, 3);
            
            /* Check if Station3 is occupied */
            if(self->stnTrack3.getOccStn->value){
                
                /* Set station 3 to occupied */
                self->stnTrack3.stnOccupied = true;
                self->stnTrack3.stnState = STN_OUTBOUND;
                
                /* Check if outgoing block is occupied */
                if(self->getOccBlkOut->value){
                    /* 
                     * Go to wait stage, wait until outgoing block is free
                     * and release STN3 train
                     */                    
                    self->hndlState = HNDL_WAIT_BLK_OUT;
                }
                /* Outgoing block is free */
                else {
                    /*
                     * Go to release STN3 train stage
                     */                    
                    self->hndlState = HNDL_OUTBOUND;
                }
            }
            
            /* When STN3 is free check if there is an incoming train */
            else if(self->getOccBlkIn->value){
                
                /* Check if outgoing block is occupied */
                if(self->getOccBlkOut->value){
                    /*
                     * Go to: wait stage outgoing block is occupied
                     * continue with incoming train as freight
                     */
                    self->stnTrack3.stnState = STN_INBOUND;
                    self->hndlState = HNDL_INBOUND;
                }
                /* Outgoing block is free */
                else{
                    /*
                     * Go to: let the incoming train pass as freight  
                     */
                    self->stnTrack3.stnState = STN_PASSING;
                    self->hndlState = HNDL_PASSING;
                }
                
            }
            else{
                self->stnTrack3.stnState = STN_EMPTY;
                self->hndlState = HNDL_IDLE;
            }
            
            /* Check if Station1 is occupied */
            if(self->stnTrack1.getOccStn->value){
                /*
                 * Start station wait random timers set STN1 to occupied
                 */
                
                /* Set station 1 to occupied */
                self->stnTrack1.stnOccupied = true;
                self->stnTrack1.stnState = STN_WAIT;
            }
            else{
                self->stnTrack1.stnState = STN_EMPTY;
            }
            
            /* Check if Station2 is occupied */
            if(self->stnTrack2.getOccStn->value){
                /*
                 * Start station wait random timers set STN2 to occupied
                 */
                
                /* Set station 2 to occupied */
                self->stnTrack2.stnOccupied = true;
                self->stnTrack2.stnState = STN_WAIT;
            }
            else{
                self->stnTrack2.stnState = STN_EMPTY;
            }
            
            /* Start executing main state machine */
            self->AppState = RUN;
            break;
            
        // </editor-fold>
            
        case RUN:
            
            switch(self->hndlState){
                
                /*
                 * HNDL_IDLE:
                 * Main IDLE state where conditions are checked what the next 
                 * action will be.
                 */
                case HNDL_IDLE:
                    
                    /*
                     * OUTBOUND:
                     * Check first if a station has the outbound status,
                     * if so, then let the train leave the station when the
                     * outbound block is free.
                     */
                    if(false == self->getOccBlkOut->value && (
                            STN_OUTBOUND == self->stnTrack1.stnState || 
                            STN_OUTBOUND == self->stnTrack2.stnState || 
                            STN_OUTBOUND == self->stnTrack3.stnState)){
                        self->hndlState = HNDL_OUTBOUND;
                    }
                    
                    /*
                     * INBOUND:
                     * When inbound and a station is empty, take the train
                     * inbound to the empty track. 
                     */
                    if(true == self->getOccBlkIn->value &&
                            
                            STN_EMPTY == self->stnTrack1.stnState){
                        self->stnTrack1.stnState = STN_INBOUND;
                        self->hndlState = HNDL_INBOUND;
                    }
                    else if(true == self->getOccBlkIn->value &&
                            STN_EMPTY == self->stnTrack2.stnState){
                        self->stnTrack2.stnState = STN_INBOUND;
                        self->hndlState = HNDL_INBOUND;
                    }
                    /*
                     * INBOUND:
                     * When inbound and only station3 is empty and the outbound
                     * track is free(false) then let the train pass the station.
                     * There will be no train storage on track 3 normally.
                     */
                    else if(true  == self->getOccBlkIn->value  && 
                            false == self->getOccBlkOut->value &&
                            STN_EMPTY == self->stnTrack3.stnState){
                        self->stnTrack3.stnState = STN_PASSING;
                        self->hndlState = HNDL_PASSING;
                    }
                    
                    break;
                    
                /*
                 * HNDL_INBOUND:
                 * Handles trains going inbound
                 */    
                case HNDL_INBOUND:
                    switch(MAINxSTATIONxINBOUND(self)){
                        case busy:
                            break;
                        case done:
                            self->hndlState = HNDL_IDLE;
                            break;
                        case nop:
                            /* Log Error no stn has the expected state */
                            break;
                        default:
                            break;
                    }
                    break;
                
                /*
                 * HNDL_OUTBOUND:
                 * Handles trains going outbound
                 */
                case HNDL_OUTBOUND:
                    switch(MAINxSTATIONxOUTBOUND(self)){
                        case busy:
                            break;
                        case done:
                            self->hndlState = HNDL_IDLE;
                            break;
                        case nop:
                            /* Log Error no stn has the expected state */
                            break;
                        default:
                            break;
                    }
                    break;
                
                /* 
                 * HNDL_PASSING:
                 * When after start or when a freight train is detected, 
                 * a pass of the station is to be executed.
                 */
                case HNDL_PASSING:
                    switch(MAINxSTATIONxPASSING(self)){
                        case busy:
                            break;
                        case done:
                            self->hndlState = HNDL_IDLE;
                            break;
                        case nop:
                            /* Log Error no stn has the expected state */
                            break;
                        default:
                            break;
                    }
                    break;
                    
                /* 
                 * HNDL_WAIT_BLK_OUT:
                 * When after start, the outgoing block is occupied, a wait
                 * must be done until outgoing track is free.
                 */
                case HNDL_WAIT_BLK_OUT:
                    if(self->getOccBlkOut->value){
                        break;
                    }
                    else{
                        /* When outgoing block is free check if track 3 is
                         * in an outbound state.
                         */
                        if(STN_OUTBOUND == self->stnTrack3.stnState){
                            self->hndlState = HNDL_OUTBOUND;
                        }
                        else{
                            self->hndlState = HNDL_IDLE;
                        }
                    }
                    break;
                    
                default:
                    self->hndlState = HNDL_IDLE;
                    break;
            }
            break;
        
        default:
            self->AppState = INIT;
        break;
    }
}

/*
 * During every xMiliseconds an interrupt will call this function to 
 * check if a train wait time is done
 */
void UPDATExTRAINxWAIT()
{
    uint32_t millis = GETxMILLIS();
    
    if(STN_WAIT == top.stnTrack1.stnState){
        if((millis - top.stnTrack1.tCountTime) > top.stnTrack1.tWaitTime){
                top.stnTrack1.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == top.stnTrack2.stnState){
        if((millis - top.stnTrack2.tCountTime) > top.stnTrack2.tWaitTime){
                top.stnTrack2.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == top.stnTrack3.stnState){
        if((millis - top.stnTrack3.tCountTime) > top.stnTrack3.tWaitTime){
                top.stnTrack3.stnState = STN_OUTBOUND;
            }
    }
    
    if(STN_WAIT == bot.stnTrack1.stnState){
        if((millis - bot.stnTrack1.tCountTime) > bot.stnTrack1.tWaitTime){
                bot.stnTrack1.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == bot.stnTrack2.stnState){
        if((millis - bot.stnTrack2.tCountTime) > bot.stnTrack2.tWaitTime){
                bot.stnTrack2.stnState = STN_OUTBOUND;
            }
    }
    if(STN_WAIT == bot.stnTrack3.stnState){
        if((millis - bot.stnTrack3.tCountTime) > bot.stnTrack3.tWaitTime){
                bot.stnTrack3.stnState = STN_OUTBOUND;
            }
    }
    
}