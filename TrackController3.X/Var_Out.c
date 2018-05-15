#include <Var_Out.h>
#include <Diagnostic_ret.h>
#include <Shift_Register.h>
#include <Fiddle_Yard.h>

void Target_Alive_Update(unsigned char ASL);
void Var_Out_Switcher(unsigned  char ASL);
void Sent_Track_Power_Meassage(unsigned char ASL);

typedef struct
{
	unsigned char 	Var_Out;	
	unsigned int	Variables_Out_Counter;
	unsigned int 	Target_Alive_Counter;
	unsigned char	Variables_Out_Counter2;
	unsigned char	Send_Var_Out_Old[5][3];
	unsigned char	ForceSendUpdate;
	unsigned char	Track_Power_Meassage;
}OUTPUT_VAR;

static OUTPUT_VAR VAR_OUT_FY[2] = { {0,0 ,0 ,0 ,{{0,0,0},{0,0,0},{0,0,0},{0,0,0},{0,0,0}},0,0},
								    {0,15,15,5, {{0,0,0},{0,0,0},{0,0,0},{0,0,0},{0,0,0}},0,0}	};

static unsigned char Send_Var_Out[3];

void Var_Out_Programm(unsigned  char ASL)
{
	
	VAR_OUT_FY[ASL].Variables_Out_Counter++;	
	if (VAR_OUT_FY[ASL].Variables_Out_Counter >= 5000)			// Auto send an update every 2 seconds to ensure up-to-date data is available in user gui
	{			
		VAR_OUT_FY[ASL].Variables_Out_Counter = 0;				// Reset counter
		VAR_OUT_FY[ASL].Var_Out = 0;							// Force start case 0
		VAR_OUT_FY[ASL].ForceSendUpdate = True;					// After all 4 cases have been send, make this variable False.
	}
	
	if (TR_MEAS(0) == 0)														// Track power measurement are equal for TOP and BOTTOM. there for sent message only once
	{
		if (VAR_OUT_FY[ASL].Track_Power_Meassage == 1)							// when falling edge off track power is detected
		{
			Sent_Track_Power_Meassage(ASL);
			VAR_OUT_FY[ASL].Track_Power_Meassage = 0;
		}
		
	}
	else if (TR_MEAS(0) == 1)                                                   // When power restored do not send message, but set the local variable Track_Power_Meassage to 1, but on a slower pase
	{
		VAR_OUT_FY[ASL].Track_Power_Meassage = 1;
	}	
	

//	VAR_OUT_FY[ASL].Variables_Out_Counter2++;
//	if (VAR_OUT_FY[ASL].Variables_Out_Counter2 >= 255)                      	// Send Frame counter
//	{			
//		VAR_OUT_FY[ASL].Variables_Out_Counter2 = 0;                             // Reset counter
//	}	
    
    Var_Out_Switcher(ASL);
}

void Var_Out_Switcher(unsigned  char ASL)
{
	switch (VAR_OUT_FY[ASL].Var_Out)
		{
			case	0	:	if (ASL == TOP)
							{
								Send_Var_Out[0] = 'M';
							}
							else if (ASL == BOTTOM)
							{
								Send_Var_Out[0] = 'Z';
							}
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | CL_10_Heart(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | SPARE2(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | F11(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | EOS_10(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | EOS_11(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | SPARE3(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | F13(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0x1;
							Send_Var_Out[2] = 0x00;
							if (VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] == Send_Var_Out[0] &&
								VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] == Send_Var_Out[1] && VAR_OUT_FY[ASL].ForceSendUpdate == False)	
							{																						// if previous send value is same as present than
																													// don't send anything
								VAR_OUT_FY[ASL].Var_Out = 1;														// VAR_OUT_FY[ASL].Send_Var_Out_Old is an array of
								break;																				// 5 (for case 0 to 4) and has 3 bytes for the previous 
							}																						// send bytes
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] = Send_Var_Out[0];	
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] = Send_Var_Out[1];
							Send_Diag_Comm(Send_Var_Out);
							VAR_OUT_FY[ASL].Var_Out = 1;
							break;
							
			case	1	:	if (ASL == TOP)
							{
								Send_Var_Out[0] = 'L';
							}
							else if (ASL == BOTTOM)
							{
								Send_Var_Out[0] = 'Y';
							}
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = (Send_Var_Out[1] << 4) | Track_Nr(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | F12(ASL);							
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Bezet_Uit_5B(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							
							if (VAR_OUT_FY[ASL].Track_Power_Meassage == 0) // when falling edge off track power is detected and occupied out signal is gone, don't update C# app, tell block is occupied
							{
								Send_Var_Out[1] = Send_Var_Out[1] | 1;
							}	
							else if (VAR_OUT_FY[ASL].Track_Power_Meassage == 1)
							{
								Send_Var_Out[1] = Send_Var_Out[1] | Bezet_Uit_8A(ASL);
							}
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							
							Send_Var_Out[1] = Send_Var_Out[1] | 0x1;
							Send_Var_Out[2] = 0x00;
							if (VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] == Send_Var_Out[0] &&
								VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] == Send_Var_Out[1] && VAR_OUT_FY[ASL].ForceSendUpdate == False)	// if previous send value is same as present than
							{																						// don't send anything
								VAR_OUT_FY[ASL].Var_Out = 2;														// VAR_OUT_FY[ASL].Send_Var_Out_Old is an array of
								break;																				// 5 (for case 0 to 4) and has 3 bytes for the previous 
							}																						// send bytes
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] = Send_Var_Out[0];	
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] = Send_Var_Out[1];
							Send_Diag_Comm(Send_Var_Out);
							VAR_OUT_FY[ASL].Var_Out = 2;
							break;
						
							
			case	2	:	if (ASL == TOP)
							{
								Send_Var_Out[0] = 'K';
							}
							else if (ASL == BOTTOM)
							{
								Send_Var_Out[0] = 'X';
							}
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Enable_Track_Status(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Bezet_In_5B_Status(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Bezet_In_6_Status(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Bezet_In_7_Status(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Bezet_Weerstand_Status(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0x1;
							Send_Var_Out[2] = 0x00;
							if (VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] == Send_Var_Out[0] &&
								VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] == Send_Var_Out[1] && VAR_OUT_FY[ASL].ForceSendUpdate == False)	// if previous send value is same as present than
							{																						// don't send anything
								VAR_OUT_FY[ASL].Var_Out = 3;														// VAR_OUT_FY[ASL].Send_Var_Out_Old is an array of
								break;																				// 5 (for case 0 to 4) and has 3 bytes for the previous 
							}																						// send bytes
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] = Send_Var_Out[0];	
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] = Send_Var_Out[1];
							Send_Diag_Comm(Send_Var_Out);
							//VAR_OUT_FY[ASL].Var_Out = 3; this case is empty tracks removed
							VAR_OUT_FY[ASL].Var_Out = 4;
							break;
					
			case	3	:	if (ASL == TOP)
							{
								Send_Var_Out[0] = 'J';
							}
							else if (ASL == BOTTOM)
							{
								Send_Var_Out[0] = 'W';
							}
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0x1;
							Send_Var_Out[2] = 0x00;
							if (VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] == Send_Var_Out[0] &&
								VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] == Send_Var_Out[1] && VAR_OUT_FY[ASL].ForceSendUpdate == False)	// if previous send value is same as present than
							{																						// don't send anything
								VAR_OUT_FY[ASL].Var_Out = 4;														// VAR_OUT_FY[ASL].Send_Var_Out_Old is an array of
								break;																				// 5 (for case 0 to 4) and has 3 bytes for the previous 
							}																						// send bytes
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] = Send_Var_Out[0];	
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] = Send_Var_Out[1];
							Send_Diag_Comm(Send_Var_Out);
							VAR_OUT_FY[ASL].Var_Out = 4;
							break;
							
			case	4	:	if (ASL == TOP)
							{
								Send_Var_Out[0] = 'I';
							}
							else if (ASL == BOTTOM)
							{
								Send_Var_Out[0] = 'V';
							}
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Bezet_Uit_6(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Bezet_Uit_7(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | TR_MEAS(ASL);		// TRACK_POWER This is sensor, for the message that the power is gone a separate message is created
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | F10(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | M10_Status(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0;
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | 0x1;
							Send_Var_Out[2] = 0x00;
							if (VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] == Send_Var_Out[0] &&
								VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] == Send_Var_Out[1] && VAR_OUT_FY[ASL].ForceSendUpdate == False)	// if previous send value is same as present than
							{																						// don't send anything
								VAR_OUT_FY[ASL].Var_Out = 0;														// VAR_OUT_FY[ASL].Send_Var_Out_Old is an array of
								break;																				// 5 (for case 0 to 4) and has 3 bytes for the previous 
							}																						// send bytes
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][0] = Send_Var_Out[0];	
							VAR_OUT_FY[ASL].Send_Var_Out_Old[VAR_OUT_FY[ASL].Var_Out][1] = Send_Var_Out[1];
							Send_Diag_Comm(Send_Var_Out);
							VAR_OUT_FY[ASL].Var_Out = 0;
							if (VAR_OUT_FY[ASL].ForceSendUpdate == True)
							{
								 VAR_OUT_FY[ASL].ForceSendUpdate = False;
							}
							break;
					
			default		:	VAR_OUT_FY[ASL].Var_Out = 0;
							break;
		}
}		


void Fiddle_Yard_Reset(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00000001;		//1
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

void Target_Ready(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00000010;		//2
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

void Sent_Track_Power_Meassage(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00000011;		//3
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}