using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Logic-only tests for the read-only global-stop recovery surface exposed to the UI:
    /// <see cref="GlobalStopStateView"/> (the projection) and its production wiring on
    /// <see cref="TrackApplicationRuntimeHost"/> through <see cref="ITrackApplicationRuntime"/>.
    /// No track controller, network or WPF is involved.
    /// </summary>
    public class GlobalStopRecoverySurfaceTests
    {
        private sealed class FakeEcosHostService : IEcosHostService
        {
            public event EventHandler<RuntimeFaultEventArgs>? Faulted;

            public bool IsRunning { get; private set; }
            public TrackControlMode? Mode { get; private set; }
            public ControlDiagnostics? Diagnostics => null;
            public bool IsUnsafe => false;

            public bool ResetSafety() => false;

            public void SetProfile(LayoutProfile profile) { }

            public IReadOnlyDictionary<int, SwitchPosition> GetSwitchPositions()
                => new Dictionary<int, SwitchPosition>();

            public Task<EcosHostStartResult> StartAsync(
                TrackControlMode mode,
                ITrackCommClient? commClient,
                TrackApplicationVariables? variables,
                CancellationToken cancellationToken = default,
                IMovementSimulation? movementSimulation = null)
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

            public void Stop()
            {
                IsRunning = false;
                Mode = null;
            }
        }

        /// <summary>
        /// The UI "enables recovery only when latched" via <c>CanRecoverGlobalStop = view.IsLatched</c>;
        /// the helper must be true only for the latched state.
        /// </summary>
        [Theory]
        [InlineData(GlobalStopState.Disarmed, false)]
        [InlineData(GlobalStopState.Armed, false)]
        [InlineData(GlobalStopState.Latched, true)]
        public void IsLatched_IsTrueOnlyWhenLatched(GlobalStopState state, bool expected)
        {
            var view = new GlobalStopStateView(state, null, recoveryPending: false);

            Assert.Equal(expected, view.IsLatched);
            Assert.Equal(state, view.State);
        }

        /// <summary>
        /// The projection is the UI-facing surface; it must not expose any mutating member, so the
        /// UI can never arm or latch the global stop on its own. Fails if a public mutator is added.
        /// </summary>
        [Fact]
        public void GlobalStopStateView_IsReadOnly_NoMutationMembers()
        {
            var mutators = typeof(GlobalStopStateView)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && !m.IsConstructor);

            Assert.Empty(mutators);
        }

        /// <summary>
        /// The host exposes the projection through the interface, reflecting the process-lifetime
        /// controller state + reason, and reports recovery as pending only once the operator has
        /// authorized a still-latched re-arm.
        /// </summary>
        [Fact]
        public void Host_GlobalStopState_ExposesLatchedAndReason_ThroughInterface()
        {
            ITrackApplicationRuntime runtime = new TrackApplicationRuntimeHost(new FakeEcosHostService());
            var host = (TrackApplicationRuntimeHost)runtime;

            var initial = runtime.GlobalStopState;
            Assert.Equal(GlobalStopState.Disarmed, initial.State);
            Assert.False(initial.IsLatched);
            Assert.Null(initial.Reason);
            Assert.False(initial.RecoveryPending);

            // Latch the controller exactly as the monitor does on an observed 111 = 1/2.
            host.GlobalStop.Latch(GlobalStopReason.Pic32HostLoss);

            var latched = runtime.GlobalStopState;
            Assert.Equal(GlobalStopState.Latched, latched.State);
            Assert.True(latched.IsLatched);
            Assert.Equal(GlobalStopReason.Pic32HostLoss, latched.Reason);
            Assert.False(latched.RecoveryPending); // authorized only after the operator acts

            // Operator authorizes recovery: pending becomes true while still latched.
            runtime.AuthorizeGlobalStopRecovery(() => true);

            var pending = runtime.GlobalStopState;
            Assert.True(pending.IsLatched);
            Assert.True(pending.RecoveryPending);
        }
    }
}
