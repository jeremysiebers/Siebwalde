#include <xc.h>
#include "pwm.h"

/**
  Section: Macro Declarations
 */

#define PWM_INITIALIZE_DUTY_VALUE    129

/**
  Section: PWM Module APIs
 */

void PWM_Initialize(void) {
    // Set the PWM

    // CCP1M PWM; DC4B 16; 
    CCP1CON = 0x0F;

    // CCPR1L 32; first 8 bit of PWM
    CCPR1L = 0x20;

    // CCP1CON 0x0; 2 LSB of PWM in 10 bit mode
    CCP1CONbits.CCP1X = 0x0;
    CCP1CONbits.CCP1Y = 0x0;

}

void PWM_LoadDutyValue(uint16_t dutyValue) {
    // Writing to 8 MSBs of pwm duty cycle in CCPRL register
    CCPR1L = ((dutyValue & 0x03FC) >> 2);

    // Writing to 2 LSBs of pwm duty cycle in CCPCON register
    CCP1CON = (CCP1CON & 0xCF) | ((dutyValue & 0x0003) << 4);
}
/**
 End of File
 */

