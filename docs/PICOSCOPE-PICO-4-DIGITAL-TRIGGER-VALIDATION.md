# PICOSCOPE PICO-4T — Digital Trigger Implementation and Validation

Date: 2026-10-23. Minimal native DIGITAL EDGE TRIGGERING in the PicoScope ps3000a
adapter, replacing the previous silent `PS3000A_CHANNEL_EXTERNAL` fallback, plus
one operator-approved physical validation on the 3406D MSO. Measurement-only; no
firmware/flash/reset/motion/AWG, no C#/Koploper change, no master merge.

## 1. Verdict

**PASS.** A real D1 falling edge (a UART start bit on the PIC32 TX line) fired the
digital trigger and was positioned exactly at the pre/post boundary (sample 62500).
Three complete FC03 read transactions were decoded from the triggered capture, all
CRC-valid under the Siebwalde bit-reversed Modbus convention.

## 2. Git branch + commit

- Branch: `feature/picoscope-pico-4t` (branched from `feature/picoscope-pico-4` @ `a639694`).
- Commits:
  - `ee32e47` — native digital edge triggering + trigger-state/position metadata + tests.
  - `b7ee4a3` — fix: don't let a zero trigger offset overwrite `pre` in `_trigger_sample_index`.

## 3. Implementation changes (exact)

`tools/picoscope/backend/adapters/picosdk.py`:
- Added `PS3000A_DIGITAL_CHANNEL_DIRECTIONS {channel:int32, direction:int32}` and
  `PS3000A_TRIGGER_CONDITIONS_V2 {channelA..pulseWidthQualifier, digital:uint32}`
  ctypes structures (`_pack_ = 1`).
- Added `DIGITAL_EDGE_DIRECTION = {rising:3, falling:4, either:5}` (the DIGITAL
  direction enum, distinct from the analog threshold direction 2/3/4).
- `configure_trigger` now routes D0..D15 sources to `_set_digital_trigger` (NOT
  `ps3000aSetSimpleTrigger`, NOT EXTERNAL); A..D/EXTERNAL still use `_set_simple_trigger`.
- `_set_digital_trigger`: verifies the target port is enabled (else `InvalidConfigError`,
  never a fallback), then calls the two digital-trigger SDK functions.
- `capture_block` records `trigger_status` (`triggered`/`timeout`/`unused`),
  `trigger_position` (`pre`) and `trigger_index`; `trigger_wait_ms` is now threaded
  through (replacing the hard-coded 5.0 s) and `_wait_ready` returns `False` on
  timeout instead of raising.

`tools/picoscope/backend/model.py` + `serialize.py`: added `trigger_status` and
`trigger_index` to `Capture`, round-tripped by the serializer.

`tools/picoscope/backend/adapters/simulator.py`: deterministic digital-edge-trigger
emulation (rising/falling, D0..D15, both ports).

`tools/picoscope/mcp/service.py`: threads `trigger_wait_ms`; exposes
`trigger_status`/`trigger_position`/`trigger_index`; preserves `_effective_pre_post()`.

## 4. Official SDK API references

Enums (values): digital channel `PS3000A_DIGITAL_CHANNEL_N = N` (D0=0..D15=15);
direction `DONT_CARE=0/LOW=1/HIGH=2/RISING=3/FALLING=4/RISING_OR_FALLING=5`;
trigger condition `DONT_CARE=0/TRUE=1/FALSE=2`.

Functions (PicoSDK ps3000a, driver 2.2.160.7569 / picosdk 1.1):
- `ps3000aSetTriggerDigitalPortProperties(handle, directions*, int16 nDirections)`.
- `ps3000aSetTriggerChannelConditionsV2(handle, conditions*, int16 nConditions)`.
- `ps3000aRunBlock` / `ps3000aIsReady` / `ps3000aGetValues` (unchanged flow).
- `ps3000aGetTriggerTimeOffset64(handle, int64* time, PS3000A_TIME_UNITS* units, uint32 segment)` (refinement only).

There is **no** `ps3000aSetDigitalTrigger` entry point (correctly not invented).

Digital trigger sequence (per official ps3000a MSO block example):
1. digital port enabled via `ps3000aSetDigitalPort`;
2. `SetTriggerDigitalPortProperties([{channel=Dn, direction=RISING|FALLING|RISING_OR_FALLING}], 1)`;
3. `SetTriggerChannelConditionsV2([{all DONT_CARE, digital=CONDITION_TRUE}], 1)`;
4. `RunBlock(pre, post, timebase, ...)` → `IsReady` → `GetValues`.

## 5. Offline test results

Full suite: **278 passed** (161 SDK + 53 analysis + 64 MCP), 0 failed, 0 skipped.
- 21 new SDK digital-trigger tests (mocked SDK: no EXTERNAL fallback, edge mapping,
  disabled-port rejection, error propagation, timeout/triggered/unused states, trigger index).
- 13 new simulator edge-emulation tests.
- 5 new MCP digital-trigger tests (real stdio MCP client).
- 4 regression tests for the `trigger_index` fallback fix.

## 6. MCP compatibility

- All five tools unchanged and working: `scope_info`, `scope_capture_block`,
  `scope_get_capture_metadata`, `scope_export_capture`, `scope_analyze_capture`.
- `scope_capture_block` accepts a digital trigger source D0..D15 (existing
  `TriggerProfile` schema; no schema change). One simulated digital-trigger
  acquisition through the real stdio MCP client succeeds.

## 7. Independent Integrator verdict

**PASS (10/10)** for `ee32e47`: correct official API usage, correct ctypes layout
(structures match picosdk 1.1), correct D0–D15 mapping, no silent fallback, correct
pre/post + trigger-index, timeout/cleanup, resource limits, no regression, MCP
compatibility, no firmware/hardware-control change. The `b7ee4a3` fix is metadata-only
and covered by 4 regression tests.

## 8. Trigger configuration and limits

- Trigger source: any digital line D0..D15 (one source per capture).
- Edges: rising, falling, either.
- Pre/post samples: configurable, validated via `_effective_pre_post()` before any SDK call.
- Bounded trigger wait: `trigger_wait_ms` (default 5000, limit 60000).
- No auto-trigger, no EXTERNAL/analog fallback, no pulse-width/multi-channel logic triggers.

## 9. Real hardware result (operator-approved)

| Field | Value |
|---|---|
| Capture ID | `68575a41f8bbd6530f37e5ab2d187299` |
| Backend / device | `ps3000a` / 3406D MSO, serial `JY908/0278`, driver `2.2.160.7569` |
| Trigger | D1 falling edge |
| Sample interval | 16 ns |
| Pre / post | 62500 (1 ms) / 187500 (3 ms) = 250 000 samples (~4 ms) |
| Threshold | 1500 mV (port 0) |
| `trigger_status` | `triggered` (the D1 falling edge fired) |
| `trigger_position` | 62500 |

## 10. Capture metadata and hashes

- Raw evidence (`tools/picoscope/evidence/mcp/`, preserved unchanged):

| File | Bytes | SHA-256 |
|---|---|---|
| `68575a41f8bbd6530f37e5ab2d187299.npz` | 4165 | `4e9add10f34fc35a59ce070c640909676b7536a0571fb2be49d40f28bad9590e` |
| `68575a41f8bbd6530f37e5ab2d187299.json` | 2397 | `e016ce3144cd7b5c1a0d6900f7a7b005fef6f66559eb18668f2399dd4f649a0f` |

- Provenance: `evidence_kind=real`, `hardware_validated=true`, `backend=ps3000a`.
- Device released after capture (`ownership_after=available`); gate closed.

## 11. Trigger-position validation

The first D1 falling edge is at **sample 62500** (delta 0 vs `pre`), and the signal
transitions idle→start (1→0) exactly there. The first decoded 9-bit frame (address
`0x04`, ninth=1) begins at 1 000 000 ns = 62500 × 16 ns. The 1 ms of pre-trigger data
contains the tail of the previous transaction (a reply tail cut by the trigger boundary,
correctly reported with `stop_ok=False` — a stop-bit error, not the `truncated` flag).

Decoded traffic (all CRC-valid, Siebwalde bit-reversed convention):

| Direction | Frames | Content |
|---|---|---|
| TX (D1, 9-bit) | 24 | `04 03 00 08 00 02 39 A2` · `06 03 00 08 00 02 7E 22` · `01 03 00 00 00 02 D0 23` |
| RX (D0, 8-bit) | 33 (32 good + 1 stop-bit error from the pre-trigger reply tail) | `04 03 04 80 D6 00 00 D0 E6` · `06 03 04 80 E3 00 00 A3 2A` · `01 03 04 01 8F 80 00 27 D5` |

Ninth-bit address marking proven: `[1,0,0,0,0,0,0,0]` per transaction (ninth=1 on the
address byte, ninth=0 on data). The first transaction is the known-good HMO2024 frame
(`04 03 00 08 00 02` → CRC `39 A2`).

## 12. Known limitations

- **`trigger_index` bug (fixed):** the stored capture JSON records `trigger_index=0`
  because `ps3000aGetTriggerTimeOffset64` returned 0 for this digital block trigger.
  For a ps3000a block capture the trigger is at `pre` by definition; commit `b7ee4a3`
  now guards the refinement so a 0/sub-sample offset falls back to `pre` (= 62500).
  The correct trigger index for this capture is 62500 (confirmed by the raw data).
- The analog/EXTERNAL trigger path is unchanged (`ps3000aSetSimpleTrigger`).
- Only ONE digital trigger source per capture is supported (no multi-channel/pulse-width logic).
- The `provenance.mcp_layer` label is still hard-coded `"pico-3"` (cosmetic).
