/**
  Section: Included Files
 */
#include <xc.h>
#include "adc.h"
#include "config.h"

unsigned int config1;
unsigned int config2;
unsigned int config3;
unsigned int config4;
unsigned int configport_h;
unsigned int configport_l;
unsigned int configscan_h;
unsigned int configscan_l;

void ADCxInitialize(){
    
    config1 = ADC_MODULE_ON & ADC_IDLE_STOP & 
    ADC_ADDMABM_ORDER & ADC_AD_12B_12BIT &
    ADC_FORMAT_SIGN_INT & ADC_CLK_TMR &
    AUTO_SAMPLING_ON & ADC_MULTIPLE ;
     
    config2 = ADC_VREF_AVDD_AVSS & ADC_SCAN_ON &
    ADC_CONVERT_CH0123 & ADC_DMA_ADD_INC_1 ;
     
    config3 =  ADC_CONV_CLK_SYSTEM & ADC_SAMPLE_TIME_3 &
    ADC_CONV_CLK_Tcy2;
     
    config4 = ADC_DMA_BUF_LOC_32;
     
    configport_h = ENABLE_ALL_ANA_16_31;
     
    configport_l = ENABLE_ALL_ANA_0_15;
     
    configscan_l = SCAN_ALL
    configscan_l = SCAN_ALL;
     
    OpenADC1(config1,config2,config3,config4,configport_l,configport_h, configscan_h,configscan_l);
}