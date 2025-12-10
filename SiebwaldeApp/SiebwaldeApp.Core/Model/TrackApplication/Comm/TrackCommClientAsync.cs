using SiebwaldeApp; // TrackApplicationVariables, TrackAmplifierItem, ReceivedMessage, SendMessage
using System;
using System.IO;
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
        private readonly System.Timers.Timer _publishTimer = new(500) { AutoReset = true };
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

            _receiveLoopTask = Task.Run(
                () => ReceiveLoopAsync(_cts.Token),
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

            if (_receiveLoopTask != null)
            {
                try { await _receiveLoopTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { /* ignore */ }
            }

            await _transport.CloseAsync(cancellationToken).ConfigureAwait(false);
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
                // EXACT dezelfde volgorde als in TrackIOHandle.HandleNewData :contentReference[oaicite:6]{index=6}

                ushort mbHeader = reader.ReadByte();
                ushort slaveNumber = reader.ReadByte();
                ushort slaveDetected = reader.ReadByte();
                ushort padding = reader.ReadByte();

                ushort[] holdingReg = new ushort[12];
                for (int i = 0; i < 12; i++)
                {
                    holdingReg[i] = reader.ReadUInt16();
                }

                ushort mbReceiveCounter = reader.ReadUInt16();
                ushort mbSentCounter = reader.ReadUInt16();

                uint mbCommError = reader.ReadUInt32();

                ushort mbExceptionCode = reader.ReadByte();
                ushort spiCommErrorCounter = reader.ReadByte();
                ushort mbFooter = reader.ReadByte();

                int index = slaveNumber; // net als in TrackIOHandle: trackAmpItems[SlaveNumber]

                if (index >= 0 && index < _variables.trackAmpItems.Count)
                {
                    TrackAmplifierItem amplifier = _variables.trackAmpItems[index];

                    amplifier.SlaveDetected = slaveDetected;
                    amplifier.HoldingReg = holdingReg;
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
            // 2) Controller/bootloader message frame: HEADER + iets anders dan SLAVEINFO
            else if (header == HEADER)
            {
                byte taskCommand = reader.ReadByte();
                byte taskState = reader.ReadByte();
                byte taskMessage = reader.ReadByte();

                var msg = new ReceivedMessage(sender, taskCommand, taskState, taskMessage);

                // In de oude TrackIOHandle werd trackControllerCommands.ReceivedMessage gezet :contentReference[oaicite:7]{index=7}
                _variables.trackControllerCommands.ReceivedMessage = msg;

                ControlMessageReceived?.Invoke(
                    this,
                    new ControlMessageEventArgs(msg));
            }
            else
            {
                // Onbekend frame (geen HEADER) -> negeren (zelfde gedrag als oude code)
            }
        }

        // NEW: periodic publish of current container data to UI (no bus I/O)
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
            await StopAsync().ConfigureAwait(false);
            await _transport.DisposeAsync().ConfigureAwait(false);
        }
    }
}
