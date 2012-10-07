#include <Var_Out.h>
#include <Diagnostic_ret.h>
#include <Shift_Register.h>
#include <Fiddle_Yard.h>
//#include <State_Machine.h>
#include <Drive_Train_IO.h>

typedef struct
{
	unsigned char 	Var_Out;	
	unsigned int	Variables_Out_Counter;
	unsigned char	Send_Var_Out_Old[5][3];
	unsigned char	ForceSendUpdate;
}OUTPUT_VAR;

static OUTPUT_VAR VAR_OUT_FY[2] = { {0,0 ,{{0,0,0},{0,0,0},{0,0,0},{0,0,0},{0,0,0}},0},
								    {0,15,{{0,0,0},{0,0,0},{0,0,0},{0,0,0},{0,0,0}},0}	};

static unsigned char Send_Var_Out[3];

void Var_Out_Programm(unsigned  char ASL)
{
	
	VAR_OUT_FY[ASL].Variables_Out_Counter++;
	
	if (VAR_OUT_FY[ASL].Variables_Out_Counter >= 5000)			// Auto send an update every second to ensure up-to-date data is available in user gui
	{			
		VAR_OUT_FY[ASL].Variables_Out_Counter = 0;				// Reset counter
		VAR_OUT_FY[ASL].Var_Out = 0;							// Force start case 0
		VAR_OUT_FY[ASL].ForceSendUpdate = True;					// After all 4 cases have been send, make this variable False.
	}

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
							Send_Var_Out[1] = Send_Var_Out[1] | Bezet_Uit_8A(ASL);
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
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[1];
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[2];
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
							VAR_OUT_FY[ASL].Var_Out = 3;
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
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[4];
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[5];
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[6];
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[7];
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[8];
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[9];
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[10];
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
							Send_Var_Out[1] = Send_Var_Out[1] | SPARE1(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | F10(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | M10_Status(ASL);
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[3];
							Send_Var_Out[1] = (Send_Var_Out[1] << 1);
							Send_Var_Out[1] = Send_Var_Out[1] | Trains_On_Fiddle_Yard(ASL)[11];
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

extern void Bridge_Open_Ok(unsigned char ASL)
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
	
extern void Bridge_Close_Ok(unsigned char ASL)
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
extern void Fiddle_One_Left_Ok(unsigned char ASL)
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
extern void Fiddle_One_Right_Ok(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00000100;		//4
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Fiddle_Multiple_Left_Ok(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00000101;		//5
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Fiddle_Multiple_Right_Ok(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00000110;		//6
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Train_Detection_Finished(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00000111;		//7
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Train_Drive_Out_Finished(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00001000;		//8
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Train_Drive_In_Finished(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00001001;		//9
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Train_Drive_In_Failed_F12(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00011011;			//27 anders is het 0x0A is line feed
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Init_Done(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00001011;		//11
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Bridge_Opening(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00001100;		//12
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Bridge_Closing(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00001101;		//13
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Init_Started(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00001110;		//14
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Train_On_5B(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00001111;		//15
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Train_Drive_In_Start(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00010000;		//16
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Train_On_8A(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00010001;		//17
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Train_Drive_Out_Start(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00010010;		//18
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Fiddle_Yard_Soft_Start(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00010011;		//19
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Fiddle_Yard_Stopped(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00010100;		//20
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Fiddle_Yard_Reset(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00010101;		//21
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Bezet_Uit_Blok_6_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00010110;		//22
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Sensor_F12_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00010111;		//23
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Bezet_Uit_Blok_6_AND_Sensor_F12_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00011000;		//24
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void CL_10_Heart_Sensor_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00011001;		//25
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Bridge_Open_Close_Timeout_Expired_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00011010;		//26
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}












extern void BridgeMotorContact_10_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00011100;		//28
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Bridge_10L_Contact_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00011101;		//29
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Bridge_10R_Contact_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00011110;		//30
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void BridgeMotorContact_11_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00011111;		//31
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void EndOffStroke_11_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00100000;		//32
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Laatste_Spoor_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00100001;		//33
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void EndOffStroke_10_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00100010;		//34
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
extern void Universal_Error_Send(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00100011;		//35
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Collect_Finished_Fy_Full(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00100100;		//36
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Collect_On(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00100101;		//37
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Collect_Off(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00100110;		//38
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}



//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Bridge error logging//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



extern void Bridge_Opening_Msg_1(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00100111;		//39
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Bridge_Opening_Msg_2(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00101000;		//40
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Bridge_Opening_Msg_3(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00101001;		//41
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Bridge_Opening_Msg_4(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00101010;		//42
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}







extern void Bridge_Opening_Msg_11(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00101011;		//43
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Bridge_Opening_Msg_12(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00101100;		//44
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Bridge_Opening_Msg_13(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00101101;		//45
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}

extern void Bridge_Opening_Msg_14(unsigned char ASL)
{
	if (ASL == TOP)
	{
		Send_Var_Out[0] = 'A';
	}
	else if (ASL == BOTTOM)
	{
		Send_Var_Out[0] = 'B';
	}
	Send_Var_Out[1] = 0b00101110;		//46
	Send_Var_Out[2] = 0x00;
	Send_Diag_Comm(Send_Var_Out);
}
