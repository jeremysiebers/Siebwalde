/* 
 * File:   mountaintrack.h
 * Author: jerem
 *
 * Created on 4 december 2023, 20:41
 */

#ifndef MOUNTAINTRACK_H
#define	MOUNTAINTRACK_H

#ifdef	__cplusplus
extern "C" {
#endif

#include <xc.h> // include processor files - each processor file is guarded.
#include <stdbool.h>
#include "debounce.h"
#include "main.h"

enum MNTSTATION_NAMES{
WALDSEE,
SIEBWALDE,
WALDBERG,
T1,
T2,
T4,
T5,
T7,
T8
};   
    
/*
 * Mountain Station Track struct
 */
typedef struct
{
    enum STATES                 stnState;
    enum STATES                 stnSequence;
    enum STATES                 stnNextState;
    bool                        stnOccupied;
    uint8_t                     trackNr;
    uint32_t                    tCountTime;
    uint32_t                    tWaitTime;
    
}MNTSTNTRACK;
    
/*
 * Mountain Station struct
 */
typedef struct
{
    uint8_t                     name;
    enum STATES                 AppState;                                       // State of the state meachine
    enum STATES                 hndlState;
    DEBOUNCE                    *getTrainEnterStationTrack1;
    DEBOUNCE                    *getTrainEnterStationTrack2;
    OCC                         *setOccAmpOut;
    DEBOUNCE                    *getOccAmpOut;
    OCC                         *setOccAmpIn;
    DEBOUNCE                    *getOccAmpIn;
    WS                          *setPath;
    MNTSTNTRACK                 mntStnTrack1;
    MNTSTNTRACK                 mntStnTrack2;
    uint32_t                    tCountTime;
    uint32_t                    tWaitTime;
            
}MNTSTATION;

MNTSTATION waldsee;
MNTSTATION siebwalde;
MNTSTATION waldberg;

OCC OCC_TO_T6       = {&LATE, 0x4};
OCC OCC_TO_T3       = {&LATE, 0x8};
REL REL_T4          = {&LATD, 0x10};
REL REL_T5          = {&LATD, 0x20};

extern void INITxMOUNTAINxSTATION(void);
extern void UPDATExMOUNTAINxSTATION(MNTSTATION *self);
extern void UPDATExMOUNTAINxTRAINxWAIT(void);

#ifdef	__cplusplus
}
#endif

#endif	/* MOUNTAINTRACK_H */

