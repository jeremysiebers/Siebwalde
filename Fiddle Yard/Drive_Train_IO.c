#include <Drive_Train_IO.h>
#include <Shift_Register.h>
#include <Var_Out.h>
#include <Fiddle_Yard.h>
#include <Track_Move_Ctrl.h>
#include <Bridge_Ctrl.h>

#define On 1
#define Off 0
#define Universal_Delay_Value 500
#define Train_Brake_Delay_Value 9000

////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0

unsigned int Train_In_Track_Out_Count_Repeater = 0;						// When no trains on fiddle yard and train out then stop drive out, reset every time used, therefore not in struct

typedef struct
{
	unsigned char			Init_Fy,//=0,								// Init Fiddle Yard
							Train_Drive_Sequencer,//=0,					// Switch for Train in and out
							Train_Drive_Sequencer_Old,//=0,				// Switch for Train in and out previous value
							Scan_Delay,//=0,							// Delay before scan IO
							*Train_In,									// Pointer used when train in fiddle yard
							Train_In_Track_Count,//=0,					// Track counter max is 11 
							*Train_Out,									// Pointer used when train out fiddle yard
							Train_Out_Track_Count,//=0,					// Track counter max is 11
							Train_In_Track[12];							// Array containing occupied track in fiddle yard
																				
	unsigned int			Train_Brake_Delay,//=0, 					// Delay for braking of Train
							Universal_Delay;//=0;						// Delay after F11 (train detection sensor)
							
							
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,0,0,0,0,0,0,{0,0,0,0,0,0,0,0,0,0,0,0},0,0},	// is 0 is BOTTOM
											 {0,0,0,0,0,0,0,0,{0,0,0,0,0,0,0,0,0,0,0,0},0,0}};	// is 1 is TOP
	

void Drive_Train_IO_Reset(unsigned char ASL)
{
	
	ACT_ST_MCHN[ASL].Train_In_Track[0]=0;														// Array containing occupied track in fiddle yard (0 is not used)
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
	ACT_ST_MCHN[ASL].Train_Drive_Sequencer=0;													// Switch for Train in and out
	ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old=0;												// Switch for Train in and out previous value
	ACT_ST_MCHN[ASL].Scan_Delay=0;																// Delay before scan IO							
	ACT_ST_MCHN[ASL].Train_In_Track_Count=0;													// Track counter max is 11 
	ACT_ST_MCHN[ASL].Train_Out_Track_Count=0;													// Track counter max is 11
	ACT_ST_MCHN[ASL].Train_Brake_Delay=0; 														// Delay before check if train has drove too far
	ACT_ST_MCHN[ASL].Universal_Delay=0;															// Delay after F11 (train detection sensor)

}


unsigned int Train_In_Track_Out_Count_Repeater_Ret(void)
{
	return 	Train_In_Track_Out_Count_Repeater;
}

void Train_In_Track_Out_Count_Set(unsigned int update_Train_In_Track_Out_Count_Repeater)
{
	Train_In_Track_Out_Count_Repeater = update_Train_In_Track_Out_Count_Repeater;	
}

void Train_In_Track_actual(unsigned char ASL, unsigned char *Train_In_Track_Val)
{
	char i;
	for(i=0; i<12; i++)
    {
	    ACT_ST_MCHN[ASL].Train_In_Track[i] = *Train_In_Track_Val;								// update after every train scan the array of the statemachine.c file to 
	    *Train_In_Track_Val++;																	// update this array with the latest detected values and the values already detected
	} 
}


unsigned char *Trains_On_Fiddle_Yard(unsigned char ASL)
{
	return (ACT_ST_MCHN[ASL].Train_In_Track); // return fiddle yard occupation status by sending array to var_out.c
}

unsigned char Fiddle_Yard_Full(unsigned char ASL, unsigned char Return_Val)
{
	if ((ACT_ST_MCHN[ASL].Train_In_Track[1] + ACT_ST_MCHN[ASL].Train_In_Track[2] + ACT_ST_MCHN[ASL].Train_In_Track[3] + ACT_ST_MCHN[ASL].Train_In_Track[4]
		+ ACT_ST_MCHN[ASL].Train_In_Track[5] + ACT_ST_MCHN[ASL].Train_In_Track[6] + ACT_ST_MCHN[ASL].Train_In_Track[7] + ACT_ST_MCHN[ASL].Train_In_Track[8]
		+ ACT_ST_MCHN[ASL].Train_In_Track[9] + ACT_ST_MCHN[ASL].Train_In_Track[10] + ACT_ST_MCHN[ASL].Train_In_Track[11]) >= 10 )	// when fiddle yard is full
	{
		Return_Val = 0;
	}
	else { Return_Val = 1; }
	
	return Return_Val;
}

unsigned char Train_Drive_In(unsigned char ASL, unsigned char Collect_Full)
{
	static char Return_Val = Busy, Return_Val_Routine = Busy;
	
	switch(ACT_ST_MCHN[ASL].Train_Drive_Sequencer)
	{
		case	0	:	ACT_ST_MCHN[ASL].Train_In = &ACT_ST_MCHN[ASL].Train_In_Track[1]; // set pointer voor check of trein op spoor
						ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 1;
						ACT_ST_MCHN[ASL].Train_In_Track_Count = (Track_Nr(ASL) - 1); // Train_In_Track_Count is counter from 0 to 10 sow track - 1
						Return_Val = Busy;
						break;
						
		case	1	:	if (ACT_ST_MCHN[ASL].Train_In_Track_Count > 10) // wanneer spoor 12
						{
							ACT_ST_MCHN[ASL].Train_In_Track_Count = 0; // dan teller terug op spoor 1
						}
						ACT_ST_MCHN[ASL].Train_In = &ACT_ST_MCHN[ASL].Train_In_Track[1]; // set pointer voor check of trein op spoor
						ACT_ST_MCHN[ASL].Train_In += ACT_ST_MCHN[ASL].Train_In_Track_Count;
						if (   *(ACT_ST_MCHN[ASL].Train_In)== 1) // wanneer een trein op een spoor
						{
							ACT_ST_MCHN[ASL].Train_In_Track_Count++;		// tellen welk spoor
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 1;
							Return_Val = Busy;
							break;
						}
						if (*(ACT_ST_MCHN[ASL].Train_In) == 0)		// wanneer geen trein op spoor dan verder
						{
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 2;
						}
						Return_Val = Busy;
						break;
						
		case	2	:	switch (Return_Val_Routine = Track_Mover(ASL,(ACT_ST_MCHN[ASL].Train_In_Track_Count + 1)))	// ga naar leeg spoor
						{							
							case	Finished	:	ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 3;
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old = ACT_ST_MCHN[ASL].Train_Drive_Sequencer;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer = ERROR;
													break;
						}
						break;
												
		case	3	:	switch (Return_Val_Routine = Bridge_Close(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 4; // bruggen dicht
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old = ACT_ST_MCHN[ASL].Train_Drive_Sequencer;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer = ERROR;
													break;
						}
						break;
						
		case	4	:	Bezet_In_7(ASL, Off);		//  blok vrij geven
						Bezet_In_6(ASL, Off);
						Bezet_In_5B(ASL, Off);
						
						if (Collect_Full == 0)
						{
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 5;
							Return_Val = Busy;
						}
						else if (Collect_Full == 1)
						{
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 0;
							Return_Val = Finished;
							break;
						}
						break;
						
		case	5	:	if (Bezet_Uit_6(ASL))	// check trein in blok 6
						{
							ACT_ST_MCHN[ASL].Universal_Delay++;
							if (ACT_ST_MCHN[ASL].Universal_Delay > Universal_Delay_Value)
							{
								Bezet_In_5B(ASL, On);
								ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 6;
								ACT_ST_MCHN[ASL].Universal_Delay = 0;
								Return_Val = Busy;
								break;
							}
						}
						Return_Val = Busy;
						break;
						
		case	6	:	if (F10(ASL))		// check trein voor F10
						{
							ACT_ST_MCHN[ASL].Universal_Delay++;
							if (ACT_ST_MCHN[ASL].Universal_Delay > Universal_Delay_Value)
							{
								ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 7;
								ACT_ST_MCHN[ASL].Universal_Delay = 0;
								Return_Val = Busy;
								break;
							}
						}
						Return_Val = Busy;
						break;
						
		case	7	:	if (Bezet_Uit_7(ASL))		//check trein op FY
						{
							ACT_ST_MCHN[ASL].Universal_Delay++;
							if (ACT_ST_MCHN[ASL].Universal_Delay > Universal_Delay_Value)
							{								
								ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 8;
								ACT_ST_MCHN[ASL].Universal_Delay = 0;
								Return_Val = Busy;
								break;
							}
						}
						Return_Val = Busy;
						break;
					
		case	8	:	if (F11(ASL))			// wanneer rem fotocel een trein ziet rijden
						{
							ACT_ST_MCHN[ASL].Universal_Delay++;
							if (ACT_ST_MCHN[ASL].Universal_Delay > Universal_Delay_Value)
							{
								Bezet_In_6(ASL, On);		// blokken bezet geven
								Bezet_In_7(ASL, On);		// blokken bezet geven
								ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 9;
								ACT_ST_MCHN[ASL].Universal_Delay = 0;
								Return_Val = Busy;
								break;
							}
						}
						Return_Val = Busy;
						break;
						
		case	9	:	if (ACT_ST_MCHN[ASL].Train_Brake_Delay >= Train_Brake_Delay_Value && !F12(ASL)) // wachten x sec trein stil staat en F12 niks ziet dan verder
						{
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 10;
							ACT_ST_MCHN[ASL].Train_Brake_Delay = 0;
							Return_Val = Busy;
							break;
						}
						else if (F12(ASL))												// wanneer F12
						{
							ACT_ST_MCHN[ASL].Universal_Delay++;
							if (ACT_ST_MCHN[ASL].Universal_Delay > Universal_Delay_Value)
							{
								Train_Drive_In_Failed_F12(ASL);							// mislukt F12 zag trein te ver door rijden
								ACT_ST_MCHN[ASL].Train_Brake_Delay = 0;
								ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 11;
								ACT_ST_MCHN[ASL].Universal_Delay = 0;
								Return_Val = Busy;
								break;
							}
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Train_Brake_Delay++;
						break;
						
		case	10	:	switch (Return_Val_Routine = Bridge_Open(ASL))
						{
							case	Finished	:	*(ACT_ST_MCHN[ASL].Train_In) = 1;							// betreffende spoor staat een trein
													//ACT_ST_MCHN[ASL].Train_In_Track_Count = 1;				// spoor teller reset
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 0;					// sequencer reset
													Train_Drive_In_Finished(ASL);
													Return_Val = Finished;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old = ACT_ST_MCHN[ASL].Train_Drive_Sequencer;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer = ERROR;
													break;
						}
						break;
												
		case	11	:	Bezet_In_6(ASL, Off);
						Bezet_In_7(ASL, Off);
						if (!F10(ASL) && !F11(ASL) && !F12(ASL) && !Bezet_Uit_6(ASL) && !Bezet_Uit_7(ASL))
						{
							Bezet_In_6(ASL, On);
							Bezet_In_7(ASL, On);
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 12;
							Return_Val = Busy;
							break;
						}
						Return_Val = Busy;
						break;
						
		case	12	:	*(ACT_ST_MCHN[ASL].Train_In) = 0;								// betreffende spoor staat geen trein
						//ACT_ST_MCHN[ASL].Train_In_Track_Count++;					// spoor teller Ophogen voor ander trein volgende keer in
						ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 0;					// sequencer reset
						Return_Val = Finished;				
						break;
		
		case	ERROR	:	break;
				
		default		:	break;
	}
	return(Return_Val);
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

unsigned char Train_Drive_Out(unsigned char ASL)
{
	static char Return_Val = Busy, Return_Val_Routine = Busy;
	
	switch(ACT_ST_MCHN[ASL].Train_Drive_Sequencer)
	{
		case	0	:	if (!BM_10(ASL) || Bridge_10L(ASL) || Bridge_10R(ASL))
						{
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 10;	//Open bridges
							Return_Val = Busy;
							break;
						}
						if (Train_In_Track_Out_Count_Repeater > 1)
						{							
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 0;
							Bezet_Weerstand(ASL, Off);
							Train_Drive_Out_Finished(ASL);
							Return_Val = Finished;
							break;
						}
						ACT_ST_MCHN[ASL].Train_Out = &ACT_ST_MCHN[ASL].Train_In_Track[1]; // set pointer voor check of trein op spoor
						ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 1;
						Bezet_Weerstand(ASL, On);
						Return_Val = Busy;
						break;
						
		case	1	:	if (ACT_ST_MCHN[ASL].Train_Out_Track_Count > 10) // wanneer spoor 12
						{
							Train_In_Track_Out_Count_Repeater++;		// Wanneer geen trein meer op Fiddle Yard
							ACT_ST_MCHN[ASL].Train_Out_Track_Count = 0; // dan teller terug op spoor 1
						}
						ACT_ST_MCHN[ASL].Train_Out += ACT_ST_MCHN[ASL].Train_Out_Track_Count;
						if (*(ACT_ST_MCHN[ASL].Train_Out) == 0) // wanneer geen trein op een spoor
						{
							ACT_ST_MCHN[ASL].Train_Out_Track_Count++;		// tellen welk spoor
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 0;
							Return_Val = Busy;
							break;
						}
						if (*(ACT_ST_MCHN[ASL].Train_Out) == 1)		// wanneer een trein op spoor dan verder
						{
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 2;
						}
						Return_Val = Busy;
						break;
		
		case	2	:	Train_In_Track_Out_Count_Repeater = 0;
						switch (Return_Val_Routine = Track_Mover(ASL,(ACT_ST_MCHN[ASL].Train_Out_Track_Count + 1)))	// ga naar vol spoor
						{							
							case	Finished	:	ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 3;
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old = ACT_ST_MCHN[ASL].Train_Drive_Sequencer;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer = ERROR;
													break;
						}
						break;
						
		case	3	:	switch (Return_Val_Routine = Bridge_Close(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 4; // bruggen dicht
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old = ACT_ST_MCHN[ASL].Train_Drive_Sequencer;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer = ERROR;
													break;
						}
						break;
						
		case	4	:	Bezet_Weerstand(ASL, Off);
						Bezet_In_7(ASL, Off);
						ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 5;
						Return_Val = Busy;
						break;
						
		case	5	:	if (BM_11(ASL) && (Bridge_10R(ASL) && Bridge_10L(ASL)) && !F10(ASL) && !F11(ASL) && !F12(ASL) && !Bezet_Uit_7(ASL))
						{
							Bezet_In_7(ASL, On);	
							ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 6;					
						}
						Return_Val = Busy;
						break;
						
		case	6	:	ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 0;
						*(ACT_ST_MCHN[ASL].Train_Out) = 0;				// trein weg gereden
						ACT_ST_MCHN[ASL].Train_Out_Track_Count++;	// spoor teller Ophogen voor andere trein volgende keer uit
						Train_Drive_Out_Finished(ASL);
						Return_Val = Finished;						
						break;
						
		case	10	:	switch (Return_Val_Routine = Bridge_Open(ASL))
						{
							case	Finished	:	ACT_ST_MCHN[ASL].Train_Drive_Sequencer = 0; // bruggen Open
													ACT_ST_MCHN[ASL].Train_Out_Track_Count = 1;
													Return_Val = Busy;
													break;
							case	Busy		:	Return_Val = Busy;
													break;
							default				:	Return_Val = Return_Val_Routine;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old = ACT_ST_MCHN[ASL].Train_Drive_Sequencer;
													ACT_ST_MCHN[ASL].Train_Drive_Sequencer = ERROR;
													break;
						}
						break;
						
		case	ERROR	:	break;
		
		default		:	break;
	}
	return (Return_Val);
}