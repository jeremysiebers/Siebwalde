# PIC18 Control Core-lite — implementation / handoff

Scope: Phase-1 B, frozen `docs/amplifier-system-architecture.md` R8. Source-only;
no firmware flash, peripheral session, live hardware, PIC32 or C# modification.

## Reproduce software verification

From the repository root:

```powershell
python TrackAmplifier4.X/tests/test_amp.py
powershell -ExecutionPolicy Bypass -File tools/firmware/build-firmware.ps1 -Project TrackAmplifier4 -Configuration Offset
```

The F1 harness requires a clean tracked tree. It uses MPLAB X 6.20 / XC8 2.31,
PIC18F25K40, Offset (application offset 0x800). Its revision-qualified manifest
records HEX SHA-256, toolchain and build exit status. Build output is not committed.

The Win64 shim uses MSVC C11, `/W4 /WX`, no hardware and no emulated PIC18 ISA.
It compiles the complete unchanged `amp_core.c`, `amp_adapter.c` and recovered
`modbus/modbushooks.c`; it extracts `AmpHw_SetDuty` and FC03/06/16 handler bodies
unchanged. The PWM peripheral, transport response submission and peripheral
standard headers are mocked. Host/PIC18 `unsigned int` width differs; narrowing
warning 4244 is suppressed only around the extracted legacy transport handlers.
CRC/UART/interrupt timing and actual ADC/BEMF execution are not exercised.

Executed coverage includes more than 260,000 assertions: HR1 set-only/edge/re-arm/
rejected-write behavior; config validation and pending-to-active isolation;
read-only/reserved/range and broadcast handling; neutral snap and duty bounds;
any-frame comm watchdog versus WRITE-only lease; fast-stop/non-resumption/release;
EMO brake-latch and last-applied PWM versus command echo; malformed FC03/06/16.
Default lease stop is exercised from full forward duty: expiry on tick 294 and
exact neutral after 343 logical ticks from renewal (584.472 ms of logical time,
plus scheduler delay). This is not a measured wall-clock or physical-stop bound.

## Execution / ownership notes

- `amp_model` owns commands, pending/active configuration, output/status, counters
  and measurements. Only `amp_adapter.c` writes the holding-register mirror.
- `Amplifier_ServiceTick` consumes the existing atomic volatile `uint8_t`
  `Update_AmplifierTicks`, without clearing/widening it. One pending tick is
  processed per super-loop pass, independently of the measurement sequencer,
  including startup. Modulo-byte tracking assumes the main loop never stalls
  for 256 ticks (436.224 ms). That scheduler assumption has not been measured.
- ADC/BEMF state-machine order/timer/channel operations are retained;
  `ADCxIO` targets the model instead of the old map. Monitoring now begins after
  address assignment rather than waiting for ENABLE. PWM permission remains
  separately config/lease/enable gated. BEMF sampling still tri-states pins,
  so applied-duty evidence is not physical-output evidence.
- `AmpHw_SetDuty` updates applied readback from the exact PWM3 call argument.
  EMO retains immediate brake-without-PWM-load semantics; a neutral command
  clears EMO, commits neutral, then a later fresh command may release the inhibit.
- Comm loss is latched while enabled/configured; new communication alone does
  not clear it. CLEAR_FAULT after liveness restoration clears that cause.
  Lease renewal clears the expired lease cause, but never clears INHIBITED.
  Missing active config sets bit4, remains provisionable and cannot drive.
- No additional inter-step ordering constraints were invented: all three ramp
  parameters are independent 1..255. The full config validator checks the
  frozen numeric ranges. Pending starts equal to absent/zero active config;
  rejected APPLY re-seeds pending from active. Normal wire fields are rejected
  before mutation. Requested PWM zero is legal, reports saturated, and applies
  minimum duty one without changing the echo. EMO/STOP zero is not saturation.
- Electrical hard trips remain Phase 2. Hard cause bits are preserved fail-closed;
  CLEAR_FAULT does not invent a way to qualify disappearance of unimplemented
  electrical causes. Identity is fixed at `0xF101`.

## Blocking contract contradiction (Lead / Architect decision required)

Frozen R8-4 requires **all HR11 writes to fail with exception 02**, while the
task also requires preserving the working bootloader-entry handshake.
The actual existing master path is
`TrackController5/firmware/src/slavefwhandler.c:FlashSequencer`, case 6
(lines 934-942 at base `6436c95`): FC write HR11 = 0 at address `0xAA`.
PIC18 `main` startup case 1 detects checksum zero, then cases 2/3 wait for
response completion and reset. Rejecting the master's write makes this normal
entry path unreachable for an ordinary nonzero-checksum application image.

This implementation deliberately follows frozen R8 read-only protection,
retains the zero-checksum/reset sequence, and tests the HR11 rejection. It does
**not** claim preserved operational bootloader entry. Do not flash this image
or declare the full work package accepted. The Lead must route a scoped contract
clarification (for example a narrowly authorized assign-window bootloader
operation) to Architect, then Developer correction and independent R2 review.
No frozen documentation or master behavior was changed here.

## AC evidence disposition (Developer self-verification only)

| AC | Disposition at this boundary |
|---|---|
| AC1 | F1 Offset compile/link succeeded during development; final clean-tree harness result and HEX hash are returned in the role result. Existing/new compiler warnings are not claimed absent. |
| AC2 | Software-proven validation, retained active config, write-result and explicit zero saturation; bootloader HR11 exception conflict remains above. |
| AC3 | Software-proven reset/neutral/config/enable gating. Actual bridge behavior unverified. |
| AC4 | Software-proven exact PWM call argument tracking, including EMO where target and applied differ; no peripheral/physical observation. |
| AC5 | Software-proven watchdog expiry and fast ramp; timeout detection plus ramp, not a claim of neutral before the timeout itself. |
| AC8 | Software-proven lease expiry with continuous FC03, stable inhibited neutral, renewal-only non-resumption and fresh-command release. Wall-clock scheduling and integrated master/host release remain unverified. |
| AC9 | Software-proven HR1 renewal independence, edge 0-to-1/re-arm and rejection behavior. |

Independent R2 review, cross-layer V3 integration, physical stopping, UART/CRC
interoperability, full-complement timing and Phase-2 protection remain outside
this software-only self-verification. Overall work-package completion is blocked
by the HR11 contract conflict, not waived by passing software tests.
