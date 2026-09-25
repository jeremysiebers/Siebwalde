# Siebwalde — Current State

**Nature of this document.** This is a compact, verified snapshot of the current project state for fast startup by future human and AI sessions. It is not a chat handoff, not a workflow journal, not a complete history, not an Active State Manifest, and not a replacement for architecture or product documentation. See the Source-of-truth note at the end.

## Purpose

Siebwalde is the control application for a model railway. Koploper owns driving behaviour and drives locomotives through the ECoS emulator; the C# application translates ECoS/Koploper commands into track-amplifier setpoints and returns occupancy feedback to Koploper. See `docs/product.md` for the confirmed purpose and scope.

## Current product baseline

- The safety-stop reachability increment is **MERGED / CLOSED** (PR #4, merge commit `3b275fa27c9197400ee40cbfa5759450443535d3`, post-merge CI PASS).
- The merged production baseline includes: DCC28 protocol-speed normalization at the ECoS boundary; logical locomotive direction retention; authoritative TrackAmplifier (`1..50`) vs backplane/configuration (`51..55`) device classification; amplifier-centric safety neutralization (`IAmplifierNeutralizer`, `AmplifierCommandTracker`, fail-honest `SafetyStopResult`); and the dedicated production `ControlTrace` forensic log.
- The ECoS emulator graceful-shutdown increment is **MERGED / CLOSED** (PR #8, merge commit `5fb751552b368015ba17f796d86f41c1d7fa7c13`, post-merge CI PASS). `EcosEmulatorServer`, `KoploperExternalInfoClient` and `TrackSimulatorBackend` now stop deterministically (idempotent, bounded `StopAsync` with tracked+awaited tasks and disposed listener/connection, plus a non-blocking sync `Stop` for the in-process host). Recorded as the completed foundation for a future Recovery & Maintenance System (see `docs/backlog.md`).
- The Recovery & Maintenance Increment 1 (controllable track runtime from WPF) is **MERGED / CLOSED** (PR #9, merge commit `a41302a42db62d1c23e98d1205a8189c0033b5d3`, post-merge CI PASS): a single in-process coordinator (`TrackApplicationRuntimeHost` behind Core `ITrackApplicationRuntime`) owns the track-control runtime lifecycle (`Stopped/Starting/Running/Stopping/Failed`), with a provable graceful stop, shared start/restart path, and failure semantics. Its B-classification limitation (no simulator `ITrackTransport`) was later closed by PR #10.
- The software-only simulator transport is **MERGED / CLOSED** (PR #10, merge commit `33a231a01b5aff4aa4a99e162b69d723a778d31b`, post-merge CI PASS): `DeterministicTrackTransport` (Core `TrackApplication.Simulator`) emulates the PIC32 master protocol so the full track runtime (comm → 9-step init → `TrackControlMain`) runs end-to-end software-only. This is software/protocol simulation evidence only — not proof of physical PWM/timing/hardware behaviour.
- Do not reopen the closed safety increment. Detailed evidence lives in `docs/handoff.md`, `docs/backlog.md` and `docs/analysis-coverage.md`.

## Current repository baseline

- Repository root: `C:\Localdata\Siebwalde` (Git; `origin https://github.com/jeremysiebers/Siebwalde.git`).
- Current `master` HEAD: `33a231a01b5aff4aa4a99e162b69d723a778d31b` (equal to `origin/master`). This is a normal merge commit for PR #10 on top of `a41302a` (PR #9, Recovery & Maintenance Increment 1); earlier baselines: `5fb7515` (PR #8), `0cf6347` (PR #7), `ec990bc` (PR #6), `a6b3467` (PR #5).
- The revision above is a **verified baseline reference**, not a permanently self-updating truth claim; always trust the actual Git state.
- Main solution: `SiebwaldeApp/SiebwaldeApp.sln` (UI, Core, EcosEmu, Integration, Core.Tests). Separate hosts: `SiebwaldeApp.Core.Host.sln`, `SiebwaldeApp.EcosEmu.sln`. Validation harness: `SiebwaldeApp/SiebwaldeApp.StopReachabilityHarness` (not in the solution).
- Immutable evidence branch retained: `feature/safety-stop-reachability` @ `825533e` (historical physical-validation provenance). Reviewer-facing branch: `feature/safety-stop-reachability-clean` @ `dab43ab`.
- The workspace also contains firmware, PCB (KiCad), Python tooling, runtime `Logging/`, and `Backup projects/` (inventoried, not deeply analyzed). Many generated artifacts are untracked.

## Current verification baseline

Executed at the verified baseline revision:

- Debug tests: **388/388 PASS** (`dotnet test SiebwaldeApp.sln`)
- Release tests: **388/388 PASS** (`dotnet test SiebwaldeApp.sln -c Release --no-build`)
- Release build: **0 errors / 175 warnings** (`dotnet build SiebwaldeApp.sln -c Release`)
- `SiebwaldeApp.StopReachabilityHarness` build: **0 errors / 0 warnings**

These results belong to the verified baseline revision; re-run to confirm before relying on them.

## Current agent/workflow baseline

- **Workflow v1 is ACTIVE ON MASTER** (`docs/development-workflow.md`, v1.0). Merged via PR #5 (`Introduce Siebwalde Development Workflow v1`), merge commit `a6b3467aca22e2f8be79c1133a5509f351c78135`, post-merge CI PASS.
- **Project Lead contract: migrated and verified.** The AI Project Lead is separated from the human Product Owner, owns the workflow, and may autonomously route bounded work to the registered roles within the active autonomy envelope (smallest sufficient role set). The old "work as a single agent by default" / "invoke subagents only when the user explicitly requests delegation" rule is removed.
- **Developer, Architect, Integrator, Designer contracts: migrated to permanent Workflow v1 role contracts.** Each is subordinate to `docs/development-workflow.md`, states the tool-permission-vs-workflow-authority boundary, reports through the Agent Result Contract, keeps `task: deny` (no nested orchestration), and no longer contains temporary phase wording. The Integrator contract preserves the historical hardware/process-safety rules and the independent-review boundary (no fix-and-self-approve). The migration passed an independent governance consistency review.
- **OpenCode permission normalization: implemented and verified** (`opencode.json`, OpenCode 1.18.30). Project-wide defaults allow routine `edit`/`bash`/`webfetch`/`websearch` work; `task` is a fail-closed allow-list limited to the four registered roles; each subagent keeps `task: deny` and `subagent_depth` is `1`, so nested delegation stays denied. The Project Lead no longer carries `task: "*": ask`.
- **Active State / checkpoint / resume: implemented and verified** (`.opencode/workflow/`). The operational Active State Manifest (`.opencode/workflow/active-state.json`, local and git-ignored) is kept separate from this durable snapshot; a tracked template (`active-state.example.json`) and a small read-only validator/reality-check helper (`active-state-check.ps1`) accompany it. The mechanism passed an independent governance review after a corrective fix.
- **Documentation reading order / current-state alignment: completed** (`AGENTS.md`, `docs/README.md`, `docs/build-test.md`, `docs/inventory.md`, `docs/project-knowledge.md`).
- **Fresh-primary-session resume acceptance test: PASS** (2026-09-22): manifest loaded, Git branch/HEAD reality verified, validator returned `CONSISTENT` (exit 0), increment recovered correctly without chat handoff, and revision-mismatch reasoning produced `STATE_DRIFT` semantics rather than blindly trusting the manifest.
- **Final Workflow v1 end-to-end eval: PASSED** (E1-E12 semantics and the fresh-session drift reasoning; independent governance review of the full `master..chore/agent-workflow-v1` diff).

## Important current limitations / known follow-ups

Only currently material items; see `docs/backlog.md` for the full list.

- **Open architecture decision (Product Owner):** complete track-amplifier group/domain configuration (`MainRailway` / `MountainRailway` / `Spare` / `Unassigned`) and the cross-domain emergency policy.
- **Open safety gap (investigate):** startup/restart established-neutral guarantee (C# does not establish/observe neutral before movement; only a partial firmware default exists). Now software-addressable via the deterministic simulator transport; the next Recovery & Maintenance increment targets its software half (see `docs/recovery-maintenance-system.md` §8).
- **Manual `SetAmplifierControl`** is outside locomotive ownership (the strongest neutralization reaches it; a loco-scoped stop does not).
- Software follow-ups: ControlTrace free-text quoting/escaping; build/commit ID in `CONTROL_TRACE_START`; trace registration idempotence; `FileLogger` hardening; synthetic `backplanecheck` / `classify` have no live-mode guard; test-project -> harness dependency.
- Remaining documentation drift: some older domain/historical documents (for example `docs/application-guide.md`, `docs/implementation.md`) still contain pre-Workflow-v1 present-tense statements; broader drift repair is not yet complete.
- **Tool-permission limitation (known):** the `opencode.json` bash ask-guards for destructive/irreversible commands (force push, hard reset, `clean`, history rewrite, interactive rebase, force branch delete) are **best-effort defense-in-depth only**, not a reliable authority boundary — bypasses exist (for example `git -C`, aliases, refspec force, `--interactive`). Workflow v1 remains the authority boundary; the guards merely add friction to the most dangerous forms. `git clean -n` / `--dry-run` currently also triggers the guard.
- Next product direction (not started): autonomous running of the physical 4-amplifier / 4-block test oval with 2 locomotives, driven by Koploper.

## Active development direction

- The **Recovery & Maintenance System** is the active direction (see `docs/recovery-maintenance-system.md`). Completed: Increment 1 (controllable runtime from WPF, PR #9) and the software-only simulator transport (PR #10). Next proposed increment: "Observed-neutral restart/stop safety — software half" (gated by Product Owner decisions 1, 2 and 5 in `docs/recovery-maintenance-system.md` §5/§8).
- The physical test oval autonomous running remains a separate, not-yet-started product direction.

## Open Product Owner decisions

- Recovery & Maintenance: see `docs/recovery-maintenance-system.md` §5 (decisions 1–5). Decisions **1** (amplifier group/domain), **2** (observed-vs-commanded neutral bar) and **5** (stop semantics while moving) gate the next increment (§8).

## Source-of-truth note

`docs/current-state.md` is a compact verified snapshot. It does not override: actual Git state; current source code; executed test/runtime evidence; explicit Product Owner decisions; or authoritative domain documentation (`docs/koploper-interface.md`, `docs/architecture.md`, `docs/implementation.md`, `docs/decisions.md`). Where this document and those sources disagree, the more authoritative source wins.
