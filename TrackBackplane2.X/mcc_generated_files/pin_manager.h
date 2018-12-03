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

// get/set AMP_ID10_SET aliases
#define AMP_ID10_SET_TRIS                 TRISAbits.TRISA0
#define AMP_ID10_SET_LAT                  LATAbits.LATA0
#define AMP_ID10_SET_PORT                 PORTAbits.RA0
#define AMP_ID10_SET_WPU                  WPUAbits.WPUA0
#define AMP_ID10_SET_OD                   ODCONAbits.ODCA0
#define AMP_ID10_SET_ANS                  ANSELAbits.ANSELA0
#define AMP_ID10_SET_SetHigh()            do { LATAbits.LATA0 = 1; } while(0)
#define AMP_ID10_SET_SetLow()             do { LATAbits.LATA0 = 0; } while(0)
#define AMP_ID10_SET_Toggle()             do { LATAbits.LATA0 = ~LATAbits.LATA0; } while(0)
#define AMP_ID10_SET_GetValue()           PORTAbits.RA0
#define AMP_ID10_SET_SetDigitalInput()    do { TRISAbits.TRISA0 = 1; } while(0)
#define AMP_ID10_SET_SetDigitalOutput()   do { TRISAbits.TRISA0 = 0; } while(0)
#define AMP_ID10_SET_SetPullup()          do { WPUAbits.WPUA0 = 1; } while(0)
#define AMP_ID10_SET_ResetPullup()        do { WPUAbits.WPUA0 = 0; } while(0)
#define AMP_ID10_SET_SetPushPull()        do { ODCONAbits.ODCA0 = 0; } while(0)
#define AMP_ID10_SET_SetOpenDrain()       do { ODCONAbits.ODCA0 = 1; } while(0)
#define AMP_ID10_SET_SetAnalogMode()      do { ANSELAbits.ANSELA0 = 1; } while(0)
#define AMP_ID10_SET_SetDigitalMode()     do { ANSELAbits.ANSELA0 = 0; } while(0)

// get/set AMP_ID9_SET aliases
#define AMP_ID9_SET_TRIS                 TRISAbits.TRISA1
#define AMP_ID9_SET_LAT                  LATAbits.LATA1
#define AMP_ID9_SET_PORT                 PORTAbits.RA1
#define AMP_ID9_SET_WPU                  WPUAbits.WPUA1
#define AMP_ID9_SET_OD                   ODCONAbits.ODCA1
#define AMP_ID9_SET_ANS                  ANSELAbits.ANSELA1
#define AMP_ID9_SET_SetHigh()            do { LATAbits.LATA1 = 1; } while(0)
#define AMP_ID9_SET_SetLow()             do { LATAbits.LATA1 = 0; } while(0)
#define AMP_ID9_SET_Toggle()             do { LATAbits.LATA1 = ~LATAbits.LATA1; } while(0)
#define AMP_ID9_SET_GetValue()           PORTAbits.RA1
#define AMP_ID9_SET_SetDigitalInput()    do { TRISAbits.TRISA1 = 1; } while(0)
#define AMP_ID9_SET_SetDigitalOutput()   do { TRISAbits.TRISA1 = 0; } while(0)
#define AMP_ID9_SET_SetPullup()          do { WPUAbits.WPUA1 = 1; } while(0)
#define AMP_ID9_SET_ResetPullup()        do { WPUAbits.WPUA1 = 0; } while(0)
#define AMP_ID9_SET_SetPushPull()        do { ODCONAbits.ODCA1 = 0; } while(0)
#define AMP_ID9_SET_SetOpenDrain()       do { ODCONAbits.ODCA1 = 1; } while(0)
#define AMP_ID9_SET_SetAnalogMode()      do { ANSELAbits.ANSELA1 = 1; } while(0)
#define AMP_ID9_SET_SetDigitalMode()     do { ANSELAbits.ANSELA1 = 0; } while(0)

// get/set AMP_ID8_SET aliases
#define AMP_ID8_SET_TRIS                 TRISAbits.TRISA2
#define AMP_ID8_SET_LAT                  LATAbits.LATA2
#define AMP_ID8_SET_PORT                 PORTAbits.RA2
#define AMP_ID8_SET_WPU                  WPUAbits.WPUA2
#define AMP_ID8_SET_OD                   ODCONAbits.ODCA2
#define AMP_ID8_SET_ANS                  ANSELAbits.ANSELA2
#define AMP_ID8_SET_SetHigh()            do { LATAbits.LATA2 = 1; } while(0)
#define AMP_ID8_SET_SetLow()             do { LATAbits.LATA2 = 0; } while(0)
#define AMP_ID8_SET_Toggle()             do { LATAbits.LATA2 = ~LATAbits.LATA2; } while(0)
#define AMP_ID8_SET_GetValue()           PORTAbits.RA2
#define AMP_ID8_SET_SetDigitalInput()    do { TRISAbits.TRISA2 = 1; } while(0)
#define AMP_ID8_SET_SetDigitalOutput()   do { TRISAbits.TRISA2 = 0; } while(0)
#define AMP_ID8_SET_SetPullup()          do { WPUAbits.WPUA2 = 1; } while(0)
#define AMP_ID8_SET_ResetPullup()        do { WPUAbits.WPUA2 = 0; } while(0)
#define AMP_ID8_SET_SetPushPull()        do { ODCONAbits.ODCA2 = 0; } while(0)
#define AMP_ID8_SET_SetOpenDrain()       do { ODCONAbits.ODCA2 = 1; } while(0)
#define AMP_ID8_SET_SetAnalogMode()      do { ANSELAbits.ANSELA2 = 1; } while(0)
#define AMP_ID8_SET_SetDigitalMode()     do { ANSELAbits.ANSELA2 = 0; } while(0)

// get/set AMP_ID7_SET aliases
#define AMP_ID7_SET_TRIS                 TRISAbits.TRISA3
#define AMP_ID7_SET_LAT                  LATAbits.LATA3
#define AMP_ID7_SET_PORT                 PORTAbits.RA3
#define AMP_ID7_SET_WPU                  WPUAbits.WPUA3
#define AMP_ID7_SET_OD                   ODCONAbits.ODCA3
#define AMP_ID7_SET_ANS                  ANSELAbits.ANSELA3
#define AMP_ID7_SET_SetHigh()            do { LATAbits.LATA3 = 1; } while(0)
#define AMP_ID7_SET_SetLow()             do { LATAbits.LATA3 = 0; } while(0)
#define AMP_ID7_SET_Toggle()             do { LATAbits.LATA3 = ~LATAbits.LATA3; } while(0)
#define AMP_ID7_SET_GetValue()           PORTAbits.RA3
#define AMP_ID7_SET_SetDigitalInput()    do { TRISAbits.TRISA3 = 1; } while(0)
#define AMP_ID7_SET_SetDigitalOutput()   do { TRISAbits.TRISA3 = 0; } while(0)
#define AMP_ID7_SET_SetPullup()          do { WPUAbits.WPUA3 = 1; } while(0)
#define AMP_ID7_SET_ResetPullup()        do { WPUAbits.WPUA3 = 0; } while(0)
#define AMP_ID7_SET_SetPushPull()        do { ODCONAbits.ODCA3 = 0; } while(0)
#define AMP_ID7_SET_SetOpenDrain()       do { ODCONAbits.ODCA3 = 1; } while(0)
#define AMP_ID7_SET_SetAnalogMode()      do { ANSELAbits.ANSELA3 = 1; } while(0)
#define AMP_ID7_SET_SetDigitalMode()     do { ANSELAbits.ANSELA3 = 0; } while(0)

// get/set AMP_ID6_SET aliases
#define AMP_ID6_SET_TRIS                 TRISAbits.TRISA4
#define AMP_ID6_SET_LAT                  LATAbits.LATA4
#define AMP_ID6_SET_PORT                 PORTAbits.RA4
#define AMP_ID6_SET_WPU                  WPUAbits.WPUA4
#define AMP_ID6_SET_OD                   ODCONAbits.ODCA4
#define AMP_ID6_SET_ANS                  ANSELAbits.ANSELA4
#define AMP_ID6_SET_SetHigh()            do { LATAbits.LATA4 = 1; } while(0)
#define AMP_ID6_SET_SetLow()             do { LATAbits.LATA4 = 0; } while(0)
#define AMP_ID6_SET_Toggle()             do { LATAbits.LATA4 = ~LATAbits.LATA4; } while(0)
#define AMP_ID6_SET_GetValue()           PORTAbits.RA4
#define AMP_ID6_SET_SetDigitalInput()    do { TRISAbits.TRISA4 = 1; } while(0)
#define AMP_ID6_SET_SetDigitalOutput()   do { TRISAbits.TRISA4 = 0; } while(0)
#define AMP_ID6_SET_SetPullup()          do { WPUAbits.WPUA4 = 1; } while(0)
#define AMP_ID6_SET_ResetPullup()        do { WPUAbits.WPUA4 = 0; } while(0)
#define AMP_ID6_SET_SetPushPull()        do { ODCONAbits.ODCA4 = 0; } while(0)
#define AMP_ID6_SET_SetOpenDrain()       do { ODCONAbits.ODCA4 = 1; } while(0)
#define AMP_ID6_SET_SetAnalogMode()      do { ANSELAbits.ANSELA4 = 1; } while(0)
#define AMP_ID6_SET_SetDigitalMode()     do { ANSELAbits.ANSELA4 = 0; } while(0)

// get/set BACKPLANE_ID_BIT1 aliases
#define BACKPLANE_ID_BIT1_TRIS                 TRISAbits.TRISA5
#define BACKPLANE_ID_BIT1_LAT                  LATAbits.LATA5
#define BACKPLANE_ID_BIT1_PORT                 PORTAbits.RA5
#define BACKPLANE_ID_BIT1_WPU                  WPUAbits.WPUA5
#define BACKPLANE_ID_BIT1_OD                   ODCONAbits.ODCA5
#define BACKPLANE_ID_BIT1_ANS                  ANSELAbits.ANSELA5
#define BACKPLANE_ID_BIT1_SetHigh()            do { LATAbits.LATA5 = 1; } while(0)
#define BACKPLANE_ID_BIT1_SetLow()             do { LATAbits.LATA5 = 0; } while(0)
#define BACKPLANE_ID_BIT1_Toggle()             do { LATAbits.LATA5 = ~LATAbits.LATA5; } while(0)
#define BACKPLANE_ID_BIT1_GetValue()           PORTAbits.RA5
#define BACKPLANE_ID_BIT1_SetDigitalInput()    do { TRISAbits.TRISA5 = 1; } while(0)
#define BACKPLANE_ID_BIT1_SetDigitalOutput()   do { TRISAbits.TRISA5 = 0; } while(0)
#define BACKPLANE_ID_BIT1_SetPullup()          do { WPUAbits.WPUA5 = 1; } while(0)
#define BACKPLANE_ID_BIT1_ResetPullup()        do { WPUAbits.WPUA5 = 0; } while(0)
#define BACKPLANE_ID_BIT1_SetPushPull()        do { ODCONAbits.ODCA5 = 0; } while(0)
#define BACKPLANE_ID_BIT1_SetOpenDrain()       do { ODCONAbits.ODCA5 = 1; } while(0)
#define BACKPLANE_ID_BIT1_SetAnalogMode()      do { ANSELAbits.ANSELA5 = 1; } while(0)
#define BACKPLANE_ID_BIT1_SetDigitalMode()     do { ANSELAbits.ANSELA5 = 0; } while(0)

// get/set BACKPLANE_ID_BIT2 aliases
#define BACKPLANE_ID_BIT2_TRIS                 TRISAbits.TRISA6
#define BACKPLANE_ID_BIT2_LAT                  LATAbits.LATA6
#define BACKPLANE_ID_BIT2_PORT                 PORTAbits.RA6
#define BACKPLANE_ID_BIT2_WPU                  WPUAbits.WPUA6
#define BACKPLANE_ID_BIT2_OD                   ODCONAbits.ODCA6
#define BACKPLANE_ID_BIT2_ANS                  ANSELAbits.ANSELA6
#define BACKPLANE_ID_BIT2_SetHigh()            do { LATAbits.LATA6 = 1; } while(0)
#define BACKPLANE_ID_BIT2_SetLow()             do { LATAbits.LATA6 = 0; } while(0)
#define BACKPLANE_ID_BIT2_Toggle()             do { LATAbits.LATA6 = ~LATAbits.LATA6; } while(0)
#define BACKPLANE_ID_BIT2_GetValue()           PORTAbits.RA6
#define BACKPLANE_ID_BIT2_SetDigitalInput()    do { TRISAbits.TRISA6 = 1; } while(0)
#define BACKPLANE_ID_BIT2_SetDigitalOutput()   do { TRISAbits.TRISA6 = 0; } while(0)
#define BACKPLANE_ID_BIT2_SetPullup()          do { WPUAbits.WPUA6 = 1; } while(0)
#define BACKPLANE_ID_BIT2_ResetPullup()        do { WPUAbits.WPUA6 = 0; } while(0)
#define BACKPLANE_ID_BIT2_SetPushPull()        do { ODCONAbits.ODCA6 = 0; } while(0)
#define BACKPLANE_ID_BIT2_SetOpenDrain()       do { ODCONAbits.ODCA6 = 1; } while(0)
#define BACKPLANE_ID_BIT2_SetAnalogMode()      do { ANSELAbits.ANSELA6 = 1; } while(0)
#define BACKPLANE_ID_BIT2_SetDigitalMode()     do { ANSELAbits.ANSELA6 = 0; } while(0)

// get/set BACKPLANE_ID_BIT4 aliases
#define BACKPLANE_ID_BIT4_TRIS                 TRISAbits.TRISA7
#define BACKPLANE_ID_BIT4_LAT                  LATAbits.LATA7
#define BACKPLANE_ID_BIT4_PORT                 PORTAbits.RA7
#define BACKPLANE_ID_BIT4_WPU                  WPUAbits.WPUA7
#define BACKPLANE_ID_BIT4_OD                   ODCONAbits.ODCA7
#define BACKPLANE_ID_BIT4_ANS                  ANSELAbits.ANSELA7
#define BACKPLANE_ID_BIT4_SetHigh()            do { LATAbits.LATA7 = 1; } while(0)
#define BACKPLANE_ID_BIT4_SetLow()             do { LATAbits.LATA7 = 0; } while(0)
#define BACKPLANE_ID_BIT4_Toggle()             do { LATAbits.LATA7 = ~LATAbits.LATA7; } while(0)
#define BACKPLANE_ID_BIT4_GetValue()           PORTAbits.RA7
#define BACKPLANE_ID_BIT4_SetDigitalInput()    do { TRISAbits.TRISA7 = 1; } while(0)
#define BACKPLANE_ID_BIT4_SetDigitalOutput()   do { TRISAbits.TRISA7 = 0; } while(0)
#define BACKPLANE_ID_BIT4_SetPullup()          do { WPUAbits.WPUA7 = 1; } while(0)
#define BACKPLANE_ID_BIT4_ResetPullup()        do { WPUAbits.WPUA7 = 0; } while(0)
#define BACKPLANE_ID_BIT4_SetPushPull()        do { ODCONAbits.ODCA7 = 0; } while(0)
#define BACKPLANE_ID_BIT4_SetOpenDrain()       do { ODCONAbits.ODCA7 = 1; } while(0)
#define BACKPLANE_ID_BIT4_SetAnalogMode()      do { ANSELAbits.ANSELA7 = 1; } while(0)
#define BACKPLANE_ID_BIT4_SetDigitalMode()     do { ANSELAbits.ANSELA7 = 0; } while(0)

// get/set AMP_ID1_SET aliases
#define AMP_ID1_SET_TRIS                 TRISBbits.TRISB0
#define AMP_ID1_SET_LAT                  LATBbits.LATB0
#define AMP_ID1_SET_PORT                 PORTBbits.RB0
#define AMP_ID1_SET_WPU                  WPUBbits.WPUB0
#define AMP_ID1_SET_OD                   ODCONBbits.ODCB0
#define AMP_ID1_SET_ANS                  ANSELBbits.ANSELB0
#define AMP_ID1_SET_SetHigh()            do { LATBbits.LATB0 = 1; } while(0)
#define AMP_ID1_SET_SetLow()             do { LATBbits.LATB0 = 0; } while(0)
#define AMP_ID1_SET_Toggle()             do { LATBbits.LATB0 = ~LATBbits.LATB0; } while(0)
#define AMP_ID1_SET_GetValue()           PORTBbits.RB0
#define AMP_ID1_SET_SetDigitalInput()    do { TRISBbits.TRISB0 = 1; } while(0)
#define AMP_ID1_SET_SetDigitalOutput()   do { TRISBbits.TRISB0 = 0; } while(0)
#define AMP_ID1_SET_SetPullup()          do { WPUBbits.WPUB0 = 1; } while(0)
#define AMP_ID1_SET_ResetPullup()        do { WPUBbits.WPUB0 = 0; } while(0)
#define AMP_ID1_SET_SetPushPull()        do { ODCONBbits.ODCB0 = 0; } while(0)
#define AMP_ID1_SET_SetOpenDrain()       do { ODCONBbits.ODCB0 = 1; } while(0)
#define AMP_ID1_SET_SetAnalogMode()      do { ANSELBbits.ANSELB0 = 1; } while(0)
#define AMP_ID1_SET_SetDigitalMode()     do { ANSELBbits.ANSELB0 = 0; } while(0)

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

// get/set ICSPCLK aliases
#define ICSPCLK_TRIS                 TRISBbits.TRISB6
#define ICSPCLK_LAT                  LATBbits.LATB6
#define ICSPCLK_PORT                 PORTBbits.RB6
#define ICSPCLK_WPU                  WPUBbits.WPUB6
#define ICSPCLK_OD                   ODCONBbits.ODCB6
#define ICSPCLK_ANS                  ANSELBbits.ANSELB6
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
#define ICSPCLK_SetAnalogMode()      do { ANSELBbits.ANSELB6 = 1; } while(0)
#define ICSPCLK_SetDigitalMode()     do { ANSELBbits.ANSELB6 = 0; } while(0)

// get/set ICSPDAT aliases
#define ICSPDAT_TRIS                 TRISBbits.TRISB7
#define ICSPDAT_LAT                  LATBbits.LATB7
#define ICSPDAT_PORT                 PORTBbits.RB7
#define ICSPDAT_WPU                  WPUBbits.WPUB7
#define ICSPDAT_OD                   ODCONBbits.ODCB7
#define ICSPDAT_ANS                  ANSELBbits.ANSELB7
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
#define ICSPDAT_SetAnalogMode()      do { ANSELBbits.ANSELB7 = 1; } while(0)
#define ICSPDAT_SetDigitalMode()     do { ANSELBbits.ANSELB7 = 0; } while(0)

// get/set SYNC aliases
#define SYNC_TRIS                 TRISCbits.TRISC0
#define SYNC_LAT                  LATCbits.LATC0
#define SYNC_PORT                 PORTCbits.RC0
#define SYNC_WPU                  WPUCbits.WPUC0
#define SYNC_OD                   ODCONCbits.ODCC0
#define SYNC_ANS                  ANSELCbits.ANSELC0
#define SYNC_SetHigh()            do { LATCbits.LATC0 = 1; } while(0)
#define SYNC_SetLow()             do { LATCbits.LATC0 = 0; } while(0)
#define SYNC_Toggle()             do { LATCbits.LATC0 = ~LATCbits.LATC0; } while(0)
#define SYNC_GetValue()           PORTCbits.RC0
#define SYNC_SetDigitalInput()    do { TRISCbits.TRISC0 = 1; } while(0)
#define SYNC_SetDigitalOutput()   do { TRISCbits.TRISC0 = 0; } while(0)
#define SYNC_SetPullup()          do { WPUCbits.WPUC0 = 1; } while(0)
#define SYNC_ResetPullup()        do { WPUCbits.WPUC0 = 0; } while(0)
#define SYNC_SetPushPull()        do { ODCONCbits.ODCC0 = 0; } while(0)
#define SYNC_SetOpenDrain()       do { ODCONCbits.ODCC0 = 1; } while(0)
#define SYNC_SetAnalogMode()      do { ANSELCbits.ANSELC0 = 1; } while(0)
#define SYNC_SetDigitalMode()     do { ANSELCbits.ANSELC0 = 0; } while(0)

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

// get/set AMP_ID5_SET aliases
#define AMP_ID5_SET_TRIS                 TRISCbits.TRISC4
#define AMP_ID5_SET_LAT                  LATCbits.LATC4
#define AMP_ID5_SET_PORT                 PORTCbits.RC4
#define AMP_ID5_SET_WPU                  WPUCbits.WPUC4
#define AMP_ID5_SET_OD                   ODCONCbits.ODCC4
#define AMP_ID5_SET_ANS                  ANSELCbits.ANSELC4
#define AMP_ID5_SET_SetHigh()            do { LATCbits.LATC4 = 1; } while(0)
#define AMP_ID5_SET_SetLow()             do { LATCbits.LATC4 = 0; } while(0)
#define AMP_ID5_SET_Toggle()             do { LATCbits.LATC4 = ~LATCbits.LATC4; } while(0)
#define AMP_ID5_SET_GetValue()           PORTCbits.RC4
#define AMP_ID5_SET_SetDigitalInput()    do { TRISCbits.TRISC4 = 1; } while(0)
#define AMP_ID5_SET_SetDigitalOutput()   do { TRISCbits.TRISC4 = 0; } while(0)
#define AMP_ID5_SET_SetPullup()          do { WPUCbits.WPUC4 = 1; } while(0)
#define AMP_ID5_SET_ResetPullup()        do { WPUCbits.WPUC4 = 0; } while(0)
#define AMP_ID5_SET_SetPushPull()        do { ODCONCbits.ODCC4 = 0; } while(0)
#define AMP_ID5_SET_SetOpenDrain()       do { ODCONCbits.ODCC4 = 1; } while(0)
#define AMP_ID5_SET_SetAnalogMode()      do { ANSELCbits.ANSELC4 = 1; } while(0)
#define AMP_ID5_SET_SetDigitalMode()     do { ANSELCbits.ANSELC4 = 0; } while(0)

// get/set AMP_ID4_SET aliases
#define AMP_ID4_SET_TRIS                 TRISCbits.TRISC5
#define AMP_ID4_SET_LAT                  LATCbits.LATC5
#define AMP_ID4_SET_PORT                 PORTCbits.RC5
#define AMP_ID4_SET_WPU                  WPUCbits.WPUC5
#define AMP_ID4_SET_OD                   ODCONCbits.ODCC5
#define AMP_ID4_SET_ANS                  ANSELCbits.ANSELC5
#define AMP_ID4_SET_SetHigh()            do { LATCbits.LATC5 = 1; } while(0)
#define AMP_ID4_SET_SetLow()             do { LATCbits.LATC5 = 0; } while(0)
#define AMP_ID4_SET_Toggle()             do { LATCbits.LATC5 = ~LATCbits.LATC5; } while(0)
#define AMP_ID4_SET_GetValue()           PORTCbits.RC5
#define AMP_ID4_SET_SetDigitalInput()    do { TRISCbits.TRISC5 = 1; } while(0)
#define AMP_ID4_SET_SetDigitalOutput()   do { TRISCbits.TRISC5 = 0; } while(0)
#define AMP_ID4_SET_SetPullup()          do { WPUCbits.WPUC5 = 1; } while(0)
#define AMP_ID4_SET_ResetPullup()        do { WPUCbits.WPUC5 = 0; } while(0)
#define AMP_ID4_SET_SetPushPull()        do { ODCONCbits.ODCC5 = 0; } while(0)
#define AMP_ID4_SET_SetOpenDrain()       do { ODCONCbits.ODCC5 = 1; } while(0)
#define AMP_ID4_SET_SetAnalogMode()      do { ANSELCbits.ANSELC5 = 1; } while(0)
#define AMP_ID4_SET_SetDigitalMode()     do { ANSELCbits.ANSELC5 = 0; } while(0)

// get/set AMP_ID3_SET aliases
#define AMP_ID3_SET_TRIS                 TRISCbits.TRISC6
#define AMP_ID3_SET_LAT                  LATCbits.LATC6
#define AMP_ID3_SET_PORT                 PORTCbits.RC6
#define AMP_ID3_SET_WPU                  WPUCbits.WPUC6
#define AMP_ID3_SET_OD                   ODCONCbits.ODCC6
#define AMP_ID3_SET_ANS                  ANSELCbits.ANSELC6
#define AMP_ID3_SET_SetHigh()            do { LATCbits.LATC6 = 1; } while(0)
#define AMP_ID3_SET_SetLow()             do { LATCbits.LATC6 = 0; } while(0)
#define AMP_ID3_SET_Toggle()             do { LATCbits.LATC6 = ~LATCbits.LATC6; } while(0)
#define AMP_ID3_SET_GetValue()           PORTCbits.RC6
#define AMP_ID3_SET_SetDigitalInput()    do { TRISCbits.TRISC6 = 1; } while(0)
#define AMP_ID3_SET_SetDigitalOutput()   do { TRISCbits.TRISC6 = 0; } while(0)
#define AMP_ID3_SET_SetPullup()          do { WPUCbits.WPUC6 = 1; } while(0)
#define AMP_ID3_SET_ResetPullup()        do { WPUCbits.WPUC6 = 0; } while(0)
#define AMP_ID3_SET_SetPushPull()        do { ODCONCbits.ODCC6 = 0; } while(0)
#define AMP_ID3_SET_SetOpenDrain()       do { ODCONCbits.ODCC6 = 1; } while(0)
#define AMP_ID3_SET_SetAnalogMode()      do { ANSELCbits.ANSELC6 = 1; } while(0)
#define AMP_ID3_SET_SetDigitalMode()     do { ANSELCbits.ANSELC6 = 0; } while(0)

// get/set AMP_ID2_SET aliases
#define AMP_ID2_SET_TRIS                 TRISCbits.TRISC7
#define AMP_ID2_SET_LAT                  LATCbits.LATC7
#define AMP_ID2_SET_PORT                 PORTCbits.RC7
#define AMP_ID2_SET_WPU                  WPUCbits.WPUC7
#define AMP_ID2_SET_OD                   ODCONCbits.ODCC7
#define AMP_ID2_SET_ANS                  ANSELCbits.ANSELC7
#define AMP_ID2_SET_SetHigh()            do { LATCbits.LATC7 = 1; } while(0)
#define AMP_ID2_SET_SetLow()             do { LATCbits.LATC7 = 0; } while(0)
#define AMP_ID2_SET_Toggle()             do { LATCbits.LATC7 = ~LATCbits.LATC7; } while(0)
#define AMP_ID2_SET_GetValue()           PORTCbits.RC7
#define AMP_ID2_SET_SetDigitalInput()    do { TRISCbits.TRISC7 = 1; } while(0)
#define AMP_ID2_SET_SetDigitalOutput()   do { TRISCbits.TRISC7 = 0; } while(0)
#define AMP_ID2_SET_SetPullup()          do { WPUCbits.WPUC7 = 1; } while(0)
#define AMP_ID2_SET_ResetPullup()        do { WPUCbits.WPUC7 = 0; } while(0)
#define AMP_ID2_SET_SetPushPull()        do { ODCONCbits.ODCC7 = 0; } while(0)
#define AMP_ID2_SET_SetOpenDrain()       do { ODCONCbits.ODCC7 = 1; } while(0)
#define AMP_ID2_SET_SetAnalogMode()      do { ANSELCbits.ANSELC7 = 1; } while(0)
#define AMP_ID2_SET_SetDigitalMode()     do { ANSELCbits.ANSELC7 = 0; } while(0)

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