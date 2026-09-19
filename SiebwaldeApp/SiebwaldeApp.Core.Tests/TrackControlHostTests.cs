using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Software-only lifecycle tests for <see cref="TrackControlHost"/>. No track
    /// controller, no hardware and no WPF are involved: the simulator path is exercised
    /// on loopback only.
    /// </summary>
    public class TrackControlHostTests : IDisposable
    {
        private readonly string _locoPath =
            Path.Combine(Path.GetTempPath(), $"siebwalde-locos-{Guid.NewGuid():N}.json");

        private static BlockTopology Topology => BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2,2>1");

        private static KoploperBlockMap BlockMap => KoploperBlockMap.Parse("1:1.01:1, 2:1.02:2");

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

        [Fact]
        public async Task StartSimulator_BringsUpTheEcosListenerAndReportsMode()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);

            await host.StartAsync(TrackControlMode.Simulator, commClient: null, variables: null);

            Assert.True(host.IsRunning);
            Assert.Equal(TrackControlMode.Simulator, host.Mode);

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            Assert.True(client.Connected);

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

            using var client = new TcpClient();
            await Assert.ThrowsAnyAsync<SocketException>(
                () => client.ConnectAsync(IPAddress.Loopback, port));
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
        public async Task Start_WhenAlreadyRunning_KeepsTheOriginalMode()
        {
            var port = GetFreeTcpPort();
            var host = CreateHost(port);

            await host.StartAsync(TrackControlMode.Simulator, null, null);
            await host.StartAsync(TrackControlMode.Simulator, null, null);

            Assert.True(host.IsRunning);
            Assert.Equal(TrackControlMode.Simulator, host.Mode);

            host.Stop();
        }

        [Fact]
        public void Stop_WhenNeverStarted_IsSafe()
        {
            var host = CreateHost(GetFreeTcpPort());

            host.Stop();
            host.Stop();

            Assert.False(host.IsRunning);
        }

        [Fact]
        public void Constructor_WithoutLocoRepositoryPath_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new TrackControlHost(" ", Topology, BlockMap));
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
