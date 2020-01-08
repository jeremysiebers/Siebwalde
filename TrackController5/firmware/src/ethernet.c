/*******************************************************************************
  MPLAB Harmony Application Source File
  
  Company:
    Microchip Technology Inc.
  
  File Name:
    ethernet.c

  Summary:
    This file contains the source code for the MPLAB Harmony application.

  Description:
    This file contains the source code for the MPLAB Harmony application.  It 
    implements the logic of the application's state machine and it may call 
    API routines of other MPLAB Harmony modules in the system, such as drivers,
    system services, and middleware.  However, it does not call any of the
    system interfaces (such as the "Initialize" and "Tasks" functions) of any of
    the modules in the system or make any assumptions about when those functions
    are called.  That is the responsibility of the configuration-specific system
    files.
 *******************************************************************************/

// DOM-IGNORE-BEGIN
/*******************************************************************************
Copyright (c) 2013-2014 released Microchip Technology Inc.  All rights reserved.

Microchip licenses to you the right to use, modify, copy and distribute
Software only when embedded on a Microchip microcontroller or digital signal
controller that is integrated into your product or third party product
(pursuant to the sublicense terms in the accompanying license agreement).

You should refer to the license agreement accompanying this Software for
additional information regarding your rights and obligations.

SOFTWARE AND DOCUMENTATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION, ANY WARRANTY OF
MERCHANTABILITY, TITLE, NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.
IN NO EVENT SHALL MICROCHIP OR ITS LICENSORS BE LIABLE OR OBLIGATED UNDER
CONTRACT, NEGLIGENCE, STRICT LIABILITY, CONTRIBUTION, BREACH OF WARRANTY, OR
OTHER LEGAL EQUITABLE THEORY ANY DIRECT OR INDIRECT DAMAGES OR EXPENSES
INCLUDING BUT NOT LIMITED TO ANY INCIDENTAL, SPECIAL, INDIRECT, PUNITIVE OR
CONSEQUENTIAL DAMAGES, LOST PROFITS OR LOST DATA, COST OF PROCUREMENT OF
SUBSTITUTE GOODS, TECHNOLOGY, SERVICES, OR ANY CLAIMS BY THIRD PARTIES
(INCLUDING BUT NOT LIMITED TO ANY DEFENSE THEREOF), OR OTHER SIMILAR COSTS.
 *******************************************************************************/
// DOM-IGNORE-END


// *****************************************************************************
// *****************************************************************************
// Section: Included Files 
// *****************************************************************************
// *****************************************************************************

#include "ethernet.h"
#include "tcpip/tcpip.h"
#include "enums.h"

// *****************************************************************************
// *****************************************************************************
// Section: Global Data Definitions
// *****************************************************************************
// *****************************************************************************

// *****************************************************************************
/* Application Data

  Summary:
    Holds application data

  Description:
    This structure holds the application's data.

  Remarks:
    This structure should be initialized by the ETHERNET_Initialize function.
    
    Application strings and buffers are be defined outside this structure.
*/
#define SERVER_PORT 10000
#define CLIENT_PORT 10001
#define CMD2ASCII   10
#define MAILBOXSIZE 8

ETHERNET_DATA ethernetData;
UDP_SOCKET_INFO SocketInfo;
IP_MULTI_ADDRESS IpAddress;

const char          *netName, *netBiosName;
static IPV4_ADDR    dwLastIP[2] = { {-1}, {-1} };
IPV4_ADDR           ipAddr;
int                 i, nNets;
TCPIP_NET_HANDLE    netH;

static bool         TransSocketReady = false;

static udpTrans_t   *M_Box_Eth_Recv_Ptr;
static udpTrans_t   *M_Box_Eth_Recv_Ptr_prev;
static udpTrans_t   *M_Box_Eth_Send_Ptr;
static udpTrans_t   *M_Box_Eth_Send_Ptr_prev;

static udpTrans_t   udpRecvBox[MAILBOXSIZE];
static udpTrans_t   udpTransBox[MAILBOXSIZE];

//unsigned char *Cmd2Ascii[] = {
//    "NOP",
//    "EXEC_MBUS_STATE_SLAVES_ON",        
//    "EXEC_MBUS_STATE_SLAVE_DETECT",     
//    "EXEC_MBUS_STATE_SLAVES_BOOT_WAIT", 
//    "EXEC_MBUS_STATE_SLAVE_FW_FLASH",
//    "EXEC_MBUS_STATE_SLAVE_INIT",       
//    "EXEC_MBUS_STATE_SLAVE_ENABLE",     
//    "EXEC_MBUS_STATE_START_DATA_UPLOAD",
//    "EXEC_MBUS_STATE_RESET",
//    "EXEC_FW_STATE_RECEIVE_FW_FILE",
//    "EXEC_FW_STATE_RECEIVE_FW_FILE_STANDBY",
//    "EXEC_FW_STATE_FW_DATA",
//    "EXEC_FW_STATE_FW_DATA_DOWNLOAD_DONE",
//    "CLIENT_CONNECTION_REQUEST"
//};

// *****************************************************************************
// *****************************************************************************
// Section: Application Callback Functions
// *****************************************************************************
// *****************************************************************************

/* TODO:  Add any necessary callback functions.
*/

// *****************************************************************************
// *****************************************************************************
// Section: Application Local Functions
// *****************************************************************************
// *****************************************************************************


void        PutDataInReceiveMailBox (udpTrans_t data);
udpTrans_t *GetDataFromSendMailBox ();
bool        CheckDataInSendMailBox ();


// *****************************************************************************
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

void ETHERNET_Initialize ( void )
{
    /* Place the App state machine in its initial state. */
    ethernetData.state = ETHERNET_TCPIP_WAIT_INIT;
    
    /* For internal use to put new data into receive mailbox*/
    M_Box_Eth_Recv_Ptr      = &udpRecvBox[0];
    /* For external use to check for new data in receive mailbox*/
    M_Box_Eth_Recv_Ptr_prev = &udpRecvBox[0];
    /* For internal use to get new data to be send from mailbox*/
    M_Box_Eth_Send_Ptr      = &udpTransBox[0];
    /* For external use to set new data to be send from send mailbox*/
    M_Box_Eth_Send_Ptr_prev = &udpTransBox[0];
    
    SYS_MESSAGE("\033[2J\033[1;1H");//clear terminal
    SYS_MESSAGE("Starting Siebwalde TrackController...\n\r");
}


/******************************************************************************
  Function:
    void ETHERNET_Tasks ( void )

  Remarks:
    See prototype in ethernet.h.
 */

void ETHERNET_Tasks ( void )
{

    /* Check the application's current state. */
    switch ( ethernetData.state )
    {
        /* Application's initial state. */
        case ETHERNET_TCPIP_WAIT_INIT:
        {
            ethernetData.tcpipStat = TCPIP_STACK_Status(sysObj.tcpip);
            if(ethernetData.tcpipStat < 0)
            {   // some error occurred
                SYS_MESSAGE("Ethernet\t: TCP/IP stack initialization failed!\r\n");
                ethernetData.state = ETHERNET_TCPIP_ERROR;
            }
            else if (ethernetData.tcpipStat == SYS_STATUS_READY){
                // now that the stack is ready we can check the 
                // available interfaces 
                nNets = TCPIP_STACK_NumberOfNetworksGet();
                for(i = 0; i < nNets; i++)
                {

                    netH = TCPIP_STACK_IndexToNet(i);
                    netName = TCPIP_STACK_NetNameGet(netH);
                    netBiosName = TCPIP_STACK_NetBIOSName(netH);

#if defined(TCPIP_STACK_USE_NBNS)
                    SYS_PRINT("Ethernet\t: Interface %s on host %s - NBNS enabled\r\n", netName, netBiosName);
#else
                    SYS_PRINT("    Interface %s on host %s - NBNS disabled\r\n", netName, netBiosName);
#endif  // defined(TCPIP_STACK_USE_NBNS)

                }
                ethernetData.state = ETHERNET_TCPIP_WAIT_FOR_IP;
            }
            break;
        }
            
        case ETHERNET_TCPIP_WAIT_FOR_IP:

            // if the IP address of an interface has changed
            // display the new value on the system console
            nNets = TCPIP_STACK_NumberOfNetworksGet();

            for (i = 0; i < nNets; i++)
            {
                netH = TCPIP_STACK_IndexToNet(i);
                
				if(!TCPIP_STACK_NetIsReady(netH))
				{
					return; // interface not ready yet!
				}
							
				ipAddr.Val = TCPIP_STACK_NetAddress(netH);
                if(dwLastIP[i].Val != ipAddr.Val)
                {
                    dwLastIP[i].Val = ipAddr.Val;
                    SYS_MESSAGE("Ethernet\t: ");
                    SYS_MESSAGE(TCPIP_STACK_NetNameGet(netH));
                    SYS_MESSAGE(" IP Address: ");
                    SYS_PRINT("%d.%d.%d.%d \r\n", ipAddr.v[0], ipAddr.v[1], ipAddr.v[2], ipAddr.v[3]);                                     
                }
            }
			// all interfaces ready. Could start transactions!!!
			ethernetData.state = ETHERNET_TCPIP_OPENING_SERVER;  
            break;
			
        case ETHERNET_TCPIP_OPENING_SERVER:
        {
            SYS_PRINT("Ethernet\t: Waiting for Client Connection on port: %d\r\n", SERVER_PORT);
            ethernetData.recvsocket = TCPIP_UDP_ServerOpen(IP_ADDRESS_TYPE_IPV4, SERVER_PORT, 0);
            if (ethernetData.recvsocket == INVALID_SOCKET)
            {
                SYS_MESSAGE("Ethernet\t: Couldn't open server recvsocket\r\n");
                break;
            }
            ethernetData.state = ETHERNET_TCPIP_WAIT_FOR_CONNECTION;
        }
        break;

        case ETHERNET_TCPIP_WAIT_FOR_CONNECTION:
        {
            if (!TCPIP_UDP_IsConnected(ethernetData.recvsocket))
            {
                return;
            }
            else
            {                
                // We got a connection
                ethernetData.state = ETHERNET_TCPIP_DATA_RX;
                SYS_MESSAGE("Ethernet\t: Received a connection\r\n");

                if (!TransSocketReady){//TCPIP_UDP_IsConnected(ethernetData.transsocket)){
                    TCPIP_UDP_SocketInfoGet(ethernetData.recvsocket, &SocketInfo);                
                    IpAddress = SocketInfo.remoteIPaddress;                
                    ethernetData.transsocket = TCPIP_UDP_ClientOpen(IP_ADDRESS_TYPE_IPV4, 
                            CLIENT_PORT, &SocketInfo.remoteIPaddress);
                    if (ethernetData.transsocket == INVALID_SOCKET)
                    {
                        SYS_MESSAGE("Ethernet\t: Couldn't open client transsocket\r\n");
                        TCPIP_UDP_Discard(ethernetData.recvsocket);
                        TCPIP_UDP_Discard(ethernetData.transsocket);
                        TransSocketReady = false;
                        break;
                    }
                    else{
                        TransSocketReady = true;
                    }
                }
                
            }
        }
        break;

        case ETHERNET_TCPIP_DATA_RX:
        {
            if (!TCPIP_UDP_IsConnected(ethernetData.recvsocket))
            {
                TCPIP_UDP_Discard(ethernetData.recvsocket);
                TCPIP_UDP_Close(ethernetData.recvsocket);
                ethernetData.state = ETHERNET_TCPIP_OPENING_SERVER;
                SYS_MESSAGE("Ethernet\t: Connection was closed\r\n");
                break;
            }
            
            int16_t wMaxGet, wMaxPut;
            udpTrans_t udpRecv;
            memset(&udpRecv, 0, sizeof(udpTrans_t));
                        
            // Figure out how many bytes have been received and how many we can transmit.
            wMaxGet = TCPIP_UDP_GetIsReady(ethernetData.recvsocket);	// Get UDP RX FIFO byte count
            wMaxPut = TCPIP_UDP_PutIsReady(ethernetData.transsocket);

            //SYS_CONSOLE_PRINT("\t%d bytes are available.\r\n", wMaxGet);
            if (wMaxGet == 0)
            {
                TCPIP_UDP_Discard(ethernetData.recvsocket);
                ethernetData.state = ETHERNET_TCPIP_DATA_TX;
                break;
            }
            
            // Transfer the data out of the TCP RX FIFO and into our local processing buffer.
            int32_t rxed = TCPIP_UDP_ArrayGet(ethernetData.recvsocket, (char *)&udpRecv, sizeof(udpTrans_t));
            PutDataInReceiveMailBox(udpRecv);
//            if(udpRecv.command < CMD2ASCII){
//                unsigned char *ptr = Cmd2Ascii[udpRecv.command];
//                SYS_PRINT("Received command: '%s' (length %d)\r\n", ptr, rxed);
//            }
            ethernetData.state = ETHERNET_TCPIP_DATA_TX;
        }
        break;
        
        case ETHERNET_TCPIP_DATA_TX:
        {
            if (!TransSocketReady)
            {
                ethernetData.state = ETHERNET_TCPIP_WAIT_FOR_CONNECTION;
                TransSocketReady = false;
                //SYS_MESSAGE("No transsocket connected\r\n");
                break;
            }
            
            if (CheckDataInSendMailBox()){
                udpTrans_t *udpSend;
                udpSend = GetDataFromSendMailBox();
                TCPIP_UDP_ArrayPut(ethernetData.transsocket, &udpSend->header, sizeof(udpTrans_t));
                TCPIP_UDP_Flush(ethernetData.transsocket);                
            }            
            ethernetData.state = ETHERNET_TCPIP_DATA_RX;
        }
        break;
        
        /* The default state should never be executed. */
        default:
        {
            /* TODO: Handle error in application's state machine. */
            break;
        }
    }
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
    
    if (M_Box_Eth_Send_Ptr != M_Box_Eth_Send_Ptr_prev){
        
        //memcpy(&data, M_Box_Eth_Send_Ptr_prev, sizeof(data));
        data = M_Box_Eth_Send_Ptr_prev;
        
        M_Box_Eth_Send_Ptr_prev++;
        if(M_Box_Eth_Send_Ptr_prev >= &udpTransBox[MAILBOXSIZE]){
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
    
    *M_Box_Eth_Send_Ptr = *data;
    M_Box_Eth_Send_Ptr++;
    
    if(M_Box_Eth_Send_Ptr >= &udpTransBox[MAILBOXSIZE]){
        M_Box_Eth_Send_Ptr = &udpTransBox[0];
    } 
}

/******************************************************************************
  Function:
    uint32_t GETxETHERNETxSTATE (void)

  Remarks:
    See prototype in mbus.h.
 */

uint32_t GETxETHERNETxSTATE (void){
    return (ethernetData.state);
}

/******************************************************************************
  Function:
    void CREATExTASKxSTATUSxMESSAGE(uint8_t taskid, uint8_t taskstate, uint8_t feedback)

  Remarks:
    See prototype in ethernet.h.
 */
//static 

void CREATExTASKxSTATUSxMESSAGE(uint8_t taskid, uint8_t taskstate, uint8_t feedback){
    
    udpTrans_t StatusMessage;
    
    StatusMessage.header = HEADER;
    StatusMessage.command = taskid;
    StatusMessage.data[0] = taskstate;
    StatusMessage.data[1] = feedback;
    
    uint8_t i=0;
    
    for(i=2; i < sizeof(StatusMessage.data); i++){
        StatusMessage.data[i] = 0;
    }
    PUTxDATAxINxSENDxMAILxBOX(&StatusMessage);
}

/******************************************************************************
  Function:
    void DISCONNECTxCLIENT()

  Remarks:
    See prototype in ethernet.h.
 */
//static 

void DISCONNECTxCLIENT(){
    
    TCPIP_UDP_Discard(ethernetData.recvsocket);
    TCPIP_UDP_Close(ethernetData.recvsocket);
    TCPIP_UDP_Discard(ethernetData.transsocket);
    TCPIP_UDP_Close(ethernetData.transsocket);
    ethernetData.state = ETHERNET_TCPIP_OPENING_SERVER;
    SYS_MESSAGE(" Ethernet:    Connection was closed\r\n");    
}

/*******************************************************************************
 End of File
 */