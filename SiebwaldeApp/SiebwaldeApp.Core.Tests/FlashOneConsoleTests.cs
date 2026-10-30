using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using SiebwaldeApp.Core.TrackApplication.Comm;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

/// <summary>
/// Offline tests for the flash-one console adapter (<see cref="FlashOneAmplifierRunner"/>,
/// <see cref="FlashOneHeartbeat"/>, <see cref="FlashOneOwnershipPreflight"/>). All tests run
/// against a scripted in-memory transport; no UDP socket is opened and no hardware is touched.
/// </summary>
public class FlashOneConsoleTests
{
    // -------------------------------------------------------------------------
    // (a) validation fails BEFORE any send (no write)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Runner_Sha256Mismatch_FailsBeforeAnySend()
    {
        var hexPath = WritePic18HexFile(seed: 100);
        try
        {
            var transport = new ScriptedTransport { Responder = BuildResponder() };
            var runner = NewRunner(transport);

            var result = await runner.RunAsync(hexPath, expectedSha256Hex: new string('0', 64), amplifier: 6);

            Assert.False(result.Success);
            Assert.Contains("SHA-256 mismatch", result.Error);
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(transport.SentFrames); // no write occurred
            Assert.False(transport.Opened);     // transport was never opened
        }
        finally { TryDelete(hexPath); }
    }

    [Fact]
    public async Task Runner_EmbeddedChecksumMismatch_FailsBeforeAnySend()
    {
        var hexPath = WritePic18HexFile(seed: 101, corruptEmbeddedChecksum: true);
        try
        {
            var transport = new ScriptedTransport { Responder = BuildResponder() };
            var runner = NewRunner(transport);

            var result = await runner.RunAsync(hexPath, expectedSha256Hex: null, amplifier: 6);

            Assert.False(result.Success);
            Assert.Contains("embedded-checksum mismatch", result.Error);
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(transport.SentFrames); // no write occurred
            Assert.False(transport.Opened);     // transport was never opened
        }
        finally { TryDelete(hexPath); }
    }

    // -------------------------------------------------------------------------
    // (b) correct 131 sequence, never 122 / host-paced 123-129 primitives
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Runner_Emits131WithSlaveId_AfterGlobalReset_AndNeverSendsHostPacedPrimitives()
    {
        var hexPath = WritePic18HexFile(seed: 102);
        try
        {
            var transport = new ScriptedTransport { Responder = BuildResponder() };
            var runner = NewRunner(transport);

            var result = await runner.RunAsync(hexPath, expectedSha256Hex: null, amplifier: 6);
            Assert.True(result.Success, result.Error);

            var commands = transport.SentFrames.Select(f => f[1]).ToArray();

            int resetIdx = Array.IndexOf(commands, (byte)TrackCommand.EXEC_MBUS_STATE_RESET);        // 107
            int onIdx = Array.IndexOf(commands, (byte)TrackCommand.EXEC_MBUS_STATE_SLAVES_ON);       // 100
            int fwIdx = Array.IndexOf(commands, (byte)TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH);  // 103
            int flashIdx = Array.IndexOf(commands, (byte)TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE); // 131

            Assert.True(resetIdx >= 0, "EXEC_MBUS_STATE_RESET (107) was not emitted.");
            Assert.True(onIdx > resetIdx, "EXEC_MBUS_STATE_SLAVES_ON (100) must follow RESET (107).");
            Assert.True(fwIdx > onIdx, "FW-handler entry (103) must follow the global reset.");
            Assert.True(flashIdx > fwIdx, "FLASH_ONE_SLAVE (131) must follow FW-handler entry.");

            // The proven 131 path never sends the all-slaves 122 and never sends the host-paced
            // 8-bit bootloader primitives 123..129 (the PIC32 FlashSequencer runs those internally).
            foreach (byte forbidden in new[]
            {
                TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES,      // 122
                TrackCommand.EXEC_FW_STATE_SELECT_SLAVE,           // 123
                TrackCommand.EXEC_FW_STATE_GET_BOOTLOADER_VERSION, // 124
                TrackCommand.EXEC_FW_STATE_ERASE_FLASH,            // 125
                TrackCommand.EXEC_FW_STATE_WRITE_FLASH,            // 126
                TrackCommand.EXEC_FW_STATE_WRITE_CONFIG,           // 127
                TrackCommand.EXEC_FW_STATE_CHECK_CHECKSUM,         // 128
                TrackCommand.EXEC_FW_STATE_SLAVE_RESET,            // 129
            })
            {
                Assert.DoesNotContain(forbidden, commands);
            }

            // The single-target command must carry exactly the selected slave id in data[0].
            var flashFrame = Assert.Single(
                transport.SentFrames.Where(f => f[1] == TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE));
            Assert.Equal(6, flashFrame[2]);
        }
        finally { TryDelete(hexPath); }
    }

    // -------------------------------------------------------------------------
    // (c) heartbeat: sends 110 only, isolated from the response channel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Heartbeat_Sends110FramesOnly_AndNeverSubscribesToResponseChannel()
    {
        var comm = new RecordingCommClient();
        var heartbeat = new FlashOneHeartbeat(comm, interval: TimeSpan.FromMilliseconds(30));

        using var cts = new CancellationTokenSource();
        heartbeat.Start(cts.Token);

        await Task.Delay(200);

        await heartbeat.DisposeAsync();

        Assert.True(comm.Sent.Count >= 2, $"expected >= 2 heartbeat frames, got {comm.Sent.Count}");
        Assert.All(comm.Sent, s => Assert.Equal(TrackCommand.HOST_HEARTBEAT, s.Command));

        // The wire counter is a little-endian u16 starting at 0.
        for (var i = 0; i < comm.Sent.Count; i++)
        {
            Assert.Equal(i, comm.Sent[i].Data[0]);
            Assert.Equal(0, comm.Sent[i].Data[1]); // low sequence values stay < 256
        }

        // Isolation: the heartbeat is send-only and never subscribed to the response channel.
        Assert.Equal(0, comm.ControlMessageSubscriberCount);
    }

    [Fact]
    public async Task Runner_HeartbeatFramesInFlight_NotConsumedAs131Ack()
    {
        var hexPath = WritePic18HexFile(seed: 103);
        try
        {
            var transport = new ScriptedTransport
            {
                Responder = BuildResponder(onFlash: frames =>
                {
                    // Simulate heartbeat-related inbound frames arriving just before the real 131
                    // ack: a hypothetical 110 echo and the real one-shot 111 GLOBAL_STOP_STATE
                    // status (Disarmed->Armed transition). Neither may satisfy the 131 predicate.
                    frames.Add(Ctrl(TaskId.CONTROLLER, TrackCommand.HOST_HEARTBEAT, TaskStates.DONE, 0));
                    frames.Add(Ctrl(TaskId.CONTROLLER, TrackCommand.GLOBAL_STOP_STATE, TaskStates.DONE, GlobalStopStatusMessage.Armed));
                    frames.Add(Ctrl(TaskId.FWHANDLER, TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE, TaskStates.DONE, TaskMessages.NONE));
                })
            };
            var runner = NewRunner(transport);

            var result = await runner.RunAsync(hexPath, expectedSha256Hex: null, amplifier: 6);

            // The heartbeat frames were discarded (not misread as the 131 ack), so the flash
            // still succeeds on the real 131 DONE.
            Assert.True(result.Success, result.Error);
        }
        finally { TryDelete(hexPath); }
    }

    // -------------------------------------------------------------------------
    // (d) fail-closed: ERROR / timeout during 131
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Runner_ErrorDuring131_ReturnsNonZeroExit_AndFailedSubStep()
    {
        var hexPath = WritePic18HexFile(seed: 104);
        try
        {
            var transport = new ScriptedTransport
            {
                Responder = BuildResponder(onFlash: frames =>
                    frames.Add(Ctrl(
                        TaskId.FWHANDLER,
                        TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE,
                        TaskStates.ERROR,
                        (byte)(FlashSequencerSubStep.Base + 11)))) // ERASE_FLASH
            };
            var runner = NewRunner(transport);

            var result = await runner.RunAsync(hexPath, expectedSha256Hex: null, amplifier: 6);

            Assert.False(result.Success);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal(11, result.FailedSubStep); // ERASE_FLASH
            Assert.Contains("ERASE_FLASH", result.Error);
        }
        finally { TryDelete(hexPath); }
    }

    [Fact]
    public async Task Runner_TimeoutDuring131_ReturnsNonZeroExit_AndNoSubStep()
    {
        var hexPath = WritePic18HexFile(seed: 105);
        try
        {
            var transport = new ScriptedTransport
            {
                Responder = BuildResponder(onFlash: _ => { /* no reply: the 131 wait times out */ })
            };
            var runner = new FlashOneAmplifierRunner(
                transportFactory: () => transport,
                preflight: new PermissivePreflight(),
                flashTimeout: TimeSpan.FromMilliseconds(300));

            var result = await runner.RunAsync(hexPath, expectedSha256Hex: null, amplifier: 6);

            Assert.False(result.Success);
            Assert.Equal(1, result.ExitCode);
            Assert.Null(result.FailedSubStep);
            Assert.Contains("Timed out", result.Error);
        }
        finally { TryDelete(hexPath); }
    }

    // -------------------------------------------------------------------------
    // (e) ownership preflight (fail closed before any send)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Runner_Preflight_AppRunning_FailsClosedBeforeAnySend()
    {
        var hexPath = WritePic18HexFile(seed: 106);
        try
        {
            var transport = new ScriptedTransport { Responder = BuildResponder() };
            var preflight = new FlashOneOwnershipPreflight(
                getPidsByName: _ => new[] { 1234 },
                isUdpPortInUse: _ => false);
            var runner = new FlashOneAmplifierRunner(
                transportFactory: () => transport, preflight: preflight);

            var result = await runner.RunAsync(hexPath, expectedSha256Hex: null, amplifier: 6);

            Assert.False(result.Success);
            Assert.Contains("running", result.Error);
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(transport.SentFrames); // nothing was transmitted
            Assert.False(transport.Opened);
        }
        finally { TryDelete(hexPath); }
    }

    [Fact]
    public async Task Runner_Preflight_PortInUse_FailsClosedBeforeAnySend()
    {
        var hexPath = WritePic18HexFile(seed: 107);
        try
        {
            var transport = new ScriptedTransport { Responder = BuildResponder() };
            var preflight = new FlashOneOwnershipPreflight(
                getPidsByName: _ => Array.Empty<int>(),
                isUdpPortInUse: _ => true);
            var runner = new FlashOneAmplifierRunner(
                transportFactory: () => transport, preflight: preflight);

            var result = await runner.RunAsync(hexPath, expectedSha256Hex: null, amplifier: 6);

            Assert.False(result.Success);
            Assert.Contains("already bound", result.Error);
            Assert.Equal(1, result.ExitCode);
            Assert.Empty(transport.SentFrames);
            Assert.False(transport.Opened);
        }
        finally { TryDelete(hexPath); }
    }

    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    private static FlashOneAmplifierRunner NewRunner(ScriptedTransport transport)
        => new(
            transportFactory: () => transport,
            preflight: new PermissivePreflight());

    private sealed class PermissivePreflight : IFlashOneOwnershipPreflight
    {
        public FlashOneOwnershipResult Check() => FlashOneOwnershipResult.Success();
    }

    /// <summary>
    /// Records every sent frame and synchronously writes the responder's reply frames into the
    /// receive channel (consumed by <see cref="TrackCommClientAsync"/>).
    /// </summary>
    private sealed class ScriptedTransport : ITrackTransport
    {
        private readonly Channel<byte[]> _frames = Channel.CreateUnbounded<byte[]>();
        public List<byte[]> SentFrames { get; } = new();
        public Func<byte[], IEnumerable<byte[]>>? Responder { get; set; }
        public bool Opened { get; private set; }

        public Task OpenAsync(CancellationToken cancellationToken = default)
        {
            Opened = true;
            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            var frame = buffer.AsSpan(offset, count).ToArray();
            SentFrames.Add(frame);
            if (Responder != null)
            {
                foreach (var reply in Responder(frame))
                    _frames.Writer.TryWrite(reply);
            }
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<byte[]> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await _frames.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_frames.Reader.TryRead(out var frame))
                    yield return frame;
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>Send-only recording comm client used to observe heartbeat sends and subscriptions.</summary>
    private sealed class RecordingCommClient : ITrackCommClient
    {
        private EventHandler<ControlMessageEventArgs>? _controlMessageReceived;

        public List<SendMessage> Sent { get; } = new();
        public int ControlMessageSubscriberCount { get; private set; }

        public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;

        public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived
        {
            add { _controlMessageReceived += value; ControlMessageSubscriberCount++; }
            remove { _controlMessageReceived -= value; ControlMessageSubscriberCount--; }
        }

        public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// Mimics the PIC32 reply sequence for the single-target flash path
    /// (CONNECT -> version -> FW-handler init -> file download -> config word -> 131).
    /// It never emits 122 or the host-paced bootloader primitives 123..129.
    /// </summary>
    private static Func<byte[], IEnumerable<byte[]>> BuildResponder(Action<List<byte[]>>? onFlash = null)
    {
        var chunkCount = 0;
        return frame =>
        {
            byte command = frame.Length > 1 ? frame[1] : (byte)0;
            switch (command)
            {
                case EnumClientCommands.CLIENT_CONNECTION_REQUEST: // 250
                    return new[] { Ctrl(TaskId.CONTROLLER, TaskStates.CONNECTED, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.GET_CONTROLLER_VERSION: // 113
                    return new[] { new byte[] { Enums.HEADER, TaskId.CONTROLLER, TrackCommand.GET_CONTROLLER_VERSION, 1, 0, 0, 0x01, 0 } };
                case TrackCommand.EXEC_MBUS_STATE_RESET: // 107
                    return new[] { Ctrl(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_RESET, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.EXEC_MBUS_STATE_SLAVES_ON: // 100
                    return new[] { Ctrl(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVES_ON, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH: // 103
                    return new[] { Ctrl(TaskId.FWHANDLER, TrackCommand.FWHANDLERINIT, TaskStates.CONNECTED, TaskMessages.NONE) };
                case TrackCommand.EXEC_FW_STATE_RECEIVE_FW_FILE: // 120
                    return new[] { Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_RECEIVE: // 141
                    chunkCount++;
                    if (chunkCount >= 480)
                    {
                        return new[]
                        {
                            Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_DATA_DOWNLOAD_DONE, TaskStates.DONE, TaskMessages.NONE),
                            Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_FW_CHECKSUM, TaskStates.DONE, TaskMessages.RECEIVED_CHECKSUM_OK),
                        };
                    }
                    return new[] { Ctrl(TrackCommand.FWFILEDOWNLOAD, TrackCommand.FILEDOWNLOAD_STATE_RECEIVE_FW_FILE_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.EXEC_FW_STATE_RECEIVE_CONFIG_WORD: // 121
                    return new[] { Ctrl(TrackCommand.FWCONFIGWORDDOWNLOAD, TrackCommand.CONFIGWORDDOWNLOAD_STATE_RECEIVE_CONFIG_WORD_STANDBY, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_RECEIVE: // 151
                    return new[] { Ctrl(TrackCommand.FWCONFIGWORDDOWNLOAD, TrackCommand.CONFIGWORDDOWNLOAD_STATE_FW_CONFIG_WORD_DOWNLOAD_DONE, TaskStates.DONE, TaskMessages.NONE) };
            }

            if (command == TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE) // 131
            {
                // When an onFlash override is supplied, its output is authoritative (an empty
                // result simulates a PIC32 that never acks, forcing the host-side timeout).
                if (onFlash == null)
                {
                    return new[] { Ctrl(TaskId.FWHANDLER, TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE, TaskStates.DONE, TaskMessages.NONE) };
                }

                var frames = new List<byte[]>();
                onFlash(frames);
                return frames;
            }

            return Array.Empty<byte[]>();
        };
    }

    private static byte[] Ctrl(byte sender, byte command, byte state, byte message)
        => new byte[] { Enums.HEADER, sender, command, state, message };

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { /* leaked handle in TrackAmplifierBootloaderHelpers constructor */ }
    }

    /// <summary>
    /// Writes a structurally valid PIC18 application Intel HEX file (1920 data rows covering
    /// 0x800..0x7FFF, a 0x0C config-word record and EOF), mirroring the linker contract and
    /// <see cref="TrackAmplifierBootloaderHelpers"/>.
    /// </summary>
    private static string WritePic18HexFile(int seed, bool corruptEmbeddedChecksum = false)
    {
        const int bootOffset = 0x800;
        const int appEnd = 0x7FFF;

        var img = new byte[0x8000];
        new Random(seed).NextBytes(img);
        for (var a = 0; a < bootOffset; a++) img[a] = 0;

        uint sum = 0;
        for (var a = bootOffset; a < 0x7FFE; a += 2)
        {
            sum = (sum + img[a] + ((uint)img[a + 1] << 8)) & 0xFFFF;
        }
        img[0x7FFE] = (byte)(sum & 0xFF);
        img[0x7FFF] = (byte)((sum >> 8) & 0xFF);
        if (corruptEmbeddedChecksum)
        {
            img[0x7FFE] ^= 0xFF; // break the embedded word
        }

        var sb = new StringBuilder();
        for (var addr = bootOffset; addr <= appEnd; addr += 16)
        {
            sb.Append(":10").Append(addr.ToString("X4")).Append("00");
            var recordSum = 0x10 + ((addr >> 8) & 0xFF) + (addr & 0xFF);
            for (var i = 0; i < 16; i++)
            {
                byte b = img[addr + i];
                sb.Append(b.ToString("X2"));
                recordSum += b;
            }
            sb.Append(((byte)(-recordSum & 0xFF)).ToString("X2")).AppendLine();
        }

        var config = new byte[12];
        new Random(seed + 1000).NextBytes(config);
        sb.Append(":0C00000000");
        var configSum = 0x0C;
        foreach (var b in config)
        {
            sb.Append(b.ToString("X2"));
            configSum += b;
        }
        sb.Append(((byte)(-configSum & 0xFF)).ToString("X2")).AppendLine();

        sb.Append(":00000001FF").AppendLine();

        var path = Path.Combine(Path.GetTempPath(), $"flashone_console_{Guid.NewGuid():N}.hex");
        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
