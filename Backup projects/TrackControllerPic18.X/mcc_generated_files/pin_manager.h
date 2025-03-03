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

// get/set BLK_SIG_10B_GR aliases
#define BLK_SIG_10B_GR_TRIS                 TRISBbits.TRISB0
#define BLK_SIG_10B_GR_LAT                  LATBbits.LATB0
#define BLK_SIG_10B_GR_PORT                 PORTBbits.RB0
#define BLK_SIG_10B_GR_SetHigh()            do { LATBbits.LATB0 = 1; } while(0)
#define BLK_SIG_10B_GR_SetLow()             do { LATBbits.LATB0 = 0; } while(0)
#define BLK_SIG_10B_GR_Toggle()             do { LATBbits.LATB0 = ~LATBbits.LATB0; } while(0)
#define BLK_SIG_10B_GR_GetValue()           PORTBbits.RB0
#define BLK_SIG_10B_GR_SetDigitalInput()    do { TRISBbits.TRISB0 = 1; } while(0)
#define BLK_SIG_10B_GR_SetDigitalOutput()   do { TRISBbits.TRISB0 = 0; } while(0)

// get/set BLK_SIG_10B_RD aliases
#define BLK_SIG_10B_RD_TRIS                 TRISBbits.TRISB1
#define BLK_SIG_10B_RD_LAT                  LATBbits.LATB1
#define BLK_SIG_10B_RD_PORT                 PORTBbits.RB1
#define BLK_SIG_10B_RD_SetHigh()            do { LATBbits.LATB1 = 1; } while(0)
#define BLK_SIG_10B_RD_SetLow()             do { LATBbits.LATB1 = 0; } while(0)
#define BLK_SIG_10B_RD_Toggle()             do { LATBbits.LATB1 = ~LATBbits.LATB1; } while(0)
#define BLK_SIG_10B_RD_GetValue()           PORTBbits.RB1
#define BLK_SIG_10B_RD_SetDigitalInput()    do { TRISBbits.TRISB1 = 1; } while(0)
#define BLK_SIG_10B_RD_SetDigitalOutput()   do { TRISBbits.TRISB1 = 0; } while(0)

// get/set BLK_SIG_1B_GR aliases
#define BLK_SIG_1B_GR_TRIS                 TRISBbits.TRISB2
#define BLK_SIG_1B_GR_LAT                  LATBbits.LATB2
#define BLK_SIG_1B_GR_PORT                 PORTBbits.RB2
#define BLK_SIG_1B_GR_SetHigh()            do { LATBbits.LATB2 = 1; } while(0)
#define BLK_SIG_1B_GR_SetLow()             do { LATBbits.LATB2 = 0; } while(0)
#define BLK_SIG_1B_GR_Toggle()             do { LATBbits.LATB2 = ~LATBbits.LATB2; } while(0)
#define BLK_SIG_1B_GR_GetValue()           PORTBbits.RB2
#define BLK_SIG_1B_GR_SetDigitalInput()    do { TRISBbits.TRISB2 = 1; } while(0)
#define BLK_SIG_1B_GR_SetDigitalOutput()   do { TRISBbits.TRISB2 = 0; } while(0)

// get/set BLK_SIG_1B_RD aliases
#define BLK_SIG_1B_RD_TRIS                 TRISBbits.TRISB3
#define BLK_SIG_1B_RD_LAT                  LATBbits.LATB3
#define BLK_SIG_1B_RD_PORT                 PORTBbits.RB3
#define BLK_SIG_1B_RD_SetHigh()            do { LATBbits.LATB3 = 1; } while(0)
#define BLK_SIG_1B_RD_SetLow()             do { LATBbits.LATB3 = 0; } while(0)
#define BLK_SIG_1B_RD_Toggle()             do { LATBbits.LATB3 = ~LATBbits.LATB3; } while(0)
#define BLK_SIG_1B_RD_GetValue()           PORTBbits.RB3
#define BLK_SIG_1B_RD_SetDigitalInput()    do { TRISBbits.TRISB3 = 1; } while(0)
#define BLK_SIG_1B_RD_SetDigitalOutput()   do { TRISBbits.TRISB3 = 0; } while(0)

// get/set BLK_SIG_2B_GR aliases
#define BLK_SIG_2B_GR_TRIS                 TRISBbits.TRISB4
#define BLK_SIG_2B_GR_LAT                  LATBbits.LATB4
#define BLK_SIG_2B_GR_PORT                 PORTBbits.RB4
#define BLK_SIG_2B_GR_SetHigh()            do { LATBbits.LATB4 = 1; } while(0)
#define BLK_SIG_2B_GR_SetLow()             do { LATBbits.LATB4 = 0; } while(0)
#define BLK_SIG_2B_GR_Toggle()             do { LATBbits.LATB4 = ~LATBbits.LATB4; } while(0)
#define BLK_SIG_2B_GR_GetValue()           PORTBbits.RB4
#define BLK_SIG_2B_GR_SetDigitalInput()    do { TRISBbits.TRISB4 = 1; } while(0)
#define BLK_SIG_2B_GR_SetDigitalOutput()   do { TRISBbits.TRISB4 = 0; } while(0)

// get/set BLK_SIG_2B_RD aliases
#define BLK_SIG_2B_RD_TRIS                 TRISBbits.TRISB5
#define BLK_SIG_2B_RD_LAT                  LATBbits.LATB5
#define BLK_SIG_2B_RD_PORT                 PORTBbits.RB5
#define BLK_SIG_2B_RD_SetHigh()            do { LATBbits.LATB5 = 1; } while(0)
#define BLK_SIG_2B_RD_SetLow()             do { LATBbits.LATB5 = 0; } while(0)
#define BLK_SIG_2B_RD_Toggle()             do { LATBbits.LATB5 = ~LATBbits.LATB5; } while(0)
#define BLK_SIG_2B_RD_GetValue()           PORTBbits.RB5
#define BLK_SIG_2B_RD_SetDigitalInput()    do { TRISBbits.TRISB5 = 1; } while(0)
#define BLK_SIG_2B_RD_SetDigitalOutput()   do { TRISBbits.TRISB5 = 0; } while(0)

// get/set BLK_SIG_3B_GR aliases
#define BLK_SIG_3B_GR_TRIS                 TRISBbits.TRISB6
#define BLK_SIG_3B_GR_LAT                  LATBbits.LATB6
#define BLK_SIG_3B_GR_PORT                 PORTBbits.RB6
#define BLK_SIG_3B_GR_SetHigh()            do { LATBbits.LATB6 = 1; } while(0)
#define BLK_SIG_3B_GR_SetLow()             do { LATBbits.LATB6 = 0; } while(0)
#define BLK_SIG_3B_GR_Toggle()             do { LATBbits.LATB6 = ~LATBbits.LATB6; } while(0)
#define BLK_SIG_3B_GR_GetValue()           PORTBbits.RB6
#define BLK_SIG_3B_GR_SetDigitalInput()    do { TRISBbits.TRISB6 = 1; } while(0)
#define BLK_SIG_3B_GR_SetDigitalOutput()   do { TRISBbits.TRISB6 = 0; } while(0)

// get/set BLK_SIG_3B_RD aliases
#define BLK_SIG_3B_RD_TRIS                 TRISBbits.TRISB7
#define BLK_SIG_3B_RD_LAT                  LATBbits.LATB7
#define BLK_SIG_3B_RD_PORT                 PORTBbits.RB7
#define BLK_SIG_3B_RD_SetHigh()            do { LATBbits.LATB7 = 1; } while(0)
#define BLK_SIG_3B_RD_SetLow()             do { LATBbits.LATB7 = 0; } while(0)
#define BLK_SIG_3B_RD_Toggle()             do { LATBbits.LATB7 = ~LATBbits.LATB7; } while(0)
#define BLK_SIG_3B_RD_GetValue()           PORTBbits.RB7
#define BLK_SIG_3B_RD_SetDigitalInput()    do { TRISBbits.TRISB7 = 1; } while(0)
#define BLK_SIG_3B_RD_SetDigitalOutput()   do { TRISBbits.TRISB7 = 0; } while(0)

// get/set WS_TO_FYRD_1_L aliases
#define WS_TO_FYRD_1_L_TRIS                 TRISCbits.TRISC0
#define WS_TO_FYRD_1_L_LAT                  LATCbits.LATC0
#define WS_TO_FYRD_1_L_PORT                 PORTCbits.RC0
#define WS_TO_FYRD_1_L_SetHigh()            do { LATCbits.LATC0 = 1; } while(0)
#define WS_TO_FYRD_1_L_SetLow()             do { LATCbits.LATC0 = 0; } while(0)
#define WS_TO_FYRD_1_L_Toggle()             do { LATCbits.LATC0 = ~LATCbits.LATC0; } while(0)
#define WS_TO_FYRD_1_L_GetValue()           PORTCbits.RC0
#define WS_TO_FYRD_1_L_SetDigitalInput()    do { TRISCbits.TRISC0 = 1; } while(0)
#define WS_TO_FYRD_1_L_SetDigitalOutput()   do { TRISCbits.TRISC0 = 0; } while(0)

// get/set WS_TO_FYRD_2_L aliases
#define WS_TO_FYRD_2_L_TRIS                 TRISCbits.TRISC1
#define WS_TO_FYRD_2_L_LAT                  LATCbits.LATC1
#define WS_TO_FYRD_2_L_PORT                 PORTCbits.RC1
#define WS_TO_FYRD_2_L_SetHigh()            do { LATCbits.LATC1 = 1; } while(0)
#define WS_TO_FYRD_2_L_SetLow()             do { LATCbits.LATC1 = 0; } while(0)
#define WS_TO_FYRD_2_L_Toggle()             do { LATCbits.LATC1 = ~LATCbits.LATC1; } while(0)
#define WS_TO_FYRD_2_L_GetValue()           PORTCbits.RC1
#define WS_TO_FYRD_2_L_SetDigitalInput()    do { TRISCbits.TRISC1 = 1; } while(0)
#define WS_TO_FYRD_2_L_SetDigitalOutput()   do { TRISCbits.TRISC1 = 0; } while(0)

// get/set WS_TO_FYRD_3_R aliases
#define WS_TO_FYRD_3_R_TRIS                 TRISCbits.TRISC2
#define WS_TO_FYRD_3_R_LAT                  LATCbits.LATC2
#define WS_TO_FYRD_3_R_PORT                 PORTCbits.RC2
#define WS_TO_FYRD_3_R_SetHigh()            do { LATCbits.LATC2 = 1; } while(0)
#define WS_TO_FYRD_3_R_SetLow()             do { LATCbits.LATC2 = 0; } while(0)
#define WS_TO_FYRD_3_R_Toggle()             do { LATCbits.LATC2 = ~LATCbits.LATC2; } while(0)
#define WS_TO_FYRD_3_R_GetValue()           PORTCbits.RC2
#define WS_TO_FYRD_3_R_SetDigitalInput()    do { TRISCbits.TRISC2 = 1; } while(0)
#define WS_TO_FYRD_3_R_SetDigitalOutput()   do { TRISCbits.TRISC2 = 0; } while(0)

// get/set WS_TO_FYRD_4_R aliases
#define WS_TO_FYRD_4_R_TRIS                 TRISCbits.TRISC3
#define WS_TO_FYRD_4_R_LAT                  LATCbits.LATC3
#define WS_TO_FYRD_4_R_PORT                 PORTCbits.RC3
#define WS_TO_FYRD_4_R_SetHigh()            do { LATCbits.LATC3 = 1; } while(0)
#define WS_TO_FYRD_4_R_SetLow()             do { LATCbits.LATC3 = 0; } while(0)
#define WS_TO_FYRD_4_R_Toggle()             do { LATCbits.LATC3 = ~LATCbits.LATC3; } while(0)
#define WS_TO_FYRD_4_R_GetValue()           PORTCbits.RC3
#define WS_TO_FYRD_4_R_SetDigitalInput()    do { TRISCbits.TRISC3 = 1; } while(0)
#define WS_TO_FYRD_4_R_SetDigitalOutput()   do { TRISCbits.TRISC3 = 0; } while(0)

// get/set WS_FR_FYRD_5_R aliases
#define WS_FR_FYRD_5_R_TRIS                 TRISCbits.TRISC4
#define WS_FR_FYRD_5_R_LAT                  LATCbits.LATC4
#define WS_FR_FYRD_5_R_PORT                 PORTCbits.RC4
#define WS_FR_FYRD_5_R_SetHigh()            do { LATCbits.LATC4 = 1; } while(0)
#define WS_FR_FYRD_5_R_SetLow()             do { LATCbits.LATC4 = 0; } while(0)
#define WS_FR_FYRD_5_R_Toggle()             do { LATCbits.LATC4 = ~LATCbits.LATC4; } while(0)
#define WS_FR_FYRD_5_R_GetValue()           PORTCbits.RC4
#define WS_FR_FYRD_5_R_SetDigitalInput()    do { TRISCbits.TRISC4 = 1; } while(0)
#define WS_FR_FYRD_5_R_SetDigitalOutput()   do { TRISCbits.TRISC4 = 0; } while(0)

// get/set WS_FR_FYRD_6_R aliases
#define WS_FR_FYRD_6_R_TRIS                 TRISCbits.TRISC5
#define WS_FR_FYRD_6_R_LAT                  LATCbits.LATC5
#define WS_FR_FYRD_6_R_PORT                 PORTCbits.RC5
#define WS_FR_FYRD_6_R_SetHigh()            do { LATCbits.LATC5 = 1; } while(0)
#define WS_FR_FYRD_6_R_SetLow()             do { LATCbits.LATC5 = 0; } while(0)
#define WS_FR_FYRD_6_R_Toggle()             do { LATCbits.LATC5 = ~LATCbits.LATC5; } while(0)
#define WS_FR_FYRD_6_R_GetValue()           PORTCbits.RC5
#define WS_FR_FYRD_6_R_SetDigitalInput()    do { TRISCbits.TRISC5 = 1; } while(0)
#define WS_FR_FYRD_6_R_SetDigitalOutput()   do { TRISCbits.TRISC5 = 0; } while(0)

// get/set WS_FR_FYRD_7_L aliases
#define WS_FR_FYRD_7_L_TRIS                 TRISCbits.TRISC6
#define WS_FR_FYRD_7_L_LAT                  LATCbits.LATC6
#define WS_FR_FYRD_7_L_PORT                 PORTCbits.RC6
#define WS_FR_FYRD_7_L_SetHigh()            do { LATCbits.LATC6 = 1; } while(0)
#define WS_FR_FYRD_7_L_SetLow()             do { LATCbits.LATC6 = 0; } while(0)
#define WS_FR_FYRD_7_L_Toggle()             do { LATCbits.LATC6 = ~LATCbits.LATC6; } while(0)
#define WS_FR_FYRD_7_L_GetValue()           PORTCbits.RC6
#define WS_FR_FYRD_7_L_SetDigitalInput()    do { TRISCbits.TRISC6 = 1; } while(0)
#define WS_FR_FYRD_7_L_SetDigitalOutput()   do { TRISCbits.TRISC6 = 0; } while(0)

// get/set WS_FR_FYRD_8_L aliases
#define WS_FR_FYRD_8_L_TRIS                 TRISCbits.TRISC7
#define WS_FR_FYRD_8_L_LAT                  LATCbits.LATC7
#define WS_FR_FYRD_8_L_PORT                 PORTCbits.RC7
#define WS_FR_FYRD_8_L_SetHigh()            do { LATCbits.LATC7 = 1; } while(0)
#define WS_FR_FYRD_8_L_SetLow()             do { LATCbits.LATC7 = 0; } while(0)
#define WS_FR_FYRD_8_L_Toggle()             do { LATCbits.LATC7 = ~LATCbits.LATC7; } while(0)
#define WS_FR_FYRD_8_L_GetValue()           PORTCbits.RC7
#define WS_FR_FYRD_8_L_SetDigitalInput()    do { TRISCbits.TRISC7 = 1; } while(0)
#define WS_FR_FYRD_8_L_SetDigitalOutput()   do { TRISCbits.TRISC7 = 0; } while(0)

// get/set WS_WALDSEE_100 aliases
#define WS_WALDSEE_100_TRIS                 TRISDbits.TRISD0
#define WS_WALDSEE_100_LAT                  LATDbits.LATD0
#define WS_WALDSEE_100_PORT                 PORTDbits.RD0
#define WS_WALDSEE_100_SetHigh()            do { LATDbits.LATD0 = 1; } while(0)
#define WS_WALDSEE_100_SetLow()             do { LATDbits.LATD0 = 0; } while(0)
#define WS_WALDSEE_100_Toggle()             do { LATDbits.LATD0 = ~LATDbits.LATD0; } while(0)
#define WS_WALDSEE_100_GetValue()           PORTDbits.RD0
#define WS_WALDSEE_100_SetDigitalInput()    do { TRISDbits.TRISD0 = 1; } while(0)
#define WS_WALDSEE_100_SetDigitalOutput()   do { TRISDbits.TRISD0 = 0; } while(0)

// get/set WS_TO_YRD_9 aliases
#define WS_TO_YRD_9_TRIS                 TRISDbits.TRISD1
#define WS_TO_YRD_9_LAT                  LATDbits.LATD1
#define WS_TO_YRD_9_PORT                 PORTDbits.RD1
#define WS_TO_YRD_9_SetHigh()            do { LATDbits.LATD1 = 1; } while(0)
#define WS_TO_YRD_9_SetLow()             do { LATDbits.LATD1 = 0; } while(0)
#define WS_TO_YRD_9_Toggle()             do { LATDbits.LATD1 = ~LATDbits.LATD1; } while(0)
#define WS_TO_YRD_9_GetValue()           PORTDbits.RD1
#define WS_TO_YRD_9_SetDigitalInput()    do { TRISDbits.TRISD1 = 1; } while(0)
#define WS_TO_YRD_9_SetDigitalOutput()   do { TRISDbits.TRISD1 = 0; } while(0)

// get/set WS_FR_YRD_10 aliases
#define WS_FR_YRD_10_TRIS                 TRISDbits.TRISD2
#define WS_FR_YRD_10_LAT                  LATDbits.LATD2
#define WS_FR_YRD_10_PORT                 PORTDbits.RD2
#define WS_FR_YRD_10_SetHigh()            do { LATDbits.LATD2 = 1; } while(0)
#define WS_FR_YRD_10_SetLow()             do { LATDbits.LATD2 = 0; } while(0)
#define WS_FR_YRD_10_Toggle()             do { LATDbits.LATD2 = ~LATDbits.LATD2; } while(0)
#define WS_FR_YRD_10_GetValue()           PORTDbits.RD2
#define WS_FR_YRD_10_SetDigitalInput()    do { TRISDbits.TRISD2 = 1; } while(0)
#define WS_FR_YRD_10_SetDigitalOutput()   do { TRISDbits.TRISD2 = 0; } while(0)

// get/set WS_WALDBERG_101 aliases
#define WS_WALDBERG_101_TRIS                 TRISDbits.TRISD3
#define WS_WALDBERG_101_LAT                  LATDbits.LATD3
#define WS_WALDBERG_101_PORT                 PORTDbits.RD3
#define WS_WALDBERG_101_SetHigh()            do { LATDbits.LATD3 = 1; } while(0)
#define WS_WALDBERG_101_SetLow()             do { LATDbits.LATD3 = 0; } while(0)
#define WS_WALDBERG_101_Toggle()             do { LATDbits.LATD3 = ~LATDbits.LATD3; } while(0)
#define WS_WALDBERG_101_GetValue()           PORTDbits.RD3
#define WS_WALDBERG_101_SetDigitalInput()    do { TRISDbits.TRISD3 = 1; } while(0)
#define WS_WALDBERG_101_SetDigitalOutput()   do { TRISDbits.TRISD3 = 0; } while(0)

// get/set DIR_INVERT_T4 aliases
#define DIR_INVERT_T4_TRIS                 TRISDbits.TRISD4
#define DIR_INVERT_T4_LAT                  LATDbits.LATD4
#define DIR_INVERT_T4_PORT                 PORTDbits.RD4
#define DIR_INVERT_T4_SetHigh()            do { LATDbits.LATD4 = 1; } while(0)
#define DIR_INVERT_T4_SetLow()             do { LATDbits.LATD4 = 0; } while(0)
#define DIR_INVERT_T4_Toggle()             do { LATDbits.LATD4 = ~LATDbits.LATD4; } while(0)
#define DIR_INVERT_T4_GetValue()           PORTDbits.RD4
#define DIR_INVERT_T4_SetDigitalInput()    do { TRISDbits.TRISD4 = 1; } while(0)
#define DIR_INVERT_T4_SetDigitalOutput()   do { TRISDbits.TRISD4 = 0; } while(0)

// get/set OCC_TO_23B aliases
#define OCC_TO_23B_TRIS                 TRISDbits.TRISD5
#define OCC_TO_23B_LAT                  LATDbits.LATD5
#define OCC_TO_23B_PORT                 PORTDbits.RD5
#define OCC_TO_23B_SetHigh()            do { LATDbits.LATD5 = 1; } while(0)
#define OCC_TO_23B_SetLow()             do { LATDbits.LATD5 = 0; } while(0)
#define OCC_TO_23B_Toggle()             do { LATDbits.LATD5 = ~LATDbits.LATD5; } while(0)
#define OCC_TO_23B_GetValue()           PORTDbits.RD5
#define OCC_TO_23B_SetDigitalInput()    do { TRISDbits.TRISD5 = 1; } while(0)
#define OCC_TO_23B_SetDigitalOutput()   do { TRISDbits.TRISD5 = 0; } while(0)

// get/set OCC_TO_21B aliases
#define OCC_TO_21B_TRIS                 TRISDbits.TRISD6
#define OCC_TO_21B_LAT                  LATDbits.LATD6
#define OCC_TO_21B_PORT                 PORTDbits.RD6
#define OCC_TO_21B_SetHigh()            do { LATDbits.LATD6 = 1; } while(0)
#define OCC_TO_21B_SetLow()             do { LATDbits.LATD6 = 0; } while(0)
#define OCC_TO_21B_Toggle()             do { LATDbits.LATD6 = ~LATDbits.LATD6; } while(0)
#define OCC_TO_21B_GetValue()           PORTDbits.RD6
#define OCC_TO_21B_SetDigitalInput()    do { TRISDbits.TRISD6 = 1; } while(0)
#define OCC_TO_21B_SetDigitalOutput()   do { TRISDbits.TRISD6 = 0; } while(0)

// get/set OCC_TO_9B aliases
#define OCC_TO_9B_TRIS                 TRISDbits.TRISD7
#define OCC_TO_9B_LAT                  LATDbits.LATD7
#define OCC_TO_9B_PORT                 PORTDbits.RD7
#define OCC_TO_9B_SetHigh()            do { LATDbits.LATD7 = 1; } while(0)
#define OCC_TO_9B_SetLow()             do { LATDbits.LATD7 = 0; } while(0)
#define OCC_TO_9B_Toggle()             do { LATDbits.LATD7 = ~LATDbits.LATD7; } while(0)
#define OCC_TO_9B_GetValue()           PORTDbits.RD7
#define OCC_TO_9B_SetDigitalInput()    do { TRISDbits.TRISD7 = 1; } while(0)
#define OCC_TO_9B_SetDigitalOutput()   do { TRISDbits.TRISD7 = 0; } while(0)

// get/set OCC_TO_STN_11 aliases
#define OCC_TO_STN_11_TRIS                 TRISEbits.TRISE0
#define OCC_TO_STN_11_LAT                  LATEbits.LATE0
#define OCC_TO_STN_11_PORT                 PORTEbits.RE0
#define OCC_TO_STN_11_SetHigh()            do { LATEbits.LATE0 = 1; } while(0)
#define OCC_TO_STN_11_SetLow()             do { LATEbits.LATE0 = 0; } while(0)
#define OCC_TO_STN_11_Toggle()             do { LATEbits.LATE0 = ~LATEbits.LATE0; } while(0)
#define OCC_TO_STN_11_GetValue()           PORTEbits.RE0
#define OCC_TO_STN_11_SetDigitalInput()    do { TRISEbits.TRISE0 = 1; } while(0)
#define OCC_TO_STN_11_SetDigitalOutput()   do { TRISEbits.TRISE0 = 0; } while(0)

// get/set OCC_TO_STN_12 aliases
#define OCC_TO_STN_12_TRIS                 TRISEbits.TRISE1
#define OCC_TO_STN_12_LAT                  LATEbits.LATE1
#define OCC_TO_STN_12_PORT                 PORTEbits.RE1
#define OCC_TO_STN_12_SetHigh()            do { LATEbits.LATE1 = 1; } while(0)
#define OCC_TO_STN_12_SetLow()             do { LATEbits.LATE1 = 0; } while(0)
#define OCC_TO_STN_12_Toggle()             do { LATEbits.LATE1 = ~LATEbits.LATE1; } while(0)
#define OCC_TO_STN_12_GetValue()           PORTEbits.RE1
#define OCC_TO_STN_12_SetDigitalInput()    do { TRISEbits.TRISE1 = 1; } while(0)
#define OCC_TO_STN_12_SetDigitalOutput()   do { TRISEbits.TRISE1 = 0; } while(0)

// get/set OCC_TO_T6 aliases
#define OCC_TO_T6_TRIS                 TRISEbits.TRISE2
#define OCC_TO_T6_LAT                  LATEbits.LATE2
#define OCC_TO_T6_PORT                 PORTEbits.RE2
#define OCC_TO_T6_SetHigh()            do { LATEbits.LATE2 = 1; } while(0)
#define OCC_TO_T6_SetLow()             do { LATEbits.LATE2 = 0; } while(0)
#define OCC_TO_T6_Toggle()             do { LATEbits.LATE2 = ~LATEbits.LATE2; } while(0)
#define OCC_TO_T6_GetValue()           PORTEbits.RE2
#define OCC_TO_T6_SetDigitalInput()    do { TRISEbits.TRISE2 = 1; } while(0)
#define OCC_TO_T6_SetDigitalOutput()   do { TRISEbits.TRISE2 = 0; } while(0)

// get/set OCC_TO_T3 aliases
#define OCC_TO_T3_TRIS                 TRISEbits.TRISE3
#define OCC_TO_T3_LAT                  LATEbits.LATE3
#define OCC_TO_T3_PORT                 PORTEbits.RE3
#define OCC_TO_T3_SetHigh()            do { LATEbits.LATE3 = 1; } while(0)
#define OCC_TO_T3_SetLow()             do { LATEbits.LATE3 = 0; } while(0)
#define OCC_TO_T3_Toggle()             do { LATEbits.LATE3 = ~LATEbits.LATE3; } while(0)
#define OCC_TO_T3_GetValue()           PORTEbits.RE3
#define OCC_TO_T3_SetDigitalInput()    do { TRISEbits.TRISE3 = 1; } while(0)
#define OCC_TO_T3_SetDigitalOutput()   do { TRISEbits.TRISE3 = 0; } while(0)

// get/set OCC_TO_8A aliases
#define OCC_TO_8A_TRIS                 TRISEbits.TRISE4
#define OCC_TO_8A_LAT                  LATEbits.LATE4
#define OCC_TO_8A_PORT                 PORTEbits.RE4
#define OCC_TO_8A_SetHigh()            do { LATEbits.LATE4 = 1; } while(0)
#define OCC_TO_8A_SetLow()             do { LATEbits.LATE4 = 0; } while(0)
#define OCC_TO_8A_Toggle()             do { LATEbits.LATE4 = ~LATEbits.LATE4; } while(0)
#define OCC_TO_8A_GetValue()           PORTEbits.RE4
#define OCC_TO_8A_SetDigitalInput()    do { TRISEbits.TRISE4 = 1; } while(0)
#define OCC_TO_8A_SetDigitalOutput()   do { TRISEbits.TRISE4 = 0; } while(0)

// get/set OCC_TO_22B aliases
#define OCC_TO_22B_TRIS                 TRISEbits.TRISE5
#define OCC_TO_22B_LAT                  LATEbits.LATE5
#define OCC_TO_22B_PORT                 PORTEbits.RE5
#define OCC_TO_22B_SetHigh()            do { LATEbits.LATE5 = 1; } while(0)
#define OCC_TO_22B_SetLow()             do { LATEbits.LATE5 = 0; } while(0)
#define OCC_TO_22B_Toggle()             do { LATEbits.LATE5 = ~LATEbits.LATE5; } while(0)
#define OCC_TO_22B_GetValue()           PORTEbits.RE5
#define OCC_TO_22B_SetDigitalInput()    do { TRISEbits.TRISE5 = 1; } while(0)
#define OCC_TO_22B_SetDigitalOutput()   do { TRISEbits.TRISE5 = 0; } while(0)

// get/set OCC_FR_21B aliases
#define OCC_FR_21B_TRIS                 TRISEbits.TRISE6
#define OCC_FR_21B_LAT                  LATEbits.LATE6
#define OCC_FR_21B_PORT                 PORTEbits.RE6
#define OCC_FR_21B_SetHigh()            do { LATEbits.LATE6 = 1; } while(0)
#define OCC_FR_21B_SetLow()             do { LATEbits.LATE6 = 0; } while(0)
#define OCC_FR_21B_Toggle()             do { LATEbits.LATE6 = ~LATEbits.LATE6; } while(0)
#define OCC_FR_21B_GetValue()           PORTEbits.RE6
#define OCC_FR_21B_SetDigitalInput()    do { TRISEbits.TRISE6 = 1; } while(0)
#define OCC_FR_21B_SetDigitalOutput()   do { TRISEbits.TRISE6 = 0; } while(0)

// get/set VOLTDET aliases
#define VOLTDET_TRIS                 TRISEbits.TRISE7
#define VOLTDET_LAT                  LATEbits.LATE7
#define VOLTDET_PORT                 PORTEbits.RE7
#define VOLTDET_SetHigh()            do { LATEbits.LATE7 = 1; } while(0)
#define VOLTDET_SetLow()             do { LATEbits.LATE7 = 0; } while(0)
#define VOLTDET_Toggle()             do { LATEbits.LATE7 = ~LATEbits.LATE7; } while(0)
#define VOLTDET_GetValue()           PORTEbits.RE7
#define VOLTDET_SetDigitalInput()    do { TRISEbits.TRISE7 = 1; } while(0)
#define VOLTDET_SetDigitalOutput()   do { TRISEbits.TRISE7 = 0; } while(0)

// get/set HALL_BLK_13 aliases
#define HALL_BLK_13_TRIS                 TRISFbits.TRISF0
#define HALL_BLK_13_LAT                  LATFbits.LATF0
#define HALL_BLK_13_PORT                 PORTFbits.RF0
#define HALL_BLK_13_ANS                  anselRF0bits.anselRF0
#define HALL_BLK_13_SetHigh()            do { LATFbits.LATF0 = 1; } while(0)
#define HALL_BLK_13_SetLow()             do { LATFbits.LATF0 = 0; } while(0)
#define HALL_BLK_13_Toggle()             do { LATFbits.LATF0 = ~LATFbits.LATF0; } while(0)
#define HALL_BLK_13_GetValue()           PORTFbits.RF0
#define HALL_BLK_13_SetDigitalInput()    do { TRISFbits.TRISF0 = 1; } while(0)
#define HALL_BLK_13_SetDigitalOutput()   do { TRISFbits.TRISF0 = 0; } while(0)
#define HALL_BLK_13_SetAnalogMode()      do { anselRF0bits.anselRF0 = 1; } while(0)
#define HALL_BLK_13_SetDigitalMode()     do { anselRF0bits.anselRF0 = 0; } while(0)

// get/set HALL_BLK_21A aliases
#define HALL_BLK_21A_TRIS                 TRISFbits.TRISF1
#define HALL_BLK_21A_LAT                  LATFbits.LATF1
#define HALL_BLK_21A_PORT                 PORTFbits.RF1
#define HALL_BLK_21A_ANS                  anselRF1bits.anselRF1
#define HALL_BLK_21A_SetHigh()            do { LATFbits.LATF1 = 1; } while(0)
#define HALL_BLK_21A_SetLow()             do { LATFbits.LATF1 = 0; } while(0)
#define HALL_BLK_21A_Toggle()             do { LATFbits.LATF1 = ~LATFbits.LATF1; } while(0)
#define HALL_BLK_21A_GetValue()           PORTFbits.RF1
#define HALL_BLK_21A_SetDigitalInput()    do { TRISFbits.TRISF1 = 1; } while(0)
#define HALL_BLK_21A_SetDigitalOutput()   do { TRISFbits.TRISF1 = 0; } while(0)
#define HALL_BLK_21A_SetAnalogMode()      do { anselRF1bits.anselRF1 = 1; } while(0)
#define HALL_BLK_21A_SetDigitalMode()     do { anselRF1bits.anselRF1 = 0; } while(0)

// get/set HALL_BLK_T4 aliases
#define HALL_BLK_T4_TRIS                 TRISFbits.TRISF2
#define HALL_BLK_T4_LAT                  LATFbits.LATF2
#define HALL_BLK_T4_PORT                 PORTFbits.RF2
#define HALL_BLK_T4_ANS                  anselRF2bits.anselRF2
#define HALL_BLK_T4_SetHigh()            do { LATFbits.LATF2 = 1; } while(0)
#define HALL_BLK_T4_SetLow()             do { LATFbits.LATF2 = 0; } while(0)
#define HALL_BLK_T4_Toggle()             do { LATFbits.LATF2 = ~LATFbits.LATF2; } while(0)
#define HALL_BLK_T4_GetValue()           PORTFbits.RF2
#define HALL_BLK_T4_SetDigitalInput()    do { TRISFbits.TRISF2 = 1; } while(0)
#define HALL_BLK_T4_SetDigitalOutput()   do { TRISFbits.TRISF2 = 0; } while(0)
#define HALL_BLK_T4_SetAnalogMode()      do { anselRF2bits.anselRF2 = 1; } while(0)
#define HALL_BLK_T4_SetDigitalMode()     do { anselRF2bits.anselRF2 = 0; } while(0)

// get/set HALL_BLK_T5 aliases
#define HALL_BLK_T5_TRIS                 TRISFbits.TRISF3
#define HALL_BLK_T5_LAT                  LATFbits.LATF3
#define HALL_BLK_T5_PORT                 PORTFbits.RF3
#define HALL_BLK_T5_ANS                  anselRF3bits.anselRF3
#define HALL_BLK_T5_SetHigh()            do { LATFbits.LATF3 = 1; } while(0)
#define HALL_BLK_T5_SetLow()             do { LATFbits.LATF3 = 0; } while(0)
#define HALL_BLK_T5_Toggle()             do { LATFbits.LATF3 = ~LATFbits.LATF3; } while(0)
#define HALL_BLK_T5_GetValue()           PORTFbits.RF3
#define HALL_BLK_T5_SetDigitalInput()    do { TRISFbits.TRISF3 = 1; } while(0)
#define HALL_BLK_T5_SetDigitalOutput()   do { TRISFbits.TRISF3 = 0; } while(0)
#define HALL_BLK_T5_SetAnalogMode()      do { anselRF3bits.anselRF3 = 1; } while(0)
#define HALL_BLK_T5_SetDigitalMode()     do { anselRF3bits.anselRF3 = 0; } while(0)

// get/set HALL_BLK_T1 aliases
#define HALL_BLK_T1_TRIS                 TRISFbits.TRISF4
#define HALL_BLK_T1_LAT                  LATFbits.LATF4
#define HALL_BLK_T1_PORT                 PORTFbits.RF4
#define HALL_BLK_T1_ANS                  anselRF4bits.anselRF4
#define HALL_BLK_T1_SetHigh()            do { LATFbits.LATF4 = 1; } while(0)
#define HALL_BLK_T1_SetLow()             do { LATFbits.LATF4 = 0; } while(0)
#define HALL_BLK_T1_Toggle()             do { LATFbits.LATF4 = ~LATFbits.LATF4; } while(0)
#define HALL_BLK_T1_GetValue()           PORTFbits.RF4
#define HALL_BLK_T1_SetDigitalInput()    do { TRISFbits.TRISF4 = 1; } while(0)
#define HALL_BLK_T1_SetDigitalOutput()   do { TRISFbits.TRISF4 = 0; } while(0)
#define HALL_BLK_T1_SetAnalogMode()      do { anselRF4bits.anselRF4 = 1; } while(0)
#define HALL_BLK_T1_SetDigitalMode()     do { anselRF4bits.anselRF4 = 0; } while(0)

// get/set HALL_BLK_T2 aliases
#define HALL_BLK_T2_TRIS                 TRISFbits.TRISF5
#define HALL_BLK_T2_LAT                  LATFbits.LATF5
#define HALL_BLK_T2_PORT                 PORTFbits.RF5
#define HALL_BLK_T2_ANS                  anselRF5bits.anselRF5
#define HALL_BLK_T2_SetHigh()            do { LATFbits.LATF5 = 1; } while(0)
#define HALL_BLK_T2_SetLow()             do { LATFbits.LATF5 = 0; } while(0)
#define HALL_BLK_T2_Toggle()             do { LATFbits.LATF5 = ~LATFbits.LATF5; } while(0)
#define HALL_BLK_T2_GetValue()           PORTFbits.RF5
#define HALL_BLK_T2_SetDigitalInput()    do { TRISFbits.TRISF5 = 1; } while(0)
#define HALL_BLK_T2_SetDigitalOutput()   do { TRISFbits.TRISF5 = 0; } while(0)
#define HALL_BLK_T2_SetAnalogMode()      do { anselRF5bits.anselRF5 = 1; } while(0)
#define HALL_BLK_T2_SetDigitalMode()     do { anselRF5bits.anselRF5 = 0; } while(0)

// get/set HALL_BLK_9B aliases
#define HALL_BLK_9B_TRIS                 TRISFbits.TRISF6
#define HALL_BLK_9B_LAT                  LATFbits.LATF6
#define HALL_BLK_9B_PORT                 PORTFbits.RF6
#define HALL_BLK_9B_ANS                  anselRF6bits.anselRF6
#define HALL_BLK_9B_SetHigh()            do { LATFbits.LATF6 = 1; } while(0)
#define HALL_BLK_9B_SetLow()             do { LATFbits.LATF6 = 0; } while(0)
#define HALL_BLK_9B_Toggle()             do { LATFbits.LATF6 = ~LATFbits.LATF6; } while(0)
#define HALL_BLK_9B_GetValue()           PORTFbits.RF6
#define HALL_BLK_9B_SetDigitalInput()    do { TRISFbits.TRISF6 = 1; } while(0)
#define HALL_BLK_9B_SetDigitalOutput()   do { TRISFbits.TRISF6 = 0; } while(0)
#define HALL_BLK_9B_SetAnalogMode()      do { anselRF6bits.anselRF6 = 1; } while(0)
#define HALL_BLK_9B_SetDigitalMode()     do { anselRF6bits.anselRF6 = 0; } while(0)

// get/set HALL_BLK_4A aliases
#define HALL_BLK_4A_TRIS                 TRISFbits.TRISF7
#define HALL_BLK_4A_LAT                  LATFbits.LATF7
#define HALL_BLK_4A_PORT                 PORTFbits.RF7
#define HALL_BLK_4A_SetHigh()            do { LATFbits.LATF7 = 1; } while(0)
#define HALL_BLK_4A_SetLow()             do { LATFbits.LATF7 = 0; } while(0)
#define HALL_BLK_4A_Toggle()             do { LATFbits.LATF7 = ~LATFbits.LATF7; } while(0)
#define HALL_BLK_4A_GetValue()           PORTFbits.RF7
#define HALL_BLK_4A_SetDigitalInput()    do { TRISFbits.TRISF7 = 1; } while(0)
#define HALL_BLK_4A_SetDigitalOutput()   do { TRISFbits.TRISF7 = 0; } while(0)

// get/set OCC_FR_STN_11 aliases
#define OCC_FR_STN_11_TRIS                 TRISGbits.TRISG0
#define OCC_FR_STN_11_LAT                  LATGbits.LATG0
#define OCC_FR_STN_11_PORT                 PORTGbits.RG0
#define OCC_FR_STN_11_SetHigh()            do { LATGbits.LATG0 = 1; } while(0)
#define OCC_FR_STN_11_SetLow()             do { LATGbits.LATG0 = 0; } while(0)
#define OCC_FR_STN_11_Toggle()             do { LATGbits.LATG0 = ~LATGbits.LATG0; } while(0)
#define OCC_FR_STN_11_GetValue()           PORTGbits.RG0
#define OCC_FR_STN_11_SetDigitalInput()    do { TRISGbits.TRISG0 = 1; } while(0)
#define OCC_FR_STN_11_SetDigitalOutput()   do { TRISGbits.TRISG0 = 0; } while(0)

// get/set OCC_FR_STN_12 aliases
#define OCC_FR_STN_12_TRIS                 TRISGbits.TRISG1
#define OCC_FR_STN_12_LAT                  LATGbits.LATG1
#define OCC_FR_STN_12_PORT                 PORTGbits.RG1
#define OCC_FR_STN_12_SetHigh()            do { LATGbits.LATG1 = 1; } while(0)
#define OCC_FR_STN_12_SetLow()             do { LATGbits.LATG1 = 0; } while(0)
#define OCC_FR_STN_12_Toggle()             do { LATGbits.LATG1 = ~LATGbits.LATG1; } while(0)
#define OCC_FR_STN_12_GetValue()           PORTGbits.RG1
#define OCC_FR_STN_12_SetDigitalInput()    do { TRISGbits.TRISG1 = 1; } while(0)
#define OCC_FR_STN_12_SetDigitalOutput()   do { TRISGbits.TRISG1 = 0; } while(0)

// get/set OCC_FR_STN_T6 aliases
#define OCC_FR_STN_T6_TRIS                 TRISGbits.TRISG2
#define OCC_FR_STN_T6_LAT                  LATGbits.LATG2
#define OCC_FR_STN_T6_PORT                 PORTGbits.RG2
#define OCC_FR_STN_T6_SetHigh()            do { LATGbits.LATG2 = 1; } while(0)
#define OCC_FR_STN_T6_SetLow()             do { LATGbits.LATG2 = 0; } while(0)
#define OCC_FR_STN_T6_Toggle()             do { LATGbits.LATG2 = ~LATGbits.LATG2; } while(0)
#define OCC_FR_STN_T6_GetValue()           PORTGbits.RG2
#define OCC_FR_STN_T6_SetDigitalInput()    do { TRISGbits.TRISG2 = 1; } while(0)
#define OCC_FR_STN_T6_SetDigitalOutput()   do { TRISGbits.TRISG2 = 0; } while(0)

// get/set OCC_FR_STN_T3 aliases
#define OCC_FR_STN_T3_TRIS                 TRISGbits.TRISG3
#define OCC_FR_STN_T3_LAT                  LATGbits.LATG3
#define OCC_FR_STN_T3_PORT                 PORTGbits.RG3
#define OCC_FR_STN_T3_SetHigh()            do { LATGbits.LATG3 = 1; } while(0)
#define OCC_FR_STN_T3_SetLow()             do { LATGbits.LATG3 = 0; } while(0)
#define OCC_FR_STN_T3_Toggle()             do { LATGbits.LATG3 = ~LATGbits.LATG3; } while(0)
#define OCC_FR_STN_T3_GetValue()           PORTGbits.RG3
#define OCC_FR_STN_T3_SetDigitalInput()    do { TRISGbits.TRISG3 = 1; } while(0)
#define OCC_FR_STN_T3_SetDigitalOutput()   do { TRISGbits.TRISG3 = 0; } while(0)

// get/set KEY_CTRL aliases
#define KEY_CTRL_TRIS                 TRISGbits.TRISG4
#define KEY_CTRL_LAT                  LATGbits.LATG4
#define KEY_CTRL_PORT                 PORTGbits.RG4
#define KEY_CTRL_SetHigh()            do { LATGbits.LATG4 = 1; } while(0)
#define KEY_CTRL_SetLow()             do { LATGbits.LATG4 = 0; } while(0)
#define KEY_CTRL_Toggle()             do { LATGbits.LATG4 = ~LATGbits.LATG4; } while(0)
#define KEY_CTRL_GetValue()           PORTGbits.RG4
#define KEY_CTRL_SetDigitalInput()    do { TRISGbits.TRISG4 = 1; } while(0)
#define KEY_CTRL_SetDigitalOutput()   do { TRISGbits.TRISG4 = 0; } while(0)

// get/set OCC_FR_BLK23B aliases
#define OCC_FR_BLK23B_TRIS                 TRISGbits.TRISG5
#define OCC_FR_BLK23B_LAT                  LATGbits.LATG5
#define OCC_FR_BLK23B_PORT                 PORTGbits.RG5
#define OCC_FR_BLK23B_SetHigh()            do { LATGbits.LATG5 = 1; } while(0)
#define OCC_FR_BLK23B_SetLow()             do { LATGbits.LATG5 = 0; } while(0)
#define OCC_FR_BLK23B_Toggle()             do { LATGbits.LATG5 = ~LATGbits.LATG5; } while(0)
#define OCC_FR_BLK23B_GetValue()           PORTGbits.RG5
#define OCC_FR_BLK23B_SetDigitalInput()    do { TRISGbits.TRISG5 = 1; } while(0)
#define OCC_FR_BLK23B_SetDigitalOutput()   do { TRISGbits.TRISG5 = 0; } while(0)

// get/set OCC_FR_BLK22B aliases
#define OCC_FR_BLK22B_TRIS                 TRISGbits.TRISG6
#define OCC_FR_BLK22B_LAT                  LATGbits.LATG6
#define OCC_FR_BLK22B_PORT                 PORTGbits.RG6
#define OCC_FR_BLK22B_SetHigh()            do { LATGbits.LATG6 = 1; } while(0)
#define OCC_FR_BLK22B_SetLow()             do { LATGbits.LATG6 = 0; } while(0)
#define OCC_FR_BLK22B_Toggle()             do { LATGbits.LATG6 = ~LATGbits.LATG6; } while(0)
#define OCC_FR_BLK22B_GetValue()           PORTGbits.RG6
#define OCC_FR_BLK22B_SetDigitalInput()    do { TRISGbits.TRISG6 = 1; } while(0)
#define OCC_FR_BLK22B_SetDigitalOutput()   do { TRISGbits.TRISG6 = 0; } while(0)

// get/set OCC_FR_BLK9B aliases
#define OCC_FR_BLK9B_TRIS                 TRISGbits.TRISG7
#define OCC_FR_BLK9B_LAT                  LATGbits.LATG7
#define OCC_FR_BLK9B_PORT                 PORTGbits.RG7
#define OCC_FR_BLK9B_SetHigh()            do { LATGbits.LATG7 = 1; } while(0)
#define OCC_FR_BLK9B_SetLow()             do { LATGbits.LATG7 = 0; } while(0)
#define OCC_FR_BLK9B_Toggle()             do { LATGbits.LATG7 = ~LATGbits.LATG7; } while(0)
#define OCC_FR_BLK9B_GetValue()           PORTGbits.RG7
#define OCC_FR_BLK9B_SetDigitalInput()    do { TRISGbits.TRISG7 = 1; } while(0)
#define OCC_FR_BLK9B_SetDigitalOutput()   do { TRISGbits.TRISG7 = 0; } while(0)

// get/set HALL_BLK_T7 aliases
#define HALL_BLK_T7_TRIS                 TRISHbits.TRISH0
#define HALL_BLK_T7_LAT                  LATHbits.LATH0
#define HALL_BLK_T7_PORT                 PORTHbits.RH0
#define HALL_BLK_T7_SetHigh()            do { LATHbits.LATH0 = 1; } while(0)
#define HALL_BLK_T7_SetLow()             do { LATHbits.LATH0 = 0; } while(0)
#define HALL_BLK_T7_Toggle()             do { LATHbits.LATH0 = ~LATHbits.LATH0; } while(0)
#define HALL_BLK_T7_GetValue()           PORTHbits.RH0
#define HALL_BLK_T7_SetDigitalInput()    do { TRISHbits.TRISH0 = 1; } while(0)
#define HALL_BLK_T7_SetDigitalOutput()   do { TRISHbits.TRISH0 = 0; } while(0)

// get/set HALL_BLK_T8 aliases
#define HALL_BLK_T8_TRIS                 TRISHbits.TRISH1
#define HALL_BLK_T8_LAT                  LATHbits.LATH1
#define HALL_BLK_T8_PORT                 PORTHbits.RH1
#define HALL_BLK_T8_SetHigh()            do { LATHbits.LATH1 = 1; } while(0)
#define HALL_BLK_T8_SetLow()             do { LATHbits.LATH1 = 0; } while(0)
#define HALL_BLK_T8_Toggle()             do { LATHbits.LATH1 = ~LATHbits.LATH1; } while(0)
#define HALL_BLK_T8_GetValue()           PORTHbits.RH1
#define HALL_BLK_T8_SetDigitalInput()    do { TRISHbits.TRISH1 = 1; } while(0)
#define HALL_BLK_T8_SetDigitalOutput()   do { TRISHbits.TRISH1 = 0; } while(0)

// get/set OCC_FR_BLK13 aliases
#define OCC_FR_BLK13_TRIS                 TRISHbits.TRISH2
#define OCC_FR_BLK13_LAT                  LATHbits.LATH2
#define OCC_FR_BLK13_PORT                 PORTHbits.RH2
#define OCC_FR_BLK13_SetHigh()            do { LATHbits.LATH2 = 1; } while(0)
#define OCC_FR_BLK13_SetLow()             do { LATHbits.LATH2 = 0; } while(0)
#define OCC_FR_BLK13_Toggle()             do { LATHbits.LATH2 = ~LATHbits.LATH2; } while(0)
#define OCC_FR_BLK13_GetValue()           PORTHbits.RH2
#define OCC_FR_BLK13_SetDigitalInput()    do { TRISHbits.TRISH2 = 1; } while(0)
#define OCC_FR_BLK13_SetDigitalOutput()   do { TRISHbits.TRISH2 = 0; } while(0)

// get/set OCC_FR_BLK4 aliases
#define OCC_FR_BLK4_TRIS                 TRISHbits.TRISH3
#define OCC_FR_BLK4_LAT                  LATHbits.LATH3
#define OCC_FR_BLK4_PORT                 PORTHbits.RH3
#define OCC_FR_BLK4_SetHigh()            do { LATHbits.LATH3 = 1; } while(0)
#define OCC_FR_BLK4_SetLow()             do { LATHbits.LATH3 = 0; } while(0)
#define OCC_FR_BLK4_Toggle()             do { LATHbits.LATH3 = ~LATHbits.LATH3; } while(0)
#define OCC_FR_BLK4_GetValue()           PORTHbits.RH3
#define OCC_FR_BLK4_SetDigitalInput()    do { TRISHbits.TRISH3 = 1; } while(0)
#define OCC_FR_BLK4_SetDigitalOutput()   do { TRISHbits.TRISH3 = 0; } while(0)

// get/set OCC_FR_STN_1 aliases
#define OCC_FR_STN_1_TRIS                 TRISHbits.TRISH4
#define OCC_FR_STN_1_LAT                  LATHbits.LATH4
#define OCC_FR_STN_1_PORT                 PORTHbits.RH4
#define OCC_FR_STN_1_ANS                  anselRH4bits.anselRH4
#define OCC_FR_STN_1_SetHigh()            do { LATHbits.LATH4 = 1; } while(0)
#define OCC_FR_STN_1_SetLow()             do { LATHbits.LATH4 = 0; } while(0)
#define OCC_FR_STN_1_Toggle()             do { LATHbits.LATH4 = ~LATHbits.LATH4; } while(0)
#define OCC_FR_STN_1_GetValue()           PORTHbits.RH4
#define OCC_FR_STN_1_SetDigitalInput()    do { TRISHbits.TRISH4 = 1; } while(0)
#define OCC_FR_STN_1_SetDigitalOutput()   do { TRISHbits.TRISH4 = 0; } while(0)
#define OCC_FR_STN_1_SetAnalogMode()      do { anselRH4bits.anselRH4 = 1; } while(0)
#define OCC_FR_STN_1_SetDigitalMode()     do { anselRH4bits.anselRH4 = 0; } while(0)

// get/set OCC_FR_STN_2 aliases
#define OCC_FR_STN_2_TRIS                 TRISHbits.TRISH5
#define OCC_FR_STN_2_LAT                  LATHbits.LATH5
#define OCC_FR_STN_2_PORT                 PORTHbits.RH5
#define OCC_FR_STN_2_ANS                  anselRH5bits.anselRH5
#define OCC_FR_STN_2_SetHigh()            do { LATHbits.LATH5 = 1; } while(0)
#define OCC_FR_STN_2_SetLow()             do { LATHbits.LATH5 = 0; } while(0)
#define OCC_FR_STN_2_Toggle()             do { LATHbits.LATH5 = ~LATHbits.LATH5; } while(0)
#define OCC_FR_STN_2_GetValue()           PORTHbits.RH5
#define OCC_FR_STN_2_SetDigitalInput()    do { TRISHbits.TRISH5 = 1; } while(0)
#define OCC_FR_STN_2_SetDigitalOutput()   do { TRISHbits.TRISH5 = 0; } while(0)
#define OCC_FR_STN_2_SetAnalogMode()      do { anselRH5bits.anselRH5 = 1; } while(0)
#define OCC_FR_STN_2_SetDigitalMode()     do { anselRH5bits.anselRH5 = 0; } while(0)

// get/set OCC_FR_STN_3 aliases
#define OCC_FR_STN_3_TRIS                 TRISHbits.TRISH6
#define OCC_FR_STN_3_LAT                  LATHbits.LATH6
#define OCC_FR_STN_3_PORT                 PORTHbits.RH6
#define OCC_FR_STN_3_ANS                  anselRH6bits.anselRH6
#define OCC_FR_STN_3_SetHigh()            do { LATHbits.LATH6 = 1; } while(0)
#define OCC_FR_STN_3_SetLow()             do { LATHbits.LATH6 = 0; } while(0)
#define OCC_FR_STN_3_Toggle()             do { LATHbits.LATH6 = ~LATHbits.LATH6; } while(0)
#define OCC_FR_STN_3_GetValue()           PORTHbits.RH6
#define OCC_FR_STN_3_SetDigitalInput()    do { TRISHbits.TRISH6 = 1; } while(0)
#define OCC_FR_STN_3_SetDigitalOutput()   do { TRISHbits.TRISH6 = 0; } while(0)
#define OCC_FR_STN_3_SetAnalogMode()      do { anselRH6bits.anselRH6 = 1; } while(0)
#define OCC_FR_STN_3_SetDigitalMode()     do { anselRH6bits.anselRH6 = 0; } while(0)

// get/set OCC_FR_STN_10 aliases
#define OCC_FR_STN_10_TRIS                 TRISHbits.TRISH7
#define OCC_FR_STN_10_LAT                  LATHbits.LATH7
#define OCC_FR_STN_10_PORT                 PORTHbits.RH7
#define OCC_FR_STN_10_ANS                  anselRH7bits.anselRH7
#define OCC_FR_STN_10_SetHigh()            do { LATHbits.LATH7 = 1; } while(0)
#define OCC_FR_STN_10_SetLow()             do { LATHbits.LATH7 = 0; } while(0)
#define OCC_FR_STN_10_Toggle()             do { LATHbits.LATH7 = ~LATHbits.LATH7; } while(0)
#define OCC_FR_STN_10_GetValue()           PORTHbits.RH7
#define OCC_FR_STN_10_SetDigitalInput()    do { TRISHbits.TRISH7 = 1; } while(0)
#define OCC_FR_STN_10_SetDigitalOutput()   do { TRISHbits.TRISH7 = 0; } while(0)
#define OCC_FR_STN_10_SetAnalogMode()      do { anselRH7bits.anselRH7 = 1; } while(0)
#define OCC_FR_STN_10_SetDigitalMode()     do { anselRH7bits.anselRH7 = 0; } while(0)

// get/set BLK_SIG_11B_GR aliases
#define BLK_SIG_11B_GR_TRIS                 TRISJbits.TRISJ0
#define BLK_SIG_11B_GR_LAT                  LATJbits.LATJ0
#define BLK_SIG_11B_GR_PORT                 PORTJbits.RJ0
#define BLK_SIG_11B_GR_SetHigh()            do { LATJbits.LATJ0 = 1; } while(0)
#define BLK_SIG_11B_GR_SetLow()             do { LATJbits.LATJ0 = 0; } while(0)
#define BLK_SIG_11B_GR_Toggle()             do { LATJbits.LATJ0 = ~LATJbits.LATJ0; } while(0)
#define BLK_SIG_11B_GR_GetValue()           PORTJbits.RJ0
#define BLK_SIG_11B_GR_SetDigitalInput()    do { TRISJbits.TRISJ0 = 1; } while(0)
#define BLK_SIG_11B_GR_SetDigitalOutput()   do { TRISJbits.TRISJ0 = 0; } while(0)

// get/set BLK_SIG_11B_RD aliases
#define BLK_SIG_11B_RD_TRIS                 TRISJbits.TRISJ1
#define BLK_SIG_11B_RD_LAT                  LATJbits.LATJ1
#define BLK_SIG_11B_RD_PORT                 PORTJbits.RJ1
#define BLK_SIG_11B_RD_SetHigh()            do { LATJbits.LATJ1 = 1; } while(0)
#define BLK_SIG_11B_RD_SetLow()             do { LATJbits.LATJ1 = 0; } while(0)
#define BLK_SIG_11B_RD_Toggle()             do { LATJbits.LATJ1 = ~LATJbits.LATJ1; } while(0)
#define BLK_SIG_11B_RD_GetValue()           PORTJbits.RJ1
#define BLK_SIG_11B_RD_SetDigitalInput()    do { TRISJbits.TRISJ1 = 1; } while(0)
#define BLK_SIG_11B_RD_SetDigitalOutput()   do { TRISJbits.TRISJ1 = 0; } while(0)

// get/set BLK_SIG_12B_GR aliases
#define BLK_SIG_12B_GR_TRIS                 TRISJbits.TRISJ2
#define BLK_SIG_12B_GR_LAT                  LATJbits.LATJ2
#define BLK_SIG_12B_GR_PORT                 PORTJbits.RJ2
#define BLK_SIG_12B_GR_SetHigh()            do { LATJbits.LATJ2 = 1; } while(0)
#define BLK_SIG_12B_GR_SetLow()             do { LATJbits.LATJ2 = 0; } while(0)
#define BLK_SIG_12B_GR_Toggle()             do { LATJbits.LATJ2 = ~LATJbits.LATJ2; } while(0)
#define BLK_SIG_12B_GR_GetValue()           PORTJbits.RJ2
#define BLK_SIG_12B_GR_SetDigitalInput()    do { TRISJbits.TRISJ2 = 1; } while(0)
#define BLK_SIG_12B_GR_SetDigitalOutput()   do { TRISJbits.TRISJ2 = 0; } while(0)

// get/set BLK_SIG_12B_RD aliases
#define BLK_SIG_12B_RD_TRIS                 TRISJbits.TRISJ3
#define BLK_SIG_12B_RD_LAT                  LATJbits.LATJ3
#define BLK_SIG_12B_RD_PORT                 PORTJbits.RJ3
#define BLK_SIG_12B_RD_SetHigh()            do { LATJbits.LATJ3 = 1; } while(0)
#define BLK_SIG_12B_RD_SetLow()             do { LATJbits.LATJ3 = 0; } while(0)
#define BLK_SIG_12B_RD_Toggle()             do { LATJbits.LATJ3 = ~LATJbits.LATJ3; } while(0)
#define BLK_SIG_12B_RD_GetValue()           PORTJbits.RJ3
#define BLK_SIG_12B_RD_SetDigitalInput()    do { TRISJbits.TRISJ3 = 1; } while(0)
#define BLK_SIG_12B_RD_SetDigitalOutput()   do { TRISJbits.TRISJ3 = 0; } while(0)

// get/set OCC_TO_STN_1 aliases
#define OCC_TO_STN_1_TRIS                 TRISJbits.TRISJ4
#define OCC_TO_STN_1_LAT                  LATJbits.LATJ4
#define OCC_TO_STN_1_PORT                 PORTJbits.RJ4
#define OCC_TO_STN_1_SetHigh()            do { LATJbits.LATJ4 = 1; } while(0)
#define OCC_TO_STN_1_SetLow()             do { LATJbits.LATJ4 = 0; } while(0)
#define OCC_TO_STN_1_Toggle()             do { LATJbits.LATJ4 = ~LATJbits.LATJ4; } while(0)
#define OCC_TO_STN_1_GetValue()           PORTJbits.RJ4
#define OCC_TO_STN_1_SetDigitalInput()    do { TRISJbits.TRISJ4 = 1; } while(0)
#define OCC_TO_STN_1_SetDigitalOutput()   do { TRISJbits.TRISJ4 = 0; } while(0)

// get/set OCC_TO_STN_2 aliases
#define OCC_TO_STN_2_TRIS                 TRISJbits.TRISJ5
#define OCC_TO_STN_2_LAT                  LATJbits.LATJ5
#define OCC_TO_STN_2_PORT                 PORTJbits.RJ5
#define OCC_TO_STN_2_SetHigh()            do { LATJbits.LATJ5 = 1; } while(0)
#define OCC_TO_STN_2_SetLow()             do { LATJbits.LATJ5 = 0; } while(0)
#define OCC_TO_STN_2_Toggle()             do { LATJbits.LATJ5 = ~LATJbits.LATJ5; } while(0)
#define OCC_TO_STN_2_GetValue()           PORTJbits.RJ5
#define OCC_TO_STN_2_SetDigitalInput()    do { TRISJbits.TRISJ5 = 1; } while(0)
#define OCC_TO_STN_2_SetDigitalOutput()   do { TRISJbits.TRISJ5 = 0; } while(0)

// get/set OCC_TO_STN_3 aliases
#define OCC_TO_STN_3_TRIS                 TRISJbits.TRISJ6
#define OCC_TO_STN_3_LAT                  LATJbits.LATJ6
#define OCC_TO_STN_3_PORT                 PORTJbits.RJ6
#define OCC_TO_STN_3_SetHigh()            do { LATJbits.LATJ6 = 1; } while(0)
#define OCC_TO_STN_3_SetLow()             do { LATJbits.LATJ6 = 0; } while(0)
#define OCC_TO_STN_3_Toggle()             do { LATJbits.LATJ6 = ~LATJbits.LATJ6; } while(0)
#define OCC_TO_STN_3_GetValue()           PORTJbits.RJ6
#define OCC_TO_STN_3_SetDigitalInput()    do { TRISJbits.TRISJ6 = 1; } while(0)
#define OCC_TO_STN_3_SetDigitalOutput()   do { TRISJbits.TRISJ6 = 0; } while(0)

// get/set OCC_TO_STN_10 aliases
#define OCC_TO_STN_10_TRIS                 TRISJbits.TRISJ7
#define OCC_TO_STN_10_LAT                  LATJbits.LATJ7
#define OCC_TO_STN_10_PORT                 PORTJbits.RJ7
#define OCC_TO_STN_10_SetHigh()            do { LATJbits.LATJ7 = 1; } while(0)
#define OCC_TO_STN_10_SetLow()             do { LATJbits.LATJ7 = 0; } while(0)
#define OCC_TO_STN_10_Toggle()             do { LATJbits.LATJ7 = ~LATJbits.LATJ7; } while(0)
#define OCC_TO_STN_10_GetValue()           PORTJbits.RJ7
#define OCC_TO_STN_10_SetDigitalInput()    do { TRISJbits.TRISJ7 = 1; } while(0)
#define OCC_TO_STN_10_SetDigitalOutput()   do { TRISJbits.TRISJ7 = 0; } while(0)


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