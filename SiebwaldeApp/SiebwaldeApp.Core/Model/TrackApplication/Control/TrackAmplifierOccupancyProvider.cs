using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Real occupancy provider: a Koploper block is occupied when any of the track
    /// amplifier sections it covers reports the occupied flag (HoldingReg2 bit 10).
    ///
    /// This reads exactly the same register value the track-amplifier page already decodes
    /// (<see cref="TrackAmplifierRegisters.OccupiedBit"/> on <see cref="TrackAmplifierItem.HoldingReg"/>),
    /// so there is a single authoritative occupancy representation.
    ///
    /// Availability: <see cref="TrackAmplifierItem.SlaveDetected"/> is written in the same step
    /// that stores the holding registers when the master's amplifier frame is parsed, so it is
    /// the existing "valid amplifier data has been received" signal. Until it is set the
    /// occupancy is unknown, not clear.
    ///
    /// The block -> amplifier section mapping comes from <see cref="KoploperBlockMap"/>.
    /// </summary>
    public sealed class TrackAmplifierOccupancyProvider : IOccupancyProvider
    {
        private readonly KoploperBlockMap _blockMap;
        private readonly Func<ushort, TrackAmplifierItem?> _getAmplifierBySection;

        public TrackAmplifierOccupancyProvider(
            KoploperBlockMap blockMap,
            Func<ushort, TrackAmplifierItem?> getAmplifierBySection)
        {
            _blockMap = blockMap ?? throw new ArgumentNullException(nameof(blockMap));
            _getAmplifierBySection = getAmplifierBySection ?? throw new ArgumentNullException(nameof(getAmplifierBySection));
        }

        /// <inheritdoc />
        public bool IsBlockOccupied(int block)
        {
            if (!_blockMap.TryGetByBlock(block, out var koploperBlock))
            {
                return false;
            }

            foreach (var section in koploperBlock.AmplifierSections)
            {
                if (TrackAmplifierRegisters.IsOccupied(_getAmplifierBySection(section)?.HoldingReg))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public bool IsBlockOccupancyKnown(int block)
        {
            if (!_blockMap.TryGetByBlock(block, out var koploperBlock) ||
                koploperBlock.AmplifierSections.Count == 0)
            {
                return false;
            }

            // A block can only be declared clear when every section that covers it is reporting.
            // One silent section is enough to make the whole block unknown.
            foreach (var section in koploperBlock.AmplifierSections)
            {
                var amplifier = _getAmplifierBySection(section);

                if (amplifier is null || amplifier.SlaveDetected == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
