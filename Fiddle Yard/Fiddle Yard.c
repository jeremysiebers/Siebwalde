/** I N C L U D E S **********************************************************/
#define THIS_IS_STACK_APPLICATION

#include <p18f97j60.h>			// uProc lib
#include <TCPIP.h>				// TCPIP header
#include <stdio.h>				// stdio lib
#include <stdlib.h>				// std lib
#include <string.h>				// string lib
#include <delays.h>				// delay routines
#include <pwm.h>				// Pwm routines
#include <Fiddle_Yard.h>		// main header file
#include <Shift_Register.h>		// IO from Fiddle Yard
#include <Command_Machine.h>	// serial commands received
#include <State_Machine.h>		// all movements and fidle yard program
#include <Diagnostic_ret.h>		// serial transmitting of IO
#include <Var_Out.h>			// building packets to transmit
#include <adc.h>				// ADC lib
#include <IO_Expander.h>		// IO Expander extra IO

//CONFIGURATION BITS//
#pragma config DEBUG = OFF
//#pragma config XINST = OFF
#pragma config STVR = ON
//#pragma config WDT = OFF
#pragma config CP0 = OFF
#pragma config FCMEN = OFF
#pragma config IESO = OFF
//#pragma config FOSC2 = ON
//#pragma config FOSC = HSPLL
#pragma config WDTPS = 1
#pragma config WAIT = OFF
#pragma config BW = 8
#pragma config MODE = MM
#pragma config EASHFT = OFF
//#pragma config ETHLED = ON
#pragma config ECCPMX = OFF
#pragma config CCP2MX = OFF

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//DECLARATIONS//


// Declare AppConfig structure and some other supporting stack variables
APP_CONFIG AppConfig;
BYTE AN0String[8];

// Private helper functions.
// These may or may not be present in all applications.
static void InitAppConfig(void);

#define Init_IO()			TRISJ=0xFF,TRISH=0xFF,TRISG=0xFC,TRISF=0xFF,TRISE=0x3F,TRISD=0x60,TRISC=0x00;TRISA=0xC;TRISB=0xC0;// All ports including Pwm and AD
#define Leds_Off()			Led1 = Off, Led2 = Off, Led3 = Off, Led4 = Off, Led5 = Off;

static void Init_Timers(void);
static void Init_Pwm(void);
static void Init_Ad(void);

static unsigned char Enable_State_Machine_Update = 0;
static DWORD dwLastIP = 0;

//MAIN ROUTINE///////////////////////////////////////////////////////////////////////////////////////////
void main()
{			
	TRISA 	= 0xFF;TRISB = 0xFF;TRISC = 0xFF;TRISD = 0xFF;TRISE = 0xFF;TRISF = 0xFF;TRISG = 0xFF;TRISH = 0xFF;TRISJ = 0xFF;				// All IO are inputs
	PORTA 	= 0x00, PORTB = 0x00, PORTC = 0x00, PORTD = 0x00, PORTE = 0x00, PORTF = 0x00, PORTG = 0x00, PORTH = 0x00, PORTJ = 0x00; 	// All IO = 0 
	ADCON0 	= 0x00;																														// AD OFF
	ADCON1 	= 0x0F;																														// All Digital
	CMCON 	= 0x07;
	
	Init_Timers();
	TickInit(); 
	InitAppConfig(); 
	StackInit(); 
	Init_Ad();	
	Init_Pwm();
	Init_IO();
	Init_IOExpander();
	Leds_Off();	
	
	while(1)
	{

	 	StackTask();
	 	StackApplications();
	 		 	
	 	Diagnostic();
	    Command();
	    
				
		if (Enable_State_Machine_Update == True && Output_Enable == True)
		{			
			Led4 = 1;
			IOExpander();
			IO();	
			State_Machine_Update(TOP);
			State_Machine_Update(BOTTOM);
			Enable_State_Machine_Update = False;
			Led4 = 0;						
		}
		
		/////Announce IP after IP change or power up event////////		
		if(dwLastIP != AppConfig.MyIPAddr.Val)
		{
			dwLastIP = AppConfig.MyIPAddr.Val;
					
			#if defined(STACK_USE_ANNOUNCE)
				AnnounceIP();
			#endif
		}
		
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Interrupt routines//
void high_isr(void);
#pragma code high_vector=0x8
void interrupt_at_high_vector (void)
{
	_asm goto high_isr _endasm
}
#pragma code
#pragma interrupt high_isr
void high_isr()
{
	
}

void low_isr(void);
#pragma code low_vector=0x18
void interrupt_at_low_vector (void)
{
	_asm goto low_isr _endasm
}
#pragma code
#pragma interrupt low_isr
void low_isr()
{
	Led2 = !Led2;
	if (INTCONbits.TMR0IF)
	{
		TickUpdate();
	}
				
	if (PIR1bits.TMR1IF)						
	{	
		TMR1H = 0xF0;//0xF3	= 3333 Hz, 0x00 = 1587 Hz, 0xF0 = 2439 Hz
		TMR1L = 0x00;
		Enable_State_Machine_Update = True;
		PIR1bits.TMR1IF=False;		
	}	
}
#pragma code
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//MAIN SUBROUTINES//

void Init_Timers()
{	
	// enable Timers and interrupts, Timer0 is used for TCPIP stack, this is configured in Tick.c
	// Enable Interrupts before stackinit etc
	OSCTUNE = 0x40;			// setting uProc speed to 41.667 MHz
	RCONbits.IPEN = 1;		// Enable interrupt priorities
    INTCONbits.GIEH = 1;	// Enables all high priority interrupts
    INTCONbits.GIEL = 1;	// Enables all low priority interrupts
	INTCONbits.INT0IE = 0;
	INTCONbits.RBIE = 0;
	
	INTCON2bits.RBPU = 1;
	
	INTCON3bits.INT3IE = 0;
	INTCON3bits.INT2IE = 0;
	INTCON3bits.INT1IE = 0;
	
	PIE1 = 0x01;					//TMR1 overflow interrupt enable on=0x1 off=0x00
	
	PIE2bits.TMR3IE = 0;
	PIE2bits.CCP2IE = 0;
	
	PIE3 = 0x00;					// Also interrupt AD converter is off
	
	IPR1 = 0x00;					//Timer 1 low priority (0x00), high when 0x01
	
	IPR2 = 0x00;					//All others low priority
	
	IPR3 = 0x00;					// All others low priority
	
	//Timer 1 for Fiddle Yard Update Tick
	
	TMR1H = 0x0;
	TMR1L = 0x0;
	T1CON = 0x81;					// 0xB1 = 1:8(40mS), 0xA1 = 1:4(22.5mS), 0x91 = 1:2(11mS), 0x81 = 1:1(5.5mS) 0x81 = 16Bit and anabled

}

void Init_Ad()
{
	ADCON1 = 0x0B;	// Channel AN0 to AN3 are configured as AD inputs for the AD converter. However AN0 to AN1 are Ethernet leds, so they are outputs.AN2 and AN3 are PORTA bits 2 and 3 (RA2, RA3)
	ADCON2 = 0x92;	// Acquisition time and converting time of the AD same for both channels 0x92 = /32	0x96 = /64 and 4 TAD (Tacq)
}

void Init_Pwm()
{
	// PWM setup using 25mA outputs to drive opto's ECCP 3 and 1 are chosen for PWM
	T3CON = 0x00;			// Timer 2 and 4 are clock sources for all CCPx/ECCPx modules, tmr2 is used for PWM mode page 186, 0x60 for tmr 3 and 4, 0x00 for tmr 1 and 2
	
	T2CON = 0x04;			// Postscale 1:1, Timer2 On, Prescaler is 1
		
	Pwm_Brake_BOTTOM = 1;
	PR2=0xff;				//PWM Period TMR2 zelfde voor PWM1(ECCP1) en PWM2(ECCP3)
	CCPR1L = 0x7F;			//PWM Duty cycle PWM1
	CCP1CON = 0x0C;			//PWM Mode
		
	Pwm_Brake_TOP = 1;
	CCPR3L = 0x7F;			//PWM Duty cycle PWM2
	CCP3CON = 0x0C;			//PWM Mode

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/*********************************************************************
 * Function:        void InitAppConfig(void)
 *
 * PreCondition:    MPFSInit() is already called.
 *
 * Input:           None
 *
 * Output:          Write/Read non-volatile config variables.
 *
 * Side Effects:    None
 *
 * Overview:        None
 *
 * Note:            None
 ********************************************************************/
// MAC Address Serialization using a MPLAB PM3 Programmer and 
// Serialized Quick Turn Programming (SQTP). 
// The advantage of using SQTP for programming the MAC Address is it
// allows you to auto-increment the MAC address without recompiling 
// the code for each unit.  To use SQTP, the MAC address must be fixed
// at a specific location in program memory.  Uncomment these two pragmas
// that locate the MAC address at 0x1FFF0.  Syntax below is for MPLAB C 
// Compiler for PIC18 MCUs. Syntax will vary for other compilers.
//#pragma romdata MACROM=0x1FFF0
static ROM BYTE SerializedMACAddress[6] = {MY_DEFAULT_MAC_BYTE1, MY_DEFAULT_MAC_BYTE2, MY_DEFAULT_MAC_BYTE3, MY_DEFAULT_MAC_BYTE4, MY_DEFAULT_MAC_BYTE5, MY_DEFAULT_MAC_BYTE6};
//#pragma romdata
static void InitAppConfig(void)
{
	AppConfig.Flags.bIsDHCPEnabled = TRUE;
	AppConfig.Flags.bInConfigMode = TRUE;
	memcpypgm2ram((void*)&AppConfig.MyMACAddr, (ROM void*)SerializedMACAddress, sizeof(AppConfig.MyMACAddr));   //(large memory model dus....)
//	{
//		_prog_addressT MACAddressAddress;
//		MACAddressAddress.next = 0x157F8;
//		_memcpy_p2d24((char*)&AppConfig.MyMACAddr, MACAddressAddress, sizeof(AppConfig.MyMACAddr));
//	}
	AppConfig.MyIPAddr.Val = MY_DEFAULT_IP_ADDR_BYTE1 | MY_DEFAULT_IP_ADDR_BYTE2<<8ul | MY_DEFAULT_IP_ADDR_BYTE3<<16ul | MY_DEFAULT_IP_ADDR_BYTE4<<24ul;
	AppConfig.DefaultIPAddr.Val = AppConfig.MyIPAddr.Val;
	AppConfig.MyMask.Val = MY_DEFAULT_MASK_BYTE1 | MY_DEFAULT_MASK_BYTE2<<8ul | MY_DEFAULT_MASK_BYTE3<<16ul | MY_DEFAULT_MASK_BYTE4<<24ul;
	AppConfig.DefaultMask.Val = AppConfig.MyMask.Val;
	AppConfig.MyGateway.Val = MY_DEFAULT_GATE_BYTE1 | MY_DEFAULT_GATE_BYTE2<<8ul | MY_DEFAULT_GATE_BYTE3<<16ul | MY_DEFAULT_GATE_BYTE4<<24ul;
	AppConfig.PrimaryDNSServer.Val = MY_DEFAULT_PRIMARY_DNS_BYTE1 | MY_DEFAULT_PRIMARY_DNS_BYTE2<<8ul  | MY_DEFAULT_PRIMARY_DNS_BYTE3<<16ul  | MY_DEFAULT_PRIMARY_DNS_BYTE4<<24ul;
	AppConfig.SecondaryDNSServer.Val = MY_DEFAULT_SECONDARY_DNS_BYTE1 | MY_DEFAULT_SECONDARY_DNS_BYTE2<<8ul  | MY_DEFAULT_SECONDARY_DNS_BYTE3<<16ul  | MY_DEFAULT_SECONDARY_DNS_BYTE4<<24ul;


	// SNMP Community String configuration
	#if defined(STACK_USE_SNMP_SERVER)
	{
		BYTE i;
		static ROM char * ROM cReadCommunities[] = SNMP_READ_COMMUNITIES;
		static ROM char * ROM cWriteCommunities[] = SNMP_WRITE_COMMUNITIES;
		ROM char * strCommunity;
		
		for(i = 0; i < SNMP_MAX_COMMUNITY_SUPPORT; i++)
		{
			// Get a pointer to the next community string
			strCommunity = cReadCommunities[i];
			if(i >= sizeof(cReadCommunities)/sizeof(cReadCommunities[0]))
				strCommunity = "";

			// Ensure we don't buffer overflow.  If your code gets stuck here, 
			// it means your SNMP_COMMUNITY_MAX_LEN definition in TCPIPConfig.h 
			// is either too small or one of your community string lengths 
			// (SNMP_READ_COMMUNITIES) are too large.  Fix either.
			if(strlenpgm(strCommunity) >= sizeof(AppConfig.readCommunity[0]))
				while(1);
			
			// Copy string into AppConfig
			strcpypgm2ram((char*)AppConfig.readCommunity[i], strCommunity);

			// Get a pointer to the next community string
			strCommunity = cWriteCommunities[i];
			if(i >= sizeof(cWriteCommunities)/sizeof(cWriteCommunities[0]))
				strCommunity = "";

			// Ensure we don't buffer overflow.  If your code gets stuck here, 
			// it means your SNMP_COMMUNITY_MAX_LEN definition in TCPIPConfig.h 
			// is either too small or one of your community string lengths 
			// (SNMP_WRITE_COMMUNITIES) are too large.  Fix either.
			if(strlenpgm(strCommunity) >= sizeof(AppConfig.writeCommunity[0]))
				while(1);

			// Copy string into AppConfig
			strcpypgm2ram((char*)AppConfig.writeCommunity[i], strCommunity);
		}
	}
	#endif

	// Load the default NetBIOS Host Name
	memcpypgm2ram(AppConfig.NetBIOSName, (ROM void*)MY_DEFAULT_HOST_NAME, 16);
	FormatNetBIOSName(AppConfig.NetBIOSName);

	#if defined(ZG_CS_TRIS)
		// Load the default SSID Name
		if (sizeof(MY_DEFAULT_SSID_NAME) > sizeof(AppConfig.MySSID))
		{
			ZGErrorHandler((ROM char *)"AppConfig.MySSID[] too small");
		}
		memcpypgm2ram(AppConfig.MySSID, (ROM void*)MY_DEFAULT_SSID_NAME, sizeof(MY_DEFAULT_SSID_NAME));
	#endif

	#if defined(EEPROM_CS_TRIS)
	{
		BYTE c;
		
	    // When a record is saved, first byte is written as 0x60 to indicate
	    // that a valid record was saved.  Note that older stack versions
		// used 0x57.  This change has been made to so old EEPROM contents
		// will get overwritten.  The AppConfig() structure has been changed,
		// resulting in parameter misalignment if still using old EEPROM
		// contents.
		XEEReadArray(0x0000, &c, 1);
	    if(c == 0x60u)
		    XEEReadArray(0x0001, (BYTE*)&AppConfig, sizeof(AppConfig));
	    else
	        SaveAppConfig();
	}
	#elif defined(SPIFLASH_CS_TRIS)
	{
		BYTE c;
		
		SPIFlashReadArray(0x0000, &c, 1);
		if(c == 0x60u)
			SPIFlashReadArray(0x0001, (BYTE*)&AppConfig, sizeof(AppConfig));
		else
			SaveAppConfig();
	}
	#endif
}

#if defined(EEPROM_CS_TRIS) || defined(SPIFLASH_CS_TRIS)
void SaveAppConfig(void)
{
	// Ensure adequate space has been reserved in non-volatile storage to 
	// store the entire AppConfig structure.  If you get stuck in this while(1) 
	// trap, it means you have a design time misconfiguration in TCPIPConfig.h.
	// You must increase MPFS_RESERVE_BLOCK to allocate more space.
	#if defined(STACK_USE_MPFS) || defined(STACK_USE_MPFS2)
		if(sizeof(AppConfig) > MPFS_RESERVE_BLOCK)
			while(1);
	#endif

	#if defined(EEPROM_CS_TRIS)
	    XEEBeginWrite(0x0000);
	    XEEWrite(0x60);
	    XEEWriteArray((BYTE*)&AppConfig, sizeof(AppConfig));
    #else
	    SPIFlashBeginWrite(0x0000);
	    SPIFlashWrite(0x60);
	    SPIFlashWriteArray((BYTE*)&AppConfig, sizeof(AppConfig));
    #endif
}
#endif

