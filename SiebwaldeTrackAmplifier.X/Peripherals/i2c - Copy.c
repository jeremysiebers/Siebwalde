/**
  Section: Included Files
 */
#include <xc.h>
#include "config.h"
#include "i2c.h"
#include <stdlib.h>
#include <stdio.h>
#include <libpic30.h>


/**
  Section: Macro Declarations
 */



/**
  Section: Global Variables
 */

/*
 * I2CxBRG = ((1/FSCL -Delay)x Fcy)-2 --> 591
 * FSCL  = 100 kHz 
 * Fcy   = 60 MHz 
 * Delay = 120 ns
 */
unsigned int config2 = 591;
unsigned int config1 = 0;
char ReceivedI2CData = 0;

void I2C_Initialize()
{
    /*
    I2C1MSK = 0x7F;         // Only 7 bit addresses are masked
    I2C1ADD = 0x4;          // Real address I2CxADD<6:0>
    
    I2C1RCV = 0;
    I2C1TRN = 0;
    
    config1 = (I2C1_ON & I2C1_IDLE_CON & I2C1_CLK_HLD &
             I2C1_IPMI_DIS & I2C1_7BIT_ADD &
             I2C1_SLW_DIS & I2C1_SM_DIS &
             I2C1_GCALL_DIS & I2C1_STR_DIS &
             I2C1_NACK & I2C1_ACK_DIS & I2C1_RCV_DIS &
             I2C1_STOP_DIS & I2C1_RESTART_DIS &
             I2C1_START_DIS);
    
    OpenI2C1(config1, config2);
    //SetPriorityIntSI2C1(7);
    EnableIntSI2C1;
    */
    
    config1 = (I2C1_ON & I2C1_IDLE_CON & I2C1_CLK_HLD &
             I2C1_IPMI_DIS & I2C1_7BIT_ADD &
             I2C1_SLW_DIS & I2C1_SM_DIS &
             I2C1_GCALL_DIS & I2C1_STR_DIS &
             I2C1_NACK & I2C1_ACK_DIS & I2C1_RCV_DIS &
             I2C1_STOP_DIS & I2C1_RESTART_DIS &
             I2C1_START_DIS);
    
    OpenI2C1(config1, config2);
    
    IdleI2C1();
    StartI2C1();
      /* Wait till Start sequence is completed */
      while(I2C1CONbits.SEN);
      /* Clear interrupt flag */
      IFS1bits.MI2C1IF = 0;
      /* Write Slave address and set master for transmission */
      MasterWriteI2C1(0x8);
      /* Wait till address is transmitted */
      while(I2C1STATbits.TBF);  // 8 clock cycles
      while(!IFS1bits.MI2C1IF); // Wait for 9th clock cycle
      IFS1bits.MI2C1IF = 0;     // Clear interrupt flag
      //while(I2C1STATbits.ACKSTAT);
      /* Transmit string of data */
      MasterputcI2C1('A');
      StopI2C1();
      /* Wait till stop sequence is completed */
      while(I2C1CONbits.PEN);
      CloseI2C1();
}

void I2C1_ISR(void){
    
    if (I2C1STATbits.RBF)
    {  
        printf("Data received\r\n");
        ReceivedI2CData = I2C1RCV;
        printf("%X\r\n",ReceivedI2CData);
        ReceivedI2CData = 0;
    }  

    if (I2C1STATbits.S)
    {
        printf("Start detected\r\n");           
    }
    
    if (I2C1STATbits.P)
    {
        printf("Stop detected\r\n");        
    }
    
}

/**
  End of File
 */
