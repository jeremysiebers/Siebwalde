using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

/// <summary>
/// Heartbeat-lifecycle tests for the GlobalStopMonitor correction. HOST_HEARTBEAT (110) eligibility
/// is now established at the validated controller-session boundary via <see cref="GlobalStopMonitor.EnableHeartbeat"/>
/// — independent of the Pic32Lost receive-freshness watch, which stays gated behind
/// <see cref="GlobalStopMonitor.EstablishLink"/>. This lets the host renew authority throughout the
/// full init (stages 1-5, including MBUS 107/100/106) without misreading the init's legitimate gaps
/// (reset / slave-detect before the first SLAVEINFO readback) as a dead link.
/// </summary>
public class GlobalStopHeartbeatLifecycleTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 11, 13, 0, 0, 0, TimeSpan.Zero);
    private static ReceivedMessage Armed => new(TaskId.CONTROLLER, TrackCommand.GLOBAL_STOP_STATE, TaskStates.DONE, 3);

    private static (DeterministicTrackTransport Transport, TrackCommClientAsync Comm, TrackApplicationVariables Variables)
        StartComm()
    {
        var variables = new TrackApplicationVariables();
        var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
        var comm = new TrackCommClientAsync(transport, variables);
        comm.StartAsync(false).GetAwaiter().GetResult();
        return (transport, comm, variables);
    }

    [Fact]
    public async Task EnableHeartbeat_SendsHeartbeat_WithoutEstablishLink()
    {
        var (transport, comm, variables) = StartComm();
        using var monitor = new GlobalStopMonitor(comm, variables);

        monitor.EnableHeartbeat();
        await monitor.TickAsync(Epoch);

        // Heartbeat is eligible at the controller-session boundary, before EstablishLink (Running).
        Assert.Single(transport.SentFrames.Where(f => f[1] == 110));
    }

    [Fact]
    public async Task EnableHeartbeat_DoesNotEnablePic32LostWatch_NoSpuriousLatchAcrossLongInitGap()
    {
        // Scenario C: an init gap longer than 2 s (no fresh 111 status, no SLAVEINFO readback yet)
        // must NOT be misread as a dead link while only heartbeat eligibility is enabled.
        var (transport, comm, variables) = StartComm();
        using var monitor = new GlobalStopMonitor(comm, variables);

        monitor.EnableHeartbeat();
        await monitor.TickAsync(Epoch);

        for (int s = 1; s <= 30; s++)
            await monitor.TickAsync(Epoch.AddSeconds(s));

        Assert.Equal(GlobalStopState.Disarmed, variables.GlobalStop.State); // no false Pic32Lost latch
        Assert.DoesNotContain(transport.SentFrames, f => f[1] == 109);      // no spurious stop
        Assert.True(transport.SentFrames.Count(f => f[1] == 110) >= 2);     // heartbeats continued
    }

    [Fact]
    public async Task EstablishLink_EnablesBothHeartbeatAndPic32LostWatch()
    {
        // Backward compatibility: EstablishLink (Running) still enables BOTH heartbeat and the
        // Pic32Lost receive-freshness watch.
        var (transport, comm, variables) = StartComm();
        using var monitor = new GlobalStopMonitor(comm, variables);

        monitor.EstablishLink(Epoch);
        monitor.ObserveStatus(Armed, Epoch);
        await monitor.TickAsync(Epoch);                               // first heartbeat (seq 0)
        await monitor.TickAsync(Epoch.AddSeconds(2).AddMilliseconds(1)); // stale status, no readback

        Assert.Equal(GlobalStopReason.Pic32Lost, variables.GlobalStop.Reason);
        Assert.True(variables.GlobalStop.IsLatched);
        Assert.DoesNotContain(transport.SentFrames, f => f[1] == 109); // Pic32Lost suppresses dead-link writes
    }
}
