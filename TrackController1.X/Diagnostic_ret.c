#include <Diagnostic_ret.h>
#include <Fiddle_Yard.h>
#include <Command_Machine.h>
#include "TCPIP Stack/TCPIP.h"

static UDP_SOCKET socket1 = INVALID_UDP_SOCKET;
static NODE_INFO TestTarget;

static unsigned char		Diag_Comm=0,				// Switch for read in to be sended command
							Comm_List[8] [4],			//	Command buffer array
							*Pcomm_List,				//	Pointer to array command buffer
							*Pcomm_List2,				//	Pointer 2 to array command buffer to write out 
							Diag_Comm2=0;				// Switch for write command

void Diagnostic(void)
{
	switch (Diag_Comm2)
	{
		case	0	:	Pcomm_List = &Comm_List[0][0];			//Init pointers 1 time
						Pcomm_List2 = &Comm_List[0][0];
						Diag_Comm2 = 20;
						break;
						
		case	1	:	Pcomm_List2 = &Comm_List[0][0];
						if (Pcomm_List2 != Pcomm_List)
						{
							if(UDPIsPutReady(socket1))
            				{
	            				UDPPutString(Comm_List[0]);
	            				UDPFlush();
		        				Pcomm_List2+=4;
								Diag_Comm2 = 2;
								break;
				   			}		
						}
						break;
						
		case	2	:	if (Pcomm_List2 != Pcomm_List)
						{
							if(UDPIsPutReady(socket1))
            				{
	            				UDPPutString(Comm_List[1]);
		        				UDPFlush();
		        				Pcomm_List2+=4;
								Diag_Comm2 = 3;
								break;
		        			}		
							
						}
						break;
						
		case	3	:	if (Pcomm_List2 != Pcomm_List)
						{
							if(UDPIsPutReady(socket1))
            				{
	            				UDPPutString(Comm_List[2]);
		        				UDPFlush();
		        				Pcomm_List2+=4;
								Diag_Comm2 = 4;
								break;
		        			}		
							
						}
						break;
		
		case	4	:	if (Pcomm_List2 != Pcomm_List)
						{
							if(UDPIsPutReady(socket1))
            				{
	            				UDPPutString(Comm_List[3]);
		        				UDPFlush();
		        				Pcomm_List2+=4;
								Diag_Comm2 = 5;
								break;
		        			}		
							
						}
						break;
						
		case	5	:	if (Pcomm_List2 != Pcomm_List)
						{
							if(UDPIsPutReady(socket1))
            				{
	            				UDPPutString(Comm_List[4]);
		        				UDPFlush();
		        				Pcomm_List2+=4;
								Diag_Comm2 = 6;
								break;
		        			}		
							
						}
						break;
						
		case	6	:	if (Pcomm_List2 != Pcomm_List)
						{
							if(UDPIsPutReady(socket1))
            				{
	            				UDPPutString(Comm_List[5]);
		        				UDPFlush();
		        				Pcomm_List2+=4;
								Diag_Comm2 = 7;
								break;
		        			}		
							
						}
						break;
						
		case	7	:	if (Pcomm_List2 != Pcomm_List)
						{
							if(UDPIsPutReady(socket1))
            				{
	            				UDPPutString(Comm_List[6]);
		        				UDPFlush();
		        				Pcomm_List2+=4;
								Diag_Comm2 = 8;
								break;
		        			}		
							
						}
						break;
						
						
		case	8	:	if (Pcomm_List2 != Pcomm_List)
						{
							if(UDPIsPutReady(socket1))
            				{
	            				UDPPutString(Comm_List[7]);
		        				UDPFlush();
		        				Pcomm_List2 = &Comm_List[0][0];	// point to Comm_List[0]
								Diag_Comm2 = 1;
		        			}									
						}
						break; 
						
		case	20	:	if(!MACIsLinked())
						{
							return;
						}
						
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
						Diag_Comm2 = 21;
						break;
						
		case	21	:	if (MAC_IP_READY == True)	// when all data is transmitted to target stet up communication back to PC.
						{
							TestTarget.MACAddr.v[0] = MACPC[0];//0x00;
							TestTarget.MACAddr.v[1] = MACPC[1];//0x0E;
							TestTarget.MACAddr.v[2] = MACPC[2];//0x0C;
							TestTarget.MACAddr.v[3] = MACPC[3];//0x74;
							TestTarget.MACAddr.v[4] = MACPC[4];//0xCC;
							TestTarget.MACAddr.v[5] = MACPC[5];//0x08;
							
							TestTarget.IPAddr.v[0] = IPPC[0];//192;
							TestTarget.IPAddr.v[1] = IPPC[1];//168; 
							TestTarget.IPAddr.v[2] = IPPC[2];//1;
							TestTarget.IPAddr.v[3] = IPPC[3];//24;
							
				            socket1 = UDPOpen(PORTPC, &TestTarget, PORTPC);  //open the socket at the communicated PORT
			            
				            if(socket1 == 0xFF) //Invalid socket
				            {
					            break;
					        }
					        else{Diag_Comm2 = 1;}
					 	}	      
				        break;
				        
		case	22	:	break;
						
		default		:	break;
	}
}


unsigned char Send_Diag_Comm(unsigned char *Comm)
{
	switch (Diag_Comm)
	{
		case	0	:	Pcomm_List = &Comm_List[0][0];	// point to Comm_List[0]
						*Pcomm_List = Comm[0];
						Pcomm_List++;
						*Pcomm_List = Comm[1];
						Pcomm_List++;
						*Pcomm_List = Comm[2];
						Pcomm_List++;
						*Pcomm_List = 0x00;				// 0x00 -> 100% terminated in case of no termination in earlier bytes
						Pcomm_List++;
						Diag_Comm = 1;
						break;
						
		case	1	:	*Pcomm_List = Comm[0];	// point to Comm_List[1]
						Pcomm_List++;
						*Pcomm_List = Comm[1];
						Pcomm_List++;
						*Pcomm_List = Comm[2];
						Pcomm_List++;
						*Pcomm_List = 0x00;
						Pcomm_List++;
						Diag_Comm = 2;
						break;
						
		case	2	:	*Pcomm_List = Comm[0];	// point to Comm_List[2]
						Pcomm_List++;
						*Pcomm_List = Comm[1];
						Pcomm_List++;
						*Pcomm_List = Comm[2];
						Pcomm_List++;
						*Pcomm_List = 0x00;
						Pcomm_List++;
						Diag_Comm = 3;
						break;
						
		case	3	:	*Pcomm_List = Comm[0];	// point to Comm_List[3]
						Pcomm_List++;
						*Pcomm_List = Comm[1];
						Pcomm_List++;
						*Pcomm_List = Comm[2];
						Pcomm_List++;
						*Pcomm_List = 0x00;
						Pcomm_List++;
						Diag_Comm = 4;
						break;
		
		case	4	:	*Pcomm_List = Comm[0];	// point to Comm_List[4]
						Pcomm_List++;
						*Pcomm_List = Comm[1];
						Pcomm_List++;
						*Pcomm_List = Comm[2];
						Pcomm_List++;
						*Pcomm_List = 0x00;
						Pcomm_List++;
						Diag_Comm = 5;
						break;
		
		case	5	:	*Pcomm_List = Comm[0];	// point to Comm_List[5]
						Pcomm_List++;
						*Pcomm_List = Comm[1];
						Pcomm_List++;
						*Pcomm_List = Comm[2];
						Pcomm_List++;
						*Pcomm_List = 0x00;
						Pcomm_List++;
						Diag_Comm = 6;
						break;
		
		case	6	:	*Pcomm_List = Comm[0];	// point to Comm_List[6]
						Pcomm_List++;
						*Pcomm_List = Comm[1];
						Pcomm_List++;
						*Pcomm_List = Comm[2];
						Pcomm_List++;
						*Pcomm_List = 0x00;
						Pcomm_List++;
						Diag_Comm = 7;
						break;
		
		case	7	:	*Pcomm_List = Comm[0];	// point to Comm_List[7]
						Pcomm_List++;
						*Pcomm_List = Comm[1];
						Pcomm_List++;
						*Pcomm_List = Comm[2];
						Pcomm_List++;
						*Pcomm_List = 0x00;												
						Pcomm_List = &Comm_List[0][0];	// point to Comm_List[0]
						Diag_Comm = 0;
						break;
		
						
		default		:	break;
	}
				
}


