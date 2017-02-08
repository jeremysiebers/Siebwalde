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

#pragma udata API_SECTION_1
    uint8_t API_50[APISIZE];
#pragma udata API_SECTION_2
    uint8_t API_51[APISIZE];
#pragma udata API_SECTION_3
    uint8_t API_52[APISIZE];
#pragma udata API_SECTION_4
    uint8_t API_53[APISIZE];
#pragma udata API_SECTION_5
    uint8_t API_54[APISIZE];


unsigned int i = 0;

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
    for(i = PWM1_GAIN; i < PWM12_GAIN; i++){
        if(TrackAmplifierxWritexAPI(0x50, i, 1)){
            printf("Set 50 Gain Error\n\r");}
		if(TrackAmplifierxWritexAPI(0x51, i, 1)){
            printf("Set 51 Gain Error\n\r");}
		if(TrackAmplifierxWritexAPI(0x52, i, 1)){
            printf("Set 52 Gain Error\n\r");}
		if(TrackAmplifierxWritexAPI(0x53, i, 1)){
            printf("Set 53 Gain Error\n\r");}
    }
        
    
    for(i = PWM1_SETPOINT; i < PWM12_SETPOINT; i++){
        if(TrackAmplifierxWritexAPI(0x50, i, 125)){
            printf("Set 50 PWM Error\n\r");}
		if(TrackAmplifierxWritexAPI(0x51, i, 125)){
            printf("Set 51 PWM Error\n\r");}
		if(TrackAmplifierxWritexAPI(0x52, i, 125)){
            printf("Set 52 PWM Error\n\r");}
		if(TrackAmplifierxWritexAPI(0x53, i, 125)){
            printf("Set 53 PWM Error\n\r");}
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
    
    for(i = PWM1_OCCUPIED; i < PWM12_OCCUPIED; i++){
        if(TrackAmplifierxReadxAPI(0x50, i, &API_50[i])){
            printf("Set 50 Occupied read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x51, i, &API_51[i])){
            printf("Set 51 Occupied read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x52, i, &API_52[i])){
            printf("Set 52 Occupied read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x53, i, &API_53[i])){
            printf("Set 53 Occupied read Error\n\r");}}
    
    for(i = PWM1_CURRENT; i < PWM12_CURRENT; i++){
        if(TrackAmplifierxReadxAPI(0x50, i, &API_50[i])){
            printf("Set 50 Current read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x51, i, &API_51[i])){
            printf("Set 51 Current read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x52, i, &API_52[i])){
            printf("Set 52 Current read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x53, i, &API_53[i])){
            printf("Set 53 Current read Error\n\r");}}
    
    for(i = PWM1_TFLAG; i < PWM12_TFLAG; i++){
        if(TrackAmplifierxReadxAPI(0x50, i, &API_50[i])){
            printf("Set 50 TFLAG read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x51, i, &API_51[i])){
            printf("Set 51 TFLAG read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x52, i, &API_52[i])){
            printf("Set 52 TFLAG read Error\n\r");}
        if(TrackAmplifierxReadxAPI(0x53, i, &API_53[i])){
            printf("Set 53 TFLAG read Error\n\r");}}
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
	if (API_50[PWM1_OCCUPIED] && API_50[PWM4_OCCUPIED]){
        
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
    if (API_50[PWM3_OCCUPIED]&& API_50[PWM2_OCCUPIED]){
        
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

