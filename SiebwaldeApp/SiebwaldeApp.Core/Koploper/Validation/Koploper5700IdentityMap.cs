using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// An immutable identity map over 5700 records: locomotive id -&gt; most recently reported block.
    /// This is the pure counterpart to the mutable <c>_locToBlock</c> accumulator in the live client.
    /// </summary>
    public sealed class Koploper5700IdentityMap
    {
        private readonly IReadOnlyDictionary<int, int> _locToBlock;

        /// <summary>An empty identity map.</summary>
        public static Koploper5700IdentityMap Empty { get; } = new Koploper5700IdentityMap();

        /// <summary>Creates an empty identity map.</summary>
        public Koploper5700IdentityMap()
        {
            _locToBlock = new Dictionary<int, int>();
        }

        private Koploper5700IdentityMap(IReadOnlyDictionary<int, int> locToBlock)
        {
            _locToBlock = locToBlock;
        }

        /// <summary>The current locomotive-to-block mapping.</summary>
        public IReadOnlyDictionary<int, int> LocToCurrentBlock => _locToBlock;

        /// <summary>Number of locomotives in the map.</summary>
        public int Count => _locToBlock.Count;

        /// <summary>Returns the current block for a locomotive, or <c>null</c> when unknown.</summary>
        public int? TryGetBlock(int locomotiveId)
        {
            return _locToBlock.TryGetValue(locomotiveId, out int block) ? block : null;
        }

        /// <summary>Returns a new identity map with <paramref name="record"/> applied.</summary>
        public Koploper5700IdentityMap Apply(Koploper5700Record record)
        {
            ArgumentNullException.ThrowIfNull(record);

            var next = new Dictionary<int, int>(_locToBlock)
            {
                [record.LocomotiveId] = record.BlockId
            };

            return new Koploper5700IdentityMap(next);
        }

        /// <summary>Builds an identity map from a sequence of records, last record wins per locomotive.</summary>
        public static Koploper5700IdentityMap FromRecords(IEnumerable<Koploper5700Record> records)
        {
            ArgumentNullException.ThrowIfNull(records);

            var accumulator = new Dictionary<int, int>();
            foreach (Koploper5700Record record in records)
            {
                if (record is null)
                {
                    throw new ArgumentException("Records must not contain null elements.", nameof(records));
                }

                accumulator[record.LocomotiveId] = record.BlockId;
            }

            return new Koploper5700IdentityMap(accumulator);
        }
    }
}
