using System;
using System.Collections.Generic;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// In-memory <see cref="IKoploperMemoryReader"/> backed by a map of base-address to byte
    /// runs. Used by the pure Core decoder/resolver tests to build arbitrary memory layouts
    /// without a live process. Every read fails as <c>false</c> when any part of the requested
    /// range is unmapped, mirroring the real reader's partial-read contract.
    /// </summary>
    internal sealed class FakeKoploperMemoryReader : IKoploperMemoryReader
    {
        private readonly Dictionary<nuint, byte[]> _map = new();

        /// <summary>Writes a little-endian 32-bit value at <paramref name="address"/>.</summary>
        public void SetU32(nuint address, uint value)
        {
            SetBytes(address, BitConverter.GetBytes(value));
        }

        /// <summary>Writes a run of bytes at <paramref name="address"/>.</summary>
        public void SetBytes(nuint address, byte[] bytes)
        {
            _map[address] = bytes;
        }

        /// <inheritdoc />
        public KoploperMemoryAccessResult Attach(int processId) => KoploperMemoryAccessResult.Attached;

        /// <inheritdoc />
        public bool TryReadUInt32(nuint address, out uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            if (!TryReadBytes(address, buffer))
            {
                value = 0;
                return false;
            }

            value = BitConverter.ToUInt32(buffer);
            return true;
        }

        /// <inheritdoc />
        public bool TryReadByte(nuint address, out byte value)
        {
            Span<byte> buffer = stackalloc byte[1];
            if (!TryReadBytes(address, buffer))
            {
                value = 0;
                return false;
            }

            value = buffer[0];
            return true;
        }

        /// <inheritdoc />
        public bool TryReadPointer32(nuint address, out uint value)
        {
            return TryReadUInt32(address, out value);
        }

        /// <inheritdoc />
        public bool TryReadBytes(nuint address, Span<byte> destination)
        {
            if (destination.IsEmpty)
            {
                return true;
            }

            for (int i = 0; i < destination.Length; i++)
            {
                if (!TryReadSingleByte(address + (nuint)i, out byte b))
                {
                    return false;
                }

                destination[i] = b;
            }

            return true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to release; the map is owned by the test.
        }

        private bool TryReadSingleByte(nuint address, out byte value)
        {
            foreach (KeyValuePair<nuint, byte[]> entry in _map)
            {
                nuint start = entry.Key;
                byte[] bytes = entry.Value;

                if (address >= start)
                {
                    nuint offset = address - start;
                    if (offset < (nuint)bytes.Length)
                    {
                        value = bytes[(int)offset];
                        return true;
                    }
                }
            }

            value = 0;
            return false;
        }
    }
}
