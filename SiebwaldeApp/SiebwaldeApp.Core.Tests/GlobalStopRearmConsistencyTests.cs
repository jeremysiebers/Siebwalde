using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Consistency tests for the authorized re-arm: the C# <see cref="GlobalStopController"/> mirror
    /// of the physical <c>REARM_GLOBAL_STOP</c> (112) and the monitor's 109 re-assertion on the
    /// latch rising edge. No track controller, network or hardware is involved.
    /// </summary>
    public class GlobalStopRearmConsistencyTests
    {
        // ------------------------------------------------------------------
        // ReArm(): the C# mirror of the physical 112 (LATCHED -> ARMED)
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(GlobalStopState.Disarmed)]
        [InlineData(GlobalStopState.Armed)]
        public void ReArm_FromNonLatched_ReturnsFalse_StateUnchanged(GlobalStopState state)
        {
            var stop = new GlobalStopController();
            if (state == GlobalStopState.Armed)
            {
                Assert.True(stop.Arm());
            }

            Assert.False(stop.ReArm());
            Assert.Equal(state, stop.State);
            Assert.Null(stop.Reason);
        }

        [Fact]
        public void ReArm_FromLatched_TransitionsToArmed_ClearsReason_AndReenablesStopNotification()
        {
            var stop = new GlobalStopController();
            Assert.True(stop.Latch(GlobalStopReason.Operator));

            // Notification claim dedup while latched.
            Assert.True(stop.TryClaimStopNotification());
            Assert.False(stop.TryClaimStopNotification());

            Assert.True(stop.ReArm());
            Assert.Equal(GlobalStopState.Armed, stop.State);
            Assert.Null(stop.Reason);
            Assert.False(stop.IsLatched);

            // A future latch is observable/claimable again (the claim was reset by ReArm).
            Assert.True(stop.Latch(GlobalStopReason.Pic32HostLoss));
            Assert.True(stop.TryClaimStopNotification());
        }

        // ------------------------------------------------------------------
        // Monitor: 109 on the latch rising edge (re-assert after an authorized re-arm)
        // ------------------------------------------------------------------

        [Fact]
        public async Task Monitor_RearmThenRelatch_Resends109_OnLatchRisingEdge()
        {
            var variables = new TrackApplicationVariables();
            await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
            await using var comm = new TrackCommClientAsync(transport, variables);
            await comm.StartAsync(false);
            using var monitor = new GlobalStopMonitor(comm, variables);

            var t0 = DateTimeOffset.UtcNow;
            monitor.EstablishLink(t0);

            // First latch (host loss) -> one 109 on the rising edge.
            monitor.ObserveStatus(
                new ReceivedMessage(10, 111, 7, GlobalStopStatusMessage.LatchedHostLoss), t0);
            await monitor.TickAsync(t0);
            Assert.Equal(1, transport.SentFrames.Count(f => f[1] == TrackCommand.EXEC_GLOBAL_STOP));

            // The transport answers the 109 with a 111 (latched-host-initiated); wait for that echo
            // to be consumed so the monitor's internal receive freshness is settled before re-arming.
            await WaitUntil(() => monitor.LastObservedPic32Status == GlobalStopStatusMessage.LatchedHostInitiated);

            // Still latched: no re-send.
            await monitor.TickAsync(DateTimeOffset.UtcNow);
            Assert.Equal(1, transport.SentFrames.Count(f => f[1] == TrackCommand.EXEC_GLOBAL_STOP));

            // Operator-authorized re-arm mirrors the physical 112 into the C# controller.
            Assert.True(variables.GlobalStop.ReArm());
            Assert.Equal(GlobalStopState.Armed, variables.GlobalStop.State);

            // A non-latched tick (fresh real-time last-status) does NOT re-latch, and resets the edge.
            await monitor.TickAsync(DateTimeOffset.UtcNow);
            Assert.Equal(GlobalStopState.Armed, variables.GlobalStop.State);

            // A second latch (host initiated) must re-assert the stop: a SECOND 109.
            monitor.ObserveStatus(
                new ReceivedMessage(10, 111, 7, GlobalStopStatusMessage.LatchedHostInitiated), DateTimeOffset.UtcNow);
            await monitor.TickAsync(DateTimeOffset.UtcNow);
            Assert.Equal(2, transport.SentFrames.Count(f => f[1] == TrackCommand.EXEC_GLOBAL_STOP));
        }

        private static async Task WaitUntil(Func<bool> predicate)
        {
            var watch = Stopwatch.StartNew();
            while (!predicate())
            {
                if (watch.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException();
                await Task.Delay(10);
            }
        }
    }
}
