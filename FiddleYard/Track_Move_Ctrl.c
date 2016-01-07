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

#define TEMPOFFSET 700

typedef struct
{
	unsigned char 			Fiddle_Track_Mover,                     			// Switch for Fiddle direction movement
							Old_Track,                          				// Previous track for track mover routine
                            FY_Homed;                                           // FiddleYard MIP50 homed variable
    char                    Return_Val_Routine;                                 // Return value of sub routine switch
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,False,Busy},                            // is 0 is BOTTOM
											 {0,0,False,Busy}};                           // is 1 is TOP

static long int TrackForward[12] = {0, 0, 42800, 85600, 128400, 171200, 214000, 256800, 299600, 342400, 385200, 428000};   // New track coordinates forward movement 1 --> 11
static long int TrackBackwardOffset[12] = {0, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, TEMPOFFSET, 0};   // New track coordinates forward movement 11 --> 1 offset number

long int temp = 0;

void Track_Move_Ctrl_Reset(unsigned char ASL)
{
	ACT_ST_MCHN[ASL].Fiddle_Track_Mover=0;										// Switch for Fiddle direction movement
	ACT_ST_MCHN[ASL].Old_Track=0;												// Previous track	
    //M10(ASL, Off);                                                              // Set MIP50 Enable to off after reset    
    //MIP50xAPIxRESET(ASL);                                                       // Clear error on MIP during reset of ucontroller        }                                                          
    ACT_ST_MCHN[ASL].FY_Homed = False;                                          // Set Homed to False after reset (force new home after reset)
    ACT_ST_MCHN[ASL].Return_Val_Routine = Busy;                                 // Set sub routine value to Busy after reset    
}
  
void TrackxCountxNumber(unsigned char ASL, char New_Track)
{   
    long int Track_Forward = 0;
    long int Track_Backward_Offset = 0;
    
    if (New_Track > Track_Nr(ASL))                                              // When New Track is movement in forward direction
    {
        temp = TrackForward[New_Track];
    } 
    else if (New_Track < Track_Nr(ASL))                                         // When New Track is movement in forward direction
    {
        Track_Forward = TrackForward[New_Track];
        Track_Backward_Offset = TrackBackwardOffset[New_Track];
        temp = Track_Forward - Track_Backward_Offset;                           // Move further back --> sow number must be smaller
    }
    else                                                                        // When new rack is equal to current track pass the forward movement count number 
    {   
        temp = TrackForward[New_Track];                                   // When New Track is movement in forward direction
    }    
}


unsigned char Track_Mover(unsigned char ASL, char New_Track)
{
	char Return_Val = Busy;    
	
	switch(ACT_ST_MCHN[ASL].Fiddle_Track_Mover)
	{
		case	0	:	Return_Val = Busy;
						if (Track_Nr(ASL) == 0 || ACT_ST_MCHN[ASL].FY_Homed == False)  //If the track is not aligned OR FY not Homed
						{
                            Enable_Track(ASL,Off);                              // Disable FiddleYard moving track
							Bezet_Weerstand(ASL, On);                           // Enable occupied resistor
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
                        
                        if (New_Track > 0 && New_Track < 12 && New_Track != Track_Nr(ASL))  // When a valid track is pointed and not equal to current track
                        {
                            Enable_Track(ASL,Off);                              // Disable FiddleYard moving track
                            Bezet_Weerstand(ASL, On);                           // Enable occupied resistor
                            ACT_ST_MCHN[ASL].Fiddle_Track_Mover = 2;            //Move Fidle Yard to requested track
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
                        TrackxCountxNumber(ASL, New_Track);
                        switch(ACT_ST_MCHN[ASL].Return_Val_Routine = MIP50xMOVE(ASL,temp))
                        {	
                            case	Finished	:	Enable_Track(ASL,On);       // Disable FiddleYard moving track
                                                    Bezet_Weerstand(ASL, Off);  // Enable occupied resistor
                                                    Return_Val = Finished;
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