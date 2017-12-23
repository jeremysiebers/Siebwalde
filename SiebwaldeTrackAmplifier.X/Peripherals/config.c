/*
 * File:   config.c
 * Author: Jeremy Siebers
 *
 * Created on April 5, 2016, 8:46 PM
 */

#include <xc.h>
#include <timer.h>
#include <adc.h>
#include "config.h"
#include <pps.h>

// DSPIC33EP512GM304 Configuration Bit Settings

// 'C' source line config statements

// FICD
#pragma config ICS = PGD2               // ICD Communication Channel Select bits (Communicate on PGEC2 and PGED2)
#pragma config JTAGEN = OFF             // JTAG Enable bit (JTAG is disabled)

// FPOR
#pragma config BOREN = OFF              // (BOR is disabled)
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

void SYSTEMxInitialize(void) {
    OSCILLATOR_Initialize();
    APIxInitialize();
    IO_Configuration();
    EUSART1xInitialize();
    ADC_Initialize();
    PWMxInitialize();
    I2CxInitialize();
    Timers_Initialize();
    PTGxInitialize();
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
    
    /*
    PMD1 = 0xFD59;
    PMD2 = 0xFFFF;
    PMD3 = 0xFFFF;    
    PMD4 = 0xFFFF;    
    PMD6 = 0x1;
    PMD7 = 0xFFFF;
    */
}

void Timers_Initialize(void){
    
    /* Timer 1 used for Led off control */
    ConfigIntTimer1(T1_INT_PRIOR_7 & T1_INT_ON);
    WriteTimer1(0);
    OpenTimer1(T1_ON & T1_GATE_OFF & T1_IDLE_STOP & T1_PS_1_256 & T1_SYNC_EXT_OFF & T1_SOURCE_INT, 0xFFFF);
    T1CONbits.TON = 0;
    
    /* Timer 2 used for input polling and PWM setpoint updates */
    ConfigIntTimer2(T2_INT_PRIOR_1 & T2_INT_ON);
    WriteTimer2(0);
    OpenTimer2(T2_ON & T2_GATE_OFF & T2_IDLE_STOP & T2_PS_1_8 & T2_SOURCE_INT, 0x2490); // 100Hz  
    
    /* Timer 3 used for putty */
    ConfigIntTimer3(T3_INT_PRIOR_7 & T3_INT_ON);
    WriteTimer3(0);
    OpenTimer3(T3_ON & T3_GATE_OFF & T3_IDLE_STOP & T3_PS_1_256 & T3_SOURCE_INT, 0xE400); // 4Hz  
    
    /* Timer 4 used for sync watchdog */
    ConfigIntTimer4(T4_INT_PRIOR_7 & T4_INT_ON);
    WriteTimer4(0);
    OpenTimer4(T4_ON & T4_GATE_OFF & T4_IDLE_STOP & T4_PS_1_256 & T4_SOURCE_INT, 0xE400); // 4Hz  
}

void IO_Configuration(void) {
    ANSELA = 0x0;
    ANSELB = 0x0;
    ANSELC = 0x0;
    TRISA = 0xFF;
    TRISB = 0xFF;
    TRISC = 0xFF;
        
    //CNPUBbits.CNPUB4 = 1;   // Use PUll up to clear FLT32    
    //TRISBbits.TRISB4 = 1;   // FLT32 latched PWM fault must be cleared before PWM will enable    
    
    PPSInput(IN_FN_PPS_U1RX  , IN_PIN_PPS_RPI34);       // Connecting UART1RX to RPI34 Pin23 3.3V tolerant  (RPINR18 only usable for UART1RX (LSB))
    PPSOutput(OUT_FN_PPS_U1TX, OUT_PIN_PPS_RP35);      // COnnecting UART1TX to RP35 Pin24 3.3V tolerant   (RPOR0 usable LSB <5-0> for RP20, MSB <13-8> for RP35)
    //RPINR18 = 0x0022;       // Connecting UART1RX to RPI34 Pin23 3.3V tolerant  (RPINR18 only usable for UART1RX (LSB))
    //RPOR0 = 0x0100;         // COnnecting UART1TX to RP35 Pin24 3.3V tolerant   (RPOR0 usable LSB <5-0> for RP20, MSB <13-8> for RP35)
    TRISBbits.TRISB3 = 0;   // Set RP35 as Output for UART1TX
    TRISBbits.TRISB2 = 1;   // Set RPI34 as Input for UART1RX
    
    //TRISCbits.TRISC4 = 1;   // I2C1 SDA1
    //TRISCbits.TRISC5 = 1;   // I2C1 SCL1
    
    PPSInput(IN_FN_PPS_SYNCI1, IN_PIN_PPS_RP48); // <-- changed pps.h for this to happen, RP48 is not added as input pin function...?
    //RPINR37 = 0x3000;       // Connecting SYNC Input to RP48 (MSB)
    TRISCbits.TRISC0 = 1;   // Set RP48 as Input for SYNCI    
    
    TRISBbits.TRISB9 = 0;   // Used for LED1
    
    TRISBbits.TRISB7 = 1;   // Used for train detection DriveBLok 1A (input)
    TRISBbits.TRISB8 = 1;   // Used for train detection BrakeBlok 1B (input)    
    
    TRISCbits.TRISC3 = 1;   // Used for train detection DriveBLok 2A (input)
    TRISAbits.TRISA9 = 1;   // Used for train detection DriveBLok 2B (input)
    
    TRISBbits.TRISB14 = 0;  // PWM1H
    TRISBbits.TRISB15 = 0;  // PWM1L
    
    TRISBbits.TRISB12 = 0;  // PWM2H
    TRISBbits.TRISB13 = 0;  // PWM2L
    
    TRISBbits.TRISB10 = 0;  // AMP1_DIRECTION //PWM3H
    TRISBbits.TRISB11 = 0;  // AMP2_DIRECTION //PWM3L    
    
    TRISAbits.TRISA10 = 0;  // PWM4H
    TRISAbits.TRISA7 = 0;   // PWM4L
    
    TRISCbits.TRISC8 = 1;   // AMP1_TFLAG //PWM5H
    TRISCbits.TRISC9 = 1;   // AMP2_TFLAG //PWM5L
    
    TRISCbits.TRISC6 = 0;   // AMP1_ENABLE //PWM6H
    TRISCbits.TRISC7 = 0;   // AMP2_ENABLE //PWM6L
}

void ADC_Initialize(){
    
    OpenADC1(0,0,0,0,0,0, 0,0,0,0,0,0,0,0, 0,0,0);
    
}