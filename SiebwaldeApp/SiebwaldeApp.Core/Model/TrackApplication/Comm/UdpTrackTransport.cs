using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace SiebwaldeApp.Core.TrackApplication.Comm
{
    /// <summary>
    /// Adapter that wraps an IRawUdpTransport and exposes it as ITrackTransport.
    /// It uses a Channel&lt;byte[]&gt; to bridge the callback-based receive loop
    /// into an async iterator (IAsyncEnumerable).
    /// </summary>
    public sealed class RawUdpTrackTransport : ITrackTransport
    {
        private readonly IRawUdpTransport _inner;
        private readonly Channel<byte[]> _frames;
        private CancellationTokenSource? _cts;
        private Task? _receiveLoopTask;

        public RawUdpTrackTransport(IRawUdpTransport inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _frames = Channel.CreateUnbounded<byte[]>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        }

        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (_cts != null)
                throw new InvalidOperationException("Transport already opened.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start the receive loop on the inner UDP transport.
            _receiveLoopTask = _inner.StartReceiveLoopAsync(
                async payload =>
                {
                    // Push every datagram into the channel.
                    // No await here except the channel write.
                    await _frames.Writer.WriteAsync(payload, _cts.Token)
                                     .ConfigureAwait(false);
                },
                _cts.Token);
        }

        public async Task CloseAsync(CancellationToken cancellationToken = default)
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

            _frames.Writer.TryComplete();
        }

        public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset != 0 || count != buffer.Length)
                throw new NotSupportedException("This transport expects full-frame sends.");

            _inner.Send(buffer);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<byte[]> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var reader = _frames.Reader;

            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] frame;
                try
                {
                    var hasItem = await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                    if (!hasItem)
                        yield break;

                    if (!reader.TryRead(out frame!))
                        continue;
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }

                yield return frame;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync().ConfigureAwait(false);
            _inner.Dispose();
        }
    }
}
