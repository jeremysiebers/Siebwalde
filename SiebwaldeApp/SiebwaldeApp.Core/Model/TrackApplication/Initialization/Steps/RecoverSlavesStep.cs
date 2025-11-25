using System.Threading;

namespace SiebwaldeApp.Core
{ 
    /// <summary>
    /// Initialization step that tries to recover a single slave that is stuck
    /// in bootloader mode and re-flashes its firmware.
    ///
    /// Legacy equivalent: RecoverSlaves (IAmplifierInitializersBaseClass).
    /// The SubMethodState values and transitions are kept identical to the
    /// original implementation.
    /// </summary>
    public sealed class RecoverSlavesStep : IInitializationStep
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly SendNextFwDataPacket _sendNextFwDataPacket;
        private readonly TrackAmplifierBootloaderHelpers _bootloaderHelpers;
        private readonly string _loggerInstance;

        // Legacy fields 1:1
        private int _subState;
        private readonly SendMessage _sendMessageTemplate;
        private uint _loopCounter;
        private readonly uint _attemptMax;

        // Hex/bootloader related
        private int _iterationCounter;
        private readonly uint _processLines;
        private readonly uint _iterations;
        private readonly int _jumpSize;

        public string Name => "RecoverSlaves";

        public RecoverSlavesStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            SendNextFwDataPacket sendNextFwDataPacket,
            TrackAmplifierBootloaderHelpers bootloaderHelpers,
            string loggerInstance = "Track",
            uint attemptMax = 3)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _sendNextFwDataPacket = sendNextFwDataPacket ?? throw new ArgumentNullException(nameof(sendNextFwDataPacket));
            _bootloaderHelpers = bootloaderHelpers ?? throw new ArgumentNullException(nameof(bootloaderHelpers));
            _loggerInstance = loggerInstance ?? "Track";

            _subState = 0;
            _loopCounter = 1;
            _attemptMax = attemptMax;

            _processLines = (Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH;
            _iterations = ((Enums.PROGMEMSIZE - Enums.BOOTLOADEROFFSET) / Enums.HEXROWWIDTH) / Enums.JUMPSIZE;
            _jumpSize = (int)Enums.JUMPSIZE;

            var dummyData = new byte[80];
            _sendMessageTemplate = new SendMessage(0, dummyData);
        }

        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            switch (_subState)
            {
                // 0: start MBUS->FWHANDLER slave FW flash
                case 0:
                    return await State0_SendExecMbusStateSlaveFwFlashAsync(cancellationToken)
                        .ConfigureAwait(false);

                // 1: wait for FWHANDLERINIT / CONNECTED
                case 1:
                    return await State1_WaitForFwHandlerConnectedAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 2: GET_BOOTLOADER_VERSION + branch OK/NOK
                case 2:
                    return State2_HandleGetBootloaderVersion(lastMessage);

                // 3..12: full flash/download/checksum/config/reset FSM
                case 3:
                    return await State3_EraseFlashAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                case 4:
                    return State4_WaitEraseFlashDone(lastMessage);

                case 5:
                    return await State5_StartDownloadAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                case 6:
                    return State6_WaitStartDownloadDone(lastMessage);

                case 7:
                    return await State7_SendFwDataChunkAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                case 8:
                    return State8_WaitChecksumResult(lastMessage);

                case 9:
                    return await State9_SendConfigWordAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                case 10:
                    return await State10_WaitConfigWordStandby(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                case 11:
                    return await State11_WaitConfigWordDone(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                case 12:
                    return await State12_WaitSlaveFlashWritten(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                
                case 13:
                    return await State13_WaitSlaveConfigWritten(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                case 14:
                    return await State14_WaitSlaveChecksum(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                // 15: reset and exit handler, go back to DetectSlaves
                case 15:
                    return await State15_ResetSlaveAsync(lastMessage, cancellationToken)
                        .ConfigureAwait(false);

                default:
                    IoC.Logger.Log(
                        "RecoverSlavesStep encountered an invalid internal state.",
                        _loggerInstance);

                    _subState = 0;
                    _loopCounter = 1;
                    return InitStepResult.Error("Invalid internal state in RecoverSlavesStep.");
            }
        }

        #region State 0..2 (detect slave in bootloader)

        private async Task<InitStepResult> State0_SendExecMbusStateSlaveFwFlashAsync(CancellationToken cancellationToken)
        {
            var msg = _sendMessageTemplate;
            msg.Command = TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH;

            await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

            _subState = 1;

            IoC.Logger.Log(
                "State.DetectSlaveRecovery => EXEC_MBUS_STATE_SLAVE_FW_FLASH.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        private async Task<InitStepResult> State1_WaitForFwHandlerConnectedAsync(
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
                    "State.DetectSlaveRecovery => FWHANDLER CONNECTED.",
                    _loggerInstance);

                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_GET_BOOTLOADER_VERSION;

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 2;

                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => EXEC_FW_STATE_GET_BOOTLOADER_VERSION.",
                    _loggerInstance);
            }

            return InitStepResult.Continue();
        }

        private InitStepResult State2_HandleGetBootloaderVersion(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.GET_BOOTLOADER_VERSION &&
                (lastMessage.Value.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_OK ||
                 lastMessage.Value.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_NOK ||
                 lastMessage.Value.Taskcommand == TrackCommand.BOOTLOADER_START_BYTE_ERROR ||
                 lastMessage.Value.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_RECEIVE_DATA_TIMEOUT) &&
                (lastMessage.Value.Taskstate == TaskStates.DONE ||
                 lastMessage.Value.Taskstate == TaskStates.ERROR))
            {
                // Slave found in bootloader mode -> continue with flash
                if (lastMessage.Value.Taskcommand == TrackCommand.GET_BOOTLOADER_VERSION_OK &&
                    lastMessage.Value.Taskstate == TaskStates.DONE)
                {
                    IoC.Logger.Log(
                        "State.DetectSlaveRecovery => GET_BOOTLOADER_VERSION_OK.",
                        _loggerInstance);
                    IoC.Logger.Log(
                        "State.DetectSlaveRecovery => Found a slave in bootloader mode!",
                        _loggerInstance);
                    IoC.Logger.Log(
                        "State.DetectSlaveRecovery => Start flash sequence for this slave only.",
                        _loggerInstance);

                    _subState = 3;
                    return InitStepResult.Continue();
                }

                // No slave in bootloader, stop detection with error (same as legacy)
                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => GET_BOOTLOADER_VERSION_NOK.",
                    _loggerInstance);
                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => No slave found, stopping slave detection.",
                    _loggerInstance);

                _subState = 0;
                _loopCounter = 1;
                return InitStepResult.Error("No slave in bootloader mode found during recovery.");
            }

            return InitStepResult.Continue();
        }

        #endregion

        #region State 3..12 (erase, download FW, checksum, config)

        /// <summary>
        /// State 3:
        /// Legacy: send EXEC_FW_STATE_ERASE_FLASH and wait for erase flash result.
        /// </summary>
        private async Task<InitStepResult> State3_EraseFlashAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            // In the legacy code, SubMethodState==3 first sends EXEC_FW_STATE_ERASE_FLASH,
            // then waits for the response in the same or next call.
            // Here we simply send once when we enter this state.
            var msg = _sendMessageTemplate;
            msg.Command = TrackCommand.EXEC_FW_STATE_ERASE_FLASH;

            await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

            _subState = 4;

            IoC.Logger.Log(
                "State.DetectSlaveRecovery => EXEC_FW_STATE_ERASE_FLASH.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 4:
        /// Wait for ERASE_FLASH result and, on success, prepare the FW download loop.
        /// </summary>
        private InitStepResult State4_WaitEraseFlashDone(ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.ERASE_FLASH &&
                (lastMessage.Value.Taskcommand == TrackCommand.ERASE_FLASH_RETURNED_OK ||
                 lastMessage.Value.Taskcommand == TrackCommand.ERASE_FLASH_RETURNED_NOK) &&
                (lastMessage.Value.Taskstate == TaskStates.DONE ||
                 lastMessage.Value.Taskstate == TaskStates.ERROR))
            {
                if (lastMessage.Value.Taskcommand == TrackCommand.ERASE_FLASH_RETURNED_OK &&
                    lastMessage.Value.Taskstate == TaskStates.DONE)
                {
                    IoC.Logger.Log(
                        "State.DetectSlaveRecovery => ERASE_FLASH_RETURNED_OK.",
                        _loggerInstance);

                    // Prepare FW download loop (same as legacy: set IterationCounter, etc.)
                    _iterationCounter = 0;

                    _subState = 5;
                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => ERASE_FLASH_RETURNED_NOK.",
                    _loggerInstance);

                _subState = 0;
                _loopCounter = 1;
                return InitStepResult.Error("Erase flash failed during slave recovery.");
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 5:
        /// Start firmware download state on the slave.
        /// </summary>
        private async Task<InitStepResult> State5_StartDownloadAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            var msg = _sendMessageTemplate;
            msg.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE;

            await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

            _subState = 6;

            IoC.Logger.Log(
                "State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 6:
        /// Wait for START_FW_DOWNLOAD_* result and on success begin sending FW data rows.
        /// </summary>
        private InitStepResult State6_WaitStartDownloadDone(ReceivedMessage? lastMessage)
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
                        "State.DetectSlaveRecovery => FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY.",
                        _loggerInstance);

                    _sendNextFwDataPacket.Execute();

                    _subState = 7;

                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => START_FW_DOWNLOAD_NOK.",
                    _loggerInstance);

                _subState = 0;
                _loopCounter = 1;
                return InitStepResult.Error("Start FW download failed during slave recovery.");
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 7:
        /// Send next FW data block(s) using the SendNextFwDataPacket helper.
        /// The legacy code uses ProcessLines, Iterations and JUMPSIZE to step
        /// through the flash memory; we preserve that here.
        /// </summary>
        private async Task<InitStepResult> State7_SendFwDataChunkAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
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
                    "State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_FW_FILE Done.",
                    _loggerInstance);

                _subState = 8;

                return InitStepResult.Continue();
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 8:
        ///  Wait for checksum result of internal checksum number comparison to the sent data
        /// the intermediate FW download responses).
        /// </summary>
        private InitStepResult State8_WaitChecksumResult(ReceivedMessage? lastMessage)
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
                        "State.DetectSlaveRecovery => RECEIVED_CHECKSUM_OK.",
                        _loggerInstance);

                    _subState = 9;

                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => GET_FW_CHECKSUM_NOK.",
                    _loggerInstance);

                _subState = 0;
                _loopCounter = 1;
                return InitStepResult.Error("Firmware checksum mismatch during slave recovery.");
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 9:
        /// Send config word to slave.
        /// </summary>
        private async Task<InitStepResult> State9_SendConfigWordAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            var msg = _sendMessageTemplate;
            msg.Command = TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD;

            await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

            _subState = 10;

            IoC.Logger.Log(
                "State.DetectSlaveRecovery => EXEC_FW_STATE_RECEIVE_CONFIG_WORD.",
                _loggerInstance);

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 10:
        /// Wait for config word OK and then request reset.
        /// </summary>
        private async Task<InitStepResult> State10_WaitConfigWordStandby(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                lastMessage.Value.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {                
                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY.",
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
                "State.DetectSlaveRecovery => EXEC_FW_STATE_SENT_CONFIG_WORD.",
                _loggerInstance);

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 11;
                return InitStepResult.Continue();
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 11:
        /// Wait for config word OK and then request reset.
        /// </summary>
        private async Task<InitStepResult> State11_WaitConfigWordDone(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                lastMessage.Value.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.DetectSlaveRecovery =>  EXEC_FW_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE.",
                    _loggerInstance);

                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_WRITE_FLASH;

                IoC.Logger.Log(
                "State.DetectSlaveRecovery => EXEC_FW_STATE_WRITE_FLASH.",
                _loggerInstance);

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 12;

                return InitStepResult.Continue();                
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 12:
        /// Wait for flash written.
        /// </summary>
        private async Task<InitStepResult> State12_WaitSlaveFlashWritten (ReceivedMessage? lastMessage, CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.WRITE_FLASH &&
                lastMessage.Value.Taskcommand == TrackCommand.WRITE_FLASH_RETURNED_OK &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.DetectSlaveRecovery =>  WRITE_FLASH_RETURNED_OK.",
                    _loggerInstance);

                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_WRITE_CONFIG;

                IoC.Logger.Log(
                "State.DetectSlaveRecovery => EXEC_FW_STATE_WRITE_CONFIG.",
                _loggerInstance);

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 13;

                return InitStepResult.Continue();
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 13:
        /// Wait for flash written.
        /// </summary>
        private async Task<InitStepResult> State13_WaitSlaveConfigWritten(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.WRITE_CONFIG &&
                lastMessage.Value.Taskcommand == TrackCommand.WRITE_CONFIG_RETURNED_OK &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.DetectSlaveRecovery =>  WRITE_CONFIG_RETURNED_OK.",
                    _loggerInstance);

                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXEC_FW_STATE_CHECK_CHECKSUM;

                IoC.Logger.Log(
                "State.DetectSlaveRecovery => EXEC_FW_STATE_CHECK_CHECKSUM.",
                _loggerInstance);

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 14;

                return InitStepResult.Continue();
            }

            return InitStepResult.Continue();
        }

        /// <summary>
        /// State 14:
        /// Wait for flash written.
        /// </summary>
        private async Task<InitStepResult> State14_WaitSlaveChecksum(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.CHECK_CHECKSUM_CONFIG &&
                (lastMessage.Value.Taskcommand == TrackCommand.CHECK_CHECKSUM_CONFIG_RETURNED_OK || &&
                    lastMessage.Value.Taskcommand == TrackCommand.CHECK_CHECKSUM_CONFIG_RETURNED_NOK) &&
                (lastMessage.Value.Taskstate == TaskStates.DONE ||
                 lastMessage.Value.Taskstate == TaskStates.ERROR))
            {
                if (lastMessage.Value.Taskcommand == TrackCommand.CHECK_CHECKSUM_CONFIG_RETURNED_OK &&
                    lastMessage.Value.Taskstate == TaskStates.DONE)
                {
                    IoC.Logger.Log(
                    "State.DetectSlaveRecovery =>  CHECK_CHECKSUM_CONFIG_RETURNED_OK.",
                    _loggerInstance);

                    var msg = _sendMessageTemplate;
                    msg.Command = TrackCommand.EXEC_FW_STATE_SLAVE_RESET;

                    IoC.Logger.Log(
                    "State.DetectSlaveRecovery => EXEC_FW_STATE_SLAVE_RESET.",
                    _loggerInstance);

                    await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                    _subState = 15;

                    return InitStepResult.Continue();
                }

                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => CHECK_CHECKSUM_CONFIG_RETURNED_NOK.",
                    _loggerInstance);

                _subState = 0;
                _loopCounter = 1;
                return InitStepResult.Error("Slave checksum check failed during slave recovery.");

            }

            return InitStepResult.Continue();
        }

        #endregion

        #region State 15 (reset + exit handler + back to DetectSlaves)

        /// <summary>
        /// State 15:
        /// Wait for slave reset
        /// </summary>
        private async Task<InitStepResult> State15_ResetSlaveAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            if (lastMessage.Value.TaskId == TrackCommand.RESET_SLAVE &&
                lastMessage.Value.Taskcommand == TrackCommand.RESET_SLAVE_OK &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                IoC.Logger.Log(
                    "State.DetectSlaveRecovery => RESET_SLAVE_OK.",
                    _loggerInstance);

                // Legacy: then EXIT_SLAVExFWxHANDLER (we model that implicitly here).
                _loopCounter++;

                var msg = _sendMessageTemplate;
                msg.Command = TrackCommand.EXIT_SLAVExFWxHANDLER;

                IoC.Logger.Log(
                "State.DetectSlaveRecovery => EXIT_SLAVExFWxHANDLER.",
                _loggerInstance);

                await _commClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);

                _subState = 0;

                return InitStepResult.Next("DetectSlaves");
            }

            return InitStepResult.Continue();
        }
        #endregion
    }
}
