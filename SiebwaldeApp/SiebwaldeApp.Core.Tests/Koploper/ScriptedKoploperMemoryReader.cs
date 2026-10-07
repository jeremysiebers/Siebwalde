using System;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// An <see cref="IKoploperMemoryReader"/> that wraps a <see cref="FakeKoploperMemoryReader"/>
    /// and applies deterministic mutations at the boundary between two object-graph walks. The
    /// watch address (the root-pointer cell, read exactly once per <see cref="IKoploperRawObjectGraphReader.Read"/>)
    /// is used to detect the start of each walk; a mutation is applied at the start of every
    /// second walk (the "PASS B" side of a coherent-read pair). This makes torn reads fully
    /// deterministic without touching production code.
    /// </summary>
    internal sealed class ScriptedKoploperMemoryReader : IKoploperMemoryReader
    {
        private readonly FakeKoploperMemoryReader _inner = new();
        private readonly nuint _watchAddress;
        private readonly System.Collections.Generic.Queue<Action<FakeKoploperMemoryReader>> _pending = new();
        private Action<FakeKoploperMemoryReader>? _persistent;
        private int _watchHits;

        public ScriptedKoploperMemoryReader(nuint watchAddress)
        {
            _watchAddress = watchAddress;
        }

        /// <summary>The underlying map, used to build the memory layout before reading.</summary>
        public FakeKoploperMemoryReader Inner => _inner;

        /// <summary>Enqueues a one-shot mutation, applied at the next PASS-B boundary.</summary>
        public void EnqueueMutation(Action<FakeKoploperMemoryReader> mutation) => _pending.Enqueue(mutation);

        /// <summary>Sets a mutation applied at every PASS-B boundary (for persistent tearing).</summary>
        public void SetPersistentMutation(Action<FakeKoploperMemoryReader> mutation) => _persistent = mutation;

        /// <summary>Number of times the watch address was read (== number of graph walks).</summary>
        public int WatchHits => _watchHits;

        /// <inheritdoc />
        public KoploperMemoryAccessResult Attach(int processId) => _inner.Attach(processId);

        /// <inheritdoc />
        public bool TryReadUInt32(nuint address, out uint value)
        {
            MaybeMutate(address);
            return _inner.TryReadUInt32(address, out value);
        }

        /// <inheritdoc />
        public bool TryReadByte(nuint address, out byte value) => _inner.TryReadByte(address, out value);

        /// <inheritdoc />
        public bool TryReadPointer32(nuint address, out uint value)
        {
            MaybeMutate(address);
            return _inner.TryReadPointer32(address, out value);
        }

        /// <inheritdoc />
        public bool TryReadBytes(nuint address, Span<byte> destination) => _inner.TryReadBytes(address, destination);

        /// <inheritdoc />
        public void Dispose() => _inner.Dispose();

        private void MaybeMutate(nuint address)
        {
            if (address != _watchAddress)
            {
                return;
            }

            _watchHits++;

            // Even hits mark the start of the second walk of a pair (PASS B). Odd hits are the
            // first walk (PASS A), which must observe the pre-boundary state.
            if ((_watchHits % 2) != 0)
            {
                return;
            }

            if (_persistent is not null)
            {
                _persistent(_inner);
            }
            else if (_pending.Count > 0)
            {
                _pending.Dequeue()(_inner);
            }
        }
    }
}
