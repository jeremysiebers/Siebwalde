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

Each increment is independently mergeable and (where possible) simulator-testable. Real-hardware restart is gated by decisions 1 and 2 (Increment 3).

1. **Increment 1 (software-only, simulator mode)** — stop/start/restart of the C# track-control runtime from WPF. Detailed below.
2. **Increment 2** — real-mode graceful **stop** (movement-safe, no restart yet): cancellation + disposal + neutral-on-stop via the existing `SetPower(false)`/`StopLayout` path; prove a stopped runtime holds no stale writes. Validation V4 (`LIVE_HARDWARE`).
3. **Increment 3** — startup/restart **neutralization guarantee**: establish + observe neutral before enabling movement after real start/restart; fix HoldingReg aliasing. Depends on decisions 1 and 2. Validation V4.
4. **Increment 4** — control-source arbiter + **manual control path** (operator recovery): manual loco (incl. slow reverse) + switch control through the arbiter + safety interlock; `SetAmplifierControl` moved under `Manual` ownership. Validation V2 (simulator) then V4.
5. **Increment 5** — **hand-back** workflow + WPF operator surface. Depends on decision 3; live Koploper validation.
6. **Increment 6 (later)** — communication/hardware recovery + controlled **Maintenance Mode** (firmware dev/test), `FIRMWARE_FLASH`-gated; re-evaluate out-of-process split here.

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
- Neutral observation depends on fixing `TrackApplicationVariables` HoldingReg aliasing and on confirming amplifier HR0 readback semantics on real hardware.
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
- **Known limitations / follow-ups (see `docs/backlog.md`):** no simulator `ITrackTransport` exists (so the full real-mode track part is composed but not executed in simulator mode); `KoploperExternalInfoClient` has no `Faulted` event (its loop retries rather than faults); `OnEcosHostFaulted` ignores faults during `Starting`; the WPF surface is build-verified (V1), not runtime-observed.
