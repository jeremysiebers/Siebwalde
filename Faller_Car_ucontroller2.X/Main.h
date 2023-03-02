#ifndef __Main_H
#define __Main_H

#include "mcc_generated_files/mcc.h"

#define On							1	
#define Off							0

#define Input                       1
#define Output                      0

#define finished                    1
#define busy                        0

#define AdcSize                     8
#define AdcRef                      (AdcSize - 1)

uint16_t     DebounceCount      = 0;
bool         UpdateRcs          = false;
bool         CarrOff            = false;

void TMR0_INT(void);
void TMR1_INT(void);
void RCS_INT(void);

#endif