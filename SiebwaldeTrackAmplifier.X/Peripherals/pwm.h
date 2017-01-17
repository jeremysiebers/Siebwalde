/* 
 * File:   pwm.h
 * Author: jsiebers
 *
 * Created on 7 april 2016, 11:45
 */

#ifndef PWM_H
#define	PWM_H

#include <xc.h>

#ifdef	__cplusplus
extern "C" {
#endif

extern void PWMxInitialize(void);
extern void PWMxSetDutyCycles(void);
extern void PWMxReadxOccupiedxSignals(void);
extern void PWMxSTART(void);
extern void PWMxSTOP(void);
extern void PWMxSETxALLxAMP(void);

#ifdef	__cplusplus
}
#endif

#endif	/* PWM_H */

