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
static void checksum_request(uint8_t fn, uint8_t address, uint16_t value)
{
    request(fn, address, HR_SW_CHECKSUM, fn == 6u ? value : 1u);
    if (fn == 16u) {
        Petit_Rx_Data.DataLen = 7u;
        Petit_Rx_Data.DataBuf[4] = 2u;
        Petit_Rx_Data.DataBuf[5] = (uint8_t)(value >> 8);
        Petit_Rx_Data.DataBuf[6] = (uint8_t)value;
    }
    if (fn == 6u) HandlePetitModbusWriteSingleRegister();
    else HandleMPetitodbusWriteMultipleRegisters();
}
static int test_bootloader(void)
{
    uint8_t fn;
    for (fn = 6u; fn <= 16u; fn += 10u) {
        AmpCore_Init(0x1234u); AmpAdapter_Init();
        Config = 1u; Startup_Machine = 1u; MODBUS_ADDRESS = 0xAAu;
        PETITMODBUS_SLAVE_ADDRESS = 0xAAu;
        checksum_request(fn, 0xAAu, 1u); /* only zero is the bootloader strobe */
        TC(errors == 1u && error_code == 2u && amp_model.checksum == 0x1234u);
        checksum_request(fn, 0u, 0u); /* broadcasts must never reset a slave */
        TC(!errors && !sends && amp_model.checksum == 0x1234u);
        Startup_Machine = 0u;
        checksum_request(fn, 0xAAu, 0u);
        TC(errors == 1u && error_code == 2u && amp_model.checksum == 0x1234u);
        Startup_Machine = 1u;
        checksum_request(fn, 0xAAu, 0u);
        TC(!errors && sends == 1u && amp_model.checksum == 0u);
        AmpAdapter_Refresh();
        TC(PetitHoldingRegisters[HR_SW_CHECKSUM].ActValue == 0u);
        ID_PORT = 0u; /* keep boot-address state-1 window active */
        service_startup(); /* unchanged main.c state-1 body */
        TC(Startup_Machine == 2u);
        amp_model.checksum = 0x1234u;
        checksum_request(fn, 0xAAu, 0u); /* window closed once in state 2 */
        TC(errors == 1u && error_code == 2u && amp_model.checksum == 0x1234u);

        /* Runtime-address write after valid configuration remains read-only. */
        AmpCore_Init(0x1234u); AmpAdapter_Init();
        TC(AmpAdapter_OnWrite(HR_ADDRESS, 1u, false) == AMP_WRITE_OK);
        AmpAdapter_AddressAssigned();
        TC(AmpAdapter_OnWrite(HR_CFG_RAMP, 0x0202u, false) == AMP_WRITE_OK);
        TC(AmpAdapter_OnWrite(HR_CFG_FAST_STOP, 8u, false) == AMP_WRITE_OK);
        TC(AmpAdapter_OnWrite(HR_CFG_WATCHDOG, 250u, false) == AMP_WRITE_OK);
        TC(AmpAdapter_OnWrite(HR_CONTROL, HR_CONTROL_APPLY, false) == AMP_WRITE_OK);
        TC(amp_model.status.config_ok && amp_model.status.id_set);
        Config = 0u; Startup_Machine = 0u; MODBUS_ADDRESS = 1u;
        PETITMODBUS_SLAVE_ADDRESS = 1u;
        checksum_request(fn, 1u, 0u);
        TC(errors == 1u && error_code == 2u && amp_model.checksum == 0x1234u);
        TC(PetitHoldingRegisters[HR_WRITE_RESULT].ActValue == 0x820Bu);
        checksum_request(fn, 0xAAu, 0u); /* stale boot address cannot reopen runtime */
        TC(errors == 1u && error_code == 2u && amp_model.checksum == 0x1234u);
    }
    return 0;
}
int test_transport(void)
{
    TC(test_bootloader() == 0);
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
    request(3u, 1u, 0u, 23u);
    HandlePetitModbusReadHoldingRegisters();
    TC(!errors && sends == 1u && Petit_Tx_Data.DataLen == 47u);
    TC(Petit_Tx_Data.DataBuf[15] == 0xF1u && Petit_Tx_Data.DataBuf[16] == 1u);
    TC(Petit_Tx_Data.DataBuf[43] == 0x01u && Petit_Tx_Data.DataBuf[44] == 0x00u); /* HR21 = 0x0100 */
    TC(Petit_Tx_Data.DataBuf[45] == 0x03u && Petit_Tx_Data.DataBuf[46] == 0x01u); /* HR22 = 0x0301 */
    request(3u, 1u, 65535u, 2u);
    HandlePetitModbusReadHoldingRegisters();
    TC(errors == 1u && error_code == 2u);
    request(3u, 1u, 0u, 0u);
    HandlePetitModbusReadHoldingRegisters();
    TC(errors == 1u && error_code == 2u);
    TC(transport_checks >= 19u);
    return 0;
}
