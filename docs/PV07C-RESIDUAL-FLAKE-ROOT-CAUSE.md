# PV07-C Residual Flake — Root-Cause, Fix & Merge Readiness

**Status:** OFFLINE root-cause investigation + minimal fix complete and independently reviewed. No merge, no flash, no deployment, no hardware.

**Baseline:** branch `feature/pv07c-diagnostic-instrumentation` on `master` `cd047cd9`. Five commits: `0a7d542d` (PIC32 exception recognition), `3a850ac7` (C# Stage-4 pause + diagnostics), `dfd56111` (torn-read fix), `7551e6b2` (PIC32 event-sequence demonstration), `6ea15d3a` (read-order TOCTOU fix — this increment's correction).

> **Correction note.** An earlier internal investigation attributed the residual flake to a "non-monotonic `DateTimeOffset.UtcNow`" and proposed a monotonic-clock fix. Independent review **contradicted** that: the Integrator's microbenchmark showed `UtcNow` is monotonic in this environment (0 backwards reads across 60M+ samples), and that a monotonic clock alone does **not** fix the race (1337 negative-age hits with `Stopwatch` + the same read order). The true root cause and fix are recorded below.

---

## Root cause B — `ConfigureValidate=Failed` (PROVEN: read-order TOCTOU)

`TrackAmplifierDataFreshness.IsFresh` (`SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Control/TrackAmplifierDataFreshness.cs:29-48`) computes `age = now - received`. The callers sample `now = DateTimeOffset.UtcNow` **before** `IsFresh` reads `received` (e.g. `AmplifierPhase1Safety.Compatible` → `HasCurrentProtocolReadback(item, DateTimeOffset.UtcNow, …)`, `AmplifierPhase1Safety.cs:17-20`). The comm thread stamps `received` (`TrackCommClientAsync.cs:300`). A stamp landing in that window makes a genuinely-fresh `received` a few ticks ahead of the caller's `now`, so `age < TimeSpan.Zero` and the guard (`:42-44`) spuriously returns stale → `AmplifierPhase1Safety.Compatible` false → `AmplifierConfiguration` throws `"compatibility changed during configuration"` → `ConfigureValidate=Failed`.

**Evidence:**
- Integrator microbenchmark: negative age reproduced 1121× in now-first order vs **0×** in received-first order vs 1337× with `Stopwatch` + now-first → the cause is read ORDER, not clock non-monotonicity.
- Deterministic regression test `FreshnessPolicy_ConcurrentStamper_ReadOrderRace_IsAlwaysFresh`: fails without the fix (~15 ms), passes with it.
- Throw-site instrumentation (Integrator): every capture re-checked `fresh=True`, `age>0` (0.4–1.3 µs), `identityOk=True`, `protocolOk=True` — the fingerprint of a time-of-check/time-of-use race.

This is a **distinct** defect from the torn-read race that `dfd56111` fixed (a real, separate 24-byte `DateTimeOffset?` torn read). `dfd56111` is still correct; only its "deterministic" claim was premature, because the read-order race remained.

## Root cause A — `RecoveryHold` / `globalStop=Latched` / `ConfigureValidate=Pending` (PROVEN: downstream of B, valid fail-closed reaction)

A is **not** an independent defect. B's config failure makes `AllDomainAmplifiersConfigured` false, so the host takes the `else` branch (`TrackApplicationRuntimeHost.cs:631-643`), which marks stages failed but still proceeds to `StartRuntime` (`:650`) → `EstablishObservedNeutralAsync` (`:657`) → `TransitionTo(Running)` (`:678`) → `_globalStopMonitor.EstablishLink` (`:679`). `EstablishLink` activates the `Pic32Lost` receive-freshness watch (`GlobalStopMonitor.cs:92-99`); the same read-order negative-age then makes `freshReadback` false → `Pic32Lost` latch → `EnterRecoveryHold` resets stages to `Pending` (`TrackInitStateMachine.cs:899-950`).

**Classification:** signature A = **(4) valid fail-closed safety reaction** (`Pic32Lost`) triggered by B's (1) real defect via the "proceed to `Running` despite Stage-4 failure" path. The pause gate (`TrackApplicationRuntimeHost.cs:605-613`) is inside the `if (allReady)` success branch, so it is skipped when config fails — hence "pause never reached".

## Fix — commit `6ea15d3a`

Minimal single-method change to `IsFresh`: re-sample `now` after `received` when `now < received`:

```csharp
if (now < received)
{
    now = DateTimeOffset.UtcNow;
}
var age = now - received;
// age < TimeSpan.Zero -> false  (fail-closed guard UNCHANGED)
```

This corrects only false-stale (sub-µs read-order race); it can never make genuinely-stale data look fresh. The `age < TimeSpan.Zero` fail-closed guard, the 2 s `DefaultStaleAfter` window, the Fresh/Cached/Stale classifier, and every caller are **unchanged** (a genuinely future/backward-clock timestamp is still rejected — covered by `FreshnessPolicy_ResampleRejectsGenuinelyFutureTimestamp`).

**Regression tests added (3):** `FreshnessPolicy_ConcurrentStamper_ReadOrderRace_IsAlwaysFresh` (deterministic reproduction of the exact `AmplifierPhase1Safety.Compatible` read order; fails without the fix), `FreshnessPolicy_ResampleAcceptsSubMicrosecondAheadTimestamp`, `FreshnessPolicy_ResampleRejectsGenuinelyFutureTimestamp`.

## Validation (after fix)

| Check | Result |
|---|---|
| `dotnet build …Core.Tests.csproj -c Debug` | **0 errors** (pre-existing warnings only) |
| `Stage4DiagnosticPauseTests` ×10 (Developer) | **21/21 every run** |
| `Stage4DiagnosticPauseTests` ×3 (Integrator, independent) | **21/21 every run** |
| `DeterministicTrackTransportEndToEndTests` ×2 | **18/18** |
| Heartbeat regression (2 classes) | **4/4** |
| Global-stop / RecoveryHold classes (6) | **40/40** |
| Full C# suite | **1243/1243 PASS** |
| `TrackAmplifierOccupancyProviderTests` (incl. 3 new) | **25/25** |
| PIC32 `test_global_stop.py` | **27 PASS** (firmware untouched) |

Regression proof (Integrator): PASS (fix) → FAIL (reverted, exact root-cause message) → PASS (restored).

## Independent review verdict

**Integrator: PASS.** Fix is correct, minimal, and safety-preserving (`TrackAmplifierDataFreshness.cs:39-47`); fail-closed guard and 2 s window/classifier unchanged; regression test deterministically catches the root cause. All 10 PO-brief §7 questions **CONFIRMED** (Q2 corrected to read-order TOCTOU). Final 5-commit diff safety audit clean: no heartbeat/lease/safety change; one-shot GROUP_READY control-layer enforced; no Stage 6 via DiagnosticMode; no auto-REARM/retry; StopAsync fail-closed; heartbeat active during pause; the two pre-existing dirty files (`AmplifierPhase1MapTests.cs`, `phase1_cases.h`) absent from all commits.

## Remaining risks

- Signature A is confirmed at the mechanism level (log-confirmed chain), but the per-amplifier freshness snapshot at the exact `Pic32Lost` latch instant was not independently re-captured after the fix (it is now deterministic under the fix, so this is low risk).
- Offline only — no hardware/physical/deployed verification.
- The `dfd56111` commit message's "deterministic" claim is historically inaccurate (the read-order race remained until `6ea15d3a`); noted, not blocking.

---

## MERGE recommendation: **GO (advisory)** — final authority with the Product Owner

The five commits are correct, the residual flake root cause is now proven and fixed, and the fix + tests are independently reviewed. No safety semantics were weakened. A separate PO decision is still required before any merge, flash, deployment, or PV07-B live-hardware work.

**Not claimed:** runtime-verified, physically accepted, or deployed.
