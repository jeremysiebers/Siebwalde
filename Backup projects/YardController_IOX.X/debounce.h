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

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

#include <xc.h> // include processor files - each processor file is guarded. 

// TODO Insert appropriate #include <>

// TODO Insert C++ class definitions if appropriate

// TODO Insert declarations

/* Debounce Delay time in y * 1_ms(timer interrupt time) */
const uint32_t  tIoSignalDebounceTime   = (uint32_t)(200);
const uint32_t  tBlkOutDebounceTime     = (uint32_t)(2000);
const uint32_t  tHallSignalDebounceTime = (uint32_t)(5);
/*
 * Debounce struct
*/
typedef struct
{
    uint16_t                debounceDelay;      // Debounce counter
    uint32_t                lastDebounceTime;   // Value when to except input value
    bool                    lastButtonState;    // Debounced value
    bool                    buttonState;        // State of the input
    volatile unsigned char  *portx_ptr;         // Reference to the input port used
    uint8_t                 pin_mask;           // Mask to point to pin used of port
    bool                    value;              // Holds the final value of the signal to be used in SW
    bool                    valueUpdated;       // Indicates a change in the value
    bool                    ResetH2L;           // value able to follow button to false? 
    bool                    ResetL2H;           // value able to follow button to true? 
    
}DEBOUNCE;

/* PORTF */
DEBOUNCE HALL_BUSSTOP_STN = {tHallSignalDebounceTime, 0, 0, 0, &PORTJ, 0x1,  0, 0,  false,  true};
DEBOUNCE HALL_BUSSTOP_IND = {tHallSignalDebounceTime, 0, 0, 0, &PORTJ, 0x2,  0, 0,  false,  true};
DEBOUNCE HALL_STOP_FDEP   = {tHallSignalDebounceTime, 0, 0, 0, &PORTJ, 0x4,  0, 0,  false,  true};

DEBOUNCE HOTRC_CH2R       = {tHallSignalDebounceTime, 0, 0, 0, &PORTH, 0x04, 0, 0,  true,  true};
DEBOUNCE HOTRC_CH2L       = {tHallSignalDebounceTime, 0, 0, 0, &PORTH, 0x08, 0, 0,  true,  true};
DEBOUNCE HOTRC_CH3        = {tHallSignalDebounceTime, 0, 0, 0, &PORTH, 0x10, 0, 0,  false,  true};
DEBOUNCE HOTRC_CH4        = {tHallSignalDebounceTime, 0, 0, 0, &PORTH, 0x20, 0, 0,  false,  true};
DEBOUNCE HOTRC_CH5        = {tHallSignalDebounceTime, 0, 0, 0, &PORTH, 0x40, 0, 0,  false,  true};
DEBOUNCE HOTRC_CH6        = {tHallSignalDebounceTime, 0, 0, 0, &PORTH, 0x80, 0, 0,  false,  true};

DEBOUNCE RESET_IOX        = {tIoSignalDebounceTime,   0, 0, 0, &PORTB, 0x1,  0, 0,  false,  true};
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
extern void DEBOUNCExIO(DEBOUNCE *instance, uint32_t *millisPtr);
extern bool DEBOUNCExGETxVALUExUPDATEDxSTATE(DEBOUNCE *instance);

// TODO Insert declarations or function prototypes (right here) to leverage 
// live documentation

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

