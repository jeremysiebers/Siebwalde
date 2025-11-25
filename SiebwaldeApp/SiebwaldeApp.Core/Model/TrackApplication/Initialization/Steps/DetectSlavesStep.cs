namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that detects all MBUS slaves and decides
    /// whether recovery is needed or we can proceed to firmware flashing.
    /// 
    /// Legacy equivalent: DetectSlaves (IAmplifierInitializersBaseClass).
    /// </summary>
    public sealed class DetectSlavesStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly string _loggerInstance;

        // Local state similar to legacy SubMethodState:
        // 0 = send EXEC_MBUS_STATE_SLAVE_DETECT
        // 1 = wait for MBUS_STATE_SLAVE_DETECT / DONE and evaluate results
        private int _subState;

        private readonly SendMessage _sendMessageTemplate;

        // Detection loop counter and maximum attempts (legacy: loopcounter / attemptmax)
        private uint _loopCounter;
        private readonly uint _attemptMax;

        // Threshold for "enough slaves detected".
        // In the legacy code this was effectively > 2.
        private readonly uint _minRequiredSlaves = 3;

        public string Name => "DetectSlaves";

        public DetectSlavesStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            string loggerInstance = "Track",
            uint attemptMax = 3)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;
            _loopCounter = 1;
            _attemptMax = attemptMax;

            // Create dummy data container (like legacy constructor).
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
                    return await ExecuteSendSlaveDetectAsync(cancellationToken).ConfigureAwait(false);

                case 1:
                    return ExecuteEvaluateDetection(lastMessage);

                default:
                    _subState = 0;
                    _loopCounter = 1;

                    IoC.Logger.Log(
                        "DetectSlavesStep encountered an invalid internal state.",
                        _loggerInstance);

                    return InitStepResult.Error("Invalid internal state in DetectSlavesStep.");
            }
        }

        /// <summary>
        /// State 0:
        /// Send EXEC_MBUS_STATE_SLAVE_DETECT once.
        /// </summary>
        private async Task<InitStepResult> ExecuteSendSlaveDetectAsync(CancellationToken cancellationToken)
        {
            var msg = _sendMessageTemplate;
            msg.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT;

            await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

            _subState = 1;

            IoC.Logger.Log(
                "State.DetectSlaves => EXEC_MBUS_STATE_SLAVE_DETECT.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 1:
        /// Wait for MBUS_STATE_SLAVE_DETECT / DONE, then count detected slaves
        /// and decide whether to continue with flashing or try recovery.
        /// </summary>
        private InitStepResult ExecuteEvaluateDetection(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
            {
                // No MBUS status yet, keep waiting.
                return InitStepResult.Continue();
            }

            if (lastMessage.Value.TaskId != TaskId.MBUS ||
                lastMessage.Value.Taskcommand != EnumMbusStatus.MBUS_STATE_SLAVE_DETECT ||
                lastMessage.Value.Taskstate != TaskStates.DONE)
            {
                // Not the message we are interested in, keep waiting.
                return InitStepResult.Continue();
            }

            IoC.Logger.Log(
                "State.DetectSlaves => MBUS_STATE_SLAVE_DETECT.",
                _loggerInstance);

            // Count all detected slaves (legacy: amplifier.SlaveDetected == 1)
            uint slaveCount = 0;
            foreach (TrackAmplifierItem amplifier in _variables.trackAmpItems)
            {
                if (amplifier.SlaveDetected == 1)
                {
                    slaveCount++;
                    IoC.Logger.Log(
                        $"State.DetectSlaves => Slave {amplifier.SlaveNumber} detected.",
                        _loggerInstance);
                }
            }

            IoC.Logger.Log(
                $"State.DetectSlaves => {slaveCount} slaves in total detected.",
                _loggerInstance);

            if (slaveCount >= _minRequiredSlaves)
            {
                // We have enough slaves; in the legacy code this resulted in:
                //  - Finished + "Remove:RecoverSlaves"
                // Here we directly jump to the next logical step: FlashFwTrackamplifiers.
                _subState = 0;
                _loopCounter = 1;

                IoC.Logger.Log(
                    "State.DetectSlaves => enough slaves detected, proceed to FlashFwTrackamplifiers.",
                    _loggerInstance);

                return InitStepResult.Next("FlashFwTrackamplifiers");
            }

            // Not enough slaves detected: either we exceeded attempts or we should try recovery.
            if (_loopCounter > _attemptMax)
            {
                IoC.Logger.Log(
                    $"State.DetectSlaves => more than {_attemptMax} recovery attempts, stopping.",
                    _loggerInstance);

                _subState = 0;
                _loopCounter = 1;
                return InitStepResult.Error("Maximum number of slave detection attempts exceeded.");
            }

            IoC.Logger.Log(
                $"State.DetectSlaves => not enough slaves, checking for one slave stuck in bootloader mode. " +
                $"Attempt {_loopCounter} of {_attemptMax}.",
                _loggerInstance);

            _subState = 0;
            _loopCounter++;

            // Try to recover the slave by running the recovery step.
            return InitStepResult.Next("RecoverSlaves");
        }
    }
}
