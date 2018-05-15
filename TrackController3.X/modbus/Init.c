#include "General.h"
#include "Init.h"

#define DIVIDER ((int)(FOSC/(4UL * BAUD_RATE) -1))

void InitUART(void)                                                             // using UART2 for modbus
{
/*****************************UART initialize*********************************/
    // disable interrupts before changing states
    PIE3bits.RC2IE = 0;
    PIE3bits.TX2IE = 0;

    SPBRG2  = DIVIDER;
    SPBRGH2 = 0;
    
    BAUDCON2bits.RXDTP  = 0;                                                    // No inversion of receive data (RXx). Idle state is a high level.
    BAUDCON2bits.TXCKP  = 0;                                                    // No inversion of transmit data (TXx); Idle state is a high level
    BAUDCON2bits.BRG16  = 1;                                                    // 16-bit Baud Rate Generator
    BAUDCON2bits.WUE    = 0;                                                    // RXx pin not monitored or rising edge detected
    BAUDCONbits.ABDEN   = 0;                                                    // Baud rate measurement disabled or completed
            
    RCSTA2bits.SPEN = 1; // 1 = Serial port enabled (configures RC7/RX/DT and RC6/TX/CK pins as serial port pins)
    RCSTA2bits.RX9  = 0; // 0 = Selects 8-bit reception
    RCSTA2bits.SREN = 0; // 0 = Disables single receive
    RCSTA2bits.CREN = 1; // 1 = Enables continuous receive until enable bit CREN is cleared (CREN overrides SREN)
    RCSTA2bits.ADDEN= 0; // 1 = Enables address detection, enables interrupt and load of the receive buffer when RSR<8> is set
        
    TXSTA2bits.CSRC = 0; // Master mode when 1 (in sync mode))
    TXSTA2bits.TXEN = 1; // Transmit Enable bit (SREN/CREN overrides TXEN in Sync mode)
    TXSTA2bits.SYNC = 0; // Asynchronous mode
    TXSTA2bits.BRGH = 1; // High Baudrate select bit
    
    PIE3bits.TX2IE  = 0;
    PIR3bits.TX2IF  = 0;
    PIE3bits.RC2IE  = 1;   
    
/**************************End Of UART2 initialize*****************************/
}

void InitTMR(void)
{
    /*Timer3 for slave data receive timeout*/
    T3CONbits.RD16      = 0;                                                    // Enables register read/write of Timer3 in two 8-bit operations
    T3CONbits.T3CCP1    = 0;
    T3CONbits.T3CCP2    = 0;    
    T3CONbits.T3CKPS    = 0b11;                                                 // 1:8 Prescale value
    T3CONbits.TMR3CS    = 0;                                                    // Internal clock (FOSC/4)
    TMR3H = 0;
    TMR3L = 0;
    IPR2bits.TMR3IP     = 0;                                                    // Low priority
    PIE2bits.TMR3IE     = 1;                                                    // Enable interrupt
    T3CONbits.TMR3ON    = 1;
        
    /*Timer4 for slave answer timeout*/
    T4CONbits.T4OUTPS   = 0b1111;                                               // Timer4 Output Postscale Select bits
    T4CONbits.T4CKPS    = 0b11;                                                 // Prescaler is 16 (Internal clock (FOSC/4))
    PR4                 = 0xFF;                                                 // 8-Bit Period register (interrupt on match with TMR4)
    TMR4                = 0x00;                                                 // 8-Bit Timer register
    IPR3bits.TMR4IP     = 0;                                                    // Low priority
    PIE3bits.TMR4IE     = 1;                                                    // Enable interrupt
    T4CONbits.TMR4ON    = 1;
}