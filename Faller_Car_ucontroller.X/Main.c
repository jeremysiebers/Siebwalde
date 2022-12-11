/*
 * File:   main.c
 * Author: Jeremy
 *
 * Created on December 10, 2012, 9:36 PM
 */
//#include <pic12f675.h>
#include <xc.h>
#include "Main.h"

// CONFIG
#pragma config FOSC = INTRCIO   // Oscillator Selection bits (INTOSC oscillator: I/O function on GP4/OSC2/CLKOUT pin, I/O function on GP5/OSC1/CLKIN)
#pragma config WDTE = OFF        // Watchdog Timer Enable bit (WDT enabled)
#pragma config PWRTE = OFF       // Power-Up Timer Enable bit (PWRT enabled)
#pragma config MCLRE = ON      // GP3/MCLR pin function select (GP3/MCLR pin function is digital I/O, MCLR internally tied to VDD)
#pragma config BOREN = OFF       // Brown-out Detect Enable bit (BOD enabled)
#pragma config CP = OFF         // Code Protection bit (Program Memory code protection is disabled)
#pragma config CPD = OFF        // Data Code Protection bit (Data memory code protection is disabled)


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//DECLARATIONS//

#define ServoOut  						GP0		// Motor PWM, output
#define REF                             GP1     // REF Volt 2.5V, analog input
#define BATT                            GP2     // Battery voltage, analog input
#define RDC                             GP3     // Reed contact (input only), input
#define FLED     						GP4		// Blue toggling flashing leds (PWM controlled), output
#define BRKLED          				GP5		// Tail light / Brake LED (PWM controlled), output


//OSCAL 0x34B4

static void Init_Timers(void);  // Initialize Timers (0)
static void Init_IO(void);      // Init IO
static void Init_ADC(void);     // init ADC

uint16_t Pulses = 0;                        // Pulses counter to generate 50 Hz servo pulse
uint16_t Update_Tick = 0;
uint16_t Output_Enable_Delay = 0;

//MAIN ROUTINE/////////////////////////////////////////////////////////////////////////////////////////

void main(void)
{
    // Hardware Initialization
    TRISIO = 0xFF; // All IO are inputs to be safe
    GPIO = 0x00; // All IO to 0
    ANSEL = 0x00; // Turn off ADC
    CMCON = 7; // Turn off Comparators
    
    Init_Timers();  // Initialize Timers (TMR0)
    Init_IO();      // Initialize In-and-Outputs
    Init_ADC();     // Initialize ADC
    
    uint8_t i = 0;
    uint16_t result = 0;
    uint16_t calc = 0;

    while (On) // Infinite loop
    {
        switch(i)
        {
            case 0  :
                ADCON0bits.GO_DONE = 1;
                i++;
                break;
                
            case 1  :
                if (PIR1bits.ADIF){
                    result = (uint16_t)((ADRESH << 8) | ADRESL);
                    i++;
                }
                break;
                
            case 2  :
                calc = 0x400 - result;
                calc = calc * 0xF;
                calc = 0xFB00 - calc; //(0xFB00 - ((0x400 - result)*0xF));
                TMR1H = (uint8_t)(calc >>8);
                TMR1L = (uint8_t)calc;
                i = 0;
                break;
                
            default: i = 0;
                break;
        }
    }
}



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Interrupt routines//

//__interrupt(void)// Here the interrupt function
void __interrupt() isr(void) 
{

    if (T0IF) // If Timer 0 interrupt
    {
        ServoOut = On;
        TMR1ON = On;
        T0IF = Off; // Clear Timer 0 interrupt flag
    }

    if (TMR1IF) // If Timer 0 interrupt
    {
        ServoOut = Off;
        TMR1ON = Off;        
        TMR1IF = Off;
    }

}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Init IO

static void Init_IO(void)
{
    TRISIObits.TRISIO0 = Output;    //Motor PWM, output
    TRISIObits.TRISIO1 = Input;     //REF Volt 2.5V, analog input
    TRISIObits.TRISIO2 = Input;     //Battery voltage, analog input
    TRISIObits.TRISIO3 = Input;     //Reed contact (input only), input
    TRISIObits.TRISIO4 = Output;    //Blue toggling flashing leds (PWM controlled), output
    TRISIObits.TRISIO5 = Output;    //Tail light / Brake LED (PWM controlled), output
}

// Setup ADC

static void Init_ADC(void)
{
    // GP1 and GP2 are analog input
    ANSELbits.ANS0 = Off;
    ANSELbits.ANS1 = On;
    ANSELbits.ANS2 = On;
    ANSELbits.ANS3 = Off;
    // FOSC/4
    ANSELbits.ADCS0 = 1;
    ANSELbits.ADCS1 = 0;
    ANSELbits.ADCS2 = 0;
    // Right justified + VREF 
    ADCON0bits.ADFM = 1;
    ADCON0bits.VCFG = 1;
    // Select AN2 to be sampled (Batt))
    ADCON0bits.CHS0 = 0;
    ADCON0bits.CHS1 = 1;
}

//Timer setup//

static void Init_Timers(void)
{
    // TMR0 prescaler
    OPTION_REGbits.PS0      = 0;
    OPTION_REGbits.PS1      = 0;
    OPTION_REGbits.PS2      = 1; // 1:32 --> (FOSC/4) /32 /256 = ~134Hz(measured)
    
    OPTION_REGbits.PSA      = 0;
    OPTION_REGbits.T0SE     = 0;
    OPTION_REGbits.T0CS     = 0;
    OPTION_REGbits.INTEDG   = 0;
    OPTION_REGbits.nGPPU    = 1; // GPIO Pull-ups disabled

    INTCONbits.GPIF         = 0;
    INTCONbits.INTF         = 0;
    INTCONbits.T0IF         = 0;
    INTCONbits.GPIE         = 0;
    INTCONbits.INTE         = 0;
    INTCONbits.T0IE         = 1; // TMR0 interrupt enable
    INTCONbits.PEIE         = 1; // Peripheral interrupt enable
    INTCONbits.GIE          = 1; // Global interrupt enable
        
    PIE1bits.TMR1IE         = 1; // TMR1 interrupt enable
    PIE1bits.CMIE           = 0;
    PIE1bits.ADIE           = 0;
    PIE1bits.EEIE           = 0;

    WPU = 0x00; // Weak pull up

    IOC = 0; // Interrupt On Change disabled on all pins

    // timer 1

    T1CONbits.TMR1GE    = 0;
    // TMR1 prescaler setting
    T1CONbits.T1CKPS0   = 0; // 1:1 --> (FOSC/4) /1 /65536
    T1CONbits.T1CKPS1   = 0;
    // 16bit requires 59.7ms. TMR0 runs on 7.45ms --> 
    // 100% duty with TMR1 = 0xE040
    //  50% duty with TMR1 = 0xF000
    //   0% duty with TMR1 = 0xFFFF 
    
    T1CONbits.T1OSCEN   = 0;
    T1CONbits.TMR1CS    = 0;   // Internal clock FOSC/4
    T1CONbits.TMR1ON    = Off;
    TMR1H = 0xFB;
    TMR1L = 0x00;
    PIR1bits.TMR1IF = Off; // Clear Timer1 interrupt flag
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//MAIN SUBROUTINES//