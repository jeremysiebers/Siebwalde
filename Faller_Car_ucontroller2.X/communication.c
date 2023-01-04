/*
 * File:   communication.c
 * Author: jeremy
 *
 * Created on January 1, 2023, 13:54 PM
 */
#include "Main.h"
#include "communication.h"
#include "mcc_generated_files/mcc.h"
#include "mcc_generated_files/adc.h"
#include "executer.h"

/******************************************************************************/
/*          GLOBAL VARIABLES                                                  */
/******************************************************************************/

/******************************************************************************/
/*          LOCAL VARIABLES                                                   */
/******************************************************************************/

STM stm = IDLE;

/******************************************************************************/
/*          Initialize Executer                                               */
/******************************************************************************/
void INITxCOMM(void)
{
    
}

/******************************************************************************/
/*          Process communication                                             */
/******************************************************************************/

uint8_t PROCESSxCOMM(bool run)
{
    if(run)
    {
        switch(stm)
        {
            case IDLE:
                if(EUSART1_is_rx_ready())
                {
                    if(EUSART1_Read() == 0x0D){
                        stm = INIT;
                    }
                }
                break;

            case INIT:
                Init_Menu();
                stm = RUN;
                break;
                
            case RUN:
                if(EUSART1_is_rx_ready())
                {
                    switch(EUSART1_Read())
                    {
                        case ENTER:
                            Init_Menu();
                            break;
                            
                        default : 
                            break;
                    }
                }
                break;

            default : stm = IDLE;
                break;
        }
    }
    return false;
}

/******************************************************************************/
/*          Show terminal menu                                                */
/******************************************************************************/

void Init_Menu(void)
{
    printf("\033[2J\033[1;1H");
    //printf("Vbatt Avg  = %d [mV].\n\r", AdcResultAvg << 2);
    //printf("ADC sample = %d [dec].\n\r", AdcResultSample);
    printf("Led # | Prog | Intensity | Prog_State | Speed\n\r");
    for(uint8_t i=0; i < 6; i++){
       printf("%d     | %d    | %d        | %d         | %d\n\r",
           (LedBit[i].Name + 1),
            LedBit[i].Prog,
            LedBit[i].Led,
            LedBit[i].Prog_State,
            LedBit[i].Speed); 
    };
    
    
    printf("\n\r");
    printf("Select PWM1-6 with a-f.");
    printf("");
    
    
    
//    LedBit[LedFlashLeft].Prog      
//LedBit[LedFlashLeft].Led       
//LedBit[LedFlashLeft].Prog_State
//LedBit[LedFlashLeft].Speed     
}
