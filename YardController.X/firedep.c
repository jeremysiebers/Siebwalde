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
    firedep.name                    = FALLER_CARS;
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
            self->getVehicleAtStop1->value = false; // double used
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
                self->getVehicleAtStop1->value = false;
                SETxVEHICLExACTION(self, HALT, FIREDEP_MID);
                self->LastState     = RESTORE_NEEDED;                
                self->stop1occupied = true;
                self->parkState1    = PARK;
                self->tWaitTime1    = (tParkTime + (GETxRANDOMxNUMBER() << tRandomShift));
                self->tCountTime1   = GETxMILLIS();
            }
            else if(true == self->getVehicleAtStop2->value &&
                    NONE == self->parkState2){
                self->getVehicleAtStop2->value = false;
                SETxVEHICLExACTION(self, HALT, FIREDEP_RIGHT);
                self->LastState     = RESTORE_NEEDED;                
                self->stop2occupied = true;
                self->parkState2    = PARK;
                self->tWaitTime2    = (tParkTime + (GETxRANDOMxNUMBER() << tRandomShift));
                self->tCountTime2   = GETxMILLIS();
            }
            else{
                /* Reset Hall when stop is occupied but a magnet was seen */
                self->getVehicleAtStop1->value = false;
            }
            
            /* 
             * If a park area is in the Wait state then restore the pathway,
             * wait a few seconds before switching the switch servo back. 
             */
            if((PARK == self->parkState1 || PARK == self->parkState2) &&
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
                self->stop1occupied = false;
                /* Restore the stop location, prep for next time */
                self->AppNextState  = STOP_RESTORE;
                self->AppState      = WAIT;                
                self->tCountTime    = GETxMILLIS();
                self->tWaitTime     = tRestoreTime; 
                /* Log the action */
                CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        DRIVE, 
                        FIREDEP_MID);
            }
            if(DRIVE == self->parkState2){
                SETxVEHICLExACTION(self, DRIVE, FIREDEP_RIGHT);
                self->parkState2 = NONE;
                self->stop2occupied = false;
                /* Restore the stop location, prep for next time */
                self->AppNextState  = STOP_RESTORE;
                self->AppState      = WAIT;                
                self->tCountTime    = GETxMILLIS();
                self->tWaitTime     = tRestoreTime; 
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
            SETxVEHICLExACTION(self, PASS, FIREDEP);//          
            /* Log the application state */
            self->AppState      = IDLE;            
            CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        NONE, 
                        NONE);
            break;
            
        case STOP_RESTORE:
            /* restore stop magnet into default position */
            SETxVEHICLExACTION(self, BRAKE, FIREDEP_MID);
            SETxVEHICLExACTION(self, BRAKE, FIREDEP_RIGHT);
            /* Log the application state */
            CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        NONE, 
                        NONE);            
            self->AppState      = IDLE;
            break;
            
        case WAIT:
            if((GETxMILLIS() - self->tCountTime) > self->tWaitTime){
                self->AppState = self->AppNextState;
            }
            break;
            
        default:
            break;
    }
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
            if(PARK == self->parkState2){
                self->parkState1 = DRIVE;
            }
            else{
                self->parkState1 = PARK;
                self->tCountTime1 = millis;
            }            
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->parkState1, 
                                           DRIVE, 
                                           NONE);
        }
    }
    
    if(PARK == self->parkState2){
        if((millis - self->tCountTime2) > self->tWaitTime2){            
            if(PARK == self->parkState1){
                self->parkState2 = DRIVE;
            }
            else{
                self->parkState2 = PARK;
                self->tCountTime2 = millis;
            }            
            CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->parkState2, 
                                           DRIVE, 
                                           NONE);
        }
    }
}
