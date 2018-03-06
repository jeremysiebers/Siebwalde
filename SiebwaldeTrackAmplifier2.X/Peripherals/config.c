// PIC16F737 Configuration Bit Settings

// 'C' source line config statements

#include <xc.h>
#include "config.h"

// #pragma config statements should precede project file includes.
// Use project enums instead of #define for ON and OFF.

// CONFIG1
#pragma config FOSC = EC        // Oscillator Selection bits (EXTCLK; port I/O function on OSC2/CLKO/RA6)
#pragma config WDTE = OFF       // Watchdog Timer Enable bit (WDT disabled)
#pragma config PWRTE = OFF      // Power-up Timer Enable bit (PWRT enabled)
#pragma config MCLRE = ON       // MCLR/VPP/RE3 Pin Function Select bit (MCLR/VPP/RE3 pin function is MCLR)
#pragma config BOREN = OFF      // Brown-out Reset Enable bit (Disabled)
#pragma config BORV = 20        // Brown-out Reset Voltage bits (VBOR set to 2.0V)
//#pragma config DEBUG = ON       // Set ICD
#pragma config CCP2MX = RC1     // CCP2 Multiplex bit (CCP2 is on RC1)
#pragma config CP = OFF         // Flash Program Memory Code Protection bits (Code protection off)

// CONFIG2
#pragma config FCMEN = OFF      // Fail-Safe Clock Monitor Enable bit (Fail-Safe Clock Monitor disabled)
#pragma config IESO = OFF       // Internal External Switchover bit (Internal External Switchover mode disabled)
#pragma config BORSEN = OFF     // Brown-out Reset Software Enable bit (Disabled)

void SYSTEM_Initialize(void) {
    //OSCILLATOR_Initialize();
    //PIN_MANAGER_Initialize();
    //TMR0_Initialize();
    //TMR2_Initialize();
    //PWM_Initialize();
    //ausart_Initialize();
    //INT0_Initialize();
    //INTERRUPT_Initialize();
    
}

void OSCILLATOR_Initialize(void) {
    // SCS FOSC; IDLEN disabled; IRCF 8MHz_HFINTOSC/4; 
    //OSCCON = 0x70;
    OSCCON = 0x00;
    
    // INTSRC disabled; PLLEN disabled; TUN 0x00; 
    //OSCTUNE = 0x00;
    // Set the secondary oscillator

}