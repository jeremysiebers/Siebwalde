using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>Outcome of attaching to a target process for read-only memory access.</summary>
    public enum KoploperMemoryAccessResult
    {
        /// <summary>The process was opened for reading successfully.</summary>
        Attached = 0,

        /// <summary>The target process does not exist.</summary>
        ProcessNotFound = 1,

        /// <summary>The target process exists but reading is denied.</summary>
        AccessDenied = 2,

        /// <summary>The supplied process id is not valid.</summary>
        InvalidParameter = 3
    }

    /// <summary>
    /// Thin read-only abstraction over a target process's memory. No domain semantics: every
    /// read is a bounded 32-bit/byte/span copy, and every read fails as <c>false</c> when the
    /// region is not readable.
    /// </summary>
    public interface IKoploperMemoryReader : IDisposable
    {
        /// <summary>Opens the target process for read-only access.</summary>
        KoploperMemoryAccessResult Attach(int processId);

        /// <summary>Reads a 32-bit unsigned value at <paramref name="address"/>.</summary>
        bool TryReadUInt32(nuint address, out uint value);

        /// <summary>Reads a single byte at <paramref name="address"/>.</summary>
        bool TryReadByte(nuint address, out byte value);

        /// <summary>Reads a 32-bit pointer-sized value at <paramref name="address"/>.</summary>
        bool TryReadPointer32(nuint address, out uint value);

        /// <summary>Reads a run of bytes into <paramref name="destination"/>.</summary>
        bool TryReadBytes(nuint address, Span<byte> destination);
    }
}
