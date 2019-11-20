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
    
extern void             INITxCOMMxHANDLER(SLAVE_INFO *location, SLAVE_INFO *Dump);
extern void             SLAVExCOMMUNICATIONxHANDLER(uint8_t SlaveAddress, uint8_t Register, uint8_t WriteRead, uint8_t *Data, uint8_t length);
extern uint8_t          CHECKxMODBUSxCOMMxSTATUS(uint8_t SlaveId, bool OverWrite);

enum reg
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
    Write                = 0xAA,
            
    SLOT1  = 0x1,
    SLOT2  = 0x2,
    SLOT3  = 0x4,
    SLOT4  = 0x8,
    SLOT5  = 0x10,
    SLOT6  = 0x20,
    SLOT7  = 0x40,
    SLOT8  = 0x80,
    SLOT9  = 0x100,
    SLOT10 = 0x200,
    TRACKBACKPLANE1 = 51,
    TRACKBACKPLANE2 = 52,
    TRACKBACKPLANE3 = 53,
    TRACKBACKPLANE4 = 54,
    TRACKBACKPLANE5 = 55,
    SLAVE_INITIAL_ADDR = 0xAA,
    BROADCAST_ADDRESS = 0,
    WAIT      = 99,
    SLAVEOK   = 100,
    SLAVENOK  = 101,
    SLAVEBUSY = 102,
};

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

