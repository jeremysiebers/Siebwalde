/*
 * Millisecond counter based timer
 * 
 * Can be used to count time, take into account that the timer will overflow
 * after 49.7 days!
 */

#include "mcc_generated_files/mcc.h"
#include "milisecond_counter.h"

/*
 * Local functions
 */
void MILLISECOND_HANDLER(void);
void (*Millisecond_Update_Handler)(void);
void (*Millisecond_Update_Handler2)(void);
void (*Millisecond_Update_Handler3)(void);

/*
 * Local variables
 */
/* volatile due to interrupt service routine */
volatile uint32_t milliseconds_counter = 1;

/*
 * Initializer to couple timer interrupt to handler. Timer requires to be 
 * setup with interrupt every millisecond.
 */
//#warning USING TIMER0 FOR MILLISECOND TIMER
void MILLIESxINIT(void)
{
    TMR0_SetInterruptHandler(MILLISECOND_HANDLER);
    SETxMILLISECONDxUPDATExHANDLER(Millisecond_DefaultUpdateHandler);
}

/*
 * Within the handler the millisecond timer is incremented 
 */
void MILLISECOND_HANDLER()
{
    milliseconds_counter++;
    
    if(Millisecond_Update_Handler)
    {
        Millisecond_Update_Handler();
    }
    if(Millisecond_Update_Handler2)
    {
        Millisecond_Update_Handler2();
    }
    if(Millisecond_Update_Handler3)
    {
        Millisecond_Update_Handler3();
    }
}

/*
 * Universal function from arduino, here re-created for this project.
 */
uint32_t GETxMILLIS()
{
    return milliseconds_counter;
}

void SETxMILLISECONDxUPDATExHANDLER(void (* InterruptHandler)(void)){
    Millisecond_Update_Handler = InterruptHandler;
}

void SETxMILLISECONDxUPDATExHANDLER2(void (* InterruptHandler)(void)){
    Millisecond_Update_Handler2 = InterruptHandler;
}

void SETxMILLISECONDxUPDATExHANDLER3(void (* InterruptHandler)(void)){
    Millisecond_Update_Handler3 = InterruptHandler;
}

void Millisecond_DefaultUpdateHandler(void){
    // add your custom code
    // or set custom function using TMR0_SetInterruptHandler()
}