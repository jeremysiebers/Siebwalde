using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    public class ControlSafetyInterlockBackendTests
    {
        private sealed class NoopStopSink : ISafetyStopSink
        {
            public SafetyStopResult StopLoco(int address) => SafetyStopResult.NotApplied();
            public SafetyStopResult StopLayout() => SafetyStopResult.NotApplied();
        }

        private sealed class RecordingBackend : IHardwareBackend
        {
            public List<(int Address, int Speed, int Direction)> LocoCommands { get; } = new();

            public bool SetPower(bool on) => true;

            public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
            {
                LocoCommands.Add((address, ecosSpeed, direction));
                return true;
            }

            public bool SetSwitch(int decoderAddress, int outputIndex, bool on) => true;
        }

        private static (ControlSafetyInterlockBackend Interlock, RecordingBackend Inner) Create(
            IMovementPermissionState? permission = null)
        {
            var diagnostics = new ControlDiagnostics();
            var guard = new ControlSafetyGuard(new NoopStopSink(), diagnostics);
            var inner = new RecordingBackend();

            return (new ControlSafetyInterlockBackend(inner, guard, diagnostics, movementPermission: permission), inner);
        }

        [Fact]
        public void NotGrantedPermission_RefusesNonZeroMovement_AndIsMovementBlocked()
        {
            var permission = new MovementPermissionController(); // NotGranted
            var (interlock, inner) = Create(permission);

            Assert.False(interlock.SetLocoSpeed(42, 40, 0));
            Assert.True(interlock.IsMovementBlocked(42));
            Assert.Empty(inner.LocoCommands);
        }

        [Fact]
        public void NotGrantedPermission_StillAllowsStop()
        {
            var permission = new MovementPermissionController(); // NotGranted
            var (interlock, inner) = Create(permission);

            Assert.True(interlock.SetLocoSpeed(42, 0, 0));
            Assert.Equal(new[] { (42, 0, 0) }, inner.LocoCommands);
        }

        [Fact]
        public void GrantedPermission_AllowsNonZeroMovement()
        {
            var permission = new MovementPermissionController();
            permission.Grant();
            var (interlock, inner) = Create(permission);

            Assert.True(interlock.SetLocoSpeed(42, 40, 0));
            Assert.False(interlock.IsMovementBlocked(42));
            Assert.Equal(new[] { (42, 40, 0) }, inner.LocoCommands);
        }

        [Fact]
        public void NullPermission_IsNotGated()
        {
            var (interlock, inner) = Create(permission: null);

            Assert.True(interlock.SetLocoSpeed(42, 40, 0));
            Assert.False(interlock.IsMovementBlocked(42));
            Assert.Equal(new[] { (42, 40, 0) }, inner.LocoCommands);
        }
    }
}
