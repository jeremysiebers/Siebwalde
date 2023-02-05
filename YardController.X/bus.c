/*
 * File:   bus.c
 * Author: jerem
 *
 * Created on February 5, 2023, 2:22 PM
 */
#include <xc.h>
#include "mcc_generated_files/mcc.h"

uint8_t State_bus = 0;
uint16_t timer_bus = 0;

void BUSxDRIVE(void) 
{
    switch(State_bus)
    {
        case 0: 
            /* Give time to power supply startup and servo PCB to startup */
            timer_bus++;
            if(timer_bus > 1000){
                timer_bus = 0;
                State_bus++;
            }
            break;
            
        case 1:
            SW_BUSSTOP_IND_LAT      = 1;
            STOP_BUS_AT_IND_LAT     = 1;
            SW_BUSSTOP_STN_LAT      = 1;
            STOP_BUS_AT_STN_LAT     = 1;
            timer_bus++;
            if(timer_bus > 500){
                SW_BUSSTOP_IND_LAT      = 0;
                STOP_BUS_AT_IND_LAT     = 1;
                SW_BUSSTOP_STN_LAT      = 0;
                STOP_BUS_AT_STN_LAT     = 1;
                timer_bus = 0;
                State_bus++;
            }
            break;
            
        case 2:
            if(HALL_BUSSTOP_STN_PORT == 1)
            {
                SW_BUSSTOP_STN_LAT      = 1;
                STOP_BUS_AT_STN_LAT     = 0;
                State_bus = 3;
            }
            
            if(HALL_BUSSTOP_IND_PORT == 1)
            {
                SW_BUSSTOP_IND_LAT      = 1;
                STOP_BUS_AT_IND_LAT     = 0;
                State_bus = 3;
            }
            break;
            
        case 3:
            timer_bus++;
            if(timer_bus > 500)
            {
                SW_BUSSTOP_STN_LAT      = 0;
                SW_BUSSTOP_IND_LAT      = 0;
                timer_bus = 0;
                State_bus = 4;
            }
            break;
            
        case 4:
            timer_bus++;
            if(timer_bus > 1000)
            {
                timer_bus = 0;
                STOP_BUS_AT_IND_LAT     = 1;
                STOP_BUS_AT_STN_LAT     = 1;
                State_bus = 2;
            }
            break;
            
        default:
            break;
    }
}
