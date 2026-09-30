using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Core.TrackApplication.Simulator
{
    /// <summary>Read-only snapshot of one locomotive's simulated position.</summary>
    public sealed class LocoPosition
    {
        public int Address { get; init; }
        public int BlockId { get; init; }
        public int SectionId { get; init; }
        public double OffsetMm { get; init; }
        public int SpeedSteps { get; init; }
        public int Direction { get; init; }
    }

    /// <summary>Describes a section-occupancy change emitted by the movement simulator.</summary>
    public sealed class SectionOccupancyChangedEventArgs : EventArgs
    {
        public SectionOccupancyChangedEventArgs(int sectionId, bool occupied)
        {
            SectionId = sectionId;
            Occupied = occupied;
        }

        public int SectionId { get; }
        public bool Occupied { get; }
    }

    /// <summary>Describes a meaningful locomotive-position change (block/section change).</summary>
    public sealed class LocoPositionChangedEventArgs : EventArgs
    {
        public LocoPositionChangedEventArgs(LocoPosition position)
        {
            Position = position;
        }

        public LocoPosition Position { get; }
    }

    /// <summary>
    /// Narrow read + write surface of the movement simulator, exposed to the Integration layer
    /// (the tee hardware backend) and the UI. It also extends <see cref="IBlockPositionProvider"/>
    /// so the real hardware backend can resolve a locomotive's block from the simulator.
    /// </summary>
    public interface IMovementSimulation : IBlockPositionProvider
    {
        IReadOnlyList<LocoPosition> GetLocoPositions();
        void PlaceLoco(int address, int blockId);

        /// <summary>
        /// Sets a locomotive's speed and direction using the raw ECoS/hardware convention shared
        /// with <c>IHardwareBackend.SetLocoSpeed</c>: <paramref name="direction"/> is 0 = forward,
        /// non-zero = reverse.
        /// </summary>
        void SetLocoSpeed(int address, int ecosSpeed, int direction);
        void SetPower(bool on);
    }

    /// <summary>
    /// Minimal, deterministic train-movement simulator driven by a <see cref="LayoutProfile"/>.
    ///
    /// It reuses the kinematics proven in the old ECoS emulator
    /// (<c>SiebwaldeApp.EcosEmu/Hardware/TrackSimulatorBackend.cs</c>): 1 speed step = 10 mm/s,
    /// position-in-section in millimetres, and a boundary crossing that moves the locomotive to
    /// the next section/block. There is no wall-clock randomness and no ECoS dependency: the
    /// simulator only emits section-occupancy events.
    /// </summary>
    public sealed class DeterministicMovementSimulator : IMovementSimulation
    {
        /// <summary>Millimetres per second per speed step (ported from the old simulator).</summary>
        private const double MmPerSecondPerStep = 10.0;

        /// <summary>Fixed simulation tick (~10 Hz).</summary>
        private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);

        private readonly LayoutProfile _profile;
        private readonly Func<IReadOnlyDictionary<int, SwitchPosition>>? _switchPositions;

        private readonly Dictionary<int, LayoutSection> _sectionById;
        private readonly Dictionary<int, LayoutBlock> _blockById;
        private readonly Dictionary<int, (int BlockId, int Index)> _sectionPlacement;

        private readonly object _lock = new();
        private readonly Dictionary<int, LocoSimState> _locos = new();

        private Timer? _timer;
        private CancellationTokenRegistration _cancelRegistration;

        public DeterministicMovementSimulator(
            LayoutProfile profile,
            Func<IReadOnlyDictionary<int, SwitchPosition>>? switchPositions = null)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _switchPositions = switchPositions;

            _sectionById = profile.Sections.ToDictionary(s => s.Id);
            _blockById = profile.Blocks.ToDictionary(b => b.Id);

            _sectionPlacement = new Dictionary<int, (int, int)>();
            foreach (var block in profile.Blocks)
            {
                for (var i = 0; i < block.SectionIds.Count; i++)
                {
                    _sectionPlacement[block.SectionIds[i]] = (block.Id, i);
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Public control surface (IMovementSimulation)
        // ---------------------------------------------------------------------------------

        /// <inheritdoc />
        public void PlaceLoco(int address, int blockId)
        {
            lock (_lock)
            {
                if (!_blockById.TryGetValue(blockId, out var block) || block.SectionIds.Count == 0)
                {
                    return;
                }

                var sectionId = block.SectionIds[0];
                _locos[address] = new LocoSimState
                {
                    Address = address,
                    BlockId = blockId,
                    SectionId = sectionId,
                    OffsetMm = 0,
                    SpeedSteps = 0,
                    Direction = 1
                };
            }
        }

        /// <inheritdoc />
        public void SetLocoSpeed(int address, int ecosSpeed, int direction)
        {
            lock (_lock)
            {
                if (!_locos.TryGetValue(address, out var loco))
                {
                    return; // unplaced locomotive has no position to move from
                }

                loco.SpeedSteps = Math.Clamp(ecosSpeed, 0, AmplifierSpeedMapper.MaxEcosSpeed);
                // ECoS/hardware direction convention (shared with IHardwareBackend): 0 = forward,
                // non-zero = reverse. Internal sim Direction is +1 = forward, -1 = reverse.
                loco.Direction = direction == 0 ? 1 : -1;
            }
        }

        /// <inheritdoc />
        public void SetPower(bool on)
        {
            if (on)
            {
                return;
            }

            lock (_lock)
            {
                foreach (var loco in _locos.Values)
                {
                    loco.SpeedSteps = 0;
                }
            }
        }

        /// <inheritdoc />
        public int? TryGetBlockForLoc(int loc)
        {
            lock (_lock)
            {
                return _locos.TryGetValue(loc, out var state) ? state.BlockId : null;
            }
        }

        /// <inheritdoc />
        public bool TryGetBlockForLoc(int address, out int blockId)
        {
            lock (_lock)
            {
                if (_locos.TryGetValue(address, out var state))
                {
                    blockId = state.BlockId;
                    return true;
                }

                blockId = 0;
                return false;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<LocoPosition> GetLocoPositions()
        {
            lock (_lock)
            {
                return _locos.Values
                    .OrderBy(l => l.Address)
                    .Select(ToLocoPosition)
                    .ToArray();
            }
        }

        // ---------------------------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------------------------

        /// <summary>Raised when a section becomes occupied (true) or free (false).</summary>
        public event EventHandler<SectionOccupancyChangedEventArgs>? SectionOccupancyChanged;

        /// <summary>Raised when a locomotive's block/section changes.</summary>
        public event EventHandler<LocoPositionChangedEventArgs>? LocoPositionChanged;

        /// <summary>IBlockPositionProvider event; intentionally never raised (read side only).</summary>
#pragma warning disable 0067 // intentionally never raised
        public event Action<int, int>? BlockEntered;
#pragma warning restore 0067

        // ---------------------------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------------------------

        /// <summary>Starts the internal ~100 ms tick. Idempotent.</summary>
        public void Start(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_timer is not null)
                {
                    return;
                }

                _cancelRegistration = cancellationToken.Register(Stop);
                _timer = new Timer(_ => Advance(TickInterval), null, TickInterval, TickInterval);
            }
        }

        /// <summary>
        /// Stops the internal tick. Does NOT emit occupancy: it only halts movement. Idempotent.
        /// </summary>
        public void Stop()
        {
            Timer? timer;
            lock (_lock)
            {
                timer = _timer;
                _timer = null;
                _cancelRegistration.Dispose();
                _cancelRegistration = default;
            }

            timer?.Dispose();
        }

        // ---------------------------------------------------------------------------------
        // Simulation step
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Advances every moving locomotive by <c>SpeedSteps * 10 mm/s * dt</c>, crossing section
        /// and block boundaries. Emits <see cref="SectionOccupancyChanged"/> (leave=false then
        /// enter=true) and <see cref="LocoPositionChanged"/> when a block changes.
        /// </summary>
        public void Advance(TimeSpan dt)
        {
            var dtSeconds = dt.TotalSeconds;
            if (dtSeconds <= 0)
            {
                return;
            }

            var occupancyEvents = new List<SectionOccupancyChangedEventArgs>();
            var positionEvents = new List<LocoPositionChangedEventArgs>();

            lock (_lock)
            {
                foreach (var loco in _locos.Values)
                {
                    MoveLoco(loco, dtSeconds, occupancyEvents, positionEvents);
                }
            }

            foreach (var e in occupancyEvents)
            {
                SectionOccupancyChanged?.Invoke(this, e);
            }

            foreach (var e in positionEvents)
            {
                LocoPositionChanged?.Invoke(this, e);
            }
        }

        private void MoveLoco(
            LocoSimState loco,
            double dtSeconds,
            List<SectionOccupancyChangedEventArgs> occupancyEvents,
            List<LocoPositionChangedEventArgs> positionEvents)
        {
            if (loco.SpeedSteps <= 0)
            {
                return;
            }

            var delta = loco.SpeedSteps * MmPerSecondPerStep * loco.Direction * dtSeconds;
            if (delta == 0)
            {
                return;
            }

            // One boundary crossing per tick, mirroring the old simulator (no overshoot carry).
            var section = _sectionById[loco.SectionId];
            var length = section.LengthMm;
            var newOffset = loco.OffsetMm + delta;

            if (newOffset >= 0 && newOffset <= length)
            {
                loco.OffsetMm = newOffset;
                return;
            }

            var placement = _sectionPlacement[loco.SectionId];
            var block = _blockById[placement.BlockId];

            // Leaving the current section.
            occupancyEvents.Add(new SectionOccupancyChangedEventArgs(loco.SectionId, false));

            var nextSectionId = loco.Direction > 0
                ? NextSectionForward(placement, block, loco)
                : NextSectionReverse(placement, block, loco);

            if (nextSectionId is null)
            {
                // No valid next section/block: stop at the boundary without emitting occupancy.
                loco.OffsetMm = loco.Direction > 0 ? length : 0;
                loco.SpeedSteps = 0;
                return;
            }

            var oldBlockId = loco.BlockId;

            loco.SectionId = nextSectionId.Value;
            loco.BlockId = _sectionPlacement[nextSectionId.Value].BlockId;
            loco.OffsetMm = loco.Direction > 0 ? 0 : _sectionById[nextSectionId.Value].LengthMm;

            // Entering the new section.
            occupancyEvents.Add(new SectionOccupancyChangedEventArgs(nextSectionId.Value, true));

            if (oldBlockId != loco.BlockId)
            {
                positionEvents.Add(new LocoPositionChangedEventArgs(ToLocoPosition(loco)));
            }
        }

        private int? NextSectionForward((int BlockId, int Index) placement, LayoutBlock block, LocoSimState loco)
        {
            if (placement.Index + 1 < block.SectionIds.Count)
            {
                return block.SectionIds[placement.Index + 1];
            }

            var nextBlock = ResolveNextBlock(loco.BlockId, +1);
            if (nextBlock is null || !_blockById.TryGetValue(nextBlock.Value, out var next) || next.SectionIds.Count == 0)
            {
                return null;
            }

            return next.SectionIds[0];
        }

        private int? NextSectionReverse((int BlockId, int Index) placement, LayoutBlock block, LocoSimState loco)
        {
            if (placement.Index - 1 >= 0)
            {
                return block.SectionIds[placement.Index - 1];
            }

            var nextBlock = ResolveNextBlock(loco.BlockId, -1);
            if (nextBlock is null || !_blockById.TryGetValue(nextBlock.Value, out var next) || next.SectionIds.Count == 0)
            {
                return null;
            }

            return next.SectionIds[next.SectionIds.Count - 1];
        }

        /// <summary>
        /// Resolves the next block for the given travel direction (+1 forward, -1 reverse) from the
        /// profile's routes. Forward matches <c>FromBlock == current</c>; reverse matches
        /// <c>ToBlock == current</c> (traversing a route in reverse). A route that requires a
        /// switch position is only taken when that switch's position is known and matches.
        /// </summary>
        private int? ResolveNextBlock(int currentBlock, int direction)
        {
            foreach (var route in _profile.Routes)
            {
                int to;
                if (direction > 0)
                {
                    if (route.FromBlock != currentBlock)
                    {
                        continue;
                    }

                    to = route.ToBlock;
                }
                else
                {
                    if (route.ToBlock != currentBlock)
                    {
                        continue;
                    }

                    to = route.FromBlock;
                }

                if (route.SwitchId is int switchId)
                {
                    var positions = _switchPositions?.Invoke();
                    if (positions is null || !positions.TryGetValue(switchId, out var actual))
                    {
                        continue;
                    }

                    if (route.RequiredSwitchPosition is SwitchPosition required && required != actual)
                    {
                        continue;
                    }
                }

                return to;
            }

            return null;
        }

        private static LocoPosition ToLocoPosition(LocoSimState loco)
            => new()
            {
                Address = loco.Address,
                BlockId = loco.BlockId,
                SectionId = loco.SectionId,
                OffsetMm = loco.OffsetMm,
                SpeedSteps = loco.SpeedSteps,
                Direction = loco.Direction
            };

        private sealed class LocoSimState
        {
            public int Address { get; init; }
            public int BlockId { get; set; }
            public int SectionId { get; set; }
            public double OffsetMm { get; set; }
            public int SpeedSteps { get; set; }
            public int Direction { get; set; }
        }
    }
}
