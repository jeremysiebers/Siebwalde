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
    DEBOUNCE                    *getTrainEnterStnTrack;
    bool                        stnOccupied;
    enum MNTSTATION_NAMES       trackNr;
    uint32_t                    tCountTime;
    uint32_t                    tWaitTime;
    
}MNTSTNTRACK;
    
/*
 * Mountain Station struct
 */
typedef struct
{
    enum MNTSTATION_NAMES       name;
    enum STATES                 AppState;                                       // State of the state meachine
    enum STATES                 AppNextState;
    enum MNTSTATION_NAMES       LastInboundStn;
    enum STATES                 LastState;
    DEBOUNCE                    *getTrainEnterSiebwaldeStn;
    OCC                         *setOccAmpOut;
    DEBOUNCE                    *getOccAmpOut;    
    WS                          *setPath;
    MNTSTNTRACK                 stnTrack1;
    MNTSTNTRACK                 stnTrack2;
    uint32_t                    tCountTime;
    uint32_t                    tWaitTime;
            
}MNTSTATION;

MNTSTATION waldsee;
//MNTSTATION siebwalde;
MNTSTATION waldberg;

OCC OCC_TO_T6       = {&LATE, 0x4};
OCC OCC_TO_T3       = {&LATE, 0x8};
REL REL_T4          = {&LATD, 0x10};
REL REL_T5          = {&LATD, 0x20};

extern void INITxMOUNTAINxSTATION(void);
extern void UPDATExMOUNTAINxSTATION(MNTSTATION *self);
extern void UPDATExMOUNTAINxTRAINxWAIT(MNTSTATION *self);

#ifdef	__cplusplus
}
#endif

#endif	/* MOUNTAINTRACK_H */

