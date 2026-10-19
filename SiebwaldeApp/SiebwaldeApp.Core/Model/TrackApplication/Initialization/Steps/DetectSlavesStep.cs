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

        // Explicit, operator-authorized recovery request. Recovery eligibility is driven by this
        // request alone, NOT by the number of detected amplifiers (count-independent). Without a
        // request, detection always proceeds to the normal deployment path.
        private readonly FirmwareRecoveryRequest? _recoveryRequest;

        public string Name => "DetectSlaves";

        public DetectSlavesStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            string loggerInstance = "Track",
            uint attemptMax = 3,
            FirmwareRecoveryRequest? recoveryRequest = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;
            _loopCounter = 1;
            _attemptMax = attemptMax;
            _recoveryRequest = recoveryRequest;

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
                    return await ExecuteEvaluateDetectionAsync(lastMessage, cancellationToken).ConfigureAwait(false);

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
        private async Task<InitStepResult> ExecuteEvaluateDetectionAsync(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
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

            await Task.Delay(2000); // waits 2 seconds for slave data to be ready

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

            // Count-independent recovery eligibility: the number of detected amplifiers no longer
            // gates recovery. Recovery is attempted only under an explicit, operator-authorized
            // recovery request; otherwise detection always proceeds to the normal deployment path
            // (normal-operation readiness, which legitimately requires every mapped amplifier, is a
            // separate concern handled later, not by a detection-count threshold here).
            if (_recoveryRequest is null)
            {
                _subState = 0;
                _loopCounter = 1;

                IoC.Logger.Log(
                    "State.DetectSlaves => no explicit recovery request; proceed to FlashFwTrackamplifiers.",
                    _loggerInstance);

                return InitStepResult.Next("FlashFwTrackamplifiers");
            }

            // Recovery requested: recovery success is recognized by the EXPLICIT target being
            // detected again, not by the fleet-wide detected count (count-independent). When the
            // target is back, recovery is complete and detection proceeds to the normal path.
            bool targetDetected =
                _variables.trackAmpItems[_recoveryRequest.TargetAmplifierAddress].SlaveDetected == 1;
            if (targetDetected)
            {
                _subState = 0;
                _loopCounter = 1;

                IoC.Logger.Log(
                    $"State.DetectSlaves => recovery target #{_recoveryRequest.TargetAmplifierAddress} detected; " +
                    "recovery complete, proceed to FlashFwTrackamplifiers.",
                    _loggerInstance);

                return InitStepResult.Next("FlashFwTrackamplifiers");
            }

            // Target still missing: bounded recovery attempts before giving up.
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
                $"State.DetectSlaves => explicit recovery request (target #{_recoveryRequest.TargetAmplifierAddress} still not detected), " +
                $"checking for one slave stuck in bootloader mode. Attempt {_loopCounter} of {_attemptMax}.",
                _loggerInstance);

            _subState = 0;
            _loopCounter++;

            // Try to recover the slave by running the recovery step.
            return InitStepResult.Next("RecoverSlaves");
        }
    }
}
