# R0 — Repository Consolidation, Evidence Preservation & Safe Cleanup

**Status:** read-only inventory + classification + cleanup plan, 2026-10-24. **No deletion performed.** This is the audit and the consolidated PO decision package.

---

## 1. Directory inventory (C:\Localdata)

### A. Main repository worktrees (share `C:\Localdata\Siebwalde\.git`) — 23 total

| Path | Branch / HEAD | Merged into master `5fc2a13b`? |
| --- | --- | --- |
| `C:\Localdata\Siebwalde` (main) | `feature/k5-observe-adapter-init-reference` @ `c9260c59` | merged (branch), **DIRTY tree** |
| `C:\Localdata\Siebwalde-k6` | `feature/k7-diagnostics-hardening` @ `38cf63c0` | merged (PR #51); holds local-only K6 evidence |
| `C:\Localdata\siebwalde-estop-wt` | `docs/amplifier-system-requirements` @ `36a62233` | merged |
| `C:\Localdata\Siebwalde-Firmware` | detached @ `e845b9d2` | reachable from master |
| `C:\Localdata\Siebwalde-fwdeploy` | `feature/firmware-deployment-authorization` @ `0a4e3eb9` | **unmerged (ahead 1)** = PR #48 (open) |
| `C:\Localdata\Siebwalde-integration` | `integration/phase1-firmware-combined` @ `0f8cd81a` | **unmerged (ahead 17)** |
| `C:\Localdata\Siebwalde-legacy-csharp` | detached @ `fe17ebe` | reachable from master |
| `C:\Localdata\Siebwalde-legacy-l0` | `baseline/legacy-l0-freeze` @ `1bf9e6f1` | **unmerged (ahead 1)** |
| `C:\Localdata\Siebwalde-legacy-l1` | `feature/legacy-l1-version-id` @ `d6b7aaa8` | **unmerged (ahead 2)** |
| `C:\Localdata\Siebwalde-legacy-l2` | `feature/legacy-l2-version-id` @ `ef8e1bb4` | **unmerged (ahead 1)** |
| `C:\Localdata\Siebwalde-legacy-minimal` | `recovery/phase1-legacy-minimal` @ `928ea7cf` | merged |
| `C:\Localdata\Siebwalde-legacy-oval` | detached @ `80b7a419` | reachable from master |
| `C:\Localdata\Siebwalde-legacy-restoration` | `feature/legacy-l2-stage-a-driver` @ `acb31f49` | **unmerged (ahead 6)** |
| `C:\Localdata\siebwalde-phase1-csharp` | `feature/amplifier-phase1-csharp` @ `de3fb5fe` | merged |
| `C:\Localdata\siebwalde-phase1-pic18` | `feature/amplifier-phase1-pic18` @ `bf9e1641` | merged |
| `C:\Localdata\siebwalde-phase1-pic32` | `feature/amplifier-phase1-pic32` @ `1382feb9` | merged |
| `C:\Localdata\Siebwalde-pico5b-checksum-wt` | `feature/pico-5b-diagnostic` @ `bea42514` | **unmerged (ahead 1)** |
| `C:\Localdata\Siebwalde-single-target-flash` | `feature/pic18-single-target-flash` @ `aa3e59ce` | **unmerged (ahead 1)** |
| `C:\Localdata\Siebwalde-stabilization` | `recovery/phase1-stabilization` @ `2a285435` | **unmerged (ahead 18)** |
| `Temp\opencode\kis10-docs-closure` | `docs/koploper-kis10-closure` @ `272e31ff` | merged |
| `Temp\opencode\siebwalde-bootrec` | `feature/recover-slaves-count-independence` @ `4850ce0e` | **unmerged (ahead 8)** |
| `Temp\opencode\siebwalde-estop` | detached @ `80b7a419` | reachable from master |
| `Temp\opencode\siebwalde-read-controller-version` | `tools/read-controller-version` @ `e21867f` | **unmerged (ahead 1)** |

### B. Non-worktree directories (independent)

| Path | Type | Size | Notes |
| --- | --- | --- | --- |
| `C:\Localdata\Siebwalde_Backup` | independent clone (own remote `Siebwalde_Backup.git`) | 527 MB | historical "analog prep" app (pre-EcosEmu), `main` @ `2c7b7fb` |
| `C:\Localdata\Siebwalde Temp if data is missing` | ordinary folder (no .git) | 449 MB | plain-file copy of the project tree (recovery copy) |
| `C:\Localdata\Siebwalde-Audit-20261020` | ordinary folder | 102 MB | prior audit archive (01–06 + zips + manifest) |
| `C:\Localdata\siebwalde-phase1-backup` | ordinary folder | <1 KB | **NOT empty** — unique `modbushooks-remnants/{modbushooks.c,modbushooks.h}` (older snapshot of files the refactor deletes; blobs not present in any branch/commit) |
| `C:\Localdata\siebwalde-reconcile-backup-2026-10-15` | ordinary folder | 0.3 MB | small reconciliation backup |
| `C:\Localdata\Siebwalde-review-integration.zip` | file (4.5 MB) | — | Siebwalde review archive (2026-10-20) |
| `C:\Localdata\Siebwalde-review-PIC32-build.zip` | file (2.1 MB) | — | Siebwalde review archive (2026-10-20) |

### C. Unrelated (out of scope)
`AIDA64`, `bootrec`, `db`, `HarmonyV3`, `Koploper`, `Miscellaneous`, `opencode-cost-analysis*`, `PacketSenderPortable`, `SOLIDWORKS Data`, `SuperSlicer_*`, `temp`, `WinAVR-20100110`, `.pytest_cache`.

---

## 2. Git worktree relationships

- One repository: `C:\Localdata\Siebwalde\.git` (common dir). 23 worktrees listed in `git worktree list`.
- `C:\Localdata\Siebwalde_Backup` is a **separate repository** (different remote, different history).

## 3. Branch / commit preservation map (85 local branches)

- **71 branches are fully merged** into master `5fc2a13b` (ahead = 0). Their commits are safe in master.
- **14 unmerged branches carry unique local-only commits** (must not be deleted until their value is preserved or PO-authorized):
  `SiebwaldeApp-stationcontrol` (9), `baseline/legacy-l0-freeze` (1), `feature/firmware-deployment-authorization` (1, = PR #48 open), `feature/legacy-l1-version-id` (2), `feature/legacy-l2-stage-a-driver` (6), `feature/legacy-l2-version-id` (1), `feature/pic18-single-target-bootloader-recovery` (6, = PR #49 open), `feature/pic18-single-target-flash` (1), `feature/pico-5b-diagnostic` (1), `feature/recover-slaves-count-independence` (8), `feature/safety-stop-reachability` (17), `integration/phase1-firmware-combined` (17), `recovery/phase1-stabilization` (18), `tools/read-controller-version` (1).
- **All three detached HEADs** (`e845b9d2`, `fe17ebe`, `80b7a419`) are reachable from master — their content is safe.
- The **local `master` ref is stale** (`376c56e0`, **55 commits** behind `origin/master` `5fc2a13b`); it needs a fast-forward (non-destructive) after the main checkout is reconciled.

## 4. Main checkout (`C:\Localdata\Siebwalde`) dirty-change classification

The checkout is on `feature/k5-observe-adapter-init-reference` @ `c9260c59` with a mixed dirty state.

| Change | Classification |
| --- | --- |
| **TrackAmplifier4.X firmware refactor** — 26 files, +276 / −3151 lines (deletes `amp_core.c/h`, `amp_model.h`, `modbushooks.c/h`, `version.h`, the entire `tests/` suite; rewrites `main.c`, `General.h`, `PetitModbus.c`, `processio.c`, `regulator.c`), fully **staged**. | **C — Valuable uncommitted source work; NOT in master or any branch → ARCHIVE_REQUIRED** |
| `KiCad/TrackAmplifier2/{TrackAmplifier.pdf, sym-lib-table}` | **C — uncommitted source modification → ARCHIVE_REQUIRED** |
| `docs/analysis-coverage.md`, `docs/handoff.md` | **C — uncommitted docs → ARCHIVE_REQUIRED (or re-derivable)** |
| Untracked `*.hex` (2), `dist/` outputs (97), `Backup projects/*/dist/*` build artifacts | **E — reproducible generated output (low preservation value)** |
| Untracked `.opencode/workflow/active-state.*-archive.json` (11) | **D — unique workflow archives (low value, keep if cheap)** |
| Untracked `other` (107 files) | **F — Unknown; requires categorization before removal** |

**Rule applied:** a staged deletion is NOT an approved deletion. The firmware refactor must be losslessly preserved (e.g., a dedicated archive branch or a byte-exact export) before the checkout is reset to master.

## 5. Unique engineering evidence inventory

- **K6 physical evidence** (`C:\Localdata\Siebwalde-k6\Logging\K6B\`): two files, hashes verified — `k6b-observe-20261024T092944Z.json` = `cf0a5cb0…6450642`, `k6b-preflight.json` = `593770a0…41ef0e9`, response SHA-256 `f40a31ed…669c8`. **Local-only.**
- **TrackAmplifier4.X uncommitted refactor** (see §4) — unique source.
- **PicoScope evidence** (`C:\Localdata\Siebwalde\tools\picoscope\evidence\` — git-ignored): `mcp`, `pico-2`…`pico-5b` captures.
- **`Siebwalde-Audit-20261020`** — prior archive (firmware artifacts + git evidence).
- Firmware HEX/dist artifacts across worktrees (mostly reproducible; exact-byte archival recommended only for the unique rollback images).

## 6. K6 off-machine backup status

**Still OPEN.** No authorized durable destination exists; the raw files remain local-only (verified intact). The committed SHA-256 metadata is not a substitute for the raw bytes. Required operator action remains: copy the two files to an off-machine backup and verify hashes (see `docs/K6-EVIDENCE-MANIFEST.md`).

## 7. OpenCode 2.1.4 / MCP validation

- OpenCode **2.1.4 verified** (`opencode.cmd --version` → `opencode v2.1.4`; npm package `2.1.4`). The `.ps1` shim fails under PowerShell execution policy, but `.cmd` works (environment limitation, not an OpenCode defect).
- **Obsolete launcher present:** `opencode2`, `opencode2.cmd`, `opencode2.ps1` (the old experimental V2) coexist with the 2.1.4 `opencode*`. Not auto-selected; retire only after separate PO approval.
- **MCP config** (`opencode.json`) hardcodes `C:\Localdata\Siebwalde\tools\{picoscope,knowledge,diagnostics}\.venv\Scripts\python.exe`. These point at the **canonical root**, which will be correct once the main checkout is clean on master (the tools are the reviewed merged versions). **No config rewrite needed**; avoid pointing MCPs at any legacy worktree.

## 8. Proposed canonical layout

- `C:\Localdata\Siebwalde` — permanent canonical repository, clean on `master` @ `5fc2a13b` (after §4 preservation).
- One active feature worktree per increment, created on demand.
- Historical accepted commits remain in Git; unique evidence archived in a dedicated archive location (TBD with PO).
- MCP paths continue to point at `C:\Localdata\Siebwalde\tools`.

## 9. Classification (summary; full detail in the cleanup manifest below)

| Category | Directories |
| --- | --- |
| **KEEP_ACTIVE** | `C:\Localdata\Siebwalde` (canonical, after §4 reconciliation) |
| **KEEP_REFERENCE** | `C:\Localdata\Siebwalde_Backup` (independent historical clone, own remote), `C:\Localdata\Siebwalde-Audit-20261020` (prior audit archive) |
| **ARCHIVE_REQUIRED** | `C:\Localdata\Siebwalde` dirty content (TrackAmplifier4.X refactor + KiCad + docs + unique untracked); `C:\Localdata\Siebwalde-k6\Logging\K6B` (K6 evidence); `C:\Localdata\siebwalde-phase1-backup` (unique `modbushooks-remnants` not in Git); `C:\Localdata\Siebwalde Temp if data is missing` (verify uniqueness, then archive or remove); `C:\Localdata\Siebwalde-review-*.zip` (review archives) |
| **REMOVAL_CANDIDATE** | the 21 non-main worktrees (tracked content merged, branches preserved in refs) + `siebwalde-reconcile-backup-2026-10-15` (0.3 MB, verify first) — **subject to ignored/untracked audit below** |
| **INVESTIGATION_REQUIRED** | the 107 `other` untracked files in the main checkout; the ignored/untracked content (firmware artifacts, captures, evidence) of each REMOVAL_CANDIDATE worktree |

**Caution:** a branch being merged does NOT prove a worktree has no unique ignored/untracked content. Every REMOVAL_CANDIDATE worktree must have its `git status --ignored` + untracked files audited before removal.

## 10. Expected disk-space recovery

Total Siebwalde-related footprint ≈ **9.7 GB** (main 3.5 GB + 22 worktrees ~0.34–0.5 GB each + backups 0.5–1.0 GB). Removing the redundant worktrees (excluding the main checkout and the archive-required content) could recover ≈ **6 GB** (each worktree holds a full `.NET` build + firmware artifacts). Exact figure pending the ignored-file audit.

## 11. Cleanup plan (dry-run, non-destructive — nothing executed)

1. **Preserve** the main checkout's dirty content: create a byte-exact export / archive branch of the TrackAmplifier4.X refactor + KiCad + docs + unique untracked files (or copy to an approved archive location), with hash verification.
2. **Archive** the K6 evidence to an approved off-machine location (still pending a destination).
3. **Audit** ignored/untracked content of every REMOVAL_CANDIDATE worktree (a small read-only helper, with dry-run output).
4. **Preserve** any unique local-only commits by pushing the 14 unmerged branches to the remote (or recording their SHAs) before any branch deletion.
5. **Remove** only the verified-redundant worktrees via `git worktree remove` (no `--force`) after their ignored content is confirmed non-unique.
6. **Reconcile** the main checkout to clean `master` (`5fc2a13b`) only after steps 1–5.

**STOP — no deletion or reset has been performed.** A genuine removal requires one consolidated PO approval.

## 12. Independent Integrator verdict

Initial verdict: **FAIL** (one material error — `siebwalde-phase1-backup` was mislabeled "empty"; it contains unique `modbushooks-remnants` not present in any branch/commit; plus the local-master staleness was understated as 9 instead of 55, and two review zips were omitted). **Corrected:** the directory is reclassified to ARCHIVE_REQUIRED, the master count is corrected to 55, and the zips are added. The corrected report is internally consistent; the Integrator's other items (23 worktrees, 14 unmerged branches, dirty-classification, K6 hashes, no deletion, preservation guard exit 0, OpenCode 2.1.4) all verified. A final re-confirmation of the corrected classification is recommended before PO cleanup approval.

## 13. Consolidated PO decision package

The PO is asked to approve, in one decision:
1. **Archive the main-checkout dirty content** (TrackAmplifier4.X refactor + KiCad + docs + unique untracked) before reconciling the checkout to master.
2. **Archive `siebwalde-phase1-backup`** (unique `modbushooks-remnants`) — preserve before any removal.
3. **Designate an off-machine backup destination** for the K6 evidence (and any other local-only evidence).
4. **Approve the REMOVAL_CANDIDATE worktrees** (after the ignored-file audit) for `git worktree remove`.
5. **Approve retiring the obsolete `opencode2` launcher** (after the 2.1.4 replacement is confirmed working).
6. **Approve pushing the 14 unmerged branches** to remote (or accept their local-only preservation) before any local branch cleanup.
7. **Decide on `Siebwalde-review-*.zip`, `Siebwalde Temp if data is missing`, and `siebwalde-reconcile-backup-2026-10-15`** (archive vs remove).
