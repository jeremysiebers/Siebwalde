#include <Track_Move_Ctrl.h>
#include <Shift_Register.h>
#include <Fiddle_Yard.h>
#include <Mip50_API.h>


////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0

////////Fiddle Yard move Errors//////
#define BridgeMotorContact_10 0x06
#define Bridge_10L_Contact 0x07
#define Bridge_10R_Contact 0x08
#define BridgeMotorContact_11 0x09
#define EndOffStroke_11 0x0A
#define Laatste_Spoor 0x0B
#define EndOffStroke_10 0x0C

typedef struct
{
	unsigned char 			Fiddle_Track_Mover,                     			// Switch for Fiddle direction movement
							Old_Track,                          				// Previous track for track mover routine
                            FY_Homed;                                           // FiddleYard MIP50 homed variable
    char                    Return_Val_Routine;                                 // Return value of sub routine switch
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,False,Busy},                            // is 0 is BOTTOM
											 {0,0,False,Busy}};                           // is 1 is TOP

static long int Track[12] = {0, 0, 42800, 85600, 128400, 171200, 214000, 256800, 299600, 342400, 385200, 428000};   // New track coordinates


void Track_Move_Ctrl_Reset(unsigned char ASL)
{
	ACT_ST_MCHN[ASL].Fiddle_Track_Mover=0;										// Switch for Fiddle direction movement
	ACT_ST_MCHN[ASL].Old_Track=0;												// Previous track	
    //M10(ASL, Off);                                                              // Set MIP50 Enable to off after reset    
    //MIP50xAPIxRESET(ASL);                                                       // Clear error on MIP during reset of ucontroller        }                                                          
    ACT_ST_MCHN[ASL].FY_Homed = False;                                          // Set Homed to False after reset (force new home after reset)
    ACT_ST_MCHN[ASL].Return_Val_Routine = Busy;                                 // Set sub routine value to Busy after reset    
}
    
unsigned char Track_Mover(unsigned char ASL, char New_Track)
{
	char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle_Track_Mover)
	{
		case	0	:	Return_Val = Busy;
						if (Track_Nr(ASL) == 0 || ACT_ST_MCHN[ASL].FY_Homed == False)  //If the track is not aligned OR FY not Homed
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 1;            // Start homing FY
							break;
						}
        
						ACT_ST_MCHN[ASL].Old_Track = Track_Nr(ASL);             // Store current track into memory
						
						if (New_Track == ACT_ST_MCHN[ASL].Old_Track)            // When same track is commanded as current track --> NOP
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
							Return_Val = Finished;
							break;
						}
                        
                        if (New_Track > 0 && New_Track < 12)                    // When a valid track is pointed
                        {
                            ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 2;                //Move Fidle Yard to requested track
                        } 
                        else
                        {
                            ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;            // Else return with finished
                            Return_Val = Finished;
                        }
						break;
						
		case	1	:	M10(ASL, On);                                                           // Enable MIP50, during home do not check EOS, allowing the MIP to home
                        switch(ACT_ST_MCHN[ASL].Return_Val_Routine = MIP50xHOME(ASL))
                        {	
                            case	Finished	:	ACT_ST_MCHN[ASL].FY_Homed = True;
                                                    ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 2;    // After homing go to the requested track
                                                    Return_Val = Busy;
                                                    break;
                            case	Busy		:	Return_Val = Busy;
                                                    break;
                            default				:	break;
                        }						
						break;
                        
        case	2	:	if (EOS_10(ASL))
						{
							Return_Val = EndOffStroke_10;							
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
							break;
						}
                        else if (EOS_11(ASL))
						{
							Return_Val = EndOffStroke_11;							
							M10(ASL, Off);
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
							break;
						}
                        switch(ACT_ST_MCHN[ASL].Return_Val_Routine = MIP50xMOVE(ASL,Track[New_Track]))
                        {	
                            case	Finished	:	Return_Val = Finished;
                                                    ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
                                                    break;
                            case	Busy		:	Return_Val = Busy;
                                                    break;
                            default				:	break;
                        }						
						break;
                        
		default		:	ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
						Return_Val = ERROR;
						break;	
	}
	return (Return_Val);
}