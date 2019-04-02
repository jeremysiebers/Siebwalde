/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef SPICOMMHANDLER_H
#define	SPICOMMHANDLER_H

#include <xc.h> // include processor files - each processor file is guarded.  
#include "main.h"

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */
    
const uint8_t DATAxSTRUCTxLENGTH = sizeof(SLAVE_INFO);

extern void INITxSPIxCOMMxHANDLER(SLAVE_INFO *location);
extern void RESETxSPIxCOMMxHANDLER(void);
extern void PROCESSxSPIxMODBUS();

extern unsigned int SpiSlaveCommErrorCounter;
extern unsigned bool InitPhase;

extern void PROCESSxMODBUSxDATA(void);

//extern uint8_t RECEIVED_BOOT_LOAD_DATA[DATAxSTRUCTxLENGTH+1];
//extern uint8_t SEND_BOOT_LOAD_DATA[DATAxSTRUCTxLENGTH];

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

