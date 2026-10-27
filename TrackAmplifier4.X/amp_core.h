#ifndef AMP_CORE_H
#define AMP_CORE_H
#include "amp_model.h"
void AmpCore_Init(uint16_t checksum);
void AmpCore_Update(void);
void AmpCore_Inhibit(void);
bool AmpCore_IsBridgeDriving(void);
uint16_t AmpCore_StepRamp(uint16_t current, uint16_t target, uint8_t step);
void AmpHw_SetDuty(uint16_t duty);
void AmpHw_SetBrake(bool on);
void AmpHw_Enable(bool on);
#endif
