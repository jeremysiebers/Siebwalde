#include "Mip50_API.h"
#include "Fiddle_Yard.h"

////////ERROR and return CODES////////
#define ERROR 0xEE	// general switch case when error
#define Busy -1
#define Finished 0

typedef struct
{
	unsigned char 			Mip50_SwitchState,                        			// Switch for Fiddle direction movement							
                            Return_Val_Routine;                                 // Return value of sub routine switch
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0},                             // is 0 is BOTTOM
											 {0,0}};                            // is 1 is TOP

void MIP50xRECEIVEDxDATA(unsigned char ASL)
{
    
}

unsigned char MIP50xHOME(unsigned char ASL)
{
    return(0);
}

extern unsigned char MIP50xMOVE(unsigned char ASL, int New_Track)
{
    return (0);
}