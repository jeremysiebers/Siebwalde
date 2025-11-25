namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that starts the MBUS data upload sequence.
    /// 
    /// Legacy equivalent: DataUpload (IAmplifierInitializersBaseClass).
    /// This step sends EXEC_MBUS_STATE_START_DATA_UPLOAD and waits for
    /// MBUS_STATE_START_DATA_UPLOAD with TaskStates.DONE.
    /// </summary>
    public sealed class DataUploadStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly string _loggerInstance;

        // Local state, equivalent to the legacy SubMethodState:
        // 0 = send EXEC_MBUS_STATE_START_DATA_UPLOAD
        // 1 = wait for MBUS_STATE_START_DATA_UPLOAD / DONE
        private int _subState;

        // Reusable SendMessage container, same as legacy mSendMessage.
        private readonly SendMessage _sendMessageTemplate;

        /// <summary>
        /// Name of this initialization step.
        /// Must match the key used by the initialization service.
        /// </summary>
        public string Name => "DataUpload";

        public DataUploadStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            string loggerInstance = "Track")
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;

            // Create dummy data container (same as in the legacy constructor).
            var dummyData = new byte[80];
            _sendMessageTemplate = new SendMessage(0, dummyData);
        }

        /// <summary>
        /// Executes one iteration of this step's state machine.
        /// The initialization service will call this method repeatedly,
        /// passing the last received control message (if any).
        /// </summary>
        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            switch (_subState)
            {
                case 0:
                    return await ExecuteSendStartDataUploadAsync(cancellationToken).ConfigureAwait(false);

                case 1:
                    return ExecuteWaitForStartDataUpload(lastMessage);

                default:
                    IoC.Logger.Log(
                        "DataUploadStep encountered an invalid internal state.",
                        _loggerInstance);

                    _subState = 0;
                    return InitStepResult.Error("Invalid internal state in DataUploadStep.");
            }
        }

        /// <summary>
        /// State 0:
        /// Send EXEC_MBUS_STATE_START_DATA_UPLOAD once.
        /// </summary>
        private async Task<InitStepResult> ExecuteSendStartDataUploadAsync(CancellationToken cancellationToken)
        {
            var messageToSend = _sendMessageTemplate;
            messageToSend.Command = TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD;

            await _commClient.SendAsync(messageToSend, cancellationToken).ConfigureAwait(false);

            _subState = 1;

            IoC.Logger.Log(
                "State.DataUploadStart => EXEC_MBUS_STATE_START_DATA_UPLOAD.",
                _loggerInstance);

            // Not done yet, continue and wait for the corresponding status message.
            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 1:
        /// Wait for MBUS_STATE_START_DATA_UPLOAD with TaskStates.DONE.
        /// When that happens, the step is completed.
        /// </summary>
        private InitStepResult ExecuteWaitForStartDataUpload(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
            {
                // No message yet, keep waiting.
                return InitStepResult.Continue();
            }

            if (lastMessage.Value.TaskId == TaskId.MBUS &&
                lastMessage.Value.Taskcommand == EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.DataUploadStart => MBUS_STATE_START_DATA_UPLOAD.",
                    _loggerInstance);

                _subState = 0;
                                
                return InitStepResult.Next("DetectSlaves");
            }

            return InitStepResult.Continue();
        }
    }
}
