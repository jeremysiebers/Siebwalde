/*
 * File:   firedep.c
 * Author: jeremy
 *
 * Created on February 5, 2023, 5:40 PM
 */


#include <xc.h>
#include "firedep.h"
#include "mcc_generated_files/mcc.h"

uint8_t State_firedep = 0;
uint16_t timer_firedep = 0;
uint8_t FDepOccRight = 1;
uint8_t FDepOccMid = 0;

/*
 * 3-way switch:
 * SW_FDEP_RIGHT_LAT = 0, SW_FDEP_MID_LAT = 0 --> LEFT LANE pass by
 * SW_FDEP_RIGHT_LAT = 1, SW_FDEP_MID_LAT = 0 --> Right lane STOP
 * SW_FDEP_RIGHT_LAT = 0, SW_FDEP_MID_LAT = 1 --> Middle lane STOP
 * SW_FDEP_RIGHT_LAT = 1, SW_FDEP_MID_LAT = 1 --> UNUSED 
 *  
 */

void FIREDEPPxDRIVE(void) 
{
    switch(State_firedep)
    {
        case 0: 
            /* Give time to power supply startup and servo PCB to startup */
            timer_firedep++;
            if(timer_firedep > 1000){
                timer_firedep = 0;
                State_firedep++;
            }
            break;
            
        case 1:
            SW_FDEP_RIGHT_LAT       = 1;
            SW_FDEP_MID_LAT         = 1;
            STOP_FDEP_AT_RIGHT_LAT  = 1;
            STOP_FDEP_AT_MID_LAT    = 1;
            timer_firedep++;
            if(timer_firedep > 500){
                SW_FDEP_RIGHT_LAT       = 0;
                SW_FDEP_MID_LAT         = 0;
                STOP_FDEP_AT_RIGHT_LAT  = 1;
                STOP_FDEP_AT_MID_LAT    = 1;
                timer_firedep = 0;
                State_firedep++;
            }
            break;
            
        case 2:
            if(HALL_STOP_FDEP_PORT == 1)
            {
//                if(FDepOccRight == 0)
//                {
//                    SW_FDEP_RIGHT_LAT       = 1;
//                    SW_FDEP_MID_LAT         = 0;
//                    STOP_FDEP_AT_RIGHT_LAT  = 0;
//                    FDepOccRight = 1;
//                }
                if(FDepOccMid == 0)
                {
                    SW_FDEP_RIGHT_LAT       = 0;
                    SW_FDEP_MID_LAT         = 1;
                    STOP_FDEP_AT_MID_LAT    = 0;
                    FDepOccMid = 1;
                }
                else
                {
                    /* Pass driving Left again */
                    SW_FDEP_RIGHT_LAT       = 0;
                    SW_FDEP_MID_LAT         = 0;
                }
                State_firedep = 3;
            }            
            break;
            
        case 3:
            timer_firedep++;
            if(timer_firedep > 500)
            {
                /* Pass driving Left again */
                SW_FDEP_RIGHT_LAT       = 0;
                SW_FDEP_MID_LAT         = 0;
                timer_firedep = 0;
                State_firedep = 4;
            }
            break;
            
        case 4:
            timer_firedep++;
            if(timer_firedep > 1500)
            {
                timer_firedep = 0;
                
                if(FDepOccMid)
                {
                    STOP_FDEP_AT_MID_LAT  = 1;
                    FDepOccMid = 0;
                }
                else
                {
                    STOP_FDEP_AT_RIGHT_LAT    = 1;
                    FDepOccRight = 0;
                }
                State_firedep = 2;
            }
            break;
            
        default:
            break;
    }
}
