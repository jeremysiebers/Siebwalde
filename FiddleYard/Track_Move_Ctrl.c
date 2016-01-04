#include <Track_Move_Ctrl.h>
#include <Shift_Register.h>
#include <Fiddle_Yard.h>
#include <Mip50_API.h>


////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0

typedef struct
{
	unsigned char 			Fiddle_Track_Mover,                     			// Switch for Fiddle direction movement
							Old_Track,                          				// Previous track for track mover routine
                            FY_Homed,                                           // FiddleYard MIP50 homed variable
                            Return_Val_Routine;                                 // Return value of sub routine switch
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,False,0},                            // is 0 is BOTTOM
											 {0,0,False,0}};                           // is 1 is TOP

static rom int Track[12] = {0, 0, 42800, 85600, 128400, 171200, 214000, 256800, 299600, 342400, 385200, 428000};   // New track coordinates


void Track_Move_Ctrl_Reset(unsigned char ASL)
{
	ACT_ST_MCHN[ASL].Fiddle_Track_Mover=0;										// Switch for Fiddle direction movement
	ACT_ST_MCHN[ASL].Old_Track=0;												// Previous track	
    ACT_ST_MCHN[ASL].FY_Homed = False;                                          // Set Homed to False after reset (force new home after reset)
    ACT_ST_MCHN[ASL].Return_Val_Routine = Busy;                                 // Set sub routine value to Busy after reset
}
    
unsigned char Track_Mover(unsigned char ASL, char New_Track)
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle_Track_Mover)
	{
		case	0	:	Return_Val = Busy;
						if (Track_Nr(ASL) == 0 || ACT_ST_MCHN[ASL].FY_Homed == False || New_Track == 0)  // When Homing is required go to homing routine or when track is not aligned or not homed
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 1;
							break;
						}
        
						ACT_ST_MCHN[ASL].Old_Track = Track_Nr(ASL);             // Store current track into memory
						
						if (New_Track == ACT_ST_MCHN[ASL].Old_Track)            // When same track is commanded as current track --> NOP
						{
							ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
							Return_Val = Finished;
							break;
						}
                        
                        ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 2;                //Move Fidle Yard to requested track
                        
						break;
						
		case	1	:	switch(ACT_ST_MCHN[ASL].Return_Val_Routine = MIP50xHOME(ASL))
                        {	
                            case	Finished	:	ACT_ST_MCHN[ASL].FY_Homed = True;
                                                    Return_Val = Finished;
                                                    break;
                            case	Busy		:	break;
                            default				:	break;
                        }						
						break;
                        
        case	2	:	switch(ACT_ST_MCHN[ASL].Return_Val_Routine = MIP50xMOVE(ASL,Track[New_Track]))
                        {	
                            case	Finished	:	Return_Val = Finished;
                                                    break;
                            case	Busy		:	break;
                            default				:	break;
                        }						
						break;
                        
		default		:	ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 0;
						Return_Val = ERROR;
						break;	
	}
	return (Return_Val);
}

/*
    printf("E1xG");
    printf("C1x2r2t0.2G");
    printf("V1x100G");
    printf("n1xG");
    printf("H1xG");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("R1x42800G");
    printf("A1x0G");
    printf("f1xG");
 */