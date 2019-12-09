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
#include "mbus.h"

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

ETHERNET_DATA ethernetData;

const char          *netName, *netBiosName;
static IPV4_ADDR    dwLastIP[2] = { {-1}, {-1} };
IPV4_ADDR           ipAddr;
int                 i, nNets;
TCPIP_NET_HANDLE    netH;

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


/* TODO:  Add any necessary local functions.
*/
uint32_t ReadCoreTimer(void);


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
                SYS_MESSAGE(" ETHERNET: TCP/IP stack initialization failed!\r\n");
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
                    SYS_PRINT("    Interface %s on host %s - NBNS enabled\r\n", netName, netBiosName);
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
            SYS_PRINT("Waiting for Client Connection on port: %d\r\n", SERVER_PORT);
            ethernetData.socket = TCPIP_UDP_ServerOpen(IP_ADDRESS_TYPE_IPV4, SERVER_PORT, 0);
            if (ethernetData.socket == INVALID_SOCKET)
            {
                SYS_MESSAGE("Couldn't open server socket\r\n");
                break;
            }
            ethernetData.state = ETHERNET_TCPIP_WAIT_FOR_CONNECTION;
        }
        break;

        case ETHERNET_TCPIP_WAIT_FOR_CONNECTION:
        {
            if (!TCPIP_UDP_IsConnected(ethernetData.socket))
            {
                return;
            }
            else
            {
                // We got a connection
                ethernetData.state = ETHERNET_TCPIP_SERVING_CONNECTION;
                SYS_MESSAGE("Received a connection\r\n");
            }
        }
        break;

        case ETHERNET_TCPIP_SERVING_CONNECTION:
        {
            if (!TCPIP_UDP_IsConnected(ethernetData.socket))
            {
                ethernetData.state = ETHERNET_TCPIP_WAIT_FOR_CONNECTION;
                SYS_MESSAGE("Connection was closed\r\n");
                break;
            }
            int16_t wMaxGet, wMaxPut, wCurrentChunk;
            uint16_t w, w2;
            uint8_t AppBuffer[32];
            memset(AppBuffer, 0, 32);
            // Figure out how many bytes have been received and how many we can transmit.
            wMaxGet = TCPIP_UDP_GetIsReady(ethernetData.socket);	// Get UDP RX FIFO byte count
            wMaxPut = TCPIP_UDP_PutIsReady(ethernetData.socket);

            //SYS_CONSOLE_PRINT("\t%d bytes are available.\r\n", wMaxGet);
            if (wMaxGet == 0)
            {
                break;
            }

            if (wMaxPut < wMaxGet)
            {
                wMaxGet = wMaxPut;
            }

            SYS_PRINT("RX Buffer has %d bytes in it\n", wMaxGet);

            // Process all bytes that we can
            // This is implemented as a loop, processing up to sizeof(AppBuffer) bytes at a time.
            // This limits memory usage while maximizing performance.  Single byte Gets and Puts are a lot slower than multibyte GetArrays and PutArrays.
            wCurrentChunk = sizeof(AppBuffer);
            for(w = 0; w < wMaxGet; w += sizeof(AppBuffer))
            {
                // Make sure the last chunk, which will likely be smaller than sizeof(AppBuffer), is treated correctly.
                if(w + sizeof(AppBuffer) > wMaxGet)
                    wCurrentChunk = wMaxGet - w;

                // Transfer the data out of the TCP RX FIFO and into our local processing buffer.
                int rxed = TCPIP_UDP_ArrayGet(ethernetData.socket, AppBuffer, sizeof(AppBuffer));

                SYS_PRINT("\tReceived a message of '%s' and length %d\r\n", AppBuffer, rxed);

                // Perform the "ToUpper" operation on each data byte
                for(w2 = 0; w2 < wCurrentChunk; w2++)
                {
                    i = AppBuffer[w2];
                    if(i >= 'a' && i <= 'z')
                    {
                            i -= ('a' - 'A');
                            AppBuffer[w2] = i;
                    }
                    else if(i == '\e')   //escape
                    {
                        SYS_MESSAGE("Connection was closed\r\n");
                    }
                }

                SYS_PRINT("\tSending a messages '%s'\r\n", AppBuffer);
                
                TCPIP_UDP_DestinationPortSet(ethernetData.socket, 10001);

                // Transfer the data out of our local processing buffer and into the TCP TX FIFO.
                TCPIP_UDP_ArrayPut(ethernetData.socket, AppBuffer, wCurrentChunk);

                TCPIP_UDP_Flush(ethernetData.socket);

                ethernetData.state = ETHERNET_TCPIP_WAIT_FOR_CONNECTION;
            }
            TCPIP_UDP_Discard(ethernetData.socket);
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
/*******************************************************************************
 End of File
 */
