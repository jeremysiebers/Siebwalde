#include <xc.h>
#include "pathway.h"
#include "enums.h"
#include "milisecond_counter.h"
#include "communication.h"

/* Disable optimizing otherwise the compiler might optimize the definitions
 * away of the inverted state of a path
 */
void SETxVEHICLExACTION(VEHICLE *self, TASK_MESSAGES action, TASK_STATE path)
{
#pragma optimize( "", off )
    
    uint8_t pin1 = self->setParkAction->pin1_mask;
    uint8_t pin2 = self->setParkAction->pin2_mask;
    uint8_t pin3 = self->setParkAction->pin3_mask;
    uint8_t pin4 = self->setParkAction->pin4_mask;
    
    switch(action){
        case BRAKE:
            if(path == INDUSTRIAL){
                *self->setParkAction->port1_ptr &= ~pin1; // switch to INDUSTRIAL
                *self->setParkAction->port2_ptr &= ~pin2; // stop at INDUSTRIAL
            }            
            if(path == STATION){
                *self->setParkAction->port1_ptr |=  pin3; // switch to STATION
                *self->setParkAction->port2_ptr &= ~pin4; // stop at STATION
            }
            if(path == FIREDEP_MID){                
                *self->setParkAction->port2_ptr &= ~pin1; // switch to FIREDEP MID
                *self->setParkAction->port2_ptr |=  pin2; // switch to FIREDEP MID
                *self->setParkAction->port1_ptr &= ~pin3; // stop at FIREDEP MID
            }
            if(path == FIREDEP_RIGHT){
                *self->setParkAction->port2_ptr |=  pin1; // switch to FIREDEP RIGHT
                *self->setParkAction->port2_ptr &= ~pin2; // switch to FIREDEP RIGHT
                *self->setParkAction->port2_ptr &= ~pin4; // stop at FIREDEP RIGHT
            }
            break;
            
        case PASS:
            if(path == INDUSTRIAL){
                *self->setParkAction->port1_ptr |=  pin1; // switch to PASS INDUSTRIAL
            }
            else if(path == STATION){
                *self->setParkAction->port1_ptr &= ~pin3; // switch to PASS STATION
            }
            else if(path == FIREDEP){
                *self->setParkAction->port2_ptr &= ~pin1; // switch to FIREDEP MID
                *self->setParkAction->port2_ptr &= ~pin2; // switch to FIREDEP RIGHT
            }
            break;
            
        case DRIVE:
            if(path == INDUSTRIAL){
                *self->setParkAction->port2_ptr |=  pin2; // DRIVE at INDUSTRIAL
            }
            else if(path == STATION){
                *self->setParkAction->port2_ptr |=  pin4; // DRIVE at STATION
            }
            else if(path == FIREDEP_MID){
                *self->setParkAction->port2_ptr |=  pin3; // FIREDEP MID
            }
            else if(path == FIREDEP_RIGHT){
                *self->setParkAction->port2_ptr |=  pin4; // FIREDEP RIGHT
            }
            break;
            
        default:break;
    }
    
    CREATExTASKxSTATUSxMESSAGE(self->name,
            SET_PATH_WAY, 
            action, 
            path);
    
#pragma optimize( "", on )
}