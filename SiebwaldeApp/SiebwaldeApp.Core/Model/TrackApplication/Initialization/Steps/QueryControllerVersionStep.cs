namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that issues a single read-only <c>GET_CONTROLLER_VERSION</c>
    /// (113) request to the PIC32 master and waits for its reply, completing the
    /// send+parse round-trip once per start.
    ///
    /// The reply itself is parsed by <see cref="TrackCommClientAsync.HandleNewData"/> and
    /// surfaced on <see cref="TrackControllerCommands.ControllerVersion"/>; this step only
    /// drives the request/response and does NOT read or gate on the version value. No
    /// ProtocolVersion gate is enabled and no amplifier/train state is changed.
    /// </summary>
    public sealed class QueryControllerVersionStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly string _loggerInstance;

        // 0 = send GET_CONTROLLER_VERSION once, 1 = wait for the reply.
        private int _subState;

        /// <summary>
        /// Name of this initialization step; must match the step key used by the
        /// initialization service and the preceding step's <c>Next(...)</c> target.
        /// </summary>
        public string Name => "QueryControllerVersion";

        public QueryControllerVersionStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            string loggerInstance = "Track")
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;
        }

        /// <summary>
        /// Execute one iteration of this step's state machine. The initialization service
        /// calls this repeatedly, passing the most recent <see cref="ReceivedMessage"/> (if any).
        /// </summary>
        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            switch (_subState)
            {
                case 0:
                    return await ExecuteSendQueryAsync(cancellationToken).ConfigureAwait(false);

                case 1:
                    return ExecuteWaitForReply(lastMessage);

                default:
                    IoC.Logger.Log(
                        "QueryControllerVersionStep encountered an invalid internal state.",
                        _loggerInstance);

                    _subState = 0;
                    return InitStepResult.Error("Invalid internal state in QueryControllerVersionStep.");
            }
        }

        /// <summary>
        /// State 0: send the read-only version query once.
        /// </summary>
        private async Task<InitStepResult> ExecuteSendQueryAsync(CancellationToken cancellationToken)
        {
            // A 113 frame carries no payload; the PIC32 ignores the data field for this command.
            var messageToSend = new SendMessage(TrackCommand.GET_CONTROLLER_VERSION, Array.Empty<byte>());

            await _commClient.SendAsync(messageToSend, cancellationToken).ConfigureAwait(false);

            _subState = 1;

            IoC.Logger.Log(
                "State.QueryControllerVersion => GET_CONTROLLER_VERSION.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 1: wait for the CONTROLLER to echo GET_CONTROLLER_VERSION (the version reply).
        /// </summary>
        private InitStepResult ExecuteWaitForReply(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
            {
                // No new message yet, keep waiting.
                return InitStepResult.Continue();
            }

            if (lastMessage.Value.TaskId == TaskId.CONTROLLER &&
                lastMessage.Value.Taskcommand == TrackCommand.GET_CONTROLLER_VERSION)
            {
                _subState = 0;

                IoC.Logger.Log(
                    "State.QueryControllerVersion => version reply received.",
                    _loggerInstance);

                return InitStepResult.Next("ResetAllSlaves");
            }

            return InitStepResult.Continue();
        }
    }
}
