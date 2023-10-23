#include <xc.h>
#include <stdbool.h>
#include "main.h"
/*
 * Function to set the desired output pin following instance and a value
 */
void SETxOCC(OCC *self, bool value)
{
    if(value)
    {
        // Set the bit to 1
        *self->portx_ptr |= self->pin_mask;
    }
    else{
        // Clear the bit to 0
        *self->portx_ptr &= ~self->pin_mask;
    }
}