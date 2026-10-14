"""Software-only tests of extracted production C functions with mocked timer/status.

Run: python TrackController5/firmware/src/tests/test_global_stop.py
Requires Visual Studio C++ tools. No PIC peripherals, firmware flash, or hardware.
The extraction keeps function bodies unchanged; it does not test Harmony scheduling.
Dispatch tests prove transport ACCEPTANCE, not slave receipt/ack or physical neutralization.
"""
from pathlib import Path
import os
import re
import subprocess
import tempfile

SRC = Path(__file__).resolve().parents[1]


def function(text, name):
    match = re.search(r"^(?:static )?(?:void|bool|unsigned char) " + re.escape(name) +
                      r"\([^)]*\)\s*\{", text, re.MULTILINE)
    assert match, name
    start = match.start()
    depth = 1
    # Ignore braces inside comments/string literals; preserve the body verbatim.
    tokens = re.compile(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|'
                        r"'(?:\\.|[^'\\])*'|[{}]", re.DOTALL)
    for token in tokens.finditer(text, match.end()):
        depth += (token.group() == "{") - (token.group() == "}")
        if depth == 0:
            return text[start:token.end()]
    raise AssertionError(f"Unclosed production function: {name}")


controller = (SRC / "controller.c").read_text()
ethernet = (SRC / "ethernet.c").read_text()
slave = (SRC / "slavehandler.c").read_text()
comm = (SRC / "slavecommhandler.c").read_text()
petit = (SRC / "modbus/PetitModbus.c").read_text()
petit_header = re.sub(r"^#include.*$", "", (SRC / "modbus/PetitModbus.h").read_text(),
                      flags=re.MULTILINE)
petit_globals = petit[petit.index("typedef enum"):
                      petit.index("/****************End of Slave Transmit")]
slave_symbols = slave[slave.index("enum SLAVE{"):slave.index("static REGISTER_PROCESSING")]
slave_symbols += slave[slave.index("static uint8_t      ProcessSlave"):
                       slave.index("bool PROCESSxNEXTxSLAVE()")]
logic = controller[controller.index("typedef enum { GLOBAL_STOP_DISARMED"):
                   controller.index("// *****************************************************************************",
                                    controller.index("typedef enum { GLOBAL_STOP_DISARMED"))]
prefix = r'''
/* Freestanding Win64 test runner: no dependency on optional MSVC CRT headers. */
typedef unsigned char uint8_t;
typedef unsigned short uint16_t;
typedef unsigned int uint32_t;
typedef signed int int32_t;
typedef unsigned char bool;
#define true 1
#define false 0
#define TRUE 1
#define FALSE 0
#define UINT32_MAX 0xffffffffu
__declspec(dllimport) void __stdcall ExitProcess(unsigned int code);
__declspec(dllimport) void * __stdcall GetStdHandle(unsigned int handle);
__declspec(dllimport) int __stdcall WriteFile(void *, const void *, unsigned int,
                                           unsigned int *, void *);
static void puts(const char *s) {
    unsigned int n=0, written;
    while(s[n]) ++n;
    WriteFile(GetStdHandle((unsigned int)-11),s,n,&written,0);
}
#define assert(expr) do { if (!(expr)) { puts("FAIL: " #expr "\n"); ExitProcess(1); } } while(0)
void *memset(void *p, int value, unsigned long long size) {
    unsigned char *bytes=p;
    while(size--) *bytes++=(unsigned char)value;
    return p;
}
#include "enums.h"
'''
prefix += petit_header + petit_globals + slave_symbols + r'''
#define MAILBOXRCD 100
static REGISTER_PROCESSING Rcd[MAILBOXRCD], *Rcd_Ptr, *Rcd_Ptr_prev;
static SLAVE_INFO *MASTER_SLAVE_DATA, *DUMP_SLAVE_DATA;
static unsigned char SlaveAmount;
#define PETITMODBUS_BROADCAST_ADDRESS 0
static REGISTER_PROCESSING Data;
static uint8_t ReadData[4], WriteData[9];
static unsigned wire_count;
static uint8_t wire_address[100];
static uint16_t wire_hr0[100];
static struct { unsigned ModbusReceiveTimeoutHandle; } mbusData;
static unsigned timer_starts, timer_stops, timer_clears;
void DRV_TMR_Start(unsigned handle) { (void)handle; ++timer_starts; }
void DRV_TMR_Stop(unsigned handle) { (void)handle; ++timer_stops; }
void DRV_TMR_CounterClear(unsigned handle) { (void)handle; ++timer_clears; }
void Petit_CRC16(unsigned char byte, unsigned int *crc) { *crc ^= byte; }
uint16_t CRC_ReverseValue(uint16_t crc) { return crc; }
unsigned char PetitModBus_UART_String(unsigned char *bytes, unsigned length) {
    /* Mock UART submission only: no slave receipt or acknowledgment. CRC mocked. */
    assert(wire_count<100 && length==11 && bytes[1]==16);
    assert(bytes[2]==0 && bytes[3]==0 && bytes[4]==0 && bytes[5]==1 && bytes[6]==2);
    wire_address[wire_count]=bytes[0];
    wire_hr0[wire_count]=(uint16_t)(((uint16_t)bytes[7]<<8)|bytes[8]);
    ++wire_count; return TRUE;
}
void Petit_RxRTU(void) { /* No simulated responses; timeout is tested independently. */ }
unsigned char Petit_RxDataAvailable(void) { return FALSE; }
void HandlePetitModbusReadHoldingRegistersSlaveReadback(void) { assert(false); }
void HandlePetitModbusWriteSingleRegisterSlaveReadback(void) { assert(false); }
void HandleMPetitodbusWriteMultipleRegistersSlaveReadback(void) { assert(false); }
void HandleMPetitodbusMbExceptionCodesSlaveReadback(void) { assert(false); }
static uint32_t now, reports, last_id, last_command, last_state, last_message;
uint32_t READxCORExTIMER(void) { return now; }
void LOG_Push(const char *s) { (void)s; }
void CREATExTASKxSTATUSxMESSAGE(uint32_t id, uint32_t command,
                              uint32_t state, uint32_t message) {
    ++reports; last_id=id; last_command=command; last_state=state; last_message=message;
}
void ENQUEUExGLOBALxSTOP(uint16_t hr0);
bool SendNextMessage(void);
'''
tests = r'''
static SLAVE_INFO slaves[NUMBER_OF_SLAVES_SIZE];
static SLAVE_INFO dump[1];
static void reset(void) {
    memset(slaves,0,sizeof(slaves)); memset(dump,0,sizeof(dump));
    MASTER_SLAVE_DATA=slaves; Rcd_Ptr=Rcd_Ptr_prev=Rcd;
    DUMP_SLAVE_DATA=dump; SlaveAmount=NUMBER_OF_SLAVES;
    Petit_Tx_State=PETIT_RXTX_IDLE; Petit_Tx_Current=0;
    memset(&Petit_Tx_Data,0,sizeof(Petit_Tx_Data));
    memset(&Petit_Rx_Data,0,sizeof(Petit_Rx_Data));
    SlaveAnswerTimeoutCounter=0; wire_count=0;
    timer_starts=timer_stops=timer_clears=0;
    ProcessSlave=0; ProcessSlaveLast=0; iProcessNextSlave=0;
    Message=MESSAGE1; SlaveInfoProcessor=1; loopcount=0;
    GlobalStopLatch=GLOBAL_STOP_DISARMED; LastHostHeartbeat=0;
    GlobalStopReason=0; now=0; reports=0;
}
static void command(uint8_t cmd, uint8_t a, uint8_t b) {
    udpTrans_t frame={0}; frame.header=HEADER; frame.command=cmd;
    frame.data[0]=a; frame.data[1]=b;
    assert(CONTROLLERxHANDLExSAFETYxCOMMAND(&frame));
}

static void test_transport_busy_then_accepted(void) {
    unsigned attempt;
    reset(); slaves[1].SlaveDetected=true; command(EXEC_GLOBAL_STOP,0,0);
    Petit_Tx_State=PETIT_RXTX_WAIT_ANSWER; iProcessNextSlave=1;
    for(attempt=0;attempt<10;++attempt) {
        assert(!SendNextMessage());
        assert(!PROCESSxNEXTxSLAVE());
        assert(Rcd_Ptr_prev==Rcd && iProcessNextSlave==1);
        assert(slaves[1].MbSentCounter==0 && wire_count==0);
    }
    Petit_Tx_State=PETIT_RXTX_IDLE;
    assert(PROCESSxNEXTxSLAVE());
    assert(Rcd_Ptr_prev==Rcd+1 && slaves[1].MbSentCounter==1);
    assert(Petit_Tx_State==PETIT_RXTX_START && Petit_Tx_Data.Address==1);
    /* Acceptance precedes UART submission and cannot prove slave receipt. */
    assert(wire_count==0);
    PROCESSxPETITxMODBUS(); assert(wire_count==1 && wire_address[0]==1);
    ProcessSlave=0; assert(!SendNextMessage());
    assert(slaves[1].MbSentCounter==1 && wire_count==1);
    puts("PASS: transport_busy_then_accepted (same FIFO entry, exactly once)\n");
}

static void test_transport_busy_rejection_is_non_destructive(void) {
    unsigned state, byte;
    uint8_t payload[7]={0,0,0,1,2,0x01,0x8f};
    reset();
    for(state=PETIT_RXTX_START;state<=PETIT_RXTX_TIMEOUT;++state) {
        Petit_Tx_State=(PETIT_RXTX_STATE)state;
        Petit_Tx_Data.Address=42; Petit_Tx_Data.Function=3; Petit_Tx_Data.DataLen=4;
        for(byte=0;byte<PETITMODBUS_RXTX_BUFFER_SIZE;++byte)
            Petit_Tx_Data.DataBuf[byte]=(uint8_t)(byte+10);
        Petit_Tx_Current=9; g_lastReqAddr=42; g_lastReqStart=6; g_lastReqQty=2;
        slaves[1].MbCommError=SLAVE_DATA_OK; dump[0].MbCommError=SLAVE_DATA_NOK;
        assert(!PetitModbusIsReady() && !PetitSendMessage());
        assert(!SENDxPETITxMODBUS(1,16,payload,7));
        assert(!SENDxPETITxMODBUS(0xaa,16,payload,7));
        assert(Petit_Tx_Data.Address==42 && Petit_Tx_Data.Function==3 && Petit_Tx_Data.DataLen==4);
        for(byte=0;byte<PETITMODBUS_RXTX_BUFFER_SIZE;++byte)
            assert(Petit_Tx_Data.DataBuf[byte]==(uint8_t)(byte+10));
        assert(Petit_Tx_Current==9 && Petit_Tx_State==(PETIT_RXTX_STATE)state);
        assert(g_lastReqAddr==42 && g_lastReqStart==6 && g_lastReqQty==2);
        assert(slaves[1].MbCommError==SLAVE_DATA_OK && dump[0].MbCommError==SLAVE_DATA_NOK);
        assert(slaves[1].MbSentCounter==0 && dump[0].MbSentCounter==0 && wire_count==0);
    }
    puts("PASS: transport_busy_rejection_is_non_destructive (all busy states)\n");
}

static void test_50_stops_fifo_timeout_progress_purge_and_idempotence(void) {
    unsigned slave, cycle, process_calls=0;
    uint32_t report_count;
    uint8_t ordinary[8]={1,WRITE,1,HOLDINGREG0,0x34,0x12,0,0};
    reset();
    for(slave=1;slave<=50;++slave) slaves[slave].SlaveDetected=true;
    ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(ordinary);
    ordinary[0]=2; ordinary[4]=0x78; ordinary[5]=0x56;
    ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(ordinary);
    assert(Rcd_Ptr==Rcd+2 && Rcd[0].RegData0==0x1234 && Rcd[1].RegData0==0x5678);
    command(EXEC_GLOBAL_STOP,0,0);
    assert(Rcd_Ptr==Rcd+50 && Rcd_Ptr_prev==Rcd);
    report_count=reports;
    for(slave=1;slave<=50;++slave) {
        assert(PetitModbusIsReady());
        assert(SendNextMessage()); /* REAL handler -> SEND -> PetitSendMessage TRUE. */
        assert(Rcd_Ptr_prev==Rcd+slave && slaves[slave].MbSentCounter==1);
        assert(Petit_Tx_State==PETIT_RXTX_START && Petit_Tx_Data.Address==slave);
        command(EXEC_GLOBAL_STOP,1,0); /* No re-enqueue, including after partial drain. */
        assert(reports==report_count && Rcd_Ptr_prev==Rcd+slave && Rcd_Ptr==Rcd+50);
        PROCESSxPETITxMODBUS(); ++process_calls;
        assert(Petit_Tx_State==PETIT_RXTX_WAIT_ANSWER && SlaveAnswerTimeoutCounter==0);
        assert(wire_count==slave && wire_address[slave-1]==slave && wire_hr0[slave-1]==399);
        if(slave<50) {
            for(cycle=0;cycle<3;++cycle) {
                assert(!SendNextMessage());
                PROCESSxPETITxMODBUS(); ++process_calls;
                assert(Petit_Tx_State==PETIT_RXTX_WAIT_ANSWER && Rcd_Ptr_prev==Rcd+slave);
                assert(wire_count==slave && slaves[slave+1].MbSentCounter==0);
            }
        }
        /* Inject the receive-timeout ISR flag, not an acknowledgment. The real
         * PROCESSxPETITxMODBUS timeout branch must release transport in one call.
         * Progress is bounded here CONDITIONALLY on timer delivery/task service;
         * this does not prove physical timer timing or success at the slave. */
        SlaveAnswerTimeoutCounter=1;
        PROCESSxPETITxMODBUS(); ++process_calls;
        assert(PetitModbusIsReady() && SlaveAnswerTimeoutCounter==0);
        assert(slaves[slave].MbCommError==SLAVE_DATA_TIMEOUT);
        assert(timer_starts==slave && timer_clears==slave && timer_stops==2*slave);
    }
    assert(process_calls<=50*5 && Rcd_Ptr_prev==Rcd_Ptr && wire_count==50);
    for(slave=1;slave<=50;++slave) assert(slaves[slave].MbSentCounter==1);
    command(EXEC_GLOBAL_STOP,0,0);
    assert(Rcd_Ptr_prev==Rcd_Ptr && reports==report_count);
    ProcessSlave=0; assert(!SendNextMessage());
    assert(wire_count==50);
    puts("PASS: 50_stop_transport_acceptances_FIFO/no_duplicates/timeout_progress/purge/idempotence\n");
}

static void test_slave_data_exch_frame_completeness(void) {
    uint8_t data[8]={1,WRITE,1,HOLDINGREG0,0x8f,0x01,0,0};
    assert(ISxVALIDxSLAVExDATAxEXCHxFRAME(data,8)); /* One register, complete. */
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(data,6)); /* Missing first register. */
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(data,7));
    data[2]=2;
    assert(ISxVALIDxSLAVExDATAxEXCHxFRAME(data,10)); /* Two registers, complete. */
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(data,8)); /* Missing second register. */
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(data,9)); /* Partial second register. */
    data[2]=0;
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(data,4)); /* Before count, zero-filled. */
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(data,8)); /* Zero registers. */
    data[2]=3;
    assert(!ISxVALIDxSLAVExDATAxEXCHxFRAME(data,12)); /* Unsupported count. */
    puts("PASS: slave_data_exch_frame_completeness (six required cases + partial-register boundaries)\n");
}

int main(void) {
    unsigned i; uint32_t before; udpTrans_t bad={0};
    uint8_t write[8]={1,WRITE,1,HOLDINGREG0,0x8f,0x01,0,0};
    test_slave_data_exch_frame_completeness();
    test_transport_busy_then_accepted();
    test_transport_busy_rejection_is_non_destructive();
    test_50_stops_fifo_timeout_progress_purge_and_idempotence();
    reset(); command(GLOBAL_STOP_STATE,0,0);
    assert(last_id==10 && last_command==111 && last_state==7 && last_message==0);
    assert(GlobalStopLatch==GLOBAL_STOP_DISARMED && LastHostHeartbeat==0 && reports==1);
    now=3000u*MILISECONDS; CHECKxHOSTxAUTHORITY();
    assert(GlobalStopLatch==GLOBAL_STOP_DISARMED && reports==1);
    command(HOST_HEARTBEAT,0,0);
    assert(GlobalStopLatch==GLOBAL_STOP_ARMED && reports==2 && last_message==3);
    for(i=1;i<=20;++i) {
        now+=HOST_HEARTBEAT_REFERENCE_MS*MILISECONDS;
        CHECKxHOSTxAUTHORITY(); assert(!ISxGLOBALxSTOPxLATCHED());
        command(HOST_HEARTBEAT,(uint8_t)i,0);
        assert(LastHostHeartbeat==now && reports==2);
    }
    command(GLOBAL_STOP_STATE,0,0);
    assert(last_command==111 && last_state==7 && last_message==3 && reports==3);
    assert(LastHostHeartbeat==now);
    now+=1999u*MILISECONDS; command(GLOBAL_STOP_STATE,0,0);
    assert(!ISxGLOBALxSTOPxLATCHED() && last_message==3);
    now+=MILISECONDS; CHECKxHOSTxAUTHORITY();
    assert(ISxGLOBALxSTOPxLATCHED() && last_message==1);
    reset(); now=3000u*MILISECONDS; CHECKxHOSTxAUTHORITY();
    assert(!ISxGLOBALxSTOPxLATCHED() && reports==0);
    assert(!CONTROLLERxHANDLExSAFETYxCOMMAND(&bad));
    command(HOST_HEARTBEAT,0xff,0xff);
    assert(GlobalStopLatch==GLOBAL_STOP_ARMED && reports==1 && last_message==3);
    assert(last_id==10 && last_command==111 && last_state==7);
    now+=1999u*MILISECONDS; CHECKxHOSTxAUTHORITY();
    assert(!ISxGLOBALxSTOPxLATCHED());
    command(HOST_HEARTBEAT,0,0); assert(reports==1);
    now+=2000u*MILISECONDS; CHECKxHOSTxAUTHORITY();
    assert(ISxGLOBALxSTOPxLATCHED() && reports==2 && last_message==1);
    command(HOST_HEARTBEAT,1,0); command(REARM_GLOBAL_STOP,0,0);
    command(EXEC_GLOBAL_STOP,0,0);
    assert(ISxGLOBALxSTOPxLATCHED() && reports==2);
    command(GLOBAL_STOP_STATE,0,0); assert(reports==3 && last_message==1);

    reset(); command(HOST_HEARTBEAT,0,0); now=2000u*MILISECONDS;
    command(HOST_HEARTBEAT,1,0); /* Late heartbeat cannot prevent expiry. */
    assert(ISxGLOBALxSTOPxLATCHED() && last_message==1);

    reset(); now=UINT32_MAX-100u*MILISECONDS;
    command(HOST_HEARTBEAT,0,0); now+=1999u*MILISECONDS;
    CHECKxHOSTxAUTHORITY(); assert(!ISxGLOBALxSTOPxLATCHED());
    now+=MILISECONDS; CHECKxHOSTxAUTHORITY(); assert(ISxGLOBALxSTOPxLATCHED());

    reset();
    for(i=0;i<NUMBER_OF_SLAVES_SIZE;++i) slaves[i].SlaveDetected=true;
    ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(write);
    assert(Rcd_Ptr==Rcd+1); reports=0;
    command(EXEC_GLOBAL_STOP,0,0);
    assert(Rcd_Ptr==Rcd+50 && Rcd_Ptr_prev==Rcd && reports==1 && last_message==2);
    for(i=0;i<50;++i) {
        assert(Rcd[i].SlaveAddress==i+1 && Rcd[i].Direction==WRITE);
        assert(Rcd[i].NoOfRegisters==1 && Rcd[i].StartRegister==0);
        assert(Rcd[i].RegData0==399);
    }
    before=reports; command(EXEC_GLOBAL_STOP,1,0);
    assert(reports==before && Rcd_Ptr==Rcd+50 && Rcd[0].RegData0==399);
    write[4]=0x90;
    ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(write);
    assert(Rcd_Ptr==Rcd+50 && reports==before);
    write[4]=0x8f;
    ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(write);
    assert(Rcd_Ptr==Rcd+51);

    reset(); slaves[1].SlaveDetected=true; slaves[50].SlaveDetected=true;
    slaves[51].SlaveDetected=true;
    command(EXEC_GLOBAL_STOP,1,0);
    assert(Rcd_Ptr==Rcd+2 && Rcd[0].SlaveAddress==1 && Rcd[1].SlaveAddress==50);
    assert(Rcd[0].RegData0==0x8000 && Rcd[1].RegData0==0x8000);

    reset(); command(EXEC_GLOBAL_STOP,2,0); assert(!ISxGLOBALxSTOPxLATCHED());
    command(EXEC_GLOBAL_STOP,0,1); assert(!ISxGLOBALxSTOPxLATCHED());
    command(GLOBAL_STOP_STATE,0,0);
    assert(last_id==10 && last_command==111 && last_state==7 && last_message==0);
    puts("PASS: production C disarmed-query/heartbeat-keepalive/latch/watchdog/wrap/fan-out/purge/rejection/EMO/idempotence tests\n");
    ExitProcess(0);
    return 0;
}
'''
vswhere = Path(os.environ["ProgramFiles(x86)"]) / "Microsoft Visual Studio/Installer/vswhere.exe"
compiler = subprocess.check_output([str(vswhere), "-latest", "-products", "*", "-find",
    r"VC\Tools\MSVC\**\bin\Hostx64\x64\cl.exe"], text=True).strip().splitlines()[-1]
vc = Path(compiler).parents[3]
kits = Path(os.environ["ProgramFiles(x86)"]) / "Windows Kits/10"
sdk = sorted((kits / "Lib").iterdir())[-1].name
env = os.environ.copy()
env["INCLUDE"] = ";".join(map(str, [vc / "include"] +
    [kits / "Include" / sdk / part for part in ("ucrt", "shared", "um")]))
env["LIB"] = ";".join(map(str, [vc / "lib/x64", kits / "Lib" / sdk / "ucrt/x64",
                                kits / "Lib" / sdk / "um/x64"]))
base = Path(os.environ["LOCALAPPDATA"]) / "Temp/opencode"
with tempfile.TemporaryDirectory(prefix="pic32-global-stop-", dir=base) as directory:
    tmp = Path(directory)
    # Production serialization intentionally truncates register words to bytes.
    # Suppress only MSVC's narrowing diagnostic around unchanged extracted code.
    (tmp / "test.c").write_text(prefix +
        "\n#pragma warning(push)\n#pragma warning(disable:4244)\n" +
        function(ethernet, "ISxVALIDxSLAVExDATAxEXCHxFRAME") +
        function(petit, "PetitSendMessage") +
        function(petit, "PetitModbusIsReady") +
        function(petit, "SENDxPETITxMODBUS") +
        function(petit, "Petit_DoSlaveTX") +
        function(petit, "Petit_TxRTU") +
        function(petit, "PROCESSxPETITxMODBUS") +
        function(comm, "SLAVExCOMMUNICATIONxHANDLER") +
        function(slave, "SendNextMessage") +
        function(slave, "PROCESSxNEXTxSLAVE") + logic +
        function(slave, "ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX") +
        function(slave, "ENQUEUExGLOBALxSTOP") + "\n#pragma warning(pop)\n" + tests)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-", f"/I{SRC}",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
