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
        Device            :  PIC16F15324
        Driver Version    :  2.00
*/

/*
    (c) 2018 Microchip Technology Inc. and its subsidiaries. 
    
    Subject to your compliance with these terms, you may use Microchip software and any 
    derivatives exclusively with Microchip products. It is your responsibility to comply with third party 
    license terms applicable to your use of third party software (including open source software) that 
    may accompany Microchip software.
    
    THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS". NO WARRANTIES, WHETHER 
    EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY 
    IMPLIED WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS 
    FOR A PARTICULAR PURPOSE.
    
    IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
    INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
    WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP 
    HAS BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE. TO 
    THE FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL 
    CLAIMS IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT 
    OF FEES, IF ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS 
    SOFTWARE.
*/

#include "mcc_generated_files/mcc.h"
#include "Main.h"
#include "executer.h"
#include "mcc_generated_files/adc.h"
/******************************************************************************/
/*          Methods                                                              */
/******************************************************************************/

/******************************************************************************/
/*          Local variables                                                              */
/******************************************************************************/

bool         UpdateLeds     = false;
bool         UpdateRcs      = false;
bool         UpdateVbatt    = false;
bool         BattProtect    = false;
bool         CarrOff        = false;
uint8_t      AdcState       = 0;
adc_result_t AdcResult[AdcSize] = {1023,1023,1023,1023,1023,1023,1023,1023};
adc_result_t AdcResultSample= 0;
adc_result_t AdcResultAvg   = 0;
uint8_t      pAdcResult     = 0;

/******************************************************************************/
/*          Main                                                              */
/******************************************************************************/

void main(void)
{
    // Initialize the device
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
    IOCCF1_SetInterruptHandler(RCS_INT);  
    /* Setup ADC Channel selection */
    ADC_SelectChannel(Vbatt);
    
    INITxEXECUTER();
       
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
                            if (AdcResultAvg < 800){
                               BattProtect = true; 
                            }
                            UpdateVbatt = false;
                            AdcState = 0;
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
        CLRWDT();
    }
}

/******************************************************************************/
/*          Timer Interrupt Routines                                                */
/******************************************************************************/
void TMR0_INT()
{
    UpdateLeds = true;    
}

void TMR1_INT(){
    /* Check battery voltage (every 10 seconds) */
    UpdateVbatt = true;
}

/******************************************************************************/
/*          Pin Interrupts                                                    */
/******************************************************************************/
void RCS_INT()
{
    /* Call RCS LED to prevent duplicated code */
    UpdateRcs = true;
    /* Restart Timer 1 to indicate car is driving and has actively stopped */
    TMR1_Reload();
}
/**
 End of File
*/
