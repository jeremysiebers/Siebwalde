# Siebwalde — Current State

**Nature of this document.** This is a compact, verified snapshot of the current project state for fast startup by future human and AI sessions. It is not a chat handoff, not a workflow journal, not a complete history, not an Active State Manifest, and not a replacement for architecture or product documentation. See the Source-of-truth note at the end.

## Purpose

Siebwalde is the control application for a model railway. Koploper owns driving behaviour and drives locomotives through the ECoS emulator; the C# application translates ECoS/Koploper commands into track-amplifier setpoints and returns occupancy feedback to Koploper. See `docs/product.md` for the confirmed purpose and scope.

## Current product baseline

- The safety-stop reachability increment is **MERGED / CLOSED** (PR #4, merge commit `3b275fa27c9197400ee40cbfa5759450443535d3`, post-merge CI PASS).
- The merged production baseline includes: DCC28 protocol-speed normalization at the ECoS boundary; logical locomotive direction retention; authoritative TrackAmplifier (`1..50`) vs backplane/configuration (`51..55`) device classification; amplifier-centric safety neutralization (`IAmplifierNeutralizer`, `AmplifierCommandTracker`, fail-honest `SafetyStopResult`); and the dedicated production `ControlTrace` forensic log.
- Do not reopen the closed safety increment. Detailed evidence lives in `docs/handoff.md`, `docs/backlog.md` and `docs/analysis-coverage.md`.

## Current repository baseline

- Repository root: `C:\Localdata\Siebwalde` (Git; `origin https://github.com/jeremysiebers/Siebwalde.git`).
- Verified baseline revision: `f1caa6b838455afc4ac1d9f5d67d534dfc83c016` (branch `master`, equal to `origin/master` at the time of the Phase 1 audit).
- The revision above is a **verified baseline reference**, not a permanently self-updating truth claim; always trust the actual Git state.
- Main solution: `SiebwaldeApp/SiebwaldeApp.sln` (UI, Core, EcosEmu, Integration, Core.Tests). Separate hosts: `SiebwaldeApp.Core.Host.sln`, `SiebwaldeApp.EcosEmu.sln`. Validation harness: `SiebwaldeApp/SiebwaldeApp.StopReachabilityHarness` (not in the solution).
- Immutable evidence branch retained: `feature/safety-stop-reachability` @ `825533e` (historical physical-validation provenance). Reviewer-facing branch: `feature/safety-stop-reachability-clean` @ `dab43ab`.
- The workspace also contains firmware, PCB (KiCad), Python tooling, runtime `Logging/`, and `Backup projects/` (inventoried, not deeply analyzed). Many generated artifacts are untracked.

## Current verification baseline

Executed at the verified baseline revision:

- Debug tests: **343/343 PASS** (`dotnet test SiebwaldeApp.sln`)
- Release tests: **343/343 PASS** (`dotnet test SiebwaldeApp.sln -c Release --no-build`)
- Release build: **0 errors / 175 warnings** (`dotnet build SiebwaldeApp.sln -c Release`)
- `SiebwaldeApp.StopReachabilityHarness` build: **0 errors / 0 warnings**

These results belong to the verified baseline revision; re-run to confirm before relying on them.

## Current agent/workflow baseline

- **Workflow v1 is installed** (`docs/development-workflow.md`, v1.0).
- **Project Lead contract: MIGRATED and fresh-session smoke-verified** (`chore/agent-workflow-v1`). The AI Project Lead is separated from the human Product Owner, owns the workflow, and may autonomously route bounded work to the registered roles within the active autonomy envelope (smallest sufficient role set). The old "work as a single agent by default" / "invoke subagents only when the user explicitly requests delegation" rule is removed.
- **Developer, Architect, Integrator, Designer contracts: MIGRATED to permanent Workflow v1 role contracts** (`chore/agent-workflow-v1`). Each is subordinate to `docs/development-workflow.md`, states the tool-permission-vs-workflow-authority boundary, reports through the Agent Result Contract, keeps `task: deny` (no nested orchestration), and no longer contains temporary phase wording. The Integrator contract preserves the historical hardware/process-safety rules and the independent-review boundary (no fix-and-self-approve). The migration passed an independent governance consistency review.
- **OpenCode permission normalization: IMPLEMENTED and smoke-verified** (`opencode.json`, OpenCode 1.18.30). Project-wide defaults allow routine `edit`/`bash`/`webfetch`/`websearch` work; `task` is a fail-closed allow-list limited to the four registered roles; each subagent keeps `task: deny` and `subagent_depth` is `1`, so nested delegation stays denied. The Project Lead no longer carries `task: "*": ask`.
- **Active State / checkpoint / resume: IMPLEMENTED and verified** (`.opencode/workflow/`, `chore/agent-workflow-v1`). The operational Active State Manifest (`.opencode/workflow/active-state.json`, local and git-ignored) is kept separate from this durable snapshot; a tracked template (`active-state.example.json`) and a small read-only validator/reality-check helper (`active-state-check.ps1`) accompany it. The mechanism passed an independent governance review after a corrective fix.
- **Documentation reading order / current-state alignment: COMPLETED** (`AGENTS.md`, `docs/README.md`, `docs/build-test.md`, `docs/inventory.md`, `docs/project-knowledge.md`).
- **Final Workflow v1 end-to-end eval: PASSED** (E1-E12 semantics and the fresh-session drift reasoning; independent governance review of the full `master..chore/agent-workflow-v1` diff).
- **Branch `chore/agent-workflow-v1`: implements Workflow v1 but is NOT yet merged to `master`.** Workflow v1 becomes current on `master` only after PR/merge authority is granted.
- Consequence: the Workflow v1 bootstrap is complete on the branch; only the fresh-primary-session resume acceptance test and Product Owner PR/merge authority remain before it lands on `master`.

## Important current limitations / known follow-ups

Only currently material items; see `docs/backlog.md` for the full list.

- **Open architecture decision (Product Owner):** complete track-amplifier group/domain configuration (`MainRailway` / `MountainRailway` / `Spare` / `Unassigned`) and the cross-domain emergency policy.
- **Open safety gap (investigate):** startup/restart established-neutral guarantee (C# does not establish/observe neutral before movement; only a partial firmware default exists).
- **Manual `SetAmplifierControl`** is outside locomotive ownership (the strongest neutralization reaches it; a loco-scoped stop does not).
- Software follow-ups: ControlTrace free-text quoting/escaping; build/commit ID in `CONTROL_TRACE_START`; trace registration idempotence; `FileLogger` hardening; synthetic `backplanecheck` / `classify` have no live-mode guard; test-project -> harness dependency.
- Remaining documentation drift: some older domain/historical documents (for example `docs/application-guide.md`, `docs/implementation.md`) still contain pre-Workflow-v1 present-tense statements; broader drift repair is not yet complete.
- **Tool-permission limitation (known):** the `opencode.json` bash ask-guards for destructive/irreversible commands (force push, hard reset, `clean`, history rewrite, interactive rebase, force branch delete) are **best-effort defense-in-depth only**, not a reliable authority boundary — bypasses exist (for example `git -C`, aliases, refspec force, `--interactive`). Workflow v1 remains the authority boundary; the guards merely add friction to the most dangerous forms. `git clean -n` / `--dry-run` currently also triggers the guard.
- Next product direction (not started): autonomous running of the physical 4-amplifier / 4-block test oval with 2 locomotives, driven by Koploper.

## Active development direction

- The immediate work is **governance**: install and activate Workflow v1 (agent contracts, permission normalization, current-state architecture) before resuming product features.
- After governance: the physical test oval (4 amplifiers at addresses 1, 3, 4, 6; 4 blocks; 2 locomotives; no switches) is the near-term target for autonomous Koploper-driven operation.

## Open Product Owner decisions

None currently blocking Workflow v1 bootstrap.

## Source-of-truth note

`docs/current-state.md` is a compact verified snapshot. It does not override: actual Git state; current source code; executed test/runtime evidence; explicit Product Owner decisions; or authoritative domain documentation (`docs/koploper-interface.md`, `docs/architecture.md`, `docs/implementation.md`, `docs/decisions.md`). Where this document and those sources disagree, the more authoritative source wins.
