using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Diagnostics tests for the Stage-5 <see cref="GroupReadyStep"/> observation sink. They verify
    /// that the per-amplifier diagnostics capture the command identity, the pre-write snapshot, the
    /// fresh during-verification observations (HR8/HR9/HR1/HR15 + sequence + freshness), and the
    /// final ACCEPTED/TIMEOUT outcome — without overclaiming fault/lease/rejection. The diagnostics
    /// read cached HoldingReg only (no controller transaction is added by the instrumentation).
    /// </summary>
    public class GroupReadyStepDiagnosticsTests
    {
#pragma warning disable CS0067
        private sealed class RecordingCommClient : ITrackCommClient
        {
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
#pragma warning restore CS0067

        private static readonly byte[] RequiredAmps = { 1, 3, 4, 6 };

        private static ushort ReadyState =>
            (ushort)(TrackAmplifierRegisters.ReadyState | TrackAmplifierRegisters.EnableActiveBit
                     | TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit);

        private static ushort StandbyState =>
            (ushort)(TrackAmplifierRegisters.StandbyState | TrackAmplifierRegisters.EnableActiveBit
                     | TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit);

        private static TrackApplicationVariables Variables(
            Action<byte, TrackAmplifierItem>? configure = null)
        {
            var variables = new TrackApplicationVariables();
            foreach (var amp in RequiredAmps)
            {
                var item = variables.trackAmpItems[amp];
                item.HoldingReg[TrackAmplifierRegisters.State] = ReadyState;
                item.HoldingReg[TrackAmplifierRegisters.Control] = 0xC000;   // enable + lease echo
                item.HoldingReg[TrackAmplifierRegisters.WriteResult] = 0x0001;
                item.ProtocolReadbackSequence = 100;
                item.LastDataReceivedUtc = DateTimeOffset.UtcNow;
                configure?.Invoke(amp, item);
            }

            return variables;
        }

        /// <summary>Simulates a fresh SLAVEINFO readback arriving for one amplifier.</summary>
        private static void FreshReadback(TrackApplicationVariables variables, byte amp)
        {
            var item = variables.trackAmpItems[amp];
            item.ProtocolReadbackSequence++;
            item.LastDataReceivedUtc = DateTimeOffset.UtcNow;
        }

        [Fact]
        public async Task AllReachReady_LogsRequestedBeforeObservedAccepted()
        {
            var variables = Variables();
            var lines = new List<string>();
            var step = new GroupReadyStep(new RecordingCommClient(), variables, RequiredAmps, log: lines.Add);

            await step.ExecuteAsync(null, CancellationToken.None); // substate 0

            foreach (var amp in RequiredAmps) FreshReadback(variables, amp);
            var result = await step.ExecuteAsync(null, CancellationToken.None); // substate 1

            Assert.Equal(InitStepResultKind.Completed, result.Kind);

            Assert.Contains(lines, l => l.Contains("REQUESTED GROUP_READY HR23=0x0002"));
            Assert.Contains(lines, l => l.StartsWith("BEFORE slave=1"));
            Assert.Contains(lines, l => l.StartsWith("OBSERVED slave=6"));
            Assert.Contains(lines, l => l.Contains("ACCEPTED"));
        }

        [Fact]
        public async Task OneAmpFails_LogsTimeoutAndRejectingAmp()
        {
            var variables = Variables((amp, item) =>
            {
                if (amp == 6) item.HoldingReg[TrackAmplifierRegisters.State] = StandbyState;
            });
            var lines = new List<string>();
            var step = new GroupReadyStep(
                new RecordingCommClient(), variables, RequiredAmps,
                verifyWindow: TimeSpan.FromMilliseconds(50), log: lines.Add);

            await step.ExecuteAsync(null, CancellationToken.None);

            InitStepResult result = default;
            for (int i = 0; i < 20; i++)
            {
                foreach (var amp in RequiredAmps) FreshReadback(variables, amp);
                result = await step.ExecuteAsync(null, CancellationToken.None);
                if (result.Kind == InitStepResultKind.Error) break;
                await Task.Delay(10);
            }

            Assert.Equal(InitStepResultKind.Error, result.Kind);
            Assert.Contains(lines, l => l.Contains("TIMEOUT") && l.Contains("[6]"));
            // Amp 6 reports not-ready while the others report ready in the same window.
            Assert.Contains(lines, l => l.Contains("slave=6") && l.Contains("ready=no"));
            Assert.Contains(lines, l => l.Contains("slave=1") && l.Contains("ready=YES"));
        }

        [Fact]
        public async Task ReportsFaultState_AsRawHr9()
        {
            var variables = Variables((amp, item) =>
            {
                if (amp == 6)
                {
                    item.HoldingReg[TrackAmplifierRegisters.State] = StandbyState;
                    item.HoldingReg[TrackAmplifierRegisters.Fault] = TrackAmplifierRegisters.LeaseExpiredFaultBit;
                }
            });
            var lines = new List<string>();
            var step = new GroupReadyStep(new RecordingCommClient(), variables, RequiredAmps, log: lines.Add);

            await step.ExecuteAsync(null, CancellationToken.None);
            foreach (var amp in RequiredAmps) FreshReadback(variables, amp);
            await step.ExecuteAsync(null, CancellationToken.None);

            Assert.Contains(lines, l => l.Contains("slave=6") && l.Contains("HR9=0x0080"));
            Assert.Contains(lines, l => l.Contains("slave=1") && l.Contains("HR9=0x0000"));
        }

        [Fact]
        public async Task MissingOrStaleData_ReportsFreshness()
        {
            var variables = Variables((amp, item) =>
            {
                if (amp == 6) item.LastDataReceivedUtc = null; // missing readback
                if (amp == 4) item.LastDataReceivedUtc = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(30); // stale
            });
            var lines = new List<string>();
            var step = new GroupReadyStep(new RecordingCommClient(), variables, RequiredAmps, log: lines.Add);

            await step.ExecuteAsync(null, CancellationToken.None); // BEFORE snapshot

            Assert.Contains(lines, l => l.Contains("slave=6") && l.Contains("freshMs=NaN"));
            Assert.Contains(lines, l => l.Contains("slave=4") && !l.Contains("freshMs=NaN"));
        }

        [Fact]
        public async Task VerificationTimeout_LogsTimeoutMessage()
        {
            var variables = Variables((amp, item) =>
            {
                if (amp == 6) item.HoldingReg[TrackAmplifierRegisters.State] = StandbyState;
            });
            var lines = new List<string>();
            var step = new GroupReadyStep(
                new RecordingCommClient(), variables, RequiredAmps,
                verifyWindow: TimeSpan.FromMilliseconds(30), log: lines.Add);

            await step.ExecuteAsync(null, CancellationToken.None);

            InitStepResult result = default;
            for (int i = 0; i < 20; i++)
            {
                result = await step.ExecuteAsync(null, CancellationToken.None);
                if (result.Kind == InitStepResultKind.Error) break;
                await Task.Delay(10);
            }

            Assert.Equal(InitStepResultKind.Error, result.Kind);
            Assert.Contains(lines, l => l.StartsWith("TIMEOUT after"));
        }

        [Fact]
        public async Task InconclusiveHr1Hr15_ReportsRawValuesWithoutOverclaim()
        {
            // Amp 6 echoes lease+enable (HR1=0xC000) and a last-write of HR1 (HR15=0x0001), yet
            // never reaches READY. The diagnostics must report these raw values without asserting
            // lease freshness, a specific fault, or firmware rejection.
            var variables = Variables((amp, item) =>
            {
                if (amp == 6) item.HoldingReg[TrackAmplifierRegisters.State] = StandbyState;
            });
            var lines = new List<string>();
            var step = new GroupReadyStep(new RecordingCommClient(), variables, RequiredAmps, log: lines.Add);

            await step.ExecuteAsync(null, CancellationToken.None);
            foreach (var amp in RequiredAmps) FreshReadback(variables, amp);
            await step.ExecuteAsync(null, CancellationToken.None);

            Assert.Contains(lines, l => l.Contains("slave=6") && l.Contains("HR1=0xC000"));
            Assert.Contains(lines, l => l.Contains("slave=6") && l.Contains("HR15=0x0001"));
            Assert.Contains(lines, l => l.Contains("slave=6") && l.Contains("ready=no"));

            // No overclaim: the diagnostics never assert lease expiry or firmware rejection.
            Assert.DoesNotContain(lines, l => l.Contains("lease expired"));
            Assert.DoesNotContain(lines, l => l.Contains("rejected by firmware"));
            Assert.DoesNotContain(lines, l => l.Contains("lease stale"));
        }
    }
}
