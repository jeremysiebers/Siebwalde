using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>Kind of external host that the application can detect.</summary>
    public enum HostKind
    {
        FiddleYard,
        TrackController,
        Koploper
    }

    /// <summary>Result of a single host presence check.</summary>
    public sealed class HostDetectionResult
    {
        public HostKind Kind { get; init; }
        public string DisplayName { get; init; } = "";
        public string Target { get; init; } = "";
        public bool IsPresent { get; init; }
        public string Detail { get; init; } = "";
    }

    /// <summary>
    /// Detects whether the expected layout hosts are reachable.
    /// FiddleYard and the TrackController (PIC32 Ethernet ModBus master) are detected with a ping
    /// on their host name/IP. Koploper is detected with a TCP connect probe.
    /// </summary>
    public static class HostDetection
    {
        public const string FiddleYardHostName = "FIDDLEYARD";
        public const string KoploperHostName = "127.0.0.1";
        public const int KoploperPort = 5700;

        public const int DefaultPingTimeoutMs = 1000;
        public const int DefaultTcpTimeoutMs = 1000;

        public static Task<HostDetectionResult> DetectFiddleYardAsync(int timeoutMs = DefaultPingTimeoutMs)
            => DetectByPingAsync(HostKind.FiddleYard, "Fiddle Yard", FiddleYardHostName, timeoutMs);

        public static Task<HostDetectionResult> DetectTrackControllerAsync(int timeoutMs = DefaultPingTimeoutMs)
            => DetectByPingAsync(HostKind.TrackController, "TrackController", CoreConfiguration.TrackControllerIpAddress, timeoutMs);

        public static Task<HostDetectionResult> DetectKoploperAsync(int timeoutMs = DefaultTcpTimeoutMs)
            => DetectByTcpAsync(HostKind.Koploper, "Koploper", KoploperHostName, KoploperPort, timeoutMs);

        private static async Task<HostDetectionResult> DetectByPingAsync(
            HostKind kind,
            string displayName,
            string host,
            int timeoutMs)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, timeoutMs).ConfigureAwait(false);
                var present = reply.Status == IPStatus.Success;

                return new HostDetectionResult
                {
                    Kind = kind,
                    DisplayName = displayName,
                    Target = host,
                    IsPresent = present,
                    Detail = present ? $"ping OK ({reply.RoundtripTime} ms)" : $"ping {reply.Status}"
                };
            }
            catch (Exception ex)
            {
                return new HostDetectionResult
                {
                    Kind = kind,
                    DisplayName = displayName,
                    Target = host,
                    IsPresent = false,
                    Detail = $"ping failed: {ex.Message}"
                };
            }
        }

        private static async Task<HostDetectionResult> DetectByTcpAsync(
            HostKind kind,
            string displayName,
            string host,
            int port,
            int timeoutMs)
        {
            var target = $"{host}:{port}";

            try
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(timeoutMs);
                await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);

                return new HostDetectionResult
                {
                    Kind = kind,
                    DisplayName = displayName,
                    Target = target,
                    IsPresent = true,
                    Detail = "TCP connect OK"
                };
            }
            catch (Exception ex)
            {
                return new HostDetectionResult
                {
                    Kind = kind,
                    DisplayName = displayName,
                    Target = target,
                    IsPresent = false,
                    Detail = $"TCP connect failed: {ex.Message}"
                };
            }
        }
    }
}
