/* 
 * File:   station.h
 * Author: jerem
 *
 * Created on September 20, 2023, 8:21 PM
 */

#ifndef STATION_H
#define	STATION_H

#include <xc.h> // include processor files - each processor file is guarded.
#include <stdbool.h>
#include "debounce.h"
#include "pathway.h"

enum STATES{
    INIT,
    RUN        
};

typedef struct
{
    volatile unsigned char      *portx_ptr;                                     // Reference to the input port used
    uint8_t                     pin_mask;                                       // Mask to point to pin used of port
}OCC;

/*
 * Station struct
*/
typedef struct
{
    enum STATES                 state;                                          // State of the state meachine
    DEBOUNCE                    *getFreightLeaveStation;
    DEBOUNCE                    *getFreightEnterStation;
    OCC                         *setOccBlkIn;
    DEBOUNCE                    *getOccBlkOut;
    OCC                         *setOccStn1;
    DEBOUNCE                    *getOccStn1;
    OCC                         *setOccStn2;
    DEBOUNCE                    *getOccStn2;
    OCC                         *setOccStn3;
    DEBOUNCE                    *getOccStn3;
    WS                          *setPath;
    uint8_t                     prevPath;
    SIG                         *setSignal;
            
}STATION;

STATION top;
STATION bot;

OCC OCC_TO_21B    = {&LATD, 0x40};
OCC OCC_TO_9B     = {&LATD, 0x80};
OCC OCC_TO_STN_1  = {&LATJ, 0x10};
OCC OCC_TO_STN_2  = {&LATJ, 0x20};
OCC OCC_TO_STN_3  = {&LATJ, 0x40};
OCC OCC_TO_STN_10 = {&LATJ, 0x80};
OCC OCC_TO_STN_11 = {&LATE, 0x1};
OCC OCC_TO_STN_12 = {&LATE, 0x2};
OCC OCC_TO_T6     = {&LATE, 0x4};
OCC OCC_TO_T3     = {&LATE, 0x8};
/* OCC_FR_x goes via debounce function */
//OCC OCC_FR_BLK13  = {&PORTH, 0x4};
//OCC OCC_FR_BLK4   = {&PORTH, 0x8};
//OCC OCC_FR_STN_1  = {&PORTH, 0x10};
//OCC OCC_FR_STN_2  = {&PORTH, 0x20};
//OCC OCC_FR_STN_3  = {&PORTH, 0x30};
//OCC OCC_FR_STN_10 = {&PORTH, 0x80};
//OCC OCC_FR_STN_11 = {&PORTG, 0x1};
//OCC OCC_FR_STN_12 = {&PORTG, 0x2};
//OCC OCC_FR_STN_T6 = {&PORTG, 0x4};
//OCC OCC_FR_STN_T3 = {&PORTG, 0x8};


extern void INITxSTATION(void);
extern void UPDATExSTATION(STATION *instance);

#ifdef	__cplusplus
extern "C" {
#endif

#ifdef	__cplusplus
}
#endif

#endif	/* STATION_H */

