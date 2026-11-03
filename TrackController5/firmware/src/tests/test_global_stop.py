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
#define CTRL_FW_PATCH 1u
#define CTRL_PROTOCOL_VERSION 0x01u
#define CTRL_RELEASE_TIMESTAMP "2026-10-26"
enum { MBUS_EV_ENQUEUE = 1, MBUS_EV_GATE_DROP = 2, MBUS_EV_FC16_SENT = 3,
       MBUS_EV_PURGE = 4, MBUS_EV_TX_DONE = 5, MBUS_EV_ACK = 6, MBUS_EV_TIMEOUT = 7 };
void MBUS_EVENT_PUSH(uint8_t type, uint8_t slave, uint8_t reg, uint16_t val, uint8_t rv) {
    (void)type; (void)slave; (void)reg; (void)val; (void)rv;
}
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
static uint8_t GoToCase, StartupMachine, EnableMachine, EnableSlave;
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

# ---- Session/connection-defect harness: real ETHERNET_Tasks/CONTROLLER_Tasks and
#      real ring mailboxes, against test-controlled Harmony UDP/TCP + MBUS stubs. ----
session_prefix = r'''
typedef unsigned char uint8_t;
typedef unsigned short uint16_t;
typedef signed short int16_t;
typedef unsigned int uint32_t;
typedef signed int int32_t;
typedef unsigned long long size_t;
typedef unsigned char bool;
#define true ((bool)1)
#define false ((bool)0)
#define NULL ((void *)0)
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
void *memcpy(void *dst, const void *src, unsigned long long size) {
    unsigned char *d=dst;
    const unsigned char *s=src;
    while(size--) *d++ = *s++;
    return dst;
}
#include "enums.h"
#define CTRL_FW_MAJOR 1u
#define CTRL_FW_MINOR 0u
#define CTRL_FW_PATCH 1u
#define CTRL_PROTOCOL_VERSION 0x01u
#define CTRL_RELEASE_TIMESTAMP "2026-10-26"
'''
session_prefix += r'''
typedef int UDP_SOCKET;
typedef int TCP_SOCKET;
#define INVALID_SOCKET (-1)
typedef union { uint32_t Val; uint8_t v[4]; } IPV4_ADDR;
typedef IPV4_ADDR IP_MULTI_ADDRESS;
typedef struct { IPV4_ADDR remoteIPaddress; } UDP_SOCKET_INFO;
typedef int SYS_STATUS;
#define SYS_STATUS_READY 2
typedef int TCPIP_NET_HANDLE;
typedef struct { int tcpip; } SYS_OBJECTS;
#define IP_ADDRESS_TYPE_IPV4 2
#define SERVER_PORT 10000
#define CLIENT_PORT 10001
#define MAILBOXSIZE 8
#define LOG_TCP_PORT 50000
SYS_OBJECTS sysObj;

typedef enum {
    ETHERNET_TCPIP_WAIT_INIT, ETHERNET_TCPIP_WAIT_FOR_IP, ETHERNET_TCPIP_OPENING_SERVER,
    ETHERNET_TCPIP_WAIT_FOR_CONNECTION, ETHERNET_TCPIP_DATA_RX, ETHERNET_TCPIP_DATA_TX,
    ETHERNET_TCPIP_ERROR
} ETHERNET_STATES;
typedef struct { ETHERNET_STATES state; SYS_STATUS tcpipStat; UDP_SOCKET recvsocket; UDP_SOCKET transsocket; } ETHERNET_DATA;
typedef enum {
    CONTROLLER_STATE_INIT=0, CONTROLLER_STATE_WAITING_FOR_CLIENT, CONTROLLER_STATE_IDLE,
    CONTROLLER_STATE_HANDLE_COMM_DATA, CONTROLLER_STATE_CHECK_MBUS_STATE
} CONTROLLER_STATES;
typedef struct { CONTROLLER_STATES state; } CONTROLLER_DATA;
typedef enum {
    MBUS_STATE_INIT=0, MBUS_STATE_WAIT, MBUS_STATE_SLAVES_ON, MBUS_STATE_SLAVES_BOOT_WAIT,
    MBUS_STATE_SLAVE_DETECT, MBUS_STATE_SLAVE_FW_FLASH, MBUS_STATE_SLAVE_INIT,
    MBUS_STATE_SLAVE_ENABLE, MBUS_STATE_START_DATA_UPLOAD, MBUS_STATE_SERVICE_TASKS,
    MBUS_STATE_RESET, MBUS_STATE_RESET_WAIT
} MBUS_STATES;

ETHERNET_DATA ethernetData;
UDP_SOCKET_INFO SocketInfo;
IP_MULTI_ADDRESS IpAddress;
const char *netName, *netBiosName;
static IPV4_ADDR dwLastIP[2];
IPV4_ADDR ipAddr;
int i, nNets;
TCPIP_NET_HANDLE netH;
static bool TransSocketReady = false;
static udpTrans_t *M_Box_Eth_Recv_Ptr;
static udpTrans_t *M_Box_Eth_Recv_Ptr_prev;
static udpTrans_t *M_Box_Eth_Send_Ptr;
static udpTrans_t *M_Box_Eth_Send_Ptr_prev;
static udpTrans_t udpRecvBox[MAILBOXSIZE];
static udpTrans_t udpTransBox[MAILBOXSIZE];
static TCP_SOCKET logSocket = INVALID_SOCKET;
static uint8_t RecvMailboxCount = 0;
static uint8_t SendMailboxCount = 0;
static uint16_t RecvOverflowCount = 0;
static uint16_t SendOverflowCount = 0;

CONTROLLER_DATA controllerData;
uint8_t ControllerCommand;
udpTrans_t *EthernetRecvData;

/* Cross-file forward declarations (mirror ethernet.h / controller.h). */
void CREATExTASKxSTATUSxMESSAGE(uint32_t task_id, uint32_t task_command, uint32_t task_state, uint32_t task_messages);
void PUTxDATAxINxSENDxMAILxBOX(udpTrans_t *data);
'''
session_prefix += r'''
static uint32_t now;
uint32_t READxCORExTIMER(void) { return now; }
void LOG_Push(const char *s, ...) { (void)s; }
void LOG_Printf(const char *s, ...) { (void)s; }
static uint32_t mbus_state;
uint32_t GETxMBUSxSTATE(void) { return mbus_state; }
void SETxMBUSxSTATE(uint32_t state) { mbus_state = state; }
void ENQUEUExGLOBALxSTOP(uint16_t hr0) { (void)hr0; }
void REPORTxGLOBALxSTOPxDELIVERY(uint8_t reason) { (void)reason; }
void ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX(uint8_t *data) { (void)data; }

static bool recv_connected, trans_connected;
static int rx_avail;
static udpTrans_t rx_frame;
static IPV4_ADDR client_ip;
static IPV4_ADDR last_client_open_ip;
static int recv_open_count, trans_open_count, recv_close_count, trans_close_count;
static int sent_count;
static udpTrans_t sent_frame;
enum { RECV_SOCK = 1, LOG_SOCK = 2, TRANS_SOCK = 3 };

SYS_STATUS TCPIP_STACK_Status(int h) { (void)h; return SYS_STATUS_READY; }
int TCPIP_STACK_NumberOfNetworksGet(void) { return 1; }
TCPIP_NET_HANDLE TCPIP_STACK_IndexToNet(int idx) { (void)idx; return 0; }
const char *TCPIP_STACK_NetNameGet(TCPIP_NET_HANDLE h) { (void)h; return "net"; }
const char *TCPIP_STACK_NetBIOSName(TCPIP_NET_HANDLE h) { (void)h; return "host"; }
bool TCPIP_STACK_NetIsReady(TCPIP_NET_HANDLE h) { (void)h; return true; }
uint32_t TCPIP_STACK_NetAddress(TCPIP_NET_HANDLE h) { (void)h; return 0x0a000001u; }
UDP_SOCKET TCPIP_UDP_ServerOpen(int t, unsigned p, void *h) { (void)t;(void)p;(void)h; ++recv_open_count; return RECV_SOCK; }
TCP_SOCKET TCPIP_TCP_ServerOpen(int t, unsigned p, void *h) { (void)t;(void)p;(void)h; return LOG_SOCK; }
UDP_SOCKET TCPIP_UDP_ClientOpen(int t, unsigned p, const IPV4_ADDR *ip) { (void)t;(void)p; ++trans_open_count; last_client_open_ip = *ip; return TRANS_SOCK; }
bool TCPIP_UDP_IsConnected(UDP_SOCKET s) {
    if (s == RECV_SOCK) return recv_connected;
    return trans_connected;
}
void TCPIP_UDP_SocketInfoGet(UDP_SOCKET s, UDP_SOCKET_INFO *info) { (void)s; info->remoteIPaddress = client_ip; }
void TCPIP_UDP_Discard(UDP_SOCKET s) { (void)s; }
void TCPIP_UDP_Close(UDP_SOCKET s) {
    (void)s;
    if (s == TRANS_SOCK) ++trans_close_count;
    else if (s == RECV_SOCK) ++recv_close_count;
}
int16_t TCPIP_UDP_GetIsReady(UDP_SOCKET s) { (void)s; return (int16_t)rx_avail; }
int16_t TCPIP_UDP_PutIsReady(UDP_SOCKET s) { (void)s; return 0; }
int32_t TCPIP_UDP_ArrayGet(UDP_SOCKET s, void *buf, size_t len) {
    int32_t n;
    (void)s;
    if (rx_avail <= 0) return 0;
    n = (int32_t)((size_t)rx_avail < len ? (size_t)rx_avail : len);
    memcpy(buf, &rx_frame, (unsigned long long)n);
    return n;
}
int32_t TCPIP_UDP_ArrayPut(UDP_SOCKET s, const void *buf, size_t len) {
    (void)s;
    if (s == TRANS_SOCK) {
        ++sent_count;
        memset(&sent_frame, 0, sizeof(sent_frame));
        memcpy(&sent_frame, buf, len < sizeof(sent_frame) ? len : sizeof(sent_frame));
    }
    return (int32_t)len;
}
void TCPIP_UDP_Flush(UDP_SOCKET s) { (void)s; }
'''
_session_logic_start = controller.index("typedef enum { GLOBAL_STOP_DISARMED")
_session_logic = controller[_session_logic_start:controller.index("// *****************************************************************************", _session_logic_start)]
session_code = session_prefix + "\n#pragma warning(push)\n#pragma warning(disable:4018 4100 4101 4189 4244 4245 4267 4020 4459)\n"
session_code += _session_logic
session_code += "".join(function(ethernet, name) for name in [
    "PutDataInReceiveMailBox", "GETxDATAxFROMxRECEIVExMAILxBOX",
    "CHECKxDATAxINxRECEIVExMAILxBOX", "PEEKxRECEIVExMAILxBOX",
    "GetDataFromSendMailBox", "CheckDataInSendMailBox",
    "PUTxDATAxINxSENDxMAILxBOX", "CREATExTASKxSTATUSxMESSAGE",
    "DISCONNECTxCLIENT", "GETxETHERNETxSTATE",
    "ISxVALIDxSLAVExDATAxEXCHxFRAME", "ETHERNET_Tasks"])
session_code += function(controller, "CONTROLLER_Tasks")
session_code += "\n#pragma warning(pop)\n" + (SRC / "tests/session_cases.h").read_text()

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
    (tmp / "session.c").write_text(session_code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-", f"/I{SRC}",
                    "session.c", "/Fe:session.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "session.exe")], cwd=tmp, check=True)
