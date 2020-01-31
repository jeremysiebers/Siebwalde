#ifndef CONFIG_H
#define	CONFIG_H

#include <xc.h>
#include "uart1.h"
#include "pwm.h"
#include "i2c.h"
#include "../api.h"
#include "ptg.h"
#include "adc.h"

//#define PWM_SLAVE1 /* Used to switch to PWM_SLAVE settings regarding SYNC_Input */
#define PWM_SLAVE2 /* Used to switch to other frequency settings */

#define Led1                    LATBbits.LATB9

enum
{
    On = 0,Off = 1                                                              // Project enumeration for different use cases
};

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

extern void SYSTEMxInitialize(void);
void OSCILLATOR_Initialize(void);
void IO_Configuration(void);
void Timers_Initialize(void);
void ADC_Initialize(void);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* CONFIG_H */

