#include <Track_Move_Ctrl.h>
#include <Shift_Register.h>
#include <Fiddle_Move_Ctrl.h>

////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0

typedef struct
{
	unsigned char 			Fiddle_Track_Mover,//=0,					// Switch for Fiddle direction movement
							Old_Track,//=0,								// Previous track for track mover routine
							Old_Track_2,//=0,							// Previous track for Error handling return to last track
							Brake_Track;//=0;							// Brake Track when slower PWM is required
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,0,0},		// is 0 is BOTTOM
											 {0,0,0,0}};	// is 1 is TOP


void Track_Move_Ctrl_Reset(unsigned char ASL)
{
	
	ACT_ST_MCHN[ASL].Fiddle_Track_Mover=0;														// Switch for Fiddle direction movement
	ACT_ST_MCHN[ASL].Old_Track=0;																// Previous track
	ACT_ST_MCHN[ASL].Old_Track_2=0;																// Previous track for Error handling return to last track
	ACT_ST_MCHN[ASL].Brake_Track=0;																// Brake Track when slower PWM is required
}

void Old_Track2_When_Error(unsigned char ASL, unsigned char Track_Nr)
{
	ACT_ST_MCHN[ASL].Old_Track_2 = Track_Nr;	
}
	
unsigned char Old_Track2_When_Error_Ret(unsigned char ASL)
{
	return ACT_ST_MCHN[ASL].Old_Track_2;
}					

unsigned char Track_Mover(unsigned char ASL, char New_Track)
{
	static char Return_Val = Busy, Return_Val_Routine = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle_Track_Mover)
	{
		case	0	:	Return_Val = Busy;
						if (Track_Nr(ASL) == 0)
						{
							Return_Val = ERROR;
							break;
						}
						ACT_ST_MCHN[ASL].Old_Track = Track_Nr(ASL);
						if ((New_Track < ACT_ST_MCHN[ASL].Old_Track) && (New_Track < (ACT_ST_MCHN[ASL].Old_Track - 1) ))
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 1; //rechts op bewegen 11 naar 1 multiple
							ACT_ST_MCHN[ASL].Brake_Track = (New_Track + 1);
							break;
						}
						
						
						if ((New_Track > ACT_ST_MCHN[ASL].Old_Track) && (New_Track > (ACT_ST_MCHN[ASL].Old_Track + 1) ))
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 3; //links op bewegen 1 naar 11 multiple
							ACT_ST_MCHN[ASL].Brake_Track = (New_Track - 1);
							break;
						}
					
						if (New_Track < ACT_ST_MCHN[ASL].Old_Track)
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 2; //rechts op bewegen 11 naar 1 single
							break; 					
						}
						
						if (New_Track > ACT_ST_MCHN[ASL].Old_Track)
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 4;	//links op bewegen 1 naar 11 single 
							break;
						} 				
						
						if (New_Track == ACT_ST_MCHN[ASL].Old_Track)
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
							Return_Val = Finished;
							break;
						}
						break;
						
		case	1	:	switch (Return_Val_Routine = Fiddle_Multiple_Right(ASL, New_Track, ACT_ST_MCHN[ASL].Brake_Track))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
													Return_Val = Finished;
													break;
													
							case	Busy		:	Return_Val = Busy;
													break;
													
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
													break;
						}
						break;
		
		case	2	:	switch (Return_Val_Routine = Fiddle_One_Right(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
													Return_Val = Finished;
													break;
													
							case	Busy		:	Return_Val = Busy;
													break;
													
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
													break;
						}
						break;
								
		case	3	:	switch (Return_Val_Routine = Fiddle_Multiple_Left(ASL, New_Track, ACT_ST_MCHN[ASL].Brake_Track))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
													Return_Val = Finished;
													break;
													
							case	Busy		:	Return_Val = Busy;
													break;
													
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
													break;
						}
						break;
		
		case	4	:	switch (Return_Val_Routine = Fiddle_One_Left(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
													Return_Val = Finished;
													break;
													
							case	Busy		:	Return_Val = Busy;
													break;
													
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
													break;
						}
						break;
				
		default		:	ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
						Return_Val = ERROR;
						break;	
	}
	return (Return_Val);
}