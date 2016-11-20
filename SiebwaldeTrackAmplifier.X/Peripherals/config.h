#ifndef CONFIG_H
#define	CONFIG_H

#include <xc.h>
#include "uart1.h"
#include "pwm.h"

//#define PWM_SLAVE /* Used to switch to PWM_SLAVE setting regarding SYNC_Input */
#define PWM_SLAVE2 /* Used to switch to other frequency setting */

#define Led1            LATBbits.LATB9
#define TrainPresent    PORTBbits.RB8

#ifdef	__cplusplus
extern "C" {
#endif /* __cplusplus */

void SYSTEM_Initialize(void);
void OSCILLATOR_Initialize(void);
void IO_Configuration(void);

#ifdef	__cplusplus
}
#endif /* __cplusplus */

#endif	/* CONFIG_H */

