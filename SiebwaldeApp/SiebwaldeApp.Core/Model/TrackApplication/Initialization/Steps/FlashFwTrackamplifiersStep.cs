using System.Diagnostics;
using System.Threading;

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

        // Get enew stopwatch
        private Stopwatch sw = new Stopwatch();

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
                    return await State2_StartDownloadAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 3: start FW download state
                case 3:
                    return State3_WaitStartDownloadDone(lastMessage);

                // 4: send FW data blocks
                case 4:
                    return State4_SendFwDataChunk(lastMessage);

                // 5: verify FW checksum
                case 5:
                    return State5_WaitChecksumResult(lastMessage);

                // 6: set config word
                case 6:
                    return await State6_SendConfigWordAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 7: Wait for config word standby
                case 7:
                    return await State7_WaitConfigWordStandbyAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 8: Wait for config word done
                case 8:
                    return await State8_WaitConfigWordDoneAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 9: Wait for all slaves flashed
                case 9:
                    return State9_WaitAllFlashed(lastMessage);

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

        #region State 1..9: connect FW handler / send FW file / check checksum / send config / start flash

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
                msg.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 2;

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE.",
                    _loggerInstance);
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 2:
        /// Start firmware download state on the slave.
        /// </summary>
        private async Task<InitStepResult> State2_StartDownloadAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            var msg = _sendMessageTemplate;
            msg.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;

            await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

            _subState = 3;

            IoC.Logger.Log(
                "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 3:
        /// Wait for START_FW_DOWNLOAD_* result and on success begin sending FW data rows.
        /// </summary>
        private InitStepResult State3_WaitStartDownloadDone(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                lastMessage.Value.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                if (lastMessage.Value.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                    lastMessage.Value.Taskstate == TaskStates.DONE)
                {
                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY.",
                        _loggerInstance);

                    _sendNextFwDataPacket.Execute();

                    _subState = 4;

                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => START_FW_DOWNLOAD_NOK.",
                    _loggerInstance);

                _subState = 0;
                return InitStepResult.Error("Start FW download failed during slave recovery.");
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 4:
        /// Send next FW data block(s) using the SendNextFwDataPacket helper.
        /// The legacy code uses ProcessLines, Iterations and JUMPSIZE to step
        /// through the flash memory; we preserve that here.
        /// </summary>
        private InitStepResult State4_SendFwDataChunk(
            ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                lastMessage.Value.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                // Use your existing helper exactly as in RecoverSlaves case 6.
                _sendNextFwDataPacket.Execute();
            }
            else if (lastMessage.Value.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                     lastMessage.Value.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE &&
                     lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                // FW data download done
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE Done.",
                    _loggerInstance);

                _subState = 5;

                return InitStepResult.Continue();
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 5:
        ///  Wait for checksum result of internal checksum number comparison to the sent data
        /// the intermediate FW download responses).
        /// </summary>
        private InitStepResult State5_WaitChecksumResult(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                lastMessage.Value.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                lastMessage.Value.Taskstate == TaskStates.DONE &&
                (lastMessage.Value.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_OK) ||
                 lastMessage.Value.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_NOK)
            {
                if (lastMessage.Value.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                    lastMessage.Value.Taskstate == TaskStates.DONE &&
                    lastMessage.Value.Taskmessage == TaskMessages.RECEIVED_CHECKSUM_OK)
                {
                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => RECEIVED_CHECKSUM_OK.",
                        _loggerInstance);

                    _subState = 6;

                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => GET_FW_CHECKSUM_NOK.",
                    _loggerInstance);

                _subState = 0;
                return InitStepResult.Error("Firmware checksum mismatch during slave recovery.");
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 6:
        /// Send config word to slave.
        /// </summary>
        private async Task<InitStepResult> State6_SendConfigWordAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            var msg = _sendMessageTemplate;
            msg.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD;

            await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

            _subState = 7;

            IoC.Logger.Log(
                "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_CONFIG_WORD.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 7:
        /// Wait for config word OK and then request reset.
        /// </summary>
        private async Task<InitStepResult> State7_WaitConfigWordStandbyAsync(ReceivedMessage? lastMessage, 
            CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                lastMessage.Value.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY.",
                    _loggerInstance);

                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE;

                List<byte> Data = new List<byte>();

                foreach (byte val in _bootloaderHelpers.GetConfigWord)
                {
                    Data.Add(val);
                }
                msg.Data = Data.ToArray();

                IoC.Logger.Log(
                "State.FlashFwTrackamplifiers => EXEC_FW_STATE_SENT_CONFIG_WORD.",
                _loggerInstance);

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 8;
                return InitStepResult.Continue();
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 8:
        /// Wait for config word OK and then request reset.
        /// </summary>
        private async Task<InitStepResult> State8_WaitConfigWordDoneAsync(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                lastMessage.Value.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers =>  EXEC_FW_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE.",
                    _loggerInstance);

                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES;

                IoC.Logger.Log(
                "State.FlashFwTrackamplifiers => EXEC_FW_STATE_FLASH_ALL_SLAVES.",
                _loggerInstance);

                // Start stopwatch for measuring flash all slaves
                sw.Start();

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 9;

                return InitStepResult.Continue();
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 9:
        ///  Wait for all amplifiers are flashed
        /// </summary>
        private InitStepResult State9_WaitAllFlashed(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TaskId.FWHANDLER &&
                lastMessage.Value.Taskcommand == TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                long elapsedtime = sw.ElapsedMilliseconds;
                sw.Stop();
                IoC.Logger.Log("State.FlashFwTrackamplifiers => Flashing took " 
                    + Convert.ToString(elapsedtime / 1000) 
                    + " seconds.", _loggerInstance);

                IoC.Logger.Log("State.FlashFwTrackamplifiers => That is on average " 
                    + Convert.ToString((float)elapsedtime / _fwFlashRequired / 1000) 
                    + " seconds per slave.", _loggerInstance);

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => EXEC_FW_STATE_FLASH_ALL_SLAVES DONE.",
                    _loggerInstance);

                _subState = 0;

                return InitStepResult.Next("InitTrackamplifiers");
            }

            return InitStepResult.Continue();
        }
        #endregion
    }
}
