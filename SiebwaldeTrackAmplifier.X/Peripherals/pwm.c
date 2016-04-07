/**
  Section: Included Files
 */
#include "pwm.h"
#include <xc.h>


void PWM_Initialize(void) {
    
    /* Disable PWM Module */
    PTCONbits.PTEN = 0;         //  bits should be changed only when PTEN = 0. Changing the clock selection during operation will yield unpredictable results.
    
    /* Set PWM Period on Primary Time Base */
    PTPER = 0x0BB8;             // Master time base @ 60MIPS 20KHz PWM --> 3000dec for Primary Master Time Base (PMTMR) Period Value bits(
    
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
    PDC1 = 32768;
    SDC1 = 32768;
    PDC2 = 32768;
    SDC2 = 32768;
    PDC3 = 32768;
    SDC3 = 32768;
    PDC4 = 32768;
    SDC4 = 32768;
    PDC5 = 32768;
    SDC5 = 32768;
    PDC6 = 32768;
    SDC6 = 32768;
    
    /* Set Dead Time Values */
    DTR1 = DTR2 = DTR3 = DTR4 = DTR5 = DTR6 = 0;
    ALTDTR1 = ALTDTR2 = ALTDTR3 = ALTDTR4 = ALTDTR5 = ALTDTR6 = 0;
    
    /* Set PWM Mode to Independent */
    IOCON1 = IOCON2 = IOCON3 = IOCON4 = IOCON5 = IOCON6 = 0xCC00;
    
    /* Set Primary Time Base, Edge-Aligned Mode and Independent Duty Cycles */
    PWMCON1 = PWMCON2 = PWMCON3 = PWMCON4 = PWMCON5 = PWMCON6 = 0x0000;
    
    /* Configure Faults */
    FCLCON1 = FCLCON2 = FCLCON3 = FCLCON4 = FCLCON5 = FCLCON6 = 0x0;
    
    /* 1:1 Prescaler */
    PTCON2 = 0x0000;            // Maximum resolution @ 60MIPS
      
    /* Enable PWM Module */
    PTCON = 0x8000;             //  bits should be changed only when PTEN = 0. Changing the clock selection during operation will yield unpredictable results.
        
}