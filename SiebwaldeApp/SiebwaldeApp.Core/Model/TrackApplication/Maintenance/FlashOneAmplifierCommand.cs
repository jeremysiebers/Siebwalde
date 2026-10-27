using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Outcome of a single-target amplifier flash sequence.
    /// </summary>
    public readonly struct FlashOneAmplifierResult
    {
        public bool Success { get; }
        public string? Error { get; }
        public byte TargetAmplifier { get; }

        /// <summary>
        /// When the PIC32 reports an ERROR, the decoded FlashSequencer sub-step (2..15)
        /// that failed, or null when the error could not be attributed to a sub-step
        /// (e.g. a timeout or a protocol deviation on the host side).
        /// </summary>
        public int? FailedSubStep { get; }

        private FlashOneAmplifierResult(bool success, string? error, byte target, int? failedSubStep)
        {
            Success = success;
            Error = error;
            TargetAmplifier = target;
            FailedSubStep = failedSubStep;
        }

        public static FlashOneAmplifierResult Ok(byte target) => new(true, null, target, null);

        public static FlashOneAmplifierResult Fail(string error, byte target, int? failedSubStep = null)
            => new(false, error, target, failedSubStep);
    }

    /// <summary>
    /// Raised when the PIC32 flash handshake deviates from the expected sequence or the
    /// acknowledged result is a failure. It carries a user-facing reason, never a partial state.
    /// </summary>
    public sealed class FlashOneAmplifierException : Exception
    {
        public FlashOneAmplifierException(string message) : base(message) { }
        public FlashOneAmplifierException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Drives the existing PIC32 firmware-flash wire path for exactly ONE amplifier:
    /// CONNECT (250) -> GET_CONTROLLER_VERSION (113) -> global MBUS reset
    /// (EXEC_MBUS_STATE_RESET 107 -> EXEC_MBUS_STATE_SLAVES_ON 100) -> enter the FW handler (103) ->
    /// download the firmware file (120 + 141 chunks) -> download the config word (121 + 151) ->
    /// EXEC_FW_STATE_FLASH_ONE_SLAVE (131) -> wait for DONE/ERROR.
    ///
    /// <see cref="ExecuteBootloaderOnlyAsync"/> drives the bootloader-only recovery variant
    /// EXEC_FW_STATE_FLASH_ONE_SLAVE_BOOTLOADER_ONLY (132): the PIC32 selects one amplifier
    /// via the backplane and runs the 8-bit bootloader sequence WITHOUT the HR11=0 entry write
    /// (the target is already in bootloader mode).
    ///
    /// It deliberately NEVER sends EXEC_FW_STATE_FLASH_ALL_SLAVES (122) and has no fall-back to it.
    /// The amplifier address is carried as the single data byte of command 131/132, which the
    /// PIC32 passes to the pre-existing <c>FlashSequencer(slave)</c>.
    /// </summary>
    public sealed class FlashOneAmplifierCommand
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly TrackAmplifierBootloaderHelpers _bootloaderHelpers;
        private readonly SendNextFwDataPacket _sendNextFwDataPacket;
        private readonly TimeSpan _messageTimeout;
        private readonly TimeSpan _flashTimeout;
        private readonly Action<ReceivedMessage>? _onProgress;

        public FlashOneAmplifierCommand(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            TrackAmplifierBootloaderHelpers bootloaderHelpers,
            SendNextFwDataPacket sendNextFwDataPacket,
            TimeSpan? messageTimeout = null,
            TimeSpan? flashTimeout = null,
            Action<ReceivedMessage>? onProgress = null)
        {
            _commClient = commClient ?? throw new ArgumentNullException(nameof(commClient));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _bootloaderHelpers = bootloaderHelpers ?? throw new ArgumentNullException(nameof(bootloaderHelpers));
            _sendNextFwDataPacket = sendNextFwDataPacket ?? throw new ArgumentNullException(nameof(sendNextFwDataPacket));
            _messageTimeout = messageTimeout ?? TimeSpan.FromSeconds(10);
            _flashTimeout = flashTimeout ?? TimeSpan.FromMinutes(15);
            _onProgress = onProgress;
        }

        public Task<FlashOneAmplifierResult> ExecuteAsync(
            byte amplifier, CancellationToken cancellationToken = default)
            => ExecuteCoreAsync(amplifier, bootloaderOnly: false, cancellationToken);

        public Task<FlashOneAmplifierResult> ExecuteBootloaderOnlyAsync(
            byte amplifier, CancellationToken cancellationToken = default)
            => ExecuteCoreAsync(amplifier, bootloaderOnly: true, cancellationToken);

        private async Task<FlashOneAmplifierResult> ExecuteCoreAsync(
            byte amplifier, bool bootloaderOnly, CancellationToken cancellationToken)
        {
            if (!TrackAmplifierAddress.IsTrackAmplifierAddress(amplifier))
            {
                return FlashOneAmplifierResult.Fail(
                    $"Amplifier address {amplifier} is outside the valid track-amplifier range 1..50.", amplifier);
            }

            if (!_bootloaderHelpers.HexFileReadSuccessful)
            {
                return FlashOneAmplifierResult.Fail("Firmware file has not been parsed successfully.", amplifier);
            }

            // Re-arm the packet iterator so a repeated operation starts at the first data packet.
            _sendNextFwDataPacket.Reset();

            var queue = Channel.CreateUnbounded<ReceivedMessage>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            void OnControlMessage(object? sender, ControlMessageEventArgs e)
                => queue.Writer.TryWrite(e.Message);

            _commClient.ControlMessageReceived += OnControlMessage;
            try
            {
                // 1. CONNECT.
                await SendAndWaitAsync(
                    queue.Reader,
                    new SendMessage(EnumClientCommands.CLIENT_CONNECTION_REQUEST, new byte[80]),
                    m => m.TaskId == TaskId.CONTROLLER &&
                         m.Taskcommand == TaskStates.CONNECTED &&
                         m.Taskstate == TaskStates.DONE,
                    _messageTimeout, cancellationToken).ConfigureAwait(false);

                // 2. Verify the controller version (113) before any write.
                await SendAndWaitAsync(
                    queue.Reader,
                    new SendMessage(TrackCommand.GET_CONTROLLER_VERSION, Array.Empty<byte>()),
                    m => m.TaskId == TaskId.CONTROLLER &&
                         m.Taskcommand == TrackCommand.GET_CONTROLLER_VERSION,
                    _messageTimeout, cancellationToken).ConfigureAwait(false);

                // The PIC32 controller protocol version is 0x01 (see controller.c / version.h).
                var version = _variables.trackControllerCommands.ControllerVersion;
                if (version is null ||
                    version.Value.Major != 1 ||
                    version.Value.ProtocolVersion != 0x01)
                {
                    var observed = version is null
                        ? "<none>"
                        : $"{version.Value.Major}.{version.Value.Minor}.{version.Value.Patch} protocol {version.Value.ProtocolVersion}";
                    return FlashOneAmplifierResult.Fail(
                        $"Controller version not acceptable: {observed}.", amplifier);
                }

                // 3. Global MBUS reset before entering the FW handler. A target amplifier at its
                // ASSIGNED address (not the default 0xAA) only accepts the FlashSequencer HR11=0
                // bootloader-entry write after a fresh global reset re-broadcasts the slave
                // addresses; without it sub-step 7 fails. The MBUS DONE ack carries the MBUS
                // STATE enum value in Taskcommand (EnumMbusStatus.MBUS_STATE_*), NOT the command
                // value 107/100 (see TrackController5/firmware/src/mbus.c CREATExTASKxSTATUSxMESSAGE).
                await SendAndWaitAsync(
                    queue.Reader,
                    new SendMessage(TrackCommand.EXEC_MBUS_STATE_RESET, new byte[80]),
                    m => m.TaskId == TaskId.MBUS &&
                         m.Taskcommand == EnumMbusStatus.MBUS_STATE_RESET &&
                         m.Taskstate == TaskStates.DONE,
                    _messageTimeout, cancellationToken).ConfigureAwait(false);

                await SendAndWaitAsync(
                    queue.Reader,
                    new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVES_ON, new byte[80]),
                    m => m.TaskId == TaskId.MBUS &&
                         m.Taskcommand == EnumMbusStatus.MBUS_STATE_SLAVES_ON &&
                         m.Taskstate == TaskStates.DONE,
                    _messageTimeout, cancellationToken).ConfigureAwait(false);

                // 4. Enter the FW handler (the master re-announces FWHANDLERINIT/CONNECTED).
                await SendAndWaitAsync(
                    queue.Reader,
                    new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, new byte[80]),
                    m => m.TaskId == TaskId.FWHANDLER &&
                         m.Taskcommand == TrackCommand.FWHANDLERINIT &&
                         m.Taskstate == TaskStates.CONNECTED,
                    _messageTimeout, cancellationToken).ConfigureAwait(false);

                // 5. Download the firmware file.
                await DownloadFirmwareFileAsync(queue.Reader, cancellationToken).ConfigureAwait(false);

                // 6. Download the config word.
                await DownloadConfigWordAsync(queue.Reader, cancellationToken).ConfigureAwait(false);

                // 7. Flash exactly one amplifier (normal 131 or bootloader-only 132).
                var flashCommand = bootloaderOnly
                    ? TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE_BOOTLOADER_ONLY
                    : TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE;

                var flashReply = await SendAndWaitAsync(
                    queue.Reader,
                    new SendMessage(flashCommand, new byte[] { amplifier }),
                    m => m.TaskId == TaskId.FWHANDLER &&
                         m.Taskcommand == flashCommand &&
                         (m.Taskstate == TaskStates.DONE || m.Taskstate == TaskStates.ERROR),
                    _flashTimeout, cancellationToken).ConfigureAwait(false);

                if (flashReply.Taskstate == TaskStates.DONE)
                    return FlashOneAmplifierResult.Ok(amplifier);

                // Decode the specific sub-step the PIC32 reported on failure.
                var stepText = FlashSequencerSubStep.TryDecode(flashReply.Taskmessage, out var step)
                    ? $"{step} ({FlashSequencerSubStep.Describe(step)})"
                    : $"unknown (taskmessage={flashReply.Taskmessage})";

                var mode = bootloaderOnly ? "bootloader-only" : "standard";
                return FlashOneAmplifierResult.Fail(
                    $"PIC32 reported ERROR during {mode} flash of amplifier {amplifier} " +
                    $"at sub-step {stepText}.",
                    amplifier,
                    FlashSequencerSubStep.TryDecode(flashReply.Taskmessage, out var substep) ? substep : (int?)null);
            }
            catch (FlashOneAmplifierException ex)
            {
                return FlashOneAmplifierResult.Fail(ex.Message, amplifier);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return FlashOneAmplifierResult.Fail(
                    "Timed out waiting for the PIC32 acknowledgement.", amplifier);
            }
            finally
            {
                _commClient.ControlMessageReceived -= OnControlMessage;
            }
        }

        private async Task DownloadFirmwareFileAsync(ChannelReader<ReceivedMessage> reader, CancellationToken ct)
        {
            // Request the FW-file download; the master replies STANDBY (140) DONE when ready.
            await SendAndWaitAsync(
                reader,
                new SendMessage(TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE, new byte[80]),
                m => m.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                     m.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                     m.Taskstate == TaskStates.DONE,
                _messageTimeout, ct).ConfigureAwait(false);

            while (true)
            {
                // Send the next 64-byte packet (reuses the existing counter-based helper).
                await _sendNextFwDataPacket.ExecuteAsync(ct).ConfigureAwait(false);

                var reply = await WaitAsync(
                    reader,
                    m => m.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                         ((m.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY &&
                           m.Taskstate == TaskStates.DONE) ||
                          (m.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE &&
                           m.Taskstate == TaskStates.DONE)),
                    _messageTimeout, ct).ConfigureAwait(false);

                if (reply.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE)
                    break;
            }

            // The master recomputes and verifies the image checksum after the last packet.
            var checksumReply = await WaitAsync(
                reader,
                m => m.TaskId == TrackCommand.FWFILEDOWNLOAD &&
                     m.Taskcommand == TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM &&
                     (m.Taskstate == TaskStates.DONE || m.Taskstate == TaskStates.ERROR),
                _messageTimeout, ct).ConfigureAwait(false);

            if (checksumReply.Taskstate != TaskStates.DONE ||
                checksumReply.Taskmessage != TaskMessages.RECEIVED_CHECKSUM_OK)
            {
                throw new FlashOneAmplifierException("Firmware file checksum verification failed on the PIC32.");
            }
        }

        private async Task DownloadConfigWordAsync(ChannelReader<ReceivedMessage> reader, CancellationToken ct)
        {
            await SendAndWaitAsync(
                reader,
                new SendMessage(TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD, new byte[80]),
                m => m.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                     m.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY &&
                     m.Taskstate == TaskStates.DONE,
                _messageTimeout, ct).ConfigureAwait(false);

            if (!_bootloaderHelpers.GetConfigWordReadSuccessful)
                throw new FlashOneAmplifierException("Firmware config word was not extracted successfully.");

            await SendAndWaitAsync(
                reader,
                new SendMessage(TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE,
                                _bootloaderHelpers.GetConfigWord),
                m => m.TaskId == TrackCommand.FWCONFIGWORDDOWNLOAD &&
                     m.Taskcommand == TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE &&
                     m.Taskstate == TaskStates.DONE,
                _messageTimeout, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends one message then waits for the first matching acknowledgement. Subscribing the
        /// shared queue up-front means a reply that races ahead of the wait is still captured.
        /// </summary>
        private async Task<ReceivedMessage> SendAndWaitAsync(
            ChannelReader<ReceivedMessage> reader,
            SendMessage message,
            Func<ReceivedMessage, bool> predicate,
            TimeSpan timeout,
            CancellationToken ct)
        {
            await _commClient.SendAsync(message, ct).ConfigureAwait(false);
            return await WaitAsync(reader, predicate, timeout, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Waits for the first message matching <paramref name="predicate"/>; non-matching
        /// messages are consumed and discarded (the protocol is strictly request/response in order).
        /// Every message read is surfaced through the optional progress callback (per-step DONE/ERROR).
        /// </summary>
        private async Task<ReceivedMessage> WaitAsync(
            ChannelReader<ReceivedMessage> reader,
            Func<ReceivedMessage, bool> predicate,
            TimeSpan timeout,
            CancellationToken ct)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);
            try
            {
                while (await reader.WaitToReadAsync(timeoutCts.Token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var message))
                    {
                        _onProgress?.Invoke(message);
                        if (predicate(message))
                            return message;
                    }
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timed out.
            }

            throw new FlashOneAmplifierException(
                $"Timed out after {timeout.TotalSeconds:0.#}s waiting for a PIC32 acknowledgement.");
        }
    }
}
