using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace SiebwaldeApp.Core
{
    public readonly struct FlashOneOwnershipResult
    {
        public bool Ok { get; }
        public string? Error { get; }

        private FlashOneOwnershipResult(bool ok, string? error)
        {
            Ok = ok;
            Error = error;
        }

        public static FlashOneOwnershipResult Success() => new(true, null);
        public static FlashOneOwnershipResult Fail(string error) => new(false, error);
    }

    /// <summary>Read-only ownership preflight, run before any UDP bind/send.</summary>
    public interface IFlashOneOwnershipPreflight
    {
        FlashOneOwnershipResult Check();
    }

    /// <summary>
    /// Read-only, fail-closed ownership preflight performed BEFORE any UDP bind/send:
    /// (1) the SiebwaldeApp control application must NOT be running (it is the normal single
    ///     owner of the controller session);
    /// (2) the configured host receive port must NOT already be bound by another controller
    ///     session.
    /// Both checks are read-only (process enumeration + the OS UDP listener table) and never open
    /// a socket or transmit a datagram. The definitive exclusivity boundary remains the transport
    /// socket bind: if this heuristic is ever wrong, the bind itself fails closed.
    /// </summary>
    public sealed class FlashOneOwnershipPreflight : IFlashOneOwnershipPreflight
    {
        /// <summary>Assembly name of the WPF control application (SiebwaldeApp.csproj).</summary>
        public const string AppProcessName = "SiebwaldeApp";

        private readonly Func<string, int[]> _getPidsByName;
        private readonly Func<int, bool> _isUdpPortInUse;

        public FlashOneOwnershipPreflight(
            Func<string, int[]>? getPidsByName = null,
            Func<int, bool>? isUdpPortInUse = null)
        {
            _getPidsByName = getPidsByName ?? DefaultGetPidsByName;
            _isUdpPortInUse = isUdpPortInUse ?? DefaultIsUdpPortInUse;
        }

        public FlashOneOwnershipResult Check()
        {
            int[] pids;
            try
            {
                pids = _getPidsByName(AppProcessName);
            }
            catch (Exception ex)
            {
                // Cannot prove exclusivity: fail closed rather than risk a competing session.
                return FlashOneOwnershipResult.Fail(
                    $"Could not enumerate running processes: {ex.Message}");
            }

            if (pids.Length > 0)
            {
                return FlashOneOwnershipResult.Fail(
                    $"The SiebwaldeApp control application is running " +
                    $"(PID {string.Join(", ", pids)}); it owns the controller session. " +
                    "Close it before flashing.");
            }

            var receivePort = CoreConfiguration.TrackControllerReceivingPort;
            if (_isUdpPortInUse(receivePort))
            {
                return FlashOneOwnershipResult.Fail(
                    $"Host receive port {receivePort} is already bound by another controller session.");
            }

            return FlashOneOwnershipResult.Success();
        }

        private static int[] DefaultGetPidsByName(string name)
        {
            var processes = Process.GetProcessesByName(name);
            try
            {
                return processes.Select(p => p.Id).ToArray();
            }
            finally
            {
                foreach (var p in processes) p.Dispose();
            }
        }

        private static bool DefaultIsUdpPortInUse(int port)
        {
            try
            {
                return IPGlobalProperties.GetIPGlobalProperties()
                    .GetActiveUdpListeners()
                    .Any(ep => ep.Port == port);
            }
            catch
            {
                // Read-only probe; on failure we cannot prove exclusivity, so fail closed.
                return true;
            }
        }
    }
}
