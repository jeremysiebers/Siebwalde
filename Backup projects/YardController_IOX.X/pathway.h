/* Microchip Technology Inc. and its subsidiaries.  You may use this software 
 * and any derivatives exclusively with Microchip products. 
 * 
 * THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS".  NO WARRANTIES, WHETHER 
 * EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY IMPLIED 
 * WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A 
 * PARTICULAR PURPOSE, OR ITS INTERACTION WITH MICROCHIP PRODUCTS, COMBINATION 
 * WITH ANY OTHER PRODUCTS, OR USE IN ANY APPLICATION. 
 *
 * IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
 * INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
 * WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP HAS 
 * BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE.  TO THE 
 * FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL CLAIMS 
 * IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT OF FEES, IF 
 * ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS SOFTWARE.
 *
 * MICROCHIP PROVIDES THIS SOFTWARE CONDITIONALLY UPON YOUR ACCEPTANCE OF THESE 
 * TERMS. 
 */

/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef PATHWAY_H
#define	PATHWAY_H

#include <xc.h> // include processor files - each processor file is guarded. 
#include "main.h"
#include "enums.h"

VEHICLESTOP BUS        = {&LATE, 0x01, &LATE, 0x02, &LATE, 0x04, &LATE, 0x08};// All servos for industrial and station


VEHICLESTOP FIRE_DEP   = {&LATE, 0x10, &LATE, 0x20, &LATE, 0x40, &LATE, 0x80};// All servos for fire department
                         /*    switch,      switch,        stop,        stop */

//    SERVO SWITCH_BUSSTOP_INDUSTRIAL     = {&LATD, 0x01};
//    SERVO STOP_DRIVE_BUSSTOP_INDUSTRIAL = {&LATD, 0x02};
//    SERVO SWITCH_BUSSTOP_STATION        = {&LATD, 0x04};
//    SERVO STOP_DRIVE_BUSSTOP_STATION    = {&LATD, 0x08};
//    
//    SERVO SWITCH_FIREDEP_RIGHT          = {&LATD, 0x10};
//    SERVO SWITCH_FIREDEP_MID            = {&LATD, 0x20};
//    SERVO STOP_DRIVE_FIREDEP_RIGHT      = {&LATD, 0x40};
//    SERVO STOP_DRIVE_FIREDEP_MID        = {&LATD, 0x80};

void SETxVEHICLExACTION(VEHICLE *self, TASK_MESSAGES action, TASK_STATE path);

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

