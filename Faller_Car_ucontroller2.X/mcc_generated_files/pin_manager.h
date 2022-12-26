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
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.81.8
        Device            :  PIC16F15345
        Driver Version    :  2.11
    The generated drivers are tested against the following:
        Compiler          :  XC8 2.36 and above
        MPLAB 	          :  MPLAB X 6.00	
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

/**
  Section: Included Files
*/

#include <xc.h>

#define INPUT   1
#define OUTPUT  0

#define HIGH    1
#define LOW     0

#define ANALOG      1
#define DIGITAL     0

#define PULL_UP_ENABLED      1
#define PULL_UP_DISABLED     0

// get/set ICSPDAT aliases
#define ICSPDAT_TRIS                 TRISAbits.TRISA0
#define ICSPDAT_LAT                  LATAbits.LATA0
#define ICSPDAT_PORT                 PORTAbits.RA0
#define ICSPDAT_WPU                  WPUAbits.WPUA0
#define ICSPDAT_OD                   ODCONAbits.ODCA0
#define ICSPDAT_ANS                  ANSELAbits.ANSA0
#define ICSPDAT_SetHigh()            do { LATAbits.LATA0 = 1; } while(0)
#define ICSPDAT_SetLow()             do { LATAbits.LATA0 = 0; } while(0)
#define ICSPDAT_Toggle()             do { LATAbits.LATA0 = ~LATAbits.LATA0; } while(0)
#define ICSPDAT_GetValue()           PORTAbits.RA0
#define ICSPDAT_SetDigitalInput()    do { TRISAbits.TRISA0 = 1; } while(0)
#define ICSPDAT_SetDigitalOutput()   do { TRISAbits.TRISA0 = 0; } while(0)
#define ICSPDAT_SetPullup()          do { WPUAbits.WPUA0 = 1; } while(0)
#define ICSPDAT_ResetPullup()        do { WPUAbits.WPUA0 = 0; } while(0)
#define ICSPDAT_SetPushPull()        do { ODCONAbits.ODCA0 = 0; } while(0)
#define ICSPDAT_SetOpenDrain()       do { ODCONAbits.ODCA0 = 1; } while(0)
#define ICSPDAT_SetAnalogMode()      do { ANSELAbits.ANSA0 = 1; } while(0)
#define ICSPDAT_SetDigitalMode()     do { ANSELAbits.ANSA0 = 0; } while(0)

// get/set ICSPCLK aliases
#define ICSPCLK_TRIS                 TRISAbits.TRISA1
#define ICSPCLK_LAT                  LATAbits.LATA1
#define ICSPCLK_PORT                 PORTAbits.RA1
#define ICSPCLK_WPU                  WPUAbits.WPUA1
#define ICSPCLK_OD                   ODCONAbits.ODCA1
#define ICSPCLK_ANS                  ANSELAbits.ANSA1
#define ICSPCLK_SetHigh()            do { LATAbits.LATA1 = 1; } while(0)
#define ICSPCLK_SetLow()             do { LATAbits.LATA1 = 0; } while(0)
#define ICSPCLK_Toggle()             do { LATAbits.LATA1 = ~LATAbits.LATA1; } while(0)
#define ICSPCLK_GetValue()           PORTAbits.RA1
#define ICSPCLK_SetDigitalInput()    do { TRISAbits.TRISA1 = 1; } while(0)
#define ICSPCLK_SetDigitalOutput()   do { TRISAbits.TRISA1 = 0; } while(0)
#define ICSPCLK_SetPullup()          do { WPUAbits.WPUA1 = 1; } while(0)
#define ICSPCLK_ResetPullup()        do { WPUAbits.WPUA1 = 0; } while(0)
#define ICSPCLK_SetPushPull()        do { ODCONAbits.ODCA1 = 0; } while(0)
#define ICSPCLK_SetOpenDrain()       do { ODCONAbits.ODCA1 = 1; } while(0)
#define ICSPCLK_SetAnalogMode()      do { ANSELAbits.ANSA1 = 1; } while(0)
#define ICSPCLK_SetDigitalMode()     do { ANSELAbits.ANSA1 = 0; } while(0)

// get/set RA2 procedures
#define RA2_SetHigh()            do { LATAbits.LATA2 = 1; } while(0)
#define RA2_SetLow()             do { LATAbits.LATA2 = 0; } while(0)
#define RA2_Toggle()             do { LATAbits.LATA2 = ~LATAbits.LATA2; } while(0)
#define RA2_GetValue()              PORTAbits.RA2
#define RA2_SetDigitalInput()    do { TRISAbits.TRISA2 = 1; } while(0)
#define RA2_SetDigitalOutput()   do { TRISAbits.TRISA2 = 0; } while(0)
#define RA2_SetPullup()             do { WPUAbits.WPUA2 = 1; } while(0)
#define RA2_ResetPullup()           do { WPUAbits.WPUA2 = 0; } while(0)
#define RA2_SetAnalogMode()         do { ANSELAbits.ANSA2 = 1; } while(0)
#define RA2_SetDigitalMode()        do { ANSELAbits.ANSA2 = 0; } while(0)

// get/set LEDA aliases
#define LEDA_TRIS                 TRISAbits.TRISA4
#define LEDA_LAT                  LATAbits.LATA4
#define LEDA_PORT                 PORTAbits.RA4
#define LEDA_WPU                  WPUAbits.WPUA4
#define LEDA_OD                   ODCONAbits.ODCA4
#define LEDA_ANS                  ANSELAbits.ANSA4
#define LEDA_SetHigh()            do { LATAbits.LATA4 = 1; } while(0)
#define LEDA_SetLow()             do { LATAbits.LATA4 = 0; } while(0)
#define LEDA_Toggle()             do { LATAbits.LATA4 = ~LATAbits.LATA4; } while(0)
#define LEDA_GetValue()           PORTAbits.RA4
#define LEDA_SetDigitalInput()    do { TRISAbits.TRISA4 = 1; } while(0)
#define LEDA_SetDigitalOutput()   do { TRISAbits.TRISA4 = 0; } while(0)
#define LEDA_SetPullup()          do { WPUAbits.WPUA4 = 1; } while(0)
#define LEDA_ResetPullup()        do { WPUAbits.WPUA4 = 0; } while(0)
#define LEDA_SetPushPull()        do { ODCONAbits.ODCA4 = 0; } while(0)
#define LEDA_SetOpenDrain()       do { ODCONAbits.ODCA4 = 1; } while(0)
#define LEDA_SetAnalogMode()      do { ANSELAbits.ANSA4 = 1; } while(0)
#define LEDA_SetDigitalMode()     do { ANSELAbits.ANSA4 = 0; } while(0)

// get/set LEDB aliases
#define LEDB_TRIS                 TRISAbits.TRISA5
#define LEDB_LAT                  LATAbits.LATA5
#define LEDB_PORT                 PORTAbits.RA5
#define LEDB_WPU                  WPUAbits.WPUA5
#define LEDB_OD                   ODCONAbits.ODCA5
#define LEDB_ANS                  ANSELAbits.ANSA5
#define LEDB_SetHigh()            do { LATAbits.LATA5 = 1; } while(0)
#define LEDB_SetLow()             do { LATAbits.LATA5 = 0; } while(0)
#define LEDB_Toggle()             do { LATAbits.LATA5 = ~LATAbits.LATA5; } while(0)
#define LEDB_GetValue()           PORTAbits.RA5
#define LEDB_SetDigitalInput()    do { TRISAbits.TRISA5 = 1; } while(0)
#define LEDB_SetDigitalOutput()   do { TRISAbits.TRISA5 = 0; } while(0)
#define LEDB_SetPullup()          do { WPUAbits.WPUA5 = 1; } while(0)
#define LEDB_ResetPullup()        do { WPUAbits.WPUA5 = 0; } while(0)
#define LEDB_SetPushPull()        do { ODCONAbits.ODCA5 = 0; } while(0)
#define LEDB_SetOpenDrain()       do { ODCONAbits.ODCA5 = 1; } while(0)
#define LEDB_SetAnalogMode()      do { ANSELAbits.ANSA5 = 1; } while(0)
#define LEDB_SetDigitalMode()     do { ANSELAbits.ANSA5 = 0; } while(0)

// get/set LEDC aliases
#define LEDC_TRIS                 TRISBbits.TRISB4
#define LEDC_LAT                  LATBbits.LATB4
#define LEDC_PORT                 PORTBbits.RB4
#define LEDC_WPU                  WPUBbits.WPUB4
#define LEDC_OD                   ODCONBbits.ODCB4
#define LEDC_ANS                  ANSELBbits.ANSB4
#define LEDC_SetHigh()            do { LATBbits.LATB4 = 1; } while(0)
#define LEDC_SetLow()             do { LATBbits.LATB4 = 0; } while(0)
#define LEDC_Toggle()             do { LATBbits.LATB4 = ~LATBbits.LATB4; } while(0)
#define LEDC_GetValue()           PORTBbits.RB4
#define LEDC_SetDigitalInput()    do { TRISBbits.TRISB4 = 1; } while(0)
#define LEDC_SetDigitalOutput()   do { TRISBbits.TRISB4 = 0; } while(0)
#define LEDC_SetPullup()          do { WPUBbits.WPUB4 = 1; } while(0)
#define LEDC_ResetPullup()        do { WPUBbits.WPUB4 = 0; } while(0)
#define LEDC_SetPushPull()        do { ODCONBbits.ODCB4 = 0; } while(0)
#define LEDC_SetOpenDrain()       do { ODCONBbits.ODCB4 = 1; } while(0)
#define LEDC_SetAnalogMode()      do { ANSELBbits.ANSB4 = 1; } while(0)
#define LEDC_SetDigitalMode()     do { ANSELBbits.ANSB4 = 0; } while(0)

// get/set LEDD aliases
#define LEDD_TRIS                 TRISBbits.TRISB5
#define LEDD_LAT                  LATBbits.LATB5
#define LEDD_PORT                 PORTBbits.RB5
#define LEDD_WPU                  WPUBbits.WPUB5
#define LEDD_OD                   ODCONBbits.ODCB5
#define LEDD_ANS                  ANSELBbits.ANSB5
#define LEDD_SetHigh()            do { LATBbits.LATB5 = 1; } while(0)
#define LEDD_SetLow()             do { LATBbits.LATB5 = 0; } while(0)
#define LEDD_Toggle()             do { LATBbits.LATB5 = ~LATBbits.LATB5; } while(0)
#define LEDD_GetValue()           PORTBbits.RB5
#define LEDD_SetDigitalInput()    do { TRISBbits.TRISB5 = 1; } while(0)
#define LEDD_SetDigitalOutput()   do { TRISBbits.TRISB5 = 0; } while(0)
#define LEDD_SetPullup()          do { WPUBbits.WPUB5 = 1; } while(0)
#define LEDD_ResetPullup()        do { WPUBbits.WPUB5 = 0; } while(0)
#define LEDD_SetPushPull()        do { ODCONBbits.ODCB5 = 0; } while(0)
#define LEDD_SetOpenDrain()       do { ODCONBbits.ODCB5 = 1; } while(0)
#define LEDD_SetAnalogMode()      do { ANSELBbits.ANSB5 = 1; } while(0)
#define LEDD_SetDigitalMode()     do { ANSELBbits.ANSB5 = 0; } while(0)

// get/set RB6 procedures
#define RB6_SetHigh()            do { LATBbits.LATB6 = 1; } while(0)
#define RB6_SetLow()             do { LATBbits.LATB6 = 0; } while(0)
#define RB6_Toggle()             do { LATBbits.LATB6 = ~LATBbits.LATB6; } while(0)
#define RB6_GetValue()              PORTBbits.RB6
#define RB6_SetDigitalInput()    do { TRISBbits.TRISB6 = 1; } while(0)
#define RB6_SetDigitalOutput()   do { TRISBbits.TRISB6 = 0; } while(0)
#define RB6_SetPullup()             do { WPUBbits.WPUB6 = 1; } while(0)
#define RB6_ResetPullup()           do { WPUBbits.WPUB6 = 0; } while(0)
#define RB6_SetAnalogMode()         do { ANSELBbits.ANSB6 = 1; } while(0)
#define RB6_SetDigitalMode()        do { ANSELBbits.ANSB6 = 0; } while(0)

// get/set RB7 procedures
#define RB7_SetHigh()            do { LATBbits.LATB7 = 1; } while(0)
#define RB7_SetLow()             do { LATBbits.LATB7 = 0; } while(0)
#define RB7_Toggle()             do { LATBbits.LATB7 = ~LATBbits.LATB7; } while(0)
#define RB7_GetValue()              PORTBbits.RB7
#define RB7_SetDigitalInput()    do { TRISBbits.TRISB7 = 1; } while(0)
#define RB7_SetDigitalOutput()   do { TRISBbits.TRISB7 = 0; } while(0)
#define RB7_SetPullup()             do { WPUBbits.WPUB7 = 1; } while(0)
#define RB7_ResetPullup()           do { WPUBbits.WPUB7 = 0; } while(0)
#define RB7_SetAnalogMode()         do { ANSELBbits.ANSB7 = 1; } while(0)
#define RB7_SetDigitalMode()        do { ANSELBbits.ANSB7 = 0; } while(0)

// get/set Vbatt aliases
#define Vbatt_TRIS                 TRISCbits.TRISC0
#define Vbatt_LAT                  LATCbits.LATC0
#define Vbatt_PORT                 PORTCbits.RC0
#define Vbatt_WPU                  WPUCbits.WPUC0
#define Vbatt_OD                   ODCONCbits.ODCC0
#define Vbatt_ANS                  ANSELCbits.ANSC0
#define Vbatt_SetHigh()            do { LATCbits.LATC0 = 1; } while(0)
#define Vbatt_SetLow()             do { LATCbits.LATC0 = 0; } while(0)
#define Vbatt_Toggle()             do { LATCbits.LATC0 = ~LATCbits.LATC0; } while(0)
#define Vbatt_GetValue()           PORTCbits.RC0
#define Vbatt_SetDigitalInput()    do { TRISCbits.TRISC0 = 1; } while(0)
#define Vbatt_SetDigitalOutput()   do { TRISCbits.TRISC0 = 0; } while(0)
#define Vbatt_SetPullup()          do { WPUCbits.WPUC0 = 1; } while(0)
#define Vbatt_ResetPullup()        do { WPUCbits.WPUC0 = 0; } while(0)
#define Vbatt_SetPushPull()        do { ODCONCbits.ODCC0 = 0; } while(0)
#define Vbatt_SetOpenDrain()       do { ODCONCbits.ODCC0 = 1; } while(0)
#define Vbatt_SetAnalogMode()      do { ANSELCbits.ANSC0 = 1; } while(0)
#define Vbatt_SetDigitalMode()     do { ANSELCbits.ANSC0 = 0; } while(0)

// get/set RC1 procedures
#define RC1_SetHigh()            do { LATCbits.LATC1 = 1; } while(0)
#define RC1_SetLow()             do { LATCbits.LATC1 = 0; } while(0)
#define RC1_Toggle()             do { LATCbits.LATC1 = ~LATCbits.LATC1; } while(0)
#define RC1_GetValue()              PORTCbits.RC1
#define RC1_SetDigitalInput()    do { TRISCbits.TRISC1 = 1; } while(0)
#define RC1_SetDigitalOutput()   do { TRISCbits.TRISC1 = 0; } while(0)
#define RC1_SetPullup()             do { WPUCbits.WPUC1 = 1; } while(0)
#define RC1_ResetPullup()           do { WPUCbits.WPUC1 = 0; } while(0)
#define RC1_SetAnalogMode()         do { ANSELCbits.ANSC1 = 1; } while(0)
#define RC1_SetDigitalMode()        do { ANSELCbits.ANSC1 = 0; } while(0)

// get/set EnMOT aliases
#define EnMOT_TRIS                 TRISCbits.TRISC2
#define EnMOT_LAT                  LATCbits.LATC2
#define EnMOT_PORT                 PORTCbits.RC2
#define EnMOT_WPU                  WPUCbits.WPUC2
#define EnMOT_OD                   ODCONCbits.ODCC2
#define EnMOT_ANS                  ANSELCbits.ANSC2
#define EnMOT_SetHigh()            do { LATCbits.LATC2 = 1; } while(0)
#define EnMOT_SetLow()             do { LATCbits.LATC2 = 0; } while(0)
#define EnMOT_Toggle()             do { LATCbits.LATC2 = ~LATCbits.LATC2; } while(0)
#define EnMOT_GetValue()           PORTCbits.RC2
#define EnMOT_SetDigitalInput()    do { TRISCbits.TRISC2 = 1; } while(0)
#define EnMOT_SetDigitalOutput()   do { TRISCbits.TRISC2 = 0; } while(0)
#define EnMOT_SetPullup()          do { WPUCbits.WPUC2 = 1; } while(0)
#define EnMOT_ResetPullup()        do { WPUCbits.WPUC2 = 0; } while(0)
#define EnMOT_SetPushPull()        do { ODCONCbits.ODCC2 = 0; } while(0)
#define EnMOT_SetOpenDrain()       do { ODCONCbits.ODCC2 = 1; } while(0)
#define EnMOT_SetAnalogMode()      do { ANSELCbits.ANSC2 = 1; } while(0)
#define EnMOT_SetDigitalMode()     do { ANSELCbits.ANSC2 = 0; } while(0)

// get/set RC3 procedures
#define RC3_SetHigh()            do { LATCbits.LATC3 = 1; } while(0)
#define RC3_SetLow()             do { LATCbits.LATC3 = 0; } while(0)
#define RC3_Toggle()             do { LATCbits.LATC3 = ~LATCbits.LATC3; } while(0)
#define RC3_GetValue()              PORTCbits.RC3
#define RC3_SetDigitalInput()    do { TRISCbits.TRISC3 = 1; } while(0)
#define RC3_SetDigitalOutput()   do { TRISCbits.TRISC3 = 0; } while(0)
#define RC3_SetPullup()             do { WPUCbits.WPUC3 = 1; } while(0)
#define RC3_ResetPullup()           do { WPUCbits.WPUC3 = 0; } while(0)
#define RC3_SetAnalogMode()         do { ANSELCbits.ANSC3 = 1; } while(0)
#define RC3_SetDigitalMode()        do { ANSELCbits.ANSC3 = 0; } while(0)

// get/set RCS aliases
#define RCS_TRIS                 TRISCbits.TRISC4
#define RCS_LAT                  LATCbits.LATC4
#define RCS_PORT                 PORTCbits.RC4
#define RCS_WPU                  WPUCbits.WPUC4
#define RCS_OD                   ODCONCbits.ODCC4
#define RCS_ANS                  ANSELCbits.ANSC4
#define RCS_SetHigh()            do { LATCbits.LATC4 = 1; } while(0)
#define RCS_SetLow()             do { LATCbits.LATC4 = 0; } while(0)
#define RCS_Toggle()             do { LATCbits.LATC4 = ~LATCbits.LATC4; } while(0)
#define RCS_GetValue()           PORTCbits.RC4
#define RCS_SetDigitalInput()    do { TRISCbits.TRISC4 = 1; } while(0)
#define RCS_SetDigitalOutput()   do { TRISCbits.TRISC4 = 0; } while(0)
#define RCS_SetPullup()          do { WPUCbits.WPUC4 = 1; } while(0)
#define RCS_ResetPullup()        do { WPUCbits.WPUC4 = 0; } while(0)
#define RCS_SetPushPull()        do { ODCONCbits.ODCC4 = 0; } while(0)
#define RCS_SetOpenDrain()       do { ODCONCbits.ODCC4 = 1; } while(0)
#define RCS_SetAnalogMode()      do { ANSELCbits.ANSC4 = 1; } while(0)
#define RCS_SetDigitalMode()     do { ANSELCbits.ANSC4 = 0; } while(0)

// get/set RC5 procedures
#define RC5_SetHigh()            do { LATCbits.LATC5 = 1; } while(0)
#define RC5_SetLow()             do { LATCbits.LATC5 = 0; } while(0)
#define RC5_Toggle()             do { LATCbits.LATC5 = ~LATCbits.LATC5; } while(0)
#define RC5_GetValue()              PORTCbits.RC5
#define RC5_SetDigitalInput()    do { TRISCbits.TRISC5 = 1; } while(0)
#define RC5_SetDigitalOutput()   do { TRISCbits.TRISC5 = 0; } while(0)
#define RC5_SetPullup()             do { WPUCbits.WPUC5 = 1; } while(0)
#define RC5_ResetPullup()           do { WPUCbits.WPUC5 = 0; } while(0)
#define RC5_SetAnalogMode()         do { ANSELCbits.ANSC5 = 1; } while(0)
#define RC5_SetDigitalMode()        do { ANSELCbits.ANSC5 = 0; } while(0)

// get/set RC6 procedures
#define RC6_SetHigh()            do { LATCbits.LATC6 = 1; } while(0)
#define RC6_SetLow()             do { LATCbits.LATC6 = 0; } while(0)
#define RC6_Toggle()             do { LATCbits.LATC6 = ~LATCbits.LATC6; } while(0)
#define RC6_GetValue()              PORTCbits.RC6
#define RC6_SetDigitalInput()    do { TRISCbits.TRISC6 = 1; } while(0)
#define RC6_SetDigitalOutput()   do { TRISCbits.TRISC6 = 0; } while(0)
#define RC6_SetPullup()             do { WPUCbits.WPUC6 = 1; } while(0)
#define RC6_ResetPullup()           do { WPUCbits.WPUC6 = 0; } while(0)
#define RC6_SetAnalogMode()         do { ANSELCbits.ANSC6 = 1; } while(0)
#define RC6_SetDigitalMode()        do { ANSELCbits.ANSC6 = 0; } while(0)

// get/set RC7 procedures
#define RC7_SetHigh()            do { LATCbits.LATC7 = 1; } while(0)
#define RC7_SetLow()             do { LATCbits.LATC7 = 0; } while(0)
#define RC7_Toggle()             do { LATCbits.LATC7 = ~LATCbits.LATC7; } while(0)
#define RC7_GetValue()              PORTCbits.RC7
#define RC7_SetDigitalInput()    do { TRISCbits.TRISC7 = 1; } while(0)
#define RC7_SetDigitalOutput()   do { TRISCbits.TRISC7 = 0; } while(0)
#define RC7_SetPullup()             do { WPUCbits.WPUC7 = 1; } while(0)
#define RC7_ResetPullup()           do { WPUCbits.WPUC7 = 0; } while(0)
#define RC7_SetAnalogMode()         do { ANSELCbits.ANSC7 = 1; } while(0)
#define RC7_SetDigitalMode()        do { ANSELCbits.ANSC7 = 0; } while(0)

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