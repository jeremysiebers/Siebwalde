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


#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

// TODO Insert appropriate #include <>
#include <xc.h> // include processor files - each processor file is guarded.
#include <stdbool.h>
#include "debounce.h"

#define busy -1
#define done 1
#define nop 2

void DebounceIO(bool trackio);
void UpdateTick(void);

/* factor to be set according to set timer interrupt value to calc seconds */
const uint32_t tFactorSec = 1000; // timer set to 1ms, 10 * 1000 = 1 second
/* 5 seconds wait time after point switch set the signal */
const uint32_t tSignalSwitchWaitTime = (uint32_t)(5 * tFactorSec);
/* Time to wait for servo to move to new position */
const uint32_t tSwitchPointWaitTime = (uint32_t)(5 * tFactorSec);
/* Time that Train waits before leaving */
const uint32_t tTrainWaitTime = (uint32_t)(30 * tFactorSec);
/* Time that Freight Train waits before leaving */
const uint32_t tFreightTrainWaitTime = (uint32_t)(5 * tFactorSec);
/* Boot wait time to get all IO read and debounced first */
const uint32_t tReadIoSignalWaitTime = (uint32_t)(2 * tFactorSec);
/* Time to wait after outbound, train fully left the station */
const uint32_t tOutboundWaitTime = (uint32_t)(10 * tFactorSec);

enum STATES{
    INIT,
    INIT2,
    RUN,
    WAIT,
    IDLE,
    
    STN_TO_SIEBWALDE,
    SIEBWALDE_TO_STN,
    SIEBWALDE_SWITCHT4T5,

    HNDL_IDLE,
    HNDL_INBOUND,
    HNDL_OUTBOUND,
    HNDL_PASSING,
    HNDL_WAIT_BLK_OUT,
    
    STN_INBOUND,
    STN_OUTBOUND,
    STN_PASSING,
    STN_WAIT,
    STN_IDLE,
    
    SEQ_IDLE,
    SEQ_WAIT,
    SEQ_SET_OCC,
    SEQ_CHK_TRAIN,
    SEQ_CHK_PASSED,
    SEQ_OUTBOUND_LEFT_STATTION,
    
    SIG_RED,
    SIG_GREEN,
};

typedef struct
{
    volatile unsigned char      *portx_ptr;                                     // Reference to the input port used
    uint8_t                     pin_mask;                                       // Mask to point to pin used of port
}OCC, REL;

typedef struct
{
    volatile unsigned char      *port1_ptr;                                     // Reference to the input port used
    uint8_t                     pin1_mask;                                      // Mask to point to pin used of port
    volatile unsigned char      *port2_ptr;                                     // Reference to the input port used
    uint8_t                     pin2_mask;                                      // Mask to point to pin used of port
    volatile unsigned char      *port3_ptr;                                     // Reference to the input port used
    uint8_t                     pin3_mask;                                      // Mask to point to pin used of port
    volatile unsigned char      *port4_ptr;                                     // Reference to the input port used
    uint8_t                     pin4_mask;                                      // Mask to point to pin used of port
    volatile unsigned char      *port5_ptr;                                     // Reference to the input port used
    uint8_t                     pin5_mask;                                      // Mask to point to pin used of port
    volatile unsigned char      *port6_ptr;                                     // Reference to the input port used
    uint8_t                     pin6_mask;                                      // Mask to point to pin used of port
    
}WS, SIG;

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* XC_HEADER_TEMPLATE_H */

