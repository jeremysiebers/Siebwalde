/**
  EUSART2 Generated Driver File

  @Company
    Microchip Technology Inc.

  @File Name
    eusart2.c

  @Summary
    This is the generated driver implementation file for the EUSART2 driver using MPLAB® Code Configurator

  @Description
    This header file provides implementations for driver APIs for EUSART2.
    Generation Information :
        Product Revision  :  MPLAB® Code Configurator - v2.25.2
        Device            :  PIC18F46K22
        Driver Version    :  2.00
    The generated drivers are tested against the following:
        Compiler          :  XC8 v1.34
        MPLAB             :  MPLAB X v2.35 or v3.00
 */

/*
Copyright (c) 2013 - 2015 released Microchip Technology Inc.  All rights reserved.

Microchip licenses to you the right to use, modify, copy and distribute
Software only when embedded on a Microchip microcontroller or digital signal
controller that is integrated into your product or third party product
(pursuant to the sublicense terms in the accompanying license agreement).

You should refer to the license agreement accompanying this Software for
additional information regarding your rights and obligations.

SOFTWARE AND DOCUMENTATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION, ANY WARRANTY OF
MERCHANTABILITY, TITLE, NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR PURPOSE.
IN NO EVENT SHALL MICROCHIP OR ITS LICENSORS BE LIABLE OR OBLIGATED UNDER
CONTRACT, NEGLIGENCE, STRICT LIABILITY, CONTRIBUTION, BREACH OF WARRANTY, OR
OTHER LEGAL EQUITABLE THEORY ANY DIRECT OR INDIRECT DAMAGES OR EXPENSES
INCLUDING BUT NOT LIMITED TO ANY INCIDENTAL, SPECIAL, INDIRECT, PUNITIVE OR
CONSEQUENTIAL DAMAGES, LOST PROFITS OR LOST DATA, COST OF PROCUREMENT OF
SUBSTITUTE GOODS, TECHNOLOGY, SERVICES, OR ANY CLAIMS BY THIRD PARTIES
(INCLUDING BUT NOT LIMITED TO ANY DEFENSE THEREOF), OR OTHER SIMILAR COSTS.
 */

/**
  Section: Included Files
 */
#include <p18f97j60.h>			// uProc lib
#include <Fiddle_Yard.h>
#include "eusart2.h"


/**
  Section: Macro Declarations
 */
#define EUSART2_TX_BUFFER_SIZE 8
#define EUSART2_RX_BUFFER_SIZE 8

#ifndef int8_t
typedef signed char int8_t;
#define int8_t int8_t
#define INT8_MIN (-128)
#define INT8_MAX (127)
#endif

#ifndef int16_t
typedef signed int int16_t;
#define int16_t int16_t
#define INT16_MIN (-32768)
#define INT16_MAX (32767)
#endif

#ifndef __CCI__
#ifndef int24_t
typedef signed short long int int24_t;
#define int24_t int24_t
#define INT24_MIN (-8388608L)
#define INT24_MAX (8388607L)
#endif
#endif

#ifndef int32_t
typedef signed long int int32_t;
#define int32_t int32_t
#define INT32_MIN (-2147483648L)
#define INT32_MAX (2147483647L)
#endif

#ifndef uint8_t
typedef unsigned char uint8_t;
#define uint8_t uint8_t
#define UINT8_MAX (255)
#endif

#ifndef uint16_t
typedef unsigned int uint16_t;
#define uint16_t uint16_t
#define UINT16_MAX (65535U)
#endif

#ifndef __CCI__
#ifndef uint24_t
typedef unsigned short long int uint24_t;
#define uint24_t uint24_t
#define UINT24_MAX (16777215UL)
#endif
#endif

#ifndef uint32_t
typedef unsigned long int uint32_t;
#define uint32_t uint32_t
#define UINT32_MAX (4294967295UL)
#endif

  /* types that are at least as wide */

#ifndef int_least8_t
typedef signed char int_least8_t;
#define int_least8_t int_least8_t
#define INT_LEAST8_MIN (-128)
#define INT_LEAST8_MAX (127)
#endif

#ifndef int_least16_t
typedef signed int int_least16_t;
#define int_least16_t int_least16_t
#define INT_LEAST16_MIN (-32768)
#define INT_LEAST16_MAX (32767)
#endif

#ifndef int_least24_t
#ifdef __CCI__
typedef signed long int int_least24_t;
#define INT_LEAST24_MIN (-2147483648L)
#define INT_LEAST24_MAX (2147483647L)
#else
typedef signed short long int int_least24_t;
#define INT_LEAST24_MIN (-8388608L)
#define INT_LEAST24_MAX (8388607L)
#endif
#define int_least24_t int_least24_t
#endif

#ifndef int_least32_t
typedef signed long int int_least32_t;
#define int_least32_t int_least32_t
#define INT_LEAST32_MIN (-2147483648L)
#define INT_LEAST32_MAX (2147483647L)
#endif

#ifndef uint_least8_t
typedef unsigned char uint_least8_t;
#define uint_least8_t uint_least8_t
#define UINT_LEAST8_MAX (255)
#endif

#ifndef uint_least16_t
typedef unsigned int uint_least16_t;
#define uint_least16_t uint_least16_t
#define UINT_LEAST16_MAX (65535UL)
#endif

#ifndef uint_least24_t
#ifdef __CCI__
typedef unsigned long int uint_least24_t;
#define UINT_LEAST24_MAX (4294967295UL)
#else
typedef unsigned short long int uint_least24_t;
#define UINT_LEAST24_MAX (16777215UL)
#endif
#define uint_least24_t uint_least24_t
#endif

#ifndef uint_least32_t
typedef unsigned long int uint_least32_t;
#define uint_least32_t uint_least32_t
#define UINT_LEAST32_MAX (4294967295UL)
#endif


  /* types that are at least as wide and are usually the fastest */

#ifndef int_fast8_t
typedef signed char int_fast8_t;
#define int_fast8_t int_fast8_t
#define INT_FAST8_MIN (-128)
#define INT_FAST8_MAX (127)
#endif

#ifndef int_fast16_t
typedef signed int int_fast16_t;
#define int_fast16_t int_fast16_t
#define INT_FAST16_MIN (-32768)
#define INT_FAST16_MAX (32767)
#endif

#ifndef int_fast24_t
#ifdef __CCI__
typedef signed long int int_fast24_t;
#define INT_FAST24_MIN (-2147483648L)
#define INT_FAST24_MAX (2147483647L)
#else
typedef signed short long int int_fast24_t;
#define INT_FAST24_MIN (-8388608L)
#define INT_FAST24_MAX (8388607L)
#endif
#define int_fast24_t int_fast24_t
#endif

#ifndef int_fast32_t
typedef signed long int int_fast32_t;
#define int_fast32_t int_fast32_t
#define INT_FAST32_MIN (-2147483648L)
#define INT_FAST32_MAX (2147483647L)
#endif

#ifndef uint_fast8_t
typedef unsigned char uint_fast8_t;
#define uint_fast8_t uint_fast8_t
#define UINT_FAST8_MAX (255)
#endif

#ifndef uint_fast16_t
typedef unsigned int uint_fast16_t;
#define uint_fast16_t uint_fast16_t
#define UINT_FAST16_MAX (65535UL)
#endif

#ifndef uint_fast24_t
#ifdef __CCI__
typedef unsigned long int uint_fast24_t;
#define UINT_FAST24_MAX (4294967295UL)
#else
typedef unsigned short long int uint_fast24_t;
#define UINT_FAST24_MAX (16777215UL)
#endif
#define uint_fast24_t uint_fast24_t
#endif

#ifndef uint_fast32_t
typedef unsigned long int uint_fast32_t;
#define uint_fast32_t uint_fast32_t
#define UINT_FAST32_MAX (4294967295UL)
#endif

#ifndef intmax_t
typedef int32_t intmax_t;
#define intmax_t intmax_t
#endif

#ifndef uintmax_t
typedef uint32_t uintmax_t;
#define uintmax_t uintmax_t
#endif

#ifndef intptr_t
typedef int16_t	intptr_t;
#define intptr_t intptr_t
#endif

#ifndef uintptr_t
typedef uint16_t uintptr_t;
#define uintptr_t uintptr_t
#endif

#ifndef intmax_t
typedef int32_t intmax_t
#define intmax_t intmax_t
#endif

#ifndef uintmax_t
typedef uint32_t uintmax_t
#define uintmax_t uintmax_t
#endif

/**
  Section: Global Variables
 */

static uint8_t eusart2TxHead = 0;
static uint8_t eusart2TxTail = 0;
static uint8_t eusart2TxBuffer[EUSART2_TX_BUFFER_SIZE];
volatile uint8_t eusart2TxBufferRemaining;

static uint8_t eusart2RxHead = 0;
static uint8_t eusart2RxTail = 0;
static uint8_t eusart2RxBuffer[EUSART2_RX_BUFFER_SIZE];
volatile uint8_t eusart2RxCount;

/**
  Section: EUSART2 APIs
 */

void EUSART2_Initialize(void) {
    // disable interrupts before changing states
    PIE3bits.RC2IE = 0;
    PIE3bits.TX2IE = 0;

    // Set the EUSART2 module to the options selected in the user interface.

    // ABDEN disabled; WUE disabled; RCIDL idle; ABDOVF no_overflow; CKTXP async_noninverted_sync_fallingedge; BRG16 16bit_generator; DTRXP not_inverted; 
    BAUDCON2 = 0x48;

    // ADDEN disabled; RX9 8-bit; RX9D 0x0; FERR no_error; CREN enabled; SPEN enabled; SREN disabled; OERR no_error; 
    RCSTA2 = 0x90;

    // CSRC slave_mode; TRMT TSR_empty; TXEN enabled; BRGH hi_speed; SYNC asynchronous; SENDB sync_break_complete; TX9D 0x0; TX9 8-bit; 
    TXSTA2 = 0x26;

    // Baud Rate = 57600; SPBRGL 180; 
    SPBRG2 = 0xB4;

    // Baud Rate = 57600; SPBRGH 0; 
    SPBRGH2 = 0x00;


    // initializing the driver state
    eusart2TxHead = 0;
    eusart2TxTail = 0;
    eusart2TxBufferRemaining = sizeof (eusart2TxBuffer);

    eusart2RxHead = 0;
    eusart2RxTail = 0;
    eusart2RxCount = 0;

    // enable receive interrupt
    PIE3bits.RC2IE = 1;
}

uint8_t EUSART2_Read(void) {
    uint8_t readValue = 0;

    while (0 == eusart2RxCount) {
    }

    PIE3bits.RC2IE = 0;

    readValue = eusart2RxBuffer[eusart2RxTail++];
    if (sizeof (eusart2RxBuffer) <= eusart2RxTail) {
        eusart2RxTail = 0;
    }
    eusart2RxCount--;
    PIE3bits.RC2IE = 1;

    return readValue;
}

void EUSART2_Write(uint8_t txData) {
    while (0 == eusart2TxBufferRemaining) {
    }

    if (0 == PIE3bits.TX2IE) {
        TXREG2 = txData;
    } else {
        PIE3bits.TX2IE = 0;
        eusart2TxBuffer[eusart2TxHead++] = txData;
        if (sizeof (eusart2TxBuffer) <= eusart2TxHead) {
            eusart2TxHead = 0;
        }
        eusart2TxBufferRemaining--;
    }
    PIE3bits.TX2IE = 1;
}
/*
char getch(void) {
    return EUSART2_Read();
}

void putch(char txData) {
    EUSART2_Write(txData);
}
*/

void EUSART2_Transmit_ISR(void) {

    // add your EUSART2 interrupt custom code
    if (sizeof (eusart2TxBuffer) > eusart2TxBufferRemaining) {
        TXREG2 = eusart2TxBuffer[eusart2TxTail++];
        if (sizeof (eusart2TxBuffer) <= eusart2TxTail) {
            eusart2TxTail = 0;
        }
        eusart2TxBufferRemaining++;
    } else {
        PIE3bits.TX2IE = 0;
    }
}

void EUSART2_Receive_ISR(void) {
    if (1 == RCSTA2bits.OERR) {
        // EUSART2 error - restart

        RCSTA2bits.CREN = 0;
        RCSTA2bits.CREN = 1;
    }

    // buffer overruns are ignored
    eusart2RxBuffer[eusart2RxHead++] = RCREG2;
    if (sizeof (eusart2RxBuffer) <= eusart2RxHead) {
        eusart2RxHead = 0;
    }
    eusart2RxCount++;
}
/**
  End of File
 */
