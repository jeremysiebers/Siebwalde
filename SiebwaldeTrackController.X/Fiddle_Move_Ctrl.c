#include <Fiddle_Move_Ctrl.h>
#include <Shift_Register.h>
#include <Fiddle_Yard.h>
#include <Var_Out.h>
#include <pwm.h>

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
#define F12TrainDetect 0x0D
#define F13TrainDetect 0x0E

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
#define MECH_HEART_CTR 125												// To delay the disable of the motor to get to the center of the heart pulse
#define PWM_FAST_COUNTER_CONST 3
#define PWM_ONE_INBETWEEN_COUNTER_CONST 625	
#define PWM_RAMP_UP_DELAY_REG_CONST 5
#define PWM_RAMP_UP_MULTIPLE_DELAY_REG_CONST 1							//	When  "1" a devider of 2 is active ( 0 - > 1 )
#define ENCODER_LOST_COUNTER_CONST 100

void PWM_Update(unsigned char ASL, unsigned int Value);
void Pwm_Brake(unsigned char ASL, unsigned char Value);

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
							Mech_Heart_Ctr,								// To delay the disable of the motor to get to the center of the heart pulse
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
							
	unsigned char 			Fiddle;//=0,								// Switch for Fiddle yard movements

							
							
}STATE_MACHINE_VAR;

static STATE_MACHINE_VAR ACT_ST_MCHN[2]= 	{{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},		// is 0 is BOTTOM
											 {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}};		// is 1 is TOP


void Fiddle_Move_Ctrl_Reset(unsigned char ASL)
{
	ACT_ST_MCHN[ASL].Fiddle = 0;
	M10(ASL,Off);
	PWM_Update(ASL, 512);
	Pwm_Brake(ASL, On);
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
	ACT_ST_MCHN[ASL].Mech_Heart_Ctr = MECH_HEART_CTR;											// To delay the disable of the motor to get to the center of the heart pulse
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
		case	TOP		:	
							break;
		case	BOTTOM	:	
							break;	
		default			:	break;
	}
}

unsigned char Fiddle_Multiple_Right(unsigned char ASL, char New_Track_Muktiple_Right, char Brake_Track)	// 11 naar 1
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle)
	{
		case	0	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))// || (CL_10(ASL) == 0x2))
						{															// laatste spoor (uiteinde) 0x2//
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right = ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Right_Init;	// reset pwm start waarde
						Enable_Track(ASL,Off);
						Bezet_Weerstand(ASL, On);
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
		case	2	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
		case	3	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
						if ((CL_10_Heart(ASL) == 1) && (Track_Nr(ASL) == Brake_Track))
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Fiddle = 4;
						}
						break;
						
		case	4	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
								ACT_ST_MCHN[ASL].Mech_Heart_Ctr--;	// Extra delay to ensure heart stop position is in the middle of the heart bit
								
								if (ACT_ST_MCHN[ASL].Mech_Heart_Ctr < 1)
								{
									ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
									ACT_ST_MCHN[ASL].Mech_Delay = 0;
									ACT_ST_MCHN[ASL].Mech_Heart_Ctr = MECH_HEART_CTR;
									ACT_ST_MCHN[ASL].Fiddle = 6;
									PWM_Update(ASL,  512 );
								}
								
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
						
		case	5	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
		case	7	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							Enable_Track(ASL,On);
							Bezet_Weerstand(ASL, Off);
							Return_Val = Finished;
							break;
						}
						if (CL_10_Heart(ASL) == 0)
						{
							ACT_ST_MCHN[ASL].Fiddle = 8;
							PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Left );
						}
						break;
		case	8	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							Enable_Track(ASL,On);
							Bezet_Weerstand(ASL, Off);
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


unsigned char Fiddle_One_Right(unsigned char ASL)	//rechts op bewegen 11 naar 1
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle)
	{
		case	0	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))// || (CL_10(ASL) == 0x2))// laatste spoor (uiteinde) 0x2//
						{															
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_One_Count_Right = ACT_ST_MCHN[ASL].Pwm_One_Count_Right_Init;	// reset pwm start waarde
						Enable_Track(ASL,Off);
						Bezet_Weerstand(ASL, On);
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
		case	2	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
						
		case	3	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
						
		case	4	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							ACT_ST_MCHN[ASL].Mech_Heart_Ctr--;
							
							if (ACT_ST_MCHN[ASL].Mech_Heart_Ctr < 1)
							{
								ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
								M10(ASL, On);
								ACT_ST_MCHN[ASL].Mech_Heart_Ctr = MECH_HEART_CTR;
								ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0;
								ACT_ST_MCHN[ASL].Fiddle = 6;
								PWM_Update(ASL,  512 );
							}
							
						}
												
						Return_Val = Busy;
						break;
												
		case	5	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
		case	7	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							Enable_Track(ASL,On);
							Bezet_Weerstand(ASL, Off);
							Return_Val = Finished;
							break;
						}
						if (CL_10_Heart(ASL) == 0)
						{
							ACT_ST_MCHN[ASL].Fiddle = 8;
							PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Left );
						}
						break;
		case	8	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							Enable_Track(ASL,On);
							Bezet_Weerstand(ASL, Off);
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

unsigned char Fiddle_Multiple_Left(unsigned char ASL, char New_Track_Muktiple_Left, char Brake_Track)	// 1 naar 11
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle)
	{
		case	0	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))// || (CL_10(ASL) == 0xD))
						{															// laatste spoor (uiteinde) 0xD//
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left = ACT_ST_MCHN[ASL].Pwm_Multiple_Count_Left_Init;	// reset pwm start waarde
						Enable_Track(ASL,Off);
						Bezet_Weerstand(ASL, On);
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
		case	2	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
		case	3	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
						
						if ((CL_10_Heart(ASL) == 1) && (Track_Nr(ASL) == Brake_Track))	// wait for 1 track before the set track
						{
							ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
							ACT_ST_MCHN[ASL].Fiddle = 4;
							
						}
						Return_Val = Busy;
						break;
						
		case	4	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
								ACT_ST_MCHN[ASL].Mech_Heart_Ctr--;	// Extra delay to ensure heart stop position is in the middle of the heart bit
								
								if (ACT_ST_MCHN[ASL].Mech_Heart_Ctr < 1)
								{
									ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
									ACT_ST_MCHN[ASL].Mech_Delay = 0;
									ACT_ST_MCHN[ASL].Mech_Heart_Ctr = MECH_HEART_CTR;
									ACT_ST_MCHN[ASL].Fiddle = 6;
									PWM_Update(ASL,  512 );
								}
								
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
		
		case	5	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
                            if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
		case	7	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							Enable_Track(ASL,On);
							Bezet_Weerstand(ASL, Off);
							Return_Val = Finished;
							break;
						}
						if (CL_10_Heart(ASL) == 0)
						{
							ACT_ST_MCHN[ASL].Fiddle = 8;
							PWM_Update(ASL,  ACT_ST_MCHN[ASL].Pwm_One_Slow_Right );
						}
						break;
		case	8	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							Enable_Track(ASL,On);
							Bezet_Weerstand(ASL, Off);
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

unsigned char Fiddle_One_Left(unsigned char ASL)	//Move from 1 to 11
{
	static char Return_Val = Busy;
	
	switch(ACT_ST_MCHN[ASL].Fiddle)
	{
		case	0	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))// || (CL_10(ASL) == 0xD)) // 0xD = spoor 11
						{
							ACT_ST_MCHN[ASL].Fiddle = 0;
							M10(ASL, Off);
							PWM_Update(ASL,  512 );
							Pwm_Brake(ASL, On);
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
							break;
						}
						Return_Val = Busy;
						ACT_ST_MCHN[ASL].Pwm_One_Count_Left = ACT_ST_MCHN[ASL].Pwm_One_Count_Left_Init;
						Enable_Track(ASL,Off);
						Bezet_Weerstand(ASL, On);
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
		case	2	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
						
		case	3	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))																													// Keep the Fiddle Yard speed constant during "Inbetween_Counter"
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
						
		case	4	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))																													// Ramp down of Fiddle Yard
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							ACT_ST_MCHN[ASL].Mech_Heart_Ctr--;
							
							if (ACT_ST_MCHN[ASL].Mech_Heart_Ctr < 1)
							{
								ACT_ST_MCHN[ASL].ADCconversion_Inuse = False;
								M10(ASL, On);
								ACT_ST_MCHN[ASL].Mech_Heart_Ctr = MECH_HEART_CTR;
								ACT_ST_MCHN[ASL].Pwm_Fast_Counter = 0;
								ACT_ST_MCHN[ASL].Fiddle = 6;
								PWM_Update(ASL,  512 );
							}
							
						}
						Return_Val = Busy;
						break;
												
		case	5	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
		case	7	:	if (EOS_11(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_11(ASL))
							{
								Return_Val = EndOffStroke_11;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							Enable_Track(ASL,On);
							Bezet_Weerstand(ASL, Off);
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
		case	8	:	if (EOS_10(ASL) || F12(ASL) || F13(ASL))
						{
							if (EOS_10(ASL))
							{
								Return_Val = EndOffStroke_10;
							}
                            else if (F12(ASL))
                            {
                                Return_Val = F12TrainDetect;
                            }
                            else if (F13(ASL))
                            {
                                Return_Val = F13TrainDetect;
                            }
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
							Enable_Track(ASL,On);
							Bezet_Weerstand(ASL, Off);
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