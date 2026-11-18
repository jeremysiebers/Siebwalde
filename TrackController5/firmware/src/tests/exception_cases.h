/* PV07-C exception-parsing cases, compiled against extracted production C.
 *
 * Exercises the REAL production receive path (CheckPetitModbusBufferComplete,
 * Petit_RxRTU, PROCESSxPETITxMODBUS) and the production exception readback
 * handler HandleMPetitodbusMbExceptionCodesSlaveReadback. The outstanding
 * request is staged directly (the TX state machine is not driven) and the CRC
 * is a reference implementation equivalent to the production lookup table.
 * Software-only: no UART, no timer, no hardware. */

static SLAVE_INFO slaves[NUMBER_OF_SLAVES_SIZE], dump[1];

static void reset_ex(void) {
    memset(slaves, 0, sizeof(slaves)); memset(dump, 0, sizeof(dump));
    MASTER_SLAVE_DATA = slaves; DUMP_SLAVE_DATA = dump; SlaveAmount = NUMBER_OF_SLAVES;
    Petit_Tx_State = PETIT_RXTX_IDLE; Petit_Tx_Current = 0;
    Petit_Rx_State = PETIT_RXTX_IDLE; Petit_Rx_Data_Available = FALSE;
    Petit_Rx_CRC16 = 0xFFFF; PetitReceiveCounter = 0; SlaveAnswerTimeoutCounter = 0;
    PetitModbusTimerValue = 0;
    g_lastReqAddr = 0; g_lastReqStart = 0; g_lastReqQty = 0;
    memset(&Petit_Tx_Data, 0, sizeof(Petit_Tx_Data));
    memset(&Petit_Rx_Data, 0, sizeof(Petit_Rx_Data));
    ev_clear();
}

/* Stage an outstanding request as if SENDxPETITxMODBUS had just dispatched it. */
static void stage(uint8_t addr, uint8_t fn) {
    Petit_Tx_State = PETIT_RXTX_WAIT_ANSWER;
    Petit_Tx_Data.Address = addr;
    Petit_Tx_Data.Function = fn;
    SlaveAnswerTimeoutCounter = 0;
    if (addr > SlaveAmount) dump[0].MbCommError = SLAVE_DATA_BUSY;
    else slaves[addr].MbCommError = SLAVE_DATA_BUSY;
}

static void stage_fc16(uint8_t addr, uint8_t reg, uint16_t value) {
    stage(addr, PETITMODBUS_WRITE_MULTIPLE_REGISTERS);
    Petit_Tx_Data.DataBuf[0] = 0;              /* start address hi */
    Petit_Tx_Data.DataBuf[1] = reg;            /* start address lo */
    Petit_Tx_Data.DataBuf[2] = 0;              /* quantity hi */
    Petit_Tx_Data.DataBuf[3] = 1;              /* quantity lo */
    Petit_Tx_Data.DataBuf[4] = 2;              /* byte count */
    Petit_Tx_Data.DataBuf[5] = (uint8_t)(value >> 8);
    Petit_Tx_Data.DataBuf[6] = (uint8_t)value;
}

static void stage_fc03(uint8_t addr, uint16_t start, uint16_t qty) {
    stage(addr, PETITMODBUS_READ_HOLDING_REGISTERS);
    g_lastReqAddr = addr; g_lastReqStart = start; g_lastReqQty = qty;
}

static void stage_fc06(uint8_t addr, uint16_t reg, uint16_t value) {
    stage(addr, PETITMODBUS_WRITE_SINGLE_REGISTER);
    Petit_Tx_Data.DataBuf[0] = (uint8_t)(reg >> 8);
    Petit_Tx_Data.DataBuf[1] = (uint8_t)reg;
    Petit_Tx_Data.DataBuf[2] = (uint8_t)(value >> 8);
    Petit_Tx_Data.DataBuf[3] = (uint8_t)value;
}

/* Build a 5-byte exception frame [addr][respFn][code][crc lo][crc hi]. */
static void inject_exception(uint8_t addr, uint8_t respFn, uint8_t code) {
    unsigned int crc = 0xFFFF; unsigned i;
    unsigned char f[3];
    f[0] = addr; f[1] = respFn; f[2] = code;
    for (i = 0; i < 3; ++i) Petit_CRC16(f[i], &crc);
    crc = CRC_ReverseValue((uint16_t)crc);
    PetitReceiveBuffer[0] = addr;
    PetitReceiveBuffer[1] = respFn;
    PetitReceiveBuffer[2] = code;
    PetitReceiveBuffer[3] = (unsigned char)(crc & 0xFF);
    PetitReceiveBuffer[4] = (unsigned char)((crc >> 8) & 0xFF);
    PetitReceiveCounter = 5;
}

static void inject_frame(const unsigned char *f, unsigned char n) {
    unsigned i;
    for (i = 0; i < n; ++i) PetitReceiveBuffer[i] = f[i];
    PetitReceiveCounter = n;
}

static void crc_bytes(const unsigned char *f, unsigned char n,
                      unsigned char *lo, unsigned char *hi) {
    unsigned int crc = 0xFFFF; unsigned i;
    for (i = 0; i < n; ++i) Petit_CRC16(f[i], &crc);
    crc = CRC_ReverseValue((uint16_t)crc);
    *lo = (unsigned char)(crc & 0xFF);
    *hi = (unsigned char)((crc >> 8) & 0xFF);
}

static void test_valid_fc16_exception(void) {
    reset_ex(); stage_fc16(6, 23, 0x0002);
    inject_exception(6, 0x90, 0x03);
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_EXCEPTION);
    assert(slaves[6].MbExceptionCode == 0x03);
    assert(slaves[6].MbReceiveCounter == 1);
    assert(evn == 1);
    assert(evs[0].type == MBUS_EV_EXCEPTION && evs[0].slave == 6 &&
           evs[0].reg == 16 && evs[0].rv == 0x90 && evs[0].val == 0x03);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);
    puts("PASS: FC16 exception 0x90/0x03 recognized, correlated and recorded\n");
}

static void test_other_exception_fns(void) {
    /* FC03 exception 0x83/0x02 (illegal data address). */
    reset_ex(); stage_fc03(6, 8, 1);
    inject_exception(6, 0x83, 0x02);
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_EXCEPTION);
    assert(evs[0].type == MBUS_EV_EXCEPTION && evs[0].reg == 3 &&
           evs[0].rv == 0x83 && evs[0].val == 0x02);

    /* FC06 exception 0x86/0x02. */
    reset_ex(); stage_fc06(6, 9, 0x0000);
    inject_exception(6, 0x86, 0x02);
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_EXCEPTION);
    assert(evs[0].type == MBUS_EV_EXCEPTION && evs[0].reg == 6 &&
           evs[0].rv == 0x86 && evs[0].val == 0x02);

    /* FC04/FC08 exception fns (0x84/0x88): frame-recognized (fixed 5 bytes) but
     * cannot correlate because this master never issues FC04/FC08. */
    reset_ex();
    PetitReceiveBuffer[0]=6; PetitReceiveBuffer[1]=0x84; PetitReceiveBuffer[2]=0x02;
    PetitReceiveBuffer[3]=0; PetitReceiveBuffer[4]=0; PetitReceiveCounter=5;
    assert(CheckPetitModbusBufferComplete() == PETIT_DATA_READY);
    reset_ex(); stage_fc16(6, 23, 0x0002);
    inject_exception(6, 0x84, 0x02);
    PROCESSxPETITxMODBUS();
    assert(evn == 0 && slaves[6].MbCommError == SLAVE_DATA_BUSY);
    reset_ex(); stage_fc16(6, 23, 0x0002);
    inject_exception(6, 0x88, 0x02);
    PROCESSxPETITxMODBUS();
    assert(evn == 0 && slaves[6].MbCommError == SLAVE_DATA_BUSY);
    puts("PASS: 0x83/0x86 correlate and record; 0x84/0x88 recognized then rejected\n");
}

static void test_invalid_crc(void) {
    reset_ex(); stage_fc16(6, 23, 0x0002);
    inject_exception(6, 0x90, 0x03);
    PetitReceiveBuffer[3] ^= 0xFF;              /* corrupt CRC low byte */
    PROCESSxPETITxMODBUS();
    assert(evn == 0);
    assert(slaves[6].MbCommError == SLAVE_DATA_BUSY);
    assert(Petit_Rx_Data_Available == FALSE);
    puts("PASS: exception frame with corrupt CRC dropped (no event, no exception)\n");
}

static void test_wrong_address(void) {
    reset_ex(); stage_fc16(6, 23, 0x0002);
    inject_exception(7, 0x90, 0x03);            /* slave 7 != outstanding 6 */
    PROCESSxPETITxMODBUS();
    assert(evn == 0);
    assert(slaves[6].MbCommError == SLAVE_DATA_BUSY);
    assert(slaves[7].MbCommError != SLAVE_DATA_EXCEPTION);
    puts("PASS: wrong-slave-address exception rejected (no event, no exception)\n");
}

static void test_unexpected_fn(void) {
    /* Supported fn (0x83 = FC03) but not the outstanding FC16 request. */
    reset_ex(); stage_fc16(6, 23, 0x0002);
    inject_exception(6, 0x83, 0x02);
    PROCESSxPETITxMODBUS();
    assert(evn == 0 && slaves[6].MbCommError == SLAVE_DATA_BUSY);

    /* Unsupported request fn (0x85 = FC05, master never issues it) is discarded
     * at the frame level exactly as before. */
    reset_ex(); stage_fc16(6, 23, 0x0002);
    PetitReceiveBuffer[0]=6; PetitReceiveBuffer[1]=0x85; PetitReceiveBuffer[2]=0x01;
    PetitReceiveBuffer[3]=0; PetitReceiveBuffer[4]=0; PetitReceiveCounter=5;
    assert(CheckPetitModbusBufferComplete() == PETIT_FALSE_FUNCTION);
    assert(PetitReceiveCounter == 0);
    puts("PASS: unexpected exception fn rejected at handler or frame level\n");
}

static void test_no_outstanding_request(void) {
    /* No request staged: Petit_Tx_Data is zeroed, so correlation must fail. */
    reset_ex();
    inject_exception(6, 0x90, 0x03);
    PROCESSxPETITxMODBUS();
    assert(evn == 0);
    assert(slaves[6].MbCommError != SLAVE_DATA_EXCEPTION);
    puts("PASS: exception without a matching outstanding request is rejected\n");
}

static void test_too_short(void) {
    reset_ex(); stage_fc16(6, 23, 0x0002);
    /* Only 4 bytes of a 5-byte exception frame must not be dispatched. */
    PetitReceiveBuffer[0]=6; PetitReceiveBuffer[1]=0x90; PetitReceiveBuffer[2]=0x03;
    PetitReceiveBuffer[3]=0; PetitReceiveCounter=4;
    PROCESSxPETITxMODBUS();
    assert(evn == 0);
    assert(slaves[6].MbCommError == SLAVE_DATA_BUSY);
    assert(Petit_Rx_Data_Available == FALSE);
    puts("PASS: too-short (4-byte) exception frame not dispatched\n");
}

static void test_timeout(void) {
    reset_ex(); stage_fc16(6, 23, 0x0002);
    SlaveAnswerTimeoutCounter = 1;              /* answer-timeout callback fired */
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_TIMEOUT);
    assert(evn == 1 && evs[0].type == MBUS_EV_TIMEOUT);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);
    puts("PASS: no-response answer timeout yields TIMEOUT (delivery not excluded)\n");
}

static void test_late_after_timeout(void) {
    reset_ex(); stage_fc16(6, 23, 0x0002);
    SlaveAnswerTimeoutCounter = 1;
    PROCESSxPETITxMODBUS();                     /* timeout fires */
    assert(slaves[6].MbCommError == SLAVE_DATA_TIMEOUT);
    inject_exception(6, 0x90, 0x03);            /* late correlated exception */
    PROCESSxPETITxMODBUS();
    /* Documented: a late exception is processed like a late normal echo and
     * overwrites the timeout result (consistent with existing late-response
     * handling; not a safety change). */
    assert(slaves[6].MbCommError == SLAVE_DATA_EXCEPTION);
    assert(evn == 2 && evs[0].type == MBUS_EV_TIMEOUT && evs[1].type == MBUS_EV_EXCEPTION);
    puts("PASS: late exception after timeout overwrites TIMEOUT (late-echo-consistent)\n");
}

static void test_fc16_ack(void) {
    unsigned char f[6]; unsigned char lo, hi, frame[8]; unsigned i;
    reset_ex(); stage_fc16(6, 23, 0x0002);
    f[0]=6; f[1]=0x10; f[2]=0; f[3]=23; f[4]=0; f[5]=1;   /* echo: addr, fn, start, qty */
    crc_bytes(f, 6, &lo, &hi);
    for (i = 0; i < 6; ++i) frame[i] = f[i];
    frame[6] = lo; frame[7] = hi;
    inject_frame(frame, 8);
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_OK);
    assert(evn == 1 && evs[0].type == MBUS_EV_ACK && evs[0].slave == 6);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);
    puts("PASS: FC16 normal echo still yields OK + ACK (regression)\n");
}

static void test_fc03_ack(void) {
    unsigned char f[5]; unsigned char lo, hi, frame[7]; unsigned i;
    reset_ex(); stage_fc03(6, 8, 1);
    f[0]=6; f[1]=0x03; f[2]=2; f[3]=0xC7; f[4]=0x08;
    crc_bytes(f, 5, &lo, &hi);
    for (i = 0; i < 5; ++i) frame[i] = f[i];
    frame[5] = lo; frame[6] = hi;
    inject_frame(frame, 7);
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_OK);
    assert(slaves[6].HoldingReg[8] == 0xC708);
    puts("PASS: FC03 normal read still yields OK and fills holding register (regression)\n");
}

static void test_fc06_ack(void) {
    unsigned char f[6]; unsigned char lo, hi, frame[8]; unsigned i;
    reset_ex(); stage_fc06(6, 1, 0x8000);
    f[0]=6; f[1]=0x06; f[2]=0; f[3]=1; f[4]=0x80; f[5]=0x00;
    crc_bytes(f, 6, &lo, &hi);
    for (i = 0; i < 6; ++i) frame[i] = f[i];
    frame[6] = lo; frame[7] = hi;
    inject_frame(frame, 8);
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_OK);
    puts("PASS: FC06 normal echo still yields OK (regression)\n");
}

static void test_fc04_default_not_exception(void) {
    /* FC04/FC08 are #ifdef'd out, so their normal responses reach the default
     * handler. The new handler must NOT report a non-exception fn as EXCEPTION. */
    unsigned char f[5]; unsigned char lo, hi, frame[7]; unsigned i;
    reset_ex(); stage_fc16(6, 23, 0x0002);
    f[0]=6; f[1]=0x04; f[2]=2; f[3]=0x00; f[4]=0x01;
    crc_bytes(f, 5, &lo, &hi);
    for (i = 0; i < 5; ++i) frame[i] = f[i];
    frame[5] = lo; frame[6] = hi;
    inject_frame(frame, 7);
    PROCESSxPETITxMODBUS();
    assert(evn == 0);
    assert(slaves[6].MbCommError != SLAVE_DATA_EXCEPTION);
    puts("PASS: FC04 (disabled) normal response no longer misreported as EXCEPTION\n");
}

static void test_partial_fc16_echo_preserves_wait_answer(void) {
    /* A segmented/corrupt FC16 echo (>=5 bytes but never the full 8-byte frame)
     * must NOT release WAIT_ANSWER. The answer timeout must still fire and
     * classify the slot-select as TIMEOUT (the slot is NOT marked detected). */
    unsigned char partial[5] = {6, 0x10, 0, 23, 0};   /* 5 of the 8 echo bytes */
    reset_ex(); stage_fc16(6, 23, 0x0002);
    inject_frame(partial, 5);
    assert(CheckPetitModbusBufferComplete() == PETIT_DATA_NOT_READY);
    assert(Petit_Tx_State == PETIT_RXTX_WAIT_ANSWER); /* WAIT_ANSWER preserved */
    PROCESSxPETITxMODBUS();                          /* answer not yet timed out */
    assert(Petit_Tx_State == PETIT_RXTX_WAIT_ANSWER);
    assert(slaves[6].MbCommError == SLAVE_DATA_BUSY); /* not classified yet */
    SlaveAnswerTimeoutCounter = 1;                   /* answer window elapsed */
    PROCESSxPETITxMODBUS();                          /* answer-timeout branch fires */
    assert(slaves[6].MbCommError == SLAVE_DATA_TIMEOUT);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);
    puts("PASS: partial FC16 echo (>=5 bytes, never DATA_READY) preserves WAIT_ANSWER so the answer timeout classifies TIMEOUT\n");
}

static void test_fc16_mismatched_echo_preserves_wait_answer(void) {
    /* A CRC-valid FC16 echo whose start address / quantity does NOT correlate with
     * the outstanding request must NOT record a terminal MbCommError and must NOT
     * idle the TX. WAIT_ANSWER must stay intact so the answer timeout still fires
     * and classifies the slot-select as TIMEOUT. This guards the readback-handler
     * premature-idle fix (PetitModbus.c HandleMPetitodbusWriteMultipleRegistersSlaveReadback). */
    unsigned char f[6]; unsigned char lo, hi, frame[8]; unsigned i;
    reset_ex(); stage_fc16(6, 23, 0x0002);
    f[0]=6; f[1]=0x10; f[2]=0; f[3]=99; f[4]=0; f[5]=1;   /* start address 99 != 23 */
    crc_bytes(f, 6, &lo, &hi);
    for (i = 0; i < 6; ++i) frame[i] = f[i];
    frame[6] = lo; frame[7] = hi;
    inject_frame(frame, 8);
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_BUSY);   /* no terminal recorded */
    assert(Petit_Tx_State == PETIT_RXTX_WAIT_ANSWER);   /* WAIT_ANSWER preserved */
    SlaveAnswerTimeoutCounter = 1;                      /* answer window elapsed */
    PROCESSxPETITxMODBUS();
    assert(slaves[6].MbCommError == SLAVE_DATA_TIMEOUT);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);
    puts("PASS: CRC-valid mismatched FC16 echo preserves WAIT_ANSWER so the answer timeout classifies TIMEOUT\n");
}

int main(void) {
    test_valid_fc16_exception();
    test_other_exception_fns();
    test_invalid_crc();
    test_wrong_address();
    test_unexpected_fn();
    test_no_outstanding_request();
    test_too_short();
    test_timeout();
    test_late_after_timeout();
    test_fc16_ack();
    test_fc03_ack();
    test_fc06_ack();
    test_fc04_default_not_exception();
    test_partial_fc16_echo_preserves_wait_answer();
    test_fc16_mismatched_echo_preserves_wait_answer();
    ExitProcess(0);
    return 0;
}
