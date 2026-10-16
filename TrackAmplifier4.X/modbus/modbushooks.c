/* Recovered application hook; all execution is in the cooperative main loop. */
#include "modbushooks.h"
#include "../main.h"
#include "../amp_adapter.h"
uint8_t OnHoldingRegisterWrite(uint16_t addr, uint16_t value, bool broadcast)
{
    return AmpAdapter_OnWrite(addr, value, broadcast);
}
