/*
 * File:   executer.c
 * Author: jerem
 *
 * Created on December 16, 2022, 5:00 PM
 */
#include "executer.h"
#include "Main.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/adc.h"

/******************************************************************************/
/*          GLOBAL VARIABLES                                                  */
/******************************************************************************/

LEDBIT LedBit[6] = {
/*   NAME          Led Program   Spd nom    pgst itt stp   PWM*/
    {LedFlashLeft,  0,  LedOff,   0,  MAX,   0,   0,  1,    PWM1_LoadDutyValue},
    {LedFlashRight, 0,  LedOff,   0,  MAX,   0,   0,  1,    PWM2_LoadDutyValue},
    {LedBackLeft,   0,  LedOff,   0,  BACK,  0,   0,  1,    PWM3_LoadDutyValue},
    {LedBackRight,  0,  LedOff,   0,  BACK,  0,   0,  1,    PWM4_LoadDutyValue},
    {LedFrontLeft,  0,  LedOff,   0,  FRONT, 0,   0,  1,    PWM5_LoadDutyValue},
    {LedFrontRight, 0,  LedOff,   0,  FRONT, 0,   0,  1,    PWM6_LoadDutyValue}};

/******************************************************************************/
/*          LOCAL VARIABLES                                                   */
/******************************************************************************/

uint8_t ReturnVal = busy; 
uint16_t Random = 0;

/******************************************************************************/
/*          Initialize Executer                                               */
/******************************************************************************/
void INITxEXECUTER(void)
{
    /* Setup first random number */
    srand(13);
    Random = rand();
    /* Setup first initial led behavior*/
    RCSxLED();
}

/******************************************************************************/
/*          Batt Protect behavior                                             */
/******************************************************************************/
void BATTxPROTECT(){
    /* Disable Dash Board Led*/
    DASHLED_SetLow();
    /* Block motor from running */
    EN_MOT_SetDigitalInput();
    
    LedBit[LedBackLeft].Prog         = LedOff;
//    LedBit[LedBackLeft].Led          = MAX;
//    LedBit[LedBackLeft].Speed        = 150;
//    
    LedBit[LedBackRight].Prog        = LedOff;
//    LedBit[LedBackRight].Led         = MAX;
//    LedBit[LedBackRight].Speed       = 150;
    
    LedBit[LedFlashLeft].Prog        = LedFlash;
    LedBit[LedFlashLeft].Led         = MAX;
    LedBit[LedFlashLeft].Speed       = 10;
    
    LedBit[LedFlashRight].Prog       = LedOff;
//    LedBit[LedFlashRight].Led        = MAX;
//    LedBit[LedFlashRight].Speed      = 150;
    
    LedBit[LedFrontLeft].Prog        = LedOff;
//    LedBit[LedFrontLeft].Led         = MAX;
//    LedBit[LedFrontLeft].Speed       = 150;
//    
    LedBit[LedFrontRight].Prog       = LedOff;
//    LedBit[LedFrontRight].Led        = MAX;
//    LedBit[LedFrontRight].Speed      = 150;
}

/******************************************************************************/
/*          RCS LED behavior                                                  */
/******************************************************************************/
void RCSxLED(void)
{
    if(RCS_GetValue()){ /* Check if the car is driving */
               
        /* Set the back lights to normal intensity */
        LedBit[LedBackLeft].Prog         = LedNom;
        LedBit[LedBackRight].Prog        = LedNom;
        
        /* Let random decide to enable flashing lights or not */
        if(Random > 16383)
        {
            /* Set the flashing lights on */
            LedBit[LedFlashLeft].Prog        = LedFlash;
            LedBit[LedFlashLeft].Led         = MAX;
            LedBit[LedFlashLeft].Prog_State  = 0;
            LedBit[LedFlashLeft].Speed       = 150;

            LedBit[LedFlashRight].Prog       = LedFlash;
            LedBit[LedFlashRight].Led        = 0;
            LedBit[LedFlashRight].Prog_State = 0;
            LedBit[LedFlashRight].Speed       = 150;

            /* Let the front lights also glow flash */
            LedBit[LedFrontLeft].Prog        = LedSlFl;
            LedBit[LedFrontLeft].Led         = FRONT_HIGH;
            LedBit[LedFrontLeft].Prog_State  = 2;
            LedBit[LedFrontLeft].Speed       = 1;

            LedBit[LedFrontRight].Prog       = LedSlFl;
            LedBit[LedFrontRight].Led        = FRONT;
            LedBit[LedFrontRight].Prog_State = 0;
            LedBit[LedFrontRight].Speed      = 1;
        }
        
        /* Disable the dashboard light */
        DASHLED_SetLow();
        
        /* Enable the motor */
        EN_MOT_LAT = true;
    }
    /* The Car has braked to stop */
    else{
        /* Set the brake light intensity */
        LedBit[LedBackLeft].Prog        = LedBrake;
        LedBit[LedBackRight].Prog       = LedBrake;
        
        /* Disable the flash light */
        LedBit[LedFlashLeft].Prog       = LedOff;
        LedBit[LedFlashRight].Prog      = LedOff;
        
        /* Set the front lights to normal */
        LedBit[LedFrontLeft].Prog       = LedNom;
        LedBit[LedFrontRight].Prog      = LedNom;
        
        if(Random < 16383)
        {
            /* Set the dash light on */
            DASHLED_SetHigh();
        }
        
        /* Disable the motor */
        EN_MOT_LAT = false;
    }
    
    /* Assign new random number */
    Random = rand();
}

/******************************************************************************/
/*          Main Led program executer                                         */
/******************************************************************************/
uint8_t EXECUTExEFFECT(void)
{
    ReturnVal = busy;
    uint8_t ReturnData = 0;
    
    for(uint8_t x=0; x<6; x++)
    {
        ReturnData += LedBit[x].Prog(x);                                         // Add all return values, call the program that was set for this led       
    }
    if(ReturnData  >= 5)                                                        // when value is lower or equal to -8 all sub routines are finished
    {
        ReturnVal = finished;                                                   // when a subroutine is still busy, this subroutine will be called again until finished is reported back
    }
    else{
        ReturnVal = busy;
    }
    return ReturnVal;
}

/******************************************************************************/
/*          Program 0 = Led off                                               */
/******************************************************************************/
uint8_t LedOff(uint8_t Led)
{
    LedBit[Led].Led = 0;                                                      // Set led bit to off and all associated reg
    LedBit[Led].Speed = 0;
    LedBit[Led].Prog_State = 0;
    LedBit[Led].Iteration = 0;
    CalcPwm(Led);
    return(finished);
}

/******************************************************************************/
/*          Program 1 = Led nominal on                                        */
/******************************************************************************/
uint8_t LedNom(uint8_t Led)
{
    LedBit[Led].Led = LedBit[Led].nominal;                                      // Set led bit to nominal intensity
    LedBit[Led].Speed = 0;
    LedBit[Led].Prog_State = 0;
    LedBit[Led].Iteration = 0;
    CalcPwm(Led);
    return(finished);
}

/******************************************************************************/
/*          Program 2 = Led Maximal on                                        */
/******************************************************************************/
uint8_t LedMax(uint8_t Led)
{
    LedBit[Led].Led = MAX;                                                      // Set led bit to maximum intensity
    LedBit[Led].Speed = 0;
    LedBit[Led].Prog_State = 0;
    LedBit[Led].Iteration = 0;    
    CalcPwm(Led);
    return(finished);
}

/******************************************************************************/
/*          Program 3 = Led BRAKE on                                        */
/******************************************************************************/
uint8_t LedBrake(uint8_t Led)
{
    LedBit[Led].Led = BRAKE;                                                    // Set led bit to BRAKE intensity
    CalcPwm(Led);
    return(finished);
}

/******************************************************************************/
/*          Program 4 Led slow on - slow off - slow on                        */
/******************************************************************************/
uint8_t LedSlFl(uint8_t Led)
{
    uint8_t ReturnVal = busy;
    
    switch(LedBit[Led].Prog_State){
        case 0 :    LedBit[Led].Iteration = 0;
                    LedBit[Led].Prog_State = 1;
                    ReturnVal = busy;
            break;
                    
        case 1 :    LedBit[Led].Iteration++;
                    if(LedBit[Led].Iteration > LedBit[Led].Speed){
                        LedBit[Led].Iteration = 0;
                        LedBit[Led].Led += LedBit[Led].StepSize;
                    }
                    if(LedBit[Led].Led > MAX){
                        LedBit[Led].Led = MAX;
                        LedBit[Led].Prog_State = 2;
                        LedBit[Led].Iteration = 0;
                    }
                    ReturnVal = finished;
            break;
                    
        case 2 :    LedBit[Led].Iteration++;
                    if(LedBit[Led].Iteration > LedBit[Led].Speed){
                        LedBit[Led].Iteration = 0;
                        LedBit[Led].Led -= LedBit[Led].StepSize;
                    }
                    if((LedBit[Led].nominal != MAX) && (LedBit[Led].Led <= LedBit[Led].nominal)){
                        LedBit[Led].Led = LedBit[Led].nominal;
                        LedBit[Led].Prog_State = 1;
                        LedBit[Led].Iteration = 0;
                    }
                    else if((LedBit[Led].nominal == MAX) && (LedBit[Led].Led <= MIN)){
                        LedBit[Led].Led = MIN;
                        LedBit[Led].Prog_State = 1;
                        LedBit[Led].Iteration = 0;
                    }
                    ReturnVal = finished;
            break;
            
        default :   LedBit[Led].Prog_State = 0;
        break;
                    
    }
    CalcPwm(Led);
    return(ReturnVal);
}

/******************************************************************************/
/*          Program 5 Led Flash (On/Off                                       */
/******************************************************************************/
uint8_t LedFlash(uint8_t Led)
{
    uint8_t ReturnVal = busy;
    
    switch(LedBit[Led].Prog_State){
        case 0 :    LedBit[Led].Iteration = 0;
                    LedBit[Led].Prog_State = 1;
                    ReturnVal = busy;
            break;
                    
        case 1 :    LedBit[Led].Iteration++;
                    if(LedBit[Led].Iteration > LedBit[Led].Speed){
                        LedBit[Led].Iteration = 0;
                        if(LedBit[Led].Led == MAX){
                            LedBit[Led].Led = 0;
                        }
                        else{
                            LedBit[Led].Led = MAX;
                        }                        
                    }
                    LedBit[Led].Prog_State = 1;
                    ReturnVal = finished;
            break;
            
        default :   LedBit[Led].Prog_State = 0;
        break;
                    
    }
    CalcPwm(Led);
    return(ReturnVal);
}

/******************************************************************************/
/*          Calculate PWM value                                               */
/******************************************************************************/
void CalcPwm(uint8_t Led){
    LedBit[Led].PWM((uint16_t)LedBit[Led].Led * 4);
}
