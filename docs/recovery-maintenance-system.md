# Recovery & Maintenance System — Feature Brief, Architecture & Roadmap

**Status:** Analysis / proposal. Analysis-only; no product code or firmware was changed.
**Prepared:** 2026-09-23, under Siebwalde Development Workflow v1 (autonomy: ANALYSIS).
**Audience:** Product Owner (decisions), Project Lead, Architect, Developer, Integrator.

This document records the Product Owner's intent as a durable feature brief, the proposed architecture, the development roadmap, and the open decisions. It deliberately separates three kinds of statement, because they have different authority:

- **CONFIRMED PRODUCT REQUIREMENTS** — Product Owner intent; not redefinable here.
- **TECHNICAL DESIGN PROPOSAL** — the Project Lead / Architect's proposed route to satisfy those requirements; reviewable and changeable.
- **OPEN PRODUCT OWNER DECISIONS** — decisions only the Product Owner can make.

---

## 1. Confirmed product requirements

Captured from the Product Owner assignment (2026-09-23). The goal is to be able to intervene at several levels from the WPF application when something goes wrong, without restarting the whole application every time.

### 1.1 Operator recovery

When a train derails, loses wagons, or stops in the wrong place, the operator must be able to recover:

- temporarily drive a locomotive manually from WPF (including a slow reverse);
- operate switches when necessary;
- Koploper and manual control must **never** give conflicting commands;
- after the physical fix the operator restores the correct situation in Koploper, hands control back, and resumes automatic traffic from Koploper.

Investigate what Koploper actually offers for this and what Siebwalde must provide itself. Do **not** assume unproven Koploper capabilities.

### 1.2 Software recovery

Stop and restart individual C# components, and also the complete C# track-control runtime, when necessary. The WPF application must stay available as much as possible.

Investigate how to bring the existing `TrackControlHost`, `TrackControlMain`, the ECoS emulator, the external connections and the backends under one common lifecycle. PR #8 already added shutdown functionality; use it as a starting point, but investigate its real guarantees and limitations.

### 1.3 Communication and hardware recovery (later)

Intervene later for communication problems with the PIC32 controller, a stuck physical controller, and faults in individual track amplifiers. Firmware development and testing of new firmware must eventually be supported through a controlled **Maintenance Mode**. Not every fault must lead to a full system reset.

### 1.4 Safety and control transfer

A software restart must **never** automatically mean trains may move again. Investigate how to prevent:

- old commands from still being executed;
- Koploper and WPF driving the same parts at the same time;
- unintended non-neutral setpoints after a restart.

Include the existing open startup/restart-neutralization question and the yet-to-be-decided amplifier groups and safety domains in the architecture. Make explicit which product decisions the Product Owner still has to take.

### 1.5 First development goal

Start with an independently controllable C# track-control runtime that can be stopped, started and restarted from WPF. Design it so operator recovery, hardware recovery and maintenance can be added later without rebuilding the whole architecture. The first implementation increment must preferably be fully testable in simulator mode. A separately controllable runtime does **not** automatically mean a separate Windows process — investigate what separation is actually needed.

---

## 2. Verified facts the design builds on

These are confirmed from source or prior validated work; the design must not silently contradict them.

### 2.1 Current lifecycle (source-verified)

- `SiebwaldeApplicationModel.StartTrackApplication()` (Core) composes the track runtime: `RawUdpTransport` → `RawUdpTrackTransport` → `TrackCommClientAsync` → initialization steps → `TrackAmplifierInitializationServiceAsync` → `TrackControlMain`; starts communication (`StartAsync(true, ct)`), runs init (`InitializeAsync(ct)`), then starts the ECoS host in Real mode via `_ecosHost.StartAsync(Real, commClient, variables, ct)`.
- `StopTrackApplication()` is **synchronous** and blocks: stops the ECoS host, `TrackControlMain.StopRuntime()`, cancels `_appCts`, then `asyncDisposable.DisposeAsync().AsTask().Wait()` (a TODO in the source admits this should be async). It nulls the comm client, init service, variables and control main.
- `TrackControlHost` (Integration) implements `IEcosHostService` and owns the ECoS server (port 15471), the Koploper external-info client (5700), the backend, and the simulator/real hardware backend. `StartAsync(mode, commClient, variables, ct)`; `Stop()` is still **synchronous**.
- PR #8 added graceful, idempotent, bounded `StopAsync` to `EcosEmulatorServer`, `KoploperExternalInfoClient` and `TrackSimulatorBackend` (tracked + awaited tasks, disposed listener/connection, restartable), but `IEcosHostService.Stop()` remained sync.
- `TrackControlMain`: a 10 Hz timer over `TrackApplicationVariables.PendingWrites`; `StartRuntime(ct)` / `StopRuntime()`.
- `TrackCommClientAsync`: `StartAsync(realHardwareMode, ct)` / `StopAsync(ct)`, `IAsyncDisposable`, 100 ms publish timer + receive loop.
- WPF today only starts (init page `InitTrackController` → `StartTrackApplication`); there is no wired stop/restart control.

### 2.2 Safety (source-verified)

- `ControlSafetyInterlockBackend` is the single movement gate; `ControlSafetyGuard` latches faults; `DivergenceChecker`; recovery is explicit reset only.
- Manual `SetAmplifierControl` (WPF) is currently **outside** locomotive ownership — it writes `TrackApplicationVariables.SetDesiredAmplifierControl` directly and is not recorded in `AmplifierCommandTracker` (known gap).
- `SimpleEcosBackend` owns logical locomotive speed/direction; physical application is best-effort through `IHardwareBackend`.
- Open items: startup/restart neutral guarantee is not established or observed by C# before movement; track-amplifier group/domain configuration (`MainRailway`/`MountainRailway`/`Spare`/`Unassigned`) and the cross-domain emergency policy are open Product Owner decisions.

### 2.3 Koploper / ECoS (only these are verified)

- Koploper owns driving; it connects **to C#** on 15471 (ECoS commands `set/get/queryObjects/request/release/create/delete`; drives via `set(id, speedstep[n])` / `set(id, dir[n])`). C# connects **to Koploper** on 5700 (position records `[EXT] Loc -> Block`; current block only, no destination/route).
- Occupancy reaches Koploper as ECoS sensor events on feedback module 100.
- Observed Koploper UI behaviour: a direction change requires speed 0 first; a locomotive cannot be removed while driving.
- **There is no verified Koploper pause/manual-control/hand-back API.** Any control-transfer mechanism must be expressed only in terms of what Koploper actually does (stop, occupancy, speed 0), not invented capabilities.

---

## 3. Technical design proposal

### 3.1 One runtime coordinator owns composition and lifetime

Today composition is split between `SiebwaldeApplicationModel` (Core, track part) and `TrackControlHost` (Integration, ECoS part), with no shared runtime state. Proposal:

- Introduce a single coordinator — **`TrackApplicationRuntimeHost`** in `SiebwaldeApp.Integration` — that composes **both** the Core track part and the `TrackControlHost`, and owns the `CancellationTokenSource` and the runtime state machine.
- Core defines the contract so dependency direction is preserved (Core has no project references): a new Core enum **`TrackRuntimeState`** and a new Core interface **`ITrackApplicationRuntime`** (start / stop / restart / recover + state/health observability + the track surface that today lives on `SiebwaldeApplicationModel`: `TrackAmplifiers`, `SetAmplifierControl`, `ActiveEcosMode`, `ControlDiagnostics`, `IsControlPathUnsafe`, `ResetControlSafety`).
- `SiebwaldeApplicationModel` keeps Fiddle Yard and becomes a thin facade over `ITrackApplicationRuntime` (or the view model talks to the runtime directly). WPF `IoC.Setup` constructs `TrackApplicationRuntimeHost` instead of `TrackControlHost` directly.

### 3.2 Runtime state machine

Runtime states (distinct from Workflow v1 workflow states):

```
Stopped -> Starting -> Running <-> Degraded
                       Running  -> Stopping -> Stopped
                       Running  -> Recovering -> Running (or Failed)
```

- `Stopped` — no track runtime objects; no comm; no ECoS host.
- `Starting` — composing transport/comm/init/control + ECoS host; init pipeline running.
- `Running` — init `Completed`; `TrackControlMain` runtime loop active; ECoS host serving in the requested mode.
- `Degraded` — `Running` with a non-safety fault (e.g. a silent amplifier → occupancy `unknown`). Movement is still gated by the existing safety machinery. "Unsafe" is **not** a separate runtime state: the latched safety condition (`ControlSafetyGuard` / `ControlDiagnostics.IsUnsafe`) is orthogonal and already surfaced.
- `Stopping` — cancel + await + dispose in progress. A disposal that exceeds its bounded timeout (or throws) transitions to `Failed`.
- `Recovering` — a bounded component-level recovery (e.g. re-init after a comm drop), returning to `Running` or `Failed`.
- `Failed` — a fatal start/stop/restart/recovery failure; explicit operator restart required.

Operations: `StartAsync(mode, ct)`, `StopAsync(ct)`, `RestartAsync(mode, ct)`, `RecoverAsync(ct)`, plus the existing `ResetSafety()`.

### 3.3 Making the stop path async

- Add `Task StopAsync(CancellationToken ct = default)` to `IEcosHostService` (additive). `TrackControlHost.StopAsync` awaits the graceful stops PR #8 added to its parts, with a bounded timeout, then nulls fields. The sync `Stop()` remains only as a non-blocking, deadlock-free wrapper for the UI/mode-transition path.
- The coordinator's `StopAsync` awaits `TrackCommClientAsync.DisposeAsync()` instead of `.Wait()`.
- All start/stop/restart entry points are `async Task`, never blocking `.Wait()` on the UI thread (a `.Wait()` on the `DispatcherSynchronizationContext` is a deadlock hazard).

### 3.4 Control transfer (Koploper ↔ manual)

C# **is** the ECoS command station (Koploper connects to C# on 15471). The control-transfer boundary therefore lives entirely in code Siebwalde controls; this is the only sound place to guarantee "no two sources command the same part", and it requires no invented Koploper capability.

- **Single control-source arbiter** (`ControlSourceArbiter`, Integration): every physical command target has exactly one owner — `Koploper` or `Manual` — at locomotive (decoder address) and switch (ECoS address) granularity.
- Every movement command — Koploper-originated (already `SimpleEcosBackend` → hardware backend) **and** manual — passes through the arbiter **and** `ControlSafetyInterlockBackend`. The non-owner's command for a target is refused (not merged): a Koploper command for a `Manual`-owned target gets a non-OK reply and no state change; a manual command for a `Koploper`-owned target is rejected and traced.
- This closes the existing gap: `SetAmplifierControl` is re-routed through the arbiter + hardware backend, so manual control is recorded under an explicit non-loco `Manual` owner at the shared choke point instead of bypassing ownership/safety.

**Taking manual control (verified semantics only):** (1) stop the loco to speed 0; (2) the arbiter marks it `Manual`, after which C# masks Koploper's `set(...speedstep/speed/dir)` for it at the ECoS layer (refuse with non-OK + no event + trace) and accepts manual commands; (3) manual slow-reverse is a normal manual `SetLocoSpeed` through the arbiter → safety interlock → hardware backend. For switches, the arbiter refuses a manual switch command that conflicts with a live Koploper route, and vice versa.

**Hand-back (no verified Koploper push API):** (1) operator fixes the physical issue; (2) operator re-establishes the loco state **in Koploper** (drag onto block, speed 0); (3) operator returns the target to `Koploper` ownership in WPF; (4) C# reconciles its retained logical state with Koploper's next commands and must **not** auto-issue any non-neutral setpoint on hand-back.

**Explicitly NOT proven in Koploper:** no pause/manual/hand-back/reserve API; C# cannot push restored state into Koploper; masking a Koploper-owned-then-manual loco risks Koploper's internal model diverging from reality — mitigated by refusing (non-OK) rather than silently acknowledging masked movement, but this must be live-validated before reliance.

### 3.5 Error handling and degraded states

| Fault class | Examples | Runtime effect | Recovery |
| --- | --- | --- | --- |
| SafetyFault (latched) | route/switch mismatch, occupancy mismatch, backend unavailable | stays `Running`; `IsUnsafe`; movement gated | explicit `ResetSafety()` (revalidates; refused while unresolved) |
| ComponentFault (non-safety) | silent amplifier → occupancy `unknown`, comm re-connect | `Degraded`; movement constrained by existing safety | `RecoverAsync` or `RestartAsync` |
| CommunicationLoss | master/Koploper/PIC32 drop | later increment; `Degraded`, master loss drives stop | later increment |
| HardwareFault | amplifier over-current/over-temp, stuck controller | later increment | Maintenance Mode (later) |

Degradation is graceful by construction: the existing safety layer already treats unknown/stale occupancy as "never clear", and latched faults never clear themselves.

### 3.6 Safety boundaries (restart must be movement-safe)

1. **Cancellation first** — `StopAsync` cancels the coordinator CTS, then awaits every loop with a bounded timeout (comm receive + publish timer, `TrackControlMain` timer, ECoS server accept + handlers, external-info loop, simulator loop). No loop survives a stop.
2. **Disposal + re-creation** — transport, comm client, init service, control main and ECoS host are disposed and nulled; a fresh `TrackApplicationVariables` (and thus a fresh `PendingWrites`) is created on the next start. No object survives a restart, so no stale command or timer can execute.
3. **Pending-write reset** — `PendingWrites` is recreated with the new variables instance, so no pre-restart setpoint is re-emitted.
4. **Neutral re-establishment (closes the open gap)** — before movement is enabled after a **real-mode** start/restart, C# must command neutral `399` to every configured legitimate track amplifier **and observe** neutral from fresh amplifier frames before any non-zero movement may pass. This is the existing startup/restart-neutralization gap.
5. **Single movement gate preserved** — `ControlSafetyInterlockBackend` remains the one gate; manual control must not bypass it.

**Two things gate real-hardware restart and must be resolved first:** (i) neutral observation needs a per-amplifier HR0 readback from **fresh SLAVEINFO frames** — the real prerequisite is the **commanded-vs-observed distinction** (reading the fresh frame value, not the locally-mutated in-memory image that `SetAmplifierPwm`/`SetAmplifierEmStop`/init mutate) plus confirmation that the firmware actually echoes physical PWM in the SLAVEINFO HR0 field; the `TrackApplicationVariables` HoldingReg aliasing (all 56 `trackAmpItems` share one initial array) is a legitimate correctness cleanup but does not persist past the first SLAVEINFO frame and is **not** the hard blocker. (ii) the amplifier group/domain configuration + cross-domain policy must be decided (see decisions 1 and 2).

### 3.7 In-process vs out-of-process

**Recommendation: in-process for now; no separate Windows process in the first increment.** Safe stop/start/restart is an in-process coordination problem (after PR #8 every loop owner already has a graceful `StopAsync`); in-process restart already keeps WPF alive. Out-of-process isolation is a hardening concern, not the current blocker.

Keep a clean seam for later: the coordinator sits behind the narrow Core `ITrackApplicationRuntime` with no WPF types; observability crosses the boundary as events/snapshots rather than the shared mutable `TrackApplicationVariables`; all inter-component I/O is already network-based (UDP to the master, TCP loopback 15471/5700). Re-evaluate the process split at the Maintenance Mode / firmware-flash increment.

---

## 4. Development roadmap

Each increment is independently mergeable and (where possible) software-tested. After PR #10 (the deterministic simulator transport), the roadmap is re-cut along the **software/protocol boundary vs physical boundary** (see §8): the C# neutralization/readback/stale-command logic can be built and software-proven first, and only the physical-fidelity half stays V4.

1. **(DONE) Increment 1** — stop/start/restart of the C# track-control runtime from WPF (software-only, simulator mode). See §7.
2. **(DONE) Simulator transport (PR #10)** — `DeterministicTrackTransport` + end-to-end tests, so the full track runtime (comm → 9-step init → `TrackControlMain`) runs software-only.
3. **Increment 2 (next, software-first, V1/V2)** — "Observed-neutral restart/stop safety — software half": neutral command on Stop and on Start/Restart; observed-neutral movement gate (fresh HR0 readback == 399); stale-command proof; failure state when neutral cannot be established. Proven against `DeterministicTrackTransport`. Gated by PO decisions 1, 2 and 5. Detailed in §8.
4. **Increment 3 (V4 only)** — real-hardware restart/stop validation of Increment 2's software: confirm the physical-fidelity questions (does SLAVEINFO HR0 reflect physical PWM vs command echo; timing; hardware failure modes).
5. **Increment 4** — control-source arbiter + **manual control path** (operator recovery): manual loco (incl. slow reverse) + switch control through the arbiter + safety interlock; `SetAmplifierControl` moved under `Manual` ownership. Validation V2 (simulator) then V4.
6. **Increment 5** — **hand-back** workflow + WPF operator surface. Depends on decision 3; live Koploper validation.
7. **Increment 6 (later)** — communication/hardware recovery + controlled **Maintenance Mode** (firmware dev/test), `FIRMWARE_FLASH`-gated; re-evaluate out-of-process split here.

### 4.1 Increment 1 — first proposed implementation (software-only, simulator)

**Objective.** Introduce the runtime state machine + a fully async stop/start/restart, wired to WPF, exercising the whole track-control runtime (transport → comm → init → control main → ECoS host) in **simulator** mode only. No real hardware, no firmware.

**Scope.**
- Add Core `TrackRuntimeState` + `ITrackApplicationRuntime` (start/stop/restart/recover + state/health observability).
- Add `IEcosHostService.StopAsync(ct)`; implement in `TrackControlHost` (await the PR #8 `StopAsync` methods, bounded); keep sync `Stop()` as the non-blocking wrapper.
- Introduce the Integration coordinator `TrackApplicationRuntimeHost` (single composition root) that composes the Core track part + `TrackControlHost` and owns the CTS; remove the blocking `.Wait()`.
- Wire WPF init-page **Start / Stop / Restart** commands to the coordinator; surface `TrackRuntimeState` on the page.
- Simulator start path composes transport/comm/init/control in simulator mode and starts the ECoS host in `TrackControlMode.Simulator`.

**Acceptance criteria (simulator-testable, V1/V2).**
- `Start`: `Stopped → Starting → Running`; ECoS host simulator active on loopback 15471, `Mode == Simulator`.
- `Stop`: `Running → Stopping → Stopped`; port 15471 released; all background tasks cancelled and awaited; no exceptions; idempotent.
- `Restart`: returns to `Running` after a full `Stopped` with **fresh** `TrackApplicationVariables`/`PendingWrites`; no stale write or timer survives; Koploper (or a scripted client) can reconnect and be served.
- WPF remains responsive throughout (no UI-thread `.Wait()`, no deadlock).
- State transitions covered by unit tests (`TrackRuntimeState` transition table) + an integration test that starts/stops/restarts and asserts port release + re-bind.
- Existing baseline stays green (Debug/Release suites).

**Explicitly NOT covered:** real-mode restart, neutralization observation, amplifier group/domain policy, manual control, arbiter, hand-back, communication/hardware recovery, Maintenance Mode, out-of-process hosting, Fiddle Yard, firmware.

**Review/validation (advisory):** R2 (lifecycle/startup/shutdown/restart + cross-component) / V1–V2 (build + simulator/harness).

---

## 5. Open Product Owner decisions

1. **Amplifier group/domain configuration + cross-domain emergency/restart policy** (already open).
   - Options: (a) define full `MainRailway`/`MountainRailway`/`Spare`/`Unassigned` config + per-domain restart/neutralization now; (b) treat all configured legitimate track amplifiers (`1..50`) as one domain for restart safety now, defer domain semantics.
   - Recommendation: (b) for the first real-restart increment — neutralize + observe all legitimate track amplifiers, identical to the existing strongest-emergency target set; do not infer domains from address ranges.

2. **Neutralization "observed vs commanded" bar** for enabling movement after restart.
   - Options: (a) require **observed** neutral (fresh HR0 readback = 399) before movement; (b) accept **commanded** neutral + fresh comm as sufficient (documented accepted risk).
   - Recommendation: (a) observed — "commanded but not observed" is exactly what prior validation showed is insufficient.

3. **Control hand-back mechanism** (no verified Koploper hand-back API).
   - Options: (a) operator restores Koploper state in the Koploper UI, then C# flips ownership back; (b) C# scripts Koploper restoration via ECoS commands (unverified, divergence-prone).
   - Recommendation: (a).

4. **Manual-control ownership granularity**.
   - Options: (a) per-loco + per-switch ownership (finer, more complex); (b) a coarse global "Manual mode" that suspends all Koploper driving.
   - Recommendation: (a) as the target; start the first manual increment with (b) if per-target masking cannot yet be live-validated.

5. **Stop semantics while trains are moving** (Stop/Restart during Koploper driving).
   - Options: (a) always issue neutral/power-off on stop (safe default, may surprise Koploper); (b) refuse Stop/Restart while any Koploper-driven loco has non-zero speed.
   - Recommendation: (a) neutral-command on stop, expressed via the existing ECoS stop/power-off path.

---

## 6. Risks / unknowns

- No verified Koploper pause/hand-back API → control transfer relies on stop + occupancy + C#-side command masking; must be live-validated.
- Neutral observation depends on confirming amplifier HR0 readback semantics on real hardware (does SLAVEINFO HR0 reflect physical PWM or a command echo) — the V4 question. (The `TrackApplicationVariables` HoldingReg aliasing noted earlier is no longer present: `TrackAmplifierItem.HoldingReg` copies on assign and `TrackCommClientAsync.HandleNewData` assigns a fresh per-frame array.)
- The sync `.Wait()` deadlock on the WPF dispatcher is removed only by making stop/restart fully async end-to-end.
- Fire-and-forget writes (`_locoRepository.SaveAsync`, `OnSensorChangedAsync`) can race disposal during stop; the coordinator must sequence them.
- The manual path currently bypasses ownership/safety until re-routed (Increment 4).
- Koploper mask-vs-refusal semantics are unvalidated against a live Koploper session.

---

## 7. Increment 1 outcome (implemented, simulator-only)

Increment 1 introduced the in-process, independently start/stop/restartable track runtime. Key implementation decisions, recorded so later increments know why things are shaped this way:

- **Runtime boundary (inside the restartable runtime):** the Core track part (`RawUdpTransport` → `RawUdpTrackTransport` → `TrackCommClientAsync` → init pipeline → `TrackControlMain`), the fresh `TrackApplicationVariables` (incl. `PendingWrites`), the per-instance `CancellationTokenSource`, and the ECoS host (`TrackControlHost`, incl. server 15471 / external-info 5700 / simulator backend / safety). **Outside (survives stop/restart):** WPF view models/pages, Fiddle Yard, logging infrastructure, the production control trace, and the host-detection state.
- **Lifecycle ownership:** one coordinator — `SiebwaldeApp.Integration.TrackApplicationRuntimeHost`, behind the Core contract `ITrackApplicationRuntime`. `SiebwaldeApplicationModel` is now a thin facade. State machine `Stopped → Starting → Running ↔ Stopping → Failed`; a `SemaphoreSlim` gate prevents overlapping operations; a per-instance CTS is never reused.
- **Stop guarantee:** a stop only reports `Stopped` after the ECoS host returns `EcosHostStopResult.Stopped`/`AlreadyStopped` (its parts return `Task<bool>` "clean" signals), the comm client is `DisposeAsync`-awaited within a budget, and every runtime field + CTS is released in a `finally`. A timeout/fault/partial-cleanup transitions to `Failed`, never `Stopped`. `IEcosHostService` gained `StopAsync`; the sync `Stop()` remains the non-blocking UI/mode-transition wrapper.
- **Start/Restart share one init path:** `RestartAsync = StopAsync (proven) → StartAsync (fresh init)`. Real-mode init now observes `InitializationStatus` and maps a failed/cancelled init to `Failed`.
- **Failure semantics:** start fail, partial-init fail, stop fail, stop timeout, background-task fault (server accept loop / simulator loop → `Faulted` → coordinator `Running → Failed`), partial cleanup, restart-fail-during-stop, restart-fail-during-start all produce `Failed` (never masked).
- **Event-driven vs polling:** lifecycle/status is event-driven — the coordinator raises `StateChanged`/`Faulted`, and the WPF init page marshals them via `Dispatcher`; the enable/disable matrix is a pure `TrackRuntimeControlPolicy`. Existing functional polling (10 Hz runtime write loop, 100 ms comm publish timer, host-detection `DispatcherTimer`) is unchanged and deliberately not converted — it is functional, not a lifecycle-discovery mechanism.
- **Amplifier data seam:** the read path is exposed on `ITrackApplicationRuntime` (`TrackAmplifiers`/`GetAmplifierListing`/re-published `AmplifierDataReceived`) so future consumers (Fiddle Yard / YardController / MMDC) consume the Core contract, not `TrackControlMain` or a WPF page. No generic-I/O/MMDC layer was added (deferred). The mutable `TrackApplicationVariables` stays inside the runtime; after stop it is nulled and the amplifier page clears (matching prior behaviour).
- **Real-mode behaviour unchanged:** the track-part composition was moved verbatim from `SiebwaldeApplicationModel` into the coordinator (same 9 init steps and order); the only intentional changes are fresh-per-start helpers and the new init-failure→`Failed` observation. Real-mode end-to-end execution is not exercised in this simulator-only increment.
- **Known limitations / follow-ups (see `docs/backlog.md`):** ~~no simulator `ITrackTransport` exists~~ **RESOLVED (2026-09-24)** — a deterministic `DeterministicTrackTransport` now lets the full real-mode track part run end-to-end software-only (see `docs/backlog.md`); `KoploperExternalInfoClient` has no `Faulted` event (its loop retries rather than faults); `OnEcosHostFaulted` ignores faults during `Starting`; the WPF surface is build-verified (V1), not runtime-observed.

---

## 8. Roadmap reassessment (post simulator transport) — and the next increment

**Headline.** The deterministic simulator transport does not change *what* safe restart requires, only *when* the C# logic can be proven. The software/protocol half of the old "real graceful stop" (Increment 2) and "startup/restart neutralisation" (Increment 3) can now be built and proven software-only; only the physical-fidelity half stays V4. The roadmap is therefore re-cut along the **software/protocol boundary vs physical boundary**, not along **stop vs start**.

**Code-level facts confirmed by independent source review (2026-09-24):**

- **Neutral is `399`** (`AmplifierSpeedMapper.NeutralPwm`), but `SetDefaultPwmSetpointsStep` writes **`400`** (`AmplifierSpeedMapper.ForwardMinPwm`, the *lowest forward* step) into the in-memory image only — never transmitted, never equal to true neutral. The next increment must reconcile this to `399`.
- **The `TrackApplicationVariables` HoldingReg aliasing is no longer present.** `TrackAmplifierItem.HoldingReg` copies on assignment and `TrackCommClientAsync.HandleNewData` assigns a fresh per-frame array; each `trackAmpItems` entry holds a distinct array. It should still be pinned by a targeted test, but it is not a blocker.
- **There is no neutral command on the lifecycle stop/restart path.** `TrackApplicationRuntimeHost.StopTrackPartAsync` only stops the loop, cancels the CTS, stops the host and disposes the comm client. Neutral-on-stop exists only in `EcosHardwareStopSink` (the latched *safety* path), never in the *lifecycle* path. This is the concrete next-increment gap.
- **The current "established" bar is commanded + fresh comm, not observed.** `EcosHardwareStopSink`/`AmplifierCommandTracker` track *commanded* neutral ("fresh + commanded" = established); there is no runtime movement-gate that requires *observed* HR0 == 399. Adding that gate is exactly the next increment.
- **The simulator proves "commanded == readback" at the software/protocol boundary** (`DeterministicTrackTransport` writes the register then echoes it via SLAVEINFO). It does **not** prove physical PWM. This is the precise V4 boundary.

**Software-provable now (against `DeterministicTrackTransport`):** stale-`PendingWrites`/command prevention across restart; neutral commanding on Stop/Restart (up to "commanded"); the observed-neutral *gating logic* (fresh HR0 readback == 399 before movement); amplifier readback/freshness (including deterministic staleness); and the "known state cannot be established → movement not enabled" failure state.

**Still V4 (cannot be software-proven):** whether real SLAVEINFO HR0 reflects *physical* PWM vs a command echo; electrical/timing/RS-422 behaviour and the 10 Hz loop vs the real master round-trip; hardware failure modes (over-current/over-temp, stuck PIC32, backplane, silent amplifier, bootloader/flash); that commanding 399 physically stops the motor.

### 8.1 Next increment (proposed, software-first)

**Name:** "Observed-neutral restart/stop safety — software half (V1/V2 against `DeterministicTrackTransport`)."

**Objective.** Establish the software side of the observed-neutral restart guarantee in real-mode composition (exercised software-only via the simulator): command neutral `399` to all configured legitimate track amplifiers on Stop and on Start/Restart; gate any non-zero movement on a fresh per-amplifier HR0 readback of `399`; hold the runtime in a not-movement-safe state (movement refused + fault surfaced) when neutral cannot be established within a bounded window; and prove the stale-command guard as part of the same increment.

**In scope.** A neutral-command-on-stop path in the coordinator's real-mode `StopAsync` (reuse `IAmplifierNeutralizer.NeutralizeAmplifiers` / `SetPower(false)`, bounded); a neutral-command + observe-neutral gate on real-mode Start/Restart (command `399`, wait a bounded window for fresh HR0 == 399, then open the movement gate; otherwise keep it closed and raise a fault/Diagnostic); the gate as an orthogonal movement-permission gate at the single movement choke point (consistent with "unsafe" being orthogonal to runtime state); freshness/readback via `TrackAmplifierDataFreshness` + `TrackAmplifierItem.HoldingReg[PwmCommand]`; correct the `400` → `399` default-setpoint discrepancy; pin the HoldingReg no-aliasing behaviour with a targeted test.

**Out of scope.** Real hardware (V4); electrical/timing; hardware failure modes; amplifier group/domain semantics beyond "all configured legitimate track amplifiers" (decision 1 default b); control-source arbiter/manual path; hand-back; generic I/O/MMDC; Maintenance Mode; Koploper live validation; firmware.

**Software-proven vs deferred.** Command path, observation logic (fresh readback == 399), stale-command guard, and the not-movement-safe failure state are V1/V2-proven. The physical-fidelity questions above are V4 and explicitly deferred.

**Does it move toward a truly safe restart of the real railway?** Yes, materially, but only the software half. It moves the software to "observed neutral before movement" (the Product Owner's stated direction) and makes the software ready for the V4 physical validation. It does not by itself make the real railway restart safe: the "observed" bar is only as trustworthy as the readback's physical fidelity, which stays unproven until V4.

### 8.2 Product Owner decisions needed before this increment starts

- **Decision 2 (observed-vs-commanded bar) — REQUIRED.** Confirm the increment may build the observation logic against the **simulated readback** (fresh HR0 readback == 399), establishing "observed" at the C# readback level, while the physical-fidelity question (does real readback reflect physical PWM) stays V4. Recommendation: yes — this is the natural software-first split of the already-stated "neutral must ultimately be observed" direction.
- **Decision 1 (amplifier group/domain) — REQUIRED (or a scoped confirmation of option b).** Confirm "all configured legitimate track amplifiers (`1..50`) as one restart-safety domain" for this increment, deferring `MainRailway`/`MountainRailway`/`Spare` semantics. Recommendation: option (b).
- **Decision 5 (stop semantics while moving) — RECOMMENDED confirm.** Confirm neutral-on-stop (safe default) is acceptable even while Koploper is driving. Recommendation: option (a), via the existing ECoS stop/power-off path.

---

## 9. V4 physical-neutral validation preparation (analysis, 2026-09-25)

Source-verified analysis of the full HR0/PWM/readback path and the V4 physical-neutral test matrix. No physical test or firmware change was performed.

### 9.1 Headline — a firmware blocker precedes any physical validation

The committed PIC18 firmware (`TrackAmplifier4.X`) is **mid-refactor and does not execute the HR0→PWM apply path**. In `main.c` the runtime loop calls `AmplifierPeriodicTasks()` → `CheckModbusTimeout()`, `Ramp_Update()`, `ControlCore_Update()` — none of which has a definition in the committed tree; `runtime_command.comms_lost` references an undefined symbol; `REGULATORxUPDATE()` (the only function that translates HR0 into a PWM3 duty) is only in a commented-out block; and `modbushooks.c/.h` (defining `OnHoldingRegisterWrite` and `last_modbus_activity_tick`, both referenced by tracked code) are **untracked**. There is also an `Update_AmplifierTicks` `uint8_t` vs `uint32_t` declaration mismatch. The `dist/*/production/*.lst` artifacts are from an **older** `main.c` that still called `REGULATORxUPDATE()`. **The committed source does not build cleanly and does not match the shipped artifact.** Consequence: "what 399 physically does" cannot be fully proven from the committed firmware, and any V4 test must first pin down which firmware image is actually flashed.

### 9.2 HR0 data path (what the value means at each hop)

| Hop | Where | Value | Meaning |
|---|---|---|---|
| C# write | `TrackApplicationVariables.BuildHr0Value` | PWM bits 0..9 + EMO bit 15 | **Requested** target (direction/brake/stop bits are NOT written by C#) |
| Queue | `PendingWrites` / `TryConsumeHr0` | same | **Commanded** |
| PIC32 → PIC18 | `slavehandler.c` FC16 write | same bytes | **Transmitted** (byte-transparent; the master does not reinterpret HR0) |
| PIC18 | `PetitHoldingRegisters[HR_PWM_COMMAND].ActValue` | same value | **Stored command** (echoable) |
| PWM3 (intended) | `regulator.c REGULATORxUPDATE` `duty = cmd & 0x03FF` | 0→1 clamp, `PWM3_LoadDutyValue` | **Applied** duty (dead code in committed tree) |
| SLAVEINFO → C# | `TrackCommClientAsync.HandleNewData` `HoldingReg[0]` | `PetitHoldingRegisters[0]` | **Echo of stored command**, NOT applied duty, NOT measurement |

- The master refreshes `HoldingReg[0]` only via its cyclic FC03 read (`slavehandler.c` MESSAGE1 reads HOLDINGREG0..1), not the write echo, so there is an inherent ≥1-poll-cycle lag.
- There are **no input registers** (`NUMBER_OF_INPUT_PETITREGISTERS = 0`); the `MODBUS_TRACK_AMPLIFIER_MAPPING.md` v1.0 `InputReg5 CURRENT_PWM`/`InputReg6 TARGET_PWM` describe an **unimplemented** newer mapping. Read-only telemetry lives in HoldingReg2/4/5/6 today.
- `REGULATORxINIT` (the only enabling PWM load that runs) sets duty 399 + HR0 399; `LM_BRAKE` is asserted at boot (`main.c:56`) and never cleared in the committed tree.

### 9.3 What 399 physically means (source-derived, not physically confirmed)

- PWM: `PR2 = 199` (10-bit), duty ratio = `duty / (4·(PR2+1))` = `399/800` = **49.875% ≈ 50%**. 400 = 50%, 799 = 99.875%, 1 = 0.125%. Dual-sided PWM with neutral at the midpoint — **50% duty ≈ zero net DC ≈ motor stopped** (design intent; electrical confirmation is physical).
- Frequency is ambiguous (T2CKPS comment "1:4" vs `T2CON=0xA0`) — affects period, not the ~50% duty ratio.
- Pin naming is misleading: PWM3 hardware output is on **RC6** (aliased "LM_DIR"); "LM_PWM" is RC4 (GPIO); "LM_BRAKE" is RC5. `LM_BRAKE=1` is the brake/output-stop signal.
- EMO (bit 15) → `LM_BRAKE=true` + `LED_ERR=true` (skip PWM). Direction (bit 10) and brake (bit 11) are defined but **unused**; STOP (bit 12) is not in firmware.

### 9.4 The four non-equivalent claims (boundary)

1. **Commanded neutral** — HR0=399 accepted at the write queue (C# trace `[WRITE] HR0=0x018F`).
2. **Protocol-observed neutral** — a fresh SLAVEINFO reports `HoldingReg[0] & 0x03FF == 399` (C# `IsNeutralObserved`). **This is still a command echo, not applied state.**
3. **Physical PWM/output neutral** — scope: PWM ~50% duty; voltmeter: ~0 V average. **Only physical measurement.**
4. **Motor actually stopped** — shaft/wheels at rest. **Only physical observation.**

### 9.5 Scenarios where fresh HR0 == 399 but physical output is NOT neutral

- **S1 (real):** apply path absent — writing HR0=399 changes only the stored register; nothing drives PWM3 after boot.
- **S2 (real):** master echoes a stale register for ≥1 poll cycle.
- **S5 (real):** `LM_BRAKE` stays asserted (boot) regardless of HR0 — a "brake held" state.
- **S4 (real code, if apply ran):** EMO bit + brake latch passes `& 0x03FF == 399` but brakes and never loads 399.
- **S10 (real):** amplifier reset returns HR0=399 while a stale master image may still show non-neutral (or vice versa).
- Hypothetical: ramping (target 399 vs still-decelerating), PWM clock glitch, H-bridge fault (no fault handler), master-reset neutral broadcast (master does not broadcast neutral on reset).

### 9.6 V4 test matrix (minimal physical action; scope on RC6/RC5/RC4 + voltmeter on motor)

- **T0 (software, V3):** `SetLocoSpeed` → `EXEC_MBUS_SLAVE_DATA_EXCH(108)` → `SimpleEcosBackend` `END 8 (SAFETY_INTERLOCK)` through a real `TrackControlHost` + `DeterministicTrackTransport`. Proves Commanded/Protocol-observed neutral (software half only).
- **T1 (physical):** moving/non-neutral → command 399 → observed 399 → scope ~50% duty + motor stops.
- **T2 (physical):** runtime Stop during active non-neutral → neutral reached, no overshoot.
- **T3 (physical):** Restart from active → permission only after observed neutral; no motion.
- **T4 (physical/protocol):** stale/lost SLAVEINFO during neutralization → C# reports failure (not silent success), latch holds.
- **T5 (physical):** amplifier reset → PWM/HR0 → 399 on reset; no runaway.
- **T6 (physical):** controller reset → observe whether slave holds last PWM or goes neutral (currently unknown; document truth).
- **T7 (physical):** brake/EMO/direction combos → EMO asserts brake line; DIR/BRAKE bits inert.

Each physical test: one amplifier at a time, manual stop reachable, `399` + power-off recovery, one controlling session.

### 9.7 Pre-V4 instrumentation (firmware; FIRMWARE_FLASH-gated)

1. **Fix the firmware build** (blocking): define/implement or stub `CheckModbusTimeout`, `Ramp_Update`, `ControlCore_Update`, `runtime_command`; **commit/recover `modbushooks.c/.h`**; resolve the `Update_AmplifierTicks` type mismatch. Without this the flashed image cannot be trusted.
2. **Expose applied PWM** (a read-only register carrying the duty actually loaded into PWM3), so the SLAVEINFO frame can distinguish command-vs-applied.
3. **Expose brake/enable line state** (LM_BRAKE/LM_PWM/LM_DIR), addressing scenario S5.

### 9.8 V3 prerequisite recommendation

**Yes, do the V3 integration test first.** It is small (a bounded test addition, no production change), software-provable with the existing `DeterministicTrackTransport`, and it closes the software/protocol half (Commanded + Protocol-observed neutral), giving the V4 matrix a deterministic software baseline. It proves nothing physical and does not reduce the V4 requirement.

### 9.9 PASS/FAIL for "is protocol-observed HR0==399 a sufficient basis for physical restart safety?"

- **PASS only if** V4 demonstrates, on the actual flashed image, that every §9.5 scenario (especially S1/S5/S10) cannot occur — i.e. HR0==399 ⇒ applied 399 ⇒ physical neutral ⇒ stopped.
- **FAIL (current default):** because (i) the committed firmware's apply path is absent, (ii) the brake latch is asserted and never released, and (iii) the protocol cannot distinguish command-vs-applied. **Until the firmware build is fixed and V4 confirms applied-neutral tracks the echo, protocol-observed HR0==399 must NOT be treated as a sufficient basis for physical restart safety.**
