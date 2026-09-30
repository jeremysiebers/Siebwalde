using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Binds the movement simulator's section-occupancy events to the controllable
    /// simulated-amplifier I/O surface (<see cref="ISimulatedTrackIo"/>), so simulated occupancy
    /// flows through the REAL transport -&gt; comm -&gt; occupancy-bridge -&gt; ECoS path. It also
    /// places the profile's locomotives and establishes the initial occupancy on start, and it is
    /// started/stopped with the FullSimulation runtime lifecycle.
    /// </summary>
    public sealed class MovementSimulationAdapter : IDisposable
    {
        private readonly DeterministicMovementSimulator _simulator;
        private readonly LayoutProfile _profile;
        private readonly Func<ISimulatedTrackIo?> _trackIoAccessor;
        private readonly IReadOnlyDictionary<int, int> _sectionToAmplifier;
        private bool _started;

        public MovementSimulationAdapter(
            DeterministicMovementSimulator simulator,
            LayoutProfile profile,
            Func<ISimulatedTrackIo?> trackIoAccessor)
        {
            _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _trackIoAccessor = trackIoAccessor ?? throw new ArgumentNullException(nameof(trackIoAccessor));

            _sectionToAmplifier = profile.Sections.ToDictionary(s => s.Id, s => s.AmplifierSlave);

            _simulator.SectionOccupancyChanged += OnSectionOccupancyChanged;
        }

        /// <summary>
        /// Places the profile's locomotives, establishes initial occupancy on the simulated I/O,
        /// and starts the movement timer. Idempotent.
        /// </summary>
        public void Start(CancellationToken cancellationToken = default)
        {
            if (_started)
            {
                return;
            }

            _started = true;

            foreach (var loco in _profile.Locomotives)
            {
                _simulator.PlaceLoco(loco.Address, loco.InitialBlock);
            }

            var io = _trackIoAccessor();
            if (io is not null)
            {
                foreach (var position in _simulator.GetLocoPositions())
                {
                    io.SetSlaveOccupancy((byte)AmplifierForSection(position.SectionId), true);
                }
            }

            _simulator.Start(cancellationToken);
        }

        /// <summary>Stops the movement timer. Occupancy is NOT cleared here (just halts).</summary>
        public void Stop()
        {
            _simulator.Stop();
            _started = false;
        }

        /// <inheritdoc />
        public void Dispose() => Stop();

        private void OnSectionOccupancyChanged(object? sender, SectionOccupancyChangedEventArgs e)
        {
            var io = _trackIoAccessor();
            if (io is null)
            {
                return;
            }

            if (_sectionToAmplifier.TryGetValue(e.SectionId, out var slave))
            {
                io.SetSlaveOccupancy((byte)slave, e.Occupied);
            }
        }

        private int AmplifierForSection(int sectionId)
            => _sectionToAmplifier.TryGetValue(sectionId, out var slave) ? slave : sectionId;
    }
}
