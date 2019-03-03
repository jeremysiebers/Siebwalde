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
#include <stdint.h>

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

#define NUMBER_OF_SLAVES    51                                                  // 0 is for the master self. 50 Track slaves => 51  

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

extern uint8_t UPDATE_TERMINAL;
extern uint8_t UPDATE_SLAVE_TOxUDP;

typedef struct
{
    uint32_t destinationAddress;
    uint16_t sourcePortNumber;
    uint16_t destinationPortNumber;    
}udpStart_t;

void UDP_DATA_RECV(int length);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

