#include <State_Machine.h>
#include <Shift_Register.h>
#include <Command_Machine.h>
#include <Var_Out.h>
#include <Fiddle_Yard.h>
#include <Mip50_API.h>

////////State_Machine_Switch////////
#define Fy_Reset 0
#define Idle 1

////////Exe_Cmd_()////////
#define Nopp 0
#define Assert_Track 1
#define Deassert_Track 2
#define MIP50_Enable 3
#define MIP50_Disable 4
#define Stop_Fiddle_Yard_Now 5     //(reset)
#define Bezet_In_5B_Switch_On 6
#define Bezet_In_5B_Switch_Off 7
#define Bezet_In_6_Switch_On 8
#define Bezet_In_6_Switch_Off 9
#define Bezet_In_7_Switch_On 10
#define Bezet_In_7_Switch_Off 11

typedef struct
{
	unsigned char			State_Machine_Switch,                               // State Machine Main Switch							
							Execute_Command;                                    // Used for executing commands when Idle, manipulate when resuming from error		
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{Fy_Reset,Nopp,},                  // is 0 is BOTTOM
											 {Fy_Reset,Nopp}};                  // is 1 is TOP
											 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void State_Machine_Reset(unsigned char ASL)
{   
    MIP50xAPIxRESET(ASL); 				
	Bezet_In_5B(ASL,On);
    Bezet_In_6(ASL,On);
    Bezet_In_7(ASL,On);
    Bezet_Weerstand(ASL,Off);
    Enable_Track(ASL,Off);																		// decouple track as default that no trains start running
    Exe_Cmd_Ret(ASL,0);                                                                         // Return with command execution ready internal                            
    ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;												// State Machine Main Switch   
    M10(ASL, Off);                                                                              // Disable MIP50    
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void State_Machine_Update(unsigned char ASL)	//ASL = Active_Struct_Level, BOTTOM(0) or TOP(1)
{		
	switch (ACT_ST_MCHN[ASL].State_Machine_Switch)
	{
		
		case Fy_Reset	:	MIP50xAPIxRESET(ASL); 				
							Bezet_In_5B(ASL,On);
							Bezet_In_6(ASL,On);
							Bezet_In_7(ASL,On);
							Bezet_Weerstand(ASL,Off);
                            Enable_Track(ASL,Off);																		// decouple track as default that no trains start running
                            Exe_Cmd_Ret(ASL,0);                                                                         // Return with command execution ready internal                            
							ACT_ST_MCHN[ASL].State_Machine_Switch = Idle;												// State Machine Main Switch   
                            M10(ASL, Off);                                                                              // Disable MIP50
							Target_Ready(ASL);    
							break;
                            
                            
                            
		
		case Idle	:		switch (ACT_ST_MCHN[ASL].Execute_Command = Exe_Cmd_(ASL))
							{
								case	Nopp					:	break; // no command received (No Opperation Pending ;-)
								
								case	Assert_Track			:	Enable_Track(ASL,On);
																	Bezet_Weerstand(ASL, Off);
																	Exe_Cmd_Ret(ASL,0);
																	Target_Ready(ASL);
																	break;
																	
								case	Deassert_Track			:	Enable_Track(ASL,Off);
																	Bezet_Weerstand(ASL, On);
																	Exe_Cmd_Ret(ASL,0);
																	Target_Ready(ASL);
																	break;
																	
                                case    MIP50_Enable            :   M10(ASL, On);
                                                                    Exe_Cmd_Ret(ASL,0);
                                                                    Target_Ready(ASL);
                                                                    break;
                                                                    
                                case    MIP50_Disable           :   M10(ASL, Off);
                                                                    Exe_Cmd_Ret(ASL,0);
                                                                    Target_Ready(ASL);
                                                                    break;
																	
								case	Stop_Fiddle_Yard_Now	:	State_Machine_Reset(ASL);
                                                                    Fiddle_Yard_Reset(ASL); // Let C# now Reset is executed
                                                                    Target_Ready(ASL);
																	break;
																	
								case 	Bezet_In_5B_Switch_On	:	Bezet_In_5B(ASL,On);
																	Exe_Cmd_Ret(ASL,0);
																	Target_Ready(ASL);
																	break;
																	
								case 	Bezet_In_5B_Switch_Off	:	Bezet_In_5B(ASL,Off);
																	Exe_Cmd_Ret(ASL,0);
																	Target_Ready(ASL);
																	break;
																	
								case 	Bezet_In_6_Switch_On	:	Bezet_In_6(ASL,On);
																	Exe_Cmd_Ret(ASL,0);
																	Target_Ready(ASL);
																	break;
																	
								case 	Bezet_In_6_Switch_Off	:	Bezet_In_6(ASL,Off);
																	Exe_Cmd_Ret(ASL,0);
																	Target_Ready(ASL);
																	break;
																	
								case 	Bezet_In_7_Switch_On	:	Bezet_In_7(ASL,On);
																	Exe_Cmd_Ret(ASL,0);
																	Target_Ready(ASL);
																	break;
																	
								case 	Bezet_In_7_Switch_Off	:	Bezet_In_7(ASL,Off);
																	Exe_Cmd_Ret(ASL,0);
																	Target_Ready(ASL);
																	break;

								default							:	break;	
							}	
							break;
		
		default						:	break;
		
	}	
	Var_Out_Programm(ASL);
}