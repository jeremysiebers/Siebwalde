using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.SystemRegressionHost
{
    /// <summary>
    /// Null ECoS host: a software-only stand-in for the production <see cref="IEcosHostService"/>
    /// (the TCP server Koploper dials). It binds NO port (15471), dials NO Koploper socket (5700)
    /// and starts no cross-process controller-session lease.
    ///
    /// It is deliberately NOT a second init implementation: it merely reports
    /// <see cref="EcosHostStartResult.Started"/> so that
    /// <see cref="TrackApplicationRuntimeHost.StartAsync"/> proceeds to compose and run the REAL
    /// init pipeline (ComposeTrackPartAsync -> TrackInitStateMachine -> 9 production steps ->
    /// TrackControlMain) against the injected deterministic transport.
    /// </summary>
    internal sealed class NullEcosHostService : IEcosHostService
    {
        public event EventHandler<RuntimeFaultEventArgs>? Faulted;

        public bool IsRunning { get; private set; }

        public TrackControlMode? Mode { get; private set; }

        public ControlDiagnostics? Diagnostics => null;

        public bool IsUnsafe => false;

        public bool ResetSafety() => false;

        public void SetProfile(LayoutProfile profile)
        {
            // No-op: the harness supplies no layout profile; the FullSimulation domain comes from
            // the configured amplifier groups passed to TrackApplicationRuntimeHost.
        }

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
}
