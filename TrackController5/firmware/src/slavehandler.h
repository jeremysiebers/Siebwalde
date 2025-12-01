/* 
 * File:   
 * Author: Jeremy Siebers
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef SLAVEHANDLER_H
#define	SLAVEHANDLER_H

#include <xc.h> // include processor files - each processor file is guarded.  
#include "modbus/PetitModbus.h"

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */
    
extern void     INITxSLAVExHANDLER                  (SLAVE_INFO *location, SLAVE_INFO *Dump);
extern bool     PROCESSxNEXTxSLAVE                  (void);
extern bool     PROCESSxSLAVExCOMMUNICATION         (void);
extern void     ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX   (uint8_t *data);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

