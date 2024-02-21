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
#pragma config WDTE = ON        // Watchdog Timer Enable bit (WDT enabled)
#pragma config PWRTE = ON       // Power-Up Timer Enable bit (PWRT enabled)
#pragma config MCLRE = OFF      // GP3/MCLR pin function select (GP3/MCLR pin function is digital I/O, MCLR internally tied to VDD)
#pragma config BOREN = ON       // Brown-out Detect Enable bit (BOD enabled)
#pragma config CP = OFF         // Code Protection bit (Program Memory code protection is disabled)
#pragma config CPD = OFF        // Data Code Protection bit (Data memory code protection is disabled)


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//DECLARATIONS//

#define Init_IO()						TRISIO = 0x14;  //0x10;	// Setting in and outputs

#define SeinStraight                    GP1     // SeinStraight Led output
#define SeinBend						GP0		// SeinBend Led output
#define ServoOut                        GP2     // Servo pulse output
#define SwMiddle                        GP3     // Spare
#define SwInput  						GP4		// Switch input to drive servo to the right or the left
#define Heart_Pol        				GP5		// Heart polaization relais output

const uint16_t SERVO_MIDDLE   =         0xF9F0;        // 1.5ms is also used for switching Heart_Pol

#define SERVO_RIGHT                                              (SERVO_MIDDLE + SERVO_DIST)
#define SERVO_LEFT                                               (SERVO_MIDDLE - SERVO_DIST)

#define SERVO_DIST                                                200//70

//#define SERVO_RIGHT                                             0xFD40          // 1ms
//#define SERVO_MIDDLE                                            0xFBD7          // 1.5ms
//#define SERVO_LEFT                                              0xFA60          // 2.0ms

//OSCAL 0x34B4

static void Init_Timers(void); // Initialize Timers (0)

unsigned int Pulses = 0;                        // Pulses counter to generate 50 Hz servo pulse
unsigned int Servo_Pos = SERVO_LEFT;
unsigned int Update_Tick = 0;
unsigned int Output_Enable_Delay = 0;

//MAIN ROUTINE/////////////////////////////////////////////////////////////////////////////////////////

void main(void)
{
    // Hardware Initialization
    TRISIO = 0xFF; // All IO are inputs to be safe
    GPIO = 0x00; // All IO to 0
//    ANSEL = 0x00; // Turn off ADC
    CMCON = 7; // Turn off Comparators

    Init_Timers(); // Initialize Timers (TMR0)

    Init_IO(); // Initialize In-and-Outputs

    while (On) // Infinite loop
    {
        CLRWDT();
    }
}



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Interrupt routines//

void __interrupt() isr(void) // Here the interrupt function
{

    if (T0IF) // If Timer 0 interrupt
    {
        if (Output_Enable_Delay < 500 && Output_Enable_Delay < 550)
        {
            Output_Enable_Delay++;
        }
        else
        {
            Output_Enable_Delay = 600;
            TRISIObits.TRISIO2 = 0x0;
        }

        //Update_Tick++;

        //if (Update_Tick > 0)
        //{
        //    Update_Tick = 1;

            if ((SwInput == Off) && (SwMiddle == Off) && (Servo_Pos < SERVO_RIGHT))
            {
            Servo_Pos = Servo_Pos + 10;
            }
            else if ((SwInput == On) && (SwMiddle == Off) && (Servo_Pos > SERVO_LEFT))
            {
                Servo_Pos = Servo_Pos - 10;
            }
            else if (SwMiddle == On)
            {
                Servo_Pos = SERVO_MIDDLE;
            }

        //}

        if ((Servo_Pos < (SERVO_RIGHT - 1)) && (Servo_Pos > (SERVO_LEFT + 1)))
        {
            SeinStraight = Off;
            SeinBend = Off;
        }

        else if (Servo_Pos >= SERVO_RIGHT)
        {
            SeinStraight = On;
            SeinBend = Off;
        }

        else if (Servo_Pos <= SERVO_LEFT)
        {
            SeinStraight = Off;
            SeinBend = On;
        }

        if (Servo_Pos > SERVO_MIDDLE)
        {
            Heart_Pol = Off;
        }
        else if (Servo_Pos < SERVO_MIDDLE)
        {
            Heart_Pol = On;
        }

        TMR1H = Servo_Pos >> 8;
        TMR1L = (uint8_t)Servo_Pos;
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
//Timer setup//

static void Init_Timers(void)
{

    PS0 = 0;
    PS1 = 0;
    PS2 = 1;
    PSA = 0;
    T0SE = 0;
    T0CS = 0;
    INTEDG = 1;
    nGPPU = 1;

    GPIF = 0;
    INTF = 0;
    T0IF = 0;
    GPIE = 0;
    INTE = 0;
    T0IE = 1;
    PEIE = 1;
    GIE = 1;

    TMR1IE = 1;
    CMIE = 0;
    ADIE = 0;
    EEIE = 0;

    WPU = 0x00; // Weak pull up only on input pin GP5

    IOC = 0;

    // timer 1

    TMR1GE = 0;
    T1CKPS0 = 0;
    T1CKPS1 = 0;
    T1OSCEN = 0;
    TMR1CS = 0;
    TMR1ON = 0;
    TMR1H = SERVO_MIDDLE>>8;
    TMR1L = (uint8_t)SERVO_MIDDLE;
    TMR1IF = Off;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//MAIN SUBROUTINES//