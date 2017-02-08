#ifndef CONFIG_H
#define	CONFIG_H

#include <xc.h>
#include "uart1.h"
#include "pwm.h"
#include "i2c.h"
#include "adc.h"
#include "../api.h"

//#define PWM_SLAVE1 /* Used to switch to PWM_SLAVE settings regarding SYNC_Input */
#define PWM_SLAVE2 /* Used to switch to other frequency settings */

#define Led1                    LATBbits.LATB9


#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

extern void SYSTEMxInitialize(void);
void OSCILLATOR_Initialize(void);
void IO_Configuration(void);
void Timers_Initialize(void);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* CONFIG_H */

