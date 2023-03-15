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

/*
 * Local variables
 */
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
}

/*
 * Universal function from arduino, here re-created for this project.
 */
uint32_t millis()
{
    return milliseconds_counter;
}

void SETxMILLISECONDxUPDATExHANDLER(void (* InterruptHandler)(void)){
    Millisecond_Update_Handler = InterruptHandler;
}

void Millisecond_DefaultUpdateHandler(void){
    // add your custom code
    // or set custom function using TMR0_SetInterruptHandler()
}