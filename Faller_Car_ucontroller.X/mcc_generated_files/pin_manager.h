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
        Device            :  PIC16F15324
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

// get/set EN_MOT aliases
#define EN_MOT_TRIS                 TRISAbits.TRISA0
#define EN_MOT_LAT                  LATAbits.LATA0
#define EN_MOT_PORT                 PORTAbits.RA0
#define EN_MOT_WPU                  WPUAbits.WPUA0
#define EN_MOT_OD                   ODCONAbits.ODCA0
#define EN_MOT_ANS                  ANSELAbits.ANSA0
#define EN_MOT_SetHigh()            do { LATAbits.LATA0 = 1; } while(0)
#define EN_MOT_SetLow()             do { LATAbits.LATA0 = 0; } while(0)
#define EN_MOT_Toggle()             do { LATAbits.LATA0 = ~LATAbits.LATA0; } while(0)
#define EN_MOT_GetValue()           PORTAbits.RA0
#define EN_MOT_SetDigitalInput()    do { TRISAbits.TRISA0 = 1; } while(0)
#define EN_MOT_SetDigitalOutput()   do { TRISAbits.TRISA0 = 0; } while(0)
#define EN_MOT_SetPullup()          do { WPUAbits.WPUA0 = 1; } while(0)
#define EN_MOT_ResetPullup()        do { WPUAbits.WPUA0 = 0; } while(0)
#define EN_MOT_SetPushPull()        do { ODCONAbits.ODCA0 = 0; } while(0)
#define EN_MOT_SetOpenDrain()       do { ODCONAbits.ODCA0 = 1; } while(0)
#define EN_MOT_SetAnalogMode()      do { ANSELAbits.ANSA0 = 1; } while(0)
#define EN_MOT_SetDigitalMode()     do { ANSELAbits.ANSA0 = 0; } while(0)

// get/set UNUSED aliases
#define UNUSED_TRIS                 TRISAbits.TRISA1
#define UNUSED_LAT                  LATAbits.LATA1
#define UNUSED_PORT                 PORTAbits.RA1
#define UNUSED_WPU                  WPUAbits.WPUA1
#define UNUSED_OD                   ODCONAbits.ODCA1
#define UNUSED_ANS                  ANSELAbits.ANSA1
#define UNUSED_SetHigh()            do { LATAbits.LATA1 = 1; } while(0)
#define UNUSED_SetLow()             do { LATAbits.LATA1 = 0; } while(0)
#define UNUSED_Toggle()             do { LATAbits.LATA1 = ~LATAbits.LATA1; } while(0)
#define UNUSED_GetValue()           PORTAbits.RA1
#define UNUSED_SetDigitalInput()    do { TRISAbits.TRISA1 = 1; } while(0)
#define UNUSED_SetDigitalOutput()   do { TRISAbits.TRISA1 = 0; } while(0)
#define UNUSED_SetPullup()          do { WPUAbits.WPUA1 = 1; } while(0)
#define UNUSED_ResetPullup()        do { WPUAbits.WPUA1 = 0; } while(0)
#define UNUSED_SetPushPull()        do { ODCONAbits.ODCA1 = 0; } while(0)
#define UNUSED_SetOpenDrain()       do { ODCONAbits.ODCA1 = 1; } while(0)
#define UNUSED_SetAnalogMode()      do { ANSELAbits.ANSA1 = 1; } while(0)
#define UNUSED_SetDigitalMode()     do { ANSELAbits.ANSA1 = 0; } while(0)

// get/set DASHLED aliases
#define DASHLED_TRIS                 TRISAbits.TRISA2
#define DASHLED_LAT                  LATAbits.LATA2
#define DASHLED_PORT                 PORTAbits.RA2
#define DASHLED_WPU                  WPUAbits.WPUA2
#define DASHLED_OD                   ODCONAbits.ODCA2
#define DASHLED_ANS                  ANSELAbits.ANSA2
#define DASHLED_SetHigh()            do { LATAbits.LATA2 = 1; } while(0)
#define DASHLED_SetLow()             do { LATAbits.LATA2 = 0; } while(0)
#define DASHLED_Toggle()             do { LATAbits.LATA2 = ~LATAbits.LATA2; } while(0)
#define DASHLED_GetValue()           PORTAbits.RA2
#define DASHLED_SetDigitalInput()    do { TRISAbits.TRISA2 = 1; } while(0)
#define DASHLED_SetDigitalOutput()   do { TRISAbits.TRISA2 = 0; } while(0)
#define DASHLED_SetPullup()          do { WPUAbits.WPUA2 = 1; } while(0)
#define DASHLED_ResetPullup()        do { WPUAbits.WPUA2 = 0; } while(0)
#define DASHLED_SetPushPull()        do { ODCONAbits.ODCA2 = 0; } while(0)
#define DASHLED_SetOpenDrain()       do { ODCONAbits.ODCA2 = 1; } while(0)
#define DASHLED_SetAnalogMode()      do { ANSELAbits.ANSA2 = 1; } while(0)
#define DASHLED_SetDigitalMode()     do { ANSELAbits.ANSA2 = 0; } while(0)

// get/set RA4 procedures
#define RA4_SetHigh()            do { LATAbits.LATA4 = 1; } while(0)
#define RA4_SetLow()             do { LATAbits.LATA4 = 0; } while(0)
#define RA4_Toggle()             do { LATAbits.LATA4 = ~LATAbits.LATA4; } while(0)
#define RA4_GetValue()              PORTAbits.RA4
#define RA4_SetDigitalInput()    do { TRISAbits.TRISA4 = 1; } while(0)
#define RA4_SetDigitalOutput()   do { TRISAbits.TRISA4 = 0; } while(0)
#define RA4_SetPullup()             do { WPUAbits.WPUA4 = 1; } while(0)
#define RA4_ResetPullup()           do { WPUAbits.WPUA4 = 0; } while(0)
#define RA4_SetAnalogMode()         do { ANSELAbits.ANSA4 = 1; } while(0)
#define RA4_SetDigitalMode()        do { ANSELAbits.ANSA4 = 0; } while(0)

// get/set RA5 procedures
#define RA5_SetHigh()            do { LATAbits.LATA5 = 1; } while(0)
#define RA5_SetLow()             do { LATAbits.LATA5 = 0; } while(0)
#define RA5_Toggle()             do { LATAbits.LATA5 = ~LATAbits.LATA5; } while(0)
#define RA5_GetValue()              PORTAbits.RA5
#define RA5_SetDigitalInput()    do { TRISAbits.TRISA5 = 1; } while(0)
#define RA5_SetDigitalOutput()   do { TRISAbits.TRISA5 = 0; } while(0)
#define RA5_SetPullup()             do { WPUAbits.WPUA5 = 1; } while(0)
#define RA5_ResetPullup()           do { WPUAbits.WPUA5 = 0; } while(0)
#define RA5_SetAnalogMode()         do { ANSELAbits.ANSA5 = 1; } while(0)
#define RA5_SetDigitalMode()        do { ANSELAbits.ANSA5 = 0; } while(0)

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

// get/set RCS aliases
#define RCS_TRIS                 TRISCbits.TRISC1
#define RCS_LAT                  LATCbits.LATC1
#define RCS_PORT                 PORTCbits.RC1
#define RCS_WPU                  WPUCbits.WPUC1
#define RCS_OD                   ODCONCbits.ODCC1
#define RCS_ANS                  ANSELCbits.ANSC1
#define RCS_SetHigh()            do { LATCbits.LATC1 = 1; } while(0)
#define RCS_SetLow()             do { LATCbits.LATC1 = 0; } while(0)
#define RCS_Toggle()             do { LATCbits.LATC1 = ~LATCbits.LATC1; } while(0)
#define RCS_GetValue()           PORTCbits.RC1
#define RCS_SetDigitalInput()    do { TRISCbits.TRISC1 = 1; } while(0)
#define RCS_SetDigitalOutput()   do { TRISCbits.TRISC1 = 0; } while(0)
#define RCS_SetPullup()          do { WPUCbits.WPUC1 = 1; } while(0)
#define RCS_ResetPullup()        do { WPUCbits.WPUC1 = 0; } while(0)
#define RCS_SetPushPull()        do { ODCONCbits.ODCC1 = 0; } while(0)
#define RCS_SetOpenDrain()       do { ODCONCbits.ODCC1 = 1; } while(0)
#define RCS_SetAnalogMode()      do { ANSELCbits.ANSC1 = 1; } while(0)
#define RCS_SetDigitalMode()     do { ANSELCbits.ANSC1 = 0; } while(0)

// get/set RC2 procedures
#define RC2_SetHigh()            do { LATCbits.LATC2 = 1; } while(0)
#define RC2_SetLow()             do { LATCbits.LATC2 = 0; } while(0)
#define RC2_Toggle()             do { LATCbits.LATC2 = ~LATCbits.LATC2; } while(0)
#define RC2_GetValue()              PORTCbits.RC2
#define RC2_SetDigitalInput()    do { TRISCbits.TRISC2 = 1; } while(0)
#define RC2_SetDigitalOutput()   do { TRISCbits.TRISC2 = 0; } while(0)
#define RC2_SetPullup()             do { WPUCbits.WPUC2 = 1; } while(0)
#define RC2_ResetPullup()           do { WPUCbits.WPUC2 = 0; } while(0)
#define RC2_SetAnalogMode()         do { ANSELCbits.ANSC2 = 1; } while(0)
#define RC2_SetDigitalMode()        do { ANSELCbits.ANSC2 = 0; } while(0)

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

// get/set RC4 procedures
#define RC4_SetHigh()            do { LATCbits.LATC4 = 1; } while(0)
#define RC4_SetLow()             do { LATCbits.LATC4 = 0; } while(0)
#define RC4_Toggle()             do { LATCbits.LATC4 = ~LATCbits.LATC4; } while(0)
#define RC4_GetValue()              PORTCbits.RC4
#define RC4_SetDigitalInput()    do { TRISCbits.TRISC4 = 1; } while(0)
#define RC4_SetDigitalOutput()   do { TRISCbits.TRISC4 = 0; } while(0)
#define RC4_SetPullup()             do { WPUCbits.WPUC4 = 1; } while(0)
#define RC4_ResetPullup()           do { WPUCbits.WPUC4 = 0; } while(0)
#define RC4_SetAnalogMode()         do { ANSELCbits.ANSC4 = 1; } while(0)
#define RC4_SetDigitalMode()        do { ANSELCbits.ANSC4 = 0; } while(0)

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