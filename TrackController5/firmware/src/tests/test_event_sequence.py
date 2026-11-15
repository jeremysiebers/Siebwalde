"""PV07-C end-to-end event-ring sequence demonstration (offline, no hardware).

Compiles the REAL production event ring (MBUS_EVENT_PUSH / MBUS_EVENT_REPORT
extracted verbatim from slavecommhandler.c) together with the REAL production
push sites (slavehandler.c / slavecommhandler.c / PetitModbus.c) and the REAL
PetitModbus receive path (CheckPetitModbusBufferComplete / Petit_RxRTU /
PROCESSxPETITxMODBUS / the exception readback handler).

A recording LOG_Printf sink captures the exact formatted lines that
MBUS_EVENT_REPORT would emit to the PIC32 TCP diagnostic log (port 50000), plus
a structured copy for deterministic assertions.

Software-only: no UART, no timer, no hardware, no live TCP. This proves software
decisions (event classification and bounded-ring behaviour), not UART timing,
PIC18 execution or physical stop.
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


controller = (SRC / "controller.c").read_text()
slave = (SRC / "slavehandler.c").read_text()
startup = (SRC / "slavestartup.c").read_text()
comm = (SRC / "slavecommhandler.c").read_text()
petit = (SRC / "modbus/PetitModbus.c").read_text()
ethernet = (SRC / "ethernet.c").read_text()

prefix = r'''
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
/* Mirrors of version.h and mbus_event.h (extracted functions reference these;
 * the harness compiles production C without the Harmony include tree). */
#define CTRL_FW_MAJOR 1u
#define CTRL_FW_MINOR 0u
#define CTRL_FW_PATCH 2u
#define CTRL_PROTOCOL_VERSION 0x01u
#define CTRL_RELEASE_TIMESTAMP "2026-11-04"
enum { MBUS_EV_ENQUEUE = 1, MBUS_EV_GATE_DROP = 2, MBUS_EV_FC16_SENT = 3,
       MBUS_EV_PURGE = 4, MBUS_EV_TX_DONE = 5, MBUS_EV_ACK = 6, MBUS_EV_TIMEOUT = 7,
       MBUS_EV_EXCEPTION = 8 };
/* Forward declarations: the real definitions are extracted from
 * slavecommhandler.c and compiled below. */
void MBUS_EVENT_PUSH(uint8_t type, uint8_t slave, uint8_t reg, uint16_t val, uint8_t rv);
void MBUS_EVENT_REPORT(void);

/* ---- recording LOG sink: captures what MBUS_EVENT_REPORT emits to TCP 50000 ---- */
#pragma warning(push)
#pragma warning(disable: 4018 4100 4101 4189 4244 4245 4267 4459)
#define CAP_MAX 8192
static char cap_text[CAP_MAX];
static unsigned cap_text_len;
static unsigned cap_lines;
typedef struct { unsigned type, slave, reg, val, rv, seq; } CAP_EVENT;
static CAP_EVENT cap_events[128];
static unsigned cap_count;

static void cap_reset(void) { cap_text[0] = '\0'; cap_text_len = 0; cap_lines = 0; cap_count = 0; }
static void cap_char(char c) {
    if (cap_text_len + 1u < CAP_MAX) { cap_text[cap_text_len++] = c; cap_text[cap_text_len] = '\0'; }
}
static void cap_str(const char *s) { while (*s) cap_char(*s++); }
static void cap_dec(unsigned v) {
    char tmp[12]; unsigned n = 0, i;
    if (v == 0) { cap_char('0'); return; }
    while (v) { tmp[n++] = (char)('0' + (v % 10u)); v /= 10u; }
    for (i = n; i > 0; --i) cap_char(tmp[i - 1u]);
}
void LOG_Push(const char *s) {
    if (s == 0) return;
    cap_str(s);
    cap_char('\r'); cap_char('\n');
    ++cap_lines;
}
/* The harness compiles with /nodefaultlib (no CRT), so <stdarg.h> is absent.
 * MBUS_EVENT_REPORT (slavecommhandler.c) is the ONLY active LOG_Printf caller in
 * the extracted production code and always emits the fixed 6-argument format:
 *   "Mbus event: type=%u slave=%u reg=%u val=%u rv=%u seq=%u"
 * The sink is therefore declared with that exact arity (a..f = type..seq). */
void LOG_Printf(const char *fmt,
                unsigned a, unsigned b, unsigned c, unsigned d, unsigned e, unsigned f) {
    if (fmt == 0) return;
    if (fmt[0] == 'M' && fmt[1] == 'b' && fmt[2] == 'u' && fmt[3] == 's') {
        if (cap_count < 128u) {
            cap_events[cap_count].type = a; cap_events[cap_count].slave = b;
            cap_events[cap_count].reg = c;   cap_events[cap_count].val = d;
            cap_events[cap_count].rv = e;    cap_events[cap_count].seq = f;
            ++cap_count;
        }
        cap_str("Mbus event: type="); cap_dec(a);
        cap_str(" slave="); cap_dec(b);
        cap_str(" reg=");   cap_dec(c);
        cap_str(" val=");   cap_dec(d);
        cap_str(" rv=");    cap_dec(e);
        cap_str(" seq=");   cap_dec(f);
    } else {
        cap_str(fmt); /* any non-event format is recorded verbatim */
    }
    cap_char('\r'); cap_char('\n');
    ++cap_lines;
}
#pragma warning(pop)
'''
prefix += re.sub(r"^#include.*$", "", (SRC / "modbus/PetitModbus.h").read_text(), flags=re.M)
prefix += petit[petit.index("typedef enum"):petit.index("/****************End of Slave Transmit")]
prefix += slave[slave.index("enum SLAVE{"):slave.index("static REGISTER_PROCESSING")]
prefix += slave[slave.index("typedef enum { STOP_UNUSED"):slave.index("void INITxSLAVExHANDLER")]
prefix += slave[slave.index("static uint8_t      ProcessSlave"):slave.index("bool PROCESSxNEXTxSLAVE()")]
prefix += r'''
#define MAILBOXRCD 100
#define PETITMODBUS_BROADCAST_ADDRESS 0
static REGISTER_PROCESSING Rcd[MAILBOXRCD], *Rcd_Ptr, *Rcd_Ptr_prev;
static SLAVE_INFO *MASTER_SLAVE_DATA, *DUMP_SLAVE_DATA;
static bool AmplifierCompatible[NUMBER_OF_AMPLIFIERS + 1];
static unsigned char SlaveAmount;
static REGISTER_PROCESSING Data;
static uint8_t ReadData[4], WriteData[9];
static struct { unsigned ModbusCharacterTimeoutHandle; unsigned ModbusReceiveTimeoutHandle; } mbusData;
static uint32_t now, reports, last_command, last_message;
static udpTrans_t last_frame;
static unsigned wire_count;
static uint8_t GoToCase, StartupMachine, EnableMachine, EnableSlave;
static uint32_t DelayCount1, DelayCount2, threshold;
/* Receive buffer + counter (production PetitModbusPort.h owns these; the harness
 * compiles production C without the port layer, so they are provided here). */
volatile unsigned char PetitReceiveBuffer[PETITMODBUS_RECEIVE_BUFFER_SIZE];
volatile unsigned char PetitReceiveCounter;

/* CheckPetitModbusBufferComplete return codes (mirror PetitModbus.c:6-9). */
#define PETIT_FALSE_FUNCTION      0
#define PETIT_FALSE_SLAVE_ADDRESS 1
#define PETIT_DATA_NOT_READY      2
#define PETIT_DATA_READY          3

/* Internal PetitModbus receive-path functions (declared before the extracted
 * PROCESSxPETITxMODBUS dispatches to them). */
unsigned char CheckPetitModbusBufferComplete(void);
void Petit_RxRTU(void);
unsigned char Petit_RxDataAvailable(void);
unsigned char Petit_CheckRxTimeout(void);

void DRV_TMR_Start(unsigned h) { (void)h; }
void DRV_TMR_Stop(unsigned h) { (void)h; }
void DRV_TMR_CounterClear(unsigned h) { (void)h; }
/* Reference Modbus CRC-16 (reflected, poly 0xA001, seed 0xFFFF): bit-equivalent
 * to the production lookup-table Petit_CRC16. Used only to seed valid test
 * frames; the production CRC algorithm is unchanged and not under test. */
void Petit_CRC16(const unsigned char b, unsigned int* CRC) {
    unsigned int i;
    *CRC = *CRC ^ (unsigned int)b;
    for (i = 8; i > 0; i--) {
        if (*CRC & 0x0001u) *CRC = (*CRC >> 1) ^ 0xA001u;
        else *CRC >>= 1;
    }
}
/* Production applies CRC_HW_REVERSE only to compensate its hardware-CRC unit;
 * a software reference CRC needs no reversal, so identity keeps frame CRCs
 * self-consistent. The reverse step itself is not what is under test. */
uint16_t CRC_ReverseValue(uint16_t crc) { return crc; }
unsigned char PetitModBus_UART_String(unsigned char *bytes, unsigned length) {
    assert((bytes[1]==16 && length==11) || (bytes[1]==3 && length==8));
    ++wire_count; return TRUE;
}
uint32_t READxCORExTIMER(void) { return now; }
void CREATExTASKxSTATUSxMESSAGE(uint32_t id, uint32_t command,
                              uint32_t state, uint32_t message) {
    (void)id; (void)state; ++reports; last_command=command; last_message=message;
}
void PUTxDATAxINxSENDxMAILxBOX(udpTrans_t *frame) {
    last_frame=*frame;
    ++reports; last_command=frame->data[0]; last_message=frame->data[2];
}
void ENQUEUExGLOBALxSTOP(uint16_t hr0);
void REPORTxGLOBALxSTOPxDELIVERY(uint8_t reason);
void SLAVExCOMMUNICATIONxHANDLER(void);
uint8_t CHECKxMODBUSxCOMMxSTATUS(uint8_t slave, bool overwrite);
bool ISxAMPLIFIERxCOMPATIBLE(uint8_t slave);
bool ISxGLOBALxSTOPxLATCHED(void);
'''
# Worker definitions need the production dependencies declared above them.
worker_start = prefix.index("typedef enum { STOP_UNUSED")
worker_end = prefix.index("static uint8_t      ProcessSlave")
worker = prefix[worker_start:worker_end]
prefix = prefix[:worker_start] + prefix[worker_end:]
logic_start = controller.index("typedef enum { GLOBAL_STOP_DISARMED")
logic = controller[logic_start:controller.index("// *****************************************************************************", logic_start)]
# Real bounded event-ring block, extracted verbatim from slavecommhandler.c.
ring_block = comm[comm.index("#define MBUS_EV_RING"):comm.index("void MBUS_EVENT_PUSH")]

code = prefix + "\n" + ring_block + "\n#pragma warning(push)\n#pragma warning(disable:4018 4100 4101 4189 4244 4245 4267 4020 4459)\n"
for text, names in [
    (startup, ["ISxAMPLIFIERxCOMPATIBLE", "VERIFYxAMPLIFIERxIDENTITY"]),
    (petit, ["PetitSendMessage", "PetitModbusIsReady", "SENDxPETITxMODBUS",
             "Petit_DoSlaveTX", "Petit_TxRTU", "HandlePetitModbusReadHoldingRegistersSlaveReadback",
             "PROCESSxPETITxMODBUS",
             "CheckPetitModbusBufferComplete", "Petit_RxRTU", "Petit_RxDataAvailable",
             "Petit_CheckRxTimeout", "HandleMPetitodbusMbExceptionCodesSlaveReadback",
             "HandleMPetitodbusWriteMultipleRegistersSlaveReadback",
             "HandlePetitModbusWriteSingleRegisterSlaveReadback"]),
    (comm, ["CHECKxMODBUSxCOMMxSTATUS", "SLAVExCOMMUNICATIONxHANDLER",
            "MBUS_EVENT_PUSH", "MBUS_EVENT_REPORT"]),
    (ethernet, ["ISxVALIDxSLAVExDATAxEXCHxFRAME"]),
]:
    code += "".join(function(text, name) for name in names)
code += worker + logic
code += function(startup, "ConfigureSlave") + function(startup, "ENABLExAMPLIFIER")
code += "".join(function(slave, name) for name in ["SendNextMessage", "PROCESSxNEXTxSLAVE",
    "PROCESSxSLAVExCOMMUNICATION", "ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX", "ENQUEUExGLOBALxSTOP"])
code += "\n#pragma warning(pop)\n" + (SRC / "tests/event_sequence_cases.h").read_text()

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
with tempfile.TemporaryDirectory(prefix="pic32-evseq-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "test.c").write_text(code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-", f"/I{SRC}",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
