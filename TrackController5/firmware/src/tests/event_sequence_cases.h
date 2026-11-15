/* PV07-C end-to-end event-ring sequence demonstration (offline, no hardware).
 *
 * Drives the REAL production event ring (MBUS_EVENT_PUSH / MBUS_EVENT_REPORT
 * extracted verbatim from slavecommhandler.c) together with the REAL production
 * push sites:
 *   - ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX() -> MBUS_EV_ENQUEUE
 *   - SLAVExCOMMUNICATIONxHANDLER()              -> MBUS_EV_GATE_DROP / MBUS_EV_FC16_SENT
 *   - Petit_TxRTU()                              -> MBUS_EV_TX_DONE
 *   - HandleMPetitodbusWriteMultipleRegistersSlaveReadback() -> MBUS_EV_ACK
 *   - HandleMPetitodbusMbExceptionCodesSlaveReadback()       -> MBUS_EV_EXCEPTION
 *   - PROCESSxPETITxMODBUS() answer-timeout      -> MBUS_EV_TIMEOUT
 *   - ENQUEUExGLOBALxSTOP()                      -> MBUS_EV_PURGE
 *
 * The recording LOG_Printf sink (defined in the harness prefix) captures the
 * exact formatted lines MBUS_EVENT_REPORT would send to the TCP log (port
 * 50000), plus a structured copy (cap_events[]) for deterministic assertions.
 *
 * Scenarios prove the ring can distinguish the five terminal outcomes:
 *   ACK, EXCEPTION, TIMEOUT, GATE_DROP, PURGE
 * and that the bounded ring drops the NEWEST event on overflow.
 *
 * Software-only: no UART, no timer, no hardware, no live TCP. */

static SLAVE_INFO slaves[NUMBER_OF_SLAVES_SIZE], dump[1];


static void reset_demo(void) {
    memset(slaves, 0, sizeof(slaves)); memset(dump, 0, sizeof(dump));
    MASTER_SLAVE_DATA = slaves; DUMP_SLAVE_DATA = dump;
    SlaveAmount = NUMBER_OF_SLAVES;
    memset(AmplifierCompatible, 0, sizeof(AmplifierCompatible));
    memset(StopDelivery, 0, sizeof(StopDelivery));
    memset(StopAttempts, 0, sizeof(StopAttempts));
    StopSlave = 0; StopReportReason = 0; StopWord = 0; StopState = 0;
    StopStarted = 0; LastLeaseRenewal = 0;
    Rcd_Ptr = Rcd_Ptr_prev = &Rcd[0];
    Petit_Tx_State = PETIT_RXTX_IDLE; Petit_Tx_Current = 0;
    Petit_Rx_State = PETIT_RXTX_IDLE; Petit_Rx_Data_Available = FALSE;
    Petit_Rx_CRC16 = 0xFFFF; Petit_Tx_CRC16 = 0xFFFF; Petit_Tx_Buf_Size = 0;
    PetitReceiveCounter = 0; SlaveAnswerTimeoutCounter = 0; PetitModbusTimerValue = 0;
    memset(&Petit_Tx_Data, 0, sizeof(Petit_Tx_Data));
    memset(&Petit_Rx_Data, 0, sizeof(Petit_Rx_Data));
    memset(&Data, 0, sizeof(Data));
    memset(ReadData, 0, sizeof(ReadData)); memset(WriteData, 0, sizeof(WriteData));
    GlobalStopLatch = GLOBAL_STOP_DISARMED;
    LastHostHeartbeat = 0; GlobalStopReason = 0;
    wire_count = 0; now = 0;
    cap_reset();
}

/* --- frame injection helpers (reference CRC, bit-equivalent to production) --- */

static void crc_bytes(const unsigned char *f, unsigned char n,
                      unsigned char *lo, unsigned char *hi) {
    unsigned int crc = 0xFFFF; unsigned i;
    for (i = 0; i < n; ++i) Petit_CRC16(f[i], &crc);
    crc = CRC_ReverseValue((uint16_t)crc);
    *lo = (unsigned char)(crc & 0xFF);
    *hi = (unsigned char)((crc >> 8) & 0xFF);
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

/* Build a valid FC16 normal echo [6][0x10][0][23][0][1][crc lo][crc hi]. */
static void inject_fc16_echo(uint8_t addr, uint8_t reg) {
    unsigned char f[6], frame[8], lo, hi; unsigned i;
    f[0] = addr; f[1] = 0x10; f[2] = 0; f[3] = reg; f[4] = 0; f[5] = 1;
    crc_bytes(f, 6, &lo, &hi);
    for (i = 0; i < 6; ++i) frame[i] = f[i];
    frame[6] = lo; frame[7] = hi;
    for (i = 0; i < 8; ++i) PetitReceiveBuffer[i] = frame[i];
    PetitReceiveCounter = 8;
}

/* --- scenario drivers --- */

/* Enqueue a command-108 FC16 write to slave 6, HR23 (GROUP_READY), value 0x0002,
 * through the real mailbox gate. Returns false if the gate refused the write. */
static bool enqueue_group_ready(void) {
    uint8_t write[8] = {6, WRITE, 1, 23, 0x02, 0x00, 0, 0};
    ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(write);
    return true;
}

/* Stage the SAME write in the global Data struct, ready for dispatch. */
static void stage_dispatch_write(void) {
    Data.SlaveAddress  = 6;
    Data.Direction     = WRITE;
    Data.NoOfRegisters = 1;
    Data.StartRegister = 23;
    Data.RegData0      = 0x0002;
    Data.RegData1      = 0;
}

/* Dispatch (SLAVExCOMMUNICATIONxHANDLER) + first PROCESSxPETITxMODBUS pass to
 * physically send the frame (FC16_SENT then TX_DONE). */
static void dispatch_and_tx(void) {
    SLAVExCOMMUNICATIONxHANDLER();       /* -> MBUS_EV_FC16_SENT (rv = SEND result) */
    PROCESSxPETITxMODBUS();              /* Petit_TxRTU -> MBUS_EV_TX_DONE          */
}

static void expect_event(unsigned i, unsigned type, unsigned slave,
                         unsigned reg, unsigned val, unsigned rv) {
    assert(i < cap_count);
    assert(cap_events[i].type == type);
    assert(cap_events[i].slave == slave);
    assert(cap_events[i].reg == reg);
    assert(cap_events[i].val == val);
    assert(cap_events[i].rv == rv);
}

static void show_capture(const char *title) {
    puts(title);
    puts("\n  ring sequence reported by MBUS_EVENT_REPORT (== TCP port 50000 lines):\n");
    if (cap_text_len) puts(cap_text);
    puts("  ----------------------------------------------------------------------\n");
}

/* 1) EXCEPTION: ENQUEUE -> FC16_SENT -> TX_DONE -> EXCEPTION(0x90/0x03). */
static void scenario_exception(void) {
    reset_demo();
    AmplifierCompatible[6] = true;
    enqueue_group_ready();
    stage_dispatch_write();
    dispatch_and_tx();
    inject_exception(6, 0x90, 0x03);     /* PIC18 rejects GROUP_READY (illegal data) */
    PROCESSxPETITxMODBUS();
    MBUS_EVENT_REPORT();

    assert(slaves[6].MbCommError == SLAVE_DATA_EXCEPTION);
    assert(cap_count == 4);
    expect_event(0, MBUS_EV_ENQUEUE,   6, 23, 0x0002, 0);
    expect_event(1, MBUS_EV_FC16_SENT, 6, 23, 0x0002, 1);
    expect_event(2, MBUS_EV_TX_DONE,   6, 23, 0x0002, 0);
    expect_event(3, MBUS_EV_EXCEPTION, 6, 16 /*fn*/, 0x03 /*code*/, 0x90 /*respFn*/);
    show_capture("SCENARIO EXCEPTION (delivered + rejected):");
}

/* 2) ACK: ENQUEUE -> FC16_SENT -> TX_DONE -> ACK. */
static void scenario_ack(void) {
    reset_demo();
    AmplifierCompatible[6] = true;
    enqueue_group_ready();
    stage_dispatch_write();
    dispatch_and_tx();
    inject_fc16_echo(6, 23);            /* PIC18 echoes the FC16 (accepted)       */
    PROCESSxPETITxMODBUS();
    MBUS_EVENT_REPORT();

    assert(slaves[6].MbCommError == SLAVE_DATA_OK);
    assert(cap_count == 4);
    expect_event(0, MBUS_EV_ENQUEUE,   6, 23, 0x0002, 0);
    expect_event(1, MBUS_EV_FC16_SENT, 6, 23, 0x0002, 1);
    expect_event(2, MBUS_EV_TX_DONE,   6, 23, 0x0002, 0);
    expect_event(3, MBUS_EV_ACK,       6, 23, 0,      0);
    show_capture("SCENARIO ACK (delivered + accepted):");
}

/* 3) TIMEOUT: ENQUEUE -> FC16_SENT -> TX_DONE -> TIMEOUT (delivery NOT excluded). */
static void scenario_timeout(void) {
    reset_demo();
    AmplifierCompatible[6] = true;
    enqueue_group_ready();
    stage_dispatch_write();
    dispatch_and_tx();
    SlaveAnswerTimeoutCounter = 1;      /* answer-window elapsed, no echo         */
    PROCESSxPETITxMODBUS();
    MBUS_EVENT_REPORT();

    assert(slaves[6].MbCommError == SLAVE_DATA_TIMEOUT);
    assert(cap_count == 4);
    expect_event(0, MBUS_EV_ENQUEUE,   6, 23, 0x0002, 0);
    expect_event(1, MBUS_EV_FC16_SENT, 6, 23, 0x0002, 1);
    expect_event(2, MBUS_EV_TX_DONE,   6, 23, 0x0002, 0);
    expect_event(3, MBUS_EV_TIMEOUT,   6, 23, 0,      0);
    show_capture("SCENARIO TIMEOUT (not delivered / delivery not excluded):");
}

/* 4) GATE_DROP: enqueued while compatible, dropped at dispatch (now incompatible). */
static void scenario_gate_drop(void) {
    reset_demo();
    AmplifierCompatible[6] = true;
    enqueue_group_ready();
    AmplifierCompatible[6] = false;    /* amp becomes incompatible before dispatch */
    stage_dispatch_write();
    SLAVExCOMMUNICATIONxHANDLER();      /* -> MBUS_EV_GATE_DROP                    */
    MBUS_EVENT_REPORT();

    assert(slaves[6].MbCommError == SLAVE_DATA_NOK);
    assert(cap_count == 2);
    expect_event(0, MBUS_EV_ENQUEUE,    6, 23, 0x0002, 0);
    expect_event(1, MBUS_EV_GATE_DROP,  6, 23, 0x0002, 0);
    show_capture("SCENARIO GATE_DROP (not delivered, gated at dispatch):");
}

/* 5) PURGE: enqueued, then the global-stop latch purges the mailbox. */
static void scenario_purge(void) {
    reset_demo();
    AmplifierCompatible[6] = true;
    enqueue_group_ready();
    ENQUEUExGLOBALxSTOP(0x0400);        /* stop latch drops enqueued-but-undispatched */
    MBUS_EVENT_REPORT();

    assert(cap_count == 2);
    expect_event(0, MBUS_EV_ENQUEUE, 6, 23, 0x0002, 0);
    expect_event(1, MBUS_EV_PURGE,   0, 0,  0x0400, 0);
    show_capture("SCENARIO PURGE (discarded by global-stop latch):");
}

/* 6) Ring overflow: push 64 events without draining. The bounded ring keeps the
 *    first 63 (MBUS_EV_RING-1) and drops the NEWEST (the 64th). */
static void scenario_overflow(void) {
    unsigned i;
    reset_demo();
    for (i = 0; i < 64; ++i) {
        MBUS_EVENT_PUSH(MBUS_EV_FC16_SENT, 6, 23, (uint16_t)i, 0u);
    }
    MBUS_EVENT_REPORT();               /* drains only what the ring actually kept */

    assert(cap_count == 63);           /* 64 pushed, 63 kept, 1 dropped */
    assert(cap_events[0].val == 0);    /* oldest survived  -> drop-NEWEST */
    assert(cap_events[62].val == 62);  /* newest kept is #62; #63 was discarded */
    puts("SCENARIO OVERFLOW (bounded ring, drop-newest):\n");
    puts("  pushed 64 events, ring capacity 63 (MBUS_EV_RING-1 sentinel)\n");
    puts("  events captured after drain: 63 (event #63 discarded)\n");
    puts("  first captured: val=0 (oldest kept);  last captured: val=62 (newest kept)\n");
    puts("  => the 64th (newest) event was dropped, proving drop-newest semantics\n\n");
}

int main(void) {
    scenario_exception();
    scenario_ack();
    scenario_timeout();
    scenario_gate_drop();
    scenario_purge();
    scenario_overflow();
    puts("PASS: event ring distinguishes ACK / EXCEPTION / TIMEOUT / GATE_DROP / PURGE\n");
    ExitProcess(0);
    return 0;
}
