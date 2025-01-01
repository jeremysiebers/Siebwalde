/*
 * File:   bus.c
 * Author: jerem
 *
 * Created on February 5, 2023, 2:22 PM
 */
#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "enums.h"
#include "pathway.h"
#include "rand.h"
#include "milisecond_counter.h"
#include "communication.h"

void INITxBUSxDIRIVE(void){
    bus.name                    = FALLER_BUS;
    bus.AppState                = INIT;
    bus.setParkAction           = &BUS;
    bus.getVehicleAtStop1       = &HALL_BUSSTOP_IND;
    bus.stop1occupied           = false;
    bus.parkState1              = NONE;
    bus.getVehicleAtStop2       = &HALL_BUSSTOP_STN;
    bus.stop2occupied           = false;
    bus.parkState2              = NONE;    
}

void UPDATExBUSxDRIVE(VEHICLE *self) 
{
    switch(self->AppState)
    {
        case INIT:
            /* Set all servo outputs to 1 */
            SETxVEHICLExACTION(self, PASS, INDUSTRIAL);
            SETxVEHICLExACTION(self, PASS, STATION);
            self->getVehicleAtStop1->value = false;
            self->getVehicleAtStop2->value = false;
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
            /* Check for BUS detection */
            if(true == self->getVehicleAtStop1->value &&
                    NONE == self->parkState1){
                self->getVehicleAtStop1->value = false;
                SETxVEHICLExACTION(self, HALT, INDUSTRIAL);
                self->stop1occupied = true;
                self->parkState1    = PARK;
                self->tWaitTime1    = (tParkTime);// + (GETxRANDOMxNUMBER() << tRandomShift));
                self->tCountTime1   = GETxMILLIS();
                self->LastState     = RESTORE_NEEDED;
                FLOWxCONTROLxState = STATION;
            }
            else{
                /* Reset Hall when stop is occupied but a magnet was seen */
                self->getVehicleAtStop1->value = false;
            }
            if(true == self->getVehicleAtStop2->value &&
                    NONE == self->parkState2){
                self->getVehicleAtStop2->value = false;
                SETxVEHICLExACTION(self, HALT, STATION);
                self->stop2occupied = true;
                self->parkState2    = PARK;
                self->tWaitTime2    = (tParkTime);// + (GETxRANDOMxNUMBER() << tRandomShift));
                self->tCountTime2   = GETxMILLIS();
                self->LastState     = RESTORE_NEEDED;
                FLOWxCONTROLxState = STATION;
            }
            else{
                /* Reset Hall when stop is occupied but a magnet was seen */
                self->getVehicleAtStop2->value = false;
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
                SETxVEHICLExACTION(self, DRIVE, INDUSTRIAL);
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
                        INDUSTRIAL);
            }
            if(DRIVE == self->parkState2){
                SETxVEHICLExACTION(self, DRIVE, STATION);
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
                        STATION);
            }
            break;
            
        case RESTORE:
            /* 
             * Restore the drive way to normal road pass condition for other 
             * traffic.
             */
            if(self->stop1occupied){
                SETxVEHICLExACTION(self, PASS, INDUSTRIAL);
                
                CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        PASS, 
                        INDUSTRIAL);
            }
            else if(self->stop2occupied){
                SETxVEHICLExACTION(self, PASS, STATION);
                
                CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        PASS, 
                        STATION);
            }
            /* Log the application state */
            self->AppState      = IDLE;            
            CREATExTASKxSTATUSxMESSAGE(self->name,
                        self->AppState, 
                        NONE, 
                        NONE);
            break;
            
        case STOP_RESTORE:
            /* restore stop magnet into default position */
            SETxVEHICLExACTION(self, BRAKE, INDUSTRIAL);
            SETxVEHICLExACTION(self, BRAKE, STATION);
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
#pragma optimize( "", off )
void UPDATExBUSxDRIVExWAIT(VEHICLE *self){
    /* Get one time the actual time */
    uint32_t millis = GETxMILLIS();
    
    if(PARK == self->parkState1){
        if((millis - self->tCountTime1) > self->tWaitTime1){
            if(STATION == FLOWxCONTROLxOrder){
                self->parkState1 = DRIVE;
                CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->parkState1, 
                                           DRIVE, 
                                           NONE);
            }
            else{
                self->parkState1 = PARK;
                self->tCountTime1 = millis;
            }
        }
    }
    
    if(PARK == self->parkState2){
        if((millis - self->tCountTime2) > self->tWaitTime2){
            if(STATION == FLOWxCONTROLxOrder){
                self->parkState2 = DRIVE;
                CREATExTASKxSTATUSxMESSAGE(self->name, 
                                           self->parkState2, 
                                           DRIVE, 
                                           NONE);
            }
            else{
                self->parkState2 = PARK;
                self->tCountTime2 = millis;
            }
        }
    }
}
#pragma optimize( "", on )