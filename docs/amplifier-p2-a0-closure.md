# Amplifier P2-A0 — Closure Record

**Status:** PARTIAL closure (baseline delivered; specific criteria deferred). **Not** full P2-A0 acceptance, **not** Phase-1 acceptance, **not** Phase-2 protection validation.

**Baseline identity:** all real-hardware measurements in P2-A0 were taken on the **pre-Phase-1 legacy PIC18 firmware `0x251F` (`928ea7c`)**, with the running C# host built from `fe17ebe` (KIS-09). Phase-1 firmware was **not deployed**.

## Acceptance matrix (final disposition)

| Criterion | Disposition | Note |
|---|---|---|
| A0-S01 instrument/channel map + safety | **PARTIAL** | `*IDN?` + PO wiring proven; digital POD ground **PO-CONFIRMED** (Addendum 001) but independent electrical verification **DEFERRED** |
| A0-S02 deployed identity | **PARTIAL** | PIC18 **VERIFIED** `0x251F`; C# **CORRELATED** `fe17ebe`; PIC32 **DEFERRED** to Phase-1 bring-up (no runtime readback) |
| A0-S03 acquisition coherence | **VALIDATED** | 3 SINGLE/COMPlete captures, hashes match, no rearm |
| A0-S04 PWM normal-operation reference | **VALIDATED** | 19.92 kHz / 42.4 % duty / 11 periods (legacy firmware) |
| A0-S05 digital UART decode + CRC | **VALIDATED** | request 9-bit / response 8-bit; FC03 addr 4; bit-reversed CRC confirmed; decoder 23/23 tests + Integrator PASS. PT2/PT1 pulse timing (D2/D3) aliased at 100 ns → limitation, not resolved |
| A0-S06 firmware processing/latency | **PARTIAL** | legacy semantics only; PT2-toggle ≠ ISR duration; no Phase-1 semantics |
| A0-S07 evidence durability | **PARTIAL** | hashed + independently reviewed; untracked — commit requires publication authority |

## Deferred items (owner / destination / required evidence)

| Item | Owner | Destination | Required evidence |
|---|---|---|---|
| PIC32 running-firmware version | PL + PO | **Phase-1 hardware bring-up** (pre-deployment gate) | a runtime version readback, or coordinated full-stack compatibility proof |
| Independent electrical verification (CH1 GND, pod ground reference) | Integrator + PO | future measurement gate | electrical reference/rating check at session time |
| PT2/PT1 pulse timing / ISR-duration | Developer | higher-resolution SINGLE (100 MSa/s) | a capture resolving the debug-pin pulses |
| Evidence commit/publication | PL + PO | publication (needs authority) | reviewed commit set |

## Explicit non-claims

P2-A0 closure does **not** imply: Phase-1 firmware deployed; Phase-1 hardware acceptance; protection functionality validated; final protection thresholds. The ~−13 V/+29 V overshoot is single-ended inductive ringing (≥10 MHz), not a validated safe/unsafe bound.

## Key artifact hashes (identity + evidence)

- PIC18 deployed: HR11 `0x251F`, dist Offset HEX SHA-256 `B848EF04…`; Phase-1 PIC18 Offset HEX `BEF26E71…`.
- PIC32 dist/Production HEX `A90E4495…`; Phase-1 PIC32 Production HEX `FFA5922B…`.
- C# host ProductVersion `1.0.0+fe17ebedd33f21717d69e03190d5e22a50d6b9e2`.
- REF-AMP01-001 ch1.raw `b5a2d639…`; REF-AMP01-002-LONG ch1.raw `fb7c1740…`; REF-AMP01-003-DIGITAL pod1.raw `a3e8282c…`.
