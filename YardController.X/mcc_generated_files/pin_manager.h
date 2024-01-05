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

// get/set SW_BUSSTOP_IND aliases
#define SW_BUSSTOP_IND_TRIS                 TRISDbits.TRISD0
#define SW_BUSSTOP_IND_LAT                  LATDbits.LATD0
#define SW_BUSSTOP_IND_PORT                 PORTDbits.RD0
#define SW_BUSSTOP_IND_SetHigh()            do { LATDbits.LATD0 = 1; } while(0)
#define SW_BUSSTOP_IND_SetLow()             do { LATDbits.LATD0 = 0; } while(0)
#define SW_BUSSTOP_IND_Toggle()             do { LATDbits.LATD0 = ~LATDbits.LATD0; } while(0)
#define SW_BUSSTOP_IND_GetValue()           PORTDbits.RD0
#define SW_BUSSTOP_IND_SetDigitalInput()    do { TRISDbits.TRISD0 = 1; } while(0)
#define SW_BUSSTOP_IND_SetDigitalOutput()   do { TRISDbits.TRISD0 = 0; } while(0)

// get/set STOP_BUS_AT_IND aliases
#define STOP_BUS_AT_IND_TRIS                 TRISDbits.TRISD1
#define STOP_BUS_AT_IND_LAT                  LATDbits.LATD1
#define STOP_BUS_AT_IND_PORT                 PORTDbits.RD1
#define STOP_BUS_AT_IND_SetHigh()            do { LATDbits.LATD1 = 1; } while(0)
#define STOP_BUS_AT_IND_SetLow()             do { LATDbits.LATD1 = 0; } while(0)
#define STOP_BUS_AT_IND_Toggle()             do { LATDbits.LATD1 = ~LATDbits.LATD1; } while(0)
#define STOP_BUS_AT_IND_GetValue()           PORTDbits.RD1
#define STOP_BUS_AT_IND_SetDigitalInput()    do { TRISDbits.TRISD1 = 1; } while(0)
#define STOP_BUS_AT_IND_SetDigitalOutput()   do { TRISDbits.TRISD1 = 0; } while(0)

// get/set SW_BUSSTOP_STN aliases
#define SW_BUSSTOP_STN_TRIS                 TRISDbits.TRISD2
#define SW_BUSSTOP_STN_LAT                  LATDbits.LATD2
#define SW_BUSSTOP_STN_PORT                 PORTDbits.RD2
#define SW_BUSSTOP_STN_SetHigh()            do { LATDbits.LATD2 = 1; } while(0)
#define SW_BUSSTOP_STN_SetLow()             do { LATDbits.LATD2 = 0; } while(0)
#define SW_BUSSTOP_STN_Toggle()             do { LATDbits.LATD2 = ~LATDbits.LATD2; } while(0)
#define SW_BUSSTOP_STN_GetValue()           PORTDbits.RD2
#define SW_BUSSTOP_STN_SetDigitalInput()    do { TRISDbits.TRISD2 = 1; } while(0)
#define SW_BUSSTOP_STN_SetDigitalOutput()   do { TRISDbits.TRISD2 = 0; } while(0)

// get/set STOP_BUS_AT_STN aliases
#define STOP_BUS_AT_STN_TRIS                 TRISDbits.TRISD3
#define STOP_BUS_AT_STN_LAT                  LATDbits.LATD3
#define STOP_BUS_AT_STN_PORT                 PORTDbits.RD3
#define STOP_BUS_AT_STN_SetHigh()            do { LATDbits.LATD3 = 1; } while(0)
#define STOP_BUS_AT_STN_SetLow()             do { LATDbits.LATD3 = 0; } while(0)
#define STOP_BUS_AT_STN_Toggle()             do { LATDbits.LATD3 = ~LATDbits.LATD3; } while(0)
#define STOP_BUS_AT_STN_GetValue()           PORTDbits.RD3
#define STOP_BUS_AT_STN_SetDigitalInput()    do { TRISDbits.TRISD3 = 1; } while(0)
#define STOP_BUS_AT_STN_SetDigitalOutput()   do { TRISDbits.TRISD3 = 0; } while(0)

// get/set SW_FDEP_RIGHT aliases
#define SW_FDEP_RIGHT_TRIS                 TRISDbits.TRISD4
#define SW_FDEP_RIGHT_LAT                  LATDbits.LATD4
#define SW_FDEP_RIGHT_PORT                 PORTDbits.RD4
#define SW_FDEP_RIGHT_SetHigh()            do { LATDbits.LATD4 = 1; } while(0)
#define SW_FDEP_RIGHT_SetLow()             do { LATDbits.LATD4 = 0; } while(0)
#define SW_FDEP_RIGHT_Toggle()             do { LATDbits.LATD4 = ~LATDbits.LATD4; } while(0)
#define SW_FDEP_RIGHT_GetValue()           PORTDbits.RD4
#define SW_FDEP_RIGHT_SetDigitalInput()    do { TRISDbits.TRISD4 = 1; } while(0)
#define SW_FDEP_RIGHT_SetDigitalOutput()   do { TRISDbits.TRISD4 = 0; } while(0)

// get/set SW_FDEP_MID aliases
#define SW_FDEP_MID_TRIS                 TRISDbits.TRISD5
#define SW_FDEP_MID_LAT                  LATDbits.LATD5
#define SW_FDEP_MID_PORT                 PORTDbits.RD5
#define SW_FDEP_MID_SetHigh()            do { LATDbits.LATD5 = 1; } while(0)
#define SW_FDEP_MID_SetLow()             do { LATDbits.LATD5 = 0; } while(0)
#define SW_FDEP_MID_Toggle()             do { LATDbits.LATD5 = ~LATDbits.LATD5; } while(0)
#define SW_FDEP_MID_GetValue()           PORTDbits.RD5
#define SW_FDEP_MID_SetDigitalInput()    do { TRISDbits.TRISD5 = 1; } while(0)
#define SW_FDEP_MID_SetDigitalOutput()   do { TRISDbits.TRISD5 = 0; } while(0)

// get/set STOP_FDEP_AT_MID aliases
#define STOP_FDEP_AT_MID_TRIS                 TRISDbits.TRISD6
#define STOP_FDEP_AT_MID_LAT                  LATDbits.LATD6
#define STOP_FDEP_AT_MID_PORT                 PORTDbits.RD6
#define STOP_FDEP_AT_MID_SetHigh()            do { LATDbits.LATD6 = 1; } while(0)
#define STOP_FDEP_AT_MID_SetLow()             do { LATDbits.LATD6 = 0; } while(0)
#define STOP_FDEP_AT_MID_Toggle()             do { LATDbits.LATD6 = ~LATDbits.LATD6; } while(0)
#define STOP_FDEP_AT_MID_GetValue()           PORTDbits.RD6
#define STOP_FDEP_AT_MID_SetDigitalInput()    do { TRISDbits.TRISD6 = 1; } while(0)
#define STOP_FDEP_AT_MID_SetDigitalOutput()   do { TRISDbits.TRISD6 = 0; } while(0)

// get/set STOP_FDEP_AT_RIGHT aliases
#define STOP_FDEP_AT_RIGHT_TRIS                 TRISDbits.TRISD7
#define STOP_FDEP_AT_RIGHT_LAT                  LATDbits.LATD7
#define STOP_FDEP_AT_RIGHT_PORT                 PORTDbits.RD7
#define STOP_FDEP_AT_RIGHT_SetHigh()            do { LATDbits.LATD7 = 1; } while(0)
#define STOP_FDEP_AT_RIGHT_SetLow()             do { LATDbits.LATD7 = 0; } while(0)
#define STOP_FDEP_AT_RIGHT_Toggle()             do { LATDbits.LATD7 = ~LATDbits.LATD7; } while(0)
#define STOP_FDEP_AT_RIGHT_GetValue()           PORTDbits.RD7
#define STOP_FDEP_AT_RIGHT_SetDigitalInput()    do { TRISDbits.TRISD7 = 1; } while(0)
#define STOP_FDEP_AT_RIGHT_SetDigitalOutput()   do { TRISDbits.TRISD7 = 0; } while(0)

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