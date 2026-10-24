# PICO-5B — Physical Session Preflight (Supervised Bootloader Test Preparation)

Date: 2026-10-23. OFFLINE PREPARATION for one supervised PIC18 firmware-download test on
amplifier address 3 only, using the diagnostic `0x2D00` image. **No flashing, reset,
power-cycling, real acquisition, or state-changing diagnostic command is authorized by
this preparation.** This document records the corrected destructive boundary, the
source-established target mapping, the bootloader-bus exclusivity assessment, the recovery
procedure, and the operator checklist.

## 1. Prototype backplane identification

- The prototype/test oval runs the **`Proto_Backplane`** build variant
  (`TrackBackplane2.X/main_proto_backplane.c`), NOT production `main.c`.
- Authoritative evidence: `docs/firmware-flash-recovery-technical-reference.md` §1.1 line 27
  ("prototype/test oval: four prototype amplifiers 1/3/4/6, six available positions, PO-CONFIRMED")
  and §1.3 (the prototype mapping table).
- Identification method: the MPLAB build configuration (`nbproject/project.xml` `default` vs
  `Proto_Backplane`; `Makefile-Proto_Backplane.mk` compiles `main_proto_backplane.c`). There is
  **no board label** — confirm the running firmware via the decode below, not by assuming.

## 2. Exact amplifier-3 physical mapping (prototype)

`SelectSlave(3)` → backplane 51, `HOLDINGREG0 = SLOT1<<2 = 0x04` (`slavefwhandler.c:1207`) →
prototype decode `0x04 → AMP_ID7_SET_LAT=0` (`main_proto_backplane.c:118`) → **RA3** (pin 5,
active-low) → net `ID_7` → **DB15 connector J21, pin 7** → amplifier DB15 pin 7 → RA0 (bootloader
entry).

Full prototype slot-decode (bit → AMP_ID → pin → connector):

| HR0 bit | AMP_ID | PIC18 pin | Connector |
|---|---|---|---|
| 0x01 | AMP_ID5 | RC4 | J9 |
| 0x02 | AMP_ID6 | RA4 | J23 |
| **0x04** | **AMP_ID7** | **RA3** | **J21** |
| 0x08 | AMP_ID8 | RA2 | J19 |
| 0x10 | AMP_ID9 | RA1 | J16 |
| 0x20 | AMP_ID10 | RA0 | J8 |

- **Off-by-N confirmed:** logical slot N → `ID(N+4)`. Address 3 maps to `ID7`, NOT `ID3`.
  On the production firmware the same address 3 would decode to `AMP_ID3`→`ID_3`→**J20** (a
  different physical connector). Do NOT assume "address 3 == slot 3".
- Multi-bit HR0 → no slot (default drives all ID lines high). Mapping is compile-time only
  (not runtime-configurable).
- Read-only verification (no erase): PicoScope digital probe on **U6 pin 5 (RA3)** or **J21 pin 7**
  (low = selected, high = deselected). There is **no per-slot LED** on the backplane.
- **Release:** `SelectSlave` case 6 writes HR0=0. **Residual risk:** the case-3/case-5 error paths
  return ERROR WITHOUT clearing HR0 (the selection stays latched) — an explicit release/cleanup
  step is required after any error.

## 3. Bootloader-bus exclusivity (mandatory precondition B)

- The 8-bit bootloader frame **has no slave address** (`pic18f_bootload.c:205-220`). Every
  bootloader-mode amplifier on the shared RS-422 bus sees and responds to every unaddressed `0x55`
  command, and **holds the RS-422 TX driver** for the whole `Run_Bootloader()` loop.
- The backplane ID pin is a **bootloader-entry selector only** and is released BEFORE the raw 8-bit
  frames; it is NOT a per-frame electrical isolator (`docs/firmware-flash-recovery-technical-reference.md`
  §4.3). A damaged amplifier can enter bootloader **automatically** (checksum mismatch), independent
  of ID selection.
- **Conclusion: software alone CANNOT guarantee that an unaddressed erase/write (125/126) affects only
  the intended amplifier.** Exclusive-target safety requires **qualified physical isolation**: unplug
  all non-target amplifiers (amps 1, 4, 6) from their backplane DB15 connectors, leaving ONLY amp 3 on
  the bus. (This is the established double-fault policy, `docs/decisions.md:627-628`.) No improvised
  bus/wiring modification.

## 4. Command 124 (GET_BOOTLOADER_VERSION) safety

- Command 124 sends `CMD_READ_VERSION` (0x00) (`slavebootloaderroutines.c:44-136`); the PIC18 handler
  (`Get_Version_Data`, `pic18f_bootload.c:260-293`) performs **table reads only** (no NVMCON WR/FREE,
  no reset). It is **genuinely non-destructive / read-only**.
- It is suitable to detect that a bootloader-mode amplifier is present (a 9-bit application amplifier
  does not answer raw 8-bit `0x55`), but it **cannot identify which amplifier answered** (the protocol
  is unaddressed). Use it as a non-destructive pre-flash probe (confirm exactly one bootloader responder)
  BEFORE 123/125.

## 5. Exact single-target command sequence (source-backed)

```
250 CLIENT_CONNECTION_REQUEST   → CONTROLLER/CONNECTED/DONE
103 SLAVE_FW_FLASH              → FWHANDLERINIT/CONNECTED
120 RECEIVE_FW_FILE             → FWFILEDOWNLOAD/140 STANDBY
141 FW_DATA ×480                → per-packet ack, then 143 + RECEIVED_CHECKSUM_OK, FWHANDLER/120/DONE
121 RECEIVE_CONFIG_WORD         → FWCONFIGWORDDOWNLOAD/150
151 config (12 B)               → 152 + FWHANDLER/121/DONE
123 SELECT_SLAVE(id=3)          → NO ack (bounded delay)   [non-destructive: forces target into bootloader]
[124 GET_BOOTLOADER_VERSION]    → 194 OK / 195 NOK         [optional non-destructive probe: confirm one responder]
125 ERASE_FLASH                 → 202  [FIRST DESTRUCTIVE — point of no return]
126 WRITE_FLASH                 → 212
127 WRITE_CONFIG                → 222
128 CHECK_CHECKSUM              → 232
129 SLAVE_RESET                 → 242
130 EXIT                        → NO ack (bounded delay)
```
Never emit 122 / 131 / 132. Never deploy L2 `0x195A`.

## 6. Corrected irreversible-operation boundary

- **The last ordinary non-destructive abort point is BEFORE command 125 (ERASE).** Commands 123
  (SELECT_SLAVE) and 124 (GET_BOOTLOADER_VERSION) are non-destructive (123 resets the target into
  bootloader but does not erase).
- **125 (ERASE) erases `0x0800..0x7FFF` (480×64 B), including the checksum word at 0x7FFE/0x7FFF.**
  The bootloader `0x0000..0x07FF` is preserved by the device fuse `WRTB=ON` (`device_config.c:88`) and
  the PIC32 start-address 0x800 (there is **no in-software low-bound guard**).
- Post-ERASE the application is gone (checksum reads 0xFFFF → any reset re-enters the bootloader), but
  the bootloader stays running and responsive. Post-partial-WRITE the bootloader is still running
  (RESET is only via CMD_RESET_DEVICE 0x09). Do NOT classify post-ERASE as a rollback-free state.

## 7. Recovery procedure

- **Before ERASE (STATE 1):** stop on any wrong response; no flash modification has occurred.
- **After ERASE / partial WRITE (STATE 2):** the amplifier remains bootloader-accessible; recovery =
  re-run the bounded 125→126→127→128→129 sequence (the bootloader is intact). This is safe ONLY while
  exactly one amplifier is on the bus (physical isolation maintained). If isolation cannot be assured,
  STOP and hand to the PO for a separately approved recovery.
- **After success (STATE 3):** verify checksum (`0x2D00`), bootloader exit, application restart, normal
  9-bit Modbus, and that amps 1/4/6 remain `0x251F`. Do NOT auto-start Koploper in a mixed state.
  Restoring `0x2D00`→`0x251F` is a separate, later controlled operation.

## 8. Diagnostic HEX identity

- Temporary image: checksum `0x2D00`, SHA-256
  `ABAEF621C25E1AB046FB98834E7A5DB84D49C0FBEAF49AFEAF62DAAF082D9E53`
  (archived `feature/pico-5b-diagnostic` @ `bea4251`).
- Rollback Offset image: `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex`, SHA-256
  `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D`.
- Do NOT substitute a Combined image for the Offset image in the normal download path.

## 9. Host / PicoScope acquisition plan

- Scope: PICO-4T baseline (native digital edge trigger), D0=RX, D1=TX, D2=AMP_ID select (RA3/J21),
  D3=MCLR (optional; software-reset = flat high; ICSP 12 V hazard — do not probe during programming).
  Threshold 1500 mV, 16 ns/sample.
- Per-event bounded captures (no single all-flash capture): B1 slot-select/bootloader-entry (trigger D2
  falling); B2 9→8-bit transition + bootloader exchange (trigger D1 falling); B3 RESET (optional).
  The 9→8-bit transition repeats per command 125–129.
- Host/scope correlation by **command identity + order** (host monotonic log vs scope sample-relative);
  no sub-millisecond cross-device timing claim. If the host upload fails before 125, report the host
  failure and do NOT erase.
- Python driver: `feature/legacy-l2-stage-a-driver` @ `401291f` (G1–G4 logging, bounded parsing, ack
  handling, no auto-retry).

## 10. Offline verification

- PicoScope regression: 278 passed (PICO-4T baseline). `flash_one.py`: 26 tests OK.
- Firmware-source reconstruction + backplane mapping verified by two independent Developer
  investigations; Integrator review below.

## 11. Independent Integrator verdict

To be completed by the Integrator role (must explicitly judge the two mandatory preconditions and the
corrected ERASE boundary).

## 12. Operator checklist (Dutch) — fysieke sessie

Controleer in deze exacte volgorde; bevestig elk punt expliciet aan de Project Lead:

**A. Hardware-identificatie**
1. Bevestig dat de **prototype-backplane** actief is (de `Proto_Backplane`-variant, 6 posities) — niet de productie-10-slot.
2. Bevestig dat **versterker 3** fysiek op **connector J21** zit (AMP_ID-lijn `ID_7`/RA3, actief-laag).
3. Bevestig de PIC32 draait **L1 0.1.0** (HEX SHA-256 `EC7E3E6E…`).
4. Bevestig de huidige versterkerpopulatie: adres 1, 3, 4, 6 draaien **`0x251F`**.

**B. Bootloader-exclusiviteit (verplicht)**
5. **Koppel de niet-doelversterkers fysiek los** (versterkers 1, 4 en 6 van hun DB15-connector) — alleen versterker 3 blijft op de bus.
6. Bevestig dat er géén andere versterker (ook geen "stille"/ongedetecteerde) op de gedeelde RS-422-bus zit.

**C. Veiligheid**
7. Koploper en alle Siebwalde-hardware-control-clients zijn **gestopt**.
8. Er is **geen** concurrerende PIC32-commandoclient actief.
9. Locomotiefbeweging is **fysiek verhinderd**; versterkeruitgangen zijn veilig.
10. Recovery-gereedschap aanwezig: rollback `0x251F` HEX + de flash-driver + de tijdelijke `0x2D00` HEX.

**D. Instrumentatie**
11. PicoScope 3406D MSO aangesloten; **PicoScope 7 gesloten**.
12. Goedgekeurde probe-bedrading: D0=RX, D1=TX, D2=AMP_ID (RA3/J21 pin 7); geen digitale ingang op een onbevestigde reset/programmeer-lijn (MCLR = software-reset, ICSP 12 V).
13. Logische ground geverifieerd (gedeelde backplane 0 V, niet de H-brug-retour).
14. Digitale drempels correct (1500 mV); host-logging (G1–G4) actief; capture-profiel gereed.

**E. Artefact-verificatie**
15. Volledige SHA-256 van de tijdelijke `0x2D00`-firmware geverifieerd: `ABAEF621C25E1AB046FB98834E7A5DB84D49C0FBEAF49AFEAF62DAAF082D9E53`.
16. Rollback `0x251F` Offset-image beschikbaar (SHA-256 `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D`); juiste bron-/toolrevisies.
17. **Geen** fleet-update-commando (122) ingeschakeld; alleen single-target 123→…→130.

De sessie begint **niet** met speculatieve bedrading of firmware-operaties. De scope wordt vóór elke destructieve stap geactiveerd.

## 13. GO/NO-GO

**CONDITIONAL GO — safe ONLY with qualified physical isolation.** Software alone cannot guarantee
exclusive-target programming (unaddressed 8-bit bootloader bus). The supervised test may proceed ONLY
if: (1) all non-target amplifiers (1, 4, 6) are physically unplugged from the backplane, leaving only
amp 3; (2) the prototype backplane variant and the amp-3 slot (J21 / ID_7 / RA3) are confirmed on the
bench; (3) a read-only command-124 probe confirms at most one bootloader responder before 123/125. If
any of these cannot be confirmed, this is **NO-GO**. No flashing is authorized by this preparation.
