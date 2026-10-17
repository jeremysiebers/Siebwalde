# Amplifier Phase-1 Hardware Bring-Up — Proposal (planning only, NOT executed)

**Status:** Proposal. **No** flash, **no** deployment in P2-A0. This is the separately-scoped bring-up increment to be authorized and executed later.

**P2-A0 finding this builds on:** the running stack is pre-Phase-1 — PIC18 `0x251F` (`928ea7c`), PIC32 dist `A90E44…` (Jan-2026), C# host `fe17ebe` (KIS-09). Phase-1 firmware is merged in source but never deployed.

## 1. Exact Phase-1 artifact identification (built from merged `e602b08`)

| Target | Artifact | SHA-256 | Size |
|---|---|---|---|
| PIC18 (Offset) | `build/firmware/out/e602b08/TrackAmplifier4/Offset/TrackAmplifier4.X.production.hex` | `BEF26E7136978A980A831388A3AC6D620EE5D390E44B8FE343740B98F3447436` | 86529 B |
| PIC32 (Production) | `build/firmware/out/e602b08/TrackController5/Production/TrackController5.X.production.hex` | `FFA5922BEC3AC3E0399A2117879F12A1F4ADAB38A6BD7B10C91610A994CEFA30` | 803630 B |
| C# host | current `master` build (62363c2) | build at deploy time | — |

Reconcile each artifact to the merged source before flashing; a filename/commit is not proof of content — re-verify the SHA-256 against a fresh reproducible build if any doubt.

## 2. Standard bootloader/flash route

- Use the existing **PIC18 Offset build → download/bootloader/flash** path (the `TrackAmplifier4.X` bootloader, address `0xAA` startup handshake, HR11=0 entry). Direct programmer is a fallback, not the default.
- PIC32 via its existing Harmony/Production route; C# host by rebuilding/running the current `master` app.

## 3. Compatibility with running PIC32/C# host

- **The three layers must be brought up together.** The Phase-1 PIC18 (21-register map) is incompatible with the running pre-Phase-1 PIC32/C# (12-register map). Deploying only the PIC18 would break the link.
- The Phase-1 C# host (62363c2) already contains the two-fresh-read HR7 identity fail-closed + config download/verify + lease/stop-confirmation, so it will detect and reject a legacy PIC18 (and vice-versa). Verify this fail-closed behavior is the expected mixed-version guard, not a defect.

## 4. Backup and recovery

- Record the currently-running pre-Phase-1 identity (PIC18 `0x251F`, PIC32 dist `A90E44…`) as the **rollback baseline**.
- Keep the on-disk `dist\Offset` (0x251F) and `dist\Production` (A90E44) HEX untouched as rollback artifacts.

## 5. Test target selection

- Amp-1 first (already scope-mapped: CH1 output, D1 MCLR, D2 PT2, D3 PT1, D6/D7 master RX/TX). Confirm address assignment and isolate other amplifiers before flashing.

## 6. Initial neutral-output verification

- After boot, command neutral `399` and verify observed applied-PWM == 399 before any non-neutral movement. Confirm no traction before INIT_DONE∧CONFIG_OK∧ENABLE.

## 7. Communication and hardware CRC verification

- Verify the real 9-bit UART + **bit-reversed CRC** round-trip on hardware (the offline-verified CRC convention must be observed at runtime). Check HR7 identity two-fresh-read + HR11 checksum readback.

## 8. PWM peripheral observation

- Observe the PWM peripheral on the physical pins/output (scope) — Phase-1 duty 0..799/neutral 399, ~20 kHz (TMR2). Confirm the applied-PWM readback matches the physical output.

## 9. Watchdog timing

- `WATCHDOG_MS=250` remains a **placeholder**; do not claim a protected limit. Measure the actual comm-watchdog + lease timings on hardware (the full-complement budget R5‑C remains a separate gate).

## 10. Runtime register-map verification

- Verify the 21-register map (HR0–HR20) and HR1 per-bit semantics (ENABLE/DISABLE/MOTION_PERMISSION set-only, APPLY/CLEAR_FAULT/RESET_COUNTERS edge 0→1) against the frozen §R8‑4 on hardware.

## 11. Rollback procedure

- Re-flash the recorded rollback baseline (`0x251F` PIC18 / `A90E44…` PIC32) via the same bootloader path if bring-up fails. Verify link restoration after rollback.

## 12. Independent hardware acceptance criteria

- Neutral-at-boot; no traction before init; identity + CRC verified; PWM observed matching command; watchdog/lease behavior observed; register map verified; independent Integrator review of the physical bring-up evidence; no protection thresholds set.

## Authority required (NOT held in P2-A0)

- `LIVE_HARDWARE` + `FIRMWARE_FLASH` (and later `MERGE` if any code change is needed). This proposal does **not** authorize deployment.
