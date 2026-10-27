using SiebwaldeApp; // TrackApplicationVariables, TrackAmplifierItem, ReceivedMessage, SendMessage
using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static SiebwaldeApp.Core.Enums; // for HEADER, SLAVEINFO

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Event-based async communication client for the track amplifiers.
    /// This replaces the legacy TrackIOHandle, but keeps exactly the same
    /// protocol behavior (frame layout, parsing, etc.).
    /// </summary>
    public sealed class TrackCommClientAsync : ITrackCommClient, IAsyncDisposable
    {
        private readonly ITrackTransport _transport;
        private readonly TrackApplicationVariables _variables;
        private CancellationTokenSource? _cts;
        private Task? _receiveLoopTask;

        public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
        public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

        // NEW: periodic publish timer (no communication, just re-push container data)
        private readonly System.Timers.Timer _publishTimer = new(100) { AutoReset = true };
        private bool _publishTickInProgress;

        public TrackCommClientAsync(ITrackTransport transport, TrackApplicationVariables variables)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));

            _publishTimer.Elapsed += PublishTimerElapsed;
        }

        // --------------------------------------------------------------------
        // Lifecycle
        // --------------------------------------------------------------------

        public async Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
        {
            if (_cts != null)
                throw new InvalidOperationException("TrackCommClientAsync is already started.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await _transport.OpenAsync(_cts.Token).ConfigureAwait(false);

            var token = _cts.Token;
            _receiveLoopTask = Task.Run(
                () => ReceiveLoopAsync(token),
                CancellationToken.None);

            // Start 2 Hz container-to-UI publish
            _publishTimer.Start();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            var cts = _cts;
            if (cts == null)
                return;

            _cts = null;
            cts.Cancel();

            _publishTimer.Stop();

            ExceptionDispatchInfo? failure = null;
            if (_receiveLoopTask != null)
            {
                try { await _receiveLoopTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { /* ignore */ }
                catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
            }

            try { await _transport.CloseAsync(cancellationToken).ConfigureAwait(false); }
            finally { failure?.Throw(); } // Preserve the receive failure even if cleanup also fails.
        }

        // --------------------------------------------------------------------
        // Send
        // --------------------------------------------------------------------

        /// <summary>
        /// Sends a command to the Ethernet target.
        /// Frame layout is identical to TrackIOHandle.ActuatorCmd:
        /// [0] = HEADER
        /// [1] = Command
        /// [2..] = Data
        /// </summary>
        public async Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.Data == null) throw new ArgumentException("SendMessage.Data must not be null.", nameof(message));

            byte[] buffer = BuildFrameFromSendMessage(message);
            await _transport.SendAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        }

        private static byte[] BuildFrameFromSendMessage(SendMessage message)
        {
            // Direct port van TrackIOHandle.ActuatorCmd :contentReference[oaicite:4]{index=4}
            byte[] dataToSend = new byte[message.Data.Length + 2];
            dataToSend[0] = HEADER;
            dataToSend[1] = message.Command;

            Buffer.BlockCopy(message.Data, 0, dataToSend, 2, message.Data.Length);

            // In de oude code stond een uitgecommentarieerde FOOTER, dus hier ook niet gebruiken.
            return dataToSend;
        }

        // --------------------------------------------------------------------
        // Receive loop
        // --------------------------------------------------------------------

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            await foreach (var frame in _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    HandleNewData(frame);
                }
                catch (Exception ex)
                {
                    // TODO: inject IoC.Logger hier als je logging wilt
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Handle new received data from Track Ethernet Target.
        /// Dit is inhoudelijk dezelfde logica als TrackIOHandle.HandleNewData,
        /// maar dan in een nette async client. :contentReference[oaicite:5]{index=5}
        /// </summary>
        private void HandleNewData(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 2)
                return;

            using var stream = new MemoryStream(buffer);
            using var reader = new BinaryReader(stream);

            // Header + Sender (taskid)
            byte header = reader.ReadByte();
            byte sender = reader.ReadByte();

            // 1) Amplifier data frame: HEADER + SLAVEINFO
            if (header == HEADER && sender == SLAVEINFO)
            {
                // Layout-aware decode: the DEPLOYED legacy PIC32 firmware sends the
                // 12-register SLAVE_INFO (footer at wire offset 40); the Phase-1 simulator
                // (and any Phase-1 firmware) sends the 23-register frame (footer at 62).
                // See TrackSlaveInfoFrame for both layouts.
                ApplySlaveInfoFrame(buffer);
            }
            // 2) Controller/bootloader message frame: HEADER + iets anders dan SLAVEINFO
            else if (header == HEADER)
            {
                if (buffer.Length < 5) return;
                byte taskCommand = reader.ReadByte();
                byte taskState = reader.ReadByte();
                byte taskMessage = reader.ReadByte();

                var msg = new ReceivedMessage(sender, taskCommand, taskState, taskMessage);
                msg.Payload = buffer.Length > 5 ? buffer[5..] : null;

                // In de oude TrackIOHandle werd trackControllerCommands.ReceivedMessage gezet :contentReference[oaicite:7]{index=7}
                _variables.trackControllerCommands.ReceivedMessage = msg;

                // Surface the PIC32 semantic version from a GET_CONTROLLER_VERSION (113) reply:
                // sender=CONTROLLER(10), data[0]=echo 113, data[1..3]=MAJOR/MINOR/PATCH,
                // data[4]=PROTOCOL_VERSION, data[5]=0, data[6..N]=build date. Informational only.
                if (sender == TaskId.CONTROLLER && taskCommand == TrackCommand.GET_CONTROLLER_VERSION &&
                    msg.Payload is { Length: >= 3 } payload)
                {
                    _variables.trackControllerCommands.ControllerVersion = new TrackControllerVersion(
                        taskState, taskMessage, payload[0], payload[1],
                        DecodeBuildDate(payload, 3));
                }

                ControlMessageReceived?.Invoke(
                    this,
                    new ControlMessageEventArgs(msg));
            }
            else
            {
                // Onbekend frame (geen HEADER) -> negeren (zelfde gedrag als oude code)
            }
        }

        /// <summary>
        /// Decodes and applies one SLAVEINFO frame in either the deployed legacy
        /// 12-register layout (footer at wire offset 40) or the Phase-1 23-register
        /// layout (65-byte simulator frame, or the real 82-byte udpTrans_t envelope with
        /// footer at wire offset 64). A legacy frame is mapped onto a 23-register array
        /// with HR12..HR22 zeroed, so HR21/HR22 (which do not exist in the 12-register
        /// firmware) are never decoded as a firmware version. Neither shape establishes
        /// FC03 freshness on its own.
        /// </summary>
        private void ApplySlaveInfoFrame(byte[] buffer)
        {
            // Inner SLAVE_INFO.Header must be present and correct.
            if (buffer.Length < 3 || buffer[2] != HEADER)
                return;

            ushort slaveNumber;
            ushort slaveDetected;
            ushort[] holdingReg = new ushort[TrackAmplifierRegisters.Count];
            ushort mbReceiveCounter;
            ushort mbSentCounter;
            uint mbCommError;
            ushort mbExceptionCode;
            ushort spiCommErrorCounter;

            if (buffer.Length >= TrackSlaveInfoFrame.Length &&
                buffer[TrackSlaveInfoFrame.FooterOffset] == FOOTER)
            {
                // Phase-1 23-register frame: the 65-byte simulator frame, or the real
                // 82-byte udpTrans_t envelope (footer at 64, trailing garbage at 65..81).
                slaveNumber = buffer[3];
                slaveDetected = buffer[4];
                for (int i = 0; i < TrackAmplifierRegisters.Count; i++)
                {
                    holdingReg[i] = ReadUInt16Le(buffer, TrackSlaveInfoFrame.RegisterOffset + 2 * i);
                }
                mbReceiveCounter = ReadUInt16Le(buffer, TrackSlaveInfoFrame.MbReceiveCounterOffset);
                mbSentCounter = ReadUInt16Le(buffer, TrackSlaveInfoFrame.MbSentCounterOffset);
                mbCommError = ReadUInt32Le(buffer, TrackSlaveInfoFrame.MbCommErrorOffset);
                mbExceptionCode = buffer[TrackSlaveInfoFrame.MbExceptionCodeOffset];
                spiCommErrorCounter = buffer[TrackSlaveInfoFrame.SpiCommErrorCounterOffset];
            }
            else if (buffer.Length >= TrackSlaveInfoFrame.LegacyMinLength &&
                     buffer[TrackSlaveInfoFrame.LegacyFooterOffset] == FOOTER)
            {
                // Deployed legacy 12-register frame. HR12..HR22 stay 0 (UNSUPPORTED).
                slaveNumber = buffer[3];
                slaveDetected = buffer[4];
                for (int i = 0; i < TrackSlaveInfoFrame.LegacyRegisterCount; i++)
                {
                    holdingReg[i] = ReadUInt16Le(buffer, TrackSlaveInfoFrame.LegacyRegisterOffset + 2 * i);
                }
                mbReceiveCounter = ReadUInt16Le(buffer, TrackSlaveInfoFrame.LegacyMbReceiveCounterOffset);
                mbSentCounter = ReadUInt16Le(buffer, TrackSlaveInfoFrame.LegacyMbSentCounterOffset);
                mbCommError = ReadUInt32Le(buffer, TrackSlaveInfoFrame.LegacyMbCommErrorOffset);
                mbExceptionCode = buffer[TrackSlaveInfoFrame.LegacyMbExceptionCodeOffset];
                spiCommErrorCounter = buffer[TrackSlaveInfoFrame.LegacySpiCommErrorCounterOffset];
            }
            else
            {
                // Not a valid Phase-1 or legacy SLAVEINFO frame: ignore (fail-closed).
                return;
            }

            int index = slaveNumber; // net als in TrackIOHandle: trackAmpItems[SlaveNumber]

            if (index >= 0 && index < _variables.trackAmpItems.Count)
            {
                TrackAmplifierItem amplifier = _variables.trackAmpItems[index];

                amplifier.SlaveDetected = slaveDetected;
                amplifier.HoldingReg = holdingReg;
                amplifier.ProtocolReadbackSequence++;
                // Identity fail-closed (AC6): a track amplifier whose HR7 does not carry the
                // compatible identity fields is blocked. Backplane/config slaves (51..55) use a
                // different register semantics and are not identity-checked here.
                if (TrackAmplifierAddress.IsTrackAmplifierAddress(slaveNumber))
                {
                    var identity = holdingReg[TrackAmplifierRegisters.Identity];
                    if (!TrackAmplifierRegisters.HasCompatibleIdentityFields(identity))
                    {
                        // Primary check: legacy/unknown firmware is never granted authority.
                        amplifier.Phase1Failure = AmplifierPhase1Safety.IncompatibleFirmware;
                        amplifier.ConfigurationVerified = false;
                    }
                    else if (!TrackAmplifierRegisters.HasCompatibleProtocolVersion(
                                 holdingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]))
                    {
                        // Secondary check, strictly behind HR7 identity: a Phase-1 identity with a
                        // mismatched or absent HR22 PROTOCOL_VERSION (0x00 = unknown/legacy) is
                        // denied write/control authority but stays readable.
                        amplifier.Phase1Failure = AmplifierPhase1Safety.IncompatibleProtocolVersion;
                        amplifier.ConfigurationVerified = false;
                    }
                    else if (amplifier.Phase1Failure is AmplifierPhase1Safety.IncompatibleFirmware or
                             AmplifierPhase1Safety.IncompatibleProtocolVersion)
                    {
                        amplifier.Phase1Failure = null;
                    }
                }
                // Explicit protocol-readback evidence: the current HoldingReg content is a
                // genuine SLAVEINFO readback from this runtime, not a locally-written default.
                amplifier.ProtocolReadbackObserved = true;
                amplifier.LastDataReceivedUtc = DateTimeOffset.UtcNow;
                amplifier.MbReceiveCounter = mbReceiveCounter;
                amplifier.MbSentCounter = mbSentCounter;
                amplifier.MbCommError = mbCommError;
                amplifier.MbExceptionCode = mbExceptionCode;
                amplifier.SpiCommErrorCounter = spiCommErrorCounter;

                AmplifierDataReceived?.Invoke(
                    this,
                    new AmplifierDataEventArgs(index, amplifier));
            }
        }

        private static ushort ReadUInt16Le(byte[] buffer, int offset)
            => (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

        private static uint ReadUInt32Le(byte[] buffer, int offset)
            => (uint)(buffer[offset] | (buffer[offset + 1] << 8) |
                      (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24));

        // NEW: periodic publish of current container data to UI (no bus I/O)
        private static string? DecodeBuildDate(byte[] payload, int offset)
        {
            int end = offset;
            while (end < payload.Length && payload[end] != 0) end++;
            if (end == offset) return null;
            return System.Text.Encoding.ASCII.GetString(payload, offset, end - offset);
        }

        private void PublishTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_publishTickInProgress)
                return;

            try
            {
                _publishTickInProgress = true;

                var items = _variables.trackAmpItems;
                if (items == null || items.Count == 0)
                    return;

                // Push all detected amplifiers; you can also do round-robin if needed
                for (int i = 0; i < items.Count; i++)
                {
                    var amp = items[i];
                    if (amp == null || amp.SlaveDetected == 0)
                        continue;

                    AmplifierDataReceived?.Invoke(this, new AmplifierDataEventArgs(i, amp));
                }
            }
            finally
            {
                _publishTickInProgress = false;
            }
        }

        // --------------------------------------------------------------------
        // Dispose
        // --------------------------------------------------------------------

        public async ValueTask DisposeAsync()
        {
            ExceptionDispatchInfo? failure = null;
            try { await StopAsync().ConfigureAwait(false); }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }

            try { await _transport.DisposeAsync().ConfigureAwait(false); }
            finally { failure?.Throw(); }
        }
    }
}
