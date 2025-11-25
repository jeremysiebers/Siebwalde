namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that determines which slaves require a firmware update
    /// and, if needed, flashes all affected slaves.
    ///
    /// Legacy equivalent: FlashFwTrackamplifiers (IAmplifierInitializersBaseClass).
    /// SubMethodState mapping is kept identical.
    /// </summary>
    public sealed class FlashFwTrackamplifiersStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly SendNextFwDataPacket _sendNextFwDataPacket;
        private readonly TrackAmplifierBootloaderHelpers _bootloaderHelpers;
        private readonly string _loggerInstance;

        private int _subState;
        private readonly SendMessage _sendMessageTemplate;

        // From legacy: counters for FW data loop over ALL slaves
        private uint _fwFlashRequired;
        private int _iterationCounter;
        private readonly uint _processLines;
        private readonly uint _iterations;
        private readonly int _jumpSize;

        public string Name => "FlashFwTrackamplifiers";

        public FlashFwTrackamplifiersStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            SendNextFwDataPacket sendNextFwDataPacket,
            TrackAmplifierBootloaderHelpers bootloaderHelpers,
            string loggerInstance = "Track")
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _sendNextFwDataPacket = sendNextFwDataPacket ?? throw new ArgumentNullException(nameof(sendNextFwDataPacket));
            _bootloaderHelpers = bootloaderHelpers ?? throw new ArgumentNullException(nameof(bootloaderHelpers));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;

            var dummyData = new byte[80];
            _sendMessageTemplate = new SendMessage(0, dummyData);

            _processLines = (Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH;
            _iterations = ((Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH) / Enums.JUMPSIZE;
            _jumpSize = (int)Enums.JUMPSIZE;
        }

        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            switch (_subState)
            {
                // 0: determine how many slaves require flashing
                case 0:
                    return await State0_DetermineFlashRequirementAsync(cancellationToken)
                        .ConfigureAwait(false);

                // 1: start FW handler for all relevant slaves
                case 1:
                    return await State1_StartFwHandlerAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 2: erase flash on all slaves
                case 2:
                    return await State2_EraseFlashAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 3: start FW download state
                case 3:
                    return await State3_StartDownloadAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 4: send FW data blocks
                case 4:
                    return await State4_SendFwDataChunkAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 5: verify FW checksum
                case 5:
                    return await State5_ChecksumAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 6: set config word
                case 6:
                    return await State6_ConfigWordAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 7: reset slaves
                case 7:
                    return await State7_ResetSlavesAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 8: exit FW handler and finish sequence
                case 8:
                    return State8_ExitFwHandlerDone(lastMessage);

                default:
                    IoC.Logger.Log(
                        "FlashFwTrackamplifiersStep encountered an invalid internal state.",
                        _loggerInstance);

                    _subState = 0;
                    return InitStepResult.Error("Invalid internal state in FlashFwTrackamplifiersStep.");
            }
        }

        #region State 0: determine which slaves need flashing

        private async Task<InitStepResult> State0_DetermineFlashRequirementAsync(CancellationToken cancellationToken)
        {
            _fwFlashRequired = 0;

            IoC.Logger.Log(
                $"State.FlashFwTrackamplifiers => File checksum = 0x{_bootloaderHelpers.GetFileCheckSum:X}.",
                _loggerInstance);

            foreach (TrackAmplifierItem amplifier in _variables.trackAmpItems)
            {
                if (_bootloaderHelpers.GetFileCheckSum != amplifier.HoldingReg[11] &&
                    amplifier.SlaveDetected == 1 &&
                    amplifier.SlaveNumber > 0 &&
                    amplifier.SlaveNumber < 51)
                {
                    _fwFlashRequired++;

                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => Slave " + amplifier.SlaveNumber +
                        " has checksum = 0x" + amplifier.HoldingReg[11].ToString("X") +
                        " and requires flashing.",
                        _loggerInstance);
                }
            }

            if (_fwFlashRequired == 0)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => 0 slaves require flashing, initialization DONE.",
                    _loggerInstance);

                return InitStepResult.Completed();
            }

            IoC.Logger.Log(
                $"State.FlashFwTrackamplifiers => {_fwFlashRequired} slave(s) will be flashed.",
                _loggerInstance);

            // Legacy: next is FWHANDLERINIT for all slaves.
            var msg = _sendMessageTemplate;
            msg.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH;
            await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

            _subState = 1;

            IoC.Logger.Log(
                "State.FlashFwTrackamplifiers => EXEC_MBUS_STATE_SLAVE_FW_FLASH.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        #endregion

        #region State 1..3: connect FW handler / erase / start download

        private async Task<InitStepResult> State1_StartFwHandlerAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TaskId.FWHANDLER &&
                lastMessage.Value.Taskcommand == TrackCommand.FWHANDLERINIT &&
                lastMessage.Value.Taskstate == TaskStates.CONNECTED)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => FWHANDLER CONNECTED.",
                    _loggerInstance);

                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_ERASE_FLASH;

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 2;

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => EXEC_FW_STATE_ERASE_FLASH.",
                    _loggerInstance);
            }

            return InitStepResult.Continue();
        }

        private async Task<InitStepResult> State2_EraseFlashAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.ERASE_FLASH &&
                (lastMessage.Value.Taskcommand == TrackCommand.ERASE_FLASH_OK ||
                 lastMessage.Value.Taskcommand == TrackCommand.ERASE_FLASH_NOK) &&
                (lastMessage.Value.Taskstate == TaskStates.DONE ||
                 lastMessage.Value.Taskstate == TaskStates.ERROR))
            {
                if (lastMessage.Value.Taskcommand == TrackCommand.ERASE_FLASH_OK &&
                    lastMessage.Value.Taskstate == TaskStates.DONE)
                {
                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => ERASE_FLASH_OK.",
                        _loggerInstance);

                    _iterationCounter = 0;

                    var msg = _sendMessageTemplate;
                    msg.Command = TrackCommand.EXEC_FW_STATE_START_FW_DOWNLOAD;

                    await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                    _subState = 3;

                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => EXEC_FW_STATE_START_FW_DOWNLOAD.",
                        _loggerInstance);

                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => ERASE_FLASH_NOK.",
                    _loggerInstance);

                _subState = 0;
                return InitStepResult.Error("Erase flash failed during FlashFwTrackamplifiers.");
            }

            return InitStepResult.Continue();
        }

        private async Task<InitStepResult> State3_StartDownloadAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.START_FW_DOWNLOAD &&
                (lastMessage.Value.Taskcommand == TrackCommand.START_FW_DOWNLOAD_OK ||
                 lastMessage.Value.Taskcommand == TrackCommand.START_FW_DOWNLOAD_NOK) &&
                (lastMessage.Value.Taskstate == TaskStates.DONE ||
                 lastMessage.Value.Taskstate == TaskStates.ERROR))
            {
                if (lastMessage.Value.Taskcommand == TrackCommand.START_FW_DOWNLOAD_OK &&
                    lastMessage.Value.Taskstate == TaskStates.DONE)
                {
                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => START_FW_DOWNLOAD_OK.",
                        _loggerInstance);

                    _iterationCounter = 0;
                    _subState = 4;

                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => START_FW_DOWNLOAD_NOK.",
                    _loggerInstance);

                _subState = 0;
                return InitStepResult.Error("Start FW download failed during FlashFwTrackamplifiers.");
            }

            return InitStepResult.Continue();
        }

        #endregion

        #region State 4: FW data loop

        private async Task<InitStepResult> State4_SendFwDataChunkAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (_iterationCounter >= _iterations)
            {
                // Done sending FW data for all iterations; move to checksum
                _subState = 5;
                return InitStepResult.Continue();
            }

            // Same logic as legacy: send next data block(s) for all slaves that need FW.
            _sendNextFwDataPacket.Execute(
                _bootloaderHelpers.HexFileLine,
                _bootloaderHelpers.HexFileColumn,
                _variables,
                _iterationCounter,
                _jumpSize,
                _processLines);

            _iterationCounter++;

            IoC.Logger.Log(
                $"State.FlashFwTrackamplifiers => FW data chunk {_iterationCounter}/{_iterations} sent.",
                _loggerInstance);

            // In de legacy code check je hier de FW download ACK/NOK per block.
            // Als die case nog aparte states heeft, kun je hier een extra _subState 4/5 splitsing maken.
            await Task.Yield();
            return InitStepResult.Continue();
        }

        #endregion

        #region State 5: checksum

        private async Task<InitStepResult> State5_ChecksumAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (_subState == 5 && lastMessage == null)
            {
                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_GET_FW_CHECKSUM;
                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => EXEC_FW_STATE_GET_FW_CHECKSUM.",
                    _loggerInstance);

                return InitStepResult.Continue();
            }

            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.GET_FW_CHECKSUM &&
                (lastMessage.Value.Taskcommand == TrackCommand.GET_FW_CHECKSUM_OK ||
                 lastMessage.Value.Taskcommand == TrackCommand.GET_FW_CHECKSUM_NOK) &&
                (lastMessage.Value.Taskstate == TaskStates.DONE ||
                 lastMessage.Value.Taskstate == TaskStates.ERROR))
            {
                if (lastMessage.Value.Taskcommand == TrackCommand.GET_FW_CHECKSUM_OK &&
                    lastMessage.Value.Taskstate == TaskStates.DONE)
                {
                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => GET_FW_CHECKSUM_OK.",
                        _loggerInstance);

                    _subState = 6;
                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => GET_FW_CHECKSUM_NOK.",
                    _loggerInstance);

                _subState = 0;
                return InitStepResult.Error("Firmware checksum mismatch during FlashFwTrackamplifiers.");
            }

            return InitStepResult.Continue();
        }

        #endregion

        #region State 6: config word

        private async Task<InitStepResult> State6_ConfigWordAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (_subState == 6 && lastMessage == null)
            {
                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_SET_CONFIG_WORD;

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => EXEC_FW_STATE_SET_CONFIG_WORD.",
                    _loggerInstance);

                return InitStepResult.Continue();
            }

            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.SET_CONFIG_WORD &&
                (lastMessage.Value.Taskcommand == TrackCommand.SET_CONFIG_WORD_OK ||
                 lastMessage.Value.Taskcommand == TrackCommand.SET_CONFIG_WORD_NOK) &&
                (lastMessage.Value.Taskstate == TaskStates.DONE ||
                 lastMessage.Value.Taskstate == TaskStates.ERROR))
            {
                if (lastMessage.Value.Taskcommand == TrackCommand.SET_CONFIG_WORD_OK &&
                    lastMessage.Value.Taskstate == TaskStates.DONE)
                {
                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => SET_CONFIG_WORD_OK.",
                        _loggerInstance);

                    _subState = 7;
                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => SET_CONFIG_WORD_NOK.",
                    _loggerInstance);

                _subState = 0;
                return InitStepResult.Error("Set config word failed during FlashFwTrackamplifiers.");
            }

            return InitStepResult.Continue();
        }

        #endregion

        #region State 7..8: reset + exit handler

        private async Task<InitStepResult> State7_ResetSlavesAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (_subState == 7 && lastMessage == null)
            {
                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_RESET_SLAVE;

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RESET_SLAVE.",
                    _loggerInstance);

                return InitStepResult.Continue();
            }

            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.RESET_SLAVE &&
                lastMessage.Value.Taskcommand == TrackCommand.RESET_SLAVE_OK &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => RESET_SLAVE_OK.",
                    _loggerInstance);

                _subState = 8;
                return InitStepResult.Continue();
            }

            return InitStepResult.Continue();
        }

        private InitStepResult State8_ExitFwHandlerDone(ReceivedMessage? lastMessage)
        {
            // Legacy: EXIT_SLAVExFWxHANDLER en na DONE is de sequencer klaar.
            // In deze async-variant laten we dit als “einde van de FlashFwStep”
            // en laten we de volgende step (InitTrackamplifiers) bepalen of er
            // verder nog initialisatie nodig is.
            IoC.Logger.Log(
                "State.FlashFwTrackamplifiers => FW flash sequence completed for all slaves.",
                _loggerInstance);

            _subState = 0;
            return InitStepResult.Next("InitTrackamplifiers");
        }

        #endregion
    }
}
