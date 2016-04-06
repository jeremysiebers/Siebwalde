/**
  EUSART1 Generated Driver File

  @Company
    Microchip Technology Inc.

  @File Name
    eusart1.c

  @Summary
    This is the generated driver implementation file for the EUSART1 driver using MPLAB® Code Configurator

  @Description
    This header file provides implementations for driver APIs for EUSART1.
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
#include "uart1.h"
#include <xc.h>

/**
  Section: Macro Declarations
 */
#define FP 60000000
#define BAUDRATE 57600
#define BRGVAL ((FP/BAUDRATE)/16)-1
#define DELAY_105uS asm volatile ("REPEAT, #4201"); Nop(); // 105uS delay

#define EUSART1_TX_BUFFER_SIZE 20
#define EUSART1_RX_BUFFER_SIZE 126

/**
  Section: Global Variables
 */

static uint8_t eusart1TxHead = 0;
static uint8_t eusart1TxTail = 0;
static uint8_t eusart1TxBuffer[EUSART1_TX_BUFFER_SIZE];
volatile uint8_t eusart1TxBufferRemaining;

static uint8_t eusart1RxHead = 0;
static uint8_t eusart1RxTail = 0;
static uint8_t eusart1RxBuffer[EUSART1_RX_BUFFER_SIZE];
volatile uint8_t eusart1RxCount;

/**
  Section: EUSART1 APIs
 */

void EUSART1_Initialize(void) {
    
    U1MODEbits.STSEL = 0; // 1-Stop bit
    U1MODEbits.PDSEL = 0; // No Parity, 8-Data bits
    U1MODEbits.ABAUD = 0; // Auto-Baud disabled
    U1MODEbits.BRGH = 0; // Standard-Speed mode
    U1BRG = BRGVAL; // Baud Rate setting for 57600
    U1STAbits.UTXISEL0 = 0; // Interrupt after one TX character is transmitted
    U1STAbits.URXISEL0 = 0; // Interrupt after one RX character is received;
    U1STAbits.UTXISEL1 = 0;
    U1STAbits.URXISEL1 = 0;
    IEC0bits.U1TXIE = 1; // Enable UART TX interrupt
    IEC0bits.U1RXIE = 1; // Enable UART RX interrupt
    U1MODEbits.UARTEN = 1; // Enable UART
    U1STAbits.UTXEN = 1; // Enable UART TX
        
    /* Wait at least 105 microseconds (1/9600) before sending first char */
    DELAY_105uS


    // initializing the driver state
    eusart1TxHead = 0;
    eusart1TxTail = 0;
    eusart1TxBufferRemaining = sizeof (eusart1TxBuffer);

    eusart1RxHead = 0;
    eusart1RxTail = 0;
    eusart1RxCount = 0;
}

uint8_t EUSART1_Read(void) {
    uint8_t readValue = 0;

    while (0 == eusart1RxCount) {
    }

    IEC0bits.U1RXIE = 0;

    readValue = eusart1RxBuffer[eusart1RxTail++];
    if (sizeof (eusart1RxBuffer) <= eusart1RxTail) {
        eusart1RxTail = 0;
    }
    eusart1RxCount--;
    IEC0bits.U1RXIE = 1;

    return readValue;
}

void EUSART1_Write(uint8_t txData) {
    while (0 == eusart1TxBufferRemaining) {
    }

    if (0 == IEC0bits.U1TXIE) {
        U1TXREG = txData;
    } else {
        IEC0bits.U1TXIE = 0;
        eusart1TxBuffer[eusart1TxHead++] = txData;
        if (sizeof (eusart1TxBuffer) <= eusart1TxHead) {
            eusart1TxHead = 0;
        }
        eusart1TxBufferRemaining--;
    }
    IEC0bits.U1TXIE = 1;
}

void EUSART1_Transmit_ISR(void) {

    // add your EUSART1 interrupt custom code
    if (sizeof (eusart1TxBuffer) > eusart1TxBufferRemaining) {
        U1TXREG = eusart1TxBuffer[eusart1TxTail++];
        if (sizeof (eusart1TxBuffer) <= eusart1TxTail) {
            eusart1TxTail = 0;
        }
        eusart1TxBufferRemaining++;
    } else {
        IEC0bits.U1TXIE = 0;
    }
}

void EUSART1_Receive_ISR(void) {
    if (1 == U1STAbits.OERR) {
        // EUSART1 error - restart

        U1STAbits.OERR = 0;        
    }

    /*
     from datasheet:
     * if(U1STAbits.URXDA == 1)
        {
        ReceivedChar = U1RXREG;
        }
     */
    // buffer overruns are ignored
    eusart1RxBuffer[eusart1RxHead++] = U1RXREG;
    if (sizeof (eusart1RxBuffer) <= eusart1RxHead) {
        eusart1RxHead = 0;
    }
    eusart1RxCount++;
}
/**
  End of File
 */
