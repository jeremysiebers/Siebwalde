#include <xc.h>
#include <stdbool.h>
#include "station.h"
#include "pathway.h"
#include "mcc_generated_files/mcc.h"
#include "main.h"
#include "milisecond_counter.h"
#include "debounce.h"

void INITxSTATION(void)
{
    top.state = INIT;
    top.freightLeftStation = &HALL_BLK_13;
    top.freightEntStation  = &HALL_BLK_9B;
    
    bot.state = INIT;
    bot.freightLeftStation = &HALL_BLK_4A;
    bot.freightEntStation  = &HALL_BLK_21A;
}

void UPDATExSTATION(STATION *instance) 
{    
    switch(instance->state)
    {
        case INIT:
            instance->freightLeftStation->value = false;        
            break;
            
        case RUN:
            if(instance->freightLeftStation->value){
                
            }
            break;
        
        default:
            instance->state = INIT;
        break;
    }
}