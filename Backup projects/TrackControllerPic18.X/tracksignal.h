/* 
 * File:   tracksignal.h
 * Author: jerem
 *
 * Created on December 3, 2023, 10:08 PM
 */

#ifndef TRACKSIGNAL_H
#define	TRACKSIGNAL_H

#include <xc.h> // include processor files - each processor file is guarded. 
#include "main.h"

extern void UPDATExSIGNAL(STATION *self, uint8_t path, enum STATES signal);

#ifdef	__cplusplus
extern "C" {
#endif




#ifdef	__cplusplus
}
#endif

#endif	/* TRACKSIGNAL_H */

