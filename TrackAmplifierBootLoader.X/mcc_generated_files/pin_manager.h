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

// get/set IO_RB1 aliases
#define IO_RB1_TRIS                 TRISBbits.TRISB1
#define IO_RB1_LAT                  LATBbits.LATB1
#define IO_RB1_PORT                 PORTBbits.RB1
#define IO_RB1_WPU                  WPUBbits.WPUB1
#define IO_RB1_OD                   ODCONBbits.ODCB1
#define IO_RB1_ANS                  ANSELBbits.ANSELB1
#define IO_RB1_SetHigh()            do { LATBbits.LATB1 = 1; } while(0)
#define IO_RB1_SetLow()             do { LATBbits.LATB1 = 0; } while(0)
#define IO_RB1_Toggle()             do { LATBbits.LATB1 = ~LATBbits.LATB1; } while(0)
#define IO_RB1_GetValue()           PORTBbits.RB1
#define IO_RB1_SetDigitalInput()    do { TRISBbits.TRISB1 = 1; } while(0)
#define IO_RB1_SetDigitalOutput()   do { TRISBbits.TRISB1 = 0; } while(0)
#define IO_RB1_SetPullup()          do { WPUBbits.WPUB1 = 1; } while(0)
#define IO_RB1_ResetPullup()        do { WPUBbits.WPUB1 = 0; } while(0)
#define IO_RB1_SetPushPull()        do { ODCONBbits.ODCB1 = 0; } while(0)
#define IO_RB1_SetOpenDrain()       do { ODCONBbits.ODCB1 = 1; } while(0)
#define IO_RB1_SetAnalogMode()      do { ANSELBbits.ANSELB1 = 1; } while(0)
#define IO_RB1_SetDigitalMode()     do { ANSELBbits.ANSELB1 = 0; } while(0)

// get/set IO_RB2 aliases
#define IO_RB2_TRIS                 TRISBbits.TRISB2
#define IO_RB2_LAT                  LATBbits.LATB2
#define IO_RB2_PORT                 PORTBbits.RB2
#define IO_RB2_WPU                  WPUBbits.WPUB2
#define IO_RB2_OD                   ODCONBbits.ODCB2
#define IO_RB2_ANS                  ANSELBbits.ANSELB2
#define IO_RB2_SetHigh()            do { LATBbits.LATB2 = 1; } while(0)
#define IO_RB2_SetLow()             do { LATBbits.LATB2 = 0; } while(0)
#define IO_RB2_Toggle()             do { LATBbits.LATB2 = ~LATBbits.LATB2; } while(0)
#define IO_RB2_GetValue()           PORTBbits.RB2
#define IO_RB2_SetDigitalInput()    do { TRISBbits.TRISB2 = 1; } while(0)
#define IO_RB2_SetDigitalOutput()   do { TRISBbits.TRISB2 = 0; } while(0)
#define IO_RB2_SetPullup()          do { WPUBbits.WPUB2 = 1; } while(0)
#define IO_RB2_ResetPullup()        do { WPUBbits.WPUB2 = 0; } while(0)
#define IO_RB2_SetPushPull()        do { ODCONBbits.ODCB2 = 0; } while(0)
#define IO_RB2_SetOpenDrain()       do { ODCONBbits.ODCB2 = 1; } while(0)
#define IO_RB2_SetAnalogMode()      do { ANSELBbits.ANSELB2 = 1; } while(0)
#define IO_RB2_SetDigitalMode()     do { ANSELBbits.ANSELB2 = 0; } while(0)

// get/set IO_RB3 aliases
#define IO_RB3_TRIS                 TRISBbits.TRISB3
#define IO_RB3_LAT                  LATBbits.LATB3
#define IO_RB3_PORT                 PORTBbits.RB3
#define IO_RB3_WPU                  WPUBbits.WPUB3
#define IO_RB3_OD                   ODCONBbits.ODCB3
#define IO_RB3_ANS                  ANSELBbits.ANSELB3
#define IO_RB3_SetHigh()            do { LATBbits.LATB3 = 1; } while(0)
#define IO_RB3_SetLow()             do { LATBbits.LATB3 = 0; } while(0)
#define IO_RB3_Toggle()             do { LATBbits.LATB3 = ~LATBbits.LATB3; } while(0)
#define IO_RB3_GetValue()           PORTBbits.RB3
#define IO_RB3_SetDigitalInput()    do { TRISBbits.TRISB3 = 1; } while(0)
#define IO_RB3_SetDigitalOutput()   do { TRISBbits.TRISB3 = 0; } while(0)
#define IO_RB3_SetPullup()          do { WPUBbits.WPUB3 = 1; } while(0)
#define IO_RB3_ResetPullup()        do { WPUBbits.WPUB3 = 0; } while(0)
#define IO_RB3_SetPushPull()        do { ODCONBbits.ODCB3 = 0; } while(0)
#define IO_RB3_SetOpenDrain()       do { ODCONBbits.ODCB3 = 1; } while(0)
#define IO_RB3_SetAnalogMode()      do { ANSELBbits.ANSELB3 = 1; } while(0)
#define IO_RB3_SetDigitalMode()     do { ANSELBbits.ANSELB3 = 0; } while(0)

// get/set IO_RB4 aliases
#define IO_RB4_TRIS                 TRISBbits.TRISB4
#define IO_RB4_LAT                  LATBbits.LATB4
#define IO_RB4_PORT                 PORTBbits.RB4
#define IO_RB4_WPU                  WPUBbits.WPUB4
#define IO_RB4_OD                   ODCONBbits.ODCB4
#define IO_RB4_ANS                  ANSELBbits.ANSELB4
#define IO_RB4_SetHigh()            do { LATBbits.LATB4 = 1; } while(0)
#define IO_RB4_SetLow()             do { LATBbits.LATB4 = 0; } while(0)
#define IO_RB4_Toggle()             do { LATBbits.LATB4 = ~LATBbits.LATB4; } while(0)
#define IO_RB4_GetValue()           PORTBbits.RB4
#define IO_RB4_SetDigitalInput()    do { TRISBbits.TRISB4 = 1; } while(0)
#define IO_RB4_SetDigitalOutput()   do { TRISBbits.TRISB4 = 0; } while(0)
#define IO_RB4_SetPullup()          do { WPUBbits.WPUB4 = 1; } while(0)
#define IO_RB4_ResetPullup()        do { WPUBbits.WPUB4 = 0; } while(0)
#define IO_RB4_SetPushPull()        do { ODCONBbits.ODCB4 = 0; } while(0)
#define IO_RB4_SetOpenDrain()       do { ODCONBbits.ODCB4 = 1; } while(0)
#define IO_RB4_SetAnalogMode()      do { ANSELBbits.ANSELB4 = 1; } while(0)
#define IO_RB4_SetDigitalMode()     do { ANSELBbits.ANSELB4 = 0; } while(0)

// get/set IO_RB5 aliases
#define IO_RB5_TRIS                 TRISBbits.TRISB5
#define IO_RB5_LAT                  LATBbits.LATB5
#define IO_RB5_PORT                 PORTBbits.RB5
#define IO_RB5_WPU                  WPUBbits.WPUB5
#define IO_RB5_OD                   ODCONBbits.ODCB5
#define IO_RB5_ANS                  ANSELBbits.ANSELB5
#define IO_RB5_SetHigh()            do { LATBbits.LATB5 = 1; } while(0)
#define IO_RB5_SetLow()             do { LATBbits.LATB5 = 0; } while(0)
#define IO_RB5_Toggle()             do { LATBbits.LATB5 = ~LATBbits.LATB5; } while(0)
#define IO_RB5_GetValue()           PORTBbits.RB5
#define IO_RB5_SetDigitalInput()    do { TRISBbits.TRISB5 = 1; } while(0)
#define IO_RB5_SetDigitalOutput()   do { TRISBbits.TRISB5 = 0; } while(0)
#define IO_RB5_SetPullup()          do { WPUBbits.WPUB5 = 1; } while(0)
#define IO_RB5_ResetPullup()        do { WPUBbits.WPUB5 = 0; } while(0)
#define IO_RB5_SetPushPull()        do { ODCONBbits.ODCB5 = 0; } while(0)
#define IO_RB5_SetOpenDrain()       do { ODCONBbits.ODCB5 = 1; } while(0)
#define IO_RB5_SetAnalogMode()      do { ANSELBbits.ANSELB5 = 1; } while(0)
#define IO_RB5_SetDigitalMode()     do { ANSELBbits.ANSELB5 = 0; } while(0)

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