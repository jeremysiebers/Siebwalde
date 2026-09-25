using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class MovementPermissionControllerTests
    {
        [Fact]
        public void InitialState_IsNotGranted()
        {
            var controller = new MovementPermissionController();

            Assert.Equal(MovementPermissionState.NotGranted, controller.State);
            Assert.False(controller.IsGranted);
        }

        [Fact]
        public void Grant_SetsGranted_AndRaisesStateChanged()
        {
            var controller = new MovementPermissionController();
            MovementPermissionState? raised = null;
            controller.StateChanged += (_, state) => raised = state;

            controller.Grant();

            Assert.Equal(MovementPermissionState.Granted, controller.State);
            Assert.True(controller.IsGranted);
            Assert.Equal(MovementPermissionState.Granted, raised);
        }

        [Fact]
        public void Withdraw_AfterGrant_SetsWithdrawn_AndIsNotGranted()
        {
            var controller = new MovementPermissionController();
            controller.Grant();

            controller.Withdraw();

            Assert.Equal(MovementPermissionState.Withdrawn, controller.State);
            Assert.False(controller.IsGranted);
        }

        [Fact]
        public void ResetToNotGranted_ReturnsToInitialState()
        {
            var controller = new MovementPermissionController();
            controller.Grant();
            controller.Withdraw();

            controller.ResetToNotGranted();

            Assert.Equal(MovementPermissionState.NotGranted, controller.State);
            Assert.False(controller.IsGranted);
        }

        [Fact]
        public void StateChanged_IsNotRaised_WhenStateIsUnchanged()
        {
            var controller = new MovementPermissionController();
            var calls = 0;
            controller.StateChanged += (_, _) => calls++;

            controller.Grant();
            controller.Grant(); // already granted: no transition

            Assert.Equal(1, calls);
        }
    }
}
