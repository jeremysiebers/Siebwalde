/**
  Section: Included Files
 */
#include "pwm.h"
#include "config.h"
#include <xc.h>

int DutyCycle = 0;

void PWM_Initialize(void) {
    
    /*reference: Example 14-30: Independent PWM Mode ? Independent Duty Cycle and Phase, Fixed Primary Period,Edge-Aligned*/
    /* Disable PWM Module */
    PTCONbits.PTEN = 0;         //  bits should be changed only when PTEN = 0. Changing the clock selection during operation will yield unpredictable results.    
    
    
    #if defined PWM_SLAVE || defined PWM_SLAVE2
    /* Synchronizing Master Time Base with External Signal */
    PTCONbits.SYNCSRC = 0; /* Select SYNC1 input as synchronizing source */
    PTCONbits.SYNCPOL = 0; /* Rising edge of SYNC1 resets the PWM Timer */
    PTCONbits.SYNCEN = 1; /* Enable external synchronization */
    #endif /* PWM_SLAVE */
    
    /* Set PWM Period on Primary Time Base, Equation 14-1: PERIOD, PHASEx and SPHASEx Register Value Calculation for Edge-Aligned Mode */
    /* Master time base @ 60MIPS(120MHz) 800dec duty cycle number --> 18.750Hz for Primary Master Time Base (PMTMR) Period Value bits */
    
    #if defined	PWM_SLAVE
    PTPER = 1000;
    DutyCycle = 400;
    #endif /* PWM_SLAVE */

    #if defined	PWM_SLAVE2
    PTPER = 6020;           // PTPER should be greater then the selected/wanted output frequency, if 20kHz is desired, PTPER should be 6000 --> external sync --> PTPER example 6100
    DutyCycle = 3100;       // in this config jitter < 25ns!!!
    #endif /* PWM_SLAVE2 */
    
    /* Set Phase Shift */
    PHASE1 = 0;
    SPHASE1 = 0;
    PHASE2 = 0;
    SPHASE2 = 0;
    PHASE3 = 0;
    SPHASE3 = 0;
    PHASE4 = 0;
    SPHASE4 = 0;
    PHASE5 = 0;
    SPHASE5 = 0;
    PHASE6 = 0;
    SPHASE6 = 0;
    
    /* Set Duty Cycles */
    PDC1 = DutyCycle;
    SDC1 = DutyCycle;
    PDC2 = DutyCycle;
    SDC2 = DutyCycle;
    PDC3 = DutyCycle;
    SDC3 = DutyCycle;
    PDC4 = DutyCycle;
    SDC4 = DutyCycle;
    PDC5 = DutyCycle;
    SDC5 = DutyCycle;
    PDC6 = DutyCycle;
    SDC6 = DutyCycle;
    
    /* Set Dead Time Values */
    DTR1 = DTR2 = DTR3 = DTR4 = DTR5 = DTR6 = 0;
    ALTDTR1 = ALTDTR2 = ALTDTR3 = ALTDTR4 = ALTDTR5 = ALTDTR6 = 0;
    
    /* Set PWM Mode to Independent */
    IOCON1 = IOCON2 = IOCON3 = IOCON4 = IOCON5 = IOCON6 = 0xCC00;
    
    /* Set Primary Time Base, Edge-Aligned Mode and Independent Duty Cycles */
    PWMCON1 = PWMCON2 = PWMCON3 = PWMCON4 = PWMCON5 = PWMCON6 = 0x0000;
    
    /* Configure Faults */
    FCLCON1 = FCLCON2 = FCLCON3 = FCLCON4 = FCLCON5 = FCLCON6 = 0x3;
    
    #if defined PWM_SLAVE
    /* 1:8 Prescaler --> @120MHz --> 15MHz PWM clock */
    PTCON2 = 0x0003;            
    #endif /* PWM_SLAVE */
    
    #if defined	PWM_SLAVE2
    /* 1:16 Prescaler --> @120MHz --> 7.5MHz PWM clock */
    PTCON2 = 0x0000;            
    #endif /* PWM_SLAVE2 */
      
    /* Enable PWM Module */
    PTCONbits.PTEN = 1;             //  bits should be changed only when PTEN = 0. Changing the clock selection during operation will yield unpredictable results.
        
}