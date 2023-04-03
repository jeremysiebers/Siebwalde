#include <xc.h>
#include "pathway.h"
#include "mcc_generated_files/mcc.h"
#include "main.h"
#include "milisecond_counter.h"



int8_t SETxSTATIONxPATHWAY(uint8_t pathway)
{
    switch(pathway){
        case 1:
            WS_TO_FYRD_1_L_LAT  = 1;
            WS_TO_FYRD_3_R_LAT  = 1;
            break;
            
        case 2:
            WS_TO_FYRD_1_L_LAT  = 0;
            WS_TO_FYRD_3_R_LAT  = 0;
            
            WS_TO_FYRD_2_L_LAT  = 1;
            WS_TO_FYRD_4_R_LAT  = 1;
            break;
            
        case 3:
            WS_TO_FYRD_1_L_LAT  = 0;
            WS_TO_FYRD_3_R_LAT  = 0;
            
            WS_TO_FYRD_2_L_LAT  = 0;
            WS_TO_FYRD_4_R_LAT  = 0;
            break;

        case 10:
            break;
            
        case 11:
            break;
            
        case 12:
            break;
            
        default:
            WS_TO_FYRD_1_L_LAT  = 0;
            WS_TO_FYRD_3_R_LAT  = 0;            
            WS_TO_FYRD_2_L_LAT  = 0;
            WS_TO_FYRD_4_R_LAT  = 0;
            
            
            break;
    }
}