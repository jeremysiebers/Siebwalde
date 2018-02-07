#ifndef _INT0_H
#define _INT0_H

/**
  Section: Included Files
 */

#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus  // Provide C++ Compatibility

extern "C" {

#endif



    /**
      Section: INT0 APIs
     */

    /**
      @Summary
        Initializes the INT0.

      @Description
        This function initializes the INT0 Registers.
        This function must be called before any other INT0 function is called.

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
            // Initialize INT0 module
            INT0_Initialize();

            // Do something else...
        }
        </code>
     */
    void INT0_Initialize(void);
   

    /**
      @Summary
        Timer Interrupt Service Routine

      @Description
        Timer Interrupt Service Routine is called by the Interrupt Manager.

      @Preconditions
        Initialize  the INT0 module with interrupt before calling this isr.

      @Param
        None

      @Returns
        None
     */
    void INT0_ISR(void);


#ifdef __cplusplus  // Provide C++ Compatibility

}

#endif

#endif // _INT0_H
/**
 End of File
 */