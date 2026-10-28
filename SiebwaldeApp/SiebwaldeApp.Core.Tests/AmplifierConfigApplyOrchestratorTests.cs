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
            TimeSpan? disableVerifyTimeout = null,
            TimeSpan? rearmConfirmTimeout = null)
            => AmplifierConfigApplyOrchestrator.Create(
                () => transport,
                acknowledgementTimeout: acknowledgementTimeout ?? TimeSpan.FromSeconds(5),
                heartbeatInterval: heartbeatInterval,
                readbackTimeout: readbackTimeout ?? TimeSpan.FromSeconds(2),
                disableVerifyTimeout: disableVerifyTimeout ?? TimeSpan.FromSeconds(2),
                rearmConfirmTimeout: rearmConfirmTimeout ?? TimeSpan.FromMilliseconds(100));

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
            Assert.False(amp.EnableActiveReported);      // logical enable-active never re-set; NOT proof of H-bridge de-energization
            Assert.False(amp.BrakeActive);
            Assert.True(amp.Inhibited);                   // still inhibited after APPLY
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, amp.AppliedPwm);
            Assert.Null(amp.Failure);

            // Heartbeat supervision ran and did not spuriously latch.
            Assert.True(result.Heartbeat.Supervised);
            Assert.True(result.Heartbeat.HeartbeatSent >= 1);
            Assert.False(result.Heartbeat.Latched);
            Assert.Equal(0, result.Heartbeat.GlobalStopSent);
            Assert.True(result.Heartbeat.Rearmed); // 112 re-arm was emitted on the completed run

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

        [Fact]
        public async Task ApplyConfig_InitOrder_Is250_107_100_106_101_104_105()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            await using var orchestrator = CreateOrchestrator(transport);

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            // The apply-config init commands must be emitted in the deployed-PIC32 order.
            byte[] initOrder = { 250, 107, 100, 106, 101, 104, 105 };
            byte[] emitted = transport.SentCommands.Where(c => initOrder.Contains(c)).ToArray();
            Assert.Equal(initOrder, emitted);

            // 107 RESET must precede 100 SLAVES_ON.
            Assert.True(
                Array.IndexOf(initOrder, (byte)107) < Array.IndexOf(initOrder, (byte)100),
                "107 RESET must precede 100 SLAVES_ON");

            AmplifierConfigApplyCommandReport resetReport = result.Init.Commands.Single(c => c.Command == 107);
            Assert.Equal("EXEC_MBUS_STATE_RESET", resetReport.Name);
            Assert.Equal(1, resetReport.Transmitted);
            Assert.Equal(1, resetReport.Acknowledged);
            Assert.Equal("DONE", resetReport.State);
            Assert.Equal("READY", result.Init.Readiness);
        }

        [Fact]
        public void ApplyConfigAllowlist_Includes107_ReadOnlyExcludes107()
        {
            // 107 is in the apply-config (state-changing) command set, never the read-only allowlist.
            Assert.True(KoploperSessionCommandApplyAllowlist.IsPermitted(TrackCommand.EXEC_MBUS_STATE_RESET));
            Assert.False(KoploperSessionCommandAllowlist.IsPermitted(TrackCommand.EXEC_MBUS_STATE_RESET));
            Assert.Equal(
                new byte[] { 250, 107, 100, 106, 101, 104, 105 },
                KoploperSessionCommandApplyAllowlist.ApplyConfigOrder);

            // REARM_GLOBAL_STOP (112) is a raw-comm safety command: it must NEVER appear in either
            // session allowlist — the read-only allowlist in particular, since it is not a read.
            Assert.False(KoploperSessionCommandAllowlist.IsPermitted(TrackCommand.REARM_GLOBAL_STOP));
            Assert.False(KoploperSessionCommandApplyAllowlist.IsPermitted(TrackCommand.REARM_GLOBAL_STOP));
        }

        // ---------------------------------------------------------------------
        // Global-stop re-arm (112): clears a prior session's latched host-loss stop
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ApplyConfig_RearmsGlobalStop_AfterConnectBeforeReset()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            await using var orchestrator = CreateOrchestrator(transport);

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            byte[] sent = transport.SentCommands.ToArray();
            int connect = Array.IndexOf(sent, (byte)KoploperSessionCommandAllowlist.ClientConnectionRequest);
            int rearm = Array.IndexOf(sent, (byte)TrackCommand.REARM_GLOBAL_STOP);
            int reset = Array.IndexOf(sent, (byte)TrackCommand.EXEC_MBUS_STATE_RESET);

            Assert.True(connect >= 0, "250 CONNECT must be emitted");
            Assert.True(rearm > connect, "112 REARM_GLOBAL_STOP must follow 250 CONNECT");
            Assert.True(reset > rearm, "107 RESET must follow 112 REARM_GLOBAL_STOP");

            // The orchestrator must never emit a GLOBAL_STOP_STATE (111) query; 111 is the PIC32's
            // unsolicited status report and is only ever consumed, never sent. Querying it before the
            // re-arm would read back a latched status (1/2) and sticky-latch the C# side.
            Assert.DoesNotContain((byte)TrackCommand.GLOBAL_STOP_STATE, sent);

            Assert.True(result.Heartbeat.Rearmed);
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
        // Repeated execution / reset idempotence (safety property)
        // ---------------------------------------------------------------------

        // Full re-arm + apply-config init sequence in order:
        // 250 CONNECT → 112 REARM → 107 RESET → 100 → 106 → 101 → 104 → 105.
        // 112 is a raw-comm command (never in the guard allowlist), tracked separately here.
        private static readonly byte[] ApplyConfigInitOrder =
        {
            KoploperSessionCommandAllowlist.ClientConnectionRequest, // 250
            TrackCommand.REARM_GLOBAL_STOP,                          // 112
            TrackCommand.EXEC_MBUS_STATE_RESET,                      // 107
            TrackCommand.EXEC_MBUS_STATE_SLAVES_ON,                  // 100
            TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD,          // 106
            TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT,               // 101
            TrackCommand.EXEC_MBUS_STATE_SLAVE_INIT,                 // 104
            TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE,               // 105
        };

        [Fact]
        public async Task ApplyConfig_RepeatedExecution_AlwaysRearmsAndResetsFirst()
        {
            // Run the orchestrator twice, each with a fresh orchestrator over a fresh deterministic
            // transport (matching the existing test pattern; the transport is single-open). Each run
            // must independently re-arm (112) and RESET (107) before any register write: 107 is what
            // clears config_ok on every run, so the second run is safe even though a prior run may
            // have left the amplifier configured/enabled.
            for (int run = 0; run < 2; run++)
            {
                var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                    detectedSlaves: new byte[] { 1 }));

                await using (var orchestrator = CreateOrchestrator(
                    transport, heartbeatInterval: TimeSpan.FromMilliseconds(20)))
                {
                    await orchestrator.RunAsync(new byte[] { 1 });
                }

                AssertRearmThenResetBeforeDataExchange(
                    transport.SentCommands, 0, transport.SentCommands.Count);
            }
        }

        [Fact]
        public async Task ApplyConfig_PriorPartialSuccess_ResetPrecedesConfig()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            await using var orchestrator = CreateOrchestrator(transport);
            await orchestrator.RunAsync(new byte[] { 1 });

            IReadOnlyList<byte[]> frames = transport.SentFrames;

            int resetIndex = -1;
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i].Length >= 2 && frames[i][1] == TrackCommand.EXEC_MBUS_STATE_RESET)
                {
                    resetIndex = i;
                    break;
                }
            }

            Assert.True(resetIndex >= 0, "107 RESET must be transmitted before any register write");

            bool anyHr0Write = false;
            bool anyEnableWrite = false;
            int disableWrites = 0;
            int configWrites = 0;
            int applyWrites = 0;

            for (int i = 0; i < frames.Count; i++)
            {
                byte[] frame = frames[i];
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
                    if (value == TrackAmplifierRegisters.EnableBit)
                    {
                        anyEnableWrite = true; // HR1=0x8000 ENABLE must never be written
                    }

                    if (value == TrackAmplifierRegisters.DisableBit)
                    {
                        disableWrites++;
                        Assert.True(i > resetIndex,
                            $"DISABLE write (frame {i}, HR1=0x{value:X4}) must follow 107 RESET (frame {resetIndex})");
                    }
                    else if (value == TrackAmplifierRegisters.ApplyConfigBit)
                    {
                        applyWrites++;
                        Assert.True(i > resetIndex,
                            $"APPLY write (frame {i}, HR1=0x{value:X4}) must follow 107 RESET (frame {resetIndex})");
                    }
                }
                else if (startReg >= (byte)TrackAmplifierRegisters.ConfigRamp &&
                         startReg <= (byte)TrackAmplifierRegisters.ConfigTag)
                {
                    configWrites++;
                    Assert.True(i > resetIndex,
                        $"CONFIG write (frame {i}, HR{startReg}) must follow 107 RESET (frame {resetIndex})");
                }
            }

            Assert.False(anyHr0Write, "apply-config must never write HR0 (PWM command)");
            Assert.False(anyEnableWrite, "apply-config must never write HR1=0x8000 (ENABLE)");
            Assert.True(disableWrites >= 1, "DISABLE (HR1=0x0400) must be written");
            Assert.True(configWrites >= 4, "CONFIG (HR3..6) must be written");
            Assert.True(applyWrites >= 1, "APPLY (HR1=0x2000) must be written");
        }

        private static void AssertRearmThenResetBeforeDataExchange(
            IReadOnlyList<byte> commands, int startIndex, int endIndex)
        {
            // The re-arm + init subsequence must appear exactly, in order, within this run's command
            // stream (heartbeat 110 and data-exchange 108 frames are interleaved and filtered out).
            byte[] emitted = commands
                .Skip(startIndex)
                .Take(endIndex - startIndex)
                .Where(c => ApplyConfigInitOrder.Contains(c))
                .ToArray();
            Assert.Equal(ApplyConfigInitOrder, emitted);

            // Explicit (redundant with the exact-order assert): 112 REARM precedes 107 RESET.
            Assert.True(
                Array.IndexOf(emitted, (byte)TrackCommand.REARM_GLOBAL_STOP) <
                Array.IndexOf(emitted, (byte)TrackCommand.EXEC_MBUS_STATE_RESET),
                "112 REARM_GLOBAL_STOP must precede 107 RESET");

            int resetIndex = -1;
            int firstDataExchangeIndex = -1;
            for (int i = startIndex; i < endIndex; i++)
            {
                byte command = commands[i];
                if (command == TrackCommand.EXEC_MBUS_STATE_RESET && resetIndex < 0)
                {
                    resetIndex = i;
                }
                else if (command == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH && firstDataExchangeIndex < 0)
                {
                    firstDataExchangeIndex = i;
                    break;
                }
            }

            Assert.True(resetIndex >= 0, "107 RESET must be transmitted in this run");
            Assert.True(firstDataExchangeIndex >= 0, "a 108 data-exchange write must be transmitted in this run");
            Assert.True(resetIndex < firstDataExchangeIndex,
                "107 RESET must precede the first DISABLE/CONFIG/APPLY (108) write");
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
            Assert.Equal(107, result.FailedCommand);
            Assert.Equal("EXEC_MBUS_STATE_RESET", result.FailedCommandName);
            Assert.Empty(result.Amplifiers);
            Assert.Equal("FAILED", result.Init.Readiness);
            Assert.True(result.Heartbeat.Rearmed); // re-arm precedes 107; a post-rearm failure still records it
        }

        [Fact]
        public async Task ApplyConfig_NoReadback_TimesOutAndReportsNoCompatible()
        {
            // Acks the full init but never emits SLAVEINFO: the readback wait times out and the
            // amplifier is reported not-detected / not-compatible (never fabricated as configured).
            var transport = new FakeInitTransport(slave: 1, emitSlaveInfo: false,
                new byte[] { 250, 107, 100, 106, 101, 104, 105 });
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
        // DISABLE verification: verify the requested STATE, not the first frame
        // ---------------------------------------------------------------------

        [Fact]
        public async Task DisableVerify_StaleFrameThenClear_WaitsForState()
        {
            // The first post-DISABLE SLAVEINFO still reports enable-active=1 (a periodic poll that
            // read HR8 before the DISABLE was dispatched); a later frame reports enable-active=0.
            // The corrected verification must keep observing until the requested state is reached,
            // not fail on the first stale frame.
            var transport = new DisableVerifyTransport(slave: 1, initialHr8: 0xD100, clearsOnDisable: true);
            await using var orchestrator = CreateOrchestrator(
                transport, disableVerifyTimeout: TimeSpan.FromSeconds(2));

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            Assert.Equal("PASS", result.Status);
            AmplifierConfigApplyAmplifierResult amp = Assert.Single(result.Amplifiers);
            Assert.True(amp.DisableAccepted);
            Assert.True(amp.DisableVerified);
            Assert.False(amp.EnableActiveAfterDisable);
            Assert.True(amp.InhibitedAfterDisable);
            Assert.True(amp.ConfigDownloaded); // DISABLE verified -> CONFIG/APPLY proceeded
        }

        [Fact]
        public async Task DisableVerify_DesiredStateImmediatelyVisible_Passes()
        {
            var transport = new DisableVerifyTransport(slave: 1, initialHr8: 0xD000, clearsOnDisable: true);
            await using var orchestrator = CreateOrchestrator(
                transport, disableVerifyTimeout: TimeSpan.FromSeconds(2));

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            Assert.Equal("PASS", result.Status);
            Assert.True(Assert.Single(result.Amplifiers).DisableVerified);
        }

        [Fact]
        public async Task DisableVerify_StateNeverReached_TimesOutAndFails_NoConfigApplied()
        {
            // The DISABLE write is accepted but enable-active never clears: the verification must
            // time out bounded, fail closed, and report the last observed HR8 — and CONFIG/APPLY must
            // never follow a failed DISABLE.
            var transport = new DisableVerifyTransport(slave: 1, initialHr8: 0xD100, clearsOnDisable: false);
            await using var orchestrator = CreateOrchestrator(
                transport, disableVerifyTimeout: TimeSpan.FromMilliseconds(300));

            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 });

            Assert.Equal("FAILED", result.Status);
            AmplifierConfigApplyAmplifierResult amp = Assert.Single(result.Amplifiers);
            Assert.True(amp.DisableAccepted);   // the DISABLE write was transmitted
            Assert.False(amp.DisableVerified);  // the requested state was never observed
            Assert.False(amp.ConfigDownloaded); // no premature CONFIG/APPLY
            Assert.False(amp.ConfigVerified);
            Assert.NotNull(amp.Failure);
            Assert.Contains("HR8=", amp.FailureResponse); // reports the last observed HR8
        }

        [Fact]
        public async Task DisableVerify_Cancelled_StopsObservingAndFailsClosed()
        {
            var transport = new DisableVerifyTransport(slave: 1, initialHr8: 0xD100, clearsOnDisable: false);
            await using var orchestrator = CreateOrchestrator(transport);
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(600));

            // Cancel mid-session: the verification must stop observing and report failure (fail-closed),
            // never hang and never proceed to CONFIG.
            AmplifierConfigApplyResult result = await orchestrator.RunAsync(new byte[] { 1 }, null, cts.Token);

            Assert.Equal("FAILED", result.Status);
            AmplifierConfigApplyAmplifierResult amp = Assert.Single(result.Amplifiers);
            Assert.False(amp.DisableVerified);
            Assert.False(amp.ConfigDownloaded);
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
        // Disable-verify transport: emits SLAVEINFO with a controllable HR8 that models the
        // periodic-poll race (a stale enable-active=1 frame can arrive after the DISABLE write,
        // before the DISABLE is applied).
        // ---------------------------------------------------------------------

        private sealed class DisableVerifyTransport : ITrackTransport
        {
            private readonly Channel<byte[]> _frames = Channel.CreateUnbounded<byte[]>();
            private readonly byte _slave;
            private readonly bool _clearsOnDisable;
            private readonly CancellationTokenSource _cts = new();
            private readonly object _sync = new();
            private readonly ushort[] _regs = new ushort[TrackAmplifierRegisters.Count];

            public DisableVerifyTransport(byte slave, ushort initialHr8, bool clearsOnDisable)
            {
                _slave = slave;
                _clearsOnDisable = clearsOnDisable;
                _regs[TrackAmplifierRegisters.Identity] = SimulatedTrackAmplifier.Phase1Identity;
                _regs[TrackAmplifierRegisters.FwVersionPatchProtocol] = 0x0001;
                _regs[TrackAmplifierRegisters.State] = initialHr8;
            }

            public Task OpenAsync(CancellationToken cancellationToken = default)
            {
                _ = Task.Run(() => PeriodicEmitLoopAsync(_cts.Token));
                return Task.CompletedTask;
            }

            public Task CloseAsync(CancellationToken cancellationToken = default)
            {
                _cts.Cancel();
                _frames.Writer.TryComplete();
                return Task.CompletedTask;
            }

            public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            {
                if (count < 2)
                {
                    return Task.CompletedTask;
                }

                byte command = buffer[offset + 1];

                if (KoploperSessionCommandAllowlist.TryGetExpectedAcknowledgement(
                        command, out ushort taskId, out ushort taskCommand, out ushort taskState))
                {
                    _frames.Writer.TryWrite(
                        new byte[] { 0xAA, (byte)taskId, (byte)taskCommand, (byte)taskState, 0x00 });
                }

                if (command == TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT)
                {
                    EmitSlaveInfo();
                }
                else if (command == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH && count >= 8)
                {
                    // frame: [0]=HEADER [1]=108 [2]=slave [3]=direction [4]=regCount [5]=startReg [6..]=value
                    byte direction = buffer[offset + 3];
                    byte startReg = buffer[offset + 5];
                    ushort value = (ushort)(buffer[offset + 6] | (buffer[offset + 7] << 8));

                    if (direction == 0xAA) // write
                    {
                        ApplyWrite(startReg, value);
                    }

                    EmitSlaveInfo();
                }

                return Task.CompletedTask;
            }

            private void ApplyWrite(byte startReg, ushort value)
            {
                lock (_sync)
                {
                    if (startReg == TrackAmplifierRegisters.Control)
                    {
                        if (value == TrackAmplifierRegisters.DisableBit && _clearsOnDisable)
                        {
                            // The DISABLE is applied a moment later (mailbox dispatch latency); a
                            // periodic poll can still read the pre-DISABLE HR8 in the meantime.
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await Task.Delay(50, _cts.Token).ConfigureAwait(false);
                                }
                                catch (OperationCanceledException)
                                {
                                    return;
                                }

                                lock (_sync) { _regs[TrackAmplifierRegisters.State] &= 0xFEFF; } // clear enable-active
                                EmitSlaveInfo();
                            });
                        }
                        else if (value == TrackAmplifierRegisters.ApplyConfigBit)
                        {
                            // APPLY: config becomes active -> INIT_DONE | CONFIG_OK (0x0600).
                            _regs[TrackAmplifierRegisters.State] |=
                                (ushort)(TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit);
                        }
                        // HR1=0 (re-arm APPLY) and ENABLE (0x8000) are intentionally not modelled.
                    }
                    else if (startReg >= TrackAmplifierRegisters.ConfigRamp &&
                             startReg <= TrackAmplifierRegisters.ConfigTag)
                    {
                        _regs[startReg] = value; // config echo
                    }
                }
            }

            private async Task PeriodicEmitLoopAsync(CancellationToken ct)
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(20, ct).ConfigureAwait(false);
                        EmitSlaveInfo();
                    }
                }
                catch (OperationCanceledException)
                {
                    // shutdown
                }
            }

            private void EmitSlaveInfo()
            {
                ushort[] snapshot;
                lock (_sync) { snapshot = (ushort[])_regs.Clone(); }
                _frames.Writer.TryWrite(TrackSlaveInfoFrame.Encode(_slave, 1, snapshot));
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
