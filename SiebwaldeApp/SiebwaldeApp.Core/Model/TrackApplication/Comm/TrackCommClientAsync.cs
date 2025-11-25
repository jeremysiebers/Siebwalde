using System;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp; // TrackApplicationVariables, TrackAmplifierItem, ReceivedMessage, SendMessage

namespace SiebwaldeApp.Core
{ 
    /// <summary>
    /// Event-based async communication client for the track amplifiers.
    /// </summary>
    public sealed class TrackCommClientAsync : ITrackCommClient
    {
        private readonly ITrackTransport _transport;
        private readonly TrackApplicationVariables _variables;
        private CancellationTokenSource? _cts;
        private Task? _receiveLoopTask;

        public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
        public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

        public TrackCommClientAsync(ITrackTransport transport, TrackApplicationVariables variables)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        }

        public async Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
        {
            if (_cts != null)
                throw new InvalidOperationException("TrackCommClientAsync is already started.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await _transport.OpenAsync(_cts.Token).ConfigureAwait(false);

            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_cts.Token), CancellationToken.None);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            var cts = _cts;
            if (cts == null)
                return;

            _cts = null;
            cts.Cancel();

            if (_receiveLoopTask != null)
            {
                try { await _receiveLoopTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { /* ignore */ }
            }

            await _transport.CloseAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
        {
            // TODO: copy frame build logic from TrackIOHandle.ActuatorCmd(...)
            byte[] buffer = BuildFrameFromSendMessage(message);
            await _transport.SendAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        }

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
                    // TODO: log using IoC.Logger once injected
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Parses incoming data frames and updates the model / raises events.
        /// Copy the logic from TrackIOHandle.HandleNewData into the methods used here.
        /// </summary>
        private void HandleNewData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            if (IsAmplifierDataFrame(data))
            {
                int index = ParseAmplifierIndex(data);
                var amplifier = _variables.trackAmpItems[index];

                UpdateAmplifierFromFrame(amplifier, data);

                AmplifierDataReceived?.Invoke(
                    this,
                    new AmplifierDataEventArgs(index, amplifier));
            }
            else
            {
                var msg = ParseReceivedMessage(data);
                _variables.trackControllerCommands.EthernetTargetRecv = msg;

                ControlMessageReceived?.Invoke(
                    this,
                    new ControlMessageEventArgs(msg));
            }
        }

        // --- TODO: deze helpers vul je door code uit TrackIOHandle over te nemen ---

        private static byte[] BuildFrameFromSendMessage(SendMessage message)
            => throw new NotImplementedException("Copy implementation from TrackIOHandle.ActuatorCmd.");

        private static bool IsAmplifierDataFrame(byte[] buffer)
            => throw new NotImplementedException("Copy discriminator logic from TrackIOHandle.HandleNewData.");

        private static int ParseAmplifierIndex(byte[] buffer)
            => throw new NotImplementedException("Copy slave index decode logic.");

        private static void UpdateAmplifierFromFrame(TrackAmplifierItem amplifier, byte[] buffer)
            => throw new NotImplementedException("Copy TrackAmplifierItem update logic.");

        private static ReceivedMessage ParseReceivedMessage(byte[] buffer)
            => throw new NotImplementedException("Copy controller message decode logic.");

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            await _transport.DisposeAsync().ConfigureAwait(false);
        }
    }
}
