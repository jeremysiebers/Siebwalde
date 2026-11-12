"""Reproduce the command-106 (MBUS_STATE_START_DATA_UPLOAD) DONE-suppression when
the global stop is latched. Extracts the production MBUS_Tasks unchanged from
mbus.c and ISxGLOBALxSTOPxLATCHED from controller.c; mocks only Harmony/peripheral
and Modbus internals (no hardware, no flash). Follows the same "extract production
C, mock peripherals/time, never the safety logic" discipline as test_global_stop.py.

Proves source-level software decisions, not UART timing or physical stop delivery:
  A. 106 with global stop clear            -> DONE emitted, state -> WAIT, upload ALL
  B. 106 with global stop already latched  -> DONE suppressed, state -> SERVICE_TASKS
  C. 106 dispatched then latch transition  -> (same MBUS_Tasks input as B) DONE suppressed
  D. latch clears after a pending 106      -> ORIGINAL request NOT resumed: the latch gate
       DESTROYS the pending START_DATA_UPLOAD state (overwrites it to SERVICE_TASKS), so
       clearing the latch later does NOT complete the original 106. Only a NEWLY-issued 106
       (fresh dispatch after re-arm) reaches MBUS_STATE_START_DATA_UPLOAD and emits DONE.
  E. normal completion                     -> DONE emitted once, state -> WAIT

Run with Python on Windows with Visual Studio C++ tools.
"""
from pathlib import Path
import os
import re
import subprocess
import tempfile

SRC = Path(__file__).resolve().parents[1]


def function(text, name):
    match = re.search(r"^(?:static )?(?:(?:void|bool|uint8_t|unsigned char|uint32_t) |"
                      r"udpTrans_t \*)" + re.escape(name) + r"\s*\([^)]*\)\s*\{",
                      text, re.MULTILINE)
    assert match, name
    depth = 1
    tokens = re.compile(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|'
                        r"'(?:\\.|[^'\\])*'|[{}]", re.DOTALL)
    for token in tokens.finditer(text, match.end()):
        depth += (token.group() == "{") - (token.group() == "}")
        if depth == 0:
            return text[match.start():token.end()] + "\n"
    raise AssertionError(name)


mbus = (SRC / "mbus.c").read_text()
controller = (SRC / "controller.c").read_text()

prefix = r'''
typedef unsigned char uint8_t;
typedef unsigned short uint16_t;
typedef unsigned int uint32_t;
typedef signed int int32_t;
typedef unsigned char bool;
typedef unsigned int DRV_HANDLE;
typedef unsigned int uintptr_t;
#define true 1
#define false 0
#define TRUE 1
#define FALSE 0
#define NULL ((void *)0)
#define UINT32_MAX 0xffffffffu
#define DRV_HANDLE_INVALID ((DRV_HANDLE)(-1))
#define DRV_IO_INTENT_EXCLUSIVE 0
#define DRV_TMR_INDEX_1 1u
#define DRV_TMR_INDEX_2 2u
#define DRV_TMR_INDEX_3 3u
#define _nop() ((void)0)
#define SECONDS (126000000u)
#define MILISECONDS (SECONDS / 1000)
#define HEADER 0xAA
#define FOOTER 0x55
#define SLAVEINFO 0xFF
#define NUMBER_OF_SLAVES 55
#define NUMBER_OF_SLAVES_SIZE (NUMBER_OF_SLAVES + 1)
#define MASTER 0
#define MBUS 20u
#define DONE 7u
#define NONE 100u
/* mbus.c timer callbacks, only registered (never run) by the INIT case. */
void ModbusCommCycleCallBack(uintptr_t context, uint32_t alarmCount) { (void)context; (void)alarmCount; }
void ModbusCharacterTimeoutCallBack(uintptr_t context, uint32_t alarmCount) { (void)context; (void)alarmCount; }
void ModbusReceiveTimeoutCallBack(uintptr_t context, uint32_t alarmCount) { (void)context; (void)alarmCount; }
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
void *memcpy(void *dst, const void *src, unsigned long long size) {
    unsigned char *d=dst;
    const unsigned char *s=src;
    while(size--) *d++ = *s++;
    return dst;
}

/* Mirrors of mbus.h type/enum surface (minimal, no Harmony tree). */
typedef enum {
    MBUS_STATE_INIT=0, MBUS_STATE_WAIT, MBUS_STATE_SLAVES_ON, MBUS_STATE_SLAVES_BOOT_WAIT,
    MBUS_STATE_SLAVE_DETECT, MBUS_STATE_SLAVE_FW_FLASH, MBUS_STATE_SLAVE_INIT,
    MBUS_STATE_SLAVE_ENABLE, MBUS_STATE_START_DATA_UPLOAD, MBUS_STATE_SERVICE_TASKS,
    MBUS_STATE_RESET, MBUS_STATE_RESET_WAIT
} MBUS_STATES;
typedef enum { UPLOAD_STATE_WAIT=0, UPLOAD_STATE_ALL, UPLOAD_STATE_SLAVES } MBUS_DATA_UPLOAD_STATES;
typedef struct {
    MBUS_STATES state;
    MBUS_DATA_UPLOAD_STATES upload;
    DRV_HANDLE ModbusCommCycleHandle;
    DRV_HANDLE ModbusCharacterTimeoutHandle;
    DRV_HANDLE ModbusReceiveTimeoutHandle;
} MBUS_DATA;

typedef struct { unsigned char Header; unsigned char Footer; } SLAVE_INFO;
typedef struct { unsigned char header; unsigned char command; unsigned char data[80]; } udpTrans_t;

/* mbus.c file-scope globals referenced by MBUS_Tasks (declared here because the
 * harness extracts only the function, not the file). */
uint32_t    DelayCount = 0;
volatile    uint16_t UpdateNextSlave = 0;
volatile    uint16_t UploadSlaveData = 0;
static      uint16_t NextSlaveCounter = 1;
static      uint16_t MaxSlaveUploadCount = 55;
static      SLAVE_INFO SlaveInfo[NUMBER_OF_SLAVES_SIZE];
static      SLAVE_INFO SlaveDump[1];
static      uint32_t SizeOfSlaveInfo = sizeof(SlaveInfo[0]);
static      bool GlobalStopServiceStarted = false;
MBUS_DATA   mbusData;
udpTrans_t  EthernetSendData;

/* slavefwhandler.c: fwData referenced by the SLAVE_FW_FLASH case. */
static struct { unsigned char SlaveBootloaderHandlingActive; } fwData;

/* Global-stop latch state (mirrors controller.c file-scope state; the production
 * ISxGLOBALxSTOPxLATCHED below reads exactly this variable). */
typedef enum { GLOBAL_STOP_DISARMED, GLOBAL_STOP_ARMED, GLOBAL_STOP_LATCHED } GLOBAL_STOP_LATCH;
static GLOBAL_STOP_LATCH GlobalStopLatch = GLOBAL_STOP_DISARMED;

/* ---- peripheral / Modbus / Harmony stubs (never the safety logic) ---- */
void MBUS_EVENT_REPORT(void) { }
void DRV_TMR_Start(unsigned h) { (void)h; }
void DRV_TMR_Stop(unsigned h) { (void)h; }
void DRV_USART0_Initialize(void) { }
unsigned DRV_TMR_Open(unsigned idx, int intent) { (void)idx; (void)intent; return 1u; }
uint32_t DRV_TMR_CounterFrequencyGet(unsigned h) { (void)h; return 126000000u; }
typedef void (*DRV_TMR_CALLBACK)(uintptr_t context, uint32_t alarmCount);
int DRV_TMR_AlarmRegister(unsigned h, uint32_t d, int repeat, uintptr_t ctx, DRV_TMR_CALLBACK cb) {
    (void)h; (void)d; (void)repeat; (void)ctx; (void)cb; return 1;
}
void Slaves_Disable_Off(void) { }
void Slaves_Disable_On(void) { }
int SLAVExDETECT(void) { return 0; }
void PROCESSxPETITxMODBUS(void) { }
int SLAVExFWxHANDLER(void) { return 0; }
int SLAVExINITxANDxCONFIG(void) { return 0; }
int ENABLExAMPLIFIER(void) { return 0; }
int PROCESSxNEXTxSLAVE(void) { return 1; }
void PROCESSxSLAVExCOMMUNICATION(void) { }
void INITxPETITxMODBUS(void *a, void *b, int n) { (void)a; (void)b; (void)n; }
void INITxCOMMxHANDLER(void *a, void *b) { (void)a; (void)b; }
void INITxSLAVExSTARTUP(void *a, void *b) { (void)a; (void)b; }
void INITxSLAVExHANDLER(void *a, void *b) { (void)a; (void)b; }
void INITxSLAVExFWxHANDLER(void *a, void *b) { (void)a; (void)b; }
uint32_t READxCORExTIMER(void) { return 0; }
void LOG_Push(const char *s) { (void)s; }
void PUTxDATAxINxSENDxMAILxBOX(udpTrans_t *f) { (void)f; }

/* Report capture: records whether a MBUS_STATE_START_DATA_UPLOAD / DONE ack was
 * ever emitted, so the cases can assert suppression vs emission. */
static int reports;
static int start_data_upload_done_seen;
void CREATExTASKxSTATUSxMESSAGE(uint32_t id, uint32_t command, uint32_t state, uint32_t message) {
    (void)message;
    ++reports;
    if (id == MBUS && command == (uint32_t)MBUS_STATE_START_DATA_UPLOAD && state == DONE)
        start_data_upload_done_seen = 1;
}
'''
prefix += function(controller, "ISxGLOBALxSTOPxLATCHED")
prefix += function(mbus, "MBUS_Tasks")

# Scenario cases: drive the extracted production MBUS_Tasks + latch state directly.
cases = r'''
static void reset(void) {
    memset(&mbusData, 0, sizeof(mbusData));
    mbusData.state = MBUS_STATE_WAIT;
    mbusData.upload = UPLOAD_STATE_WAIT;
    mbusData.ModbusCommCycleHandle = 1u;
    mbusData.ModbusCharacterTimeoutHandle = 2u;
    mbusData.ModbusReceiveTimeoutHandle = 3u;
    GlobalStopLatch = GLOBAL_STOP_DISARMED;
    GlobalStopServiceStarted = false;
    reports = 0;
    start_data_upload_done_seen = 0;
}

static void test_A_clear(void) {
    reset();
    GlobalStopLatch = GLOBAL_STOP_ARMED;             /* not latched */
    mbusData.state = MBUS_STATE_START_DATA_UPLOAD;   /* 106 dispatched, accepted */
    MBUS_Tasks();
    assert(mbusData.state == MBUS_STATE_WAIT);
    assert(mbusData.upload == UPLOAD_STATE_ALL);
    assert(start_data_upload_done_seen == 1);
    puts("PASS: A - 106 with global stop clear emits DONE and returns to WAIT\n");
}

static void test_B_latched(void) {
    reset();
    GlobalStopLatch = GLOBAL_STOP_LATCHED;           /* latched before dispatch */
    mbusData.state = MBUS_STATE_START_DATA_UPLOAD;   /* controller still sets the state */
    MBUS_Tasks();
    assert(mbusData.state == MBUS_STATE_SERVICE_TASKS);
    assert(mbusData.upload == UPLOAD_STATE_WAIT);
    assert(start_data_upload_done_seen == 0);        /* DONE suppressed */
    puts("PASS: B - 106 with global stop latched is silently preempted to SERVICE_TASKS, no DONE\n");
}

static void test_C_dispatch_then_latch(void) {
    reset();
    /* Dispatch 106 (state set) while clear, then the latch trips before MBUS_Tasks
     * runs next superloop iteration -- same MBUS_Tasks input as B. */
    GlobalStopLatch = GLOBAL_STOP_ARMED;
    mbusData.state = MBUS_STATE_START_DATA_UPLOAD;
    GlobalStopLatch = GLOBAL_STOP_LATCHED;
    MBUS_Tasks();
    assert(mbusData.state == MBUS_STATE_SERVICE_TASKS);
    assert(start_data_upload_done_seen == 0);
    puts("PASS: C - 106 dispatched then global-stop transition suppresses the DONE\n");
}

static void test_D_latch_clears(void) {
    reset();
    /* 106 is issued while the latch is clear, so the state is set. */
    GlobalStopLatch = GLOBAL_STOP_ARMED;
    mbusData.state = MBUS_STATE_START_DATA_UPLOAD;

    /* The latch trips AFTER the dispatch but BEFORE the next MBUS_Tasks pass:
     * the gate overwrites START_DATA_UPLOAD with SERVICE_TASKS, destroying the
     * pending request (no queue, no rejection, no DONE). */
    GlobalStopLatch = GLOBAL_STOP_LATCHED;
    MBUS_Tasks();
    assert(mbusData.state == MBUS_STATE_SERVICE_TASKS);
    assert(start_data_upload_done_seen == 0);

    /* The latch then clears (operator re-arm). The DESTROYED request does NOT
     * resume: SERVICE_TASKS has no transition back to START_DATA_UPLOAD, so the
     * original 106 stays lost. */
    GlobalStopLatch = GLOBAL_STOP_ARMED;
    MBUS_Tasks();
    assert(mbusData.state == MBUS_STATE_SERVICE_TASKS);
    assert(start_data_upload_done_seen == 0);

    /* Completion requires a NEWLY-issued 106 (fresh dispatch, a continuation of
     * nothing): only then is MBUS_STATE_START_DATA_UPLOAD reached and DONE sent. */
    mbusData.state = MBUS_STATE_START_DATA_UPLOAD;
    MBUS_Tasks();
    assert(mbusData.state == MBUS_STATE_WAIT);
    assert(start_data_upload_done_seen == 1);

    puts("PASS: D - a pending 106 overwritten by SERVICE_TASKS is lost; clearing the latch does not resume it, only a newly-issued 106 completes\n");
}

static void test_E_normal_completion(void) {
    reset();
    GlobalStopLatch = GLOBAL_STOP_ARMED;
    mbusData.state = MBUS_STATE_START_DATA_UPLOAD;
    MBUS_Tasks();
    assert(mbusData.state == MBUS_STATE_WAIT);
    assert(mbusData.upload == UPLOAD_STATE_ALL);
    assert(start_data_upload_done_seen == 1);
    assert(reports == 1);   /* exactly one status message (the DONE) */
    puts("PASS: E - normal command-106 completion emits exactly one DONE\n");
}

int main(void) {
    test_A_clear();
    test_B_latched();
    test_C_dispatch_then_latch();
    test_D_latch_clears();
    test_E_normal_completion();
    ExitProcess(0);
    return 0;
}
'''

code = prefix + cases

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
with tempfile.TemporaryDirectory(prefix="pic32-mbus106-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "test.c").write_text(code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
