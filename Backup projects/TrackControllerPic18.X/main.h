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
#ifndef MAIN_H
#define	MAIN_H

// TODO Insert appropriate #include <>
#include <xc.h> // include processor files - each processor file is guarded. 
#include "debounce.h"
 
// TODO Insert C++ class definitions if appropriate

// TODO Insert declarations

DEBOUNCE HALL_BLK_13    = {10, 0, 0, 0, &PORTF, 0x1, false};
DEBOUNCE HALL_BLK_21A   = {10, 0, 0, 0, &PORTF, 0x2, false};
DEBOUNCE HALL_BLK_T4    = {10, 0, 0, 0, &PORTF, 0x4, false};
DEBOUNCE HALL_BLK_T5    = {10, 0, 0, 0, &PORTF, 0x8, false};
DEBOUNCE HALL_BLK_T1    = {10, 0, 0, 0, &PORTF, 0x10, false};
DEBOUNCE HALL_BLK_T2    = {10, 0, 0, 0, &PORTF, 0x20, false};
DEBOUNCE HALL_BLK_9B    = {10, 0, 0, 0, &PORTF, 0x40, false};
DEBOUNCE HALL_BLK_4A    = {10, 0, 0, 0, &PORTF, 0x80, false};
DEBOUNCE HALL_BLK_T7    = {10, 0, 0, 0, &PORTH, 0x1, false};
DEBOUNCE HALL_BLK_T8    = {10, 0, 0, 0, &PORTH, 0x2, false};
DEBOUNCE OCC_FR_BLK13   = {10, 0, 0, 0, &PORTH, 0x4, false};
DEBOUNCE OCC_FR_BLK4    = {10, 0, 0, 0, &PORTH, 0x8, false};
DEBOUNCE OCC_FR_STN_1   = {10, 0, 0, 0, &PORTH, 0x10, false};
DEBOUNCE OCC_FR_STN_2   = {10, 0, 0, 0, &PORTH, 0x20, false};
DEBOUNCE OCC_FR_STN_3   = {10, 0, 0, 0, &PORTH, 0x40, false};
DEBOUNCE OCC_FR_STN_10  = {10, 0, 0, 0, &PORTH, 0x80, false};
DEBOUNCE OCC_FR_STN_11  = {10, 0, 0, 0, &PORTG, 0x1, false};
DEBOUNCE OCC_FR_STN_12  = {10, 0, 0, 0, &PORTG, 0x2, false};
DEBOUNCE OCC_FR_STN_T6  = {10, 0, 0, 0, &PORTG, 0x4, false};
DEBOUNCE OCC_FR_STN_T3  = {10, 0, 0, 0, &PORTG, 0x8, false};

  

// Comment a function and leverage automatic documentation with slash star star
/**
    <p><b>Function prototype:</b></p>
  
    <p><b>Summary:</b></p>

    <p><b>Description:</b></p>

    <p><b>Precondition:</b></p>

    <p><b>Parameters:</b></p>

    <p><b>Returns:</b></p>

    <p><b>Example:</b></p>
    <code>
 
    </code>

    <p><b>Remarks:</b></p>
 */
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

