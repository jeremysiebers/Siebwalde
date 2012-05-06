#include <Train_Detection.h>
#include <Shift_Register.h>
#include <Track_Move_Ctrl.h>
#include <Bridge_Ctrl.h>
#include <Drive_Train_IO.h>
#include <Var_Out.h>

#define On 1
#define Off 0

////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0

typedef struct
{
	unsigned char			Train_Detector,//=0,						// Switch for train counter (trains on fiddle yard)
							Train_Detector_Move,//=1,					// Move to track for train detection
							Train_In_Track[12],							// Array containing occupied track in fiddle yard used is 1 to 11, 0 is not used
							*PTIT;										// Pointer to Train In Track
							
							
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,1,{0,0,0,0,0,0,0,0,0,0,0,0},0},	// is 0 is BOTTOM
											 {0,1,{0,0,0,0,0,0,0,0,0,0,0,0},0}};	// is 1 is TOP
	

void Train_Detection_Reset(unsigned char ASL)
{
	ACT_ST_MCHN[ASL].Train_Detector=0;															// Switch for train counter (trains on fiddle yard)
	ACT_ST_MCHN[ASL].Train_Detector_Move=1;														// Move to track for train detection
	ACT_ST_MCHN[ASL].Train_In_Track[0]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[1]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[2]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[3]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[4]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[5]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[6]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[7]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[8]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[9]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[10]=0;														// Array containing occupied track in fiddle yard
	ACT_ST_MCHN[ASL].Train_In_Track[11]=0;														// Array containing occupied track in fiddle yard
}

unsigned char Train_Detection(unsigned char ASL)
{
	static char Return_Val = Busy, Return_Val_Routine = Busy;
	unsigned int Train_In_Track_pointer = 0;
	
	switch(ACT_ST_MCHN[ASL].Train_Detector)
	{
		case	0	:	ACT_ST_MCHN[ASL].PTIT = &ACT_ST_MCHN[ASL].Train_In_Track[0];
						*(ACT_ST_MCHN[ASL].PTIT) = 0;
						Bezet_Weerstand(ASL, On);
						Bezet_In_7(ASL, On);
						Bezet_In_6(ASL, On);
						Bezet_In_5B(ASL, On);
						ACT_ST_MCHN[ASL].Train_Detector = 1;
						Return_Val = Busy;
						break;
						
		case	1	:	switch (Return_Val_Routine = Track_Mover(ASL,ACT_ST_MCHN[ASL].Train_Detector_Move))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].PTIT++;
													ACT_ST_MCHN[ASL].Train_Detector = 2;
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Detector = 0;
													break;
						}
						break;
						
		case	2	:	switch (Return_Val_Routine = Bridge_Close(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Train_Detector = 3;
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Detector = 0;
													break;
						}
						break;
						
		case	3	:	if (F10(ASL) == 1)
						{
							*(ACT_ST_MCHN[ASL].PTIT) = 1;
						}
						else { *(ACT_ST_MCHN[ASL].PTIT) = 0;}
						Train_In_Track_actual(ASL, &ACT_ST_MCHN[ASL].Train_In_Track[0]);		// Update the Train_In_Track array in State_Machine.c with the
						ACT_ST_MCHN[ASL].Train_Detector = 4;									// the latest status of the Train_In_Track array in Train_Detection.c
						Return_Val = Busy;
						break;
						
		case	4	:	switch (Return_Val_Routine = Bridge_Open(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Train_Detector = 5;
													ACT_ST_MCHN[ASL].Train_Detector_Move++;
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Detector = 0;
													break;
						}
						break;
						
		case	5	:	if (ACT_ST_MCHN[ASL].Train_Detector_Move >= 12)
						{
							ACT_ST_MCHN[ASL].Train_Detector_Move = 1;
							ACT_ST_MCHN[ASL].Train_Detector = 0;
							Bezet_Weerstand(ASL, Off);							
							Train_Detection_Finished(ASL);
							Return_Val = Finished;
							break;
						}
						else {ACT_ST_MCHN[ASL].Train_Detector = 1;Return_Val = Busy;}
						Return_Val = Busy;
						break;
		
		default		:	break;
	}
	return (Return_Val);
}