/*
 * File:   config.c
 * Author: Jeremy Siebers
 *
 * Created on Januari 20, 2011, 22:46 PM
 */

#include <xc.h>
#include "Peripherals/config.h"
#include "api.h"



uint8_t API[APISIZE];                                                               // API RAM space
uint8_t API_RW[APISIZE];                                                            // API Read/Write register, defines whether a location is writable and not read only (0 = read only)

/******************************************************************************
 * Function:        APIxInitialize
 *
 * PreCondition:    Execute once to init API
 *
 * Input:           None
 *
 * Output:          Sets Read only permissions for API[256] mem map
 *                  0 is readonly, 1 is writable
 *
 * Side Effects:    None
 *
 * Overview:        None
 *****************************************************************************/

void APIxInitialize(){
    
    unsigned int i;
    for(i = 0; i < (APISIZE + 1); i++ )
    {
        API_RW[i] = 0;                                                          // Initialize RW_API with 0 to make sure all locations are Read Only at first
        API[i]    = 0;                                                          // Initialize API with 0 to make sure all locations are 0 at first
    }
    
    API[API_SIZE] = APISIZE;                                                    // Set the APISIZE on the API_SIZE location within API
    
    /*  Set the RW directions*/
    API_RW[APISIZE]                 =   RO;
    API_RW[PWM_OUTPUT_MODE]         =   RW;
	
	API_RW[PWM1_SETPOINT ]          =   RW;
	API_RW[PWM2_SETPOINT ]          =   RW;
	API_RW[PWM3_SETPOINT ]          =   RW;
	API_RW[PWM4_SETPOINT ]          =   RW;
	API_RW[PWM5_SETPOINT ]          =   RW;
	API_RW[PWM6_SETPOINT ]          =   RW;
	API_RW[PWM7_SETPOINT ]          =   RW;
	API_RW[PWM8_SETPOINT ]          =   RW;
	API_RW[PWM9_SETPOINT ]          =   RW;
	API_RW[PWM10_SETPOINT]          =   RW;
	API_RW[PWM11_SETPOINT]          =   RW;
	API_RW[PWM12_SETPOINT]          =   RW;
	
	API[PWM1_SETPOINT ]             =   125;									// Set default to 50% dutycycle
	API[PWM2_SETPOINT ]             =   125;
	API[PWM3_SETPOINT ]             =   125;
	API[PWM4_SETPOINT ]             =   125;
	API[PWM5_SETPOINT ]             =   125;
	API[PWM6_SETPOINT ]             =   125;
	API[PWM7_SETPOINT ]             =   125;
	API[PWM8_SETPOINT ]             =   125;
	API[PWM9_SETPOINT ]             =   125;
	API[PWM10_SETPOINT]             =   125;
	API[PWM11_SETPOINT]             =   125;
	API[PWM12_SETPOINT]             =   125;
    
    API_RW[PWM1_GAIN]          		=   RW;
	API_RW[PWM2_GAIN]          		=   RW;
	API_RW[PWM3_GAIN]          		=   RW;
	API_RW[PWM4_GAIN]          		=   RW;
	API_RW[PWM5_GAIN]          		=   RW;
	API_RW[PWM6_GAIN]          		=   RW;
	API_RW[PWM7_GAIN]          		=   RW;
	API_RW[PWM8_GAIN]          		=   RW;
	API_RW[PWM9_GAIN]          		=   RW;
	API_RW[PWM10_GAIN]         		=   RW;
	API_RW[PWM11_GAIN]         		=   RW;
	API_RW[PWM12_GAIN]         		=   RW;
	
	API[PWM1_GAIN]          		=   3;                                      // Set Gain default to 1
	API[PWM2_GAIN]          		=   3;
	API[PWM3_GAIN]          		=   1;
	API[PWM4_GAIN]          		=   1;
	API[PWM5_GAIN]          		=   1;
	API[PWM6_GAIN]          		=   1;
	API[PWM7_GAIN]          		=   1;
	API[PWM8_GAIN]          		=   1;
	API[PWM9_GAIN]          		=   1;
	API[PWM10_GAIN]         		=   1;
	API[PWM11_GAIN]         		=   1;
	API[PWM12_GAIN]         		=   1;
	
	if (API[PWM_OUTPUT_MODE] == SSPWM){                                         // When Single Sided PWM is programmed by Master (in API!!) then set RW_API to RW
		API_RW[PWM1_DIRECTION ]         =   RW;
		API_RW[PWM2_DIRECTION ]         =   RW;
		API_RW[PWM3_DIRECTION ]         =   RW;
		API_RW[PWM4_DIRECTION ]         =   RW;
		API_RW[PWM5_DIRECTION ]         =   RW;
		API_RW[PWM6_DIRECTION ]         =   RW;
		API_RW[PWM7_DIRECTION ]         =   RW;
		API_RW[PWM8_DIRECTION ]         =   RW;
		API_RW[PWM9_DIRECTION ]         =   RW;
		API_RW[PWM10_DIRECTION]         =   RW;
		API_RW[PWM11_DIRECTION]         =   RW;
		API_RW[PWM12_DIRECTION]         =   RW;
	}
	else{                                                                       // When Dual Sided PWM is programmed by Master then set RW API to RO
		API_RW[PWM1_DIRECTION ]         =   RO;
		API_RW[PWM2_DIRECTION ]         =   RO;
		API_RW[PWM3_DIRECTION ]         =   RO;
		API_RW[PWM4_DIRECTION ]         =   RO;
		API_RW[PWM5_DIRECTION ]         =   RO;
		API_RW[PWM6_DIRECTION ]         =   RO;
		API_RW[PWM7_DIRECTION ]         =   RO;
		API_RW[PWM8_DIRECTION ]         =   RO;
		API_RW[PWM9_DIRECTION ]         =   RO;
		API_RW[PWM10_DIRECTION]         =   RO;
		API_RW[PWM11_DIRECTION]         =   RO;
		API_RW[PWM12_DIRECTION]         =   RO;
		
		API[PWM1_DIRECTION ]            =   1;                                  // And set the direction output to HIGH level (this will be tied to P input of LMD18200T)
	    API[PWM2_DIRECTION ]            =   1;
	    API[PWM3_DIRECTION ]            =   1;
	    API[PWM4_DIRECTION ]            =   1;
	    API[PWM5_DIRECTION ]            =   1;
	    API[PWM6_DIRECTION ]            =   1;
	    API[PWM7_DIRECTION ]            =   1;
	    API[PWM8_DIRECTION ]            =   1;
	    API[PWM9_DIRECTION ]            =   1;
	    API[PWM10_DIRECTION]            =   1;
	    API[PWM11_DIRECTION]            =   1;
	    API[PWM12_DIRECTION]            =   1;
	}
	
	API_RW[PWM1_ENABLE ]            =   RW;
	API_RW[PWM2_ENABLE ]            =   RW;
	API_RW[PWM3_ENABLE ]            =   RW;
	API_RW[PWM4_ENABLE ]            =   RW;
	API_RW[PWM5_ENABLE ]            =   RW;
	API_RW[PWM6_ENABLE ]            =   RW;
	API_RW[PWM7_ENABLE ]            =   RW;
	API_RW[PWM8_ENABLE ]            =   RW;
	API_RW[PWM9_ENABLE ]            =   RW;
	API_RW[PWM10_ENABLE]            =   RW;
	API_RW[PWM11_ENABLE]            =   RW;
	API_RW[PWM12_ENABLE]            =   RW;
	
	API_RW[PWM1_OCCUPIED ]          =   RO;
	API_RW[PWM2_OCCUPIED ]          =   RO;
	API_RW[PWM3_OCCUPIED ]          =   RO;
	API_RW[PWM4_OCCUPIED ]          =   RO;
	API_RW[PWM5_OCCUPIED ]          =   RO;
	API_RW[PWM6_OCCUPIED ]          =   RO;
	API_RW[PWM7_OCCUPIED ]          =   RO;
	API_RW[PWM8_OCCUPIED ]          =   RO;
	API_RW[PWM9_OCCUPIED ]          =   RO;
	API_RW[PWM10_OCCUPIED]          =   RO;
	API_RW[PWM11_OCCUPIED]          =   RO;
	API_RW[PWM12_OCCUPIED]          =   RO;
	
	API_RW[PWM1_CURRENT ]           =   RO;
	API_RW[PWM2_CURRENT ]           =   RO;
	API_RW[PWM3_CURRENT ]           =   RO;
	API_RW[PWM4_CURRENT ]           =   RO;
	API_RW[PWM5_CURRENT ]           =   RO;
	API_RW[PWM6_CURRENT ]           =   RO;
	API_RW[PWM7_CURRENT ]           =   RO;
	API_RW[PWM8_CURRENT ]           =   RO;
	API_RW[PWM9_CURRENT ]           =   RO;
	API_RW[PWM10_CURRENT]           =   RO;
	API_RW[PWM11_CURRENT]           =   RO;
	API_RW[PWM12_CURRENT]           =   RO;	
}

/******************************************************************************
 * Function:        unsigned char API_RW(unsigned char index)
 *
 * PreCondition:    None
 *
 * Input:           Index number to get RW/RO status
 *
 * Output:          returns RW/RO status on index position in API[]
 *
 * Side Effects:    None
 *
 * Overview:        None
 *****************************************************************************/

unsigned char GETxAPIxRW(unsigned char index){
	return API_RW[index];
}