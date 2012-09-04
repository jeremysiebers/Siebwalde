#include <Fiddle_Init.h>
#include <Shift_Register.h>
#include <Var_Out.h>
#include <Track_Move_Ctrl.h>
#include <Bridge_Ctrl.h>
#include <Train_Detection.h>
#include <Fiddle_Move_Ctrl.h>

////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0

#define On 1
#define Off 0


typedef struct
{
	unsigned char			Init_Fy;//=0,								// Init Fiddle Yard
							
							
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0},	// is 0 is BOTTOM
											 {0}};	// is 1 is TOP
		

void Fiddle_Init_Reset(unsigned char ASL)
{
	ACT_ST_MCHN[ASL].Init_Fy=0;																	// Reset Init Fiddle Yard switch var
}


unsigned char Init_Fiddle_Yard(unsigned char ASL, unsigned char Train_Detection_Cmd)
{
	static char Return_Val = Busy, Return_Val_Routine = Busy;
	
	switch(ACT_ST_MCHN[ASL].Init_Fy)
	{
		case	0	:	Init_Started(ASL);
						Return_Val = Busy;
						if ((Track_Nr(ASL) > 0) && !F12(ASL))
						{
							ACT_ST_MCHN[ASL].Init_Fy = 1; //Situatie 1 init
							Return_Val = Busy;
						}
						else {ACT_ST_MCHN[ASL].Init_Fy = 2; Return_Val = Busy;} //Situatie 2 init
						break;
						
		case	1	:	if (Off == Train_Detection_Cmd)
						{
							switch (Return_Val_Routine = Train_Detection(ASL))
							{
								case	Finished	:	ACT_ST_MCHN[ASL].Init_Fy = 0; //Sporen in lezen klaar init done
														Return_Val = Finished;
														Init_Done(ASL);
														break;
								case	Busy		:	Return_Val = Busy;
														break;
								default				:	Return_Val = Return_Val_Routine;
														ACT_ST_MCHN[ASL].Init_Fy = 0;
														break;
							}
						}
						else if (On == Train_Detection_Cmd)
						{
							ACT_ST_MCHN[ASL].Init_Fy = 0; //init done
							Return_Val = Finished;
							Init_Done(ASL);
							break;	
						}
						break;
						
		case	2	:	if (Track_Nr(ASL) == 0)
						{
							ACT_ST_MCHN[ASL].Init_Fy = 3; // Not aligned, Move 1 track
							Return_Val = Busy;
							break;
						}
						if (F10(ASL) && (Bezet_Uit_6(ASL) || F12(ASL)))
						{
							ACT_ST_MCHN[ASL].Init_Fy = 4; // Trein uit/door laten rijden
							Return_Val = Busy;
							break;
						}
						else
						{
							ACT_ST_MCHN[ASL].Init_Fy = 5;
							Return_Val = Busy;
							break;	
						}
						if (!F10(ASL) && !F11(ASL) && !F12(ASL) && !Bezet_Uit_6(ASL) && !Bezet_Uit_7(ASL))
						{
							ACT_ST_MCHN[ASL].Init_Fy = 5; // Geen trein aanwezig wel bruggen dicht > bruggen open
							Return_Val = Busy;
							break;
						}						
						Return_Val = Busy;
						break;
						
		case	3	:	switch (Return_Val_Routine = Fiddle_One_Left(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Init_Fy = 0; // check again if everything ok?
													Return_Val = Busy;
													break;
													
							case	Busy		:	Return_Val = Busy;
													break;
													
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Init_Fy = 0;
													break;
						}
						break;
						
		case	4	:	Bezet_In_6(ASL, Off);
						Bezet_In_7(ASL, Off);
						if (!F10(ASL) && !F11(ASL) && !F12(ASL) && !Bezet_Uit_6(ASL) && !Bezet_Uit_7(ASL))
						{
							Bezet_In_6(ASL, On);
							Bezet_In_7(ASL, On);
							Bezet_In_5B(ASL, On);
							ACT_ST_MCHN[ASL].Init_Fy = 5; // Geen trein meer aanwezig wel bruggen dicht > bruggen open
						}
						Return_Val = Busy;
						break;
		
		case	5	:	switch (Return_Val_Routine = Bridge_Open(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Init_Fy = 0;				// check again if everything ok?
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Init_Fy = 0;
													break;
						}
						break;
						
		default		:	break;
	}
	return(Return_Val);	
}