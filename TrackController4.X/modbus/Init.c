#include "General.h"
#include "Init.h"

#define DIVIDER ((int)(FOSC/(16UL * BAUD_RATE) -1))

void InitUART(void)
{
/*****************************UART1 initialize*********************************/
    // disable interrupts before changing states
    PIE1bits.RCIE = 0;
    PIE1bits.TXIE = 0;

    SPBRG = DIVIDER;
    
    RCSTAbits.SPEN = 1; // 1 = Serial port enabled (configures RC7/RX/DT and RC6/TX/CK pins as serial port pins)
    RCSTAbits.RX9  = 0; // 0 = Selects 8-bit reception
    RCSTAbits.SREN = 0; // 0 = Disables single receive
    RCSTAbits.CREN = 1; // 1 = Enables continuous receive until enable bit CREN is cleared (CREN overrides SREN)
    RCSTAbits.ADDEN= 0; // 1 = Enables address detection, enables interrupt and load of the receive buffer when RSR<8> is set
        
    TXSTAbits.CSRC = 0; // Master mode when 1 (in sync mode))
    TXSTAbits.TXEN = 1; // Transmit Enable bit (SREN/CREN overrides TXEN in Sync mode)
    TXSTAbits.SYNC = 0; // Asynchronous mode
    TXSTAbits.BRGH = 1; // High Baudrate select bit
    
    PIE1bits.TXIE = 0;
    PIR1bits.RCIF = 0;
    PIE1bits.RCIE = 1;   
    
/**************************End Of UART1 initialize*****************************/
}

void InitTMR1(void)
{    
    // TMR0 Prescaler (default assigned)
    OPTION_REGbits.PS = 0b011; //1:16               0b111;//1:128
    
    OPTION_REGbits.T0CS = 0;
    
    OPTION_REGbits.T0SE = 0;
    
    OPTION_REGbits.PSA = 0;
    
    // Clearing IF flag.
    INTCONbits.TMR0IF = 0;
    
    // TMR0 0x0; 
    TMR0 = 0x00;
    
    // Enable interrupt
    INTCONbits.TMR0IE = 1;
}