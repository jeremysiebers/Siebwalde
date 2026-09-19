using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Real occupancy provider: a Koploper block is occupied when any of the track
    /// amplifier sections it covers reports the occupied flag (HoldingReg2 bit 10).
    ///
    /// The block -> amplifier section mapping comes from <see cref="KoploperBlockMap"/>.
    /// </summary>
    public sealed class TrackAmplifierOccupancyProvider : IOccupancyProvider
    {
        private readonly KoploperBlockMap _blockMap;
        private readonly Func<ushort, ushort[]?> _getHoldingRegistersBySection;

        public TrackAmplifierOccupancyProvider(
            KoploperBlockMap blockMap,
            Func<ushort, ushort[]?> getHoldingRegistersBySection)
        {
            _blockMap = blockMap ?? throw new ArgumentNullException(nameof(blockMap));
            _getHoldingRegistersBySection = getHoldingRegistersBySection ?? throw new ArgumentNullException(nameof(getHoldingRegistersBySection));
        }

        public bool IsBlockOccupied(int block)
        {
            if (!_blockMap.TryGetByBlock(block, out var koploperBlock))
            {
                return false;
            }

            foreach (var section in koploperBlock.AmplifierSections)
            {
                if (TrackAmplifierRegisters.IsOccupied(_getHoldingRegistersBySection(section)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
