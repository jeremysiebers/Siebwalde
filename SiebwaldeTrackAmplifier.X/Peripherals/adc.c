/**
  Section: Included Files
 */
#include <xc.h>
#include "adc.h"
#include "config.h"

void ADCxInitialize(){
    
    //ADCON1 Register
    //Set up A/D for Automatic Sampling
    //Use Timer3 to provide sampling time
    //Set up A/D conversrion results to be read in 1.15 fractional
    //number format.
    //All other bits to their default state
    bla

    //ADCON2 Register
    //Set up A/D for interrupting after 16 samples get filled in the buffer
    //All other bits to their default state
    

    //ADCON3 Register
    //We would like to set up a sampling rate of 8KHz
    //Total Conversion Time= 1/Sampling Rate = 125 microseconds
    //At 29.4 MIPS, Tcy = 33.9 ns = Instruction Cycle Time
    //Tad > 667ns (for -40C to 125C temperature range)
    //We will set up Sampling Time using Timer3 & Tad using ADCS<5:0> bits
    //All other bits to their default state
    //Let's set up ADCS arbitrarily to the maximum possible amount = 63
    //So Tad = Tcy*(ADCS+1)/2 = 1.085 microseconds
    //So, the A/D converter will take 14*Tad periods to convert each sample
    

    //Next, we will to set up Timer 3 to time-out every 125 microseconds
    //As a result, the module will stop sampling and trigger a conversion
    //on every Timer3 time-out, i.e., 125 microseconds. At that time,
    //the conversion process starts and completes 14*Tad periods later.
    //When the conversion completes, the module starts sampling again
    //However, since Timer3 is already on and counting, about 110
    //microseconds later (=125 microseconds - 14*Tad), Timer3 will expire
    //again. Effectively, the module samples for 110 microseconds and
    //converts for 15 microseconds
    //NOTE: The actual sampling rate realized may be 7998.698 Hz
    //      due to a small round off error. Ensure you provide the
    //      true sampling rate to dsPICworks if you are trying to plot
    //      the sampled or filtered signal.
    

    //ADCHS Register
    //Set up A/D Channel Select Register to convert AN7 on Mux A input
    

    //ADCSSL Register
    //Channel Scanning is disabled. All bits left to their default state
    

    //ADPCFG Register
    //Set up channels AN7 as analog input and configure rest as digital
    //Recall that we configured all A/D pins as digital when code execution
    //entered main() out of reset
    

    //Clear the A/D interrupt flag bit
    
    
    //Set the A/D interrupt enable bit
    

    //Turn on the A/D converter
    //This is typically done after configuring other registers
    

    //Start Timer 3
    
}