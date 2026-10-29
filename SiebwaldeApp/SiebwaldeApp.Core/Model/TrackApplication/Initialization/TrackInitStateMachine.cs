using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>Execution mode of the initialization state machine.</summary>
    public enum TrackInitRunMode
    {
        Idle,
        Automatic,
        Manual
    }

    /// <summary>Raised when the whole initialization run completes, fails or is cancelled.</summary>
    public sealed class TrackInitRunStatusChangedEventArgs : EventArgs
    {
        public InitializationStatus Status { get; }

        public TrackInitRunStatusChangedEventArgs(InitializationStatus status)
        {
            Status = status;
        }
    }

    /// <summary>
    /// The single controlled-initialization state machine: it groups the SAME production
    /// <see cref="IInitializationStep"/>s into the seven Ovaaltje2 stages, owns the per-stage status
    /// and preconditions/postconditions, and exposes BOTH automatic (advance stage-by-stage,
    /// fail-closed) and manual (step/retry/backtrack/abort/resume) execution over the SAME stage
    /// actions and verification.
    ///
    /// There is no second initialization stack: the machine drives the exact same step instances
    /// (and the same message channel) the previous linear runner used. Stage 6 (track power) is
    /// always operator-gated (<see cref="AuthorizeTrackPowerEnable"/>), so outputs are never
    /// silently energized.
    /// </summary>
    public sealed class TrackInitStateMachine : IAsyncDisposable
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly string _loggerInstance;
        private readonly string _sessionId;

        private readonly Dictionary<string, IInitializationStep> _steps;
        private readonly Dictionary<string, TrackInitStage> _stepStage;
        private readonly Func<IReadOnlyCollection<byte>> _requiredAmps;
        private readonly Channel<ReceivedMessage> _messages;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private readonly Dictionary<TrackInitStage, TrackInitStageStatus> _status;

        private CancellationTokenSource? _runCts;
        private string _currentStep = "ConnectToEthernetTarget";
        private TrackInitStage _currentStage = TrackInitStage.ControllerConnection;
        private TrackInitRunMode _mode = TrackInitRunMode.Idle;

        private InitializationStatus _runStatus = InitializationStatus.Idle;

        private volatile bool _trackPowerAuthorized;
        private volatile bool _trackPowerOn;
        private TrackPowerEnableStep? _trackPowerStep;
        private readonly TimeSpan? _trackPowerVerifyWindow;
        private volatile IReadOnlyList<TrackInitAmpStatus> _trackPowerDeenergizeStatus = Array.Empty<TrackInitAmpStatus>();

        public event EventHandler<TrackInitStageStatusChangedEventArgs>? StageStatusChanged;
        public event EventHandler<TrackInitRunStatusChangedEventArgs>? RunStatusChanged;
        public event EventHandler<TrackInitLogEventArgs>? LogEvent;

        /// <summary>The first step of the pipeline (stage 1).</summary>
        public const string FirstStepName = "ConnectToEthernetTarget";

        public TrackInitStateMachine(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            IEnumerable<IInitializationStep> steps,
            Func<IReadOnlyCollection<byte>> requiredAmps,
            string loggerInstance = "Track",
            TimeSpan? trackPowerVerifyWindow = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _loggerInstance = loggerInstance ?? "Track";
            _requiredAmps = requiredAmps ?? throw new ArgumentNullException(nameof(requiredAmps));
            _sessionId = Guid.NewGuid().ToString("N");
            _trackPowerVerifyWindow = trackPowerVerifyWindow;

            _steps = new Dictionary<string, IInitializationStep>();
            foreach (var step in steps)
            {
                _steps[step.Name] = step;
            }

            _stepStage = new Dictionary<string, TrackInitStage>(StringComparer.Ordinal);
            foreach (var step in _steps.Keys)
            {
                _stepStage[step] = StageOfStep(step);
            }

            _status = new Dictionary<TrackInitStage, TrackInitStageStatus>();
            foreach (var stage in TrackInitStageInfo.All)
            {
                _status[stage] = new TrackInitStageStatus(stage, TrackInitStageState.Pending);
            }

            _messages = Channel.CreateUnbounded<ReceivedMessage>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            _commClient.ControlMessageReceived += OnControlMessageReceived;
        }

        /// <summary>Read-only snapshot of the seven stage statuses (ordered by stage id).</summary>
        public IReadOnlyList<TrackInitStageStatus> Stages
            => TrackInitStageInfo.All.Select(s => _status[s]).ToArray();

        /// <summary>The stage the machine is currently executing (or last executed).</summary>
        public TrackInitStage CurrentStage => _currentStage;

        /// <summary>The current execution mode.</summary>
        public TrackInitRunMode Mode => _mode;

        /// <summary>True once the whole initialization run has completed (stage 7 passed).</summary>
        public bool IsCompleted => _runStatus == InitializationStatus.Completed;

        /// <summary>True when the run failed (a stage failed and the machine stopped, fail-closed).</summary>
        public bool IsFailed => _runStatus == InitializationStatus.Failed;

        /// <summary>The overall run status (mirrors <see cref="InitializationStatus"/>).</summary>
        public InitializationStatus RunStatus => _runStatus;

        /// <summary>True when the operator has authorized the Stage 6 track-power transition.</summary>
        public bool TrackPowerAuthorized => _trackPowerAuthorized;

        /// <summary>True when track power is currently ON (TRACK_POWER_ON verified).</summary>
        public bool TrackPowerOn => _trackPowerOn;

        /// <summary>
        /// Per-amplifier de-energization confirmation from the most recent group de-energize
        /// (disable or backtrack). An entry's <see cref="TrackInitAmpStatus.Ok"/> is true when that
        /// amplifier was confirmed inhibited==1 (de-energized). Empty before the first de-energize.
        /// </summary>
        public IReadOnlyList<TrackInitAmpStatus> TrackPowerDeenergizeStatus => _trackPowerDeenergizeStatus;

        // ---------------------------------------------------------------------------------
        // Step -> stage mapping
        // ---------------------------------------------------------------------------------

        /// <summary>Maps a production step name to its initialization stage.</summary>
        public static TrackInitStage StageOfStep(string stepName) => stepName switch
        {
            "ConnectToEthernetTarget" => TrackInitStage.ControllerConnection,
            "QueryControllerVersion" => TrackInitStage.ControllerConnection,
            "GlobalStopPrecondition" => TrackInitStage.ControllerConnection,
            "ResetAllSlaves" => TrackInitStage.FirmwareAddress,
            "DataUpload" => TrackInitStage.FirmwareAddress,
            "DetectSlaves" => TrackInitStage.FirmwareAddress,
            "RecoverSlaves" => TrackInitStage.FirmwareAddress,
            "FlashFwTrackamplifiers" => TrackInitStage.FirmwareAddress,
            "InitTrackamplifiers" => TrackInitStage.AmplifierStandby,
            "SetDefaultPwmSetpoints" => TrackInitStage.GlobalReady,
            "TrackPowerEnable" => TrackInitStage.AmpPowerEnable,
            "EnableTrackamplifiers" => TrackInitStage.NormalOperation,
            _ => TrackInitStage.ConfigureValidate
        };

        /// <summary>
        /// True for stages whose Passed/Failed status is finalized by the host (after its own
        /// amplifier-level verification) rather than by the step-runner transition. Stage 4
        /// (configure + validate) and Stage 5 (global ready) require the WHOLE required amplifier
        /// set to be ready, which the step runner alone cannot establish.
        /// </summary>
        private static bool IsHostFinalized(TrackInitStage stage)
            => stage is TrackInitStage.ConfigureValidate or TrackInitStage.GlobalReady;

        // ---------------------------------------------------------------------------------
        // Message channel (single mechanism, shared by both modes)
        // ---------------------------------------------------------------------------------

        private void OnControlMessageReceived(object? sender, ControlMessageEventArgs e)
            => _messages.Writer.TryWrite(e.Message);

        private async Task<ReceivedMessage?> ReadNextMessageOrNullAsync(CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(100));

            try
            {
                var reader = _messages.Reader;
                var hasItem = await reader.WaitToReadAsync(timeoutCts.Token).ConfigureAwait(false);
                if (!hasItem)
                {
                    return null;
                }

                return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        // ---------------------------------------------------------------------------------
        // Automatic execution
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Runs the initialization automatically, stage by stage, stopping (fail-closed) on the first
        /// stage failure. Stage 4 (configure + validate) and Stage 5 (global ready) are finalized by
        /// the host via <see cref="MarkStagePassed"/>/<see cref="MarkStageFailed"/> after their
        /// host-level verification; Stage 6 (track power) is never auto-run (it stays gated).
        /// </summary>
        public async Task RunAutomaticAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                BeginRun(TrackInitRunMode.Automatic, cancellationToken);
                await RunStagesToEndAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                SetRunStatus(InitializationStatus.Cancelled);
            }
            catch (Exception ex)
            {
                MarkStageFailed(_currentStage, ex.Message);
                SetRunStatus(InitializationStatus.Failed);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Resumes automatic progression from the current stage (after a manual interlude).</summary>
        public async Task ResumeAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_runStatus == InitializationStatus.Failed)
                {
                    throw new InvalidOperationException("Cannot resume a failed initialization; retry the failed stage first.");
                }

                if (_mode == TrackInitRunMode.Idle)
                {
                    BeginRun(TrackInitRunMode.Automatic, cancellationToken);
                }
                else
                {
                    _mode = TrackInitRunMode.Automatic;
                }

                await RunStagesToEndAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                SetRunStatus(InitializationStatus.Cancelled);
            }
            catch (Exception ex)
            {
                MarkStageFailed(_currentStage, ex.Message);
                SetRunStatus(InitializationStatus.Failed);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Runs the current stage (and any following stages) through to completion.</summary>
        private async Task RunStagesToEndAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var outcome = await RunCurrentStageAsync(ct).ConfigureAwait(false);
                if (outcome == TrackInitStageState.Failed || outcome == TrackInitStageState.Blocked)
                {
                    if (outcome == TrackInitStageState.Failed)
                    {
                        SetRunStatus(InitializationStatus.Failed);
                    }

                    return; // fail-closed: never silently advance past a failed/blocked stage
                }

                if (_runStatus == InitializationStatus.Completed)
                {
                    return;
                }

                // Stop at Stage 6: track power is gated and never auto-run.
                if (_currentStage == TrackInitStage.AmpPowerEnable)
                {
                    SetStageState(TrackInitStage.AmpPowerEnable, TrackInitStageState.Blocked,
                        "Automatic progression is gated at track power; explicit operator authorization is required.");
                    return;
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Manual execution (shares RunCurrentStageAsync with automatic mode)
        // ---------------------------------------------------------------------------------

        /// <summary>Manually runs exactly one stage (advancing to the next stage boundary).</summary>
        public async Task<TrackInitStageState> StepAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_mode == TrackInitRunMode.Idle)
                {
                    BeginRun(TrackInitRunMode.Manual, cancellationToken);
                }
                else
                {
                    _mode = TrackInitRunMode.Manual;
                }

                var outcome = await RunCurrentStageAsync(cancellationToken).ConfigureAwait(false);
                if (outcome == TrackInitStageState.Failed)
                {
                    SetRunStatus(InitializationStatus.Failed);
                }

                return outcome;
            }
            catch (OperationCanceledException)
            {
                SetRunStatus(InitializationStatus.Cancelled);
                return TrackInitStageState.Pending;
            }
            catch (Exception ex)
            {
                MarkStageFailed(_currentStage, ex.Message);
                SetRunStatus(InitializationStatus.Failed);
                return TrackInitStageState.Failed;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Re-runs the current stage (used after a stage failure).</summary>
        public async Task<TrackInitStageState> RetryAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_runStatus == InitializationStatus.Failed)
                {
                    _runStatus = InitializationStatus.Idle;
                }

                SetStageState(_currentStage, TrackInitStageState.Pending);
                var outcome = await RunCurrentStageAsync(cancellationToken).ConfigureAwait(false);
                if (outcome == TrackInitStageState.Failed)
                {
                    SetRunStatus(InitializationStatus.Failed);
                }

                return outcome;
            }
            catch (OperationCanceledException)
            {
                SetRunStatus(InitializationStatus.Cancelled);
                return TrackInitStageState.Pending;
            }
            catch (Exception ex)
            {
                MarkStageFailed(_currentStage, ex.Message);
                SetRunStatus(InitializationStatus.Failed);
                return TrackInitStageState.Failed;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Moves the current-stage pointer back one stage and marks it Pending for re-execution.
        /// Irreversible flash is never auto-undone: the operator must re-authorize the flash stage.
        /// Leaving Stage 6 (track power) while the H-bridges are energized de-energizes the whole
        /// group first, so backtracking never leaves the track energized.
        /// </summary>
        public void Backtrack()
        {
            lock (_gate)
            {
                var index = Array.IndexOf(TrackInitStageInfo.All, _currentStage);
                if (index <= 0)
                {
                    return;
                }

                // Gap B: leaving Stage 6 while track power is ON must de-energize the group
                // (fail-closed) so the H-bridge is never left energized by a backtrack.
                if (_currentStage == TrackInitStage.AmpPowerEnable && _trackPowerOn)
                {
                    _trackPowerOn = false;
                    _trackPowerAuthorized = false;
                    try
                    {
                        // Backtrack() is synchronous; run the same clear-write helper to completion.
                        DeEnergizeGroupAsync(verify: false, CancellationToken.None).GetAwaiter().GetResult();
                        Log(TrackInitStage.AmpPowerEnable,
                            "Backtrack de-energized the group (HR_TRACK_POWER bit 0 cleared on every required amplifier).");
                    }
                    catch (Exception ex)
                    {
                        Log(TrackInitStage.AmpPowerEnable, $"Backtrack could not de-energize the group: {ex.Message}");
                    }
                }

                var previous = TrackInitStageInfo.All[index - 1];
                SetStageState(previous, TrackInitStageState.Pending);
                _currentStage = previous;
                _currentStep = FirstStepOfStage(previous);
                _mode = TrackInitRunMode.Manual;
                Log(TrackInitStage.ControllerConnection, $"Backtracked to stage '{TrackInitStageInfo.DisplayName(previous)}'.");
            }
        }

        /// <summary>Aborts the current run (automatic or manual).</summary>
        public void Abort()
        {
            _runCts?.Cancel();
            SetRunStatus(InitializationStatus.Cancelled);
        }

        // ---------------------------------------------------------------------------------
        // Shared stage action (used by BOTH automatic and manual modes)
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Runs the steps of the current stage through the single message channel until the pipeline
        /// leaves the stage, completes, fails, or (for Stage 6) is gated by missing authorization.
        /// </summary>
        private async Task<TrackInitStageState> RunCurrentStageAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return TrackInitStageState.Pending;
            }

            // Stage 6 gating: the track-power action may only run after explicit authorization.
            if (_currentStage == TrackInitStage.AmpPowerEnable && !_trackPowerAuthorized)
            {
                SetStageState(TrackInitStage.AmpPowerEnable, TrackInitStageState.Blocked,
                    "Track power requires explicit operator authorization.");
                return TrackInitStageState.Blocked;
            }

            // Stage 4 (configure + validate) has no step: it is finalized by the host.
            if (_currentStage == TrackInitStage.ConfigureValidate)
            {
                SetStageState(TrackInitStage.ConfigureValidate, TrackInitStageState.Running);
                // The host's InitializePhase1AmplifiersAsync performs the config download and then
                // marks this stage passed/failed via MarkStagePassed/MarkStageFailed.
                return _status[TrackInitStage.ConfigureValidate].State;
            }

            SetStageState(_currentStage, TrackInitStageState.Running);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (!_steps.TryGetValue(_currentStep, out var step))
                {
                    throw new InvalidOperationException($"Unknown init step: {_currentStep}");
                }

                var lastMessage = await ReadNextMessageOrNullAsync(ct).ConfigureAwait(false);
                var result = await step.ExecuteAsync(lastMessage, ct).ConfigureAwait(false);

                switch (result.Kind)
                {
                    case InitStepResultKind.Continue:
                        continue;

                    case InitStepResultKind.NextStep:
                    {
                        var next = result.NextStepName
                            ?? throw new InvalidOperationException("Next step name may not be null.");
                        var nextStage = _stepStage.TryGetValue(next, out var ns) ? ns : StageOfStep(next);

                        if (nextStage != _currentStage)
                        {
                            if (!IsHostFinalized(_currentStage))
                            {
                                MarkStagePassed(_currentStage);
                            }

                            _currentStage = nextStage;
                            _currentStep = next;
                            return TrackInitStageState.Passed;
                        }

                        _currentStep = next;
                        continue;
                    }

                    case InitStepResultKind.Completed:
                        if (!IsHostFinalized(_currentStage))
                        {
                            MarkStagePassed(_currentStage);
                        }

                        if (_currentStage != TrackInitStage.NormalOperation)
                        {
                            _currentStage = TrackInitStage.NormalOperation;
                        }
                        SetRunStatus(InitializationStatus.Completed);
                        return TrackInitStageState.Passed;

                    case InitStepResultKind.Error:
                        MarkStageFailed(_currentStage, result.ErrorMessage ?? "unknown error");
                        return TrackInitStageState.Failed;
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Stage 6: track power (gated)
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Explicit operator authorization for the Stage 6 track-power transition. Without this the
        /// automatic mode stops (Blocked) at Stage 6 and never energizes the H-bridges.
        /// </summary>
        public void AuthorizeTrackPowerEnable()
        {
            _trackPowerAuthorized = true;
            Log(TrackInitStage.AmpPowerEnable, "Track power enable authorized by operator.");
        }

        /// <summary>Revokes a prior track-power authorization (does not de-energize; see DisableTrackPowerAsync).</summary>
        public void RevokeTrackPowerAuthorization()
        {
            _trackPowerAuthorized = false;
            Log(TrackInitStage.AmpPowerEnable, "Track power enable authorization revoked.");
        }

        /// <summary>
        /// Runs the Stage 6 track-power action (write HR_TRACK_POWER + verify HR_STATE per amplifier)
        /// over the required amplifier set. Fails closed if any amplifier does not reach the verified
        /// TRACK_POWER_ON state.
        /// </summary>
        public async Task<bool> EnableTrackPowerAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Defense-in-depth (R2): Stage 6 may only energize once Stage 5 (GlobalReady) has
                // Passed — the whole required amplifier set is group-READY. This adds a precondition
                // on top of the operator-authorization gate, so a manual operator can never energize
                // after a Stage 5 failure.
                if (_status[TrackInitStage.GlobalReady].State != TrackInitStageState.Passed)
                {
                    SetStageState(TrackInitStage.AmpPowerEnable, TrackInitStageState.Blocked,
                        "Track power requires Stage 5 (GlobalReady) to be Passed first.");
                    return false;
                }

                if (!_trackPowerAuthorized)
                {
                    SetStageState(TrackInitStage.AmpPowerEnable, TrackInitStageState.Blocked,
                        "Track power requires explicit operator authorization.");
                    return false;
                }

                var required = _requiredAmps().ToArray();
                _trackPowerStep = new TrackPowerEnableStep(_commClient, _variables, required, _loggerInstance, _trackPowerVerifyWindow);

                _currentStage = TrackInitStage.AmpPowerEnable;
                SetStageState(TrackInitStage.AmpPowerEnable, TrackInitStageState.Running);

                InitStepResult result;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var lastMessage = await ReadNextMessageOrNullAsync(cancellationToken).ConfigureAwait(false);
                    result = await _trackPowerStep.ExecuteAsync(lastMessage, cancellationToken).ConfigureAwait(false);
                    if (result.Kind is InitStepResultKind.Completed or InitStepResultKind.Error)
                    {
                        break;
                    }
                }

                if (result.Kind == InitStepResultKind.Completed)
                {
                    _trackPowerOn = true;
                    MarkStagePassed(TrackInitStage.AmpPowerEnable);
                    Log(TrackInitStage.AmpPowerEnable, "TRACK_POWER_ON: every required amplifier energized at neutral.");
                    return true;
                }

                _trackPowerOn = false;
                // Gap A: a partial or failed group activation must de-energize the WHOLE group
                // (fail-closed). Clear bit 0 on every required amplifier so no amp that already
                // energized is left energized. Idempotent and independent of cancellation: use
                // CancellationToken.None so the clear is never skipped.
                await DeEnergizeGroupOrLogAsync(CancellationToken.None).ConfigureAwait(false);
                MarkStageFailed(TrackInitStage.AmpPowerEnable, result.ErrorMessage ?? "unknown error");
                return false;
            }
            catch (Exception ex)
            {
                MarkStageFailed(TrackInitStage.AmpPowerEnable, ex.Message);
                _trackPowerOn = false;
                // Same fail-closed de-energize for the exception path (e.g. a write threw midway).
                await DeEnergizeGroupOrLogAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Clears HR_TRACK_POWER (bit 0 = 0) on every required amplifier, de-energizing the bridge,
        /// then reads back HR_STATE per amplifier and verifies each reports inhibited==1
        /// (de-energized). Mirrors the firmware: clearing the request re-latches inhibit.
        /// Returns true when every required amplifier is confirmed de-energized; false (fail-closed)
        /// when any amplifier cannot be confirmed, with per-amplifier detail in
        /// <see cref="TrackPowerDeenergizeStatus"/>.
        /// </summary>
        public async Task<bool> DisableTrackPowerAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var allDeenergized = await DeEnergizeGroupAsync(verify: true, cancellationToken).ConfigureAwait(false);

                _trackPowerOn = false;
                _trackPowerAuthorized = false;
                SetStageState(TrackInitStage.AmpPowerEnable, TrackInitStageState.Pending);

                if (allDeenergized)
                {
                    Log(TrackInitStage.AmpPowerEnable,
                        "Track power disabled: HR_TRACK_POWER cleared, every bridge verified de-energized.");
                }
                else
                {
                    var unconfirmed = _trackPowerDeenergizeStatus.Where(s => !s.Ok).Select(s => s.SlaveNumber).ToArray();
                    Log(TrackInitStage.AmpPowerEnable,
                        $"Track power disabled, but de-energization NOT confirmed for amplifier(s) {string.Join(",", unconfirmed)}.");
                }

                return allDeenergized;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// De-energizes the group: clears HR_TRACK_POWER (bit 0 = 0) on every required amplifier.
        /// When <paramref name="verify"/> is true, also reads back HR_STATE per amplifier and confirms
        /// inhibited==1, recording the per-amplifier status in <see cref="TrackPowerDeenergizeStatus"/>.
        /// The caller must hold the gate. Returns true when verification is disabled, or when every
        /// amplifier is confirmed de-energized.
        /// </summary>
        private async Task<bool> DeEnergizeGroupAsync(bool verify, CancellationToken ct)
        {
            var required = _requiredAmps().ToArray();

            foreach (var amp in required)
            {
                await _commClient.SendAsync(
                    AmplifierConfigurationDownloader.Exchange(
                        amp,
                        (byte)TrackAmplifierRegisters.TrackPower,
                        0),
                    ct).ConfigureAwait(false);
            }

            if (!verify)
            {
                return true;
            }

            foreach (var amp in required)
            {
                await _commClient.SendAsync(
                    AmplifierConfigurationDownloader.Exchange(
                        amp,
                        (byte)TrackAmplifierRegisters.State,
                        0,
                        read: true),
                    ct).ConfigureAwait(false);
            }

            var statuses = new List<TrackInitAmpStatus>(required.Length);
            var allConfirmed = true;
            foreach (var amp in required)
            {
                var item = _variables.trackAmpItems[amp];
                var inhibited = TrackPowerEnableStep.IsInhibited(item);
                if (!inhibited)
                {
                    allConfirmed = false;
                }

                statuses.Add(new TrackInitAmpStatus(
                    amp,
                    inhibited ? "de-energized (inhibited)" : "de-energization not confirmed",
                    inhibited));
            }

            _trackPowerDeenergizeStatus = statuses;
            return allConfirmed;
        }

        /// <summary>
        /// Best-effort, fail-closed de-energize for the failure paths: clears bit 0 on every
        /// required amplifier without verification, and logs (rather than throws) if the clear
        /// itself fails, so a de-energize problem never masks the original failure.
        /// </summary>
        private async Task DeEnergizeGroupOrLogAsync(CancellationToken ct)
        {
            try
            {
                await DeEnergizeGroupAsync(verify: false, ct).ConfigureAwait(false);
                Log(TrackInitStage.AmpPowerEnable,
                    "Group de-energized (HR_TRACK_POWER bit 0 cleared on every required amplifier).");
            }
            catch (Exception ex)
            {
                Log(TrackInitStage.AmpPowerEnable, $"Group de-energize FAILED: {ex.Message}");
            }
        }

        // ---------------------------------------------------------------------------------
        // Stage status
        // ---------------------------------------------------------------------------------

        /// <summary>Host hook: marks a host-verified stage passed (stages 4 and 5 finalization).</summary>
        public void MarkStagePassed(TrackInitStage stage)
        {
            if (_status[stage].State == TrackInitStageState.Passed)
            {
                return;
            }

            _status[stage] = new TrackInitStageStatus(stage, TrackInitStageState.Passed, amps: BuildAmpStatuses(stage));
            StageStatusChanged?.Invoke(this, new TrackInitStageStatusChangedEventArgs(_status[stage]));
            Log(stage, $"Stage '{TrackInitStageInfo.DisplayName(stage)}' passed.");
        }

        /// <summary>Host hook: marks a host-verified stage failed with a reason and recovery action.</summary>
        public void MarkStageFailed(TrackInitStage stage, string reason, string? recoveryAction = null)
        {
            _status[stage] = new TrackInitStageStatus(stage, TrackInitStageState.Failed, reason, recoveryAction, BuildAmpStatuses(stage));
            StageStatusChanged?.Invoke(this, new TrackInitStageStatusChangedEventArgs(_status[stage]));
            Log(stage, $"Stage '{TrackInitStageInfo.DisplayName(stage)}' failed: {reason}.");
        }

        private void SetStageState(TrackInitStage stage, TrackInitStageState state, string? reason = null, string? recovery = null)
        {
            if (_status[stage].State == state && state is not (TrackInitStageState.Running or TrackInitStageState.Blocked))
            {
                return;
            }

            _status[stage] = new TrackInitStageStatus(stage, state, reason, recovery, BuildAmpStatuses(stage));
            StageStatusChanged?.Invoke(this, new TrackInitStageStatusChangedEventArgs(_status[stage]));
        }

        private IReadOnlyList<TrackInitAmpStatus> BuildAmpStatuses(TrackInitStage stage)
        {
            if (stage != TrackInitStage.AmpPowerEnable && stage != TrackInitStage.ConfigureValidate && stage != TrackInitStage.GlobalReady)
            {
                return Array.Empty<TrackInitAmpStatus>();
            }

            var list = new List<TrackInitAmpStatus>();
            foreach (var amp in _requiredAmps())
            {
                var item = _variables.trackAmpItems[amp];
                var ok = stage switch
                {
                    TrackInitStage.AmpPowerEnable => TrackPowerEnableStep.IsTrackPowerOn(item),
                    TrackInitStage.GlobalReady =>
                        item.ConfigurationVerified && item.Phase1Failure is null && GroupReadyStep.IsGroupReady(item),
                    _ => item.ConfigurationVerified && item.Phase1Failure is null
                };
                list.Add(new TrackInitAmpStatus(amp, DescribeAmp(item, stage), ok));
            }

            return list;
        }

        private static string DescribeAmp(TrackAmplifierItem item, TrackInitStage stage)
        {
            if (item is null || item.HoldingReg is null || item.HoldingReg.Length <= TrackAmplifierRegisters.State)
            {
                return "no data";
            }

            if (item.Phase1Failure is { Length: > 0 })
            {
                return $"not ready: {item.Phase1Failure}";
            }

            if (!item.ConfigurationVerified)
            {
                return "not configured";
            }

            if (stage == TrackInitStage.GlobalReady && !GroupReadyStep.IsGroupReady(item))
            {
                return "not group-ready (STANDBY)";
            }

            var state = item.HoldingReg[TrackAmplifierRegisters.State];
            var inhibited = (state & TrackAmplifierRegisters.InhibitedBit) != 0;
            var enableActive = (state & TrackAmplifierRegisters.EnableActiveBit) != 0;
            var configOk = (state & TrackAmplifierRegisters.ConfigOkBit) != 0;
            return $"inhibited={(inhibited ? 1 : 0)} enable={(enableActive ? 1 : 0)} config_ok={(configOk ? 1 : 0)}";
        }

        // ---------------------------------------------------------------------------------
        // Run lifecycle
        // ---------------------------------------------------------------------------------

        private void BeginRun(TrackInitRunMode mode, CancellationToken ct)
        {
            _mode = mode;
            _runStatus = InitializationStatus.Running;
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            RunStatusChanged?.Invoke(this, new TrackInitRunStatusChangedEventArgs(InitializationStatus.Running));
            Log(_currentStage, $"Initialization started ({mode}) with session id {_sessionId}.");
        }

        private void SetRunStatus(InitializationStatus status)
        {
            if (_runStatus == status)
            {
                return;
            }

            _runStatus = status;
            RunStatusChanged?.Invoke(this, new TrackInitRunStatusChangedEventArgs(status));
        }

        private static string FirstStepOfStage(TrackInitStage stage) => stage switch
        {
            TrackInitStage.ControllerConnection => "ConnectToEthernetTarget",
            TrackInitStage.FirmwareAddress => "ResetAllSlaves",
            TrackInitStage.AmplifierStandby => "InitTrackamplifiers",
            TrackInitStage.GlobalReady => "SetDefaultPwmSetpoints",
            TrackInitStage.AmpPowerEnable => "TrackPowerEnable",
            TrackInitStage.NormalOperation => "EnableTrackamplifiers",
            _ => "ConnectToEthernetTarget"
        };

        private void Log(TrackInitStage stage, string message, byte? amp = null)
        {
            IoC.Logger.Log($"TrackInit[{stage}]: {message}", _loggerInstance);
            LogEvent?.Invoke(this, new TrackInitLogEventArgs(DateTimeOffset.UtcNow, _sessionId, stage, message, amp));
        }

        public async ValueTask DisposeAsync()
        {
            _commClient.ControlMessageReceived -= OnControlMessageReceived;
            _runCts?.Cancel();
            _runCts?.Dispose();
            await Task.CompletedTask;
        }
    }
}
