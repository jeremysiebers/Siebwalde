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

#define READ			0b00000001;
#define WRITE			0b00000000;
#define WRITE_REG		16			// Number of registers that have to be initialized on the IO expanders

//------------------------------------------------------------------------------------------------------------------------------------------------------------------//
rom unsigned char GREG[WRITE_REG]={IODIRA,IODIRB,IPOLA,IPOLB,GPINTENA,GPINTENB,DEFVALA,DEFVALB,INTCONA,INTCONB,IOCON,IOCON,GPPUA,GPPUB,GPIOA,GPIOB};

typedef struct
{
	unsigned char	GPORTA[8],
 					GPORTB[8];
}PORT;
PORT PORTS[EXPNDNQ]={

								{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}	//

#if EXPNDNQ >= 2													//								
								{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}	//
#endif																//

#if EXPNDNQ >= 3													//
								{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}	//
#endif																//

#if EXPNDNQ >= 4													//
								{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}	//
#endif																//

#if EXPNDNQ >= 5													//
								{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}	//
#endif																//

#if EXPNDNQ >= 6													//
								{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}	//
#endif																//

#if EXPNDNQ >= 7													//
								{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}	//
#endif																//

#if EXPNDNQ >= 8													//
								{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}	//
#endif																//
								
					};

typedef struct
{
	unsigned char 	GADDR,
					GDATA[WRITE_REG];
}IOEXPANDER;
rom IOEXPANDER IOEXP[EXPNDNQ]={	
														// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0xFE,				// GTRISA IOdirection (1 is input):
									0xFE,				// GTRISB IOdirection (1 is input):
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
									0xFF,				// GPIOA Port A initialize output values
									0xFF,	},			// GPIOB Port B initialize output values
														// End off intializer
									
								
														
#if EXPNDNQ >= 2										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0xFE,				// GTRISA IOdirection (1 is input):
									0xFE,				// GTRISB IOdirection (1 is input):
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
									0xFF,				// GPIOA Port A initialize output values
									0xFF,	},			// GPIOB Port B initialize output values
#endif													// End off intializer

														
#if EXPNDNQ >= 3										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0xFE,				// GTRISA IOdirection (1 is input):
									0xFE,				// GTRISB IOdirection (1 is input):
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
									0xFF,				// GPIOA Port A initialize output values
									0xFF,	},			// GPIOB Port B initialize output values
#endif													// End off intializer
														
#if EXPNDNQ >= 4										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0xFE,				// GTRISA IOdirection (1 is input):
									0xFE,				// GTRISB IOdirection (1 is input):
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
									0xFF,				// GPIOA Port A initialize output values
									0xFF,	},			// GPIOB Port B initialize output values
#endif													// End off intializer
														
#if EXPNDNQ >= 5										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0xFE,				// GTRISA IOdirection (1 is input):
									0xFE,				// GTRISB IOdirection (1 is input):
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
									0xFF,				// GPIOA Port A initialize output values
									0xFF,	},			// GPIOB Port B initialize output values
#endif													// End off intializer
														
#if EXPNDNQ >= 6										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0xFE,				// GTRISA IOdirection (1 is input):
									0xFE,				// GTRISB IOdirection (1 is input):
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
									0xFF,				// GPIOA Port A initialize output values
									0xFF,	},			// GPIOB Port B initialize output values
#endif													// End off intializer
														
#if EXPNDNQ >= 7										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0xFE,				// GTRISA IOdirection (1 is input):
									0xFE,				// GTRISB IOdirection (1 is input):
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
									0xFF,				// GPIOA Port A initialize output values
									0xFF,	},			// GPIOB Port B initialize output values
#endif													// End off intializer
														
#if EXPNDNQ >= 8										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed
									0x4E,				// Adress of first expander with write bit (0)
								{	0xFE,				// GTRISA IOdirection (1 is input):
									0xFE,				// GTRISB IOdirection (1 is input):
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
									0xFF,				// GPIOA Port A initialize output values
									0xFF,	},			// GPIOB Port B initialize output values
#endif													// End off intializer
									
					            };
//------------------------------------------------------------------------------------------------------------------------------------------------------------------//

enum {INIT, IDLE, ADDR, REG, DATA, START, STARTI2C, RSTRTI2C, STOPI2C, WRITE1, WRITE2, WRITE3};
static unsigned char data, CM = INIT, CM2 = INIT, Active_IOEXP, Active_REG, Active_DATA;
static unsigned char Return_Val = Busy, Return_Val_Routine;

//------------------------------------------------------------------------------------------------------------------------------------------------------------------//
void IOExpander(void)
{
	switch ( CM )
	{
		case	INIT	:	
							break;
		default			:	break;
	}
}

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
														
									case	WRITE3	:	CM = DATA;
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
								
			case	DATA	:	switch ( Return_Val_Routine = WriteI2C2 (IOEXP[Active_IOEXP].GDATA[Active_DATA]) )
								{
									case	Finished	:	CM = START;
															break;
									
									case	Busy		:	CM = DATA;
															break;
																															
									default				:	CM = DATA;
															break;
								}
								break;
								
			default			:	break;
		}
	}
}
