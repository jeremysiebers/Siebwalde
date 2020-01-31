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
        Device            :  PIC18F27K40
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

// get/set modbus_sync aliases
#define modbus_sync_TRIS                 TRISAbits.TRISA6
#define modbus_sync_LAT                  LATAbits.LATA6
#define modbus_sync_PORT                 PORTAbits.RA6
#define modbus_sync_WPU                  WPUAbits.WPUA6
#define modbus_sync_OD                   ODCONAbits.ODCA6
#define modbus_sync_ANS                  ANSELAbits.ANSELA6
#define modbus_sync_SetHigh()            do { LATAbits.LATA6 = 1; } while(0)
#define modbus_sync_SetLow()             do { LATAbits.LATA6 = 0; } while(0)
#define modbus_sync_Toggle()             do { LATAbits.LATA6 = ~LATAbits.LATA6; } while(0)
#define modbus_sync_GetValue()           PORTAbits.RA6
#define modbus_sync_SetDigitalInput()    do { TRISAbits.TRISA6 = 1; } while(0)
#define modbus_sync_SetDigitalOutput()   do { TRISAbits.TRISA6 = 0; } while(0)
#define modbus_sync_SetPullup()          do { WPUAbits.WPUA6 = 1; } while(0)
#define modbus_sync_ResetPullup()        do { WPUAbits.WPUA6 = 0; } while(0)
#define modbus_sync_SetPushPull()        do { ODCONAbits.ODCA6 = 0; } while(0)
#define modbus_sync_SetOpenDrain()       do { ODCONAbits.ODCA6 = 1; } while(0)
#define modbus_sync_SetAnalogMode()      do { ANSELAbits.ANSELA6 = 1; } while(0)
#define modbus_sync_SetDigitalMode()     do { ANSELAbits.ANSELA6 = 0; } while(0)

// get/set modbus_send aliases
#define modbus_send_TRIS                 TRISAbits.TRISA7
#define modbus_send_LAT                  LATAbits.LATA7
#define modbus_send_PORT                 PORTAbits.RA7
#define modbus_send_WPU                  WPUAbits.WPUA7
#define modbus_send_OD                   ODCONAbits.ODCA7
#define modbus_send_ANS                  ANSELAbits.ANSELA7
#define modbus_send_SetHigh()            do { LATAbits.LATA7 = 1; } while(0)
#define modbus_send_SetLow()             do { LATAbits.LATA7 = 0; } while(0)
#define modbus_send_Toggle()             do { LATAbits.LATA7 = ~LATAbits.LATA7; } while(0)
#define modbus_send_GetValue()           PORTAbits.RA7
#define modbus_send_SetDigitalInput()    do { TRISAbits.TRISA7 = 1; } while(0)
#define modbus_send_SetDigitalOutput()   do { TRISAbits.TRISA7 = 0; } while(0)
#define modbus_send_SetPullup()          do { WPUAbits.WPUA7 = 1; } while(0)
#define modbus_send_ResetPullup()        do { WPUAbits.WPUA7 = 0; } while(0)
#define modbus_send_SetPushPull()        do { ODCONAbits.ODCA7 = 0; } while(0)
#define modbus_send_SetOpenDrain()       do { ODCONAbits.ODCA7 = 1; } while(0)
#define modbus_send_SetAnalogMode()      do { ANSELAbits.ANSELA7 = 1; } while(0)
#define modbus_send_SetDigitalMode()     do { ANSELAbits.ANSELA7 = 0; } while(0)

// get/set SCK1 aliases
#define SCK1_TRIS                 TRISBbits.TRISB0
#define SCK1_LAT                  LATBbits.LATB0
#define SCK1_PORT                 PORTBbits.RB0
#define SCK1_WPU                  WPUBbits.WPUB0
#define SCK1_OD                   ODCONBbits.ODCB0
#define SCK1_ANS                  ANSELBbits.ANSELB0
#define SCK1_SetHigh()            do { LATBbits.LATB0 = 1; } while(0)
#define SCK1_SetLow()             do { LATBbits.LATB0 = 0; } while(0)
#define SCK1_Toggle()             do { LATBbits.LATB0 = ~LATBbits.LATB0; } while(0)
#define SCK1_GetValue()           PORTBbits.RB0
#define SCK1_SetDigitalInput()    do { TRISBbits.TRISB0 = 1; } while(0)
#define SCK1_SetDigitalOutput()   do { TRISBbits.TRISB0 = 0; } while(0)
#define SCK1_SetPullup()          do { WPUBbits.WPUB0 = 1; } while(0)
#define SCK1_ResetPullup()        do { WPUBbits.WPUB0 = 0; } while(0)
#define SCK1_SetPushPull()        do { ODCONBbits.ODCB0 = 0; } while(0)
#define SCK1_SetOpenDrain()       do { ODCONBbits.ODCB0 = 1; } while(0)
#define SCK1_SetAnalogMode()      do { ANSELBbits.ANSELB0 = 1; } while(0)
#define SCK1_SetDigitalMode()     do { ANSELBbits.ANSELB0 = 0; } while(0)

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

// get/set RC0 procedures
#define RC0_SetHigh()            do { LATCbits.LATC0 = 1; } while(0)
#define RC0_SetLow()             do { LATCbits.LATC0 = 0; } while(0)
#define RC0_Toggle()             do { LATCbits.LATC0 = ~LATCbits.LATC0; } while(0)
#define RC0_GetValue()              PORTCbits.RC0
#define RC0_SetDigitalInput()    do { TRISCbits.TRISC0 = 1; } while(0)
#define RC0_SetDigitalOutput()   do { TRISCbits.TRISC0 = 0; } while(0)
#define RC0_SetPullup()             do { WPUCbits.WPUC0 = 1; } while(0)
#define RC0_ResetPullup()           do { WPUCbits.WPUC0 = 0; } while(0)
#define RC0_SetAnalogMode()         do { ANSELCbits.ANSELC0 = 1; } while(0)
#define RC0_SetDigitalMode()        do { ANSELCbits.ANSELC0 = 0; } while(0)

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

// get/set SDI1 aliases
#define SDI1_TRIS                 TRISCbits.TRISC4
#define SDI1_LAT                  LATCbits.LATC4
#define SDI1_PORT                 PORTCbits.RC4
#define SDI1_WPU                  WPUCbits.WPUC4
#define SDI1_OD                   ODCONCbits.ODCC4
#define SDI1_ANS                  ANSELCbits.ANSELC4
#define SDI1_SetHigh()            do { LATCbits.LATC4 = 1; } while(0)
#define SDI1_SetLow()             do { LATCbits.LATC4 = 0; } while(0)
#define SDI1_Toggle()             do { LATCbits.LATC4 = ~LATCbits.LATC4; } while(0)
#define SDI1_GetValue()           PORTCbits.RC4
#define SDI1_SetDigitalInput()    do { TRISCbits.TRISC4 = 1; } while(0)
#define SDI1_SetDigitalOutput()   do { TRISCbits.TRISC4 = 0; } while(0)
#define SDI1_SetPullup()          do { WPUCbits.WPUC4 = 1; } while(0)
#define SDI1_ResetPullup()        do { WPUCbits.WPUC4 = 0; } while(0)
#define SDI1_SetPushPull()        do { ODCONCbits.ODCC4 = 0; } while(0)
#define SDI1_SetOpenDrain()       do { ODCONCbits.ODCC4 = 1; } while(0)
#define SDI1_SetAnalogMode()      do { ANSELCbits.ANSELC4 = 1; } while(0)
#define SDI1_SetDigitalMode()     do { ANSELCbits.ANSELC4 = 0; } while(0)

// get/set SDO1 aliases
#define SDO1_TRIS                 TRISCbits.TRISC5
#define SDO1_LAT                  LATCbits.LATC5
#define SDO1_PORT                 PORTCbits.RC5
#define SDO1_WPU                  WPUCbits.WPUC5
#define SDO1_OD                   ODCONCbits.ODCC5
#define SDO1_ANS                  ANSELCbits.ANSELC5
#define SDO1_SetHigh()            do { LATCbits.LATC5 = 1; } while(0)
#define SDO1_SetLow()             do { LATCbits.LATC5 = 0; } while(0)
#define SDO1_Toggle()             do { LATCbits.LATC5 = ~LATCbits.LATC5; } while(0)
#define SDO1_GetValue()           PORTCbits.RC5
#define SDO1_SetDigitalInput()    do { TRISCbits.TRISC5 = 1; } while(0)
#define SDO1_SetDigitalOutput()   do { TRISCbits.TRISC5 = 0; } while(0)
#define SDO1_SetPullup()          do { WPUCbits.WPUC5 = 1; } while(0)
#define SDO1_ResetPullup()        do { WPUCbits.WPUC5 = 0; } while(0)
#define SDO1_SetPushPull()        do { ODCONCbits.ODCC5 = 0; } while(0)
#define SDO1_SetOpenDrain()       do { ODCONCbits.ODCC5 = 1; } while(0)
#define SDO1_SetAnalogMode()      do { ANSELCbits.ANSELC5 = 1; } while(0)
#define SDO1_SetDigitalMode()     do { ANSELCbits.ANSELC5 = 0; } while(0)

// get/set SS1 aliases
#define SS1_TRIS                 TRISCbits.TRISC6
#define SS1_LAT                  LATCbits.LATC6
#define SS1_PORT                 PORTCbits.RC6
#define SS1_WPU                  WPUCbits.WPUC6
#define SS1_OD                   ODCONCbits.ODCC6
#define SS1_ANS                  ANSELCbits.ANSELC6
#define SS1_SetHigh()            do { LATCbits.LATC6 = 1; } while(0)
#define SS1_SetLow()             do { LATCbits.LATC6 = 0; } while(0)
#define SS1_Toggle()             do { LATCbits.LATC6 = ~LATCbits.LATC6; } while(0)
#define SS1_GetValue()           PORTCbits.RC6
#define SS1_SetDigitalInput()    do { TRISCbits.TRISC6 = 1; } while(0)
#define SS1_SetDigitalOutput()   do { TRISCbits.TRISC6 = 0; } while(0)
#define SS1_SetPullup()          do { WPUCbits.WPUC6 = 1; } while(0)
#define SS1_ResetPullup()        do { WPUCbits.WPUC6 = 0; } while(0)
#define SS1_SetPushPull()        do { ODCONCbits.ODCC6 = 0; } while(0)
#define SS1_SetOpenDrain()       do { ODCONCbits.ODCC6 = 1; } while(0)
#define SS1_SetAnalogMode()      do { ANSELCbits.ANSELC6 = 1; } while(0)
#define SS1_SetDigitalMode()     do { ANSELCbits.ANSELC6 = 0; } while(0)

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