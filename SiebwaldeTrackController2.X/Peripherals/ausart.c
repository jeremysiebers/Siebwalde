/**
  Section: Included Files
 */
#include "ausart.h"
#include <xc.h>
/**
  Section: Macro Declarations
 */
#define ausart_TX_BUFFER_SIZE 20
#define ausart_RX_BUFFER_SIZE 20

/**
  Section: Global Variables
 */

static uint8_t ausartTxHead = 0;
static uint8_t ausartTxTail = 0;
static uint8_t ausartTxBuffer[ausart_TX_BUFFER_SIZE];
volatile uint8_t ausartTxBufferRemaining;

static uint8_t ausartRxHead = 0;
static uint8_t ausartRxTail = 0;
static uint8_t ausartRxBuffer[ausart_RX_BUFFER_SIZE];
volatile uint8_t ausartRxCount;

/**
  Section: ausart APIs
 */

void ausart_Initialize(void) {
    // disable interrupts before changing states
    PIE1bits.RCIE = 0;
    PIE1bits.TXIE = 0;

    // Baud Rate = 19200; 
    SPBRG = 64;    

    
    RCSTAbits.SPEN = 1; // 1 = Serial port enabled (configures RC7/RX/DT and RC6/TX/CK pins as serial port pins)
    RCSTAbits.RX9  = 0; // 0 = Selects 8-bit reception
    RCSTAbits.SREN = 0; // 0 = Disables single receive
    RCSTAbits.CREN = 1; // 1 = Enables continuous receive
    RCSTAbits.ADDEN= 0; // 1 = Enables address detection, enables interrupt and load of the receive buffer when RSR<8> is set
        
    TXSTAbits.CSRC = 1; // Master mode
    TXSTAbits.TXEN = 1; // Transmit Enable bit (SREN/CREN overrides TXEN in Sync mode)
    TXSTAbits.SYNC = 0; // Asynchronous mode
    TXSTAbits.BRGH = 1; // Used in sync mode
    TXSTAbits.TX9  = 1; // Selects 9-bit transmission
    TXSTAbits.TX9D = 0; // 9th bit of transmit data
    

    // initializing the driver state
    ausartTxHead = 0;
    ausartTxTail = 0;
    ausartTxBufferRemaining = sizeof (ausartTxBuffer);

    ausartRxHead = 0;
    ausartRxTail = 0;
    ausartRxCount = 0;

    // enable/disable receive/transmit interrupt
    PIE1bits.RCIE = 1;
    PIE1bits.TXIE = 0;
}

/*
uint8_t ausart_Read(void) {
    uint8_t readValue = 0;

    if (0 == ausartRxCount) {
        readValue = 0;
    }
    else
    {
        PIE1bits.RC1IE = 0;

        readValue = ausartRxBuffer[ausartRxTail++];
        if (sizeof (ausartRxBuffer) <= ausartRxTail) {
            ausartRxTail = 0;
        }
        ausartRxCount--;
        PIE1bits.RC1IE = 1;
    }
    return readValue;
}
*/

uint8_t ausart_Read(void) {
    uint8_t readValue = 0;

    while (0 == ausartRxCount) {
    }

    PIE1bits.RCIE = 0;

    readValue = ausartRxBuffer[ausartRxTail++];
    if (sizeof (ausartRxBuffer) <= ausartRxTail) {
        ausartRxTail = 0;
    }
    ausartRxCount--;
    PIE1bits.RCIE = 1;

    return readValue;
}

void ausart_Write(uint8_t txData) {
    while (0 == ausartTxBufferRemaining) {
    }

    if (0 == PIE1bits.TXIE) {
        TXREG = txData;
    } else {
        PIE1bits.TXIE = 0;
        ausartTxBuffer[ausartTxHead++] = txData;
        if (sizeof (ausartTxBuffer) <= ausartTxHead) {
            ausartTxHead = 0;
        }
        ausartTxBufferRemaining--;
    }
    PIE1bits.TXIE = 1;
}

int _user_putc(char c)
 {
    ausart_Write(c); //(standard out for printf)    
 }

void ausart_Transmit_ISR(void) {

    // add your ausart interrupt custom code
    if (sizeof (ausartTxBuffer) > ausartTxBufferRemaining) {
        TXREG = ausartTxBuffer[ausartTxTail++];
        if (sizeof (ausartTxBuffer) <= ausartTxTail) {
            ausartTxTail = 0;
        }
        ausartTxBufferRemaining++;
    } else {
        PIE1bits.TXIE = 0;
    }
}

void ausart_Receive_ISR(void) {
    if (1 == RCSTAbits.OERR) {
        // ausart error - restart

        RCSTAbits.CREN = 0;
        RCSTAbits.CREN = 1;
    }

    // buffer overruns are ignored
    ausartRxBuffer[ausartRxHead++] = RCREG;
    if (sizeof (ausartRxBuffer) <= ausartRxHead) {
        ausartRxHead = 0;
    }
    ausartRxCount++;
}
/**
  End of File
 */
