#include <p18cxxx.h>
#include <i2c2.h>

/********************************************************************
*     Function Name:    ReadI2C2                                    *
*     Return Value:     contents of SSP2BUF register                *
*     Parameters:       void                                        *
*     Description:      Read single byte from I2C2 bus.             *
* 						Adapted for Cooperative Multitasking		*
********************************************************************/
#if defined (I2C_V3) || defined (I2C_V6) || defined (I2C_V6_1)
static unsigned char 	CM=0;	// Cooperative Multitasking status bit
						
unsigned char ReadI2C2( void )
{
	if( ((SSP2CON1&0x0F)==0x08) || ((SSP2CON1&0x0F)==0x0B) )	//master mode only
	{
		static char Return_Val = Busy;
		
		switch (CM)
		{
			case	0	:	SSP2CON2bits.RCEN = 1;          // enable master for 1 byte reception
							Return_Val = Busy;				// return that routine is still busy (-3)
							CM = 1;							// goto case 1 to check if byte is received
							break;
							
			case	1	:	if ( !SSP2STATbits.BF )      	// check if byte received 
							{
								Return_Val = Busy;			// byte not received, return that routine is still busy (-3)
								CM = 1;						// keep case 1 active
								break;
							}
							else							// byte was received
							{
								Return_Val = SSP2BUF;		// return value is equal to buffer
								CM = 0;						// reset CM for next read itteration
								break;
							}
										
			default		:	CM = 0;							// default reset CM
							Return_Val = Busy;
							break;
		}
				
		return ( Return_Val );              				// return with read byte or status of routine
	}
}
#endif
