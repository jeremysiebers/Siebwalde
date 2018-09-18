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

// get/set RC6 procedures
#define RC6_SetHigh()               do { LATCbits.LATC6 = 1; } while(0)
#define RC6_SetLow()                do { LATCbits.LATC6 = 0; } while(0)
#define RC6_Toggle()                do { LATCbits.LATC6 = ~LATCbits.LATC6; } while(0)
#define RC6_GetValue()              PORTCbits.RC6
#define RC6_SetDigitalInput()       do { TRISCbits.TRISC6 = 1; } while(0)
#define RC6_SetDigitalOutput()      do { TRISCbits.TRISC6 = 0; } while(0)

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

// get/set ModbusReset aliases
#define ModbusReset_TRIS                 TRISGbits.TRISG3
#define ModbusReset_LAT                  LATGbits.LATG3
#define ModbusReset_PORT                 PORTGbits.RG3
#define ModbusReset_SetHigh()            do { LATGbits.LATG3 = 1; } while(0)
#define ModbusReset_SetLow()             do { LATGbits.LATG3 = 0; } while(0)
#define ModbusReset_Toggle()             do { LATGbits.LATG3 = ~LATGbits.LATG3; } while(0)
#define ModbusReset_GetValue()           PORTGbits.RG3
#define ModbusReset_SetDigitalInput()    do { TRISGbits.TRISG3 = 1; } while(0)
#define ModbusReset_SetDigitalOutput()   do { TRISGbits.TRISG3 = 0; } while(0)


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