using System.Globalization;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.FlashOneConsole;

/// <summary>
/// Minimal production-compatible console adapter for the proven PIC18 single-target flash path
/// (command 131 EXEC_FW_STATE_FLASH_ONE_SLAVE).
///
/// Usage:
///   SiebwaldeApp.FlashOneConsole flash-one --hex &lt;path&gt; --sha256 &lt;hex&gt; --amplifier &lt;1..50&gt;
///
/// The tool is fail-closed: any preflight/validation/flash failure exits non-zero, and it never
/// auto-retries. It reuses the REAL UDP transport (192.168.1.193, ports 10000/10001) and the
/// production TrackCommClientAsync, so it must be run exclusively (the WPF SiebwaldeApp must not
/// be running, and no other controller session may hold the receive port).
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Console-only logger: Core already defaults to a Debug logger; add console visibility.
        IoC.ConfigureLogger(new BaseLogFactory
        {
            LogOutputLevel = LogOutputLevel.Informative,
            IncludeLogOriginDetails = false
        });

        var options = ParseArgs(args);
        if (options.Error is { } parseError)
        {
            PrintUsage(parseError);
            return 2;
        }

        Console.WriteLine($"flash-one: target amplifier {options.Amplifier}");
        Console.WriteLine($"  HEX    : {options.HexPath}");
        Console.WriteLine($"  SHA-256: {options.ExpectedSha256Hex ?? "<not supplied>"}");
        Console.WriteLine();

        var runner = new FlashOneAmplifierRunner(onProgress: PrintProgress);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("Cancellation requested; stopping...");
            cts.Cancel();
        };

        var result = await runner.RunAsync(
            options.HexPath, options.ExpectedSha256Hex, options.Amplifier, cts.Token);

        Console.WriteLine();
        if (result.Success)
        {
            Console.WriteLine($"SUCCESS: amplifier {result.TargetAmplifier} flashed.");
        }
        else
        {
            var subStep = result.FailedSubStep is { } s
                ? $" (FlashSequencer sub-step {s})"
                : string.Empty;
            Console.WriteLine($"FAILED: {result.Error}{subStep}");
        }

        Console.WriteLine($"Exit code {result.ExitCode}.");
        return result.ExitCode;
    }

    private static void PrintProgress(ReceivedMessage m)
        => Console.WriteLine(
            $"  < task={m.TaskId} cmd={m.Taskcommand} state={m.Taskstate} msg={m.Taskmessage}");

    private static FlashOneOptions ParseArgs(string[] args)
    {
        string? hex = null;
        string? sha256 = null;
        byte? amplifier = null;

        var tokens = args.ToList();
        // Accept an optional leading "flash-one" verb.
        if (tokens.Count > 0 && string.Equals(tokens[0], "flash-one", StringComparison.OrdinalIgnoreCase))
            tokens.RemoveAt(0);

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            switch (token.ToLowerInvariant())
            {
                case "--hex":
                case "-h":
                    if (++i >= tokens.Count) return FlashOneOptions.WithError("--hex requires a path.");
                    hex = tokens[i];
                    break;
                case "--sha256":
                case "--sha":
                    if (++i >= tokens.Count) return FlashOneOptions.WithError("--sha256 requires a hex digest.");
                    sha256 = tokens[i];
                    break;
                case "--amplifier":
                case "-a":
                    if (++i >= tokens.Count) return FlashOneOptions.WithError("--amplifier requires a value.");
                    if (!byte.TryParse(tokens[i], NumberStyles.None, CultureInfo.InvariantCulture, out var amp))
                        return FlashOneOptions.WithError($"Invalid amplifier '{tokens[i]}'.");
                    amplifier = amp;
                    break;
                default:
                    return FlashOneOptions.WithError($"Unknown argument '{token}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(hex))
            return FlashOneOptions.WithError("A firmware HEX path (--hex) is required.");

        if (amplifier is null)
            return FlashOneOptions.WithError("A target amplifier (--amplifier 1..50) is required.");

        if (!TrackAmplifierAddress.IsTrackAmplifierAddress(amplifier.Value))
            return FlashOneOptions.WithError(
                $"Amplifier {amplifier.Value} is outside the valid track-amplifier range 1..50.");

        return FlashOneOptions.Success(hex, sha256, amplifier.Value);
    }

    private static void PrintUsage(string? error)
    {
        if (error is not null)
            Console.WriteLine($"ERROR: {error}");

        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SiebwaldeApp.FlashOneConsole flash-one --hex <path> [--sha256 <hex>] --amplifier <1..50>");
        Console.WriteLine();
        Console.WriteLine("The firmware HEX is validated (SHA-256 + embedded checksum) before any write.");
        Console.WriteLine("The tool is fail-closed and never auto-retries. Run it exclusively (no SiebwaldeApp).");
    }
}

internal readonly struct FlashOneOptions
{
    public string HexPath { get; }
    public string? ExpectedSha256Hex { get; }
    public byte Amplifier { get; }
    public string? Error { get; }

    private FlashOneOptions(string hexPath, string? expectedSha256Hex, byte amplifier, string? error)
    {
        HexPath = hexPath;
        ExpectedSha256Hex = expectedSha256Hex;
        Amplifier = amplifier;
        Error = error;
    }

    public static FlashOneOptions Success(string hexPath, string? expectedSha256Hex, byte amplifier)
        => new(hexPath, expectedSha256Hex, amplifier, null);

    public static FlashOneOptions WithError(string error)
        => new(string.Empty, null, 0, error);
}
