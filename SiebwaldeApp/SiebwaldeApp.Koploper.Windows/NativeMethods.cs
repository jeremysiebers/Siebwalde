using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace SiebwaldeApp.Koploper.Windows
{
    /// <summary>
    /// Minimal read-only Win32 surface for the Koploper adapter. Deliberately excludes every
    /// write / injection primitive: no WriteProcessMemory, no VirtualAllocEx, no
    /// CreateRemoteThread, no SetWindowsHookEx, no LoadLibrary. The only callers are the
    /// process locator and the memory reader.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>LIST_MODULES_32BIT filter for K32EnumProcessModulesEx (the target is a 32-bit PE).</summary>
        internal const uint LIST_MODULES_32BIT = 0x01;

        internal const int ERROR_INVALID_PARAMETER = 87;
        internal const int ERROR_ACCESS_DENIED = 5;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeProcessHandle OpenProcess(
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadProcessMemory(
            SafeProcessHandle hProcess,
            nuint lpBaseAddress,
            [Out] byte[] lpBuffer,
            nuint nSize,
            out nuint lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool K32EnumProcessModulesEx(
            SafeProcessHandle hProcess,
            [Out] IntPtr[] lphModule,
            uint cb,
            out uint lpcbNeeded,
            uint dwFilterFlag);

        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetModuleFileNameEx(
            SafeProcessHandle hProcess,
            IntPtr hModule,
            [Out] StringBuilder lpFilename,
            uint nSize);
    }
}
