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
    /// Occupancy becomes observable once <b>fresh</b> amplifier data has been received.
    /// <see cref="TrackAmplifierItem.LastDataReceivedUtc"/> is stamped when a frame is parsed and
    /// <see cref="TrackAmplifierDataFreshness"/> decides whether it is still current, so stale
    /// data does not keep occupancy observable after communication stops.
    ///
    /// Switch feedback does not exist on real hardware yet, so it stays unavailable and is never
    /// reported as a confirmation.
    /// </summary>
    public sealed class AmplifierOccupancyObservability : IObservability
    {
        private readonly TrackApplicationVariables _variables;
        private readonly Func<DateTimeOffset> _clock;
        private readonly TimeSpan _staleAfter;

        public AmplifierOccupancyObservability(
            TrackApplicationVariables variables,
            Func<DateTimeOffset>? clock = null,
            TimeSpan? staleAfter = null)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
            _staleAfter = staleAfter ?? TrackAmplifierDataFreshness.DefaultStaleAfter;
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

                var now = _clock();

                foreach (var amplifier in items)
                {
                    if (TrackAmplifierDataFreshness.IsCurrentData(amplifier, now, _staleAfter))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
