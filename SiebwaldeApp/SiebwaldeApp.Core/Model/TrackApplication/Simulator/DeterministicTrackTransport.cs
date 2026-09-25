using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core.TrackApplication.Simulator
{
    /// <summary>
    /// Immutable configuration for the deterministic track-bus simulator.
    /// </summary>
    public sealed class TrackSimulatorConfig
    {
        /// <summary>Default firmware checksum reported by every detected slave (verified production hex).</summary>
        public const ushort DefaultFirmwareChecksum = 0x251F;

        /// <summary>Default set of detected ModBus slave addresses.</summary>
        public static readonly IReadOnlyList<byte> DefaultDetectedSlaves = new byte[] { 1, 2, 3 };

        /// <summary>Firmware checksum (HoldingReg11) reported by every detected slave.</summary>
        public ushort FirmwareChecksum { get; }

        /// <summary>ModBus slave addresses that the simulated master detects.</summary>
        public IReadOnlyList<byte> DetectedSlaves { get; }

        /// <summary>
        /// Detected slave addresses whose <c>EXEC_MBUS_SLAVE_DATA_EXCH</c> write produces no
        /// SLAVEINFO echo (their readback therefore goes stale). Empty by default.
        /// </summary>
        public IReadOnlyCollection<byte> DroppedEchoSlaves { get; }

        /// <summary>
        /// Per-slave override for the HoldingReg0 (PWM command) value reported in a SLAVEINFO
        /// readback, used to model "commanded neutral but observed non-neutral". Empty by default.
        /// </summary>
        public IReadOnlyDictionary<byte, ushort> Hr0ReadbackOverrides { get; }

        public TrackSimulatorConfig(
            ushort firmwareChecksum = DefaultFirmwareChecksum,
            IReadOnlyList<byte>? detectedSlaves = null,
            IReadOnlyCollection<byte>? droppedEchoSlaves = null,
            IReadOnlyDictionary<byte, ushort>? hr0ReadbackOverrides = null)
        {
            FirmwareChecksum = firmwareChecksum;
            DetectedSlaves = detectedSlaves is null
                ? DefaultDetectedSlaves
                : new List<byte>(detectedSlaves);
            DroppedEchoSlaves = droppedEchoSlaves is null
                ? Array.Empty<byte>()
                : new List<byte>(droppedEchoSlaves);
            Hr0ReadbackOverrides = hr0ReadbackOverrides is null
                ? new Dictionary<byte, ushort>()
                : new Dictionary<byte, ushort>(hr0ReadbackOverrides);
        }
    }

    /// <summary>
    /// Deterministic, software-only emulation of the PIC32 track master and its track-amplifier
    /// slaves, exposed as an <see cref="ITrackTransport"/>.
    ///
    /// There is no randomness, no timer, and no wall-clock read. Inbound commands are dispatched on
    /// their command byte (<c>copy[1]</c>) and produce exactly the same control/SLAVEINFO frames on
    /// every run, so the real track runtime (comm client -> init pipeline -> <c>TrackControlMain</c>
    /// write loop) can be exercised end-to-end without any network or hardware.
    /// </summary>
    public sealed class DeterministicTrackTransport : ITrackTransport
    {
        private const byte Header = 0xAA;
        private const byte SlaveInfo = 0xFF;
        private const byte Footer = 0x55;
        private const byte WriteDirection = 0xAA;   // Enums.WRITE
        private const int HoldingRegisterCount = 12;
        private const int FirmwareChecksumRegister = TrackAmplifierRegisters.SwChecksum; // 11

        private readonly TrackSimulatorConfig _config;
        private readonly Dictionary<byte, ushort[]> _registers = new();
        private readonly HashSet<byte> _detected = new();
        private readonly HashSet<byte> _droppedEchoSlaves = new();
        private readonly List<byte> _sentCommands = new();
        private readonly object _sync = new();

        private Channel<byte[]>? _frames;
        private CancellationTokenSource? _cts;

        public DeterministicTrackTransport(TrackSimulatorConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            foreach (var slave in _config.DetectedSlaves)
            {
                var registers = new ushort[HoldingRegisterCount];
                registers[FirmwareChecksumRegister] = _config.FirmwareChecksum;
                _registers[slave] = registers;
                _detected.Add(slave);
            }

            foreach (var slave in _config.DroppedEchoSlaves)
            {
                _droppedEchoSlaves.Add(slave);
            }
        }

        // --------------------------------------------------------------------
        // Test-visible state
        // --------------------------------------------------------------------

        /// <summary>Returns a defensive copy of the simulated HoldingRegisters for a slave.</summary>
        public ushort[] GetRegisters(byte slaveNumber)
        {
            lock (_sync)
            {
                if (!_registers.TryGetValue(slaveNumber, out var registers))
                    throw new KeyNotFoundException($"No registers for slave {slaveNumber}.");

                var copy = new ushort[registers.Length];
                Array.Copy(registers, copy, registers.Length);
                return copy;
            }
        }

        /// <summary>Snapshot of every command byte that was sent through this transport, in order.</summary>
        public IReadOnlyList<byte> SentCommands
        {
            get
            {
                lock (_sync)
                {
                    return _sentCommands.ToArray();
                }
            }
        }

        /// <summary>Number of times <see cref="OpenAsync"/> was invoked.</summary>
        public int OpenCallCount { get; private set; }

        /// <summary>Number of times <see cref="CloseAsync"/> was invoked.</summary>
        public int CloseCallCount { get; private set; }

        /// <summary>Number of times <see cref="DisposeAsync"/> was invoked.</summary>
        public int DisposeCallCount { get; private set; }

        /// <summary>True once the transport has been disposed (and therefore closed).</summary>
        public bool IsDisposed => DisposeCallCount >= 1;

        // --------------------------------------------------------------------
        // ITrackTransport
        // --------------------------------------------------------------------

        public Task OpenAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_frames != null)
                    throw new InvalidOperationException("DeterministicTrackTransport is already opened.");

                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _frames = Channel.CreateUnbounded<byte[]>(
                    new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

                OpenCallCount++;
            }

            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            CancellationTokenSource? cts;
            Channel<byte[]>? frames;

            lock (_sync)
            {
                cts = _cts;
                frames = _frames;
                _cts = null;
                CloseCallCount++;
            }

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            frames?.Writer.TryComplete();

            return Task.CompletedTask;
        }

        public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (count <= 0)
                return Task.CompletedTask;

            // The caller reuses its buffer, so take a private copy before dispatching.
            var copy = new byte[count];
            Array.Copy(buffer, offset, copy, 0, count);

            lock (_sync)
            {
                if (copy.Length >= 2)
                {
                    byte command = copy[1];
                    _sentCommands.Add(command);
                    Dispatch(command, copy);
                }
            }

            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<byte[]> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Channel<byte[]>? frames;
            lock (_sync)
            {
                frames = _frames;
            }

            if (frames == null)
                yield break;

            var reader = frames.Reader;

            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] frame;
                try
                {
                    var hasItem = await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                    if (!hasItem)
                        yield break;

                    if (!reader.TryRead(out frame!))
                        continue;
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }

                yield return frame;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync().ConfigureAwait(false);
            DisposeCallCount++;
        }

        // --------------------------------------------------------------------
        // Command dispatch
        // --------------------------------------------------------------------

        private void Dispatch(byte command, byte[] frame)
        {
            var writer = _frames?.Writer;
            if (writer == null)
                return; // not opened yet; drop defensively

            switch (command)
            {
                case EnumClientCommands.CLIENT_CONNECTION_REQUEST: // 250
                    writer.TryWrite(BuildControlFrame(TaskId.CONTROLLER, TaskStates.CONNECTED, TaskStates.DONE));
                    break;

                case TrackCommand.EXEC_MBUS_STATE_RESET: // 107
                    writer.TryWrite(BuildControlFrame(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_RESET, TaskStates.DONE));
                    break;

                case TrackCommand.EXEC_MBUS_STATE_SLAVES_ON: // 100
                    writer.TryWrite(BuildControlFrame(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVES_ON, TaskStates.DONE));
                    break;

                case TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD: // 106
                    writer.TryWrite(BuildControlFrame(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD, TaskStates.DONE));
                    break;

                case TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT: // 101
                    writer.TryWrite(BuildControlFrame(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE));
                    foreach (var slave in _config.DetectedSlaves)
                    {
                        writer.TryWrite(BuildSlaveInfoFrame(slave));
                    }
                    break;

                case TrackCommand.EXEC_MBUS_STATE_SLAVE_INIT: // 104
                    writer.TryWrite(BuildControlFrame(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_INIT, TaskStates.DONE));
                    break;

                case TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE: // 105
                    writer.TryWrite(BuildControlFrame(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE, TaskStates.DONE));
                    break;

                case TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH: // 108
                    HandleDataExchange(frame, writer);
                    break;

                default:
                    // Any other command (FW/bootloader 120..243, etc.) is intentionally not emulated.
                    break;
            }
        }

        /// <summary>
        /// Handles an <c>EXEC_MBUS_SLAVE_DATA_EXCH</c> write:
        /// <c>data = frame[2..]</c> with <c>slave=data[0]</c>, <c>direction=data[1]</c>,
        /// <c>regCount=data[2]</c>, <c>startReg=data[3]</c>, then <c>regCount</c> ushort LE values.
        /// </summary>
        private void HandleDataExchange(byte[] frame, ChannelWriter<byte[]> writer)
        {
            // [0]=HEADER, [1]=108, [2]=slave, [3]=direction, [4]=regCount, [5]=startReg, [6..]=values
            if (frame.Length < 6)
                return;

            byte slave = frame[2];
            byte direction = frame[3];
            byte regCount = frame[4];
            byte startReg = frame[5];

            if (direction != WriteDirection)
                return; // only writes are emulated

            var registers = GetOrCreateRegisters(slave);

            for (int i = 0; i < regCount; i++)
            {
                int registerIndex = startReg + i;
                if (registerIndex < 0 || registerIndex >= registers.Length)
                    continue;

                // Never overwrite the firmware checksum (HR11) reported to the init pipeline.
                if (registerIndex == FirmwareChecksumRegister)
                    continue;

                int byteIndex = 6 + i * 2;
                if (byteIndex + 1 >= frame.Length)
                    break;

                ushort value = (ushort)(frame[byteIndex] | (frame[byteIndex + 1] << 8));
                registers[registerIndex] = value;
            }

            // A configured "dropped echo" slave still receives the write but produces no
            // SLAVEINFO readback, so the control path's copy of that slave goes stale.
            if (_droppedEchoSlaves.Contains(slave))
                return;

            writer.TryWrite(BuildSlaveInfoFrame(slave));
        }

        // --------------------------------------------------------------------
        // Frame builders
        // --------------------------------------------------------------------

        private static byte[] BuildControlFrame(byte sender, byte taskCommand, byte taskState)
            => new byte[] { Header, sender, taskCommand, taskState, 0x00 };

        private byte[] BuildSlaveInfoFrame(byte slave)
        {
            var detected = _detected.Contains(slave) ? (byte)1 : (byte)0;
            var registers = GetOrCreateRegisters(slave);
            ushort? pwmOverride = _config.Hr0ReadbackOverrides.TryGetValue(slave, out var overrideValue)
                ? overrideValue
                : (ushort?)null;

            return BuildSlaveInfoFrame(slave, detected, registers, pwmOverride);
        }

        /// <summary>
        /// Exact 41-byte SLAVEINFO frame layout:
        /// <c>[0xAA, 0xFF, 0xAA, slave, detected, 0x00, HR0..HR11 LE, mbReceiveCounter LE,
        /// mbSentCounter LE, mbCommError LE(u32), mbExceptionCode, spiCommErrorCounter, 0x55]</c>.
        /// </summary>
        private static byte[] BuildSlaveInfoFrame(byte slave, byte detected, ushort[] registers, ushort? pwmOverride = null)
        {
            var frame = new byte[41];

            frame[0] = Header;      // HEADER
            frame[1] = SlaveInfo;   // SLAVEINFO
            frame[2] = Header;      // mbHeader
            frame[3] = slave;
            frame[4] = detected;
            frame[5] = 0x00;        // padding

            for (int i = 0; i < HoldingRegisterCount; i++)
            {
                ushort value = (i == TrackAmplifierRegisters.PwmCommand && pwmOverride is not null)
                    ? pwmOverride.Value
                    : registers[i];

                frame[6 + i * 2] = (byte)(value & 0xFF);
                frame[7 + i * 2] = (byte)((value >> 8) & 0xFF);
            }

            // mbReceiveCounter (ushort LE) at 30..31
            frame[30] = 0x00;
            frame[31] = 0x00;

            // mbSentCounter (ushort LE) at 32..33
            frame[32] = 0x00;
            frame[33] = 0x00;

            // mbCommError (uint32 LE) at 34..37
            frame[34] = 0x00;
            frame[35] = 0x00;
            frame[36] = 0x00;
            frame[37] = 0x00;

            // mbExceptionCode at 38
            frame[38] = 0x00;

            // spiCommErrorCounter at 39
            frame[39] = 0x00;

            // footer at 40
            frame[40] = Footer; // 0x55

            return frame;
        }

        /// <summary>Gets the register array for a slave, creating a fresh one when absent. Call under <c>_sync</c>.</summary>
        private ushort[] GetOrCreateRegisters(byte slave)
        {
            if (!_registers.TryGetValue(slave, out var registers))
            {
                registers = new ushort[HoldingRegisterCount];
                _registers[slave] = registers;
            }

            return registers;
        }
    }
}
