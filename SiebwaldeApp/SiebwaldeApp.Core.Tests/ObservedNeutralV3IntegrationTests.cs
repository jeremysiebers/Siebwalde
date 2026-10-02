using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Properties;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// V3 software integration tests for the observed-neutral movement gate.
    ///
    /// These tests drive a normal locomotive-speed request end-to-end through the REAL production
    /// composition (no second implementation, no fake <see cref="IEcosHostService"/>):
    ///
    ///   <c>set(1000,speedstep[10])</c>
    ///     -> <see cref="SimpleEcosBackend"/>
    ///     -> <see cref="ControlSafetyInterlockBackend"/> (movement gate / safety interlock)
    ///     -> <see cref="TrackAmplifierHardwareBackend.SetLocoSpeed"/>
    ///     -> <see cref="TrackApplicationVariables.SetDesiredAmplifierControl"/> (shared gate)
    ///     -> <see cref="TrackControlMain"/> write loop
    ///     -> <c>EXEC_MBUS_SLAVE_DATA_EXCH</c> (108)
    ///     -> <see cref="DeterministicTrackTransport"/> (software PIC32 emulation)
    ///     -> SLAVEINFO echo
    ///     -> C# <c>HoldingReg[0]</c> readback.
    ///
    /// Software-only: the simulated HR0 readback is the PIC18 holding-register echo, NOT applied
    /// physical PWM. No claim about physical neutral or physical movement is made here.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class ObservedNeutralV3IntegrationTests
    {
        private static BlockTopology Topology => BlockTopology.Parse("amps: 1:1,2:2 ; routes: 1>2,2>1");

        private static KoploperBlockMap BlockMap => KoploperBlockMap.Parse("1:1.01:1, 2:1.02:2");

        private static readonly byte[] DetectedSlaves = { 1, 2, 3 };

        // ---------------------------------------------------------------------------------
        // Scenarios
        // ---------------------------------------------------------------------------------

        [Fact]
        public async Task AcceptedPath_GrantedPermission_NonNeutralRequestReachesTheTransportAndEchoes()
        {
            var groups = TrackAmplifierGroups.Create(new[] { 1, 2, 3 }, null, null);

            await RunAsync(groups, async (runtime, simulator, ecosPort, ct) =>
            {
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.True(runtime.IsMovementSafe);

                // The expected non-neutral HR0 is computed through the production mappers, so the
                // test tracks them without hardcoding the value. speedstep[10] on DCC28 normalizes
                // to 45, and the default direction (1) maps into the reverse PWM band.
                Assert.True(ProtocolSpeedNormalizer.TryNormalize("DCC28", 10, out var normalizedSpeed));
                var expectedPwm = AmplifierSpeedMapper.ToPwm(normalizedSpeed, direction: 1);
                var expectedHr0 = TrackApplicationVariables.BuildHr0Value(expectedPwm, emoStop: false);
                Assert.NotEqual(AmplifierSpeedMapper.NeutralPwm, (int)expectedHr0);

                var commandsBefore = simulator.SentCommands.Count;

                // The PO's normal locomotive-speed request, through the real ECoS host.
                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));

                Assert.Equal("<END 0 (OK)>", endLine);

                // The 10 Hz write loop pushes the pending non-neutral HR0 through a 108 write; the
                // simulator stores it and echoes it back as SLAVEINFO, which the C# readback mirrors.
                await WaitUntilAsync(
                    () => simulator.GetRegisters(1)[0] == expectedHr0
                          && runtime.TrackAmplifiers[1].HoldingReg[0] == expectedHr0,
                    TimeSpan.FromSeconds(5));

                // A new 108 write carried the non-neutral HR0 to the transport.
                Assert.True(simulator.SentCommands.Count > commandsBefore);
                Assert.Contains((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);

                // Commanded vs readback: the simulator register is the written (command) echo, and
                // the C# HoldingReg[0] is the parsed SLAVEINFO readback. Both equal the commanded
                // HR0. This is the PIC18 holding-register echo only -- NOT applied physical PWM.
                Assert.Equal(expectedHr0, simulator.GetRegisters(1)[0]);
                Assert.Equal(expectedHr0, runtime.TrackAmplifiers[1].HoldingReg[0]);
            });
        }

        [Fact]
        public async Task BlockedPath_EmptySafetyDomain_BlocksEveryCallerAndCannotBeBypassed()
        {
            // An empty safety domain is fail-closed: observed neutral can never be established, so
            // movement permission stays NotGranted.
            var groups = TrackAmplifierGroups.Empty;

            await RunAsync(groups, async (runtime, simulator, ecosPort, ct) =>
            {
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);

                // (a) The normal ECoS locomotive-speed request is refused as a safety interlock.
                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));
                Assert.Equal("<END 8 (SAFETY_INTERLOCK)>", endLine);

                // Nothing was queued, so no 108 write reaches the transport and amplifier 1 stays
                // at its initial (never-written) value.
                await Task.Delay(300, ct);
                Assert.DoesNotContain((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);
                Assert.Equal(0, simulator.GetRegisters(1)[0] & 0x03FF);

                // (b) A different caller (the manual/operator path) cannot bypass the gate either:
                // SetDesiredAmplifierControl refuses the non-neutral setpoint regardless of caller.
                runtime.SetAmplifierControl(1, 500, false);
                await Task.Delay(300, ct);
                Assert.DoesNotContain((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator.SentCommands);
                Assert.Equal(0, simulator.GetRegisters(1)[0] & 0x03FF);
            });
        }

        [Fact]
        public async Task RealMode_PhysicalMappingProfile_ResolvesPhysicalDomain_AndGrantsAfterObservedNeutral()
        {
            var profile = BuildPhysicalMappingProfile();

            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-physical-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-physical-locos-{Guid.NewGuid():N}.json");
            var ecosPort = GetFreeTcpPort();
            var externalPort = GetFreeTcpPort();

            FakeKoploperExternalInfoServer? externalServer = null;
            DeterministicTrackTransport? simulator = null;
            TrackApplicationRuntimeHost? runtime = null;

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                externalServer = new FakeKoploperExternalInfoServer(externalPort, locoAddress: 1000, blockNumber: 1);

                var ecosHost = new TrackControlHost(
                    locoRepositoryPath: locoPath,
                    topology: profile.ToBlockTopology(),
                    blockMap: profile.ToKoploperBlockMap(),
                    ecosListenPort: ecosPort,
                    koploperExternalInfoHost: "127.0.0.1",
                    koploperExternalInfoPort: externalPort,
                    trackAmplifierGroups: TrackAmplifierGroups.Empty);

                // Real mode must be profile-driven: bind the physical projection (1/3/4/6).
                ecosHost.SetProfile(profile);

                runtime = new TrackApplicationRuntimeHost(
                    ecosHost,
                    controlTrace: null,
                    transportFactory: () =>
                    {
                        simulator = new DeterministicTrackTransport(
                            new TrackSimulatorConfig(detectedSlaves: new byte[] { 1, 3, 4, 6 }));
                        return simulator;
                    },
                    amplifierGroups: TrackAmplifierGroups.Empty,
                    fullSimulationProfile: profile);

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                // The observed-neutral domain is the profile's physical binding {1,3,4,6}.
                Assert.Equal(new[] { 1, 3, 4, 6 }, runtime.PhysicalAmplifierBinding);

                // Movement permission is granted only after observed neutral on that physical domain.
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.True(runtime.IsMovementSafe);
            }
            finally
            {
                if (runtime is not null)
                {
                    try { await runtime.DisposeAsync(); } catch { /* best-effort */ }
                }

                if (externalServer is not null)
                {
                    try { await externalServer.DisposeAsync(); } catch { /* best-effort */ }
                }

                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
                try { File.Delete(locoPath); } catch { /* best-effort */ }
            }
        }

        [Fact]
        public async Task RealMode_DefaultConstruction_NoProfile_NoRealProjection_FailsClosed_AndCannotAddressAmplifier()
        {
            // A directly-constructed host (no SetProfile, no explicit real projection) plus a
            // runtime with no fullSimulationProfile must stay fail-closed: Real mode must not
            // compose movement on an implicit logical-to-physical amplifier mapping.
            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-default-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-default-locos-{Guid.NewGuid():N}.json");
            var ecosPort = GetFreeTcpPort();
            var externalPort = GetFreeTcpPort();

            FakeKoploperExternalInfoServer? externalServer = null;
            DeterministicTrackTransport? simulator = null;
            TrackApplicationRuntimeHost? runtime = null;

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                externalServer = new FakeKoploperExternalInfoServer(externalPort, locoAddress: 1000, blockNumber: 1);

                // NO SetProfile and NO explicit realTopology/realBlockMap: the constructor default
                // keeps the real projection empty (fail-closed), so Real mode cannot route on an
                // implicit logical mapping.
                var ecosHost = new TrackControlHost(
                    locoRepositoryPath: locoPath,
                    topology: Topology,
                    blockMap: BlockMap,
                    ecosListenPort: ecosPort,
                    koploperExternalInfoHost: "127.0.0.1",
                    koploperExternalInfoPort: externalPort,
                    trackAmplifierGroups: TrackAmplifierGroups.Empty);

                // NO fullSimulationProfile: the observed-neutral domain is empty, so permission
                // stays NotGranted and no block can address an amplifier.
                runtime = new TrackApplicationRuntimeHost(
                    ecosHost,
                    controlTrace: null,
                    transportFactory: () =>
                    {
                        simulator = new DeterministicTrackTransport(
                            new TrackSimulatorConfig(detectedSlaves: DetectedSlaves));
                        return simulator;
                    },
                    amplifierGroups: TrackAmplifierGroups.Empty);

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);

                // A normal locomotive-speed request is refused as a safety interlock and produces
                // no 108 write, proving the default construction cannot address an amplifier.
                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));
                Assert.Equal("<END 8 (SAFETY_INTERLOCK)>", endLine);

                await Task.Delay(300, timeout.Token);
                Assert.DoesNotContain((byte)TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, simulator!.SentCommands);
                Assert.Equal(0, simulator.GetRegisters(1)[0] & 0x03FF);
            }
            finally
            {
                if (runtime is not null)
                {
                    try { await runtime.DisposeAsync(); } catch { /* best-effort */ }
                }

                if (externalServer is not null)
                {
                    try { await externalServer.DisposeAsync(); } catch { /* best-effort */ }
                }

                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
                try { File.Delete(locoPath); } catch { /* best-effort */ }
            }
        }

        [Fact]
        public async Task RealMode_NonEmptyDomain_ButNoRealProjection_RoutesNoLogicalAmplifier()
        {
            // Isolates the ROUTING default from the DOMAIN default. The runtime resolves a NON-EMPTY
            // observed-neutral domain (the profile's physical binding {1,3,4,6}) so movement
            // permission IS granted, but the directly-constructed host has NO real projection (no
            // SetProfile, no realTopology/realBlockMap), so the empty real routing maps no block to
            // an amplifier. A pre-fix constructor (real = logical) would route block 1 -> logical
            // amp 1 and emit a 108 write; this test must fail against that pre-fix default.
            var profile = BuildPhysicalMappingProfile();

            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-nonempty-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-nonempty-locos-{Guid.NewGuid():N}.json");
            var ecosPort = GetFreeTcpPort();
            var externalPort = GetFreeTcpPort();

            FakeKoploperExternalInfoServer? externalServer = null;
            DeterministicTrackTransport? simulator = null;
            TrackApplicationRuntimeHost? runtime = null;

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                externalServer = new FakeKoploperExternalInfoServer(externalPort, locoAddress: 1000, blockNumber: 1);

                // NO SetProfile and NO explicit realTopology/realBlockMap: the constructor default
                // keeps the real projection EMPTY (fail-closed routing), even though the runtime's
                // observed-neutral domain (below) is non-empty.
                var ecosHost = new TrackControlHost(
                    locoRepositoryPath: locoPath,
                    topology: Topology,
                    blockMap: BlockMap,
                    ecosListenPort: ecosPort,
                    koploperExternalInfoHost: "127.0.0.1",
                    koploperExternalInfoPort: externalPort,
                    trackAmplifierGroups: TrackAmplifierGroups.Empty);

                // fullSimulationProfile drives only the observed-neutral DOMAIN (physical {1,3,4,6});
                // it does not touch the host's real routing. The transport detects {1,3,4,6} so
                // neutral IS observed and movement permission IS granted.
                runtime = new TrackApplicationRuntimeHost(
                    ecosHost,
                    controlTrace: null,
                    transportFactory: () =>
                    {
                        simulator = new DeterministicTrackTransport(
                            new TrackSimulatorConfig(detectedSlaves: new byte[] { 1, 3, 4, 6 }));
                        return simulator;
                    },
                    amplifierGroups: TrackAmplifierGroups.Empty,
                    fullSimulationProfile: profile);

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

                Assert.Equal(TrackRuntimeState.Running, runtime.State);

                // The DOMAIN is non-empty (physical {1,3,4,6}) and neutral was observed, so movement
                // permission is granted. This proves the domain default is independent of the empty
                // routing default.
                Assert.Equal(new[] { 1, 3, 4, 6 }, runtime.PhysicalAmplifierBinding);
                Assert.Equal(MovementPermissionState.Granted, runtime.MovementPermission);
                Assert.True(runtime.IsMovementSafe);

                // The neutral-establishment 108 writes already happened during StartAsync; snapshot
                // the transport so we can prove the movement command adds no new write.
                var commandsBefore = simulator!.SentCommands.Count;

                var endLine = await SendEcosCommandAsync(ecosPort, "set(1000,speedstep[10])", TimeSpan.FromSeconds(10));

                // No safety interlock: the domain granted movement permission. But the empty real
                // routing maps no block to an amplifier, so the command has no physical target.
                Assert.Equal("<END 0 (OK)>", endLine);

                // Bounded negative assertion: the movement command produced no new outbound write.
                // A pre-fix constructor (real = logical) would route block 1 -> logical amp 1 and
                // emit an additional 108 write here.
                await Task.Delay(300, timeout.Token);
                Assert.Equal(commandsBefore, simulator.SentCommands.Count);
            }
            finally
            {
                if (runtime is not null)
                {
                    try { await runtime.DisposeAsync(); } catch { /* best-effort */ }
                }

                if (externalServer is not null)
                {
                    try { await externalServer.DisposeAsync(); } catch { /* best-effort */ }
                }

                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
                try { File.Delete(locoPath); } catch { /* best-effort */ }
            }
        }

        [Fact]
        public async Task RealMode_PhysicalDomain_Amp6ReadsNonNeutral_NotGranted()
        {
            // Physical domain {1,3,4,6} with amplifier 6 reading back non-neutral (500): observed
            // neutral can never be established, so movement permission stays NotGranted.
            var config = new TrackSimulatorConfig(
                detectedSlaves: new byte[] { 1, 3, 4, 6 },
                hr0ReadbackOverrides: new Dictionary<byte, ushort> { { 6, 500 } });

            await RunPhysicalProfileRealAsync(config, (runtime, _, ct) =>
            {
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(new[] { 1, 3, 4, 6 }, runtime.PhysicalAmplifierBinding);

                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task RealMode_PhysicalDomain_LogicalAmp2CannotSubstituteForAmp6_NotGranted()
        {
            // Only {1,2,3,4} detected: amplifier 2 is present and neutral, but amplifier 6 is
            // absent. The physical domain {1,3,4,6} still requires amplifier 6, so amplifier 2's
            // neutral is irrelevant and movement permission stays NotGranted.
            var config = new TrackSimulatorConfig(detectedSlaves: new byte[] { 1, 2, 3, 4 });

            await RunPhysicalProfileRealAsync(config, (runtime, _, ct) =>
            {
                Assert.Equal(TrackRuntimeState.Running, runtime.State);
                Assert.Equal(new[] { 1, 3, 4, 6 }, runtime.PhysicalAmplifierBinding);

                Assert.Equal(MovementPermissionState.NotGranted, runtime.MovementPermission);
                Assert.False(runtime.IsMovementSafe);

                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Composes the REAL chain from <see cref="BuildPhysicalMappingProfile"/> (physical domain
        /// {1,3,4,6}) with the supplied simulator config, starts Real mode and runs the test body.
        /// The profile is bound both to the host (<see cref="IEcosHostService.SetProfile"/>) and to
        /// the runtime (fullSimulationProfile), so the physical projection and the observed-neutral
        /// domain are the profile's physical binding.
        /// </summary>
        private static async Task RunPhysicalProfileRealAsync(
            TrackSimulatorConfig simulatorConfig,
            Func<TrackApplicationRuntimeHost, DeterministicTrackTransport, CancellationToken, Task> testBody)
        {
            var profile = BuildPhysicalMappingProfile();

            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-physical-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-physical-locos-{Guid.NewGuid():N}.json");
            var ecosPort = GetFreeTcpPort();
            var externalPort = GetFreeTcpPort();

            FakeKoploperExternalInfoServer? externalServer = null;
            DeterministicTrackTransport? simulator = null;
            TrackApplicationRuntimeHost? runtime = null;

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                externalServer = new FakeKoploperExternalInfoServer(externalPort, locoAddress: 1000, blockNumber: 1);

                var ecosHost = new TrackControlHost(
                    locoRepositoryPath: locoPath,
                    topology: profile.ToBlockTopology(),
                    blockMap: profile.ToKoploperBlockMap(),
                    ecosListenPort: ecosPort,
                    koploperExternalInfoHost: "127.0.0.1",
                    koploperExternalInfoPort: externalPort,
                    trackAmplifierGroups: TrackAmplifierGroups.Empty);

                // Real mode must be profile-driven: bind the physical projection (1/3/4/6).
                ecosHost.SetProfile(profile);

                runtime = new TrackApplicationRuntimeHost(
                    ecosHost,
                    controlTrace: null,
                    transportFactory: () =>
                    {
                        simulator = new DeterministicTrackTransport(simulatorConfig);
                        return simulator;
                    },
                    amplifierGroups: TrackAmplifierGroups.Empty,
                    fullSimulationProfile: profile);

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

                await testBody(runtime, simulator!, timeout.Token);
            }
            finally
            {
                if (runtime is not null)
                {
                    try { await runtime.DisposeAsync(); } catch { /* best-effort */ }
                }

                if (externalServer is not null)
                {
                    try { await externalServer.DisposeAsync(); } catch { /* best-effort */ }
                }

                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
                try { File.Delete(locoPath); } catch { /* best-effort */ }
            }
        }

        /// <summary>
        /// A 4-section Simple Loop profile whose REAL physical binding is the prototype amplifiers
        /// 1/3/4/6 while the logical (simulated) amplifierSlave values remain 1..4.
        /// </summary>
        private static LayoutProfile BuildPhysicalMappingProfile()
            => new LayoutProfile
            {
                Name = "Simple Loop (Real physical)",
                Description = "4 sections; physical binding 1/3/4/6.",
                Sections = new[]
                {
                    new LayoutSection { Id = 1, AmplifierSlave = 1, Bezetmelders = new[] { "1.01" }, LengthMm = 1000.0 },
                    new LayoutSection { Id = 2, AmplifierSlave = 2, Bezetmelders = new[] { "1.02" }, LengthMm = 1000.0 },
                    new LayoutSection { Id = 3, AmplifierSlave = 3, Bezetmelders = new[] { "1.03" }, LengthMm = 1000.0 },
                    new LayoutSection { Id = 4, AmplifierSlave = 4, Bezetmelders = new[] { "1.04" }, LengthMm = 1000.0 }
                },
                Blocks = new[]
                {
                    new LayoutBlock { Id = 1, SectionIds = new[] { 1 } },
                    new LayoutBlock { Id = 2, SectionIds = new[] { 2 } },
                    new LayoutBlock { Id = 3, SectionIds = new[] { 3 } },
                    new LayoutBlock { Id = 4, SectionIds = new[] { 4 } }
                },
                PhysicalAmplifierMapping = new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                    new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 3 },
                    new LayoutPhysicalAmplifierBinding { SectionId = 3, PhysicalAmplifier = 4 },
                    new LayoutPhysicalAmplifierBinding { SectionId = 4, PhysicalAmplifier = 6 }
                }
            };

        // ---------------------------------------------------------------------------------
        // Harness
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Composes the REAL <see cref="TrackControlHost"/> with the deterministic simulator
        /// transport, starts the runtime in Real mode, and hands the started runtime + simulator to
        /// <paramref name="testBody"/>. A stub Koploper external-info server reports loco 1000 in
        /// block 1 so the real hardware backend can resolve the locomotive to block/amplifier 1.
        /// </summary>
        private static async Task RunAsync(
            TrackAmplifierGroups groups,
            Func<TrackApplicationRuntimeHost, DeterministicTrackTransport, int, CancellationToken, Task> testBody)
        {
            // The real-mode init pipeline reads the firmware hex in FlashFwTrackamplifiersStep even
            // when flashing is skipped, so the test must not depend on the (untracked) repo dist/
            // hex being present. Generate a minimal self-contained hex whose checksum equals the
            // simulator's FirmwareChecksum (0x251F).
            var originalFwPath = CoreSettings.Default.TrackAmplifierFwPath;
            var tempHexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-fw-{Guid.NewGuid():N}.hex");
            var locoPath = Path.Combine(Path.GetTempPath(), $"siebwalde-v3-locos-{Guid.NewGuid():N}.json");
            var ecosPort = GetFreeTcpPort();
            var externalPort = GetFreeTcpPort();

            FakeKoploperExternalInfoServer? externalServer = null;
            DeterministicTrackTransport? simulator = null;
            TrackApplicationRuntimeHost? runtime = null;

            try
            {
                WriteTestFirmwareHex(tempHexPath);
                CoreSettings.Default.TrackAmplifierFwPath = tempHexPath;

                externalServer = new FakeKoploperExternalInfoServer(externalPort, locoAddress: 1000, blockNumber: 1);

                var ecosHost = new TrackControlHost(
                    locoRepositoryPath: locoPath,
                    topology: Topology,
                    blockMap: BlockMap,
                    ecosListenPort: ecosPort,
                    koploperExternalInfoHost: "127.0.0.1",
                    koploperExternalInfoPort: externalPort,
                    trackAmplifierGroups: groups,
                    realTopology: Topology,
                    realBlockMap: BlockMap);

                runtime = new TrackApplicationRuntimeHost(
                    ecosHost,
                    controlTrace: null,
                    transportFactory: () =>
                    {
                        simulator = new DeterministicTrackTransport(
                            new TrackSimulatorConfig(detectedSlaves: DetectedSlaves));
                        return simulator;
                    },
                    amplifierGroups: groups);

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await runtime.StartAsync(TrackControlMode.Real, timeout.Token);

                await testBody(runtime, simulator!, ecosPort, timeout.Token);
            }
            finally
            {
                if (runtime is not null)
                {
                    try { await runtime.DisposeAsync(); } catch { /* best-effort */ }
                }

                if (externalServer is not null)
                {
                    try { await externalServer.DisposeAsync(); } catch { /* best-effort */ }
                }

                try { CoreSettings.Default.TrackAmplifierFwPath = originalFwPath; } catch { /* best-effort */ }
                try { File.Delete(tempHexPath); } catch { /* best-effort */ }
                try { File.Delete(locoPath); } catch { /* best-effort */ }
            }
        }

        /// <summary>Reserves and releases a port so the test does not collide with 15471/5700.</summary>
        private static int GetFreeTcpPort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        /// <summary>
        /// Connects to the ECoS listener, sends one ASCII command (which ends with ')'), and reads
        /// the reply's <c>&lt;END ...&gt;</c> line. The initial feedback <c>&lt;EVENT ...&gt;</c> the
        /// backend emits on first attach is skipped.
        /// </summary>
        private static async Task<string> SendEcosCommandAsync(int port, string command, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
            using var reader = new StreamReader(stream, Encoding.ASCII);

            await writer.WriteAsync(command);
            await writer.FlushAsync();

            var replyHeaderSeen = false;
            while (true)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync().WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"Timed out waiting for the reply to '{command}' after {timeout}.");
                }

                if (line is null)
                {
                    throw new IOException($"The ECoS connection closed before replying to '{command}'.");
                }

                if (!replyHeaderSeen && line.StartsWith("<REPLY ", StringComparison.Ordinal))
                {
                    replyHeaderSeen = true;
                    continue;
                }

                if (replyHeaderSeen && line.StartsWith("<END ", StringComparison.Ordinal))
                {
                    return line;
                }
            }
        }

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (condition())
                    return;

                await Task.Delay(25);
            }

            throw new TimeoutException($"Condition was not met within {timeout}.");
        }

        /// <summary>
        /// Writes a minimal Intel HEX that <see cref="TrackAmplifierBootloaderHelpers.Execute"/>
        /// parses to a file checksum of 0x251F (the simulator's <see cref="TrackSimulatorConfig.DefaultFirmwareChecksum"/>).
        /// </summary>
        private static void WriteTestFirmwareHex(string path)
        {
            const int dataRows = (0x8000 - 0x800) / 16; // 1920, matching TrackAmplifierBootloaderHelpers.Execute

            var sb = new StringBuilder();

            for (int row = 0; row < dataRows; row++)
            {
                string address = (row * 16).ToString("X4");

                // 32 hex chars = 16 data bytes. The checksum byte (CC) is not read by Execute().
                string data = row == dataRows - 1
                    ? "1F25" + new string('0', 28)
                    : new string('0', 32);

                sb.Append(':').Append("10").Append(address).Append("00").Append(data).Append("00").AppendLine();
            }

            // One config row: record length 0x0C (12 bytes -> 24 hex chars). Its content is only
            // used when flashing, which is skipped, so any 12 bytes are acceptable.
            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();

            File.WriteAllText(path, sb.ToString());
        }

        /// <summary>
        /// Minimal stub for Koploper's external-information server. The real
        /// <see cref="KoploperExternalInfoClient"/> connects OUT to this port and reads records,
        /// so this stub reports "loco <c>locoAddress</c> -> block <c>blockNumber</c>", which is how
        /// a locomotive obtains a known block in a software-only test.
        /// </summary>
        private sealed class FakeKoploperExternalInfoServer : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _acceptLoop;

            public FakeKoploperExternalInfoServer(int port, int locoAddress, int blockNumber)
            {
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start();
                _acceptLoop = AcceptLoopAsync(locoAddress, blockNumber, _cts.Token);
            }

            private async Task AcceptLoopAsync(int locoAddress, int blockNumber, CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await _listener.AcceptTcpClientAsync(ct);
                    }
                    catch
                    {
                        break;
                    }

                    _ = ServeAsync(client, locoAddress, blockNumber, ct);
                }
            }

            private static async Task ServeAsync(TcpClient client, int locoAddress, int blockNumber, CancellationToken ct)
            {
                using (client)
                {
                    var stream = client.GetStream();

                    // Koploper external-info record: 5 ASCII fields separated by 0x1B.
                    // fields[0] = "&<loco>", fields[1] = "<block>", fields[2..4] = times/description.
                    // Use an explicit (char)0x1B separator: a "\x1B" escape is greedy over following
                    // hex digits (the "00:00:00" time fields), so it must not be used here.
                    const char sep = (char)0x1B;
                    var record = Encoding.ASCII.GetBytes(
                        $"&{locoAddress}{sep}{blockNumber}{sep}00:00:00{sep}00:00:00{sep}test{sep}");

                    try
                    {
                        await stream.WriteAsync(record, 0, record.Length, ct);
                        await stream.FlushAsync(ct);

                        // Hold the connection open (the client sends nothing) so it does not enter
                        // a reconnect loop. Exit when the peer closes or we are cancelled.
                        var buffer = new byte[64];
                        while (!ct.IsCancellationRequested)
                        {
                            int n = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                            if (n <= 0)
                                break;
                        }
                    }
                    catch
                    {
                        // Peer closed (or cancelled during shutdown): nothing to do.
                    }
                }
            }

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();
                _listener.Stop();
                try { await _acceptLoop; } catch { /* best-effort */ }
                _cts.Dispose();
            }
        }
    }
}
