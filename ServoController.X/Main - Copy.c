#include <Main.h>
#include <pic.h>

__CONFIG(FOSC_INTRCIO & WDTE_OFF & PWRTE_OFF & MCLRE_OFF & BOREN_OFF & CP_OFF & CPD_OFF);

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//DECLARATIONS//

#define Init_IO()						TRISIO = 0x01;	// Setting in and outputs

//#define HeartStraight						GP0		// Activity status Led
#define HeartBend						GP1		// Servo pulse output
#define ServoOut                                                GP2             // Input for track switching
//#define SwInput                                               GP3
#define SeinStraight						GP4		// SeinStraight Led output
//#define SeinBend        					GP5		// SeinBend Led output
#define SwInput                         			GP0

#define SERVO_LEFT                                              0xFC3C          // 1ms
#define SERVO_RIGHT                                             0xFB84          // 2.0ms

//#define SERVO_LEFT                                              0xFD40          // 1ms
//#define SERVO_MIDDLE                                            0xFBE0          // 1.5ms
//#define SERVO_RIGHT                                             0xFA60          // 2.0ms


static void Init_Timers(void); // Initialize Timers (0)

unsigned int Pulses = 0;                        // Pulses counter to generate 50 Hz servo pulse
unsigned int Servo_Pos = SERVO_RIGHT;
unsigned int Update_Tick = 0;

//MAIN ROUTINE/////////////////////////////////////////////////////////////////////////////////////////

void main(void)
{
    // Hardware Initialization
    TRISIO = 0xFF; // All IO are inputs to be safe
    GPIO = 0x00; // All IO to 0
    ANSEL = 0x00; // Turn off ADC
    CMCON = 7; // Turn off Comparators

    Init_Timers(); // Initialize Timers (TMR0)

    Init_IO(); // Initialize In-and-Outputs   

    while (On) // Infinite loop
    {      
        
    }
}



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Interrupt routines//

static void interrupt
isr(void) // Here the interrupt function
{

    if (T0IF) // If Timer 0 interrupt
    {
        TMR1H = Servo_Pos >> 8;
        TMR1L = Servo_Pos;
        ServoOut = On;
        TMR1ON = On;

        Update_Tick++;

        if (Update_Tick > 4)
        {
            Update_Tick = 0;

            if ((SwInput == On) && (Servo_Pos < SERVO_LEFT))
            {
            Servo_Pos = Servo_Pos + 0x1;
            }
            if ((SwInput == Off) && (Servo_Pos > SERVO_RIGHT))
            {
                Servo_Pos = Servo_Pos - 0x1;
            }

        }

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
    TMR1H = 0;
    TMR1L = 0;

}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//MAIN SUBROUTINES//