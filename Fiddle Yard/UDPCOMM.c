#include <UDPCOMM.h>
#include <Fiddle_Yard.h>
#include "TCPIP Stack/TCPIP.h"

static UDP_SOCKET socket = INVALID_UDP_SOCKET;
BYTE buffer[10];    //set an array of 10 bytes, just for testing
BYTE in_pointer = 0;
static unsigned char incount = 0;
static enum _UDPState    {
  	SM_HOME = 0,
    SM_SOCKET_OBTAIN,
    SM_SOCKET_OBTAINED,
    SM_DISCONNECT,
    SM_DONE
    } UDPState = SM_SOCKET_OBTAIN;
    
    
    
static UDP_SOCKET socket2 = INVALID_UDP_SOCKET;
static NODE_INFO TestTarget;
static int send=0;
static unsigned char v=0x61;
void UDP_Send(void);

void UDP_Update(void) 
{
	if(!MACIsLinked())
		return;
	
	#if defined(STACK_USE_DHCP_CLIENT)
	{
		static DWORD dwTimer = 0;
		
		// Wait until DHCP module is finished
		if(!DHCPIsBound(0))
		{
			dwTimer = TickGet();
			return;
		}

		// Wait an additional half second after DHCP is finished to let the announce module and any other stack state machines to reach normal operation
		if(TickGet() - dwTimer < TICK_SECOND/2)
			return;
	}
	#endif
	
    switch(UDPState) {

        case SM_SOCKET_OBTAIN: 
            	
			
            socket = UDPOpen(0x7000, NULL, 0x7000);  //open the socket 0x7000 is 28672 !!!
            
            if(socket == 0xFF) //Invalid socket
                break;

            UDPState++;
            break;

		case SM_SOCKET_OBTAINED:  //get the first 10 bytes and send to uart for examination
            in_pointer = 0;
            incount = 0;
            if(UDPIsGetReady(socket))  
            {
	            
                while (incount < 1)   //and while count is less than 30
        		{
           			UDPGet(&buffer[in_pointer++]); //place the data in buffer array
       				incount++;
     			}
     			v = buffer[0];
				UDP_Send();
			}
			
            break;
            
        case SM_DISCONNECT:
            UDPClose(socket);
            socket = 0xFF; //INVALID_SOCKET

            UDPState = SM_DONE;
            break;
    
        case SM_DONE:
            break; 
        }
}




void UDP_Send(void)
{
	switch(send) {

        case 0: 
            
            
			TestTarget.MACAddr.v[0] = 0x00;
			TestTarget.MACAddr.v[1] = 0x20;
			TestTarget.MACAddr.v[2] = 0xED;
			TestTarget.MACAddr.v[3] = 0x3D;
			TestTarget.MACAddr.v[4] = 0xC8;
			TestTarget.MACAddr.v[5] = 0x24;
			
			TestTarget.IPAddr.v[0] = 10;
			TestTarget.IPAddr.v[1] = 0;
			TestTarget.IPAddr.v[2] = 0;
			TestTarget.IPAddr.v[3] = 1;
			
			
            socket2 = UDPOpen(0x7000, &TestTarget, 0x7000);  //open the socket 
            
            if(socket2 == 0xFF) //Invalid socket
                break;

            send++;
            break;

		case 1:  
            if(UDPIsPutReady(socket2))
            {
	            UDPPut(v);
		        UDPFlush();
		    }
		    if(v > 0x7A)
		    {
			    v = 0x61;
			}
			else { v++;}
            break;
            
        case 2:
            UDPClose(socket);
            socket = 0xFF; //INVALID_SOCKET

            UDPState = SM_DONE;
            break;
    
        case 3:
            break; 
        }
}