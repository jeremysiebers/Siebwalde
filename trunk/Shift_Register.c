#include <Shift_Register.h>
#include <Fiddle_Yard.h>
#include <stdlib.h>
#include <p18f97j60.h>

static void Calc_Track_Nr(unsigned char y);

typedef struct
{
	unsigned char 	Bridge_10L_,//=0,
					CL_10_,//=0, 
					CL_10_Heart_,//=0,
					BM_10_,//=0, 
					F11_,//=0,
					EOS_10_,//=0,
					EOS_11_,//=0, 
					BM_11_,//=0, 
					Bridge_10R_,//=0, 
					F10_,//=0, 
					F12_,//=0, 
					Bezet_Uit_5B_,//=0, 
					Bezet_Uit_6_,//=0, 
					Bezet_Uit_7_,//=0,
					Bezet_Uit_8A_,//=0, 
					Track_Nr_,//=0; 
						
					M10_,//=0, 
					M11_,//=0, 
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
	
		PLATFORMS_IO[1].Bridge_10L_ = PORTJbits.RJ0;
		PLATFORMS_IO[0].Bridge_10L_ = PORTGbits.RG4;
		
		PLATFORMS_IO[1].CL_10_= ((PORTJ & 0x1E) >> 1); // Port J bits 1 to 4
		PLATFORMS_IO[0].CL_10_= (((PORTG & 0xE0) >> 5) | ((PORTF & 0x01) << 3) ); // Port G bits 5,6 to 7 and Port F bit 0
				
		PLATFORMS_IO[1].CL_10_Heart_ = PORTJbits.RJ5;
		PLATFORMS_IO[0].CL_10_Heart_ = PORTFbits.RF1;
		
		PLATFORMS_IO[1].BM_10_ = PORTJbits.RJ6;
		PLATFORMS_IO[0].BM_10_ = PORTFbits.RF2;
		
		PLATFORMS_IO[1].F11_ = PORTJbits.RJ7;	
		PLATFORMS_IO[0].F11_ = PORTFbits.RF3;	
		
		PLATFORMS_IO[1].EOS_10_ = PORTHbits.RH0;	
		PLATFORMS_IO[0].EOS_10_ = PORTFbits.RF4;	
		
		PLATFORMS_IO[1].EOS_11_ = PORTHbits.RH1;	
		PLATFORMS_IO[0].EOS_11_ = PORTFbits.RF5;		
		
		PLATFORMS_IO[1].BM_11_ = PORTHbits.RH2;
		PLATFORMS_IO[0].BM_11_ = PORTFbits.RF6;
		
		PLATFORMS_IO[1].Bridge_10R_ = PORTHbits.RH3;
		PLATFORMS_IO[0].Bridge_10R_ = PORTFbits.RF7;
		
		PLATFORMS_IO[1].F10_ = PORTHbits.RH4;
		PLATFORMS_IO[0].F10_ = PORTEbits.RE0;
		
		PLATFORMS_IO[1].F12_ = PORTHbits.RH5;
		PLATFORMS_IO[0].F12_ = PORTEbits.RE1;
		
		PLATFORMS_IO[1].Bezet_Uit_5B_ = PORTHbits.RH6;
		PLATFORMS_IO[0].Bezet_Uit_5B_ = PORTEbits.RE2;
			
		PLATFORMS_IO[1].Bezet_Uit_6_ = PORTHbits.RH7;
		PLATFORMS_IO[0].Bezet_Uit_6_ = PORTEbits.RE3;
		
		PLATFORMS_IO[1].Bezet_Uit_7_ = PORTGbits.RG2;			// RG0 and RG1 are used for PWM and brake output
		PLATFORMS_IO[0].Bezet_Uit_7_ = PORTEbits.RE4;
		
		PLATFORMS_IO[1].Bezet_Uit_8A_ = PORTGbits.RG3;
		PLATFORMS_IO[0].Bezet_Uit_8A_ = PORTEbits.RE5;			// PORTEbits.RE6 and PORTEbits.RE7 spare inputs via opto
		
		PORTDbits.RD5 = PLATFORMS_IO[1].Bezet_Weerstand_;		// PORTDbits.RD6 and PORTDbits.RD7 spare outputs via opto
		PORTCbits.RC7 = PLATFORMS_IO[0].Bezet_Weerstand_;
		
		PORTDbits.RD4 = PLATFORMS_IO[1].Bezet_In_7_;
		PORTCbits.RC6 = PLATFORMS_IO[0].Bezet_In_7_;
		
		PORTDbits.RD3 = PLATFORMS_IO[1].Bezet_In_6_;		
		PORTCbits.RC5 = PLATFORMS_IO[0].Bezet_In_6_;				
		
		PORTDbits.RD2 = PLATFORMS_IO[1].Bezet_In_5B_;
		PORTCbits.RC4 = PLATFORMS_IO[0].Bezet_In_5B_;
		
		PORTDbits.RD1 = PLATFORMS_IO[1].M11_;
		PORTCbits.RC3 = PLATFORMS_IO[0].M11_;					// RC1 and RC2 are used for PWM and brake output
		
		PORTDbits.RD0 = PLATFORMS_IO[1].M10_;
		PORTCbits.RC0 = PLATFORMS_IO[0].M10_;	
		
		
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

unsigned char Bridge_10L(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bridge_10L_);	
}

unsigned char CL_10(unsigned char ASL)
{
   return(PLATFORMS_IO[ASL].CL_10_);
}

unsigned char CL_10_Heart(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].CL_10_Heart_);	
}

unsigned char BM_10(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].BM_10_);	
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

unsigned char BM_11(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].BM_11_);	
}

unsigned char Bridge_10R(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].Bridge_10R_);	
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

unsigned char M11_Status(unsigned char ASL)
{
	return(PLATFORMS_IO[ASL].M11_);	
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

void M11(unsigned char ASL, unsigned char M11_Val)
{
	PLATFORMS_IO[ASL].M11_ = M11_Val;
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
