using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

public class QueryControllerVersionTests
{
    /// <summary>
    /// Exercises the read-only send+parse round-trip: the comm client sends
    /// GET_CONTROLLER_VERSION (113) through the deterministic simulator, the simulator
    /// replies with the version frame, and the reply is parsed and surfaced on
    /// TrackControllerCommands.ControllerVersion. No amplifier/train state is touched.
    /// </summary>
    [Fact]
    public async Task GetControllerVersion_SendParseRoundTrip_SurfacesVersion()
    {
        var variables = new TrackApplicationVariables();
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
        await using var client = new TrackCommClientAsync(transport, variables);
        await client.StartAsync(realHardwareMode: false);

        Assert.Null(variables.trackControllerCommands.ControllerVersion);

        await client.SendAsync(new SendMessage(TrackCommand.GET_CONTROLLER_VERSION, Array.Empty<byte>()));

        await GracefulShutdownTestHelpers.WaitUntilAsync(
            () => variables.trackControllerCommands.ControllerVersion != null);

        var v = variables.trackControllerCommands.ControllerVersion!.Value;
        Assert.Equal(1, v.Major);
        Assert.Equal(0, v.Minor);
        Assert.Equal(0, v.Patch);
        Assert.Equal(0x01, v.ProtocolVersion);
        Assert.Equal("2026-10-17", v.BuildDate);
        Assert.Equal("1.0.0", v.ToString());
    }

    /// <summary>
    /// The invocation step issues exactly one 113 send, ignores unrelated control messages
    /// (no gating), and advances to ResetAllSlaves only on the CONTROLLER 113 reply.
    /// </summary>
    [Fact]
    public async Task QueryControllerVersionStep_Sends113AndAdvancesOnReply()
    {
        var variables = new TrackApplicationVariables();
        var comm = new RecordingCommClient();
        var step = new QueryControllerVersionStep(comm, variables);

        var first = await step.ExecuteAsync(null, CancellationToken.None);
        Assert.Equal(InitStepResultKind.Continue, first.Kind);
        Assert.Equal(new byte[] { TrackCommand.GET_CONTROLLER_VERSION }, comm.Sent.ToArray());

        // An unrelated message keeps it waiting and changes nothing.
        var waiting = await step.ExecuteAsync(
            new ReceivedMessage(TaskId.MBUS, TrackCommand.EXEC_MBUS_STATE_RESET, TaskStates.DONE, 0),
            CancellationToken.None);
        Assert.Equal(InitStepResultKind.Continue, waiting.Kind);
        Assert.Null(variables.trackControllerCommands.ControllerVersion);

        // The CONTROLLER 113 reply completes the round-trip and advances.
        var advanced = await step.ExecuteAsync(
            new ReceivedMessage(TaskId.CONTROLLER, TrackCommand.GET_CONTROLLER_VERSION, 1, 0),
            CancellationToken.None);
        Assert.Equal(InitStepResultKind.NextStep, advanced.Kind);
        Assert.Equal("GlobalStopPrecondition", advanced.NextStepName);
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
}
