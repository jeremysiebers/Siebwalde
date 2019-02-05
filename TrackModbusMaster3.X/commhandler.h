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

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */
    
extern void             InitSlaveCommunication(SLAVE_INFO *location);
extern void             ProcessNextSlave(void);
extern unsigned int     ProcessSlaveCommunication();
extern void             SendDataToEthernet(void);
extern void             SLAVExCOMMANDxHANDLER(uint16_t State);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

