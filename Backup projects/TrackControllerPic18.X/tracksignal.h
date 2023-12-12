/* 
 * File:   tracksignal.h
 * Author: jerem
 *
 * Created on December 3, 2023, 10:08 PM
 */

#ifndef TRACKSIGNAL_H
#define	TRACKSIGNAL_H

#include <xc.h> // include processor files - each processor file is guarded. 
#include "enums.h"
#include "mainstation.h"

SIG SIG_TOP  = {&LATB, 0x01, &LATB, 0x02, &LATJ, 0x01, &LATJ, 0x02, &LATJ, 0x04, &LATJ, 0x08};// Signal Leds 10B, 11B and 12B Green and Red
SIG SIG_BOT  = {&LATB, 0x04, &LATB, 0x08, &LATB, 0x10, &LATB, 0x20, &LATB, 0x40, &LATB, 0x80};// Signal Leds 1B, 2B and 3B Green and Red

extern void SETxSIGNAL(STATION *self, uint8_t path, TASK_STATE signal);

#ifdef	__cplusplus
extern "C" {
#endif




#ifdef	__cplusplus
}
#endif

#endif	/* TRACKSIGNAL_H */

