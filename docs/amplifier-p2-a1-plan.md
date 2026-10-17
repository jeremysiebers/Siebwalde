# Amplifier P2-A1 — Current-Chain Characterization Plan (proposal, not executed)

**Status:** Proposal / handoff. **NOT** P2-A0 execution. **No** protection thresholds, no fault insertion, no trip limits are set here.

**Predecessor finding (P2-A0):** the deployed PIC18 firmware is the pre-Phase-1 `928ea7c` build (HR11 checksum `0x251F`), so the current-sense chain semantics below are those of the legacy build, not Phase-1. Phase-1 (21-register) firmware is **not deployed**. Any P2-A1 measurement must be attributed to the actually-running image.

## 1. Objective

Characterize the amplifier-1 current-sensing path end-to-end (physical current → LMD18200 CSEN → CSEN_FLT filter → PIC18 ADC → raw `HR18`) and establish an **independent calibrated current reference**, so that later Phase-2 work can reason about current *values* instead of raw ADC counts. Establish *candidate* limits only after evidence, never from a fuse/LMD rating/ADC voltage alone.

## 2. Known analog chain (from schematic/source — verify on the actual board revision)

**IMPORTANT (P2-A0 finding):** the deployed PIC18 firmware is the **pre-Phase-1 `928ea7c` (12-register map HR0–HR11)**, so **HR18 is NOT available** on the running hardware. The Phase-1 firmware (which publishes raw ADC as HR18) is **not deployed**. The current-sense HARDWARE chain (CSEN → RB0 → ADC) exists, but its register exposure on the legacy build differs from Phase-1.

| Stage | Path | Notes (source/schematic, to verify) |
|---|---|---|
| Physical current | motor/track load → H-bridge | PWM conduction + freewheel intervals differ; instantaneous current ≠ filtered ADC |
| LMD18200 sense | pin 8 CSEN → R24 3.3 kΩ → GND | nominal 377 µA/A → ~1.244 V/A (nominal, not calibrated) |
| Filter | R25 4.99 kΩ / C29 100 nF → RB0 | nominal τ ≈ 0.499 ms; PWM freewheel current not fully captured |
| ADC | `ADREF=0x00` (ADPREF=VDD, ADNREF=VSS) | MCP1525 2.5 V external VREF exists on board but is NOT the selected ADC reference in inspected source |
| Register (legacy deployed) | raw ADC → legacy HR (NOT HR18) | legacy 12-register map; exact legacy current register must be identified |
| Register (Phase-1, NOT deployed) | `amp_model.current` = `ADCC_GetConversionResult()` → HR18 | requires Phase-1 firmware or reviewed temporary instrumentation |

## 2b. Legacy vs Phase-1 measurement split

- **Possible on legacy firmware:** CSEN voltage (CH1/DMM), CSEN_FLT (RB0) waveform, ADC input node voltage, raw ADC via the legacy register (if a legacy current/status register exists — to identify), occupancy, temperature, PWM.
- **Requires Phase-1 firmware OR reviewed temporary instrumentation:** HR18 (raw current ADC), the 21-register map, identity/ramp/watchdog/applied-PWM semantics.
- The independent-current-reference requirement is unchanged and applies to whichever firmware is measured.

**Open facts to establish on hardware:** actual board revision + population; actual VDD; ADC resolution/alignment; whether the deployed build uses VDD or an external reference; sense blind spots during PWM freewheel; the legacy register that carries the current-sense value (if any).

## 3. Independent current reference (required equipment — PO to confirm availability)

The earlier investigation did **not** establish what calibrated current measurement is available. P2-A1 requires, at minimum, ONE of:

- a calibrated **AC/DC current probe** (e.g. Hall clamp) on the motor/rail lead; or
- a known **shunt** with a differential voltage measurement (scope CH1+CH2 differential, or a DMM in series at low frequency); or
- a fixed known **resistive load fixture** with a measured applied voltage (so current = V/R) for static DC operating points.

Without an independent reference, raw CSEN/HR18 cannot be converted to mA. **This is the primary unblock for P2-A1.**

## 4. Measurement matrix (safe normal-load points only; PO performs physical installs)

| ID | Question | Setup | Evidence condition |
|---|---|---|---|
| A1-S01 | VDD and ADC reference actually used | measure VDD at the PIC18 (and the MCP1525 2.5 V node) with DMM/scope | logged VDD + confirmed ADREF |
| A1-S02 | CSEN→RB0 static gain (DC) | fixed resistive load, known applied voltage/current (I = V/R) | measure CSEN voltage vs reference current at ≥3 DC points (0 A, mid, near full) |
| A1-S03 | HR18 vs current (DC) | same fixed load; read HR18 via the running host read-only | HR18(ADC) vs reference current correlation + quantization step |
| A1-S04 | Dynamic response / RC lag | step current (bounded, normal-load) while sampling CSEN_FLT + PT marker + HR18 timing | filter τ, lag, settling; ADC sample timing vs PT1 marker |
| A1-S05 | PWM conduction vs freewheel | normal running load; correlate CH1 PWM edges vs CSEN/current | freewheel current blind spot quantified |
| A1-S06 | Saturation / headroom | measure where CSEN/ADC saturates vs current | saturation point vs the nominal 5 V / VDD headroom |

Each DC point: record reference current, CSEN voltage, HR18, VDD, and temperature (MCP9701A), with stable supply/load.

## 5. Correlation targets (what must be reconciled)

Physical reference current ↔ LMD CSEN ↔ CSEN_FLT (filtered) ↔ ADC sample (PT1 marker timing) ↔ `amp_model.current` ↔ published **HR18** — while accounting for PWM conduction/freewheel, bandwidth, saturation, aliasing and RC lag. Document offset/gain, accuracy, edge response, quantization, and blind spots per operating condition.

## 6. Later, separately-agreed fault sessions (NOT in P2-A1 baseline)

- Detection-to-physical-output shutdown latency; comm-loss / PIC reset / startup-restart.
- Controlled high-resistance/long-wire and low-resistance/short-wire faults **inserted by PO** only, with bounded energy/time and pre-approved recovery.

## 7. Threshold discussion — deferred

Candidate thresholds (~2.0–2.5 A) remain **hypotheses**. After P2-A1 evidence: discuss threshold, hysteresis, persistence/filtering, max fault energy, latching/reset, degraded measurement, and independent hardware thermal limits. **No accepted limit may derive from a fuse, LMD max rating, or arbitrary ADC voltage.**

## 8. Required PO/physical actions to start P2-A1

1. Confirm which **independent current reference** is available (current probe / shunt+DMM / known resistive fixture).
2. Provide a safe **fixed load** (or authorize a specific normal-load point) for static DC characterization.
3. Physical installs/changes are PO work (probe placement, series shunt, load fixture).

## 9. Out of scope (do not start automatically)

Full Phase-2 protection implementation; production trip thresholds; unsanctioned shorts/stress; speculative schematic redesign; firmware flashing.
