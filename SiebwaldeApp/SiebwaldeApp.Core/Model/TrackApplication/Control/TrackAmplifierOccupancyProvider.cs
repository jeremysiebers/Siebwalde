using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Real occupancy provider: a Koploper block is occupied when one of the track amplifier
    /// sections that covers it reports the occupied flag (HoldingReg2 bit 10) from <b>fresh</b>
    /// data.
    ///
    /// This reads exactly the same register value the track-amplifier page already decodes
    /// (<see cref="TrackAmplifierRegisters.OccupiedBit"/> on <see cref="TrackAmplifierItem.HoldingReg"/>),
    /// so there is a single authoritative occupancy representation.
    ///
    /// Freshness: <see cref="TrackAmplifierItem.LastDataReceivedUtc"/> is stamped when a frame is
    /// parsed, and <see cref="TrackAmplifierDataFreshness"/> decides whether that is recent enough.
    /// <see cref="TrackAmplifierItem.SlaveDetected"/> alone is not sufficient: it is never cleared
    /// and the cached container is republished continuously, so it only proves that a frame was
    /// seen at some point in the past.
    ///
    /// The block -> amplifier section mapping comes from <see cref="KoploperBlockMap"/>.
    /// </summary>
    public sealed class TrackAmplifierOccupancyProvider : IOccupancyProvider
    {
        private readonly KoploperBlockMap _blockMap;
        private readonly Func<ushort, TrackAmplifierItem?> _getAmplifierBySection;
        private readonly Func<DateTimeOffset> _clock;
        private readonly TimeSpan _staleAfter;

        public TrackAmplifierOccupancyProvider(
            KoploperBlockMap blockMap,
            Func<ushort, TrackAmplifierItem?> getAmplifierBySection,
            Func<DateTimeOffset>? clock = null,
            TimeSpan? staleAfter = null)
        {
            _blockMap = blockMap ?? throw new ArgumentNullException(nameof(blockMap));
            _getAmplifierBySection = getAmplifierBySection ?? throw new ArgumentNullException(nameof(getAmplifierBySection));
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
            _staleAfter = staleAfter ?? TrackAmplifierDataFreshness.DefaultStaleAfter;
        }

        /// <inheritdoc />
        public bool IsBlockOccupied(int block)
        {
            if (!_blockMap.TryGetByBlock(block, out var koploperBlock))
            {
                return false;
            }

            var now = _clock();

            foreach (var section in koploperBlock.AmplifierSections)
            {
                var amplifier = _getAmplifierBySection(section);

                // Only a fresh occupied reading proves occupancy. A stale reading may describe a
                // train that has already left, so it must not be promoted to definite occupation.
                if (TrackAmplifierDataFreshness.IsCurrentData(amplifier, now, _staleAfter) &&
                    TrackAmplifierRegisters.IsOccupied(amplifier!.HoldingReg))
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

            // One fresh occupied section proves the block is occupied even when another section is
            // silent, so definite occupancy is never turned into unknown.
            if (IsBlockOccupied(block))
            {
                return true;
            }

            var now = _clock();

            // "Clear" can only be claimed when every covering section has current data. A single
            // stale or never-received section makes the whole block unknown.
            foreach (var section in koploperBlock.AmplifierSections)
            {
                if (!TrackAmplifierDataFreshness.IsCurrentData(_getAmplifierBySection(section), now, _staleAfter))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
