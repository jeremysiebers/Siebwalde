using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

public class FlashOneAmplifierTests
{
    // -------------------------------------------------------------------------
    // (a) single-target command sequence
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_Sends131WithSlaveId_AndNeverSends122()
    {
        var hexPath = WritePic18HexFile(seed: 1);
        try
        {
            var validation = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
            Assert.True(validation.Success, validation.Error);
            var artifact = validation.Artifact!;

            var transport = new ScriptedPIC32Transport { Responder = BuildFlashSequenceResponder() };
            var variables = new TrackApplicationVariables();
            await using var comm = new TrackCommClientAsync(transport, variables);
            await comm.StartAsync(realHardwareMode: false);

            var sendNext = new SendNextFwDataPacket(comm, artifact.Helpers);
            var command = new FlashOneAmplifierCommand(
                comm, variables, artifact.Helpers, sendNext,
                messageTimeout: TimeSpan.FromSeconds(10), flashTimeout: TimeSpan.FromSeconds(10));

            var result = await command.ExecuteAsync(6);

            Assert.True(result.Success, result.Error);
            Assert.Equal(6, result.TargetAmplifier);

            var commands = transport.SentFrames.Select(f => f[1]).ToArray();
            Assert.Contains((byte)TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE, commands);
            Assert.DoesNotContain((byte)TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES, commands);

            // The single-target command must carry exactly the selected slave id in data[0].
            var flashFrame = Assert.Single(transport.SentFrames.Where(f => f[1] == TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE));
            Assert.Equal(6, flashFrame[2]);
        }
        finally
        {
            TryDelete(hexPath);
        }
    }

    // -------------------------------------------------------------------------
    // (b) wrong target / wrong artifact -> no write
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task ExecuteAsync_InvalidAmplifier_ReturnsError_AndSendsNothing(byte amplifier)
    {
        var hexPath = WritePic18HexFile(seed: 2);
        try
        {
            var validation = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
            var artifact = validation.Artifact!;

            var transport = new ScriptedPIC32Transport { Responder = BuildFlashSequenceResponder() };
            var variables = new TrackApplicationVariables();
            await using var comm = new TrackCommClientAsync(transport, variables);
            await comm.StartAsync(realHardwareMode: false);

            var command = new FlashOneAmplifierCommand(
                comm, variables, artifact.Helpers, new SendNextFwDataPacket(comm, artifact.Helpers));

            var result = await command.ExecuteAsync(amplifier);

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
            Assert.Empty(transport.SentFrames); // nothing transmitted
        }
        finally
        {
            TryDelete(hexPath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_UnparsedArtifact_ReturnsError_AndSendsNothing()
    {
        // A helpers instance whose Execute() failed (nonexistent file) simulates a wrong artifact.
        var helpers = new TrackAmplifierBootloaderHelpers(
            Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.hex"), "test");
        helpers.Execute();
        Assert.False(helpers.HexFileReadSuccessful);

        var transport = new ScriptedPIC32Transport { Responder = BuildFlashSequenceResponder() };
        var variables = new TrackApplicationVariables();
        await using var comm = new TrackCommClientAsync(transport, variables);
        await comm.StartAsync(realHardwareMode: false);

        var command = new FlashOneAmplifierCommand(
            comm, variables, helpers, new SendNextFwDataPacket(comm, helpers));

        var result = await command.ExecuteAsync(6);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Empty(transport.SentFrames);
    }

    // -------------------------------------------------------------------------
    // (c) SHA-256 / embedded-checksum mismatch -> no write
    // -------------------------------------------------------------------------

    [Fact]
    public void Verifier_Sha256Mismatch_ReturnsFailure()
    {
        var hexPath = WritePic18HexFile(seed: 3);
        try
        {
            var result = FirmwareArtifactVerifier.Validate(
                hexPath, expectedSha256Hex: "0000000000000000000000000000000000000000000000000000000000000000");
            Assert.False(result.Success);
            Assert.Contains("SHA-256 mismatch", result.Error);
        }
        finally
        {
            TryDelete(hexPath);
        }
    }

    [Fact]
    public void Verifier_EmbeddedChecksumMismatch_ReturnsFailure()
    {
        var hexPath = WritePic18HexFile(seed: 4, corruptEmbeddedChecksum: true);
        try
        {
            var result = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
            Assert.False(result.Success);
            Assert.Contains("embedded-checksum mismatch", result.Error);
        }
        finally
        {
            TryDelete(hexPath);
        }
    }

    [Fact]
    public void Verifier_ValidArtifact_ReturnsSuccessWithMatchingChecksum()
    {
        var hexPath = WritePic18HexFile(seed: 5);
        try
        {
            var result = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
            Assert.True(result.Success, result.Error);
            Assert.Equal(result.Artifact!.EmbeddedChecksum, result.Artifact.RecomputedChecksum);
            Assert.True(result.Artifact.Helpers.HexFileReadSuccessful);
            Assert.True(result.Artifact.Helpers.GetConfigWordReadSuccessful);
        }
        finally
        {
            TryDelete(hexPath);
        }
    }

    // -------------------------------------------------------------------------
    // (d) packet counter / parser re-creation between repeated operations
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendNextFwDataPacket_Reset_RestartsFromFirstPacket()
    {
        var hexPath = WritePic18HexFile(seed: 6);
        try
        {
            var validation = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex: null);
            var helpers = validation.Artifact!.Helpers;

            var recording = new RecordingCommClient();
            var sendNext = new SendNextFwDataPacket(recording, helpers);

            // The full image is 1920 rows = 480 packets of 4 rows.
            for (int i = 0; i < 480; i++)
            {
                await sendNext.ExecuteAsync();
            }
            Assert.Equal(480, recording.Sent.Count);

            var firstPacket = helpers.GetHexFileData[0][1]
                .Concat(helpers.GetHexFileData[1][1])
                .Concat(helpers.GetHexFileData[2][1])
                .Concat(helpers.GetHexFileData[3][1])
                .ToArray();

            // Without a reset the next ExecuteAsync would index past the end; with a reset it
            // restarts from the first 64 bytes.
            sendNext.Reset();
            await sendNext.ExecuteAsync();

            Assert.Equal(481, recording.Sent.Count);
            Assert.Equal(firstPacket, recording.Sent[480].Data);
        }
        finally
        {
            TryDelete(hexPath);
        }
    }

    [Fact]
    public void BootloaderHelpers_ExecuteTwice_DoesNotAccumulate()
    {
        var hexPath = WritePic18HexFile(seed: 7);
        try
        {
            var helpers = new TrackAmplifierBootloaderHelpers(hexPath, "test");

            helpers.Execute();
            Assert.True(helpers.HexFileReadSuccessful);
            int rowCountFirst = helpers.GetHexFileData.Count;
            ushort checksumFirst = helpers.GetFileCheckSum;

            helpers.Execute();
            Assert.True(helpers.HexFileReadSuccessful);
            Assert.Equal(rowCountFirst, helpers.GetHexFileData.Count);   // no buffer carry-over
            Assert.Equal(checksumFirst, helpers.GetFileCheckSum);        // no checksum accumulation
        }
        finally
        {
            TryDelete(hexPath);
        }
    }

    // -------------------------------------------------------------------------
    // Test infrastructure
    // -------------------------------------------------------------------------

    /// <summary>
    /// Best-effort temp-file cleanup. TrackAmplifierBootloaderHelpers' constructor opens (and
    /// never disposes) a StreamReader over the file, so deletion can legitimately fail with a
    /// sharing violation until the finalizer runs. That leak is a pre-existing defect outside
    /// the §7.4 fix boundary; the OS temp cleaner reclaims any file we cannot delete here.
    /// </summary>
    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { /* leaked handle in TrackAmplifierBootloaderHelpers constructor */ }
    }

    /// <summary>
    /// A scripted PIC32 transport: records every sent frame and synchronously writes the
    /// responder's reply frames into the receive channel (consumed by TrackCommClientAsync).
    /// </summary>
    private sealed class ScriptedPIC32Transport : ITrackTransport
    {
        private readonly Channel<byte[]> _frames = Channel.CreateUnbounded<byte[]>();
        public List<byte[]> SentFrames { get; } = new();
        public Func<byte[], IEnumerable<byte[]>>? Responder { get; set; }

        public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

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

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingCommClient : ITrackCommClient
    {
        public List<(byte Command, byte[] Data)> Sent { get; } = new();

        public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
        public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

        public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add((message.Command, (byte[])message.Data.Clone()));
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// Returns a responder that mimics the PIC32 reply sequence for the single-target flash
    /// command path (CONNECT -> version -> FW-handler init -> file download -> config word ->
    /// FLASH_ONE_SLAVE). It never emits 122.
    /// </summary>
    private static Func<byte[], IEnumerable<byte[]>> BuildFlashSequenceResponder()
    {
        int chunkCount = 0;
        return frame =>
        {
            byte command = frame.Length > 1 ? frame[1] : (byte)0;
            switch (command)
            {
                case EnumClientCommands.CLIENT_CONNECTION_REQUEST: // 250
                    return new[] { Ctrl(TaskId.CONTROLLER, TaskStates.CONNECTED, TaskStates.DONE, TaskMessages.NONE) };
                case TrackCommand.GET_CONTROLLER_VERSION: // 113
                    return new[] { new byte[] { Enums.HEADER, TaskId.CONTROLLER, TrackCommand.GET_CONTROLLER_VERSION, 1, 0, 0, 0x01, 0 } };
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
                case TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE: // 131
                    return new[] { Ctrl(TaskId.FWHANDLER, TrackCommand.EXEC_FW_STATE_FLASH_ONE_SLAVE, TaskStates.DONE, TaskMessages.NONE) };
                default:
                    return Array.Empty<byte[]>();
            }
        };
    }

    private static byte[] Ctrl(byte sender, byte command, byte state, byte message)
        => new byte[] { Enums.HEADER, sender, command, state, message };

    /// <summary>
    /// Writes a structurally valid PIC18 application Intel HEX file (1920 data rows covering
    /// 0x800..0x7FFF, a 0x0C config-word record and EOF). The embedded checksum word at
    /// 0x7FFE/0x7FFF is the little-endian word sum over 0x800..0x7FFD, matching both
    /// TrackAmplifierBootloaderHelpers and the PIC18 linker contract.
    /// </summary>
    private static string WritePic18HexFile(int seed, bool corruptEmbeddedChecksum = false)
    {
        const int bootOffset = 0x800;
        const int appEnd = 0x7FFF;

        var img = new byte[0x8000];
        new Random(seed).NextBytes(img);
        for (int a = 0; a < bootOffset; a++) img[a] = 0;

        uint sum = 0;
        for (int a = bootOffset; a < 0x7FFE; a += 2)
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
        for (int addr = bootOffset; addr <= appEnd; addr += 16)
        {
            sb.Append(":10").Append(addr.ToString("X4")).Append("00");
            int recordSum = 0x10 + ((addr >> 8) & 0xFF) + (addr & 0xFF);
            for (int i = 0; i < 16; i++)
            {
                byte b = img[addr + i];
                sb.Append(b.ToString("X2"));
                recordSum += b;
            }
            sb.Append(((byte)(-recordSum & 0xFF)).ToString("X2")).AppendLine();
        }

        byte[] config = new byte[12];
        new Random(seed + 1000).NextBytes(config);
        sb.Append(":0C00000000");
        int configSum = 0x0C;
        foreach (var b in config)
        {
            sb.Append(b.ToString("X2"));
            configSum += b;
        }
        sb.Append(((byte)(-configSum & 0xFF)).ToString("X2")).AppendLine();

        sb.Append(":00000001FF").AppendLine();

        var path = Path.Combine(Path.GetTempPath(), $"flashone_{Guid.NewGuid():N}.hex");
        File.WriteAllText(path, sb.ToString());
        return path;
    }
}
