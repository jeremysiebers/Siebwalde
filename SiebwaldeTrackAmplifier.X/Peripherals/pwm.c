/**
  Section: Included Files
 */
#include "pwm.h"
#include "config.h"
#include <xc.h>


/******************************************************************************
 * Function:        PWM init
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Here all PWM signals are initialized
 *****************************************************************************/
void PWMxInitialize(void) {
    
    int DutyCycle = 0;
    
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
    DutyCycle = 3000;       // in this config jitter < 25ns!!!
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
    
	/* Set API register to current values */
	API[PWM1 ] = DutyCycle / 24;
	API[PWM2 ] = DutyCycle / 24;
	API[PWM3 ] = DutyCycle / 24;
	API[PWM4 ] = DutyCycle / 24;
	API[PWM5 ] = DutyCycle / 24;
	API[PWM6 ] = DutyCycle / 24;
	API[PWM7 ] = DutyCycle / 24;
	API[PWM8 ] = DutyCycle / 24;
	API[PWM9 ] = DutyCycle / 24;
	API[PWM10] = DutyCycle / 24;
	API[PWM11] = DutyCycle / 24;
	API[PWM12] = DutyCycle / 24;
}

/******************************************************************************
 * Function:        PWMxSetDutyCycles
 *
 * PreCondition:    SYNC broadcast from ISC1 Master
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    API value is multiplied due to char(255) to PWM 6000 value
 *
 * Overview:        Here all PWM duty cycles are set according to content in
 *                  the API. This is triggered by a broadcast from the Master.
 *                  All PWM generating dsPIC's used are synced this way
 *****************************************************************************/
void PWMxSetDutyCycles(){
    unsigned int _PWM1;
    unsigned int _PWM2;
    unsigned int _PWM3;
    unsigned int _PWM4;
    unsigned int _PWM5;
    unsigned int _PWM6;
    unsigned int _PWM7;
    unsigned int _PWM8;
    unsigned int _PWM9;
    unsigned int _PWM10;
    unsigned int _PWM11;
    unsigned int _PWM12;
    
    /* Set Duty Cycles */
    _PWM1  = ( unsigned char )API[PWM1 ] * 24;
    _PWM2  = ( unsigned char )API[PWM2 ] * 24;
    _PWM3  = ( unsigned char )API[PWM3 ] * 24;
    _PWM4  = ( unsigned char )API[PWM4 ] * 24;
    _PWM5  = ( unsigned char )API[PWM5 ] * 24;
    _PWM6  = ( unsigned char )API[PWM6 ] * 24;
    _PWM7  = ( unsigned char )API[PWM7 ] * 24;
    _PWM8  = ( unsigned char )API[PWM8 ] * 24;
    _PWM9  = ( unsigned char )API[PWM9 ] * 24;
    _PWM10 = ( unsigned char )API[PWM10] * 24;
    _PWM11 = ( unsigned char )API[PWM11] * 24;
    _PWM12 = ( unsigned char )API[PWM12] * 24;
	
	PDC1 = _PWM1 ;
    SDC1 = _PWM2 ;
    PDC2 = _PWM3 ;
    SDC2 = _PWM4 ;
    PDC3 = _PWM5 ;
    SDC3 = _PWM6 ;
    PDC4 = _PWM7 ;
    SDC4 = _PWM8 ;
    PDC5 = _PWM9 ;
    SDC5 = _PWM10;
    PDC6 = _PWM11;
    SDC6 = _PWM12;
}

/******************************************************************************
 * Function:        PWMxReadxOccupiedxSignals
 *
 * PreCondition:    Execute this routine every cycle
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    Occupied is low active!
 *
 * Overview:        Read every cycle all occupied signals from the amplifier
 *                  boards and put the content in the API
 *****************************************************************************/
void PWMxReadxOccupiedxSignals(){
    API[PWM1_OCC]  = !OCC_1;
    API[PWM2_OCC]  = !OCC_2;
    API[PWM3_OCC]  = !OCC_3;
    API[PWM4_OCC]  = !OCC_4;
    API[PWM5_OCC]  = !OCC_5;
    API[PWM6_OCC]  = !OCC_6;
    API[PWM7_OCC]  = !OCC_7;
    API[PWM8_OCC]  = !OCC_8;
    API[PWM9_OCC]  = !OCC_9;
    API[PWM10_OCC] = !OCC_10;
    API[PWM11_OCC] = !OCC_11;
    API[PWM12_OCC] = !OCC_12;    
}

/******************************************************************************
 * Function:        PWMxSETxALLxAMP
 *
 * PreCondition:    Execute this routine every cycle
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Here all PWM ENABLE signals are set determined by
 *                  the actual content of the API
 *****************************************************************************/
void PWMxSETxALLxAMP(){
    /*
    AMP_1  = API[PWM1_ENA]; 
    AMP_2  = API[PWM2_ENA]; 
    AMP_3  = API[PWM3_ENA]; 
    AMP_4  = API[PWM4_ENA]; 
    AMP_5  = API[PWM5_ENA]; 
    AMP_6  = API[PWM6_ENA]; 
    AMP_7  = API[PWM7_ENA]; 
    AMP_8  = API[PWM8_ENA];
    AMP_9  = API[PWM9_ENA]; 
    AMP_10 = API[PWM10_ENA];
    AMP_11 = API[PWM11_ENA];
    AMP_12 = API[PWM12_ENA];
    */
}

/******************************************************************************
 * Function:        PWMxSTART
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          Set all outputs via PWMxSETxALLxAMP()
 *
 * Side Effects:    None
 *
 * Overview:        Here all PWM signals are enabled all together (triggered
 *                  by a Master broadcast). The content from the API is
 *                  directly written to the outputs by calling PWMxSETxALLxAMP()
 *****************************************************************************/
void PWMxSTART(){
    API[PWM1_ENA]  = 1;
    API[PWM2_ENA]  = 1;
    API[PWM3_ENA]  = 1;
    API[PWM4_ENA]  = 1;
    API[PWM5_ENA]  = 1;
    API[PWM6_ENA]  = 1;
    API[PWM7_ENA]  = 1;
    API[PWM8_ENA]  = 1;
    API[PWM9_ENA]  = 1;
    API[PWM10_ENA] = 1;
    API[PWM11_ENA] = 1;
    API[PWM12_ENA] = 1;
	PWMxSETxALLxAMP();
}

/******************************************************************************
 * Function:        PWMxSTOP (emergency STOP)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          Set all outputs via PWMxSETxALLxAMP()
 *
 * Side Effects:    None
 *
 * Overview:        Here all PWM signals are disabled all together (triggered
 *                  by a Master broadcast). The content from the API is
 *                  directly written to the outputs by calling PWMxSETxALLxAMP()
 *****************************************************************************/
void PWMxSTOP(){
    API[PWM1_ENA]  = 0;
    API[PWM2_ENA]  = 0;
    API[PWM3_ENA]  = 0;
    API[PWM4_ENA]  = 0;
    API[PWM5_ENA]  = 0;
    API[PWM6_ENA]  = 0;
    API[PWM7_ENA]  = 0;
    API[PWM8_ENA]  = 0;
    API[PWM9_ENA]  = 0;
    API[PWM10_ENA] = 0;
    API[PWM11_ENA] = 0;
    API[PWM12_ENA] = 0;
	PWMxSETxALLxAMP();
}