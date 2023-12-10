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
    
    extern void UDPxDATAxRECV(int length);
    extern void PROCESSxETHxDATA(void);
    
    typedef struct
    {
        uint32_t destinationAddress;    // To which IP address to send data back to   
        uint16_t destinationPortNumber; // To which port number to the destinationAddress
        uint32_t sourceAddress;         // The IP address of the uC
        uint16_t sourcePortNumber;      // Which port number to be used from uC         
    }udpStart_t;
    
    typedef struct
    {
        uint8_t command;
        uint8_t action;
    }udpRecv_t;
    
#ifdef	__cplusplus
}
#endif

#endif	/* COMMUNICATION_H */

