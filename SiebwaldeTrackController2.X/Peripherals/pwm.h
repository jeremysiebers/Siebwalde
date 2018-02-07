#ifndef _PWM_H
#define _PWM_H

/**
  Section: Included Files
 */

#include <xc.h>
#include <stdint.h>

#ifdef __cplusplus  // Provide C++ Compatibility

extern "C" {

#endif

    /**
      Section: PWM Module APIs
     */

    /**
      @Summary
        Initializes the PWM

      @Description
        This routine initializes the PWM_Initialize
        This routine must be called before any other CCP4 routine is called.
        This routine should only be called once during system initialization.

      @Preconditions
        None

      @Param
        None

      @Returns
        None

      @Comment
    

     @Example
        <code>
        uint16_t dutycycle;

        PWM_Initialize();
        PWM_LoadDutyValue(dutycycle);
        </code>
     */
    void PWM_Initialize(void);

    /**
      @Summary
        Loads 16-bit duty cycle.

      @Description
        This routine loads the 16 bit duty cycle value.

      @Preconditions
        PWM_Initialize() function should have been called
        before calling this function.

      @Param
        Pass 16bit duty cycle value.

      @Returns
        None

      @Example
        <code>
        uint16_t dutycycle;

        PWM_Initialize();
        PWM_LoadDutyValue(dutycycle);
        </code>
     */
    extern void PWM_LoadDutyValue(uint16_t dutyValue);

#ifdef __cplusplus  // Provide C++ Compatibility

}

#endif

#endif	//PWM_H
/**
 End of File
 */
