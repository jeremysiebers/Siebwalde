#include <Drive_Train_IO.h>
#include <Shift_Register.h>
#include <Var_Out.h>
#include <Fiddle_Yard.h>
#include <Track_Move_Ctrl.h>

#define On 1
#define Off 0
#define Universal_Delay_Value 500
#define Train_Brake_Delay_Value 9000

////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0


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
								//move train in track array to state_machine.c and all ref to and from it !!!												
	unsigned int			Train_Brake_Delay,//=0, 					// Delay for braking of Train
							Train_In_Track_Out_Count_Repeater,			// When no trains on fiddle yard and train out then stop drive out, reset every time used
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
	ACT_ST_MCHN[ASL].Train_In_Track_Out_Count_Repeater=0;										// Counter, when no train is on the fiddle yard, the drive out is skipped

}
