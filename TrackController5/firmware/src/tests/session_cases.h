/* Software-only regression cases for the corrected Ethernet/controller session
 * state machines. Exercises the REAL ETHERNET_Tasks / CONTROLLER_Tasks and the
 * REAL ring mailboxes against test-controlled Harmony UDP/TCP + MBUS stubs. */

static void sreset(void) {
    ethernetData.state = ETHERNET_TCPIP_OPENING_SERVER;
    ethernetData.recvsocket = INVALID_SOCKET;
    ethernetData.transsocket = INVALID_SOCKET;
    TransSocketReady = false;
    M_Box_Eth_Recv_Ptr = &udpRecvBox[0];
    M_Box_Eth_Recv_Ptr_prev = &udpRecvBox[0];
    M_Box_Eth_Send_Ptr = &udpTransBox[0];
    M_Box_Eth_Send_Ptr_prev = &udpTransBox[0];
    RecvMailboxCount = 0;
    SendMailboxCount = 0;
    RecvOverflowCount = 0;
    SendOverflowCount = 0;
    memset(udpRecvBox, 0, sizeof(udpRecvBox));
    memset(udpTransBox, 0, sizeof(udpTransBox));
    controllerData.state = CONTROLLER_STATE_INIT;
    ControllerCommand = 0;
    EthernetRecvData = NULL;
    GlobalStopLatch = GLOBAL_STOP_DISARMED;
    LastHostHeartbeat = 0;
    GlobalStopReason = 0;
    mbus_state = MBUS_STATE_WAIT;
    recv_connected = false;
    trans_connected = false;
    rx_avail = 0;
    memset(&rx_frame, 0, sizeof(rx_frame));
    client_ip.Val = 0;
    last_client_open_ip.Val = 0;
    recv_open_count = 0;
    trans_open_count = 0;
    recv_close_count = 0;
    trans_close_count = 0;
    sent_count = 0;
    memset(&sent_frame, 0, sizeof(sent_frame));
}

static int send_is_connected(udpTrans_t *f) {
    return f != NULL && f->header == HEADER && f->command == CONTROLLER &&
           f->data[0] == CONNECTED && f->data[1] == DONE;
}

/* Drive ETHERNET_Tasks from OPENING_SERVER through WAIT_FOR_CONNECTION to
 * DATA_RX, opening the transsocket to `ip`. */
static void open_and_connect(uint32_t ip) {
    int before = trans_open_count;
    client_ip.Val = ip;
    ETHERNET_Tasks();   /* OPENING_SERVER -> WAIT_FOR_CONNECTION */
    assert(ethernetData.state == ETHERNET_TCPIP_WAIT_FOR_CONNECTION);
    recv_connected = true;
    ETHERNET_Tasks();   /* WAIT_FOR_CONNECTION -> DATA_RX, opens transsocket */
    assert(ethernetData.state == ETHERNET_TCPIP_DATA_RX);
    assert(TransSocketReady == true);
    assert(ethernetData.transsocket == TRANS_SOCK);
    assert(trans_open_count == before + 1);
    assert(last_client_open_ip.Val == ip);
}

static void put_250(void) {
    udpTrans_t frame;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)CLIENT_CONNECTION_REQUEST;
    PutDataInReceiveMailBox(frame);
}

/* (a) Mailbox saturation: the 8-slot recv ring must not silently overwrite, the
 * GET helper must return NULL when empty, and a 250 must still be answered once
 * the ring has room. */
static void test_mailbox_saturation_and_connect(void) {
    udpTrans_t frame, *got;
    int k;
    sreset();
    for (k = 0; k < MAILBOXSIZE; ++k) {
        memset(&frame, 0, sizeof(frame));
        frame.header = (uint8_t)HEADER;
        frame.command = (uint8_t)EXEC_MBUS_SLAVE_DATA_EXCH;   /* non-250 */
        frame.data[0] = (uint8_t)k;
        PutDataInReceiveMailBox(frame);
        assert(RecvMailboxCount == (uint8_t)(k + 1));
    }
    assert(CHECKxDATAxINxRECEIVExMAILxBOX() == true);
    /* A 250 that arrives while full is refused and counted, never overwrites. */
    put_250();
    assert(RecvMailboxCount == MAILBOXSIZE);
    assert(RecvOverflowCount == 1);
    /* Draining returns the 8 original frames in FIFO order, then NULL. */
    for (k = 0; k < MAILBOXSIZE; ++k) {
        got = GETxDATAxFROMxRECEIVExMAILxBOX();
        assert(got != NULL && got->header == HEADER);
        assert(got->command == (uint8_t)EXEC_MBUS_SLAVE_DATA_EXCH);
        assert(got->data[0] == (uint8_t)k);
    }
    assert(!CHECKxDATAxINxRECEIVExMAILxBOX());
    assert(GETxDATAxFROMxRECEIVExMAILxBOX() == NULL);
    /* With room again, a 250 is accepted and answered. */
    put_250();
    controllerData.state = CONTROLLER_STATE_WAITING_FOR_CLIENT;
    CONTROLLER_Tasks();
    assert(controllerData.state == CONTROLLER_STATE_IDLE);
    assert(CheckDataInSendMailBox());
    got = GetDataFromSendMailBox();
    assert(send_is_connected(got));
    puts("PASS: recv ring full/empty guard; explicit drop counter; empty GET returns NULL; 250 answered\n");
}

/* (b) A 250 must be answered even while MBUS is busy, without disturbing MBUS,
 * and without letting a subsequent non-250 command through until MBUS is idle. */
static void test_connect_while_mbus_busy(void) {
    udpTrans_t frame, *got;
    sreset();
    controllerData.state = CONTROLLER_STATE_CHECK_MBUS_STATE;
    mbus_state = MBUS_STATE_SLAVE_DETECT;
    put_250();
    CONTROLLER_Tasks();
    /* 250 answered, but the controller must stay in CHECK_MBUS_STATE (not IDLE). */
    assert(controllerData.state == CONTROLLER_STATE_CHECK_MBUS_STATE);
    assert(mbus_state == MBUS_STATE_SLAVE_DETECT);
    assert(CheckDataInSendMailBox());
    got = GetDataFromSendMailBox();
    assert(send_is_connected(got));

    /* A subsequent non-250 command stays queued while MBUS is still busy. */
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)EXEC_MBUS_STATE_SLAVES_ON;
    PutDataInReceiveMailBox(frame);
    CONTROLLER_Tasks();
    assert(controllerData.state == CONTROLLER_STATE_CHECK_MBUS_STATE);
    assert(CHECKxDATAxINxRECEIVExMAILxBOX() == true);   /* non-250 not consumed */

    /* Once MBUS reaches WAIT, the deferred non-250 command is accepted. */
    mbus_state = MBUS_STATE_WAIT;
    CONTROLLER_Tasks();   /* CHECK_MBUS_STATE -> IDLE */
    assert(controllerData.state == CONTROLLER_STATE_IDLE);
    CONTROLLER_Tasks();   /* IDLE -> HANDLE_COMM_DATA */
    assert(controllerData.state == CONTROLLER_STATE_HANDLE_COMM_DATA);
    CONTROLLER_Tasks();   /* HANDLE_COMM_DATA applies EXEC_MBUS_STATE_SLAVES_ON */
    assert(mbus_state == MBUS_STATE_SLAVES_ON);
    puts("PASS: 250 answered while MBUS busy; stays in CHECK_MBUS_STATE; non-250 deferred until MBUS idle\n");
}

/* (c) Both disconnect paths must reset TransSocketReady and re-open transsocket. */
static void test_lost_client(void) {
    sreset();
    open_and_connect(0x0a000001u);
    DISCONNECTxCLIENT();
    assert(TransSocketReady == false);
    assert(ethernetData.state == ETHERNET_TCPIP_OPENING_SERVER);
    assert(trans_close_count == 1 && recv_close_count == 1);
    open_and_connect(0x0a000001u);
    assert(trans_open_count == 2);

    sreset();
    open_and_connect(0x0a000002u);
    recv_connected = false;
    ETHERNET_Tasks();   /* DATA_RX -> loss path -> OPENING_SERVER */
    assert(TransSocketReady == false);
    assert(ethernetData.state == ETHERNET_TCPIP_OPENING_SERVER);
    assert(trans_close_count == 1 && recv_close_count == 1);
    open_and_connect(0x0a000002u);
    assert(trans_open_count == 2);
    puts("PASS: both disconnect paths reset TransSocketReady and re-open transsocket\n");
}

/* (d) Repeated 250 -> CONNECTED, then another 250 -> CONNECTED again. */
static void test_repeated_connect(void) {
    udpTrans_t *got;
    sreset();
    controllerData.state = CONTROLLER_STATE_WAITING_FOR_CLIENT;
    put_250();
    CONTROLLER_Tasks();
    assert(controllerData.state == CONTROLLER_STATE_IDLE);
    assert(CheckDataInSendMailBox());
    got = GetDataFromSendMailBox();
    assert(send_is_connected(got));

    put_250();
    CONTROLLER_Tasks();   /* IDLE -> HANDLE_COMM_DATA */
    assert(controllerData.state == CONTROLLER_STATE_HANDLE_COMM_DATA);
    CONTROLLER_Tasks();   /* HANDLE_COMM_DATA -> CONNECTED -> CHECK_MBUS_STATE */
    assert(controllerData.state == CONTROLLER_STATE_CHECK_MBUS_STATE);
    CONTROLLER_Tasks();   /* CHECK_MBUS_STATE (MBUS idle) -> IDLE */
    assert(controllerData.state == CONTROLLER_STATE_IDLE);
    assert(CheckDataInSendMailBox());
    got = GetDataFromSendMailBox();
    assert(send_is_connected(got));
    puts("PASS: repeated 250 -> CONNECTED twice, controller returns to IDLE\n");
}

/* (e) A reconnect from a different client endpoint must re-bind the transsocket. */
static void test_changing_client_endpoint(void) {
    uint32_t ipA = 0x0a000001u, ipB = 0x0a000002u;
    sreset();
    open_and_connect(ipA);
    assert(last_client_open_ip.Val == ipA);
    DISCONNECTxCLIENT();
    assert(TransSocketReady == false);
    open_and_connect(ipB);
    assert(trans_open_count == 2);
    assert(last_client_open_ip.Val == ipB);
    assert(last_client_open_ip.Val != ipA);
    puts("PASS: reconnect re-binds transsocket to the changed client endpoint\n");
}

/* PO check-4 regression: host authority is renewed ONLY by correctly validated,
 * authorized initialization commands dispatched in CONTROLLER_STATE_HANDLE_COMM_DATA.
 * Every other HEADER-framed command (unknown, query, data exchange, flash,
 * handshake) must leave LastHostHeartbeat untouched, and a dead host must still
 * latch after HOST_AUTHORITY_WATCHDOG_MS. The 109-112 direct safety path must be
 * unchanged. */

static void test_init_command_renews_authority(void) {
    static const struct { int cmd; uint32_t mbus; } init_cmds[] = {
        { EXEC_MBUS_STATE_SLAVES_ON,         MBUS_STATE_SLAVES_ON },
        { EXEC_MBUS_STATE_SLAVE_DETECT,      MBUS_STATE_SLAVE_DETECT },
        { EXEC_MBUS_STATE_SLAVE_INIT,        MBUS_STATE_SLAVE_INIT },
        { EXEC_MBUS_STATE_SLAVE_ENABLE,      MBUS_STATE_SLAVE_ENABLE },
        { EXEC_MBUS_STATE_START_DATA_UPLOAD, MBUS_STATE_START_DATA_UPLOAD },
        { EXEC_MBUS_STATE_RESET,             MBUS_STATE_RESET },
    };
    unsigned k, n;
    udpTrans_t frame;
    n = (unsigned)(sizeof(init_cmds) / sizeof(init_cmds[0]));
    for (k = 0; k < n; ++k) {
        sreset();
        now = 1000u * MILISECONDS;
        memset(&frame, 0, sizeof(frame));
        frame.header = (uint8_t)HEADER;
        frame.command = (uint8_t)HOST_HEARTBEAT;
        assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
        assert(GlobalStopLatch == GLOBAL_STOP_ARMED);
        assert(LastHostHeartbeat == now);

        /* Mid-init: the host drives a state command while a heartbeat (110) is
         * being dropped by the ethernet RX FIFO path. */
        now += 1500u * MILISECONDS;
        memset(&frame, 0, sizeof(frame));
        frame.header = (uint8_t)HEADER;
        frame.command = (uint8_t)init_cmds[k].cmd;
        controllerData.state = CONTROLLER_STATE_IDLE;
        PutDataInReceiveMailBox(frame);
        CONTROLLER_Tasks();   /* IDLE -> HANDLE_COMM_DATA */
        assert(controllerData.state == CONTROLLER_STATE_HANDLE_COMM_DATA);
        CONTROLLER_Tasks();   /* HANDLE_COMM_DATA dispatches the init command */
        assert(mbus_state == init_cmds[k].mbus);
        assert(LastHostHeartbeat == now);   /* renewed at dispatch */

        /* 1500 ms after renewal (3000 ms after the arm): authority must hold. */
        now += 1500u * MILISECONDS;
        CHECKxHOSTxAUTHORITY();
        assert(!ISxGLOBALxSTOPxLATCHED());
    }
    puts("PASS: 6 authorized init commands renew LastHostHeartbeat at dispatch and keep authority alive\n");
}

static void test_unknown_command_does_not_renew(void) {
    udpTrans_t frame, *got;
    sreset();
    now = 1000u * MILISECONDS;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)HOST_HEARTBEAT;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
    assert(GlobalStopLatch == GLOBAL_STOP_ARMED);
    assert(LastHostHeartbeat == now);
    while (CheckDataInSendMailBox()) GetDataFromSendMailBox();   /* drop arm report */

    now += 1500u * MILISECONDS;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)0xEE;   /* unknown / malformed command */
    controllerData.state = CONTROLLER_STATE_IDLE;
    PutDataInReceiveMailBox(frame);
    CONTROLLER_Tasks();   /* IDLE -> HANDLE_COMM_DATA */
    assert(controllerData.state == CONTROLLER_STATE_HANDLE_COMM_DATA);
    CONTROLLER_Tasks();   /* HANDLE_COMM_DATA -> default: unknown, no renewal */
    assert(LastHostHeartbeat == 1000u * MILISECONDS);   /* not renewed */
    /* The unknown command is reported, proving it hit the default branch. */
    assert(CheckDataInSendMailBox());
    got = GetDataFromSendMailBox();
    assert(got->command == (uint8_t)CONTROLLER &&
           got->data[0] == (uint8_t)RECEIVED_UNKNOWN_COMMAND);

    /* Authority still expires from the original arm (3000 ms > 2000 ms). */
    now += 1500u * MILISECONDS;
    CHECKxHOSTxAUTHORITY();
    assert(ISxGLOBALxSTOPxLATCHED());
    puts("PASS: unknown HEADER command hits default, does not renew, authority still expires\n");
}

static void test_non_init_commands_do_not_renew(void) {
    static const uint8_t non_init_cmds[] = {
        (uint8_t)GET_CONTROLLER_VERSION,         /* 113 read-only query */
        (uint8_t)EXEC_MBUS_SLAVE_DATA_EXCH,      /* 108 data exchange */
        (uint8_t)EXEC_MBUS_STATE_SLAVE_FW_FLASH, /* 103 firmware flash */
        (uint8_t)CLIENT_CONNECTION_REQUEST,      /* 250 session handshake */
    };
    unsigned k, n;
    udpTrans_t frame;
    n = (unsigned)(sizeof(non_init_cmds) / sizeof(non_init_cmds[0]));
    for (k = 0; k < n; ++k) {
        sreset();
        now = 1000u * MILISECONDS;
        memset(&frame, 0, sizeof(frame));
        frame.header = (uint8_t)HEADER;
        frame.command = (uint8_t)HOST_HEARTBEAT;
        assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
        assert(LastHostHeartbeat == now);

        now += 1500u * MILISECONDS;
        memset(&frame, 0, sizeof(frame));
        frame.header = (uint8_t)HEADER;
        frame.command = non_init_cmds[k];
        controllerData.state = CONTROLLER_STATE_IDLE;
        PutDataInReceiveMailBox(frame);
        CONTROLLER_Tasks();   /* IDLE -> HANDLE_COMM_DATA */
        CONTROLLER_Tasks();   /* dispatch known non-init command */
        assert(LastHostHeartbeat == 1000u * MILISECONDS);   /* not renewed */
    }
    puts("PASS: known non-init commands (113/108/103/250) do not renew authority\n");
}

static void test_dead_host_latches(void) {
    udpTrans_t frame;
    sreset();
    now = 1000u * MILISECONDS;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)HOST_HEARTBEAT;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
    assert(GlobalStopLatch == GLOBAL_STOP_ARMED);
    assert(!ISxGLOBALxSTOPxLATCHED());

    /* Dead host: no further commands of any kind. */
    now += (HOST_AUTHORITY_WATCHDOG_MS * MILISECONDS) - MILISECONDS;   /* 1999 ms */
    CHECKxHOSTxAUTHORITY();
    assert(!ISxGLOBALxSTOPxLATCHED());   /* boundary: still inside grace */

    now += MILISECONDS;   /* exactly 2000 ms */
    controllerData.state = CONTROLLER_STATE_IDLE;
    CONTROLLER_Tasks();   /* CHECKxHOSTxAUTHORITY runs BEFORE the state machine */
    assert(ISxGLOBALxSTOPxLATCHED());
    assert(GlobalStopReason == 1);   /* host-loss */
    puts("PASS: dead host still latches host-loss at 2000 ms (check precedes state machine)\n");
}

static void test_safety_path_unchanged(void) {
    udpTrans_t frame;
    uint32_t armed_at;

    /* 110 HOST_HEARTBEAT renews authority and arms when disarmed. */
    sreset();
    now = 1000u * MILISECONDS;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)HOST_HEARTBEAT;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
    assert(GlobalStopLatch == GLOBAL_STOP_ARMED);
    assert(LastHostHeartbeat == now);

    /* 111 GLOBAL_STOP_STATE is a query: it must not renew authority. */
    now += 1000u * MILISECONDS;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)GLOBAL_STOP_STATE;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
    assert(LastHostHeartbeat == 1000u * MILISECONDS);   /* unchanged */

    /* 112 REARM_GLOBAL_STOP re-arms only when latched, granting a bounded grace. */
    now += HOST_AUTHORITY_WATCHDOG_MS * MILISECONDS;   /* past expiry */
    CHECKxHOSTxAUTHORITY();
    assert(ISxGLOBALxSTOPxLATCHED());
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)REARM_GLOBAL_STOP;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
    assert(GlobalStopLatch == GLOBAL_STOP_ARMED);
    assert(LastHostHeartbeat == now);   /* bounded grace refreshed */

    /* 112 when armed (not latched) must NOT renew. */
    sreset();
    now = 1000u * MILISECONDS;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)HOST_HEARTBEAT;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));   /* arm */
    armed_at = LastHostHeartbeat;
    now += 1000u * MILISECONDS;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)REARM_GLOBAL_STOP;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));   /* armed, not latched */
    assert(GlobalStopLatch == GLOBAL_STOP_ARMED);
    assert(LastHostHeartbeat == armed_at);   /* unchanged */

    /* 109 EXEC_GLOBAL_STOP latches host-initiated (reason 2). */
    sreset();
    now = 1000u * MILISECONDS;
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)HOST_HEARTBEAT;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));   /* arm */
    memset(&frame, 0, sizeof(frame));
    frame.header = (uint8_t)HEADER;
    frame.command = (uint8_t)EXEC_GLOBAL_STOP;
    frame.data[0] = 0;
    frame.data[1] = 0;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
    assert(ISxGLOBALxSTOPxLATCHED());
    assert(GlobalStopReason == 2);   /* host-initiated */

    puts("PASS: 109-112 safety path unchanged (110 renews; 111 doesn't; 112 re-arms only when latched; 109 latches host-initiated)\n");
}

int main(void) {
    test_mailbox_saturation_and_connect();
    test_connect_while_mbus_busy();
    test_lost_client();
    test_repeated_connect();
    test_changing_client_endpoint();
    test_init_command_renews_authority();
    test_unknown_command_does_not_renew();
    test_non_init_commands_do_not_renew();
    test_dead_host_latches();
    test_safety_path_unchanged();
    ExitProcess(0);
    return 0;
}
