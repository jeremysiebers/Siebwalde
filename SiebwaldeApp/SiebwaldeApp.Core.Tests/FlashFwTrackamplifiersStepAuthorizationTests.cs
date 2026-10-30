using System.Security.Cryptography;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

public class FlashFwTrackamplifiersStepAuthorizationTests
{
    private const ushort LegacyChecksum = 0x251F;
    private const ushort TargetChecksum = 0xE906;

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

    private sealed class Fixture : IDisposable
    {
        public RecordingCommClient Comm { get; } = new();
        public DeploymentEventLog Log { get; } = new();
        public string HexPath { get; }
        public string Sha256Hex { get; }
        public TrackApplicationVariables Variables { get; }

        public Fixture(TrackApplicationVariables variables, ushort fileChecksum)
        {
            Variables = variables;
            HexPath = Path.Combine(Path.GetTempPath(), $"siebwalde-fwstep-{Guid.NewGuid():N}.hex");
            File.WriteAllText(
                HexPath,
                FirmwareHexTestFactory.Emit(FirmwareHexTestFactory.BuildImage(fileChecksum)));
            Sha256Hex = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(HexPath)));
        }

        public FlashFwTrackamplifiersStep CreateStep(FirmwareDeploymentSession? session)
        {
            // Fresh bootloader helper per step: Execute() appends to its own GetHexFileData.
            var bootloader = new TrackAmplifierBootloaderHelpers(HexPath, "Track");
            var sendNext = new SendNextFwDataPacket(Comm, bootloader);
            return new FlashFwTrackamplifiersStep(Comm, Variables, sendNext, bootloader, "Track", session, Log);
        }

        public FirmwareDeploymentSession Session(
            bool authorized,
            IEnumerable<DeploymentTarget> targets,
            DateTimeOffset expiresAt,
            string? sha256Override = null,
            ushort checksum = TargetChecksum)
            => new("s1", authorized, targets,
                new FirmwareArtifactIdentity(HexPath, sha256Override ?? Sha256Hex, checksum),
                expiresAt);

        public void Dispose()
        {
            try
            {
                File.Delete(HexPath);
            }
            catch
            {
                // best-effort temp cleanup
            }
        }
    }

    private static TrackApplicationVariables Mismatch(params ushort[] amplifiers)
    {
        var variables = new TrackApplicationVariables();
        foreach (var number in amplifiers)
        {
            var amp = variables.trackAmpItems.First(a => a.SlaveNumber == number);
            amp.SlaveDetected = 1;
            amp.HoldingReg[TrackAmplifierRegisters.SwChecksum] = LegacyChecksum;
        }

        return variables;
    }

    [Fact]
    public async Task NoSession_LegacyMismatch_DoesNotFlash()
    {
        using var fixture = new Fixture(Mismatch(1), TargetChecksum);
        var step = fixture.CreateStep(session: null);

        var result = await step.ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(InitStepResultKind.NextStep, result.Kind);
        Assert.Equal("InitTrackamplifiers", result.NextStepName);
        Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, fixture.Comm.Sent);
        Assert.Contains(fixture.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.NotAuthorized);
    }

    [Fact]
    public async Task ActiveSession_WrongTarget_NoFlash()
    {
        using var fixture = new Fixture(Mismatch(1), TargetChecksum);
        var session = fixture.Session(
            true, new[] { DeploymentTarget.ForAmplifier(2) }, DateTimeOffset.UtcNow.AddHours(1));
        var step = fixture.CreateStep(session);

        var result = await step.ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(InitStepResultKind.NextStep, result.Kind);
        Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, fixture.Comm.Sent);
        Assert.Contains(fixture.Log.DeploymentEvents,
            e => e.Kind == DeploymentEventKind.NotAuthorized && e.Result.Contains("outside the deployment session scope"));
    }

    [Fact]
    public async Task ActiveSession_WrongArtifactHash_NoFlash()
    {
        using var fixture = new Fixture(Mismatch(1), TargetChecksum);
        var session = fixture.Session(
            true,
            new[] { DeploymentTarget.ForAmplifier(1) },
            DateTimeOffset.UtcNow.AddHours(1),
            sha256Override: new string('0', 64));
        var step = fixture.CreateStep(session);

        var result = await step.ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(InitStepResultKind.NextStep, result.Kind);
        Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, fixture.Comm.Sent);
        Assert.Contains(fixture.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.ArtifactVerificationFailed);
    }

    [Fact]
    public async Task ActiveSession_WrongArtifactChecksum_NoFlash()
    {
        using var fixture = new Fixture(Mismatch(1), TargetChecksum);
        var session = fixture.Session(
            true,
            new[] { DeploymentTarget.ForAmplifier(1) },
            DateTimeOffset.UtcNow.AddHours(1),
            checksum: 0xAAAA); // correct sha256, wrong expected embedded checksum
        var step = fixture.CreateStep(session);

        var result = await step.ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(InitStepResultKind.NextStep, result.Kind);
        Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, fixture.Comm.Sent);
        Assert.Contains(fixture.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.ArtifactVerificationFailed);
    }

    [Fact]
    public async Task ActiveSession_Expired_NoFlash()
    {
        using var fixture = new Fixture(Mismatch(1), TargetChecksum);
        var session = fixture.Session(
            true, new[] { DeploymentTarget.ForAmplifier(1) }, DateTimeOffset.UtcNow.AddSeconds(-1));
        var step = fixture.CreateStep(session);

        var result = await step.ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(InitStepResultKind.NextStep, result.Kind);
        Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, fixture.Comm.Sent);
        Assert.Contains(fixture.Log.DeploymentEvents,
            e => e.Kind == DeploymentEventKind.NotAuthorized && e.Result.Contains("expired"));
    }

    [Fact]
    public async Task ActiveSession_ValidTargetAndArtifact_FlashAuthorized()
    {
        using var fixture = new Fixture(Mismatch(1), TargetChecksum);
        var session = fixture.Session(
            true, new[] { DeploymentTarget.ForAmplifier(1) }, DateTimeOffset.UtcNow.AddHours(1));
        var step = fixture.CreateStep(session);

        var result = await step.ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(InitStepResultKind.Continue, result.Kind);
        Assert.Contains(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, fixture.Comm.Sent);
        Assert.Contains(fixture.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.ArtifactVerified);
        Assert.Contains(fixture.Log.DeploymentEvents,
            e => e.Kind == DeploymentEventKind.Authorized && e.Target == DeploymentTarget.ForAmplifier(1));
        Assert.Contains(fixture.Log.DeploymentEvents,
            e => e.Kind == DeploymentEventKind.FlashStarted && e.Target == DeploymentTarget.ForAmplifier(1));
    }

    [Fact]
    public async Task MultipleLegacy_ScopedToOne_OnlyThatOneAuthorized_OthersUntouched()
    {
        using var fixture = new Fixture(Mismatch(1, 2), TargetChecksum);
        var session = fixture.Session(
            true, new[] { DeploymentTarget.ForAmplifier(1) }, DateTimeOffset.UtcNow.AddHours(1));
        var step = fixture.CreateStep(session);

        var result = await step.ExecuteAsync(null, CancellationToken.None);

        // Fail-closed: amplifier #2 is NOT authorized, and the flash-all protocol cannot isolate #1,
        // so nothing is flashed.
        Assert.Equal(InitStepResultKind.NextStep, result.Kind);
        Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, fixture.Comm.Sent);

        Assert.Contains(fixture.Log.DeploymentEvents,
            e => e.Kind == DeploymentEventKind.Authorized && e.Target == DeploymentTarget.ForAmplifier(1));
        Assert.Contains(fixture.Log.DeploymentEvents,
            e => e.Kind == DeploymentEventKind.NotAuthorized && e.Target == DeploymentTarget.ForAmplifier(2));
        Assert.Contains(fixture.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.DeploymentBlocked);
        Assert.NotEmpty(fixture.Log.RollbackEvents);
    }

    [Fact]
    public async Task FailedTransfer_FailSafe_NoRetry_Auditable()
    {
        using var fixture = new Fixture(Mismatch(1), TargetChecksum);
        var session = fixture.Session(
            true, new[] { DeploymentTarget.ForAmplifier(1) }, DateTimeOffset.UtcNow.AddHours(1));
        var step = fixture.CreateStep(session);

        // Drive the flash FSM to the checksum-result failure (communication/transfer loss).
        await step.ExecuteAsync(null, CancellationToken.None);
        await step.ExecuteAsync(
            new ReceivedMessage(TaskId.FWHANDLER, TrackCommand.FWHANDLERINIT, TaskStates.CONNECTED, 0),
            CancellationToken.None);
        await step.ExecuteAsync(
            new ReceivedMessage(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY, TaskStates.DONE, 0),
            CancellationToken.None);
        await step.ExecuteAsync(
            new ReceivedMessage(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE, TaskStates.DONE, 0),
            CancellationToken.None);

        var failed = await step.ExecuteAsync(
            new ReceivedMessage(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM, TaskStates.DONE, TaskMessages.RECEIVED_CHECKSUM_NOK),
            CancellationToken.None);

        Assert.Equal(InitStepResultKind.Error, failed.Kind);
        Assert.Contains(fixture.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.FlashFailed);
        Assert.NotEmpty(fixture.Log.RollbackEvents);

        // No automatic retry: exactly one flash invocation was issued, and no further flash commands
        // were sent after the failure.
        Assert.Equal(1, fixture.Comm.Sent.Count(c => c == TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH));
    }

    [Fact]
    public async Task RestartAfterInterruptedDeployment_DoesNotAutoResume()
    {
        // Interrupted deployment: an authorized flash that fails mid-transfer.
        using (var first = new Fixture(Mismatch(1), TargetChecksum))
        {
            var session = first.Session(
                true, new[] { DeploymentTarget.ForAmplifier(1) }, DateTimeOffset.UtcNow.AddHours(1));
            var step = first.CreateStep(session);

            await step.ExecuteAsync(null, CancellationToken.None);
            await step.ExecuteAsync(
                new ReceivedMessage(TaskId.FWHANDLER, TrackCommand.FWHANDLERINIT, TaskStates.CONNECTED, 0),
                CancellationToken.None);
            await step.ExecuteAsync(
                new ReceivedMessage(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY, TaskStates.DONE, 0),
                CancellationToken.None);
            await step.ExecuteAsync(
                new ReceivedMessage(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE, TaskStates.DONE, 0),
                CancellationToken.None);
            var failed = await step.ExecuteAsync(
                new ReceivedMessage(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM, TaskStates.DONE, TaskMessages.RECEIVED_CHECKSUM_NOK),
                CancellationToken.None);

            Assert.Equal(InitStepResultKind.Error, failed.Kind);
            Assert.Contains(first.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.FlashFailed);
            Assert.NotEmpty(first.Log.RollbackEvents);
        }

        // Restart: a fresh runtime with NO session (sessions are in-memory and not persisted).
        using var restart = new Fixture(Mismatch(1), TargetChecksum);
        var restartStep = restart.CreateStep(session: null);

        var result = await restartStep.ExecuteAsync(null, CancellationToken.None);

        Assert.Equal(InitStepResultKind.NextStep, result.Kind);
        Assert.Equal("InitTrackamplifiers", result.NextStepName);
        Assert.DoesNotContain(TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH, restart.Comm.Sent);
        Assert.DoesNotContain(restart.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.FlashStarted);
        Assert.Contains(restart.Log.DeploymentEvents, e => e.Kind == DeploymentEventKind.NotAuthorized);
    }
}
