/* Recovered application hook; all execution is in the cooperative main loop. */
#include "modbushooks.h"
#include "../main.h"
#include "../amp_adapter.h"
uint8_t ValidateHoldingRegisterWrite(uint8_t frame_address, uint16_t addr, uint16_t value)
{
    if (Amplifier_IsBootloaderWrite(frame_address, addr, value)) return AMP_WRITE_OK;
    return AmpAdapter_ValidateWrite(addr, value);
}
uint8_t OnHoldingRegisterWrite(uint8_t frame_address, uint16_t addr, uint16_t value, bool broadcast)
{
    if (!broadcast && Amplifier_TryBootloaderWrite(frame_address, addr, value)) return AMP_WRITE_OK;
    return AmpAdapter_OnWrite(addr, value, broadcast);
}
