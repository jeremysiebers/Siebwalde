using System;
using System.Threading.Tasks;

namespace SiebwaldeApp.StopReachabilityHarness
{
    /// <summary>Command-line options for the stop-reachability harness.</summary>
    internal sealed class HarnessOptions
    {
        public bool DryRun { get; set; } = true;
        public bool Script { get; set; }
        public bool Help { get; set; }
        public int EcosId { get; set; } = 1001;
        public int Address { get; set; } = 2;
        public string Protocol { get; set; } = "DCC28";
        public int FromBlock { get; set; } = 1;
        public int TargetBlock { get; set; } = 3;

        public static HarnessOptions Parse(string[] args, out string? error)
        {
            error = null;
            var options = new HarnessOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg.ToLowerInvariant())
                {
                    case "--dry-run":
                        options.DryRun = true;
                        break;
                    case "--live":
                        options.DryRun = false;
                        break;
                    case "--script":
                        options.Script = true;
                        break;
                    case "--help":
                    case "-h":
                    case "/?":
                        options.Help = true;
                        break;
                    case "--ecos-id":
                        if (!TryInt(args, ref i, out var ecosId)) { error = "Missing value for --ecos-id."; return options; }
                        options.EcosId = ecosId;
                        break;
                    case "--address":
                        if (!TryInt(args, ref i, out var address)) { error = "Missing value for --address."; return options; }
                        options.Address = address;
                        break;
                    case "--protocol":
                        if (i + 1 >= args.Length) { error = "Missing value for --protocol."; return options; }
                        options.Protocol = args[++i];
                        break;
                    case "--from":
                        if (!TryInt(args, ref i, out var from)) { error = "Missing value for --from."; return options; }
                        options.FromBlock = from;
                        break;
                    case "--to":
                        if (!TryInt(args, ref i, out var to)) { error = "Missing value for --to."; return options; }
                        options.TargetBlock = to;
                        break;
                    default:
                        error = $"Unknown argument '{arg}'.";
                        return options;
                }
            }

            if (options.Script && !options.DryRun)
            {
                error = "The scripted sequence is dry-run only; live stages must be operator-controlled.";
            }

            return options;
        }

        private static bool TryInt(string[] args, ref int index, out int value)
        {
            value = 0;
            if (index + 1 >= args.Length)
            {
                return false;
            }

            return int.TryParse(args[++index], out value);
        }
    }

    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var options = HarnessOptions.Parse(args, out var error);
            if (error is not null)
            {
                Console.Error.WriteLine(error);
                PrintUsage();
                return 2;
            }

            if (options.Help)
            {
                PrintUsage();
                return 0;
            }

            Console.WriteLine("Siebwalde stop-reachability test harness");
            Console.WriteLine($"Mode   : {(options.DryRun ? "DRY-RUN (recording comm client, no hardware)" : "LIVE (real track controller; explicit authorization required)")}");
            Console.WriteLine($"Loco   : ecosId={options.EcosId} address={options.Address} protocol={options.Protocol}");
            Console.WriteLine($"Blocks : {options.FromBlock} -> {options.TargetBlock}");
            Console.WriteLine($"Driver : {(options.Script ? "scripted dry-run self-test" : "interactive operator control")}");
            Console.WriteLine();

            var harness = new StopReachabilityHarness(options);

            try
            {
                await harness.ComposeAsync();

                if (options.Script)
                {
                    await harness.RunScriptAsync();
                }
                else
                {
                    await harness.RunInteractiveAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"HARNESS FAILED: {ex}");
                return 1;
            }
            finally
            {
                await harness.DisposeAsync();
            }

            return 0;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: SiebwaldeApp.StopReachabilityHarness [options]");
            Console.WriteLine();
            Console.WriteLine("  --dry-run            use the recording comm client (default; no hardware)");
            Console.WriteLine("  --live               use the real track controller and production init pipeline");
            Console.WriteLine("  --script             dry-run only: run stages 0..5 and the layout-stop check");
            Console.WriteLine("  --ecos-id <n>        ECoS object id of the test loco (default 1001)");
            Console.WriteLine("  --address <n>        decoder/address of the test loco (default 2)");
            Console.WriteLine("  --protocol <name>    loco protocol, e.g. DCC28 (default DCC28)");
            Console.WriteLine("  --from <block>       start block (default 1)");
            Console.WriteLine("  --to <block>         target block of the A->B transition (default 3)");
            Console.WriteLine("  --help               show this help");
            Console.WriteLine();
            Console.WriteLine("Interactive commands: stage0 stage1 stage2 stage3 stage4 stage5 layoutstop backplanecheck resetsafety status help quit");
        }
    }
}
