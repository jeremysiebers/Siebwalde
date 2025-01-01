/**
  @Generated Pin Manager Header File

  @Company:
    Microchip Technology Inc.

  @File Name:
    pin_manager.h

  @Summary:
    This is the Pin Manager file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  @Description:
    This header file provides implementations for pin APIs for all pins selected in the GUI.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.81.8
        Device            :  PIC18F97J60
        Version           :  2.0
    The generated drivers are tested against the following:
        Compiler          :  XC8 2.36 and above
        MPLAB             :  MPLAB X 6.00

    Copyright (c) 2013 - 2015 released Microchip Technology Inc.  All rights reserved.
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

// get/set ETHLEDA aliases
#define ETHLEDA_TRIS                 TRISAbits.TRISA0
#define ETHLEDA_LAT                  LATAbits.LATA0
#define ETHLEDA_PORT                 PORTAbits.RA0
#define ETHLEDA_ANS                  anselRA0bits.anselRA0
#define ETHLEDA_SetHigh()            do { LATAbits.LATA0 = 1; } while(0)
#define ETHLEDA_SetLow()             do { LATAbits.LATA0 = 0; } while(0)
#define ETHLEDA_Toggle()             do { LATAbits.LATA0 = ~LATAbits.LATA0; } while(0)
#define ETHLEDA_GetValue()           PORTAbits.RA0
#define ETHLEDA_SetDigitalInput()    do { TRISAbits.TRISA0 = 1; } while(0)
#define ETHLEDA_SetDigitalOutput()   do { TRISAbits.TRISA0 = 0; } while(0)
#define ETHLEDA_SetAnalogMode()      do { anselRA0bits.anselRA0 = 1; } while(0)
#define ETHLEDA_SetDigitalMode()     do { anselRA0bits.anselRA0 = 0; } while(0)

// get/set ETHLEDB aliases
#define ETHLEDB_TRIS                 TRISAbits.TRISA1
#define ETHLEDB_LAT                  LATAbits.LATA1
#define ETHLEDB_PORT                 PORTAbits.RA1
#define ETHLEDB_ANS                  anselRA1bits.anselRA1
#define ETHLEDB_SetHigh()            do { LATAbits.LATA1 = 1; } while(0)
#define ETHLEDB_SetLow()             do { LATAbits.LATA1 = 0; } while(0)
#define ETHLEDB_Toggle()             do { LATAbits.LATA1 = ~LATAbits.LATA1; } while(0)
#define ETHLEDB_GetValue()           PORTAbits.RA1
#define ETHLEDB_SetDigitalInput()    do { TRISAbits.TRISA1 = 1; } while(0)
#define ETHLEDB_SetDigitalOutput()   do { TRISAbits.TRISA1 = 0; } while(0)
#define ETHLEDB_SetAnalogMode()      do { anselRA1bits.anselRA1 = 1; } while(0)
#define ETHLEDB_SetDigitalMode()     do { anselRA1bits.anselRA1 = 0; } while(0)

// get/set TP3 aliases
#define TP3_TRIS                 TRISAbits.TRISA2
#define TP3_LAT                  LATAbits.LATA2
#define TP3_PORT                 PORTAbits.RA2
#define TP3_ANS                  anselRA2bits.anselRA2
#define TP3_SetHigh()            do { LATAbits.LATA2 = 1; } while(0)
#define TP3_SetLow()             do { LATAbits.LATA2 = 0; } while(0)
#define TP3_Toggle()             do { LATAbits.LATA2 = ~LATAbits.LATA2; } while(0)
#define TP3_GetValue()           PORTAbits.RA2
#define TP3_SetDigitalInput()    do { TRISAbits.TRISA2 = 1; } while(0)
#define TP3_SetDigitalOutput()   do { TRISAbits.TRISA2 = 0; } while(0)
#define TP3_SetAnalogMode()      do { anselRA2bits.anselRA2 = 1; } while(0)
#define TP3_SetDigitalMode()     do { anselRA2bits.anselRA2 = 0; } while(0)

// get/set TP4 aliases
#define TP4_TRIS                 TRISAbits.TRISA3
#define TP4_LAT                  LATAbits.LATA3
#define TP4_PORT                 PORTAbits.RA3
#define TP4_ANS                  anselRA3bits.anselRA3
#define TP4_SetHigh()            do { LATAbits.LATA3 = 1; } while(0)
#define TP4_SetLow()             do { LATAbits.LATA3 = 0; } while(0)
#define TP4_Toggle()             do { LATAbits.LATA3 = ~LATAbits.LATA3; } while(0)
#define TP4_GetValue()           PORTAbits.RA3
#define TP4_SetDigitalInput()    do { TRISAbits.TRISA3 = 1; } while(0)
#define TP4_SetDigitalOutput()   do { TRISAbits.TRISA3 = 0; } while(0)
#define TP4_SetAnalogMode()      do { anselRA3bits.anselRA3 = 1; } while(0)
#define TP4_SetDigitalMode()     do { anselRA3bits.anselRA3 = 0; } while(0)

// get/set TP1 aliases
#define TP1_TRIS                 TRISAbits.TRISA4
#define TP1_LAT                  LATAbits.LATA4
#define TP1_PORT                 PORTAbits.RA4
#define TP1_SetHigh()            do { LATAbits.LATA4 = 1; } while(0)
#define TP1_SetLow()             do { LATAbits.LATA4 = 0; } while(0)
#define TP1_Toggle()             do { LATAbits.LATA4 = ~LATAbits.LATA4; } while(0)
#define TP1_GetValue()           PORTAbits.RA4
#define TP1_SetDigitalInput()    do { TRISAbits.TRISA4 = 1; } while(0)
#define TP1_SetDigitalOutput()   do { TRISAbits.TRISA4 = 0; } while(0)

// get/set TP2 aliases
#define TP2_TRIS                 TRISAbits.TRISA5
#define TP2_LAT                  LATAbits.LATA5
#define TP2_PORT                 PORTAbits.RA5
#define TP2_ANS                  anselRA5bits.anselRA5
#define TP2_SetHigh()            do { LATAbits.LATA5 = 1; } while(0)
#define TP2_SetLow()             do { LATAbits.LATA5 = 0; } while(0)
#define TP2_Toggle()             do { LATAbits.LATA5 = ~LATAbits.LATA5; } while(0)
#define TP2_GetValue()           PORTAbits.RA5
#define TP2_SetDigitalInput()    do { TRISAbits.TRISA5 = 1; } while(0)
#define TP2_SetDigitalOutput()   do { TRISAbits.TRISA5 = 0; } while(0)
#define TP2_SetAnalogMode()      do { anselRA5bits.anselRA5 = 1; } while(0)
#define TP2_SetDigitalMode()     do { anselRA5bits.anselRA5 = 0; } while(0)

// get/set IO_RB0 aliases
#define IO_RB0_TRIS                 TRISBbits.TRISB0
#define IO_RB0_LAT                  LATBbits.LATB0
#define IO_RB0_PORT                 PORTBbits.RB0
#define IO_RB0_SetHigh()            do { LATBbits.LATB0 = 1; } while(0)
#define IO_RB0_SetLow()             do { LATBbits.LATB0 = 0; } while(0)
#define IO_RB0_Toggle()             do { LATBbits.LATB0 = ~LATBbits.LATB0; } while(0)
#define IO_RB0_GetValue()           PORTBbits.RB0
#define IO_RB0_SetDigitalInput()    do { TRISBbits.TRISB0 = 1; } while(0)
#define IO_RB0_SetDigitalOutput()   do { TRISBbits.TRISB0 = 0; } while(0)

// get/set IO_RB1 aliases
#define IO_RB1_TRIS                 TRISBbits.TRISB1
#define IO_RB1_LAT                  LATBbits.LATB1
#define IO_RB1_PORT                 PORTBbits.RB1
#define IO_RB1_SetHigh()            do { LATBbits.LATB1 = 1; } while(0)
#define IO_RB1_SetLow()             do { LATBbits.LATB1 = 0; } while(0)
#define IO_RB1_Toggle()             do { LATBbits.LATB1 = ~LATBbits.LATB1; } while(0)
#define IO_RB1_GetValue()           PORTBbits.RB1
#define IO_RB1_SetDigitalInput()    do { TRISBbits.TRISB1 = 1; } while(0)
#define IO_RB1_SetDigitalOutput()   do { TRISBbits.TRISB1 = 0; } while(0)

// get/set TP5 aliases
#define TP5_TRIS                 TRISBbits.TRISB2
#define TP5_LAT                  LATBbits.LATB2
#define TP5_PORT                 PORTBbits.RB2
#define TP5_SetHigh()            do { LATBbits.LATB2 = 1; } while(0)
#define TP5_SetLow()             do { LATBbits.LATB2 = 0; } while(0)
#define TP5_Toggle()             do { LATBbits.LATB2 = ~LATBbits.LATB2; } while(0)
#define TP5_GetValue()           PORTBbits.RB2
#define TP5_SetDigitalInput()    do { TRISBbits.TRISB2 = 1; } while(0)
#define TP5_SetDigitalOutput()   do { TRISBbits.TRISB2 = 0; } while(0)

// get/set IOX_RESET aliases
#define IOX_RESET_TRIS                 TRISBbits.TRISB3
#define IOX_RESET_LAT                  LATBbits.LATB3
#define IOX_RESET_PORT                 PORTBbits.RB3
#define IOX_RESET_SetHigh()            do { LATBbits.LATB3 = 1; } while(0)
#define IOX_RESET_SetLow()             do { LATBbits.LATB3 = 0; } while(0)
#define IOX_RESET_Toggle()             do { LATBbits.LATB3 = ~LATBbits.LATB3; } while(0)
#define IOX_RESET_GetValue()           PORTBbits.RB3
#define IOX_RESET_SetDigitalInput()    do { TRISBbits.TRISB3 = 1; } while(0)
#define IOX_RESET_SetDigitalOutput()   do { TRISBbits.TRISB3 = 0; } while(0)

// get/set IO_RB4 aliases
#define IO_RB4_TRIS                 TRISBbits.TRISB4
#define IO_RB4_LAT                  LATBbits.LATB4
#define IO_RB4_PORT                 PORTBbits.RB4
#define IO_RB4_SetHigh()            do { LATBbits.LATB4 = 1; } while(0)
#define IO_RB4_SetLow()             do { LATBbits.LATB4 = 0; } while(0)
#define IO_RB4_Toggle()             do { LATBbits.LATB4 = ~LATBbits.LATB4; } while(0)
#define IO_RB4_GetValue()           PORTBbits.RB4
#define IO_RB4_SetDigitalInput()    do { TRISBbits.TRISB4 = 1; } while(0)
#define IO_RB4_SetDigitalOutput()   do { TRISBbits.TRISB4 = 0; } while(0)

// get/set EXT_WDT aliases
#define EXT_WDT_TRIS                 TRISBbits.TRISB5
#define EXT_WDT_LAT                  LATBbits.LATB5
#define EXT_WDT_PORT                 PORTBbits.RB5
#define EXT_WDT_SetHigh()            do { LATBbits.LATB5 = 1; } while(0)
#define EXT_WDT_SetLow()             do { LATBbits.LATB5 = 0; } while(0)
#define EXT_WDT_Toggle()             do { LATBbits.LATB5 = ~LATBbits.LATB5; } while(0)
#define EXT_WDT_GetValue()           PORTBbits.RB5
#define EXT_WDT_SetDigitalInput()    do { TRISBbits.TRISB5 = 1; } while(0)
#define EXT_WDT_SetDigitalOutput()   do { TRISBbits.TRISB5 = 0; } while(0)

// get/set NC_1 aliases
#define NC_1_TRIS                 TRISBbits.TRISB6
#define NC_1_LAT                  LATBbits.LATB6
#define NC_1_PORT                 PORTBbits.RB6
#define NC_1_SetHigh()            do { LATBbits.LATB6 = 1; } while(0)
#define NC_1_SetLow()             do { LATBbits.LATB6 = 0; } while(0)
#define NC_1_Toggle()             do { LATBbits.LATB6 = ~LATBbits.LATB6; } while(0)
#define NC_1_GetValue()           PORTBbits.RB6
#define NC_1_SetDigitalInput()    do { TRISBbits.TRISB6 = 1; } while(0)
#define NC_1_SetDigitalOutput()   do { TRISBbits.TRISB6 = 0; } while(0)

// get/set NC_2 aliases
#define NC_2_TRIS                 TRISBbits.TRISB7
#define NC_2_LAT                  LATBbits.LATB7
#define NC_2_PORT                 PORTBbits.RB7
#define NC_2_SetHigh()            do { LATBbits.LATB7 = 1; } while(0)
#define NC_2_SetLow()             do { LATBbits.LATB7 = 0; } while(0)
#define NC_2_Toggle()             do { LATBbits.LATB7 = ~LATBbits.LATB7; } while(0)
#define NC_2_GetValue()           PORTBbits.RB7
#define NC_2_SetDigitalInput()    do { TRISBbits.TRISB7 = 1; } while(0)
#define NC_2_SetDigitalOutput()   do { TRISBbits.TRISB7 = 0; } while(0)

// get/set IO_RC0 aliases
#define IO_RC0_TRIS                 TRISCbits.TRISC0
#define IO_RC0_LAT                  LATCbits.LATC0
#define IO_RC0_PORT                 PORTCbits.RC0
#define IO_RC0_SetHigh()            do { LATCbits.LATC0 = 1; } while(0)
#define IO_RC0_SetLow()             do { LATCbits.LATC0 = 0; } while(0)
#define IO_RC0_Toggle()             do { LATCbits.LATC0 = ~LATCbits.LATC0; } while(0)
#define IO_RC0_GetValue()           PORTCbits.RC0
#define IO_RC0_SetDigitalInput()    do { TRISCbits.TRISC0 = 1; } while(0)
#define IO_RC0_SetDigitalOutput()   do { TRISCbits.TRISC0 = 0; } while(0)

// get/set IO_RC1 aliases
#define IO_RC1_TRIS                 TRISCbits.TRISC1
#define IO_RC1_LAT                  LATCbits.LATC1
#define IO_RC1_PORT                 PORTCbits.RC1
#define IO_RC1_SetHigh()            do { LATCbits.LATC1 = 1; } while(0)
#define IO_RC1_SetLow()             do { LATCbits.LATC1 = 0; } while(0)
#define IO_RC1_Toggle()             do { LATCbits.LATC1 = ~LATCbits.LATC1; } while(0)
#define IO_RC1_GetValue()           PORTCbits.RC1
#define IO_RC1_SetDigitalInput()    do { TRISCbits.TRISC1 = 1; } while(0)
#define IO_RC1_SetDigitalOutput()   do { TRISCbits.TRISC1 = 0; } while(0)

// get/set IO_RC2 aliases
#define IO_RC2_TRIS                 TRISCbits.TRISC2
#define IO_RC2_LAT                  LATCbits.LATC2
#define IO_RC2_PORT                 PORTCbits.RC2
#define IO_RC2_SetHigh()            do { LATCbits.LATC2 = 1; } while(0)
#define IO_RC2_SetLow()             do { LATCbits.LATC2 = 0; } while(0)
#define IO_RC2_Toggle()             do { LATCbits.LATC2 = ~LATCbits.LATC2; } while(0)
#define IO_RC2_GetValue()           PORTCbits.RC2
#define IO_RC2_SetDigitalInput()    do { TRISCbits.TRISC2 = 1; } while(0)
#define IO_RC2_SetDigitalOutput()   do { TRISCbits.TRISC2 = 0; } while(0)

// get/set IO_RC3 aliases
#define IO_RC3_TRIS                 TRISCbits.TRISC3
#define IO_RC3_LAT                  LATCbits.LATC3
#define IO_RC3_PORT                 PORTCbits.RC3
#define IO_RC3_SetHigh()            do { LATCbits.LATC3 = 1; } while(0)
#define IO_RC3_SetLow()             do { LATCbits.LATC3 = 0; } while(0)
#define IO_RC3_Toggle()             do { LATCbits.LATC3 = ~LATCbits.LATC3; } while(0)
#define IO_RC3_GetValue()           PORTCbits.RC3
#define IO_RC3_SetDigitalInput()    do { TRISCbits.TRISC3 = 1; } while(0)
#define IO_RC3_SetDigitalOutput()   do { TRISCbits.TRISC3 = 0; } while(0)

// get/set IO_RC4 aliases
#define IO_RC4_TRIS                 TRISCbits.TRISC4
#define IO_RC4_LAT                  LATCbits.LATC4
#define IO_RC4_PORT                 PORTCbits.RC4
#define IO_RC4_SetHigh()            do { LATCbits.LATC4 = 1; } while(0)
#define IO_RC4_SetLow()             do { LATCbits.LATC4 = 0; } while(0)
#define IO_RC4_Toggle()             do { LATCbits.LATC4 = ~LATCbits.LATC4; } while(0)
#define IO_RC4_GetValue()           PORTCbits.RC4
#define IO_RC4_SetDigitalInput()    do { TRISCbits.TRISC4 = 1; } while(0)
#define IO_RC4_SetDigitalOutput()   do { TRISCbits.TRISC4 = 0; } while(0)

// get/set IO_RC5 aliases
#define IO_RC5_TRIS                 TRISCbits.TRISC5
#define IO_RC5_LAT                  LATCbits.LATC5
#define IO_RC5_PORT                 PORTCbits.RC5
#define IO_RC5_SetHigh()            do { LATCbits.LATC5 = 1; } while(0)
#define IO_RC5_SetLow()             do { LATCbits.LATC5 = 0; } while(0)
#define IO_RC5_Toggle()             do { LATCbits.LATC5 = ~LATCbits.LATC5; } while(0)
#define IO_RC5_GetValue()           PORTCbits.RC5
#define IO_RC5_SetDigitalInput()    do { TRISCbits.TRISC5 = 1; } while(0)
#define IO_RC5_SetDigitalOutput()   do { TRISCbits.TRISC5 = 0; } while(0)

// get/set IO_RC6 aliases
#define IO_RC6_TRIS                 TRISCbits.TRISC6
#define IO_RC6_LAT                  LATCbits.LATC6
#define IO_RC6_PORT                 PORTCbits.RC6
#define IO_RC6_SetHigh()            do { LATCbits.LATC6 = 1; } while(0)
#define IO_RC6_SetLow()             do { LATCbits.LATC6 = 0; } while(0)
#define IO_RC6_Toggle()             do { LATCbits.LATC6 = ~LATCbits.LATC6; } while(0)
#define IO_RC6_GetValue()           PORTCbits.RC6
#define IO_RC6_SetDigitalInput()    do { TRISCbits.TRISC6 = 1; } while(0)
#define IO_RC6_SetDigitalOutput()   do { TRISCbits.TRISC6 = 0; } while(0)

// get/set IO_RC7 aliases
#define IO_RC7_TRIS                 TRISCbits.TRISC7
#define IO_RC7_LAT                  LATCbits.LATC7
#define IO_RC7_PORT                 PORTCbits.RC7
#define IO_RC7_SetHigh()            do { LATCbits.LATC7 = 1; } while(0)
#define IO_RC7_SetLow()             do { LATCbits.LATC7 = 0; } while(0)
#define IO_RC7_Toggle()             do { LATCbits.LATC7 = ~LATCbits.LATC7; } while(0)
#define IO_RC7_GetValue()           PORTCbits.RC7
#define IO_RC7_SetDigitalInput()    do { TRISCbits.TRISC7 = 1; } while(0)
#define IO_RC7_SetDigitalOutput()   do { TRISCbits.TRISC7 = 0; } while(0)

// get/set IO_RD0 aliases
#define IO_RD0_TRIS                 TRISDbits.TRISD0
#define IO_RD0_LAT                  LATDbits.LATD0
#define IO_RD0_PORT                 PORTDbits.RD0
#define IO_RD0_SetHigh()            do { LATDbits.LATD0 = 1; } while(0)
#define IO_RD0_SetLow()             do { LATDbits.LATD0 = 0; } while(0)
#define IO_RD0_Toggle()             do { LATDbits.LATD0 = ~LATDbits.LATD0; } while(0)
#define IO_RD0_GetValue()           PORTDbits.RD0
#define IO_RD0_SetDigitalInput()    do { TRISDbits.TRISD0 = 1; } while(0)
#define IO_RD0_SetDigitalOutput()   do { TRISDbits.TRISD0 = 0; } while(0)

// get/set IO_RD1 aliases
#define IO_RD1_TRIS                 TRISDbits.TRISD1
#define IO_RD1_LAT                  LATDbits.LATD1
#define IO_RD1_PORT                 PORTDbits.RD1
#define IO_RD1_SetHigh()            do { LATDbits.LATD1 = 1; } while(0)
#define IO_RD1_SetLow()             do { LATDbits.LATD1 = 0; } while(0)
#define IO_RD1_Toggle()             do { LATDbits.LATD1 = ~LATDbits.LATD1; } while(0)
#define IO_RD1_GetValue()           PORTDbits.RD1
#define IO_RD1_SetDigitalInput()    do { TRISDbits.TRISD1 = 1; } while(0)
#define IO_RD1_SetDigitalOutput()   do { TRISDbits.TRISD1 = 0; } while(0)

// get/set IO_RD2 aliases
#define IO_RD2_TRIS                 TRISDbits.TRISD2
#define IO_RD2_LAT                  LATDbits.LATD2
#define IO_RD2_PORT                 PORTDbits.RD2
#define IO_RD2_SetHigh()            do { LATDbits.LATD2 = 1; } while(0)
#define IO_RD2_SetLow()             do { LATDbits.LATD2 = 0; } while(0)
#define IO_RD2_Toggle()             do { LATDbits.LATD2 = ~LATDbits.LATD2; } while(0)
#define IO_RD2_GetValue()           PORTDbits.RD2
#define IO_RD2_SetDigitalInput()    do { TRISDbits.TRISD2 = 1; } while(0)
#define IO_RD2_SetDigitalOutput()   do { TRISDbits.TRISD2 = 0; } while(0)

// get/set IO_RD3 aliases
#define IO_RD3_TRIS                 TRISDbits.TRISD3
#define IO_RD3_LAT                  LATDbits.LATD3
#define IO_RD3_PORT                 PORTDbits.RD3
#define IO_RD3_SetHigh()            do { LATDbits.LATD3 = 1; } while(0)
#define IO_RD3_SetLow()             do { LATDbits.LATD3 = 0; } while(0)
#define IO_RD3_Toggle()             do { LATDbits.LATD3 = ~LATDbits.LATD3; } while(0)
#define IO_RD3_GetValue()           PORTDbits.RD3
#define IO_RD3_SetDigitalInput()    do { TRISDbits.TRISD3 = 1; } while(0)
#define IO_RD3_SetDigitalOutput()   do { TRISDbits.TRISD3 = 0; } while(0)

// get/set IO_RD4 aliases
#define IO_RD4_TRIS                 TRISDbits.TRISD4
#define IO_RD4_LAT                  LATDbits.LATD4
#define IO_RD4_PORT                 PORTDbits.RD4
#define IO_RD4_SetHigh()            do { LATDbits.LATD4 = 1; } while(0)
#define IO_RD4_SetLow()             do { LATDbits.LATD4 = 0; } while(0)
#define IO_RD4_Toggle()             do { LATDbits.LATD4 = ~LATDbits.LATD4; } while(0)
#define IO_RD4_GetValue()           PORTDbits.RD4
#define IO_RD4_SetDigitalInput()    do { TRISDbits.TRISD4 = 1; } while(0)
#define IO_RD4_SetDigitalOutput()   do { TRISDbits.TRISD4 = 0; } while(0)

// get/set IO_SDA2 aliases
#define IO_SDA2_TRIS                 TRISDbits.TRISD5
#define IO_SDA2_LAT                  LATDbits.LATD5
#define IO_SDA2_PORT                 PORTDbits.RD5
#define IO_SDA2_SetHigh()            do { LATDbits.LATD5 = 1; } while(0)
#define IO_SDA2_SetLow()             do { LATDbits.LATD5 = 0; } while(0)
#define IO_SDA2_Toggle()             do { LATDbits.LATD5 = ~LATDbits.LATD5; } while(0)
#define IO_SDA2_GetValue()           PORTDbits.RD5
#define IO_SDA2_SetDigitalInput()    do { TRISDbits.TRISD5 = 1; } while(0)
#define IO_SDA2_SetDigitalOutput()   do { TRISDbits.TRISD5 = 0; } while(0)

// get/set IO_SCL2 aliases
#define IO_SCL2_TRIS                 TRISDbits.TRISD6
#define IO_SCL2_LAT                  LATDbits.LATD6
#define IO_SCL2_PORT                 PORTDbits.RD6
#define IO_SCL2_SetHigh()            do { LATDbits.LATD6 = 1; } while(0)
#define IO_SCL2_SetLow()             do { LATDbits.LATD6 = 0; } while(0)
#define IO_SCL2_Toggle()             do { LATDbits.LATD6 = ~LATDbits.LATD6; } while(0)
#define IO_SCL2_GetValue()           PORTDbits.RD6
#define IO_SCL2_SetDigitalInput()    do { TRISDbits.TRISD6 = 1; } while(0)
#define IO_SCL2_SetDigitalOutput()   do { TRISDbits.TRISD6 = 0; } while(0)

// get/set IO_RD7 aliases
#define IO_RD7_TRIS                 TRISDbits.TRISD7
#define IO_RD7_LAT                  LATDbits.LATD7
#define IO_RD7_PORT                 PORTDbits.RD7
#define IO_RD7_SetHigh()            do { LATDbits.LATD7 = 1; } while(0)
#define IO_RD7_SetLow()             do { LATDbits.LATD7 = 0; } while(0)
#define IO_RD7_Toggle()             do { LATDbits.LATD7 = ~LATDbits.LATD7; } while(0)
#define IO_RD7_GetValue()           PORTDbits.RD7
#define IO_RD7_SetDigitalInput()    do { TRISDbits.TRISD7 = 1; } while(0)
#define IO_RD7_SetDigitalOutput()   do { TRISDbits.TRISD7 = 0; } while(0)

// get/set SW_BUSSTOP_IND aliases
#define SW_BUSSTOP_IND_TRIS                 TRISEbits.TRISE0
#define SW_BUSSTOP_IND_LAT                  LATEbits.LATE0
#define SW_BUSSTOP_IND_PORT                 PORTEbits.RE0
#define SW_BUSSTOP_IND_SetHigh()            do { LATEbits.LATE0 = 1; } while(0)
#define SW_BUSSTOP_IND_SetLow()             do { LATEbits.LATE0 = 0; } while(0)
#define SW_BUSSTOP_IND_Toggle()             do { LATEbits.LATE0 = ~LATEbits.LATE0; } while(0)
#define SW_BUSSTOP_IND_GetValue()           PORTEbits.RE0
#define SW_BUSSTOP_IND_SetDigitalInput()    do { TRISEbits.TRISE0 = 1; } while(0)
#define SW_BUSSTOP_IND_SetDigitalOutput()   do { TRISEbits.TRISE0 = 0; } while(0)

// get/set STOP_BUS_AT_IND aliases
#define STOP_BUS_AT_IND_TRIS                 TRISEbits.TRISE1
#define STOP_BUS_AT_IND_LAT                  LATEbits.LATE1
#define STOP_BUS_AT_IND_PORT                 PORTEbits.RE1
#define STOP_BUS_AT_IND_SetHigh()            do { LATEbits.LATE1 = 1; } while(0)
#define STOP_BUS_AT_IND_SetLow()             do { LATEbits.LATE1 = 0; } while(0)
#define STOP_BUS_AT_IND_Toggle()             do { LATEbits.LATE1 = ~LATEbits.LATE1; } while(0)
#define STOP_BUS_AT_IND_GetValue()           PORTEbits.RE1
#define STOP_BUS_AT_IND_SetDigitalInput()    do { TRISEbits.TRISE1 = 1; } while(0)
#define STOP_BUS_AT_IND_SetDigitalOutput()   do { TRISEbits.TRISE1 = 0; } while(0)

// get/set SW_BUSSTOP_STN aliases
#define SW_BUSSTOP_STN_TRIS                 TRISEbits.TRISE2
#define SW_BUSSTOP_STN_LAT                  LATEbits.LATE2
#define SW_BUSSTOP_STN_PORT                 PORTEbits.RE2
#define SW_BUSSTOP_STN_SetHigh()            do { LATEbits.LATE2 = 1; } while(0)
#define SW_BUSSTOP_STN_SetLow()             do { LATEbits.LATE2 = 0; } while(0)
#define SW_BUSSTOP_STN_Toggle()             do { LATEbits.LATE2 = ~LATEbits.LATE2; } while(0)
#define SW_BUSSTOP_STN_GetValue()           PORTEbits.RE2
#define SW_BUSSTOP_STN_SetDigitalInput()    do { TRISEbits.TRISE2 = 1; } while(0)
#define SW_BUSSTOP_STN_SetDigitalOutput()   do { TRISEbits.TRISE2 = 0; } while(0)

// get/set STOP_BUS_AT_STN aliases
#define STOP_BUS_AT_STN_TRIS                 TRISEbits.TRISE3
#define STOP_BUS_AT_STN_LAT                  LATEbits.LATE3
#define STOP_BUS_AT_STN_PORT                 PORTEbits.RE3
#define STOP_BUS_AT_STN_SetHigh()            do { LATEbits.LATE3 = 1; } while(0)
#define STOP_BUS_AT_STN_SetLow()             do { LATEbits.LATE3 = 0; } while(0)
#define STOP_BUS_AT_STN_Toggle()             do { LATEbits.LATE3 = ~LATEbits.LATE3; } while(0)
#define STOP_BUS_AT_STN_GetValue()           PORTEbits.RE3
#define STOP_BUS_AT_STN_SetDigitalInput()    do { TRISEbits.TRISE3 = 1; } while(0)
#define STOP_BUS_AT_STN_SetDigitalOutput()   do { TRISEbits.TRISE3 = 0; } while(0)

// get/set SW_FDEP_RIGHT aliases
#define SW_FDEP_RIGHT_TRIS                 TRISEbits.TRISE4
#define SW_FDEP_RIGHT_LAT                  LATEbits.LATE4
#define SW_FDEP_RIGHT_PORT                 PORTEbits.RE4
#define SW_FDEP_RIGHT_SetHigh()            do { LATEbits.LATE4 = 1; } while(0)
#define SW_FDEP_RIGHT_SetLow()             do { LATEbits.LATE4 = 0; } while(0)
#define SW_FDEP_RIGHT_Toggle()             do { LATEbits.LATE4 = ~LATEbits.LATE4; } while(0)
#define SW_FDEP_RIGHT_GetValue()           PORTEbits.RE4
#define SW_FDEP_RIGHT_SetDigitalInput()    do { TRISEbits.TRISE4 = 1; } while(0)
#define SW_FDEP_RIGHT_SetDigitalOutput()   do { TRISEbits.TRISE4 = 0; } while(0)

// get/set SW_FDEP_MID aliases
#define SW_FDEP_MID_TRIS                 TRISEbits.TRISE5
#define SW_FDEP_MID_LAT                  LATEbits.LATE5
#define SW_FDEP_MID_PORT                 PORTEbits.RE5
#define SW_FDEP_MID_SetHigh()            do { LATEbits.LATE5 = 1; } while(0)
#define SW_FDEP_MID_SetLow()             do { LATEbits.LATE5 = 0; } while(0)
#define SW_FDEP_MID_Toggle()             do { LATEbits.LATE5 = ~LATEbits.LATE5; } while(0)
#define SW_FDEP_MID_GetValue()           PORTEbits.RE5
#define SW_FDEP_MID_SetDigitalInput()    do { TRISEbits.TRISE5 = 1; } while(0)
#define SW_FDEP_MID_SetDigitalOutput()   do { TRISEbits.TRISE5 = 0; } while(0)

// get/set STOP_FDEP_AT_MID aliases
#define STOP_FDEP_AT_MID_TRIS                 TRISEbits.TRISE6
#define STOP_FDEP_AT_MID_LAT                  LATEbits.LATE6
#define STOP_FDEP_AT_MID_PORT                 PORTEbits.RE6
#define STOP_FDEP_AT_MID_SetHigh()            do { LATEbits.LATE6 = 1; } while(0)
#define STOP_FDEP_AT_MID_SetLow()             do { LATEbits.LATE6 = 0; } while(0)
#define STOP_FDEP_AT_MID_Toggle()             do { LATEbits.LATE6 = ~LATEbits.LATE6; } while(0)
#define STOP_FDEP_AT_MID_GetValue()           PORTEbits.RE6
#define STOP_FDEP_AT_MID_SetDigitalInput()    do { TRISEbits.TRISE6 = 1; } while(0)
#define STOP_FDEP_AT_MID_SetDigitalOutput()   do { TRISEbits.TRISE6 = 0; } while(0)

// get/set STOP_FDEP_AT_RIGHT aliases
#define STOP_FDEP_AT_RIGHT_TRIS                 TRISEbits.TRISE7
#define STOP_FDEP_AT_RIGHT_LAT                  LATEbits.LATE7
#define STOP_FDEP_AT_RIGHT_PORT                 PORTEbits.RE7
#define STOP_FDEP_AT_RIGHT_SetHigh()            do { LATEbits.LATE7 = 1; } while(0)
#define STOP_FDEP_AT_RIGHT_SetLow()             do { LATEbits.LATE7 = 0; } while(0)
#define STOP_FDEP_AT_RIGHT_Toggle()             do { LATEbits.LATE7 = ~LATEbits.LATE7; } while(0)
#define STOP_FDEP_AT_RIGHT_GetValue()           PORTEbits.RE7
#define STOP_FDEP_AT_RIGHT_SetDigitalInput()    do { TRISEbits.TRISE7 = 1; } while(0)
#define STOP_FDEP_AT_RIGHT_SetDigitalOutput()   do { TRISEbits.TRISE7 = 0; } while(0)

// get/set IO_RF0 aliases
#define IO_RF0_TRIS                 TRISFbits.TRISF0
#define IO_RF0_LAT                  LATFbits.LATF0
#define IO_RF0_PORT                 PORTFbits.RF0
#define IO_RF0_ANS                  anselRF0bits.anselRF0
#define IO_RF0_SetHigh()            do { LATFbits.LATF0 = 1; } while(0)
#define IO_RF0_SetLow()             do { LATFbits.LATF0 = 0; } while(0)
#define IO_RF0_Toggle()             do { LATFbits.LATF0 = ~LATFbits.LATF0; } while(0)
#define IO_RF0_GetValue()           PORTFbits.RF0
#define IO_RF0_SetDigitalInput()    do { TRISFbits.TRISF0 = 1; } while(0)
#define IO_RF0_SetDigitalOutput()   do { TRISFbits.TRISF0 = 0; } while(0)
#define IO_RF0_SetAnalogMode()      do { anselRF0bits.anselRF0 = 1; } while(0)
#define IO_RF0_SetDigitalMode()     do { anselRF0bits.anselRF0 = 0; } while(0)

// get/set IO_RF1 aliases
#define IO_RF1_TRIS                 TRISFbits.TRISF1
#define IO_RF1_LAT                  LATFbits.LATF1
#define IO_RF1_PORT                 PORTFbits.RF1
#define IO_RF1_ANS                  anselRF1bits.anselRF1
#define IO_RF1_SetHigh()            do { LATFbits.LATF1 = 1; } while(0)
#define IO_RF1_SetLow()             do { LATFbits.LATF1 = 0; } while(0)
#define IO_RF1_Toggle()             do { LATFbits.LATF1 = ~LATFbits.LATF1; } while(0)
#define IO_RF1_GetValue()           PORTFbits.RF1
#define IO_RF1_SetDigitalInput()    do { TRISFbits.TRISF1 = 1; } while(0)
#define IO_RF1_SetDigitalOutput()   do { TRISFbits.TRISF1 = 0; } while(0)
#define IO_RF1_SetAnalogMode()      do { anselRF1bits.anselRF1 = 1; } while(0)
#define IO_RF1_SetDigitalMode()     do { anselRF1bits.anselRF1 = 0; } while(0)

// get/set IO_RF2 aliases
#define IO_RF2_TRIS                 TRISFbits.TRISF2
#define IO_RF2_LAT                  LATFbits.LATF2
#define IO_RF2_PORT                 PORTFbits.RF2
#define IO_RF2_ANS                  anselRF2bits.anselRF2
#define IO_RF2_SetHigh()            do { LATFbits.LATF2 = 1; } while(0)
#define IO_RF2_SetLow()             do { LATFbits.LATF2 = 0; } while(0)
#define IO_RF2_Toggle()             do { LATFbits.LATF2 = ~LATFbits.LATF2; } while(0)
#define IO_RF2_GetValue()           PORTFbits.RF2
#define IO_RF2_SetDigitalInput()    do { TRISFbits.TRISF2 = 1; } while(0)
#define IO_RF2_SetDigitalOutput()   do { TRISFbits.TRISF2 = 0; } while(0)
#define IO_RF2_SetAnalogMode()      do { anselRF2bits.anselRF2 = 1; } while(0)
#define IO_RF2_SetDigitalMode()     do { anselRF2bits.anselRF2 = 0; } while(0)

// get/set IO_RF3 aliases
#define IO_RF3_TRIS                 TRISFbits.TRISF3
#define IO_RF3_LAT                  LATFbits.LATF3
#define IO_RF3_PORT                 PORTFbits.RF3
#define IO_RF3_ANS                  anselRF3bits.anselRF3
#define IO_RF3_SetHigh()            do { LATFbits.LATF3 = 1; } while(0)
#define IO_RF3_SetLow()             do { LATFbits.LATF3 = 0; } while(0)
#define IO_RF3_Toggle()             do { LATFbits.LATF3 = ~LATFbits.LATF3; } while(0)
#define IO_RF3_GetValue()           PORTFbits.RF3
#define IO_RF3_SetDigitalInput()    do { TRISFbits.TRISF3 = 1; } while(0)
#define IO_RF3_SetDigitalOutput()   do { TRISFbits.TRISF3 = 0; } while(0)
#define IO_RF3_SetAnalogMode()      do { anselRF3bits.anselRF3 = 1; } while(0)
#define IO_RF3_SetDigitalMode()     do { anselRF3bits.anselRF3 = 0; } while(0)

// get/set IO_RF4 aliases
#define IO_RF4_TRIS                 TRISFbits.TRISF4
#define IO_RF4_LAT                  LATFbits.LATF4
#define IO_RF4_PORT                 PORTFbits.RF4
#define IO_RF4_ANS                  anselRF4bits.anselRF4
#define IO_RF4_SetHigh()            do { LATFbits.LATF4 = 1; } while(0)
#define IO_RF4_SetLow()             do { LATFbits.LATF4 = 0; } while(0)
#define IO_RF4_Toggle()             do { LATFbits.LATF4 = ~LATFbits.LATF4; } while(0)
#define IO_RF4_GetValue()           PORTFbits.RF4
#define IO_RF4_SetDigitalInput()    do { TRISFbits.TRISF4 = 1; } while(0)
#define IO_RF4_SetDigitalOutput()   do { TRISFbits.TRISF4 = 0; } while(0)
#define IO_RF4_SetAnalogMode()      do { anselRF4bits.anselRF4 = 1; } while(0)
#define IO_RF4_SetDigitalMode()     do { anselRF4bits.anselRF4 = 0; } while(0)

// get/set IO_RF5 aliases
#define IO_RF5_TRIS                 TRISFbits.TRISF5
#define IO_RF5_LAT                  LATFbits.LATF5
#define IO_RF5_PORT                 PORTFbits.RF5
#define IO_RF5_ANS                  anselRF5bits.anselRF5
#define IO_RF5_SetHigh()            do { LATFbits.LATF5 = 1; } while(0)
#define IO_RF5_SetLow()             do { LATFbits.LATF5 = 0; } while(0)
#define IO_RF5_Toggle()             do { LATFbits.LATF5 = ~LATFbits.LATF5; } while(0)
#define IO_RF5_GetValue()           PORTFbits.RF5
#define IO_RF5_SetDigitalInput()    do { TRISFbits.TRISF5 = 1; } while(0)
#define IO_RF5_SetDigitalOutput()   do { TRISFbits.TRISF5 = 0; } while(0)
#define IO_RF5_SetAnalogMode()      do { anselRF5bits.anselRF5 = 1; } while(0)
#define IO_RF5_SetDigitalMode()     do { anselRF5bits.anselRF5 = 0; } while(0)

// get/set IO_RF6 aliases
#define IO_RF6_TRIS                 TRISFbits.TRISF6
#define IO_RF6_LAT                  LATFbits.LATF6
#define IO_RF6_PORT                 PORTFbits.RF6
#define IO_RF6_ANS                  anselRF6bits.anselRF6
#define IO_RF6_SetHigh()            do { LATFbits.LATF6 = 1; } while(0)
#define IO_RF6_SetLow()             do { LATFbits.LATF6 = 0; } while(0)
#define IO_RF6_Toggle()             do { LATFbits.LATF6 = ~LATFbits.LATF6; } while(0)
#define IO_RF6_GetValue()           PORTFbits.RF6
#define IO_RF6_SetDigitalInput()    do { TRISFbits.TRISF6 = 1; } while(0)
#define IO_RF6_SetDigitalOutput()   do { TRISFbits.TRISF6 = 0; } while(0)
#define IO_RF6_SetAnalogMode()      do { anselRF6bits.anselRF6 = 1; } while(0)
#define IO_RF6_SetDigitalMode()     do { anselRF6bits.anselRF6 = 0; } while(0)

// get/set IO_RF7 aliases
#define IO_RF7_TRIS                 TRISFbits.TRISF7
#define IO_RF7_LAT                  LATFbits.LATF7
#define IO_RF7_PORT                 PORTFbits.RF7
#define IO_RF7_SetHigh()            do { LATFbits.LATF7 = 1; } while(0)
#define IO_RF7_SetLow()             do { LATFbits.LATF7 = 0; } while(0)
#define IO_RF7_Toggle()             do { LATFbits.LATF7 = ~LATFbits.LATF7; } while(0)
#define IO_RF7_GetValue()           PORTFbits.RF7
#define IO_RF7_SetDigitalInput()    do { TRISFbits.TRISF7 = 1; } while(0)
#define IO_RF7_SetDigitalOutput()   do { TRISFbits.TRISF7 = 0; } while(0)

// get/set IO_RG0 aliases
#define IO_RG0_TRIS                 TRISGbits.TRISG0
#define IO_RG0_LAT                  LATGbits.LATG0
#define IO_RG0_PORT                 PORTGbits.RG0
#define IO_RG0_SetHigh()            do { LATGbits.LATG0 = 1; } while(0)
#define IO_RG0_SetLow()             do { LATGbits.LATG0 = 0; } while(0)
#define IO_RG0_Toggle()             do { LATGbits.LATG0 = ~LATGbits.LATG0; } while(0)
#define IO_RG0_GetValue()           PORTGbits.RG0
#define IO_RG0_SetDigitalInput()    do { TRISGbits.TRISG0 = 1; } while(0)
#define IO_RG0_SetDigitalOutput()   do { TRISGbits.TRISG0 = 0; } while(0)

// get/set IO_RG1 aliases
#define IO_RG1_TRIS                 TRISGbits.TRISG1
#define IO_RG1_LAT                  LATGbits.LATG1
#define IO_RG1_PORT                 PORTGbits.RG1
#define IO_RG1_SetHigh()            do { LATGbits.LATG1 = 1; } while(0)
#define IO_RG1_SetLow()             do { LATGbits.LATG1 = 0; } while(0)
#define IO_RG1_Toggle()             do { LATGbits.LATG1 = ~LATGbits.LATG1; } while(0)
#define IO_RG1_GetValue()           PORTGbits.RG1
#define IO_RG1_SetDigitalInput()    do { TRISGbits.TRISG1 = 1; } while(0)
#define IO_RG1_SetDigitalOutput()   do { TRISGbits.TRISG1 = 0; } while(0)

// get/set IO_RG2 aliases
#define IO_RG2_TRIS                 TRISGbits.TRISG2
#define IO_RG2_LAT                  LATGbits.LATG2
#define IO_RG2_PORT                 PORTGbits.RG2
#define IO_RG2_SetHigh()            do { LATGbits.LATG2 = 1; } while(0)
#define IO_RG2_SetLow()             do { LATGbits.LATG2 = 0; } while(0)
#define IO_RG2_Toggle()             do { LATGbits.LATG2 = ~LATGbits.LATG2; } while(0)
#define IO_RG2_GetValue()           PORTGbits.RG2
#define IO_RG2_SetDigitalInput()    do { TRISGbits.TRISG2 = 1; } while(0)
#define IO_RG2_SetDigitalOutput()   do { TRISGbits.TRISG2 = 0; } while(0)

// get/set IO_RG3 aliases
#define IO_RG3_TRIS                 TRISGbits.TRISG3
#define IO_RG3_LAT                  LATGbits.LATG3
#define IO_RG3_PORT                 PORTGbits.RG3
#define IO_RG3_SetHigh()            do { LATGbits.LATG3 = 1; } while(0)
#define IO_RG3_SetLow()             do { LATGbits.LATG3 = 0; } while(0)
#define IO_RG3_Toggle()             do { LATGbits.LATG3 = ~LATGbits.LATG3; } while(0)
#define IO_RG3_GetValue()           PORTGbits.RG3
#define IO_RG3_SetDigitalInput()    do { TRISGbits.TRISG3 = 1; } while(0)
#define IO_RG3_SetDigitalOutput()   do { TRISGbits.TRISG3 = 0; } while(0)

// get/set IO_RG4 aliases
#define IO_RG4_TRIS                 TRISGbits.TRISG4
#define IO_RG4_LAT                  LATGbits.LATG4
#define IO_RG4_PORT                 PORTGbits.RG4
#define IO_RG4_SetHigh()            do { LATGbits.LATG4 = 1; } while(0)
#define IO_RG4_SetLow()             do { LATGbits.LATG4 = 0; } while(0)
#define IO_RG4_Toggle()             do { LATGbits.LATG4 = ~LATGbits.LATG4; } while(0)
#define IO_RG4_GetValue()           PORTGbits.RG4
#define IO_RG4_SetDigitalInput()    do { TRISGbits.TRISG4 = 1; } while(0)
#define IO_RG4_SetDigitalOutput()   do { TRISGbits.TRISG4 = 0; } while(0)

// get/set IO_RG5 aliases
#define IO_RG5_TRIS                 TRISGbits.TRISG5
#define IO_RG5_LAT                  LATGbits.LATG5
#define IO_RG5_PORT                 PORTGbits.RG5
#define IO_RG5_SetHigh()            do { LATGbits.LATG5 = 1; } while(0)
#define IO_RG5_SetLow()             do { LATGbits.LATG5 = 0; } while(0)
#define IO_RG5_Toggle()             do { LATGbits.LATG5 = ~LATGbits.LATG5; } while(0)
#define IO_RG5_GetValue()           PORTGbits.RG5
#define IO_RG5_SetDigitalInput()    do { TRISGbits.TRISG5 = 1; } while(0)
#define IO_RG5_SetDigitalOutput()   do { TRISGbits.TRISG5 = 0; } while(0)

// get/set IO_RG6 aliases
#define IO_RG6_TRIS                 TRISGbits.TRISG6
#define IO_RG6_LAT                  LATGbits.LATG6
#define IO_RG6_PORT                 PORTGbits.RG6
#define IO_RG6_SetHigh()            do { LATGbits.LATG6 = 1; } while(0)
#define IO_RG6_SetLow()             do { LATGbits.LATG6 = 0; } while(0)
#define IO_RG6_Toggle()             do { LATGbits.LATG6 = ~LATGbits.LATG6; } while(0)
#define IO_RG6_GetValue()           PORTGbits.RG6
#define IO_RG6_SetDigitalInput()    do { TRISGbits.TRISG6 = 1; } while(0)
#define IO_RG6_SetDigitalOutput()   do { TRISGbits.TRISG6 = 0; } while(0)

// get/set IO_RG7 aliases
#define IO_RG7_TRIS                 TRISGbits.TRISG7
#define IO_RG7_LAT                  LATGbits.LATG7
#define IO_RG7_PORT                 PORTGbits.RG7
#define IO_RG7_SetHigh()            do { LATGbits.LATG7 = 1; } while(0)
#define IO_RG7_SetLow()             do { LATGbits.LATG7 = 0; } while(0)
#define IO_RG7_Toggle()             do { LATGbits.LATG7 = ~LATGbits.LATG7; } while(0)
#define IO_RG7_GetValue()           PORTGbits.RG7
#define IO_RG7_SetDigitalInput()    do { TRISGbits.TRISG7 = 1; } while(0)
#define IO_RG7_SetDigitalOutput()   do { TRISGbits.TRISG7 = 0; } while(0)

// get/set IO_RH0 aliases
#define IO_RH0_TRIS                 TRISHbits.TRISH0
#define IO_RH0_LAT                  LATHbits.LATH0
#define IO_RH0_PORT                 PORTHbits.RH0
#define IO_RH0_SetHigh()            do { LATHbits.LATH0 = 1; } while(0)
#define IO_RH0_SetLow()             do { LATHbits.LATH0 = 0; } while(0)
#define IO_RH0_Toggle()             do { LATHbits.LATH0 = ~LATHbits.LATH0; } while(0)
#define IO_RH0_GetValue()           PORTHbits.RH0
#define IO_RH0_SetDigitalInput()    do { TRISHbits.TRISH0 = 1; } while(0)
#define IO_RH0_SetDigitalOutput()   do { TRISHbits.TRISH0 = 0; } while(0)

// get/set IO_RH1 aliases
#define IO_RH1_TRIS                 TRISHbits.TRISH1
#define IO_RH1_LAT                  LATHbits.LATH1
#define IO_RH1_PORT                 PORTHbits.RH1
#define IO_RH1_SetHigh()            do { LATHbits.LATH1 = 1; } while(0)
#define IO_RH1_SetLow()             do { LATHbits.LATH1 = 0; } while(0)
#define IO_RH1_Toggle()             do { LATHbits.LATH1 = ~LATHbits.LATH1; } while(0)
#define IO_RH1_GetValue()           PORTHbits.RH1
#define IO_RH1_SetDigitalInput()    do { TRISHbits.TRISH1 = 1; } while(0)
#define IO_RH1_SetDigitalOutput()   do { TRISHbits.TRISH1 = 0; } while(0)

// get/set IO_RH2 aliases
#define IO_RH2_TRIS                 TRISHbits.TRISH2
#define IO_RH2_LAT                  LATHbits.LATH2
#define IO_RH2_PORT                 PORTHbits.RH2
#define IO_RH2_SetHigh()            do { LATHbits.LATH2 = 1; } while(0)
#define IO_RH2_SetLow()             do { LATHbits.LATH2 = 0; } while(0)
#define IO_RH2_Toggle()             do { LATHbits.LATH2 = ~LATHbits.LATH2; } while(0)
#define IO_RH2_GetValue()           PORTHbits.RH2
#define IO_RH2_SetDigitalInput()    do { TRISHbits.TRISH2 = 1; } while(0)
#define IO_RH2_SetDigitalOutput()   do { TRISHbits.TRISH2 = 0; } while(0)

// get/set IO_RH3 aliases
#define IO_RH3_TRIS                 TRISHbits.TRISH3
#define IO_RH3_LAT                  LATHbits.LATH3
#define IO_RH3_PORT                 PORTHbits.RH3
#define IO_RH3_SetHigh()            do { LATHbits.LATH3 = 1; } while(0)
#define IO_RH3_SetLow()             do { LATHbits.LATH3 = 0; } while(0)
#define IO_RH3_Toggle()             do { LATHbits.LATH3 = ~LATHbits.LATH3; } while(0)
#define IO_RH3_GetValue()           PORTHbits.RH3
#define IO_RH3_SetDigitalInput()    do { TRISHbits.TRISH3 = 1; } while(0)
#define IO_RH3_SetDigitalOutput()   do { TRISHbits.TRISH3 = 0; } while(0)

// get/set IO_RH4 aliases
#define IO_RH4_TRIS                 TRISHbits.TRISH4
#define IO_RH4_LAT                  LATHbits.LATH4
#define IO_RH4_PORT                 PORTHbits.RH4
#define IO_RH4_ANS                  anselRH4bits.anselRH4
#define IO_RH4_SetHigh()            do { LATHbits.LATH4 = 1; } while(0)
#define IO_RH4_SetLow()             do { LATHbits.LATH4 = 0; } while(0)
#define IO_RH4_Toggle()             do { LATHbits.LATH4 = ~LATHbits.LATH4; } while(0)
#define IO_RH4_GetValue()           PORTHbits.RH4
#define IO_RH4_SetDigitalInput()    do { TRISHbits.TRISH4 = 1; } while(0)
#define IO_RH4_SetDigitalOutput()   do { TRISHbits.TRISH4 = 0; } while(0)
#define IO_RH4_SetAnalogMode()      do { anselRH4bits.anselRH4 = 1; } while(0)
#define IO_RH4_SetDigitalMode()     do { anselRH4bits.anselRH4 = 0; } while(0)

// get/set IO_RH5 aliases
#define IO_RH5_TRIS                 TRISHbits.TRISH5
#define IO_RH5_LAT                  LATHbits.LATH5
#define IO_RH5_PORT                 PORTHbits.RH5
#define IO_RH5_ANS                  anselRH5bits.anselRH5
#define IO_RH5_SetHigh()            do { LATHbits.LATH5 = 1; } while(0)
#define IO_RH5_SetLow()             do { LATHbits.LATH5 = 0; } while(0)
#define IO_RH5_Toggle()             do { LATHbits.LATH5 = ~LATHbits.LATH5; } while(0)
#define IO_RH5_GetValue()           PORTHbits.RH5
#define IO_RH5_SetDigitalInput()    do { TRISHbits.TRISH5 = 1; } while(0)
#define IO_RH5_SetDigitalOutput()   do { TRISHbits.TRISH5 = 0; } while(0)
#define IO_RH5_SetAnalogMode()      do { anselRH5bits.anselRH5 = 1; } while(0)
#define IO_RH5_SetDigitalMode()     do { anselRH5bits.anselRH5 = 0; } while(0)

// get/set IO_RH6 aliases
#define IO_RH6_TRIS                 TRISHbits.TRISH6
#define IO_RH6_LAT                  LATHbits.LATH6
#define IO_RH6_PORT                 PORTHbits.RH6
#define IO_RH6_ANS                  anselRH6bits.anselRH6
#define IO_RH6_SetHigh()            do { LATHbits.LATH6 = 1; } while(0)
#define IO_RH6_SetLow()             do { LATHbits.LATH6 = 0; } while(0)
#define IO_RH6_Toggle()             do { LATHbits.LATH6 = ~LATHbits.LATH6; } while(0)
#define IO_RH6_GetValue()           PORTHbits.RH6
#define IO_RH6_SetDigitalInput()    do { TRISHbits.TRISH6 = 1; } while(0)
#define IO_RH6_SetDigitalOutput()   do { TRISHbits.TRISH6 = 0; } while(0)
#define IO_RH6_SetAnalogMode()      do { anselRH6bits.anselRH6 = 1; } while(0)
#define IO_RH6_SetDigitalMode()     do { anselRH6bits.anselRH6 = 0; } while(0)

// get/set IO_RH7 aliases
#define IO_RH7_TRIS                 TRISHbits.TRISH7
#define IO_RH7_LAT                  LATHbits.LATH7
#define IO_RH7_PORT                 PORTHbits.RH7
#define IO_RH7_ANS                  anselRH7bits.anselRH7
#define IO_RH7_SetHigh()            do { LATHbits.LATH7 = 1; } while(0)
#define IO_RH7_SetLow()             do { LATHbits.LATH7 = 0; } while(0)
#define IO_RH7_Toggle()             do { LATHbits.LATH7 = ~LATHbits.LATH7; } while(0)
#define IO_RH7_GetValue()           PORTHbits.RH7
#define IO_RH7_SetDigitalInput()    do { TRISHbits.TRISH7 = 1; } while(0)
#define IO_RH7_SetDigitalOutput()   do { TRISHbits.TRISH7 = 0; } while(0)
#define IO_RH7_SetAnalogMode()      do { anselRH7bits.anselRH7 = 1; } while(0)
#define IO_RH7_SetDigitalMode()     do { anselRH7bits.anselRH7 = 0; } while(0)

// get/set HALL_BUSSTOP_STN_IO aliases
#define HALL_BUSSTOP_STN_IO_TRIS                 TRISJbits.TRISJ0
#define HALL_BUSSTOP_STN_IO_LAT                  LATJbits.LATJ0
#define HALL_BUSSTOP_STN_IO_PORT                 PORTJbits.RJ0
#define HALL_BUSSTOP_STN_IO_SetHigh()            do { LATJbits.LATJ0 = 1; } while(0)
#define HALL_BUSSTOP_STN_IO_SetLow()             do { LATJbits.LATJ0 = 0; } while(0)
#define HALL_BUSSTOP_STN_IO_Toggle()             do { LATJbits.LATJ0 = ~LATJbits.LATJ0; } while(0)
#define HALL_BUSSTOP_STN_IO_GetValue()           PORTJbits.RJ0
#define HALL_BUSSTOP_STN_IO_SetDigitalInput()    do { TRISJbits.TRISJ0 = 1; } while(0)
#define HALL_BUSSTOP_STN_IO_SetDigitalOutput()   do { TRISJbits.TRISJ0 = 0; } while(0)

// get/set HALL_BUSSTOP_IND_IO aliases
#define HALL_BUSSTOP_IND_IO_TRIS                 TRISJbits.TRISJ1
#define HALL_BUSSTOP_IND_IO_LAT                  LATJbits.LATJ1
#define HALL_BUSSTOP_IND_IO_PORT                 PORTJbits.RJ1
#define HALL_BUSSTOP_IND_IO_SetHigh()            do { LATJbits.LATJ1 = 1; } while(0)
#define HALL_BUSSTOP_IND_IO_SetLow()             do { LATJbits.LATJ1 = 0; } while(0)
#define HALL_BUSSTOP_IND_IO_Toggle()             do { LATJbits.LATJ1 = ~LATJbits.LATJ1; } while(0)
#define HALL_BUSSTOP_IND_IO_GetValue()           PORTJbits.RJ1
#define HALL_BUSSTOP_IND_IO_SetDigitalInput()    do { TRISJbits.TRISJ1 = 1; } while(0)
#define HALL_BUSSTOP_IND_IO_SetDigitalOutput()   do { TRISJbits.TRISJ1 = 0; } while(0)

// get/set HALL_STOP_FDEP_IO aliases
#define HALL_STOP_FDEP_IO_TRIS                 TRISJbits.TRISJ2
#define HALL_STOP_FDEP_IO_LAT                  LATJbits.LATJ2
#define HALL_STOP_FDEP_IO_PORT                 PORTJbits.RJ2
#define HALL_STOP_FDEP_IO_SetHigh()            do { LATJbits.LATJ2 = 1; } while(0)
#define HALL_STOP_FDEP_IO_SetLow()             do { LATJbits.LATJ2 = 0; } while(0)
#define HALL_STOP_FDEP_IO_Toggle()             do { LATJbits.LATJ2 = ~LATJbits.LATJ2; } while(0)
#define HALL_STOP_FDEP_IO_GetValue()           PORTJbits.RJ2
#define HALL_STOP_FDEP_IO_SetDigitalInput()    do { TRISJbits.TRISJ2 = 1; } while(0)
#define HALL_STOP_FDEP_IO_SetDigitalOutput()   do { TRISJbits.TRISJ2 = 0; } while(0)

// get/set IO_RJ3 aliases
#define IO_RJ3_TRIS                 TRISJbits.TRISJ3
#define IO_RJ3_LAT                  LATJbits.LATJ3
#define IO_RJ3_PORT                 PORTJbits.RJ3
#define IO_RJ3_SetHigh()            do { LATJbits.LATJ3 = 1; } while(0)
#define IO_RJ3_SetLow()             do { LATJbits.LATJ3 = 0; } while(0)
#define IO_RJ3_Toggle()             do { LATJbits.LATJ3 = ~LATJbits.LATJ3; } while(0)
#define IO_RJ3_GetValue()           PORTJbits.RJ3
#define IO_RJ3_SetDigitalInput()    do { TRISJbits.TRISJ3 = 1; } while(0)
#define IO_RJ3_SetDigitalOutput()   do { TRISJbits.TRISJ3 = 0; } while(0)

// get/set IO_RJ4 aliases
#define IO_RJ4_TRIS                 TRISJbits.TRISJ4
#define IO_RJ4_LAT                  LATJbits.LATJ4
#define IO_RJ4_PORT                 PORTJbits.RJ4
#define IO_RJ4_SetHigh()            do { LATJbits.LATJ4 = 1; } while(0)
#define IO_RJ4_SetLow()             do { LATJbits.LATJ4 = 0; } while(0)
#define IO_RJ4_Toggle()             do { LATJbits.LATJ4 = ~LATJbits.LATJ4; } while(0)
#define IO_RJ4_GetValue()           PORTJbits.RJ4
#define IO_RJ4_SetDigitalInput()    do { TRISJbits.TRISJ4 = 1; } while(0)
#define IO_RJ4_SetDigitalOutput()   do { TRISJbits.TRISJ4 = 0; } while(0)

// get/set IO_RJ5 aliases
#define IO_RJ5_TRIS                 TRISJbits.TRISJ5
#define IO_RJ5_LAT                  LATJbits.LATJ5
#define IO_RJ5_PORT                 PORTJbits.RJ5
#define IO_RJ5_SetHigh()            do { LATJbits.LATJ5 = 1; } while(0)
#define IO_RJ5_SetLow()             do { LATJbits.LATJ5 = 0; } while(0)
#define IO_RJ5_Toggle()             do { LATJbits.LATJ5 = ~LATJbits.LATJ5; } while(0)
#define IO_RJ5_GetValue()           PORTJbits.RJ5
#define IO_RJ5_SetDigitalInput()    do { TRISJbits.TRISJ5 = 1; } while(0)
#define IO_RJ5_SetDigitalOutput()   do { TRISJbits.TRISJ5 = 0; } while(0)

// get/set IO_RJ6 aliases
#define IO_RJ6_TRIS                 TRISJbits.TRISJ6
#define IO_RJ6_LAT                  LATJbits.LATJ6
#define IO_RJ6_PORT                 PORTJbits.RJ6
#define IO_RJ6_SetHigh()            do { LATJbits.LATJ6 = 1; } while(0)
#define IO_RJ6_SetLow()             do { LATJbits.LATJ6 = 0; } while(0)
#define IO_RJ6_Toggle()             do { LATJbits.LATJ6 = ~LATJbits.LATJ6; } while(0)
#define IO_RJ6_GetValue()           PORTJbits.RJ6
#define IO_RJ6_SetDigitalInput()    do { TRISJbits.TRISJ6 = 1; } while(0)
#define IO_RJ6_SetDigitalOutput()   do { TRISJbits.TRISJ6 = 0; } while(0)

// get/set IO_RJ7 aliases
#define IO_RJ7_TRIS                 TRISJbits.TRISJ7
#define IO_RJ7_LAT                  LATJbits.LATJ7
#define IO_RJ7_PORT                 PORTJbits.RJ7
#define IO_RJ7_SetHigh()            do { LATJbits.LATJ7 = 1; } while(0)
#define IO_RJ7_SetLow()             do { LATJbits.LATJ7 = 0; } while(0)
#define IO_RJ7_Toggle()             do { LATJbits.LATJ7 = ~LATJbits.LATJ7; } while(0)
#define IO_RJ7_GetValue()           PORTJbits.RJ7
#define IO_RJ7_SetDigitalInput()    do { TRISJbits.TRISJ7 = 1; } while(0)
#define IO_RJ7_SetDigitalOutput()   do { TRISJbits.TRISJ7 = 0; } while(0)


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