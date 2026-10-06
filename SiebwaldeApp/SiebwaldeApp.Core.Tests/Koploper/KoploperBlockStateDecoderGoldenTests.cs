using System.Linq;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Golden evidence tests encoding the PROVEN PoC05/PoC06 semantics from the technical handoff
    /// (<c>docs/koploper-internal-state-integration.md</c> §6.4 and PoC05) as deterministic,
    /// hard-coded fixtures. No CSV is parsed at runtime. Each fixture pins one documented
    /// property so a future regression in the validation matrix fails here.
    /// </summary>
    public class KoploperBlockStateDecoderGoldenTests
    {
        // PoC05 demo config: 30 blocks, 3 locomotives (internal ids 2, 8, 24). Loco 24 is the one
        // documented as holding multiple future reserved blocks at once.
        private const uint Loco24ObjectAddress = 0x00800000;
        private const uint Loco24InternalId = 24;
        private const uint Loco2ObjectAddress = 0x00800200;
        private const uint Loco2InternalId = 2;

        [Fact]
        public void Golden_StateCycle_DecodesFreeReservedOccupiedTransitionFree()
        {
            // Handoff §6.4: the normally observed cycle is 0 -> 1 -> 2 -> 9 -> 0, which must
            // decode as Free -> Reserved -> Occupied -> Transition -> Free. Transition (9) is
            // never coerced to Free; each state value is validated, not guessed.
            var registry = Registry(
                new[]
                {
                    Block(1, state: 0, owner: 0),                              // Free
                    Block(2, state: 1, owner: Loco24ObjectAddress),            // Reserved (loco 24)
                    Block(3, state: 2, owner: Loco24ObjectAddress),            // Occupied (loco 24)
                    Block(4, state: 9, owner: 0),                              // Transition
                    Block(5, state: 0, owner: 0),                              // Free
                },
                Loco(Loco24ObjectAddress, Loco24InternalId));

            var result = new KoploperBlockStateDecoder().Decode(registry);

            Assert.Equal(5, result.Snapshots.Count);
            Assert.Equal(
                new[]
                {
                    KoploperBlockState.Free,
                    KoploperBlockState.Reserved,
                    KoploperBlockState.Occupied,
                    KoploperBlockState.Transition,
                    KoploperBlockState.Free,
                },
                result.Snapshots.Select(s => s.State).ToArray());
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void Golden_StateOwnerPairing_MatchesPoC05Observation()
        {
            // PoC05: every observed state-1 (reserved) and state-2 (occupied) record had a valid
            // owner; every observed state-0 (free) and state-9 (transition) record had no owner.
            var registry = Registry(
                new[]
                {
                    Block(1, state: 2, owner: Loco2ObjectAddress),             // occupied, owner loco 2
                    Block(2, state: 1, owner: Loco2ObjectAddress),             // reserved, owner loco 2
                    Block(3, state: 0, owner: 0),                              // free, no owner
                    Block(4, state: 9, owner: 0),                              // transition, no owner
                    Block(5, state: 1, owner: Loco24ObjectAddress),            // reserved, owner loco 24
                },
                Loco(Loco2ObjectAddress, Loco2InternalId),
                Loco(Loco24ObjectAddress, Loco24InternalId));

            var result = new KoploperBlockStateDecoder().Decode(registry);

            Assert.Empty(result.Diagnostics);

            // State 1/2 snapshots publish the loco's internal id (valid owner).
            Assert.Equal((int?)Loco2InternalId, result.Snapshots[0].OwnerLocomotiveId);
            Assert.Equal((int?)Loco2InternalId, result.Snapshots[1].OwnerLocomotiveId);
            Assert.Equal((int?)Loco24InternalId, result.Snapshots[4].OwnerLocomotiveId);

            // State 0/9 snapshots publish no owner.
            Assert.Null(result.Snapshots[2].OwnerLocomotiveId);
            Assert.Null(result.Snapshots[3].OwnerLocomotiveId);
        }

        [Fact]
        public void Golden_MultipleFutureReservedBlocks_PerLoco()
        {
            // PoC05 ("multiple future reserved blocks per loc"): loco 24 occupied block 17 while
            // simultaneously reserving blocks 19 and 20. Multiple Reserved snapshots may point at
            // the same internal loco id; the published id is the loco id, not the owner pointer.
            var registry = Registry(
                new[]
                {
                    Block(17, state: 2, owner: Loco24ObjectAddress),           // Occupied
                    Block(19, state: 1, owner: Loco24ObjectAddress),           // Reserved
                    Block(20, state: 1, owner: Loco24ObjectAddress),           // Reserved
                },
                Loco(Loco24ObjectAddress, Loco24InternalId));

            var result = new KoploperBlockStateDecoder().Decode(registry);

            Assert.Empty(result.Diagnostics);
            Assert.Equal(KoploperBlockState.Occupied, result.Snapshots[0].State);
            Assert.Equal(KoploperBlockState.Reserved, result.Snapshots[1].State);
            Assert.Equal(KoploperBlockState.Reserved, result.Snapshots[2].State);

            // One loco id across the occupied + two reserved blocks; the id is the internal id.
            Assert.All(result.Snapshots, s => Assert.Equal((int?)Loco24InternalId, s.OwnerLocomotiveId));
        }

        private static KoploperRawRegistry Registry(
            KoploperRawBlock[] blocks,
            params KoploperRawLocomotive[] locomotives)
            => new(blocks, locomotives);

        private static KoploperRawBlock Block(uint id, byte state, uint owner, uint tick = 0)
            => new(id, id, owner, state, 0, tick);

        private static KoploperRawLocomotive Loco(uint objectAddress, uint internalId)
            => new(internalId, 0, 0, objectAddress);
    }
}
