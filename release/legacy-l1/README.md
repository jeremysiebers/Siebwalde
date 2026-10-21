# Legacy L1 — PIC32 firmware-version identification (0.1.0) archival

The L1 checkpoint: a minimal, additive PIC32 semantic firmware-version command (113),
built on the proven legacy L0 baseline.

## Deployed artifact (authoritative, hardware-tested)

- **File:** `release/legacy-l1/PIC32-Master-L1-version-0.1.0.hex`
- **SHA-256:** `EC7E3E6E120C2A0A40DD4E13D16ABCBE62A76F8A14F22983C7DB6E399124D2A6`
- **Source:** `feature/legacy-l1-version-id` @ `39577845d1ecccbd0c1699af88d41ce43cd7dcdd` (base `928ea7c`), 3 files / 46 insertions.
- **Firmware version:** `0.1.0` · **protocol tag:** `0x00` · **release tag:** `2026-10-20`.

## Command-113 wire contract

82-byte `udpTrans_t` reply:
`[0xAA, 10, 113, 0, 1, 0, 0x00, 0, "2026-10-20", zero-pad…]`
→ semantic `0.1.0`, protocol `0x00` (does NOT advertise Phase-1 capability; commands
109–112/131/132 remain unhandled → `RECEIVED_UNKNOWN_COMMAND`).

## Build + toolchain

- MPLAB X v6.20, XC32 2.50, Harmony v2_06, PIC32MZ-EF_DFP 1.4.168, device PIC32MZ2048EFH144.
- **Known limitation:** the whole-image build is not byte-reproducible because of the
  inherited `__DATE__`/`__TIME__` log string in `controller.c:156` (legacy base). The L1
  `CTRL_RELEASE_TIMESTAMP` is deterministic. This deployed `EC7E3E6E…` is authoritative —
  do NOT rebuild/substitute.

## PIC18 amplifiers (unchanged)

- Addresses 1, 3, 4, 6 — legacy checksum `0x251F` (Offset `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex`).
- Backplane `Proto_Backplane`; C# reference `80b7a41`.

## Rollback

`release/baseline-0/PIC32-Master-legacy-A90E44.hex` (SHA `A90E4495…`) via ICD3.

## Regression status

- Normal operation / Koploper / locomotive: **PASS** (PO-confirmed + Integrator CONNECT/113/detection).
- L1 bootloader (Offset) flash: **DEFERRED / NOT YET TESTED** (see LEGACY-L1-ACCEPTANCE.md).

See also: `LEGACY-L1-ACCEPTANCE.md`, `docs/LEGACY-L1-DETAILED-DESIGN.md`, and the L2 design `docs/LEGACY-L2-DETAILED-DESIGN.md`.
