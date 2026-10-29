#ifndef AMP_CORE_H
#define AMP_CORE_H
#include "amp_model.h"
/* LED display modes (mirrors the main.c LED section). */
typedef enum { AMP_LED_OFF = 0, AMP_LED_BLINK = 1, AMP_LED_ON = 2 } AmpLedMode;
void AmpCore_Init(uint16_t checksum);
void AmpCore_Update(void);
void AmpCore_Inhibit(void);
uint16_t AmpCore_StepRamp(uint16_t current, uint16_t target, uint8_t step);
/* Computes the RUN/WAR/ERR LED display modes from the model + PWM3EN input.
 * Pure (no peripheral writes), so the host-shim tests exercise it directly. */
void AmpLed_Modes(bool pwm3en, AmpLedMode *run_mode, AmpLedMode *war_mode, AmpLedMode *err_mode);
void AmpHw_SetDuty(uint16_t duty);
void AmpHw_SetBrake(bool on);
void AmpHw_Enable(bool on);
#endif
