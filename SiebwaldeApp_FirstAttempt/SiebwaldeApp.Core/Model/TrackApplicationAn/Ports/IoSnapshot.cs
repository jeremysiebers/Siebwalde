using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Strongly-typed representation of one PIC UDP frame after parsing.
    /// Keep only what the domain needs (no business rules here).
    /// </summary>
    public sealed class IoSnapshot
    {
        // High-level flags
        public bool TopIncoming { get; init; }
        public bool BottomIncoming { get; init; }

        public bool TopExitFree { get; init; }
        public bool BottomExitFree { get; init; }

        // If you have a separate freight detector per side, keep that too.
        public bool TopIsFreight { get; init; }
        public bool BottomIsFreight { get; init; }

        // Per-track quick view (numbering per your layout)
        public IReadOnlyList<TrackIo> TopTracks { get; init; } = Array.Empty<TrackIo>();
        public IReadOnlyList<TrackIo> BottomTracks { get; init; } = Array.Empty<TrackIo>();

        public static IoSnapshot Empty => new IoSnapshot();

        public sealed record TrackIo(int Number, bool Entry, bool Occupied);

        /// <summary>
        /// Parse PIC UDP payload into IoSnapshot. 
        /// TODO: Replace the dummy mapping with your real byte/bit mapping.
        /// </summary>
        public static IoSnapshot Parse(byte[] b)
        {
            // TODO: Map real bytes/bits. The example below is placeholder.
            // Example assumption:
            // b[0] bit0: TopIncoming, bit1: BottomIncoming
            // b[0] bit2: TopExitFree, bit3: BottomExitFree
            // b[0] bit4: TopIsFreight, bit5: BottomIsFreight
            // Entry/Occupied per track mapped in b[1..] etc.

            bool HasBit(int index, int bit) => (b[index] & (1 << bit)) != 0;

            var topIncoming = b.Length > 0 && HasBit(0, 0);
            var bottomIncoming = b.Length > 0 && HasBit(0, 1);
            var topExitFree = b.Length > 0 && HasBit(0, 2);
            var bottomExitFree = b.Length > 0 && HasBit(0, 3);
            var topIsFreight = b.Length > 0 && HasBit(0, 4);
            var bottomIsFreight = b.Length > 0 && HasBit(0, 5);

            // Example tracks:
            // Top:    10, 11, 12 (middle is 12)
            // Bottom:  1,  2,  3 (middle is 3)
            // Suppose b[1]: Top entries (bits 0..2), b[2]: Top occupied (bits 0..2)
            //         b[3]: Bottom entries,          b[4]: Bottom occupied
            bool SafeHasBit(int idx, int bit) => b.Length > idx && HasBit(idx, bit);

            var topTracks = new[]
            {
                new TrackIo(10, SafeHasBit(1, 0), SafeHasBit(2, 0)),
                new TrackIo(11, SafeHasBit(1, 1), SafeHasBit(2, 1)),
                new TrackIo(12, SafeHasBit(1, 2), SafeHasBit(2, 2))
            };

            var bottomTracks = new[]
            {
                new TrackIo(1, SafeHasBit(3, 0), SafeHasBit(4, 0)),
                new TrackIo(2, SafeHasBit(3, 1), SafeHasBit(4, 1)),
                new TrackIo(3, SafeHasBit(3, 2), SafeHasBit(4, 2))
            };

            return new IoSnapshot
            {
                TopIncoming = topIncoming,
                BottomIncoming = bottomIncoming,
                TopExitFree = topExitFree,
                BottomExitFree = bottomExitFree,
                TopIsFreight = topIsFreight,
                BottomIsFreight = bottomIsFreight,
                TopTracks = topTracks,
                BottomTracks = bottomTracks
            };
        }
    }
}
