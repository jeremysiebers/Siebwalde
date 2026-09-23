using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.EcosEmu;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Shared software-only helpers for the graceful-shutdown tests. Every async wait is
    /// time-bounded so a hang fails fast instead of deadlocking the test run.
    /// </summary>
    internal static class GracefulShutdownTestHelpers
    {
        /// <summary>Reserves and releases a loopback port so a test does not collide with 15471/5700.</summary>
        public static int GetFreeTcpPort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        /// <summary>Fails if the task does not complete within the bound.</summary>
        public static async Task TimeBoundAsync(Task task, int timeoutMs = 10000)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (!ReferenceEquals(completed, task))
            {
                throw new TimeoutException($"Operation did not complete within {timeoutMs}ms.");
            }

            await task; // observe the result / propagate any exception
        }

        /// <summary>Polls a condition until it becomes true or the bound expires.</summary>
        public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                    return;
                await Task.Delay(25);
            }

            throw new TimeoutException("Condition was not met within the timeout.");
        }

        public static async Task AssertPortIsServed(int port)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            Assert.True(client.Connected);
        }

        public static async Task AssertPortIsFree(int port)
        {
            using var client = new TcpClient();
            await Assert.ThrowsAnyAsync<SocketException>(
                () => client.ConnectAsync(IPAddress.Loopback, port));
        }

        /// <summary>
        /// Asserts that the remote side closed the connection: reading returns EOF (or the
        /// socket errors). Fails if nothing arrives before the bound.
        /// </summary>
        public static async Task AssertConnectionClosed(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[1];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                int n = await stream.ReadAsync(buffer, 0, 1, cts.Token);
                Assert.True(n <= 0, "Expected the connection to be closed (EOF).");
            }
            catch (IOException)
            {
                // Closed by reset — also a closed connection.
            }
            catch (ObjectDisposedException)
            {
                // Closed.
            }
            catch (OperationCanceledException)
            {
                Assert.Fail("The connection was not closed within the timeout.");
            }
        }
    }

    // =====================================================================
    // EcosEmulatorServer
    // =====================================================================

    public class EcosEmulatorServerGracefulShutdownTests
    {
        private sealed class FakeParser : IEcosCommandParser
        {
            public EcosCommand? Parse(string line)
                => new EcosCommand(line, line, null, Array.Empty<string>());
        }

        private sealed class CountingBackend : IEcosBackend
        {
            private int _count;
            public int Count => Volatile.Read(ref _count);

            public Task HandleAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct)
            {
                Interlocked.Increment(ref _count);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task StopAsync_ReleasesPort_CompletesHandlers_AndAllowsRestart()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();
            var backend = new CountingBackend();
            var server = new EcosEmulatorServer(port, new FakeParser(), backend);

            server.Start();

            // Connect a client and issue a minimal command, then keep the connection open
            // so StopAsync has to tear down an active client.
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            var stream = client.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes("get(1,info)"));
            await stream.FlushAsync();

            await GracefulShutdownTestHelpers.WaitUntilAsync(() => backend.Count >= 1);
            Assert.True(backend.Count >= 1, "The client command should have been handled.");

            await GracefulShutdownTestHelpers.TimeBoundAsync(server.StopAsync());

            // Port must be released and the active connection closed by the server.
            await GracefulShutdownTestHelpers.AssertPortIsFree(port);
            await GracefulShutdownTestHelpers.AssertConnectionClosed(client);
            client.Dispose();

            // Restart on the same port and verify it serves again.
            server.Start();
            await GracefulShutdownTestHelpers.AssertPortIsServed(port);

            await GracefulShutdownTestHelpers.TimeBoundAsync(server.StopAsync());
            await GracefulShutdownTestHelpers.AssertPortIsFree(port);
        }

        [Fact]
        public async Task StopAsync_IsIdempotent()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();
            var server = new EcosEmulatorServer(port, new FakeParser(), new CountingBackend());

            server.Start();
            await GracefulShutdownTestHelpers.TimeBoundAsync(server.StopAsync());
            await GracefulShutdownTestHelpers.TimeBoundAsync(server.StopAsync());

            await GracefulShutdownTestHelpers.AssertPortIsFree(port);
        }

        [Fact]
        public async Task Stop_ReleasesPortAndIsIdempotent()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();
            var server = new EcosEmulatorServer(port, new FakeParser(), new CountingBackend());

            server.Start();
            await GracefulShutdownTestHelpers.AssertPortIsServed(port);

            server.Stop();
            server.Stop();

            await GracefulShutdownTestHelpers.AssertPortIsFree(port);
        }
    }

    // =====================================================================
    // KoploperExternalInfoClient
    // =====================================================================

    public class KoploperExternalInfoClientGracefulShutdownTests
    {
        /// <summary>Minimal in-process stand-in for Koploper's external-info server (port 5700).</summary>
        private sealed class FakeKoploperListener : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task<TcpClient> _acceptTask;

            public FakeKoploperListener(int port)
            {
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                _acceptTask = AcceptOneAsync();
            }

            private async Task<TcpClient> AcceptOneAsync()
                => await _listener.AcceptTcpClientAsync(_cts.Token);

            public async Task<TcpClient> WaitForConnectionAsync(int timeoutMs = 5000)
            {
                var completed = await Task.WhenAny(_acceptTask, Task.Delay(timeoutMs));
                if (completed != _acceptTask)
                    throw new TimeoutException("The external-info client did not connect in time.");
                return await _acceptTask;
            }

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();
                _listener.Stop();
                _listener.Server.Dispose();
                try { await _acceptTask; } catch { }
            }
        }

        [Fact]
        public async Task StopAsync_WhileConnected_CompletesAndClosesConnection()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();
            await using var listener = new FakeKoploperListener(port);
            var client = new KoploperExternalInfoClient("127.0.0.1", port);

            client.Start();
            var serverSide = await listener.WaitForConnectionAsync();

            // Let the client settle into its read loop.
            await Task.Delay(200);

            await GracefulShutdownTestHelpers.TimeBoundAsync(client.StopAsync());

            await GracefulShutdownTestHelpers.AssertConnectionClosed(serverSide);
            serverSide.Dispose();
        }

        [Fact]
        public async Task StopAsync_WhileNotConnected_Completes()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort(); // nothing is listening
            var client = new KoploperExternalInfoClient("127.0.0.1", port);

            client.Start();

            // Let the connect attempt fail and the client enter its reconnect delay.
            await Task.Delay(300);

            await GracefulShutdownTestHelpers.TimeBoundAsync(client.StopAsync());
        }

        [Fact]
        public async Task StopAsync_ThenStartAgain_Reconnects()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();

            var client = new KoploperExternalInfoClient("127.0.0.1", port);
            client.Start();
            await Task.Delay(200);
            await GracefulShutdownTestHelpers.TimeBoundAsync(client.StopAsync());

            await using var listener = new FakeKoploperListener(port);
            client.Start();
            var serverSide = await listener.WaitForConnectionAsync();

            await GracefulShutdownTestHelpers.TimeBoundAsync(client.StopAsync());
            await GracefulShutdownTestHelpers.AssertConnectionClosed(serverSide);
            serverSide.Dispose();
        }
    }

    // =====================================================================
    // TrackSimulatorBackend
    // =====================================================================

    public class TrackSimulatorBackendGracefulShutdownTests
    {
        private sealed class FakeBlockPositionProvider : IBlockPositionProvider
        {
            public event Action<int, int>? BlockEntered;

            public int? TryGetBlockForLoc(int loc) => null;

            public void RaiseBlockEntered(int locoId, int blockId) => BlockEntered?.Invoke(locoId, blockId);
        }

        private sealed class CountingFeedbackSink : IHardwareFeedbackSink
        {
            private int _count;
            public int Count => Volatile.Read(ref _count);

            public Task OnSwitchChangedAsync(int ecosId, int decoderAddress, int outputIndex)
                => Task.CompletedTask;

            public Task OnSensorChangedAsync(int sensorId, bool occupied)
            {
                Interlocked.Increment(ref _count);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task StopAsync_EndsSimulationLoop_AndAllowsRestart()
        {
            var provider = new FakeBlockPositionProvider();
            var backend = new TrackSimulatorBackend(provider);
            var sink = new CountingFeedbackSink();

            backend.AttachFeedbackSink(sink);
            backend.SetPower(true);

            // Put a locomotive on block 1 and set it moving so the simulation loop produces
            // sensor events while it runs.
            provider.RaiseBlockEntered(42, 1);
            backend.SetLocoSpeed(42, 100, 1);

            await GracefulShutdownTestHelpers.WaitUntilAsync(() => sink.Count > 0);
            int before = sink.Count;
            await Task.Delay(800);
            Assert.True(sink.Count > before, "The simulation loop should keep producing sensor events while running.");

            await GracefulShutdownTestHelpers.TimeBoundAsync(backend.StopAsync());

            // After stop, no more sensor events may be produced.
            int stopped = sink.Count;
            await Task.Delay(500);
            Assert.Equal(stopped, sink.Count);

            // Restart: attaching the sink again must reset sensors and start a fresh loop.
            backend.AttachFeedbackSink(sink);
            Assert.True(sink.Count > stopped, "Restart should reset sensors and produce new events.");

            await GracefulShutdownTestHelpers.TimeBoundAsync(backend.StopAsync());
        }
    }
}
