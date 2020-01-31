/*********************************************************************
/*********************************************************************
 *
 *	IO Expander c file
 *  
 *
 *********************************************************************
 * FileName:        IO_Expander.c
 * Dependencies:    IO_Expander.h
 *					i2c2.h
 *					Fiddle_Yard.h
 *					delays.h
 * Processor:       PIC18F97J60 Family
 * Compiler:        Microchip C18 v3.30 or higher
 * Company:         Siebers Technology, Inc.
 *
 * Author               Date   	 	Comment
 *~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * Jeremy siebers		19-01-2011	
 *********************************************************************/
#include <IO_Expander.h>			// IO Expander extra IO
#include <i2c2.h>					// I^2C lib adapted for cooperative multitasking
#include <Fiddle_Yard.h>			// To enherit the MCLR port of the attached io expanders and some definitions
#include <delays.h>					// delay routines

//------------------------------------------------------------------------------------------------------------------------------------------------------------------//
//Define here total number of IO expanders (8 max, 1 min)
#define EXPNDNQ			1

// GPIO Expander registers with adresses
//IOCON.BANK=0
#define IODIRA			0x00 // IO Direction
#define IODIRB			0x01 // IO Direction
#define IPOLA			0x02 // Polarity inverse of bit
#define IPOLB			0x03 // Polarity inverse of bit
#define GPINTENA		0x04 // Interrupt enable
#define GPINTENB		0x05 // Interrupt enable
#define DEFVALA			0x06 // Default comparison value
#define DEFVALB			0x07 // Default comparison value
#define INTCONA			0x08 // Interrupt config
#define INTCONB			0x09 // Interrupt config
#define IOCON			0x0A //#define IOCON 0x0B IOCON is shared between the two ports
#define GPPUA			0x0C // internall pull up
#define GPPUB			0x0D // internall pull up
#define INTFA			0x0E // interrupt flags read only
#define INTFB			0x0F // interrupt flags read only
#define INTCAPA			0x10 // Port value @ interrupt read only
#define INTCAPB			0x11 // Port value @ interrupt read only
#define GPIOA			0x12 // Port register write to modify port, read to read Port
#define GPIOB			0x13 // Port register write to modify port, read to read Port
#define OLATA			0x14 // Output latches, read does only read OLAT and not the port self, write modifies output latches
#define OLATB			0x15 // Output latches, read does only read OLAT and not the port self, write modifies output latches
//------------------------------------------------------------------------------------------------------------------------------------------------------------------//

#define READ			0b11111111;
#define WRITE			0b11111110;
#define WRITE_REG		16			// Number of registers that have to be initialized on the IO expanders

//------------------------------------------------------------------------------------------------------------------------------------------------------------------//
rom unsigned char GREG[WRITE_REG]={IODIRA,IODIRB,IPOLA,IPOLB,GPINTENA,GPINTENB,DEFVALA,DEFVALB,INTCONA,INTCONB,IOCON,IOCON,GPPUA,GPPUB,GPIOA,GPIOB};

typedef struct
{
	unsigned char 	GADDR,
					GDATA[WRITE_REG];
}IOEXPANDER;
IOEXPANDER IOEXP[EXPNDNQ]={	
														// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0x00,				// GTRISA IOdirection (1 is input):
									0x00,				// GTRISB IOdirection (1 is input):
									0x00,				// GPOLA IO Polarity (0 is same logic state of the input pin	
									0x00,				// GPOLB IO Polarity (0 is same logic state of the input pin	
									0x00,				// GINTA Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GINTB Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GDEFVLA Default compare for interrupt on change from defaults
									0x00,				// GDEFVLB Default compare for interrupt on change from defaults
									0x00,				// GINTCONA Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GINTCONB Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GIOCONA IO must be same as GIOCONB
									0x00,				// GIOCONB IO must be same as GIOCONA														
									0x00,				// GPPUEA Internall pull up register (0 is pull up disabled)
									0x00,				// GPPUEB Internall pull up register (0 is pull up disabled)
									0xFF,				// GPIOA Port A initialize output values and variable
									0xFF,	},			// GPIOB Port B initialize output values and variable
														// End off intializer
									
								
														
#if EXPNDNQ >= 2										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x00,				// Adress of first expander with write bit (0)
								{	0x00,				// GTRISA IOdirection (1 is input):
									0x00,				// GTRISB IOdirection (1 is input):
									0x00,				// GPOLA IO Polarity (0 is same logic state of the input pin	
									0x00,				// GPOLB IO Polarity (0 is same logic state of the input pin	
									0x00,				// GINTA Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GINTB Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GDEFVLA Default compare for interrupt on change from defaults
									0x00,				// GDEFVLB Default compare for interrupt on change from defaults
									0x00,				// GINTCONA Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GINTCONB Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GIOCONA IO must be same as GIOCONB
									0x00,				// GIOCONB IO must be same as GIOCONA														
									0x00,				// GPPUEA Internall pull up register (0 is pull up disabled)
									0x00,				// GPPUEB Internall pull up register (0 is pull up disabled)
									0x00,				// GPIOA Port A initialize output values and variable
									0x00,	},			// GPIOB Port B initialize output values and variable
#endif													// End off intializer

														
#if EXPNDNQ >= 3										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x00,				// Adress of first expander with write bit (0)
								{	0x00,				// GTRISA IOdirection (1 is input):
									0x00,				// GTRISB IOdirection (1 is input):
									0x00,				// GPOLA IO Polarity (0 is same logic state of the input pin	
									0x00,				// GPOLB IO Polarity (0 is same logic state of the input pin	
									0x00,				// GINTA Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GINTB Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GDEFVLA Default compare for interrupt on change from defaults
									0x00,				// GDEFVLB Default compare for interrupt on change from defaults
									0x00,				// GINTCONA Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GINTCONB Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GIOCONA IO must be same as GIOCONB
									0x00,				// GIOCONB IO must be same as GIOCONA														
									0x00,				// GPPUEA Internall pull up register (0 is pull up disabled)
									0x00,				// GPPUEB Internall pull up register (0 is pull up disabled)
									0x00,				// GPIOA Port A initialize output values and variable
									0x00,	},			// GPIOB Port B initialize output values and variable
#endif													// End off intializer
														
#if EXPNDNQ >= 4										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x00,				// Adress of first expander with write bit (0)
								{	0x00,				// GTRISA IOdirection (1 is input):
									0x00,				// GTRISB IOdirection (1 is input):
									0x00,				// GPOLA IO Polarity (0 is same logic state of the input pin	
									0x00,				// GPOLB IO Polarity (0 is same logic state of the input pin	
									0x00,				// GINTA Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GINTB Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GDEFVLA Default compare for interrupt on change from defaults
									0x00,				// GDEFVLB Default compare for interrupt on change from defaults
									0x00,				// GINTCONA Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GINTCONB Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GIOCONA IO must be same as GIOCONB
									0x00,				// GIOCONB IO must be same as GIOCONA														
									0x00,				// GPPUEA Internall pull up register (0 is pull up disabled)
									0x00,				// GPPUEB Internall pull up register (0 is pull up disabled)
									0x00,				// GPIOA Port A initialize output values and variable
									0x00,	},			// GPIOB Port B initialize output values and variable
#endif													// End off intializer
														
#if EXPNDNQ >= 5										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x00,				// Adress of first expander with write bit (0)
								{	0x00,				// GTRISA IOdirection (1 is input):
									0x00,				// GTRISB IOdirection (1 is input):
									0x00,				// GPOLA IO Polarity (0 is same logic state of the input pin	
									0x00,				// GPOLB IO Polarity (0 is same logic state of the input pin	
									0x00,				// GINTA Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GINTB Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GDEFVLA Default compare for interrupt on change from defaults
									0x00,				// GDEFVLB Default compare for interrupt on change from defaults
									0x00,				// GINTCONA Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GINTCONB Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GIOCONA IO must be same as GIOCONB
									0x00,				// GIOCONB IO must be same as GIOCONA														
									0x00,				// GPPUEA Internall pull up register (0 is pull up disabled)
									0x00,				// GPPUEB Internall pull up register (0 is pull up disabled)
									0x00,				// GPIOA Port A initialize output values and variable
									0x00,	},			// GPIOB Port B initialize output values and variable
#endif													// End off intializer
														
#if EXPNDNQ >= 6										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x00,				// Adress of first expander with write bit (0)
								{	0x00,				// GTRISA IOdirection (1 is input):
									0x00,				// GTRISB IOdirection (1 is input):
									0x00,				// GPOLA IO Polarity (0 is same logic state of the input pin	
									0x00,				// GPOLB IO Polarity (0 is same logic state of the input pin	
									0x00,				// GINTA Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GINTB Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GDEFVLA Default compare for interrupt on change from defaults
									0x00,				// GDEFVLB Default compare for interrupt on change from defaults
									0x00,				// GINTCONA Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GINTCONB Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GIOCONA IO must be same as GIOCONB
									0x00,				// GIOCONB IO must be same as GIOCONA														
									0x00,				// GPPUEA Internall pull up register (0 is pull up disabled)
									0x00,				// GPPUEB Internall pull up register (0 is pull up disabled)
									0x00,				// GPIOA Port A initialize output values and variable
									0x00,	},			// GPIOB Port B initialize output values and variable
#endif													// End off intializer
														
#if EXPNDNQ >= 7										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x00,				// Adress of first expander with write bit (0)
								{	0x00,				// GTRISA IOdirection (1 is input):
									0x00,				// GTRISB IOdirection (1 is input):
									0x00,				// GPOLA IO Polarity (0 is same logic state of the input pin	
									0x00,				// GPOLB IO Polarity (0 is same logic state of the input pin	
									0x00,				// GINTA Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GINTB Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GDEFVLA Default compare for interrupt on change from defaults
									0x00,				// GDEFVLB Default compare for interrupt on change from defaults
									0x00,				// GINTCONA Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GINTCONB Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GIOCONA IO must be same as GIOCONB
									0x00,				// GIOCONB IO must be same as GIOCONA														
									0x00,				// GPPUEA Internall pull up register (0 is pull up disabled)
									0x00,				// GPPUEB Internall pull up register (0 is pull up disabled)
									0x00,				// GPIOA Port A initialize output values and variable
									0x00,	},			// GPIOB Port B initialize output values and variable
#endif													// End off intializer
														
#if EXPNDNQ >= 8										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x00,				// Adress of first expander with write bit (0)
								{	0x00,				// GTRISA IOdirection (1 is input):
									0x00,				// GTRISB IOdirection (1 is input):
									0x00,				// GPOLA IO Polarity (0 is same logic state of the input pin	
									0x00,				// GPOLB IO Polarity (0 is same logic state of the input pin	
									0x00,				// GINTA Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GINTB Interrupt enable	(0 is disable interrupt on change event)
									0x00,				// GDEFVLA Default compare for interrupt on change from defaults
									0x00,				// GDEFVLB Default compare for interrupt on change from defaults
									0x00,				// GINTCONA Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GINTCONB Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x00,				// GIOCONA IO must be same as GIOCONB
									0x00,				// GIOCONB IO must be same as GIOCONA														
									0x00,				// GPPUEA Internall pull up register (0 is pull up disabled)
									0x00,				// GPPUEB Internall pull up register (0 is pull up disabled)
									0x00,				// GPIOA Port A initialize output values and variable
									0x00,	},			// GPIOB Port B initialize output values and variable
#endif													// End off intializer
									
					            };


#if EXPNDNQ >= 1
 #define DIR_K	IOEXP[0].GDATA[0]
 #define PORTK	IOEXP[0].GDATA[14]
 #define DIR_L	IOEXP[0].GDATA[1]
 #define PORTL	IOEXP[0].GDATA[15]
#endif
#if EXPNDNQ >= 2
 #define DIR_M	IOEXP[1].GDATA[0]
 #define PORTM	IOEXP[1].GDATA[14]
 #define DIR_N	IOEXP[1].GDATA[1]
 #define PORTN	IOEXP[1].GDATA[15]
#endif
#if EXPNDNQ >= 3
 #define DIR_O	IOEXP[2].GDATA[0]
 #define PORTO	IOEXP[2].GDATA[14]
 #define DIR_P	IOEXP[2].GDATA[1]
 #define PORTP	IOEXP[2].GDATA[15]
#endif
#if EXPNDNQ >= 4
 #define DIR_Q	IOEXP[3].GDATA[0]
 #define PORTQ	IOEXP[3].GDATA[14]
 #define DIR_R	IOEXP[3].GDATA[1]
 #define PORTR	IOEXP[3].GDATA[15]
#endif
#if EXPNDNQ >= 5
 #define DIR_S	IOEXP[4].GDATA[0]
 #define PORTS	IOEXP[4].GDATA[14]
 #define DIR_T	IOEXP[4].GDATA[1]
 #define PORTT	IOEXP[4].GDATA[15]
#endif
#if EXPNDNQ >= 6
 #define DIR_U	IOEXP[5].GDATA[0]
 #define PORTU	IOEXP[5].GDATA[14]
 #define DIR_V	IOEXP[5].GDATA[1]
 #define PORTV	IOEXP[5].GDATA[15]
#endif
#if EXPNDNQ >= 7
 #define DIR_W	IOEXP[6].GDATA[0]
 #define PORTW	IOEXP[6].GDATA[14]
 #define DIR_X	IOEXP[6].GDATA[1]
 #define PORTX	IOEXP[6].GDATA[15]
#endif
#if EXPNDNQ >= 8
 #define DIR_Y	IOEXP[7].GDATA[0]
 #define PORTY	IOEXP[7].GDATA[14]
 #define DIR_Z	IOEXP[7].GDATA[1]
 #define PORTZ	IOEXP[7].GDATA[15]
#endif

					            
//------------------------------------------------------------------------------------------------------------------------------------------------------------------//

typedef enum {INIT, IDLE, ADDR, REG, DATAOUT, DATAIN, START, STARTI2C, RSTRTI2C, STOPI2C, WRITE1, WRITE2, WRITE3, NOPP};

static unsigned char data = 0, CMI = INIT, CMI2 = INIT, Active_IOEXP = 0, Active_REG = 0, Active_DATA = 0, Active_PORT = 0,
					           CMR = INIT, CMR2 = INIT; 
														
static unsigned char Return_Val = Busy, Return_Val_Routine, IOExpander_Updater = False;
static unsigned int Return_Val_Routine2;


//------------------------------------------------------------------------------------------------------------------------------------------------------------------//

/******************************************************************************
 * Function:        void IOExpander_Update(unsigned char Update)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        MACInit enables the Ethernet module, waits for the
 *                  to become ready, and programs all registers for future
 *                  TX/RX operations.
 *
 * Note:            This function blocks for at least 1ms, waiting for the
 *                  hardware to stabilize.
 *****************************************************************************/
void IOExpander_Update(unsigned char Update)
{
	IOExpander_Updater = Update;
}

/******************************************************************************
 * Function:        void IOExpander(void)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        MACInit enables the Ethernet module, waits for the
 *                  to become ready, and programs all registers for future
 *                  TX/RX operations.
 *
 * Note:            This function blocks for at least 1ms, waiting for the
 *                  hardware to stabilize.
 *****************************************************************************/
void IOExpander(void)
{	
	switch ( CMR )
	{
		case	INIT	:	//init Active_IOEXP with te number of total IOexpanders
							Active_IOEXP = 0;
							CMR2 = INIT;
							CMR = NOPP;
							PORTK = 0x00;
							PORTL = 0x00;
							break;
								
		case	START	:	switch ( CMR2 )
							{
								case	INIT	:	if ( (Active_IOEXP >= EXPNDNQ))
													{
														CMR = NOPP;
														CMR2 = INIT;
														Active_IOEXP = 0;
														break;
													}
													Active_REG 		= 0x00;
													Active_DATA 	= 0x00;
													Active_PORT 	= 0x00;
													CMR2			= IDLE;
													break;
														
									case	IDLE	:	if ( Active_PORT >= 2 )
														{
															CMR2 = INIT;
															Active_IOEXP++;
															break;
														}
														CMR = IDLE;
														CMR2 = STARTI2C;
														break;
														
									case	STARTI2C:	StartI2C2();
														CMR2 = WRITE1;
														break;
														
									case	WRITE1	:	if (IOEXP[Active_IOEXP].GDATA[Active_PORT]== 0xFF)
														{
															IOEXP[Active_IOEXP].GADDR &= READ;
														}
														else
														{
															IOEXP[Active_IOEXP].GADDR &= WRITE;
														}
														CMR = ADDR;
														CMR2 = WRITE2;
														break;
														
									case	WRITE2	:	if ( Active_PORT )
														{
															Active_REG = 15;
														}
														else
														{
															Active_REG = 14;
														}
														CMR = REG;
														CMR2 = WRITE3;
														break;
														
									case	WRITE3	:	switch ( Active_PORT )
														{
															case	0	:	if ( IOEXP[Active_IOEXP].GDATA[Active_PORT]== 0xFF )
																			{
																				Active_DATA = 14;
																				CMR = DATAIN;
																				CMR2 = STOPI2C;
																				break;
																			}
																			else
																			{
																				Active_DATA = 14;
																				CMR = DATAOUT;
																				CMR2 = STOPI2C;
																				break;
																			}
																																						
															case	1	:	if ( IOEXP[Active_IOEXP].GDATA[Active_PORT]== 0xFF )
																			{
																				Active_DATA = 15;
																				CMR = DATAIN;
																				CMR2 = STOPI2C;
																				break;
																			}
																			else
																			{
																				Active_DATA = 15;
																				CMR = DATAOUT;
																				CMR2 = STOPI2C;
																				break;
																			}
																			
															default		:	break;
														}
														break;
														
									case	STOPI2C:	Active_PORT++;
														StopI2C2();
														CMR2 = RSTRTI2C;
														break;
														
									case	RSTRTI2C:	RestartI2C2();
														CMR2 = IDLE;
														break;
														
									default			:	break;
								}
								break;
								
			case	IDLE	:	switch ( Return_Val_Routine = IdleI2C2() )								//check for bus idle condition in multi master communication
								{
									case	Finished	:	CMR = START;
															break;
															
									case	Busy		:	CMR = IDLE;
															break;
																
									default				:	CMR = IDLE;
															break;
								}
								break;
								
			case	ADDR	:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GADDR) )
								{
									case	Finished	:	CMR = START;
															break;
									
									case	Busy		:	CMR = ADDR;
															break;
																															
									default				:	CMR = ADDR;
															break;
								}
								break;
								
			case	REG	:	switch ( Return_Val_Routine = WriteI2C2 (GREG[Active_REG]) )
								{
									case	Finished	:	CMR = START;
															break;
									
									case	Busy		:	CMR = REG;
															break;
																															
									default				:	CMR = REG;
															break;
								}
								break;
								
			case	DATAOUT:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GDATA[Active_DATA]) )
								{
									case	Finished	:	CMR = START;
															break;
									
									case	Busy		:	CMR = DATAOUT;
															break;
																															
									default				:	CMR = DATAOUT;
															break;
								}
								break;
								
			case	DATAIN	:	switch ( Return_Val_Routine2 = ReadI2C2 () )
								{
									case	Busy		:	CMR = DATAIN;
															break;
																															
									default				:	IOEXP[Active_IOEXP].GDATA[Active_DATA] = Return_Val_Routine2;
															CMR = START;
															break;
								}
								break;
								
			case	NOPP	:	if ( IOExpander_Updater == True )
								{
									CMR = START;
									IOExpander_Updater = True;
									//Led5 = !Led5;
									PORTK = !PORTK;
									PORTL = !PORTL;
									break;
								}
								IOExpander_Updater = True;
								break;
								
			default			:	break;
	}
		
}

/******************************************************************************
 * Function:        void Init_IOExpander(void)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        MACInit enables the Ethernet module, waits for the
 *                  to become ready, and programs all registers for future
 *                  TX/RX operations.
 *
 * Note:            This function blocks for at least 1ms, waiting for the
 *                  hardware to stabilize.
 *****************************************************************************/
void Init_IOExpander(void)
{
	while (Return_Val == Busy)
	{
		switch ( CMI )
		{
			case	INIT	:	// Clear MCLR pins of the IOexpanders
								IO_Expander_Enable = True;
								
								// Give the IO expander some time after de-reset
								Delay1KTCYx(10);
								
								// Use MSSP2 the pins of this module are connected to RD5 and RD6, 
								// all calls to I2C lib must be nummerated with 2 : OpenI2C2																													   _																																	
								//---INITIALISE THE I2C MODULE FOR MASTER MODE WITH 100KHz ---							
								OpenI2C2(MASTER,SLEW_ON);
								
								//400kHz Baud clock @41.667MHz = 0x19 // 100kHz Baud clock @41.667MHz = 0x67 // 1MHz Baud clock @41.667MHz = 0x09 // 1.7MHzBaud clock @41.667MHz = 0x05
								SSP2ADD=0x09;
								
								//read any previous stored content in buffer to clear buffer full status   EXPNDNQ
								data = SSP2BUF;	
								
								//init Active_IOEXP with te number of total IOexpanders
								Active_IOEXP = 0;
								
								CMI = START;
								break;
								
			case	START	:	switch ( CMI2 )
								{
									case	INIT	:	if ( (Active_IOEXP >= EXPNDNQ))
														{
															Return_Val = Finished;
															CMI = INIT;
															CMI2 = INIT;
															Active_IOEXP = 0;
															break;
														}
														Active_REG 		= 0;
														Active_DATA 	= 0;
														CMI2				= IDLE;
														break;
														
									case	IDLE	:	if ( Active_REG >= WRITE_REG )
														{
															CMI2 = INIT;
															Active_IOEXP++;
															break;
														}
														CMI = IDLE;
														CMI2 = STARTI2C;
														break;
														
									case	STARTI2C:	StartI2C2();
														CMI2 = WRITE1;
														break;
														
									case	WRITE1	:	CMI = ADDR;
														CMI2 = WRITE2;
														break;
														
									case	WRITE2	:	CMI = REG;
														CMI2 = WRITE3;
														break;
														
									case	WRITE3	:	CMI = DATAOUT;
														CMI2 = STOPI2C;
														break;
														
									case	STOPI2C	:	Active_REG++;
														Active_DATA++;
														StopI2C2();
														CMI2 = RSTRTI2C;
														break;
														
									case	RSTRTI2C:	RestartI2C2();
														CMI2 = IDLE;
														break;
														
									default			:	break;
								}
								break;
								
			case	IDLE	:	switch ( Return_Val_Routine = IdleI2C2() )								//check for bus idle condition in multi master communication
								{
									case	Finished	:	CMI = START;
															break;
															
									case	Busy		:	CMI = IDLE;
															break;
																
									default				:	CMI = IDLE;
															break;
								}
								break;
								
			case	ADDR	:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GADDR) )
								{
									case	Finished	:	CMI = START;
															break;
									
									case	Busy		:	CMI = ADDR;
															break;
																															
									default				:	CMI = ADDR;
															break;
								}
								break;
								
			case	REG		:	switch ( Return_Val_Routine = WriteI2C2 (GREG[Active_REG]) )
								{
									case	Finished	:	CMI = START;
															break;
									
									case	Busy		:	CMI = REG;
															break;
																															
									default				:	CMI = REG;
															break;
								}
								break;
								
			case	DATAOUT	:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GDATA[Active_DATA]) )
								{
									case	Finished	:	CMI = START;
															break;
									
									case	Busy		:	CMI = DATAOUT;
															break;
																															
									default				:	CMI = DATAOUT;
															break;
								}
								break;
								
			default			:	break;
		}
	}
}



