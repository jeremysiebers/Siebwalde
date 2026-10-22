# PICOSCOPE PICO-4 — First Physical UART Validation

Date: 2026-10-22. First physical validation of the PICO-4 UART decoder against the
real working Siebwalde PIC32/PIC18 communication, using the PicoScope 3406D MSO.
Measurement-only; no firmware/flash/reset/motion, no simulator fallback, no C# change.

## 1. Verdict

**PASS.** The real PIC32 9-bit address-marked master request and the real PIC18
8-bit reply were captured and decoded correctly. Three complete Modbus RTU FC03
read transactions (request + response) were reconstructed and are **CRC-valid**
under the Siebwalde bit-reversed Modbus convention. The ninth-bit address marking
(ninth=1 on the address byte, ninth=0 on data) is proven by capture.

## 2. Git branch + commit

- Branch: `feature/picoscope-pico-4` (offline implementation commit `c338a5c`).
- This physical-validation work adds: the capture script, the decode script,
  the evidence result files, and this document.

## 3. Hardware setup

- **Device:** PicoScope 3406D MSO, serial `JY908/0278`, variant `3406DMSO`,
  driver `2.2.160.7569`, backend `ps3000a`.
- **System under test:** PIC32 L1 `0.1.0` master (HEX `EC7E3E6E…`) polling four
  PIC18 amplifiers (`0x251F`), in normal safe stationary operation (no motion).

### 3.1 Test points (PROVEN from firmware source)

| Signal | PIC32 pin | Role | Evidence |
|---|---|---|---|
| UART2 TX | **RPC2** (U2TX) | 9-bit master request, logic side before the RS-485 driver DI | `TrackController5/firmware/src/system_config/Production/framework/system/ports/src/sys_ports_static.c:183` |
| UART2 RX | **RPC3** (U2RX) | 8-bit PIC18 reply, logic side after the RS-485 receiver RO | `sys_ports_static.c:177` |

- RS-485 transceiver: **ST485EBDR** (half-duplex, 5 V VCC). The differential A/B
  lines were **NOT** probed; only the single-ended logic-side signals.
- Logic levels: TX ≈ 3.3 V (PIC32MZ2048EFH144 VDD). RX ≈ 3.3 V or 5 V (receiver
  VCC dependent) — verified by the operator during the preflight checklist.

### 3.2 Actual probe connections (determined from the capture data)

The physical probes landed **swapped** relative to the proposed D0=TX/D1=RX
mapping. The decoder determined the true direction from the data itself:

- **D1 = PIC32 UART TX** (9-bit master request, RPC2).
- **D0 = PIC32 UART RX** (8-bit PIC18 reply, RPC3).

This resolves the long-standing HMO2024 channel-direction ambiguity (one
historical doc claimed D6=TX/D7=RX, another the reverse). The decoder's
direction determination is based on the frame-length signature: the 9-bit master
line **fails** an 8-bit decode (21/24 frames in error) and the 8-bit reply line
**passes** an 8-bit decode (0/27 in error).

## 4. Acquisition configuration

| Parameter | Value |
|---|---|
| Backend | `ps3000a` (real) |
| Digital port | 0 (D0..D7) |
| Logic threshold | 1500 mV |
| Trigger | **none** (triggerless) |
| Sample interval | **16 ns** (timebase 4, returned by `ps3000aGetTimebase2`) |
| Sample count | 250 000 (~4 ms) |
| Ownership after | `available` (released) |

**Triggerless rationale:** the `ps3000a` digital-line trigger is NOT implemented
in the adapter (`picosdk.py` maps a digital trigger source to EXTERNAL, which
would never fire). A bounded triggerless window was used instead. The master
cyclic polling is ~1.20 ms/transaction (comm-cycle timer rate 832 Hz -> 1/832 s,
`mbus.c:245`), so a ~4 ms window reliably contains several transactions.

## 5. Capture evidence

- **Capture ID:** `aabc7f9168855cae367864b54895c9f3`.
- **Provenance:** `evidence_kind=real`, `hardware_validated=true`, `backend=ps3000a`.
- Artifacts (raw evidence, preserved unchanged):

| File | Bytes | SHA-256 |
|---|---|---|
| `tools/picoscope/evidence/mcp/aabc7f9168855cae367864b54895c9f3.npz` | 4120 | `7bbc8d4cae54e73a7fcca34307a20b1b5cea8571415c1efe5d3c758002abf70d` |
| `tools/picoscope/evidence/mcp/aabc7f9168855cae367864b54895c9f3.json` | 2301 | `72b79aefc0f50ba2c9130eba28a330d91dffa6f4fd6444abf5722be70abc4be6` |

Decode result: `tools/picoscope/evidence/pico-4/pico4_uart_decode_result.json`.

## 6. Decoded traffic (all CRC-valid, Siebwalde bit-reversed convention)

### 6.1 Master TX (D1, 9-bit, address-marked)

Three requests, each 8 bytes (1 address + 1 function + 2 reg + 2 count + 2 CRC):

| # | Address (ninth=1) | Payload | CRC | CRC-valid |
|---|---|---|---|---|
| 1 | `01` | `01 03 00 02 00 02` | `D3 A6` | yes |
| 2 | `03` | `03 03 00 02 00 02` | `94 26` | yes |
| 3 | `04` | `04 03 00 02 00 02` | `79 A6` | yes |

Ninth-bit pattern (proven by capture): ninth=1 on the address byte only, ninth=0
on the following 7 data bytes of every transaction.

### 6.2 PIC18 reply (D0, 8-bit)

Three responses, each 9 bytes (1 address + 1 function + 1 byte-count + 4 data + 2 CRC):

| # | Address | Payload | CRC | CRC-valid |
|---|---|---|---|---|
| 1 | `01` | `01 03 04 2A 02 00 00` | `D7 CA` | yes |
| 2 | `03` | `03 03 04 2E 04 00 00` | `58 89` | yes |
| 3 | `04` | `04 03 04 2A 07 00 00` | `57 68` | yes |

The reply is 8-bit (10 bits/frame) with no ninth bit — consistent with the
legacy PIC18 `TX9=0` behavior.

### 6.3 Response timing

| Request addr | Response delay (request start -> response start) |
|---|---|
| `01` | 171.9 µs |
| `03` | 173.5 µs |
| `04` | 171.3 µs |

Consistent ~172 µs response delay across the three transactions.

## 7. Baud / timing measurements

- Nominal baud (historically observed): **2.016 Mbaud** (~496 ns/bit).
- Decode at 2 016 000 baud: **0 frame errors** across all 51 frames (24 TX + 27 RX).
- Measured 1-bit period (mode of the inter-edge cluster, 16 ns grid): 30–32
  samples/bit -> ~1.95–2.08 Mbaud, i.e. the nominal baud **within the ±1 sample
  (±3.2 %) quantization** of the 16 ns sample interval.
- Start/stop-bit integrity: all 51 frames `stop_ok=True`, 0 framing errors, 0
  truncated frames.

## 8. Source vs hardware comparison

| Fact | Classification |
|---|---|
| RPC2=U2TX (9-bit master TX), RPC3=U2RX (8-bit reply in) | PROVEN BY SOURCE |
| 9-bit address-marked request (ninth=1 address, ninth=0 data) | **PROVEN BY CAPTURE** |
| PIC18 reply TX9=0 (8-bit, no ninth bit) | **PROVEN BY CAPTURE** |
| Baud 2.016 Mbaud (~496 ns/bit) | PROVEN BY CAPTURE (within quantization) |
| Modbus RTU FC03 read transactions + bit-reversed CRC | **PROVEN BY CAPTURE** |
| Master cyclic poll period ~1.20 ms (comm-cycle rate 832 Hz -> 1/832 s) | INFERRED (firmware) + PROVEN BY CAPTURE (transaction spacing 1.11–1.20 ms) |
| Response delay ~172 µs | PROVEN BY CAPTURE |
| Absolute physical bit position (which wire = D0) | NOT VERIFIED (no known pattern) |

## 9. Acceptance criteria (directive §12)

| # | Criterion | Result |
|---|---|---|
| 1 | Raw capture confirmed real 3406D MSO | PASS (`evidence_kind=real`, serial `JY908/0278`) |
| 2 | Intended PIC32 TX/RX signals verified | PASS (both decoded) |
| 3 | Actual digital transitions present | PASS (D0: 120 edges, D1: 104 edges) |
| 4 | Sample interval + channel mapping correct | PASS (16 ns, D0/D1) |
| 5 | >=1 complete transaction reconstructed + correlated | PASS (3 transactions) |
| 6 | Ninth-bit treatment supported by evidence | PASS (address marking proven) |
| 7 | Framing/timing diagnostics consistent | PASS (0 errors) |
| 8 | Raw evidence preserved unchanged | PASS (SHA-256 recorded) |
| 9 | Analysis reproducible offline | PASS (deterministic decode script) |
| 10 | Real hardware access disabled after | PASS (ownership `available`, gate closed) |

## 10. Test execution results

- Full PICO-4 offline regression (before this session): **235 passed**.
- Capture script simulator dry-run: PASS (triggerless digital capture path).
- Real capture: PASS (16 ns, 250 000 samples, triggerless).
- Offline decode: PASS (51 frames, 0 errors, 6/6 CRC-valid transactions).

## 11. Remaining uncertainties / limitations

- **Digital trigger not implemented** (`picosdk.py` maps D0 trigger -> EXTERNAL).
  The capture used a bounded triggerless window. A future hardware session could
  implement `ps3000aSetDigitalTrigger` (or MSO digital trigger) and validate it,
  enabling start-edge-triggered captures.
- **Probe swap:** D0/D1 landed swapped vs the proposed mapping; both signals were
  captured correctly, so no re-capture is needed.
- **Absolute physical bit position** (which wire is physical D0) is still not
  verified against a known pattern.
- **D3–D7, D8–D15** remain unconnected (not part of this 2-signal capture).
- Minor: the capture `provenance.mcp_layer` label is hard-coded `"pico-3"` in
  `service.py:512` (cosmetic only; `evidence_kind`/`backend` are correct).
- Only 3 of the 4 amplifiers (1, 3, 4) were polled within the 4 ms window; the
  amp-6 poll fell just past the window end.

## 12. Independent Integrator verdict

**PASS (9/9).** The Integrator independently re-loaded the raw capture and
re-decoded D0/D1, re-computed the Siebwalde bit-reversed CRC for all 6
transactions, verified the SHA-256 of the raw evidence, and confirmed the
commit scope (5 files, no firmware/C#/.opencode) and the safety boundary
(triggerless, no AWG/flash/reset/motion, gate process-scoped). Direction
resolution, ninth-bit address marking, 16 ns sample interval, ~172 µs response
delay and the ~1.20 ms transaction spacing (1/832 s) were all independently
confirmed. One documentation-only nit (the comm-cycle figure) was corrected.

## 13. GO/NO-GO

**GO for further Siebwalde measurements.** The PICO-4 9-bit + 8-bit UART decoder
is physically validated against real legacy traffic (address marking, 8-bit
reply, baud, CRC, response timing). Bootloader-transition and PWM captures
remain separately authorized (they require different probe points and, for the
bootloader transition, flash/AMP_ID selection — not performed here).
