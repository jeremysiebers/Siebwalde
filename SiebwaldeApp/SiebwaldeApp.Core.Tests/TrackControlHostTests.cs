using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Software-only lifecycle and mode-transition tests for <see cref="TrackControlHost"/>.
    /// No track controller, no hardware and no WPF are involved: the simulator and the real
    /// backend are both driven on loopback with fakes.
    /// </summary>
    public class TrackControlHostTests : IDisposable
    {
        private readonly string _locoPath =
            Path.Combine(Path.GetTempPath(), $"siebwalde-locos-{Guid.NewGuid():N}.json");

        private static BlockTopology Topology => BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2,2>1");

        private static KoploperBlockMap BlockMap => KoploperBlockMap.Parse("1:1.01:1, 2:1.02:2");

        /// <summary>Minimal stand-in for the real track communication client.</summary>
        private sealed class FakeCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
#pragma warning disable CS0067
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;
#pragma warning restore CS0067

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        /// <summary>Reserves and releases a port so the test does not collide with 15471.</summary>
        private static int GetFreeTcpPort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private TrackControlHost CreateHost(int port)
            => new(_locoPath, Topology, BlockMap, ecosListenPort: port);

        private static async Task AssertPortIsServed(int port)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            Assert.True(client.Connected);
        }

        private static async Task AssertPortIsFree(int port)
        {
            using var client = new TcpClient();
            await Assert.ThrowsAnyAsync<SocketException>(
                () => client.ConnectAsync(IPAddress.Loopback, port));
        }

        [Fact]
        public async Task StartSimulator_BringsUpTheEcosListenerAndReportsMode()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);

            var result = await host.StartAsync(TrackControlMode.Simulator, commClient: null, variables: null);

            Assert.Equal(EcosHostStartResult.Started, result);
            Assert.True(host.IsRunning);
            Assert.Equal(TrackControlMode.Simulator, host.Mode);

            await AssertPortIsServed(port);

            host.Stop();
        }

        [Fact]
        public async Task Stop_ReleasesTheListenerAndClearsState()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);

            await host.StartAsync(TrackControlMode.Simulator, null, null);
            host.Stop();

            Assert.False(host.IsRunning);
            Assert.Null(host.Mode);

            await AssertPortIsFree(port);
        }

        [Fact]
        public async Task StartSimulator_CreatesAnEmptyLocoRepository()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);

            await host.StartAsync(TrackControlMode.Simulator, null, null);
            host.Stop();

            Assert.True(File.Exists(_locoPath), "The loco repository should be created so Koploper can synchronise into it.");
        }

        [Fact]
        public async Task StartReal_WithoutTrackPieces_Throws()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);

            await Assert.ThrowsAsync<ArgumentException>(
                () => host.StartAsync(TrackControlMode.Real, commClient: null, variables: null));

            Assert.False(host.IsRunning);
        }

        [Fact]
        public async Task Constructor_WithoutLocoRepositoryPath_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new TrackControlHost(" ", Topology, BlockMap));
        }

        [Fact]
        public void Stop_WhenNeverStarted_IsSafe()
        {
            var host = CreateHost(GetFreeTcpPort());

            host.Stop();
            host.Stop();

            Assert.False(host.IsRunning);
        }

        // ---------------------------------------------------------------------
        // Mode transition semantics
        // ---------------------------------------------------------------------

        [Fact]
        public async Task SimulatorToSimulator_IsIdempotent()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);

            var first = await host.StartAsync(TrackControlMode.Simulator, null, null);
            var second = await host.StartAsync(TrackControlMode.Simulator, null, null);

            Assert.Equal(EcosHostStartResult.Started, first);
            Assert.Equal(EcosHostStartResult.AlreadyActive, second);
            Assert.Equal(TrackControlMode.Simulator, host.Mode);

            await AssertPortIsServed(port);
            host.Stop();
        }

        [Fact]
        public async Task RealToReal_IsIdempotent()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);
            var variables = new TrackApplicationVariables();

            var first = await host.StartAsync(TrackControlMode.Real, new FakeCommClient(), variables);
            var second = await host.StartAsync(TrackControlMode.Real, new FakeCommClient(), variables);

            Assert.Equal(EcosHostStartResult.Started, first);
            Assert.Equal(EcosHostStartResult.AlreadyActive, second);
            Assert.Equal(TrackControlMode.Real, host.Mode);

            await AssertPortIsServed(port);
            host.Stop();
        }

        [Fact]
        public async Task RealToSimulator_IsRejectedAndKeepsTheRealHostRunning()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);
            var variables = new TrackApplicationVariables();

            await host.StartAsync(TrackControlMode.Real, new FakeCommClient(), variables);
            var result = await host.StartAsync(TrackControlMode.Simulator, null, null);

            Assert.Equal(EcosHostStartResult.Rejected, result);
            Assert.True(host.IsRunning);
            Assert.Equal(TrackControlMode.Real, host.Mode);

            // The rejection must not have disturbed the live host.
            await AssertPortIsServed(port);
            host.Stop();
        }

        [Fact]
        public async Task SimulatorToReal_TransitionsAndKeepsThePortServed()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);
            var variables = new TrackApplicationVariables();

            await host.StartAsync(TrackControlMode.Simulator, null, null);
            var result = await host.StartAsync(TrackControlMode.Real, new FakeCommClient(), variables);

            Assert.Equal(EcosHostStartResult.Transitioned, result);
            Assert.True(host.IsRunning);
            Assert.Equal(TrackControlMode.Real, host.Mode);

            // The transition must not leave port 15471 (here: the test port) unserved.
            await AssertPortIsServed(port);

            host.Stop();
            await AssertPortIsFree(port);
        }

        [Fact]
        public async Task SimulatorToReal_WithoutTrackPieces_ThrowsAndLeavesTheSimulatorRunning()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);

            await host.StartAsync(TrackControlMode.Simulator, null, null);

            // A real request without the runtime pieces is invalid: it must be rejected
            // before the running simulator is torn down.
            await Assert.ThrowsAsync<ArgumentException>(
                () => host.StartAsync(TrackControlMode.Real, commClient: null, variables: null));

            Assert.True(host.IsRunning);
            Assert.Equal(TrackControlMode.Simulator, host.Mode);

            await AssertPortIsServed(port);
            host.Stop();
        }

        [Fact]
        public async Task SimulatorToRealToSimulator_IsRejectedAfterTheTransition()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);
            var variables = new TrackApplicationVariables();

            await host.StartAsync(TrackControlMode.Simulator, null, null);
            await host.StartAsync(TrackControlMode.Real, new FakeCommClient(), variables);

            var result = await host.StartAsync(TrackControlMode.Simulator, null, null);

            Assert.Equal(EcosHostStartResult.Rejected, result);
            Assert.Equal(TrackControlMode.Real, host.Mode);

            host.Stop();
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_locoPath))
                {
                    File.Delete(_locoPath);
                }
            }
            catch
            {
                // Temp-file cleanup must never fail a test run.
            }
        }
    }
}
