#include <xc.h>
#include "tracksignal.h"
#include "enums.h"
#include "mainstation.h"
#include "milisecond_counter.h"
#include "communication.h"

/*
 * Set the required signal
 */
void SETxSIGNAL(STATION *self, TASK_MESSAGES path, TASK_STATE signal)
{
    #pragma optimize( "", off )
    uint8_t pin1 = self->setSignal->pin1_mask;
    uint8_t pin2 = self->setSignal->pin2_mask;
    uint8_t pin3 = self->setSignal->pin3_mask;
    uint8_t pin4 = self->setSignal->pin4_mask;
    uint8_t pin5 = self->setSignal->pin5_mask;
    uint8_t pin6 = self->setSignal->pin6_mask;
    
    switch(signal){
        case SIG_RED:
            if(path == TRACK1|| path == TRACK10){
                *self->setSignal->port1_ptr &= ~pin1; // Green
                *self->setSignal->port2_ptr |=  pin2; // Red
            }
            else if(path == TRACK2 || path == TRACK11){
                *self->setSignal->port3_ptr &= ~pin3; // Green
                *self->setSignal->port4_ptr |=  pin4; // Red
            }
            else if(path == TRACK3 || path == TRACK12){
                *self->setSignal->port5_ptr &= ~pin5; // Green
                *self->setSignal->port6_ptr |=  pin6; // Red
            }
            break;
            
        case SIG_GREEN:
            if(path == TRACK1|| path == TRACK10){
                *self->setSignal->port1_ptr |=  pin1; // Green
                *self->setSignal->port2_ptr &= ~pin2; // Red
            }
            else if(path == TRACK2 || path == TRACK11){
                *self->setSignal->port3_ptr |=  pin3; // Green
                *self->setSignal->port4_ptr &= ~pin4; // Red
            }
            else if(path == TRACK3 || path == TRACK12){
                *self->setSignal->port5_ptr |=  pin5; // Green
                *self->setSignal->port6_ptr &= ~pin6; // Red
            }
            break;
            
        default: break;
    }
    
    CREATExTASKxSTATUSxMESSAGE(self->name,
            NONE, 
            signal, 
            path);
    
    #pragma optimize( "", on )
}