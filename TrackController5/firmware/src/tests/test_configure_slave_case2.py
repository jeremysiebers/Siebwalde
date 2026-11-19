"""Focused offline regression for ConfigureSlave (SLAVE_INIT) case 2/4/6 liveness.

Extracts the production `ConfigureSlave`, `ISxAMPLIFIERxCOMPATIBLE` and
`CHECKxMODBUSxCOMMxSTATUS` functions unchanged from slavestartup.c /
slavecommhandler.c and compiles them with MSVC. Only peripherals/time/logging are
mocked, and the Modbus comm result is driven through `MASTER_SLAVE_DATA[].MbCommError`
/ `DUMP_SLAVE_DATA[0].MbCommError` (the same inputs the production
CHECKxMODBUSxCOMMxSTATUS reads). The ConfigureSlave state machine itself is never
mocked (VERIFYxAMPLIFIERxIDENTITY, reached only via case 10, is stubbed because the
tests drive cases 2/4/6 directly and never enter the identity verify).

Proves the systematic liveness fix (bounded BUSY + terminal cases, no `default:`
hang):
  1. ConfigureSlave case 2 (slot-select verify): perpetually-SLAVEBUSY with the
     transmitter IDLE exhausts the bound and advances to deselect (no livelock);
     with the transmitter ARMED it does NOT prematurely advance.
  2. ConfigureSlave case 2 terminal cases (SLAVE_DATA_TIMEOUT/NOK/EXCEPTION/UNKNOWN/
     IDLE) each advance to deselect (no `default:` hang).
  3. ConfigureSlave case 4 (init-write verify): BUSY bound + terminal advance.
  4. ConfigureSlave case 6 (deselect verify): BUSY bound + terminal advance.
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


startup = (SRC / "slavestartup.c").read_text()
comm = (SRC / "slavecommhandler.c").read_text()

prefix = r'''
typedef unsigned char uint8_t;
typedef unsigned short uint16_t;
typedef unsigned int uint32_t;
typedef signed int int32_t;
typedef unsigned char bool;
#define true 1
#define false 0
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
prefix += re.sub(r"^#include.*$", "", (SRC / "modbus/PetitModbus.h").read_text(), flags=re.M)
prefix += r'''
/* File-scope state of slavestartup.c referenced by ConfigureSlave. */
static SLAVE_INFO *MASTER_SLAVE_DATA, *DUMP_SLAVE_DATA;
static REGISTER_PROCESSING Data;
static bool AmplifierCompatible[NUMBER_OF_AMPLIFIERS + 1];
static uint8_t GoToCase;
static uint8_t StartupMachine;
static uint32_t DelayCount1, DelayCount2, threshold;
#define CONFIG_SELECT_BUSY_MAX_RETRIES 20u
#define CONFIG_WRITE_BUSY_MAX_RETRIES 20u
#define CONFIG_DESELECT_BUSY_MAX_RETRIES 20u
static uint8_t ConfigSelectBusyRetries;
static uint8_t ConfigWriteBusyRetries;
static uint8_t ConfigDeselectBusyRetries;

/* Peripheral / time / logging mocks (never the safety logic). */
static uint32_t now;
static unsigned dispatch_count;
uint32_t READxCORExTIMER(void) { return now; }
void SLAVExCOMMUNICATIONxHANDLER(void) { ++dispatch_count; }
void RENEWxHOSTxAUTHORITY(void) { }
void LOG_Push(const char *str) { (void)str; }
void LOG_Printf(const char *fmt, ...) { (void)fmt; }
void CREATExTASKxSTATUSxMESSAGE(uint32_t a, uint32_t b, uint32_t c, uint32_t d) {
    (void)a; (void)b; (void)c; (void)d;
}

/* Transmitter state is peripheral state; the production PetitModbusIsReady() is
 * a one-line accessor over Petit_Tx_State. Both are mocked here only to drive
 * the readiness input of the ConfigureSlave state machine under test. */
typedef enum {
    PETIT_RXTX_IDLE,
    PETIT_RXTX_START,
    PETIT_RXTX_DATABUF,
    PETIT_RXTX_WAIT_ANSWER,
    PETIT_RXTX_TIMEOUT
} PETIT_RXTX_STATE;
static PETIT_RXTX_STATE Petit_Tx_State = PETIT_RXTX_IDLE;
unsigned char PetitModbusIsReady(void) {
    return (unsigned char)(Petit_Tx_State == PETIT_RXTX_IDLE);
}

/* VERIFYxAMPLIFIERxIDENTITY is only reached via case 10 (identity verify), which
 * these tests never enter; a stub satisfies the ConfigureSlave link dependency. */
bool VERIFYxAMPLIFIERxIDENTITY(uint8_t slave) { (void)slave; return false; }

/* Forward declaration: ConfigureSlave (extracted first) calls
 * CHECKxMODBUSxCOMMxSTATUS (extracted after it). */
uint8_t CHECKxMODBUSxCOMMxSTATUS(uint8_t SlaveId, bool OverWrite);
'''

cases = r'''
static SLAVE_INFO slaves[NUMBER_OF_SLAVES_SIZE], dump[1];

static void reset(void) {
    memset(slaves, 0, sizeof(slaves));
    memset(dump, 0, sizeof(dump));
    MASTER_SLAVE_DATA = slaves;
    DUMP_SLAVE_DATA = dump;
    memset(AmplifierCompatible, 0, sizeof(AmplifierCompatible));
    memset(&Data, 0, sizeof(Data));
    GoToCase = 0;
    StartupMachine = 0;
    DelayCount1 = DelayCount2 = threshold = 0;
    ConfigSelectBusyRetries = 0;
    ConfigWriteBusyRetries = 0;
    ConfigDeselectBusyRetries = 0;
    now = 0;
    dispatch_count = 0;
    Petit_Tx_State = PETIT_RXTX_IDLE;
}

/* Drive ConfigureSlave through case 0 -> 1 into case 2 (slot-select verify). */
static void enter_case2(void) {
    reset();
    slaves[51].SlaveDetected = true;
    slaves[1].SlaveDetected = true;
    assert(ConfigureSlave(51, SLOT1, 1, false) == false);  /* case 0 -> 1 */
    assert(StartupMachine == 1);
    assert(ConfigureSlave(51, SLOT1, 1, false) == false);  /* case 1 -> 2 */
    assert(StartupMachine == 2);
    assert(dispatch_count == 1);                            /* one slot-select dispatch */
}

static void test_case2_busy_bounded(void) {
    unsigned observations = 0;
    enter_case2();
    slaves[51].MbCommError = SLAVE_DATA_BUSY;
    /* transmitter IDLE -> PetitModbusIsReady() true -> the bound increments. */
    while (StartupMachine == 2) {
        assert(observations < 2u * CONFIG_SELECT_BUSY_MAX_RETRIES);
        (void)ConfigureSlave(51, SLOT1, 1, false);
        ++observations;
    }
    assert(observations == CONFIG_SELECT_BUSY_MAX_RETRIES);
    assert(StartupMachine == 5);              /* advanced to deselect */
    assert(ConfigSelectBusyRetries == 0);     /* reset on exhaustion */
    puts("PASS: case-2 perpetually-BUSY slot-select exhausts the bound and advances (no livelock)\n");
}

static void test_case2_busy_armed_does_not_advance(void) {
    unsigned observations = 0;
    enter_case2();
    Petit_Tx_State = PETIT_RXTX_WAIT_ANSWER;   /* transmitter armed, answer pending */
    slaves[51].MbCommError = SLAVE_DATA_BUSY;
    while (observations < 2u * CONFIG_SELECT_BUSY_MAX_RETRIES) {
        assert(ConfigureSlave(51, SLOT1, 1, false) == false);
        assert(StartupMachine == 2);           /* still in case 2 */
        assert(ConfigSelectBusyRetries == 0);  /* bound did not advance */
        ++observations;
    }
    puts("PASS: case-2 BUSY while the transmitter is armed is NOT prematurely advanced\n");
}

static void test_case2_terminal_cases(void) {
    const uint8_t outcomes[] = { SLAVE_DATA_TIMEOUT, SLAVE_DATA_NOK,
                                 SLAVE_DATA_EXCEPTION, SLAVE_DATA_UNKNOWN,
                                 SLAVE_DATA_IDLE };
    unsigned i;
    for (i = 0; i < sizeof(outcomes) / sizeof(outcomes[0]); ++i) {
        enter_case2();
        slaves[51].MbCommError = outcomes[i];
        assert(ConfigureSlave(51, SLOT1, 1, false) == false);
        assert(StartupMachine == 5);           /* advanced to deselect (no default: hang) */
        assert(ConfigSelectBusyRetries == 0);
    }
    puts("PASS: case-2 TIMEOUT/NOK/EXCEPTION/UNKNOWN/IDLE each advance to deselect\n");
}

static void test_case4_busy_bounded(void) {
    unsigned observations = 0;
    reset();
    StartupMachine = 4;
    dump[0].MbCommError = SLAVE_DATA_BUSY;
    while (StartupMachine == 4) {
        assert(observations < 2u * CONFIG_WRITE_BUSY_MAX_RETRIES);
        (void)ConfigureSlave(51, SLOT1, 1, false);
        ++observations;
    }
    assert(observations == CONFIG_WRITE_BUSY_MAX_RETRIES);
    assert(StartupMachine == 5);              /* advanced */
    assert(AmplifierCompatible[1] == false);  /* not silently accepted */
    assert(ConfigWriteBusyRetries == 0);
    puts("PASS: case-4 perpetually-BUSY init-write verify exhausts the bound and advances\n");
}

static void test_case4_terminal(void) {
    reset();
    StartupMachine = 4;
    /* Any non-BUSY non-OK dump result collapses to SLAVENOK via the DUMP path. */
    dump[0].MbCommError = SLAVE_DATA_TIMEOUT;
    assert(ConfigureSlave(51, SLOT1, 1, false) == false);
    assert(StartupMachine == 5);
    assert(AmplifierCompatible[1] == false);
    assert(ConfigWriteBusyRetries == 0);
    puts("PASS: case-4 terminal (SLAVENOK) advances and does not accept the amplifier\n");
}

static void test_case6_busy_bounded(void) {
    unsigned observations = 0;
    reset();
    StartupMachine = 6;
    slaves[51].MbCommError = SLAVE_DATA_BUSY;
    while (StartupMachine == 6) {
        assert(observations < 2u * CONFIG_DESELECT_BUSY_MAX_RETRIES);
        (void)ConfigureSlave(51, SLOT1, 1, false);
        ++observations;
    }
    assert(observations == CONFIG_DESELECT_BUSY_MAX_RETRIES);
    assert(StartupMachine == 0);              /* reset to done */
    assert(ConfigDeselectBusyRetries == 0);
    puts("PASS: case-6 perpetually-BUSY deselect verify exhausts the bound and advances\n");
}

static void test_case6_terminal_cases(void) {
    const uint8_t outcomes[] = { SLAVE_DATA_TIMEOUT, SLAVE_DATA_NOK,
                                 SLAVE_DATA_EXCEPTION, SLAVE_DATA_UNKNOWN,
                                 SLAVE_DATA_IDLE };
    unsigned i;
    for (i = 0; i < sizeof(outcomes) / sizeof(outcomes[0]); ++i) {
        reset();
        StartupMachine = 6;
        slaves[51].MbCommError = outcomes[i];
        assert(ConfigureSlave(51, SLOT1, 1, false) == true);  /* done */
        assert(StartupMachine == 0);
        assert(ConfigDeselectBusyRetries == 0);
    }
    puts("PASS: case-6 TIMEOUT/NOK/EXCEPTION/UNKNOWN/IDLE each advance (no default: hang)\n");
}

int main(void) {
    test_case2_busy_bounded();
    test_case2_busy_armed_does_not_advance();
    test_case2_terminal_cases();
    test_case4_busy_bounded();
    test_case4_terminal();
    test_case6_busy_bounded();
    test_case6_terminal_cases();
    ExitProcess(0);
    return 0;
}
'''

code = prefix + "\n#pragma warning(push)\n#pragma warning(disable:4459)\n"
code += function(startup, "ISxAMPLIFIERxCOMPATIBLE")
code += function(startup, "ConfigureSlave")
code += function(comm, "CHECKxMODBUSxCOMMxSTATUS")
code += "\n#pragma warning(pop)\n" + cases

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
with tempfile.TemporaryDirectory(prefix="pic32-config-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "test.c").write_text(code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-", f"/I{SRC}",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
