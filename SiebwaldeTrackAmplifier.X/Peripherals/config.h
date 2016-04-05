#ifndef CONFIG_H
#define	CONFIG_H

#include <xc.h>

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

