using SiebwaldeApp.EcosEmu;
using System.Net;

class Program
{
    static void Main(string[] args)
    {
        // Koploper external information client (port 5700)
        var externalInfo = new KoploperExternalInfoClient();
        externalInfo.Start();

        // Hardware backend (dummy for now, but with feedback hook)
        var hardware = new DummyHardwareBackend();

        // Loco repository
        var locoRepository = new JsonLocoRepository("C:\\Localdata\\Siebwalde\\Logging\\locos.json");
        locoRepository.LoadAsync().GetAwaiter().GetResult();

        // ECoS emulator backend
        var backend = new SimpleEcosBackend(hardware, locoRepository, externalInfo);

        // Wire hardware feedback → backend (for switch events)
        if (backend is IHardwareFeedbackSink feedbackSink)
        {
            hardware.AttachFeedbackSink(feedbackSink);
        }

        var server = new EcosEmulatorServer(15471, new SimpleEcosCommandParser(), backend);
        server.Start();

        Console.WriteLine("ENTER to stop");
        Console.ReadLine();

        externalInfo.Stop();
    }
}

class DummyHardwareBackend : IHardwareBackend
{
    private IHardwareFeedbackSink? _feedbackSink;

    /// <summary>
    /// Attach a feedback sink so that the hardware backend can report
    /// external changes (e.g. switch position changes) back to the ECoS backend.
    /// </summary>
    public void AttachFeedbackSink(IHardwareFeedbackSink sink)
    {
        _feedbackSink = sink;
    }

    public void SetPower(bool on)
    {
        Console.WriteLine($"[HW] Power {(on ? "ON" : "OFF")}");
    }

    public void SetLocoSpeed(int address, int ecosSpeed, int direction)
    {
        Console.WriteLine($"[HW] Loco addr={address} speed={ecosSpeed} dir={direction}");
    }

    public void SetSwitch(int decoderAddress, int outputIndex, bool on)
    {
        Console.WriteLine($"[HW] Switch addr={decoderAddress} index={outputIndex} state={on}");

        // For now, mirror the change back as if the real hardware has confirmed it.
        // Simple 1:1 mapping: ecosId == decoderAddress.
        if (_feedbackSink != null)
        {
            int ecosId = decoderAddress;
            // Fire-and-forget; we do not block the caller on network I/O.
            //_ = _feedbackSink.OnSwitchChangedAsync(ecosId, decoderAddress, outputIndex);
        }
    }
}