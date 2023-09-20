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

enum STATES{
    INIT,
    RUN        
};
/*
 * Station struct
*/
typedef struct
{
    enum STATES                 state;                                          // State of the state meachine
    DEBOUNCE                    *freightLeftStation;
    DEBOUNCE                    *freightEntStation;
        
}STATION;

STATION top;
STATION bot;
STATION mnt;

extern void INITxSTATION(void);
extern void UPDATExSTATION(STATION *instance);

#ifdef	__cplusplus
extern "C" {
#endif

#ifdef	__cplusplus
}
#endif

#endif	/* STATION_H */

