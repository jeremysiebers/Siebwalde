/* 
 * File:   
 * Author: Jeremy Siebers
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef SLAVECOMMHANDLER_H
#define	SLAVECOMMHANDLER_H

#include <xc.h> // include processor files - each processor file is guarded.  
#include "modbus/PetitModbus.h"
#include "enums.h"

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

REGISTER_PROCESSING     Data;
    
extern void             INITxCOMMxHANDLER(SLAVE_INFO *location, SLAVE_INFO *Dump);
extern void             SLAVExCOMMUNICATIONxHANDLER(void);
extern uint8_t          CHECKxMODBUSxCOMMxSTATUS(uint8_t SlaveId, bool OverWrite);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

