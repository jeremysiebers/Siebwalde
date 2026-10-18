using System.Diagnostics;
using System.Threading;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that determines which slaves require a firmware update and, when an
    /// explicit deployment session authorizes it, flashes those slaves.
    ///
    /// Fail-closed flash gate (Phase-1 autonomous bring-up): a checksum mismatch NEVER triggers a
    /// flash on normal startup. Flashing requires an ACTIVE, valid <see cref="FirmwareDeploymentSession"/>
    /// that targets the specific amplifier, plus a verified artifact (SHA-256 + embedded checksum).
    /// Without a session, a mismatch is surfaced as a read-only diagnostic and the step proceeds
    /// without flashing. A legacy amplifier (e.g. checksum 0x251F) is never auto-flashed.
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
        private readonly FirmwareDeploymentSession? _deploymentSession;
        private readonly DeploymentEventLog? _eventLog;

        private int _subState;
        private readonly SendMessage _sendMessageTemplate;

        // From legacy: counters for FW data loop over ALL slaves
        private uint _fwFlashRequired;

        // Targets authorized for flash in the current run (for audit on success/failure).
        private readonly List<DeploymentTarget> _flashingTargets = new();
        private string? _artifactSha256;

        // Get enew stopwatch
        private Stopwatch sw = new Stopwatch();

        public string Name => "FlashFwTrackamplifiers";

        public FlashFwTrackamplifiersStep(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            SendNextFwDataPacket sendNextFwDataPacket,
            TrackAmplifierBootloaderHelpers bootloaderHelpers,
            string loggerInstance = "Track",
            FirmwareDeploymentSession? deploymentSession = null,
            DeploymentEventLog? eventLog = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _sendNextFwDataPacket = sendNextFwDataPacket ?? throw new ArgumentNullException(nameof(sendNextFwDataPacket));
            _bootloaderHelpers = bootloaderHelpers ?? throw new ArgumentNullException(nameof(bootloaderHelpers));
            _loggerInstance = loggerInstance ?? "Track";
            _deploymentSession = deploymentSession;
            _eventLog = eventLog;

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
            switch (_subState)
            {
                // 0: determine how many slaves require flashing (and whether flashing is authorized)
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

        #region State 0: determine which slaves need flashing (and authorize it)

        private async Task<InitStepResult> State0_DetermineFlashRequirementAsync(CancellationToken cancellationToken)
        {
            _fwFlashRequired = 0;
            _flashingTargets.Clear();
            _artifactSha256 = null;

            if (!_bootloaderHelpers.HexFileReadSuccessful)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => HexFileReadSuccessful == False.",
                    _loggerInstance);
                return InitStepResult.Error("Firmware file read unsuccessful.");
            }

            // Get checksum for comparison
            var fileChecksum = _bootloaderHelpers.GetFileCheckSum;

            IoC.Logger.Log(
                $"State.FlashFwTrackamplifiers => File checksum = 0x{fileChecksum:X}.",
                _loggerInstance);

            // Read-only identification: which detected amplifiers do not carry the bundled checksum.
            var mismatches = new List<TrackAmplifierItem>();
            foreach (TrackAmplifierItem amplifier in _variables.trackAmpItems)
            {
                if (fileChecksum != amplifier.HoldingReg[TrackAmplifierRegisters.SwChecksum] &&
                    amplifier.SlaveDetected == 1 &&
                    amplifier.SlaveNumber > 0 &&
                    amplifier.SlaveNumber < 51)
                {
                    mismatches.Add(amplifier);
                }
            }

            if (mismatches.Count == 0)
            {
                IoC.Logger.Log(
                    "State.FlashFwTrackamplifiers => 0 slaves require flashing, initialization DONE.",
                    _loggerInstance);

                return InitStepResult.Next("InitTrackamplifiers");
            }

            // Mismatches exist: flashing is fail-closed. It proceeds only under an explicit,
            // active deployment session whose targets and verified artifact cover every mismatch.
            return await PlanFlashAuthorizationAsync(mismatches, fileChecksum, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<InitStepResult> PlanFlashAuthorizationAsync(
            List<TrackAmplifierItem> mismatches,
            ushort fileChecksum,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            // (a) Verify the session's artifact once, independently (SHA-256 + embedded checksum).
            bool artifactVerified = false;
            string? artifactSha256 = null;
            if (_deploymentSession is not null)
            {
                var verification = FirmwareArtifactVerifier.Verify(_deploymentSession.Artifact);
                artifactSha256 = verification.ComputedSha256Hex;
                artifactVerified = verification.IsVerified;

                if (!verification.IsVerified)
                {
                    Record(DeploymentEventKind.ArtifactVerificationFailed, null, artifactSha256,
                        verification.Reason ?? "artifact verification failed");
                    IoC.Logger.Log(
                        "State.FlashFwTrackamplifiers => " + (verification.Reason ?? "artifact verification failed."),
                        _loggerInstance);
                }
                else
                {
                    Record(DeploymentEventKind.ArtifactVerified, null, artifactSha256,
                        $"artifact verified (embedded checksum 0x{verification.EmbeddedChecksum:X4})");
                }
            }

            // (b) Per-amplifier authorization decision (read-only decision; no flash side effect yet).
            var authorized = new List<TrackAmplifierItem>();
            foreach (var amplifier in mismatches)
            {
                var target = DeploymentTarget.ForAmplifier(amplifier.SlaveNumber);
                var decision = DeploymentAuthorizer.Authorize(_deploymentSession, target, artifactVerified, now);

                var hr11 = amplifier.HoldingReg[TrackAmplifierRegisters.SwChecksum];
                if (decision.Granted)
                {
                    authorized.Add(amplifier);
                    Record(DeploymentEventKind.Authorized, target, artifactSha256,
                        $"amplifier #{amplifier.SlaveNumber} authorized for flash (HR11 0x{hr11:X4} != file 0x{fileChecksum:X4})");
                }
                else
                {
                    Record(DeploymentEventKind.NotAuthorized, target, artifactSha256,
                        $"{decision.DenialReason} — amplifier #{amplifier.SlaveNumber} (HR11 0x{hr11:X4} != file 0x{fileChecksum:X4})");
                    IoC.Logger.Log(
                        $"State.FlashFwTrackamplifiers => Slave {amplifier.SlaveNumber} has checksum = 0x{hr11:X4} " +
                        $"but will NOT be flashed: {decision.DenialReason}.",
                        _loggerInstance);
                }
            }

            // (c) Fail-closed: the flash-all protocol cannot isolate a single slave, so flashing is
            // permitted only when EVERY detected mismatching amplifier is explicitly authorized.
            if (authorized.Count == 0 || authorized.Count != mismatches.Count)
            {
                if (authorized.Count != mismatches.Count && authorized.Count > 0)
                {
                    Record(DeploymentEventKind.DeploymentBlocked, null, artifactSha256,
                        $"{authorized.Count} of {mismatches.Count} mismatching amplifier(s) authorized; " +
                        "refusing to flash (cannot safely isolate a single slave with the flash-all protocol).");
                    RecordRollback(new RollbackEvent(null, artifactSha256,
                        "deployment blocked (fleet isolation); no flash", now));
                    IoC.Logger.Log(
                        $"State.FlashFwTrackamplifiers => Deployment blocked: {authorized.Count} of {mismatches.Count} " +
                        "mismatching amplifier(s) authorized; refusing to flash (fleet isolation).",
                        _loggerInstance);
                }

                foreach (var amplifier in mismatches)
                {
                    IoC.Logger.Log(
                        $"State.FlashFwTrackamplifiers => Slave {amplifier.SlaveNumber} " +
                        "requires a firmware update but will NOT be auto-flashed (no authorized deployment).",
                        _loggerInstance);
                }

                _fwFlashRequired = 0;
                return InitStepResult.Next("InitTrackamplifiers");
            }

            // (d) Fully authorized: proceed to flash (the ONLY path that sends the flash command).
            _fwFlashRequired = (uint)authorized.Count;
            _artifactSha256 = artifactSha256;

            foreach (var amplifier in authorized)
            {
                _flashingTargets.Add(DeploymentTarget.ForAmplifier(amplifier.SlaveNumber));
                Record(DeploymentEventKind.FlashStarted, DeploymentTarget.ForAmplifier(amplifier.SlaveNumber),
                    artifactSha256, $"flash sequence started for amplifier #{amplifier.SlaveNumber}");
            }

            IoC.Logger.Log(
                $"State.FlashFwTrackamplifiers => {_fwFlashRequired} slave(s) will be flashed " +
                $"under deployment session '{_deploymentSession?.Id}'.",
                _loggerInstance);

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

                RecordFlashFailure("start FW download failed during slave recovery");
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

                RecordFlashFailure("firmware checksum mismatch during slave recovery");
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

                    RecordFlashFailure("config word read unsuccessful");
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

                foreach (var target in _flashingTargets)
                {
                    Record(DeploymentEventKind.FlashSucceeded, target, _artifactSha256,
                        "flash sequence completed");
                }

                _subState = 0;

                return InitStepResult.Next("InitTrackamplifiers");
            }

            return InitStepResult.Continue();
        }

        #endregion

        #region Audit helpers

        private void Record(DeploymentEventKind kind, DeploymentTarget? target, string? artifactSha256, string result)
        {
            _eventLog?.Record(new DeploymentEvent(kind, target, artifactSha256, result, DateTimeOffset.UtcNow));
        }

        private void RecordRollback(RollbackEvent rollbackEvent)
        {
            _eventLog?.RecordRollback(rollbackEvent);
        }

        private void RecordFlashFailure(string reason)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var target in _flashingTargets)
            {
                _eventLog?.Record(new DeploymentEvent(
                    DeploymentEventKind.FlashFailed, target, _artifactSha256, reason, now));
            }

            _eventLog?.RecordRollback(new RollbackEvent(null, _artifactSha256, reason, now));
        }

        #endregion
    }
}
