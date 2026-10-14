using System.Diagnostics;
using System.Text;
using SiebwaldeApp.Core.Properties;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

[Collection("RealModeEndToEnd")]
public class GlobalControlledStopTests : IDisposable
{
    private readonly List<string> _repositoryPaths = new();
    public void Dispose()
    {
        foreach (var path in _repositoryPaths) if (File.Exists(path)) File.Delete(path);
    }
    private sealed class Hardware : IHardwareBackend
    {
        public int PowerOffCount;
        public bool SetPower(bool on) { if (!on) PowerOffCount++; return true; }
        public bool SetLocoSpeed(int address, int ecosSpeed, int direction) => true;
        public bool SetSwitch(int decoderAddress, int outputIndex, bool on) => true;
    }
    private sealed class Blocks : IBlockPositionProvider
    {
        public event Action<int, int>? BlockEntered { add { } remove { } }
        public int? TryGetBlockForLoc(int loc) => null;
    }
    private sealed class Host : IEcosHostService
    {
        public event EventHandler<RuntimeFaultEventArgs>? Faulted { add { } remove { } }
        public bool IsRunning { get; private set; }
        public TrackControlMode? Mode { get; private set; }
        public ControlDiagnostics? Diagnostics => null;
        public bool IsUnsafe => false;
        public bool ResetSafety() => false;
        public void SetProfile(LayoutProfile profile) { }
        public IReadOnlyDictionary<int, SwitchPosition> GetSwitchPositions() => new Dictionary<int, SwitchPosition>();
        public Task<EcosHostStartResult> StartAsync(TrackControlMode mode, ITrackCommClient? commClient,
            TrackApplicationVariables? variables, CancellationToken cancellationToken = default,
            IMovementSimulation? movementSimulation = null)
        { IsRunning = true; Mode = mode; return Task.FromResult(EcosHostStartResult.Started); }
        public void Stop() { IsRunning = false; Mode = null; }
        public Task<EcosHostStopResult> StopAsync(CancellationToken ct = default)
        { Stop(); return Task.FromResult(EcosHostStopResult.Stopped); }
    }

    private SimpleEcosBackend Backend(GlobalStopController stop, out Hardware hardware)
    {
        hardware = new Hardware();
        var path = Path.Combine(Path.GetTempPath(), $"estop-{Guid.NewGuid():N}.json");
        _repositoryPaths.Add(path);
        var backend = new SimpleEcosBackend(hardware,
            new JsonLocoRepository(path), new Blocks());
        backend.BindGlobalStop(stop);
        return backend;
    }
    private static async Task Command(SimpleEcosBackend backend, StringWriter writer, string text)
    {
        var parser = new SimpleEcosCommandParser();
        await backend.HandleAsync(parser.Parse(text)!, writer, CancellationToken.None);
    }
    private static ReceivedMessage Armed => new(TaskId.CONTROLLER, TrackCommand.GLOBAL_STOP_STATE, TaskStates.DONE, 3);
    private static readonly DateTimeOffset Epoch = new(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task T1_T2_StopWrites399ToEveryKnownAmplifier_ExcludesBackplanes_AndDeduplicates(bool koploper)
    {
        await using var runtime = new TrackApplicationRuntimeHost(new Host());
        var variables = new TrackApplicationVariables { GlobalStop = runtime.GlobalStop };
        foreach (var amp in variables.trackAmpItems) amp.SlaveDetected = 1;
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
            detectedSlaves: Enumerable.Range(1, 55).Select(i => (byte)i).ToArray()));
        await using var comm = new TrackCommClientAsync(transport, variables);
        await comm.StartAsync(false);
        using var monitor = new GlobalStopMonitor(comm, variables);
        var writer = new TrackControlMain("GlobalStopTests", comm, variables);
        var backend = Backend(runtime.GlobalStop, out var hardware);
        using var output = new StringWriter();
        try
        {
            foreach (ushort slave in Enumerable.Range(1, 50).Select(i => (ushort)i))
                Assert.True(variables.SetDesiredAmplifierControl(slave, 500, false));
            if (koploper) await Command(backend, output, "set(1,stop)");
            else runtime.EmergencyStop(); // The exact WPF command entry.
            Assert.Equal(GlobalStopState.Latched, runtime.GlobalStop.State);
            Assert.All(variables.PendingWrites.Values, p => Assert.Equal(399, (int)p.Hr0Value));
            Assert.False(variables.SetDesiredAmplifierControl(1, 500, false));
            Assert.False(variables.SetDesiredAmplifierControl(51, 399, false));
            await monitor.TickAsync(Epoch);
            writer.StartRuntime(CancellationToken.None);
            await WaitUntil(() => transport.SentFrames.Count(f => f[1] == 108) == 50);
            var frames = transport.SentFrames.Where(f => f[1] == 108).ToArray();
            Assert.Equal(Enumerable.Range(1, 50), frames.Select(f => (int)f[2]).OrderBy(i => i));
            Assert.All(frames, f => Assert.Equal(new byte[] { 0xAA, 108, f[2], 0xAA, 1, 0, 0x8F, 1 }, f));
            Assert.Single(transport.SentFrames.Where(f => f[1] == 109));
            Assert.Equal(new byte[] { 0xAA, 109, 0, 0 }, transport.SentFrames.Single(f => f[1] == 109));
            if (koploper) await Command(backend, output, "set(1,stop)"); else runtime.EmergencyStop();
            await Command(backend, output, "set(1,go)");
            await monitor.TickAsync(Epoch.AddMilliseconds(500));
            await Task.Delay(220);
            Assert.Equal(50, transport.SentFrames.Count(f => f[1] == 108));
            Assert.Single(transport.SentFrames.Where(f => f[1] == 109));
            Assert.Equal(koploper ? 1 : 0, hardware.PowerOffCount);
            Assert.True(runtime.GlobalStop.IsLatched);
            Assert.False(runtime.GlobalStop.Arm());
            await backend.StopNotificationTask;
            Assert.Equal(1, output.ToString().Split("<EVENT 1>").Length - 1);
        }
        finally { writer.StopRuntime(); backend.DetachGlobalStop(); variables.DetachGlobalStop(); }
    }

    [Fact]
    public async Task Heartbeat_LinkEstablished_500ms_Stationary_AndLittleEndianSequence()
    {
        var variables = new TrackApplicationVariables();
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
        await using var comm = new TrackCommClientAsync(transport, variables);
        await comm.StartAsync(false);
        using var monitor = new GlobalStopMonitor(comm, variables);
        await monitor.TickAsync(Epoch);
        Assert.Empty(transport.SentFrames);
        monitor.ObserveStatus(new ReceivedMessage(20, 111, 7, 3), Epoch);
        await monitor.TickAsync(Epoch);
        Assert.Empty(transport.SentFrames);
        monitor.EstablishLink(Epoch);
        for (int i = 0; i <= 260; i++)
        {
            var now = Epoch.AddMilliseconds(500 * i);
            monitor.ObserveStatus(new ReceivedMessage(10, 111, 7, GlobalStopStatusMessage.Disarmed), now);
            await monitor.TickAsync(now);
            await monitor.TickAsync(now.AddMilliseconds(499));
        }
        var heartbeats = transport.SentFrames.ToArray();
        Assert.Equal(261, heartbeats.Length);
        for (int i = 0; i < heartbeats.Length; i++)
            Assert.Equal(new byte[] { 0xAA, 110, (byte)i, (byte)(i >> 8) }, heartbeats[i]);
        Assert.Empty(variables.PendingWrites); // no speed change needed to keep authority alive
        Assert.Equal(GlobalStopState.Disarmed, variables.GlobalStop.State); // confirmation is not the heartbeat gate
    }

    [Fact]
    public async Task Pic32Loss_ClearsStaleCommands_NoWrites_OneStopEvent_StatusAndOtherEventsContinue()
    {
        var variables = new TrackApplicationVariables();
        variables.trackAmpItems[1].SlaveDetected = 1;
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
        await using var comm = new TrackCommClientAsync(transport, variables);
        await comm.StartAsync(false);
        using var monitor = new GlobalStopMonitor(comm, variables);
        var backend = Backend(variables.GlobalStop, out _);
        using var output = new StringWriter();
        try
        {
            await Command(backend, output, "get(1,info,status)");
            output.GetStringBuilder().Clear();
            Assert.True(variables.SetDesiredAmplifierControl(1, 600, false));
            monitor.EstablishLink(Epoch);
            monitor.ObserveStatus(Armed, Epoch);
            await monitor.TickAsync(Epoch.AddMilliseconds(1999));
            Assert.Equal(GlobalStopState.Armed, variables.GlobalStop.State);
            int beforeLoss = transport.SentFrames.Count;
            await monitor.TickAsync(Epoch.AddSeconds(2));
            await backend.StopNotificationTask;
            Assert.Equal(GlobalStopReason.Pic32Lost, variables.GlobalStop.Reason);
            Assert.Empty(variables.PendingWrites);
            Assert.False(variables.SetDesiredAmplifierControl(1, 500, false));
            await monitor.TickAsync(Epoch.AddSeconds(3));
            Assert.Equal(beforeLoss, transport.SentFrames.Count); // no 109 or 108 on dead link
            Assert.Equal($"<EVENT 1>{Environment.NewLine}1 status[STOP]{Environment.NewLine}<END 0 (OK)>{Environment.NewLine}", output.ToString());
            await Command(backend, output, "get(1,info,status)");
            int requestStart = output.GetStringBuilder().Length;
            await Command(backend, output, "request(1,view)");
            Assert.Equal($"<REPLY request(1,view)>{Environment.NewLine}<END 0 (OK)>{Environment.NewLine}",
                output.ToString().Substring(requestStart));
            await Command(backend, output, "set(1,go)");
            await backend.OnSensorChangedAsync(1, true);
            await backend.OnSwitchChangedAsync(11, 1, 0);
            await Command(backend, output, "set(1000,func[0][1])");
            Assert.Equal(1, output.ToString().Split("<EVENT 1>").Length - 1);
            Assert.Equal(2, output.ToString().Split("1 status[STOP]").Length - 1);
            Assert.Contains("<EVENT 100>", output.ToString());
            Assert.Contains("<EVENT 11>", output.ToString());
            Assert.Contains("<EVENT 1000>", output.ToString());
            backend.DetachGlobalStop();
            var restartedBackend = Backend(variables.GlobalStop, out _);
            using var restartedOutput = new StringWriter();
            try
            {
                await Command(restartedBackend, restartedOutput, "get(1,info,status)");
                Assert.Contains("1 status[STOP]", restartedOutput.ToString());
                Assert.DoesNotContain("<EVENT 1>", restartedOutput.ToString());
            }
            finally { restartedBackend.DetachGlobalStop(); }
        }
        finally { backend.DetachGlobalStop(); }
    }

    [Theory]
    [InlineData(1, GlobalStopReason.Pic32HostLoss)]
    [InlineData(2, GlobalStopReason.Pic32HostInitiated)]
    public async Task Pic32LatchStatus_IsSticky(int message, GlobalStopReason reason)
    {
        var variables = new TrackApplicationVariables();
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
        await using var comm = new TrackCommClientAsync(transport, variables);
        using var monitor = new GlobalStopMonitor(comm, variables);
        monitor.ObserveStatus(new ReceivedMessage(10, 111, 7, (ushort)message), Epoch);
        monitor.ObserveStatus(Armed, Epoch.AddSeconds(1));
        Assert.Equal(reason, variables.GlobalStop.Reason);
        Assert.Equal(GlobalStopState.Latched, variables.GlobalStop.State);
    }

    [Fact]
    public async Task FreshReadback_NotCachedPublish_RenewsReceiveFreshness()
    {
        var variables = new TrackApplicationVariables();
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
        await using var comm = new TrackCommClientAsync(transport, variables);
        await comm.StartAsync(false);
        using var monitor = new GlobalStopMonitor(comm, variables);
        monitor.EstablishLink(Epoch);
        monitor.ObserveStatus(Armed, Epoch);
        var amp = variables.trackAmpItems[1];
        amp.ProtocolReadbackObserved = true;
        amp.LastDataReceivedUtc = Epoch.AddSeconds(1);
        await monitor.TickAsync(Epoch.AddSeconds(2.5));
        Assert.Equal(GlobalStopState.Armed, variables.GlobalStop.State);
        await monitor.TickAsync(Epoch.AddSeconds(3.1));
        Assert.Equal(GlobalStopReason.Pic32Lost, variables.GlobalStop.Reason);
    }

    [Fact]
    public async Task NewTransportSession_RetainedArmedState_IsNotHeartbeatAuthority()
    {
        var variables = new TrackApplicationVariables();
        variables.GlobalStop.Arm(); // state retained from a previous runtime session
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
        await using var comm = new TrackCommClientAsync(transport, variables);
        await comm.StartAsync(false);
        using var monitor = new GlobalStopMonitor(comm, variables);
        await monitor.TickAsync(Epoch);
        await monitor.TickAsync(Epoch.AddSeconds(10));
        Assert.Empty(transport.SentFrames);
        Assert.False(variables.GlobalStop.IsLatched);
        monitor.EstablishLink(Epoch.AddSeconds(10));
        await monitor.TickAsync(Epoch.AddSeconds(10));
        Assert.Equal(new byte[] { 0xAA, 110, 0, 0 }, Assert.Single(transport.SentFrames));
    }

    [Fact]
    public async Task Bootstrap_ConnectedPeerReceivesFirstHeartbeat_BeforeArmedConfirmation()
    {
        var variables = new TrackApplicationVariables();
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(emitGlobalStopArmedStatus: true));
        await using var comm = new TrackCommClientAsync(transport, variables);
        using var monitor = new GlobalStopMonitor(comm, variables);
        await comm.StartAsync(false);
        await comm.SendAsync(new SendMessage(EnumClientCommands.CLIENT_CONNECTION_REQUEST, new byte[80]));
        await WaitUntil(() => variables.trackControllerCommands.ReceivedMessage.Taskcommand == TaskStates.CONNECTED);
        Assert.Null(monitor.LastObservedPic32Status);
        Assert.Equal(GlobalStopState.Disarmed, variables.GlobalStop.State);
        var now = DateTimeOffset.UtcNow;
        monitor.EstablishLink(now);
        await monitor.TickAsync(now);
        Assert.Equal(new byte[] { 0xAA, 110, 0, 0 }, Assert.Single(transport.SentFrames.Where(f => f[1] == 110)));
        // The simulated PIC32 produces 111/3 only because it received that first heartbeat.
        await WaitUntil(() => monitor.LastObservedPic32Status == GlobalStopStatusMessage.Armed);
        Assert.Equal(GlobalStopState.Armed, variables.GlobalStop.State);
    }

    [Fact]
    public async Task DisarmedStatus_DoesNotLatchOrBlockHeartbeat_AndCannotClearStickyStop()
    {
        var variables = new TrackApplicationVariables();
        await using var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
        await using var comm = new TrackCommClientAsync(transport, variables);
        await comm.StartAsync(false);
        using var monitor = new GlobalStopMonitor(comm, variables);
        monitor.EstablishLink(Epoch);
        monitor.ObserveStatus(new ReceivedMessage(10, 111, 7, GlobalStopStatusMessage.Disarmed), Epoch);
        await monitor.TickAsync(Epoch);
        Assert.Equal(GlobalStopStatusMessage.Disarmed, monitor.LastObservedPic32Status);
        Assert.Equal(GlobalStopState.Disarmed, variables.GlobalStop.State);
        Assert.Equal(new byte[] { 0xAA, 110, 0, 0 }, Assert.Single(transport.SentFrames));
        // Receive-loss protection also applies before an armed confirmation is delivered.
        await monitor.TickAsync(Epoch.AddSeconds(2));
        Assert.Equal(GlobalStopReason.Pic32Lost, variables.GlobalStop.Reason);
        monitor.ObserveStatus(new ReceivedMessage(10, 111, 7, GlobalStopStatusMessage.Disarmed), Epoch.AddSeconds(3));
        await monitor.TickAsync(Epoch.AddSeconds(3));
        Assert.True(variables.GlobalStop.IsLatched);
        Assert.Single(transport.SentFrames);
    }

    [Fact]
    public async Task RuntimeComposition_ParsesArmedStatus_SendsHeartbeat_AndWpfStopNeutral()
    {
        var original = CoreSettings.Default.TrackAmplifierFwPath;
        var path = Path.Combine(Path.GetTempPath(), $"estop-fw-{Guid.NewGuid():N}.hex");
        var data = new StringBuilder();
        for (int row = 0; row < 1920; row++)
            data.Append($":10{row * 16:X4}00{(row == 1919 ? "1F25" + new string('0', 28) : new string('0', 32))}00\n");
        data.Append(":0C000000" + new string('F', 24) + "00\n");
        File.WriteAllText(path, data.ToString());
        var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
            periodicSlaveInfoInterval: TimeSpan.FromMilliseconds(200), emitGlobalStopArmedStatus: true));
        try
        {
            CoreSettings.Default.TrackAmplifierFwPath = path;
            await using var runtime = new TrackApplicationRuntimeHost(new Host(), transportFactory: () => transport,
                amplifierGroups: TrackAmplifierGroups.Create(new[] { 1, 2, 3 }, null, null));
            await runtime.StartAsync(TrackControlMode.Real);
            await WaitUntil(() => runtime.GlobalStop.State == GlobalStopState.Armed);
            Assert.Equal(GlobalStopState.Armed, runtime.GlobalStop.State);
            await WaitUntil(() => transport.SentFrames.Count(f => f[1] == 110) >= 3);
            runtime.SetAmplifierControl(1, 500, false);
            await WaitUntil(() => transport.GetRegisters(1)[0] == 500);
            runtime.EmergencyStop();
            await WaitUntil(() => transport.GetRegisters(1)[0] == 399 && transport.SentFrames.Any(f => f[1] == 109));
            runtime.SetAmplifierControl(1, 600, false);
            await Task.Delay(250);
            Assert.Equal(399, (int)transport.GetRegisters(1)[0]);
            await runtime.StopAsync();
            Assert.True(runtime.GlobalStop.IsLatched); // lifecycle stop does not reset global latch
        }
        finally
        {
            CoreSettings.Default.TrackAmplifierFwPath = original;
            // Existing TrackAmplifierBootloaderHelpers constructor opens an unretained reader.
            // Finalize that reader before deleting this fixture; repairing it is outside this increment.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.Delete(path);
        }
    }

    private static async Task WaitUntil(Func<bool> predicate)
    {
        var watch = Stopwatch.StartNew();
        while (!predicate())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(8)) throw new TimeoutException();
            await Task.Delay(20);
        }
    }
}
