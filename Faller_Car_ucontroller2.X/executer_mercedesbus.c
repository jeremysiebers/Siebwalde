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

LEDBIT LedBit[7] = {
/*   NAME           Led Program    Spd nom    pgst itt stp   PWM*/
    {LedFlashLeft,  0,  Led_Off,   0,  MAX,   0,   0,  1,    PWM1},
    {LedFlashRight, 0,  Led_Off,   0,  MAX,   0,   0,  1,    PWM2},
    {LedBackLR,     0,  Led_Off,   0,  BACK,  0,   0,  1,    PWM3},
    {LedCabin,      0,  Led_Off,   0,  CAB,   0,   0,  1,    PWM4},
    {LedFrontLR,    0,  Led_Off,   0,  FRONT, 0,   0,  1,    PWM5},
    {LedVehicle,    0,  Led_Off,   0,  MARK,  0,   0,  1,    PWM6}};

/******************************************************************************/
/*          LOCAL VARIABLES                                                   */
/******************************************************************************/

uint8_t ReturnVal = busy; 
uint16_t Random = 0;
uint8_t     ReedState   = 0;
bool        DriveStatus = true;

/******************************************************************************/
/*          Initialize Executer                                               */
/******************************************************************************/
void INITxEXECUTER(void)
{
    /* Setup first random number */
    srand(13);
    Random = rand();
    /* Setup first initial led behavior*/
    UpdateCarStatus(false);
}

/******************************************************************************/
/*          Batt Protect behavior                                             */
/******************************************************************************/
void BATTxPROTECT(){
    /* Disable Dash Board Led*/
    LEDA_SetLow();
    LEDB_SetLow();
    LEDD_SetLow();
    // brake FET disable
    LEDC_SetLow();
    /* Block motor from running */
    EnMOT_SetDigitalInput();
    
    LedBit[LedBackLR].Prog           = Led_Off;
//    LedBit[LedBackLeft].Led          = MAX;
//    LedBit[LedBackLeft].Speed        = 150;
//    
    LedBit[LedCabin].Prog            = Led_Off;
//    LedBit[LedBackRight].Led         = MAX;
//    LedBit[LedBackRight].Speed       = 150;
    
    LedBit[LedFlashLeft].Prog        = Led_Flash;
    LedBit[LedFlashLeft].Led         = MAX;
    LedBit[LedFlashLeft].Speed       = 150;
    
    LedBit[LedFlashRight].Prog       = Led_Off;
//    LedBit[LedFlashRight].Led        = MAX;
//    LedBit[LedFlashRight].Speed      = 150;
    
    LedBit[LedFrontLR].Prog          = Led_Off;
//    LedBit[LedFrontLeft].Led         = MAX;
//    LedBit[LedFrontLeft].Speed       = 150;
//    
    LedBit[LedVehicle].Prog          = Led_Off;
//    LedBit[LedFrontRight].Led        = MAX;
//    LedBit[LedFrontRight].Speed      = 150;
}

/******************************************************************************/
/*          RCS LED behavior                                                  */
/******************************************************************************/
void RCSxLED(bool ReedStatus)
{
    switch (ReedState){
        case 0:
            /* When reed contact switch sees a magnet, the car has to stop, inverse logic */
            if(ReedStatus && !CarrOff){
                DebounceCount = 0;
                /* car stops driving */
                UpdateCarStatus(true);
                ReedState++;
            }
            break;
            
        case 1:
            /* Take into account some wait time to verify reed contact still switched so car does not overshoot driving */
            if(DebounceCount > 5000 && !CarrOff){
                ReedState++;
            }
            break;
            
        case 2:
            if(!ReedStatus && !CarrOff){
                /* car start driving again */
                UpdateCarStatus(false);
                ReedState = 0;
            }
            break;
            
        default: 
            break;
    }
}
    
void UpdateCarStatus(bool DriveStatus){
    if(!DriveStatus){ /* Check if the car is driving, mercedesbus has inverted reed contact logic! */
               
        /* Set the back lights to normal intensity */
        LedBit[LedBackLR].Prog           = Led_Nom;
        LedBit[LedCabin].Prog            = Led_Nom;
        /* Set the front lights to normal */
        LedBit[LedFrontLR].Prog          = Led_Nom;
        LedBit[LedVehicle].Prog          = Led_Mark;
        LedBit[LedFlashRight].Prog       = Led_Off;
        LedBit[LedFlashLeft].Prog        = Led_Off;
        LEDA_SetHigh();
        LEDB_SetHigh();
        LEDD_SetHigh();
        // brake FET disable
        LEDC_SetLow();
        
        /* Enable the motor */
        EnMOT_LAT = true;
    }
    /* The Car has braked to stop */
    else{
        /* Set the brake light intensity */
        EnMOT_LAT = false;
        // brake FET enable
        LEDC_SetHigh();
        LedBit[LedBackLR].Prog            = Led_Brake;
        LedBit[LedCabin].Prog             = Led_Max;
        LedBit[LedFrontLR].Prog           = Led_Mark;
        LedBit[LedFlashRight].Prog        = Led_Flash;
        LedBit[LedFlashRight].Led         = MAX;
        LedBit[LedFlashRight].Speed       = 254; 
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
        ReturnData += Effect_Prog(LedBit[x].Prog, x);                           // Add all return values, call the program that was set for this led       
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
/*          Effect_Prog                                                       */
/******************************************************************************/
uint8_t Effect_Prog(uint8_t Prog, uint8_t Led)
{
    uint8_t ReturnData = 0;
    switch (Prog)                                                               // Start the selected program for the pointed led
    {
        case    Led_Off  :  ReturnData = (LedOff(Led));                                // return also the status of the subroutine that is called
                            break;  

        case    Led_Nom  :  ReturnData = (LedNom(Led));
                            break;

        case    Led_Max  :  ReturnData = (LedMax(Led));
                            break;

        case    Led_Brake:  ReturnData = (LedBrake(Led));
                            break;

        case    Led_SlFl :  ReturnData = (LedSlFl(Led));
                            break;

        case    Led_Flash:  ReturnData = (LedFlash(Led));
                            break;
            
        case    Led_Mark:  ReturnData = (LedMark(Led));
                            break;
            
        default          :
                            break;
    }
    return ReturnData;
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
                    if((LedBit[Led].nominal != MAX) && (LedBit[Led].Led <= SLOW_FLASH_LOW)){
                        LedBit[Led].Led = SLOW_FLASH_LOW;
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
/*          Program 6 = Led soft on                                        */
/******************************************************************************/
uint8_t LedMark(uint8_t Led)
{
    LedBit[Led].Led = MARK;                                                      // Set led bit to soft intensity
    LedBit[Led].Speed = 0;
    LedBit[Led].Prog_State = 0;
    LedBit[Led].Iteration = 0;    
    CalcPwm(Led);
    return(finished);
}
/******************************************************************************/
/*          Calculate PWM value                                               */
/******************************************************************************/
void CalcPwm(uint8_t Led){
    uint16_t calc = (uint16_t)LedBit[Led].Led * 4;
    
    switch(LedBit[Led].PWM){
        case PWM1: PWM1_LoadDutyValue(calc);
            break;
        case PWM2: PWM2_LoadDutyValue(calc);
            break;
        case PWM3: PWM3_LoadDutyValue(calc);
            break;
        case PWM4: PWM4_LoadDutyValue(calc);
            break;
        case PWM5: PWM5_LoadDutyValue(calc);
            break;
        case PWM6: PWM6_LoadDutyValue(calc);
            break;
        default:
            break;
    }    
    //LedBit[Led].PWM((uint16_t)LedBit[Led].Led * 4);
}
