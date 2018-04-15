#ifndef _TMR1_H
#define _TMR1_H

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
      Section: TMR1 APIs
     */

    /**
      @Summary
        Initializes the TMR1 module.

      @Description
        This function initializes the TMR1 Registers.
        This function must be called before any other TMR1 function is called.

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
            // Initialize TMR1 module
            TMR1_Initialize();

            // Do something else...
        }
        </code>
     */
    void TMR1_Initialize(void);

    /**
      @Summary
        This function starts the TMR1.

      @Description
        This function starts the TMR1 operation.
        This function must be called after the initialization of TMR1.

      @Preconditions
        Initialize  the TMR1 before calling this function.

      @Param
        None

      @Returns
        None

      @Example
        <code>
        // Initialize TMR1 module

        // Start TMR1
        TMR1_StartTimer();

        // Do something else...
        </code>
     */
    void TMR1_StartTimer(void);

    /**
      @Summary
        This function stops the TMR1.

      @Description
        This function stops the TMR1 operation.
        This function must be called after the start of TMR1.

      @Preconditions
        Initialize  the TMR1 before calling this function.

      @Param
        None

      @Returns
        None

      @Example
        <code>
        // Initialize TMR1 module

        // Start TMR1
        TMR1_StartTimer();

        // Do something else...

        // Stop TMR1;
        TMR1_StopTimer();
        </code>
     */
    void TMR1_StopTimer(void);

    /**
      @Summary
        Reads the TMR1 register.

      @Description
        This function reads the TMR1 register value and return it.

      @Preconditions
        Initialize  the TMR1 before calling this function.

      @Param
        None

      @Returns
        This function returns the current value of TMR1 register.

      @Example
        <code>
        // Initialize TMR1 module

        // Start TMR1
        TMR1_StartTimer();

        // Read the current value of TMR1
        if(0 == TMR1_ReadTimer())
        {
            // Do something else...

            // Reload the TMR value
            TMR1_Reload();
        }
        </code>
     */
    uint8_t TMR1_ReadTimer(void);

    /**
      @Summary
        Writes the TMR1 register.

      @Description
        This function writes the TMR1 register.
        This function must be called after the initialization of TMR1.

      @Preconditions
        Initialize  the TMR1 before calling this function.

      @Param
        timerVal - Value to write into TMR1 register.

      @Returns
        None

      @Example
        <code>
        #define PERIOD 0x80
        #define ZERO   0x00

        while(1)
        {
            // Read the TMR1 register
            if(ZERO == TMR1_ReadTimer())
            {
                // Do something else...

                // Write the TMR1 register
                TMR1_WriteTimer(PERIOD);
            }

            // Do something else...
        }
        </code>
     */
    void TMR1_WriteTimer(uint8_t timerVal);

    
    /**
      @Summary
        Boolean routine to poll or to check for the match flag on the fly.

      @Description
        This function is called to check for the timer match flag.
        This function is used in timer polling method.

      @Preconditions
        Initialize  the TMR1 module before calling this routine.

      @Param
        None

      @Returns
        true - timer match has occurred.
        false - timer match has not occurred.

      @Example
        <code>
        while(1)
        {
            // check the match flag
            if(TMR1_HasOverflowOccured())
            {
                // Do something else...

                // Reload the TMR1 value
                TMR1_Reload();
            }
        }
        </code>
     */
    bool TMR1_HasOverflowOccured(void);

#ifdef __cplusplus  // Provide C++ Compatibility

}

#endif

#endif // _TMR1_H
/**
 End of File
 */

