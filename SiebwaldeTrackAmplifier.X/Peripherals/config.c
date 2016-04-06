/*
 * File:   config.c
 * Author: Jeremy Siebers
 *
 * Created on April 5, 2016, 8:46 PM
 */

#include "xc.h"
#include "config.h"
#include "uart1.h"

// DSPIC33EP512GM304 Configuration Bit Settings

// 'C' source line config statements

// FICD
#pragma config ICS = PGD2               // ICD Communication Channel Select bits (Communicate on PGEC2 and PGED2)
#pragma config JTAGEN = OFF             // JTAG Enable bit (JTAG is disabled)

// FPOR
#pragma config BOREN = OFF              //  (BOR is disabled)
#pragma config ALTI2C1 = OFF            // Alternate I2C1 pins (I2C1 mapped to SDA1/SCL1 pins)
#pragma config ALTI2C2 = OFF            // Alternate I2C2 pins (I2C2 mapped to SDA2/SCL2 pins)
#pragma config WDTWIN = WIN25           // Watchdog Window Select bits (WDT Window is 25% of WDT period)

// FWDT
#pragma config WDTPOST = PS32768        // Watchdog Timer Postscaler bits (1:32,768)
#pragma config WDTPRE = PR128           // Watchdog Timer Prescaler bit (1:128)
#pragma config PLLKEN = ON              // PLL Lock Enable bit (Clock switch to PLL source will wait until the PLL lock signal is valid.)
#pragma config WINDIS = OFF             // Watchdog Timer Window Enable bit (Watchdog Timer in Non-Window mode)
#pragma config FWDTEN = OFF             // Watchdog Timer Enable bit (Watchdog timer enabled/disabled by user software)

// FOSC
#pragma config POSCMD = NONE            // Primary Oscillator Mode Select bits (Primary Oscillator disabled)
#pragma config OSCIOFNC = ON            // OSC2 Pin Function bit (OSC2 is general purpose digital I/O pin)
#pragma config IOL1WAY = ON             // Peripheral pin select configuration (Allow only one reconfiguration)
#pragma config FCKSM = CSECME           // Clock Switching Mode bits (Both Clock switching and Fail-safe Clock Monitor are enabled)

// FOSCSEL
#pragma config FNOSC = FRCPLL           // Oscillator Source Selection (Fast RC Oscillator with divide-by-N with PLL module (FRCPLL))
#pragma config PWMLOCK = OFF            // PWM Lock Enable bit (PWM registers may be written without key sequence)
#pragma config IESO = ON                // Two-speed Oscillator Start-up Enable bit (Start up device with FRC, then switch to user-selected oscillator source)

// FGS
#pragma config GWRP = OFF               // General Segment Write-Protect bit (General Segment may be written)
#pragma config GCP = OFF                // General Segment Code-Protect bit (General Segment Code protect is Disabled)


void SYSTEM_Initialize(void) {
    OSCILLATOR_Initialize();
    IO_Configuration();
    EUSART1_Initialize();
}

void OSCILLATOR_Initialize(void) {

    /* Configure Oscillator to operate the device at 60Mhz PWM frequency 120MHz
	   Fosc= Fin*M/(N1*N2), Fcy=Fosc/2
 	   Fosc= 7.37*(65)/(2*2)=120Mhz for Fosc, Fcy = 60Mhz */

	/* Configure PLL prescaler, PLL postscaler, PLL divisor */
	PLLFBD=63; 				/* M = PLLFBD + 2 */
	CLKDIVbits.PLLPOST=0;   /* N1 = 2 */
	CLKDIVbits.PLLPRE=0;    /* N2 = 2 */

    __builtin_write_OSCCONH(0x01);			/* New Oscillator FRC w/ PLL */
    __builtin_write_OSCCONL(0x01);  		/* Enable Switch */
      
	while(OSCCONbits.COSC != 0b001);		/* Wait for new Oscillator to become FRC w/ PLL */  
    while(OSCCONbits.LOCK != 1);			/* Wait for Pll to Lock */

}

void IO_Configuration(void) {
    ANSELA = 0x0;
    ANSELB = 0x0;
    ANSELC = 0x0;
    TRISA = 0xFF;
    TRISB = 0xFF;
    TRISC = 0xFF;
    RPINR18 = 0x0018;       // Connecting UART1RX to RPI24 Pin32 5V tolerant   (RPINR18 only usable for UART1RX)
    RPOR1 = 0x0001;         // COnnecting UART1TX to RP36 Pin33 5V tolerant     (RPOR1 usable LSB <5-0> for RP36, MSB <13-8> for RP37)
    TRISBbits.TRISB4 = 0;   // Set RP36 as Output for UART1TX
    TRISAbits.TRISA8 = 1;   // Set RPI24 as Input for UART1RX
    TRISBbits.TRISB9 = 0;   // Used for LED1
    
}