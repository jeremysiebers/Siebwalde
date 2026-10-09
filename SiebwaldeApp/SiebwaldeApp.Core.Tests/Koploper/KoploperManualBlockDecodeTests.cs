using System.Linq;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for the manual-blocked dimension of the typed block-state decoder: raw byte
    /// 0/1/other at +0x198, the Invalid diagnostic, and that the manual dimension never alters the
    /// automatic state derivation.
    /// </summary>
    public class KoploperManualBlockDecodeTests
    {
        [Fact]
        public void Decode_ManualBlockedRaw0_IsNotBlocked()
        {
            var result = Decode(Block(id: 1, state: 0, owner: 0, manualBlocked: 0));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperManualBlockState.NotBlocked, snapshot.ManualBlocked);
            Assert.DoesNotContain(result.Diagnostics, d => d.Code == KoploperDiagnosticCode.KOPLOPER_MANUAL_BLOCK_INVALID);
        }

        [Fact]
        public void Decode_ManualBlockedRaw1_IsBlocked()
        {
            var result = Decode(Block(id: 1, state: 0, owner: 0, manualBlocked: 1));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperManualBlockState.Blocked, snapshot.ManualBlocked);
            Assert.Empty(result.Diagnostics);
        }

        [Fact]
        public void Decode_ManualBlockedRawOther_IsInvalidWithDiagnostic()
        {
            var result = Decode(Block(id: 1, state: 0, owner: 0, manualBlocked: 2));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperManualBlockState.Invalid, snapshot.ManualBlocked);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(KoploperDiagnosticCode.KOPLOPER_MANUAL_BLOCK_INVALID, diagnostic.Code);
        }

        [Fact]
        public void Decode_ManualInvalidDiagnostic_PreservesRawStateAndOwnerPointer()
        {
            const uint owner = 0x00BEEF00;
            var result = Decode(Block(id: 7, state: 3, owner: owner, manualBlocked: 9));

            var manual = Assert.Single(result.Diagnostics, d => d.Code == KoploperDiagnosticCode.KOPLOPER_MANUAL_BLOCK_INVALID);
            Assert.Equal(7, manual.InternalBlockId);
            Assert.Equal((uint)3, manual.RawState);
            Assert.Equal(owner, manual.OwnerPointer);
        }

        [Fact]
        public void Decode_ManualBlocked_DoesNotAlterAutomaticState()
        {
            // A manually blocked occupied block is still occupied; the manual dimension is orthogonal.
            const uint locoObjectAddress = 0x00800000;
            var result = Decode(Block(id: 1, state: 2, owner: locoObjectAddress, manualBlocked: 1), Loco(locoObjectAddress, 24));

            var snapshot = Assert.Single(result.Snapshots);
            Assert.Equal(KoploperBlockState.Occupied, snapshot.State);
            Assert.Equal((int?)24, snapshot.OwnerLocomotiveId);
            Assert.Equal(KoploperManualBlockState.Blocked, snapshot.ManualBlocked);
            Assert.Empty(result.Diagnostics);
        }

        private static KoploperBlockStateDecodeResult Decode(KoploperRawBlock block, params KoploperRawLocomotive[] locomotives)
        {
            var registry = new KoploperRawRegistry(new[] { block }, locomotives);
            return new KoploperBlockStateDecoder().Decode(registry);
        }

        private static KoploperRawBlock Block(uint id, byte state, uint owner, byte manualBlocked, uint tick = 0)
            => new(id, id, owner, state, 0, tick, manualBlocked);

        private static KoploperRawLocomotive Loco(uint objectAddress, uint internalId)
            => new(internalId, 0, 0, objectAddress);
    }
}
