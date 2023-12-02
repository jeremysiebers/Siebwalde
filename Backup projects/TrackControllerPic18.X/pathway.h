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

WS WS_TOP    = {&LATC, 0x10, &LATC, 0x20, &LATC, 0x40, &LATC, 0x80,0,0,0,0};    // All switches for BOT part of station W5,6,7 and 8
WS WS_BOT    = {&LATC, 0x01, &LATC, 0x02, &LATC, 0x04, &LATC, 0x08,0,0,0,0};    // All switches for TOP part of station W1,2,3 and 4
 
SIG SIG_TOP  = {&LATB, 0x01, &LATB, 0x02, &LATJ, 0x01, &LATJ, 0x02, &LATJ, 0x04, &LATJ, 0x08};// Signal Leds 10B, 11B and 12B Green and Red
SIG SIG_BOT  = {&LATB, 0x04, &LATB, 0x08, &LATB, 0x10, &LATB, 0x20, &LATB, 0x40, &LATB, 0x80};// Signal Leds 1B, 2B and 3B Green and Red

extern void INITxPATHWAY(STATION *reftop, STATION *refbot);
extern void SETxSTATIONxPATHWAY(STATION *self, uint8_t path);
extern void UPDATExSIGNAL(void);
void setSignal(STATION *self);



#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

