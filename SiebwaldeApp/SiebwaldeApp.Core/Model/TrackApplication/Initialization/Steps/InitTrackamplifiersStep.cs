namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that initializes all track amplifiers after firmware update.
    /// </summary>
    public sealed class InitTrackamplifiersStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly string _loggerInstance;

        private int _subState;
        private readonly SendMessage _sendMessageTemplate;

        public string Name => "InitTrackamplifiers";

        public InitTrackamplifiersStep(
            ITrackCommClient commClient,
            string loggerInstance = "Track")
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;

            var dummyData = new byte[80];
            _sendMessageTemplate = new SendMessage(0, dummyData);
        }

        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            switch (_subState)
            {
                case 0:
                    // Legacy: send an MBUS command to initialize slaves.
                    // Check the exact command in InitTrackamplifiers.cs and use that here.
                    var msg = _sendMessageTemplate;
                    msg.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_INIT; // TODO: verify exact command
                    await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                    _subState = 1;

                    IoC.Logger.Log(
                        "State.InitTrackamplifiers => EXEC_MBUS_STATE_SLAVE_INIT.",
                        _loggerInstance);

                    return InitStepResult.Continue();

                case 1:
                    if (!lastMessage.HasValue)
                        return InitStepResult.Continue();

                    // Legacy: check TaskId / Taskcommand / Taskstate for the init done condition.
                    // Replace EnumMbusStatus.MBUS_STATE_SLAVE_INIT_DONE and TaskId.MBUS with exact values.
                    if (lastMessage.Value.TaskId == TaskId.MBUS &&
                        lastMessage.Value.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_INIT &&
                        lastMessage.Value.Taskstate == TaskStates.DONE)
                    {
                        IoC.Logger.Log(
                            "State.InitTrackamplifiers => MBUS_STATE_SLAVE_INIT_DONE.",
                            _loggerInstance);

                        _subState = 0;
                        return InitStepResult.Next("EnableTrackamplifiers");
                    }

                    return InitStepResult.Continue();

                default:
                    IoC.Logger.Log(
                        "InitTrackamplifiersStep encountered an invalid internal state.",
                        _loggerInstance);
                    _subState = 0;
                    return InitStepResult.Error("Invalid internal state in InitTrackamplifiersStep.");
            }
        }
    }
}
