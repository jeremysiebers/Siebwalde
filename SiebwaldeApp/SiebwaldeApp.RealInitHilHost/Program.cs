using System.Text;
using System.Text.Json;

namespace SiebwaldeApp.RealInitHilHost
{
    /// <summary>
    /// Headless, minimal hardware-in-the-loop (HIL) entrypoint for the REAL production
    /// initialization chain (TrackControlMode.Real, real UDP transport) through GLOBAL READY only.
    ///
    /// Usage:
    ///   SiebwaldeApp.RealInitHilHost hil-a [--jsonl &lt;path&gt;] [--timeout &lt;seconds&gt;]
    ///   SiebwaldeApp.RealInitHilHost hil-b [--jsonl &lt;path&gt;] [--domain 1,3,4,6] [--timeout &lt;seconds&gt;]
    ///   SiebwaldeApp.RealInitHilHost hil-c [--jsonl &lt;path&gt;] [--domain 1,3,4,6] [--timeout &lt;seconds&gt;]
    ///                       [--poll-interval &lt;seconds&gt;] [--max-duration &lt;seconds&gt;] [--stop-file &lt;path&gt;]
    ///   SiebwaldeApp.RealInitHilHost hil-d [--jsonl &lt;path&gt;] [--domain 1,3,4,6] [--timeout &lt;seconds&gt;]
    ///                       [--max-duration &lt;seconds&gt;] [--stop-file &lt;path&gt;] [--continue-on-fail]
    ///
    /// hil-a is passive diagnostics (read-only). hil-b composes the REAL production runtime host and
    /// runs the conditional Stage 1-5 init once, asserting the expected terminal state. hil-c does the
    /// same init once, then HOLDS the session (single-session READY endurance — NOT a repeated-init
    /// test) and monitors for up to --max-duration (default 8 h). hil-d is the REARM + repeated
    /// Stage 1-5 init campaign loop: it re-arms (112) ONLY a proven host-loss latch and fails closed
    /// on a host-initiated/EMO/unknown latch. With --continue-on-fail, hil-d instead counts each
    /// recoverable init failure and CONTINUES the soak (never re-arming an unexpected/unsafe
    /// global-stop state), stopping only on unexpected/unsafe conditions. Exit code 0 on pass,
    /// non-zero on fail. No subcommand flashes firmware or energizes track power.
    /// </summary>
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string? subcommand = null;
            string? jsonlPath = null;
            var domain = new[] { 1, 3, 4, 6 };
            var timeout = TimeSpan.FromSeconds(60);
            var pollInterval = TimeSpan.FromSeconds(30);
            var maxDuration = TimeSpan.FromHours(8);
            string? stopFilePath = null;
            bool continueOnFail = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--jsonl" when i + 1 < args.Length:
                        jsonlPath = args[++i];
                        break;
                    case "--domain" when i + 1 < args.Length:
                        domain = ParseDomain(args[++i]);
                        break;
                    case "--timeout" when i + 1 < args.Length:
                        timeout = ParseTimeout(args[++i]);
                        break;
                    case "--poll-interval" when i + 1 < args.Length:
                        pollInterval = ParseTimeout(args[++i]);
                        break;
                    case "--max-duration" when i + 1 < args.Length:
                        maxDuration = ParseTimeout(args[++i]);
                        break;
                    case "--stop-file" when i + 1 < args.Length:
                        stopFilePath = args[++i];
                        break;
                    case "--continue-on-fail":
                        continueOnFail = true;
                        break;
                    case "hil-a":
                    case "hil-b":
                    case "hil-c":
                    case "hil-d":
                        subcommand = args[i];
                        break;
                    case "--help":
                    case "-h":
                        PrintUsage();
                        return 2;
                    default:
                        Console.Error.WriteLine($"Unknown argument: {args[i]}");
                        PrintUsage();
                        return 2;
                }
            }

            if (subcommand is null)
            {
                Console.Error.WriteLine("Missing required subcommand (hil-a | hil-b | hil-c | hil-d).");
                PrintUsage();
                return 2;
            }

            // Ctrl+C is the operator STOP signal for the long-running hil-c hold and the hil-d
            // repeated-init campaign: defer termination and cancel the run cleanly. For hil-a/hil-b
            // the default terminate-on-Ctrl+C is kept.
            using var operatorStop = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                if (subcommand == "hil-c" || subcommand == "hil-d")
                {
                    e.Cancel = true;
                    operatorStop.Cancel();
                }
            };

            try
            {
                HilResult result = subcommand switch
                {
                    "hil-a" => await HilScenarios.RunHilAAsync(timeout).ConfigureAwait(false),
                    "hil-b" => await HilScenarios.RunHilBAsync(domain, timeout).ConfigureAwait(false),
                    "hil-c" => await HilScenarios.RunHilCAsync(
                        domain, timeout, pollInterval, maxDuration, jsonlPath, stopFilePath, operatorStop.Token).ConfigureAwait(false),
                    "hil-d" => await HilScenarios.RunHilDAsync(
                        domain, timeout, maxDuration, jsonlPath, stopFilePath, operatorStop.Token, continueOnFail).ConfigureAwait(false),
                    _ => throw new ArgumentException($"Unknown subcommand '{subcommand}'."),
                };

                PrintSummary(result);

                // For hil-c/hil-d the --jsonl path is written by the runner (per-poll samples for
                // hil-c, per-cycle lines + summary for hil-d); the HilResult is printed to the console
                // only, so the JSONL file keeps a single JSON shape.
                if (jsonlPath is not null && subcommand != "hil-c" && subcommand != "hil-d")
                {
                    await WriteJsonlAsync(jsonlPath, result).ConfigureAwait(false);
                }

                return result.Pass ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"HIL ERROR: {ex}");
                return 1;
            }
        }

        private static int[] ParseDomain(string value)
        {
            var parsed = value
                .Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToArray();

            if (parsed.Length == 0)
            {
                throw new ArgumentException("--domain must contain at least one address.");
            }

            return parsed;
        }

        private static TimeSpan ParseTimeout(string value)
        {
            if (!int.TryParse(value, out var seconds) || seconds <= 0)
            {
                throw new ArgumentException("--timeout must be a positive integer number of seconds.");
            }

            return TimeSpan.FromSeconds(seconds);
        }

        private static void PrintSummary(HilResult result)
        {
            Console.WriteLine();
            Console.WriteLine($"=== {result.Subcommand}: {(result.Pass ? "PASS" : "FAIL")} ===");

            if (result.ControllerVersion is not null)
            {
                Console.WriteLine($"  ControllerVersion      = {result.ControllerVersion}");
            }

            if (result.GlobalStopState is not null)
            {
                Console.WriteLine($"  GlobalStopState        = {result.GlobalStopState}");
            }

            if (result.RuntimeState.Length > 0)
            {
                Console.WriteLine($"  RuntimeState           = {result.RuntimeState}");
            }

            if (result.RunStatus.Length > 0)
            {
                Console.WriteLine($"  RunStatus              = {result.RunStatus}");
            }

            if (result.MovementPermission.Length > 0)
            {
                Console.WriteLine($"  MovementPermission     = {result.MovementPermission}");
            }

            if (result.Stages.Count > 0)
            {
                Console.WriteLine($"  TrackPowerOn           = {result.TrackPowerOn}");
                Console.WriteLine($"  FirmwareDeployAuth     = {result.FirmwareDeploymentAuthorized}");
                Console.WriteLine("  Stages:");
                foreach (var kv in result.Stages)
                {
                    Console.WriteLine($"    {kv.Key,-22} = {kv.Value}");
                }
            }

            foreach (var check in result.Checks)
            {
                Console.WriteLine($"  {(check.Pass ? "PASS" : "FAIL")}  {check.Name}: {check.Detail}");
            }

            if (result.Amps.Count > 0)
            {
                Console.WriteLine("  Amplifiers:");
                foreach (var amp in result.Amps)
                {
                    Console.WriteLine(
                        $"    amp{amp.Slave,-3} detected={amp.SlaveDetected} " +
                        $"configVerified={amp.ConfigurationVerified} " +
                        $"phase1Failure={(amp.Phase1Failure ?? "<null>")} " +
                        $"HR7={amp.Hr7} HR11={amp.Hr11} HR8={amp.Hr8} HR21={amp.Hr21} HR22={amp.Hr22} " +
                        $"(protocol={amp.Hr22Protocol})");
                }
            }

            if (result.Faults.Count > 0)
            {
                Console.WriteLine($"  Faults ({result.Faults.Count}):");
                foreach (var fault in result.Faults)
                {
                    Console.WriteLine($"    - {fault}");
                }
            }

            Console.WriteLine($"  RESULT: {(result.Pass ? "PASS" : "FAIL")}");
        }

        private static async Task WriteJsonlAsync(string path, HilResult result)
        {
            var line = JsonSerializer.Serialize(result.ToJson());
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.AppendAllTextAsync(fullPath, line + Environment.NewLine, Encoding.UTF8).ConfigureAwait(false);
            Console.WriteLine($"JSONL evidence appended to: {fullPath}");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: SiebwaldeApp.RealInitHilHost hil-a|hil-b|hil-c|hil-d [--jsonl <path>] [--domain 1,3,4,6]");
            Console.WriteLine("                                    [--timeout <seconds>]");
            Console.WriteLine("  hil-c additionally: [--poll-interval <seconds>] [--max-duration <seconds>] [--stop-file <path>]");
            Console.WriteLine("  hil-d additionally: [--max-duration <seconds>] [--stop-file <path>] [--continue-on-fail]");
            Console.WriteLine();
            Console.WriteLine("Subcommands (REAL hardware, NOT run in offline builds):");
            Console.WriteLine("  hil-a  Passive diagnostics: controller identity (113), global-stop state (111), and");
            Console.WriteLine("         PIC18 detection + HR7/HR11 via bounded telemetry-enablement (250 -> 100 -> 106 -> 101).");
            Console.WriteLine("         NO 104 address-write, NO 105 enable, NO 108 write, NO config, NO firmware, NO track power.");
            Console.WriteLine("  hil-b  Single conditional Stage 1-5 init run through the production runtime host");
            Console.WriteLine("         (transportFactory: null -> real UDP, TrackControlMode.Real). Asserts the expected");
            Console.WriteLine("         terminal state (RunStatus == StoppedAtGlobalReady, stages 1-5 Passed, no Stage 6).");
            Console.WriteLine("  hil-c  SINGLE-SESSION READY ENDURANCE (the authorized fallback; NOT a repeated-init test).");
            Console.WriteLine("         Runs the same Stage 1-5 init ONCE, then HOLDS the session (heartbeat + lease +");
            Console.WriteLine("         communication) and polls (default every 30 s) up to --max-duration (default 8 h).");
            Console.WriteLine("         Fails closed on the first deviation (global-stop latch / amp fault / HR8 leaving");
            Console.WriteLine("         READY / stale lease). No REARM (112), no Stage 6/7, no controller reset, no");
            Console.WriteLine("         watchdog bypass. Stops cleanly on the 8 h bound, an operator STOP (Ctrl+C or the");
            Console.WriteLine("         --stop-file appearing), or the first deviation. Per-poll JSONL evidence is written");
            Console.WriteLine("         to --jsonl when given.");
            Console.WriteLine("  hil-d  REARM + REPEATED Stage 1-5 init (the authorized campaign loop). Per cycle it reads");
            Console.WriteLine("         the 111 global-stop state, then: a LATCHED-HOST-LOSS latch (1) authorizes a single");
            Console.WriteLine("         REARM (112); a host-initiated/EMO latch (2) or an unknown/unexpected/unreachable");
            Console.WriteLine("         state STOPS (never re-arms); otherwise it runs the Stage 1-5 init and verifies every");
            Console.WriteLine("         domain amplifier is READY (recording per-amp HR8/HR9/HR22). Bounded per cycle and");
            Console.WriteLine("         overall (--max-duration, default 8 h); operator STOP via Ctrl+C or --stop-file.");
            Console.WriteLine("         Fails closed on the first unexpected deviation; --continue-on-fail instead counts each");
            Console.WriteLine("         recoverable init failure and CONTINUES (never re-arming an unexpected/unsafe global-stop");
            Console.WriteLine("         state), stopping only on unexpected/unsafe conditions. No Stage 6/7, no energization,");
            Console.WriteLine("         no flash. Per-cycle JSONL evidence is written to --jsonl when given.");
            Console.WriteLine();
            Console.WriteLine("Safety:");
            Console.WriteLine("  No subcommand flashes firmware (no 122/131/123/125/126/127, no bootloader ERASE/PROGRAM)");
            Console.WriteLine("  and none energizes track power (Stage 6 is never called). hil-b/hil-c/hil-d use the");
            Console.WriteLine("  production controller-session lease acquired by StartAsync(Real); this harness never");
            Console.WriteLine("  pre-acquires it. hil-d REARMs (112) ONLY for a proven host-loss latch (111=1), never for a");
            Console.WriteLine("  host-initiated/EMO latch or an unknown state.");
            Console.WriteLine();
            Console.WriteLine("Exit code 0 on pass, non-zero on fail.");
        }
    }
}
