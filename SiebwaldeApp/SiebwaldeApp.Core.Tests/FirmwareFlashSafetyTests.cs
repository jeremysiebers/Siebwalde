using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Comm;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Offline regression tests for the unauthorized automatic-firmware-flash defect. A firmware
    /// checksum mismatch must DETECT and REPORT, never auto-flash (no 103/120/121/122/ERASE/PROGRAM).
    /// The destructive path is reachable only through an explicit, session-scoped authorization that
    /// reuses the proven per-amplifier command-131 service. No network or hardware is involved.
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class FirmwareFlashSafetyTests
    {
        // -----------------------------------------------------------------
        // (1) installed firmware differs from configured -> NO FLASH, Blocked
        // -----------------------------------------------------------------

        [Fact]
        public async Task FlashStep_InstalledDiffers_Blocks_NoFlashCommandEmitted()
        {
            var hexPath = WritePic18HexFile(seed: 1);
            try
            {
                var validation = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
                Assert.True(validation.Success, validation.Error);
                var fileChecksum = validation.Artifact!.RecomputedChecksum;

                var variables = new TrackApplicationVariables();
                foreach (var amp in new byte[] { 1, 3, 4, 6 })
                {
                    variables.trackAmpItems[amp].SlaveDetected = 1;
                    variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.SwChecksum] =
                        (ushort)(fileChecksum ^ 0x1234); // installed differs from configured
                }

                var comm = new RecordingCommClient();
                var auth = new FirmwareDeploymentAuthorization();
                var sendNext = new SendNextFwDataPacket(comm, validation.Artifact.Helpers);
                var step = new FlashFwTrackamplifiersStep(comm, variables, sendNext, validation.Artifact.Helpers, "test", auth);

                var result = await step.ExecuteAsync(null, CancellationToken.None);

                Assert.Equal(InitStepResultKind.Blocked, result.Kind);
                Assert.Contains("checksum", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("0x" + fileChecksum.ToString("X4"), result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

                // Nothing was sent: no 103/120/121/122/ERASE/PROGRAM/131.
                Assert.Empty(comm.Sent);

                // The mismatch was recorded for the operator; no implicit authorization.
                Assert.Equal(4, auth.Mismatches.Count);
                Assert.False(auth.Authorized);
                Assert.Equal(fileChecksum, auth.ExpectedChecksum);
            }
            finally { TryDelete(hexPath); }
        }

        // -----------------------------------------------------------------
        // (2) automatic init mismatch -> FirmwareAddress BLOCKED, not Failed
        // -----------------------------------------------------------------

        [Fact]
        public async Task StateMachine_AutoInitMismatch_FirmwareAddressBlocked_NotFailed()
        {
            var auth = new FirmwareDeploymentAuthorization();
            var (machine, _) = BuildMachine(auth,
                new FakeStep("FlashFwTrackamplifiers", () => InitStepResult.Blocked(
                    "Firmware checksum mismatch on amplifier(s) 1,3,4,6; expected checksum 0x0E39.")));

            await machine.RunAutomaticAsync(CancellationToken.None);

            var firmwareAddress = machine.Stages.First(s => s.Stage == TrackInitStage.FirmwareAddress);
            Assert.Equal(TrackInitStageState.Blocked, firmwareAddress.State);
            Assert.NotEqual(TrackInitStageState.Failed, firmwareAddress.State);
            Assert.Contains("checksum mismatch", firmwareAddress.FailureReason, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("authorize firmware deployment", firmwareAddress.RecoveryAction);

            Assert.False(machine.IsFailed, $"RunStatus={machine.RunStatus}");
            Assert.False(auth.Authorized);
        }

        // -----------------------------------------------------------------
        // (3) manual Step / Retry / Backtrack cannot silently flash
        // -----------------------------------------------------------------

        [Fact]
        public async Task StateMachine_StepRetryBacktrack_DoesNotAuthorize()
        {
            var auth = new FirmwareDeploymentAuthorization();
            var comm = new RecordingCommClient();
            var (machine, _) = BuildMachine(auth,
                new FakeStep("FlashFwTrackamplifiers", () => InitStepResult.Blocked("mismatch")));

            var first = await machine.StepAsync(CancellationToken.None);          // stage 1 passes
            Assert.Equal(TrackInitStageState.Passed, first);

            var blocked = await machine.StepAsync(CancellationToken.None);        // FirmwareAddress blocks
            Assert.Equal(TrackInitStageState.Blocked, blocked);
            Assert.False(auth.Authorized);

            var retried = await machine.RetryAsync(CancellationToken.None);       // re-run: still blocked
            Assert.Equal(TrackInitStageState.Blocked, retried);
            Assert.False(auth.Authorized);

            machine.Backtrack();                                                  // backtrack
            Assert.False(auth.Authorized);

            Assert.Empty(comm.Sent);
        }

        // -----------------------------------------------------------------
        // (4) missing / incorrect firmware artifact -> fail closed (no send)
        // -----------------------------------------------------------------

        [Fact]
        public async Task FlashStep_MissingArtifact_FailsClosed_NoSend()
        {
            var missing = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.hex");
            var helpers = new TrackAmplifierBootloaderHelpers(missing, "test");
            helpers.Execute(); // file absent -> HexFileReadSuccessful == false
            Assert.False(helpers.HexFileReadSuccessful);

            var variables = new TrackApplicationVariables();
            variables.trackAmpItems[1].SlaveDetected = 1;
            variables.trackAmpItems[1].HoldingReg[TrackAmplifierRegisters.SwChecksum] = 0x0E39;

            var comm = new RecordingCommClient();
            var auth = new FirmwareDeploymentAuthorization();
            var sendNext = new SendNextFwDataPacket(comm, helpers);
            var step = new FlashFwTrackamplifiersStep(comm, variables, sendNext, helpers, "test", auth);

            var result = await step.ExecuteAsync(null, CancellationToken.None);

            Assert.Equal(InitStepResultKind.Error, result.Kind);
            Assert.Empty(comm.Sent); // fail closed, no send
        }

        // -----------------------------------------------------------------
        // (5) wrong SHA-256 -> no destructive action (authorized path)
        // -----------------------------------------------------------------

        [Fact]
        public async Task AuthorizedDeployment_WrongSha256_NoDestructiveAction()
        {
            var hexPath = WritePic18HexFile(seed: 5);
            try
            {
                var transport = new ScriptedTransport { Responder = BuildResponder() };
                var runner = new FlashOneAmplifierRunner(
                    transportFactory: () => transport,
                    preflight: new PermissivePreflight());

                var result = await runner.RunAsync(
                    hexPath, expectedSha256Hex: new string('0', 64), amplifier: 6);

                Assert.False(result.Success);
                Assert.Contains("SHA-256 mismatch", result.Error);
                Assert.Empty(transport.SentFrames); // validation fails before any send
                Assert.False(transport.Opened);
            }
            finally { TryDelete(hexPath); }
        }

        // -----------------------------------------------------------------
        // (6) explicitly authorized deployment uses command-131, never 122
        // -----------------------------------------------------------------

        [Fact]
        public async Task AuthorizeFirmwareDeployment_UsesCommand131_Never122()
        {
            var hexPath = WritePic18HexFile(seed: 6);
            try
            {
                var sha256 = FirmwareArtifactVerifier.ComputeSha256Hex(hexPath);
                var transport = new ScriptedTransport { Responder = BuildResponder() };

                var host = new TrackApplicationRuntimeHost(
                    new FakeEcosHostService(),
                    flashRunnerFactory: () => new FlashOneAmplifierRunner(
                        transportFactory: () => transport,
                        preflight: new PermissivePreflight()));

                var results = await host.AuthorizeFirmwareDeploymentAsync(
                    new byte[] { 6 }, sha256, hexPath);

                Assert.Single(results);
                Assert.True(results[0].Success, results[0].Error);

                var commands = transport.SentFrames.Select(f => f[1]).ToArray();
                Assert.Contains((byte)TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE, commands);
                Assert.DoesNotContain((byte)TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES, commands);

                // The single-target frame carries exactly the selected amplifier id.
                var flashFrame = Assert.Single(transport.SentFrames.Where(
                    f => f[1] == TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE));
                Assert.Equal(6, flashFrame[2]);

                // One-shot: the grant is consumed regardless of outcome.
                Assert.False(host.FirmwareDeploymentAuthorized);
            }
            finally { TryDelete(hexPath); }
        }

        // -----------------------------------------------------------------
        // (7) restart / session recovery cannot reuse an expired authorization
        // -----------------------------------------------------------------

        [Fact]
        public void Authorization_NewInstance_StartsUnauthorized()
        {
            var first = new FirmwareDeploymentAuthorization();
            first.RecordMismatch(0x0E39, "935A04E5", new[] { new FirmwareAmpMismatch(1, 0x5784) });
            first.Authorize();
            Assert.True(first.Authorized);

            // A fresh session (new instance) starts unauthorized and never retains the prior grant.
            var second = new FirmwareDeploymentAuthorization();
            Assert.False(second.Authorized);
            Assert.Empty(second.Mismatches);
            Assert.Empty(second.TargetAmps);
            Assert.Equal(0, second.ExpectedChecksum);
        }

        // -----------------------------------------------------------------
        // (8) correct firmware on all amps -> normal init continues
        // -----------------------------------------------------------------

        [Fact]
        public async Task FlashStep_CorrectFirmware_ContinuesToInitTrackamplifiers()
        {
            var hexPath = WritePic18HexFile(seed: 8);
            try
            {
                var validation = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
                Assert.True(validation.Success, validation.Error);
                var fileChecksum = validation.Artifact!.RecomputedChecksum;

                var variables = new TrackApplicationVariables();
                foreach (var amp in new byte[] { 1, 3, 4, 6 })
                {
                    variables.trackAmpItems[amp].SlaveDetected = 1;
                    variables.trackAmpItems[amp].HoldingReg[TrackAmplifierRegisters.SwChecksum] = fileChecksum;
                }

                var comm = new RecordingCommClient();
                var auth = new FirmwareDeploymentAuthorization();
                var sendNext = new SendNextFwDataPacket(comm, validation.Artifact.Helpers);
                var step = new FlashFwTrackamplifiersStep(comm, variables, sendNext, validation.Artifact.Helpers, "test", auth);

                var result = await step.ExecuteAsync(null, CancellationToken.None);

                Assert.Equal(InitStepResultKind.NextStep, result.Kind);
                Assert.Equal("InitTrackamplifiers", result.NextStepName);
                Assert.Empty(comm.Sent);
                Assert.False(auth.Authorized);
            }
            finally { TryDelete(hexPath); }
        }

        // -----------------------------------------------------------------
        // (9) legacy configured artifact -> diagnosis-first Blocked, no flash
        // -----------------------------------------------------------------

        [Fact]
        public async Task FlashStep_LegacyConfiguredArtifact_Blocks_NoFlash()
        {
            var hexPath = WriteHexWithChecksum(0x5784);
            try
            {
                var helpers = new TrackAmplifierBootloaderHelpers(hexPath, "test");
                helpers.Execute();
                Assert.True(helpers.HexFileReadSuccessful);
                Assert.Equal((ushort)0x5784, helpers.GetFileCheckSum);

                var variables = new TrackApplicationVariables();
                // The amplifier matches the configured (legacy) artifact checksum, so the legacy
                // pre-diagnosis is the ONLY thing preventing a silent match + proceed.
                variables.trackAmpItems[1].SlaveDetected = 1;
                variables.trackAmpItems[1].HoldingReg[TrackAmplifierRegisters.SwChecksum] = 0x5784;

                var comm = new RecordingCommClient();
                var auth = new FirmwareDeploymentAuthorization();
                var sendNext = new SendNextFwDataPacket(comm, helpers);
                var step = new FlashFwTrackamplifiersStep(comm, variables, sendNext, helpers, "test", auth);

                var result = await step.ExecuteAsync(null, CancellationToken.None);

                Assert.Equal(InitStepResultKind.Blocked, result.Kind);
                Assert.Contains("legacy 0x5784", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("0x0E39", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

                // No flash command was sent and no implicit authorization.
                Assert.Empty(comm.Sent);
                Assert.False(auth.Authorized);
            }
            finally { TryDelete(hexPath); }
        }

        // -----------------------------------------------------------------
        // (10) stage dependency: no false downstream PASS / Completed
        // -----------------------------------------------------------------

        [Fact]
        public async Task StateMachine_NormalOperationNotPassed_WhileStages4And5NotPassed()
        {
            var (machine, _) = BuildMachine(new FirmwareDeploymentAuthorization());

            await machine.RunAutomaticAsync(CancellationToken.None);

            // NormalOperation must NOT be Passed and the run must NOT be Completed while
            // ConfigureValidate (4) and GlobalReady (5) are not yet Passed.
            Assert.False(machine.IsCompleted, $"RunStatus={machine.RunStatus}");
            Assert.NotEqual(TrackInitStageState.Passed, machine.Stages.First(s => s.Stage == TrackInitStage.NormalOperation).State);
            Assert.NotEqual(TrackInitStageState.Passed, machine.Stages.First(s => s.Stage == TrackInitStage.ConfigureValidate).State);

            // Once the host finalizes stages 4 and 5, Stage 6 (track power) is still required, so
            // the run stops at GlobalReady (test scope complete) rather than completing.
            machine.MarkStagePassed(TrackInitStage.ConfigureValidate);
            Assert.False(machine.IsCompleted); // still missing stages 5 and 6
            machine.MarkStagePassed(TrackInitStage.GlobalReady);

            Assert.False(machine.IsCompleted);
            Assert.Equal(InitializationStatus.StoppedAtGlobalReady, machine.RunStatus);
            Assert.NotEqual(TrackInitStageState.Passed, machine.Stages.First(s => s.Stage == TrackInitStage.NormalOperation).State);
            Assert.NotEqual(TrackInitStageState.Passed, machine.Stages.First(s => s.Stage == TrackInitStage.AmpPowerEnable).State);

            // Stage 6 passed completes the run in order.
            machine.MarkStagePassed(TrackInitStage.AmpPowerEnable);

            Assert.True(machine.IsCompleted);
            Assert.Equal(TrackInitStageState.Passed, machine.Stages.First(s => s.Stage == TrackInitStage.NormalOperation).State);
        }

        // -----------------------------------------------------------------
        // (11) the flash path requires BOTH authorization AND session lease
        // -----------------------------------------------------------------

        [Fact]
        public async Task FlashPath_RequiresSessionLease_AndFailsWhenOwned()
        {
            var hexPath = WritePic18HexFile(seed: 10);
            try
            {
                var sha256 = FirmwareArtifactVerifier.ComputeSha256Hex(hexPath);
                var transport = new ScriptedTransport { Responder = BuildResponder() };

                // Hold the WPF session lease: the deployment runner must fail closed and never send.
                using (TrackControllerSessionLease.TryAcquire(
                    TrackControllerSessionLease.WpfOwnerName, out _))
                {
                    var runner = new FlashOneAmplifierRunner(
                        transportFactory: () => transport,
                        preflight: new PermissivePreflight());

                    var result = await runner.RunAsync(hexPath, sha256, amplifier: 6);

                    Assert.False(result.Success);
                    Assert.Contains("owned by", result.Error, StringComparison.OrdinalIgnoreCase);
                    Assert.Empty(transport.SentFrames);
                }
            }
            finally { TryDelete(hexPath); }
        }

        [Fact]
        public async Task Host_AuthorizeFirmwareDeployment_RequiresExplicitAuthorization()
        {
            // Without an explicit authorization call, the host reports unauthorized and, if the
            // deployment method is invoked without a SHA, refuses (no runner, no send).
            var host = new TrackApplicationRuntimeHost(new FakeEcosHostService());
            Assert.False(host.FirmwareDeploymentAuthorized);

            var results = await host.AuthorizeFirmwareDeploymentAsync(
                new byte[] { 6 }, expectedSha256Hex: "");

            Assert.Single(results);
            Assert.False(results[0].Success);
            Assert.False(host.FirmwareDeploymentAuthorized);
        }

        // -----------------------------------------------------------------
        // RecoverSlavesStep: bootloader-detected -> fail closed (no flash)
        // -----------------------------------------------------------------

        [Fact]
        public async Task RecoverSlavesStep_BootloaderDetected_Blocks_NoFlash()
        {
            var hexPath = WritePic18HexFile(seed: 11);
            try
            {
                var validation = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
                var comm = new RecordingCommClient();
                var auth = new FirmwareDeploymentAuthorization();
                var sendNext = new SendNextFwDataPacket(comm, validation.Artifact!.Helpers);
                var step = new RecoverSlavesStep(comm, new TrackApplicationVariables(), sendNext, validation.Artifact.Helpers, "test",
                    firmwareDeployment: auth);

                // Drive state 0 (send 103) -> state 1 waits for CONNECTED -> state 2 sees a bootloader.
                _ = await step.ExecuteAsync(null, CancellationToken.None);
                var connected = new ReceivedMessage(TaskId.FWHANDLER, TrackCommand.FWHANDLERINIT, TaskStates.CONNECTED, TaskMessages.NONE);
                _ = await step.ExecuteAsync(connected, CancellationToken.None);
                var versionOk = new ReceivedMessage(
                    TrackCommand.GET_BOOTLOADER_VERSION,
                    TrackCommand.GET_BOOTLOADER_VERSION_OK,
                    TaskStates.DONE,
                    TaskMessages.NONE);
                var result = await step.ExecuteAsync(versionOk, CancellationToken.None);

                Assert.Equal(InitStepResultKind.Blocked, result.Kind);
                Assert.False(auth.Authorized);

                // No destructive recovery command (125/126/127/128/129/131) was emitted.
                var destructive = new[]
                {
                    (byte)TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES,
                    (byte)TrackCommand.EXEC_FW_STATE_ERASE_FLASH,
                    (byte)TrackCommand.EXEC_FW_STATE_WRITE_FLASH,
                    (byte)TrackCommand.EXEC_FW_STATE_WRITE_CONFIG,
                    (byte)TrackCommand.EXEC_FW_STATE_CHECK_CHECKSUM,
                    (byte)TrackCommand.EXEC_FW_STATE_SLAVE_RESET,
                    (byte)TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE,
                };
                Assert.All(comm.Sent, m => Assert.DoesNotContain(m.Command, destructive));
            }
            finally { TryDelete(hexPath); }
        }

        // -----------------------------------------------------------------
        // Infrastructure
        // -----------------------------------------------------------------

        private sealed class FakeStep : IInitializationStep
        {
            private readonly Func<InitStepResult> _result;
            public FakeStep(string name, Func<InitStepResult> result) { Name = name; _result = result; }
            public string Name { get; }
            public Task<InitStepResult> ExecuteAsync(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
                => Task.FromResult(_result());
        }

        /// <summary>Production-shaped chain reaching NormalOperation via a completed EnableTrackamplifiers.</summary>
        private static IInitializationStep[] HappyChain() => new IInitializationStep[]
        {
            new FakeStep("ConnectToEthernetTarget", () => InitStepResult.Next("QueryControllerVersion")),
            new FakeStep("QueryControllerVersion", () => InitStepResult.Next("GlobalStopPrecondition")),
            new FakeStep("GlobalStopPrecondition", () => InitStepResult.Next("ResetAllSlaves")),
            new FakeStep("ResetAllSlaves", () => InitStepResult.Next("DataUpload")),
            new FakeStep("DataUpload", () => InitStepResult.Next("DetectSlaves")),
            new FakeStep("DetectSlaves", () => InitStepResult.Next("FlashFwTrackamplifiers")),
            new FakeStep("FlashFwTrackamplifiers", () => InitStepResult.Next("InitTrackamplifiers")),
            new FakeStep("InitTrackamplifiers", () => InitStepResult.Next("SetDefaultPwmSetpoints")),
            new FakeStep("SetDefaultPwmSetpoints", () => InitStepResult.Next("EnableTrackamplifiers")),
            new FakeStep("EnableTrackamplifiers", () => InitStepResult.Completed()),
        };

        private static (TrackInitStateMachine, RecordingCommClient) BuildMachine(
            FirmwareDeploymentAuthorization auth, params IInitializationStep[] overrides)
        {
            var comm = new RecordingCommClient();
            var variables = new TrackApplicationVariables();
            var steps = HappyChain().ToList();

            foreach (var ov in overrides)
            {
                var idx = steps.FindIndex(s => s.Name == ov.Name);
                if (idx >= 0) steps[idx] = ov;
                else steps.Add(ov);
            }

            var machine = new TrackInitStateMachine(
                comm, variables, steps,
                requiredAmps: () => new byte[] { 1, 3, 4, 6 },
                firmwareDeployment: auth);
            return (machine, comm);
        }

        private sealed class RecordingCommClient : ITrackCommClient
        {
            public List<SendMessage> Sent { get; } = new();
            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;
            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }
            public ValueTask DisposeAsync() => default;
        }

        private sealed class PermissivePreflight : IFlashOneOwnershipPreflight
        {
            public FlashOneOwnershipResult Check() => FlashOneOwnershipResult.Success();
        }

        private sealed class FakeEcosHostService : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted;
            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;
            public bool ResetSafety() => false;
            public void SetProfile(LayoutProfile profile) { }
            public IReadOnlyDictionary<int, SwitchPosition> GetSwitchPositions() => new Dictionary<int, SwitchPosition>();
            public Task<EcosHostStartResult> StartAsync(
                TrackControlMode mode, ITrackCommClient? commClient, TrackApplicationVariables? variables,
                CancellationToken cancellationToken = default, IMovementSimulation? movementSimulation = null)
            {
                Mode = mode;
                IsRunning = true;
                return Task.FromResult(EcosHostStartResult.Started);
            }
            public Task<EcosHostStopResult> StopAsync(CancellationToken ct = default)
            {
                IsRunning = false;
                Mode = null;
                return Task.FromResult(EcosHostStopResult.Stopped);
            }
            public void Stop() { IsRunning = false; Mode = null; }
        }

        private sealed class ScriptedTransport : ITrackTransport
        {
            private readonly Channel<byte[]> _frames = Channel.CreateUnbounded<byte[]>();
            public List<byte[]> SentFrames { get; } = new();
            public Func<byte[], IEnumerable<byte[]>>? Responder { get; set; }
            public bool Opened { get; private set; }

            public Task OpenAsync(CancellationToken cancellationToken = default) { Opened = true; return Task.CompletedTask; }
            public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            {
                var frame = buffer.AsSpan(offset, count).ToArray();
                SentFrames.Add(frame);
                if (Responder != null)
                    foreach (var reply in Responder(frame)) _frames.Writer.TryWrite(reply);
                return Task.CompletedTask;
            }
            public async IAsyncEnumerable<byte[]> ReceiveAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                while (await _frames.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    while (_frames.Reader.TryRead(out var frame)) yield return frame;
            }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private static Func<byte[], IEnumerable<byte[]>> BuildResponder()
        {
            var chunkCount = 0;
            return frame =>
            {
                byte command = frame.Length > 1 ? frame[1] : (byte)0;
                switch (command)
                {
                    case EnumClientCommands.CLIENT_CONNECTION_REQUEST:
                        return new[] { Ctrl(TaskId.CONTROLLER, TaskStates.CONNECTED, TaskStates.DONE, TaskMessages.NONE) };
                    case TrackCommand.GET_CONTROLLER_VERSION:
                        return new[] { new byte[] { Enums.HEADER, TaskId.CONTROLLER, TrackCommand.GET_CONTROLLER_VERSION, 1, 0, 0, 0x01, 0 } };
                    case TrackCommand.EXEC_MBUS_STATE_RESET:
                        return new[] { Ctrl(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_RESET, TaskStates.DONE, TaskMessages.NONE) };
                    case TrackCommand.EXEC_MBUS_STATE_SLAVES_ON:
                        return new[] { Ctrl(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVES_ON, TaskStates.DONE, TaskMessages.NONE) };
                    case TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH:
                        return new[] { Ctrl(TaskId.FWHANDLER, TrackCommand.FWHANDLERINIT, TaskStates.CONNECTED, TaskMessages.NONE) };
                    case TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE:
                        return new[] { Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                    case TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE:
                        chunkCount++;
                        if (chunkCount >= 480)
                            return new[]
                            {
                                Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE, TaskStates.DONE, TaskMessages.NONE),
                                Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM, TaskStates.DONE, TaskMessages.RECEIVED_CHECKSUM_OK),
                            };
                        return new[] { Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                    case TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD:
                        return new[] { Ctrl(TrackCommand.FWCONFIGWORDDOWNLOAD, TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                    case TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE:
                        return new[] { Ctrl(TrackCommand.FWCONFIGWORDDOWNLOAD, TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE, TaskStates.DONE, TaskMessages.NONE) };
                }

                if (command == TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE)
                    return new[] { Ctrl(TaskId.FWHANDLER, TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE, TaskStates.DONE, TaskMessages.NONE) };

                return Array.Empty<byte[]>();
            };
        }

        private static byte[] Ctrl(byte sender, byte command, byte state, byte message)
            => new byte[] { Enums.HEADER, sender, command, state, message };

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { /* leaked handle in TrackAmplifierBootloaderHelpers constructor */ }
        }

        /// <summary>Writes a valid PIC18 application Intel HEX file (mirrors the linker contract).</summary>
        private static string WritePic18HexFile(int seed, bool corruptEmbeddedChecksum = false)
        {
            const int bootOffset = 0x800;
            const int appEnd = 0x7FFF;

            var img = new byte[0x8000];
            new Random(seed).NextBytes(img);
            for (var a = 0; a < bootOffset; a++) img[a] = 0;

            uint sum = 0;
            for (var a = bootOffset; a < 0x7FFE; a += 2)
                sum = (sum + img[a] + ((uint)img[a + 1] << 8)) & 0xFFFF;
            img[0x7FFE] = (byte)(sum & 0xFF);
            img[0x7FFF] = (byte)((sum >> 8) & 0xFF);
            if (corruptEmbeddedChecksum) img[0x7FFE] ^= 0xFF;

            var sb = new StringBuilder();
            for (var addr = bootOffset; addr <= appEnd; addr += 16)
            {
                sb.Append(":10").Append(addr.ToString("X4")).Append("00");
                var recordSum = 0x10 + ((addr >> 8) & 0xFF) + (addr & 0xFF);
                for (var i = 0; i < 16; i++)
                {
                    byte b = img[addr + i];
                    sb.Append(b.ToString("X2"));
                    recordSum += b;
                }
                sb.Append(((byte)(-recordSum & 0xFF)).ToString("X2")).AppendLine();
            }

            var config = new byte[12];
            new Random(seed + 1000).NextBytes(config);
            sb.Append(":0C00000000");
            var configSum = 0x0C;
            foreach (var b in config) { sb.Append(b.ToString("X2")); configSum += b; }
            sb.Append(((byte)(-configSum & 0xFF)).ToString("X2")).AppendLine();

            sb.Append(":00000001FF").AppendLine();

            var path = Path.Combine(Path.GetTempPath(), $"flash_safety_{Guid.NewGuid():N}.hex");
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        /// <summary>
        /// Writes a minimal Intel HEX whose <see cref="TrackAmplifierBootloaderHelpers.Execute"/>
        /// computes <paramref name="checksum"/> (1920 zero data rows; the last row's first word
        /// carries the checksum little-endian), followed by one config row. Used to model a legacy
        /// configured artifact path.
        /// </summary>
        private static string WriteHexWithChecksum(ushort checksum)
        {
            const int dataRows = (0x8000 - 0x800) / 16; // 1920

            var sb = new StringBuilder();
            for (int row = 0; row < dataRows; row++)
            {
                string address = (row * 16).ToString("X4");
                string data = row == dataRows - 1
                    ? (checksum & 0xFF).ToString("X2") + ((checksum >> 8) & 0xFF).ToString("X2") + new string('0', 28)
                    : new string('0', 32);

                sb.Append(':').Append("10").Append(address).Append("00").Append(data).Append("00").AppendLine();
            }

            sb.Append(':').Append("0C").Append("0000").Append("00").Append(new string('F', 24)).Append("00").AppendLine();

            var path = Path.Combine(Path.GetTempPath(), $"flash_legacy_{Guid.NewGuid():N}.hex");
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}
