# PICOSCOPE PROFILE B — Bootloader Measurement Plan (PICO-5B)

Date: 2026-10-23. OFF-LINE PREPARATION for one controlled PIC18 bootloader validation
experiment. This document records the temporary checksum-test firmware, the source-reconstructed
flash sequence, the Profile B instrumentation plan, the safety constraints, and the GO/NO-GO.
**No hardware was flashed, connected, reset or powered during this preparation.**

## 1. Temporary firmware (checksum-test image)

### 1.1 Source change (the ONLY change)
- Source commit: `928ea7cf6061737d26db56580c236347f0f746a8` (`928ea7c`).
- `TrackAmplifier4.X/main.c` line 27:
  ```c
  //static uint8_t test = 0xFE;   ->   static uint8_t test = 0xFE;
  ```
  `test` is never referenced (dead variable); no `volatile`, no additional references.
- The source was **restored** immediately after the build; `main.c:27` is back to the commented form
  (`git diff` on tracked files empty).

### 1.2 Toolchain / configuration
- Build configuration: `Offset` (PIC18F25K40, `-O0`, `-mcodeoffset=0x800`,
  linker checksum `800-7FFD@7FFE,width=-2,algorithm=2`).
- Compiler XC8 v2.31, MPLAB X v6.20, headless `prjMakefilesGenerator` + GNU `make`.

### 1.3 Checksum result
| | Value |
|---|---|
| Legacy baseline (0x7FFE/0x7FFF) | `0x251F` |
| Temporary image (0x7FFE/0x7FFF) | **`0x2D00`** |

Temporary HEX SHA-256: `ABAEF621C25E1AB046FB98834E7A5DB84D49C0FBEAF49AFEAF62DAAF082D9E53`
(86 529 bytes). Verified by three independent methods (linker word, record recompute, host row algorithm).

### 1.4 Binary equivalence assessment
- `test` IS emitted (not optimized away at `-O0`): RAM `_test` @ `0x0051`, ROM initializer `0xFE` @ `0x20C4`;
  it is **dead** (zero code references).
- The delta is a **full re-link, not byte-minimal**: all code shifts +2 bytes, RAM shifts +1 byte
  (`_MODBUS_ADDRESS` spills COMRAM 0x57 → BANK0 0xE5), program 6358→6361 bytes, data 295→296.
  ~5.9 KB differ across the image.
- **Functional equivalence:** transparent — every relocated symbol is accessed consistently by the
  re-linked code; no control path (PWM/Modbus/UART/interrupt/safety/watchdog/timing) changes semantics.
- Caveat: the checksum is a whole-region sum (`0x800..0x7FFD`), so ANY re-link changes it. This image
  is checksum-distinct but not near-identical. (Accepted: the goal is a distinct image identity with no
  new intended behavior, which this achieves.)

### 1.5 Archive (immutable)
- Branch `feature/pico-5b-diagnostic`, commit `bea4251`: `release/diagnostic-pico-5b/`
  (HEX + `.map`/`.sym` + provenance README). No baseline artifact overwritten
  (`release/baseline-0/`, `release/legacy-l0/`, `release/legacy-l1/` untouched).

## 2. Flash sequence (source-reconstructed)

### 2.1 Command IDs (`TrackController5/firmware/src/enums.h`)
103 `EXEC_MBUS_STATE_SLAVE_FW_FLASH` · 120 `RECEIVE_FW_FILE` · 121 `RECEIVE_CONFIG_WORD` ·
122 `FLASH_ALL_SLAVES` · 123 `SELECT_SLAVE` · 124 `GET_BOOTLOADER_VERSION` · 125 `ERASE_FLASH` ·
126 `WRITE_FLASH` · 127 `WRITE_CONFIG` · 128 `CHECK_CHECKSUM` · 129 `SLAVE_RESET` · 130 `EXIT`.
ACKs: 202/212/222/232/242 = `*_RETURNED_OK` (task_state DONE); 203/213/223/233/243 = NOK (ERROR).

### 2.2 Single-target sequence (amp 3) — the ONLY path to use
```
250 CLIENT_CONNECTION_REQUEST  → CONTROLLER/CONNECTED/DONE
103 SLAVE_FW_FLASH             → FWHANDLERINIT/CONNECTED
120 RECEIVE_FW_FILE            → FWFILEDOWNLOAD/140 STANDBY
141 FW_DATA ×480               → per-packet ack, then 143 + RECEIVED_CHECKSUM_OK, FWHANDLER/120/DONE
121 RECEIVE_CONFIG_WORD        → FWCONFIGWORDDOWNLOAD/150
151 config (12 B)              → 152 + FWHANDLER/121/DONE
123 SELECT_SLAVE(id=3)         → NO ack (bounded delay)   [state-changing: forces target into bootloader]
125 ERASE_FLASH                → 202  [FIRST DESTRUCTIVE]
126 WRITE_FLASH                → 212
127 WRITE_CONFIG               → 222
128 CHECK_CHECKSUM             → 232
129 SLAVE_RESET                → 242
130 EXIT                       → NO ack (bounded delay)
(100/106/101 read-only verify, never 105)
```
Never emit 122 / 131 / 132.

### 2.3 Safe-abort region and last safe abort point
- **First destructive point:** command **125** (`CMD_ERASE_FLASH` 0x03 over the PIC32→PIC18 8-bit UART).
  Everything before 125 (250/103/120/141×480/121/151/123) is non-destructive (flash intact; 123 resets
  the target into bootloader but does not erase).
- **Last safe abort point:** after receiving `ERASE_FLASH_RETURNED_OK` (202), **before** sending 126
  (WRITE). Aborting mid-126 leaves a partially-programmed image — `flash_one.py` aborts on any
  `*_RETURNED_NOK`/timeout and never auto-resets/retries.

### 2.4 Target selection + isolation
- `SelectSlave(3)` → backplane 51, `HOLDINGREG0 = SLOT1<<2 = 0x04` (single bit). The backplane decodes
  HOLDINGREG0 bit-by-bit to exactly one active-low `AMP_IDx_SET_LAT` line (production decode `0x04→AMP_ID3`).
- Bootloader entry is keyed to the selected slot only (the amplifier whose `ID_PORT`/RA0 is pulled low
  answers the `SLAVE_INITIAL_ADDR 0xAA` / `HR11=0` write and enters bootloader).
- Protocol separation: running app amplifiers are 9-bit address-detect (RC1STA 0xD8) and ignore the 8-bit
  bootloader frames (RC1STA 0x90); only the bootloader-mode amplifier acts on 125–129.

### 2.5 Residual risk — two preconditions (must be verified at the physical session)
1. **Backplane firmware variant:** production `main.c` decodes `0x04→AMP_ID3` (10-slot); the prototype
   `main_proto_backplane.c` decodes `0x04→AMP_ID7` (6-slot). The test oval (amps 1/3/4/6) is documented
   as the prototype backplane, so "slave 3 == slot 3" must be confirmed on the bench (or the physical
   slot identified) before flashing.
2. **No amplifier stuck in bootloader:** the 8-bit bootloader bus is a shared, unaddressed party-line.
   A second amplifier already in bootloader would also receive/act on ERASE/WRITE. The single-target path
   omits the 124 probe; a read-only `GET_BOOTLOADER_VERSION` (124) or `SLAVE_DETECT` readback must
   confirm no stuck amplifier **before** 123/125.

### 2.6 RECEIVED_WRONG_COMMAND (host→PIC32, not the PIC18 bus)
Triggered when the PIC32 reads a non-141 frame from its **Ethernet receive mailbox** during
`FwFileDownload` case 1 (`slavefwhandler.c:557-565`). It is a HOST→PIC32 UDP/file-transfer fault, distinct
from the PIC32→PIC18 bootloader bus (which has its own error vocabulary 180/181/193/201/211/221/231/241).
The L2 Stage-A `flash_one.py` mitigates it via stale-mailbox drain, strict ack gating, and bounded
no-ack delays; a stale frame already inside the PIC32 mailbox can only be cleared by a power-cycle.

## 3. Instrumentation (Profile B)

### 3.1 Signal / test-point map (INSPECTED, must be re-confirmed on the bench)
| Signal | Point | Net/pin | Level | Hazard |
|---|---|---|---|---|
| PIC32 U2TX | piggyback ST485 U3 DI pin 4 | `Tx` → RPC2 | 3.3 V | — |
| PIC32 U2RX | piggyback ST485 U2 RO pin 1 | `Rx` → RPC3 | up to 5 V (ST485 VCC) | verify RO high level |
| Amp MCLR | PIC18F25K40 pin 1 | `~MCLR` | 5 V idle | 12 V VPP during HVP ICSP — do not probe then |
| AMP_ID select | backplane PORTC/RA (see note) | `AMP_ID3=RC6` (prod) / `AMP_ID7=RA3` (proto) | 5 V active-low | confirm backplane variant |
| Ground | shared backplane 0 V / GND | `GND` | 0 V | not the H-bridge return |

- **MCLR is a software-reset observation only**: the flash path uses the PIC18 `RESET()` instruction
  (not an external MCLR pulse), so MCLR stays high through 123→130; it is not a usable trigger (flat-high
  is the negative proof that reset is software). Its 12 V ICSP exposure makes it a "do not probe during
  programming" point.
- **AMP_ID discrepancy (must resolve):** production `0x04→AMP_ID3=RC6 (PORTC)`; prototype `0x04→AMP_ID7=RA3
  (PORTA)`. Confirm which backplane firmware is loaded before wiring the select-line trigger.

### 3.2 Acquisition configuration (all captures)
- Sample interval **16 ns** (tb4); digital threshold **1500 mV** port 0; baud **2 016 000** (same for app
  and bootloader — only the frame width changes 9→8 bit).
- Digital edge trigger available (PICO-4T): one source D0..D15, rising/falling/either, `trigger_wait_ms`
  default 5000 (max 60000). Channel mapping: D0=RX, D1=TX, D2=AMP_ID select, D3=MCLR (optional).

### 3.3 Per-event captures (bounded; no single all-flash capture)
- **B1 — slot select + bootloader entry (123):** trigger D2 falling (select asserts); pre ~1.6 ms, post ~100 ms;
  decode D1 9-bit (ninth=1 address marker).
- **B2 — 9→8-bit transition + bootloader exchange:** trigger D1 falling (first 8-bit frame); pre ~1.6 ms,
  post ~50 ms; decode D1 8-bit (`0x55` start byte = verified marker). The transition repeats per command
  125–129, and WRITE (126) streams 480 blocks, so 8-bit activity is ample.
- **B3 (optional) — RESET (129) + software-reset proof:** trigger D1 falling near 129; D3 (MCLR) flat-high
  through the ack proves a software reset.

### 3.4 Decode plan
Reuse the PICO-4 decoder (`tools/picoscope/analysis/uart.py`, MCP `scope_analyze_capture`): decode D1 in
9-bit mode for app/entry, in 8-bit mode for bootloader segments, with explicit mode markers (ninth-bit
address marking vs `0x55` prefix). Never silently guess a mode.

### 3.5 Limitations
- No cross-capture sub-microsecond timing (each capture has its own clock + trigger; inter-capture gaps are
  not measured). No single all-flash block capture (16 ns ⇒ ~2 s max block; WRITE alone spans seconds).
- 16 ns is sample-resolution limited (±16 ns/edge), not calibrated.
- `tools/picoscope/profiles/siebwalde.py` Profile B still lists historical D6/D7 + D0/D1 assignments — stale
  vs PICO-4T (D1=TX, D0=RX); correct to D0/D1/D2/D3 before any run.

### 3.6 Host / PicoScope correlation
Three independent time bases: host monotonic, scope sample-relative, scope trigger-relative. Correlate by
**command identity + order**, not timestamps: the host log proves what was commanded; the capture proves what
appeared on the UART. Without a shared hardware reference there is no sub-millisecond cross-device accuracy
claim. If the host file upload fails before bootloader entry, the report must state no bootloader transition
was demonstrated.

## 4. Python UDP diagnostic logging (implemented)
`Siebwalde-legacy-restoration/TrackController5/Python/flash_one.py`, commit `401291f`:
G1 monotonic timestamp per line, G2 expected-vs-actual ack on discard, G3 raw TX/RX hex,
G4 per-run correlation id. No sequencing/gating/control-flow change; 26 tests OK.

## 5. Safety
- Single-target only (amp 3); never command 122/131/132; never L2 `0x195A`.
- Operator physically present; Siebwalde/Koploper stopped; locomotive motion physically prevented; outputs safe.
- Correct PIC32 L1 firmware (HEX `EC7E3E6E…`) confirmed; exact temporary HEX (`0x2D00`) + rollback `0x251F`
  (`B848EF04…`) identified.
- Scope armed before any destructive command; UDP logging active; source-defined sequence reviewed.
- **Stop conditions:** any `*_RETURNED_NOK`/timeout → stop (no auto-reset/retry). If upload fails before 125,
  no flash modification occurred. If a partially-programmed state is reached, preserve evidence and stop at
  the safest verified state (recovery only under a separately approved procedure).
- Restoration is a SEPARATE later plan using the proven `0x251F` image; it is NOT authorized by this prep.

## 6. Verification
- Offline PicoScope regression: **278 passed** (PICO-4T baseline; unchanged by this prep).
- `flash_one.py` logging tests: **26 OK**.
- Temporary firmware: checksum `0x251F → 0x2D00`; baseline rebuild byte-identical (`B848EF04…`); source restored.

## 7. Git branches / commits
- `feature/pico-5b-diagnostic` @ `bea4251` (temporary HEX archive).
- `feature/legacy-l2-stage-a-driver` @ `401291f` (flash_one.py logging).
- (Main repo PICO-4T unchanged @ `80694e9`; no master merge.)

## 8. Final decision

**CONDITIONAL GO — FOR SUPERVISED PROFILE B FLASH VALIDATION.**

The preparation is complete: the checksum-distinct temporary image exists (functionally equivalent), the
single-target command sequence is source-correct with a documented safe-abort region, the instrumentation
plan is credible, and the host logging is in place. **Execution is NOT authorized here** — before the
supervised physical test the operator must (i) confirm the backplane firmware variant and the physical
amp-3 slot, and (ii) confirm (read-only 124 probe / SLAVE_DETECT) that no amplifier is stuck in bootloader.
If either cannot be confirmed, this becomes NO-GO for the single-target flash. Readiness is not permission
to execute.
