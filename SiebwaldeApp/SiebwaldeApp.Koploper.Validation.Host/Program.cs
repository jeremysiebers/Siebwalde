using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.Koploper.Validation;

namespace SiebwaldeApp.Koploper.Validation.Host
{
    /// <summary>
    /// KIS-06 simulator cross-validation harness. Commands:
    /// <list type="bullet">
    ///   <item><c>validate</c> — live: run the real read-only reservation observer loop and write
    ///       <see cref="KoploperValidationSample"/> JSONL, then print a summary.</item>
    ///   <item><c>replay</c> — offline: read golden <see cref="KoploperGoldenComparison"/> JSONL and
    ///       feed each line deterministically through <see cref="KoploperComparisonEngine"/>,
    ///       verifying any stored expected result.</item>
    /// </list>
    /// No write access to Koploper and no hardware actions are performed.
    /// </summary>
    public static class Program
    {
        private const string DefaultTracePath = "koploper-validation.jsonl";
        private const int DefaultCycles = 10;
        private const int DefaultIntervalMs = 500;

        private static readonly KoploperValidationOptions ValidationOptions = new();

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            string command = args[0].ToLowerInvariant();
            string[] rest = args.Skip(1).ToArray();

            return command switch
            {
                "validate" => RunValidate(rest),
                "replay" => RunReplay(rest),
                "--help" or "-h" or "/?" => Help(),
                _ => Unknown(command)
            };
        }

        // ---------------------------------------------------------------- validate (live)

        private static int RunValidate(string[] args)
        {
            int cycles = DefaultCycles;
            int intervalMs = DefaultIntervalMs;
            string tracePath = DefaultTracePath;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--cycles" when i + 1 < args.Length:
                        cycles = int.Parse(args[++i]);
                        break;
                    case "--interval-ms" when i + 1 < args.Length:
                        intervalMs = int.Parse(args[++i]);
                        break;
                    case "--trace" when i + 1 < args.Length:
                        tracePath = args[++i];
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown validate argument: {args[i]}");
                        return 2;
                }
            }

            if (cycles < 0)
            {
                Console.Error.WriteLine("--cycles must be >= 0.");
                return 2;
            }

            var composition = ValidationHostComposition.Create(tracePath);
            var observer = composition.Observer;
            var trace = composition.TraceWriter;

            int authoritative = 0;
            var occupiedByLoc = new Dictionary<int, int?>();
            var reservedByLoc = new Dictionary<int, IReadOnlyList<int>>();
            var results = new SortedSet<KoploperValidationResult>();

            try
            {
                trace.WriteScenarioStart("validate-live");

                for (int cycle = 0; cycle < cycles; cycle++)
                {
                    KoploperReservationObservation observation = observer.Refresh();

                    // Live composition has no independent cross-check source, so the comparison
                    // engine is exercised with a null independent sample: an authoritative
                    // observation yields SourceUnavailable, a non-authoritative one yields
                    // NonAuthoritativeSource.
                    KoploperValidationResult result =
                        KoploperComparisonEngine.Compare(observation, independent: null, ValidationOptions);
                    results.Add(result);

                    if (observation.IsAuthoritative)
                    {
                        authoritative++;
                    }

                    if (observation.Locomotives.Count == 0)
                    {
                        trace.WriteSample(ToSample(observation, result, locId: null, occupiedBlock: null, Array.Empty<int>()));
                    }
                    else
                    {
                        foreach (KoploperLocomotiveTrajectory locomotive in observation.Locomotives)
                        {
                            occupiedByLoc[locomotive.InternalLocomotiveId] = locomotive.OccupiedBlock;
                            reservedByLoc[locomotive.InternalLocomotiveId] = locomotive.ReservedBlocks.ToArray();
                            trace.WriteSample(ToSample(
                                observation,
                                result,
                                locomotive.InternalLocomotiveId,
                                locomotive.OccupiedBlock,
                                locomotive.ReservedBlocks.ToArray()));
                        }
                    }

                    if (cycle < cycles - 1)
                    {
                        Thread.Sleep(intervalMs);
                    }
                }

                trace.WriteScenarioEnd("validate-live");
            }
            finally
            {
                trace.Dispose();
                observer.Dispose();
            }

            Console.WriteLine("== Koploper validate summary ==");
            Console.WriteLine($"Cycles: {cycles}");
            Console.WriteLine($"Authoritative observations: {authoritative}");
            Console.WriteLine($"Per-locomotive occupied/reserved seen ({occupiedByLoc.Count} locos):");
            foreach (int locId in occupiedByLoc.Keys.OrderBy(id => id))
            {
                string reserved = reservedByLoc.TryGetValue(locId, out IReadOnlyList<int>? r)
                    ? string.Join(",", r.OrderBy(b => b))
                    : string.Empty;
                Console.WriteLine($"  loc {locId}: occupied={occupiedByLoc[locId]?.ToString() ?? "?"} reserved=[{reserved}]");
            }

            Console.WriteLine($"Validation results seen: {string.Join(", ", results.Select(r => r.ToString()))}");
            return 0;
        }

        // ---------------------------------------------------------------- replay (offline)

        private static int RunReplay(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("replay requires a golden JSONL file path.");
                return 2;
            }

            string goldenPath = args[0];
            if (!File.Exists(goldenPath))
            {
                Console.Error.WriteLine($"Golden file not found: {goldenPath}");
                return 2;
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            int total = 0;
            int agree = 0;
            var mismatches = new List<string>();

            foreach (string line in File.ReadLines(goldenPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                KoploperGoldenComparison? input;
                try
                {
                    input = JsonSerializer.Deserialize<KoploperGoldenComparison>(line, jsonOptions);
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine($"Skipping malformed line {total + 1}: {ex.Message}");
                    continue;
                }

                if (input is null)
                {
                    continue;
                }

                total++;

                KoploperValidationResult actual =
                    KoploperComparisonEngine.Compare(input.Observation, input.Independent, ValidationOptions);

                if (input.Expected is null || actual == input.Expected)
                {
                    agree++;
                }
                else
                {
                    mismatches.Add($"line {total}: expected={input.Expected} actual={actual}");
                }
            }

            Console.WriteLine("== Koploper replay summary ==");
            Console.WriteLine($"Golden file: {goldenPath}");
            Console.WriteLine($"Samples: {total}");
            Console.WriteLine($"Agree: {agree}");
            Console.WriteLine($"Disagree: {total - agree}");
            foreach (string mismatch in mismatches)
            {
                Console.WriteLine($"  {mismatch}");
            }

            return total == agree ? 0 : 1;
        }

        // ---------------------------------------------------------------- sample flattening

        private static KoploperValidationSample ToSample(
            KoploperReservationObservation observation,
            KoploperValidationResult result,
            int? locId,
            int? occupiedBlock,
            IReadOnlyList<int> reservedBlocks)
        {
            return new KoploperValidationSample(
                observation.ObservedAtUtc,
                observation.Generation?.ProcessId ?? 0,
                observation.Generation,
                observation.SourceSequence,
                observation.ObserverSequence,
                observation.SourceHealth,
                observation.IsAuthoritative,
                observation.AuthorityReason,
                locId,
                occupiedBlock,
                reservedBlocks,
                Port5700CurrentBlock: null,
                Port5700Loc: null,
                CrossCheckSource: null,
                GuiMarker: null,
                ScenarioId: "validate-live",
                result,
                Note: null);
        }

        // ---------------------------------------------------------------- usage

        private static int Help()
        {
            PrintUsage();
            return 0;
        }

        private static int Unknown(string command)
        {
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 2;
        }

        private static void PrintUsage()
        {
            Console.WriteLine(
                "SiebwaldeApp.Koploper.Validation.Host — KIS-06 simulator cross-validation harness\n" +
                "Usage:\n" +
                "  validate [--cycles N] [--interval-ms N] [--trace PATH]\n" +
                "      Run the live read-only reservation observer loop, write KoploperValidationSample\n" +
                "      JSONL, and print a summary.\n" +
                "  replay <golden.jsonl>\n" +
                "      Feed golden KoploperGoldenComparison JSONL through KoploperComparisonEngine\n" +
                "      deterministically and verify each expected result.\n");
        }
    }
}
