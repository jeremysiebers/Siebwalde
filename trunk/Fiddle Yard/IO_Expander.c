#include <i2c2.h>					// I^2C lib adapted for cooperative multitasking
#include <Fiddle_Yard.h>			// To enherit the MCLR port of the attached io expanders and some definitions

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

//------------------------------------------------------------------------------------------------------------------------------------------------------------------//
typedef struct
{
	unsigned char 	GTRISA, GTRISB,
					GPOLA, GPOLB,
					GINTA, GINTB,
					GDEFVLA, GDEFVLB,
					GINTCONA, GINTCONB,
					GIOCON,
					GPPUEA, GPPUEB,
					GADDR;					
}IOEXPANDER;
rom IOEXPANDER IOEXP[EXPNDNQ]={	
														// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed														
									0xFE,0xFE,			// GTRISA GTRISB IOdirection (1 is input):
									0,0,				// GPOLA GPOLB IO Polarity (0 is same logic state of the input pin	
									0,0,				// GINTA GINTB Interrupt enable	(0 is disable interrupt on change event)
									0,0,				// GDEFVLA GDEFVLB Default compare for interrupt on change from defaults
									0,0,				// GINTCONA & B Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x73,				// GIOCON IO 	
									0,0,				// GPPUEA GPPUEB Internall pull up register (0 is pull up disabled)
									0x4E,				// Adress of first expander with write bit (0)
														// End off intializer
														
#if EXPNDNQ >= 2										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed														
									0xFE,0xFE,			// GTRISA GTRISB IOdirection (1 is input):
									0,0,				// GPOLA GPOLB IO Polarity (0 is same logic state of the input pin	
									0,0,				// GINTA GINTB Interrupt enable	(0 is disable interrupt on change event)
									0,0,				// GDEFVLA GDEFVLB Default compare for interrupt on change from defaults
									0,0,				// GINTCONA & B Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x73,				// GIOCON IO 	
									0,0,				// GPPUEA GPPUEB Internall pull up register (0 is pull up disabled)
									0x4E,				// Adress of first expander with write bit (0)
#endif													// End off intializer

														
#if EXPNDNQ >= 3										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed														
									0xFE,0xFE,			// GTRISA GTRISB IOdirection (1 is input):
									0,0,				// GPOLA GPOLB IO Polarity (0 is same logic state of the input pin	
									0,0,				// GINTA GINTB Interrupt enable	(0 is disable interrupt on change event)
									0,0,				// GDEFVLA GDEFVLB Default compare for interrupt on change from defaults
									0,0,				// GINTCONA & B Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x73,				// GIOCON IO 	
									0,0,				// GPPUEA GPPUEB Internall pull up register (0 is pull up disabled)
									0x4E,				// Adress of first expander with write bit (0)
#endif													// End off intializer
														
#if EXPNDNQ >= 4										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed														
									0xFE,0xFE,			// GTRISA GTRISB IOdirection (1 is input):
									0,0,				// GPOLA GPOLB IO Polarity (0 is same logic state of the input pin	
									0,0,				// GINTA GINTB Interrupt enable	(0 is disable interrupt on change event)
									0,0,				// GDEFVLA GDEFVLB Default compare for interrupt on change from defaults
									0,0,				// GINTCONA & B Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x73,				// GIOCON IO 	
									0,0,				// GPPUEA GPPUEB Internall pull up register (0 is pull up disabled)
									0x4E,				// Adress of first expander with write bit (0)
#endif													// End off intializer
														
#if EXPNDNQ >= 5										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed														
									0xFE,0xFE,			// GTRISA GTRISB IOdirection (1 is input):
									0,0,				// GPOLA GPOLB IO Polarity (0 is same logic state of the input pin	
									0,0,				// GINTA GINTB Interrupt enable	(0 is disable interrupt on change event)
									0,0,				// GDEFVLA GDEFVLB Default compare for interrupt on change from defaults
									0,0,				// GINTCONA & B Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x73,				// GIOCON IO 	
									0,0,				// GPPUEA GPPUEB Internall pull up register (0 is pull up disabled)
									0x4E,				// Adress of first expander with write bit (0)
#endif													// End off intializer
														
#if EXPNDNQ >= 6										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed														
									0xFE,0xFE,			// GTRISA GTRISB IOdirection (1 is input):
									0,0,				// GPOLA GPOLB IO Polarity (0 is same logic state of the input pin	
									0,0,				// GINTA GINTB Interrupt enable	(0 is disable interrupt on change event)
									0,0,				// GDEFVLA GDEFVLB Default compare for interrupt on change from defaults
									0,0,				// GINTCONA & B Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x73,				// GIOCON IO 	
									0,0,				// GPPUEA GPPUEB Internall pull up register (0 is pull up disabled)
									0x4E,				// Adress of first expander with write bit (0)
#endif													// End off intializer
														
#if EXPNDNQ >= 7										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed														
									0xFE,0xFE,			// GTRISA GTRISB IOdirection (1 is input):
									0,0,				// GPOLA GPOLB IO Polarity (0 is same logic state of the input pin	
									0,0,				// GINTA GINTB Interrupt enable	(0 is disable interrupt on change event)
									0,0,				// GDEFVLA GDEFVLB Default compare for interrupt on change from defaults
									0,0,				// GINTCONA & B Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x73,				// GIOCON IO 	
									0,0,				// GPPUEA GPPUEB Internall pull up register (0 is pull up disabled)
									0x4E,				// Adress of first expander with write bit (0)
#endif													// End off intializer
														
#if EXPNDNQ >= 8										// This initializer must be repeated as many as is specified bij EXPNDNQ and configured as needed														
									0xFE,0xFE,			// GTRISA GTRISB IOdirection (1 is input):
									0,0,				// GPOLA GPOLB IO Polarity (0 is same logic state of the input pin	
									0,0,				// GINTA GINTB Interrupt enable	(0 is disable interrupt on change event)
									0,0,				// GDEFVLA GDEFVLB Default compare for interrupt on change from defaults
									0,0,				// GINTCONA & B Interrupt on change control register (0 pin is compared against previous pin value, 1 -> Default)
									0x73,				// GIOCON IO 	
									0,0,				// GPPUEA GPPUEB Internall pull up register (0 is pull up disabled)
									0x4E,				// Adress of first expander with write bit (0)
#endif													// End off intializer
									
					            };
//------------------------------------------------------------------------------------------------------------------------------------------------------------------//
enum {INIT, IDLE, ADDR, REG, DATA, START};
static char add1,w,data,status,length,CM=INIT,Return_Caller=2;

//------------------------------------------------------------------------------------------------------------------------------------------------------------------//
void Init_IOExpander(void)
{
	static char Return_Val = Busy;
	
	// Clear MCLR pins of the IOexpanders
	IO_Expander_Enable = True;							
	
	// Use MSSP2 the pins of this module are connected to RD5 and RD6, 
	// all calls to I2C lib must be nummerated with 2 : OpenI2C2																													   _																																	
	//---INITIALISE THE I2C MODULE FOR MASTER MODE WITH 100KHz ---							
	OpenI2C2(MASTER,SLEW_OFF);
	
	//400kHz Baud clock @41.667MHz = 0x19 // 100kHz Baud clock @41.667MHz = 0x67
	SSP2ADD=0x67;
	
	//read any previous stored content in buffer to clear buffer full status
	data = SSP2BUF;
	
	while (Return_Val == Busy)
	{
		switch ( CM )
		{
			case	INIT	:	
								Return_Val = Busy;
								CM = START;
								break;
								
			case	IDLE	:	switch ( IdleI2C2() )								//check for bus idle condition in multi master communication
								{
									case	Finished	:	CM = Return_Caller;
															Return_Val = Busy;
															break;
															
									case	Busy		:	CM = IDLE;
															Return_Val = Busy;
															break;
																
									default				:	Return_Val = Busy;
															break;
								}
								break;
								
			case	ADDR	:	switch ( WriteI2C2(IOEXP[0].GADDR) )
								{
									case	Finished	:	CM = Return_Caller;
															break;
									
									case	Busy		:	CM = ADDR;
															break;
																															
									default				:	CM = ADDR;
															break;
								}
								
			case	REG		:	switch ( WriteI2C2(IOCON) )
								{
									case	Finished	:	CM = Return_Caller;
															break;
									
									case	Busy		:	CM = REG;
															break;
																															
									default				:	CM = REG;
															break;
								}
								
			case	DATA	:	switch ( WriteI2C2(IOEXP[0].GIOCON) )
								{
									case	Finished	:	CM = Return_Caller;
															break;
									
									case	Busy		:	CM = DATA;
															break;
																															
									default				:	CM = DATA;
															break;
								}
								
			case	START	:	StopI2C2();    
	    						StartI2C2();
	    						CM = 16;
	    						break;
	    		
			default			:	break;
		}
	}
		
	/*
    do
    {
    status = WriteI2C2( add1 | 0x00 );    //write the address of slave
        if(status == -1)        //check if bus collision happened
        {
            data = SSP2BUF;        //upon bus collision detection clear the buffer,
            SSP2CON1bits.WCOL=0;    // clear the bus collision status bit
        }
    }
    while(status!=0);        //write untill successful communication
    
    do
    {
    status = WriteI2C2( IODIRA );    //write the address register!! of slave
        if(status == -1)        //check if bus collision happened
        {
            data = SSP2BUF;        //upon bus collision detection clear the buffer,
            SSP2CON1bits.WCOL=0;    // clear the bus collision status bit
        }
    }
    while(status!=0);        //write untill successful communication
    
    do
    {
    status = WriteI2C2( IOEXP[0].GTRISA );    //write the address register value of slave
        if(status == -1)        //check if bus collision happened
        {
            data = SSP2BUF;        //upon bus collision detection clear the buffer,
            SSP2CON1bits.WCOL=0;    // clear the bus collision status bit
        }
    }
    while(status!=0);        //write untill successful communication
    IdleI2C2();
    
    StopI2C2();
    
    StartI2C2();
    IdleI2C2();
    
    do
    {
    status = WriteI2C2( add1 | 0x00 );    //write the address of slave
        if(status == -1)        //check if bus collision happened
        {
            data = SSP2BUF;        //upon bus collision detection clear the buffer,
            SSP2CON1bits.WCOL=0;    // clear the bus collision status bit
        }
    }
    while(status!=0);        //write untill successful communication
    
    do
    {
    status = WriteI2C2( GPIOA );    //write the address register!! of slave
        if(status == -1)        //check if bus collision happened
        {
            data = SSP2BUF;        //upon bus collision detection clear the buffer,
            SSP2CON1bits.WCOL=0;    // clear the bus collision status bit
        }
    }
    while(status!=0);        //write untill successful communication
    
    do
    {
    status = WriteI2C2( IOEXP[0].GPXIOA );    //write the address register value of slave
        if(status == -1)        //check if bus collision happened
        {
            data = SSP2BUF;        //upon bus collision detection clear the buffer,
            SSP2CON1bits.WCOL=0;    // clear the bus collision status bit
        }
    }
    while(status!=0);        //write untill successful communication
    IdleI2C2();
    
    StopI2C2();
        
    */
    
    /*
	//R/W BIT IS '0' FOR FURTHER WRITE TO SLAVE

	//***WRITE THE THE DATA TO BE SENT FOR SLAVE***
     while(putsI2C2(I2C_Send)!=0);    //write string of data to be transmitted to slave

	//---TERMINATE COMMUNICATION FROM MASTER SIDE---
     IdleI2C2();




	
	//---RESTART I2C COMMUNICATION---
     RestartI2C2();
     IdleI2C2();
	//***write the address of the device for communication***
     data = SSPBUF;        //read any previous stored content in buffer to clear buffer full status

	//R/W BIT IS '1' FOR READ FROM SLAVE
    add1 = 0xA2;
    do
    {
    status = WriteI2C( add1 | 0x01 );  //write the address of slave
        if(status == -1)        //check if bus collision happened
        {
            data = SSPBUF;        //upon bus collision detection clear the buffer,
            SSPCON1bits.WCOL=0;    // clear the bus collision status bit
        }
    }
    while(status!=0);            //write untill successful communication

	//*** Recieve data from slave ***
    while( getsI2C(I2C_Recv,20) );        //recieve data string of lenght 20 from slave
    I2C_Recv[20] = '\0' ;

        NotAckI2C();                    //send the end of transmission signal through nack
        while( SSPCON2bits.ACKEN!=0);        //wait till ack sequence is complete

	//*** close I2C ***
      CloseI2C();                                //close I2C module

	*/
}
