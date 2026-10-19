/* Software-only cases for unchanged extracted production functions. */
static SLAVE_INFO slaves[NUMBER_OF_SLAVES_SIZE], dump[1];
static void reset(void) {
    memset(slaves,0,sizeof(slaves)); memset(dump,0,sizeof(dump));
    memset(AmplifierCompatible,0,sizeof(AmplifierCompatible));
    memset(StopDelivery,0,sizeof(StopDelivery));
    memset(StopAttempts,0,sizeof(StopAttempts));
    MASTER_SLAVE_DATA=slaves; DUMP_SLAVE_DATA=dump; SlaveAmount=NUMBER_OF_SLAVES;
    Rcd_Ptr=Rcd_Ptr_prev=Rcd;
    Petit_Tx_State=PETIT_RXTX_IDLE; Petit_Tx_Current=0;
    SlaveAnswerTimeoutCounter=0; wire_count=0;
    ProcessSlave=0; ProcessSlaveLast=0; iProcessNextSlave=0;
    Message=MESSAGE1; SlaveInfoProcessor=1; loopcount=0;
    GlobalStopLatch=GLOBAL_STOP_DISARMED; LastHostHeartbeat=0; GlobalStopReason=0;
    StopSlave=0; LastLeaseRenewal=0; now=0; reports=0;
    StopReportReason=0; memset(&last_frame,0,sizeof(last_frame));
    GoToCase=StartupMachine=EnableMachine=EnableSlave=0;
}
static void command(uint8_t cmd, uint8_t a) {
    udpTrans_t frame={0}; frame.header=HEADER; frame.command=cmd; frame.data[0]=a;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
}
static void ack(void) {
    /* Inject transport result, not physical execution. */
    if (Petit_Tx_Data.Address>NUMBER_OF_SLAVES) dump[0].MbCommError=SLAVE_DATA_OK;
    else slaves[Petit_Tx_Data.Address].MbCommError=SLAVE_DATA_OK;
    Petit_Tx_State=PETIT_RXTX_IDLE;
}
static void reply(uint16_t value) {
    assert(Petit_Tx_Data.Function==3 && g_lastReqQty==1);
    Petit_Rx_Data.Address=g_lastReqAddr;
    Petit_Rx_Data.DataBuf[0]=2;
    Petit_Rx_Data.DataBuf[1]=(uint8_t)(value>>8);
    Petit_Rx_Data.DataBuf[2]=(uint8_t)value;
    HandlePetitModbusReadHoldingRegistersSlaveReadback();
}
static void timeout(void) {
    PROCESSxPETITxMODBUS();
    assert(Petit_Tx_State==PETIT_RXTX_WAIT_ANSWER);
    SlaveAnswerTimeoutCounter=1; PROCESSxPETITxMODBUS();
    assert(PetitModbusIsReady());
}
static void test_identity(void) {
    uint16_t identities[][2]={{0xf101,0xf101},{0x1101,0x1102},{0,0},
        {0x0101,0x0101},{0xf201,0xf201},{0xf102,0xf102}};
    unsigned i;
    for(i=0;i<6;++i) {
        reset(); dump[0].HoldingReg[7]=0xf101; /* Cache must not qualify. */
        assert(!VERIFYxAMPLIFIERxIDENTITY(1));
        assert(g_lastReqAddr==0xaa && g_lastReqStart==7 && g_lastReqQty==1);
        assert(!VERIFYxAMPLIFIERxIDENTITY(1) && !ISxAMPLIFIERxCOMPATIBLE(1));
        reply(identities[i][0]);
        assert(!VERIFYxAMPLIFIERxIDENTITY(1));
        assert(!VERIFYxAMPLIFIERxIDENTITY(1));
        assert(dump[0].MbSentCounter==2 && Petit_Tx_Data.Function==3);
        assert(!VERIFYxAMPLIFIERxIDENTITY(1));
        reply(identities[i][1]);
        assert(VERIFYxAMPLIFIERxIDENTITY(1));
        assert(ISxAMPLIFIERxCOMPATIBLE(1)==(i==0));
        assert(!ISxAMPLIFIERxCOMPATIBLE(2));
    }
    reset(); assert(!VERIFYxAMPLIFIERxIDENTITY(1)); timeout();
    assert(VERIFYxAMPLIFIERxIDENTITY(1) && !ISxAMPLIFIERxCOMPATIBLE(1));
    reset(); assert(!VERIFYxAMPLIFIERxIDENTITY(1)); reply(0xf101);
    assert(!VERIFYxAMPLIFIERxIDENTITY(1)); assert(!VERIFYxAMPLIFIERxIDENTITY(1));
    timeout(); assert(VERIFYxAMPLIFIERxIDENTITY(1) && !ISxAMPLIFIERxCOMPATIBLE(1));
    puts("PASS: HR7 two distinct fresh FC03 reads, legacy mimic, version/class/capability, timeouts\n");
}
static void test_poll_and_lease(void) {
    unsigned sip, seen=0, reg, start, qty;
    reset(); ProcessSlave=1;
    for(Message=MESSAGE1;Message<=MESSAGE3;++Message) {
        for(sip=1;sip<=(Message==MESSAGE3?(unsigned)SIPBOXSIZE:1u);++sip) {
            SlaveInfoProcessor=(uint8_t)sip; assert(SendNextMessage());
            assert(Petit_Tx_Data.Function==3);
            start=g_lastReqStart; qty=g_lastReqQty;
            assert(qty==(start==22?1u:2u) && start+qty<=23);
            for(reg=start;reg<start+qty;++reg) seen |= 1u<<reg;
            ack();
        }
    }
    assert(seen==0x7fffffu && sizeof(slaves[1].HoldingReg)==46);
    reset(); slaves[0].SlaveDetected=slaves[1].SlaveDetected=true; seen=0;
    for(sip=0;sip<30;++sip) {
        unsigned calls=0;
        while(!PROCESSxNEXTxSLAVE()) assert(++calls<10);
        assert(Data.SlaveAddress==1 && Data.Direction==READ);
        for(reg=g_lastReqStart;reg<(unsigned)g_lastReqStart+g_lastReqQty;++reg) seen |= 1u<<reg;
        ack();
    }
    assert(seen==0x7fffffu); ProcessSlave=1;
    now=99u*MILISECONDS; Message=MESSAGE1;
    assert(SendNextMessage() && Data.Direction==READ); ack();
    now=100u*MILISECONDS;
    assert(SendNextMessage() && Data.SlaveAddress==0 && Data.Direction==WRITE);
    assert(Data.StartRegister==1 && Data.RegData0==0x4000); ack();
    command(EXEC_GLOBAL_STOP,0); now=200u*MILISECONDS;
    assert(SendNextMessage() && Data.Direction==READ); ack(); /* Polling, never renewal. */
    command(REARM_GLOBAL_STOP,0); assert(GlobalStopLatch==GLOBAL_STOP_ARMED);
    assert(SendNextMessage() && Data.SlaveAddress==0 && Data.RegData0==0x4000); ack();
    now+=HOST_AUTHORITY_WATCHDOG_MS*MILISECONDS; CHECKxHOSTxAUTHORITY();
    assert(ISxGLOBALxSTOPxLATCHED()); /* Re-arm without heartbeat isn't infinite authority. */
    puts("PASS: full 23-register poll rotation; 100 ms direct lease, latch gating and re-arm\n");
}
static void test_initialization_path(void) {
    unsigned compatible;
    for(compatible=0;compatible<2;++compatible) {
        reset(); slaves[51].SlaveDetected=slaves[1].SlaveDetected=true;
        assert(!ConfigureSlave(51,1,1,0)); /* Select slot, then delay. */
        assert(!ConfigureSlave(51,1,1,0) && Data.SlaveAddress==51); ack();
        assert(!ConfigureSlave(51,1,1,0)); now+=16u*MILISECONDS;
        assert(!ConfigureSlave(51,1,1,0) && StartupMachine==10);
        assert(!ConfigureSlave(51,1,1,0) && Data.Direction==READ && Data.StartRegister==7);
        reply(0x1101);
        assert(!ConfigureSlave(51,1,1,0));
        assert(!ConfigureSlave(51,1,1,0) && Data.Direction==READ);
        reply((uint16_t)(compatible?0x1101:0x1102));
        assert(!ConfigureSlave(51,1,1,0));
        assert(StartupMachine==(compatible?3:5));
        if(compatible) {
            assert(!ConfigureSlave(51,1,1,0));
            assert(Data.SlaveAddress==0xaa && Data.Direction==WRITE && Data.StartRegister==2); ack();
            assert(!ConfigureSlave(51,1,1,0));
        }
        assert(!ConfigureSlave(51,1,1,0) && Data.SlaveAddress==51 && Data.RegData0==0); ack();
        assert(ConfigureSlave(51,1,1,0));
        assert(ISxAMPLIFIERxCOMPATIBLE(1)==compatible);
    }
    /* Enable is never broadcast and never targets an unverified amplifier. */
    reset(); AmplifierCompatible[1]=AmplifierCompatible[3]=true;
    assert(!ENABLExAMPLIFIER()); now+=1001u*MILISECONDS;
    assert(!ENABLExAMPLIFIER());
    assert(!ENABLExAMPLIFIER() && Data.SlaveAddress==1 && Data.RegData0==0x8000); ack();
    now+=1001u*MILISECONDS; assert(!ENABLExAMPLIFIER());
    assert(!ENABLExAMPLIFIER() && Data.SlaveAddress==3); ack();
    now+=1001u*MILISECONDS; assert(!ENABLExAMPLIFIER());
    assert(!ENABLExAMPLIFIER()); assert(ENABLExAMPLIFIER());
    assert(slaves[2].MbSentCounter==0 && slaves[0].MbSentCounter==0);
    puts("PASS: production ConfigureSlave reads identity before address write; incompatible slot skipped; enable unicast only\n");
}
static void test_address_write_busy_guard(void) {
    unsigned before;
    /* Reach case 3 (HR2 address write) through the production ConfigureSlave FSM. */
    reset(); slaves[51].SlaveDetected=slaves[1].SlaveDetected=true;
    assert(!ConfigureSlave(51,1,1,0));
    assert(!ConfigureSlave(51,1,1,0) && Data.SlaveAddress==51); ack();
    assert(!ConfigureSlave(51,1,1,0)); now+=16u*MILISECONDS;
    assert(!ConfigureSlave(51,1,1,0) && StartupMachine==10);
    assert(!ConfigureSlave(51,1,1,0) && Data.Direction==READ && Data.StartRegister==7);
    reply(0x1101);
    assert(!ConfigureSlave(51,1,1,0));
    assert(!ConfigureSlave(51,1,1,0) && Data.Direction==READ);
    reply(0x1101);
    assert(!ConfigureSlave(51,1,1,0));
    assert(StartupMachine==3);

    /* Transmitter busy: the HR2 write must be deferred, never silently dropped.
     * A dropped write would leave MbCommError IDLE and case 4 would misread it
     * as SLAVENOK, clearing AmplifierCompatible for a write never transmitted. */
    Petit_Tx_State=PETIT_RXTX_WAIT_ANSWER;
    before=reports;
    assert(!ConfigureSlave(51,1,1,0));
    assert(StartupMachine==3);           /* stayed pending, not advanced to case 4 */
    assert(ISxAMPLIFIERxCOMPATIBLE(1));  /* no premature incompatible */
    assert(reports==before);             /* no ERROR (SLAVENOK) notification */

    /* Transmitter free: the write is issued and the FSM advances to case 4. */
    Petit_Tx_State=PETIT_RXTX_IDLE;
    assert(!ConfigureSlave(51,1,1,0));
    assert(Data.SlaveAddress==0xaa && Data.Direction==WRITE && Data.StartRegister==2);
    assert(StartupMachine==4);

    /* A genuine write failure (NOK response) still surfaces as an error. */
    dump[0].MbCommError=SLAVE_DATA_NOK; Petit_Tx_State=PETIT_RXTX_IDLE;
    before=reports;
    assert(!ConfigureSlave(51,1,1,0));
    assert(!ISxAMPLIFIERxCOMPATIBLE(1));
    assert(reports==before+1);           /* ERROR notification emitted */
    assert(StartupMachine==5);           /* advanced to deselect */

    puts("PASS: HR2 address write guarded by PetitModbusIsReady; busy defers, genuine NOK still errors\n");
}
static void test_stop_confirmation(void) {
    reset(); slaves[1].SlaveDetected=true; AmplifierCompatible[1]=true;
    slaves[1].HoldingReg[HR_IDENTITY]=0xf101;
    slaves[1].HoldingReg[8]=HR_STATE_INHIBITED; slaves[1].HoldingReg[14]=399;
    command(EXEC_GLOBAL_STOP,0);
    assert(StopDelivery[1]==STOP_QUEUED && Rcd_Ptr==Rcd_Ptr_prev);
    Petit_Tx_State=PETIT_RXTX_WAIT_ANSWER;
    assert(!SendNextMessage() && StopAttempts[1]==0);
    Petit_Tx_State=PETIT_RXTX_IDLE;
    assert(SendNextMessage() && Data.RegData0==(399u|0x1000u));
    assert(StopDelivery[1]==STOP_WRITE_WAIT && StopAttempts[1]==1);
    command(EXEC_GLOBAL_STOP,1); assert(StopAttempts[1]==1 && StopWord==(399u|0x1000u));
    ack(); assert(!PROCESSxSLAVExCOMMUNICATION());
    assert(SendNextMessage() && g_lastReqStart==8); reply(HR_STATE_INHIBITED|HR_STATE_STOPPING_ACTIVE);
    assert(SendNextMessage() && g_lastReqStart==14); reply(399);
    assert(SendNextMessage() && StopDelivery[1]!=STOP_CONFIRMED && g_lastReqStart==8);
    reply(HR_STATE_INHIBITED);
    assert(SendNextMessage() && g_lastReqStart==14); reply(400);
    assert(SendNextMessage() && StopDelivery[1]!=STOP_CONFIRMED && g_lastReqStart==8);
    reply(HR_STATE_INHIBITED);
    assert(SendNextMessage() && g_lastReqStart==14); reply(399u|0x0400u);
    assert(!SendNextMessage() && StopDelivery[1]==STOP_CONFIRMED);
    assert(last_message==2 && last_frame.data[3]==1 && (last_frame.data[4]&3u)==GLOBAL_STOP_CONFIRMED);
    assert(slaves[1].MbSentCounter==7); /* ACK alone and cached data cannot confirm. */
    puts("PASS: ACK distinct from fresh HR8/HR14 stable completion; applied duty mask\n");
}
static void test_retries_and_failures(void) {
    unsigned slave, attempt;
    reset();
    for(slave=1;slave<=50;++slave) { slaves[slave].SlaveDetected=true; AmplifierCompatible[slave]=true; }
    command(EXEC_GLOBAL_STOP,0);
    for(slave=1;slave<=50;++slave) {
        for(attempt=1;attempt<=3;++attempt) {
            assert(SendNextMessage() && Data.SlaveAddress==slave && Data.Direction==WRITE);
            assert(StopAttempts[slave]==attempt); timeout();
        }
        if(slave<50) {
            /* Consume result without dispatching next slave in this call. */
            assert(StopDelivery[slave]==STOP_WRITE_WAIT);
        }
    }
    assert(!SendNextMessage());
    for(slave=1;slave<=50;++slave) assert(StopDelivery[slave]==STOP_FAILED && StopAttempts[slave]==3);
    assert(wire_count==150);
    reset(); slaves[1].SlaveDetected=true; command(EXEC_GLOBAL_STOP,0);
    assert(StopDelivery[1]==STOP_FAILED && !SendNextMessage()); /* Legacy: no probe writes. */
    reset(); slaves[1].SlaveDetected=true; AmplifierCompatible[1]=true;
    command(EXEC_GLOBAL_STOP,1); assert(SendNextMessage() && Data.RegData0==0x8000);
    ack(); assert(SendNextMessage());
    /* Malformed FC03 must terminate NOK, never leave the worker BUSY forever. */
    Petit_Rx_Data.Address=1; Petit_Rx_Data.DataBuf[0]=0;
    HandlePetitModbusReadHoldingRegistersSlaveReadback();
    assert(slaves[1].MbCommError==SLAVE_DATA_NOK);
    assert(SendNextMessage() && StopAttempts[1]==2 && Data.Direction==WRITE);
    ack(); now+=GLOBAL_STOP_CONFIRM_MS*MILISECONDS;
    assert(SendNextMessage() && StopAttempts[1]==3);
    timeout(); assert(!SendNextMessage() && StopDelivery[1]==STOP_FAILED);
    reset(); slaves[1].SlaveDetected=true; AmplifierCompatible[1]=true;
    slaves[1].HoldingReg[HR_IDENTITY]=0x2101; /* No applied-readback capability. */
    command(EXEC_GLOBAL_STOP,0); assert(SendNextMessage()); ack();
    assert(SendNextMessage()); reply(HR_STATE_INHIBITED);
    assert(SendNextMessage()); reply(399);
    assert(SendNextMessage() && StopDelivery[1]!=STOP_CONFIRMED);
    puts("PASS: 50-amplifier bounded retry/timeout fan-out; legacy blocked; NOK/confirmation deadline/EMO\n");
}
static void test_transport_busy(void) {
    unsigned state;
    uint8_t payload[7]={0,0,0,1,2,0x11,0x8f};
    reset(); AmplifierCompatible[1]=true;
    for(state=PETIT_RXTX_START;state<=PETIT_RXTX_TIMEOUT;++state) {
        Petit_Tx_State=(PETIT_RXTX_STATE)state;
        Petit_Tx_Data.Address=42; Petit_Tx_Data.Function=3; Petit_Tx_Data.DataBuf[0]=77;
        assert(!SENDxPETITxMODBUS(1,16,payload,7));
        assert(!SENDxPETITxMODBUS(0xaa,16,payload,7));
        assert(Petit_Tx_Data.Address==42 && Petit_Tx_Data.Function==3 && Petit_Tx_Data.DataBuf[0]==77);
        assert(slaves[1].MbSentCounter==0 && dump[0].MbSentCounter==0);
    }
    puts("PASS: production transport busy rejection remains non-destructive\n");
}
static void test_a2_encoding_and_bootloader(void) {
    unsigned i, before;
    reset();
    slaves[1].SlaveDetected=slaves[3].SlaveDetected=slaves[7].SlaveDetected=true;
    slaves[20].SlaveDetected=slaves[50].SlaveDetected=true;
    slaves[0].SlaveDetected=slaves[51].SlaveDetected=true;
    AmplifierCompatible[1]=AmplifierCompatible[3]=AmplifierCompatible[7]=true;
    AmplifierCompatible[20]=AmplifierCompatible[50]=true;
    command(EXEC_GLOBAL_STOP,0);
    assert(last_frame.header==HEADER && last_frame.command==CONTROLLER);
    assert(last_frame.data[0]==111 && last_frame.data[1]==DONE && last_frame.data[2]==2);
    assert(last_frame.data[3]==5 && last_frame.data[4]==0x55 && last_frame.data[5]==1);
    StopDelivery[1]=STOP_CONFIRMED; StopDelivery[3]=STOP_QUEUED;
    StopDelivery[7]=STOP_FAILED; StopDelivery[20]=STOP_CONFIRMED; StopDelivery[50]=STOP_FAILED;
    command(GLOBAL_STOP_STATE,0);
    assert(last_frame.data[3]==5 && last_frame.data[4]==0x24 && last_frame.data[5]==2);
    for(i=6;i<80;++i) assert(last_frame.data[i]==0);
    /* Captured detected list is stable; no holes indexed by raw slave address. */
    slaves[3].SlaveDetected=false; command(GLOBAL_STOP_STATE,0);
    assert(last_frame.data[3]==5 && last_frame.data[4]==0x24);
    reset();
    for(i=1;i<=50;++i) { slaves[i].SlaveDetected=true; AmplifierCompatible[i]=true; }
    command(EXEC_GLOBAL_STOP,0);
    assert(last_frame.data[3]==50);
    for(i=4;i<16;++i) assert(last_frame.data[i]==0x55);
    assert(last_frame.data[16]==0x05); /* Final byte has two entries, no padding garbage. */
    for(i=1;i<=50;++i) StopDelivery[i]=STOP_FAILED;
    command(GLOBAL_STOP_STATE,0);
    for(i=4;i<16;++i) assert(last_frame.data[i]==0xaa);
    assert(last_frame.data[16]==0x0a);
    for(i=1;i<=50;++i) StopDelivery[i]=STOP_CONFIRMED;
    command(GLOBAL_STOP_STATE,0);
    for(i=4;i<80;++i) assert(last_frame.data[i]==0);
    reset(); command(EXEC_GLOBAL_STOP,0);
    assert(last_frame.data[3]==0);
    for(i=4;i<80;++i) assert(last_frame.data[i]==0);
    before=reports; command(REARM_GLOBAL_STOP,0);
    REPORTxGLOBALxSTOPxDELIVERY(2); assert(reports==before+1); /* No stale latched report. */
    /* Existing FlashSequencer boot-address HR11=0 must bypass runtime gating. */
    reset(); Data.SlaveAddress=0xaa; Data.Direction=WRITE;
    Data.NoOfRegisters=1; Data.StartRegister=HOLDINGREG11; Data.RegData0=0;
    SLAVExCOMMUNICATIONxHANDLER();
    assert(Petit_Tx_State==PETIT_RXTX_START && Petit_Tx_Data.Address==0xaa);
    assert(Petit_Tx_Data.Function==16 && Petit_Tx_Data.DataBuf[1]==11);
    assert(Petit_Tx_Data.DataBuf[5]==0 && Petit_Tx_Data.DataBuf[6]==0);
    assert(dump[0].MbSentCounter==1 && !ISxAMPLIFIERxCOMPATIBLE(1));
    puts("PASS: A2 sparse/full/empty packed confirmation payload, query/re-arm, boot-address HR11 write\n");
}
static void test_heartbeat_and_mailbox(void) {
    unsigned i, before;
    uint8_t write[8]={1,WRITE,1,0,0x8f,0x01,0,0};
    reset(); command(GLOBAL_STOP_STATE,0); assert(last_message==0);
    now=3000u*MILISECONDS; CHECKxHOSTxAUTHORITY(); assert(!ISxGLOBALxSTOPxLATCHED());
    command(HOST_HEARTBEAT,0); assert(GlobalStopLatch==GLOBAL_STOP_ARMED);
    for(i=0;i<20;++i) { now+=500u*MILISECONDS; command(HOST_HEARTBEAT,0); assert(!ISxGLOBALxSTOPxLATCHED()); }
    now+=2000u*MILISECONDS; command(HOST_HEARTBEAT,0); assert(ISxGLOBALxSTOPxLATCHED());
    command(REARM_GLOBAL_STOP,0); assert(GlobalStopLatch==GLOBAL_STOP_ARMED);
    command(HOST_HEARTBEAT,0); now+=1999u*MILISECONDS; CHECKxHOSTxAUTHORITY(); assert(!ISxGLOBALxSTOPxLATCHED());
    now+=MILISECONDS; CHECKxHOSTxAUTHORITY(); assert(ISxGLOBALxSTOPxLATCHED());
    reset(); now=UINT32_MAX-100u*MILISECONDS; command(HOST_HEARTBEAT,0);
    now+=2000u*MILISECONDS; CHECKxHOSTxAUTHORITY(); assert(ISxGLOBALxSTOPxLATCHED());
    reset(); ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(write); assert(Rcd_Ptr==Rcd);
    AmplifierCompatible[1]=true; ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(write); assert(Rcd_Ptr==Rcd+1);
    command(EXEC_GLOBAL_STOP,0); assert(Rcd_Ptr==Rcd_Ptr_prev);
    ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(write); assert(Rcd_Ptr==Rcd_Ptr_prev);
    reset(); AmplifierCompatible[1]=true;
    for(i=0;i<99;++i) ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(write);
    before=(unsigned)(Rcd_Ptr-Rcd); now=100u*MILISECONDS;
    assert(SendNextMessage() && Data.SlaveAddress==0 && (unsigned)(Rcd_Ptr-Rcd)==before);
    assert(ISxVALIDxSLAVExDATAxEXCHxFRAME(write,8));
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(write,7));
    write[2]=2; assert(ISxVALIDxSLAVExDATAxEXCHxFRAME(write,10));
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(write,9));
    puts("PASS: heartbeat/late expiry/wrap/re-arm; identity write gate; purge; full-mailbox lease priority; frame completeness\n");
}
int main(void) {
    test_identity(); test_initialization_path(); test_address_write_busy_guard();
    test_poll_and_lease(); test_stop_confirmation();
    test_retries_and_failures(); test_heartbeat_and_mailbox();
    test_transport_busy();
    test_a2_encoding_and_bootloader();
    ExitProcess(0); return 0;
}
