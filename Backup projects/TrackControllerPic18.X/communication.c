/* 
 * https://www.microchip.com/en-us/software-library/tcpipstack 
 * (ip.dst==192.168.1.176 and udp.port==60000 and ip.src==192.168.1.50) or (ip.dst==192.168.1.50 and udp.port==60000 and ip.src==192.168.1.176) and !(udp.stream eq 16)
 */
#include <xc.h>
#include <stdbool.h>
#include <time.h>
#include "communication.h"
#include "enums.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/TCPIPLibrary/udpv4.h"
#include "mcc_generated_files/TCPIPLibrary/tcpip_config.h"
//#include "mcc_generated_files/TCPIPLibrary/dhcp_client.h"

static ETH_STATES   ethernetState;
static udpStart_t   udpPacketHeader;

static udpTrans_t   udpReceivedData;
static udpTrans_t   udpRecvBox[MAILBOXSIZE];
static udpTrans_t   udpTransBox[MAILBOXSIZE];

static udpTrans_t   *M_Box_Eth_Recv_Ptr;
static udpTrans_t   *M_Box_Eth_Recv_Ptr_prev;
static udpTrans_t   *M_Box_Eth_Send_Ptr;
static udpTrans_t   *M_Box_Eth_Send_Ptr_prev;

static time_t       Time = 0;
static uint32_t     ip_addr_ntp;

bool         isUdpConnected;

static error_msg    ret = ERROR;

// *****************************************************************************
// Section: Application Local Methods
// *****************************************************************************

void        PutDataInReceiveMailBox (udpTrans_t data);
udpTrans_t *GetDataFromSendMailBox (void);
bool        CheckDataInSendMailBox (void);

// *****************************************************************************
// Section: Application Initialization and State Machine Functions
// *****************************************************************************
// *****************************************************************************

/*******************************************************************************
  Function:
    void ETHERNET_Initialize ( void )

  Remarks:
    See prototype in ethernet.h.
 */

void PROCESSxETHxDATAxINIT ( void )
{
    /* Place the App state machine in its initial state. */
    ethernetState = ETHERNET_LINKUP;
    
    /* used for initial port setup after receiving first message */
    isUdpConnected = false;
    
    /* For internal use to put new data into receive mailbox*/
    M_Box_Eth_Recv_Ptr      = &udpRecvBox[0];
    /* For external use to check for new data in receive mailbox*/
    M_Box_Eth_Recv_Ptr_prev = &udpRecvBox[0];
    /* For internal use to get new data to be send from mailbox*/
    M_Box_Eth_Send_Ptr      = &udpTransBox[0];
    /* For external use to set new data to be send from send mailbox*/
    M_Box_Eth_Send_Ptr_prev = &udpTransBox[0];
    
}


// *****************************************************************************
// Section: Ethernet Application
// *****************************************************************************

void PROCESSxETHxDATA(void)
{   
    /* Manage TCP/IP Stack */
    TP4_SetHigh();
    Network_Manage();    
    TP4_SetLow();
    
    switch(ethernetState){
        case ETHERNET_LINKUP:
            if(ETH_CheckLinkUp()){ //&& DHCP_GET_BOUND()){
                ethernetState = ETHERNET_CONNECT;
            }
            break;
            
        case ETHERNET_CONNECT:
            /* When first data is received from client store the client info */
            if(CHECKxDATAxINxRECEIVExMAILxBOX()){
                udpPacketHeader.destinationAddress    = UDP_GetDestIP();
                udpPacketHeader.destinationPortNumber = UDP_GetDestPort();
                udpPacketHeader.sourceAddress         = UDP_GetSrcIP();
                udpPacketHeader.sourcePortNumber      = UDP_GetSrcPort();
                
                ret = UDP_Start(udpPacketHeader.destinationAddress, udpPacketHeader.sourcePortNumber, udpPacketHeader.destinationPortNumber);
                if(ret == SUCCESS){
                    isUdpConnected = true;
                    ethernetState = ETHERNET_DATA_TX;
                }
            }
            break;
            
//        case ETHERNET_DATA_RX:
//            if(CHECKxDATAxINxRECEIVExMAILxBOX() == true){
//                /* Do something with the data */
//                udpTrans_t *udpSend;
//                udpSend = GETxDATAxFROMxRECEIVExMAILxBOX();
//                PUTxDATAxINxSENDxMAILxBOX(udpSend);
//            }
//            ethernetState = ETHERNET_DATA_TX;
//            break;
            
        case ETHERNET_DATA_TX:
            if (CheckDataInSendMailBox()){
                ret = UDP_Start(udpPacketHeader.destinationAddress, udpPacketHeader.sourcePortNumber, udpPacketHeader.destinationPortNumber);
                if(ret == SUCCESS){
                    udpTrans_t *udpSend;
                    udpSend = GetDataFromSendMailBox();
                    UDP_WriteBlock((char *)udpSend,udpTrans_t_length);
                    UDP_Send();
                }
                else{
                    //printf("ret: %02X\n\r", ret);
                    if(ret == BUFFER_BUSY){
                        //printf("Flush and reset.");
                        UDP_FlushTXPackets();
                        //UDP_FlushRxdPacket(); 
                    }                
                }
            }
            ethernetState = ETHERNET_DATA_TX;
            break;
            
        default: ethernetState = ETHERNET_LINKUP;
        break;
    }
}

/*
 * Catch incoming UDP packets on the specified port
 */
void UDPxDATAxRECV(int16_t length)
{
    if(length == udpTrans_t_length){        
        UDP_ReadBlock(&udpReceivedData,udpTrans_t_length);
        PutDataInReceiveMailBox(udpReceivedData);   
    }
    else{
        UDP_FlushRxdPacket();
    }
}

/******************************************************************************
  Function:
    void CREATExTASKxSTATUSxMESSAGE(uint8_t taskid, uint8_t taskstate, uint8_t feedback)

  Remarks:
    See prototype in ethernet.h.
 */
//static 

//void CREATExTASKxSTATUSxMESSAGE(uint8_t taskid, uint8_t taskstate, uint8_t feedback){
void CREATExTASKxSTATUSxMESSAGE(uint8_t task_id, uint8_t task_command, uint8_t task_state, uint8_t task_messages){
    
    udpTrans_t StatusMessage;
    
    StatusMessage.header  = (uint8_t)HEADER;
    StatusMessage.command = (uint8_t)task_id;
    StatusMessage.data[0] = (uint8_t)task_command;
    StatusMessage.data[1] = (uint8_t)task_state;
    StatusMessage.data[2] = (uint8_t)task_messages;
    
    uint8_t i=0;
    
    for(i=3; i < sizeof(StatusMessage.data); i++){
        StatusMessage.data[i] = 0;
    }
    PUTxDATAxINxSENDxMAILxBOX(&StatusMessage);
}

/******************************************************************************
  Function:
    void PutDataInReceiveMailBox (udpTrans_t data)

  Remarks:
    See prototype in ethernet.h.
 */

void PutDataInReceiveMailBox (udpTrans_t data){
    
    if(data.header == HEADER){
        *M_Box_Eth_Recv_Ptr = data;
        M_Box_Eth_Recv_Ptr++;
        if(M_Box_Eth_Recv_Ptr >= &udpRecvBox[MAILBOXSIZE]){
            M_Box_Eth_Recv_Ptr = &udpRecvBox[0];
        }
    }    
}

/******************************************************************************
  Function:
    udpTrans_t GETxDATAxFROMxRECEIVExMAILxBOX()

  Remarks:
    See prototype in ethernet.h.
 */

udpTrans_t *GETxDATAxFROMxRECEIVExMAILxBOX(){
    
    udpTrans_t *data;
    
    if (M_Box_Eth_Recv_Ptr != M_Box_Eth_Recv_Ptr_prev){
        
        data = M_Box_Eth_Recv_Ptr_prev;
        //memcpy(&data, M_Box_Eth_Recv_Ptr_prev, sizeof(data));
        
        M_Box_Eth_Recv_Ptr_prev++;        
        if(M_Box_Eth_Recv_Ptr_prev >= &udpRecvBox[MAILBOXSIZE]){
            M_Box_Eth_Recv_Ptr_prev = &udpRecvBox[0];
        }
    }
    return (data);     
}

/******************************************************************************
  Function:
    udpTrans_t GETxDATAxFROMxRECEIVExMAILxBOX()

  Remarks:
    See prototype in ethernet.h.
 */

bool CHECKxDATAxINxRECEIVExMAILxBOX(){
    
    if (M_Box_Eth_Recv_Ptr != M_Box_Eth_Recv_Ptr_prev){
        
        return (true);
    }
    else{
        return (false);
    }
}

/******************************************************************************
  Function:
    udpTrans_t GetDataFromSendMailBox ()

  Remarks:
    See prototype in ethernet.h.
 */
udpTrans_t *GetDataFromSendMailBox (){
    
    udpTrans_t *data;
    /* Check if pointers are not equal (check for mail) */
    if (M_Box_Eth_Send_Ptr != M_Box_Eth_Send_Ptr_prev){
        
        //memcpy(&data, M_Box_Eth_Send_Ptr_prev, sizeof(data));
        /* get the data from the send buffer */
        data = M_Box_Eth_Send_Ptr_prev;
        /* increase the prev send pointer */
        M_Box_Eth_Send_Ptr_prev++;
        /* Check if the send prev pointer is still pointing to the buffer */
        if(M_Box_Eth_Send_Ptr_prev >= &udpTransBox[MAILBOXSIZE]){
            /* Reset the pointer to the begin of the buffer */
            M_Box_Eth_Send_Ptr_prev = &udpTransBox[0];
        }
        
    }
    return (data);      
}

/******************************************************************************
  Function:
    udpTrans_t GetDataFromSendMailBox ()

  Remarks:
    See prototype in ethernet.h.
 */
bool CheckDataInSendMailBox (){
    
    if (M_Box_Eth_Send_Ptr != M_Box_Eth_Send_Ptr_prev){
        
        return(true);
    }
    else{
        return (false);
    }  
}

/******************************************************************************
  Function:
    void PUTxDATAxINxSENDxMAILxBOX (udpTrans_t *data)

  Remarks:
    See prototype in ethernet.h.
 */
void PUTxDATAxINxSENDxMAILxBOX (udpTrans_t *data){
    
    /* Park the data in the buffer */
    *M_Box_Eth_Send_Ptr = *data;
    /* increase the pointer to point to next cell */
    M_Box_Eth_Send_Ptr++;
    /* Check if pointer is still pointing within the buffer */
    if(M_Box_Eth_Send_Ptr >= &udpTransBox[MAILBOXSIZE]){
        /* reset pointer to begin of buffer */
        M_Box_Eth_Send_Ptr = &udpTransBox[0];
    } 
}