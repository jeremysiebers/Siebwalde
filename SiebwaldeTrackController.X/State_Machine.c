#include <stdio.h>				// stdio lib
#include <stdlib.h>				// std lib
#include <string.h>				// string lib
#include "State_Machine.h"
#include "Shift_Register.h"
#include "Command_Machine.h"
#include "Var_Out.h"
#include "Fiddle_Yard.h"
#include "api.h"
#include "TrackAmplifier.h"


uint8_t API[APISIZE];


/******************************************************************************
 * Function:        StatexMachinexInit
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        
 *****************************************************************************/
void StatexMachinexInit()	
{		
	switch (TrackAmplifierxWritexAPI(0x50, PWM1_GAIN, 1)){
        case ACK  : break;
        
        case NACK :printf("Set 50:NACK\n\r");
            break;
        case WCOL :printf("Set 50:WCOL\n\r");
            break;
        case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
            break;
        default : break;
    }  
    
    switch (TrackAmplifierxWritexAPI(0x50, PWM2_GAIN, 1)){
        case ACK  : break;
        
        case NACK :printf("Set 50:NACK\n\r");
            break;
        case WCOL :printf("Set 50:WCOL\n\r");
            break;
        case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
            break;
        default : break;
    } 
    
    switch (TrackAmplifierxWritexAPI(0x50, PWM1_SETPOINT, 125)){
        case ACK  : break;
        
        case NACK :printf("Set 50:NACK\n\r");
            break;
        case WCOL :printf("Set 50:WCOL\n\r");
            break;
        case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
            break;
        default : break;
    }  
    
    switch (TrackAmplifierxWritexAPI(0x50, PWM2_SETPOINT, 125)){
        case ACK  : break;
        
        case NACK :printf("Set 50:NACK\n\r");
            break;
        case WCOL :printf("Set 50:WCOL\n\r");
            break;
        case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
            break;
        default : break;
    } 
    
    TrackAmplifierxSynchronizexSetpoints();
        
}

/******************************************************************************
 * Function:        ReadxAmplifiers
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        
 *****************************************************************************/
void ReadxAmplifiers(){
    
    switch (TrackAmplifierxReadxAPI(0x50, PWM1_OCCUPIED, &API[PWM1_OCCUPIED])){
        case ACK  : break;
        
        case NACK :printf("Read 50:NACK\n\r");//Led2 = 0;
            break;
        case WCOL :printf("Read 50:WCOL\n\r");//Led2 = 0;
            break;
        case TIMEOUT :printf("Read 50:TIMEOUT\n\r");//Led2 = 0;
            break;
        default : break;
    }
    
    switch (TrackAmplifierxReadxAPI(0x50, PWM2_OCCUPIED, &API[PWM2_OCCUPIED])){
        case ACK  : break;
        
        case NACK :printf("Read 50:NACK\n\r");//Led2 = 0;
            break;
        case WCOL :printf("Read 50:WCOL\n\r");//Led2 = 0;
            break;
        case TIMEOUT :printf("Read 50:TIMEOUT\n\r");//Led2 = 0;
            break;
        default : break;
    }
    
    switch (TrackAmplifierxReadxAPI(0x50, PWM3_OCCUPIED, &API[PWM3_OCCUPIED])){
        case ACK  : break;
        
        case NACK :printf("Read 50:NACK\n\r");//Led2 = 0;
            break;
        case WCOL :printf("Read 50:WCOL\n\r");//Led2 = 0;
            break;
        case TIMEOUT :printf("Read 50:TIMEOUT\n\r");//Led2 = 0;
            break;
        default : break;
    }
    
    switch (TrackAmplifierxReadxAPI(0x50, PWM4_OCCUPIED, &API[PWM4_OCCUPIED])){
        case ACK  : break;
        
        case NACK :printf("Read 50:NACK\n\r");//Led2 = 0;
            break;
        case WCOL :printf("Read 50:WCOL\n\r");//Led2 = 0;
            break;
        case TIMEOUT :printf("Read 50:TIMEOUT\n\r");//Led2 = 0;
            break;
        default : break;
    }    
}

/******************************************************************************
 * Function:        StatexMachinexUpdate
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Overview:        
 *****************************************************************************/
void StatexMachinexUpdate()	
{
    /* Block 1*/
	if (API[PWM1_OCCUPIED] && API[PWM4_OCCUPIED]){
        
        switch (TrackAmplifierxWritexAPI(0x50, PWM2_SETPOINT, 125)){
        case ACK  : break;
        
        case NACK :printf("Set 50:NACK\n\r");
            break;
        case WCOL :printf("Set 50:WCOL\n\r");
            break;
        case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
            break;
        default : break;
        } 
    }
    else //if (!API[PWM1_OCCUPIED] && API[PWM4_OCCUPIED])
    {
        switch (TrackAmplifierxWritexAPI(0x50, PWM2_SETPOINT, 200)){
        case ACK  : break;
        
        case NACK :printf("Set 50:NACK\n\r");
            break;
        case WCOL :printf("Set 50:WCOL\n\r");
            break;
        case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
            break;
        default : break;
        } 
    }
    
    /* Block 2*/
    if (API[PWM3_OCCUPIED]&& API[PWM2_OCCUPIED]){
        
        switch (TrackAmplifierxWritexAPI(0x50, PWM1_SETPOINT, 125)){
        case ACK  : break;
        
        case NACK :printf("Set 50:NACK\n\r");
            break;
        case WCOL :printf("Set 50:WCOL\n\r");
            break;
        case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
            break;
        default : break;
        } 
    }
    else //if (!API[PWM3_OCCUPIED] && API[PWM2_OCCUPIED])
    {
        switch (TrackAmplifierxWritexAPI(0x50, PWM1_SETPOINT, 200)){
        case ACK  : break;
        
        case NACK :printf("Set 50:NACK\n\r");
            break;
        case WCOL :printf("Set 50:WCOL\n\r");
            break;
        case TIMEOUT :printf("Set 50:TIMEOUT\n\r");
            break;
        default : break;
        } 
    }
    
    TrackAmplifierxSynchronizexSetpoints();
}

