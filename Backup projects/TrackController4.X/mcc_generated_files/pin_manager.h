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
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.65.2
        Device            :  PIC18F97J60
        Version           :  2.0
    The generated drivers are tested against the following:
        Compiler          :  XC8 1.45
        MPLAB             :  MPLAB X 4.15

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

#define INPUT   1
#define OUTPUT  0

#define HIGH    1
#define LOW     0

#define ANALOG      1
#define DIGITAL     0

#define PULL_UP_ENABLED      1
#define PULL_UP_DISABLED     0

// get/set LEDA aliases
#define LEDA_TRIS                 TRISAbits.TRISA0
#define LEDA_LAT                  LATAbits.LATA0
#define LEDA_PORT                 PORTAbits.RA0
#define LEDA_ANS                  anselRA0bits.anselRA0
#define LEDA_SetHigh()            do { LATAbits.LATA0 = 1; } while(0)
#define LEDA_SetLow()             do { LATAbits.LATA0 = 0; } while(0)
#define LEDA_Toggle()             do { LATAbits.LATA0 = ~LATAbits.LATA0; } while(0)
#define LEDA_GetValue()           PORTAbits.RA0
#define LEDA_SetDigitalInput()    do { TRISAbits.TRISA0 = 1; } while(0)
#define LEDA_SetDigitalOutput()   do { TRISAbits.TRISA0 = 0; } while(0)
#define LEDA_SetAnalogMode()      do { anselRA0bits.anselRA0 = 1; } while(0)
#define LEDA_SetDigitalMode()     do { anselRA0bits.anselRA0 = 0; } while(0)

// get/set LEDB aliases
#define LEDB_TRIS                 TRISAbits.TRISA1
#define LEDB_LAT                  LATAbits.LATA1
#define LEDB_PORT                 PORTAbits.RA1
#define LEDB_ANS                  anselRA1bits.anselRA1
#define LEDB_SetHigh()            do { LATAbits.LATA1 = 1; } while(0)
#define LEDB_SetLow()             do { LATAbits.LATA1 = 0; } while(0)
#define LEDB_Toggle()             do { LATAbits.LATA1 = ~LATAbits.LATA1; } while(0)
#define LEDB_GetValue()           PORTAbits.RA1
#define LEDB_SetDigitalInput()    do { TRISAbits.TRISA1 = 1; } while(0)
#define LEDB_SetDigitalOutput()   do { TRISAbits.TRISA1 = 0; } while(0)
#define LEDB_SetAnalogMode()      do { anselRA1bits.anselRA1 = 1; } while(0)
#define LEDB_SetDigitalMode()     do { anselRA1bits.anselRA1 = 0; } while(0)

// get/set SS1_Check aliases
#define SS1_Check_TRIS                 TRISBbits.TRISB0
#define SS1_Check_LAT                  LATBbits.LATB0
#define SS1_Check_PORT                 PORTBbits.RB0
#define SS1_Check_SetHigh()            do { LATBbits.LATB0 = 1; } while(0)
#define SS1_Check_SetLow()             do { LATBbits.LATB0 = 0; } while(0)
#define SS1_Check_Toggle()             do { LATBbits.LATB0 = ~LATBbits.LATB0; } while(0)
#define SS1_Check_GetValue()           PORTBbits.RB0
#define SS1_Check_SetDigitalInput()    do { TRISBbits.TRISB0 = 1; } while(0)
#define SS1_Check_SetDigitalOutput()   do { TRISBbits.TRISB0 = 0; } while(0)

// get/set Read_Check aliases
#define Read_Check_TRIS                 TRISBbits.TRISB1
#define Read_Check_LAT                  LATBbits.LATB1
#define Read_Check_PORT                 PORTBbits.RB1
#define Read_Check_SetHigh()            do { LATBbits.LATB1 = 1; } while(0)
#define Read_Check_SetLow()             do { LATBbits.LATB1 = 0; } while(0)
#define Read_Check_Toggle()             do { LATBbits.LATB1 = ~LATBbits.LATB1; } while(0)
#define Read_Check_GetValue()           PORTBbits.RB1
#define Read_Check_SetDigitalInput()    do { TRISBbits.TRISB1 = 1; } while(0)
#define Read_Check_SetDigitalOutput()   do { TRISBbits.TRISB1 = 0; } while(0)

// get/set LED1 aliases
#define LED1_TRIS                 TRISBbits.TRISB2
#define LED1_LAT                  LATBbits.LATB2
#define LED1_PORT                 PORTBbits.RB2
#define LED1_SetHigh()            do { LATBbits.LATB2 = 1; } while(0)
#define LED1_SetLow()             do { LATBbits.LATB2 = 0; } while(0)
#define LED1_Toggle()             do { LATBbits.LATB2 = ~LATBbits.LATB2; } while(0)
#define LED1_GetValue()           PORTBbits.RB2
#define LED1_SetDigitalInput()    do { TRISBbits.TRISB2 = 1; } while(0)
#define LED1_SetDigitalOutput()   do { TRISBbits.TRISB2 = 0; } while(0)

// get/set LED2 aliases
#define LED2_TRIS                 TRISBbits.TRISB3
#define LED2_LAT                  LATBbits.LATB3
#define LED2_PORT                 PORTBbits.RB3
#define LED2_SetHigh()            do { LATBbits.LATB3 = 1; } while(0)
#define LED2_SetLow()             do { LATBbits.LATB3 = 0; } while(0)
#define LED2_Toggle()             do { LATBbits.LATB3 = ~LATBbits.LATB3; } while(0)
#define LED2_GetValue()           PORTBbits.RB3
#define LED2_SetDigitalInput()    do { TRISBbits.TRISB3 = 1; } while(0)
#define LED2_SetDigitalOutput()   do { TRISBbits.TRISB3 = 0; } while(0)

// get/set LED3 aliases
#define LED3_TRIS                 TRISBbits.TRISB4
#define LED3_LAT                  LATBbits.LATB4
#define LED3_PORT                 PORTBbits.RB4
#define LED3_SetHigh()            do { LATBbits.LATB4 = 1; } while(0)
#define LED3_SetLow()             do { LATBbits.LATB4 = 0; } while(0)
#define LED3_Toggle()             do { LATBbits.LATB4 = ~LATBbits.LATB4; } while(0)
#define LED3_GetValue()           PORTBbits.RB4
#define LED3_SetDigitalInput()    do { TRISBbits.TRISB4 = 1; } while(0)
#define LED3_SetDigitalOutput()   do { TRISBbits.TRISB4 = 0; } while(0)

// get/set SEND_UDP aliases
#define SEND_UDP_TRIS                 TRISBbits.TRISB7
#define SEND_UDP_LAT                  LATBbits.LATB7
#define SEND_UDP_PORT                 PORTBbits.RB7
#define SEND_UDP_SetHigh()            do { LATBbits.LATB7 = 1; } while(0)
#define SEND_UDP_SetLow()             do { LATBbits.LATB7 = 0; } while(0)
#define SEND_UDP_Toggle()             do { LATBbits.LATB7 = ~LATBbits.LATB7; } while(0)
#define SEND_UDP_GetValue()           PORTBbits.RB7
#define SEND_UDP_SetDigitalInput()    do { TRISBbits.TRISB7 = 1; } while(0)
#define SEND_UDP_SetDigitalOutput()   do { TRISBbits.TRISB7 = 0; } while(0)

// get/set RC6 procedures
#define RC6_SetHigh()               do { LATCbits.LATC6 = 1; } while(0)
#define RC6_SetLow()                do { LATCbits.LATC6 = 0; } while(0)
#define RC6_Toggle()                do { LATCbits.LATC6 = ~LATCbits.LATC6; } while(0)
#define RC6_GetValue()              PORTCbits.RC6
#define RC6_SetDigitalInput()       do { TRISCbits.TRISC6 = 1; } while(0)
#define RC6_SetDigitalOutput()      do { TRISCbits.TRISC6 = 0; } while(0)

// get/set ModbusReset aliases
#define ModbusReset_TRIS                 TRISDbits.TRISD3
#define ModbusReset_LAT                  LATDbits.LATD3
#define ModbusReset_PORT                 PORTDbits.RD3
#define ModbusReset_SetHigh()            do { LATDbits.LATD3 = 1; } while(0)
#define ModbusReset_SetLow()             do { LATDbits.LATD3 = 0; } while(0)
#define ModbusReset_Toggle()             do { LATDbits.LATD3 = ~LATDbits.LATD3; } while(0)
#define ModbusReset_GetValue()           PORTDbits.RD3
#define ModbusReset_SetDigitalInput()    do { TRISDbits.TRISD3 = 1; } while(0)
#define ModbusReset_SetDigitalOutput()   do { TRISDbits.TRISD3 = 0; } while(0)

// get/set SDO2 aliases
#define SDO2_TRIS                 TRISDbits.TRISD4
#define SDO2_LAT                  LATDbits.LATD4
#define SDO2_PORT                 PORTDbits.RD4
#define SDO2_SetHigh()            do { LATDbits.LATD4 = 1; } while(0)
#define SDO2_SetLow()             do { LATDbits.LATD4 = 0; } while(0)
#define SDO2_Toggle()             do { LATDbits.LATD4 = ~LATDbits.LATD4; } while(0)
#define SDO2_GetValue()           PORTDbits.RD4
#define SDO2_SetDigitalInput()    do { TRISDbits.TRISD4 = 1; } while(0)
#define SDO2_SetDigitalOutput()   do { TRISDbits.TRISD4 = 0; } while(0)

// get/set SDI2 aliases
#define SDI2_TRIS                 TRISDbits.TRISD5
#define SDI2_LAT                  LATDbits.LATD5
#define SDI2_PORT                 PORTDbits.RD5
#define SDI2_SetHigh()            do { LATDbits.LATD5 = 1; } while(0)
#define SDI2_SetLow()             do { LATDbits.LATD5 = 0; } while(0)
#define SDI2_Toggle()             do { LATDbits.LATD5 = ~LATDbits.LATD5; } while(0)
#define SDI2_GetValue()           PORTDbits.RD5
#define SDI2_SetDigitalInput()    do { TRISDbits.TRISD5 = 1; } while(0)
#define SDI2_SetDigitalOutput()   do { TRISDbits.TRISD5 = 0; } while(0)

// get/set SCK2 aliases
#define SCK2_TRIS                 TRISDbits.TRISD6
#define SCK2_LAT                  LATDbits.LATD6
#define SCK2_PORT                 PORTDbits.RD6
#define SCK2_SetHigh()            do { LATDbits.LATD6 = 1; } while(0)
#define SCK2_SetLow()             do { LATDbits.LATD6 = 0; } while(0)
#define SCK2_Toggle()             do { LATDbits.LATD6 = ~LATDbits.LATD6; } while(0)
#define SCK2_GetValue()           PORTDbits.RD6
#define SCK2_SetDigitalInput()    do { TRISDbits.TRISD6 = 1; } while(0)
#define SCK2_SetDigitalOutput()   do { TRISDbits.TRISD6 = 0; } while(0)

// get/set SS2 aliases
#define SS2_TRIS                 TRISDbits.TRISD7
#define SS2_LAT                  LATDbits.LATD7
#define SS2_PORT                 PORTDbits.RD7
#define SS2_SetHigh()            do { LATDbits.LATD7 = 1; } while(0)
#define SS2_SetLow()             do { LATDbits.LATD7 = 0; } while(0)
#define SS2_Toggle()             do { LATDbits.LATD7 = ~LATDbits.LATD7; } while(0)
#define SS2_GetValue()           PORTDbits.RD7
#define SS2_SetDigitalInput()    do { TRISDbits.TRISD7 = 1; } while(0)
#define SS2_SetDigitalOutput()   do { TRISDbits.TRISD7 = 0; } while(0)

// get/set IO_RE0 aliases
#define IO_RE0_TRIS                 TRISEbits.TRISE0
#define IO_RE0_LAT                  LATEbits.LATE0
#define IO_RE0_PORT                 PORTEbits.RE0
#define IO_RE0_SetHigh()            do { LATEbits.LATE0 = 1; } while(0)
#define IO_RE0_SetLow()             do { LATEbits.LATE0 = 0; } while(0)
#define IO_RE0_Toggle()             do { LATEbits.LATE0 = ~LATEbits.LATE0; } while(0)
#define IO_RE0_GetValue()           PORTEbits.RE0
#define IO_RE0_SetDigitalInput()    do { TRISEbits.TRISE0 = 1; } while(0)
#define IO_RE0_SetDigitalOutput()   do { TRISEbits.TRISE0 = 0; } while(0)

// get/set IO_RE1 aliases
#define IO_RE1_TRIS                 TRISEbits.TRISE1
#define IO_RE1_LAT                  LATEbits.LATE1
#define IO_RE1_PORT                 PORTEbits.RE1
#define IO_RE1_SetHigh()            do { LATEbits.LATE1 = 1; } while(0)
#define IO_RE1_SetLow()             do { LATEbits.LATE1 = 0; } while(0)
#define IO_RE1_Toggle()             do { LATEbits.LATE1 = ~LATEbits.LATE1; } while(0)
#define IO_RE1_GetValue()           PORTEbits.RE1
#define IO_RE1_SetDigitalInput()    do { TRISEbits.TRISE1 = 1; } while(0)
#define IO_RE1_SetDigitalOutput()   do { TRISEbits.TRISE1 = 0; } while(0)

// get/set IO_RE2 aliases
#define IO_RE2_TRIS                 TRISEbits.TRISE2
#define IO_RE2_LAT                  LATEbits.LATE2
#define IO_RE2_PORT                 PORTEbits.RE2
#define IO_RE2_SetHigh()            do { LATEbits.LATE2 = 1; } while(0)
#define IO_RE2_SetLow()             do { LATEbits.LATE2 = 0; } while(0)
#define IO_RE2_Toggle()             do { LATEbits.LATE2 = ~LATEbits.LATE2; } while(0)
#define IO_RE2_GetValue()           PORTEbits.RE2
#define IO_RE2_SetDigitalInput()    do { TRISEbits.TRISE2 = 1; } while(0)
#define IO_RE2_SetDigitalOutput()   do { TRISEbits.TRISE2 = 0; } while(0)

// get/set IO_RE3 aliases
#define IO_RE3_TRIS                 TRISEbits.TRISE3
#define IO_RE3_LAT                  LATEbits.LATE3
#define IO_RE3_PORT                 PORTEbits.RE3
#define IO_RE3_SetHigh()            do { LATEbits.LATE3 = 1; } while(0)
#define IO_RE3_SetLow()             do { LATEbits.LATE3 = 0; } while(0)
#define IO_RE3_Toggle()             do { LATEbits.LATE3 = ~LATEbits.LATE3; } while(0)
#define IO_RE3_GetValue()           PORTEbits.RE3
#define IO_RE3_SetDigitalInput()    do { TRISEbits.TRISE3 = 1; } while(0)
#define IO_RE3_SetDigitalOutput()   do { TRISEbits.TRISE3 = 0; } while(0)

// get/set IO_RE4 aliases
#define IO_RE4_TRIS                 TRISEbits.TRISE4
#define IO_RE4_LAT                  LATEbits.LATE4
#define IO_RE4_PORT                 PORTEbits.RE4
#define IO_RE4_SetHigh()            do { LATEbits.LATE4 = 1; } while(0)
#define IO_RE4_SetLow()             do { LATEbits.LATE4 = 0; } while(0)
#define IO_RE4_Toggle()             do { LATEbits.LATE4 = ~LATEbits.LATE4; } while(0)
#define IO_RE4_GetValue()           PORTEbits.RE4
#define IO_RE4_SetDigitalInput()    do { TRISEbits.TRISE4 = 1; } while(0)
#define IO_RE4_SetDigitalOutput()   do { TRISEbits.TRISE4 = 0; } while(0)

// get/set IO_RE5 aliases
#define IO_RE5_TRIS                 TRISEbits.TRISE5
#define IO_RE5_LAT                  LATEbits.LATE5
#define IO_RE5_PORT                 PORTEbits.RE5
#define IO_RE5_SetHigh()            do { LATEbits.LATE5 = 1; } while(0)
#define IO_RE5_SetLow()             do { LATEbits.LATE5 = 0; } while(0)
#define IO_RE5_Toggle()             do { LATEbits.LATE5 = ~LATEbits.LATE5; } while(0)
#define IO_RE5_GetValue()           PORTEbits.RE5
#define IO_RE5_SetDigitalInput()    do { TRISEbits.TRISE5 = 1; } while(0)
#define IO_RE5_SetDigitalOutput()   do { TRISEbits.TRISE5 = 0; } while(0)

// get/set IO_RE6 aliases
#define IO_RE6_TRIS                 TRISEbits.TRISE6
#define IO_RE6_LAT                  LATEbits.LATE6
#define IO_RE6_PORT                 PORTEbits.RE6
#define IO_RE6_SetHigh()            do { LATEbits.LATE6 = 1; } while(0)
#define IO_RE6_SetLow()             do { LATEbits.LATE6 = 0; } while(0)
#define IO_RE6_Toggle()             do { LATEbits.LATE6 = ~LATEbits.LATE6; } while(0)
#define IO_RE6_GetValue()           PORTEbits.RE6
#define IO_RE6_SetDigitalInput()    do { TRISEbits.TRISE6 = 1; } while(0)
#define IO_RE6_SetDigitalOutput()   do { TRISEbits.TRISE6 = 0; } while(0)

// get/set IO_RE7 aliases
#define IO_RE7_TRIS                 TRISEbits.TRISE7
#define IO_RE7_LAT                  LATEbits.LATE7
#define IO_RE7_PORT                 PORTEbits.RE7
#define IO_RE7_SetHigh()            do { LATEbits.LATE7 = 1; } while(0)
#define IO_RE7_SetLow()             do { LATEbits.LATE7 = 0; } while(0)
#define IO_RE7_Toggle()             do { LATEbits.LATE7 = ~LATEbits.LATE7; } while(0)
#define IO_RE7_GetValue()           PORTEbits.RE7
#define IO_RE7_SetDigitalInput()    do { TRISEbits.TRISE7 = 1; } while(0)
#define IO_RE7_SetDigitalOutput()   do { TRISEbits.TRISE7 = 0; } while(0)

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