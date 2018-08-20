/**
  @Generated Pin Manager Header File

  @Company:
    Microchip Technology Inc.

  @File Name:
    pin_manager.h

  @Summary:
    This is the Pin Manager file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  @Description
    This header file provides APIs for driver for .
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.65.2
        Device            :  PIC16F18857
        Driver Version    :  2.01
    The generated drivers are tested against the following:
        Compiler          :  XC8 1.45
        MPLAB 	          :  MPLAB X 4.15	
*/

/*
    (c) 2018 Microchip Technology Inc. and its subsidiaries. 
    
    Subject to your compliance with these terms, you may use Microchip software and any 
    derivatives exclusively with Microchip products. It is your responsibility to comply with third party 
    license terms applicable to your use of third party software (including open source software) that 
    may accompany Microchip software.
    
    THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS". NO WARRANTIES, WHETHER 
    EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY 
    IMPLIED WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS 
    FOR A PARTICULAR PURPOSE.
    
    IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
    INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
    WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP 
    HAS BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE. TO 
    THE FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL 
    CLAIMS IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT 
    OF FEES, IF ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS 
    SOFTWARE.
*/

#ifndef PIN_MANAGER_H
#define PIN_MANAGER_H

#define INPUT   1
#define OUTPUT  0

#define HIGH    1
#define LOW     0

#define ANALOG      1
#define DIGITAL     0

#define PULL_UP_ENABLED      1
#define PULL_UP_DISABLED     0

// get/set LED_TX aliases
#define LED_TX_TRIS                 TRISBbits.TRISB0
#define LED_TX_LAT                  LATBbits.LATB0
#define LED_TX_PORT                 PORTBbits.RB0
#define LED_TX_WPU                  WPUBbits.WPUB0
#define LED_TX_OD                   ODCONBbits.ODCB0
#define LED_TX_ANS                  ANSELBbits.ANSB0
#define LED_TX_SetHigh()            do { LATBbits.LATB0 = 1; } while(0)
#define LED_TX_SetLow()             do { LATBbits.LATB0 = 0; } while(0)
#define LED_TX_Toggle()             do { LATBbits.LATB0 = ~LATBbits.LATB0; } while(0)
#define LED_TX_GetValue()           PORTBbits.RB0
#define LED_TX_SetDigitalInput()    do { TRISBbits.TRISB0 = 1; } while(0)
#define LED_TX_SetDigitalOutput()   do { TRISBbits.TRISB0 = 0; } while(0)
#define LED_TX_SetPullup()          do { WPUBbits.WPUB0 = 1; } while(0)
#define LED_TX_ResetPullup()        do { WPUBbits.WPUB0 = 0; } while(0)
#define LED_TX_SetPushPull()        do { ODCONBbits.ODCB0 = 0; } while(0)
#define LED_TX_SetOpenDrain()       do { ODCONBbits.ODCB0 = 1; } while(0)
#define LED_TX_SetAnalogMode()      do { ANSELBbits.ANSB0 = 1; } while(0)
#define LED_TX_SetDigitalMode()     do { ANSELBbits.ANSB0 = 0; } while(0)

// get/set I2C_CLK aliases
#define I2C_CLK_TRIS                 TRISBbits.TRISB1
#define I2C_CLK_LAT                  LATBbits.LATB1
#define I2C_CLK_PORT                 PORTBbits.RB1
#define I2C_CLK_WPU                  WPUBbits.WPUB1
#define I2C_CLK_OD                   ODCONBbits.ODCB1
#define I2C_CLK_ANS                  ANSELBbits.ANSB1
#define I2C_CLK_SetHigh()            do { LATBbits.LATB1 = 1; } while(0)
#define I2C_CLK_SetLow()             do { LATBbits.LATB1 = 0; } while(0)
#define I2C_CLK_Toggle()             do { LATBbits.LATB1 = ~LATBbits.LATB1; } while(0)
#define I2C_CLK_GetValue()           PORTBbits.RB1
#define I2C_CLK_SetDigitalInput()    do { TRISBbits.TRISB1 = 1; } while(0)
#define I2C_CLK_SetDigitalOutput()   do { TRISBbits.TRISB1 = 0; } while(0)
#define I2C_CLK_SetPullup()          do { WPUBbits.WPUB1 = 1; } while(0)
#define I2C_CLK_ResetPullup()        do { WPUBbits.WPUB1 = 0; } while(0)
#define I2C_CLK_SetPushPull()        do { ODCONBbits.ODCB1 = 0; } while(0)
#define I2C_CLK_SetOpenDrain()       do { ODCONBbits.ODCB1 = 1; } while(0)
#define I2C_CLK_SetAnalogMode()      do { ANSELBbits.ANSB1 = 1; } while(0)
#define I2C_CLK_SetDigitalMode()     do { ANSELBbits.ANSB1 = 0; } while(0)

// get/set I2C_DATA aliases
#define I2C_DATA_TRIS                 TRISBbits.TRISB2
#define I2C_DATA_LAT                  LATBbits.LATB2
#define I2C_DATA_PORT                 PORTBbits.RB2
#define I2C_DATA_WPU                  WPUBbits.WPUB2
#define I2C_DATA_OD                   ODCONBbits.ODCB2
#define I2C_DATA_ANS                  ANSELBbits.ANSB2
#define I2C_DATA_SetHigh()            do { LATBbits.LATB2 = 1; } while(0)
#define I2C_DATA_SetLow()             do { LATBbits.LATB2 = 0; } while(0)
#define I2C_DATA_Toggle()             do { LATBbits.LATB2 = ~LATBbits.LATB2; } while(0)
#define I2C_DATA_GetValue()           PORTBbits.RB2
#define I2C_DATA_SetDigitalInput()    do { TRISBbits.TRISB2 = 1; } while(0)
#define I2C_DATA_SetDigitalOutput()   do { TRISBbits.TRISB2 = 0; } while(0)
#define I2C_DATA_SetPullup()          do { WPUBbits.WPUB2 = 1; } while(0)
#define I2C_DATA_ResetPullup()        do { WPUBbits.WPUB2 = 0; } while(0)
#define I2C_DATA_SetPushPull()        do { ODCONBbits.ODCB2 = 0; } while(0)
#define I2C_DATA_SetOpenDrain()       do { ODCONBbits.ODCB2 = 1; } while(0)
#define I2C_DATA_SetAnalogMode()      do { ANSELBbits.ANSB2 = 1; } while(0)
#define I2C_DATA_SetDigitalMode()     do { ANSELBbits.ANSB2 = 0; } while(0)

// get/set LED_ERR aliases
#define LED_ERR_TRIS                 TRISBbits.TRISB3
#define LED_ERR_LAT                  LATBbits.LATB3
#define LED_ERR_PORT                 PORTBbits.RB3
#define LED_ERR_WPU                  WPUBbits.WPUB3
#define LED_ERR_OD                   ODCONBbits.ODCB3
#define LED_ERR_ANS                  ANSELBbits.ANSB3
#define LED_ERR_SetHigh()            do { LATBbits.LATB3 = 1; } while(0)
#define LED_ERR_SetLow()             do { LATBbits.LATB3 = 0; } while(0)
#define LED_ERR_Toggle()             do { LATBbits.LATB3 = ~LATBbits.LATB3; } while(0)
#define LED_ERR_GetValue()           PORTBbits.RB3
#define LED_ERR_SetDigitalInput()    do { TRISBbits.TRISB3 = 1; } while(0)
#define LED_ERR_SetDigitalOutput()   do { TRISBbits.TRISB3 = 0; } while(0)
#define LED_ERR_SetPullup()          do { WPUBbits.WPUB3 = 1; } while(0)
#define LED_ERR_ResetPullup()        do { WPUBbits.WPUB3 = 0; } while(0)
#define LED_ERR_SetPushPull()        do { ODCONBbits.ODCB3 = 0; } while(0)
#define LED_ERR_SetOpenDrain()       do { ODCONBbits.ODCB3 = 1; } while(0)
#define LED_ERR_SetAnalogMode()      do { ANSELBbits.ANSB3 = 1; } while(0)
#define LED_ERR_SetDigitalMode()     do { ANSELBbits.ANSB3 = 0; } while(0)

// get/set LED_WAR aliases
#define LED_WAR_TRIS                 TRISBbits.TRISB4
#define LED_WAR_LAT                  LATBbits.LATB4
#define LED_WAR_PORT                 PORTBbits.RB4
#define LED_WAR_WPU                  WPUBbits.WPUB4
#define LED_WAR_OD                   ODCONBbits.ODCB4
#define LED_WAR_ANS                  ANSELBbits.ANSB4
#define LED_WAR_SetHigh()            do { LATBbits.LATB4 = 1; } while(0)
#define LED_WAR_SetLow()             do { LATBbits.LATB4 = 0; } while(0)
#define LED_WAR_Toggle()             do { LATBbits.LATB4 = ~LATBbits.LATB4; } while(0)
#define LED_WAR_GetValue()           PORTBbits.RB4
#define LED_WAR_SetDigitalInput()    do { TRISBbits.TRISB4 = 1; } while(0)
#define LED_WAR_SetDigitalOutput()   do { TRISBbits.TRISB4 = 0; } while(0)
#define LED_WAR_SetPullup()          do { WPUBbits.WPUB4 = 1; } while(0)
#define LED_WAR_ResetPullup()        do { WPUBbits.WPUB4 = 0; } while(0)
#define LED_WAR_SetPushPull()        do { ODCONBbits.ODCB4 = 0; } while(0)
#define LED_WAR_SetOpenDrain()       do { ODCONBbits.ODCB4 = 1; } while(0)
#define LED_WAR_SetAnalogMode()      do { ANSELBbits.ANSB4 = 1; } while(0)
#define LED_WAR_SetDigitalMode()     do { ANSELBbits.ANSB4 = 0; } while(0)

// get/set LED_RUN aliases
#define LED_RUN_TRIS                 TRISBbits.TRISB5
#define LED_RUN_LAT                  LATBbits.LATB5
#define LED_RUN_PORT                 PORTBbits.RB5
#define LED_RUN_WPU                  WPUBbits.WPUB5
#define LED_RUN_OD                   ODCONBbits.ODCB5
#define LED_RUN_ANS                  ANSELBbits.ANSB5
#define LED_RUN_SetHigh()            do { LATBbits.LATB5 = 1; } while(0)
#define LED_RUN_SetLow()             do { LATBbits.LATB5 = 0; } while(0)
#define LED_RUN_Toggle()             do { LATBbits.LATB5 = ~LATBbits.LATB5; } while(0)
#define LED_RUN_GetValue()           PORTBbits.RB5
#define LED_RUN_SetDigitalInput()    do { TRISBbits.TRISB5 = 1; } while(0)
#define LED_RUN_SetDigitalOutput()   do { TRISBbits.TRISB5 = 0; } while(0)
#define LED_RUN_SetPullup()          do { WPUBbits.WPUB5 = 1; } while(0)
#define LED_RUN_ResetPullup()        do { WPUBbits.WPUB5 = 0; } while(0)
#define LED_RUN_SetPushPull()        do { ODCONBbits.ODCB5 = 0; } while(0)
#define LED_RUN_SetOpenDrain()       do { ODCONBbits.ODCB5 = 1; } while(0)
#define LED_RUN_SetAnalogMode()      do { ANSELBbits.ANSB5 = 1; } while(0)
#define LED_RUN_SetDigitalMode()     do { ANSELBbits.ANSB5 = 0; } while(0)

// get/set ICSPCLK aliases
#define ICSPCLK_TRIS                 TRISBbits.TRISB6
#define ICSPCLK_LAT                  LATBbits.LATB6
#define ICSPCLK_PORT                 PORTBbits.RB6
#define ICSPCLK_WPU                  WPUBbits.WPUB6
#define ICSPCLK_OD                   ODCONBbits.ODCB6
#define ICSPCLK_ANS                  ANSELBbits.ANSB6
#define ICSPCLK_SetHigh()            do { LATBbits.LATB6 = 1; } while(0)
#define ICSPCLK_SetLow()             do { LATBbits.LATB6 = 0; } while(0)
#define ICSPCLK_Toggle()             do { LATBbits.LATB6 = ~LATBbits.LATB6; } while(0)
#define ICSPCLK_GetValue()           PORTBbits.RB6
#define ICSPCLK_SetDigitalInput()    do { TRISBbits.TRISB6 = 1; } while(0)
#define ICSPCLK_SetDigitalOutput()   do { TRISBbits.TRISB6 = 0; } while(0)
#define ICSPCLK_SetPullup()          do { WPUBbits.WPUB6 = 1; } while(0)
#define ICSPCLK_ResetPullup()        do { WPUBbits.WPUB6 = 0; } while(0)
#define ICSPCLK_SetPushPull()        do { ODCONBbits.ODCB6 = 0; } while(0)
#define ICSPCLK_SetOpenDrain()       do { ODCONBbits.ODCB6 = 1; } while(0)
#define ICSPCLK_SetAnalogMode()      do { ANSELBbits.ANSB6 = 1; } while(0)
#define ICSPCLK_SetDigitalMode()     do { ANSELBbits.ANSB6 = 0; } while(0)

// get/set ICSPDAT aliases
#define ICSPDAT_TRIS                 TRISBbits.TRISB7
#define ICSPDAT_LAT                  LATBbits.LATB7
#define ICSPDAT_PORT                 PORTBbits.RB7
#define ICSPDAT_WPU                  WPUBbits.WPUB7
#define ICSPDAT_OD                   ODCONBbits.ODCB7
#define ICSPDAT_ANS                  ANSELBbits.ANSB7
#define ICSPDAT_SetHigh()            do { LATBbits.LATB7 = 1; } while(0)
#define ICSPDAT_SetLow()             do { LATBbits.LATB7 = 0; } while(0)
#define ICSPDAT_Toggle()             do { LATBbits.LATB7 = ~LATBbits.LATB7; } while(0)
#define ICSPDAT_GetValue()           PORTBbits.RB7
#define ICSPDAT_SetDigitalInput()    do { TRISBbits.TRISB7 = 1; } while(0)
#define ICSPDAT_SetDigitalOutput()   do { TRISBbits.TRISB7 = 0; } while(0)
#define ICSPDAT_SetPullup()          do { WPUBbits.WPUB7 = 1; } while(0)
#define ICSPDAT_ResetPullup()        do { WPUBbits.WPUB7 = 0; } while(0)
#define ICSPDAT_SetPushPull()        do { ODCONBbits.ODCB7 = 0; } while(0)
#define ICSPDAT_SetOpenDrain()       do { ODCONBbits.ODCB7 = 1; } while(0)
#define ICSPDAT_SetAnalogMode()      do { ANSELBbits.ANSB7 = 1; } while(0)
#define ICSPDAT_SetDigitalMode()     do { ANSELBbits.ANSB7 = 0; } while(0)

// get/set RC1 procedures
#define RC1_SetHigh()               do { LATCbits.LATC1 = 1; } while(0)
#define RC1_SetLow()                do { LATCbits.LATC1 = 0; } while(0)
#define RC1_Toggle()                do { LATCbits.LATC1 = ~LATCbits.LATC1; } while(0)
#define RC1_GetValue()              PORTCbits.RC1
#define RC1_SetDigitalInput()       do { TRISCbits.TRISC1 = 1; } while(0)
#define RC1_SetDigitalOutput()      do { TRISCbits.TRISC1 = 0; } while(0)
#define RC1_SetPullup()             do { WPUCbits.WPUC1 = 1; } while(0)
#define RC1_ResetPullup()           do { WPUCbits.WPUC1 = 0; } while(0)
#define RC1_SetAnalogMode()         do { ANSELCbits.ANSC1 = 1; } while(0)
#define RC1_SetDigitalMode()        do { ANSELCbits.ANSC1 = 0; } while(0)

// get/set RC3 procedures
#define RC3_SetHigh()               do { LATCbits.LATC3 = 1; } while(0)
#define RC3_SetLow()                do { LATCbits.LATC3 = 0; } while(0)
#define RC3_Toggle()                do { LATCbits.LATC3 = ~LATCbits.LATC3; } while(0)
#define RC3_GetValue()              PORTCbits.RC3
#define RC3_SetDigitalInput()       do { TRISCbits.TRISC3 = 1; } while(0)
#define RC3_SetDigitalOutput()      do { TRISCbits.TRISC3 = 0; } while(0)
#define RC3_SetPullup()             do { WPUCbits.WPUC3 = 1; } while(0)
#define RC3_ResetPullup()           do { WPUCbits.WPUC3 = 0; } while(0)
#define RC3_SetAnalogMode()         do { ANSELCbits.ANSC3 = 1; } while(0)
#define RC3_SetDigitalMode()        do { ANSELCbits.ANSC3 = 0; } while(0)

// get/set LED_RX aliases
#define LED_RX_TRIS                 TRISCbits.TRISC7
#define LED_RX_LAT                  LATCbits.LATC7
#define LED_RX_PORT                 PORTCbits.RC7
#define LED_RX_WPU                  WPUCbits.WPUC7
#define LED_RX_OD                   ODCONCbits.ODCC7
#define LED_RX_ANS                  ANSELCbits.ANSC7
#define LED_RX_SetHigh()            do { LATCbits.LATC7 = 1; } while(0)
#define LED_RX_SetLow()             do { LATCbits.LATC7 = 0; } while(0)
#define LED_RX_Toggle()             do { LATCbits.LATC7 = ~LATCbits.LATC7; } while(0)
#define LED_RX_GetValue()           PORTCbits.RC7
#define LED_RX_SetDigitalInput()    do { TRISCbits.TRISC7 = 1; } while(0)
#define LED_RX_SetDigitalOutput()   do { TRISCbits.TRISC7 = 0; } while(0)
#define LED_RX_SetPullup()          do { WPUCbits.WPUC7 = 1; } while(0)
#define LED_RX_ResetPullup()        do { WPUCbits.WPUC7 = 0; } while(0)
#define LED_RX_SetPushPull()        do { ODCONCbits.ODCC7 = 0; } while(0)
#define LED_RX_SetOpenDrain()       do { ODCONCbits.ODCC7 = 1; } while(0)
#define LED_RX_SetAnalogMode()      do { ANSELCbits.ANSC7 = 1; } while(0)
#define LED_RX_SetDigitalMode()     do { ANSELCbits.ANSC7 = 0; } while(0)

/**
   @Param
    none
   @Returns
    none
   @Description
    GPIO and peripheral I/O initialization
   @Example
    PIN_MANAGER_Initialize();
 */
void PIN_MANAGER_Initialize (void);

/**
 * @Param
    none
 * @Returns
    none
 * @Description
    Interrupt on Change Handling routine
 * @Example
    PIN_MANAGER_IOC();
 */
void PIN_MANAGER_IOC(void);



#endif // PIN_MANAGER_H
/**
 End of File
*/