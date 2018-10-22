/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef XC_HEADER_TEMPLATE_H
#define	XC_HEADER_TEMPLATE_H

#include <xc.h> // include processor files - each processor file is guarded.  


#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

#define NUMBER_OF_SLAVES    5   

typedef enum
{
    SLAVE_DATA_IDLE = 0,
    SLAVE_DATA_BUSY = 1,
    SLAVE_DATA_OK = 2,
    SLAVE_DATA_NOK = 3,
    SLAVE_DATA_TIMEOUT = 4,
    SLAVE_DATA_EXCEPTION = 5
}SLAVE_DATA;

typedef struct
{
    unsigned char       Header;
    unsigned char       SlaveNumber;
    unsigned int        HoldingReg[4];
    unsigned int        InputReg[6];
    unsigned int        DiagReg[2];
    unsigned int        MbReceiveCounter;
    unsigned int        MbSentCounter;
    SLAVE_DATA          MbCommError;
    unsigned char       MbExceptionCode;
    unsigned int        SpiCommErrorCounter;
    unsigned char       Footer;
    
}SLAVE_INFO;

extern unsigned char UPDATExTERMINAL;

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

