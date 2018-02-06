#ifndef _TMR0_H
#define _TMR0_H

/**
  Section: Included Files
 */

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus  // Provide C++ Compatibility

extern "C" {

#endif


    /**
      Section: Macro Declarations
     */

    /**
      Section: TMR0 APIs
     */

    /**
      @Summary
        Initializes the TMR0 module.

      @Description
        This function initializes the TMR0 Registers.
        This function must be called before any other TMR0 function is called.

      @Preconditions
        None

      @Param
        None

      @Returns
        None

      @Comment
    

      @Example
        <code>
        main()
        {
            // Initialize TMR0 module
            TMR0_Initialize();

            // Do something else...
        }
        </code>
     */
    void TMR0_Initialize(void);

    
    /**
      @Summary
        Reads the TMR0 register.

      @Description
        This function reads the TMR0 register value and return it.

      @Preconditions
        Initialize  the TMR0 before calling this function.

      @Param
        None

      @Returns
        This function returns the current value of TMR0 register.

      @Example
        <code>
        // Initialize TMR0 module

        // Start TMR0
        TMR0_StartTimer();

        // Read the current value of TMR0
        if(0 == TMR0_ReadTimer())
        {
            // Do something else...

            // Reload the TMR value
            TMR0_Reload();
        }
        </code>
     */
    uint8_t TMR0_ReadTimer(void);

    /**
      @Summary
        Writes the TMR0 register.

      @Description
        This function writes the TMR0 register.
        This function must be called after the initialization of TMR0.

      @Preconditions
        Initialize  the TMR0 before calling this function.

      @Param
        timerVal - Value to write into TMR0 register.

      @Returns
        None

      @Example
        <code>
        #define PERIOD 0x80
        #define ZERO   0x00

        while(1)
        {
            // Read the TMR0 register
            if(ZERO == TMR0_ReadTimer())
            {
                // Do something else...

                // Write the TMR0 register
                TMR0_WriteTimer(PERIOD);
            }

            // Do something else...
        }
        </code>
     */
    void TMR0_WriteTimer(uint8_t timerVal);
	
	
	void TMR0_ISR(void);

#ifdef __cplusplus  // Provide C++ Compatibility

}

#endif

#endif // _TMR0_H
/**
 End of File
 */

