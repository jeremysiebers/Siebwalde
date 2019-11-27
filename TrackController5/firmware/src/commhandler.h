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

typedef struct
{
    uint8_t  SlaveAddress;
    uint8_t  Direction;
    uint8_t  NoOfRegisters;    
    uint8_t  StartRegister;
    uint16_t RegData0;
    uint16_t RegData1;    
}REGISTER_PROCESSING;

REGISTER_PROCESSING        Data;
    
extern void             INITxCOMMxHANDLER(SLAVE_INFO *location, SLAVE_INFO *Dump);
extern void             SLAVExCOMMUNICATIONxHANDLER(void);
extern uint8_t          CHECKxMODBUSxCOMMxSTATUS(uint8_t SlaveId, bool OverWrite);

enum reg
{
    HOLDINGREG0         = 0, 
    HOLDINGREG1         = 1, 
    HOLDINGREG2         = 2, 
    HOLDINGREG3         = 3, 
    HOLDINGREG4         = 4, 
    HOLDINGREG5         = 5, 
    HOLDINGREG6         = 6, 
    HOLDINGREG7         = 7, 
    HOLDINGREG8         = 8, 
    HOLDINGREG9         = 9, 
    HOLDINGREG10        = 10,
    HOLDINGREG11        = 11,
    HOLDINGREG12        = 12,
    READ                 = 0x55,    
    WRITE                = 0xAA,
            
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

