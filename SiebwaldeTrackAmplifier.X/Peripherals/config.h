#ifndef CONFIG_H
#define	CONFIG_H

#include <xc.h>
#include "uart1.h"
#include "pwm.h"
#include "i2c.h"

//#define PWM_SLAVE /* Used to switch to PWM_SLAVE setting regarding SYNC_Input */
#define PWM_SLAVE2 /* Used to switch to other frequency setting */

#define Led1                    LATBbits.LATB9
#define TPDriveBLok_1A          PORTBbits.RB7
#define TPBrakeBLok_1B          PORTBbits.RB8
#define TPDriveBLok_2A          PORTCbits.RC3
#define TPBrakeBLok_2B          PORTAbits.RA9



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

