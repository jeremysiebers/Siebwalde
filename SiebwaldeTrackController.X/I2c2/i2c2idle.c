#include <p18cxxx.h>
#include <i2c2.h>


/********************************************************************
*     Function Name:    IdleI2C2                                    *
*     Return Value:     error condition status                      *
*     Parameters:       void                                        *
*     Description:      Test until I2C2 module is idle.    			*
*						Adapted for Cooperative Multitasking		*
********************************************************************/

#if defined (I2C_V3) || defined (I2C_V6) || defined (I2C_V6_1)
#undef IdleI2C2

unsigned char IdleI2C2( void )
{
	static char Return_Val = Busy;
			
	#if defined (I2C_SFR_V1)
    if ((SSP2CON2 & 0x1F) | (SSP2STATbits.R_NOT_W))
  	#else
    if ((SSP2CON2 & 0x1F) | (SSP2STATbits.R_W))
  	#endif	
  	{
	  	Return_Val = Busy;
	}
	else
	{
		Return_Val = Finished;
	}
		
	
	return ( Return_Val );    
}
#endif
