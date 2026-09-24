using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Pure transition-policy matrix for the runtime lifecycle. This is the single source of truth
    /// that the coordinator enforces and the UI uses to enable/disable its commands.
    /// </summary>
    public class TrackRuntimeControlPolicyTests
    {
        [Theory]
        [InlineData(TrackRuntimeState.Stopped, true)]
        [InlineData(TrackRuntimeState.Starting, false)]
        [InlineData(TrackRuntimeState.Running, false)]
        [InlineData(TrackRuntimeState.Stopping, false)]
        [InlineData(TrackRuntimeState.Failed, false)]
        public void CanStart_MatchesTransitionTable(TrackRuntimeState state, bool expected)
        {
            Assert.Equal(expected, TrackRuntimeControlPolicy.CanStart(state));
        }

        [Theory]
        [InlineData(TrackRuntimeState.Stopped, false)]
        [InlineData(TrackRuntimeState.Starting, false)]
        [InlineData(TrackRuntimeState.Running, true)]
        [InlineData(TrackRuntimeState.Stopping, false)]
        [InlineData(TrackRuntimeState.Failed, true)]
        public void CanStop_MatchesTransitionTable(TrackRuntimeState state, bool expected)
        {
            Assert.Equal(expected, TrackRuntimeControlPolicy.CanStop(state));
        }

        [Theory]
        [InlineData(TrackRuntimeState.Stopped, false)]
        [InlineData(TrackRuntimeState.Starting, false)]
        [InlineData(TrackRuntimeState.Running, true)]
        [InlineData(TrackRuntimeState.Stopping, false)]
        [InlineData(TrackRuntimeState.Failed, true)]
        public void CanRestart_MatchesTransitionTable(TrackRuntimeState state, bool expected)
        {
            Assert.Equal(expected, TrackRuntimeControlPolicy.CanRestart(state));
        }
    }
}
