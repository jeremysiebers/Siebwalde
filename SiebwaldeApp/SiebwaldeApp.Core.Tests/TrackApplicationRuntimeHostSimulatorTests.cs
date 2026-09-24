using System;
using System.IO;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Integration tests that drive <see cref="TrackApplicationRuntimeHost"/> against a real
    /// <see cref="TrackControlHost"/> in simulator mode on a free loopback port. No track
    /// controller or hardware is involved; the simulator backend and the ECoS server run in
    /// process on loopback only.
    /// </summary>
    public class TrackApplicationRuntimeHostSimulatorTests : IDisposable
    {
        private readonly string _locoPath =
            Path.Combine(Path.GetTempPath(), $"siebwalde-locos-{Guid.NewGuid():N}.json");

        private static BlockTopology Topology => BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2,2>1");

        private static KoploperBlockMap BlockMap => KoploperBlockMap.Parse("1:1.01:1, 2:1.02:2");

        private TrackApplicationRuntimeHost CreateRuntime(int port)
            => new(new TrackControlHost(_locoPath, Topology, BlockMap, ecosListenPort: port));

        [Fact]
        public async Task StartSimulator_ThenStop_ReleasesPort()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();
            var runtime = CreateRuntime(port);

            await runtime.StartAsync(TrackControlMode.Simulator);

            Assert.Equal(TrackRuntimeState.Running, runtime.State);
            Assert.Equal(TrackControlMode.Simulator, runtime.ActiveEcosMode);
            await GracefulShutdownTestHelpers.AssertPortIsServed(port);

            await runtime.StopAsync();

            Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            Assert.Null(runtime.ActiveEcosMode);
            await GracefulShutdownTestHelpers.AssertPortIsFree(port);
        }

        [Fact]
        public async Task RepeatedStartStopStart_UsesFreshResources()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();
            var runtime = CreateRuntime(port);

            await runtime.StartAsync(TrackControlMode.Simulator);
            await GracefulShutdownTestHelpers.AssertPortIsServed(port);
            await runtime.StopAsync();
            await GracefulShutdownTestHelpers.AssertPortIsFree(port);

            await runtime.StartAsync(TrackControlMode.Simulator);
            await GracefulShutdownTestHelpers.AssertPortIsServed(port);
            Assert.Equal(TrackRuntimeState.Running, runtime.State);

            await runtime.StopAsync();
            await GracefulShutdownTestHelpers.AssertPortIsFree(port);
        }

        [Fact]
        public async Task MultipleRestartCycles_ReturnToRunningEachTime()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();
            var runtime = CreateRuntime(port);

            await runtime.StartAsync(TrackControlMode.Simulator);

            for (var i = 0; i < 3; i++)
            {
                await runtime.RestartAsync();
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(TrackControlMode.Simulator, runtime.ActiveEcosMode);
                await GracefulShutdownTestHelpers.AssertPortIsServed(port);
            }

            await runtime.StopAsync();
            await GracefulShutdownTestHelpers.AssertPortIsFree(port);
        }

        [Fact]
        public async Task Stop_WhenStopped_IsIdempotent()
        {
            var port = GracefulShutdownTestHelpers.GetFreeTcpPort();
            var runtime = CreateRuntime(port);

            await runtime.StopAsync();
            await runtime.StopAsync();

            Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            await GracefulShutdownTestHelpers.AssertPortIsFree(port);
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
