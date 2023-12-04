#include <xc.h>
#include "tracksignal.h"
#include "main.h"
#include "milisecond_counter.h"

/*
 * Set the required signal
 */
void SETxSIGNAL(STATION *self, uint8_t path, enum STATES signal)
{
#pragma optimize( "", off )
    uint8_t pin1 = self->setPath->pin1_mask;
    uint8_t pin2 = self->setPath->pin2_mask;
    uint8_t pin3 = self->setPath->pin3_mask;
    uint8_t pin4 = self->setPath->pin4_mask;
    uint8_t pin5 = self->setPath->pin5_mask;
    uint8_t pin6 = self->setPath->pin6_mask;
    
    switch(signal){
        case SIG_RED:
            if(path == 1 || path == 10){
                *self->setSignal->port1_ptr &= ~self->setPath->pin1_mask; // Green
                *self->setSignal->port2_ptr |=  self->setPath->pin2_mask; // Red
            }
            else if(path == 2 || path == 11){
                *self->setSignal->port3_ptr &= ~self->setPath->pin3_mask; // Green
                *self->setSignal->port4_ptr |=  self->setPath->pin4_mask; // Red
            }
            else if(path == 3 || path == 12){
                *self->setSignal->port5_ptr &= ~self->setPath->pin5_mask; // Green
                *self->setSignal->port6_ptr |=  self->setPath->pin6_mask; // Red
            }
            break;
            
        case SIG_GREEN:
            if(path == 1 || path == 10){
                *self->setSignal->port1_ptr |=  self->setPath->pin1_mask; // Green
                *self->setSignal->port2_ptr &= ~self->setPath->pin2_mask; // Red
            }
            else if(path == 2 || path == 11){
                *self->setSignal->port3_ptr |=  self->setPath->pin3_mask; // Green
                *self->setSignal->port4_ptr &= ~self->setPath->pin4_mask; // Red
            }
            else if(path == 3 || path == 12){
                *self->setSignal->port5_ptr |=  self->setPath->pin5_mask; // Green
                *self->setSignal->port6_ptr &= ~self->setPath->pin6_mask; // Red
            }
            break;
            
        default: break;
    }
#pragma optimize( "", on )
}