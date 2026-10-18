using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Phase-1 C# host work-package tests: HR7 identity fail-closed, config download + APPLY +
    /// readback round-trip, applied-PWM gating, INHIBITED/lease consumption, 111 stop-confirmation
    /// consumption, release-authority path, and deterministic simulator ramp/lease modelling.
    /// Software/protocol only: no hardware or live application.
    /// </summary>
    public class AmplifierPhase1HostTests
    {
        // ---------------------------------------------------------------------
        // HR7 identity fail-closed (AC6)
        // ---------------------------------------------------------------------

        [Theory]
        [InlineData(0x0000)]
        [InlineData(0x0201)] // wrong map version
        [InlineData(0x0121)] // wrong device class
        [InlineData(0x0100)] // zero capability
        public void Identity_FailClosed_MarksIncompatibleFirmware(ushort identity)
        {
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            var client = new TrackCommClientAsync(transport, variables);

            // Deliver a SLAVEINFO frame whose HR7 identity is incompatible.
            var regs = new ushort[TrackAmplifierRegisters.Count];
            regs[TrackAmplifierRegisters.Identity] = identity;
            Parse(client, TrackSlaveInfoFrame.Encode(1, 1, regs));

            var item = variables.trackAmpItems[1];
            Assert.Equal(AmplifierPhase1Safety.IncompatibleFirmware, item.Phase1Failure);
            Assert.False(item.ConfigurationVerified);
            Assert.False(AmplifierPhase1Safety.Compatible(item));
        }

        [Fact]
        public void Identity_Compatible_ClearsIncompatibleFailure()
        {
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);
            var item = variables.trackAmpItems[1];
            item.SlaveDetected = 1;
            item.Phase1Failure = AmplifierPhase1Safety.IncompatibleFirmware;

            var regs = new ushort[TrackAmplifierRegisters.Count];
            regs[TrackAmplifierRegisters.Identity] = SimulatedTrackAmplifier.Phase1Identity;
            regs[TrackAmplifierRegisters.FwVersionPatchProtocol] = 0x0001; // protocol 0x01
            Parse(client, TrackSlaveInfoFrame.Encode(1, 1, regs));

            Assert.Null(item.Phase1Failure);
            Assert.True(TrackAmplifierRegisters.HasCompatibleIdentityFields(SimulatedTrackAmplifier.Phase1Identity));
        }

        [Fact]
        public void Identity_BackplaneSlaves_AreNotIdentityChecked()
        {
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);

            var regs = new ushort[TrackAmplifierRegisters.Count]; // HR7 = 0 (not a track amp)
            Parse(client, TrackSlaveInfoFrame.Encode(51, 1, regs));

            Assert.Null(variables.trackAmpItems[51].Phase1Failure);
        }

        // ---------------------------------------------------------------------
        // Protocol version gate (HR22, secondary to HR7 identity)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ProtocolVersion01_Compatible_AllowsConfigDownload()
        {
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            var client = new TrackCommClientAsync(transport, variables);
            await client.StartAsync(false);

            await client.SendAsync(new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT, Array.Empty<byte>()));
            await WaitUntilAsync(() => variables.trackAmpItems[1].ProtocolReadbackObserved, TimeSpan.FromSeconds(2));

            var item = variables.trackAmpItems[1];
            Assert.Equal(0x01, TrackAmplifierRegisters.FwProtocolVersion(
                item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]));
            Assert.True(AmplifierPhase1Safety.Compatible(item));
            Assert.Null(AmplifierPhase1Safety.IncompatibilityReason(item));

            var downloader = new AmplifierConfigurationDownloader(client);
            var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);
            Assert.True(ok);
            Assert.True(item.ConfigurationVerified);
            Assert.Null(item.Phase1Failure);

            await client.DisposeAsync();
        }

        [Fact]
        public async Task ProtocolVersion02_Incompatible_BlocksConfigDownload()
        {
            var config = new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 },
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x02 } });
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(config);
            var client = new TrackCommClientAsync(transport, variables);
            await client.StartAsync(false);

            await client.SendAsync(new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT, Array.Empty<byte>()));
            await WaitUntilAsync(() => variables.trackAmpItems[1].ProtocolReadbackObserved, TimeSpan.FromSeconds(2));

            var item = variables.trackAmpItems[1];
            // HR7 identity is Phase-1 compatible, but HR22 protocol is not.
            Assert.True(TrackAmplifierRegisters.HasCompatibleIdentityFields(
                item.HoldingReg[TrackAmplifierRegisters.Identity]));
            Assert.False(TrackAmplifierRegisters.HasCompatibleProtocolVersion(
                item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]));
            Assert.False(AmplifierPhase1Safety.Compatible(item));
            Assert.Equal(AmplifierPhase1Safety.IncompatibleProtocolVersion,
                AmplifierPhase1Safety.IncompatibilityReason(item));
            Assert.Equal(AmplifierPhase1Safety.IncompatibleProtocolVersion, item.Phase1Failure);

            var downloader = new AmplifierConfigurationDownloader(client);
            var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);
            Assert.False(ok);
            Assert.False(item.ConfigurationVerified);
            Assert.Equal(AmplifierPhase1Safety.IncompatibleProtocolVersion, item.Phase1Failure);

            await client.DisposeAsync();
        }

        [Fact]
        public async Task ProtocolVersion00_Absent_IsUnknownIncompatible_Blocked()
        {
            var config = new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 },
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x00 } });
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(config);
            var client = new TrackCommClientAsync(transport, variables);
            await client.StartAsync(false);

            await client.SendAsync(new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT, Array.Empty<byte>()));
            await WaitUntilAsync(() => variables.trackAmpItems[1].ProtocolReadbackObserved, TimeSpan.FromSeconds(2));

            var item = variables.trackAmpItems[1];
            Assert.True(TrackAmplifierRegisters.HasCompatibleIdentityFields(
                item.HoldingReg[TrackAmplifierRegisters.Identity]));
            Assert.Equal(0x00, TrackAmplifierRegisters.FwProtocolVersion(
                item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]));
            // An absent/exception/legacy HR22 (0x00) is UNKNOWN/INCOMPATIBLE, never "compatible by default".
            Assert.False(TrackAmplifierRegisters.HasCompatibleProtocolVersion(
                item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]));
            Assert.False(AmplifierPhase1Safety.Compatible(item));
            Assert.Equal(AmplifierPhase1Safety.IncompatibleProtocolVersion,
                AmplifierPhase1Safety.IncompatibilityReason(item));
            Assert.Equal(AmplifierPhase1Safety.IncompatibleProtocolVersion, item.Phase1Failure);

            var downloader = new AmplifierConfigurationDownloader(client);
            var ok = await downloader.DownloadAsync(item, new AmplifierConfiguration(), CancellationToken.None);
            Assert.False(ok);
            Assert.False(item.ConfigurationVerified);

            await client.DisposeAsync();
        }

        [Fact]
        public void LegacyIdentity_BlockedAsIncompatibleFirmware_EvenWhenProtocolValid()
        {
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(new UnusedTransport(), variables);

            // A legacy 12-register device exposes an unstable counter on HR7 (the shape check
            // rejects it). HR22 is set to a would-be-valid protocol to prove the protocol check is
            // strictly secondary: a legacy identity is never misreported as a protocol problem.
            var regs = new ushort[TrackAmplifierRegisters.Count];
            regs[TrackAmplifierRegisters.Identity] = 0x1234; // counter value, not a compatible identity
            regs[TrackAmplifierRegisters.FwVersionPatchProtocol] = 0x0001;
            Parse(client, TrackSlaveInfoFrame.Encode(1, 1, regs));

            var item = variables.trackAmpItems[1];
            Assert.Equal(AmplifierPhase1Safety.IncompatibleFirmware, item.Phase1Failure);
            Assert.Equal(AmplifierPhase1Safety.IncompatibleFirmware, AmplifierPhase1Safety.IncompatibilityReason(item));
            Assert.NotEqual(AmplifierPhase1Safety.IncompatibleProtocolVersion, item.Phase1Failure);
            Assert.False(item.ConfigurationVerified);
        }

        [Fact]
        public async Task ReadOnlyVersionReporting_StillAvailable_WhenControlBlocked()
        {
            var config = new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 },
                protocolVersionOverrides: new Dictionary<byte, ushort> { { 1, 0x02 } });
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(config);
            var client = new TrackCommClientAsync(transport, variables);
            await client.StartAsync(false);

            await client.SendAsync(new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT, Array.Empty<byte>()));
            await WaitUntilAsync(() => variables.trackAmpItems[1].ProtocolReadbackObserved, TimeSpan.FromSeconds(2));

            var item = variables.trackAmpItems[1];
            Assert.False(AmplifierPhase1Safety.Compatible(item)); // write/control authority denied

            // Version/protocol reporting (HR21/HR22) remains readable.
            Assert.Equal(1, TrackAmplifierRegisters.FwMajor(item.HoldingReg[TrackAmplifierRegisters.FwVersionMajorMinor]));
            Assert.Equal(0, TrackAmplifierRegisters.FwMinor(item.HoldingReg[TrackAmplifierRegisters.FwVersionMajorMinor]));
            Assert.Equal(0x02, TrackAmplifierRegisters.FwProtocolVersion(
                item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]));

            // The read-only PIC32 controller version (113) round-trip is unaffected (no gating).
            await client.SendAsync(new SendMessage(TrackCommand.GET_CONTROLLER_VERSION, Array.Empty<byte>()));
            await WaitUntilAsync(() => variables.trackControllerCommands.ControllerVersion != null, TimeSpan.FromSeconds(2));
            Assert.Equal(0x01, variables.trackControllerCommands.ControllerVersion!.Value.ProtocolVersion);

            await client.DisposeAsync();
        }

        // ---------------------------------------------------------------------
        // Config download + APPLY + readback round-trip (AC2/AC3/AC6)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ConfigDownload_AppliesAndVerifiesInitDoneConfigOk()
        {
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 }));
            var client = new TrackCommClientAsync(transport, variables);
            await client.StartAsync(false);

            // Detect so the simulator emits a fresh SLAVEINFO readback (detected + fresh + genuine).
            await client.SendAsync(new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT, Array.Empty<byte>()));
            await WaitUntilAsync(() => variables.trackAmpItems[1].ProtocolReadbackObserved, TimeSpan.FromSeconds(2));

            var item = variables.trackAmpItems[1];
            Assert.True(AmplifierPhase1Safety.Compatible(item));

            var downloader = new AmplifierConfigurationDownloader(client);
            var config = new AmplifierConfiguration(Acceleration: 3, Deceleration: 4, FastStop: 9,
                WatchdogMs: 300, Tag: 42);
            var ok = await downloader.DownloadAsync(item, config, CancellationToken.None);

            Assert.True(ok);
            Assert.True(item.ConfigurationVerified);
            Assert.Null(item.Phase1Failure);

            var regs = transport.GetRegisters(1);
            Assert.Equal(0x0403, regs[TrackAmplifierRegisters.ConfigRamp]); // accel 3, decel 4
            Assert.Equal(9, regs[TrackAmplifierRegisters.ConfigFastStop]);
            Assert.Equal(300, regs[TrackAmplifierRegisters.ConfigWatchdog]);
            Assert.Equal(42, regs[TrackAmplifierRegisters.ConfigTag]);
            Assert.Equal((ushort)(TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit),
                (ushort)(regs[TrackAmplifierRegisters.State] &
                    (TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit)));
            // Write-result after APPLY: last-written address HR1, result ok.
            Assert.Equal(1, regs[TrackAmplifierRegisters.WriteResult] & 0xFF);
            Assert.Equal(0, (regs[TrackAmplifierRegisters.WriteResult] >> 8) & 0x7);
            Assert.True(AmplifierPhase1Safety.Ready(item));

            await client.DisposeAsync();
        }

        [Fact]
        public void ConfigApply_RejectsOutOfRange_AndLeavesConfigNotVerified()
        {
            var amplifier = new SimulatedTrackAmplifier(1);
            // Watchdog 30 (< 50) is out of range.
            amplifier.ApplyWrite(TrackAmplifierRegisters.ConfigWatchdog, new ushort[] { 30 });
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.ApplyConfigBit });

            var regs = amplifier.Snapshot();
            Assert.Equal(0, regs[TrackAmplifierRegisters.State] & (TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit));
            Assert.NotEqual(0, regs[TrackAmplifierRegisters.Fault] & TrackAmplifierRegisters.ConfigInvalidFaultBit);
            // Rejected APPLY reports relationship + retained.
            Assert.Equal(3, (regs[TrackAmplifierRegisters.WriteResult] >> 8) & 0x7);
            Assert.NotEqual(0, regs[TrackAmplifierRegisters.WriteResult] & TrackAmplifierRegisters.WriteRetainedBit);
        }

        // ---------------------------------------------------------------------
        // Applied-PWM gating (item 3)
        // ---------------------------------------------------------------------

        [Fact]
        public void AppliedNeutral_RequiresAppliedReadbackCapability()
        {
            var neutralWithCapability = new ushort[TrackAmplifierRegisters.Count];
            neutralWithCapability[TrackAmplifierRegisters.Identity] = SimulatedTrackAmplifier.Phase1Identity;
            neutralWithCapability[TrackAmplifierRegisters.AppliedPwm] = AmplifierSpeedMapper.NeutralPwm;
            Assert.True(AmplifierPhase1Safety.AppliedNeutral(neutralWithCapability));

            var noCapability = (ushort[])neutralWithCapability.Clone();
            noCapability[TrackAmplifierRegisters.Identity] = 0x0101; // capability 0
            Assert.False(AmplifierPhase1Safety.AppliedNeutral(noCapability));

            var nonNeutral = (ushort[])neutralWithCapability.Clone();
            nonNeutral[TrackAmplifierRegisters.AppliedPwm] = 500;
            Assert.False(AmplifierPhase1Safety.AppliedNeutral(nonNeutral));
        }

        [Fact]
        public void AppliedNeutralGate_DistinguishesCommandEchoFromAppliedOutput()
        {
            // Command echo neutral (HR0 == 399) but applied output non-neutral (HR14 == 400): the
            // applied gate must return false — the command echo is never treated as applied evidence.
            var items = new List<TrackAmplifierItem>
            {
                Amplifier(1, identity: SimulatedTrackAmplifier.Phase1Identity, applied: 400,
                    command: AmplifierSpeedMapper.NeutralPwm)
            };

            // The legacy command-echo predicate passes (HR0 == 399)...
            Assert.True(TrackAmplifierNeutralObservation.AreAllObservedNeutral(items, new byte[] { 1 }));
            // ...but the applied predicate fails (HR14 == 400).
            Assert.False(TrackAmplifierNeutralObservation.AreAllAppliedNeutral(items, new byte[] { 1 }));
        }

        [Fact]
        public async Task AppliedPwmReadbackOverride_ForcesNonNeutralApplied_AndBlocksAppliedGate()
        {
            var config = new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1 },
                appliedPwmReadbackOverrides: new Dictionary<byte, ushort> { { 1, 400 } });
            var transport = new DeterministicTrackTransport(config);
            var variables = new TrackApplicationVariables();
            var client = new TrackCommClientAsync(transport, variables);
            await client.StartAsync(false);
            await client.SendAsync(new SendMessage(TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT, Array.Empty<byte>()));
            await WaitUntilAsync(() => variables.trackAmpItems[1].ProtocolReadbackObserved, TimeSpan.FromSeconds(2));

            // The override surfaces HR14 = 400, so the applied gate fails even though the command
            // echo is still 399.
            Assert.False(TrackAmplifierNeutralObservation.AreAllAppliedNeutral(
                variables.trackAmpItems, new byte[] { 1 }));

            await client.DisposeAsync();
        }

        // ---------------------------------------------------------------------
        // INHIBITED / lease consumption (item 4)
        // ---------------------------------------------------------------------

        [Fact]
        public void Ready_BlocksInhibitedAndLeaseExpired_UntilReleaseCommand()
        {
            const ushort initialized = TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit;

            // Baseline: INIT_DONE+CONFIG_OK, state READY (1).
            var ready = Amplifier(1, identity: SimulatedTrackAmplifier.Phase1Identity, applied: AmplifierSpeedMapper.NeutralPwm,
                command: AmplifierSpeedMapper.NeutralPwm);
            ready.HoldingReg[TrackAmplifierRegisters.State] = (ushort)(initialized | 1);
            ready.ConfigurationVerified = true;
            Assert.True(AmplifierPhase1Safety.Ready(ready));

            // INHIBITED latched => not ready.
            ready.HoldingReg[TrackAmplifierRegisters.State] = (ushort)(initialized | 1 | TrackAmplifierRegisters.InhibitedBit);
            Assert.False(AmplifierPhase1Safety.Ready(ready));

            // LEASE_EXPIRED state (7) => not ready.
            ready.HoldingReg[TrackAmplifierRegisters.State] = (ushort)(initialized | 7 | TrackAmplifierRegisters.InhibitedBit);
            Assert.False(AmplifierPhase1Safety.Ready(ready));

            // Release command tolerates the lease-expired fault but not other faults.
            ready.HoldingReg[TrackAmplifierRegisters.Fault] = TrackAmplifierRegisters.LeaseExpiredFaultBit;
            ready.HoldingReg[TrackAmplifierRegisters.State] = (ushort)(initialized | 7 | TrackAmplifierRegisters.InhibitedBit);
            Assert.True(AmplifierPhase1Safety.Ready(ready, releaseCommand: true));
            ready.HoldingReg[TrackAmplifierRegisters.Fault] = TrackAmplifierRegisters.ConfigInvalidFaultBit;
            Assert.False(AmplifierPhase1Safety.Ready(ready, releaseCommand: true));
        }

        [Fact]
        public void Simulator_LeaseExpiry_LatchesInhibitedAndFastRampsToNeutral()
        {
            var amplifier = new SimulatedTrackAmplifier(1);
            amplifier.ApplyWrite(TrackAmplifierRegisters.ConfigWatchdog, new ushort[] { 250 });
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.ApplyConfigBit });
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.EnableBit });
            // Command a non-neutral duty so there is traction to lose on lease expiry.
            amplifier.ApplyWrite(TrackAmplifierRegisters.PwmCommand, new ushort[] { 600 });

            // Arm the lease (MOTION_PERMISSION renewal), then advance until expiry + fast ramp to neutral.
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.MotionPermissionBit });
            for (int i = 0; i < SimulatedTrackAmplifier.LeaseTimeoutTicks + 200; i++)
            {
                amplifier.Step();
            }

            var regs = amplifier.Snapshot();
            Assert.Equal(TrackAmplifierRegisters.LeaseExpiredState, regs[TrackAmplifierRegisters.State] & 0xF);
            Assert.NotEqual(0, regs[TrackAmplifierRegisters.State] & TrackAmplifierRegisters.InhibitedBit);
            Assert.NotEqual(0, regs[TrackAmplifierRegisters.Fault] & TrackAmplifierRegisters.LeaseExpiredFaultBit);
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, regs[TrackAmplifierRegisters.AppliedPwm] & TrackAmplifierRegisters.PwmMask);
        }

        [Fact]
        public void Simulator_LeaseStartsExpired_NoRunUntilRenewalThenFreshCommand()
        {
            var amplifier = new SimulatedTrackAmplifier(1);
            // Configure + APPLY + ENABLE so the only missing precondition is a fresh lease.
            amplifier.ApplyWrite(TrackAmplifierRegisters.ConfigRamp, new ushort[] { 0x0403 }); // accel 3, decel 4
            amplifier.ApplyWrite(TrackAmplifierRegisters.ConfigFastStop, new ushort[] { 9 });
            amplifier.ApplyWrite(TrackAmplifierRegisters.ConfigWatchdog, new ushort[] { 300 });
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.ApplyConfigBit });
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.EnableBit });

            // A fresh non-neutral command BEFORE any renewal must NOT produce RUN: the lease starts
            // expired (mirrors PIC18 amp_core.c lease_ticks = AMP_LEASE_TICKS at init).
            amplifier.ApplyWrite(TrackAmplifierRegisters.PwmCommand, new ushort[] { 600 });
            Assert.NotEqual(2, amplifier.Snapshot()[TrackAmplifierRegisters.State] & 0xF); // not RUN

            // An explicit MOTION_PERMISSION renewal arms the lease.
            amplifier.ApplyWrite(TrackAmplifierRegisters.Control, new ushort[] { TrackAmplifierRegisters.MotionPermissionBit });

            // A fresh non-neutral command AFTER the renewal produces RUN.
            amplifier.ApplyWrite(TrackAmplifierRegisters.PwmCommand, new ushort[] { 500 });
            Assert.Equal(2, amplifier.Snapshot()[TrackAmplifierRegisters.State] & 0xF); // RUN
        }

        // ---------------------------------------------------------------------
        // 111 stop-confirmation consumption (item 4)
        // ---------------------------------------------------------------------

        [Fact]
        public void StopConfirmation_DecodesTwoBitsPerAmplifier()
        {
            var detected = new ushort[] { 1, 2, 3, 4 };
            // A2 wire layout: payload[0] = count (4), then one packed byte: amplifier 0 confirmed
            // (0b00), 1 pending (0b01), 2 failed (0b10), 3 reserved (0b11 -> failed).
            var payload = new byte[] { 4, 0b11_10_01_00 };

            var decoded = TrackAmplifierStopConfirmation.Decode(payload, detected);

            Assert.Equal(AmplifierStopConfirmation.Confirmed, decoded[1]);
            Assert.Equal(AmplifierStopConfirmation.Pending, decoded[2]);
            Assert.Equal(AmplifierStopConfirmation.Failed, decoded[3]);
            Assert.Equal(AmplifierStopConfirmation.Failed, decoded[4]); // reserved -> failed
            Assert.False(TrackAmplifierStopConfirmation.AllConfirmed(payload, detected));
        }

        [Fact]
        public void StopConfirmation_CountByteBoundsDecode_NoZeroPaddingLeak()
        {
            // The PIC32 encodes count=2 for detected {1,2}; the single code byte's upper bits are
            // zero-padding and must NOT be decoded as extra "confirmed" amplifiers beyond count.
            var detected = new ushort[] { 1, 2, 3, 4 };
            var payload = new byte[] { 2, 0b00_00 }; // 0x00

            var decoded = TrackAmplifierStopConfirmation.Decode(payload, detected);

            Assert.Equal(AmplifierStopConfirmation.Confirmed, decoded[1]);
            Assert.Equal(AmplifierStopConfirmation.Confirmed, decoded[2]);
            Assert.Equal(AmplifierStopConfirmation.Failed, decoded[3]); // beyond count => failed
            Assert.Equal(AmplifierStopConfirmation.Failed, decoded[4]); // beyond count => failed
            Assert.False(TrackAmplifierStopConfirmation.AllConfirmed(payload, detected));
        }

        [Theory]
        [InlineData(new byte[] { 1, 0x00 }, true)]  // count=1, confirmed
        [InlineData(new byte[] { 1, 0x01 }, false)] // count=1, pending
        [InlineData(new byte[] { 1, 0x02 }, false)] // count=1, failed
        public void StopConfirmation_AllConfirmed_GatesRelease(byte[] payload, bool expected)
        {
            var detected = new ushort[] { 1 };
            Assert.Equal(expected, TrackAmplifierStopConfirmation.AllConfirmed(payload, detected));
        }

        [Fact]
        public void StopConfirmation_TruncatedPayload_IsNotConfirmed()
        {
            var detected = new ushort[] { 1, 2, 3, 4, 5 }; // count=5 needs 2 code bytes
            Assert.False(TrackAmplifierStopConfirmation.AllConfirmed(new byte[] { 5, 0x00 }, detected));
        }

        [Fact]
        public async Task GlobalStopConfirmationState_Consumes111Payload()
        {
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
            var client = new TrackCommClientAsync(transport, variables);
            await client.StartAsync(false);
            using var state = new GlobalStopConfirmationState(client);
            state.SetDetectedAmplifiers(new ushort[] { 1, 2, 3 });

            // A2 wire payload with amplifiers 1 confirmed, 2 pending, 3 confirmed: count=3 then codes.
            var payload = new byte[] { 3, 0b00_01_00 };
            state.FeedPayload(payload);
            Assert.False(state.AreAllConfirmed);

            state.FeedPayload(new byte[] { 3, 0x00 });
            Assert.True(state.AreAllConfirmed);

            await client.DisposeAsync();
        }

        [Fact]
        public async Task GlobalStopConfirmationState_DecodesWireShapedA2Payload_ThroughTransport()
        {
            var variables = new TrackApplicationVariables();
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 2, 3 }));
            var client = new TrackCommClientAsync(transport, variables);
            await client.StartAsync(false);

            using var state = new GlobalStopConfirmationState(client);
            // Physical detected order = ascending slave address (the PIC32 pack order).
            state.SetDetectedAmplifiers(new ushort[] { 1, 2, 3 });

            // Trigger the stop; the simulator emits the real >=6-byte A2 payload (count + codes)
            // through the transport receive path, not via FeedPayload.
            await client.SendAsync(new SendMessage(TrackCommand.EXEC_GLOBAL_STOP, new byte[] { 0, 0 }));

            await WaitUntilAsync(() => state.AreAllConfirmed, TimeSpan.FromSeconds(2));

            Assert.Equal(AmplifierStopConfirmation.Confirmed, state.Confirmations[1]);
            Assert.Equal(AmplifierStopConfirmation.Confirmed, state.Confirmations[2]);
            Assert.Equal(AmplifierStopConfirmation.Confirmed, state.Confirmations[3]);

            await client.DisposeAsync();
        }

        // ---------------------------------------------------------------------
        // Release authority path (item 5)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task ReleaseAuthority_BlocksWhenStopInputsNotClear()
        {
            var comm = new FakeCommClient();
            var authority = new TrackAmplifierReleaseAuthority(comm, physicalStopInputsClear: () => false);

            var result = await authority.ReleaseAsync(1, 500);

            Assert.Equal(AmplifierReleaseResultKind.BlockedStopInputsNotClear, result.Kind);
            Assert.Empty(comm.Sent);
        }

        [Fact]
        public async Task ReleaseAuthority_BlocksWhenAmplifierUnconfirmed()
        {
            var comm = new FakeCommClient();
            var authority = new TrackAmplifierReleaseAuthority(comm, physicalStopInputsClear: () => true,
                allAmplifiersConfirmed: () => false);

            var result = await authority.ReleaseAsync(1, 500);

            Assert.Equal(AmplifierReleaseResultKind.BlockedUnconfirmedAmplifier, result.Kind);
            Assert.Empty(comm.Sent);
        }

        [Fact]
        public async Task ReleaseAuthority_EmitsRearmThenFreshCommand_NoOldPwm()
        {
            var comm = new FakeCommClient();
            var authority = new TrackAmplifierReleaseAuthority(comm, physicalStopInputsClear: () => true,
                allAmplifiersConfirmed: () => true);

            var result = await authority.ReleaseAsync(1, 500);

            Assert.Equal(AmplifierReleaseResultKind.Released, result.Kind);
            Assert.Equal(2, comm.Sent.Count);
            Assert.Equal(TrackCommand.REARM_GLOBAL_STOP, comm.Sent[0].Command);
            Assert.Equal(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, comm.Sent[1].Command);
            // The fresh command writes exactly one HR0 = 500 (no old PWM is replayed).
            Assert.Equal(1, comm.Sent[1].Data[2]); // reg count
            Assert.Equal(TrackAmplifierRegisters.PwmCommand, comm.Sent[1].Data[3]); // HR0
            Assert.Equal(500, comm.Sent[1].Data[4] | (comm.Sent[1].Data[5] << 8));
        }

        [Fact]
        public async Task ReleaseAuthority_RejectsNeutralCommand()
        {
            var comm = new FakeCommClient();
            var authority = new TrackAmplifierReleaseAuthority(comm, physicalStopInputsClear: () => true);

            var result = await authority.ReleaseAsync(1, AmplifierSpeedMapper.NeutralPwm);

            Assert.Equal(AmplifierReleaseResultKind.Failed, result.Kind);
            Assert.Empty(comm.Sent);
        }

        // ---------------------------------------------------------------------
        // Simulator ramp modelling (item 6)
        // ---------------------------------------------------------------------

        [Fact]
        public void Simulator_RampStepsAppliedTowardTarget_AndSnapsToNeutral()
        {
            var amplifier = new SimulatedTrackAmplifier(1);
            amplifier.ApplyWrite(TrackAmplifierRegisters.PwmCommand, new ushort[] { 405 });

            int previous = amplifier.Snapshot()[TrackAmplifierRegisters.AppliedPwm] & TrackAmplifierRegisters.PwmMask;
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm, previous); // starts neutral

            // Step until the ramp reaches the 405 target; applied must move in bounded steps.
            int steps = 0;
            while ((amplifier.Snapshot()[TrackAmplifierRegisters.AppliedPwm] & TrackAmplifierRegisters.PwmMask) != 405)
            {
                amplifier.Step();
                steps++;
                Assert.True(steps < 50, "ramp did not converge in bounded steps");
            }
            Assert.True(steps > 1, "applied must step, not jump, to the target");

            // Command neutral: the ramp must snap exactly to 399 on the final step.
            amplifier.ApplyWrite(TrackAmplifierRegisters.PwmCommand, new ushort[] { AmplifierSpeedMapper.NeutralPwm });
            for (int i = 0; i < 50; i++)
            {
                amplifier.Step();
                if ((amplifier.Snapshot()[TrackAmplifierRegisters.AppliedPwm] & TrackAmplifierRegisters.PwmMask) == AmplifierSpeedMapper.NeutralPwm)
                {
                    break;
                }
            }
            Assert.Equal(AmplifierSpeedMapper.NeutralPwm,
                amplifier.Snapshot()[TrackAmplifierRegisters.AppliedPwm] & TrackAmplifierRegisters.PwmMask);
        }

        [Fact]
        public void Simulator_WriteResultReportsLastWrittenAddressAndReason()
        {
            var amplifier = new SimulatedTrackAmplifier(1);
            amplifier.ApplyWrite(TrackAmplifierRegisters.ConfigTag, new ushort[] { 7 });
            Assert.Equal(TrackAmplifierRegisters.ConfigTag, amplifier.Snapshot()[TrackAmplifierRegisters.WriteResult] & 0xFF);

            // Write to a read-only register (HR7 identity) => read-only reason (2), retained.
            amplifier.ApplyWrite(TrackAmplifierRegisters.Identity, new ushort[] { 0x1234 });
            var regs = amplifier.Snapshot();
            Assert.Equal(TrackAmplifierRegisters.Identity, regs[TrackAmplifierRegisters.WriteResult] & 0xFF);
            Assert.Equal(2, (regs[TrackAmplifierRegisters.WriteResult] >> 8) & 0x7);
            Assert.Equal(SimulatedTrackAmplifier.Phase1Identity, regs[TrackAmplifierRegisters.Identity]); // unchanged
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private static TrackAmplifierItem Amplifier(ushort slave, ushort identity, ushort applied, ushort command)
        {
            var regs = new ushort[TrackAmplifierRegisters.Count];
            regs[TrackAmplifierRegisters.Identity] = identity;
            regs[TrackAmplifierRegisters.AppliedPwm] = applied;
            regs[TrackAmplifierRegisters.PwmCommand] = command;
            regs[TrackAmplifierRegisters.FwVersionPatchProtocol] = 0x0001; // protocol 0x01
            return new TrackAmplifierItem
            {
                SlaveNumber = slave,
                SlaveDetected = 1,
                HoldingReg = regs,
                ProtocolReadbackObserved = true,
                LastDataReceivedUtc = DateTimeOffset.UtcNow
            };
        }

        private static void Parse(TrackCommClientAsync client, byte[] frame)
            => typeof(TrackCommClientAsync).GetMethod("HandleNewData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(client, new object[] { frame });

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (condition())
                {
                    return;
                }

                await Task.Delay(10);
            }

            throw new TimeoutException($"Condition was not met within {timeout}.");
        }

        private sealed class UnusedTransport : ITrackTransport
        {
            public Task OpenAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task CloseAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
            public IAsyncEnumerable<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
            public ValueTask DisposeAsync() => default;
        }

        private sealed class FakeCommClient : ITrackCommClient
        {
            public List<SendMessage> Sent { get; } = new();
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }
            public ValueTask DisposeAsync() => default;
        }
    }
}
