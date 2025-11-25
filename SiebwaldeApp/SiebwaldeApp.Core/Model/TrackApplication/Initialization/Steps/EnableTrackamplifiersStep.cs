namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Final initialization step that enables all track amplifiers.
    /// </summary>
    public sealed class EnableTrackamplifiersStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly string _loggerInstance;

        private int _subState;
        private readonly SendMessage _sendMessageTemplate;

        public string Name => "EnableTrackamplifiers";

        public EnableTrackamplifiersStep(
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
                    var msg = _sendMessageTemplate;
                    msg.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE;

                    await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                    _subState = 1;

                    IoC.Logger.Log(
                        "State.EnableTrackamplifiers => EXEC_MBUS_STATE_SLAVE_ENABLE.",
                        _loggerInstance);

                    return InitStepResult.Continue();

                case 1:
                    if (!lastMessage.HasValue)
                        return InitStepResult.Continue();

                    if (lastMessage.Value.TaskId == TaskId.MBUS &&
                        lastMessage.Value.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE &&
                        lastMessage.Value.Taskstate == TaskStates.DONE)
                    {
                        IoC.Logger.Log(
                            "State.EnableTrackamplifiers => MBUS_STATE_SLAVE_ENABLE DONE.",
                            _loggerInstance);

                        _subState = 0;
                        // This is the final step => Completed.
                        return InitStepResult.Completed();
                    }

                    return InitStepResult.Continue();

                default:
                    IoC.Logger.Log(
                        "EnableTrackamplifiersStep encountered an invalid internal state.",
                        _loggerInstance);
                    _subState = 0;
                    return InitStepResult.Error("Invalid internal state in EnableTrackamplifiersStep.");
            }
        }
    }
}
