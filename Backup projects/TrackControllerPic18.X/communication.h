/* 
 * File:   communication.h
 * Author: jeremy
 *
 * Created on 10 december 2023, 21:19
 */

#ifndef COMMUNICATION_H
#define	COMMUNICATION_H

#ifdef	__cplusplus
extern "C" {
#endif
    
    #include <stdbool.h>
    #include "enums.h"

    #define MAILBOXSIZE 8
    #define UDP_RXTX_PORT 60000
    
    typedef struct
    {
        uint32_t destinationAddress;    // To which IP address to send data back to   
        uint16_t destinationPortNumber; // To which port number to the destinationAddress
        uint32_t sourceAddress;         // The IP address of the uC
        uint16_t sourcePortNumber;      // Which port number to be used from uC         
    }udpStart_t;
   
    typedef struct
    {
        uint8_t header;
        uint8_t command;
        uint8_t data[8]; 
    }udpTrans_t;
    
    const int udpTrans_t_length = sizeof(udpTrans_t);
    
    typedef enum
    {
        /* Application's state machine's initial state. */
        ETHERNET_LINKUP,
        ETHERNET_CONNECT,
        ETHERNET_NTP,
        ETHERNET_DATA_RX,
        ETHERNET_DATA_TX,
        ETHERNET_TCPIP_ERROR,

        /* TODO: Define states used by the application state machine. */

    } ETH_STATES;
    
        
    extern void PROCESSxETHxDATAxINIT(void);
    extern void UDPxDATAxRECV(int16_t length);
    extern void PROCESSxETHxDATA(void);
    
    extern udpTrans_t   *GETxDATAxFROMxRECEIVExMAILxBOX(void);
    extern void         PUTxDATAxINxSENDxMAILxBOX (udpTrans_t *data);
    extern bool         CHECKxDATAxINxRECEIVExMAILxBOX(void);
    extern void         CREATExTASKxSTATUSxMESSAGE(
                            uint8_t task_id, 
                            uint8_t task_command, 
                            uint8_t task_state, 
                            uint8_t task_messages);
    extern bool         isUdpConnected;

    
#ifdef	__cplusplus
}
#endif

#endif	/* COMMUNICATION_H */

