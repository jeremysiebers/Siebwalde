using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Properties;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.RealInitHilHost;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Offline tests for the hil-d repeated-init campaign loop's <c>--continue-on-fail</c> flag,
    /// driven through <see cref="HilScenarios.RunHilDAsync"/> against the deterministic simulator
    /// transport (no track controller, no network, no hardware). Covers: (a) with continue-on-fail a
    /// recoverable init failure is counted and the next cycle still runs, (b) the fail-closed default
    /// still stops on the first init failure, (c) an unexpected global-stop state still STOPS (and
    /// never re-arms) even with continue-on-fail, and (d) a host-loss latch is still re-armed (112)
    /// and proceeds.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class HilDContinueOnFailTests
    {
        private static readonly byte[] DetectedSlaves = { 1, 3, 4, 6 };
        private static readonly int[] Domain = { 1, 3, 4, 6 };
        private static readonly TimeSpan InitTimeout = TimeSpan.FromSeconds(30);

        // ---------------------------------------------------------------------
        // Firmware-path fixture (FlashFwTrackamplifiersStep validates the configured hex checksum).
        // ---------------------------------------------------------------------

        private sealed class FirmwarePathScope : IDisposable
        {
            private readonly string _original;
            private readonly string _temp;

            public FirmwarePathScope()
            {
                _original = CoreSettings.Default.TrackAmplifierFwPath;
                _temp = Path.Combine(Path.GetTempPath(), $"hil-d-{Guid.NewGuid():N}.hex");
                WriteTestFirmwareHex(_temp);
                CoreSettings.Default.TrackAmplifierFwPath = _temp;
            }

            public void Dispose()
            {
                try { CoreSettings.Default.TrackAmplifierFwPath = _original; } catch { /* best-effort */ }
                try { File.Delete(_temp); } catch { /* best-effort */ }
            }
        }

        private static void WriteTestFirmwareHex(string path)
        {
            const int dataRows = (0x8000 - 0x800) / 16;
            var sb = new System.Text.StringBuilder();
            for (int row = 0; row < dataRows; row++)
            {
                string address = (row * 16).ToString("X4");
                string data = row == dataRows - 1 ? "390E" + new string('0', 28) : new string('0', 32);
                sb.Append(':').Append("10").Append(address).Append("00").Append(data).Append("00").AppendLine();
            }

            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();
            File.WriteAllText(path, sb.ToString());
        }

        // ---------------------------------------------------------------------
        // Campaign transport factory. Per cycle the runner first creates the 111 PROBE transport and
        // then the INIT transport (both through the same factory), so the factory returns the probe
        // transport on odd calls and an init transport on even calls.
        // ---------------------------------------------------------------------

        private sealed class CampaignTransportFactory
        {
            private readonly TrackSimulatorConfig _probeConfig;
            private readonly Func<int, TrackSimulatorConfig> _initConfig;
            private int _call;
            private int _initCalls;
            private readonly List<DeterministicTrackTransport> _initTransports = new();

            public CampaignTransportFactory(
                TrackSimulatorConfig probeConfig,
                Func<int, TrackSimulatorConfig> initConfig)
            {
                _probeConfig = probeConfig;
                _initConfig = initConfig;
            }

            public IReadOnlyList<DeterministicTrackTransport> InitTransports => _initTransports;

            public int InitCallCount => Volatile.Read(ref _initCalls);

            public ITrackTransport Create()
            {
                int n = Interlocked.Increment(ref _call);
                bool isProbe = (n % 2) == 1; // 1st call per cycle = probe, 2nd = init
                if (isProbe)
                {
                    return new DeterministicTrackTransport(_probeConfig);
                }

                int init = Interlocked.Increment(ref _initCalls) - 1; // 0-based init index
                var transport = new DeterministicTrackTransport(_initConfig(init));
                lock (_initTransports)
                {
                    _initTransports.Add(transport);
                }

                return transport;
            }
        }

        private static TrackSimulatorConfig Config(byte globalStopState, bool detectSlavesMute = false)
            => new TrackSimulatorConfig(
                detectedSlaves: DetectedSlaves,
                initialGlobalStopState: globalStopState,
                detectSlavesMute: detectSlavesMute);

        // ---------------------------------------------------------------------
        // Tests
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ContinueOnFail_RecoverableInitFailure_IsCounted_AndNextCycleRuns()
        {
            using var fw = new FirmwarePathScope();
            var factory = new CampaignTransportFactory(
                Config(GlobalStopStatusMessage.Disarmed),
                init => Config(GlobalStopStatusMessage.Disarmed, detectSlavesMute: init == 0));

            var summary = await HilScenarios.RunHilDAsync(
                Domain, InitTimeout, TimeSpan.FromSeconds(20), jsonlPath: null, stopFilePath: null,
                CancellationToken.None, continueOnFail: true, transportFactory: factory.Create);

            Assert.False(summary.Pass);
            Assert.Equal(1, summary.FailCount);
            Assert.True(summary.PassCount >= 1,
                $"expected at least one passing cycle after the failure, got PassCount={summary.PassCount}.");
            Assert.True(summary.Cycle >= 2,
                $"expected the loop to continue past the first failure, got {summary.Cycle} cycles.");
            Assert.NotNull(summary.FirstFailure);
            Assert.Equal("CompletedWithFailures", summary.Disposition);
        }

        [Fact]
        public async Task FailClosedDefault_FirstInitFailure_StopsCampaign()
        {
            using var fw = new FirmwarePathScope();
            var factory = new CampaignTransportFactory(
                Config(GlobalStopStatusMessage.Disarmed),
                init => Config(GlobalStopStatusMessage.Disarmed, detectSlavesMute: init == 0));

            var summary = await HilScenarios.RunHilDAsync(
                Domain, InitTimeout, TimeSpan.FromSeconds(30), jsonlPath: null, stopFilePath: null,
                CancellationToken.None, continueOnFail: false, transportFactory: factory.Create);

            Assert.False(summary.Pass);
            Assert.Equal(1, summary.Cycle);
            Assert.Equal(0, summary.PassCount);
            Assert.Equal(1, summary.FailCount);
            Assert.Equal("FirstFailure", summary.Disposition);
            Assert.NotNull(summary.FirstFailure);
        }

        [Fact]
        public async Task ContinueOnFail_UnexpectedGlobalStop_StillStops_NeverRearms()
        {
            using var fw = new FirmwarePathScope();
            var factory = new CampaignTransportFactory(
                Config(GlobalStopStatusMessage.LatchedHostInitiated),
                init => Config(GlobalStopStatusMessage.Disarmed));

            var summary = await HilScenarios.RunHilDAsync(
                Domain, InitTimeout, TimeSpan.FromSeconds(30), jsonlPath: null, stopFilePath: null,
                CancellationToken.None, continueOnFail: true, transportFactory: factory.Create);

            Assert.False(summary.Pass);
            Assert.Equal(1, summary.Cycle);
            Assert.Equal(0, summary.PassCount);
            Assert.Equal(1, summary.FailCount);
            Assert.Equal("FirstFailure", summary.Disposition);
            Assert.NotNull(summary.FirstFailure);

            // The unexpected latch maps to StopFailClosed, and the STOP happens at the 111 pre-read,
            // BEFORE any init transport is composed — so no REARM (112) was ever emitted.
            Assert.Equal(
                GlobalStopRecoveryAction.StopFailClosed,
                GlobalStopRecoveryDecision.Decide(GlobalStopStatusMessage.LatchedHostInitiated));
            Assert.Equal(0, factory.InitCallCount);
        }

        [Fact]
        public async Task LatchedHostLoss_RearmsAndProceeds_ThroughCampaign()
        {
            using var fw = new FirmwarePathScope();
            var factory = new CampaignTransportFactory(
                Config(GlobalStopStatusMessage.LatchedHostLoss),
                init => Config(GlobalStopStatusMessage.LatchedHostLoss));

            var summary = await HilScenarios.RunHilDAsync(
                Domain, InitTimeout, TimeSpan.FromSeconds(10), jsonlPath: null, stopFilePath: null,
                CancellationToken.None, continueOnFail: false, transportFactory: factory.Create);

            Assert.True(summary.Pass);
            Assert.Equal(0, summary.FailCount);
            Assert.True(summary.PassCount >= 1);
            Assert.Equal("DurationBoundReached", summary.Disposition);

            // The campaign re-armed the host-loss latch (112) on the first init transport.
            Assert.True(factory.InitTransports.Count >= 1, "expected at least one init transport.");
            Assert.Contains(TrackCommand.REARM_GLOBAL_STOP, factory.InitTransports[0].SentCommands);
        }
    }
}
