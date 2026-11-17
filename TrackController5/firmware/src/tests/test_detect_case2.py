"""Focused offline regression for DetectSlave case-2 (backplane detect-read).

Extracts the production `DetectSlave`, `SLAVExDETECT` and `CHECKxMODBUSxCOMMxSTATUS`
functions unchanged from slavestartup.c / slavecommhandler.c and compiles them with
MSVC. Only peripherals/time/logging are mocked, and the Modbus comm result is driven
through `MASTER_SLAVE_DATA[SlaveId].MbCommError` (the same input the production
CHECKxMODBUSxCOMMxSTATUS reads). The detect/advance state machine itself is never
mocked.

Follows the same "extract production C, mock peripherals/time, never mock the safety
logic" discipline as test_global_stop.py. No hardware, no flash, no PIC32 build.

Proves source-level decisions, not UART timing or physical backplane presence:
  1. SLAVE_DATA_NOK / EXCEPTION / UNKNOWN / IDLE advance case 2 to not-detected.
  2. SLAVE_DATA_TIMEOUT still advances (regression).
  3. SLAVEOK still marks detected and advances (regression).
  4. A perpetually-SLAVEBUSY backplane terminates via the bounded retry counter.
  5. A failed backplane is never marked detected, and its downstream amplifier
     is never detected.
  6. SLAVExDETECT completes a full scan (backplanes all failing) without hanging.
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
/* File-scope state of slavestartup.c referenced by the extracted functions. */
static SLAVE_INFO *MASTER_SLAVE_DATA, *DUMP_SLAVE_DATA;
static REGISTER_PROCESSING Data;
static uint8_t GoToCase;
static uint8_t RunSlaveDetect;
static uint8_t BackplaneId, PrevBackplaneId, ShiftSlot2;
uint16_t ShiftSlotNr;
static uint8_t BackplaneSelectRetries;
static uint8_t BackplaneBusyRetries;
uint8_t SlaveId;
uint8_t iSlaveDetect;
static uint32_t DelayCount1, DelayCount2, threshold;
#define BACKPLANE_SELECT_MAX_RETRIES 3u
#define BACKPLANE_BUSY_MAX_RETRIES 20u

/* Peripheral / time / logging mocks (never the safety logic). */
static uint32_t now;
uint32_t READxCORExTIMER(void) { return now; }
void SLAVExCOMMUNICATIONxHANDLER(void) { }
void RENEWxHOSTxAUTHORITY(void) { }
void LOG_Push(const char *str) { (void)str; }
void LOG_Printf(const char *fmt, ...) { (void)fmt; }

/* Forward declaration: DetectSlave (extracted first) calls CHECKxMODBUSxCOMMxSTATUS
 * (extracted after it). */
uint8_t CHECKxMODBUSxCOMMxSTATUS(uint8_t SlaveId, bool OverWrite);
'''

cases = r'''
static SLAVE_INFO slaves[NUMBER_OF_SLAVES_SIZE], dump[1];

static void reset(void) {
    memset(slaves, 0, sizeof(slaves));
    memset(dump, 0, sizeof(dump));
    MASTER_SLAVE_DATA = slaves;
    DUMP_SLAVE_DATA = dump;
    memset(&Data, 0, sizeof(Data));
    GoToCase = 0;
    RunSlaveDetect = 0;
    BackplaneId = PrevBackplaneId = ShiftSlot2 = 0;
    ShiftSlotNr = 0;
    BackplaneSelectRetries = 0;
    BackplaneBusyRetries = 0;
    SlaveId = 51;
    iSlaveDetect = 0;
    DelayCount1 = DelayCount2 = threshold = 0;
    now = 0;
}

/* Drive one backplane through case 0 -> WAIT -> 1 -> 2 and return the terminal
 * outcome of the case-2 verification (true = advanced). */
static bool detect_backplane(uint8_t id) {
    assert(!DetectSlave(id));                 /* case 0 -> WAIT */
    assert(RunSlaveDetect == WAIT);
    now += 16u * MILISECONDS;                 /* exceed WAIT_TIME1 threshold */
    assert(!DetectSlave(id));                 /* WAIT -> case 1 */
    assert(!DetectSlave(id));                 /* case 1 -> case 2 */
    return DetectSlave(id);                   /* case-2 terminal evaluation */
}

static void test_terminal_not_detected(void) {
    const uint8_t outcomes[] = { SLAVE_DATA_NOK, SLAVE_DATA_EXCEPTION,
                                 SLAVE_DATA_UNKNOWN, SLAVE_DATA_IDLE,
                                 SLAVE_DATA_TIMEOUT };
    unsigned i;
    for (i = 0; i < sizeof(outcomes) / sizeof(outcomes[0]); ++i) {
        reset();
        slaves[51].MbCommError = outcomes[i];
        assert(detect_backplane(51) == true);
        assert(RunSlaveDetect == 0);
        assert(slaves[51].SlaveDetected == false);
        /* OverWrite=true re-poll guard: the failed comm result is consumed and
         * reset to IDLE, so a re-poll can never re-enter case 2 against a stale
         * non-BUSY value. */
        assert(slaves[51].MbCommError == SLAVE_DATA_IDLE);
    }
    puts("PASS: NOK/EXCEPTION/UNKNOWN/IDLE/TIMEOUT each advance case 2 to not-detected\n");
}

static void test_detected_ok(void) {
    reset();
    slaves[51].MbCommError = SLAVE_DATA_OK;
    assert(detect_backplane(51) == true);
    assert(RunSlaveDetect == 0);
    assert(slaves[51].SlaveDetected == true);
    puts("PASS: SLAVEOK still marks the backplane detected and advances\n");
}

static void test_busy_bounded(void) {
    unsigned busy_observations = 0;
    reset();
    slaves[51].MbCommError = SLAVE_DATA_BUSY;
    assert(!DetectSlave(51));                 /* case 0 -> WAIT */
    now += 16u * MILISECONDS;
    assert(!DetectSlave(51));                 /* WAIT -> case 1 */
    assert(!DetectSlave(51));                 /* case 1 -> case 2 */
    while (1) {
        assert(busy_observations < 2u * BACKPLANE_BUSY_MAX_RETRIES);
        if (DetectSlave(51)) break;
        ++busy_observations;
    }
    /* Exactly BACKPLANE_BUSY_MAX_RETRIES-1 BUSY observations, then exhaustion. */
    assert(busy_observations == BACKPLANE_BUSY_MAX_RETRIES - 1u);
    assert(RunSlaveDetect == 0);
    assert(slaves[51].SlaveDetected == false);
    assert(BackplaneBusyRetries == 0);        /* reset on the terminal branch */
    puts("PASS: perpetually-BUSY backplane terminates after bounded retries as not-detected\n");
}

static void test_failed_backplane_blocks_downstream(void) {
    reset();
    slaves[51].MbCommError = SLAVE_DATA_NOK;
    assert(detect_backplane(51) == true);
    assert(slaves[51].SlaveDetected == false);
    /* Downstream amplifier 1 maps to backplane 51; with that backplane not
     * detected, case 3 skips the amplifier without any probe write. */
    assert(!DetectSlave(1));                  /* case 0 -> RunSlaveDetect = 3 */
    assert(RunSlaveDetect == 3);
    assert(DetectSlave(1) == true);           /* case 3 -> skip, advance */
    assert(slaves[1].SlaveDetected == false);
    assert(RunSlaveDetect == 0);
    puts("PASS: a failed backplane is not detected and its downstream amplifier is never detected\n");
}

static bool pump_slavedetect(unsigned max_iterations) {
    unsigned i;
    for (i = 0; i < max_iterations; ++i) {
        if (SLAVExDETECT()) return true;
        now += 16u * MILISECONDS;
    }
    return false;
}

static void test_slavedetect_no_hang(void) {
    unsigned i;
    reset();
    for (i = 51; i <= 55; ++i) slaves[i].MbCommError = SLAVE_DATA_NOK;
    assert(pump_slavedetect(10000u) == true);
    assert(iSlaveDetect == 0);                /* wrapped back to backplane phase */
    assert(SlaveId == 51);                    /* reset for the next cycle */
    for (i = 51; i <= 55; ++i) assert(slaves[i].SlaveDetected == false);
    for (i = 1; i <= 50; ++i) assert(slaves[i].SlaveDetected == false);
    puts("PASS: SLAVExDETECT completes a full scan with all backplanes failing; nothing detected; no hang\n");
}

int main(void) {
    test_terminal_not_detected();
    test_detected_ok();
    test_busy_bounded();
    test_failed_backplane_blocks_downstream();
    test_slavedetect_no_hang();
    ExitProcess(0);
    return 0;
}
'''

code = prefix + "\n#pragma warning(push)\n#pragma warning(disable:4459)\n"
code += function(startup, "DetectSlave") + function(startup, "SLAVExDETECT")
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
with tempfile.TemporaryDirectory(prefix="pic32-detect-case2-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "test.c").write_text(code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-", f"/I{SRC}",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
