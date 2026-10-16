/* Appended after verbatim production FC03/06/16 handlers by test_amp.py. */
static unsigned transport_checks;
#define TC(e) do { transport_checks++; if (!(e)) return __LINE__; } while (0)
static void request(uint8_t fn, uint8_t slave, uint16_t start, uint16_t count)
{
    unsigned i;
    for (i = 0; i < sizeof(Petit_Rx_Data.DataBuf); i++) Petit_Rx_Data.DataBuf[i] = 0;
    Petit_Rx_Data.Function = fn;
    Petit_Rx_Data.Address = slave;
    Petit_Rx_Data.DataBuf[0] = (uint8_t)(start >> 8);
    Petit_Rx_Data.DataBuf[1] = (uint8_t)start;
    Petit_Rx_Data.DataBuf[2] = (uint8_t)(count >> 8);
    Petit_Rx_Data.DataBuf[3] = (uint8_t)count;
    Petit_Rx_Data.DataLen = 4;
    sends = errors = error_code = 0;
}
int test_transport(void)
{
    AmpCore_Init(0x1234u); AmpAdapter_Init();
    request(6u, 0u, HR_CONTROL, HR_CONTROL_ENABLE);
    HandlePetitModbusWriteSingleRegister();
    TC(!sends && !errors && amp_model.command.enable);
    request(6u, 0u, HR_CONTROL, HR_CONTROL_LEASE);
    HandlePetitModbusWriteSingleRegister();
    TC(!sends && !errors && amp_model.command.enable && amp_model.lease_ticks == 0);
    request(6u, 1u, HR_SW_CHECKSUM, 0u);
    HandlePetitModbusWriteSingleRegister();
    TC(errors == 1u && error_code == 2u && amp_model.checksum == 0x1234u);
    TC(PetitHoldingRegisters[HR_WRITE_RESULT].ActValue == 0x820Bu);
    request(6u, 0u, 65535u, 0xFFFFu);
    HandlePetitModbusWriteSingleRegister();
    TC(!sends && !errors && (amp_model.write_result & 0x0700u) == 0x0100u);
    request(6u, 1u, HR_COMMAND, 800u);
    HandlePetitModbusWriteSingleRegister();
    TC(errors == 1u && error_code == 3u && amp_model.command_echo == 399u);
    request(6u, 1u, HR_COMMAND, 0u);
    HandlePetitModbusWriteSingleRegister();
    TC(!errors && sends == 1u && amp_model.command_echo == 0u);
    TC(amp_model.write_result == 0x0400u);
    request(6u, 1u, HR_CONTROL, HR_CONTROL_DISABLE);
    HandlePetitModbusWriteSingleRegister();
    TC(!errors && sends == 1u && !amp_model.command.enable);
    request(6u, 1u, HR_CONTROL, HR_CONTROL_ENABLE);
    Petit_Rx_Data.DataLen = 3;
    HandlePetitModbusWriteSingleRegister();
    TC(errors == 1u && error_code == 3u && !amp_model.command.enable);
    /* FC16 config range, including broadcast validation. */
    request(16u, 0u, HR_CFG_RAMP, 2u);
    Petit_Rx_Data.DataLen = 9;
    Petit_Rx_Data.DataBuf[4] = 4;
    Petit_Rx_Data.DataBuf[5] = 2; Petit_Rx_Data.DataBuf[6] = 2;
    Petit_Rx_Data.DataBuf[7] = 0; Petit_Rx_Data.DataBuf[8] = 8;
    HandleMPetitodbusWriteMultipleRegisters();
    TC(!sends && !errors && amp_model.pending.accel_step == 2u && amp_model.pending.fast_stop_step == 8u);
    TC(!amp_model.status.config_ok);
    request(16u, 1u, HR_CFG_TAG, 2u); /* second register is read-only */
    Petit_Rx_Data.DataLen = 9; Petit_Rx_Data.DataBuf[4] = 4;
    Petit_Rx_Data.DataBuf[6] = 99;
    HandleMPetitodbusWriteMultipleRegisters();
    TC(errors == 1u && error_code == 2u && !amp_model.pending.tag);
    TC(amp_model.write_result == 0x8207u);
    request(16u, 0u, HR_CFG_TAG, 2u);
    Petit_Rx_Data.DataLen = 9; Petit_Rx_Data.DataBuf[4] = 4;
    Petit_Rx_Data.DataBuf[6] = 99;
    HandleMPetitodbusWriteMultipleRegisters();
    TC(!errors && !sends && !amp_model.pending.tag && amp_model.write_result == 0x8207u);
    request(16u, 0u, 65535u, 65535u);
    Petit_Rx_Data.DataLen = 5;
    HandleMPetitodbusWriteMultipleRegisters();
    TC(!errors && !sends && (amp_model.write_result & 0x0700u) == 0x0100u);
    request(16u, 1u, HR_CFG_RAMP, 2u);
    Petit_Rx_Data.DataLen = 8; Petit_Rx_Data.DataBuf[4] = 4;
    HandleMPetitodbusWriteMultipleRegisters();
    TC(errors == 1u && error_code == 3u);
    request(3u, 1u, 0u, 21u);
    HandlePetitModbusReadHoldingRegisters();
    TC(!errors && sends == 1u && Petit_Tx_Data.DataLen == 43u);
    TC(Petit_Tx_Data.DataBuf[15] == 0xF1u && Petit_Tx_Data.DataBuf[16] == 1u);
    request(3u, 1u, 65535u, 2u);
    HandlePetitModbusReadHoldingRegisters();
    TC(errors == 1u && error_code == 2u);
    request(3u, 1u, 0u, 0u);
    HandlePetitModbusReadHoldingRegisters();
    TC(errors == 1u && error_code == 2u);
    TC(transport_checks >= 19u);
    return 0;
}
