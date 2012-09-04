#include <Bridge_Ctrl.h>
#include <Shift_Register.h>
#include <Fiddle_Yard.h>
#include <Var_Out.h>

#define On 1
#define Off 0
#define Open 1
#define Closed 0
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


typedef struct
{
	unsigned char 			Bridge_Open,//=0,							// Switch for bridge open
							Bridge_Close,//=0,							// Switch for bridge close
							Brigde_Status;//=0,							// Old bridge status when Error
																					
	unsigned int			Bridge_Open_Close_Timeout_Counter;//=0;		// Timeout counter for bridge movement
							
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,0,0},	// is 0 is BOTTOM
											 {0,0,0,0}};// is 1 is TOP
											 

void Bridge_Ctrl_Reset(unsigned char ASL)
{
	M11(ASL,Off);																				// Disable bridge motor
	ACT_ST_MCHN[ASL].Bridge_Open=0;																// Switch for bridge open
	ACT_ST_MCHN[ASL].Bridge_Close=0;															// Switch for bridge close
	ACT_ST_MCHN[ASL].Brigde_Status=2;															// Old bridge status when Error
	ACT_ST_MCHN[ASL].Bridge_Open_Close_Timeout_Counter=0;										// Time out counter for bridges when waiting for sensor on Motor	
}

unsigned char Bridge_Open(unsigned char ASL)
{
	static char Return_Val = Busy;
	static char send_top, send_bottom;
	
	Bridge_Opening(ASL);
	Bridge_Open_Ok(ASL);
	Return_Val = Finished;
	
	/*
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
	}*/
	return (Return_Val);
}

unsigned char Bridge_Close(unsigned char ASL)
{
	static char Return_Val = Busy;
	Bridge_Closing(ASL);
	Bridge_Close_Ok(ASL);
	Return_Val = Finished;
	
	/*	
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
	}*/
	return (Return_Val);
}