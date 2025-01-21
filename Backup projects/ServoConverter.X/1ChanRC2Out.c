/*
 * File:   main.c
 * Author: Jeremy Siebers
 *
 * Created on January 02, 2025, 9:36 AM
 */
//#include <pic12f675.h>
#include <xc.h>
#include "Main.h"

// CONFIG
#pragma config FOSC = INTRCIO   // Oscillator Selection bits (INTOSC oscillator: I/O function on GP4/OSC2/CLKOUT pin, I/O function on GP5/OSC1/CLKIN)
#pragma config WDTE = ON        // Watchdog Timer Enable bit (WDT enabled)
#pragma config PWRTE = ON       // Power-Up Timer Enable bit (PWRT enabled)
#pragma config MCLRE = OFF      // GP3/MCLR pin function select (GP3/MCLR pin function is digital I/O, MCLR internally tied to VDD)
#pragma config BOREN = ON       // Brown-out Detect Enable bit (BOD enabled)
#pragma config CP = OFF         // Code Protection bit (Program Memory code protection is disabled)
#pragma config CPD = OFF        // Data Code Protection bit (Data memory code protection is disabled)


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//DECLARATIONS//

#define Init_IO()						TRISIO = 0b011000;  // Setting in and outputs
#define Out2Led  						GP0		// 
#define Out1Led                         GP1     // 
#define Out1                            GP2     // Pulse output
#define In1                             GP3     // RC input
#define In2             				GP4		// RC input
#define Out2    						GP5		// Pulse output

//OSCAL 0x34B4

static void Init_Timers(void); // Initialize Timers (0)

uint8_t Input1Count = 0;
uint8_t Input2Count = 0;

const uint8_t Servo11ms = 20; // no of counts equal to 1.1ms
const uint8_t Servo15ms = 28; // no of counts equal to 1.5ms
const uint8_t Servo19ms = 36; // no of counts equal to 1.9ms

//MAIN ROUTINE/////////////////////////////////////////////////////////////////////////////////////////

void main(void)
{
    // Hardware Initialization
    TRISIO = 0xFF; // All IO are inputs to be safe
    GPIO = 0x00; // All IO to 0
#ifdef _12F675
    ANSEL = 0x00; // Turn off ADC (PIC12F675))
#endif
    CMCON = 7; // Turn off Comparators
    Init_Timers(); // Initialize Timers
    Init_IO(); // Initialize In-and-Outputs    
    T1IF = Off; // Clear Timer 1 interrupt flag
    TMR1ON = Off;
    T0IE = Off;
    
    while (On) // Infinite loop
    {
        CLRWDT();
        
        if(T0IE == Off && TMR1ON == Off){
            if(In1){
                T0IE = On;
                TMR1ON = On;
                TMR1H = 0xE0;
                TMR1L = 0x00;
            }
        }
        else{
            if(!In1){
                T0IE = Off;
            }
        }
    }
}



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Interrupt routines//

void __interrupt() isr(void) // Here the interrupt function
{
    // Timer 1 interrupt measurement time (check first to prevent timer 0 from firing again)
    if (T1IF){
        TMR1ON = Off;
        T0IE = Off;
        
        //Out1 = Off;
        if(Input1Count < Servo11ms){
            Out1 = Out1Led = On;
            Out2 = Out2Led = Off;
        }
        if(Input1Count > Servo19ms){
            Out2 = Out2Led = On;
            Out1 = Out1Led = Off;
        }
        else{
            Out1 = Out1Led = Off;
            Out2 = Out2Led = Off;
        }
//        if(Input2Count > Servo15ms){
//            Out2 = Out2Led =On;
//        }
//        else{
//            Out2 = Out2Led = Off;
//        }
        Input1Count = 0;
//        Input2Count = 0;
        T1IF = 0;
        T0IF = 0;
    }
    
    // Timer 0 sample freq = 9523 Hz
    if (T0IF)
    {
        if(In1){
            Input1Count++;
        }
//        if(In2){
//            Input2Count++;
//        }        
        TMR0 = 0xF0;
        //Out2 = !Out2; // 38 pulses per 2ms rc servo pulse, 28 pulses == 1.5ms
        T0IF = 0;
    }
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Timer setup//

static void Init_Timers(void)
{
    // Timer 0 OPTION_REG
    PS0 = 0;
    PS1 = 0;
    PS2 = 0;
    PSA = 0;
    T0SE = 0;
    T0CS = 0;
    INTEDG = 0;
    nGPPU = 1;

    // INTCON Interrupt reg
    GPIF = 0;
    INTF = 0;
    T0IF = 0;
    GPIE = 0;
    INTE = 0;
    T0IE = 0; //Timer 0 enable Interrupt
    PEIE = 1;
    GIE = 1;

    // PIE1 Peripheral interrupt enable register
    TMR1IE = 1;  //Timer 1 enable Interrupt
    CMIE = 0;
    ADIE = 0;
    EEIE = 0;

    WPU = 0x00; // Weak pull up only on input pin GP5
    // Interrupt on change pins
    IOC = 0;

    // timer 1 T1CON
    TMR1GE = 0;
    T1CKPS0 = 0;
    T1CKPS1 = 0;
    T1OSCEN = 0;
    TMR1CS = 0;
    TMR1ON = 0;
    TMR1IF = Off;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//MAIN SUBROUTINES//