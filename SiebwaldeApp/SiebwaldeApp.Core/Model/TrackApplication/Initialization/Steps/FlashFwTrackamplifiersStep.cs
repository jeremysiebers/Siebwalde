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
        private readonly FirmwareDeploymentAuthorization? _firmwareDeployment;

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
            string loggerInstance = "Track",
            FirmwareDeploymentAuthorization? firmwareDeployment = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _sendNextFwDataPacket = sendNextFwDataPacket ?? throw new ArgumentNullException(nameof(sendNextFwDataPacket));
            _bootloaderHelpers = bootloaderHelpers ?? throw new ArgumentNullException(nameof(bootloaderHelpers));
            _loggerInstance = loggerInstance ?? "Track";
            _firmwareDeployment = firmwareDeployment;

            _subState = 0;

            var dummyData = new byte[80];
            _sendMessageTemplate = new SendMessage(0, dummyData);

            // First execute the bootloader helper to prepare the FW file checksum and config word.
            _bootloaderHelpers.Execute();
        }

        public async Task<InitStepResult> ExecuteAsync(
            ReceivedMessage? lastMessage,
            CancellationToken cancellationToken)
        {
            // Defense-in-depth: the destructive flash FSM (states 1..9) is reachable ONLY under an
            // explicit, session-scoped deployment authorization. State 0 (detect/require) is the
            // only state reachable through the automatic/manual init path and never enters the
            // flash sequence; it reports a mismatch as Blocked instead. No 103/120/121/122/ERASE/
            // PROGRAM can be emitted without authorization.
            if (_subState >= 1 && _firmwareDeployment?.Authorized != true)
            {
                IoC.Logger.Log(
                    "FlashFwTrackamplifiersStep: destructive flash sequence refused (not authorized).",
                    _loggerInstance);

                _subState = 0;
                return InitStepResult.Blocked(
                    "Firmware flash is not authorized: the destructive flash sequence requires an explicit operator authorization.");
            }

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

                //// 2: erase flash on all slaves
                //case 2:
                //    return await State2_StartDownloadAsync(lastMessage, cancellationToken)
                //        .ConfigureAwait(false);

                // 3: start FW download state
                case 3:
                    return await State3_WaitStartDownloadDoneAsync(lastMessage)
                        .ConfigureAwait(false);

                // 4: send FW data blocks
                case 4:
                    return await State4_SendFwDataChunkAsync(lastMessage)
                        .ConfigureAwait(false);

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

        private Task<InitStepResult> State0_DetermineFlashRequirementAsync(CancellationToken cancellationToken)
        {
            _fwFlashRequired = 0;

            if(!_bootloaderHelpers.HexFileReadSuccessful)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => HexFileReadSuccessful == False.",
                    _loggerInstance);
                return Task.FromResult(InitStepResult.Error("Firmware file read unsuccessful."));
            }

            // Get checksum for comparison
            var checksum = _bootloaderHelpers.GetFileCheckSum;

            IoC.Logger.Log(
                $"State.FlashFwTrackamplifiers => File checksum = 0x{_bootloaderHelpers.GetFileCheckSum:X}.",
                _loggerInstance);

            // Legacy-artifact diagnosis (0x5784 / 0x251F / 0xE906 / 0x5263 / 0xFFC2 -> 0x0E39):
            // when the CONFIGURED artifact resolves to a KNOWN LEGACY checksum, it points at a
            // legacy artifact rather than the intended 0x0E39 release. Detect + report + block;
            // never silently match file<->amp checksum and proceed, never auto-migrate the path and
            // never auto-flash.
            if (IsLegacyFirmwareChecksum(checksum))
            {
                IoC.Logger.Log(
                    $"State.FlashFwTrackamplifiers => configured TrackAmplifierFwPath points at legacy 0x{checksum:X4}, not the intended 0x0E39 release.",
                    _loggerInstance);

                return Task.FromResult(InitStepResult.Blocked(
                    $"configured TrackAmplifierFwPath points at legacy 0x{checksum:X4}, not the intended 0x0E39 release."));
            }

            var mismatches = new List<FirmwareAmpMismatch>();
            foreach (TrackAmplifierItem amplifier in _variables.trackAmpItems)
            {
                if (checksum != amplifier.HoldingReg[TrackAmplifierRegisters.SwChecksum] &&
                    amplifier.SlaveDetected == 1 &&
                    amplifier.SlaveNumber > 0 &&
                    amplifier.SlaveNumber < 51)
                {
                    var actual = amplifier.HoldingReg[TrackAmplifierRegisters.SwChecksum];
                    _fwFlashRequired++;
                    mismatches.Add(new FirmwareAmpMismatch((byte)amplifier.SlaveNumber, actual));

                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => Slave " + amplifier.SlaveNumber +
                        " has checksum = 0x" + actual.ToString("X") +
                        " and requires flashing.",
                        _loggerInstance);
                }
            }

            if (_fwFlashRequired == 0)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => 0 slaves require flashing, initialization DONE.",
                    _loggerInstance);

                return Task.FromResult(InitStepResult.Next("InitTrackamplifiers"));
            }

            // SAFETY: a firmware checksum mismatch must NEVER auto-trigger destructive flashing.
            // Detect + report the mismatch, record it for the operator, and FAIL CLOSED (Blocked).
            // The flash sequence (103/120/121/122/ERASE/PROGRAM) is not entered and nothing is sent.
            var expectedSha256 = TryComputeSha256();
            _firmwareDeployment?.RecordMismatch(checksum, expectedSha256, mismatches);

            var ampDetail = string.Join(", ", mismatches
                .Select(m => $"amp {m.Amplifier} (actual 0x{m.ActualChecksum:X4})"));
            var message =
                $"Firmware checksum mismatch on {mismatches.Count} detected amplifier(s): {ampDetail}; " +
                $"expected checksum 0x{checksum:X4}" +
                (string.IsNullOrEmpty(expectedSha256) ? string.Empty : $", expected SHA-256 {expectedSha256}") +
                ". Firmware deployment is NOT automatic; authorize an explicit per-amplifier " +
                "deployment (command 131) or correct the configured firmware. No flash command was sent.";

            IoC.Logger.Log("State.FlashFwTrackamplifiers => " + message, _loggerInstance);

            return Task.FromResult(InitStepResult.Blocked(message));
        }

        /// <summary>
        /// Best-effort SHA-256 identity of the configured firmware file (empty when it cannot be
        /// hashed). Reported to the operator so the deployment can be authorized against an exact
        /// artifact identity.
        /// </summary>
        private string TryComputeSha256()
        {
            try
            {
                return FirmwareArtifactVerifier.ComputeSha256Hex(_bootloaderHelpers.Path);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// True when the configured artifact checksum is a KNOWN LEGACY build (pre-0x0E39): the
        /// legacy 12-register image (0x251F) and the intermediate/other release checksums
        /// (0x5784, 0xE906, 0x5263, 0xFFC2). Used diagnosis-first to detect a mis-configured
        /// <c>TrackAmplifierFwPath</c> that points at a legacy artifact rather than the intended
        /// 0x0E39 release.
        /// </summary>
        private static bool IsLegacyFirmwareChecksum(ushort checksum)
        {
            switch (checksum)
            {
                case 0x5784:
                case 0x251F:
                case 0xE906:
                case 0x5263:
                case 0xFFC2:
                    return true;
                default:
                    return false;
            }
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

                _subState = 3;

                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => EXEC_FW_STATE_RECEIVE_FW_FILE.",
                    _loggerInstance);
            }

            return InitStepResult.Continue();
        }
              

        /// <summary>
        /// State 3:
        /// Wait for START_FW_DOWNLOAD_* result and on success begin sending FW data rows.
        /// </summary>
        private async Task<InitStepResult> State3_WaitStartDownloadDoneAsync(ReceivedMessage? lastMessage)
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

                    await _sendNextFwDataPacket.ExecuteAsync().ConfigureAwait(false);

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
        private async Task<InitStepResult> State4_SendFwDataChunkAsync(
            ReceivedMessage? lastMessage)
        {
            if (!lastMessage.HasValue)
                return InitStepResult.Continue();

            if (lastMessage.Value.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                lastMessage.Value.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                lastMessage.Value.Taskstate == TaskStates.DONE)
            {
                // Use your existing helper exactly as in RecoverSlaves case 6.
                await _sendNextFwDataPacket.ExecuteAsync().ConfigureAwait(false);
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

                if (!_bootloaderHelpers.GetConfigWordReadSuccessful)
                {
                    IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => GetConfigWordReadSuccessful == False.",
                    _loggerInstance);

                    _subState = 0;
                    return InitStepResult.Error("GetConfigWordReadSuccessful NOK.");
                }
                var configWord = _bootloaderHelpers.GetConfigWord;

                foreach (byte val in configWord)
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
