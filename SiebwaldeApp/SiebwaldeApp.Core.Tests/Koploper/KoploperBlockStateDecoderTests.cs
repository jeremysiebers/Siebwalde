using System;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for the typed Koploper block-state decoder. These construct
    /// <see cref="KoploperRawRegistry"/> directly (no fake memory reader) and always run the
    /// production <see cref="KoploperBlockStateDecoder"/> — there is no test-only bypass.
    /// </summary>
    public class KoploperBlockStateDecoderTests
    {
        private const uint LocoObjectAddress = 0x00800000;
        private const uint LocoInternalId = 24;

        [Fact]
        public void Decode_RawState0_NoOwner_IsFree()
        {
            var result = Decode(Block(1, 101, owner: 0, state: 0));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Free, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void Decode_RawState1_KnownOwner_IsReserved()
        {
            var result = Decode(Block(1, 101, owner: LocoObjectAddress, state: 1), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Reserved, snapshot.State);
            Assert.Equal((int?)LocoInternalId, snapshot.OwnerLocomotiveId);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void Decode_RawState2_KnownOwner_IsOccupied()
        {
            var result = Decode(Block(1, 101, owner: LocoObjectAddress, state: 2), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Occupied, snapshot.State);
            Assert.Equal((int?)LocoInternalId, snapshot.OwnerLocomotiveId);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void Decode_RawState9_NoOwner_IsTransition()
        {
            var result = Decode(Block(1, 101, owner: 0, state: 9));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Transition, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void Decode_UnknownRawState_IsUnknownWithUnknownBlockState()
        {
            var result = Decode(Block(1, 101, owner: 0, state: 7));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_UNKNOWN_BLOCK_STATE, diagnostic.Code);
        }

        [Fact]
        public void Decode_UnknownRawState_WithOwner_IsUnknownWithUnknownBlockState()
        {
            // The "other" raw row is owner-agnostic: an owner pointer must not change the result.
            var result = Decode(Block(1, 101, owner: LocoObjectAddress, state: 5), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_UNKNOWN_BLOCK_STATE, diagnostic.Code);
        }

        [Fact]
        public void Decode_RawState1_NoOwner_IsUnknownWithOwnerNotFound()
        {
            var result = Decode(Block(1, 101, owner: 0, state: 1), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_OWNER_NOT_FOUND, diagnostic.Code);
        }

        [Fact]
        public void Decode_RawState2_NoOwner_IsUnknownWithOwnerNotFound()
        {
            var result = Decode(Block(1, 101, owner: 0, state: 2), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_OWNER_NOT_FOUND, diagnostic.Code);
        }

        [Fact]
        public void Decode_RawState1_UnresolvedOwner_IsUnknownWithOwnerNotFound()
        {
            const uint unresolvedPointer = 0x00DEAD00;
            var result = Decode(Block(1, 101, owner: unresolvedPointer, state: 1), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_OWNER_NOT_FOUND, diagnostic.Code);
            Assert.Equal(unresolvedPointer, diagnostic.OwnerPointer);
        }

        [Fact]
        public void Decode_RawState2_UnresolvedOwner_IsUnknownWithOwnerNotFound()
        {
            const uint unresolvedPointer = 0x00DEAD00;
            var result = Decode(Block(1, 101, owner: unresolvedPointer, state: 2), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_OWNER_NOT_FOUND, diagnostic.Code);
        }

        [Fact]
        public void Decode_RawState0_WithOwner_IsUnknownWithStateOwnerInconsistent()
        {
            var result = Decode(Block(1, 101, owner: LocoObjectAddress, state: 0), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_STATE_OWNER_INCONSISTENT, diagnostic.Code);
        }

        [Fact]
        public void Decode_RawState9_WithOwner_IsUnknownWithStateOwnerInconsistent()
        {
            var result = Decode(Block(1, 101, owner: LocoObjectAddress, state: 9), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Null(snapshot.OwnerLocomotiveId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_STATE_OWNER_INCONSISTENT, diagnostic.Code);
        }

        [Fact]
        public void Decode_InternalBlockIdAndDisplayId_StaySeparate()
        {
            // PoC06: internal ids are not always the display numbers (e.g. internal 30 -> display 2).
            var result = Decode(Block(30, 2, owner: 0, state: 0));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(30, snapshot.InternalBlockId);
            Assert.Equal((int?)2, snapshot.DisplayBlockNumber);
        }

        [Fact]
        public void Decode_OwnerIdentity_TranslatesPointerToInternalLocoId()
        {
            // The owner pointer is a locomotive object address; the published owner is the loco's
            // internal id, never the pointer itself.
            const uint objectAddress = 0x00BEEF00;
            const uint internalId = 42;

            var registry = new KoploperRawRegistry(
                new[] { Block(1, 101, owner: objectAddress, state: 1) },
                new[] { Loco(objectAddress, internalId) });

            var result = new KoploperBlockStateDecoder().Decode(registry);

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal((int?)internalId, snapshot.OwnerLocomotiveId);
            Assert.NotEqual((int?)objectAddress, snapshot.OwnerLocomotiveId);
        }

        [Fact]
        public void Decode_UnknownRawState_PreservedAsRawValueInSnapshot()
        {
            const byte rawState = 3;
            var result = Decode(Block(1, 101, owner: 0, state: rawState));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Unknown, snapshot.State);
            Assert.Equal(rawState, snapshot.RawState);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(rawState, diagnostic.RawState);
        }

        [Fact]
        public void Decode_DuplicateObjectAddress_FirstWins()
        {
            // Two locomotives sharing an object address: the first registered internal id wins
            // (TryAdd semantics); this never publishes the second id.
            const uint objectAddress = 0x00CAFE00;
            var registry = new KoploperRawRegistry(
                new[] { Block(1, 101, owner: objectAddress, state: 2) },
                new[]
                {
                    Loco(objectAddress, 7),
                    Loco(objectAddress, 99)
                });

            var result = new KoploperBlockStateDecoder().Decode(registry);

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Occupied, snapshot.State);
            Assert.Equal((int?)7, snapshot.OwnerLocomotiveId);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void Decode_PreservesRawStateAndUpdateTick()
        {
            const uint tick = 0x1000;
            var result = Decode(Block(1, 101, owner: LocoObjectAddress, state: 2, tick: tick), Loco());

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(2u, snapshot.RawState);
            Assert.Equal((uint?)tick, snapshot.RawUpdateTick);
        }

        private static KoploperBlockStateDecodeResult Decode(
            KoploperRawBlock block,
            params KoploperRawLocomotive[] locomotives)
        {
            var registry = new KoploperRawRegistry(
                new[] { block },
                locomotives.Length == 0 ? Array.Empty<KoploperRawLocomotive>() : locomotives);
            return new KoploperBlockStateDecoder().Decode(registry);
        }

        private static KoploperRawBlock Block(uint id, uint display, uint owner, byte state, uint tick = 0)
            => new(id, display, owner, state, 0, tick, 0);

        private static KoploperRawLocomotive Loco()
            => Loco(LocoObjectAddress, LocoInternalId);

        private static KoploperRawLocomotive Loco(uint objectAddress, uint internalId)
            => new(internalId, 0, 0, objectAddress);
    }
}
