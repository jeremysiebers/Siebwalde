using SiebwaldeApp; // Sender, Receiver (existing types)
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// UDP-based transport for the legacy Ethernet target.
    /// </summary>
    public sealed class UdpTrackTransport : ITrackTransport
    {
        private readonly int _receivePort;
        private readonly int _sendPort;
        private readonly string _loggerInstance;

        private Sender? _sender;
        private Receiver? _receiver;

        public UdpTrackTransport(int receivePort, int sendPort, string loggerInstance)
        {
            _receivePort = receivePort;
            _sendPort = sendPort;
            _loggerInstance = loggerInstance;
        }

        public Task OpenAsync(CancellationToken cancellationToken = default)
        {
            // TODO: copy setup logic from TrackIOHandle constructor and Start()
            _sender = new Sender(_sendPort, _loggerInstance);
            _receiver = new Receiver(_receivePort, _loggerInstance);

            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            _receiver?.Dispose();
            _sender?.Dispose();
            _receiver = null;
            _sender = null;

            return Task.CompletedTask;
        }

        public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_sender == null)
                throw new InvalidOperationException("Transport not opened.");

            // TODO: adapt to real Sender API
            _sender.Send(buffer, offset, count);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<byte[]> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_receiver == null)
                throw new InvalidOperationException("Transport not opened.");

            while (!cancellationToken.IsCancellationRequested)
            {
                // TODO: adapt to real Receiver API (blocking read or callback-based).
                byte[] frame = _receiver.Receive();
                yield return frame;

                await Task.Yield();
            }
        }

        public ValueTask DisposeAsync()
        {
            _receiver?.Dispose();
            _sender?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
