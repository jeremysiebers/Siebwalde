// File: SiebwaldeApp.Core/Infrastructure/RawUdpTransport.cs
using System.Net;
using System.Net.Sockets;

/*
 *   udp &&
 *   ((ip.src == 192.168.1.12 && ip.dst == 192.168.1.193) ||
 *   (ip.src == 192.168.1.193 && ip.dst == 192.168.1.12)) &&
 /   !(udp contains AA:FF)
 */

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Minimal UDP transport abstraction so the adapter can send/receive bytes
    /// without depending on any legacy classes.
    /// 
    /// NOTE:
    /// - We send to a fixed remote endpoint (PIC32 IP + port).
    /// - We bind to a fixed local port so we can receive replies on that port.
    /// - We do NOT filter on the remote port when receiving, because the PIC32
    ///   may respond from a different port than it listens on.
    /// </summary>
    public interface IRawUdpTransport : IDisposable
    {
        EndPoint RemoteEndPointClient { get; }
        EndPoint RemoteEndPointHost { get; }
        void Send(byte[] payload);
        Task StartReceiveLoopAsync(Func<byte[], Task> onFrameAsync, CancellationToken token);
    }

    /// <summary>
    /// Simple UDP client implementation. All traffic goes over a single UDP socket:
    /// - The socket is BOUND to a known local port (the port your Python script used).
    /// - Outgoing datagrams are sent to the configured remote endpoint (PIC32 address).
    /// - Incoming datagrams on the local port are forwarded to the callback.
    /// </summary>
    public sealed class RawUdpTransport : IRawUdpTransport
    {
        private readonly UdpClient _socket;

        public EndPoint RemoteEndPointClient { get; }
        public EndPoint RemoteEndPointHost { get; }

        /// <summary>
        /// Creates a UDP transport that:
        /// - sends to (remoteIpAddress, remotePort)
        /// - listens on localPort for replies.
        /// </summary>
        /// <param name="remoteIpAddress">PIC32 IP address (e.g. "192.168.1.193").</param>
        /// <param name="remotePort">PIC32 UDP listen port (e.g. 10000).</param>
        /// <param name="localPort">
        /// Local UDP port to bind to (the port where the PIC32 sends replies to).
        /// Use the same local port as in your working Python script.
        /// </param>
        public RawUdpTransport(string remoteIpAddress, int remotePort, int localPort)
        {
            if (remoteIpAddress is null)
                throw new ArgumentNullException(nameof(remoteIpAddress));
            if (remotePort <= 0 || remotePort > 65535)
                throw new ArgumentOutOfRangeException(nameof(remotePort));
            if (localPort < 0 || localPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(localPort));

            var remoteIp = IPAddress.Parse(remoteIpAddress);
            RemoteEndPointClient = new IPEndPoint(remoteIp, remotePort);

            // Host endpoint is currently not used, but we expose it for compatibility.
            RemoteEndPointHost = RemoteEndPointClient;

            // IMPORTANT:
            // Bind the local socket to the *same* local port you used in your
            // Python test. The PIC32 will send responses to this port.
            _socket = new UdpClient(localPort);
        }

        /// <summary>
        /// Convenience constructor: if you don't care about the local port and
        /// let the OS pick an ephemeral one. (Usually not what you want when
        /// the PIC32 sends to a fixed client port.)
        /// </summary>
        public RawUdpTransport(string remoteIpAddress, int remotePort)
            : this(remoteIpAddress, remotePort, 0)
        {
        }

        /// <summary>
        /// Sends one UDP datagram to the PIC32 (remote endpoint).
        /// </summary>
        public void Send(byte[] payload)
        {
            if (payload is null) throw new ArgumentNullException(nameof(payload));

            _socket.Send(payload, payload.Length, (IPEndPoint)RemoteEndPointClient);
        }

        /// <summary>
        /// Starts an asynchronous receive loop. Every datagram received on the
        /// bound local port is forwarded to the given callback.
        /// 
        /// NOTE:
        /// - We do not filter on the remote endpoint here. Any packet that hits
        ///   this local port is assumed to belong to this session.
        /// </summary>
        public async Task StartReceiveLoopAsync(Func<byte[], Task> onFrameAsync, CancellationToken token)
        {
            if (onFrameAsync is null) throw new ArgumentNullException(nameof(onFrameAsync));

            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _socket.ReceiveAsync(token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // In production you would log this; for now we just delay a bit.
                    await Task.Delay(50, token).ConfigureAwait(false);
                    continue;
                }

                if (result.Buffer is { Length: > 0 })
                {
                    // Optional debug logging so you can see raw UDP packets.
                    //Console.WriteLine($"[UDP-RAW] Received {result.Buffer.Length} bytes from {result.RemoteEndPoint}");

                    try
                    {
                        await onFrameAsync(result.Buffer).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow exceptions from the callback to keep the loop alive.
                    }
                }
            }
        }

        public void Dispose()
        {
            try { _socket.Dispose(); } catch { /* ignore */ }
        }
    }
}
