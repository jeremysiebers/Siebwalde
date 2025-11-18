using SiebwaldeApp.EcosEmu;

class Program
{
    //static void Main(string[] args)
    //{
    //    // 5700 block position client (from Koploper to C#)
    //    var externalInfo = new KoploperExternalInfoClient();
    //    externalInfo.Start();

    //    // Hardware backend
    //    IHardwareBackend hw = new DummyHardwareBackend();

    //    // Loco repository (JSON)
    //    // You can change the path if you want it somewhere else.
    //    var locoRepository = new JsonLocoRepository("C:\\Localdata\\Siebwalde\\Logging\\locos.json");
    //    locoRepository.LoadAsync().GetAwaiter().GetResult();

    //    // ECoS emulator backend with hardware + loco repository + block position provider
    //    IEcosBackend backend = new SimpleEcosBackend(hw, locoRepository, externalInfo);

    //    //backend.LocoListSynced += () =>
    //    //{
    //    //    // Here you can use locoRepository + externalInfo to
    //    //    // send an initial snapshot of loco→block mapping to Koploper
    //    //    // via port 5700, once you know the exact binary/ASCII format.
    //    //};

    //    var server = new EcosEmulatorServer(15471, new SimpleEcosCommandParser(), backend);
    //    server.Start();

    //    Console.WriteLine("ENTER to stop");
    //    Console.ReadLine();
    //}
    static void Main(string[] args)
    {
        // Koploper external information client (port 5700)
        var externalInfo = new KoploperExternalInfoClient();
        externalInfo.Start();

        // Hardware backend
        IHardwareBackend hardware = new DummyHardwareBackend();

        // Loco repository
        var locoRepository = new JsonLocoRepository("C:\\Localdata\\Siebwalde\\Logging\\locos.json");
        locoRepository.LoadAsync().GetAwaiter().GetResult();

        // ECoS emulator backend
        IEcosBackend backend = new SimpleEcosBackend(hardware, locoRepository, externalInfo);

        var server = new EcosEmulatorServer(15471, new SimpleEcosCommandParser(), backend);
        server.Start();

        Console.WriteLine("ENTER to stop");
        Console.ReadLine();

        externalInfo.Stop();
    }
}

class DummyHardwareBackend : IHardwareBackend
{
    public void SetPower(bool on)
    {
        Console.WriteLine($"[HW] Power {(on ? "ON" : "OFF")}");
    }

    public void SetLocoSpeed(int address, int ecosSpeed, int direction)
    {
        Console.WriteLine($"[HW] Loco addr={address} speed={ecosSpeed} dir={direction}");
    }
}