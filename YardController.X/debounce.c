/*
 * File:   debounce.c
 * Author: jeremy
 *
 * Created on February 5, 2023, 1:10 PM
 */


#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "debounce.h"
/*
_HALL_BUSSTOP_STN_GetValue()
_HALL_BUSSTOP_IND_GetValue()
_HALL_STOP_FDEP_GetValue()
 * */

DEBOUNCE HallBuSStopStn = {0,4};
DEBOUNCE HallBuSStopInd = {0,4};
DEBOUNCE HallStopFdep   = {0,4};


void DEBOUNCExIO(void) {
        
    switch(PORTGbits.RG0){
        case 0:
            if(HallBuSStopStn.Value == 1)
            {
                HallBuSStopStn.Count++;
                if(HallBuSStopStn.Count > HallBuSStopStn.Threshold)
                {
                    HallBuSStopStn.Count = 0;
                    HallBuSStopStn.Value = 0;
                }
            }
            else
            {
                HallBuSStopStn.Count = 0;
            }
            break;
            
        case 1:
            if(HallBuSStopStn.Value == 0)
            {
                HallBuSStopStn.Count++;
                if(HallBuSStopStn.Count > HallBuSStopStn.Threshold)
                {
                    HallBuSStopStn.Count = 0;
                    HallBuSStopStn.Value = 1;
                }
            }
            else
            {
                HallBuSStopStn.Count = 0;
            }
            break;
            
        default:
            break;
    }
    
    
    
}
