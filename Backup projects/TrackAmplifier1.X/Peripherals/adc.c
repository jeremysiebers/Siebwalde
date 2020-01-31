/**
  Section: Included Files
 */
#include <xc.h>
#include "adc.h"
#include "config.h"

void ADCxInitialize(){
    
    //ADCON1 Register
    //ADC is off
    AD1CON1bits.ADON = Off;
    //DMA buffers are written in the order of conversion; the module provides an 
    //address to the DMA channel that is the same as the address used for the non-DMA stand-alone buffer
    AD1CON1bits.ADDMABM = 1;
    //0 = 10-bit, 4-channel ADC operation
    AD1CON1bits.AD12B = 0;
    //111 = Internal counter ends sampling and starts conversion (auto-convert)
    AD1CON1bits.SSRC = 7;
    AD1CON1bits.SSRCG = 0;
    //1 = Samples CH0, CH1, CH2, CH3 simultaneously (when CHPS<1:0> = 1x), or samples CH0 and CH1
    //simultaneously (when CHPS<1:0> = 01)
    AD1CON1bits.SIMSAM = 0;
    // 1 = Sampling begins immediately after last conversion; SAMP bit is auto-set
    AD1CON1bits.ASAM = 1;
    
    //ADCON2 Register
    //000 AVDD AVSS
    AD1CON2bits.VCFG = 0;
    //1x = Converts CH0, CH1, CH2 and CH3
    AD1CON2bits.CHPS = 2;
    //When ADDMAEN = 1:
    //11111 = Increments the DMA address after completion of every 32nd sample/conversion operation
    AD1CON2bits.SMPI = 0b11111;
    //0 = Always uses channel input selects for Sample MUXA
    AD1CON2bits.ALTS = 0;
    
    //ADCON3 Register
    //0 = Clock derived from system clock
    AD1CON3bits.ADRC = 0;
    //11111 = 31 TAD
    AD1CON3bits.SAMC = 0b11111;
    //11111111 = TP ? (ADCS<7:0> + 1) = TP ? 256 = TAD
    AD1CON3bits.ADCS = 0b11111111;
    
    //ADCON4 Register
    //0 = Conversion results are stored in the ADC1BUF0 through ADC1BUFF registers; DMA will not be used
    AD1CON4bits.ADDMAEN = 0;
    //111 = Allocates 128 words of DMA buffer to each analog input
    AD1CON4bits.DMABL = 0b111;
    
    AD1CHS123bits.
}