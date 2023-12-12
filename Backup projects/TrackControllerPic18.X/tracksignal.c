#include <xc.h>
#include "tracksignal.h"
#include "enums.h"
#include "mainstation.h"
#include "milisecond_counter.h"

/*
 * Set the required signal
 */
void SETxSIGNAL(STATION *self, uint8_t path, TASK_STATE signal)
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
            if(path == 1 || path == 10){
                *self->setSignal->port1_ptr &= ~pin1; // Green
                *self->setSignal->port2_ptr |=  pin2; // Red
            }
            else if(path == 2 || path == 11){
                *self->setSignal->port3_ptr &= ~pin3; // Green
                *self->setSignal->port4_ptr |=  pin4; // Red
            }
            else if(path == 3 || path == 12){
                *self->setSignal->port5_ptr &= ~pin5; // Green
                *self->setSignal->port6_ptr |=  pin6; // Red
            }
            break;
            
        case SIG_GREEN:
            if(path == 1 || path == 10){
                *self->setSignal->port1_ptr |=  pin1; // Green
                *self->setSignal->port2_ptr &= ~pin2; // Red
            }
            else if(path == 2 || path == 11){
                *self->setSignal->port3_ptr |=  pin3; // Green
                *self->setSignal->port4_ptr &= ~pin4; // Red
            }
            else if(path == 3 || path == 12){
                *self->setSignal->port5_ptr |=  pin5; // Green
                *self->setSignal->port6_ptr &= ~pin6; // Red
            }
            break;
            
        default: break;
    }
    #pragma optimize( "", on )
}