#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "mainstation.h"
#include "pathway.h"
#include "setocc.h"
#include "mainStationOutbound.h"
#include "mainStationInbound.h"

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
    
    top.stnTrack1.trackNr       = 1;
    top.stnTrack1.stnState      = STN_EMPTY;
    top.stnTrack1.stnSequence   = SEQ_IDLE;
    top.stnTrack1.setOccStn     = &OCC_TO_STN_10;
    top.stnTrack1.getOccStn     = &OCC_FR_STN_10;
    
    top.stnTrack2.trackNr       = 2;
    top.stnTrack2.stnState      = STN_EMPTY;    
    top.stnTrack2.stnSequence   = SEQ_IDLE;
    top.stnTrack2.setOccStn     = &OCC_TO_STN_11;
    top.stnTrack2.getOccStn     = &OCC_FR_STN_11;
    
    top.stnTrack3.trackNr       = 3;
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
                    self->hndlState = HNDL_OUTGOING;
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
                case HNDL_IDLE:
                    /* 
                     * 
                     */
                    break;
                    
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
                    
                case HNDL_OUTGOING:
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
                    
                case HNDL_PASSING:
                    
                    break;
                    
                case HNDL_WAIT_BLK_OUT:
                    
                    break;
                    
                default: 
                    break;
            }
            break;
        
        default:
            self->AppState = INIT;
        break;
    }
}