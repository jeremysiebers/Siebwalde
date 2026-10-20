"""Offline regression for the TrackBackplane2.X broadcast rejection.

Extracts the production ProcessPetitModbus dispatch and the FC06/FC16 write
handlers unchanged from modbus/PetitModbus.c, mocks PIC18 peripherals/UART/CRC,
and asserts that a broadcast (address 0) frame is rejected BEFORE any
holding-register write while valid unicast writes still work. No hardware.
"""
from pathlib import Path
import os
import re
import subprocess
import tempfile

SRC = Path(__file__).resolve().parents[1]  # TrackBackplane2.X
petit = (SRC / "modbus/PetitModbus.c").read_text()


def function(text, name):
    match = re.search(r"^(?:void|unsigned char|unsigned short|uint16_t) " + re.escape(name) +
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


prefix = r'''
typedef unsigned char uint8_t;
typedef unsigned int uint16_t;
#define NOP() ((void)0)
#define TRUE 1
#define FALSE 0
__declspec(dllimport) void __stdcall ExitProcess(unsigned int code);
__declspec(dllimport) void * __stdcall GetStdHandle(unsigned int handle);
__declspec(dllimport) int __stdcall WriteFile(void *, const void *, unsigned int, unsigned int *, void *);
static void puts(const char *s) {
    unsigned int n = 0, w;
    while (s[n]) ++n;
    WriteFile(GetStdHandle((unsigned int)-11), s, n, &w, 0);
}
#define assert(expr) do { if (!(expr)) { puts("FAIL: " #expr "\n"); ExitProcess(1); } } while (0)

/* --- PetitModbus configuration (mirrors modbus/PetitModbus.h) --- */
#define NUMBER_OF_HOLDING_PETITREGISTERS                 4
#define NUMBER_OF_INPUT_PETITREGISTERS                   6
#define NUMBER_OF_DIAGNOSTIC_PETITREGISTERS              2
#define PETITMODBUS_TIMEOUTTIMER                         2
#define PETITMODBUS_READ_HOLDING_REGISTERS_ENABLED      ( 1 )
#define PETITMODBUSWRITE_SINGLE_REGISTER_ENABLED        ( 1 )
#define PETITMODBUS_WRITE_MULTIPLE_REGISTERS_ENABLED    ( 1 )
#define PETITMODBUS_READ_INPUT_REGISTERS_ENABLED        ( 1 )
#define PETITMODBUS_DIAGNOSTIC_REGISTERS_ENABLED        ( 1 )
#define PETITMODBUS_RECEIVE_BUFFER_SIZE                 (NUMBER_OF_HOLDING_PETITREGISTERS*2 + 10)
#define PETITMODBUS_TRANSMIT_BUFFER_SIZE                PETITMODBUS_RECEIVE_BUFFER_SIZE
#define PETITMODBUS_RXTX_BUFFER_SIZE                    PETITMODBUS_TRANSMIT_BUFFER_SIZE
#define PETITMODBUS_READ_COILS                          1
#define PETITMODBUS_READ_DISCRETE_INPUTS                2
#define PETITMODBUS_READ_HOLDING_REGISTERS              3
#define PETITMODBUS_READ_INPUT_REGISTERS                4
#define PETITMODBUS_WRITE_SINGLE_COIL                   5
#define PETITMODBUS_WRITE_SINGLE_REGISTER               6
#define PETITMODBUS_DIAGNOSTIC_REGISTERS                8
#define PETITMODBUS_WRITE_MULTIPLE_COILS                15
#define PETITMODBUS_WRITE_MULTIPLE_REGISTERS            16
#define PETIT_FALSE_FUNCTION                            0
#define PETIT_FALSE_SLAVE_ADDRESS                       1
#define PETIT_DATA_NOT_READY                            2
#define PETIT_DATA_READY                                3
#define PETIT_ERROR_CODE_01                             0x01
#define PETIT_ERROR_CODE_02                             0x02
#define PETIT_ERROR_CODE_03                             0x03

typedef struct { unsigned int ActValue; } PetitRegStructure;
PetitRegStructure PetitHoldingRegisters[NUMBER_OF_HOLDING_PETITREGISTERS];
PetitRegStructure PetitInputRegisters[NUMBER_OF_INPUT_PETITREGISTERS];
PetitRegStructure PetitDiagnosticRegisters[NUMBER_OF_DIAGNOSTIC_PETITREGISTERS];
unsigned char PETITMODBUS_SLAVE_ADDRESS = 51;
unsigned char PETITMODBUS_BROADCAST_ADDRESS = 0;

typedef enum {
    PETIT_RXTX_IDLE, PETIT_RXTX_START, PETIT_RXTX_DATABUF,
    PETIT_RXTX_WAIT_ANSWER, PETIT_RXTX_TIMEOUT
} PETIT_RXTX_STATE;
typedef struct {
    unsigned char Address;
    unsigned char Function;
    unsigned char DataBuf[PETITMODBUS_RXTX_BUFFER_SIZE];
    unsigned short DataLen;
} PETIT_RXTX_DATA;
PETIT_RXTX_DATA Petit_Tx_Data;
PETIT_RXTX_STATE Petit_Tx_State = PETIT_RXTX_IDLE;
PETIT_RXTX_DATA Petit_Rx_Data;
unsigned char Petit_Rx_Data_Available = 0;
unsigned char Petit_Tx_Buf[PETITMODBUS_TRANSMIT_BUFFER_SIZE];
unsigned int Petit_Tx_Buf_Size = 0;
unsigned int Petit_Tx_CRC16 = 0xFFFF;
unsigned int Petit_Rx_CRC16 = 0xFFFF;
PETIT_RXTX_STATE Petit_Rx_State = PETIT_RXTX_IDLE;
unsigned int Petit_Tx_Current = 0;
volatile unsigned int LED_TX = 0, LED_RX = 0;
volatile unsigned char PetitReceiveBuffer[PETITMODBUS_RECEIVE_BUFFER_SIZE];
volatile unsigned char PetitReceiveCounter = 0;
unsigned short PetitModbusTimerValue = 0;

/* --- PIC18 peripheral mocks --- */
static unsigned tx_count = 0;
void Petit_RxRTU(void) { }                      /* test sets Petit_Rx_Data directly */
void Petit_TxRTU(void) { ++tx_count; }          /* count attempted responses */
void Petit_DoSlaveTX(void) { }
void Petit_CRC16(const unsigned char d, unsigned int *crc) { (void)d; (void)crc; }
unsigned char PetitModBus_UART_String(unsigned char *s, unsigned int l) { (void)s; (void)l; return 1; }
void PetitModBus_UART_Initialise(void) { }
void PetitModBus_TIMER_Initialise(void) { }
void PetitModBus_UART_Putch(unsigned char c) { (void)c; }
void ReceiveInterrupt(unsigned char d, unsigned char n) { (void)d; (void)n; }
void PetitModBus_TimerValues(void) { }

/* --- forward declarations (production functions extracted below) --- */
void HandlePetitModbusError(unsigned char ErrorCode, unsigned char Function);
unsigned char PetitSendMessage(void);
void HandlePetitModbusReadHoldingRegisters(void);
void HandlePetitModbusReadInputRegisters(void);
void HandlePetitModbusWriteSingleRegister(void);
void HandleMPetitodbusDiagnosticRegisters(void);
void HandleMPetitodbusWriteMultipleRegisters(void);
unsigned char Petit_RxDataAvailable(void);
void ProcessPetitModbus(void);
'''

code = prefix + "\n#pragma warning(disable:4244)\n"
for name in ["HandlePetitModbusError",
             "PetitSendMessage",
             "HandlePetitModbusReadHoldingRegisters",
             "HandlePetitModbusReadInputRegisters",
             "HandlePetitModbusWriteSingleRegister",
             "HandleMPetitodbusDiagnosticRegisters",
             "HandleMPetitodbusWriteMultipleRegisters",
             "Petit_RxDataAvailable",
             "ProcessPetitModbus"]:
    code += function(petit, name)

code += r'''
static void rx(unsigned char addr, unsigned char fn, const unsigned char *buf, unsigned short len) {
    unsigned short i;
    Petit_Rx_Data.Address = addr;
    Petit_Rx_Data.Function = fn;
    Petit_Rx_Data.DataLen = len;
    for (i = 0; i < len; ++i) Petit_Rx_Data.DataBuf[i] = buf[i];
    Petit_Rx_Data_Available = 1;
    Petit_Tx_State = PETIT_RXTX_IDLE;
}

int main(void) {
    unsigned char w6[4] = {0, 0, 0, 1};       /* FC06: HR0 <- 1 */
    unsigned char w6sel[4] = {0, 0, 0, 2};    /* FC06: HR0 <- 2 (select) */
    unsigned char w6desel[4] = {0, 0, 0, 0};  /* FC06: HR0 <- 0 (deselect) */
    unsigned char w6h1[4] = {0, 1, 0, 5};     /* FC06: HR1 <- 5 */
    unsigned char w6bad[4] = {0, 99, 0, 1};   /* FC06: register 99 (invalid) */
    unsigned char w16[7] = {0, 0, 0, 1, 2, 0, 1}; /* FC16: HR0 <- 1 */

    /* 1. Broadcast FC06 write to HR0 -> rejected (no write, no response). */
    rx(0, 6, w6, 4); ProcessPetitModbus();
    assert(PetitHoldingRegisters[0].ActValue == 0);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);

    /* 2. Broadcast FC16 spanning HR0 -> rejected. */
    rx(0, 16, w16, 7); ProcessPetitModbus();
    assert(PetitHoldingRegisters[0].ActValue == 0);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);

    /* 3. Broadcast FC06 to another register (HR1) -> rejected. */
    rx(0, 6, w6h1, 4); ProcessPetitModbus();
    assert(PetitHoldingRegisters[1].ActValue == 0);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);

    /* 4. Broadcast FC06 with invalid register address -> rejected (no OOB). */
    rx(0, 6, w6bad, 4); ProcessPetitModbus();
    assert(PetitHoldingRegisters[0].ActValue == 0);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);

    /* 5. Valid unicast HR0 select -> accepted + response. */
    rx(51, 6, w6sel, 4); ProcessPetitModbus();
    assert(PetitHoldingRegisters[0].ActValue == 2);
    assert(Petit_Tx_State == PETIT_RXTX_START);

    /* 6. Valid unicast HR0 deselect -> accepted + response. */
    rx(51, 6, w6desel, 4); ProcessPetitModbus();
    assert(PetitHoldingRegisters[0].ActValue == 0);
    assert(Petit_Tx_State == PETIT_RXTX_START);

    /* 7. Broadcast then a valid unicast -> the unicast still works. */
    rx(0, 6, w6, 4); ProcessPetitModbus();
    assert(PetitHoldingRegisters[0].ActValue == 0);
    rx(51, 6, w6sel, 4); ProcessPetitModbus();
    assert(PetitHoldingRegisters[0].ActValue == 2);

    /* 8. Repeated broadcast -> no state corruption. */
    rx(0, 6, w6, 4); ProcessPetitModbus();
    rx(0, 16, w16, 7); ProcessPetitModbus();
    rx(0, 6, w6, 4); ProcessPetitModbus();
    assert(PetitHoldingRegisters[0].ActValue == 2);
    assert(PetitHoldingRegisters[1].ActValue == 0);
    assert(Petit_Tx_State == PETIT_RXTX_IDLE);

    puts("PASS: backplane rejects all broadcasts before any write; unicast HR0 select/deselect still work\n");
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
with tempfile.TemporaryDirectory(prefix="bp-broadcast-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "test.c").write_text(code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
