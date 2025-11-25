namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that connects to the Ethernet target (track controller).
    /// 
    /// Old equivalent: ConnectToEthernetTarget (IAmplifierInitializersBaseClass).
    /// This class is the async/modern replacement that implements IInitializationStep.
    /// </summary>
    public sealed class ConnectToEthernetTargetStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly string _loggerInstance;

        // Local state, similar to old SubMethodState (0 = send request, 1 = wait for response).
        private int _subState;

        // Reusable SendMessage container, similar to the old mSendMessage field.
        private readonly SendMessage _sendMessageTemplate;

        /// <summary>
        /// Name of this initialization step.
        /// Must match the key used by the initialization service (e.g. "ConnectToEthernetTarget").
        /// </summary>
        public string Name => "ConnectToEthernetTarget";

        public ConnectToEthernetTargetStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            string loggerInstance = "Track")
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;

            // Create dummy buffer, as done in the legacy constructor.
            var dummyData = new byte[80];
            _sendMessageTemplate = new SendMessage(0, dummyData);
        }

        /// <summary>
        /// Executes the step logic.
        /// This method is called repeatedly by the initialization service.
        /// 
        /// The behavior is:
        /// - State 0: send CLIENT_CONNECTION_REQUEST once, then move to state 1.
        /// - State 1: wait for a controller message that confirms the connection.
        /// </summary>
        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            switch (_subState)
            {
                case 0:
                    return await ExecuteSendConnectionRequestAsync(cancellationToken).ConfigureAwait(false);

                case 1:
                    return ExecuteWaitForConnection(lastMessage);

                default:
                    // Invalid state, treat as error and reset.
                    _subState = 0;
                    IoC.Logger.Log(
                        "ConnectToEthernetTargetStep encountered an invalid internal state.",
                        _loggerInstance);
                    return InitStepResult.Error("Invalid internal state in ConnectToEthernetTargetStep.");
            }
        }

        /// <summary>
        /// State 0: send the CLIENT_CONNECTION_REQUEST once.
        /// </summary>
        private async Task<InitStepResult> ExecuteSendConnectionRequestAsync(CancellationToken cancellationToken)
        {
            // Copy the behavior from old ConnectToEthernetTarget case 0.
            var messageToSend = _sendMessageTemplate;
            messageToSend.Command = EnumClientCommands.CLIENT_CONNECTION_REQUEST;

            await _commClient.SendAsync(messageToSend, cancellationToken).ConfigureAwait(false);

            _subState = 1;

            IoC.Logger.Log(
                "State.ConnectToEthernetTarget => CLIENT_CONNECTION_REQUEST.",
                _loggerInstance);

            // This step is not done yet, the next call should wait for a response.
            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 1: wait for the controller to report CONNECTED and DONE.
        /// </summary>
        private InitStepResult ExecuteWaitForConnection(ReceivedMessage? lastMessage)
        {
            if (lastMessage == null)
            {
                // No new message yet, keep waiting.
                return InitStepResult.Continue();
            }

            // Copy condition from old code:
            // if (receivedMessage.TaskId == TaskId.CONTROLLER &&
            //     receivedMessage.Taskcommand == TaskStates.CONNECTED &&
            //     receivedMessage.Taskstate == TaskStates.DONE)
            if (lastMessage.Value.TaskId == TaskId.CONTROLLER &&
                lastMessage.Value.Taskcommand == TaskStates.CONNECTED &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                _subState = 0;

                // Optionally update a model flag (if you later add such a property).
                // Example:
                // _variables.TrackControllerConnected = true;

                IoC.Logger.Log(
                    "State.ConnectToEthernetTarget => CLIENT_CONNECTED.",
                    _loggerInstance);

                // We want to continue with the next step in the sequence.
                // The name "ResetAllSlaves" must correspond to another IInitializationStep.Name.
                return InitStepResult.Next("ResetAllSlaves");
            }

            // Message is not the one we are looking for, keep waiting.
            return InitStepResult.Continue();
        }
    }
}
