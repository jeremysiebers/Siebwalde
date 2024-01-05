/*
 * File:   firedep.c
 * Author: jeremy
 *
 * Created on February 5, 2023, 5:40 PM
 */
#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "enums.h"
#include "pathway.h"
#include "rand.h"
#include "milisecond_counter.h"
#include "communication.h"

uint8_t State_firedep = 0;
uint16_t timer_firedep = 0;
uint8_t FDepOccRight = 1;
uint8_t FDepOccMid = 0;

/*
 * 3-way switch:
 * SW_FDEP_RIGHT_LAT = 0, SW_FDEP_MID_LAT = 0 --> LEFT LANE pass by
 * SW_FDEP_RIGHT_LAT = 1, SW_FDEP_MID_LAT = 0 --> Right lane STOP
 * SW_FDEP_RIGHT_LAT = 0, SW_FDEP_MID_LAT = 1 --> Middle lane STOP
 * SW_FDEP_RIGHT_LAT = 1, SW_FDEP_MID_LAT = 1 --> UNUSED 
 *  
 */

void INITxFIREDEP(void){
    firedep.name                    = FALLER_BUS;
    firedep.AppState                = INIT;
    firedep.setParkAction           = &FIRE_DEP;
    firedep.getVehicleAtStop1       = &HALL_STOP_FDEP;
    firedep.stop1occupied           = false;
    firedep.parkState1              = NONE;
    firedep.getVehicleAtStop2       = &HALL_STOP_FDEP;
    firedep.stop2occupied           = false;
    firedep.parkState2              = NONE;    
}

void UPDATExFIREDEPxDRIVE(VEHICLE *self) 
{
    switch(self->AppState)
    {
        case INIT:
            /* Set all servo outputs to 1 */
            SETxVEHICLExACTION(self, PASS, FIREDEP);
            self->AppNextState  = IDLE;
            self->AppState      = WAIT;
            self->tCountTime    = GETxMILLIS();
            self->tWaitTime     = tSwitchPointWaitTime;
            
            /* Log the application state */
            CREATExTASKxSTATUSxMESSAGE(self->name,
                    self->AppNextState, 
                    TIME, 
                    ((uint8_t)(self->tWaitTime/1000)));                        
            break;

        case IDLE:
            /* Check for firedep detection */
            if(true == self->getVehicleAtStop1->value &&
                    NONE == self->parkState1){
                SETxVEHICLExACTION(self, BRAKE, FIREDEP_MID);
                self->LastState     = IDLE;                
                self->stop1occupied = true;
                self->parkState1    = PARK;
                self->tWaitTime1    = (tParkTime + (GETxRANDOMxNUMBER() << tRandomShift));
                self->tCountTime1   = GETxMILLIS();
            }
            else if(true == self->getVehicleAtStop2->value &&
                    NONE == self->parkState2){
                SETxVEHICLExACTION(self, BRAKE, FIREDEP_RIGHT);
                self->LastState     = IDLE;                
                self->stop2occupied = true;
                self->parkState2    = PARK;
                self->tWaitTime2    = (tParkTime + (GETxRANDOMxNUMBER() << tRandomShift));
                self->tCountTime2   = GETxMILLIS();
            }
            
            /* 
             * If a park area is in the Wait state then restore the pathway,
             * wait a few seconds before switching the switch servo back. 
             */
            if((WAIT == self->parkState1 || WAIT == self->parkState2) &&
                    self->LastState != RESTORE){
                self->AppNextState  = RESTORE;
                self->AppState      = WAIT;
                self->LastState     = RESTORE;
                self->tCountTime    = GETxMILLIS();
                self->tWaitTime     = tRestoreTime;
                
                /* Log the application state */
                CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppNextState, 
                        TIME, 
                        ((uint8_t)(self->tWaitTime/1000)));
            }
            
            if(DRIVE == self->parkState1){
                SETxVEHICLExACTION(self, DRIVE, FIREDEP_MID);
                self->parkState1 = NONE;
                /* Log the action */
                CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        DRIVE, 
                        FIREDEP_MID);
            }
            if(DRIVE == self->parkState2){
                SETxVEHICLExACTION(self, DRIVE, FIREDEP_RIGHT);
                self->parkState2 = NONE;
                /* Log the action */
                CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        DRIVE, 
                        FIREDEP_RIGHT);
            }
            break;
            
        case RESTORE:
            /* 
             * Restore the drive way to normal road pass condition for other 
             * traffic.
             */
            if(self->stop1occupied){
                SETxVEHICLExACTION(self, PASS, FIREDEP_MID);
                
                CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        PASS, 
                        FIREDEP_MID);
            }
            else if(self->stop2occupied){
                SETxVEHICLExACTION(self, PASS, FIREDEP_RIGHT);
                
                CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        PASS, 
                        FIREDEP_RIGHT);
            }
            /* Log the application state */
            self->AppState      = IDLE;            
            CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        NONE, 
                        NONE);
            break;
            
        case WAIT:
            if((GETxMILLIS() - self->tCountTime) > self->tWaitTime){
                self->AppState = self->AppNextState;
            }
            break;
            
        default:
            break;
    }
    
//    switch(State_firedep)
//    {
//        case 0: 
//            /* Give time to power supply startup and servo PCB to startup */
//            timer_firedep++;
//            if(timer_firedep > 1000){
//                timer_firedep = 0;
//                State_firedep++;
//            }
//            break;
//            
//        case 1:
//            SW_FDEP_RIGHT_LAT       = 1;
//            SW_FDEP_MID_LAT         = 1;
//            STOP_FDEP_AT_RIGHT_LAT  = 1;
//            STOP_FDEP_AT_MID_LAT    = 1;
//            timer_firedep++;
//            if(timer_firedep > 500){
//                SW_FDEP_RIGHT_LAT       = 0;
//                SW_FDEP_MID_LAT         = 0;
//                STOP_FDEP_AT_RIGHT_LAT  = 1;
//                STOP_FDEP_AT_MID_LAT    = 1;
//                timer_firedep = 0;
//                State_firedep++;
//            }
//            break;
//            
//        case 2:
//            if(HALL_STOP_FDEP_IO_PORT == 1)
//            {
////                if(FDepOccRight == 0)
////                {
////                    SW_FDEP_RIGHT_LAT       = 1;
////                    SW_FDEP_MID_LAT         = 0;
////                    STOP_FDEP_AT_RIGHT_LAT  = 0;
////                    FDepOccRight = 1;
////                }
//                if(FDepOccMid == 0)
//                {
//                    SW_FDEP_RIGHT_LAT       = 0;
//                    SW_FDEP_MID_LAT         = 1;
//                    STOP_FDEP_AT_MID_LAT    = 0;
//                    FDepOccMid = 1;
//                }
//                else
//                {
//                    /* Pass driving Left again */
//                    SW_FDEP_RIGHT_LAT       = 0;
//                    SW_FDEP_MID_LAT         = 0;
//                }
//                State_firedep = 3;
//            }            
//            break;
//            
//        case 3:
//            timer_firedep++;
//            if(timer_firedep > 500)
//            {
//                /* Pass driving Left again */
//                SW_FDEP_RIGHT_LAT       = 0;
//                SW_FDEP_MID_LAT         = 0;
//                timer_firedep = 0;
//                State_firedep = 4;
//            }
//            break;
//            
//        case 4:
//            timer_firedep++;
//            if(timer_firedep > 1500)
//            {
//                timer_firedep = 0;
//                
//                if(FDepOccMid)
//                {
//                    STOP_FDEP_AT_MID_LAT  = 1;
//                    FDepOccMid = 0;
//                }
//                else
//                {
//                    STOP_FDEP_AT_RIGHT_LAT    = 1;
//                    FDepOccRight = 0;
//                }
//                State_firedep = 2;
//            }
//            break;
//            
//        default:
//            break;
//    }
}

/*
 * During every xMiliseconds an interrupt will call this function to 
 * check if a vehicle wait time is done 
 */
void UPDATExFIREDEPxDRIVExWAIT(VEHICLE *self){
    /* Get one time the actual time */
    uint32_t millis = GETxMILLIS();
    
    if(PARK == self->parkState1){
        if((millis - self->tCountTime1) > self->tWaitTime1){
            self->parkState1 = DRIVE;
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->parkState1, 
                                           DRIVE, 
                                           NONE);
        }
    }
    
    if(PARK == self->parkState2){
        if((millis - self->tCountTime2) > self->tWaitTime2){
            self->parkState2 = DRIVE;
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->parkState2, 
                                           DRIVE, 
                                           NONE);
        }
    }
}
