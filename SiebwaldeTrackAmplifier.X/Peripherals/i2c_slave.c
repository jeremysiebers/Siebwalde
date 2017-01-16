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

#define USE_I2C_Clock_Stretch

/**
  Section: Global Variables
 */
uint8_t         API[256];                   //API for Master I2C device
uint8_t         *apiPtr;                    //Pointer to API memory locations
struct          FlagType flag;
unsigned char   MasterCmd = 0;              //Holds received command from master

/******************************************************************************
 * Function:       void I2C1_Init(void)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Initializes I2C1 peripheral as Slave.
 *****************************************************************************/
void I2C_Initialize()
{  
    /*
    I2C1MSK = 0x0;         // 0x7F Only 7 bit addresses are masked, 0x3FF 10 bit
    I2C1ADD = 0x50;        // Real address I2CxADD<6:0>
      
    config1 = (I2C1_ON & I2C1_IDLE_CON & I2C1_CLK_HLD &
             I2C1_IPMI_DIS & I2C1_7BIT_ADD &
             I2C1_SLW_DIS & I2C1_SM_DIS &
             I2C1_GCALL_DIS & I2C1_STR_EN &
             I2C1_ACK & I2C1_ACK_DIS & I2C1_RCV_DIS &
             I2C1_STOP_DIS & I2C1_RESTART_DIS &
             I2C1_START_DIS);
       
    OpenI2C1(config1, config2);*/
    
    #if !defined( USE_I2C_Clock_Stretch )
    I2C1CON = 0x8000;       //Enable I2C1 module
    #else
    I2C1CON = 0x9040;       //Enable I2C1 module, enable clock stretching
    #endif
    I2C1ADD = 0x50;         // 7-bit I2C slave address must be initialised here.
    I2C1MSK = 0x0;          // Mask --> I2C1 should react on adresses 0x40(broadcast) and 0x4
    I2C1CONbits.GCEN = 1;   // General Call address enabled for broadcast purposes, to write all new PWM values sync
    IFS1 = 0;
    apiPtr = &API[0];       //set the RAM pointer and points to beginning of API
    flag.AddrFlag = 0;      //Initlize Addflag
    flag.DataFlag = 0;      //Initlize Dataflag
    _SI2C1IE = 1;    
}

/******************************************************************************
 * Function:   void __attribute__((interrupt,no_auto_psv)) _SI2C1Interrupt(void)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        This is the ISR for I2C1 Slave interrupt.
 *****************************************************************************/
void I2C1_ISR()
{
    unsigned char   temp;   //used for dummy read
    if( (I2C1STATbits.R_W == 0) && (I2C1STATbits.D_A == 0) )                    //I2C1 Address matched and a write from master is detected
    {
        
        temp = I2C1RCV >> 1;     //dummy read (address received)
        if (I2C1STATbits.GCSTAT == 1)
        {
            flag.GCFlag = 1;
        }
        flag.AddrFlag = 1;                                                      //next byte will be TrackAmplifier Memory address
        #if defined( USE_I2C_Clock_Stretch )
        I2C1CONbits.SCLREL = 1;                 //Release SCL1 line
        #endif
    }
    
    else if( (I2C1STATbits.R_W == 0) && (I2C1STATbits.D_A == 1) )               //check for data
    {        
        if( flag.AddrFlag && !flag.GCFlag)
        {
            flag.AddrFlag = 0;
            flag.DataFlag = 1;                      //next byte is data
            temp = I2C1RCV;
            if (temp == 'M'){                                                   // Master indicates a read from specified address will be read next time
                MasterCmd = temp;
            }
            else{
                MasterCmd = 0;                                                  // reset Master command
                apiPtr = apiPtr + temp;
            }
            #if defined( USE_I2C_Clock_Stretch )
            I2C1CONbits.SCLREL = 1;                 //Release SCL1 line
            #endif
        }
        else if( flag.DataFlag && MasterCmd == 0 && !flag.GCFlag)
        {
            *apiPtr = ( unsigned char ) I2C1RCV;    // store data into RAM
            flag.AddrFlag = 0;                      //end of tx
            flag.DataFlag = 0;
            apiPtr = &API[0];                       //reset the RAM pointer
            #if defined( USE_I2C_Clock_Stretch )
            I2C1CONbits.SCLREL = 1;                 //Release SCL1 line
            #endif            
        }
        else if( flag.DataFlag && MasterCmd != 0 && !flag.GCFlag){
            apiPtr = apiPtr + I2C1RCV;                                          // Hold the address the Master wants to read next time
            flag.AddrFlag = 0;                                                  //end of tx
            flag.DataFlag = 0;
            #if defined( USE_I2C_Clock_Stretch )
            I2C1CONbits.SCLREL = 1;                 //Release SCL1 line
            #endif            
        }
        else if (flag.GCFlag){
            flag.GCFlag   = 0;
            flag.AddrFlag = 0;                                                  
            flag.DataFlag = 0;
            temp = I2C1RCV;
            if (temp == 'R'){                       // When an R is received during broadcast
                API[1] = 0;                         // Reset mem
                Led1 = 0;
            }
            #if defined( USE_I2C_Clock_Stretch )
            I2C1CONbits.SCLREL = 1;                 //Release SCL1 line
            #endif
        }
    }
    else if( (I2C1STATbits.R_W == 1) && (I2C1STATbits.D_A == 0) )               //I2C1 Address matched and a read from master is detected
    {        
        temp = I2C1RCV;
        I2C1TRN = *apiPtr;      //Read data from RAM & send data to I2C master device
        I2C1CONbits.SCLREL = 1; //Release SCL1 line
        while( I2C1STATbits.TBF );
        //Wait till all
        apiPtr = &API[0]; //reset the RAM pointer           
    }
}
/**
  End of File
 */
