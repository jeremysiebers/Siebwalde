using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;
using SiebwaldeApp.Core.Koploper;

namespace SiebwaldeApp.Koploper.Windows
{
    /// <summary>
    /// Locates the (32-bit) Koploper process and resolves its run-specific identity: process id,
    /// start time, module base (via 32-bit module enumeration) and executable path. ASLR means
    /// the module base must be resolved per process instance, never hardcoded.
    /// </summary>
    public sealed class WindowsKoploperProcessLocator : IKoploperProcessLocator
    {
        /// <summary>Process name (without extension) used to find Koploper.</summary>
        public const string KoploperProcessName = "koploper";

        /// <inheritdoc />
        public KoploperProcessStatus TryLocate(out KoploperProcessInfo? info)
        {
            info = null;

            Process[] candidates = Process.GetProcessesByName(KoploperProcessName);
            if (candidates.Length == 0)
            {
                return KoploperProcessStatus.NotFound;
            }

            if (candidates.Length > 1)
            {
                foreach (Process candidate in candidates)
                {
                    candidate.Dispose();
                }

                return KoploperProcessStatus.MultipleMatches;
            }

            using Process process = candidates[0];
            try
            {
                DateTimeOffset startTimeUtc = process.StartTime.ToUniversalTime();

                if (!TryResolveModuleInfo(process.Id, out nuint moduleBase, out string moduleFilePath))
                {
                    return KoploperProcessStatus.AccessDenied;
                }

                string executablePath = ResolveExecutablePath(process, moduleFilePath);
                string executableName = ResolveExecutableName(executablePath, process.ProcessName);

                info = new KoploperProcessInfo(
                    process.Id,
                    startTimeUtc,
                    moduleBase,
                    executablePath,
                    executableName);

                return KoploperProcessStatus.Found;
            }
            catch (Exception ex) when (
                ex is Win32Exception ||
                ex is InvalidOperationException ||
                ex is NotSupportedException)
            {
                // The process exited during inspection or the current user cannot open it.
                return KoploperProcessStatus.AccessDenied;
            }
        }

        /// <inheritdoc />
        public bool IsRestart(KoploperProcessInfo? previous, KoploperProcessInfo current)
        {
            return previous is null
                || previous.ProcessId != current.ProcessId
                || previous.ProcessStartTimeUtc != current.ProcessStartTimeUtc;
        }

        private static string ResolveExecutablePath(Process process, string moduleFilePath)
        {
            try
            {
                string? mainModulePath = process.MainModule?.FileName;
                if (!string.IsNullOrEmpty(mainModulePath))
                {
                    return mainModulePath;
                }
            }
            catch
            {
                // MainModule can throw for a 32-bit target or an elevated process; fall back to
                // the module-file API result obtained during module enumeration.
            }

            return moduleFilePath;
        }

        private static string ResolveExecutableName(string executablePath, string processName)
        {
            string? fileName = Path.GetFileName(executablePath);
            return string.IsNullOrEmpty(fileName) ? processName : fileName;
        }

        private static bool TryResolveModuleInfo(int processId, out nuint moduleBase, out string moduleFilePath)
        {
            moduleBase = 0;
            moduleFilePath = string.Empty;

            using SafeProcessHandle handle = NativeMethods.OpenProcess(
                KoploperProcessAccess.LocatorDesiredAccess,
                false,
                processId);

            if (handle.IsInvalid)
            {
                return false;
            }

            // The target is a 32-bit PE; enumerate its modules with the 32-bit filter so the
            // list is correct even when this adapter runs as a 64-bit process.
            IntPtr[] modules = new IntPtr[1024];
            if (!NativeMethods.K32EnumProcessModulesEx(
                    handle,
                    modules,
                    (uint)(modules.Length * IntPtr.Size),
                    out uint bytesNeeded,
                    NativeMethods.LIST_MODULES_32BIT))
            {
                return false;
            }

            int moduleCount = (int)(bytesNeeded / (uint)IntPtr.Size);
            if (moduleCount <= 0 || modules[0] == IntPtr.Zero)
            {
                return false;
            }

            // The first module is the executable image; its handle is the image base address.
            moduleBase = (nuint)modules[0];

            StringBuilder pathBuffer = new StringBuilder(1024);
            uint length = NativeMethods.GetModuleFileNameEx(
                handle,
                modules[0],
                pathBuffer,
                (uint)pathBuffer.Capacity);

            if (length > 0)
            {
                moduleFilePath = pathBuffer.ToString();
            }

            return true;
        }
    }
}
