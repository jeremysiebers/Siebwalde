#include <State_Machine.h>
#include <Shift_Register.h>
#include <pwm.h>
#include <Command_Machine.h>
#include <Var_Out.h>
#include <Fiddle_Yard.h>

#define On 1
#define Off 0
#define Open 1
#define Closed 0
#define Universal_Delay_Value 500
#define Train_Brake_Delay_Value 9000
#define Bridge_Open_Close_Timeout 5000

////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0

////////Bridge Errors//////
#define Bezet_Uit_Blok_6 0x01
#define Sensor_F12 0x02
#define Bezet_Uit_Blok_6_AND_Sensor_F12 0x03
#define CL_10_Heart_Sensor 0x04
#define Bridge_Open_Close_Timeout_Expired 0x05

////////Fiddle Yard move Errors//////
#define BridgeMotorContact_10 0x06
#define Bridge_10L_Contact 0x07
#define Bridge_10R_Contact 0x08
#define BridgeMotorContact_11 0x09
#define EndOffStroke_11 0x0A
#define Laatste_Spoor 0x0B
#define EndOffStroke_10 0x0C

////////State_Machine_Switch////////
#define Fiddle_Yard_Busy 255
#define Fiddle_Yard_Running 254
#define Resume 100
#define Fy_Reset 0
#define Idle 1
#define Fy_Standard_Run 2
#define Fy_Running_Standard 3
#define Fy_Collect 4
#define ERROR_Handler_Idle 9

////////Exe_Cmd_()////////
#define Nopp 0
#define Open_Bridge 1
#define Close_Bridge 2
#define Fiddle_Yard_One_Left 3
#define Fiddle_Yard_One_Right 4
#define Track_1 5
#define Track_2 6
#define Track_3 7
#define Track_4 8
#define Track_5 9
#define Track_6 10
#define Track_7 11
#define Track_8 12
#define Track_9 13
#define Track_10 14
#define Track_11 15
#define Detection_Train 16
#define Run_Fiddle_Yard 17
#define Stop_Fiddle_Yard 18
#define Stop_Fiddle_Yard_Now 19
#define Bezet_In_5B_Switch_On 20
#define Bezet_In_5B_Switch_Off 21
#define Bezet_In_6_Switch_On 22
#define Bezet_In_6_Switch_Off 23
#define Bezet_In_7_Switch_On 24
#define Bezet_In_7_Switch_Off 25
#define Restart_Previous_Command 26
#define Start_Collect 27

////////Fy_Running (standard)////////
#define Train_On_5B_Start 0
#define No_Train_On_8_Start 1
#define Drive_Train_In 2
#define Drive_Train_Out 3
#define Fy_Collect_Full 4

////////Motor max PWM settings////////
#define PWM_ONE_LEFT 700
#define PWM_ONE_SLOW_LEFT 600
#define PWM_ONE_RIGHT 323
#define PWM_ONE_SLOW_RIGHT 424

#define PWM_MULTIPLE_LEFT 800
#define PWM_MULTIPLE_SLOW_LEFT 600
#define PWM_MULTIPLE_RIGHT 223
#define PWM_MULTIPLE_SLOW_RIGHT 424

#define PWM_ONE_COUNT_LEFT_INIT 512
#define PWM_MULTIPLE_COUNT_LEFT_INIT 512
#define PWM_ONE_COUNT_RIGHT_INIT 512
#define PWM_MULTIPLE_COUNT_RIGHT_INIT 512

#define PWM_ADC_MULTIPLE_SPEED 720			//hysteresis upper
#define PWM_ADC_MULTIPLE_SPEED_LOW 700		//hysteresis lower
#define PWM_ADC_ONE_SPEED 570
#define PWM_ADC_ONE_SPEED_LOW 550
#define PWM_ADC_SLOW 280
#define PWM_ADC_SLOW_LOW 260
#define PWM_ADC_ENC_STALL 50


#define MECH_CONSTANT 5
#define MECH_REWIND 500
#define MECH_SKIP_HEART 200
#define PWM_FAST_COUNTER_CONST 3
#define PWM_ONE_INBETWEEN_COUNTER_CONST 625	
#define PWM_RAMP_UP_DELAY_REG_CONST 5
#define PWM_RAMP_UP_MULTIPLE_DELAY_REG_CONST 1							//	When  "1" a devider of 2 is active ( 0 - > 1 )
#define ENCODER_LOST_COUNTER_CONST 100

static unsigned char Init_Fiddle_Yard(unsigned char ASL, unsigned char Train_Detection_Cmd);
static unsigned char Train_Detection(unsigned char ASL);
static unsigned char Train_Drive_In(unsigned char ASL, unsigned char Collect_Full);
static unsigned char Train_Drive_Out(unsigned char ASL);
static unsigned char Bridge_Open(unsigned char ASL);
static unsigned char Bridge_Close(unsigned char ASL);
static unsigned char Fiddle_One_Right(unsigned char ASL);
static unsigned char Fiddle_One_Left(unsigned char ASL);
static unsigned char Track_Mover(unsigned char ASL, char New_Track);
static unsigned char Fiddle_Multiple_Right(unsigned char ASL, char New_Track_Muktiple_Right);
static unsigned char Fiddle_Multiple_Left(unsigned char ASL, char New_Track_Muktiple_Left);
static unsigned char Fiddle_Yard_Full(unsigned char ASL, unsigned char Return_Val);

static unsigned int Send_Var_Out[3];

void ERROR_Code_Report(unsigned char ASL, unsigned char Code);
void PWM_Update(unsigned char ASL, unsigned int Value);
void Pwm_Brake(unsigned char ASL, unsigned char Value);

unsigned int Train_In_Track_Out_Count_Repeater = 0;						// When no trains on fiddle yard and train out then stop drive out, reset every time used, therefore not in struct

typedef struct
{
	unsigned int		 	Pwm_One_Left,//=800, 						// Max speed one left
							Pwm_One_Slow_Left,//=750, 					// Slow speed left when to much right
							Pwm_One_Count_Left_Init,// = 576,			// Start value PWM left
							Pwm_One_Right,//=224, 						// Max speed one right
							Pwm_One_Slow_Right,//=250,					// Slow speed right when to much left
							Pwm_One_Count_Right_Init,// = 400,			// Start value PWM right
							Mech_Constant,// = 5,						// Delay for check if heart = 0
							Mech_Rewind,// = 500,						// Delay before oposite movement
							Pwm_Fast_Counter_Const,// = 4,				// Delay PWM increment/decrement
							Pwm_One_Inbetween_Counter_Const,			// When moving one track, slow start - run until this counter has passed - to slow left - stop
							
							Pwm_Multiple_Left,//=1023,					// Max speed Multiple left
							Pwm_Multiple_Slow_Left,//=750, 				// Slow speed left when to much right or speed after brake
							Pwm_Multiple_Count_Left_Init,// = 576,		// Start value PWM left
							Pwm_Multiple_Right,//=0,	 				// Max speed Multiple right
							Pwm_Multiple_Slow_Right,//=250,				// Slow speed right when to much left or speed after brake
							Pwm_Multiple_Count_Right_Init;// = 400;		// Start value PWM right
							
	unsigned int 			Pwm_One_Count_Left,//=0,					// PWM One left value
							Pwm_One_Count_Right,//=0,					// PWM One right value
							
							Pwm_Multiple_Count_Left,//=0,				// PWM Multiple left value
							Pwm_Multiple_Count_Right,//=0,				// PWM Multiple right value
							
							Mech_Delay,//=0,							// Delay counter for check if heart = 0
							Pwm_Fast_Counter,//=0,						// Delay PWM increment/decrement counter
							
							Pwm_One_Inbetween_Counter,					// When moving one track, slow start - run until this counter has passed - to slow left - stop
							Pwm_Ramp_up_delay_Reg,						// Frequentie divider from the 2.4KHz to determine delta of actual and wanted motor speed of the regulator							
							Pwm_Reg_Switch,								// Switch of regulator
							AdcResults,									// Contains ADC results after conversion
							Encoder_Lost_Counter,						// When no encoder pulses are ariving(disconnected) Pwm must be limited after several times lower dan adc result of 25.
							ADCconversion_Inuse;						// When an ADC conversion is busy the other routine has to wait with setting its Go bit for conversion
							
	unsigned char 			Bridge_Open,//=0,							// Switch for bridge open
							Bridge_Close,//=0,							// Switch for bridge close
							Brigde_Status,//=0,							// Old bridge status when Error
							Fiddle,//=0,								// Switch for Fiddle yard movements
							Set_Action;//=0;							// repeat command until finished						

	unsigned char 			Fiddle_Track_Mover,//=0,					// Switch for Fiddle direction movement
							Old_Track,//=0,								// Previous track for track mover routine
							Old_Track_2,//=0,							// Previous track for Error handling return to last track
							Brake_Track;//=0;							// Brake Track when slower PWM is required
							
	unsigned char			Init_Fy,//=0,								// Init Fiddle Yard
							Train_Drive_Sequencer,//=0,					// Switch for Train in and out
							Train_Drive_Sequencer_Old,//=0,				// Switch for Train in and out previous value
							Scan_Delay,//=0,							// Delay before scan IO
							Train_Detector,//=0,						// Switch for train counter (trains on fiddle yard)
							Train_Detector_Move,//=1,					// Move to track for train detection
							Train_In_Track[12],							// Array containing occupied track in fiddle yard
							*PTIT,										// Pointer to Train In Track
							*Train_In,									// Pointer used when train in fiddle yard
							Train_In_Track_Count,//=0,					// Track counter max is 11 
							*Train_Out,									// Pointer used when train out fiddle yard
							Train_Out_Track_Count,//=0,					// Track counter max is 11
							
							State_Machine_Switch,						// State Machine Main Switch
							Fy_Running,// = Train_On_5B_Start,			// Switch for standard in<>out program
							Fy_Running_Old,// = 0,						// Old state when error occured
							Fy_Running_2,
							Fy_Running_Old_2,
							Fy_Init_Done,// = Off,						// Used for soft start of FY
							Execute_Command,// = Nopp,					// Used for executing commands when Idle, manipulate when resuming from error
							Execute_Command_Old,// = Nopp;				// Used when resuming
							FY_Running_Error,							// Switch used when resuming from error inside program
							Collect; // = Off;							// When trains need to be collected
													
	unsigned int			Train_Brake_Delay,//=0, 					// Delay for braking of Train
							Universal_Delay,//=0,						// Delay after F11 (train detection sensor)
							Bridge_Open_Close_Timeout_Counter;//=0;		// Timeout counter for bridge movement
							
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,{0,0,0,0,0,0,0,0,0,0,0,0},0,0,0,0,0,Fy_Reset,Train_On_5B_Start,0,0,0,Off,Nopp,Nopp,0,0,0,0,0},	// is 0 is BOTTOM
											 {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,{0,0,0,0,0,0,0,0,0,0,0,0},0,0,0,0,0,Fy_Reset,Train_On_5B_Start,0,0,0,Off,Nopp,Nopp,0,0,0,0,0}};	// is 1 is TOP
											 

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
void State_Machine_Update(unsigned char ASL)	//ASL = Active_Struct_Level, BOTTOM(0) or TOP(1)
{
	static char Return_Val_Routine = Busy;
	
	if ((ACT_ST_MCHN[TOP].ADCconversion_Inuse == True) || (ACT_ST_MCHN[BOTTOM].ADCconversion_Inuse == True))
	{
		Led3 = 1;
	}
	else { Led3 = 0;}
		
	switch (ACT_ST_MCHN[ASL].State_Machine_Switch)
	{
						case Fy_Reset	:	M10(ASL,Off);
											M11(ASL,Off);
											PWM_Update(ASL, 512);
											Pwm_Brake(ASL, On);
											Bezet_In_5B(ASL,On);
											Bezet_In_6(ASL,On);
											Bezet_In_7(ASL,On);
											Bezet_Weerstand(ASL,Off);
											
											ACT_ST_MCHN[1].Pwm_One_Left=PWM_ONE_LEFT;													// Max speed one left TOP
											ACT_ST_MCHN[1].Pwm_One_Slow_Left=PWM_ONE_SLOW_LEFT;											// Slow speed left when to much right TOP
											ACT_ST_MCHN[0].Pwm_One_Left=PWM_ONE_LEFT;													// Max speed one left BOTTOM
											ACT_ST_MCHN[0].Pwm_One_Slow_Left=PWM_ONE_SLOW_LEFT;											// Slow speed left when to much right BOTTOM
											
											ACT_ST_MCHN[ASL].Pwm_One_Count_Left_Init = PWM_ONE_COUNT_LEFT_INIT;							// Start value PWM left
											
											ACT_ST_MCHN[1].Pwm_One_Right=PWM_ONE_RIGHT;													// Max speed one right TOP
											ACT_ST_MCHN[1].Pwm_One_Slow_Right=PWM_ONE_SLOW_RIGHT;										// Slow speed right when to much left TOP
											ACT_ST_MCHN[0].Pwm_One_Right=PWM_ONE_RIGHT;													// Max speed one right BOTTOM
											ACT_ST_MCHN[0].Pwm_One_Slow_Right=PWM_ONE_SLOW_RIGHT;										// Slow speed right when to much left BOTTOM
											
											ACT_ST_MCHN[ASL].Pwm_One_Count_Right_Init = PWM_ONE_COUNT_RIGHT_INIT;						// Start value PWM right
											ACT_ST_MCHN[ASL].Mech_Constant = MECH_CONSTANT;												// Delay for check if heart = 0
											ACT_ST_MCHN[ASL].Mech_Rewind = MECH_REWIND;													// Delay before oposite movement
											ACT_ST_MCHN[ASL].Pwm_Fast_Counter_Const = PWM_FAST_COUNTER_CONST;							// Delay PWM increment/decrement
											ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter_Const = PWM_ONE_INBETWEEN_COUNTER_CONST;			// When moving one track, slow start - run until this counter has passed - to slow left - stop
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;													// Frequentie divider from the 2.4KHz to determine delta of actual and wanted motor speed of the regulator							
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;														// Switch of regulator
											ACT_ST_MCHN[ASL].AdcResults = 0;															// ADC results after conversion of ADc
											ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;													// When no encoder pulses are ariving(disconnected) Pwm must be limited after several times lower dan adc result of 25.
											ACT_ST_MCHN[ASL].ADCconversion_Inuse = 0;													// When an ADC conversion is busy the other routine has to wait with setting its Go bit for conversion
											
											ACT_ST_MCHN[1].Pwm_Multiple_Left=PWM_MULTIPLE_LEFT;											// Max speed Multiple left
											ACT_ST_MCHN[1].Pwm_Multiple_Slow_Left=PWM_MULTIPLE_SLOW_LEFT;								// Slow speed left when to much right or speed after brake
											ACT_ST_MCHN[0].Pwm_Multiple_Left=PWM_MULTIPLE_LEFT;											// Max speed Multiple left
											ACT_ST_MCHN[0].Pwm_Multiple_Slow_Left=PWM_MULTIPLE_SLOW_LEFT;								// Slow speed left when to much right or speed after brake
											
											ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left_Init = PWM_MULTIPLE_COUNT_LEFT_INIT;				// Start value PWM left
											
											ACT_ST_MCHN[1].Pwm_Multiple_Right=PWM_MULTIPLE_RIGHT;	 									// Max speed Multiple right											
											ACT_ST_MCHN[1].Pwm_Multiple_Slow_Right=PWM_MULTIPLE_SLOW_RIGHT;								// Slow speed right when to much left or speed after brake
											ACT_ST_MCHN[0].Pwm_Multiple_Right=PWM_MULTIPLE_RIGHT; 										// Max speed Multiple right											
											ACT_ST_MCHN[0].Pwm_Multiple_Slow_Right=PWM_MULTIPLE_SLOW_RIGHT;								// Slow speed right when to much left or speed after brake
											
											ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right_Init = PWM_MULTIPLE_COUNT_RIGHT_INIT;				// Start value PWM right
											
											ACT_ST_MCHN[ASL].Pwm_One_Count_Left=0;														// PWM One left value
											ACT_ST_MCHN[ASL].Pwm_One_Count_Right=0;														// PWM One right value
											
											ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left=0;													// PWM Multiple left value
											ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right=0;												// PWM Multiple right value
											
											ACT_ST_MCHN[ASL].Mech_Delay=0;																// Delay counter for check if heart = 0
											ACT_ST_MCHN[ASL].Pwm_Fast_Counter=0;														// Delay PWM increment/decrement counter
											ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter = 0;												// When moving one track, slow start - run until this counter has passed - to slow left - stop
											
					 						ACT_ST_MCHN[ASL].Bridge_Open=0;																// Switch for bridge open
					 						ACT_ST_MCHN[ASL].Bridge_Close=0;															// Switch for bridge close
					 						ACT_ST_MCHN[ASL].Brigde_Status=2;															// Old bridge status when Error
											ACT_ST_MCHN[ASL].Fiddle=0;																	// Switch for Fiddle yard movements
											ACT_ST_MCHN[ASL].Set_Action=0;																// repeat command until finished						
					
					 						ACT_ST_MCHN[ASL].Fiddle_Track_Mover=0;														// Switch for Fiddle direction movement
					 						ACT_ST_MCHN[ASL].Old_Track=0;																// Previous track
					 						ACT_ST_MCHN[ASL].Old_Track_2=0;																// Previous track for Error handling return to last track
											ACT_ST_MCHN[ASL].Brake_Track=0;																// Brake Track when slower PWM is required
											
											ACT_ST_MCHN[ASL].Init_Fy=0;																	// Init Fiddle Yard
											ACT_ST_MCHN[ASL].Train_Drive_Sequencer=0;													// Switch for Train in and out
											ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old=0;												// Switch for Train in and out previous value
											ACT_ST_MCHN[ASL].Scan_Delay=0;																// Delay before scan IO
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
											ACT_ST_MCHN[ASL].Train_In_Track_Count=0;													// Track counter max is 11 
											ACT_ST_MCHN[ASL].Train_Out_Track_Count=0;													// Track counter max is 11
											ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;												// State Machine Main Switch
											ACT_ST_MCHN[ASL].Fy_Running = Train_On_5B_Start;											// Switch for standard in<>out program
											ACT_ST_MCHN[ASL].Fy_Running_Old = 0;														// Old state when error occured
											ACT_ST_MCHN[ASL].Fy_Running_2 = Train_On_5B_Start;											// Switch for Collect program
											ACT_ST_MCHN[ASL].Fy_Running_Old_2 = 0;														// Old state when error occured
											ACT_ST_MCHN[ASL].Fy_Init_Done = Off;														// Used for soft start of FY
											ACT_ST_MCHN[ASL].Execute_Command = Nopp;													// Used for executing commands when Idle, manipulate when resuming from error
											ACT_ST_MCHN[ASL].Execute_Command_Old = Nopp;												// Used when resuming
											ACT_ST_MCHN[ASL].FY_Running_Error = 0;														// Switch used when resuming from error inside program
											ACT_ST_MCHN[ASL].Collect = Off;																// When trains need to be collected
																	
											ACT_ST_MCHN[ASL].Train_Brake_Delay=0, 
											ACT_ST_MCHN[ASL].Universal_Delay=0,															// Delay after F11 (train detection sensor)
											ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter=0;										// Time out counter for bridges when waiting for sensor on Motor
											
											ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
											break;
						
						case Idle	:		switch (ACT_ST_MCHN[ASL].Execute_Command = Exe_Cmd_(ASL))
											{
												case	Nopp					:	break; // no command received (No Opperation Pending ;-)
												case	Open_Bridge				:	switch(Return_Val_Routine = Bridge_Open(ASL))
																					{
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												ACT_ST_MCHN[ASL].Execute_Command_Old = ACT_ST_MCHN[ASL].Execute_Command;
																												Exe_Cmd_Ret(ASL,0);
																												ACT_ST_MCHN[ASL].State_Machine_Switch = ERROR_Handler_Idle;
																												break;
																					}
																					break;
																					
												case	Close_Bridge			:	switch(Return_Val_Routine = Bridge_Close(ASL))
																					{
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												ACT_ST_MCHN[ASL].Execute_Command_Old = ACT_ST_MCHN[ASL].Execute_Command;
																												Exe_Cmd_Ret(ASL,0);
																												ACT_ST_MCHN[ASL].State_Machine_Switch = ERROR_Handler_Idle;
																												break;
																					}
																					break;
																					
												case	Fiddle_Yard_One_Left	:	switch(Return_Val_Routine = Fiddle_One_Left(ASL))
																					{
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
																					
												case	Fiddle_Yard_One_Right	:	switch(Return_Val_Routine = Fiddle_One_Right(ASL))
																					{
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
																																		
												case	Track_1					:	switch(Return_Val_Routine = Track_Mover(ASL,1))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_2					:	switch(Return_Val_Routine = Track_Mover(ASL,2))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_3					:	switch(Return_Val_Routine = Track_Mover(ASL,3))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_4					:	switch(Return_Val_Routine = Track_Mover(ASL,4))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_5					:	switch(Return_Val_Routine = Track_Mover(ASL,5))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_6					:	switch(Return_Val_Routine = Track_Mover(ASL,6))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_7					:	switch(Return_Val_Routine = Track_Mover(ASL,7))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_8					:	switch(Return_Val_Routine = Track_Mover(ASL,8))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_9					:	switch(Return_Val_Routine = Track_Mover(ASL,9))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_10				:	switch(Return_Val_Routine = Track_Mover(ASL,10))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
												case	Track_11				:	switch(Return_Val_Routine = Track_Mover(ASL,11))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												Exe_Cmd_Ret(ASL,0);
																												break;
																					}
																					break;
																					
												case	Detection_Train			:	switch(Return_Val_Routine = Train_Detection(ASL))
																					{	
																						case	Finished	:	Exe_Cmd_Ret(ASL,0);
																												break;
																						case	Busy		:	break;
																						default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																												ACT_ST_MCHN[ASL].Execute_Command_Old = ACT_ST_MCHN[ASL].Execute_Command;
																												Exe_Cmd_Ret(ASL,0);
																												ACT_ST_MCHN[ASL].State_Machine_Switch = ERROR_Handler_Idle;
																												break;
																					}
																					break;
																					
												case	Run_Fiddle_Yard			:	if (ACT_ST_MCHN[ASL].Fy_Init_Done == On)
																					{
																						Fiddle_Yard_Soft_Start(ASL);
																						if (ACT_ST_MCHN[ASL].Collect == Off)
																						{
																							ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Standard_Run;
																						}
																						else if (ACT_ST_MCHN[ASL].Collect == On)
																						{
																							ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Collect;
																						}
																						
																					}
																					else if (ACT_ST_MCHN[ASL].Fy_Init_Done == Off)
																					{
																						ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Standard_Run;
																					}
																					Exe_Cmd_Ret(ASL,1);	// ASL,1 (1) means no other commands allowed
																					break;
																					
												case	Stop_Fiddle_Yard_Now	:	Fiddle_Yard_Reset(ASL);
																					ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Reset;
																					Exe_Cmd_Ret(ASL,0);
																					break;
																					
												case 	Bezet_In_5B_Switch_On	:	Bezet_In_5B(ASL,On);
																					Exe_Cmd_Ret(ASL,0);
																					break;
																					
												case 	Bezet_In_5B_Switch_Off	:	Bezet_In_5B(ASL,Off);
																					Exe_Cmd_Ret(ASL,0);
																					break;
																					
												case 	Bezet_In_6_Switch_On	:	Bezet_In_6(ASL,On);
																					Exe_Cmd_Ret(ASL,0);
																					break;
																					
												case 	Bezet_In_6_Switch_Off	:	Bezet_In_6(ASL,Off);
																					Exe_Cmd_Ret(ASL,0);
																					break;
																					
												case 	Bezet_In_7_Switch_On	:	Bezet_In_7(ASL,On);
																					Exe_Cmd_Ret(ASL,0);
																					break;
																					
												case 	Bezet_In_7_Switch_Off	:	Bezet_In_7(ASL,Off);
																					Exe_Cmd_Ret(ASL,0);
																					break;
																					
												case 	Start_Collect			:	ACT_ST_MCHN[ASL].Collect = !ACT_ST_MCHN[ASL].Collect;
																					Exe_Cmd_Ret(ASL,0);
																					ACT_ST_MCHN[ASL].Fy_Running = Train_On_5B_Start;
																					switch (ACT_ST_MCHN[ASL].Collect)
																					{
																						case	On	:	Collect_On(ASL); break;
																						case	Off	:	Collect_Off(ASL); break;
																						default		:	break;
																					}
																					break;

												default							:	break;	
											}	
										break;
							
		case Fy_Standard_Run		:	if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard_Now)										// Init Program
										{
											Fiddle_Yard_Reset(ASL);
											ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Reset;
											break;
										} 
										switch (Return_Val_Routine = Init_Fiddle_Yard(ASL,ACT_ST_MCHN[ASL].Fy_Init_Done))
										{
											case	Finished	:	ACT_ST_MCHN[ASL].Fy_Init_Done = On;
																	if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard)
																	{
																		Fiddle_Yard_Stopped(ASL);
																		ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																		break;
																	}
																	if (ACT_ST_MCHN[ASL].Collect == Off)
																	{
																		ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Running_Standard;
																	}
																	else if (ACT_ST_MCHN[ASL].Collect == On)
																	{
																		ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Collect;
																	} 
																	break;
											case	Busy		:	break;
											default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																	ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																	break;	//TERUG LATEN SCHREEUWEN wanneer error naar boven komt!!!!!!!!!!!!!!!!!!!
										}										
										break;
							
		case Fy_Running_Standard	:	if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard_Now)										// Drive in - Drive Out program
										{
											Fiddle_Yard_Stopped(ASL);
											ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Reset;
											break;
										}
										ACT_ST_MCHN[ASL].Collect = Off;
										
										switch (ACT_ST_MCHN[ASL].Fy_Running)
										{
											case	Train_On_5B_Start	:	if ((Bezet_Uit_5B(ASL)) && (Fiddle_Yard_Full(ASL,0)))
																			{
																				Train_In_Track_Out_Count_Repeater = 0;
																				Train_On_5B(ASL);
																				Train_Drive_In_Start(ASL);
																				ACT_ST_MCHN[ASL].Fy_Running = Drive_Train_In; // Train Drive In
																				break;
																			}
																			else {ACT_ST_MCHN[ASL].Fy_Running = No_Train_On_8_Start;}
																			break;	
																			
											case	Drive_Train_In		:	switch (Return_Val_Routine = Train_Drive_In(ASL,0))
																			{
																				case	Finished	:	if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard)
																										{
																											Fiddle_Yard_Stopped(ASL);
																											ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																											break;
																										} 
																										ACT_ST_MCHN[ASL].Fy_Running = No_Train_On_8_Start; // Train out?
																										break;
																				case	Busy		:	break;
																				default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																										ACT_ST_MCHN[ASL].Fy_Running_Old = ACT_ST_MCHN[ASL].Fy_Running;
																										ACT_ST_MCHN[ASL].Fy_Running = ERROR;
																										//ACT_ST_MCHN[ASL].Fy_Init_Done = Off;
																										break;	//TERUG LATEN SCHREEUWEN wanneer error naar boven komt!!!!!!!!!!!!!!!!!!!
																			}
																			break;
																			
											case	No_Train_On_8_Start	:	if (!(Bezet_Uit_8A(ASL)) && (Train_In_Track_Out_Count_Repeater == 0))
																			{
																				Train_On_8A(ASL);
																				ACT_ST_MCHN[ASL].Fy_Running = Drive_Train_Out; // Train Drive Out
																				Train_Drive_Out_Start(ASL);
																				break;
																			}
																			else {ACT_ST_MCHN[ASL].Fy_Running = Train_On_5B_Start;}
																			break;		
																			
											case	Drive_Train_Out		:	switch (Return_Val_Routine = Train_Drive_Out(ASL))
																			{
																				case	Finished	:	if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard)
																										{
																											Fiddle_Yard_Stopped(ASL);
																											ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																											break;
																										} 
																										ACT_ST_MCHN[ASL].Fy_Running = Train_On_5B_Start; // back to main
																										break;
																				case	Busy		:	break;
																				default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																										ACT_ST_MCHN[ASL].Fy_Running_Old = ACT_ST_MCHN[ASL].Fy_Running;
																										ACT_ST_MCHN[ASL].Fy_Running = ERROR;
																										//ACT_ST_MCHN[ASL].Fy_Init_Done = Off;
																										break;	//TERUG LATEN SCHREEUWEN wanneer error naar boven komt!!!!!!!!!!!!!!!!!!!
																			}
																			break;
																			
											case	ERROR				:	switch (ACT_ST_MCHN[ASL].FY_Running_Error)
																			{
																				case	0	:	ACT_ST_MCHN[ASL].Old_Track_2 = Track_Nr(ASL);
																								ACT_ST_MCHN[ASL].FY_Running_Error = 1;
																								ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																								Bezet_Weerstand(ASL, Off);
																								break;
																								
																				case	1	:	switch (Return_Val_Routine = Init_Fiddle_Yard(ASL,Off))
																								{
																									case	Finished	:	ACT_ST_MCHN[ASL].Fy_Init_Done = On;
																															if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard)
																															{
																																Fiddle_Yard_Stopped(ASL);
																																ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																																break;
																															}
																															ACT_ST_MCHN[ASL].FY_Running_Error = 2;				// bridges are open after init=ok
																															break;
																									case	Busy		:	break;
																									default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																															ACT_ST_MCHN[ASL].Fy_Running = ERROR;
																															ACT_ST_MCHN[ASL].FY_Running_Error = 1;
																															ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																															break;
																								}
																								break;
																								
																				case	2	:	switch (Return_Val_Routine = Track_Mover(ASL,ACT_ST_MCHN[ASL].Old_Track_2))
																								{
																									case	Finished	:	ACT_ST_MCHN[ASL].FY_Running_Error = 0; 
																															ACT_ST_MCHN[ASL].Fy_Running = ACT_ST_MCHN[ASL].Fy_Running_Old;
																															ACT_ST_MCHN[ASL].Train_Drive_Sequencer = ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old;
																															break;
																									case	Busy		:	break;
																									default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																															ACT_ST_MCHN[ASL].Fy_Running = ERROR;
																															ACT_ST_MCHN[ASL].FY_Running_Error = 1;
																															ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																															break;
																								}
																								break;
																																																												
																				default		:	break;
																				
																			}
																			break;
																																						
											default						:	ACT_ST_MCHN[ASL].Fy_Running = Train_On_5B_Start;
																			break;	
										}
										break;
										
		case	Fy_Collect			:	if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard_Now)							// Collect all incoming trains, leave 1 track empty
										{
											Fiddle_Yard_Stopped(ASL);
											ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Reset;
											break;
										}
										if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard)
										{
											Fiddle_Yard_Stopped(ASL);
											ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
										} 
																				
										switch (ACT_ST_MCHN[ASL].Fy_Running_2)
										{
											case	Train_On_5B_Start	:	if ((Bezet_Uit_5B(ASL)) && (Fiddle_Yard_Full(ASL,0)))
																			{
																				Train_In_Track_Out_Count_Repeater = 0;
																				Train_On_5B(ASL);
																				Train_Drive_In_Start(ASL);
																				ACT_ST_MCHN[ASL].Fy_Running_2 = Drive_Train_In; // Train Drive In
																				break;
																			}
																			else if (Fiddle_Yard_Full(ASL,0) == 0)
																			{
																				ACT_ST_MCHN[ASL].Fy_Running_2 = Fy_Collect_Full;
																				Collect_Finished_Fy_Full(ASL);																				
																			}
																			break;	
																			
											case	Drive_Train_In		:	switch (Return_Val_Routine = Train_Drive_In(ASL,0))
																			{
																				case	Finished	:	if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard)
																										{
																											Fiddle_Yard_Stopped(ASL);
																											ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																											break;
																										} 
																										ACT_ST_MCHN[ASL].Fy_Running_2 = Train_On_5B_Start; // Train out?
																										break;
																				case	Busy		:	break;
																				default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																										ACT_ST_MCHN[ASL].Fy_Running_Old_2 = ACT_ST_MCHN[ASL].Fy_Running_2;
																										ACT_ST_MCHN[ASL].Fy_Running_2 = ERROR;
																										break;	//TERUG LATEN SCHREEUWEN wanneer error naar boven komt!!!!!!!!!!!!!!!!!!!
																			}
																			break;
																			
											case	Fy_Collect_Full		:	switch (Return_Val_Routine = Train_Drive_In(ASL,1))
																			{
																				case	Finished	:	if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard)
																										{
																											Fiddle_Yard_Stopped(ASL);
																											ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																											break;
																										} 
																										ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																										ACT_ST_MCHN[ASL].Fy_Running_2 = Train_On_5B_Start;
																										ACT_ST_MCHN[ASL].Collect = Off;
																										break;
																				case	Busy		:	break;
																				default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																										ACT_ST_MCHN[ASL].Fy_Running_Old_2 = ACT_ST_MCHN[ASL].Fy_Running_2;
																										ACT_ST_MCHN[ASL].Fy_Running_2 = ERROR;
																										break;	//TERUG LATEN SCHREEUWEN wanneer error naar boven komt!!!!!!!!!!!!!!!!!!!
																			}
																			break;
																			
											case	ERROR				:	switch (ACT_ST_MCHN[ASL].FY_Running_Error)
																			{
																				case	0	:	ACT_ST_MCHN[ASL].Old_Track_2 = Track_Nr(ASL);
																								ACT_ST_MCHN[ASL].FY_Running_Error = 1;
																								ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																								Bezet_Weerstand(ASL, Off);
																								break;
																								
																				case	1	:	switch (Return_Val_Routine = Init_Fiddle_Yard(ASL,Off))
																								{
																									case	Finished	:	ACT_ST_MCHN[ASL].Fy_Init_Done = On;
																															if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard)
																															{
																																Fiddle_Yard_Stopped(ASL);
																																ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																																break;
																															}
																															ACT_ST_MCHN[ASL].FY_Running_Error = 2;				// bridges are open after init=ok
																															break;
																									case	Busy		:	break;
																									default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																															ACT_ST_MCHN[ASL].Fy_Running_2 = ERROR;
																															ACT_ST_MCHN[ASL].FY_Running_Error = 1;
																															ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																															break;
																								}
																								break;
																								
																				case	2	:	switch (Return_Val_Routine = Track_Mover(ASL,ACT_ST_MCHN[ASL].Old_Track_2))
																								{
																									case	Finished	:	ACT_ST_MCHN[ASL].FY_Running_Error = 0; 
																															ACT_ST_MCHN[ASL].Fy_Running_2 = ACT_ST_MCHN[ASL].Fy_Running_Old_2;
																															ACT_ST_MCHN[ASL].Train_Drive_Sequencer = ACT_ST_MCHN[ASL].Train_Drive_Sequencer_Old;
																															break;
																									case	Busy		:	break;
																									default				:	ERROR_Code_Report(ASL,Return_Val_Routine);
																															ACT_ST_MCHN[ASL].Fy_Running_2 = ERROR;
																															ACT_ST_MCHN[ASL].FY_Running_Error = 1;
																															ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
																															break;
																								}
																								break;
																																																												
																				default		:	break;
																				
																			}
																			break;
										}
										break;
										
		case	ERROR_Handler_Idle	:	if (Exe_Cmd_(ASL) == Restart_Previous_Command)	// for direct commands
										{
											Exe_Cmd_Resume(ASL,ACT_ST_MCHN[ASL].Execute_Command_Old);
											ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;
										}
										if (Exe_Cmd_(ASL) == Stop_Fiddle_Yard_Now)
										{
											Fiddle_Yard_Reset(ASL);
											ACT_ST_MCHN[ASL].State_Machine_Switch = Fy_Reset;
											break;
										}
										break;
		
		default						:	break;
		
	}
	
	Var_Out_Programm(ASL);
		
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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

void ERROR_Code_Report(unsigned char ASL, unsigned char Code)
{
	switch (Code)
	{
		////////Bridge Errors//////
		case	Bezet_Uit_Blok_6					:	Bezet_Uit_Blok_6_Send(ASL);						break;	//Bridge Error Code
		case	Sensor_F12							:	Sensor_F12_Send(ASL);							break;	//Bridge Error Code
		case	Bezet_Uit_Blok_6_AND_Sensor_F12		:	Bezet_Uit_Blok_6_AND_Sensor_F12_Send(ASL);		break;	//Bridge Error Code
		case	CL_10_Heart_Sensor					:	CL_10_Heart_Sensor_Send(ASL);					break;	//Bridge Error Code
		case	Bridge_Open_Close_Timeout_Expired	:	Bridge_Open_Close_Timeout_Expired_Send(ASL);	break;	//Bridge Error Code
		
		////////Fiddle Yard move Errors//////
		case	BridgeMotorContact_10				:	BridgeMotorContact_10_Send(ASL);				break;
		case	Bridge_10L_Contact					:	Bridge_10L_Contact_Send(ASL);					break;
		case	Bridge_10R_Contact					:	Bridge_10R_Contact_Send(ASL);					break;
		case	BridgeMotorContact_11				:	BridgeMotorContact_11_Send(ASL);				break;
		case	EndOffStroke_11						:	EndOffStroke_11_Send(ASL);						break;
		case	Laatste_Spoor						:	Laatste_Spoor_Send(ASL);						break;
		case	EndOffStroke_10						:	EndOffStroke_10_Send(ASL);						break;
		default										:	Universal_Error_Send(ASL);						break;
	}
}

void PWM_Update(unsigned char ASL, unsigned int Value)
{
	switch (ASL)
	{
		case	TOP		:	SetDCPWM3(Value);
							break;
		case	BOTTOM	:	SetDCPWM1(Value);
							break;
		default			:	break;
	}	
}

void Pwm_Brake(unsigned char ASL, unsigned char Value)
{
	switch (ASL)
	{
		case	TOP		:	Pwm_Brake_TOP =! Value;
							break;
		case	BOTTOM	:	Pwm_Brake_BOTTOM =! Value;
							break;	
		default			:	break;
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
unsigned char *Trains_On_Fiddle_Yard(unsigned char ASL)
{
	return (ACT_ST_MCHN[ASL].Train_In_Track); // return fiddle yard occupation status by sending array to var_out.c
}

///////////////////////////////////////INIT//////////////////////////////////////////////////////////////////////////////////////////////////////////////
static unsigned char Init_Fiddle_Yard(unsigned char ASL, unsigned char Train_Detection_Cmd)
{
	static char Return_Val = Busy, Return_Val_Routine = Busy;
	
	switch(ACT_ST_MCHN[ASL].Init_Fy)
	{
		case	0	:	Init_Started(ASL);
						Return_Val = Busy;
						if (BM_10(ASL) && !BM_11(ASL) && !Bridge_10R(ASL) && !Bridge_10L(ASL) && (Track_Nr(ASL) > 0) && !Bezet_Uit_7(ASL))
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
						
		case	2	:	if (Track_Nr(ASL) == 0 && BM_10(ASL))
						{
							ACT_ST_MCHN[ASL].Init_Fy = 3; // Not aligned, Move 1 track
							Return_Val = Busy;
							break;
						}
						if ((BM_11(ASL) && (Bridge_10R(ASL) || Bridge_10L(ASL)) && F10(ASL)) || Bezet_Uit_6(ASL) || Bezet_Uit_7(ASL))
						{
							ACT_ST_MCHN[ASL].Init_Fy = 4; // Trein uit/door laten rijden
							Return_Val = Busy;
							break;
						}
						if (BM_11(ASL) && (Bridge_10R(ASL) || Bridge_10L(ASL)) && !F10(ASL) && !F11(ASL) && !F12(ASL) && !Bezet_Uit_6(ASL) && !Bezet_Uit_7(ASL))
						{
							ACT_ST_MCHN[ASL].Init_Fy = 5; // Geen trein aanwezig wel bruggen dicht > bruggen open
							Return_Val = Busy;
							break;
						}
						if (!BM_10(ASL) && !BM_11(ASL))
						{
							ACT_ST_MCHN[ASL].Init_Fy = 5;
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
						if (BM_11(ASL) && (Bridge_10R(ASL) && Bridge_10L(ASL)) && !F10(ASL) && !F11(ASL) && !F12(ASL) && !Bezet_Uit_6(ASL) && !Bezet_Uit_7(ASL))
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
///////////////////////////////////////	///////////////////////////////////////	

static unsigned char Train_Drive_In(unsigned char ASL, unsigned char Collect_Full)
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
								Bezet_In_6(ASL, On);
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

static unsigned char Train_Drive_Out(unsigned char ASL)
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

///////////////////////////////////////	///////////////////////////////////////	

static unsigned char Train_Detection(unsigned char ASL)
{
	static char Return_Val = Busy, Return_Val_Routine = Busy;
	
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
						ACT_ST_MCHN[ASL].Train_Detector = 4;
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
///////////////////////////////////////	///////////////////////////////////////	
static unsigned char Track_Mover(unsigned char ASL, char New_Track)
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
						
		case	1	:	switch (Return_Val_Routine = Fiddle_Multiple_Right(ASL, New_Track))
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
								
		case	3	:	switch (Return_Val_Routine = Fiddle_Multiple_Left(ASL, New_Track))
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
///////////////////////////////////////	///////////////////////////////////////	
///////////////////////////////////////	///////////////////////////////////////	
///////////////////////////////////////	///////////////////////////////////////	
///////////////////////////////////////	///////////////////////////////////////	
///////////////////////////////////////	///////////////////////////////////////	
///////////////////////////////////////	///////////////////////////////////////	

static unsigned char Fiddle_Multiple_Right(unsigned char ASL, char New_Track_Muktiple_Right)	// 11 naar 1
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle)
	{
		case	0	:	if (!BM_10(ASL) || Bridge_10L(ASL) || Bridge_10R(ASL) || BM_11(ASL) || EOS_10(ASL) || (CL_10(ASL) == 0x2))
						{															// laatste spoor (uiteinde) 0x2//
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							if (!BM_10(ASL))
							{
								Return_Val = BridgeMotorContact_10;
							}
							else if ( Bridge_10L(ASL))
							{
								Return_Val = Bridge_10L_Contact;
							}
							else if (Bridge_10R(ASL))
							{
								Return_Val = Bridge_10R_Contact;
							}
							else if (BM_11(ASL))
							{
								Return_Val = BridgeMotorContact_11;
							}
							else if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
							else if ((CL_10(ASL) == 0x2))
							{
								Return_Val = Laatste_Spoor;
							}
							
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right = ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right_Init;	// reset pwm start waarde
						ACT_ST_MCHN[ASL].Fiddle = 1;
						break;		
		case	1	:	Return_Val = Busy;
						M10(ASL, On);
						Pwm_Brake(ASL, Off);
						PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right );	//start met pwm rustig naar rechts
						if (ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right <= ACT_ST_MCHN[ASL].Pwm_Multiple_Right)	//wanneer pwm is max naar rechts
						{
							ACT_ST_MCHN[ASL].Fiddle = 2;
						}
						if ( ACT_ST_MCHN[ASL].Pwm_Fast_Counter >= ACT_ST_MCHN[ASL].Pwm_Fast_Counter_Const)//wanneer x aantal keer loop in geweest
						{
							ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right--;	// verhoog de snelheid naar rechts
							ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right--;	// verhoog de snelheid naar rechts
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0; // loop herhaling resetten
						}
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter++;	// loop herhaling ophogen
						break;
		case	2	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, On);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter=0;
						ACT_ST_MCHN[ASL].Mech_Delay++;	
						if ((CL_10_Heart(ASL) == 0) && (ACT_ST_MCHN[ASL].Mech_Delay > ACT_ST_MCHN[ASL].Mech_Constant))	// vertraging voor check heart==0
						{	
							ACT_ST_MCHN[ASL].Fiddle = 3;
							ACT_ST_MCHN[ASL].Mech_Delay = 0;
						}
						break;						
		case	3	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						
						switch (ACT_ST_MCHN[ASL].Pwm_Reg_Switch)
						{
							case	0	:	if (ACT_ST_MCHN[!ASL].ADCconversion_Inuse == False)																	// Check if the OTHER (!ASL) is busy with an AD converion
											{
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = True;
												if (ASL == TOP)
												{
													ADCON0 = 0x09;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												else if (ASL == BOTTOM)
												{
													ADCON0 = 0x0D;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}																												// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 1;		
											}
											else{ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;}
											break;
											
							case	1	:	ADCON0bits.GO = 1;
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 2;
											break;
											
							case	2	:	if (ADCON0bits.GO == 0)
											{
												
												ACT_ST_MCHN[ASL].AdcResults = (((unsigned int)ADRESH)<<8)|(ADRESL);	
												
												if (ACT_ST_MCHN[ASL].AdcResults >= PWM_ADC_MULTIPLE_SPEED)																		// When move started, Pwm_One_Left=700 == AdcResult=400
												{
													ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right++;													
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right);
												}
												else if ((ACT_ST_MCHN[ASL].AdcResults <= PWM_ADC_MULTIPLE_SPEED_LOW) && (ACT_ST_MCHN[ASL].AdcResults > PWM_ADC_ENC_STALL))							// Hysteresis of 20
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right--;
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right);
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
												}
												else if (ACT_ST_MCHN[ASL].AdcResults < PWM_ADC_ENC_STALL)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter++;
												}
												if (ACT_ST_MCHN[ASL].Encoder_Lost_Counter > ENCODER_LOST_COUNTER_CONST)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Fiddle = 5;
													PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Right );
												}
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 3;
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
											}
											break;
											
							case	3	:	if (ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg >= PWM_RAMP_UP_DELAY_REG_CONST)
											{
												ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											}
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg++;
											break;
											
							default		:	ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 4;
						}
						
						Return_Val = Busy;
						if ((CL_10_Heart(ASL) == 1) && (Track_Nr(ASL) == ACT_ST_MCHN[ASL].Brake_Track))
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Fiddle = 4;
						}
						break;
						
		case	4	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
		
						switch (ACT_ST_MCHN[ASL].Pwm_Reg_Switch)
						{
							case	0	:	if (ACT_ST_MCHN[!ASL].ADCconversion_Inuse == False)																	// Check if the OTHER (!ASL) is busy with an AD converion
											{
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = True;
												if (ASL == TOP)
												{
													ADCON0 = 0x09;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												else if (ASL == BOTTOM)
												{
													ADCON0 = 0x0D;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}																												// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 1;		
											}
											else{ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;}
											break;
											
							case	1	:	ADCON0bits.GO = 1;
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 2;
											break;
											
							case	2	:	if (ADCON0bits.GO == 0)
											{
												ACT_ST_MCHN[ASL].AdcResults = (((unsigned int)ADRESH)<<8)|(ADRESL);
												
												if ((ACT_ST_MCHN[ASL].AdcResults >= PWM_ADC_SLOW))// && (ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right < 430))
												{
													ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right++;													
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right);
												}
												
												else if ((ACT_ST_MCHN[ASL].AdcResults <= PWM_ADC_SLOW_LOW) && (ACT_ST_MCHN[ASL].AdcResults > PWM_ADC_ENC_STALL))// lower limit dec=180 -> hex= 0xB4    80-0x50	40-0x28	20-0x14
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right--;
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right);
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
												}												
												else if (ACT_ST_MCHN[ASL].AdcResults < PWM_ADC_ENC_STALL)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter++;
												}
												if (ACT_ST_MCHN[ASL].Encoder_Lost_Counter > ENCODER_LOST_COUNTER_CONST)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Fiddle = 5;
													PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Right );
												}
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 3;
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
											}
											break;
											
							case	3	:	if (ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg >= PWM_RAMP_UP_MULTIPLE_DELAY_REG_CONST)
											{
												ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											}
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg++;
											break;
											
							default		:	ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											break;
						}
						
						ACT_ST_MCHN[ASL].Mech_Delay++;	
						if (ACT_ST_MCHN[ASL].Mech_Delay > MECH_SKIP_HEART)
						{
							ACT_ST_MCHN[ASL].Mech_Delay = MECH_SKIP_HEART;
							
							if (CL_10_Heart(ASL) == 1)
							{
								ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
								ACT_ST_MCHN[ASL].Mech_Delay = 0;
								ACT_ST_MCHN[ASL].Fiddle = 6;
								PWM_Update(ASL,  512 );
							}
						}
						Return_Val = Busy;
						break;
						
						/*if (ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right >= ACT_ST_MCHN[ASL].Pwm_Multiple_Slow_Right)	//wanneer pwm is afgeremd naar langzaam pwm
						{
							ACT_ST_MCHN[ASL].Fiddle = 5;
						}
						PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right );	//Update pwm met vorige waarde
					
						if ( ACT_ST_MCHN[ASL].Pwm_Fast_Counter >= ACT_ST_MCHN[ASL].Pwm_Fast_Counter_Const)//wanneer x aantal keer loop in geweest
						{
							ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right++;	// verlaag de snelheid naar rechts
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0; // loop herhaling resetten
						}
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter++;	// loop herhaling ophogen
						*/
						//break;
						
		case	5	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							ACT_ST_MCHN[ASL].Fiddle = 6;
							PWM_Update(ASL,  512 );
						}
						break;
		case	6	:	Return_Val = Busy;
						ACT_ST_MCHN[ASL].Mech_Delay++;
						if (ACT_ST_MCHN[ASL].Mech_Delay >= ACT_ST_MCHN[ASL].Mech_Rewind)	// extra vertraging voor de check te ver gereden
						{
							ACT_ST_MCHN[ASL].Fiddle = 7;
						}
						break;						
		case	7	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							Fiddle_Multiple_Right_Ok(ASL);
							Return_Val = Finished;
							break;
						}
						if (CL_10_Heart(ASL) == 0)
						{
							ACT_ST_MCHN[ASL].Fiddle = 8;
							PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Left );
						}
						break;
		case	8	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							ACT_ST_MCHN[ASL].Mech_Delay=0;
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							Fiddle_Multiple_Right_Ok(ASL);
							Return_Val = Finished;
							break;
						}
						break;
		
		default		:	ACT_ST_MCHN[ASL].Fiddle = 0;
						Return_Val = ERROR;
						break;
	}
	return (Return_Val);		
}

///////////////////////////////////////	///////////////////////////////////////	


static unsigned char Fiddle_One_Right(unsigned char ASL)	//rechts op bewegen 11 naar 1
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle)
	{
		case	0	:	if (!BM_10(ASL) || Bridge_10L(ASL) || Bridge_10R(ASL) || BM_11(ASL) || EOS_10(ASL) || (CL_10(ASL) == 0x2))// laatste spoor (uiteinde) 0x2//
						{															
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							if (!BM_10(ASL))
							{
								Return_Val = BridgeMotorContact_10;
							}
							else if ( Bridge_10L(ASL))
							{
								Return_Val = Bridge_10L_Contact;
							}
							else if (Bridge_10R(ASL))
							{
								Return_Val = Bridge_10R_Contact;
							}
							else if (BM_11(ASL))
							{
								Return_Val = BridgeMotorContact_11;
							}
							else if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
							else if ((CL_10(ASL) == 0x2))
							{
								Return_Val = Laatste_Spoor;
							}
							
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_One_Count_Right = ACT_ST_MCHN[ASL].Pwm_One_Count_Right_Init;	// reset pwm start waarde
						ACT_ST_MCHN[ASL].Fiddle = 1;
						break;		
		case	1	:	M10(ASL, On);
						Pwm_Brake(ASL, Off);
						PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Count_Right );	//start met pwm rustig naar rechts
						if (ACT_ST_MCHN[ASL].Pwm_One_Count_Right <= ACT_ST_MCHN[ASL].Pwm_One_Right)	//wanneer pwm is max naar rechts
						{
							ACT_ST_MCHN[ASL].Fiddle = 2;
						}
						if ( ACT_ST_MCHN[ASL].Pwm_Fast_Counter >= ACT_ST_MCHN[ASL].Pwm_Fast_Counter_Const)//wanneer x aantal keer loop in geweest
						{
							ACT_ST_MCHN[ASL].Pwm_One_Count_Right--;	// verhoog de snelheid naar rechts
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0; // loop herhaling resetten
						}
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter++;	// loop herhaling ophogen
						Return_Val = Busy;
						break;
		case	2	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, On);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter=0;
						ACT_ST_MCHN[ASL].Mech_Delay++;	
						if ((CL_10_Heart(ASL) == 0) && (ACT_ST_MCHN[ASL].Mech_Delay > ACT_ST_MCHN[ASL].Mech_Constant))	// vertraging voor check heart==0
						{	
							ACT_ST_MCHN[ASL].Fiddle = 3;
							ACT_ST_MCHN[ASL].Mech_Delay = 0;
						}
						break;
						
		case	3	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						
						switch (ACT_ST_MCHN[ASL].Pwm_Reg_Switch)
						{
							case	0	:	if (ACT_ST_MCHN[!ASL].ADCconversion_Inuse == False)																	// Check if the OTHER (!ASL) is busy with an AD converion
											{
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = True;
												if (ASL == TOP)
												{
													ADCON0 = 0x09;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												else if (ASL == BOTTOM)
												{
													ADCON0 = 0x0D;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}																												// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 1;		
											}
											else{ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;}
											break;
											
							case	1	:	ADCON0bits.GO = 1;
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 2;
											break;
											
							case	2	:	if (ADCON0bits.GO == 0)
											{
												ACT_ST_MCHN[ASL].AdcResults = (((unsigned int)ADRESH)<<8)|(ADRESL);	
												
												if (ACT_ST_MCHN[ASL].AdcResults >= PWM_ADC_ONE_SPEED)																		// When move started, Pwm_One_Left=700 == AdcResult=400
												{
													ACT_ST_MCHN[ASL].Pwm_One_Count_Right++;													
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_One_Count_Right);
												}
												else if ((ACT_ST_MCHN[ASL].AdcResults <= PWM_ADC_ONE_SPEED_LOW) && (ACT_ST_MCHN[ASL].AdcResults > PWM_ADC_ENC_STALL))							// Hysteresis of 20
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Pwm_One_Count_Right--;
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_One_Count_Right);
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
												}
												else if (ACT_ST_MCHN[ASL].AdcResults < PWM_ADC_ENC_STALL)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter++;
												}
												if (ACT_ST_MCHN[ASL].Encoder_Lost_Counter > ENCODER_LOST_COUNTER_CONST)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Fiddle = 5;
													PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Right );
												}
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 3;
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
											}
											break;
											
							case	3	:	if (ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg >= PWM_RAMP_UP_DELAY_REG_CONST)
											{
												ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											}
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg++;
											break;
											
							default		:	ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 4;
						}
						
						if (ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter > ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter_Const)
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter = 0;
							ACT_ST_MCHN[ASL].Fiddle = 4;
							break;
						}
						if (CL_10_Heart(ASL) == 1)
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Fiddle = 6;
							PWM_Update(ASL,  512 );
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter++;
						break;
						
		case	4	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						
						switch (ACT_ST_MCHN[ASL].Pwm_Reg_Switch)
						{
							case	0	:	if (ACT_ST_MCHN[!ASL].ADCconversion_Inuse == False)																	// Check if the OTHER (!ASL) is busy with an AD converion
											{
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = True;
												if (ASL == TOP)
												{
													ADCON0 = 0x09;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												else if (ASL == BOTTOM)
												{
													ADCON0 = 0x0D;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}																												// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 1;		
											}
											else{ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;}
											break;
											
							case	1	:	ADCON0bits.GO = 1;
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 2;
											break;
											
							case	2	:	if (ADCON0bits.GO == 0)
											{
												ACT_ST_MCHN[ASL].AdcResults = (((unsigned int)ADRESH)<<8)|(ADRESL);												// back to dec=200 -> hex= 0xC8   100-0x64	50-0x32	25-0x19
												
												if (ACT_ST_MCHN[ASL].AdcResults >= PWM_ADC_SLOW)
												{
													ACT_ST_MCHN[ASL].Pwm_One_Count_Right++;													
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_One_Count_Right);
												}
												else if ((ACT_ST_MCHN[ASL].AdcResults <= PWM_ADC_SLOW_LOW) && (ACT_ST_MCHN[ASL].AdcResults > PWM_ADC_ENC_STALL))							// lower limit dec=180 -> hex= 0xB4    80-0x50	40-0x28	20-0x14
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Pwm_One_Count_Right--;
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_One_Count_Right);
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
												}
												else if (ACT_ST_MCHN[ASL].AdcResults < PWM_ADC_ENC_STALL)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter++;
												}
												if (ACT_ST_MCHN[ASL].Encoder_Lost_Counter > ENCODER_LOST_COUNTER_CONST)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Fiddle = 5;
													PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Right );
												}
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 3;
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
											}
											break;
											
							case	3	:	if (ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg >= PWM_RAMP_UP_DELAY_REG_CONST)
											{
												ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											}
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg++;
											break;
											
							default		:	ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											break;
						}
						
						
						if (CL_10_Heart(ASL) == 1)
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0;
							ACT_ST_MCHN[ASL].Fiddle = 6;
							PWM_Update(ASL,  512 );
						}
						Return_Val = Busy;
						break;
												
		case	5	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							ACT_ST_MCHN[ASL].Fiddle = 6;
							PWM_Update(ASL,  512 );
						}
						break;
		case	6	:	ACT_ST_MCHN[ASL].Mech_Delay++;
						if (ACT_ST_MCHN[ASL].Mech_Delay >= ACT_ST_MCHN[ASL].Mech_Rewind)	// extra vertraging voor de check te ver gereden
						{
							ACT_ST_MCHN[ASL].Fiddle = 7;
						}
						Return_Val = Busy;
						break;						
		case	7	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							Fiddle_One_Right_Ok(ASL);
							Return_Val = Finished;
							break;
						}
						if (CL_10_Heart(ASL) == 0)
						{
							ACT_ST_MCHN[ASL].Fiddle = 8;
							PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Left );
						}
						break;
		case	8	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							ACT_ST_MCHN[ASL].Mech_Delay=0;
							ACT_ST_MCHN[ASL].Pwm_One_Count_Right = ACT_ST_MCHN[ASL].Pwm_One_Count_Right_Init;	// reset pwm start waarde
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							Fiddle_One_Right_Ok(ASL);
							Return_Val = Finished;
							break;
						}
						break;
																		
		default		:	ACT_ST_MCHN[ASL].Fiddle = 0;
						Return_Val = ERROR;
						break;
	}
	return (Return_Val);	
}
///////////////////////////////////////	///////////////////////////////////////	
///////////////////////////////////////	///////////////////////////////////////	
///////////////////////////////////////	///////////////////////////////////////	

static unsigned char Fiddle_Multiple_Left(unsigned char ASL, char New_Track_Muktiple_Left)	// 1 naar 11
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle)
	{
		case	0	:	if (!BM_10(ASL) || Bridge_10L(ASL) || Bridge_10R(ASL) || BM_11(ASL) || EOS_11(ASL) || (CL_10(ASL) == 0xD))
						{															// laatste spoor (uiteinde) 0xD//
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							if (!BM_10(ASL))
							{
								Return_Val = BridgeMotorContact_10;
							}
							else if ( Bridge_10L(ASL))
							{
								Return_Val = Bridge_10L_Contact;
							}
							else if (Bridge_10R(ASL))
							{
								Return_Val = Bridge_10R_Contact;
							}
							else if (BM_11(ASL))
							{
								Return_Val = BridgeMotorContact_11;
							}
							else if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
							else if ((CL_10(ASL) == 0xD))
							{
								Return_Val = Laatste_Spoor;
							}
							
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left = ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left_Init;	// reset pwm start waarde
						ACT_ST_MCHN[ASL].Fiddle = 1;
						break;		
		case	1	:	Return_Val = Busy;
						M10(ASL, On);
						Pwm_Brake(ASL, Off);
						PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left );	//start met pwm rustig naar rechts
						if (ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left >= ACT_ST_MCHN[ASL].Pwm_Multiple_Left)	//wanneer pwm is max naar rechts
						{
							ACT_ST_MCHN[ASL].Fiddle = 2;
						}
						if ( ACT_ST_MCHN[ASL].Pwm_Fast_Counter >= ACT_ST_MCHN[ASL].Pwm_Fast_Counter_Const)//wanneer x aantal keer loop in geweest
						{
							ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left++;	// verhoog de snelheid naar rechts
							ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left++;	// verhoog de snelheid naar rechts
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0; // loop herhaling resetten
						}
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter++;	// loop herhaling ophogen
						break;
		case	2	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter=0;
						ACT_ST_MCHN[ASL].Mech_Delay++;	
						if ((CL_10_Heart(ASL) == 0) && (ACT_ST_MCHN[ASL].Mech_Delay > ACT_ST_MCHN[ASL].Mech_Constant))	// delay check heart==0
						{	
							ACT_ST_MCHN[ASL].Fiddle = 3;
							ACT_ST_MCHN[ASL].Mech_Delay = 0;
						}
						break;						
		case	3	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						
						switch (ACT_ST_MCHN[ASL].Pwm_Reg_Switch)
						{
							case	0	:	if (ACT_ST_MCHN[!ASL].ADCconversion_Inuse == False)																	// Check if the OTHER (!ASL) is busy with an AD converion
											{
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = True;
												if (ASL == TOP)
												{
													ADCON0 = 0x09;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												else if (ASL == BOTTOM)
												{
													ADCON0 = 0x0D;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 1;		
											}
											else{ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;}
											break;
											
							case	1	:	ADCON0bits.GO = 1;
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 2;
											break;
											
							case	2	:	if (ADCON0bits.GO == 0)
											{
											
												
												ACT_ST_MCHN[ASL].AdcResults = (((unsigned int)ADRESH)<<8)|(ADRESL);	
												
												if (ACT_ST_MCHN[ASL].AdcResults >= PWM_ADC_MULTIPLE_SPEED)																			// When move started, Pwm_One_Left=700 == AdcResult=400
												{
													ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left--;													
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left);
												}
												else if ((ACT_ST_MCHN[ASL].AdcResults <= PWM_ADC_MULTIPLE_SPEED_LOW) && (ACT_ST_MCHN[ASL].AdcResults > PWM_ADC_ENC_STALL))							// Hysteresis of 20
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left++;
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left);
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
												}
												else if (ACT_ST_MCHN[ASL].AdcResults < PWM_ADC_ENC_STALL)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter++;
												}
												if (ACT_ST_MCHN[ASL].Encoder_Lost_Counter > ENCODER_LOST_COUNTER_CONST)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Fiddle = 5;
													PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Left );
												}
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 3;
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
											}
											break;
											
							case	3	:	if (ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg >= PWM_RAMP_UP_DELAY_REG_CONST)
											{
												ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											}
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg++;
											break;
											
							default		:	ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 4;
						}
						
						if ((CL_10_Heart(ASL) == 1) && (Track_Nr(ASL) == ACT_ST_MCHN[ASL].Brake_Track))	// wait for 1 track before the set track
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Fiddle = 4;
							
						}
						Return_Val = Busy;
						break;
						
		case	4	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
		
						switch (ACT_ST_MCHN[ASL].Pwm_Reg_Switch)
						{
							case	0	:	if (ACT_ST_MCHN[!ASL].ADCconversion_Inuse == False)																	// Check if the OTHER (!ASL) is busy with an AD converion
											{
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = True;
												if (ASL == TOP)
												{
													ADCON0 = 0x09;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												else if (ASL == BOTTOM)
												{
													ADCON0 = 0x0D;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}																												// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 1;		
											}
											else{ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;}
											break;
											
							case	1	:	ADCON0bits.GO = 1;
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 2;
											break;
											
							case	2	:	if (ADCON0bits.GO == 0)
											{
																								
												ACT_ST_MCHN[ASL].AdcResults = (((unsigned int)ADRESH)<<8)|(ADRESL);												// back to dec=200 -> hex= 0xC8   100-0x64	50-0x32	25-0x19
												
												if (ACT_ST_MCHN[ASL].AdcResults >= PWM_ADC_SLOW)
												{
													//ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left--;	
													ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left--;												
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left);
												}
												else if ((ACT_ST_MCHN[ASL].AdcResults <= PWM_ADC_SLOW_LOW) && (ACT_ST_MCHN[ASL].AdcResults > PWM_ADC_ENC_STALL))							// lower limit dec=180 -> hex= 0xB4    80-0x50	40-0x28	20-0x14
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													//ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left++;
													ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left++;
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left);
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
												}
												else if (ACT_ST_MCHN[ASL].AdcResults < PWM_ADC_ENC_STALL)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter++;
												}
												if (ACT_ST_MCHN[ASL].Encoder_Lost_Counter > ENCODER_LOST_COUNTER_CONST)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Fiddle = 5;
													PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Left );
												}												
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 3;
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
											}
											break;
											
							case	3	:	if (ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg >= PWM_RAMP_UP_MULTIPLE_DELAY_REG_CONST)
											{
												ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											}
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg++;
											break;
											
							default		:	ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 4;
						}
						
						ACT_ST_MCHN[ASL].Mech_Delay++;	
						if (ACT_ST_MCHN[ASL].Mech_Delay > MECH_SKIP_HEART)
						{
							ACT_ST_MCHN[ASL].Mech_Delay = MECH_SKIP_HEART;
							
							if (CL_10_Heart(ASL) == 1)
							{
								ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
								ACT_ST_MCHN[ASL].Mech_Delay = 0;
								ACT_ST_MCHN[ASL].Fiddle = 6;
								PWM_Update(ASL,  512 );
							}
						}
						Return_Val = Busy;
						break;
						
						/*if (ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left <= ACT_ST_MCHN[ASL].Pwm_Multiple_Slow_Left)	//wanneer pwm is afgeremd naar langzaam pwm
						{
							ACT_ST_MCHN[ASL].Fiddle = 5;
						}
						PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left );	//Update pwm met vorige waarde
					
						if ( ACT_ST_MCHN[ASL].Pwm_Fast_Counter >= ACT_ST_MCHN[ASL].Pwm_Fast_Counter_Const)//wanneer x aantal keer loop in geweest
						{
							ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left--;	// verlaag de snelheid naar rechts
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0; // loop herhaling resetten
						}
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter++;	// loop herhaling ophogen
						*/
						//break;						
		
		case	5	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							ACT_ST_MCHN[ASL].Fiddle = 6;
							PWM_Update(ASL,  512 );
						}
						break;
		case	6	:	Return_Val = Busy;
						ACT_ST_MCHN[ASL].Mech_Delay++;
						if (ACT_ST_MCHN[ASL].Mech_Delay >= ACT_ST_MCHN[ASL].Mech_Rewind)	// extra vertraging voor de check te ver gereden
						{
							ACT_ST_MCHN[ASL].Fiddle = 7;
						}
						break;						
		case	7	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							Fiddle_Multiple_Left_Ok(ASL);
							Return_Val = Finished;
							break;
						}
						if (CL_10_Heart(ASL) == 0)
						{
							ACT_ST_MCHN[ASL].Fiddle = 8;
							PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Right );
						}
						break;
		case	8	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						Return_Val = Busy;
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							ACT_ST_MCHN[ASL].Mech_Delay=0;
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							Fiddle_Multiple_Left_Ok(ASL);
							Return_Val = Finished;
							break;
						}
						break;
		
		default		:	ACT_ST_MCHN[ASL].Fiddle = 0;
						Return_Val = ERROR;
						break;
	}
	return (Return_Val);		
}

///////////////////////////////////////////////////////////////////////////////////

static unsigned char Fiddle_One_Left(unsigned char ASL)	//Move from 1 to 11
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle)
	{
		case	0	:	if (!BM_10(ASL) || Bridge_10L(ASL) || Bridge_10R(ASL) || BM_11(ASL) || EOS_11(ASL) || (CL_10(ASL) == 0xD)) // 0xD = spoor 11
						{
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							if (!BM_10(ASL))
							{
								Return_Val = BridgeMotorContact_10;
							}
							else if ( Bridge_10L(ASL))
							{
								Return_Val = Bridge_10L_Contact;
							}
							else if (Bridge_10R(ASL))
							{
								Return_Val = Bridge_10R_Contact;
							}
							else if (BM_11(ASL))
							{
								Return_Val = BridgeMotorContact_11;
							}
							else if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
							else if ((CL_10(ASL) == 0xD))
							{
								Return_Val = Laatste_Spoor;
							}
							
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_One_Count_Left = ACT_ST_MCHN[ASL].Pwm_One_Count_Left_Init;
						ACT_ST_MCHN[ASL].Fiddle = 1;
						break;		
		case	1	:	M10(ASL, On);																														// Start Fiddle Yard move to next track
						Pwm_Brake(ASL, Off);
						PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Count_Left );
					
						if (ACT_ST_MCHN[ASL].Pwm_One_Count_Left >= ACT_ST_MCHN[ASL].Pwm_One_Left)
						{
							ACT_ST_MCHN[ASL].Fiddle = 2;
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0;
							break;
						}
						if ( ACT_ST_MCHN[ASL].Pwm_Fast_Counter >= ACT_ST_MCHN[ASL].Pwm_Fast_Counter_Const)
						{
							ACT_ST_MCHN[ASL].Pwm_One_Count_Left++;
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0;
						}
						ACT_ST_MCHN[ASL].Pwm_Fast_Counter++;
						
						Return_Val = Busy;
						break;
		case	2	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						ACT_ST_MCHN[ASL].Mech_Delay++;
						if ((CL_10_Heart(ASL) == 0) && (ACT_ST_MCHN[ASL].Mech_Delay > ACT_ST_MCHN[ASL].Mech_Constant))										// check if fiddle yard has left track
						{
							ACT_ST_MCHN[ASL].Fiddle = 3;
							ACT_ST_MCHN[ASL].Mech_Delay = 0;
						}
						Return_Val = Busy;
						break;
						
		case	3	:	if (EOS_11(ASL))																													// Keep the Fiddle Yard speed constant during "Inbetween_Counter"
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						
						switch (ACT_ST_MCHN[ASL].Pwm_Reg_Switch)
						{
							case	0	:	if (ACT_ST_MCHN[!ASL].ADCconversion_Inuse == False)																	// Check if the OTHER (!ASL) is busy with an AD converion
											{
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = True;
												if (ASL == TOP)
												{
													ADCON0 = 0x09;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												else if (ASL == BOTTOM)
												{
													ADCON0 = 0x0D;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}																												// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 1;		
											}
											else{ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;}
											break;
											
							case	1	:	ADCON0bits.GO = 1;
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 2;
											break;
											
							case	2	:	if (ADCON0bits.GO == 0)
											{
												ACT_ST_MCHN[ASL].AdcResults = (((unsigned int)ADRESH)<<8)|(ADRESL);	
												
												if (ACT_ST_MCHN[ASL].AdcResults >= PWM_ADC_ONE_SPEED)																		// When move started, Pwm_One_Left=700 == AdcResult=400
												{
													ACT_ST_MCHN[ASL].Pwm_One_Count_Left--;													
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_One_Count_Left);
												}
												else if ((ACT_ST_MCHN[ASL].AdcResults <= PWM_ADC_ONE_SPEED_LOW) && (ACT_ST_MCHN[ASL].AdcResults > PWM_ADC_ENC_STALL))							// Hysteresis of 20
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Pwm_One_Count_Left++;
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_One_Count_Left);
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
												}
												else if (ACT_ST_MCHN[ASL].AdcResults < PWM_ADC_ENC_STALL)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter++;
												}
												if (ACT_ST_MCHN[ASL].Encoder_Lost_Counter > ENCODER_LOST_COUNTER_CONST)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Fiddle = 5;
													PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Left );
												}
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 3;
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
											}
											break;
											
							case	3	:	if (ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg >= PWM_RAMP_UP_DELAY_REG_CONST)
											{
												ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											}
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg++;
											break;
											
							default		:	ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 4;
						}
						
						if (ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter > ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter_Const)									// When counter is finished ramp down starts in Fiddle=4
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter = 0;
							ACT_ST_MCHN[ASL].Fiddle = 4;
							break;
						}
						if (CL_10_Heart(ASL) == 1)
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Fiddle = 5;	// stop Fiddle Yard
							PWM_Update(ASL,  512 );
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_One_Inbetween_Counter++;
						break;
						
		case	4	:	if (EOS_11(ASL))																													// Ramp down of Fiddle Yard
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
							
						switch (ACT_ST_MCHN[ASL].Pwm_Reg_Switch)
						{
							case	0	:	if (ACT_ST_MCHN[!ASL].ADCconversion_Inuse == False)																	// Check if the OTHER (!ASL) is busy with an AD converion
											{
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = True;
												if (ASL == TOP)
												{
													ADCON0 = 0x09;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}
												else if (ASL == BOTTOM)
												{
													ADCON0 = 0x0D;																								// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												}																												// select analog channel and set AD on.(needs to be done every time before converting channel between switching from top to bottom)
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 1;		
											}
											else{ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;}
											break;
											
							case	1	:	ADCON0bits.GO = 1;
											ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 2;
											break;
											
							case	2	:	if (ADCON0bits.GO == 0)
											{
												ACT_ST_MCHN[ASL].AdcResults = (((unsigned int)ADRESH)<<8)|(ADRESL);											// back to dec=200 -> hex= 0xC8   100-0x64	50-0x32	25-0x19
												
												if (ACT_ST_MCHN[ASL].AdcResults >= PWM_ADC_SLOW)
												{
													ACT_ST_MCHN[ASL].Pwm_One_Count_Left--;													
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_One_Count_Left);
												}
												else if ((ACT_ST_MCHN[ASL].AdcResults <= PWM_ADC_SLOW_LOW) && (ACT_ST_MCHN[ASL].AdcResults > PWM_ADC_ENC_STALL))							// lower limit dec=180 -> hex= 0xB4    80-0x50	40-0x28	20-0x14
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Pwm_One_Count_Left++;
													PWM_Update(ASL, ACT_ST_MCHN[ASL].Pwm_One_Count_Left);
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
												}
												else if (ACT_ST_MCHN[ASL].AdcResults < PWM_ADC_ENC_STALL)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter++;
												}
												if (ACT_ST_MCHN[ASL].Encoder_Lost_Counter > ENCODER_LOST_COUNTER_CONST)
												{
													ACT_ST_MCHN[ASL].Encoder_Lost_Counter = 0;
													ACT_ST_MCHN[ASL].Fiddle = 5;
													PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Left );
												}												
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 3;
												ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
											}
											break;
											
							case	3	:	if (ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg >= PWM_RAMP_UP_DELAY_REG_CONST)
											{
												ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg = 0;
												ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 0;
											}
											ACT_ST_MCHN[ASL].Pwm_Ramp_up_delay_Reg++;
											break;
											
							default		:	ACT_ST_MCHN[ASL].Pwm_Reg_Switch = 4;
						}
						
						
						if (CL_10_Heart(ASL) == 1)
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0;
							ACT_ST_MCHN[ASL].Fiddle = 6;
							PWM_Update(ASL,  512 );
						}
						Return_Val = Busy;
						break;
												
		case	5	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Fiddle = 6;
							PWM_Update(ASL,  512 );
						}
						Return_Val = Busy;
						break;
		case	6	:	ACT_ST_MCHN[ASL].Mech_Delay++;
						if (ACT_ST_MCHN[ASL].Mech_Delay >= ACT_ST_MCHN[ASL].Mech_Rewind)
						{
							ACT_ST_MCHN[ASL].Fiddle = 7;
						}
						Return_Val = Busy;
						break;
		case	7	:	if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							Fiddle_One_Left_Ok(ASL);
							Return_Val = Finished;
							break;
						}
						if (CL_10_Heart(ASL) == 0)
						{
							M10(ASL, On);
							ACT_ST_MCHN[ASL].Fiddle = 8;
							PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Right );
						}
						Return_Val = Busy;
						break;
		case	8	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							break;
						}
						if (CL_10_Heart(ASL) == 1)
						{
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle = 0;
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							ACT_ST_MCHN[ASL].Mech_Delay=0;
							ACT_ST_MCHN[ASL].Pwm_One_Count_Left = ACT_ST_MCHN[ASL].Pwm_One_Count_Left_Init;
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							Fiddle_One_Left_Ok(ASL);
							Return_Val = Finished;
							break;
						}
						Return_Val = Busy;
						break;
						
		default		:	ACT_ST_MCHN[ASL].Fiddle = 0;
						Return_Val = ERROR;
						break;
	}
	return (Return_Val);
}

///////////////////////////////////////	///////////////////////////////////////	

static unsigned char Bridge_Open(unsigned char ASL)
{
	static char Return_Val = Busy;
	static char send_top, send_bottom;
	
	switch(ACT_ST_MCHN[ASL].Bridge_Open)
	{
		case	0		:	Bridge_Opening(ASL);
							if (Bezet_Uit_6(ASL) || F12(ASL) )
							{
								ACT_ST_MCHN[ASL].Bridge_Open = 0;
								M11(ASL, Off);
								if (Bezet_Uit_6(ASL) && F12(ASL))
								{
									Return_Val = Bezet_Uit_Blok_6_AND_Sensor_F12;
								}
								else if ( F12(ASL))
								{
									Return_Val = Sensor_F12;
								}
								else if (Bezet_Uit_6(ASL))
								{
									Return_Val = Bezet_Uit_Blok_6;
								}
								break;
							}
							Return_Val = Busy;
							ACT_ST_MCHN[ASL].Bridge_Open = 1;
							break;
								
		case	1		:	M11(ASL, On);
							Return_Val = Busy;
							ACT_ST_MCHN[ASL].Bridge_Open = 2;
							break;
							
		case	2		:	ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter++;
							if (ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter >= Bridge_Open_Close_Timeout)
							{
								M11(ASL, Off);
								Return_Val = Bridge_Open_Close_Timeout_Expired;	
								ACT_ST_MCHN[ASL].Bridge_Open = 0;
								ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter = 0;
								break;
							}	
							if (BM_10(ASL) && !Bridge_10R(ASL) && !Bridge_10L(ASL))
							{
								M11(ASL, Off);
								ACT_ST_MCHN[ASL].Bridge_Open = 0;
								Bridge_Open_Ok(ASL);
								Return_Val = Finished;
								ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter = 0;
								break;
							}
							Return_Val = Busy;
							break;
	
		default			:	ACT_ST_MCHN[ASL].Bridge_Open = 0;
							Return_Val = ERROR;
							break;
	}
	return (Return_Val);
}

static unsigned char Bridge_Close(unsigned char ASL)
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Bridge_Close)
	{
		case	0	:	Bridge_Closing(ASL);
												
						if (Bezet_Uit_6(ASL) || F12(ASL) || !CL_10_Heart(ASL))
						{
							ACT_ST_MCHN[ASL].Bridge_Close = 0;
							M11(ASL, Off);
							if (Bezet_Uit_6(ASL) && F12(ASL))
							{
								Return_Val = Bezet_Uit_Blok_6_AND_Sensor_F12;
							}
							else if ( F12(ASL))
							{
								Return_Val = Sensor_F12;
							}
							else if (Bezet_Uit_6(ASL))
							{
								Return_Val = Bezet_Uit_Blok_6;
							}
							else if (Track_Nr(ASL) == 0)
							{
								Return_Val = CL_10_Heart_Sensor;
							}
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Bridge_Close = 1;
						break;
								
		case	1	:	M11(ASL, On);
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Bridge_Close = 2;
						break;
						
		case	2	:	ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter++;
						if (ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter >= Bridge_Open_Close_Timeout)
						{
							M11(ASL, Off);
							Return_Val = Bridge_Open_Close_Timeout_Expired;	
							ACT_ST_MCHN[ASL].Bridge_Close = 0;
							ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter = 0;
							break;
						}	
						if (BM_11(ASL) &&  Bridge_10R(ASL) && Bridge_10L(ASL))
						{
							M11(ASL, Off);
							ACT_ST_MCHN[ASL].Bridge_Close = 0;
							Bridge_Close_Ok(ASL);
							Return_Val = Finished;
							ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter = 0;
							break;
						}
						Return_Val = Busy;
						break;
						
		default		:	ACT_ST_MCHN[ASL].Bridge_Close = 0;
						Return_Val = ERROR;
						break;
	}
	return (Return_Val);
}

//

