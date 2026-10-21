# L2-STAGE-A-PYTHON-COMPARISON-AND-RETRY — technical reference

Status: OFFLINE investigation + minimal driver correction (2026-10-21). No hardware, no flash, no firmware/C# change. This feeds the Stage-A retry decision.

## A. Existing implementations

| Implementation | Path | Derives from C#? | Flash-tested? |
|---|---|---|---|
| Proven C# uploader | `Siebwalde-legacy-oval` @ `80b7a41` (`FlashFwTrackamplifiersStep.cs`, `RecoverSlavesStep.cs`, `SendNextFwDataPacket.cs`, `TrackAmplifierBootloaderHelpers.cs`, `TrackCommClientAsync.cs`, `PublicEnums.cs`) | (authoritative) | **YES — proven on hardware** |
| `StateMachines.py` | `TrackController5/Python/` | Yes (1:1 port of the init FSM) | No hardware evidence |
| `bootload.py` | `TrackController5/Python/` | Partially (hex parsing shared; the 0x55 UART driver is an older direct-serial path) | Older path, unknown |
| `flash_one.py` (new) | `TrackController5/Python/` (in `Siebwalde-legacy-restoration`) | No (new single-target 123 path) | Offline-only |

Note: "FlashTrackamplifiers" is a *method* in `StateMachines.py`, not a file.

## B. Flash transaction comparison

### Proven C# fleet path (`FlashFwTrackamplifiersStep`)
`250 CONNECT → 103 → 120 → 141×480 (64-byte blocks, per-ack) → 143 checksum(104) → 121 → 151 → 122 FLASH_ALL_SLAVES`. `122` is **not** a broadcast: `FlashAllSlavesAuto()` (`slavefwhandler.c:722-820`) loops 1..50 and flashes only `SlaveDetected && HR11 != fwchecksum`, via `FlashSequencer()`.

### Proven C# single-slave recovery (`RecoverSlavesStep`) — NOT SELECT_SLAVE
`250 → 103 → 124 GET_BOOTLOADER_VERSION → 125 ERASE → 120 download → 121 config → 126 WRITE → 127 WRITE_CONFIG → 128 CHECK_CHECKSUM → 129 SLAVE_RESET → 130 EXIT`. It does **not** emit `SELECT_SLAVE (123)`; it assumes the target is already in bootloader mode.

### `flash_one.py` (new)
`250 → 103 → 120 → 141×480 → 121 → 151 → 123 SELECT_SLAVE → 125 → 126 → 127 → 128 → 129 → 130 → 100/106/101 readback`. The `123` path is the manual exposure of `SelectSlave()` (the same function `FlashSequencer` uses internally to force a live amp into bootloader via backplane HR0 select + HR11=0), but it is **not** present in the proven C#.

**Closest to C#:** `StateMachines.py` (1:1 port). `flash_one.py` is the most robust (per-ack gating, ERROR detection, timeouts) but its `123` tail is a new sequence.

## C. Root-cause analysis — RECEIVED_WRONG_COMMAND (frames `aa208c0965`, `aa1e780964`)

- `aa208c0965` = `FWFILEDOWNLOAD(32)/STANDBY(140)/ERROR(9)/RECEIVED_WRONG_COMMAND(101)` (`slavefwhandler.c:561-565`).
- `aa1e780964` = `FWHANDLER(30)/RECEIVE_FW_FILE(120)/ERROR(9)/NONE(100)` (`slavefwhandler.c:182-189`).

**Proven mechanism:** `FwFileDownload()` case 1 (`slavefwhandler.c:529-569`) accepts ONLY command 141; any other command byte in the shared receive mailbox during download → `RECEIVED_WRONG_COMMAND` + FW-handler ERROR.

**Trigger (SPECULATIVE — not provable from the two frames, which are the error *response*, not the cause):** most likely a **stale datagram from a prior interrupted attempt** left in the PIC32 receive mailbox (consistent with the previously documented stale-mailbox `RECEIVED_CHECKSUM_NOK` incident). A full capture with the host TX preceding the frames is required to prove the exact byte.

## D. Minimal driver correction (proven defects only)

1. **125–129 DO emit host acks** — corrected `_manual_primitives()` to wait for `ERASE_FLASH_RETURNED_OK(202)/WRITE_FLASH_RETURNED_OK(212)/WRITE_CONFIG_RETURNED_OK(222)/CHECK_CHECKSUM_CONFIG_RETURNED_OK(232)/RESET_SLAVE_OK(242) / DONE`, abort on the ERROR variant (proven values from `RecoverSlavesStep.cs`). 123/130 remain no-ack bounded-delay (they truly emit no host ack).
2. **Stale-mailbox drain** — added `_drain_feedback()` (bounded idle read before `120`). A clean power-cycle remains the safest mitigation for a stale command already inside the PIC32 mailbox.

Retained (correct): `Comm.py` `ReadSerial` bounded loop; `_wait_feedback` ERROR-abort; unbuffered logging.

## E. Offline verification

- `python test_flash_one.py` → **21/21 OK** (was 17→18→21 across the fixes). Covers: exact command order, 103 entry, 125–129 bootloader acks + ERROR abort, stale-frame drain, forbidden 122/131/132 absence, HEX checksum (offline fixture `0x7907`) + config word parse. (The L2 *target* checksum is `0x195A`, asserted in §G's hardware verification, not in the offline fixture.)
- `--dry-run` emits `250 103 120 141×480 121 151 123 125 126 127 128 129 130 100 106 101`.
- No firmware/C#/UART/Modbus/bootloader file changed.

## F. Independent Integrator verdict

**PASS** (offline, read-only). All 8 review items confirmed: C# fleet (103→120→141×480→121/151→122, sequential selective) + recovery (103→124→125→…→130, no 123) reconstruction; 125–129 bootloader acks (202/212/222/232/242); stale-mailbox mechanism proven + trigger speculative; no MBUS preamble; no fleet/131/132; bounded handling; 21/21 tests; L1 (`d6b7aaa`) + L0 (`1bf9e6f`) untouched, no firmware/C# change.

Non-blocking findings: (F1) offline fixture checksum is `0x7907` (corrected above); (F2) the `80b7a41` C# is a merge commit, not a branch; (F3) the driver omits `GET_BOOTLOADER_VERSION`(124) confirmation between select and erase; (F4) download-before-erase ordering is fleet-like, not recovery-like; (F5) the 60 s WRITE ack budget is unverified; (F6) the post-flash readback `SLAVES_ON(100)` powers on all amps.

**Verdict:** the driver is **software-safe** for a supervised first hardware attempt (single target, clean power-cycle, stop conditions, operator-in-the-loop, rollback `0x251F`), but the `123→125-129→130` tail is a **new composition, not yet hardware-validated**.

## G. Single-amplifier retry plan (supervised, NOT executed)

1. Clean power-cycle the PIC32 + amps (safest stale-mailbox mitigation), then confirm CONNECT.
2. `flash_one.py --slave-id 3 --hex <L2 0x195A HEX> --ip 192.168.1.193` — driver sends 250→103→120→141×480→121→151→123(amp3)→125–129 (ack-waiting)→130→readback.
3. Verify fresh: amp 3 HR11=`0x195A`, HR3=`0xF100`, HR9=`0x0001`, HR10=`0x0000`; amps 1/4/6 HR11=`0x251F`. NO command 122, NO 131/132.
4. Stop conditions: abort on any `*_RETURNED_NOK`/ERROR; do NOT reset on checksum NOK; rollback = `0x251F` via the same path (or PO physical intervention).

## H. Remaining uncertainty

- The exact offending byte that triggered the two captured frames (requires the host-TX capture).
- Whether the stale-mailbox drain + a clean power-cycle fully eliminates the `RECEIVED_WRONG_COMMAND`.
- The 12-vs-14 config-word `memcpy` (`slavefwhandler.c:669`) is benign but unproven on hardware.
- The `123` single-target path, though it drives the proven `SelectSlave()` code, has not itself been hardware-validated.
