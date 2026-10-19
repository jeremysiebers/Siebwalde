"""Extract production C unchanged; mock peripherals/time, not the safety logic.
Run with Python on Windows with Visual Studio C++ tools. No hardware or flash.
FC03 replies exercise the production readback parser; ACKs/timeouts are injected.
This proves software decisions, not UART timing, PIC18 execution or physical stop.
"""
from pathlib import Path
import os
import re
import subprocess
import tempfile

SRC = Path(__file__).resolve().parents[1]


def function(text, name):
    match = re.search(r"^(?:static )?(?:void|bool|uint8_t|unsigned char) " + re.escape(name) +
                      r"\([^)]*\)\s*\{", text, re.MULTILINE)
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
#include "version.h"
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
static struct { unsigned ModbusReceiveTimeoutHandle; } mbusData;
static uint32_t now, reports, last_command, last_message;
static udpTrans_t last_frame;
static unsigned wire_count;
static uint8_t GoToCase, StartupMachine, EnableMachine, EnableSlave, AddressWriteRetries;
static uint32_t DelayCount1, DelayCount2, threshold;
void DRV_TMR_Start(unsigned h) { (void)h; }
void DRV_TMR_Stop(unsigned h) { (void)h; }
void DRV_TMR_CounterClear(unsigned h) { (void)h; }
void Petit_CRC16(unsigned char b, unsigned int *crc) { *crc ^= b; }
uint16_t CRC_ReverseValue(uint16_t crc) { return crc; }
unsigned char PetitModBus_UART_String(unsigned char *bytes, unsigned length) {
    assert((bytes[1]==16 && length==11) || (bytes[1]==3 && length==8));
    ++wire_count; return TRUE;
}
void Petit_RxRTU(void) { }
unsigned char Petit_RxDataAvailable(void) { return FALSE; }
void HandlePetitModbusWriteSingleRegisterSlaveReadback(void) { assert(false); }
void HandleMPetitodbusWriteMultipleRegistersSlaveReadback(void) { assert(false); }
void HandleMPetitodbusMbExceptionCodesSlaveReadback(void) { assert(false); }
uint32_t READxCORExTIMER(void) { return now; }
void LOG_Push(const char *s) { (void)s; }
void LOG_Printf(const char *s, ...) { (void)s; }
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
code = prefix + "\n#pragma warning(push)\n#pragma warning(disable:4244)\n"
for text, names in [
    (startup, ["ISxAMPLIFIERxCOMPATIBLE", "VERIFYxAMPLIFIERxIDENTITY"]),
    (petit, ["PetitSendMessage", "PetitModbusIsReady", "SENDxPETITxMODBUS",
             "Petit_DoSlaveTX", "Petit_TxRTU", "HandlePetitModbusReadHoldingRegistersSlaveReadback",
             "PROCESSxPETITxMODBUS"]),
    (comm, ["CHECKxMODBUSxCOMMxSTATUS", "SLAVExCOMMUNICATIONxHANDLER"]),
    (ethernet, ["ISxVALIDxSLAVExDATAxEXCHxFRAME"]),
]:
    code += "".join(function(text, name) for name in names)
code += worker + logic
code += function(startup, "ConfigureSlave") + function(startup, "ENABLExAMPLIFIER")
code += "".join(function(slave, name) for name in ["SendNextMessage", "PROCESSxNEXTxSLAVE",
    "PROCESSxSLAVExCOMMUNICATION", "ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX", "ENQUEUExGLOBALxSTOP"])
code += "\n#pragma warning(pop)\n" + (SRC / "tests/phase1_cases.h").read_text()

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
with tempfile.TemporaryDirectory(prefix="pic32-phase1-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "test.c").write_text(code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-", f"/I{SRC}",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
