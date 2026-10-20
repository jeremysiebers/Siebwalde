# LEGACY-L0-GIT-INVENTORY — Worktrees, changes, classifications, provenance

L0 rule: do NOT `git add .`, `git commit -a`, `git reset --hard`, or `git clean`. All existing dirty worktrees are preserved exactly as found. This L0 archival commit lives on a separate branch and touches none of them.

## Worktree status (captured 2026-10-20)

| Worktree | Branch | HEAD | Staged | Unstaged | Untracked |
|---|---|---|---|---|---|
| `Siebwalde` (main) | docs/firmware-flash-recovery-reference | d7167e8 | 25 | 10 | 198 |
| `Siebwalde-integration` | integration/phase1-firmware-combined | 0f8cd81 | 0 | 7 | 13 |
| `Siebwalde-legacy-restoration` | recovery/full-legacy-restoration | 928ea7c | 0 | 1 | 8 |
| `Siebwalde-stabilization` | recovery/phase1-stabilization | 2a28543 | 0 | 1 | 5 |
| `Siebwalde-Firmware` | detached | e845b9d | 0 | 0 | 0 |
| `Siebwalde-legacy-minimal` | recovery/phase1-legacy-minimal | 928ea7c | 0 | 0 | 0 |
| `Siebwalde-legacy-csharp` | detached | fe17ebe | 0 | 0 | 0 |
| `Siebwalde-legacy-oval` | detached | 80b7a41 | 0 | 0 | 0 |
| `Siebwalde-fwdeploy`, `siebwalde-phase1-{csharp,pic18,pic32}`, `Siebwalde-single-target-flash`, `siebwalde-bootrec`, `siebwalde-estop`, `siebwalde-read-controller-version`, `kis10-docs-closure`, `siebwalde-estop-wt` | (various) | (various) | 0 | 0 | 0 |

## Classification of the main-repo dirty changes (`Siebwalde`)

### Staged (25) — PO's successful legacy restoration of TrackAmplifier4.X
- `D` (deleted): the Phase-1 1.0.0 additions removed to restore the legacy source —
  `version.h`, `amp_core.c/.h`, `amp_adapter.c/.h`, `amp_model.h`, `modbus/modbushooks.c/.h`,
  `tests/` (test_amp.c, test_amp.py, test_transport.c, petit_shim.h, README.md),
  `AGENT_TRACK_AMPLIFIER*.md`, `MODBUS_TRACK_AMPLIFIER_MAPPING.md`, `TRACK_AMPLIFIER_STATE_MACHINE.md`,
  `Track Amplifier Firmware Reusable Agent Promp.txt`.
- `M` (modified): legacy source restored to `928ea7c` — `main.c/.h`, `processio.c`, `regulator.c`,
  `modbus/PetitModbus.c/.h`, `modbus/General.h`, `nbproject/configurations.xml`.
- These make `TrackAmplifier4.X` match the legacy `928ea7c` source (producing `0x251F`).

### Untracked (198) — generated/build-only (NOT source)
- `TrackAmplifier4.X/dist/Combined/**` (`.hex`, `.elf`, `.map`, `.lst`, `.o`, `.sdb`, …), `TrackAmplifier4.X/dist/Offset/**` build outputs, `TrackAmplifier4.X/TrackAmplifier4.X.zip`, `.vs/` Visual Studio caches, and other build artifacts.

### Provenance classification
- Phase-1 experimental changes (9-bit TX `0xCD1B`, 63 MHz clock, ISR diagnostics, case-4 retry, HR15 workaround): present only in the Phase-1 branches/worktrees (`integration/phase1-firmware-combined`, `recovery/phase1-stabilization`, `siebwalde-phase1-*`, the non-Git `bootrec` tree), NOT in the legacy source.
- PO's legacy restoration: the `Siebwalde` main-repo staged TrackAmplifier4.X restore + the KIS-10 `80b7a41` C# build.
- Generated/build-only: the untracked `dist/**`, `.vs/`, `.zip`.
- Unrelated PO work: not individually reclassified; preserved in place.

## Preserved experimental work (NOT discarded)

- PIC18 `0xE906` (1.0.0) and experimental `0xCD1B` (9-bit TX) — in the Phase-1 branches/build scratch.
- PIC32 1.0.0 + guard-only `A8C67978` + clock/baud experiments + bootloader 131/132 — in Phase-1 branches + `bootrec`.
- Backplane broadcast rejection + C# integration — in `recovery/phase1-stabilization` / `integration/phase1-firmware-combined`.

## Source provenance summary

- `928ea7c` = authoritative legacy firmware source (TrackController5, TrackAmplifier4.X, TrackBackplane2.X, TrackAmplifierBootLoader.X).
- `e845b9d2` = `928ea7c` + 5 agent/guidance docs (identical firmware component trees; verified via `git diff --stat`).
- PIC32 legacy binary = `A90E4495…` (authoritative; readback-verified). Exact source→binary provenance for PIC32 is NOT claimed (recorded limitation).
- PIC18 legacy application = `0x251F` (`B848EF04…`); Combined bootloader+app = `66404C8B…`.
