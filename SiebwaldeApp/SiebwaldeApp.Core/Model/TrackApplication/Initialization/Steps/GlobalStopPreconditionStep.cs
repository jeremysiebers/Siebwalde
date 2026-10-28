namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization precondition that reads the PIC32 <c>GLOBAL_STOP_STATE</c> (111) and gates
    /// the following MBUS reset (107) on the observed global-stop state.
    ///
    /// A latched global stop (state 1 = latched-host-loss, 2 = latched-host-initiated) forces the
    /// PIC32 MBUS handler into <c>MBUS_STATE_SERVICE_TASKS</c> (TrackController5/firmware/src/mbus.c
    /// 202-210), which swallows the 107 DONE ack, so initialization would otherwise stall and fail.
    /// This step therefore:
    ///   - issues a consume-only 111 query and, when the state is 0 (disarmed) or 3 (armed),
    ///     proceeds immediately;
    ///   - when the state is 1 or 2 (latched), it does NOT proceed: it reports an operator-facing
    ///     state and blocks until the operator explicitly authorizes recovery AND the physical stop
    ///     inputs are confirmed clear, after which it re-arms the PIC32 through the existing
    ///     <see cref="TrackAmplifierReleaseAuthority"/> (<c>REARM_GLOBAL_STOP</c> 112 only — never a
    ///     fresh HR0 movement command).
    ///
    /// The 111 reply frame is parsed exactly like <see cref="GlobalStopMonitor.ObserveStatus"/>:
    /// sender = CONTROLLER (10), command = GLOBAL_STOP_STATE (111), state = DONE (7), and
    /// taskmessage = the global-stop state (0/1/2/3). This step never writes a slave register and
    /// never emits an ENABLE or a movement command.
    /// </summary>
    public sealed class GlobalStopPreconditionStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly string _loggerInstance;
        private readonly TrackAmplifierReleaseAuthority _releaseAuthority;
        private readonly Func<bool> _operatorPermitRecovery;

        // 0 = send 111 query, 1 = await initial reply, 2 = await operator recovery, 3 = send
        // post-re-arm 111 query, 4 = await post-re-arm reply.
        private int _subState;

        /// <summary>Name of this initialization step; must match the preceding step's Next target.</summary>
        public string Name => "GlobalStopPrecondition";

        /// <summary>
        /// Creates the precondition.
        /// </summary>
        /// <param name="commClient">The transport comm client used to send 111 (query) and 112 (re-arm).</param>
        /// <param name="operatorPermitRecovery">
        /// Explicit operator-action gate for the re-arm. Defaults to fail-closed (never permitted),
        /// so a latched controller can never be re-armed without an explicit operator authorization.
        /// </param>
        /// <param name="physicalStopInputsClear">
        /// Physical stop-inputs-clear evidence, injected by the caller (the V4 physical evidence
        /// boundary is not crossed here). Defaults to fail-closed (never clear).
        /// </param>
        /// <param name="loggerInstance">Logger instance for progress messages.</param>
        public GlobalStopPreconditionStep(
            ITrackCommClient commClient,
            Func<bool>? operatorPermitRecovery = null,
            Func<bool>? physicalStopInputsClear = null,
            string loggerInstance = "Track")
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _loggerInstance = loggerInstance ?? "Track";
            _operatorPermitRecovery = operatorPermitRecovery ?? (() => false);
            _releaseAuthority = new TrackAmplifierReleaseAuthority(
                commClient,
                physicalStopInputsClear ?? (() => false));
            _subState = 0;
        }

        /// <summary>
        /// Executes one iteration of this step's state machine. The initialization service calls this
        /// repeatedly, passing the most recent control message (if any).
        /// </summary>
        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            switch (_subState)
            {
                case 0:
                    _subState = 1;
                    return await SendQueryAsync(cancellationToken).ConfigureAwait(false);

                case 1:
                    return EvaluateInitialReply(lastMessage);

                case 2:
                    return await AwaitOperatorRecoveryAsync(cancellationToken).ConfigureAwait(false);

                case 3:
                    _subState = 4;
                    return await SendQueryAsync(cancellationToken).ConfigureAwait(false);

                case 4:
                    return EvaluatePostRearmReply(lastMessage);

                default:
                    IoC.Logger.Log(
                        "GlobalStopPreconditionStep encountered an invalid internal state.",
                        _loggerInstance);
                    _subState = 0;
                    return InitStepResult.Error("Invalid internal state in GlobalStopPreconditionStep.");
            }
        }

        /// <summary>Issues the consume-only 111 query once.</summary>
        private async Task<InitStepResult> SendQueryAsync(CancellationToken cancellationToken)
        {
            // A 111 frame carries no payload; the PIC32 replies with its current global-stop state.
            var messageToSend = new SendMessage(TrackCommand.GLOBAL_STOP_STATE, Array.Empty<byte>());

            await _commClient.SendAsync(messageToSend, cancellationToken).ConfigureAwait(false);

            IoC.Logger.Log(
                "State.GlobalStopPrecondition => GLOBAL_STOP_STATE query.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        /// <summary>Evaluates the initial 111 reply: proceed when not latched, else gate on recovery.</summary>
        private InitStepResult EvaluateInitialReply(ReceivedMessage? lastMessage)
        {
            if (!TryReadState(lastMessage, out var state))
            {
                return InitStepResult.Continue();
            }

            if (state == GlobalStopStatusMessage.Disarmed || state == GlobalStopStatusMessage.Armed)
            {
                _subState = 0;
                return InitStepResult.Next("ResetAllSlaves");
            }

            if (state == GlobalStopStatusMessage.LatchedHostLoss ||
                state == GlobalStopStatusMessage.LatchedHostInitiated)
            {
                _subState = 2;
                IoC.Logger.Log(
                    $"Global stop latched (state {state}) — operator re-arm required.",
                    _loggerInstance);
                return InitStepResult.Continue();
            }

            // An unexpected state cannot be interpreted safely: fail closed.
            IoC.Logger.Log(
                $"GlobalStopPreconditionStep: unexpected global-stop state {state}.",
                _loggerInstance);
            _subState = 0;
            return InitStepResult.Error($"Unexpected global-stop state: {state}.");
        }

        /// <summary>
        /// Waits for the explicit operator authorization, then re-arms (112) through the release
        /// authority once the physical stop inputs are confirmed clear. Never auto-releases: without
        /// the operator gate and the physical-clear gate this state keeps polling.
        /// </summary>
        private async Task<InitStepResult> AwaitOperatorRecoveryAsync(CancellationToken cancellationToken)
        {
            if (!_operatorPermitRecovery())
            {
                return InitStepResult.Continue();
            }

            var result = await _releaseAuthority.RearmGlobalStopAsync(cancellationToken)
                .ConfigureAwait(false);

            switch (result.Kind)
            {
                case AmplifierReleaseResultKind.Released:
                    IoC.Logger.Log(
                        "Global stop recovery: REARM_GLOBAL_STOP emitted; re-confirming state.",
                        _loggerInstance);
                    _subState = 3;
                    return InitStepResult.Continue();

                case AmplifierReleaseResultKind.BlockedStopInputsNotClear:
                    IoC.Logger.Log(
                        "Global stop recovery blocked: physical stop inputs are not confirmed clear.",
                        _loggerInstance);
                    return InitStepResult.Continue();

                default:
                    IoC.Logger.Log(
                        $"Global stop recovery failed: {result.Detail ?? "unknown error"}.",
                        _loggerInstance);
                    return InitStepResult.Continue();
            }
        }

        /// <summary>Evaluates the post-re-arm reply: proceed when no longer latched, else re-gate.</summary>
        private InitStepResult EvaluatePostRearmReply(ReceivedMessage? lastMessage)
        {
            if (!TryReadState(lastMessage, out var state))
            {
                return InitStepResult.Continue();
            }

            if (state == GlobalStopStatusMessage.Disarmed || state == GlobalStopStatusMessage.Armed)
            {
                _subState = 0;
                return InitStepResult.Next("ResetAllSlaves");
            }

            // Still latched (re-arm did not clear it, or it re-latched): return to awaiting operator.
            IoC.Logger.Log(
                $"Global stop still latched (state {state}); re-arm required again.",
                _loggerInstance);
            _subState = 2;
            return InitStepResult.Continue();
        }

        /// <summary>
        /// Returns true (with the state in <paramref name="state"/>) only for a genuine CONTROLLER
        /// 111/DONE reply; mirrors <see cref="GlobalStopMonitor.ObserveStatus"/>'s frame matching.
        /// </summary>
        private static bool TryReadState(ReceivedMessage? lastMessage, out ushort state)
        {
            state = 0;
            if (!lastMessage.HasValue)
            {
                return false;
            }

            var m = lastMessage.Value;
            if (m.TaskId != TaskId.CONTROLLER ||
                m.Taskcommand != TrackCommand.GLOBAL_STOP_STATE ||
                m.Taskstate != TaskStates.DONE)
            {
                return false;
            }

            state = m.Taskmessage;
            return true;
        }
    }
}
