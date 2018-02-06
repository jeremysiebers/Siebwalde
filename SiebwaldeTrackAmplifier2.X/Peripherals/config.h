/* 
 * File:   config.h
 * Author: Jeremy
 *
 * Created on November 24, 2015, 9:52 PM
 */

#ifndef CONFIG_H
#define	CONFIG_H

#ifdef	__cplusplus
extern "C" {
#endif

#include <xc.h>
#include "pin_manager.h"
#include <stdint.h>
#include <stdbool.h>
#include "interrupt_manager.h"
#include "tmr2.h"
#include "pwm.h"
#include "INT0.h"
#include "tmr0.h"

#define _XTAL_FREQ  8000000
    
enum
{
    On = 1,Off = 0                                                              // Project enumeration for different use cases
};

/**
 * @Param
    none
 * @Returns
    none
 * @Description
    Initializes the device to the default states configured in the
 *                  MCC GUI
 * @Example
    SYSTEM_Initialize(void);
 */
void SYSTEM_Initialize(void);

/**
 * @Param
    none
 * @Returns
    none
 * @Description
    Initializes the oscillator to the default states configured in the
 *                  MCC GUI
 * @Example
    OSCILLATOR_Initialize(void);
 */
void OSCILLATOR_Initialize(void);

#ifdef	__cplusplus
}
#endif

#endif	/* CONFIG_H */

