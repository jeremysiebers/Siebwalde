#include <xc.h>
#include "pathway.h"
#include "enums.h"
#include "mainstation.h"
#include "mountaintrack.h"
#include "milisecond_counter.h"
#include "communication.h"

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
void SETxSTATIONxPATHWAY(STATION *self, TASK_MESSAGES path, TASK_STATE dir)
{
#pragma optimize( "", off )
    
    uint8_t pin1 = self->setPath->pin1_mask;
    uint8_t pin2 = self->setPath->pin2_mask;
    uint8_t pin3 = self->setPath->pin3_mask;
    uint8_t pin4 = self->setPath->pin4_mask;
    
    switch(dir){
        case STN_INBOUND:
            if(path == TRACK1 || path == TRACK10 || path == TRACK11){
                *self->setPath->port1_ptr |=  pin1; // W1, W5
            }
            else if(path == TRACK2 || path == TRACK3 || path == TRACK12){
                *self->setPath->port1_ptr &= ~pin1; // W1, W5
            } 
            
            if(path == TRACK2 || path == TRACK10){                
                *self->setPath->port2_ptr |=  pin2; // W2, W6 
            }
            else if(path == TRACK3 || path == TRACK11){                
                *self->setPath->port2_ptr &= ~pin2; // W2, W6 
            }
            break;
            
        case STN_OUTBOUND:
            if(path == TRACK1 || path == TRACK11){
                *self->setPath->port3_ptr |=  pin3; // W3, W7
            }
            else if(path == TRACK2 || path == TRACK12){
                *self->setPath->port3_ptr &= ~pin3; // W3, W7 
            }
            
            if(path == TRACK3 || path == TRACK11 || path == TRACK12){
                *self->setPath->port4_ptr &= ~pin4; // W4, W8 
            }
            else if(path == TRACK1 || path == TRACK2 || path == TRACK10){
                *self->setPath->port4_ptr |=  pin4; // W4, W8
            }
            break;
            
        case STN_PASSING:
            if(path == TRACK1 || path == TRACK10 || path == TRACK11){
                *self->setPath->port1_ptr |=  pin1; // W1, W5
            }
            else if(path == TRACK2 || path == TRACK3 || path == TRACK12){
                *self->setPath->port1_ptr &= ~pin1; // W1, W5
            }
            
            if(path == TRACK2 || path == TRACK10){
                *self->setPath->port2_ptr |=  pin2; // W2, W6
            }
            else if(path == TRACK3 || path == TRACK11){
                *self->setPath->port2_ptr &= ~pin2; // W2, W6
            }
            
            if(path == TRACK1 || path == TRACK11){
                *self->setPath->port3_ptr |=  pin3; // W3, W7
            }
            else if(path == TRACK2 || path == TRACK12){
                *self->setPath->port3_ptr &= ~pin3; // W3, W7
            }
            
            if(path == TRACK1 || path == TRACK2 || path == TRACK10){
                *self->setPath->port4_ptr |=  pin4; // W4, W8
            }
            else if(path == TRACK3 || path == TRACK11 || path == TRACK12){
                *self->setPath->port4_ptr &= ~pin4; // W4, W8
            }            
            break;
            
        default:break;
    }
    
    CREATExTASKxSTATUSxMESSAGE(self->name,
            NONE, 
            dir, 
            path);
    
#pragma optimize( "", on )
}

void SETxMNTSTATIONxPATHWAY(MNTSTATION *self, TASK_MESSAGES path)
{
    #pragma optimize( "", off )

    uint8_t pin1 = self->setPath->pin1_mask;    
    
    if(path == T1){        
        *self->setPath->port1_ptr |=  pin1;
    }
    else if(path == T2){
        *self->setPath->port1_ptr &= ~pin1;
    }    
    else if(path == T7){        
        *self->setPath->port1_ptr |=  pin1;
    }
    else if(path == T8){
        *self->setPath->port1_ptr &= ~pin1;
    }

    CREATExTASKxSTATUSxMESSAGE(self->name,
            NONE, 
            NONE, 
            path);
    
    #pragma optimize( "", on )
}