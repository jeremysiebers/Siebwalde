#include <p18cxxx.h>
#include <i2c2.h>


/********************************************************************
*     Function Name:    getsI2C2                                    *
*     Return Value:     error condition status                      *
*     Parameters:       address of read string storage location     *
*                       length of string bytes to read              *
*     Description:      This routine reads a predetemined string    *
*                       length in from the I2C2 bus. The routine is *
*                       developed for the Master mode. The bus ACK  *
*                       condition is generated within this routine. *
*						Adapted for Cooperative Multitasking. JSIF	*
********************************************************************/
#if defined (I2C_V3) || defined (I2C_V6) || defined (I2C_V6_1)

static char 	CM=0, 					// Cooperative Multitasking status bit
				Return_Val_Routine;		// Return variable for nested subroutines/macro's

unsigned char getsI2C2( unsigned char *rdptr, unsigned char length )
{
	static char Return_Val = Busy;
	
	switch (CM)
	{
		case	0	:	if (length --)												// perform getcI2C2() for 'length' number of bytes
						{
							CM = 1;
							Return_Val = Busy;
							break;
						}
						else														// 'length' number of bytes was 0 exit program return a 0
						{
							Return_Val = 0;
							CM = 0;
						}
						break;
						
		case	1	:	Return_Val_Routine = getcI2C2();       						// save byte received from sub routine
						CM = 2;
						Return_Val = Busy;
						break;
						
		case	2	:	switch (Return_Val_Routine)
						{
							case	-3	:	Return_Val = Busy;						// In case that getcI2C2 is busy redirect back to case 1 to getcI2C2 again
											CM = 1;
											break;
											
							default		:	*rdptr++ = Return_Val_Routine;			// In case that getcI2C2 was finished it returned the vaulue of SSP2BUF
											Return_Val = Busy;
											CM = 3;										
											break;											
						}
						
		case	3	:	if ( SSP2CON2bits.RCEN ) 									// check that receive sequence is over
						{
							CM = 3;
							Return_Val = Busy;
							break; 
						}
						else
						{
							CM = 4;
							Return_Val = Busy;
							break; 
						}
						
		case	4	:	
	#if defined (I2C_V3) || defined (I2C_V6)
						if ( PIR3bits.BCL2IF )           							// test for bus collision
	#elif defined (I2C_V6_1)
						if ( PIR2bits.BCL2IF )           							// test for bus collision
	#endif  
						{
							Return_Val = -1;            							// return with Bus Collision error and stop this program
							CM = 0;
							break;
						}
						else														// No bus collision error continue
						{
							CM = 5;
							Return_Val = Busy;
							break;
						}
						
		case	5	:	if( ((SSP2CON1&0x0F)==0x08) || ((SSP2CON1&0x0F)==0x0B) )	// master mode only
						{
							if ( length )               							// test if 'length' bytes have been read
					      	{
					        	SSP2CON2bits.ACKDT = 0;    							// set acknowledge bit state for ACK        
					        	SSP2CON2bits.ACKEN = 1;    							// initiate bus acknowledge sequence
					        	CM = 6;												// Check if ACK sequence is over
								Return_Val = Busy;
								break;
					      	} 
					      	else													// No 'length' bytes have been read
					      	{
						      	CM = 0;
								Return_Val = Busy;
								break;
						    }
						}
						else														// Not master mode
						{
							CM = 0;
							Return_Val = Busy;
							break;
						}
						
		case	6	:	if ( SSP2CON2bits.ACKEN )									// if ACK sequence is not over
						{
							CM = 6;
							Return_Val = Busy;
							break;						
						}
						else														// if ACK sequence is over								
						{
							CM = 0;
							Return_Val = Busy;
							break;
						}							
							
		default		:	CM=0;
						Return_Val = Busy;
						break;
	}
	return ( Return_Val );
}
#endif
