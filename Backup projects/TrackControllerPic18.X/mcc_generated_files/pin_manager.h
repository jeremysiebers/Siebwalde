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

// get/set An_In1 aliases
#define An_In1_TRIS                 TRISAbits.TRISA2
#define An_In1_LAT                  LATAbits.LATA2
#define An_In1_PORT                 PORTAbits.RA2
#define An_In1_ANS                  anselRA2bits.anselRA2
#define An_In1_SetHigh()            do { LATAbits.LATA2 = 1; } while(0)
#define An_In1_SetLow()             do { LATAbits.LATA2 = 0; } while(0)
#define An_In1_Toggle()             do { LATAbits.LATA2 = ~LATAbits.LATA2; } while(0)
#define An_In1_GetValue()           PORTAbits.RA2
#define An_In1_SetDigitalInput()    do { TRISAbits.TRISA2 = 1; } while(0)
#define An_In1_SetDigitalOutput()   do { TRISAbits.TRISA2 = 0; } while(0)
#define An_In1_SetAnalogMode()      do { anselRA2bits.anselRA2 = 1; } while(0)
#define An_In1_SetDigitalMode()     do { anselRA2bits.anselRA2 = 0; } while(0)

// get/set An_In2 aliases
#define An_In2_TRIS                 TRISAbits.TRISA3
#define An_In2_LAT                  LATAbits.LATA3
#define An_In2_PORT                 PORTAbits.RA3
#define An_In2_ANS                  anselRA3bits.anselRA3
#define An_In2_SetHigh()            do { LATAbits.LATA3 = 1; } while(0)
#define An_In2_SetLow()             do { LATAbits.LATA3 = 0; } while(0)
#define An_In2_Toggle()             do { LATAbits.LATA3 = ~LATAbits.LATA3; } while(0)
#define An_In2_GetValue()           PORTAbits.RA3
#define An_In2_SetDigitalInput()    do { TRISAbits.TRISA3 = 1; } while(0)
#define An_In2_SetDigitalOutput()   do { TRISAbits.TRISA3 = 0; } while(0)
#define An_In2_SetAnalogMode()      do { anselRA3bits.anselRA3 = 1; } while(0)
#define An_In2_SetDigitalMode()     do { anselRA3bits.anselRA3 = 0; } while(0)

// get/set Led1 aliases
#define Led1_TRIS                 TRISAbits.TRISA4
#define Led1_LAT                  LATAbits.LATA4
#define Led1_PORT                 PORTAbits.RA4
#define Led1_SetHigh()            do { LATAbits.LATA4 = 1; } while(0)
#define Led1_SetLow()             do { LATAbits.LATA4 = 0; } while(0)
#define Led1_Toggle()             do { LATAbits.LATA4 = ~LATAbits.LATA4; } while(0)
#define Led1_GetValue()           PORTAbits.RA4
#define Led1_SetDigitalInput()    do { TRISAbits.TRISA4 = 1; } while(0)
#define Led1_SetDigitalOutput()   do { TRISAbits.TRISA4 = 0; } while(0)

// get/set Led2 aliases
#define Led2_TRIS                 TRISAbits.TRISA5
#define Led2_LAT                  LATAbits.LATA5
#define Led2_PORT                 PORTAbits.RA5
#define Led2_ANS                  anselRA5bits.anselRA5
#define Led2_SetHigh()            do { LATAbits.LATA5 = 1; } while(0)
#define Led2_SetLow()             do { LATAbits.LATA5 = 0; } while(0)
#define Led2_Toggle()             do { LATAbits.LATA5 = ~LATAbits.LATA5; } while(0)
#define Led2_GetValue()           PORTAbits.RA5
#define Led2_SetDigitalInput()    do { TRISAbits.TRISA5 = 1; } while(0)
#define Led2_SetDigitalOutput()   do { TRISAbits.TRISA5 = 0; } while(0)
#define Led2_SetAnalogMode()      do { anselRA5bits.anselRA5 = 1; } while(0)
#define Led2_SetDigitalMode()     do { anselRA5bits.anselRA5 = 0; } while(0)

// get/set Spare_Swt_Top aliases
#define Spare_Swt_Top_TRIS                 TRISBbits.TRISB0
#define Spare_Swt_Top_LAT                  LATBbits.LATB0
#define Spare_Swt_Top_PORT                 PORTBbits.RB0
#define Spare_Swt_Top_SetHigh()            do { LATBbits.LATB0 = 1; } while(0)
#define Spare_Swt_Top_SetLow()             do { LATBbits.LATB0 = 0; } while(0)
#define Spare_Swt_Top_Toggle()             do { LATBbits.LATB0 = ~LATBbits.LATB0; } while(0)
#define Spare_Swt_Top_GetValue()           PORTBbits.RB0
#define Spare_Swt_Top_SetDigitalInput()    do { TRISBbits.TRISB0 = 1; } while(0)
#define Spare_Swt_Top_SetDigitalOutput()   do { TRISBbits.TRISB0 = 0; } while(0)

// get/set Spare_Swt_Bot aliases
#define Spare_Swt_Bot_TRIS                 TRISBbits.TRISB1
#define Spare_Swt_Bot_LAT                  LATBbits.LATB1
#define Spare_Swt_Bot_PORT                 PORTBbits.RB1
#define Spare_Swt_Bot_SetHigh()            do { LATBbits.LATB1 = 1; } while(0)
#define Spare_Swt_Bot_SetLow()             do { LATBbits.LATB1 = 0; } while(0)
#define Spare_Swt_Bot_Toggle()             do { LATBbits.LATB1 = ~LATBbits.LATB1; } while(0)
#define Spare_Swt_Bot_GetValue()           PORTBbits.RB1
#define Spare_Swt_Bot_SetDigitalInput()    do { TRISBbits.TRISB1 = 1; } while(0)
#define Spare_Swt_Bot_SetDigitalOutput()   do { TRISBbits.TRISB1 = 0; } while(0)

// get/set Led3 aliases
#define Led3_TRIS                 TRISBbits.TRISB2
#define Led3_LAT                  LATBbits.LATB2
#define Led3_PORT                 PORTBbits.RB2
#define Led3_SetHigh()            do { LATBbits.LATB2 = 1; } while(0)
#define Led3_SetLow()             do { LATBbits.LATB2 = 0; } while(0)
#define Led3_Toggle()             do { LATBbits.LATB2 = ~LATBbits.LATB2; } while(0)
#define Led3_GetValue()           PORTBbits.RB2
#define Led3_SetDigitalInput()    do { TRISBbits.TRISB2 = 1; } while(0)
#define Led3_SetDigitalOutput()   do { TRISBbits.TRISB2 = 0; } while(0)

// get/set Iox_Enable aliases
#define Iox_Enable_TRIS                 TRISBbits.TRISB3
#define Iox_Enable_LAT                  LATBbits.LATB3
#define Iox_Enable_PORT                 PORTBbits.RB3
#define Iox_Enable_SetHigh()            do { LATBbits.LATB3 = 1; } while(0)
#define Iox_Enable_SetLow()             do { LATBbits.LATB3 = 0; } while(0)
#define Iox_Enable_Toggle()             do { LATBbits.LATB3 = ~LATBbits.LATB3; } while(0)
#define Iox_Enable_GetValue()           PORTBbits.RB3
#define Iox_Enable_SetDigitalInput()    do { TRISBbits.TRISB3 = 1; } while(0)
#define Iox_Enable_SetDigitalOutput()   do { TRISBbits.TRISB3 = 0; } while(0)

// get/set Output_Enable aliases
#define Output_Enable_TRIS                 TRISBbits.TRISB4
#define Output_Enable_LAT                  LATBbits.LATB4
#define Output_Enable_PORT                 PORTBbits.RB4
#define Output_Enable_SetHigh()            do { LATBbits.LATB4 = 1; } while(0)
#define Output_Enable_SetLow()             do { LATBbits.LATB4 = 0; } while(0)
#define Output_Enable_Toggle()             do { LATBbits.LATB4 = ~LATBbits.LATB4; } while(0)
#define Output_Enable_GetValue()           PORTBbits.RB4
#define Output_Enable_SetDigitalInput()    do { TRISBbits.TRISB4 = 1; } while(0)
#define Output_Enable_SetDigitalOutput()   do { TRISBbits.TRISB4 = 0; } while(0)

// get/set Externall_WDT aliases
#define Externall_WDT_TRIS                 TRISBbits.TRISB5
#define Externall_WDT_LAT                  LATBbits.LATB5
#define Externall_WDT_PORT                 PORTBbits.RB5
#define Externall_WDT_SetHigh()            do { LATBbits.LATB5 = 1; } while(0)
#define Externall_WDT_SetLow()             do { LATBbits.LATB5 = 0; } while(0)
#define Externall_WDT_Toggle()             do { LATBbits.LATB5 = ~LATBbits.LATB5; } while(0)
#define Externall_WDT_GetValue()           PORTBbits.RB5
#define Externall_WDT_SetDigitalInput()    do { TRISBbits.TRISB5 = 1; } while(0)
#define Externall_WDT_SetDigitalOutput()   do { TRISBbits.TRISB5 = 0; } while(0)

// get/set ICSP1 aliases
#define ICSP1_TRIS                 TRISBbits.TRISB6
#define ICSP1_LAT                  LATBbits.LATB6
#define ICSP1_PORT                 PORTBbits.RB6
#define ICSP1_SetHigh()            do { LATBbits.LATB6 = 1; } while(0)
#define ICSP1_SetLow()             do { LATBbits.LATB6 = 0; } while(0)
#define ICSP1_Toggle()             do { LATBbits.LATB6 = ~LATBbits.LATB6; } while(0)
#define ICSP1_GetValue()           PORTBbits.RB6
#define ICSP1_SetDigitalInput()    do { TRISBbits.TRISB6 = 1; } while(0)
#define ICSP1_SetDigitalOutput()   do { TRISBbits.TRISB6 = 0; } while(0)

// get/set ICSP2 aliases
#define ICSP2_TRIS                 TRISBbits.TRISB7
#define ICSP2_LAT                  LATBbits.LATB7
#define ICSP2_PORT                 PORTBbits.RB7
#define ICSP2_SetHigh()            do { LATBbits.LATB7 = 1; } while(0)
#define ICSP2_SetLow()             do { LATBbits.LATB7 = 0; } while(0)
#define ICSP2_Toggle()             do { LATBbits.LATB7 = ~LATBbits.LATB7; } while(0)
#define ICSP2_GetValue()           PORTBbits.RB7
#define ICSP2_SetDigitalInput()    do { TRISBbits.TRISB7 = 1; } while(0)
#define ICSP2_SetDigitalOutput()   do { TRISBbits.TRISB7 = 0; } while(0)

// get/set M10_Top aliases
#define M10_Top_TRIS                 TRISDbits.TRISD0
#define M10_Top_LAT                  LATDbits.LATD0
#define M10_Top_PORT                 PORTDbits.RD0
#define M10_Top_SetHigh()            do { LATDbits.LATD0 = 1; } while(0)
#define M10_Top_SetLow()             do { LATDbits.LATD0 = 0; } while(0)
#define M10_Top_Toggle()             do { LATDbits.LATD0 = ~LATDbits.LATD0; } while(0)
#define M10_Top_GetValue()           PORTDbits.RD0
#define M10_Top_SetDigitalInput()    do { TRISDbits.TRISD0 = 1; } while(0)
#define M10_Top_SetDigitalOutput()   do { TRISDbits.TRISD0 = 0; } while(0)

// get/set M11_Top aliases
#define M11_Top_TRIS                 TRISDbits.TRISD1
#define M11_Top_LAT                  LATDbits.LATD1
#define M11_Top_PORT                 PORTDbits.RD1
#define M11_Top_SetHigh()            do { LATDbits.LATD1 = 1; } while(0)
#define M11_Top_SetLow()             do { LATDbits.LATD1 = 0; } while(0)
#define M11_Top_Toggle()             do { LATDbits.LATD1 = ~LATDbits.LATD1; } while(0)
#define M11_Top_GetValue()           PORTDbits.RD1
#define M11_Top_SetDigitalInput()    do { TRISDbits.TRISD1 = 1; } while(0)
#define M11_Top_SetDigitalOutput()   do { TRISDbits.TRISD1 = 0; } while(0)

// get/set Occupied_Res_Top aliases
#define Occupied_Res_Top_TRIS                 TRISDbits.TRISD2
#define Occupied_Res_Top_LAT                  LATDbits.LATD2
#define Occupied_Res_Top_PORT                 PORTDbits.RD2
#define Occupied_Res_Top_SetHigh()            do { LATDbits.LATD2 = 1; } while(0)
#define Occupied_Res_Top_SetLow()             do { LATDbits.LATD2 = 0; } while(0)
#define Occupied_Res_Top_Toggle()             do { LATDbits.LATD2 = ~LATDbits.LATD2; } while(0)
#define Occupied_Res_Top_GetValue()           PORTDbits.RD2
#define Occupied_Res_Top_SetDigitalInput()    do { TRISDbits.TRISD2 = 1; } while(0)
#define Occupied_Res_Top_SetDigitalOutput()   do { TRISDbits.TRISD2 = 0; } while(0)

// get/set M10_Bot aliases
#define M10_Bot_TRIS                 TRISDbits.TRISD3
#define M10_Bot_LAT                  LATDbits.LATD3
#define M10_Bot_PORT                 PORTDbits.RD3
#define M10_Bot_SetHigh()            do { LATDbits.LATD3 = 1; } while(0)
#define M10_Bot_SetLow()             do { LATDbits.LATD3 = 0; } while(0)
#define M10_Bot_Toggle()             do { LATDbits.LATD3 = ~LATDbits.LATD3; } while(0)
#define M10_Bot_GetValue()           PORTDbits.RD3
#define M10_Bot_SetDigitalInput()    do { TRISDbits.TRISD3 = 1; } while(0)
#define M10_Bot_SetDigitalOutput()   do { TRISDbits.TRISD3 = 0; } while(0)

// get/set M11_Bot aliases
#define M11_Bot_TRIS                 TRISDbits.TRISD4
#define M11_Bot_LAT                  LATDbits.LATD4
#define M11_Bot_PORT                 PORTDbits.RD4
#define M11_Bot_SetHigh()            do { LATDbits.LATD4 = 1; } while(0)
#define M11_Bot_SetLow()             do { LATDbits.LATD4 = 0; } while(0)
#define M11_Bot_Toggle()             do { LATDbits.LATD4 = ~LATDbits.LATD4; } while(0)
#define M11_Bot_GetValue()           PORTDbits.RD4
#define M11_Bot_SetDigitalInput()    do { TRISDbits.TRISD4 = 1; } while(0)
#define M11_Bot_SetDigitalOutput()   do { TRISDbits.TRISD4 = 0; } while(0)

// get/set IO_RD5 aliases
#define IO_RD5_TRIS                 TRISDbits.TRISD5
#define IO_RD5_LAT                  LATDbits.LATD5
#define IO_RD5_PORT                 PORTDbits.RD5
#define IO_RD5_SetHigh()            do { LATDbits.LATD5 = 1; } while(0)
#define IO_RD5_SetLow()             do { LATDbits.LATD5 = 0; } while(0)
#define IO_RD5_Toggle()             do { LATDbits.LATD5 = ~LATDbits.LATD5; } while(0)
#define IO_RD5_GetValue()           PORTDbits.RD5
#define IO_RD5_SetDigitalInput()    do { TRISDbits.TRISD5 = 1; } while(0)
#define IO_RD5_SetDigitalOutput()   do { TRISDbits.TRISD5 = 0; } while(0)

// get/set IO_RD6 aliases
#define IO_RD6_TRIS                 TRISDbits.TRISD6
#define IO_RD6_LAT                  LATDbits.LATD6
#define IO_RD6_PORT                 PORTDbits.RD6
#define IO_RD6_SetHigh()            do { LATDbits.LATD6 = 1; } while(0)
#define IO_RD6_SetLow()             do { LATDbits.LATD6 = 0; } while(0)
#define IO_RD6_Toggle()             do { LATDbits.LATD6 = ~LATDbits.LATD6; } while(0)
#define IO_RD6_GetValue()           PORTDbits.RD6
#define IO_RD6_SetDigitalInput()    do { TRISDbits.TRISD6 = 1; } while(0)
#define IO_RD6_SetDigitalOutput()   do { TRISDbits.TRISD6 = 0; } while(0)

// get/set Occupied_Res_Bot aliases
#define Occupied_Res_Bot_TRIS                 TRISDbits.TRISD7
#define Occupied_Res_Bot_LAT                  LATDbits.LATD7
#define Occupied_Res_Bot_PORT                 PORTDbits.RD7
#define Occupied_Res_Bot_SetHigh()            do { LATDbits.LATD7 = 1; } while(0)
#define Occupied_Res_Bot_SetLow()             do { LATDbits.LATD7 = 0; } while(0)
#define Occupied_Res_Bot_Toggle()             do { LATDbits.LATD7 = ~LATDbits.LATD7; } while(0)
#define Occupied_Res_Bot_GetValue()           PORTDbits.RD7
#define Occupied_Res_Bot_SetDigitalInput()    do { TRISDbits.TRISD7 = 1; } while(0)
#define Occupied_Res_Bot_SetDigitalOutput()   do { TRISDbits.TRISD7 = 0; } while(0)


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