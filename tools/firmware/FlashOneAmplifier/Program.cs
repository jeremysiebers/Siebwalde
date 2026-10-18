using System;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Comm;

namespace FlashOneAmplifier
{
    /// <summary>
    /// Standalone CLI that flashes exactly ONE track amplifier through the existing PIC32
    /// firmware-flash wire path. It reuses <see cref="TrackCommClientAsync"/>,
    /// <see cref="RawUdpTrackTransport"/>, <see cref="TrackAmplifierBootloaderHelpers"/> and
    /// <see cref="SendNextFwDataPacket"/>.
    ///
    /// It never sends EXEC_FW_STATE_FLASH_ALL_SLAVES (122). The single amplifier address is
    /// carried as the data byte of EXEC_FW_STATE_FLASH_ONE_SLAVE (131).
    /// </summary>
    internal static class Program
    {
        private const string DefaultIpAddress = "192.168.1.193";
        private const int DefaultSendPort = 10000;
        private const int DefaultReceivePort = 10001;

        private static async Task<int> Main(string[] args)
        {
            if (!TryParseArgs(args, out var options, out var error))
            {
                Console.Error.WriteLine($"Error: {error}");
                PrintUsage();
                return 2;
            }

            // 1. Offline validation (SHA-256 + embedded checksum) BEFORE any write is attempted.
            var validation = FirmwareArtifactVerifier.Validate(options.HexPath, options.Sha256Hex);
            if (!validation.Success)
            {
                Console.Error.WriteLine($"Refusing to flash (no write performed): {validation.Error}");
                return 2;
            }

            var artifact = validation.Artifact!;
            Console.WriteLine($"Firmware SHA-256            : {artifact.Sha256Hex}");
            Console.WriteLine($"Firmware checksum           : embedded 0x{artifact.EmbeddedChecksum:X4}, recomputed 0x{artifact.RecomputedChecksum:X4}");

            // 2. Compose the real UDP transport + comm client.
            var variables = new TrackApplicationVariables();
            var transport = new RawUdpTrackTransport(
                new RawUdpTransport(options.IpAddress, options.SendPort, options.ReceivePort));
            await using var commClient = new TrackCommClientAsync(transport, variables);

            await commClient.StartAsync(realHardwareMode: true).ConfigureAwait(false);

            try
            {
                var sendNextFwDataPacket = new SendNextFwDataPacket(commClient, artifact.Helpers);
                var command = new FlashOneAmplifierCommand(
                    commClient, variables, artifact.Helpers, sendNextFwDataPacket);

                Console.WriteLine($"Flashing amplifier {options.Amplifier} via EXEC_FW_STATE_FLASH_ONE_SLAVE (131).");
                var result = await command.ExecuteAsync((byte)options.Amplifier).ConfigureAwait(false);

                if (result.Success)
                {
                    Console.WriteLine($"Amplifier {result.TargetAmplifier} flashed successfully (DONE).");
                    return 0;
                }

                Console.Error.WriteLine($"Flash FAILED for amplifier {result.TargetAmplifier}: {result.Error}");
                return 1;
            }
            finally
            {
                await commClient.StopAsync().ConfigureAwait(false);
            }
        }

        private static bool TryParseArgs(string[] args, out CliOptions options, out string error)
        {
            options = new CliOptions();
            error = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--amp":
                        if (!TryReadValue(args, ref i, out var ampText) || !int.TryParse(ampText, out var amp))
                        {
                            error = "Missing or invalid value for --amp (expected 1..50).";
                            return false;
                        }
                        options.Amplifier = amp;
                        break;

                    case "--hex":
                        if (!TryReadValue(args, ref i, out var hex))
                        {
                            error = "Missing value for --hex (expected a path to an Intel HEX file).";
                            return false;
                        }
                        options.HexPath = hex;
                        break;

                    case "--sha256":
                        if (!TryReadValue(args, ref i, out var sha256))
                        {
                            error = "Missing value for --sha256 (expected a 64-char hex digest).";
                            return false;
                        }
                        options.Sha256Hex = sha256;
                        break;

                    case "--ip":
                        if (!TryReadValue(args, ref i, out var ip))
                        {
                            error = "Missing value for --ip.";
                            return false;
                        }
                        options.IpAddress = ip;
                        break;

                    case "--send-port":
                        if (!TryReadValue(args, ref i, out var sendPort) || !int.TryParse(sendPort, out var sp))
                        {
                            error = "Missing or invalid value for --send-port.";
                            return false;
                        }
                        options.SendPort = sp;
                        break;

                    case "--recv-port":
                        if (!TryReadValue(args, ref i, out var recvPort) || !int.TryParse(recvPort, out var rp))
                        {
                            error = "Missing or invalid value for --recv-port.";
                            return false;
                        }
                        options.ReceivePort = rp;
                        break;

                    case "-h":
                    case "--help":
                        error = "help";
                        return false;

                    default:
                        error = $"Unknown argument: {args[i]}";
                        return false;
                }
            }

            if (options.Amplifier < TrackAmplifierAddress.MinTrackAmplifier ||
                options.Amplifier > TrackAmplifierAddress.MaxTrackAmplifier)
            {
                error = $"--amp must be in range {TrackAmplifierAddress.MinTrackAmplifier}..{TrackAmplifierAddress.MaxTrackAmplifier}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.HexPath))
            {
                error = "--hex <path> is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.Sha256Hex))
            {
                error = "--sha256 <expected> is required (exact SHA-256 identity before any write).";
                return false;
            }

            return true;
        }

        private static bool TryReadValue(string[] args, ref int i, out string value)
        {
            value = string.Empty;
            if (i + 1 >= args.Length)
                return false;
            value = args[++i];
            return true;
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine(
                "Usage: FlashOneAmplifier --amp <1..50> --hex <path> --sha256 <expected> " +
                "[--ip <ip>] [--send-port <port>] [--recv-port <port>]");
            Console.Error.WriteLine($"Defaults: --ip {DefaultIpAddress} --send-port {DefaultSendPort} --recv-port {DefaultReceivePort}");
        }

        private sealed class CliOptions
        {
            public int Amplifier { get; set; }
            public string HexPath { get; set; } = string.Empty;
            public string Sha256Hex { get; set; } = string.Empty;
            public string IpAddress { get; set; } = DefaultIpAddress;
            public int SendPort { get; set; } = DefaultSendPort;
            public int ReceivePort { get; set; } = DefaultReceivePort;
        }
    }
}
