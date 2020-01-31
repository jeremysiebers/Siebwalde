#include <Fiddle_Yard.h>
#include "TCPIP Stack/TCPIP.h"
#include <Command_Machine.h>

#define Enter 13

static UDP_SOCKET socket2 = INVALID_UDP_SOCKET;

static void Command_Exe(unsigned char Command[3]);

static unsigned char Cmd[3],Cmd_Read_Switch=20;
static unsigned char Exe_Cmd[2] = {0,0};
static unsigned char Exe_Cmd_Ret_1=0;
static unsigned char SuccesFull_Read = 0;	//3 bytes received or less?
unsigned char MACPC[6] = {0,0,0,0,0,0};
unsigned char IPPC[4] = {0,0,0,0};
unsigned int PORTPC = 0xFFFF;
unsigned char MAC_IP_READY = FALSE;

void ResetPic(void);

unsigned char Exe_Cmd_(unsigned char ASL)
{
	return(Exe_Cmd[ASL]);
}

unsigned char Exe_Cmd_Ret(unsigned char ASL, char Exe_Cmd_Ret_)
{
	Exe_Cmd_Ret_1 = Exe_Cmd_Ret_;
	Exe_Cmd[ASL] = 0;					// reset commando to 0, after executing the command. Next command allowed
}

void Exe_Cmd_Resume(unsigned char ASL, char Resume_Cmd)
{
	Exe_Cmd[ASL] = Resume_Cmd;	
}

void Command()
{
	switch (Cmd_Read_Switch)
	{
		case	0	:	if(UDPIsGetReady(socket2))
						{
							SuccesFull_Read = UDPGetArray(Cmd, 0x3);
							if (Cmd[0] == Enter || Cmd[0] == 0 || SuccesFull_Read < 3)
							{
								Cmd_Read_Switch = 0;
								Cmd[0] = 0;
								break;
							}							
							else if (Cmd[2] == Enter)
							{
								Cmd_Read_Switch = 0;
								Command_Exe(Cmd);									
							}
							Cmd_Read_Switch = 0;
							Cmd[0] = 0;
							Cmd[1] = 0;
							Cmd[2] = 0;
							break;
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
						Cmd_Read_Switch = 21;
						break;
						
		case	21	:	socket2 = UDPOpen(0x6FFF, NULL, 0x6FFF);  //open the socket 
			            
			            if(socket2 == 0xFF) //Invalid socket
			            {
				            break;
				        }
				        else{Cmd_Read_Switch = 0;}
				        
				        Led1=1;
				        Output_Enable = 1; 
				        
				        break;				        
		
		default		:	break;
	}
	
}

void Command_Exe(unsigned char Command[3])
{
	switch (Command[0])
	{
		case	'a'		:	if (Exe_Cmd[1] == 0 || Command[1] != 19) // block new commands until last one has finished
							{
								
								switch (Command[1])
								{
									case	'1'		:	Exe_Cmd[1] = 1;	//Enable Track			Exe_Cmd[1] = TOP	Exe_Cmd[0] = Bottom
														break;
									case	'2'		:	Exe_Cmd[1] = 2;	//Disable Track
														break;
									case	'3'		:	Exe_Cmd[1] = 3;	//Fiddle yard naar links = spoor++
														break;
									case	'4'		:	Exe_Cmd[1] = 4;	//Fiddle yard naar rechts = spoor--
														break;
									case	'5'		:	Exe_Cmd[1] = 5;	// Ga naar spoor 1
														//Exe_Cmd[0] = 5;	// Ga naar spoor 1
														break;
									case	'6'		:	Exe_Cmd[1] = 6;	// ga naar spoor 2
														break;
									case	'7'		:	Exe_Cmd[1] = 7;	//ga naar spoor 3
														break;
									case	'8'		:	Exe_Cmd[1] = 8;	//ga naar spoor 4
														break;
									case	'9'		:	Exe_Cmd[1] = 9;	//ga naar spoor 5
														break;
									case	'A'		:	Exe_Cmd[1] = 10;	//ga naar spoor 6
														break;
									case	'B'		:	Exe_Cmd[1] = 11;	//ga naar spoor 7
														break;
									case	'C'		:	Exe_Cmd[1] = 12;	//ga naar spoor 8
														break;
									case	'D'		:	Exe_Cmd[1] = 13;	//ga naar spoor 9
														break;
									case	'E'		:	Exe_Cmd[1] = 14;	//ga naar spoor 10
														break;
									case	'F'		:	Exe_Cmd[1] = 15;	//ga naar spoor 11
														//Exe_Cmd[0] = 15;	//ga naar spoor 11
														break;
									case	'G'		:	Exe_Cmd[1] = 16;	//Trein detectie
														//Exe_Cmd[0] = 16;	//Trein detectie
														break;
									case	'H'		:	Exe_Cmd[1] = 17;	//Start Fiddle Yard
														break;
									case	'I'		:	Exe_Cmd[1] = 18;	//Stop Fiddle Yard
														break;
									case	'J'		:	Exe_Cmd[1] = 19;	//Stop Fiddle Yard NOW
														break;
									case	'K'		:	Exe_Cmd[1] = 20;	//Bezet_In_5B_Switch_On
														break;
									case	'L'		:	Exe_Cmd[1] = 21;	//Bezet_In_5B_Switch_Off
														break;
									case	'M'		:	Exe_Cmd[1] = 22;	//Bezet_In_6_Switch_On
														break;
									case	'N'		:	Exe_Cmd[1] = 23;	//Bezet_In_6_Switch_Off
														break;
									case	'O'		:	Exe_Cmd[1] = 24;	//Bezet_In_7_Switch_On
														break;
									case	'P'		:	Exe_Cmd[1] = 25;	//Bezet_In_7_Switch_Off
														break;
									case	'Q'		:	Exe_Cmd[1] = 26;	//Resume previous operation
														break;
									case	'R'		:	Exe_Cmd[1] = 27;	//Collect Start
														break;
									case	'S'		:	Exe_Cmd[1] = 0;	//Bruggen open
														break;
									case	'T'		:	Exe_Cmd[1] = 0;	//Bruggen open
														break;
									case	'U'		:	Exe_Cmd[1] = 0;	//Bruggen open
														break;
									case	'V'		:	Exe_Cmd[1] = 0;	//Bruggen open
														break;
									case	'W'		:	Exe_Cmd[1] = 0;	//Bruggen open
														break;
									case	'X'		:	Exe_Cmd[1] = 0;	//Bruggen open
														break;
									case	'Y'		:	Exe_Cmd[1] = 0;	//Bruggen open
														break;
									case	'Z'		:	Exe_Cmd[1] = 0;	//Bruggen open
														break;
									default			:	Exe_Cmd[1] = 0;
														break;
								}
							}
							break;
							
		case	'b'		:	if (Exe_Cmd[0] == 0 || Command[1] != 19)	// block new commands until last one has finished except with Reset (stop NOW)
							{
								
								switch (Command[1])
								{
									case	'1'		:	Exe_Cmd[0] = 1;	//Bruggen open			Exe_Cmd[1] = TOP	Exe_Cmd[0] = Bottom
														break;
									case	'2'		:	Exe_Cmd[0] = 2;	//Bruggen dicht
														break;
									case	'3'		:	Exe_Cmd[0] = 3;	//Fiddle yard naar links = spoor++
														break;
									case	'4'		:	Exe_Cmd[0] = 4;	//Fiddle yard naar rechts = spoor--
														break;
									case	'5'		:	Exe_Cmd[0] = 5;	// Ga naar spoor 1
														break;
									case	'6'		:	Exe_Cmd[0] = 6;	// ga naar spoor 2
														break;
									case	'7'		:	Exe_Cmd[0] = 7;	//ga naar spoor 3
														break;
									case	'8'		:	Exe_Cmd[0] = 8;	//ga naar spoor 4
														break;
									case	'9'		:	Exe_Cmd[0] = 9;	//ga naar spoor 5
														break;
									case	'A'		:	Exe_Cmd[0] = 10;	//ga naar spoor 6
														break;
									case	'B'		:	Exe_Cmd[0] = 11;	//ga naar spoor 7
														break;
									case	'C'		:	Exe_Cmd[0] = 12;	//ga naar spoor 8
														break;
									case	'D'		:	Exe_Cmd[0] = 13;	//ga naar spoor 9
														break;
									case	'E'		:	Exe_Cmd[0] = 14;	//ga naar spoor 10
														break;
									case	'F'		:	Exe_Cmd[0] = 15;	//ga naar spoor 11
														break;
									case	'G'		:	Exe_Cmd[0] = 16;	//Trein detectie
														break;
									case	'H'		:	Exe_Cmd[0] = 17;	//Start Fiddle Yard
														break;
									case	'I'		:	Exe_Cmd[0] = 18;	//Stop Fiddle Yard
														break;
									case	'J'		:	Exe_Cmd[0] = 19;	//Stop Fiddle Yard NOW (RESET)
														break;
									case	'K'		:	Exe_Cmd[0] = 20;	//Bezet_In_5B_Switch_On
														break;
									case	'L'		:	Exe_Cmd[0] = 21;	//Bezet_In_5B_Switch_Off
														break;
									case	'M'		:	Exe_Cmd[0] = 22;	//Bezet_In_6_Switch_On
														break;
									case	'N'		:	Exe_Cmd[0] = 23;	//Bezet_In_6_Switch_Off
														break;
									case	'O'		:	Exe_Cmd[0] = 24;	//Bezet_In_7_Switch_On
														break;
									case	'P'		:	Exe_Cmd[0] = 25;	//Bezet_In_7_Switch_Off
														break;
									case	'Q'		:	Exe_Cmd[0] = 26;	//Resume previous operation
														break;
									case	'R'		:	Exe_Cmd[0] = 27;	//Collect Start
														break;
									case	'S'		:	Exe_Cmd[0] = 0;	//Bruggen open
														break;
									case	'T'		:	Exe_Cmd[0] = 0;	//Bruggen open
														break;
									case	'U'		:	Exe_Cmd[0] = 0;	//Bruggen open
														break;
									case	'V'		:	Exe_Cmd[0] = 0;	//Bruggen open
														break;
									case	'W'		:	Exe_Cmd[0] = 0;	//Bruggen open
														break;
									case	'X'		:	Exe_Cmd[0] = 0;	//Bruggen open
														break;
									case	'Y'		:	Exe_Cmd[0] = 0;	//Bruggen open
														break;
									case	'Z'		:	Exe_Cmd[0] = 0;	//Bruggen open
														break;
									default			:	Exe_Cmd[0] = 0;
														break;
								}
							}	
							break;
							
							
		case	'r'		:	PORTPC = Command[1];
							break;
							
		case	's'		:	PORTPC = (PORTPC << 8) | Command[1];
							break;
							
		case	't'		:	MAC_IP_READY = Command[1];	// when MAC and IP is send over send the MAC_IP_READY command to initiate adress copy and send info back
							break;
		
		case	'u'		:	MACPC[0] = Command[1] << 4;
							break;
							
		case	'v'		:	MACPC[0] = MACPC[0] | Command[1];
							break;
							
		case	'w'		:	MACPC[1] = Command[1] << 4;
							break;
							
		case	'x'		:	MACPC[1] = MACPC[1] | Command[1];
							break;
							
		case	'y'		:	MACPC[2] = Command[1] << 4;
							break;
							
		case	'z'		:	MACPC[2] = MACPC[2] | Command[1];
							break;
				
		case	'0'		:	MACPC[3] = Command[1] << 4;
							break;
							
		case	'1'		:	MACPC[3] = MACPC[3] | Command[1];
							break;
							
		case	'2'		:	MACPC[4] = Command[1] << 4;
							break;
							
		case	'3'		:	MACPC[4] = MACPC[4] | Command[1];
							break;
							
		case	'4'		:	MACPC[5] = Command[1] << 4;
							break;
							
		case	'5'		:	MACPC[5] = MACPC[5] | Command[1];
							break;
							
		case	'6'		:	IPPC[0] = Command[1];
							break;
							
		case	'7'		:	IPPC[1] = Command[1];
							break;
							
		case	'8'		:	IPPC[2] = Command[1];
							break;
							
		case	'9'		:	IPPC[3] = Command[1];
							break;
												
		default			:	break;
	}
}

void ResetPic(void)
{
	//Reset();
	/*_asm
	reset
	_endasm*/
}	