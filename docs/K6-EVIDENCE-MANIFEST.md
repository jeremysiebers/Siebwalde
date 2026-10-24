# K6 Physical OBSERVE Evidence Manifest

**Purpose:** a durable, source-committed reference to the K6 physical evidence. The raw evidence files are git-ignored (local-only) and are **not** committed to Git or LadybugDB; this manifest pins their content hashes and metadata so their integrity and provenance can be re-verified on any workstation where the files are present.

## Evidence files (git-ignored, local to the K6/K7 worktree)

| File (relative to worktree) | SHA-256 (of the file) |
| --- | --- |
| `Logging/K6B/k6b-observe-20261024T092944Z.json` | `cf0a5cb08d402beaaaf05409022d7a9be3de8e7b0471585a8654aee9e6450642` |
| `Logging/K6B/k6b-preflight.json` | `593770a0d7103c201c2f95d1f962619e80c15aa5ba4649c3fb0690b3241ef0e9` |

## Captured transaction (from the observe file, re-verified)

| Field | Value |
| --- | --- |
| run_id | `20261024T092944Z` |
| command / request | 113 (`aa71`), request_count 1 |
| endpoint | `192.168.1.193` |
| response bytes (hex) | `aa0a710001000000323032362d31302d323000…` (82-byte `udpTrans_t`) |
| response SHA-256 | `f40a31edf3924488f82e1ee542bac9400d6dd9d3864249b8492504acb4a669c8` |
| decoded controller version | 0.1.0 / protocol 0 / build "2026-10-20" |
| classification | `OBSERVE_PASS` |
| authorization | single-use consumed (`authorized_command_count_remaining: 0`) |
| recorded_at | `2026-10-24T09:29:44.338552+00:00` |

The response SHA-256 above was recomputed from the recorded `response_bytes` during K7 and matches the recorded value (K6 committed this hash into `docs/K6-LIVE-OBSERVE-VALIDATION.md`).

## Source revision binding

The physical transaction used source revision `ae18cb44` (`k6b_observe.py` live entry point) on branch `feature/k6-observe`, built on transport `5cc2402f`, base `5269624e`. Final K6 closure `bd5a9fa2`.

## Preservation status and limitation

- **Preserved:** the two JSON files are intact and their hashes are stable (verified 2026-10-24, K7).
- **Not durably backed up:** the files live only under the local worktree `Logging/K6B/` (git-ignored). They are **not** in Git, not in LadybugDB, and not on any known remote/backup. A workstation loss would lose the raw captures (the hashes in this manifest and the committed report would remain, but the raw files would not).
- **Recommended follow-up:** copy the two files to a durable offline backup location and/or record their hashes in the project's external evidence store (out of scope for K7, which is offline-only and must not introduce new storage side effects).
- **No regeneration:** these are the original physical captures; K7 must never replace them with regenerated evidence.

## Evidence-hierarchy note

The physical readback proves the PIC32 *reported* 0.1.0 / protocol 0 / "2026-10-20" at that instant. It is not an independent cryptographic measurement of the programmed flash contents, and it says nothing about PIC18 amplifier firmware.
