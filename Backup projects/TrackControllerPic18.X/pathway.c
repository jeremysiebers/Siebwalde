#include <xc.h>
#include "pathway.h"
#include "main.h"
#include "mainstation.h"
#include "mountaintrack.h"
#include "milisecond_counter.h"

STATION *refTOP;
STATION *refBOT;

MNTSTATION *refWALDSEE;
MNTSTATION *refWALDBERG;

void INITxPATHWAYxSTATION(STATION *reftop, STATION *refbot)
{
    refTOP = reftop;
    refBOT = refbot;
}

void INITxPATHWAYxMNTSTATION(MNTSTATION *refwaldsee, MNTSTATION *refwaldberg)
{
    refWALDSEE  = refwaldsee;
    refWALDBERG = refwaldberg;
}

/* Disable optimizing otherwise the compiler might optimize the definitions
 * away of the inverted state of a path
 */
void SETxSTATIONxPATHWAY(STATION *self, uint8_t path, enum STATES dir)
{
#pragma optimize( "", off )
    
    uint8_t pin1 = self->setPath->pin1_mask;
    uint8_t pin2 = self->setPath->pin2_mask;
    uint8_t pin3 = self->setPath->pin3_mask;
    uint8_t pin4 = self->setPath->pin4_mask;
    
    switch(dir){
        case STN_INBOUND:
            if(path == 1 || path == 10 || path == 11){
                *self->setPath->port1_ptr |=  pin1; // W1, W5
            }
            else if(path == 2 || path == 3 || path == 12){
                *self->setPath->port1_ptr &= ~pin1; // W1, W5
            } 
            
            if(path == 2 || path == 10){                
                *self->setPath->port2_ptr |=  pin2; // W2, W6 
            }
            else if(path == 3 || path == 11){                
                *self->setPath->port2_ptr &= ~pin2; // W2, W6 
            }
            break;
            
        case STN_OUTBOUND:
            if(path == 1 || path == 11){
                *self->setPath->port3_ptr |=  pin3; // W3, W7
            }
            else if(path == 2 || path == 12){
                *self->setPath->port3_ptr &= ~pin3; // W3, W7 
            }
            
            if(path == 3 || path == 11 || path == 12){
                *self->setPath->port4_ptr &= ~pin4; // W4, W8 
            }
            else if(path == 1 || path == 2 || path == 10){
                *self->setPath->port4_ptr |=  pin4; // W4, W8
            }
            break;
            
        case STN_PASSING:
            if(path == 1 || path == 10 || path == 11){
                *self->setPath->port1_ptr |=  pin1; // W1, W5
            }
            else if(path == 2 || path == 3 || path == 12){
                *self->setPath->port1_ptr &= ~pin1; // W1, W5
            }
            
            if(path == 2 || path == 10){
                *self->setPath->port2_ptr |=  pin2; // W2, W6
            }
            else if(path == 3 || path == 11){
                *self->setPath->port2_ptr &= ~pin2; // W2, W6
            }
            
            if(path == 1 || path == 11){
                *self->setPath->port3_ptr |=  pin3; // W3, W7
            }
            else if(path == 2 || path == 12){
                *self->setPath->port3_ptr &= ~pin3; // W3, W7
            }
            
            if(path == 1 || path == 2 || path == 10){
                *self->setPath->port4_ptr |=  pin4; // W4, W8
            }
            else if(path == 3 || path == 11 || path == 12){
                *self->setPath->port4_ptr &= ~pin4; // W4, W8
            }            
            break;
            
        default:break;
    }
#pragma optimize( "", on )
}

void SETxMNTSTATIONxPATHWAY(MNTSTATION *self, uint8_t path)
{
    #pragma optimize( "", off )
    #pragma optimize( "", on )
}