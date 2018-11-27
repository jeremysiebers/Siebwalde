#include "General.h"
// CONFIG1H
#pragma config OSC = HSPLL      // Oscillator Selection bits (HS oscillator, PLL enabled (clock frequency = 4 x FOSC1))
#pragma config FCMEN = ON       // Fail-Safe Clock Monitor Enable bit (Fail-Safe Clock Monitor enabled)
#pragma config IESO = ON        // Internal External Oscillator Switchover bit (Internal External Switchover mode enabled)

// CONFIG2L
#pragma config PWRTEN = ON      // Power-up Timer Enable bit (PWRT enabled)
#pragma config BOREN = ON       // Brown-out Reset Enable bits (Brown-out Reset enabled)
// BORV = No Setting

// CONFIG2H
#pragma config WDTEN = OFF      // Watchdog Timer Enable bit (WDT disabled (control is placed on the SWDTEN bit))
#pragma config WDPS = 32768     // Watchdog Timer Postscale Select bits (1:32768)
#pragma config WINEN = ON       // Watchdog Timer Window Enable bit (WDT window enabledbled)

// CONFIG3L
#pragma config PWMPIN = OFF     // PWM output pins Reset state control (PWM outputs disabled upon Reset (default))
#pragma config LPOL = HIGH      // Low-Side Transistors Polarity (PWM0, 2, 4 and 6 are active-high)
#pragma config HPOL = HIGH      // High-Side Transistors Polarity (PWM1, 3, 5 and 7 are active-high)
#pragma config T1OSCMX = OFF    // Timer1 Oscillator MUX (Standard (legacy) Timer1 oscillator operation)

// CONFIG3H
#pragma config MCLRE = ON       // MCLR Pin Enable bit (Enabled)

// CONFIG4L
#pragma config STVREN = ON      // Stack Full/Underflow Reset Enable bit (Stack full/underflow will cause Reset)
#pragma config LVP = OFF        // Low-Voltage ICSP Enable bit (Low-voltage ICSP disabled)

// CONFIG5L
#pragma config CP0 = ON         // Code Protection bit (Block 0 (000200-000FFFh) code-protected)
#pragma config CP1 = ON         // Code Protection bit (Block 1 (001000-001FFF) code-protected)
#pragma config CP2 = ON         // Code Protection bit (Block 2 (002000-002FFFh) code-protected)
#pragma config CP3 = ON         // Code Protection bit (Block 3 (003000-003FFFh) code-protected)

// CONFIG5H
#pragma config CPB = ON         // Boot Block Code Protection bit (Boot Block (000000-0001FFh) code-protected)
#pragma config CPD = ON         // Data EEPROM Code Protection bit (Data EEPROM code-protected)

// CONFIG6L
#pragma config WRT0 = ON        // Write Protection bit (Block 0 (000200-000FFFh) write-protected)
#pragma config WRT1 = ON        // Write Protection bit (Block 1 (001000-001FFF) write-protected)
#pragma config WRT2 = ON        // Write Protection bit (Block 2 (002000-002FFFh) write-protected)
#pragma config WRT3 = ON        // Write Protection bit (Block 3 (003000-003FFFh) write-protected)

// CONFIG6H
#pragma config WRTC = ON        // Configuration Register Write Protection bit (Configuration registers (300000-3000FFh) write-protected)
#pragma config WRTB = ON        // Boot Block Write Protection bit (Boot Block (000000-0001FFh) write-protected)
#pragma config WRTD = ON        // Data EEPROM Write Protection bit (Data EEPROM write-protected)

// CONFIG7L
#pragma config EBTR0 = ON       // Table Read Protection bit (Block 0 (000200-000FFFh) protected from table reads executed in other blocks)
#pragma config EBTR1 = ON       // Table Read Protection bit (Block 1 (001000-001FFF) protected from table reads executed in other blocks)
#pragma config EBTR2 = ON       // Table Read Protection bit (Block 2 (002000-002FFFh) protected from table reads executed in other blocks)
#pragma config EBTR3 = ON       // Table Read Protection bit (Block 3 (003000-003FFFh) protected from table reads executed in other blocks)

// CONFIG7H
#pragma config EBTRB = ON       // Boot Block Table Read Protection bit (Boot Block (000000-0001FFh) not protected from table reads executed in other blocks)

void InitOSC(void)
{
    OSCCONbits.SCS  = 0b00;                                                     // Primary OSC selected
    
    while(OSCCONbits.OSTS == 0);
}

void InitIO(void)
{
    LATA                =0x0000;
    TRISA               =0x0007;

    LATB                =0x0000;
    TRISB               =0x0000;
    
    LATC                =0x0000;
    TRISC               =0x0081;
}

#define DIVIDER ((int)(FOSC/(16UL * BAUD_RATE) -1))

void InitUART(void)
{
/*****************************UART1 initialize*********************************/
    SPBRG   = DIVIDER;     	
    TXSTA   = 0x24;
    RCSTA   = 0x90;
    TRISC6  = 0;
    TRISC7  = 1;
    
    RCIF    = 0;
    RCIE    = 1;            // Alim kesmesi aciliyor
/**************************End Of UART1 initialize*****************************/
}

void InitTMR1(void)
{
    T1CONbits.RD16      =1;                                                     // 8 bit tanimlaniyor
    T1CONbits.T1CKPS    =0b11;                                                  // 1:8
    T1CONbits.TMR1CS    =0;                                                     // Fosc/4
    TMR1H               =0;
    TMR1L               =0;
    
    TMR1IF              =0;
    TMR1IE              =1;
    
    T1CONbits.TMR1ON    =1;
}
