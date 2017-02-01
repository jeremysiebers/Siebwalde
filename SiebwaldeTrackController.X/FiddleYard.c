/** I N C L U D E S **********************************************************/
#define THIS_IS_STACK_APPLICATION
#define USE_TCPIP

#include <p18f97j60.h>			// uProc lib
#include <stdio.h>				// stdio lib
#include <stdlib.h>				// std lib
#include <string.h>				// string lib
#include <delays.h>				// delay routines
#include <pwm.h>				// Pwm routines
#include <adc.h>				// ADC lib
#include "Microchip/Include/TCPIP Stack/TCPIP.h"
#include "Command_Machine.h"	// serial commands received
#include "Diagnostic_ret.h"		// serial transmitting of IO
#include "Fiddle_Yard.h"		// main header file
#include "Shift_Register.h"		// IO from Fiddle Yard
#include "Var_Out.h"			// building packets to transmit
#include "I2c2/i2c.h"           // I2C lib
#include "eusart1.h"            // EUSART1 lib 
#include "eusart2.h"            // EUSART2 lib 
#include "api.h"                // API of the PWM slaves
#include "TrackAmplifier.h"     // Track Amplifier communication interface
#include "State_Machine.h"      // Trackamplifier state machine
//#include <IO_Expander.h>		// IO Expander extra IO

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

#define Init_IO()			TRISJ=0x00,TRISH=0x00,TRISG=0x00,TRISF=0x00,TRISE=0x00,TRISD=0x00,TRISC=0x00;TRISA=0x00;TRISB=0x00;// All ports are outputs !!!
#define Leds_Off()			Led1 = Off, Led2 = Off, Led3 = Off;

// Declare AppConfig structure and some other supporting stack variables
APP_CONFIG AppConfig;
BYTE AN0String[8];

// Private helper functions.
// These may or may not be present in all applications.
static void InitAppConfig(void);
static DWORD dwLastIP = 0;

static void Init_Timers(void);
static void Init_Pwm(void);
static void I2C1_Initialize(void);

unsigned char Enable_State_Machine_Update = 0;
unsigned char Data_receieved[1];
unsigned char *DataR;
unsigned int I2C_write_counter = 1, I2C_read_counter = 1;

//MAIN ROUTINE///////////////////////////////////////////////////////////////////////////////////////////
void main()
{			
	TRISA 	= 0xFF;TRISB = 0xFF;TRISC = 0xFF;TRISD = 0xFF;TRISE = 0xFF;TRISF = 0xFF;TRISG = 0xFF;TRISH = 0xFF;TRISJ = 0xFF;				// All IO are inputs
	PORTA 	= 0x00, PORTB = 0x00, PORTC = 0x00, PORTD = 0x00, PORTE = 0x00, PORTF = 0x00, PORTG = 0x00, PORTH = 0x00, PORTJ = 0x00; 	// All IO = 0 
	ADCON0 	= 0x00;																														// AD OFF
	ADCON1 	= 0x0F;																														// All Digital
	CMCON 	= 0x07;
	
	Init_Timers();
    #if defined (USE_TCPIP)
	TickInit(); 
	InitAppConfig(); 
	StackInit();
    #endif
	Init_Pwm();
	Init_IO();	
	Leds_Off();	
    EUSART1_Initialize();
    //EUSART2_Initialize();
    I2C1_Initialize();
    
    DataR = &Data_receieved[0];
    
    printf("PIC18f97j60 started up!!!\n\r");
    printf("\f");                                                               // Clear terminal (printf("\033[2J");)
    printf("PIC18f97j60 started up!!!\n\r");                                    // Welcome message
    
    StatexMachinexInit();
        
    while(1)
	{
        #if defined (USE_TCPIP)
	 	StackTask();
	 	StackApplications();
        #endif
	 	Diagnostic();
	    Command();
	    
        if (Enable_State_Machine_Update == True)
		{
            ReadxAmplifiers();
            StatexMachinexUpdate();
            Enable_State_Machine_Update = False;
            
            /*
            //printf("\f");
            //printf("Start write: %d\n\r",I2C_write_counter);
            //printf("Start read: %d\n\r",I2C_read_counter);
            switch (TrackAmplifierxWritexAPI(0x50, PWM1_SETPOINT, 0xE0)){
                case ACK  : I2C_write_counter++;
                    break;
                case NACK :printf("Set 50:NACK\n\r");
                    break;
                case WCOL :printf("Set 50:WCOL\n\r");
                    break;
                case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
                    break;
                default : break;
            }            
            switch (TrackAmplifierxReadxAPI(0x50, PWM1_SETPOINT, DataR)){
                case ACK  : I2C_read_counter++;
                    break;
                case NACK :printf("Read 50:NACK\n\r");//Led2 = 0;
                    break;
                case WCOL :printf("Read 50:WCOL\n\r");//Led2 = 0;
                    break;
                case TIMEOUT :printf("Read 50:TIMEOUT\n\r");//Led2 = 0;
                    break;
                default : break;
            }
            //printf("Done\n\r");
            //printf("\033[3A");
            //Delay10KTCYx(255);
            if (I2C_write_counter >99){
                I2C_write_counter = 0;
                Led2 ^= 1;
            }
            if (I2C_read_counter >99){
                I2C_read_counter = 0;
                Led3 ^= 1;
            }
            switch (TrackAmplifierxWritexAPI(0x50, PWM2_SETPOINT, 0xE0)){
                case ACK  : 
                    break;
                case NACK :printf("Set 51:NACK\n\r");
                    break;
                case WCOL :printf("Set 51:WCOL\n\r");
                    break;
                case TIMEOUT :printf("Set 51:TIMEOUT\n\r");
                    break;
                default : break;
            }            
            switch (TrackAmplifierxReadxAPI(0x50, PWM2_SETPOINT, DataR)){
                case ACK  : 
                    break;
                case NACK :printf("Read 51:NACK\n\r");//Led2 = 0;
                    break;
                case WCOL :printf("Read 51:WCOL\n\r");//Led2 = 0;
                    break;
                case TIMEOUT :printf("Read 51:TIMEOUT\n\r");//Led2 = 0;
                    break;
                default : break;
            }
            switch (TrackAmplifierxWritexAPI(0x52, PWM1_SETPOINT, 0x55)){
                case ACK  : 
                    break;
                case NACK :printf("Set 52:NACK\n\r");
                    break;
                case WCOL :printf("Set 52:WCOL\n\r");
                    break;
                case TIMEOUT :printf("Set 52:TIMEOUT\n\r");
                    break;
                default : break;
            }            
            switch (TrackAmplifierxReadxAPI(0x52, PWM1_SETPOINT, DataR)){
                case ACK  : 
                    break;
                case NACK :printf("Read 52:NACK\n\r");//Led2 = 0;
                    break;
                case WCOL :printf("Read 52:WCOL\n\r");//Led2 = 0;
                    break;
                case TIMEOUT :printf("Read 52:TIMEOUT\n\r");//Led2 = 0;
                    break;
                default : break;
            }
            switch (TrackAmplifierxWritexAPI(0x53, PWM1_SETPOINT, 0x55)){
                case ACK  : 
                    break;
                case NACK :printf("Set 53:NACK\n\r");
                    break;
                case WCOL :printf("Set 53:WCOL\n\r");
                    break;
                case TIMEOUT :printf("Set 53:TIMEOUT\n\r");
                    break;
                default : break;
            }            
            switch (TrackAmplifierxReadxAPI(0x53, PWM1_SETPOINT, DataR)){
                case ACK  : 
                    break;
                case NACK :printf("Read 53:NACK\n\r");//Led2 = 0;
                    break;
                case WCOL :printf("Read 53:WCOL\n\r");//Led2 = 0;
                    break;
                case TIMEOUT :printf("Read 53:TIMEOUT\n\r");//Led2 = 0;
                    break;
                default : break;
            }
            
            IdleI2C2();                            
            StartI2C2();
            IdleI2C2();  
            WriteI2C2(0);
            IdleI2C2(); 
            putcI2C2('R');
            StopI2C2();
            
            
            //T1CON = 0x81;
            */
        }
        
        #if defined (USE_TCPIP)
		/////Announce IP after IP change or power up event////////		
		if(dwLastIP != AppConfig.MyIPAddr.Val)
		{
			dwLastIP = AppConfig.MyIPAddr.Val;
					
			#if defined(STACK_USE_ANNOUNCE)
				AnnounceIP();
			#endif
		}
        #endif
		
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
	
	if (INTCONbits.TMR0IF){
        #if defined (USE_TCPIP)
		TickUpdate();
        #endif
	}
				
	if (PIR1bits.TMR1IF){	
		Enable_State_Machine_Update = True;
        Led1 ^= 1; 
        //T1CONbits.TMR1ON = 0;
        TMR1H = 0x00;//0xF3	= 3333 Hz, 0x00 = 1587 Hz, 0xF0 = 2439 Hz
		TMR1L = 0x00;
        PIR1bits.TMR1IF=False;        	
	}	
    
    if (PIE3bits.RC2IE == 1 && PIR3bits.RC2IF == 1) {
        EUSART2_Receive_ISR();
    } 
    if (PIE3bits.TX2IE == 1 && PIR3bits.TX2IF == 1) {
        EUSART2_Transmit_ISR();
    } 
    if (PIE1bits.RC1IE == 1 && PIR1bits.RC1IF == 1) {
        EUSART1_Receive_ISR();
    } 
    if (PIE1bits.TX1IE == 1 && PIR1bits.TX1IF == 1) {
        EUSART1_Transmit_ISR();
    }
}
#pragma code
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//MAIN SUBROUTINES//

/******************************************************************************
 * Function:        Init_Timers
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        None
 *****************************************************************************/
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
	
	PIE1 = 0x1;					//TMR1 overflow interrupt enable on=0x1 off=0x00
	
	PIE2bits.TMR3IE = 0;
	PIE2bits.CCP2IE = 0;
	
	PIE3 = 0x00;					// Also interrupt AD converter is off
	
	IPR1 = 0x00;					//Timer 1 low priority (0x00), high when 0x01
	
	IPR2 = 0x00;					//All others low priority
	
	IPR3 = 0x00;					// All others low priority
	
	//Timer 1 for Fiddle Yard Update Tick
	
	TMR1H = 0x0;
	TMR1L = 0x0;
	T1CON = 0xB0; //0xB1 16 Bit and ON and 1:8 prescale              //T1CON = 0x81;					// 0xB1 = 1:8(40mS), 0xA1 = 1:4(22.5mS), 0x91 = 1:2(11mS), 0x81 = 1:1(5.5mS) 0x81 = 16Bit and enabled
    T4CON = 0x00; //Timer OFF
}

/******************************************************************************
 * Function:        Init_Timers
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        None
 *****************************************************************************/

void Init_Pwm()
{
	// PWM setup using 25mA outputs to drive opto's ECCP 3 and 1 are chosen for PWM
	T3CON = 0x00;			// Timer 2 and 4 are clock sources for all CCPx/ECCPx modules, tmr2 is used for PWM mode page 186, 0x60 for tmr 3 and 4, 0x00 for tmr 1 and 2
	
	T2CON = 0x05;			// Postscale 1:1, Timer2 On, Prescaler is 4		
	
	PR2=129;				// 129 = 20.03 kHz SYNC signal, 138 = 18.74 kHz SYNC signal to dsPIC   PWM Period TMR2 zelfde voor PWM1(ECCP1) en PWM2(ECCP3)
			
	
	CCPR3L = 1;             // 1 = 366 ns, 7 = SYNC pulse of 2.67 us to dsPIC     PWM Duty cycle PWM2
	CCP3CON = 0x0C;			// PWM Mode

}

/******************************************************************************
 * Function:        I2C1_Initialize
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        None
 *****************************************************************************/
char temp;
void I2C1_Initialize(){
    // Use MSSP2, the pins of this module are connected to RD5 and RD6, 
    // all calls to I2C lib must be nummerated with 2 : OpenI2C2																													   _																																	
    //---INITIALISE THE I2C MODULE FOR MASTER MODE WITH 100KHz ---		
    //400kHz Baud clock @41.667MHz = 0x19 // 100kHz Baud clock @41.667MHz = 0x67 // 1MHz Baud clock @41.667MHz = 0x09 // 1.7MHzBaud clock @41.667MHz = 0x05
    SSP2ADD= 0x19;
    //read any previous stored content in buffer to clear buffer full status   EXPNDNQ
    temp = SSP2BUF;
    OpenI2C2(MASTER,SLEW_OFF);
    T1CONbits.TMR1ON = 1; // start timer1 here after I2C is ready
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
