# Amplifier Phase-1 Hardware Bring-Up — Preflight (revised)

**Status:** Preflight. **No firmware flashing authorized yet.** This documents the historical artifacts, recovery routes, compatibility matrix and migration order so deployment is safe once authorized.

## 1. Historical HEX inventory (verified)

| Target | Artifact | SHA-256 | Git-tracked | Checksum/notes |
|---|---|---|---|---|
| PIC18 legacy (running) | `TrackAmplifier4.X/dist/Offset/production/TrackAmplifier4.X.production.hex` | `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D` | **YES** | HR11 `0x251F`, source `928ea7c`, bootloader-compatible |
| PIC18 Phase-1 (target) | `build/firmware/out/…/TrackAmplifier4.X.production.hex` | `4F9ED4ECD6B925066D67A9F465022FA5F8DF3716F040F209B58190FE1F305A2A` | NO (build output) | HR11 `0xE906`, v1.0.0 |
| PIC32 legacy (running, identity UNKNOWN) | `TrackController5/firmware/TrackController5.X/dist/Production/production/TrackController5.X.production.hex` | `A90E44956A7CA15BFF7439D36FFA9FD0ADC81F8630D0B27A30E31932E3025584` | **NO (untracked, on-disk only)** | Jan-2026 build |
| PIC32 historical (committed, `default` config) | `TrackController5/firmware/TrackController5.X/dist/default/production/…` @ `89dfe79` | `1BF22EE39208973FE90A00386914D6A9B2E52CD6B53A3A0103842176DC902A53` | YES (history) | different config, not the running Production image |
| PIC32 Phase-1 (target) | `build/firmware/out/…/TrackController5.X.production.hex` | `6CEC61D4C28F514D34C22B4537D9D11A2F4ADEA9A7BDB5C2957E3E2674152101` | NO (build output) | v1.0.0, deterministic |

**Recovery gap:** the PIC32 legacy Production HEX (`A90E44…`) is **not Git-tracked** — it exists only on the local disk. This is a rollback-blocker risk: if the local disk is lost, the exact running PIC32 image has no Git backup. The committed `default`-config HEX (`1BF22EE3…`) is a different configuration, not a proven substitute.

## 2. PIC32 ICD3 programmer qualification

- **Hardware:** PIC32MZ2048EFH144; ICD3 physically connected; **no PIC32 application bootloader** (PO-confirmed).
- **Installed toolchain:** MPLAB X IDE **v6.20** (the last ICD3-supporting release; ICD3 support discontinued from 6.25) + v6.05. A "Microchip WinUSB Device" is present (ICD3 candidate).
- **Route:** ICD3 is the intended PIC32 update + recovery method.
- **To verify at deployment (non-destructive, isolated target):** device ID read, flash readback availability, code/read protection, Boot Flash + Configuration-memory handling, erase behavior, power-supply interaction, recovery after interrupted programming.
- **Constraint:** do NOT connect/halt a running PIC32 while the railway is active — require an isolated test condition first.

## 3. Recovery contracts

### 3.1 PIC18 (bootloader route — preferred)
- Path: C# → PIC32 → RS-422 (9-bit) → PIC18 bootloader (`0xAA` handshake, HR11=0 entry) → Offset image.
- Contract preserved: bootloader `0x0000–0x07FF`, application `0x0800–0x7FFF`, checksum coverage `0x0800–0x7FFD`, storage `0x7FFE/0x7FFF`, algorithm = 16-bit additive little-endian word sum.
- Verify legacy `0x251F` and new `0xE906` with `tools/firmware/checksum-verify.ps1`.
- **Dependency:** PIC18 bootloader flash flows through the PIC32 master. The PIC32 image used during migration must support the bootloader/flash path. Direct ICSP is documented as a last-resort option only.

### 3.2 PIC32 (ICD3 route)
- Legacy rollback image = `A90E44…` (on-disk) — preserve it; verify SHA-256 before use; a source commit is not a flashable rollback artifact.
- If non-destructive readback of the running PIC32 is possible and safe, capture it + compare to `A90E44…`; otherwise retain runtime identity UNKNOWN and use `A90E44…` as the proposed rollback image (with an independent controlled recovery test + recorded residual risk).

## 4. Compatibility matrix + migration sequence

| State | PIC18 | PIC32 | C# | Verdict |
|---|---|---|---|---|
| LEGACY (running) | 0x251F (12-reg) | legacy | legacy (fe17ebe) | current, works |
| PIC18-only new | 1.0.0 (21+2) | legacy (12-reg poll) | legacy | **RISK**: legacy C# sees HR11 `0xE906`≠`0x251F` → attempts re-flash/revert; reject |
| PIC32-only new | 0x251F (12-reg) | 1.0.0 (21+2 poll + identity) | legacy | fail-closed: new PIC32 identity check detects legacy HR7 counter → blocks PIC18 (neutral), no movement |
| PIC32+C# new | 0x251F (12-reg) | 1.0.0 | current + gate | fail-closed: C# + PIC32 block legacy PIC18s |
| TARGET | 1.0.0 | 1.0.0 | current + gate | compatible |

**Migration order (derived, safe):**
1. Update **C#** to current master + ProtocolVersion gate (already merged, `376c56e` + `128a0d8`).
2. Update **PIC32** (ICD3) to v1.0.0 — the new master fail-closes on the still-legacy PIC18s (neutral, no movement).
3. Update **PIC18** amplifiers via the bootloader (through the now-compatible PIC32) to v1.0.0, one isolated prototype amplifier first.
4. Verify full-stack: identity, protocol version, register map, neutral output, PWM, CRC, watchdog, E-stop, comm-loss recovery.

PIC32-first is selected because PIC18-first causes the legacy C# to attempt a checksum-mismatch re-flash. A simultaneous multi-device flash is not permitted as the initial experiment.

## 5. Hardware test plan + acceptance (18 items)

1. Exact target identification · 2. Archived HEX verification · 3. ICD3 target/programmer compatibility · 4. PIC32 recovery readiness · 5. PIC18 bootloader availability · 6. PIC18 checksum acceptance · 7. Safe migration ordering · 8. Firmware version readback (HR21/HR22, 113) · 9. Protocol/register-map compatibility · 10. Neutral H-bridge output · 11. PWM vs P2-A0 reference (19.92 kHz / 42.4%) · 12. Hardware CRC · 13. Watchdog timing/recovery · 14. Emergency-stop · 15. Comm-loss recovery · 16. Restart/reconnect · 17. Failed-update recovery · 18. Independent Integrator acceptance.

Use P2-A0 legacy measurements as historical reference, not target-firmware validation. No Phase-2 thresholds, no intentional short-circuit.

## 6. GO/NO-GO preflight verdict

**Software preflight: READY.** Blockers for physical deployment:
1. **PIC32 legacy Production HEX is not Git-backed** (on-disk only) — must be preserved/committed or its recovery path explicitly risk-accepted before flashing.
2. **PIC32 runtime identity UNKNOWN** — need runtime readback or a verified safe recovery image + residual-risk acceptance.
3. **C# bundled firmware HEX is stale** (`dist/Offset` = `0x251F`) — must be updated to the new `4F9ED4EC`/`0xE906` artifact before the PIC18 flash, else the flash step would compare against the wrong checksum.
4. **`LIVE_HARDWARE` + `FIRMWARE_FLASH` authority** — not yet granted.

Future: a PIC32 application bootloader is a later improvement (not a prerequisite).
