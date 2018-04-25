#include <xc.h>
#include "INT0.h"
#include "config.h"

/**
  Section: Global Variables Definitions
 */


/**
  Section: INT0 APIs
 */

void INT0_Initialize(void){
    // INTEDGx bit in the INTCON2 register is set (= 1), the interrupt is triggered by a rising edge;
    OPTION_REG = 0;
    OPTION_REGbits.INTEDG = 1;
    
    // Enable INT0 interrupt
    INTCONbits.INT0IE = 1;
}

void INT0_ISR(void) {

    //PORTCbits.RC1 = On;
    
    T2CONbits.TMR2ON = Off;
    TRISC = 0x04;
    CCPR1L = ((0 & 0x03FC) >> 2);
    CCP1CON = (CCP1CON & 0xCF) | ((0 & 0x0003) << 4);
    
    TMR2 = 0;
    PORTCbits.RC2 = Off;
    
    while (PORTBbits.RB0 == 1){
        PORTCbits.RC1 = On;
    }    
    
    PORTCbits.RC1 = Off;   
    
    CCPR1L = ((208 & 0x03FC) >> 2);
    CCP1CON = (CCP1CON & 0xCF) | ((208 & 0x0003) << 4);
    TRISC = 0x00;
    T2CONbits.TMR2ON = 1;
    
    //PORTCbits.RC3 = !PORTCbits.RC3;
    INTCONbits.INT0IF = 0;        
}
/**
  End of File
 */