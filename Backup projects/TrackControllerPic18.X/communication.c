/* https://www.microchip.com/en-us/software-library/tcpipstack */
#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/TCPIPLibrary/udpv4.h"
#include "mcc_generated_files/TCPIPLibrary/tcpip_config.h"
#include "communication.h"

uint8_t ProcessEthData = false;
uint8_t data=0;

error_msg ret = ERROR;

static udpStart_t   udpPacketHeader;
static udpRecv_t    udpReceivedData;

void PROCESSxETHxDATA(void)
{   
    /* Manage TCP/IP Stack */
    TP4_SetHigh();
    Network_Manage();    
    TP4_SetLow();
    
    if(true == ProcessEthData){
        ProcessEthData = false;

        udpPacketHeader.destinationAddress    = UDP_GetDestIP();
        udpPacketHeader.destinationPortNumber = UDP_GetDestPort();
        udpPacketHeader.sourceAddress         = UDP_GetSrcIP();
        udpPacketHeader.sourcePortNumber      = UDP_GetSrcPort();
                
        ret = UDP_Start(udpPacketHeader.destinationAddress, udpPacketHeader.sourcePortNumber, udpPacketHeader.destinationPortNumber);
        if(ret == SUCCESS){
            UDP_WriteBlock(&udpReceivedData,sizeof(udpRecv_t));
            UDP_Send();
        }
    }
}

/*
 * 
 */
void UDPxDATAxRECV(int length)
{    
    UDP_ReadBlock(&udpReceivedData,sizeof(udpRecv_t));
    ProcessEthData = true;
}