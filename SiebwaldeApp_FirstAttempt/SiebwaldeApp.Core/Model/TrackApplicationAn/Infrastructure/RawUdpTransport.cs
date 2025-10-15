// File: SiebwaldeApp.Core/Infrastructure/RawUdpTransport.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Minimal UDP transport abstraction so the adapter can send/receive bytes
    /// without depending on any legacy classes.
    /// </summary>
    public interface IRawUdpTransport : IDisposable
    {
        EndPoint RemoteEndPoint { get; }
        void Send(byte[] payload);
        Task StartReceiveLoopAsync(Func<byte[], Task> onFrameAsync, CancellationToken token);
    }

    /// <summary>
    /// Simple UDP client implementation. If you already have a TrackIOHandle/receiver,
    /// you can skip this and call Pic18UdpAdapter.ProcessIncomingBytes(...) directly.
    /// </summary>
    public sealed class RawUdpTransport : IRawUdpTransport
    {
        private readonly UdpClient _client;
        public EndPoint RemoteEndPoint { get; }

        public RawUdpTransport(string ipAddress, int port)
        {
            _client = new UdpClient();
            _client.Connect(ipAddress, port);
            RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        }

        public void Send(byte[] payload)
        {
            _client.Send(payload, payload.Length);
        }

        public async Task StartReceiveLoopAsync(Func<byte[], Task> onFrameAsync, CancellationToken token)
        {
            // Bind locally if needed. If you expect responses from a fixed remote,
            // UdpClient with Connect will filter packets from other sources.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _client.ReceiveAsync(token);
                    if (result.Buffer is { Length: > 0 })
                        await onFrameAsync(result.Buffer);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // swallow; in production, inject a logger and log the exception
                    await Task.Delay(50, token);
                }
            }
        }

        public void Dispose()
        {
            try { _client.Dispose(); } catch { /* ignore */ }
        }
    }
}
