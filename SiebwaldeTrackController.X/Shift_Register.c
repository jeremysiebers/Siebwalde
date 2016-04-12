#include <Shift_Register.h>
#include <Fiddle_Yard.h>
#include <stdlib.h>
#include <p18f97j60.h>

static void Calc_Track_Nr(unsigned char y);

typedef struct
{
	unsigned char 	TR_MEAS_,//=0,
					CL_10_,//=0, 
					CL_10_Heart_,//=0,
					SPARE2_,//=0, 
					F11_,//=0,
					EOS_10_,//=0,
					EOS_11_,//=0, 
					SPARE3_,//=0, 
					F13_,//=0, 
					F10_,//=0, 
					F12_,//=0, 
					Bezet_Uit_5B_,//=0, 
					Bezet_Uit_6_,//=0, 
					Bezet_Uit_7_,//=0,
					Bezet_Uit_8A_,//=0, 
					Track_Nr_,//=0; 
						
					M10_,//=0, 
					Enable_Track_,//=0, 
					Bezet_In_5B_,//=1, 
					Bezet_In_6_,//=1, 
					Bezet_In_7_,//=1, 
					Bezet_Weerstand_;//=0;
}INPUT_VAR;

static INPUT_VAR PLATFORMS_IO[2] = {{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0},
									{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0}};
		
////////////////////////////////////////////////////////////////////////////////////////////////////
void IO(void)
{			
	
		PLATFORMS_IO[1].TR_MEAS_ = PORTCbits.RC1;				// PORTGbits.RG1 Not used, not connected (same name as TR_MEAS, no new name required, spare1 -> TR_MEAS
		PLATFORMS_IO[0].TR_MEAS_ = PORTCbits.RC1;				// Measures the 15 V track voltage, if not present inhibit all fiddle yard desision actions 
																// because occupied signals will go to 0V if track voltage is disabled by emergency button
		PLATFORMS_IO[1].CL_10_= ((PORTJ & 0x0F)); // Port J bits 0 to 3
		PLATFORMS_IO[0].CL_10_= ((PORTF & 0x0F)); // Port F bits 0 to 3
				
		PLATFORMS_IO[1].CL_10_Heart_ = PORTJbits.RJ4;
		PLATFORMS_IO[0].CL_10_Heart_ = PORTFbits.RF4;
		
		PLATFORMS_IO[1].SPARE2_ = PORTJbits.RJ5;
		PLATFORMS_IO[0].SPARE2_ = PORTFbits.RF5;
		
		PLATFORMS_IO[1].F11_ = PORTGbits.RG5;	
		PLATFORMS_IO[0].F11_ = PORTCbits.RC5;	
		
		PLATFORMS_IO[1].EOS_10_ = PORTGbits.RG7;	
		PLATFORMS_IO[0].EOS_10_ = PORTCbits.RC7;	
		
		PLATFORMS_IO[1].EOS_11_ = PORTJbits.RJ7;	
		PLATFORMS_IO[0].EOS_11_ = PORTFbits.RF7;		
		
		PLATFORMS_IO[1].SPARE3_ = PORTJbits.RJ6;
		PLATFORMS_IO[0].SPARE3_ = PORTFbits.RF6;
		
		PLATFORMS_IO[1].F13_ = PORTGbits.RG3;
		PLATFORMS_IO[0].F13_ = PORTCbits.RC3;
		
		PLATFORMS_IO[1].F10_ = !PORTGbits.RG4;					// Invert logic from IO port, after this the logical state = train present -> F10 True
		PLATFORMS_IO[0].F10_ = !PORTCbits.RC4;					// Invert logic from IO port, after this the logical state = train present -> F10 True
		
		PLATFORMS_IO[1].F12_ = !PORTGbits.RG6;					// Invert logic from IO port, after this the logical state = train present -> F12 True
		PLATFORMS_IO[0].F12_ = !PORTCbits.RC6;					// Invert logic from IO port, after this the logical state = train present -> F12 True
		
		PLATFORMS_IO[1].Bezet_Uit_5B_ = PORTHbits.RH0;			// This means the occupied output of the PWM Rail regulator to the uProc
		PLATFORMS_IO[0].Bezet_Uit_5B_ = PORTHbits.RH4;
			
		PLATFORMS_IO[1].Bezet_Uit_6_ = PORTHbits.RH1;
		PLATFORMS_IO[0].Bezet_Uit_6_ = PORTHbits.RH5;
		
		PLATFORMS_IO[1].Bezet_Uit_7_ = PORTHbits.RH2;			// RG0 and RG1 are used for PWM and brake BOTTOM output
		PLATFORMS_IO[0].Bezet_Uit_7_ = PORTHbits.RH6;
		
		PLATFORMS_IO[1].Bezet_Uit_8A_ = PORTHbits.RH3;
		PLATFORMS_IO[0].Bezet_Uit_8A_ = PORTHbits.RH7;			// PORTEbits.RE6 and PORTEbits.RE7 spare outputs via opto
		
		PORTDbits.RD7 = PLATFORMS_IO[1].Bezet_Weerstand_;		// PORTDbits.RD5 and PORTDbits.RD6 are I^2C Clock and Data
		PORTDbits.RD2 = PLATFORMS_IO[0].Bezet_Weerstand_;
		
		PORTEbits.RE2 = PLATFORMS_IO[1].Bezet_In_7_;			// This means to the "occupied" input of the PWM Rail regulator from the uProc
		PORTEbits.RE6 = PLATFORMS_IO[0].Bezet_In_7_;
		
		PORTEbits.RE1 = PLATFORMS_IO[1].Bezet_In_6_;		
		PORTEbits.RE5 = PLATFORMS_IO[0].Bezet_In_6_;				
		
		PORTEbits.RE0 = PLATFORMS_IO[1].Bezet_In_5B_;
		PORTEbits.RE4 = PLATFORMS_IO[0].Bezet_In_5B_;
		
		PORTDbits.RD1 = PLATFORMS_IO[1].Enable_Track_;
		PORTDbits.RD4 = PLATFORMS_IO[0].Enable_Track_;					// RC0 and RC2 are used for PWM and brake TOP output
		
		PORTDbits.RD0 = PLATFORMS_IO[1].M10_;
		PORTDbits.RD3 = PLATFORMS_IO[0].M10_;	
		
		
		if (PLATFORMS_IO[1].CL_10_Heart_ == 1)
		{
			Calc_Track_Nr(1);
		}
		else { PLATFORMS_IO[1].Track_Nr_ = 0;}
		
		if (PLATFORMS_IO[0].CL_10_Heart_ == 1)
		{
			Calc_Track_Nr(0);
		}
		else { PLATFORMS_IO[0].Track_Nr_ = 0;}

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void Calc_Track_Nr(unsigned char y)
{
	switch (PLATFORMS_IO[y].CL_10_)
	{
		case	0x2		:	PLATFORMS_IO[y].Track_Nr_ = 1;
							break;
		case	0x3		:	PLATFORMS_IO[y].Track_Nr_ = 2;
							break;
		case	0x1		:	PLATFORMS_IO[y].Track_Nr_ = 3;
							break;
		case	0x5		:	PLATFORMS_IO[y].Track_Nr_ = 4;
							break;
		case	0x7		:	PLATFORMS_IO[y].Track_Nr_ = 5;
							break;
		case	0x6		:	PLATFORMS_IO[y].Track_Nr_ = 6;
							break;
		case	0x4		:	PLATFORMS_IO[y].Track_Nr_ = 7;
							break;
		case	0xC		:	PLATFORMS_IO[y].Track_Nr_ = 8;
							break;
		case	0xE		:	PLATFORMS_IO[y].Track_Nr_ = 9;
							break;
		case	0xF		:	PLATFORMS_IO[y].Track_Nr_ = 10;
							break;
		case	0xD		:	PLATFORMS_IO[y].Track_Nr_ = 11;
							break;
		default			:	PLATFORMS_IO[y].Track_Nr_ = 0;
							break;
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////Inputs///////////////////////////////////////////////////////////////////////////////////////////////////////////////

unsigned char TR_MEAS(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].TR_MEAS_);	
}

unsigned char CL_10(unsigned char ASL)
{
   return(PLATFORMS_IO[ASL].CL_10_);
}

unsigned char CL_10_Heart(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].CL_10_Heart_);	
}

unsigned char SPARE2(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].SPARE2_);	
}

unsigned char F11(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].F11_);	
}

unsigned char EOS_10(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].EOS_10_);	
}

unsigned char EOS_11(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].EOS_11_);	
}

unsigned char SPARE3(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].SPARE3_);	
}

unsigned char F13(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].F13_);	
}

unsigned char F10(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].F10_);	
}

unsigned char F12(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].F12_);	
}

unsigned char Bezet_Uit_5B(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bezet_Uit_5B_);	
}

unsigned char Bezet_Uit_6(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bezet_Uit_6_);	
}

unsigned char Bezet_Uit_7(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bezet_Uit_7_);	
}

unsigned char Bezet_Uit_8A(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bezet_Uit_8A_);	
}

unsigned char Track_Nr(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Track_Nr_);	
}

unsigned unsigned char M10_Status(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].M10_);	
}

unsigned char Enable_Track_Status(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Enable_Track_);	
}

unsigned char Bezet_In_5B_Status(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bezet_In_5B_);	
}

unsigned char Bezet_In_6_Status(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bezet_In_6_);	
}

unsigned char Bezet_In_7_Status(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bezet_In_7_);	
}

unsigned char Bezet_Weerstand_Status(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bezet_Weerstand_);	
}
////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////OUTPUTS////////////////////////////////////////////////////////////////////////

void M10(unsigned char ASL, unsigned char M10_Val)
{
	PLATFORMS_IO[ASL].M10_ = M10_Val;
}

void Enable_Track(unsigned char ASL, unsigned char Enable_Track_Val)
{
	PLATFORMS_IO[ASL].Enable_Track_ = Enable_Track_Val;
}

void Bezet_In_5B(unsigned char ASL, unsigned char Bezet_In_5B_Val)
{
	PLATFORMS_IO[ASL].Bezet_In_5B_ = Bezet_In_5B_Val;
}

void Bezet_In_6(unsigned char ASL, unsigned char Bezet_In_6_Val)
{
	PLATFORMS_IO[ASL].Bezet_In_6_ = Bezet_In_6_Val;
}

void Bezet_In_7(unsigned char ASL, unsigned char Bezet_In_7_Val)
{
	PLATFORMS_IO[ASL].Bezet_In_7_ = Bezet_In_7_Val;
}

void Bezet_Weerstand(unsigned char ASL, unsigned char Bezet_Weerstand_Val)
{
	PLATFORMS_IO[ASL].Bezet_Weerstand_ = Bezet_Weerstand_Val;
}