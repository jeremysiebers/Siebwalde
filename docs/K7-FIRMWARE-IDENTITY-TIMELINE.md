# K7 — PIC32 Firmware Identity Timeline (source- and evidence-backed)

**Status:** source/evidence synthesis, 2026-10-24. This documents what is VERIFIED against source + physical captures, what is HYPOTHESIS, and what is UNKNOWN. It does not change firmware, and it does not grant any hardware authority.

## Conclusion (summary)

The PIC32 **1.0.0 image was actually deployed** (flashed ~2026-10-18 and command-113-confirmed ~2026-10-19), but the K6 physical readback (2026-10-24) showed **0.1.0 / protocol 0 / build "2026-10-20"** — a **different build** than the master-branch source (`1.0.0 / protocol 0x01 / "2026-10-17"`). The physical firmware therefore does **not** match the current master source, and the transition from the confirmed 1.0.0 to the observed 0.1.0 is **UNDOCUMENTED (UNKNOWN)** — there is no captured evidence of the re-flash.

## Verified source facts

- `version.h` was **introduced at 1.0.0** in commit `f9ae0ed9` ("semantic versioning + runtime identification"): `CTRL_FW_MAJOR 1u`, `CTRL_FW_MINOR 0u`, `CTRL_FW_PATCH 0u`, `CTRL_PROTOCOL_VERSION 0x01u`, `CTRL_BUILD_DATE __DATE__ " " __TIME__`.
- Commit `68d1a6c2` ("deterministic PIC32 build") replaced `__DATE__/__TIME__` with deterministic `CTRL_RELEASE_TIMESTAMP "2026-10-17"`.
- `REPLYxCONTROLLERxVERSION()` (command 113 reply) was **introduced in `f9ae0ed9`**. Before that commit there was **no** version reply and no tracked `version.h`. The reply serializes `data[0]=113` (echo), `data[1]=MAJOR`, `data[2]=MINOR`, `data[3]=PATCH`, `data[4]=PROTOCOL`, `data[5]=0`, `data[6..]=release timestamp` (null-terminated), inside the 82-byte `udpTrans_t { header, command, data[80] }`.
- `docs/firmware-flash-recovery-technical-reference.md` §2.2/§2.3 records: PIC32 Production 1.0.0 (`SHA 6CEC61D4…`), "real ICD3 flash/rollback/reflash reported PASS; version command 113 CONFIRMED (1.0.0, protocol 0x01, build 2026-10-17)", and "PIC32 last programmed image = 1.0.0".

## Verified physical readback (K6, 2026-10-24)

- Request `aa71` (header 0xAA + command 113), response `aa0a710001000000323032362d31302d323000…` (82 bytes, SHA-256 `f40a31ed…669c8`).
- Decode is **correct** (verified byte-for-byte): header `0xAA`, command `0x0A` (CONTROLLER), echo `0x71` (113), MAJOR `0x00`, MINOR `0x01`, PATCH `0x00`, PROTOCOL `0x00`, reserved `0x00`, build `"2026-10-20"`.
- This is **0.1.0 / protocol 0 / build "2026-10-20"**. It is not a byte-shift of 1.0.0: MAJOR=0 (vs 1), MINOR=1 (vs 0), PROTOCOL=0 (vs 0x01), build date 2026-10-20 (vs 2026-10-17). Fields are unambiguous.

## Timeline

| Date (approx.) | Event | Status |
| --- | --- | --- |
| before `f9ae0ed9` | PIC32 ran "L1 0.1.0" (Knowledge `firmware:pic32-0.1.0`, HEX prefix `EC7E3E6E`); no command-113 version reply existed in tracked source. | PROVEN_SOURCE / PROVEN_HARDWARE (artifact identity) |
| `f9ae0ed9` | `version.h` 1.0.0 + command 113 + version reply introduced. | PROVEN_SOURCE |
| `68d1a6c2` | build date made deterministic → `"2026-10-17"`. | PROVEN_SOURCE |
| ~2026-10-18/19 | PIC32 flashed to 1.0.0 (recorded ~2026-10-18, commit `69a501f2`); **command 113 CONFIRMED 1.0.0 / protocol 0x01 / "2026-10-17"** (recorded ~2026-10-19, commit `d7167e85`); "last programmed = 1.0.0". (Note: the "2026-10-19" date in §2.5 refers to the PIC18 amp-1 flash, not the PIC32.) | PROVEN_HARDWARE (flash + readback reported) |
| ~2026-10-20 | A **different** PIC32 image reporting 0.1.0 / protocol 0 / `__DATE__`-style build "2026-10-20" was built. No tracked source commit corresponds to this. | HYPOTHESIS (build date inferred from the physical report) |
| between 2026-10-19 and 2026-10-24 | PIC32 transitioned from 1.0.0 to 0.1.0 (re-flash or rollback). **No captured evidence of this transition.** | UNKNOWN |
| 2026-10-24 (K6) | command 113 readback = 0.1.0 / protocol 0 / "2026-10-20". | PROVEN_HARDWARE (observed) |

## Key interpretations

1. **1.0.0 was deployed, not merely staged.** The flash-recovery reference records a real ICD3 flash + a command-113 readback of 1.0.0/protocol 0x01/"2026-10-17" (flash ~2026-10-18, confirmation ~2026-10-19). The directive's caution ("do not conclude 1.0.0 was NEVER flashed from one 0.1.0 response") is correct: it WAS flashed; the PIC32 was later changed.
2. **The current physical firmware is NOT the master source.** It reports 0.1.0/protocol 0/"2026-10-20", which matches neither the master `version.h` (1.0.0/0x01/"2026-10-17") nor any tracked `version.h` (which only ever held 1.0.0). The physical image corresponds to an **untracked/separate build**.
3. **The reported build date "2026-10-20"** is most likely a non-deterministic `__DATE__` (the 1.0.0 build had already switched to the deterministic "2026-10-17" timestamp), implying the physical image was compiled from a state where the build-date macro was still `__DATE__` and the version macros were 0.1.0 / protocol 0.
4. **The transition is UNKNOWN.** There is no captured log, flash transcript, or readback between the ~2026-10-18/19 1.0.0 confirmation and the 2026-10-24 0.1.0 observation. Do not fabricate a reason.

## Knowledge-graph implications

- `firmware:pic32-1.0.0` ("controller-version 113 = 1.0.0/protocol 0x01", PROVEN_HARDWARE) is **correct for 2026-10-19** but is not the current state.
- `firmware:pic32-0.1.0` (L1, "kept on the PIC32 for the L2/PICO-5B tests") is **consistent with the K6 readback**, but its exact HEX/SHA↔report linkage and its build date "2026-10-20" are not yet proven byte-identically against the physical device.
- The two assertions are **temporally ordered, not contradictory** — but the graph lacks an explicit "1.0.0 was later replaced by 0.1.0 (UNKNOWN mechanism)" link, which K7 should record.

## Verification limit

A physical version response proves what the controller **reported at that time**, not an independent cryptographic measurement of flash contents. No K7 step measures flash; the physical image identity (beyond the reported version string) remains UNKNOWN unless a future authorized readback or artifact-hash verification is performed.
