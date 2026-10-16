"""Compile production PIC18 core + adapter on Win64, no hardware or XC8 emulation.

Run: python TrackAmplifier4.X/tests/test_amp.py
Uses installed MSVC and Windows SDK like the PIC32 test_global_stop.py harness.
Minimal standard headers avoid a dependency on optional CRT development files.
"""
from pathlib import Path
import os
import re
import subprocess
import tempfile

SRC = Path(__file__).resolve().parents[1]
vswhere = Path(os.environ["ProgramFiles(x86)"]) / "Microsoft Visual Studio/Installer/vswhere.exe"
compiler = subprocess.check_output([str(vswhere), "-latest", "-products", "*", "-find",
    r"VC\Tools\MSVC\**\bin\Hostx64\x64\cl.exe"], text=True).strip().splitlines()[-1]
vc = Path(compiler).parents[3]
kits = Path(os.environ["ProgramFiles(x86)"]) / "Windows Kits/10"
sdk = sorted((kits / "Lib").iterdir())[-1].name
env = os.environ.copy()
env["LIB"] = str(kits / "Lib" / sdk / "um/x64")
base = Path(os.environ["LOCALAPPDATA"]) / "Temp/opencode"
regulator = (SRC / "regulator.c").read_text()
match = re.search(r"void AmpHw_SetDuty\(uint16_t duty\)\s*\{[^}]*\}", regulator)
assert match, "Production PWM accessor must be extracted unchanged"

def function(text, name):
    found = re.search(r"^(?:void|bool) " + re.escape(name) + r"\([^)]*\)\s*\{", text, re.MULTILINE)
    assert found, name
    depth = 1
    tokens = re.compile(r'//[^\n]*|/\*.*?\*/|"(?:\\.|[^"\\])*"|[{}]', re.DOTALL)
    for token in tokens.finditer(text, found.end()):
        depth += (token.group() == "{") - (token.group() == "}")
        if depth == 0:
            return text[found.start():token.end()]
    raise AssertionError(name)

transport_prefix = r'''
#include "amp_adapter.h"
#include "amp_core.h"
#include "modbus/General.h"
#include "modbus/modbushooks.h"
#include "tests/petit_shim.h"
#define NUMBER_OF_HOLDING_PETITREGISTERS 21u
#define PETITMODBUS_READ_HOLDING_REGISTERS 3
#define PETITMODBUS_WRITE_SINGLE_REGISTER 6
#define PETITMODBUS_WRITE_MULTIPLE_REGISTERS 16
#define PETIT_ERROR_CODE_02 2
#define PETIT_ERROR_CODE_03 3
static uint8_t PETITMODBUS_BROADCAST_ADDRESS = 0;
static uint8_t PETITMODBUS_SLAVE_ADDRESS = 1;
typedef struct { uint8_t Address, Function, DataBuf[52]; uint16_t DataLen; } Frame;
static Frame Petit_Rx_Data, Petit_Tx_Data;
static unsigned errors, sends, error_code;
static void HandlePetitModbusError(uint8_t code, uint8_t fn) {
    (void)fn;
    if (Petit_Rx_Data.Address != 0) { errors++; error_code = code; }
}
static void PetitSendMessage(void) { sends++; }
'''
petit = (SRC / "modbus/PetitModbus.c").read_text()
main = (SRC / "main.c").read_text()
# Compile unchanged startup guards and the actual state-1 body, mocking only IO.
startup_prefix = '\n#include "main.h"\n'
for name in ("MODBUS_ADDRESS", "Config", "Startup_Machine"):
    declaration = re.search(r"^static uint8_t " + name + r" = [^;]+;", main, re.MULTILINE)
    assert declaration, name
    startup_prefix += declaration.group() + "\n"
startup_prefix += r'''
static uint8_t ID_PORT, LED_ERR, LED_ERR_LAT, LED_RUN_LAT, LED_WAR_LAT, LED_TX_LAT, LED_RX_LAT;
static uint8_t ProcessPetitModbus(void) { return 1; }
void Led_Blink(void) { }
static void InitPetitModbus(uint8_t address) { (void)address; }
'''
startup_prefix += function(main, "Amplifier_IsBootloaderWrite") + "\n"
startup_prefix += function(main, "Amplifier_TryBootloaderWrite") + "\n"
state1 = main[main.index("                case 1 :"):main.index("                case 2 :")]
startup_prefix += "\nstatic void service_startup(void) { uint8_t result = 0; switch (Startup_Machine) {\n" + state1 + "\ndefault: break; } (void)result; }\n"
# PIC18 unsigned int is 16-bit; host unsigned int is 32-bit. Suppress narrowing
# only around the unchanged legacy transport functions, not the core/adapter.
transport = transport_prefix + startup_prefix + "\n#pragma warning(push)\n#pragma warning(disable:4244)\n" + "\n".join(function(petit, name) for name in (
    "HandlePetitModbusReadHoldingRegisters", "HandlePetitModbusWriteSingleRegister",
    "HandleMPetitodbusWriteMultipleRegisters")) + "\n#pragma warning(pop)\n" + (SRC / "tests/test_transport.c").read_text()
with tempfile.TemporaryDirectory(prefix="pic18-control-", dir=base) as directory:
    tmp = Path(directory)
    (tmp / "stdint.h").write_text("#pragma once\ntypedef unsigned char uint8_t;\n"
        "typedef unsigned short uint16_t;\ntypedef unsigned long uint32_t;\n")
    (tmp / "stdbool.h").write_text("#pragma once\n#define bool _Bool\n#define true 1\n#define false 0\n")
    (tmp / "string.h").write_text("void *memset(void *, int, unsigned long long);\n")
    (tmp / "test.c").write_text((SRC / "tests/test_amp.c").read_text() + "\n" + match.group())
    (tmp / "transport.c").write_text(transport)
    subprocess.run([compiler, "/nologo", "/std:c11", "/W4", "/WX", "/GS-", "/Oi-", "/DAMP_HOST_TEST",
        f"/I{tmp}", f"/I{SRC}", f"/I{SRC / 'tests'}", "test.c", str(SRC / "amp_core.c"),
        str(SRC / "amp_adapter.c"), str(SRC / "modbus/modbushooks.c"), "transport.c", "/Fe:test.exe", "/link", "/entry:main", "/subsystem:console",
        "/nodefaultlib", "kernel32.lib"], cwd=tmp, env=env, check=True)
    subprocess.run([str(tmp / "test.exe")], cwd=tmp, check=True)
print("PASS: unchanged production core + adapter + duty accessor; software-only")
