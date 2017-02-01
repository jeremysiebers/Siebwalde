/**
  Section: Included Files
 */
#include "pwm.h"
#include "config.h"
#include <xc.h>

#define SET_NO_OF_PWM_GENERATORS_ACTIVE 1

static unsigned int _PWM1,_PWM2,_PWM3,_PWM4,_PWM5,_PWM6,_PWM7,_PWM8,_PWM9,_PWM10,_PWM11,_PWM12 = 3000;


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
    PTPER = 6120;           // PTPER should be greater then the selected/wanted output frequency, if 20kHz is desired, PTPER should be 6000 --> external sync --> PTPER example 6100
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
    unsigned int PWM_SET_REGISTER = 0xCC00;
    switch(SET_NO_OF_PWM_GENERATORS_ACTIVE){
        case 0 : break;
        case 1 : IOCON1 = PWM_SET_REGISTER; break;
        case 2 : IOCON1 = IOCON2 = PWM_SET_REGISTER; break;
        case 3 : IOCON1 = IOCON2 = IOCON3 = PWM_SET_REGISTER; break;
        case 4 : IOCON1 = IOCON2 = IOCON3 = IOCON4 = PWM_SET_REGISTER; break;
        case 5 : IOCON1 = IOCON2 = IOCON3 = IOCON4 = IOCON5 = PWM_SET_REGISTER; break;
        case 6 : IOCON1 = IOCON2 = IOCON3 = IOCON4 = IOCON5 = IOCON6 = PWM_SET_REGISTER; break;
        default: break;
    }
    //IOCON1 = IOCON2 = IOCON3 = IOCON4 = IOCON5 = IOCON6 = 0xCC00;
    
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
    
	/* Set API register to current values 
	API[PWM1_SETPOINT ] = DutyCycle / 24;
	API[PWM2_SETPOINT ] = DutyCycle / 24;
	API[PWM3_SETPOINT ] = DutyCycle / 24;
	API[PWM4_SETPOINT ] = DutyCycle / 24;
	API[PWM5_SETPOINT ] = DutyCycle / 24;
	API[PWM6_SETPOINT ] = DutyCycle / 24;
	API[PWM7_SETPOINT ] = DutyCycle / 24;
	API[PWM8_SETPOINT ] = DutyCycle / 24;
	API[PWM9_SETPOINT ] = DutyCycle / 24;
	API[PWM10_SETPOINT] = DutyCycle / 24;
	API[PWM11_SETPOINT] = DutyCycle / 24;
	API[PWM12_SETPOINT] = DutyCycle / 24;*/
	
	_PWM1  = DutyCycle;
	_PWM2  = DutyCycle;
	_PWM3  = DutyCycle;
	_PWM4  = DutyCycle;
	_PWM5  = DutyCycle;
	_PWM6  = DutyCycle;
	_PWM7  = DutyCycle;
	_PWM8  = DutyCycle;
	_PWM9  = DutyCycle;
	_PWM10 = DutyCycle;
	_PWM11 = DutyCycle;
	_PWM12 = DutyCycle;
	
	
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
 * Side Effects:    API value is multiplied by 24 due to char(255) 
 *                  to PWM 61200 value
 *
 * Overview:        Here all PWM duty cycles are set according to the 
 *                  PWMx_SETPOINT in the API. 
 *                  This is triggered by a broadcast from the Master.
 *                  All PWM generating dsPIC's used are synced this way
 *****************************************************************************/
void PWMxSetDutyCycles(){
    
    /* Set new Setpoints (Duty Cycles) */
    _PWM1  = ( unsigned char )API[PWM1_SETPOINT ] * 24;
    _PWM2  = ( unsigned char )API[PWM2_SETPOINT ] * 24;
    _PWM3  = ( unsigned char )API[PWM3_SETPOINT ] * 24;
    _PWM4  = ( unsigned char )API[PWM4_SETPOINT ] * 24;
    _PWM5  = ( unsigned char )API[PWM5_SETPOINT ] * 24;
    _PWM6  = ( unsigned char )API[PWM6_SETPOINT ] * 24;
    _PWM7  = ( unsigned char )API[PWM7_SETPOINT ] * 24;
    _PWM8  = ( unsigned char )API[PWM8_SETPOINT ] * 24;
    _PWM9  = ( unsigned char )API[PWM9_SETPOINT ] * 24;
    _PWM10 = ( unsigned char )API[PWM10_SETPOINT] * 24;
    _PWM11 = ( unsigned char )API[PWM11_SETPOINT] * 24;
    _PWM12 = ( unsigned char )API[PWM12_SETPOINT] * 24;
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
    API[PWM1_OCCUPIED]  = !OCCUPIED_1;
    API[PWM2_OCCUPIED]  = !OCCUPIED_2;
    API[PWM3_OCCUPIED]  = !OCCUPIED_3;
    API[PWM4_OCCUPIED]  = !OCCUPIED_4;
    API[PWM5_OCCUPIED]  = !OCCUPIED_5;
    API[PWM6_OCCUPIED]  = !OCCUPIED_6;
    API[PWM7_OCCUPIED]  = !OCCUPIED_7;
    API[PWM8_OCCUPIED]  = !OCCUPIED_8;
    API[PWM9_OCCUPIED]  = !OCCUPIED_9;
    API[PWM10_OCCUPIED] = !OCCUPIED_10;
    API[PWM11_OCCUPIED] = !OCCUPIED_11;
    API[PWM12_OCCUPIED] = !OCCUPIED_12;    
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
    
    AMP1_ENABLE  = API[PWM1_ENABLE]; 
    AMP2_ENABLE  = API[PWM2_ENABLE];
    /*
    AMP_3  = API[PWM3_ENABLE]; 
    AMP_4  = API[PWM4_ENABLE]; 
    AMP_5  = API[PWM5_ENABLE]; 
    AMP_6  = API[PWM6_ENABLE]; 
    AMP_7  = API[PWM7_ENABLE]; 
    AMP_8  = API[PWM8_ENABLE];
    AMP_9  = API[PWM9_ENABLE]; 
    AMP_10 = API[PWM10_ENABLE];
    AMP_11 = API[PWM11_ENABLE];
    AMP_12 = API[PWM12_ENABLE];
    */
}

/******************************************************************************
 * Function:        PWMxSETxALLxAMPxDIRECTIONS
 *
 * PreCondition:    Execute this routine every cycle
 *
 * Input:           0 = backwards, 1 = forwards
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        Here all PWM Directions are set according to the 
 *                  PWMx_DIRECTION in the API. 
 *****************************************************************************/
void PWMxSETxALLxAMPxDIRECTIONS(){
    
    AMP1_DIRECTION  = API[PWM1_DIRECTION]; 
	AMP2_DIRECTION  = API[PWM2_DIRECTION]; 	
    /*
	AMP3_DIRECTION  = API[PWM3_DIRECTION];  
	AMP4_DIRECTION  = API[PWM4_DIRECTION];  
	AMP5_DIRECTION  = API[PWM5_DIRECTION];  
	AMP6_DIRECTION  = API[PWM6_DIRECTION];  
	AMP7_DIRECTION  = API[PWM7_DIRECTION];  
	AMP8_DIRECTION  = API[PWM8_DIRECTION];  
	AMP9_DIRECTION  = API[PWM9_DIRECTION];  
	AMP10_DIRECTION = API[PWM10_DIRECTION]; 
	AMP11_DIRECTION = API[PWM11_DIRECTION]; 
	AMP12_DIRECTION = API[PWM12_DIRECTION]; 
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
    API[PWM1_ENABLE]  = 1;
    API[PWM2_ENABLE]  = 1;
    API[PWM3_ENABLE]  = 1;
    API[PWM4_ENABLE]  = 1;
    API[PWM5_ENABLE]  = 1;
    API[PWM6_ENABLE]  = 1;
    API[PWM7_ENABLE]  = 1;
    API[PWM8_ENABLE]  = 1;
    API[PWM9_ENABLE]  = 1;
    API[PWM10_ENABLE] = 1;
    API[PWM11_ENABLE] = 1;
    API[PWM12_ENABLE] = 1;
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
    API[PWM1_ENABLE]  = 0;
    API[PWM2_ENABLE]  = 0;
    API[PWM3_ENABLE]  = 0;
    API[PWM4_ENABLE]  = 0;
    API[PWM5_ENABLE]  = 0;
    API[PWM6_ENABLE]  = 0;
    API[PWM7_ENABLE]  = 0;
    API[PWM8_ENABLE]  = 0;
    API[PWM9_ENABLE]  = 0;
    API[PWM10_ENABLE] = 0;
    API[PWM11_ENABLE] = 0;
    API[PWM12_ENABLE] = 0;
	PWMxSETxALLxAMP();
}

/******************************************************************************
 * Function:        PWMxPROCESSxSETPOINTS
 *
 * PreCondition:    Periodic update via timer
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    After sync via I2C the new _PWMx value is set to the 
 *                  designated PWM output linear
 *
 * Overview:        
 * 
 *****************************************************************************/
void PWMxPROCESSxSETPOINTS(){
    
    if (_PWM1 > PDC1){
        PDC1 = PDC1 + API[PWM1_GAIN];
    }
    else if(_PWM1 < PDC1){
        PDC1 = PDC1- API[PWM1_GAIN];
    }
    
    if (_PWM2 > SDC1){
        SDC1 = SDC1+ API[PWM2_GAIN];
    }
    else if (_PWM2 < SDC1){
        SDC1 = SDC1- API[PWM2_GAIN];
    }
    
    if (_PWM3 > PDC2){
        PDC2++;
    }
    else if(_PWM3 < PDC2){
        PDC2--;
    }
    
    if (_PWM4 > SDC2){
        SDC2++;
    }
    else if(_PWM4 < SDC2){
        SDC2--;
    }
    
    if (_PWM5 > PDC3){
        PDC3++;
    }
    else if(_PWM5 < PDC3){
        PDC3--;
    }
    
    if (_PWM6 > SDC3){
        SDC3++;
    }
    else if(_PWM6 < SDC3){
        SDC3--;
    }
    
    if (_PWM7 > PDC4){
        PDC4++;
    }
    else if(_PWM7 < PDC4){
        PDC4--;
    }
    
    if (_PWM8 > SDC4){
        SDC4++;
    }
    else if(_PWM8 < SDC4){
        SDC4--;
    }
    
    if (_PWM9 > PDC5){
        PDC5++;
    }
    else if(_PWM9 < PDC5){
        PDC5--;
    }
    
    if (_PWM10 > SDC5){
        SDC5++;
    }
    else if(_PWM10 < SDC5){
        SDC5--;
    }
    
    if (_PWM11 > PDC6){
        PDC6++;
    }
    else if(_PWM11 < PDC6){
        PDC6--;
    }
    
    if (_PWM12 > SDC6){
        SDC6++;
    }
    else if(_PWM12 < SDC6){
        SDC6--;
    }
}