using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Offline tests for the safe CONFIG/APPLY orchestration. Software/protocol only: these
    /// exercises run against the deterministic simulator or in-memory fake transports and never
    /// touch a socket or hardware. "DISABLE verified" / "inhibited" / "config verified" are
    /// register-report assertions, not physical H-bridge proof.
    /// </summary>
    public class AmplifierConfigApplyOrchestratorTests
    {
        private static AmplifierConfigApplyOrchestrator CreateOrchestrator(
            ITrackTransport transport,
            TimeSpan? acknowledgementTimeout = null,
            TimeSpan? heartbeatInterval = null,
            TimeSpan? readbackTimeout = null,
            TimeSpan? disableVerifyTimeout = null)
            => AmplifierConfigApplyOrchestrator.Create(
                () => transport,
                acknowledgementTimeout: acknowledgementTimeout ?? TimeSpan.FromSeconds(5),
                heartbeatInterval: heartbeatInterval,
                readbackTimeout: readbackTimeout ?? TimeSpan.FromSeconds(2),
                disableVerifyTimeout: disableVerifyTimeout ?? TimeSpan.FromSeconds(2));

        // ---------------------------------------------------------------------
        // Full flow: DISABLE gate -> CONFIG/APPLY -> verified
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ApplyConfig_FullFlow_DisablesThenConfigures()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 },
                emitGlobalStopArmedStatus: true));
            await using var orchestrator = CreateOrchestrator(
                transport, heartbeatInterval: TimeSpan.FromMilliseconds(20));

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            Assert.Equal("PASS", result.Status);
            Assert.Null(result.Failure);
            Assert.Null(result.FailedCommand);
            Assert.Equal("READY", result.Init.Readiness);

            AmplifierConfigApplyAmplifierResult amp = Assert.Single(result.Amplifiers);
            Assert.Equal(1, amp.SlaveNumber);
            Assert.True(amp.Detected);
            Assert.True(amp.Compatible);
            Assert.True(amp.IdentityVerified);
            Assert.True(amp.DisableAccepted);
            Assert.True(amp.DisableVerified);
            Assert.False(amp.EnableActiveAfterDisable);   // enable cleared by DISABLE
            Assert.True(amp.InhibitedAfterDisable);       // output inhibited after DISABLE
            Assert.True(amp.ConfigDownloaded);
            Assert.True(amp.ConfigVerified);
            Assert.True(amp.InitDoneConfigOk);
            Assert.False(amp.Pwm3Enabled);                // bridge never (re-)enabled
            Assert.False(amp.BrakeActive);
            Assert.True(amp.Inhibited);                   // still inhibited after APPLY
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, amp.AppliedPwm);
            Assert.Null(amp.Failure);

            // Heartbeat supervision ran and did not spuriously latch.
            Assert.True(result.Heartbeat.Supervised);
            Assert.True(result.Heartbeat.HeartbeatSent >= 1);
            Assert.False(result.Heartbeat.Latched);
            Assert.Equal(0, result.Heartbeat.GlobalStopSent);

            // Config values landed in the simulator registers (echo-verified).
            ushort[] regs = transport.GetRegisters(1);
            Assert.Equal(
                (ushort)(TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit),
                (ushort)(regs[TrackAmplifierRegisters.State] &
                         (TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit)));
        }

        [Fact]
        public async Task ApplyConfig_InitReport_ShowsCommand104Done()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            await using var orchestrator = CreateOrchestrator(transport);

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            AmplifierConfigApplyCommandReport init = result.Init.Commands.Single(c => c.Command == 104);
            Assert.Equal("EXEC_MBUS_STATE_SLAVE_INIT", init.Name);
            Assert.Equal(1, init.Transmitted);
            Assert.Equal(1, init.Acknowledged);
            Assert.Equal("DONE", init.State);
        }

        // ---------------------------------------------------------------------
        // Safety invariant: never write HR0, never ENABLE; DISABLE before APPLY
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ApplyConfig_NeverWritesHr0OrEnable_DisablePrecedesApply()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            await using var orchestrator = CreateOrchestrator(transport);

            await orchestrator.RunAsync(new byte[] { 1 });

            bool anyHr0Write = false;
            var hr1Writes = new List<ushort>();
            foreach (byte[] frame in transport.SentFrames)
            {
                if (frame.Length < 8 || frame[1] != TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH)
                {
                    continue;
                }

                byte direction = frame[3];
                byte count = frame[4];
                byte startReg = frame[5];
                if (direction != Enums.WRITE || count == 0)
                {
                    continue; // reads only
                }

                ushort value = (ushort)(frame[6] | (frame[7] << 8));
                if (startReg == (byte)TrackAmplifierRegisters.PwmCommand)
                {
                    anyHr0Write = true;
                }

                if (startReg == (byte)TrackAmplifierRegisters.Control)
                {
                    hr1Writes.Add(value);
                }
            }

            Assert.False(anyHr0Write, "apply-config must never write HR0 (PWM command)");
            Assert.DoesNotContain(TrackAmplifierRegisters.EnableBit, hr1Writes); // never ENABLE (0x8000)

            int disableIdx = hr1Writes.IndexOf(TrackAmplifierRegisters.DisableBit);
            int applyIdx = hr1Writes.IndexOf(TrackAmplifierRegisters.ApplyConfigBit);
            Assert.True(disableIdx >= 0, "DISABLE (0x0400) must be written before APPLY");
            Assert.True(applyIdx > disableIdx, "APPLY (0x2000) must follow DISABLE (0x0400)");
        }

        // ---------------------------------------------------------------------
        // Failure paths
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ApplyConfig_InvalidConfig_FailsClosed()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            await using var orchestrator = CreateOrchestrator(transport);

            // Watchdog 30 (< 50) is out of range: APPLY is rejected, so config verification fails.
            AmplifierConfigApplyResult result = await orchestrator.RunAsync(
                new byte[] { 1 }, new AmplifierConfiguration(WatchdogMs: 30));

            Assert.Equal("FAILED", result.Status);
            AmplifierConfigApplyAmplifierResult amp = Assert.Single(result.Amplifiers);
            Assert.True(amp.Compatible);
            Assert.True(amp.DisableVerified);
            Assert.False(amp.ConfigDownloaded);
            Assert.False(amp.ConfigVerified);
            Assert.False(amp.InitDoneConfigOk);
            Assert.NotNull(amp.Failure);
        }

        [Fact]
        public async Task ApplyConfig_IncompatibleAmplifier_IsNotWritten()
        {
            var config = new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 },
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x02 } });
            var transport = new DeterministicTrackTransport(config);
            await using var orchestrator = CreateOrchestrator(transport);

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            Assert.Equal("FAILED", result.Status);
            Assert.Equal("no_compatible_amplifiers", result.Failure);
            AmplifierConfigApplyAmplifierResult amp = Assert.Single(result.Amplifiers);
            Assert.False(amp.Compatible);
            Assert.Equal(AmplifierPhase1Safety.IncompatibleProtocolVersion, amp.IncompatibilityReason);
            Assert.False(amp.IdentityVerified);
            Assert.False(amp.DisableAccepted); // no write attempted for an incompatible amp
        }

        [Fact]
        public async Task ApplyConfig_InterruptedInit_ReportsAckTimeout()
        {
            // A fake transport that acknowledges only 250 (connect); the 100 command times out.
            var transport = new FakeInitTransport(slave: 1, emitSlaveInfo: false,
                new byte[] { 250 });
            await using var orchestrator = CreateOrchestrator(
                transport, acknowledgementTimeout: TimeSpan.FromMilliseconds(200));

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            Assert.Equal("FAILED", result.Status);
            Assert.Equal("init_ack_timeout", result.Failure);
            Assert.Equal(100, result.FailedCommand);
            Assert.Equal("EXEC_MBUS_STATE_SLAVES_ON", result.FailedCommandName);
            Assert.Empty(result.Amplifiers);
            Assert.Equal("FAILED", result.Init.Readiness);
        }

        [Fact]
        public async Task ApplyConfig_NoReadback_TimesOutAndReportsNoCompatible()
        {
            // Acks the full init but never emits SLAVEINFO: the readback wait times out and the
            // amplifier is reported not-detected / not-compatible (never fabricated as configured).
            var transport = new FakeInitTransport(slave: 1, emitSlaveInfo: false,
                new byte[] { 250, 100, 101, 104, 106, 105 });
            await using var orchestrator = CreateOrchestrator(
                transport, readbackTimeout: TimeSpan.FromMilliseconds(200));

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            Assert.Equal("FAILED", result.Status);
            Assert.Equal("no_compatible_amplifiers", result.Failure);
            AmplifierConfigApplyAmplifierResult amp = Assert.Single(result.Amplifiers);
            Assert.False(amp.Detected);
            Assert.False(amp.Compatible);
            Assert.False(amp.DisableAccepted);
        }

        // ---------------------------------------------------------------------
        // Retained-ENABLE handling (model level: DISABLE clears enable-active)
        // ---------------------------------------------------------------------

        [Fact]
        public void RetainedEnable_DisableClearsEnableAndInhibits()
        {
            var amplifier = new SimulatedTrackAmplifier(1);

            // Retained-ENABLE: ENABLE was requested (enable-active set).
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.EnableBit });
            ushort[] afterEnable = amplifier.Snapshot();
            Assert.NotEqual(0, afterEnable[TrackAmplifierRegisters.State] & TrackAmplifierRegisters.EnableActiveBit);

            // DISABLE (the safety gate) must clear enable-active and inhibit output.
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.DisableBit });
            ushort[] afterDisable = amplifier.Snapshot();
            Assert.Equal(0, afterDisable[TrackAmplifierRegisters.State] & TrackAmplifierRegisters.EnableActiveBit);
            Assert.NotEqual(0, afterDisable[TrackAmplifierRegisters.State] & TrackAmplifierRegisters.InhibitedBit);
        }

        // ---------------------------------------------------------------------
        // Cleanup
        // ---------------------------------------------------------------------

        [Fact]
        public async Task DisposeAsync_StopsTransport()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            var orchestrator = CreateOrchestrator(transport);

            await orchestrator.RunAsync(new byte[] { 1 });
            await orchestrator.DisposeAsync();

            Assert.True(transport.CloseCallCount >= 1);
        }

        // ---------------------------------------------------------------------
        // Fake init transport (acknowledges selected commands, optional SLAVEINFO)
        // ---------------------------------------------------------------------

        private sealed class FakeInitTransport : ITrackTransport
        {
            private readonly Channel<byte[]> _frames = Channel.CreateUnbounded<byte[]>();
            private readonly HashSet<byte> _ackCommands;
            private readonly bool _emitSlaveInfo;
            private readonly byte _slave;

            public FakeInitTransport(byte slave, bool emitSlaveInfo, params byte[] ackCommands)
            {
                _slave = slave;
                _emitSlaveInfo = emitSlaveInfo;
                _ackCommands = new HashSet<byte>(ackCommands);
            }

            public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task CloseAsync(CancellationToken cancellationToken = default)
            {
                _frames.Writer.TryComplete();
                return Task.CompletedTask;
            }

            public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            {
                if (count >= 2 && _ackCommands.Contains(buffer[offset + 1]))
                {
                    byte command = buffer[offset + 1];

                    // Emit the exact master DONE acknowledgement the guard waits for.
                    if (KoploperSessionCommandAllowlist.TryGetExpectedAcknowledgement(
                            command, out ushort taskId, out ushort taskCommand, out ushort taskState))
                    {
                        _frames.Writer.TryWrite(new byte[]
                            { 0xAA, (byte)taskId, (byte)taskCommand, (byte)taskState, 0x00 });
                    }

                    if (command == TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT && _emitSlaveInfo)
                    {
                        var regs = new ushort[TrackAmplifierRegisters.Count];
                        regs[TrackAmplifierRegisters.Identity] = SimulatedTrackAmplifier.Phase1Identity;
                        regs[TrackAmplifierRegisters.FwVersionPatchProtocol] = 0x0001;
                        _frames.Writer.TryWrite(TrackSlaveInfoFrame.Encode(_slave, 1, regs));
                    }
                }

                return Task.CompletedTask;
            }

            public async IAsyncEnumerable<byte[]> ReceiveAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                while (await _frames.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (_frames.Reader.TryRead(out byte[]? frame))
                    {
                        yield return frame;
                    }
                }
            }

            public ValueTask DisposeAsync() => default;
        }
    }
}
