#include <p18cxxx.h>
#include <i2c2.h>


/*********************************************************************
*     Function Name:    putsI2C2                                     *
*     Return Value:     error condition status                       *
*     Parameters:       address of write string storage location     *
*     Description:      This routine writes a string to the I2C2 bus,*
*                       until a null character is reached. If Master *
*                       function putcI2C is called. When trans-      *
*                       mission is complete then test for ack-       *
*                       nowledge bit. If Slave transmitter wait for  *
*                       null character or not ACK received from bus  *
*                       device.                                      *
*********************************************************************/
#if defined (I2C_V3) || defined (I2C_V6) || defined (I2C_V6_1)

unsigned static char 	CM=0, 					// Cooperative Multitasking status bit
						Return_Val_Routine;		// Return variable for nested subroutines/macro's

unsigned char putsI2C2( unsigned char *wrptr )
{
	static char Return_Val = Busy;
   	   	
   	switch ( CM )
   	{
	   	case	0	:	if ( *wrptr )                 		// transmit data until null character 
	   					{
		   					if ( SSP2CON1bits.SSPM3 )      	// if Master transmitter then execute the following
    						{
	    						Return_Val = Busy;
			   					CM = 1;
			   					break;
			   				}
			   				else							// else Slave transmitter
			   				{
				   				
		#if defined (I2C_V3) || defined (I2C_V6)
							    PIR3bits.SSP2IF = 0;         // reset SSP2IF bit
		#elif defined (I2C_V6_1)
								PIR2bits.SSP2IF = 0;         // reset SSP2IF bit
		#endif		
							    SSP2BUF = *wrptr;            // load SSP2BUF with new data
							    SSP2CON1bits.CKP = 1;        // release clock line 
							    Return_Val = Busy;
			   					CM = 5;
			   					break;
				   			}
	    						
		   				}
		   				else
		   				{
			   				Return_Val = Finished;			// Write done
			   				CM = 0;
			   				break;
			   			}
			   			break;
	   	
	   	case	1	:	switch ( Return_Val_Routine = putcI2C2 ( *wrptr ) )
	   					{
		   					case	0	:	Return_Val = Busy;
			   								CM = 2;
			   								break;
			   								
		   					case	-1	:	Return_Val = -1;
			   								CM = 0;
			   								break;
			   								
		   					case	-2	:	Return_Val = -2;
			   								CM = 0;
			   								break;
			   								
		   					default		:	Return_Val = Busy;
			   								CM = 1;
			   								break;
		   				}
		   				break;
		   				
		case	2	:	wrptr ++;                        // increment pointer
						Return_Val = Busy;
			   			CM = 0;
			   			break;
			   			
		case	5	:	
		#if defined (I2C_V3) || defined (I2C_V6)
      					if ( !PIR3bits.SSP2IF )  // if ninth clock pulse received
		#elif defined (I2C_V6_1)
	  					if ( !PIR2bits.SSP2IF )  // if ninth clock pulse received
		#endif	 
						{
							Return_Val = Busy;
			   				CM = 5;
			   				break;
						}
						else
						{
		#if defined (I2C_SFR_V1) 
     						if ( ( SSP2CON1bits.CKP ) && ( !SSP2STATbits.BF ) )
	 	#else
      						if ( ( SSP2CON1bits.CKP ) && ( !SSP2STATbits.BF ) )		// if R/W=0 and BF=0, NOT ACK was received
	 	#endif 
      						{
	      						Return_Val = -2;									// terminate PutsI2C2() function
			   					CM = 0;
			   					break;        
      						}
						}
						Return_Val = Busy;
			   			CM = 2;
			   			break;
	   	
	   	default		:	Return_Val = Busy;
	   					CM = 0;
	   					break;
	}
	
	return ( Return_Val );
}
#endif
