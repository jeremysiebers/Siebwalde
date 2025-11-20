using SiebwaldeApp.EcosEmu;
using System.Diagnostics.Metrics;
using System.Net;

class Program
{
    static void Main(string[] args)
    {
        var externalInfo = new KoploperExternalInfoClient();
        externalInfo.Start();

        // Hardware backend: track simulator, now aware of Koploper block positions
        var hardware = new TrackSimulatorBackend(externalInfo);

        var locoRepo = new JsonLocoRepository("C:\\Localdata\\Siebwalde\\Logging\\locos.json");

        var backend = new SimpleEcosBackend(hardware, locoRepo, externalInfo);

        // Let the simulator send sensor events back into the ECoS backend
        hardware.AttachFeedbackSink(backend);

        var server = new EcosEmulatorServer(15471, new SimpleEcosCommandParser(), backend);
        server.Start();

        Console.WriteLine("ENTER to stop");
        Console.ReadLine();
        server.Stop();

        //// Koploper external information client (port 5700)
        //var externalInfo = new KoploperExternalInfoClient();
        //externalInfo.Start();

        //// Hardware backend (dummy for now, but with feedback hook)
        //var hardware = new DummyHardwareBackend();

        //// Loco repository
        //var locoRepository = new JsonLocoRepository("C:\\Localdata\\Siebwalde\\Logging\\locos.json");
        //locoRepository.LoadAsync().GetAwaiter().GetResult();

        //// ECoS emulator backend
        //var backend = new SimpleEcosBackend(hardware, locoRepository, externalInfo);

        //// Wire hardware feedback → backend (for switch/occupancy sensor events)
        //if (backend is IHardwareFeedbackSink feedbackSink)
        //{
        //    hardware.AttachFeedbackSink(feedbackSink);
        //}

        //var server = new EcosEmulatorServer(15471, new SimpleEcosCommandParser(), backend);
        //server.Start();

        //_ = Task.Run(async () =>
        //{
        //    await Task.Delay(TimeSpan.FromSeconds(10));
        //    await hardware.SimulateExternalSensorChangeAsync(1, true);
        //    await Task.Delay(TimeSpan.FromSeconds(10));
        //    await hardware.SimulateExternalSensorChangeAsync(1, false);
        //});

        //Console.WriteLine("ENTER to stop");
        //Console.ReadLine();

        //externalInfo.Stop();
    }
}

//class DummyHardwareBackend : IHardwareBackend
//{
//    private IHardwareFeedbackSink? _feedbackSink;

//    /// <summary>
//    /// Attaches a feedback sink so this hardware backend can report
//    /// external changes (e.g. switch or sensor state) back to the ECoS backend.
//    /// </summary>
//    public void AttachFeedbackSink(IHardwareFeedbackSink sink)
//    {
//        _feedbackSink = sink ?? throw new ArgumentNullException(nameof(sink));
//    }

//    public void SetPower(bool on)
//    {
//        Console.WriteLine($"[HW] Power {(on ? "ON" : "OFF")}");
//    }

//    public void SetLocoSpeed(int address, int ecosSpeed, int direction)
//    {
//        Console.WriteLine($"[HW] Loco addr={address} speed={ecosSpeed} dir={direction}");
//    }

//    public void SetSwitch(int decoderAddress, int outputIndex, bool on)
//    {
//        Console.WriteLine($"[HW] Switch addr={decoderAddress} index={outputIndex} state={on}");

//        // IMPORTANT:
//        // Do NOT call back into the feedback sink here for now, because this
//        // method is usually triggered by the ECoS backend itself in response
//        // to a "set(...)" command from Koploper. If we reported a change here
//        // as external feedback, we would create duplicate events.
//        //
//        // External switch changes (e.g. hardware/manual changes) should use
//        // the helper method SimulateExternalSwitchChangeAsync instead.
//    }

//    /// <summary>
//    /// Test helper: simulate an EXTERNAL switch change (not coming from Koploper).
//    /// This will notify the ECoS backend via the feedback sink.
//    /// </summary>
//    public async Task SimulateExternalSwitchChangeAsync(int ecosId, int decoderAddress, int outputIndex)
//    {
//        if (_feedbackSink == null)
//            return; // method is async Task → mag zonder waarde terugkeren

//        Console.WriteLine($"[HW-SIM] External switch change ecosId={ecosId} addr={decoderAddress} idx={outputIndex}");

//        // Delay 5 seconds
//        await Task.Delay(TimeSpan.FromSeconds(1));

//        Console.WriteLine($"[HW-SIM] External switch change ecosId={ecosId} addr={decoderAddress} idx={outputIndex} (after delay)");

//        await _feedbackSink.OnSwitchChangedAsync(ecosId, decoderAddress, outputIndex);
//    }

//    /// <summary>
//    /// Test helper: simulate an EXTERNAL sensor/occupancy change.
//    /// </summary>
//    public async Task SimulateExternalSensorChangeAsync(int sensorId, bool occupied)
//    {
//        if (_feedbackSink == null)
//            return;

//        Console.WriteLine($"[HW-SIM] External sensor change requested sensorId={sensorId} occupied={occupied}");

//        // ADD DELAY HERE
//        await Task.Delay(TimeSpan.FromSeconds(1));

//        Console.WriteLine($"[HW-SIM] External sensor change sensorId={sensorId} occupied={occupied}");
//        await _feedbackSink.OnSensorChangedAsync(sensorId, occupied);
//    }
//}