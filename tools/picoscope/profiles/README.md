# Siebwalde capture profiles (config only)

Reusable, non-executing configuration for three Siebwalde measurement scenarios.
These are **PROPOSALS**, not physical-wiring proof.

| Profile | Scenario | Intended signals |
|---|---|---|
| **A** | legacy Modbus UART | PIC32 TX (9-bit request) / PIC18 TX (8-bit reply), response timing, idle gaps |
| **B** | bootloader transition | TX/RX + RESET + AMP_ID, 9->8-bit transition |
| **C** | amplifier PWM | PWM output + enable/brake timing |

## Usage

```python
from tools.picoscope.profiles import get_profile, all_profiles

p = get_profile("A")
print(p.to_dict())
print(all_profiles())
```

## Channel assignments are PROPOSALS

`D6`/`D7` for the request/reply directions are the historical POD channel
assignments from the P2-A0 capture set (`tools/hmo2024/uart/README.md`). They are
a starting point, not a verified physical-wiring claim. The RESET/AMP_ID/PWM
assignments are placeholders. Physical wiring must be confirmed during a
controlled hardware session (`docs/PICOSCOPE-PICO-4-HARDWARE-TEST-PROCEDURE.md`).

## Decode mode is explicit

Every profile records an explicit UART mode per direction (9-bit request vs
8-bit reply/bootloader). The decoder never guesses a mode; the caller supplies
the mode per segment (see `tools/picoscope/analysis/uart.py`).
