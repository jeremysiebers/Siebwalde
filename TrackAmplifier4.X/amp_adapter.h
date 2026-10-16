#ifndef AMP_ADAPTER_H
#define AMP_ADAPTER_H
#include "amp_model.h"
enum { AMP_WRITE_OK, AMP_WRITE_RANGE, AMP_WRITE_READ_ONLY, AMP_WRITE_RELATIONSHIP, AMP_WRITE_SATURATED };
void AmpAdapter_Init(void);
void AmpAdapter_OnFrame(uint8_t addr, uint8_t fn);
uint8_t AmpAdapter_ValidateWrite(uint16_t addr, uint16_t value);
uint8_t AmpAdapter_OnWrite(uint16_t addr, uint16_t value, bool broadcast);
void AmpAdapter_Refresh(void);
void AmpAdapter_OnTransmit(void);
void AmpAdapter_AddressAssigned(void);
#endif
