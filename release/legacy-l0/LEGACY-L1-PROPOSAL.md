# LEGACY-L1-PROPOSAL — minimal PIC32 semantic firmware-version identification

PROPOSAL ONLY — no implementation in L0. Subject to PO approval.

## Standing increment rules (from the PO directive)

- Each increment starts from the last physically proven checkpoint (L0 legacy).
- One small, justified functional delta per increment.
- No unrelated refactoring; no spontaneous clock/UART/bootloader modification.
- No simultaneous master+amplifier protocol changes unless explicitly approved.
- Offline build + tests before hardware; independent Integrator review; explicit PO hardware authorization.
- Physical regression against the previous checkpoint; immediate rollback on failure; new checkpoint only after PASS.
- Firmware source + compiled HEX must have traceable provenance; the previous checkpoint stays immutable.

## L1 objective

Add a **minimal PIC32 semantic firmware-version identification** so the restored legacy PIC32
can report its own version (for traceability), without reintroducing the full Phase-1 1.0.0 stack.

## Intended source delta (small, no unrelated changes)

1. PIC32 (`TrackController5`): add a committed semantic version constant (e.g. a `version.h`
   with `MAJOR/MINOR/PATCH` + a build/protocol tag), and a single read-only command
   `GET_CONTROLLER_VERSION` (= 113) handled unconditionally in `controller.c` that returns it.
   - No UART/clock/baud change, no ISR change, no FlashSequencer change.
2. C# (matching legacy build): add a bounded `QueryControllerVersionStep` that sends 113 and
   surfaces the version; no protocol gate, no firmware-rejection logic.
3. No amplifier (PIC18) change. No bootloader change.

## Why justified

The legacy PIC32 currently returns `RECEIVED_UNKNOWN_COMMAND` for 113, so there is no
runtime-readable firmware identity. A minimal version command gives durable provenance for the
restored PIC32 without touching the protected 8/9-bit communication contract.

## Tests

- Offline: PIC32 build + a unit test asserting 113 returns the expected version string and does
  not alter UART/baud/FlashSequencer paths.
- Hardware (after PO authorization): CONNECT → 113 → expect the version reply, then re-run the
  L0 regression checklist.

## Integration dependencies

- The legacy command set is unchanged (113 is additive); the existing legacy C# (`80b7a41`) and
  Python CLI ignore 113. The version command must not gate startup or reject legacy firmware.

## Explicitly OUT of scope for L1

- No cherry-pick of the whole 1.0.0 stack; no 131/132; no 9-bit-TX; no clock/baud changes;
  no C# protocol-version gate; no amplifier changes.
