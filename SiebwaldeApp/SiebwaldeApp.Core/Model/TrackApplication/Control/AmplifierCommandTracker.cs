using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Retains the physical track amplifiers that have been successfully commanded non-neutral and
    /// have not yet been commanded neutral, attributed to the locomotive that owns them.
    ///
    /// This is the safety reachability bookkeeping. A normal A -> B block transition never
    /// neutralizes the vacated amplifier, and look-ahead intentionally commands a second
    /// amplifier before entry, so a locomotive can have a set of physical outputs carrying
    /// non-neutral commands at once. A stop that only resolves the current block mapping would
    /// leave those outputs unreachable.
    ///
    /// Ownership: an amplifier has exactly one owner at a time. When another locomotive commands
    /// the same amplifier non-neutral, ownership transfers to that locomotive, because the most
    /// recent non-neutral command is the one that must be neutralized. This keeps one
    /// locomotive's bookkeeping from clearing another locomotive's outstanding target.
    ///
    /// Lifecycle:
    /// - add on a concrete non-neutral physical command (not on mere logical intent);
    /// - remove only after a neutral command has been issued for that amplifier;
    /// - never remove because the locomotive moved to another block, the route changed, or the
    ///   block mapping disappeared.
    ///
    /// This tracks commanded state, not observed state. A removed target means "neutral was
    /// commanded", not "neutral was physically confirmed".
    /// </summary>
    public sealed class AmplifierCommandTracker
    {
        private readonly Dictionary<int, HashSet<ushort>> _ownedByLoco = new();
        private readonly object _lock = new();

        /// <summary>
        /// Records that a locomotive successfully issued a non-neutral command to an amplifier.
        /// Ownership transfers away from any other locomotive.
        /// </summary>
        public void RecordNonNeutral(int locoAddress, ushort amplifier)
        {
            if (amplifier == 0)
            {
                return;
            }

            lock (_lock)
            {
                foreach (var entry in _ownedByLoco)
                {
                    if (entry.Key != locoAddress)
                    {
                        entry.Value.Remove(amplifier);
                    }
                }

                if (!_ownedByLoco.TryGetValue(locoAddress, out var owned))
                {
                    owned = new HashSet<ushort>();
                    _ownedByLoco[locoAddress] = owned;
                }

                owned.Add(amplifier);
            }
        }

        /// <summary>Records several non-neutral amplifier commands for one locomotive.</summary>
        public void RecordNonNeutral(int locoAddress, IEnumerable<ushort> amplifiers)
        {
            if (amplifiers is null)
            {
                return;
            }

            foreach (var amplifier in amplifiers)
            {
                RecordNonNeutral(locoAddress, amplifier);
            }
        }

        /// <summary>
        /// Records that a locomotive issued a neutral command to an amplifier, so that amplifier
        /// is no longer an outstanding target for it. Other locomotives' ownership is untouched.
        /// </summary>
        public void RecordNeutral(int locoAddress, ushort amplifier)
        {
            if (amplifier == 0)
            {
                return;
            }

            lock (_lock)
            {
                if (!_ownedByLoco.TryGetValue(locoAddress, out var owned))
                {
                    return;
                }

                owned.Remove(amplifier);

                if (owned.Count == 0)
                {
                    _ownedByLoco.Remove(locoAddress);
                }
            }
        }

        /// <summary>Records neutral commands for several amplifiers of one locomotive.</summary>
        public void RecordNeutral(int locoAddress, IEnumerable<ushort> amplifiers)
        {
            if (amplifiers is null)
            {
                return;
            }

            foreach (var amplifier in amplifiers)
            {
                RecordNeutral(locoAddress, amplifier);
            }
        }

        /// <summary>
        /// Records that a global operation (a central power-off or an amplifier-centric layout
        /// neutralization) commanded the given amplifiers neutral, clearing them from every
        /// locomotive's outstanding set. This is the deliberate global escalation case.
        /// </summary>
        public void RecordNeutralGlobally(IEnumerable<ushort> amplifiers)
        {
            if (amplifiers is null)
            {
                return;
            }

            var toClear = amplifiers.Where(a => a != 0).Distinct().ToArray();
            if (toClear.Length == 0)
            {
                return;
            }

            lock (_lock)
            {
                var emptied = new List<int>();

                foreach (var entry in _ownedByLoco)
                {
                    foreach (var amplifier in toClear)
                    {
                        entry.Value.Remove(amplifier);
                    }

                    if (entry.Value.Count == 0)
                    {
                        emptied.Add(entry.Key);
                    }
                }

                foreach (var locoAddress in emptied)
                {
                    _ownedByLoco.Remove(locoAddress);
                }
            }
        }

        /// <summary>The outstanding non-neutral amplifiers owned by one locomotive, ordered.</summary>
        public IReadOnlyList<ushort> GetOutstanding(int locoAddress)
        {
            lock (_lock)
            {
                return _ownedByLoco.TryGetValue(locoAddress, out var owned)
                    ? owned.OrderBy(a => a).ToArray()
                    : System.Array.Empty<ushort>();
            }
        }

        /// <summary>Every outstanding non-neutral amplifier across all locomotives, ordered.</summary>
        public IReadOnlyList<ushort> GetAllOutstanding()
        {
            lock (_lock)
            {
                var all = new SortedSet<ushort>();
                foreach (var owned in _ownedByLoco.Values)
                {
                    all.UnionWith(owned);
                }

                return all.ToArray();
            }
        }

        /// <summary>True when the locomotive owns at least one outstanding non-neutral target.</summary>
        public bool HasOutstanding(int locoAddress)
        {
            lock (_lock)
            {
                return _ownedByLoco.TryGetValue(locoAddress, out var owned) && owned.Count > 0;
            }
        }

        /// <summary>The locomotives that currently own at least one outstanding target.</summary>
        public IReadOnlyList<int> TrackedLocos
        {
            get
            {
                lock (_lock)
                {
                    return _ownedByLoco.Where(e => e.Value.Count > 0).Select(e => e.Key).OrderBy(a => a).ToArray();
                }
            }
        }

        /// <summary>Forgets one locomotive's outstanding targets (does not command anything).</summary>
        public void Clear(int locoAddress)
        {
            lock (_lock)
            {
                _ownedByLoco.Remove(locoAddress);
            }
        }

        /// <summary>Forgets every outstanding target (does not command anything).</summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _ownedByLoco.Clear();
            }
        }
    }
}
