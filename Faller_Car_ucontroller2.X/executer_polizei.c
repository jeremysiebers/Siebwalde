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
/*   NAME          Led Program   Spd nom    pgst itt stp   PWM*/
    {LedFlashLeft,  0,  Led_Off,   0,  MAX,   0,   0,  1,    PWM1},
    {LedFlashRight, 0,  Led_Off,   0,  MAX,   0,   0,  1,    PWM2},
    {LedBackLeft,   0,  Led_Off,   0,  BACK,  0,   0,  1,    PWM3},
    {LedBackRight,  0,  Led_Off,   0,  BACK,  0,   0,  1,    PWM4},
    {LedFrontLeft,  0,  Led_Off,   0,  FRONT, 0,   0,  1,    PWM5},
    {LedFrontRight, 0,  Led_Off,   0,  FRONT, 0,   0,  1,    PWM6},
    {LedA,          0,  Led_Off,   0,  MAX,   0,   0,  1,    LEDA}};

/******************************************************************************/
/*          LOCAL VARIABLES                                                   */
/******************************************************************************/

uint8_t     ReturnVal   = busy; 
uint16_t    Random      = 0;
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
    /* Force startup with flashing lights */
    Random = 40000;
    /* Setup first initial led behavior*/
    UpdateCarStatus(true);
}

/******************************************************************************/
/*          Batt Protect behavior                                             */
/******************************************************************************/
void BATTxPROTECT(){
    /* Disable Dash Board Led*/
    LEDB_SetLow();
    LEDC_SetLow();
    LEDD_SetLow();
    /* Block motor from running */
    EnMOT_SetDigitalInput();
    
    LedBit[LedBackLeft].Prog         = Led_Off;
    LedBit[LedBackRight].Prog        = Led_Off;    
    LedBit[LedFlashLeft].Prog        = Led_Off;    
    LedBit[LedFlashRight].Prog       = Led_Off;
    LedBit[LedFrontLeft].Prog        = Led_Off;
    LedBit[LedFrontRight].Prog       = Led_Off;
    
    LedBit[LedA].Prog                = Led_Flash;
    LedBit[LedA].Led                 = ON;
    LedBit[LedA].Prog_State          = 0;
    LedBit[LedA].Speed               = 252;
}

/******************************************************************************/
/*          RCS LED behavior                                                  */
/******************************************************************************/
void RCSxLED(bool ReedStatus)
{
    switch (ReedState){
        case 0:
            /* When reed contact switch sees a magnet, the car has to stop, inverse logic */
            if(!ReedStatus && !CarrOff){
                DebounceCount = 0;
                /* car stops driving */
                UpdateCarStatus(false);
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
            if(ReedStatus && !CarrOff){
                /* car start driving again */
                UpdateCarStatus(true);
                ReedState = 0;
            }
            break;
            
        default: 
            break;
    }
}

void UpdateCarStatus(bool DriveStatus){
    
    if(DriveStatus){ /* Check if the car is driving */
               
        /* Set the back lights to normal intensity */
        LedBit[LedBackLeft].Prog         = Led_Nom;
        LedBit[LedBackRight].Prog        = Led_Nom;
        /* Set the front lights to normal */
        LedBit[LedFrontLeft].Prog       = Led_Nom;
        LedBit[LedFrontRight].Prog      = Led_Nom;
        
        /* Let random decide to enable flashing lights or not */
        if(Random > 20000)
        {
            /* Set the flashing lights on */
            LedBit[LedFlashLeft].Prog        = Led_Flash;
            LedBit[LedFlashLeft].Led         = MAX;
            LedBit[LedFlashLeft].Prog_State  = 0;
            LedBit[LedFlashLeft].Speed       = 250;
            
            LedBit[LedFlashRight].Prog       = Led_Flash;
            LedBit[LedFlashRight].Led        = MAX;
            LedBit[LedFlashRight].Prog_State = 0;
            LedBit[LedFlashRight].Speed      = 250;

            /* Let the front lights also glow flash */
            LedBit[LedFrontLeft].Prog        = Led_SlFl;
            LedBit[LedFrontLeft].Led         = FRONT;
            LedBit[LedFrontLeft].Prog_State  = 2;
            LedBit[LedFrontLeft].Speed       = 0;
            LedBit[LedFrontLeft].StepSize    = 5;

            LedBit[LedFrontRight].Prog       = Led_SlFl;
            LedBit[LedFrontRight].Led        = FRONT;
            LedBit[LedFrontRight].Prog_State = 2;
            LedBit[LedFrontRight].Speed      = 0;
            LedBit[LedFrontRight].StepSize   = 5;
            
            LedBit[LedA].Prog                = Led_Flash;
            LedBit[LedA].Led                 = ON;
            LedBit[LedA].Prog_State          = 0;
            LedBit[LedA].Speed               = 100;
        }
        
        /* Disable the dashboard light */
        //LEDA_SetLow();
        //LEDB_SetHigh();
        
        /* Enable the motor */
        EnMOT_LAT = true;
    }
    /* The Car has braked to stop */
    else{
        /* Set the brake light intensity */
        LedBit[LedBackLeft].Prog        = Led_Brake;
        LedBit[LedBackRight].Prog       = Led_Brake;
        
        /* Disable the flash light */
        LedBit[LedFlashLeft].Prog       = Led_Mark;
        LedBit[LedFlashRight].Prog      = Led_Mark;
        
        /* Set the front lights to normal */
        LedBit[LedFrontLeft].Prog       = Led_Nom;
        LedBit[LedFrontRight].Prog      = Led_Nom;
        
        /* Set the single flash led to off */
        LedBit[LedA].Prog                = Led_Off;
        
        if(Random < 16383)
        {
            /* Set the dash light on */
            //LEDA_SetHigh();
        }
        else{
            /* Disable the dashboard light */
            //LEDA_SetLow();
        }
        //LEDB_SetLow();
        /* Disable the motor */
        EnMOT_LAT = false;
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
    
    for(uint8_t x=0; x<7; x++)
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
                            
        case    Led_Mark :  ReturnData = (LedMark(Led));
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
/*          Program 6 = Led direct control                                    */
/******************************************************************************/
uint8_t LedMark(uint8_t Led)
{
    LedBit[Led].Led = MARK;                                                     // Set led bit to Marking intensity
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
                    
        case 1 :    LedBit[Led].Iteration+= LedBit[Led].StepSize;
                    if(LedBit[Led].Iteration > LedBit[Led].Speed){
                        LedBit[Led].Iteration = 0;
                        LedBit[Led].Led += 1;
                    }
                    if(LedBit[Led].Led > MAX){
                        LedBit[Led].Led = MAX;
                        LedBit[Led].Prog_State = 2;
                        LedBit[Led].Iteration = 0;
                    }
                    ReturnVal = finished;
            break;
                    
        case 2 :    LedBit[Led].Iteration+= LedBit[Led].StepSize;
                    if(LedBit[Led].Iteration > LedBit[Led].Speed){
                        LedBit[Led].Iteration = 0;
                        LedBit[Led].Led -= 1;
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
        default  : LED_StandardOutput(LedBit[Led].PWM, calc);
            break;
    }    
    //LedBit[Led].PWM((uint16_t)LedBit[Led].Led * 4);
}

/******************************************************************************/
/*          Calculate standard output value                                   */
/******************************************************************************/
void LED_StandardOutput(uint8_t PWM, uint16_t val)
{
    bool LedOn = false;
    if(val > 511){
        LedOn = true;
    }
    
    switch (PWM){
        case LEDA: LEDA_LAT = LedOn;
            break;
        case LEDB: LEDB_LAT = LedOn;
            break;
        case LEDC: LEDC_LAT = LedOn;
            break;
        case LEDD: LEDD_LAT = LedOn;
            break;        
        default :
            break;
    }
}
