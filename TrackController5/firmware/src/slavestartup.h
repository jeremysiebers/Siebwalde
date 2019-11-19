/* 
 * File:   slavestartup.h
 * Author: j.siebers
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef SLAVESTARTUP_H
#define	SLAVESTARTUP_H

#include <xc.h> // include processor files - each processor file is guarded.  

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 
    extern void INITxSLAVExSTARTUP(SLAVE_INFO *location, SLAVE_INFO *Dump);
    extern bool SLAVExINITxANDxCONFIG(void);
    extern unsigned int ENABLExAMPLIFIER(void);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

