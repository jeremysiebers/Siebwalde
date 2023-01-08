/**
  Generated Main Source File

  Company:
    Microchip Technology Inc.

  File Name:
    main.c

  Summary:
    This is the main file generated using PIC10 / PIC12 / PIC16 / PIC18 MCUs

  Description:
    This header file provides implementations for driver APIs for all modules selected in the GUI.
    Generation Information :
        Product Revision  :  PIC10 / PIC12 / PIC16 / PIC18 MCUs - 1.81.8
        Device            :  PIC16F15345
        Driver Version    :  2.00
*/
#include "mcc_generated_files/mcc.h"
#include "Main.h"
#include "executer.h"
#include "mcc_generated_files/adc.h"
#include "communication.h"

/******************************************************************************/
/*          SAF area                                                          */
/******************************************************************************/
/* warning: (1604) Storage Area Flash has been enabled by the configuration 
 * bits; ensure this memory is not used for program code 
*/
/* "XC8 Global Options Additional Options" --> "-mreserve=rom@0x1f80:0x1fff"  */
/* OR occupy the SAF area with defined data */
//typedef struct { uint32_t a, b, c, d, e, f, g, h; }saf_struct;
//const saf_struct saf[4] __at(0x1F80) = {
//    {0x88888888, 0x99999999, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC, 0xDDDDDDDD, 0xEEEEEEEE, 0xFFFFFFFF },
//    {0x88888888, 0x99999999, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC, 0xDDDDDDDD, 0xEEEEEEEE, 0xFFFFFFFF },
//    {0x88888888, 0x99999999, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC, 0xDDDDDDDD, 0xEEEEEEEE, 0xFFFFFFFF },
//    {0x88888888, 0x99999999, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC, 0xDDDDDDDD, 0xEEEEEEEE, 0xFFFFFFFF },
//};

/******************************************************************************/
/*          Methods                                                           */
/******************************************************************************/

/******************************************************************************/
/*          Local variables                                                   */
/******************************************************************************/

bool         UpdateLeds         = false;
bool         UpdateRcs          = false;
bool         UpdateVbatt        = false;
bool         BattProtect        = false;
bool         CarrOff            = false;
uint8_t      AdcState           = 0;
adc_result_t AdcResult[AdcSize] = {1023,1023,1023,1023,1023,1023,1023,1023};
adc_result_t AdcResultSample    = 0;
adc_result_t AdcResultAvg       = 0;
uint8_t      pAdcResult         = 0;
uint24_t     CalcMv             = 0;
uint8_t      StartUp            = 0;
bool         FirstLoop          = true;

/******************************************************************************/
/*          Main                                                              */
/******************************************************************************/
void main(void)
{    
    // initialize the device
    SYSTEM_Initialize();

    // When using interrupts, you need to set the Global and Peripheral Interrupt Enable bits
    // Use the following macros to:

    // Enable the Global Interrupts
    INTERRUPT_GlobalInterruptEnable();

    // Enable the Peripheral Interrupts
    INTERRUPT_PeripheralInterruptEnable();
    
    // Disable the Global Interrupts
    //INTERRUPT_GlobalInterruptDisable();

    // Disable the Peripheral Interrupts
    //INTERRUPT_PeripheralInterruptDisable();
    
    /* Setup Timer interrupt */
    TMR0_SetInterruptHandler(TMR0_INT);
    TMR1_SetInterruptHandler(TMR1_INT);
    /* Setup RCS pin interrupt */
    IOCBF4_SetInterruptHandler(RCS_INT);  
    /* Setup ADC Channel selection */
    ADC_SelectChannel(Vbatt);
    
    INITxEXECUTER();
    
    /* Clear terminal set cursor to first position */
    printf("\033[2J\033[1;1H");
    
    /* Main infinite loop */
    while (1)
    {
        /* Main Led routine updater */
        if(UpdateLeds){
            if (EXECUTExEFFECT() == finished )                                  // When all switch cases are stable in the underlying routines
            {                
                UpdateLeds = false;                                             // Set to OFF for next update of timer0
            }
        }
        
        /* When reed contact switch sees a magnet, the car has to stop */
        if(UpdateRcs && !CarrOff){
            RCSxLED();
            UpdateRcs = false;
        }
        
        /* Measure the Battery voltage and calc average */
        if(UpdateVbatt){
            switch(AdcState){
                case 0: ADC_StartConversion();
                        AdcState = 1;
                    break;
                case 1: if(ADC_IsConversionDone()){                            
                            AdcResultSample = ADC_GetConversionResult();
                            AdcResult[pAdcResult] = AdcResultSample;
                                    
                            pAdcResult++;
                            if(pAdcResult > AdcRef){
                                pAdcResult = 0;
                            }
                            
                            AdcResultAvg = 0;
                            for(uint8_t i=0; i < AdcSize; i++){
                                AdcResultAvg += AdcResult[i];
                            };
                            /* 8 samples --> divide by 8 = 3 bits shift */
                            AdcResultAvg = AdcResultAvg >> 3;
                            if (AdcResultAvg < 750){                            // 750 ~2.90V
                               BattProtect = true; 
                            }
                            UpdateVbatt = false;
                            AdcState = 0;
                            
                            /* mV = ADC result / 500 * 2000 --> = ADC result * 4 */
                            printf("\033[2J\033[1;1H");
                            printf("ADC = %d [mV].\n\r", AdcResultAvg << 2);
                            printf("ADC = %d [dec].\n\r", AdcResultSample);
                            
                            if(EUSART1_is_rx_ready())
                            {
                                EUSART1_Write(EUSART1_Read());
                            }
                        }                        
                    break;
                    
                default: AdcState = 0;
                    break;
            }
        }
         /* When battery voltage is too low, execute protection */
        if(BattProtect){            
            /* Call indication once */
            BATTxPROTECT();
            BattProtect = false;
            /* Block RCS interrupt from overwriting */
            CarrOff = true;
        }
    }
}
/******************************************************************************/
/*          Timer Interrupt Routines                                                */
/******************************************************************************/
void TMR0_INT()
{
    /* Check battery voltage (every second) */
    UpdateVbatt = true;
}

void TMR1_INT(){
    UpdateLeds = true;    
}

/******************************************************************************/
/*          Pin Interrupts                                                    */
/******************************************************************************/
void RCS_INT()
{
    /* Call RCS LED to prevent duplicated code */
    UpdateRcs = true;
    /* Restart Timer 0 to indicate car is driving and has actively stopped */
    TMR0_Reload();
}
/**
 End of File
*/