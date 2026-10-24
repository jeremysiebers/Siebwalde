# K7 — Diagnostics Hardening, Firmware Identity Reconciliation & Next OBSERVE Readiness

**Increment:** K7 (offline-only corrective + investigation increment)
**Status:** offline implementation and independent review complete. No live hardware. No master merge.
**Branch:** `feature/k7-diagnostics-hardening` (base = K6 HEAD `bd5a9fa2` = `5269624e` + K6 chain)

---

## Software — corrective changes

### Sent-state accuracy correction (§6)
`tools/diagnostics/live_transport.py` now reports a typed `send_state` (last completed phase) and an accurate `sent` flag:
`not_attempted → bind_failed → send_failed → sent → received → validated`.
- `sent` is `True` **only** after `sendto` returns. A socket-create/bind failure yields `UNKNOWN` with `sent: False` and `send_state: "bind_failed"`; a `sendto` failure yields `sent: False`, `send_state: "send_failed"`.
- Socket creation was moved inside the guarded path, so a socket-creation failure maps to `UNKNOWN` instead of raising.
- A decoded `PASS` carries `controller_reported: true` and `physical_execution: "unknown"` (a readback proves the reported version, never the programmed flash).

### Single-use authorization enforcement (§7)
- `Command113Transport` now requires an `ObserveSession` and consumes it **atomically (thread-safe)** before any I/O. Absent/invalid/already-consumed session → `UNKNOWN`, no send.
- `ObserveSession.consume()` is guarded by `threading.Lock` (exactly one concurrent winner).
- No failure path restores authority; the token is not restored after an I/O error.
- Documented boundary: the token is an **in-memory, single-process** authority — not a system-wide hardware lock; process restart ≠ renewed permission.

### Tests
20 new offline tests (11 send-state accuracy, 9 single-use/concurrency) in `tools/diagnostics/tests/test_send_state_accuracy.py` + `test_single_use_authorization.py`.

### Regression (actual counts)
| Suite | Result |
| --- | --- |
| Diagnostics Core + MCP | **111 passed** |
| K3 firmware driver (`test_flash_one.py`) | **43 OK** |
| Comm.py (`test_Comm.py`) | **5 OK** |
| Knowledge Foundation | **28 passed** (25 baseline + 3 K7 assertions) |
| Tool preservation guard | **exit 0** |
| PicoScope offline | unchanged (not re-run; no K7 change to PicoScope) |

---

## Engineering evidence

### Firmware-identity timeline (§5)
Full analysis in `docs/K7-FIRMWARE-IDENTITY-TIMELINE.md`. Summary:
- PIC32 **1.0.0 was actually deployed** (ICD3 flash + command-113 readback `1.0.0 / protocol 0x01 / "2026-10-17"` confirmed 2026-10-19).
- K6 (2026-10-24) read back **0.1.0 / protocol 0 / "2026-10-20"** — a **different, untracked build** that matches neither master `version.h` (1.0.0) nor any tracked `version.h`.
- The 82-byte reply was decoded **correctly** (not a byte-shift): MAJOR 0 / MINOR 1 / PATCH 0 / PROTOCOL 0 / build "2026-10-20".
- The 1.0.0 → 0.1.0 transition is **UNKNOWN** (no captured re-flash evidence). Do not fabricate a cause.

### Physical evidence integrity (§8)
`docs/K6-EVIDENCE-MANIFEST.md` pins the two git-ignored K6 evidence files and their hashes:
- `Logging/K6B/k6b-observe-20261024T092944Z.json` — file SHA-256 `cf0a5cb0…`; response SHA-256 `f40a31ed…` (recomputed and matching).
- `Logging/K6B/k6b-preflight.json` — file SHA-256 `593770a0…`.
- **Preservation status:** local-only (git-ignored, not in Git/LadybugDB/remote) — NOT durably backed up; hashes remain committed for re-verification.

### Knowledge Foundation updates (§10)
See the K7 knowledge update (graph rebuilt; new nodes `obs:k7-pic32-identity-transition`, `obs:k7-cmd124-ineligible`, `obs:k7-next-readonly-hr11-slaveryinfo`).

---

## Future diagnostics — recommended next read-only operation (§9)

**Operation:** passive **SLAVEINFO** observation of HR11 (+ HR7/HR21/HR22) for amplifiers **1 and 3** at their assigned Modbus addresses during normal `SERVICE_TASKS` runtime — via the existing cyclic FC03 poll → SLAVEINFO stream.

**Data path:** PC (UDP) ← PIC32 SLAVEINFO frame ← `MASTER_SLAVE_DATA[addr].HoldingReg[11]` ← FC03 read `HOLDINGREG10, qty=2` (SIP4) ← PIC18 `PetitHoldingRegisters[HR_SW_CHECKSUM]` (passive).

**Contract:** no new host command needed — the master already streams SLAVEINFO in `SERVICE_TASKS`. A cold-start trigger would be `100 → 106 → 101 → 104 → 105`, which is **not fully passive** (105 ENABLE turns outputs on), so a truly passive read needs the system already in `SERVICE_TASKS`.

**HR11 semantics:** `0x251F` = legacy, `0xE906` = Phase-1 1.0.0, `0xFFFF`/no-answer = erased/bootloader-only. HR11 alone cannot exclude an arbitrary legacy build; HR7/HR21/HR22 disambiguate 1.0.0.

**Physical prerequisites:** amplifiers powered; PIC32 in `SERVICE_TASKS` (or bounded bring-up); exclusive ownership (Koploper + all Siebwalde hardware clients stopped, no competing PIC32 client, no locomotive movement).

**GO criteria:** exclusive session confirmed; fresh SLAVEINFO frame per target with `SlaveDetected=1`; `HR11 ∈ {0x251F, 0xE906}`; if `0xE906` also HR7/HR21/HR22 populated; no HR11 read-0 with `MbCommError=BUSY`.

**NO-GO criteria:** ambiguous/multiple responders; `SlaveDetected=0` (bootloader-only/erased — not recoverable via this route); unknown HR7 protocol on a 1.0.0-expected amp; any competing client; any motion/output state; any `122/131/132` or `123/124` emission.

**Command 124 = INELIGIBLE** for a read-only grant (requires bootloader entry via state-changing 123+reset; unaddressed so cannot identify which amplifier; returns bootloader version not HR11; reported non-functional and PO-removed).

**Remaining UNKNOWN states:** live HR11 of amp-1/amp-3; whether a cold-start readback without 105 can complete a SIP rotation; whether a single-register read on an arbitrary address exists (no such host command in the inspected route table); `--read-hr` CLI (PR #49) not on this branch.

---

## Governance

- **Architect verdict:** `DESIGN_ACCEPTABLE` — transport-level single-use is atomic + enforced before I/O; send-state accurate (no false `sent`); allowlist `{113}` double-enforced; in-memory single-process boundary correctly documented; offline MCP separation intact. Residual risks (non-blocking): per-token (not process-global) authority; no runtime session type-check; port/recv_port range-validation still open (backlog item 3).
- **Integrator verdict:** `PASS` — all 7 items independently verified (firmware-identity interpretation, result-state accuracy, authorization consumption + concurrency, offline-only safety, knowledge provenance, tool preservation, regression 111/43/5/25). One advisory date-attribution correction applied to `docs/K7-FIRMWARE-IDENTITY-TIMELINE.md` (PIC32 flash ~2026-10-18 commit `69a501f2`, command-113 confirmation ~2026-10-19 commit `d7167e85`; §2.5's "2026-10-19" is the PIC18 amp-1 flash).
- **Branch / HEAD:** `feature/k7-diagnostics-hardening` (final HEAD set at closure commit).
- **K6/K7 integration readiness (§13):** K6 (3 commits) + K7 corrective changes form one reviewed feature chain on top of `5269624e`. Both must be merged together; K7 must not be merged without K6. Recommend a single reviewed PR from `feature/k7-diagnostics-hardening` after the K7 reviews pass. **PR creation/merge requires applicable Workflow v1 + PO authorization** (not performed in K7).
- **Required next PO authorizations:** (1) PR/merge approval for the K6+K7 chain; (2) a new scoped LIVE_HARDWARE grant for the recommended SLAVEINFO HR11 readback session (the K6 grant is consumed and non-transferable).

## Authorization boundary (K7)

AUTHORIZED (done): offline source/evidence inspection; Diagnostics Core corrections; authorization safety improvements; offline tests + fault injection; Knowledge Foundation updates; tool-registry preservation; architecture review; independent Integrator review; branch commits + push.
NOT AUTHORIZED (not done): any live PIC32/PIC18 command; reuse of K6 authorization; firmware changes; flashing/recovery; reset/power-cycling; live measurement; operational C# changes; tool retirement/deletion; master merge; generalized hardware-control MCP.
