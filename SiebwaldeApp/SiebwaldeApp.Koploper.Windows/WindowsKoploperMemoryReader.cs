using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Koploper.Windows
{
    /// <summary>
    /// Read-only memory reader over a target process, built on OpenProcess + ReadProcessMemory.
    /// Opens the target with minimal read/query rights and never requests write access.
    /// </summary>
    public sealed class WindowsKoploperMemoryReader : IKoploperMemoryReader
    {
        private SafeProcessHandle? _handle;

        /// <inheritdoc />
        public KoploperMemoryAccessResult Attach(int processId)
        {
            Release();

            SafeProcessHandle handle = NativeMethods.OpenProcess(
                KoploperProcessAccess.MemoryReaderDesiredAccess,
                false,
                processId);

            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                handle.Dispose();
                return ClassifyOpenFailure(error);
            }

            _handle = handle;
            return KoploperMemoryAccessResult.Attached;
        }

        /// <inheritdoc />
        public bool TryReadBytes(nuint address, Span<byte> destination)
        {
            SafeProcessHandle? handle = _handle;
            if (handle is null || handle.IsInvalid)
            {
                return false;
            }

            if (destination.IsEmpty)
            {
                return true;
            }

            byte[] buffer = new byte[destination.Length];
            if (!NativeMethods.ReadProcessMemory(
                    handle,
                    address,
                    buffer,
                    (nuint)buffer.Length,
                    out nuint bytesRead))
            {
                return false;
            }

            // Honor partial reads: the read only succeeds when every requested byte arrived.
            if (bytesRead != (nuint)destination.Length)
            {
                return false;
            }

            buffer.CopyTo(destination);
            return true;
        }

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
            // In a 32-bit target a pointer-sized value is a plain 32-bit word.
            return TryReadUInt32(address, out value);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Release();
        }

        private void Release()
        {
            _handle?.Dispose();
            _handle = null;
        }

        private static KoploperMemoryAccessResult ClassifyOpenFailure(int error)
        {
            // OpenProcess returns ERROR_INVALID_PARAMETER when the pid does not identify a live
            // process, and ERROR_ACCESS_DENIED when it does but access is refused.
            if (error == NativeMethods.ERROR_INVALID_PARAMETER)
            {
                return KoploperMemoryAccessResult.ProcessNotFound;
            }

            if (error == NativeMethods.ERROR_ACCESS_DENIED)
            {
                return KoploperMemoryAccessResult.AccessDenied;
            }

            return KoploperMemoryAccessResult.InvalidParameter;
        }
    }
}
