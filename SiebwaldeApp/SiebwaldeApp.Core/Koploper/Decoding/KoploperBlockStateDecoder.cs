using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Typed Koploper block-state decoder. A pure, stateless function of
    /// <see cref="KoploperRawRegistry"/>: it resolves owner pointers against the raw locomotive
    /// registry and applies the validated validation matrix. No memory reader, no P/Invoke, no
    /// process access, no polling, no freshness, no TrackControl and no new offsets.
    /// </summary>
    public sealed class KoploperBlockStateDecoder : IKoploperBlockStateDecoder
    {
        /// <inheritdoc />
        public KoploperBlockStateDecodeResult Decode(KoploperRawRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            // ownerToLocoId: owner pointer (locomotive object address) -> internal locomotive id.
            // First-wins on duplicate addresses; duplicates are tolerated rather than rejected
            // because the raw object-graph decoder already guarantees unique internal ids.
            var ownerToLocoId = new Dictionary<uint, uint>();
            foreach (KoploperRawLocomotive loco in registry.Locomotives)
            {
                ownerToLocoId.TryAdd(loco.ObjectAddress, loco.InternalLocomotiveId);
            }

            var snapshots = new List<KoploperBlockSnapshot>(registry.Blocks.Count);
            var diagnostics = new List<KoploperBlockDiagnostic>();

            foreach (KoploperRawBlock block in registry.Blocks)
            {
                bool absent = block.OwnerPointer == 0;
                uint ownerLocoId = 0;
                bool resolved = !absent && ownerToLocoId.TryGetValue(block.OwnerPointer, out ownerLocoId);

                KoploperBlockState state;
                int? ownerId = null;
                KoploperDiagnosticCode? diagnostic = null;

                switch (block.RawState)
                {
                    case 0:
                        if (absent)
                        {
                            state = KoploperBlockState.Free;
                        }
                        else
                        {
                            state = KoploperBlockState.Unknown;
                            diagnostic = KoploperDiagnosticCode.KOPLOPER_STATE_OWNER_INCONSISTENT;
                        }
                        break;

                    case 1:
                        if (resolved)
                        {
                            state = KoploperBlockState.Reserved;
                            ownerId = (int)ownerLocoId;
                        }
                        else
                        {
                            state = KoploperBlockState.Unknown;
                            diagnostic = KoploperDiagnosticCode.KOPLOPER_OWNER_NOT_FOUND;
                        }
                        break;

                    case 2:
                        if (resolved)
                        {
                            state = KoploperBlockState.Occupied;
                            ownerId = (int)ownerLocoId;
                        }
                        else
                        {
                            state = KoploperBlockState.Unknown;
                            diagnostic = KoploperDiagnosticCode.KOPLOPER_OWNER_NOT_FOUND;
                        }
                        break;

                    case 9:
                        if (absent)
                        {
                            state = KoploperBlockState.Transition;
                        }
                        else
                        {
                            state = KoploperBlockState.Unknown;
                            diagnostic = KoploperDiagnosticCode.KOPLOPER_STATE_OWNER_INCONSISTENT;
                        }
                        break;

                    default:
                        state = KoploperBlockState.Unknown;
                        diagnostic = KoploperDiagnosticCode.KOPLOPER_UNKNOWN_BLOCK_STATE;
                        break;
                }

                snapshots.Add(new KoploperBlockSnapshot(
                    (int)block.InternalBlockId,
                    (int?)block.DisplayBlockNumber,
                    ownerId,
                    state,
                    block.RawState,
                    block.UpdateTick));

                if (diagnostic.HasValue)
                {
                    diagnostics.Add(new KoploperBlockDiagnostic(
                        (int)block.InternalBlockId,
                        diagnostic.Value,
                        block.RawState,
                        block.OwnerPointer));
                }
            }

            return new KoploperBlockStateDecodeResult(snapshots, diagnostics);
        }
    }
}
