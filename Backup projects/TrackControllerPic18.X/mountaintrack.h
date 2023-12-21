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
#include "enums.h"
    
/*
 * Mountain Station Track struct
 */
typedef struct
{
    TASK_COMMAND                stnName;
    TASK_STATE                  stnState;
    TASK_STATE                  stnSequence;
    TASK_STATE                  stnNextState;
    DEBOUNCE                    *getTrainEnterStnTrack;
    bool                        stnOccupied;
    TASK_MESSAGES               trackNr;
    uint32_t                    tCountTime;
    uint32_t                    tWaitTime;
    
}MNTSTNTRACK;
    
/*
 * Mountain Station struct
 */
typedef struct
{
    TASK_ID                     name;
    TASK_STATE                  AppState;                                       // State of the state meachine
    TASK_STATE                  AppNextState;
    TASK_MESSAGES               LastInboundStn;
    TASK_STATE                  LastState;
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

