/* 
 * File:   
 * Author: Jeremy Siebers
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef COMMHANDLER_H
#define	COMMHANDLER_H

#include <xc.h> // include processor files - each processor file is guarded.  
#include "modbus/PetitModbus.h"

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */
    
extern void             INITXSLAVEXCOMMUNICATION(SLAVE_INFO *location, SLAVE_INFO *Dump);
extern void             PROCESSxNEXTxSLAVE(void);
extern bool             PROCESSxSLAVExCOMMUNICATION(uint8_t SlaveAddress);
extern void             SLAVExCOMMUNICATIONxHANDLER(uint8_t SlaveAddress, uint8_t Register, uint8_t WriteRead, uint8_t *Data, uint8_t length);

typedef enum
{
    HoldingReg0          = 0, 
    HoldingReg1          = 1, 
    HoldingReg2          = 2, 
    HoldingReg3          = 3, 
    HoldingReg4          = 4, 
    HoldingReg5          = 5, 
    HoldingReg6          = 6, 
    HoldingReg7          = 7, 
    HoldingReg8          = 8, 
    HoldingReg9          = 9, 
    HoldingReg10         = 10, 
    HoldingReg11         = 11, 
    HoldingReg12         = 12,
    Read                 = 0x55,
    Write                = 0xAA
}Reg;

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

