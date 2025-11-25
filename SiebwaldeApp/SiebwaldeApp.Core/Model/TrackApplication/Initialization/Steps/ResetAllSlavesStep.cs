namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that resets all MBUS slaves and turns them on.
    /// 
    /// Legacy equivalent: ResetAllSlaves (IAmplifierInitializersBaseClass).
    /// This step sends EXEC_MBUS_STATE_RESET and waits for MBUS_STATE_RESET/DONE,
    /// then sends EXEC_MBUS_STATE_SLAVES_ON and waits for MBUS_STATE_SLAVES_ON/DONE.
    /// </summary>
    public sealed class ResetAllSlavesStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly string _loggerInstance;

        // Local state, equivalent to the legacy SubMethodState:
        // 0 = send reset command
        // 1 = wait for MBUS_STATE_RESET DONE, then send SLAVES_ON
        // 2 = wait for MBUS_STATE_SLAVES_ON DONE, then complete
        private int _subState;

        // Reusable SendMessage container, like the old mSendMessage.
        private readonly SendMessage _sendMessageTemplate;

        /// <summary>
        /// Name of this initialization step.
        /// Must match the step key used by the initialization service.
        /// </summary>
        public string Name => "ResetAllSlaves";

        public ResetAllSlavesStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            string loggerInstance = "Track")
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;

            // Create dummy data container (same as legacy constructor).
            var dummyData = new byte[80];
            _sendMessageTemplate = new SendMessage(0, dummyData);
        }

        /// <summary>
        /// Execute one iteration of this step's state machine.
        /// The initialization service will call this method repeatedly,
        /// passing the most recent ReceivedMessage (if any).
        /// </summary>
        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            switch (_subState)
            {
                case 0:
                    return await ExecuteSendResetAsync(cancellationToken).ConfigureAwait(false);

                case 1:
                    return await ExecuteWaitForResetThenSendSlavesOnAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                case 2:
                    return ExecuteWaitForSlavesOn(lastMessage);

                default:
                    // Invalid internal state, log and fail the step.
                    IoC.Logger.Log(
                        "ResetAllSlavesStep encountered an invalid internal state.",
                        _loggerInstance);

                    _subState = 0;
                    return InitStepResult.Error("Invalid internal state in ResetAllSlavesStep.");
            }
        }

        /// <summary>
        /// State 0:
        /// Send EXEC_MBUS_STATE_RESET once to request a full MBUS reset.
        /// </summary>
        private async Task<InitStepResult> ExecuteSendResetAsync(CancellationToken cancellationToken)
        {
            var messageToSend = _sendMessageTemplate;
            messageToSend.Command = TrackCommand.EXEC_MBUS_STATE_RESET;

            await _commClient.SendAsync(messageToSend, cancellationToken).ConfigureAwait(false);

            _subState = 1;

            IoC.Logger.Log(
                "State.ResetAllSlaves => EXEC_MBUS_STATE_RESET.",
                _loggerInstance);

            // Keep running this step; we need to wait for a response.
            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 1:
        /// Wait for the MBUS to report MBUS_STATE_RESET with TaskStates.DONE.
        /// When that happens, send EXEC_MBUS_STATE_SLAVES_ON.
        /// </summary>
        private async Task<InitStepResult> ExecuteWaitForResetThenSendSlavesOnAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
            {
                // No new message yet, keep waiting.
                return InitStepResult.Continue();
            }

            if (lastMessage.Value.TaskId == TaskId.MBUS &&
                lastMessage.Value.Taskcommand == EnumMbusStatus.MBUS_STATE_RESET &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.ResetAllSlaves => MBUS_STATE_RESET.",
                    _loggerInstance);

                var messageToSend = _sendMessageTemplate;
                messageToSend.Command = TrackCommand.EXEC_MBUS_STATE_SLAVES_ON;

                await _commClient.SendAsync(messageToSend, cancellationToken).ConfigureAwait(false);

                _subState = 2;

                IoC.Logger.Log(
                    "State.ResetAllSlaves => EXEC_MBUS_STATE_SLAVES_ON.",
                    _loggerInstance);
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 2:
        /// Wait for MBUS_STATE_SLAVES_ON with TaskStates.DONE.
        /// When received, the step is completed.
        /// </summary>
        private InitStepResult ExecuteWaitForSlavesOn(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
            {
                return InitStepResult.Continue();
            }

            if (lastMessage.Value.TaskId == TaskId.MBUS &&
                lastMessage.Value.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVES_ON &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.ResetAllSlaves => MBUS_STATE_SLAVES_ON.",
                    _loggerInstance);

                _subState = 0;

                return InitStepResult.Next("DataUpload");
            }

            return InitStepResult.Continue();
        }
    }
}
