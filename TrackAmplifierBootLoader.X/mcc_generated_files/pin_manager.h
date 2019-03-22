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
        Device            :  PIC18F25K40
        Driver Version    :  2.11
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

// get/set RA0 procedures
#define RA0_SetHigh()            do { LATAbits.LATA0 = 1; } while(0)
#define RA0_SetLow()             do { LATAbits.LATA0 = 0; } while(0)
#define RA0_Toggle()             do { LATAbits.LATA0 = ~LATAbits.LATA0; } while(0)
#define RA0_GetValue()              PORTAbits.RA0
#define RA0_SetDigitalInput()    do { TRISAbits.TRISA0 = 1; } while(0)
#define RA0_SetDigitalOutput()   do { TRISAbits.TRISA0 = 0; } while(0)
#define RA0_SetPullup()             do { WPUAbits.WPUA0 = 1; } while(0)
#define RA0_ResetPullup()           do { WPUAbits.WPUA0 = 0; } while(0)
#define RA0_SetAnalogMode()         do { ANSELAbits.ANSELA0 = 1; } while(0)
#define RA0_SetDigitalMode()        do { ANSELAbits.ANSELA0 = 0; } while(0)

// get/set RA5 procedures
#define RA5_SetHigh()            do { LATAbits.LATA5 = 1; } while(0)
#define RA5_SetLow()             do { LATAbits.LATA5 = 0; } while(0)
#define RA5_Toggle()             do { LATAbits.LATA5 = ~LATAbits.LATA5; } while(0)
#define RA5_GetValue()              PORTAbits.RA5
#define RA5_SetDigitalInput()    do { TRISAbits.TRISA5 = 1; } while(0)
#define RA5_SetDigitalOutput()   do { TRISAbits.TRISA5 = 0; } while(0)
#define RA5_SetPullup()             do { WPUAbits.WPUA5 = 1; } while(0)
#define RA5_ResetPullup()           do { WPUAbits.WPUA5 = 0; } while(0)
#define RA5_SetAnalogMode()         do { ANSELAbits.ANSELA5 = 1; } while(0)
#define RA5_SetDigitalMode()        do { ANSELAbits.ANSELA5 = 0; } while(0)

// get/set LED_TX aliases
#define LED_TX_TRIS                 TRISBbits.TRISB1
#define LED_TX_LAT                  LATBbits.LATB1
#define LED_TX_PORT                 PORTBbits.RB1
#define LED_TX_WPU                  WPUBbits.WPUB1
#define LED_TX_OD                   ODCONBbits.ODCB1
#define LED_TX_ANS                  ANSELBbits.ANSELB1
#define LED_TX_SetHigh()            do { LATBbits.LATB1 = 1; } while(0)
#define LED_TX_SetLow()             do { LATBbits.LATB1 = 0; } while(0)
#define LED_TX_Toggle()             do { LATBbits.LATB1 = ~LATBbits.LATB1; } while(0)
#define LED_TX_GetValue()           PORTBbits.RB1
#define LED_TX_SetDigitalInput()    do { TRISBbits.TRISB1 = 1; } while(0)
#define LED_TX_SetDigitalOutput()   do { TRISBbits.TRISB1 = 0; } while(0)
#define LED_TX_SetPullup()          do { WPUBbits.WPUB1 = 1; } while(0)
#define LED_TX_ResetPullup()        do { WPUBbits.WPUB1 = 0; } while(0)
#define LED_TX_SetPushPull()        do { ODCONBbits.ODCB1 = 0; } while(0)
#define LED_TX_SetOpenDrain()       do { ODCONBbits.ODCB1 = 1; } while(0)
#define LED_TX_SetAnalogMode()      do { ANSELBbits.ANSELB1 = 1; } while(0)
#define LED_TX_SetDigitalMode()     do { ANSELBbits.ANSELB1 = 0; } while(0)

// get/set LED_RX aliases
#define LED_RX_TRIS                 TRISBbits.TRISB2
#define LED_RX_LAT                  LATBbits.LATB2
#define LED_RX_PORT                 PORTBbits.RB2
#define LED_RX_WPU                  WPUBbits.WPUB2
#define LED_RX_OD                   ODCONBbits.ODCB2
#define LED_RX_ANS                  ANSELBbits.ANSELB2
#define LED_RX_SetHigh()            do { LATBbits.LATB2 = 1; } while(0)
#define LED_RX_SetLow()             do { LATBbits.LATB2 = 0; } while(0)
#define LED_RX_Toggle()             do { LATBbits.LATB2 = ~LATBbits.LATB2; } while(0)
#define LED_RX_GetValue()           PORTBbits.RB2
#define LED_RX_SetDigitalInput()    do { TRISBbits.TRISB2 = 1; } while(0)
#define LED_RX_SetDigitalOutput()   do { TRISBbits.TRISB2 = 0; } while(0)
#define LED_RX_SetPullup()          do { WPUBbits.WPUB2 = 1; } while(0)
#define LED_RX_ResetPullup()        do { WPUBbits.WPUB2 = 0; } while(0)
#define LED_RX_SetPushPull()        do { ODCONBbits.ODCB2 = 0; } while(0)
#define LED_RX_SetOpenDrain()       do { ODCONBbits.ODCB2 = 1; } while(0)
#define LED_RX_SetAnalogMode()      do { ANSELBbits.ANSELB2 = 1; } while(0)
#define LED_RX_SetDigitalMode()     do { ANSELBbits.ANSELB2 = 0; } while(0)

// get/set LED_ERR aliases
#define LED_ERR_TRIS                 TRISBbits.TRISB3
#define LED_ERR_LAT                  LATBbits.LATB3
#define LED_ERR_PORT                 PORTBbits.RB3
#define LED_ERR_WPU                  WPUBbits.WPUB3
#define LED_ERR_OD                   ODCONBbits.ODCB3
#define LED_ERR_ANS                  ANSELBbits.ANSELB3
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
#define LED_ERR_SetAnalogMode()      do { ANSELBbits.ANSELB3 = 1; } while(0)
#define LED_ERR_SetDigitalMode()     do { ANSELBbits.ANSELB3 = 0; } while(0)

// get/set LED_WAR aliases
#define LED_WAR_TRIS                 TRISBbits.TRISB4
#define LED_WAR_LAT                  LATBbits.LATB4
#define LED_WAR_PORT                 PORTBbits.RB4
#define LED_WAR_WPU                  WPUBbits.WPUB4
#define LED_WAR_OD                   ODCONBbits.ODCB4
#define LED_WAR_ANS                  ANSELBbits.ANSELB4
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
#define LED_WAR_SetAnalogMode()      do { ANSELBbits.ANSELB4 = 1; } while(0)
#define LED_WAR_SetDigitalMode()     do { ANSELBbits.ANSELB4 = 0; } while(0)

// get/set LED_RUN aliases
#define LED_RUN_TRIS                 TRISBbits.TRISB5
#define LED_RUN_LAT                  LATBbits.LATB5
#define LED_RUN_PORT                 PORTBbits.RB5
#define LED_RUN_WPU                  WPUBbits.WPUB5
#define LED_RUN_OD                   ODCONBbits.ODCB5
#define LED_RUN_ANS                  ANSELBbits.ANSELB5
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
#define LED_RUN_SetAnalogMode()      do { ANSELBbits.ANSELB5 = 1; } while(0)
#define LED_RUN_SetDigitalMode()     do { ANSELBbits.ANSELB5 = 0; } while(0)

// get/set RC1 procedures
#define RC1_SetHigh()            do { LATCbits.LATC1 = 1; } while(0)
#define RC1_SetLow()             do { LATCbits.LATC1 = 0; } while(0)
#define RC1_Toggle()             do { LATCbits.LATC1 = ~LATCbits.LATC1; } while(0)
#define RC1_GetValue()              PORTCbits.RC1
#define RC1_SetDigitalInput()    do { TRISCbits.TRISC1 = 1; } while(0)
#define RC1_SetDigitalOutput()   do { TRISCbits.TRISC1 = 0; } while(0)
#define RC1_SetPullup()             do { WPUCbits.WPUC1 = 1; } while(0)
#define RC1_ResetPullup()           do { WPUCbits.WPUC1 = 0; } while(0)
#define RC1_SetAnalogMode()         do { ANSELCbits.ANSELC1 = 1; } while(0)
#define RC1_SetDigitalMode()        do { ANSELCbits.ANSELC1 = 0; } while(0)

// get/set TX_ENA aliases
#define TX_ENA_TRIS                 TRISCbits.TRISC2
#define TX_ENA_LAT                  LATCbits.LATC2
#define TX_ENA_PORT                 PORTCbits.RC2
#define TX_ENA_WPU                  WPUCbits.WPUC2
#define TX_ENA_OD                   ODCONCbits.ODCC2
#define TX_ENA_ANS                  ANSELCbits.ANSELC2
#define TX_ENA_SetHigh()            do { LATCbits.LATC2 = 1; } while(0)
#define TX_ENA_SetLow()             do { LATCbits.LATC2 = 0; } while(0)
#define TX_ENA_Toggle()             do { LATCbits.LATC2 = ~LATCbits.LATC2; } while(0)
#define TX_ENA_GetValue()           PORTCbits.RC2
#define TX_ENA_SetDigitalInput()    do { TRISCbits.TRISC2 = 1; } while(0)
#define TX_ENA_SetDigitalOutput()   do { TRISCbits.TRISC2 = 0; } while(0)
#define TX_ENA_SetPullup()          do { WPUCbits.WPUC2 = 1; } while(0)
#define TX_ENA_ResetPullup()        do { WPUCbits.WPUC2 = 0; } while(0)
#define TX_ENA_SetPushPull()        do { ODCONCbits.ODCC2 = 0; } while(0)
#define TX_ENA_SetOpenDrain()       do { ODCONCbits.ODCC2 = 1; } while(0)
#define TX_ENA_SetAnalogMode()      do { ANSELCbits.ANSELC2 = 1; } while(0)
#define TX_ENA_SetDigitalMode()     do { ANSELCbits.ANSELC2 = 0; } while(0)

// get/set RC3 procedures
#define RC3_SetHigh()            do { LATCbits.LATC3 = 1; } while(0)
#define RC3_SetLow()             do { LATCbits.LATC3 = 0; } while(0)
#define RC3_Toggle()             do { LATCbits.LATC3 = ~LATCbits.LATC3; } while(0)
#define RC3_GetValue()              PORTCbits.RC3
#define RC3_SetDigitalInput()    do { TRISCbits.TRISC3 = 1; } while(0)
#define RC3_SetDigitalOutput()   do { TRISCbits.TRISC3 = 0; } while(0)
#define RC3_SetPullup()             do { WPUCbits.WPUC3 = 1; } while(0)
#define RC3_ResetPullup()           do { WPUCbits.WPUC3 = 0; } while(0)
#define RC3_SetAnalogMode()         do { ANSELCbits.ANSELC3 = 1; } while(0)
#define RC3_SetDigitalMode()        do { ANSELCbits.ANSELC3 = 0; } while(0)

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