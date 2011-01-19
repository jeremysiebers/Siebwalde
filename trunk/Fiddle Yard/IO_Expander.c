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

enum {INIT, IDLE, ADDR, REG, DATAOUT, DATAIN, START, STARTI2C, RSTRTI2C, STOPI2C, WRITE1, WRITE2, WRITE3, INIT1, IDLE1, ADDR1, REG1, DATAOUT1, DATAIN1, START1, STARTI2C1, RSTRTI2C1, STOPI2C1, WRITE11, WRITE21, WRITE31};
//enum {INIT1, IDLE1, ADDR1, REG1, DATAOUT1 DATAIN1, START1, STARTI2C1, RSTRTI2C1, STOPI2C1, WRITE11, WRITE21, WRITE31};

static unsigned char data = 0, CM = INIT, CM2 = INIT, Active_IOEXP = 0, Active_REG = 0, Active_DATA = 0, Active_PORT = 0;
static unsigned char CM11 = INIT1, CM21 = INIT1;
static unsigned char Return_Val = Busy, Return_Val_Routine;
static unsigned int Return_Val_Routine2;

//------------------------------------------------------------------------------------------------------------------------------------------------------------------//

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
	switch ( CM11 )
	{
		case	INIT1	:	//init Active_IOEXP with te number of total IOexpanders
							Active_IOEXP = 0;
							CM21 = INIT1;
							CM11 = START1;
							PORTK = 0x00;
							PORTL = 0x00;
							break;
								
		case	START1	:	switch ( CM21 )
							{
								case	INIT1	:	if ( (Active_IOEXP >= EXPNDNQ))
													{
														CM11 = START1;
														CM21 = INIT1;
														Active_IOEXP = 0;
														break;
													}
													Active_REG 		= 0x00;
													Active_DATA 	= 0x00;
													Active_PORT 	= 0x00;
													CM21				= IDLE1;
													break;
														
									case	IDLE1	:	if ( Active_PORT >= 1 )
														{
															CM21 = INIT1;
															Active_IOEXP++;
															break;
														}
														CM11 = IDLE1;
														CM21 = STARTI2C1;
														break;
														
									case	STARTI2C1:	StartI2C2();
														CM21 = WRITE11;
														break;
														
									case	WRITE11	:	if (IOEXP[Active_IOEXP].GDATA[Active_PORT]== 0xFF)
														{
															IOEXP[Active_IOEXP].GADDR &= READ;
														}
														else
														{
															IOEXP[Active_IOEXP].GADDR &= WRITE;
														}
														CM11 = ADDR1;
														CM21 = WRITE21;
														break;
														
									case	WRITE21	:	if ( Active_PORT )
														{
															Active_REG = GPIOB;
														}
														else
														{
															Active_REG = GPIOA;
														}
														CM11 = REG1;
														CM21 = WRITE31;
														break;
														
									case	WRITE31	:	switch ( Active_PORT )
														{
															case	0	:	if ( IOEXP[Active_IOEXP].GDATA[Active_PORT]== 0xFF )
																			{
																				Active_DATA = 14;
																				CM11 = DATAIN1;
																				CM21 = STOPI2C1;
																				break;
																			}
																			else
																			{
																				Active_DATA = 14;
																				CM11 = DATAOUT1;
																				CM21 = STOPI2C1;
																				break;
																			}
																																						
															case	1	:	if ( IOEXP[Active_IOEXP].GDATA[Active_PORT]== 0xFF )
																			{
																				Active_DATA = 15;
																				CM11 = DATAIN1;
																				CM21 = STOPI2C1;
																				break;
																			}
																			else
																			{
																				Active_DATA = 15;
																				CM11 = DATAOUT1;
																				CM21 = STOPI2C1;
																				break;
																			}
																			
															default		:	break;
														}
														break;
														
									case	STOPI2C1:	Active_PORT++;
														StopI2C2();
														CM21 = RSTRTI2C1;
														break;
														
									case	RSTRTI2C1:	RestartI2C2();
														CM21 = IDLE1;
														break;
														
									default			:	break;
								}
								break;
								
			case	IDLE1	:	switch ( Return_Val_Routine = IdleI2C2() )								//check for bus idle condition in multi master communication
								{
									case	Finished	:	CM11 = START1;
															break;
															
									case	Busy		:	CM11 = IDLE1;
															break;
																
									default				:	CM11 = IDLE1;
															break;
								}
								break;
								
			case	ADDR1	:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GADDR) )
								{
									case	Finished	:	CM11 = START1;
															break;
									
									case	Busy		:	CM11 = ADDR1;
															break;
																															
									default				:	CM11 = ADDR1;
															break;
								}
								break;
								
			case	REG1	:	switch ( Return_Val_Routine = WriteI2C2 (GREG[Active_REG]) )
								{
									case	Finished	:	CM = START;
															break;
									
									case	Busy		:	CM = REG;
															break;
																															
									default				:	CM = REG;
															break;
								}
								break;
								
			case	DATAOUT1:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GDATA[Active_DATA]) )
								{
									case	Finished	:	CM11 = START1;
															break;
									
									case	Busy		:	CM11 = DATAOUT1;
															break;
																															
									default				:	CM11 = DATAOUT1;
															break;
								}
								break;
								
			case	DATAIN1	:	switch ( Return_Val_Routine2 = ReadI2C2 () )
								{
									case	Busy		:	CM11 = DATAIN1;
															break;
																															
									default				:	IOEXP[Active_IOEXP].GDATA[Active_DATA] = Return_Val_Routine2;
															CM11 = START1;
															break;
								}
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
		switch ( CM )
		{
			case	INIT	:	// Clear MCLR pins of the IOexpanders
								IO_Expander_Enable = True;
								
								// Give the IO expander some time after de-reset
								Delay1KTCYx(10);
								
								// Use MSSP2 the pins of this module are connected to RD5 and RD6, 
								// all calls to I2C lib must be nummerated with 2 : OpenI2C2																													   _																																	
								//---INITIALISE THE I2C MODULE FOR MASTER MODE WITH 100KHz ---							
								OpenI2C2(MASTER,SLEW_ON);
								
								//400kHz Baud clock @41.667MHz = 0x19 // 100kHz Baud clock @41.667MHz = 0x67
								SSP2ADD=0x19;
								
								//read any previous stored content in buffer to clear buffer full status   EXPNDNQ
								data = SSP2BUF;	
								
								//init Active_IOEXP with te number of total IOexpanders
								Active_IOEXP = 0;
								
								CM = START;
								break;
								
			case	START	:	switch ( CM2 )
								{
									case	INIT	:	if ( (Active_IOEXP >= EXPNDNQ))
														{
															Return_Val = Finished;
															CM = INIT;
															CM2 = INIT;
															Active_IOEXP = 0;
															break;
														}
														Active_REG 		= 0;
														Active_DATA 	= 0;
														CM2				= IDLE;
														break;
														
									case	IDLE	:	if ( Active_REG >= WRITE_REG )
														{
															CM2 = INIT;
															Active_IOEXP++;
															break;
														}
														CM = IDLE;
														CM2 = STARTI2C;
														break;
														
									case	STARTI2C:	StartI2C2();
														CM2 = WRITE1;
														break;
														
									case	WRITE1	:	CM = ADDR;
														CM2 = WRITE2;
														break;
														
									case	WRITE2	:	CM = REG;
														CM2 = WRITE3;
														break;
														
									case	WRITE3	:	CM = DATAOUT;
														CM2 = STOPI2C;
														break;
														
									case	STOPI2C	:	Active_REG++;
														Active_DATA++;
														StopI2C2();
														CM2 = RSTRTI2C;
														break;
														
									case	RSTRTI2C:	RestartI2C2();
														CM2 = IDLE;
														break;
														
									default			:	break;
								}
								break;
								
			case	IDLE	:	switch ( Return_Val_Routine = IdleI2C2() )								//check for bus idle condition in multi master communication
								{
									case	Finished	:	CM = START;
															break;
															
									case	Busy		:	CM = IDLE;
															break;
																
									default				:	CM = IDLE;
															break;
								}
								break;
								
			case	ADDR	:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GADDR) )
								{
									case	Finished	:	CM = START;
															break;
									
									case	Busy		:	CM = ADDR;
															break;
																															
									default				:	CM = ADDR;
															break;
								}
								break;
								
			case	REG		:	switch ( Return_Val_Routine = WriteI2C2 (GREG[Active_REG]) )
								{
									case	Finished	:	CM = START;
															break;
									
									case	Busy		:	CM = REG;
															break;
																															
									default				:	CM = REG;
															break;
								}
								break;
								
			case	DATAOUT	:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GDATA[Active_DATA]) )
								{
									case	Finished	:	CM = START;
															break;
									
									case	Busy		:	CM = DATAOUT;
															break;
																															
									default				:	CM = DATAOUT;
															break;
								}
								break;
								
			default			:	break;
		}
	}
}
