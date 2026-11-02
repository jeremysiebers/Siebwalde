using System.Text;
using System.Text.Json;

namespace SiebwaldeApp.SystemRegressionHost
{
    /// <summary>
    /// Headless, programmable, OFFLINE system-regression entrypoint.
    ///
    /// Usage:
    ///   SiebwaldeApp.SystemRegressionHost --scenario A|B|C|D|E|F [--jsonl <path>] [--domain 1,2,3]
    ///
    /// Each scenario composes the REAL production C# initialization pipeline
    /// (<see cref="SiebwaldeApp.Integration.TrackApplicationRuntimeHost"/> -> ComposeTrackPartAsync ->
    /// TrackInitStateMachine -> 9 production steps -> TrackControlMain) against an injected
    /// deterministic PIC32/PIC18 simulator transport, then asserts observable effects and writes
    /// machine-readable JSONL evidence. Exit code 0 on pass, non-zero on fail.
    /// </summary>
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string? scenarioId = null;
            string? jsonlPath = null;
            var domain = new[] { 1, 2, 3 };

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--scenario" when i + 1 < args.Length:
                        scenarioId = args[++i];
                        break;
                    case "--jsonl" when i + 1 < args.Length:
                        jsonlPath = args[++i];
                        break;
                    case "--domain" when i + 1 < args.Length:
                        domain = ParseDomain(args[++i]);
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

            if (scenarioId is null)
            {
                Console.Error.WriteLine("Missing required --scenario argument.");
                PrintUsage();
                return 2;
            }

            try
            {
                var runner = new ScenarioRunner(domain);
                var result = await runner.RunAsync(scenarioId);

                PrintSummary(result);

                if (jsonlPath is not null)
                {
                    await WriteJsonlAsync(jsonlPath, result);
                }

                return result.Pass ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"SCENARIO ERROR: {ex}");
                return 1;
            }
        }

        private static int[] ParseDomain(string value)
        {
            var parsed = value
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToArray();

            if (parsed.Length == 0)
            {
                throw new ArgumentException("--domain must contain at least one address.");
            }

            return parsed;
        }

        private static void PrintSummary(ScenarioResult result)
        {
            Console.WriteLine();
            Console.WriteLine($"=== SCENARIO {result.Scenario}: {(result.Pass ? "PASS" : "FAIL")} ===");
            foreach (var check in result.Checks)
            {
                Console.WriteLine($"  {(check.Pass ? "PASS" : "FAIL")}  {check.Name}: {check.Detail}");
            }

            Console.WriteLine($"  RunStatus             = {result.RunStatus}");
            Console.WriteLine($"  MovementPermission    = {result.MovementPermission}");
            Console.WriteLine($"  TrackPowerOn          = {result.TrackPowerOn}");
            if (!string.IsNullOrEmpty(result.FirmwareDeploymentAuthorized))
            {
                Console.WriteLine($"  FirmwareDeployAuthorized = {result.FirmwareDeploymentAuthorized}");
            }

            Console.WriteLine($"  SentCommands          = [{string.Join(",", result.SentCommands)}]");
            if (result.Stages.Count > 0)
            {
                Console.WriteLine("  Stages:");
                foreach (var kv in result.Stages)
                {
                    Console.WriteLine($"    {kv.Key,-22} = {kv.Value}");
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

        private static async Task WriteJsonlAsync(string path, ScenarioResult result)
        {
            var line = JsonSerializer.Serialize(result.ToJson());
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.AppendAllTextAsync(fullPath, line + Environment.NewLine, Encoding.UTF8);
            Console.WriteLine($"JSONL evidence appended to: {fullPath}");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: SiebwaldeApp.SystemRegressionHost --scenario A|B|C|D|E|F [--jsonl <path>] [--domain 1,2,3]");
            Console.WriteLine();
            Console.WriteLine("Scenarios (headless, offline, real production init pipeline + deterministic simulator transport):");
            Console.WriteLine("  A  Successful init stopping at GLOBAL READY (non-energized).");
            Console.WriteLine("  B  Firmware mismatch (+ legacy-checksum guard).");
            Console.WriteLine("  C  Missing amp / comm failure (controller mute).");
            Console.WriteLine("  D  Incorrect protocol / stale info.");
            Console.WriteLine("  E  Stage dependencies.");
            Console.WriteLine("  F  Simulated Stage 6 (three concepts).");
            Console.WriteLine();
            Console.WriteLine("Exit code 0 on pass, non-zero on fail.");
        }
    }
}
