using System.Text;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Regression tests for count-independent bootloader recovery (PR #48 MAJOR):
    /// (1) recovery eligibility must NOT depend on the number of detected amplifiers, and
    /// (2) recovery must be fail-closed — no flash without an explicit, verified-artifact request.
    /// </summary>
    public class RecoveryCountIndependenceTests
    {
        // ---------------------------------------------------------------------
        // Count-independence (DetectSlavesStep)
        // ---------------------------------------------------------------------

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async Task DetectSlaves_NoRecoveryRequest_AlwaysProceedsToNormalPath_RegardlessOfCount(int detected)
        {
            var variables = VariablesWithDetected(detected);
            var comm = new RecordingCommClient();
            var step = new DetectSlavesStep(comm, variables); // no recovery request

            // state 0 -> send DETECT
            var s0 = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, s0.Kind);

            // state 1 -> evaluate detection, route (count must not matter)
            var s1 = await step.ExecuteAsync(
                new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE, 0),
                CancellationToken.None);

            Assert.Equal(InitStepResultKind.NextStep, s1.Kind);
            Assert.Equal("FlashFwTrackamplifiers", s1.NextStepName);
        }

        [Fact]
        public async Task DetectSlaves_WithRecoveryRequest_TargetMissing_RoutesToRecovery_EvenWhenOtherAmpsDetected()
        {
            // Target amplifier 1 is missing, but three OTHER amplifiers (3,4,6) are detected. The
            // recovery must still be attempted: eligibility is target-based, not count-based.
            var variables = new TrackApplicationVariables();
            variables.trackAmpItems[3].SlaveDetected = 1;
            variables.trackAmpItems[4].SlaveDetected = 1;
            variables.trackAmpItems[6].SlaveDetected = 1;

            var comm = new RecordingCommClient();
            var request = RecoveryRequest(1);
            var step = new DetectSlavesStep(comm, variables, recoveryRequest: request);

            var s0 = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, s0.Kind);

            var s1 = await step.ExecuteAsync(
                new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE, 0),
                CancellationToken.None);

            Assert.Equal(InitStepResultKind.NextStep, s1.Kind);
            Assert.Equal("RecoverSlaves", s1.NextStepName);
        }

        [Fact]
        public async Task DetectSlaves_WithRecoveryRequest_TargetDetected_ProceedsToNormalPath()
        {
            // Recovery target 1 is already detected -> recovery complete, proceed to normal path.
            var variables = new TrackApplicationVariables();
            variables.trackAmpItems[1].SlaveDetected = 1;

            var comm = new RecordingCommClient();
            var request = RecoveryRequest(1);
            var step = new DetectSlavesStep(comm, variables, recoveryRequest: request);

            var s0 = await step.ExecuteAsync(null, CancellationToken.None);
            Assert.Equal(InitStepResultKind.Continue, s0.Kind);

            var s1 = await step.ExecuteAsync(
                new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE, 0),
                CancellationToken.None);

            Assert.Equal(InitStepResultKind.NextStep, s1.Kind);
            Assert.Equal("FlashFwTrackamplifiers", s1.NextStepName);
        }

        [Fact]
        public async Task DetectSlaves_RecoveryRequest_TargetStillMissing_ExhaustsAttempts_Errors()
        {
            // Target 1 never appears. With attemptMax=1, the second detection evaluation must stop
            // recovery and return Error (bounded attempts, retained).
            var variables = new TrackApplicationVariables(); // nothing detected -> target 1 missing
            var comm = new RecordingCommClient();
            var request = RecoveryRequest(1);
            var step = new DetectSlavesStep(comm, variables, attemptMax: 1, recoveryRequest: request);

            // First cycle: send detect + evaluate -> routes to recovery.
            await step.ExecuteAsync(null, CancellationToken.None);
            var first = await step.ExecuteAsync(
                new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE, 0),
                CancellationToken.None);
            Assert.Equal(InitStepResultKind.NextStep, first.Kind);
            Assert.Equal("RecoverSlaves", first.NextStepName);

            // Second cycle: attempt bound exceeded -> error (no further recovery routing).
            await step.ExecuteAsync(null, CancellationToken.None);
            var second = await step.ExecuteAsync(
                new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE, 0),
                CancellationToken.None);
            Assert.Equal(InitStepResultKind.Error, second.Kind);
        }

        // ---------------------------------------------------------------------
        // Fail-closed recovery (RecoverSlavesStep)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task RecoverSlaves_NoRequest_IsFailClosed_NoFlash()
        {
            var hexPath = WritePic18HexFile(seed: 1);
            try
            {
                var artifact = FirmwareArtifactVerifier.Validate(hexPath, null).Artifact!;
                var comm = new RecordingCommClient();
                var step = NewRecoverStep(comm, artifact, recoveryRequest: null);

                var result = await step.ExecuteAsync(null, CancellationToken.None);

                Assert.Equal(InitStepResultKind.NextStep, result.Kind);
                Assert.Equal("FlashFwTrackamplifiers", result.NextStepName);
                Assert.Empty(comm.Sent); // no flash write occurred
            }
            finally
            {
                TryDelete(hexPath);
            }
        }

        [Fact]
        public async Task RecoverSlaves_WithMatchingVerifiedArtifact_ProceedsToFlash()
        {
            var hexPath = WritePic18HexFile(seed: 2);
            try
            {
                var artifact = FirmwareArtifactVerifier.Validate(hexPath, null).Artifact!;
                var comm = new RecordingCommClient();
                var request = new FirmwareRecoveryRequest(1, artifact, FirmwareOperationType.EmergencyRecovery);
                var step = NewRecoverStep(comm, artifact, request);

                var result = await step.ExecuteAsync(null, CancellationToken.None);

                Assert.Equal(InitStepResultKind.Continue, result.Kind);
                Assert.Equal(new[] { (byte)TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH }, comm.Sent.ToArray());
            }
            finally
            {
                TryDelete(hexPath);
            }
        }

        [Fact]
        public async Task RecoverSlaves_WithUnmatchedFlashDataChecksum_RefusesToFlash()
        {
            // Build a step whose bootloader helper has NOT parsed a matching image (checksum 0),
            // but whose recovery request carries a verified artifact with a non-zero checksum.
            // The fail-closed guard must refuse to flash (checksum mismatch / zero).
            var hexPath = WritePic18HexFile(seed: 3);
            try
            {
                var artifact = FirmwareArtifactVerifier.Validate(hexPath, null).Artifact!;
                var comm = new RecordingCommClient();
                var request = new FirmwareRecoveryRequest(1, artifact);

                // A fresh, un-executed bootloader helper reports checksum 0 != artifact.EmbeddedChecksum.
                var unexecutedHelpers = new TrackAmplifierBootloaderHelpers(hexPath, "test");
                var sendNext = new SendNextFwDataPacket(comm, unexecutedHelpers);
                var step = new RecoverSlavesStep(comm, new TrackApplicationVariables(), sendNext, unexecutedHelpers,
                    recoveryRequest: request);

                var result = await step.ExecuteAsync(null, CancellationToken.None);

                Assert.Equal(InitStepResultKind.Error, result.Kind);
                Assert.Empty(comm.Sent); // no flash write occurred
            }
            finally
            {
                TryDelete(hexPath);
            }
        }

        // ---------------------------------------------------------------------
        // FirmwareRecoveryRequest validation
        // ---------------------------------------------------------------------

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        public void RecoveryRequest_InvalidTarget_Throws(int address)
        {
            var artifact = FirmwareArtifactVerifier.Validate(WritePic18HexFile(seed: 4), null).Artifact!;
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FirmwareRecoveryRequest((byte)address, artifact));
        }

        [Fact]
        public void RecoveryRequest_NullArtifact_Throws()
            => Assert.Throws<ArgumentNullException>(() => new FirmwareRecoveryRequest(1, null!));

        [Fact]
        public void RecoveryRequest_DefaultsToEmergencyRecovery()
        {
            var artifact = FirmwareArtifactVerifier.Validate(WritePic18HexFile(seed: 5), null).Artifact!;
            var request = new FirmwareRecoveryRequest(1, artifact);
            Assert.Equal(FirmwareOperationType.EmergencyRecovery, request.OperationType);
            Assert.Equal(1, request.TargetAmplifierAddress);
            Assert.Same(artifact, request.Artifact);
        }

        // ---------------------------------------------------------------------
        // Infrastructure
        // ---------------------------------------------------------------------

        private static TrackApplicationVariables VariablesWithDetected(int detected)
        {
            var variables = new TrackApplicationVariables();
            for (int i = 1; i <= detected; i++)
            {
                variables.trackAmpItems[i].SlaveDetected = 1;
            }
            return variables;
        }

        private static FirmwareRecoveryRequest RecoveryRequest(byte target)
        {
            var hexPath = WritePic18HexFile(seed: 6);
            var artifact = FirmwareArtifactVerifier.Validate(hexPath, null).Artifact!;
            return new FirmwareRecoveryRequest(target, artifact, FirmwareOperationType.EmergencyRecovery);
        }

        private static RecoverSlavesStep NewRecoverStep(
            RecordingCommClient comm, FirmwareArtifact artifact, FirmwareRecoveryRequest? recoveryRequest)
        {
            var sendNext = new SendNextFwDataPacket(comm, artifact.Helpers);
            return new RecoverSlavesStep(comm, new TrackApplicationVariables(), sendNext, artifact.Helpers,
                recoveryRequest: recoveryRequest);
        }

        private sealed class RecordingCommClient : ITrackCommClient
        {
            public readonly List<byte> Sent = new();

            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message.Command);
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => default;
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { /* leaked reader in TrackAmplifierBootloaderHelpers */ }
        }

        /// <summary>
        /// Writes a structurally valid PIC18 application Intel HEX (1920 data rows covering
        /// 0x800..0x7FFF, a 0x0C config-word record and EOF). The embedded checksum word at
        /// 0x7FFE/0x7FFF is the little-endian word sum over 0x800..0x7FFD, matching
        /// TrackAmplifierBootloaderHelpers and the PIC18 linker contract.
        /// </summary>
        private static string WritePic18HexFile(int seed)
        {
            const int bootOffset = 0x800;
            const int appEnd = 0x7FFF;

            var img = new byte[0x8000];
            new Random(seed).NextBytes(img);
            for (int a = 0; a < bootOffset; a++) img[a] = 0;

            uint sum = 0;
            for (int a = bootOffset; a < 0x7FFE; a += 2)
            {
                sum = (sum + img[a] + ((uint)img[a + 1] << 8)) & 0xFFFF;
            }
            img[0x7FFE] = (byte)(sum & 0xFF);
            img[0x7FFF] = (byte)((sum >> 8) & 0xFF);

            var sb = new StringBuilder();
            for (int addr = bootOffset; addr <= appEnd; addr += 16)
            {
                sb.Append(":10").Append(addr.ToString("X4")).Append("00");
                int recordSum = 0x10 + ((addr >> 8) & 0xFF) + (addr & 0xFF);
                for (int i = 0; i < 16; i++)
                {
                    byte b = img[addr + i];
                    sb.Append(b.ToString("X2"));
                    recordSum += b;
                }
                sb.Append(((byte)(-recordSum & 0xFF)).ToString("X2")).AppendLine();
            }

            byte[] config = new byte[12];
            new Random(seed + 1000).NextBytes(config);
            sb.Append(":0C00000000");
            int configSum = 0x0C;
            foreach (var b in config)
            {
                sb.Append(b.ToString("X2"));
                configSum += b;
            }
            sb.Append(((byte)(-configSum & 0xFF)).ToString("X2")).AppendLine();

            sb.Append(":00000001FF").AppendLine();

            var path = Path.Combine(Path.GetTempPath(), $"recover_{Guid.NewGuid():N}.hex");
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}
