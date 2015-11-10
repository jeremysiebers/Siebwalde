#include <Main.h>
#include <pic.h>

__CONFIG(FOSC_INTRCIO & WDTE_OFF & PWRTE_OFF & MCLRE_ON & BOREN_OFF & CP_OFF & CPD_OFF);

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//DECLARATIONS//

#define Init_IO()						TRISIO = 0x10 //0x18;	// Setting in and outputs

#define SeinStraight                         			GP0             // SeinStraight Led output
#define SeinBend						GP1		// SeinBend Led output
#define ServoOut                                                GP2             // Servo pulse output
#define Spare                                                   GP3             // Spare
#define SwInput  						GP4		// Switch inout to drive servo to the right or the left
#define Heart_Pol        					GP5		// Heart polaization relais output

#define SERVO_LEFT                                                (SERVO_MIDDLE + SERVO_DIST)
#define SERVO_RIGHT                                               (SERVO_MIDDLE - SERVO_DIST)

#define SERVO_MIDDLE                                              0xFBD7          // 1.5ms is also used for switching Heart_Pol

#define SERVO_DIST                                                0//75

//#define SERVO_LEFT                                              0xFD40          // 1ms
//#define SERVO_MIDDLE                                            0xFBD7          // 1.5ms
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
        Update_Tick++;

        if (Update_Tick > 0)
        {
            Update_Tick = 0;

            if ((SwInput == Off) && (Servo_Pos < SERVO_LEFT))
            {
            Servo_Pos = Servo_Pos + 0x1;//SERVO_LEFT; //Servo_Pos + 0x1;
            }
            if ((SwInput == On) && (Servo_Pos > SERVO_RIGHT))
            {
                Servo_Pos = Servo_Pos - 0x1;//SERVO_RIGHT; //Servo_Pos - 0x1;
            }

        }

        if ((Servo_Pos < (SERVO_LEFT - 1)) && (Servo_Pos > (SERVO_RIGHT + 1)))
        {
            SeinStraight = Off;
            SeinBend = Off;
        }

        else if (Servo_Pos == SERVO_LEFT)
        {
            SeinStraight = On;
            SeinBend = Off;
        }

        else if (Servo_Pos == SERVO_RIGHT)
        {
            SeinStraight = Off;
            SeinBend = On;
        }

        if (Servo_Pos > SERVO_MIDDLE)
        {
            Heart_Pol = On;
        }

        else if (Servo_Pos < SERVO_MIDDLE)
        {
            Heart_Pol = Off;
        }

        TMR1H = Servo_Pos >> 8;
        TMR1L = Servo_Pos;
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

    PS0 = 1;
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