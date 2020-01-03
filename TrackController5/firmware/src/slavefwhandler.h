/* 
 * File:   slavestartup.h
 * Author: j.siebers
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef SLAVEFWHANDLER_H
#define	SLAVEFWHANDLER_H

#include <xc.h> // include processor files - each processor file is guarded.  
#include "enums.h"

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    typedef enum
    {
        /* Application's state machine's initial state. */
        FW_STATE_INIT=0,
        FW_STATE_WAITING_FOR_COMMAND,
        FW_STATE_COMMAND_HANDLING,
    } FW_HANDLER_STATES;
    
    typedef struct
    {
        /* The application's data */
        FW_HANDLER_STATES           state;
        uint32_t                    command;
        uint8_t                     datacount;
        uint8_t                     buffer[32];
        bool                        bootloader_receive_error;
        uint16_t                    fwchecksum;
        bool                        SlaveBootloaderHandlingActive;
    } FW_DATA;

    FW_DATA fwData;
    
    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 
    extern void INITxSLAVExFWxHANDLER(SLAVE_INFO *location, SLAVE_INFO *Dump);
    extern bool SLAVExFWxHANDLER(void);
    extern void SLAVExBOOTLOADERxDATAxRETURN(uint8_t data);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

