/*
 * File:   debounce.c
 * Author: jeremy
 *
 * Created on February 5, 2023, 1:10 PM
 */
#include <xc.h>
#include "mcc_generated_files/mcc.h"
#include "debounce.h"
#include "milisecond_counter.h"

void DEBOUNCExIO(DEBOUNCE *instance) 
{    
    // Read the input signal and store it in the struct
    instance->buttonState = (bool)((*instance->portx_ptr) & instance->pin_mask);
    
    // Check if the button state has changed
    if (instance->buttonState != instance->lastButtonState && instance->lastDebounceTime == 0) {
      // Reset the debounce timer
      instance->lastDebounceTime = millis();
    }

    // Check if the debounce time has elapsed
    if ((millis() - instance->lastDebounceTime) > instance->debounceDelay) {
      // Update the last button state
      instance->lastButtonState = instance->buttonState;
      
      // re-set the lastDebounceTime
      instance->lastDebounceTime = 0;

      // Process the button state change
      if (instance->buttonState == HIGH && instance->ResetL2H) {
        // Handle low-to-high transition
          instance->value = true;
      } 
      if (instance->buttonState == LOW && instance->ResetH2L) {
        // Handle high-to-low transition
        instance->value = false;
        }
    }
}
