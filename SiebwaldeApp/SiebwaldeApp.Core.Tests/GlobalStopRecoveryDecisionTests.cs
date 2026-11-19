using System;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Offline tests for the campaign REARM policy (<see cref="GlobalStopRecoveryDecision"/>). The
    /// campaign may only re-arm a LATCHED-HOST-LOSS latch (111 = 1); a host-initiated latch (2) or an
    /// unknown/unexpected state (including an unreachable controller, i.e. null) must stop fail-closed.
    /// </summary>
    public class GlobalStopRecoveryDecisionTests
    {
        [Theory]
        [InlineData(GlobalStopStatusMessage.Disarmed)]  // 0
        [InlineData(GlobalStopStatusMessage.Armed)]     // 3
        public void ProceedWithoutRearm_ForDisarmedOrArmed(ushort state)
        {
            Assert.Equal(GlobalStopRecoveryAction.ProceedNoRearm, GlobalStopRecoveryDecision.Decide(state));
        }

        [Fact]
        public void Rearm_OnlyForLatchedHostLoss()
        {
            // The ONLY state that may be re-armed by an automated campaign.
            Assert.Equal(GlobalStopRecoveryAction.Rearm, GlobalStopRecoveryDecision.Decide(GlobalStopStatusMessage.LatchedHostLoss));
        }

        [Fact]
        public void NeverRearm_ForLatchedHostInitiated()
        {
            // Host-initiated / EMO / operator stop: the campaign must stop, never blindly re-arm.
            Assert.Equal(GlobalStopRecoveryAction.StopFailClosed, GlobalStopRecoveryDecision.Decide(GlobalStopStatusMessage.LatchedHostInitiated));
        }

        [Theory]
        [InlineData((ushort)4)]
        [InlineData((ushort)5)]
        [InlineData((ushort)0xFF)]
        public void NeverRearm_ForUnexpectedState(ushort state)
        {
            Assert.Equal(GlobalStopRecoveryAction.StopFailClosed, GlobalStopRecoveryDecision.Decide(state));
        }

        [Fact]
        public void NeverRearm_ForUnknownState_Null()
        {
            // No 111 reply (controller unreachable / read timed out) is unknown -> fail closed.
            Assert.Equal(GlobalStopRecoveryAction.StopFailClosed, GlobalStopRecoveryDecision.Decide(null));
        }

        [Fact]
        public void RearmAction_NeverEqualsStop_AndViceVersa()
        {
            // Sanity: the classifier has exactly three distinct actions and Rearm is exclusive to
            // LatchedHostLoss. Enumerate every byte value to prove no other value maps to Rearm.
            for (int v = 0; v <= 255; v++)
            {
                var action = GlobalStopRecoveryDecision.Decide((ushort)v);
                if (v == GlobalStopStatusMessage.LatchedHostLoss)
                {
                    Assert.Equal(GlobalStopRecoveryAction.Rearm, action);
                }
                else
                {
                    Assert.NotEqual(GlobalStopRecoveryAction.Rearm, action);
                }
            }
        }
    }
}
