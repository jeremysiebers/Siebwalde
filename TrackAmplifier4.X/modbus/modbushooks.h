#ifndef MODBUSHOOKS_H
#define MODBUSHOOKS_H
#include <stdint.h>
#include <stdbool.h>
uint8_t ValidateHoldingRegisterWrite(uint8_t frame_address, uint16_t addr, uint16_t value);
uint8_t OnHoldingRegisterWrite(uint8_t frame_address, uint16_t addr, uint16_t value, bool broadcast);
#endif
