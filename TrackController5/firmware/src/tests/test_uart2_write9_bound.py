"""Offline liveness test for the bounded UART2 TX busy-wait (PV07b defect 3).

Extracts the production UART2_Write9 / UART2_RecoverTx / PetitModBus_UART_String
path unchanged from PetitModbusPort.c (only the noinline attribute, a codegen hint,
is stripped so the extractor can match the signature) and mocks the PIC32 UART2 SFRs
and the core timer. It proves the busy-wait is bounded: a perpetually-stuck UTXBF
returns FALSE after a deadline (and forces a TX recovery), and the failure propagates
out of PetitModBus_UART_String so the caller can classify the transaction.

Software-only: no UART, no timer hardware, no flash, no PIC32 build. Follows the same
"extract production C, mock peripherals/time, never mock safety logic" discipline as
test_detect_case2.py / test_global_stop.py.
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


port = (SRC / "modbus/PetitModbusPort.c").read_text()
# noinline is a codegen hint, not safety logic; strip it so the extractor matches.
port = port.replace("__attribute__((noinline)) ", "")

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

/* Mirror of the production timer calibration (enums.h: MICROSECONDS = 126 at the
 * 126 MHz core-timer rate) and of the file-scope #define in PetitModbusPort.c. */
#define MICROSECONDS 126
#define PETITMODBUS_TX_WRITE_DEADLINE (30u * MICROSECONDS)

/* PIC32 UART2 SFR mocks (control flow only; the exact bit layout is irrelevant). */
static struct {
    unsigned char UTXBF;   /* TX buffer full (read-only hardware flag) */
    unsigned char UTXEN;
    unsigned char URXEN;
    unsigned char OERR;
    unsigned char URXDA;
} U2STAbits;
static struct {
    unsigned char ON;
    unsigned char PDSEL;
    unsigned char STSEL;
} U2MODEbits;
static unsigned short U2TXREG;
static unsigned short U2RXREG;

/* Transmit-path dependencies. */
static uint8_t DummyCounter;
volatile unsigned int LED_TX;
#define Led1On()   ((void)0)
#define Led1Off()  ((void)0)

/* Core timer mock: advances one tick per read so the write deadline can expire. */
static uint32_t now;
uint32_t READxCORExTIMER(void) { return now++; }
'''

cases = r'''
static void reset_uart(void) {
    U2STAbits.UTXBF = 0;
    U2STAbits.UTXEN = 0;
    U2STAbits.URXEN = 0;
    U2STAbits.OERR = 0;
    U2STAbits.URXDA = 0;
    U2MODEbits.ON = 0;
    U2MODEbits.PDSEL = 0;
    U2MODEbits.STSEL = 0;
    U2TXREG = 0;
    U2RXREG = 0;
    DummyCounter = 0;
    LED_TX = 0;
    now = 0;
}

static void test_write9_success(void) {
    reset_uart();
    assert(UART2_Write9(0x0100u | 0x33u) == TRUE);   /* address byte, 9th bit set */
    assert(U2TXREG == (0x0133u & 0x01FFu));           /* payload written */
    puts("PASS: UART2_Write9 writes the byte when UTXBF is clear\n");
}

static void test_write9_stuck_utxbf_bounded(void) {
    reset_uart();
    U2STAbits.UTXBF = 1;                 /* perpetually-full TX buffer */
    U2MODEbits.ON = 1;                   /* UART was on before the failure */
    assert(UART2_Write9(0x0033u) == FALSE);  /* returns failure, does NOT hang */
    /* Recovery re-established 9-bit framing and re-asserted TX/RX enables. */
    assert(U2MODEbits.ON == 1);
    assert(U2MODEbits.PDSEL == 0x3);
    assert(U2STAbits.UTXEN == 1);
    assert(U2STAbits.URXEN == 1);
    puts("PASS: perpetually-stuck UTXBF returns FALSE after a bounded deadline (no hang) and recovers\n");
}

static void test_uart_string_propagates_failure(void) {
    unsigned char frame[8];
    reset_uart();
    frame[0] = 0x33;
    U2STAbits.UTXBF = 1;                 /* first byte write never drains */
    assert(PetitModBus_UART_String(frame, 8) == FALSE);
    puts("PASS: PetitModBus_UART_String returns FALSE when a byte write fails\n");
}

static void test_uart_string_success(void) {
    unsigned char frame[8];
    unsigned i;
    reset_uart();
    for (i = 0; i < 8; ++i) frame[i] = (unsigned char)i;
    assert(PetitModBus_UART_String(frame, 8) == TRUE);
    assert(LED_TX == 2);                 /* incremented once before and once after */
    puts("PASS: PetitModBus_UART_String returns TRUE when all bytes transmit\n");
}

int main(void) {
    test_write9_success();
    test_write9_stuck_utxbf_bounded();
    test_uart_string_propagates_failure();
    test_uart_string_success();
    ExitProcess(0);
    return 0;
}
'''

code = prefix + "\n"
code += "".join(function(port, name) for name in [
    "UART2_RecoverTx", "UART2_Write9",
    "PetitModBus_UART_PutchData", "PetitModBus_UART_PutchAddr",
    "PetitModBus_UART_String"])
code += cases

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
with tempfile.TemporaryDirectory(prefix="pic32-uart2-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "test.c").write_text(code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-", f"/I{SRC}",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
