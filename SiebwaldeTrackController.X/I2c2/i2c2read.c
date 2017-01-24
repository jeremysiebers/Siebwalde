#include <p18cxxx.h>
#include <i2c.h>

/********************************************************************
*     Function Name:    ReadI2C2                                    *
*     Return Value:     contents of SSP2BUF register                *
*     Parameters:       void                                        *
*     Description:      Read single byte from I2C2 bus.             *
********************************************************************/

unsigned long int timeout = 0;
unsigned int _return = 0;

#if defined (I2C_V3) || defined (I2C_V6) || defined (I2C_V6_1)
unsigned int ReadI2C2( void )
{

if( ((SSP2CON1&0x0F)==0x08) || ((SSP2CON1&0x0F)==0x0B) )	//master mode only
  SSP2CON2bits.RCEN = 1;           // enable master for 1 byte reception
  while ( !SSP2STATbits.BF ){      // wait until byte received  
      timeout ++;
      if (timeout > 0x8000){
          timeout = 0;
          _return = 0xFF << 2;
      }
  }
  return ( SSP2BUF |  _return);              // return with read byte 
}
#endif
