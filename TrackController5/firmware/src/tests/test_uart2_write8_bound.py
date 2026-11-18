"""Offline liveness test for the bounded UART2 TX busy-wait in bootloader mode.

Extracts the production UART2_Write8 unchanged from slavebootloaderroutines.c (only
the noinline attribute, a codegen hint, is stripped so the extractor can match the
signature) and mocks the PIC32 UART2 SFRs and the core timer. It proves the
busy-wait is bounded: a perpetually-stuck UTXBF returns false (and re-asserts the
transmitter), so SendDataToBootloader can report failure instead of hanging.

Software-only: no UART, no timer hardware, no flash, no PIC32 build. Mirrors the
test_uart2_write9_bound.py discipline ("extract production C, mock peripherals/time,
never mock safety logic").
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


btldr = (SRC / "slavebootloaderroutines.c").read_text()
btldr = btldr.replace("__attribute__((noinline)) ", "")

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

/* Mirror of the production timer calibration (enums.h: MICROSECONDS = 126) and of
 * the file-scope #define in slavebootloaderroutines.c. */
#define MICROSECONDS 126
#define BOOTLOADER_TX_WRITE_DEADLINE (30u * MICROSECONDS)

/* PIC32 UART2 SFR mocks (control flow only; the exact bit layout is irrelevant). */
static struct {
    unsigned char UTXBF;   /* TX buffer full (read-only hardware flag) */
    unsigned char UTXEN;
} U2STAbits;
static unsigned short U2TXREG;

/* Core timer mock: advances one tick per read so the write deadline can expire. */
static uint32_t now;
uint32_t READxCORExTIMER(void) { return now++; }
'''

cases = r'''
static void reset_uart(void) {
    U2STAbits.UTXBF = 0;
    U2STAbits.UTXEN = 0;
    U2TXREG = 0;
    now = 0;
}

static void test_write8_success(void) {
    reset_uart();
    assert(UART2_Write8(0x33u) == true);
    assert(U2TXREG == (0x0033u & 0x00FFu));          /* payload written */
    puts("PASS: UART2_Write8 writes the byte when UTXBF is clear\n");
}

static void test_write8_stuck_utxbf_bounded(void) {
    reset_uart();
    U2STAbits.UTXBF = 1;                 /* perpetually-full TX buffer */
    assert(UART2_Write8(0x33u) == false); /* returns failure, does NOT hang */
    assert(U2STAbits.UTXEN == 1);         /* recovery re-asserted the transmitter */
    puts("PASS: perpetually-stuck UTXBF returns false after a bounded deadline (no hang) and recovers\n");
}

int main(void) {
    test_write8_success();
    test_write8_stuck_utxbf_bounded();
    ExitProcess(0);
    return 0;
}
'''

code = prefix + "\n"
code += function(btldr, "UART2_Write8")
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
with tempfile.TemporaryDirectory(prefix="pic32-uart2w8-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "test.c").write_text(code)
    subprocess.run([compiler, "/nologo", "/W4", "/WX", "/GS-", f"/I{SRC}",
                    "test.c", "/Fe:test.exe", "/link", "/entry:main",
                    "/subsystem:console", "/nodefaultlib", "kernel32.lib"],
                   cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
