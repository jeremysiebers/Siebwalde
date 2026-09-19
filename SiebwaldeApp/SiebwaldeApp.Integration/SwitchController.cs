using System;
using System.Collections.Generic;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Translates ECoS/Koploper switch commands into physical switch output drives, using the
    /// configured <see cref="SwitchMapping"/>. One instance is shared by real and simulator
    /// mode: only the <see cref="ISwitchOutput"/> behind it differs, so there is no separate
    /// control path for the simulator.
    ///
    /// The logical (ECoS) position and the physical position are tracked separately, because
    /// a mapping may invert them. The logical positions feed the routing/look-ahead model,
    /// which speaks Koploper's `g`/`r` semantics.
    /// </summary>
    public sealed class SwitchController
    {
        private readonly ISwitchOutput _output;
        private readonly Action<string>? _log;

        private readonly Dictionary<int, SwitchPosition> _logicalByEcosAddress = new();
        private readonly Dictionary<int, SwitchPosition> _physicalByAddress = new();
        private readonly object _lock = new();

        public SwitchController(SwitchMapping mapping, ISwitchOutput output, Action<string>? log = null)
        {
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _log = log;

            foreach (var error in Mapping.Errors)
            {
                _log?.Invoke($"Switch mapping problem: {error}");
            }
        }

        /// <summary>The mapping this controller translates with.</summary>
        public SwitchMapping Mapping { get; }

        /// <summary>
        /// Translates and applies an ECoS switch request. Returns false (and drives nothing)
        /// when the ECoS address is not mapped, so an unmapped command can never reach an
        /// unrelated output.
        /// </summary>
        public bool TryApply(int ecosAddress, SwitchPosition requested, out SwitchPosition applied)
        {
            applied = requested;

            if (!Mapping.TryGetEntry(ecosAddress, out var entry))
            {
                _log?.Invoke($"Switch {ecosAddress} is not mapped; the command is ignored and no output is driven.");
                return false;
            }

            applied = entry.ToPhysical(requested);

            _output.SetPosition(entry.PhysicalAddress, applied);

            lock (_lock)
            {
                // Koploper's logical state (drives routing/look-ahead).
                _logicalByEcosAddress[ecosAddress] = requested;
                // What was actually driven, for diagnostics.
                _physicalByAddress[entry.PhysicalAddress] = applied;
            }

            _log?.Invoke(
                $"Switch {ecosAddress} -> physical {entry.PhysicalAddress}: ECoS {requested} = physical {applied}.");

            return true;
        }

        /// <summary>
        /// Drives every mapped switch that has a configured default position, so the layout
        /// starts from a known state. Entries configured as <c>keep</c> are deliberately left
        /// alone and are not reported as known, because guessing a physical rest position
        /// would let the logical and physical state disagree silently.
        /// </summary>
        public void Initialize()
        {
            foreach (var entry in Mapping.Entries)
            {
                if (entry.DefaultPosition is null)
                {
                    _log?.Invoke(
                        $"Switch {entry.EcosAddress} default is 'keep'; leaving physical {entry.PhysicalAddress} untouched.");
                    continue;
                }

                var logical = entry.DefaultPosition.Value;
                var physical = entry.ToPhysical(logical);

                _output.SetPosition(entry.PhysicalAddress, physical);

                lock (_lock)
                {
                    _logicalByEcosAddress[entry.EcosAddress] = logical;
                    _physicalByAddress[entry.PhysicalAddress] = physical;
                }

                _log?.Invoke(
                    $"Switch {entry.EcosAddress} initialized to {logical} (physical {entry.PhysicalAddress} = {physical}).");
            }
        }

        /// <summary>
        /// The logical switch positions known so far, keyed by ECoS/Koploper switch address.
        /// Used as the switch-position source for routing and look-ahead.
        /// </summary>
        public IReadOnlyDictionary<int, SwitchPosition> GetLogicalPositions()
        {
            lock (_lock)
            {
                return new Dictionary<int, SwitchPosition>(_logicalByEcosAddress);
            }
        }

        /// <summary>The physical position last driven to an output, for diagnostics.</summary>
        public bool TryGetPhysicalPosition(int physicalAddress, out SwitchPosition position)
        {
            lock (_lock)
            {
                return _physicalByAddress.TryGetValue(physicalAddress, out position);
            }
        }
    }
}
