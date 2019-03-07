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

extern void INITxSPIxCOMMxHANDLER(SLAVE_INFO *location);
extern void RESETxSPIxCOMMxHANDLER(void);
extern void PROCESSxSPIxMODBUS();
extern void PROCESSxSPIxBOOTLOAD();

extern unsigned int SpiSlaveCommErrorCounter;
extern unsigned bool InitPhase;

void ProcessModbusData(void);
void ProcessBootloadData(void);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

