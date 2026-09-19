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

    /// <summary>
    /// Real-mode observability over the existing amplifier data path.
    ///
    /// Occupancy becomes observable once valid amplifier data has been received.
    /// <see cref="TrackAmplifierItem.SlaveDetected"/> is written in the same step that stores the
    /// holding registers when the master's amplifier frame is parsed, so it is the existing
    /// "valid data received" signal - no new freshness mechanism and no extra state are added.
    ///
    /// Switch feedback does not exist on real hardware yet, so it stays unavailable and is never
    /// reported as a confirmation.
    /// </summary>
    public sealed class AmplifierOccupancyObservability : IObservability
    {
        private readonly TrackApplicationVariables _variables;

        public AmplifierOccupancyObservability(TrackApplicationVariables variables)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        }

        /// <inheritdoc />
        public bool SwitchFeedbackAvailable => false;

        /// <inheritdoc />
        public bool OccupancyAvailable
        {
            get
            {
                var items = _variables.trackAmpItems;

                if (items is null)
                {
                    return false;
                }

                foreach (var amplifier in items)
                {
                    if (amplifier is not null && amplifier.SlaveDetected != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
