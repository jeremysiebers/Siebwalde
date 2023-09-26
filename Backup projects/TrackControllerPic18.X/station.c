#include <xc.h>
#include <stdbool.h>
#include "main.h"
#include "station.h"
#include "pathway.h"

void setOCC(OCC *instance, bool value);

unsigned char test = 0;

void INITxSTATION(void)
{
    INITxPATHWAY(&top, &bot);
    
    top.state = INIT;
    top.getFreightLeaveStation  = &HALL_BLK_13;
    top.getFreightEnterStation  = &HALL_BLK_9B;
    top.setOccBlkIn             = &OCC_TO_9B;
    top.setOccStn1              = &OCC_TO_STN_10;
    top.setOccStn2              = &OCC_TO_STN_11;
    top.setOccStn3              = &OCC_TO_STN_12;
    top.getOccBlkOut            = &OCC_FR_BLK4;
    top.getOccStn1              = &OCC_FR_STN_10;
    top.getOccStn2              = &OCC_FR_STN_11;
    top.getOccStn3              = &OCC_FR_STN_12;
    top.setPath                 = &WS_TOP;
    top.prevPath                = 0;
    top.setSignal               = &SIG_TOP;    
    
    bot.state = INIT;
    bot.getFreightLeaveStation  = &HALL_BLK_4A;
    bot.getFreightEnterStation  = &HALL_BLK_21A;
    bot.setOccBlkIn             = &OCC_TO_21B;
    bot.setOccStn1              = &OCC_TO_STN_1;
    bot.setOccStn2              = &OCC_TO_STN_2;
    bot.setOccStn3              = &OCC_TO_STN_3;
    bot.getOccBlkOut            = &OCC_FR_BLK13;
    bot.getOccStn1              = &OCC_FR_STN_1;
    bot.getOccStn2              = &OCC_FR_STN_2;
    bot.getOccStn3              = &OCC_FR_STN_3;
    bot.setPath                 = &WS_BOT;
    bot.prevPath                = 0;
    bot.setSignal               = &SIG_BOT;
}

void UPDATExSTATION(STATION *instance) 
{    
    switch(instance->state)
    {
        case INIT:
            /* Set both hall sensors debounced values to False */
            instance->getFreightLeaveStation->value  = false;
            instance->getFreightEnterStation->value = false;
            
            /* Set the occupied signals to all blocks to true to stop
             * all trains from driving. */
            setOCC(instance->setOccBlkIn, true);
            setOCC(instance->setOccStn1 , true);
            setOCC(instance->setOccStn2 , true);
            setOCC(instance->setOccStn3 , true);
            
            /* Set freight passing path */
            SETxSTATIONxPATHWAY(instance, 3);
            
            
            
            instance->state = RUN;
            break;
            
        case RUN:
            if(instance->getFreightLeaveStation->value){
                
            }
            break;
        
        default:
            instance->state = INIT;
        break;
    }
}

/*
 * Function to set the desired output pin following instance and a value
 */
void setOCC(OCC *instance, bool value)
{
    if(value)
    {
        // Set the bit to 1
        *instance->portx_ptr |= instance->pin_mask;
    }
    else{
        // Clear the bit to 0
        *instance->portx_ptr &= ~instance->pin_mask;
    }
}