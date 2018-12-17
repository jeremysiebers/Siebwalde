#include <p18cxxx.h>
#include <i2c2.h>

/********************************************************************
*     Function Name:    WriteI2C2                                   *
*     Return Value:     Status byte for WCOL detection.             *
*     Parameters:       Single data byte for I2C2 bus.              *
*     Description:      This routine writes a single byte to the    * 
*                       I2C2 bus.                                   *
*						Adapted for Cooperative Multitasking		*
********************************************************************/
#if defined (I2C_V3) || defined (I2C_V6) || defined (I2C_V6_1)

unsigned static char 	CM=0, 					// Cooperative Multitasking status bit
						Return_Val_Routine;		// Return variable for nested subroutines/macro's

unsigned char WriteI2C2( unsigned char data_out )
{
	static char Return_Val = Busy;
	
	switch ( CM )
	{
		case	0	:	SSP2BUF = data_out;           	// write single byte to SSP2BUF
  						if ( SSP2CON1bits.WCOL )      	// test if write collision occurred
    					{
	    					Return_Val = -1;          	// if WCOL bit is set return negative #
	    					CM = 0;
	    					break;
	    				}
	    				else
	    				{
		    				if( ((SSP2CON1&0x0F)!=0x08) && ((SSP2CON1&0x0F)!=0x0B) )	//slave mode only
							{
	#if defined (I2C_SFR_V1) 
								if ( ( !SSP2STATbits.R_NOT_W ) && ( !SSP2STATbits.BF ) )
	#else
								if ( ( !SSP2STATbits.R_W ) && ( !SSP2STATbits.BF ) )// if R/W=0 and BF=0, NOT ACK was received
	#endif 
						      	{
						        	Return_Val = -2;             // return NACK
						        	CM = 0;
	    							break;
						      	}
						      	else
						      	{
							  		Return_Val = 0;				// return ACK
							  		CM = 0;
	    							break;
							  	}
							}
							else if( ((SSP2CON1&0x0F)==0x08) || ((SSP2CON1&0x0F)==0x0B) )	//master mode only
							{
								Return_Val = Busy;          	
	    						CM = 1;
	    						break;							    
							}
						}
						
		case	1	:	if ( SSP2STATbits.BF )   // if write cycle is not complete     
						{
							Return_Val = Busy;          	
	    					CM = 1;
	    					break;
	    				}
	    				else					// if write cycle is complete   
	    				{
		    				Return_Val = Busy;          	
	    					CM = 2;
	    					break;
	    				}
	    				
	    case	2	:	switch ( IdleI2C2() )               			// ensure module is idle
	    				{
		    				case	-3	:	Return_Val = Busy;          	
					    					CM = 2;
					    					break;
		    				
		    				case	0	:	if ( SSP2CON2bits.ACKSTAT ) // test for ACK condition received
		    								{
			    								Return_Val = -2;       	//return NACK   	
					    						CM = 0;
					    						break;
					    					}
					    					else
					    					{
						    					Return_Val = 0;       	//return ACK   	
					    						CM = 0;
					    						break;
					    					}
		    				
		    				default		:	Return_Val = Busy;          	
					    					CM = 2;
					    					break;
		    			}
		    			break;
		
		default		:	Return_Val = Busy;          	
					    CM = 0;
					    break;
		
	}
	return ( Return_Val );
}
#endif
