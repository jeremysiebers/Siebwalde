#ifndef PETIT_SHIM_H
#define PETIT_SHIM_H
#include <stdint.h>
typedef struct { uint16_t ActValue; } PetitRegStructure;
extern PetitRegStructure PetitHoldingRegisters[21];
#endif
