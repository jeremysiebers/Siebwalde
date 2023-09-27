/* Microchip Technology Inc. and its subsidiaries.  You may use this software 
 * and any derivatives exclusively with Microchip products. 
 * 
 * THIS SOFTWARE IS SUPPLIED BY MICROCHIP "AS IS".  NO WARRANTIES, WHETHER 
 * EXPRESS, IMPLIED OR STATUTORY, APPLY TO THIS SOFTWARE, INCLUDING ANY IMPLIED 
 * WARRANTIES OF NON-INFRINGEMENT, MERCHANTABILITY, AND FITNESS FOR A 
 * PARTICULAR PURPOSE, OR ITS INTERACTION WITH MICROCHIP PRODUCTS, COMBINATION 
 * WITH ANY OTHER PRODUCTS, OR USE IN ANY APPLICATION. 
 *
 * IN NO EVENT WILL MICROCHIP BE LIABLE FOR ANY INDIRECT, SPECIAL, PUNITIVE, 
 * INCIDENTAL OR CONSEQUENTIAL LOSS, DAMAGE, COST OR EXPENSE OF ANY KIND 
 * WHATSOEVER RELATED TO THE SOFTWARE, HOWEVER CAUSED, EVEN IF MICROCHIP HAS 
 * BEEN ADVISED OF THE POSSIBILITY OR THE DAMAGES ARE FORESEEABLE.  TO THE 
 * FULLEST EXTENT ALLOWED BY LAW, MICROCHIP'S TOTAL LIABILITY ON ALL CLAIMS 
 * IN ANY WAY RELATED TO THIS SOFTWARE WILL NOT EXCEED THE AMOUNT OF FEES, IF 
 * ANY, THAT YOU HAVE PAID DIRECTLY TO MICROCHIP FOR THIS SOFTWARE.
 *
 * MICROCHIP PROVIDES THIS SOFTWARE CONDITIONALLY UPON YOUR ACCEPTANCE OF THESE 
 * TERMS. 
 */

/* 
 * File:   
 * Author: 
 * Comments:
 * Revision history: 
 */

// This is a guard condition so that contents of this file are not included
// more than once.  
#ifndef DEBOUNCE_H
#define	DEBOUNCE_H

#include <xc.h> // include processor files - each processor file is guarded.  

// TODO Insert appropriate #include <>

// TODO Insert C++ class definitions if appropriate

// TODO Insert declarations

/*
 * Debounce struct
*/
typedef struct
{
    uint8_t                 debounceDelay;      // Debounce counter
    uint32_t                lastDebounceTime;   // Value when to except input value
    bool                    lastButtonState;    // Debounced value
    bool                    buttonState;        // State of the input
    volatile unsigned char  *portx_ptr;         // Reference to the input port used
    uint8_t                 pin_mask;           // Mask to point to pin used of port
    bool                    value;              // Holds the final value of the signal
    bool                    ResetH2L;           // value able to follow button to false? 
    bool                    ResetL2H;           // value able to follow button to true? 
    
}DEBOUNCE;

DEBOUNCE HALL_BLK_13    = {1, 0, 0, 0, &PORTF, 0x1,  0,  false,  true};
DEBOUNCE HALL_BLK_21A   = {1, 0, 0, 0, &PORTF, 0x2,  0,  false,  true};
DEBOUNCE HALL_BLK_T4    = {1, 0, 0, 0, &PORTF, 0x4,  0,  false,  true};
DEBOUNCE HALL_BLK_T5    = {1, 0, 0, 0, &PORTF, 0x8,  0,  false,  true};
DEBOUNCE HALL_BLK_T1    = {1, 0, 0, 0, &PORTF, 0x10, 0,  false,  true};
DEBOUNCE HALL_BLK_T2    = {1, 0, 0, 0, &PORTF, 0x20, 0,  false,  true};
DEBOUNCE HALL_BLK_9B    = {1, 0, 0, 0, &PORTF, 0x40, 0,  false,  true};
DEBOUNCE HALL_BLK_4A    = {1, 0, 0, 0, &PORTF, 0x80, 0,  false,  true};
DEBOUNCE HALL_BLK_T7    = {1, 0, 0, 0, &PORTH, 0x1,  0,  false,  true};
DEBOUNCE HALL_BLK_T8    = {1, 0, 0, 0, &PORTH, 0x2,  0,  false,  true};
DEBOUNCE OCC_FR_BLK13   = {1, 0, 0, 0, &PORTH, 0x4,  0,  true,   true};
DEBOUNCE OCC_FR_BLK4    = {1, 0, 0, 0, &PORTH, 0x8,  0,  true,   true};
DEBOUNCE OCC_FR_STN_1   = {1, 0, 0, 0, &PORTH, 0x10, 0,  true,   true};
DEBOUNCE OCC_FR_STN_2   = {1, 0, 0, 0, &PORTH, 0x20, 0,  true,   true};
DEBOUNCE OCC_FR_STN_3   = {1, 0, 0, 0, &PORTH, 0x40, 0,  true,   true};
DEBOUNCE OCC_FR_STN_10  = {1, 0, 0, 0, &PORTH, 0x80, 0,  true,   true};
DEBOUNCE OCC_FR_STN_11  = {1, 0, 0, 0, &PORTG, 0x1,  0,  true,   true};
DEBOUNCE OCC_FR_STN_12  = {1, 0, 0, 0, &PORTG, 0x2,  0,  true,   true};
DEBOUNCE OCC_FR_STN_T6  = {1, 0, 0, 0, &PORTG, 0x4,  0,  true,   true};
DEBOUNCE OCC_FR_STN_T3  = {1, 0, 0, 0, &PORTG, 0x8,  0,  true,   true};
DEBOUNCE CTRL_OFF       = {1, 0, 0, 0, &PORTG, 0x10, 0,  true,   true};
DEBOUNCE OCC_FR_9B      = {1, 0, 0, 0, &PORTG, 0x20, 0,  true,   true};
DEBOUNCE OCC_FR_21B     = {1, 0, 0, 0, &PORTG, 0x40, 0,  true,   true};

/**
  @Summary
    Debounces an input that is given

  @Description
    This function debounces an input of the microcontroller.
    This function must be called after the creation of an instance of
    the DEBOUNCE struct.

  @Preconditions
    None

  @Param
    instance address of an instantiated DEBOUNCE struct.

  @Returns
    None

  @Comment
    
	
  @Example
    <code>
        creation: DEBOUNCE HALL_BLK_13    = {10, 0, 0, 0, &PORTF, 0x1, false};
        usage: DEBOUNCExIO(&HALL_BLK_13);
    </code>
*/
extern void DEBOUNCExIO(DEBOUNCE *instance);

// TODO Insert declarations or function prototypes (right here) to leverage 
// live documentation

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

    // TODO If C++ is being used, regular C code needs function names to have C 
    // linkage so the functions can be used by the c code. 

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

