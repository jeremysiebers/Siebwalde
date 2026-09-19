using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Reads switch positions back from the simulator, where the full switch state is
    /// observable.
    /// </summary>
    public sealed class SimulatorSwitchObserver : ISwitchObserver
    {
        private readonly TrackSimulatorBackend _backend;

        public SimulatorSwitchObserver(TrackSimulatorBackend backend)
        {
            _backend = backend;
        }

        /// <inheritdoc />
        public bool TryGetObservedPosition(int physicalAddress, out SwitchPosition position)
        {
            position = default;

            if (!_backend.TryGetSwitchPosition(physicalAddress, out var raw))
            {
                return false;
            }

            position = raw == 0 ? SwitchPosition.Straight : SwitchPosition.Diverging;
            return true;
        }
    }

    /// <summary>
    /// Observer for a mode where switch feedback does not exist yet. It always reports "not
    /// observable" instead of inventing a confirmation.
    /// </summary>
    public sealed class UnobservableSwitchObserver : ISwitchObserver
    {
        /// <inheritdoc />
        public bool TryGetObservedPosition(int physicalAddress, out SwitchPosition position)
        {
            position = default;
            return false;
        }
    }

    /// <summary>Describes what a mode can observe, so unavailable feedback is never a fault.</summary>
    public sealed class ModeObservability : IObservability
    {
        /// <inheritdoc />
        public bool SwitchFeedbackAvailable { get; init; }

        /// <inheritdoc />
        public bool OccupancyAvailable { get; init; }
    }
}
