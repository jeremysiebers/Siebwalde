using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SiebwaldeApp.EcosEmu;

class Program
{
    static async Task Main(string[] args)
    {
        // Capture all console output (Koploper external info, ECoS commands, feedback)
        // into a trace file so a Koploper session can be analysed afterwards.
        using var trace = EcosEmuTrace.Start();

        var externalInfo = new KoploperExternalInfoClient();

        var locoRepo = new JsonLocoRepository("C:\\Localdata\\Siebwalde\\Logging\\locos.json");
        locoRepo.LoadAsync().GetAwaiter().GetResult();

        // Hardware backend: track simulator, now aware of Koploper block positions
        var hardware = new TrackSimulatorBackend(externalInfo);
        var backend = new SimpleEcosBackend(hardware, locoRepo, externalInfo);

        // Let the simulator send sensor events back into the ECoS backend
        hardware.AttachFeedbackSink(backend);

        var server = new EcosEmulatorServer(15471, new SimpleEcosCommandParser(), backend);

        // NOW start external info, AFTER feedback sink hookup:
        externalInfo.Start();

        server.Start();

        Console.WriteLine("ENTER to stop");
        Console.ReadLine();

        // Graceful shutdown: stop the server first (so no new Koploper connections arrive),
        // then the external-info client, and finally the simulator loop. Each stop is bounded
        // so a stuck connection cannot hang process exit.
        await server.StopAsync();
        await externalInfo.StopAsync();
        await hardware.StopAsync();
    }
}

/// <summary>
/// Tees everything written to the console into a timestamped trace file under the
/// Siebwalde Logging directory, so Koploper/ECoS sessions can be analysed later.
/// </summary>
internal static class EcosEmuTrace
{
    public static IDisposable Start()
    {
        try
        {
            const string logDirectory = "C:\\Localdata\\Siebwalde\\Logging";
            Directory.CreateDirectory(logDirectory);

            var path = Path.Combine(logDirectory, $"{DateTime.Now:dd-MM-yyyy}_EcosEmuTrace.txt");
            var fileWriter = new StreamWriter(path, append: true) { AutoFlush = true };

            Console.SetOut(new TeeTextWriter(Console.Out, fileWriter));
            Console.WriteLine($"=== ECoS emulator trace started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

            return fileWriter;
        }
        catch
        {
            // Tracing must never prevent the emulator from starting.
            return new NoopDisposable();
        }
    }

    private sealed class TeeTextWriter : TextWriter
    {
        private readonly TextWriter _first;
        private readonly TextWriter _second;

        public TeeTextWriter(TextWriter first, TextWriter second)
        {
            _first = first;
            _second = second;
        }

        public override Encoding Encoding => _first.Encoding;

        public override void Write(char value)
        {
            _first.Write(value);
            _second.Write(value);
        }

        public override void Write(string? value)
        {
            _first.Write(value);
            _second.Write(value);
        }

        public override void WriteLine(string? value)
        {
            _first.WriteLine(value);
            _second.WriteLine(value);
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
