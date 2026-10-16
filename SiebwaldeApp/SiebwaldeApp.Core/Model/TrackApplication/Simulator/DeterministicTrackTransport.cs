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

        /// <summary>
        /// Per-slave override for the HR_APPLIED_PWM (HoldingReg14) value reported in a SLAVEINFO
        /// readback, used to model "command echo neutral but applied output non-neutral" — the
        /// applied-evidence mismatch the Phase-1 movement gate must NOT paper over. Empty by default.
        /// </summary>
        public IReadOnlyDictionary<byte, ushort> AppliedPwmReadbackOverrides { get; }

        /// <summary>
        /// When set, the transport emits a fresh SLAVEINFO frame for every detected slave on this
        /// interval (modelling the real master's cyclic FC03 refresh). When null, the transport
        /// stays fully deterministic with no timer (the pre-existing contract).
        /// </summary>
        public TimeSpan? PeriodicSlaveInfoInterval { get; }
        /// <summary>Opt-in armed confirmation on the first host heartbeat, modelling PIC32 bootstrap.
        /// Software protocol model only, not proof of physical watchdog behaviour.</summary>
        public bool EmitGlobalStopArmedStatus { get; }

        public TrackSimulatorConfig(
            ushort firmwareChecksum = DefaultFirmwareChecksum,
            IReadOnlyList<byte>? detectedSlaves = null,
            IReadOnlyCollection<byte>? droppedEchoSlaves = null,
            IReadOnlyDictionary<byte, ushort>? hr0ReadbackOverrides = null,
            IReadOnlyDictionary<byte, ushort>? appliedPwmReadbackOverrides = null,
            TimeSpan? periodicSlaveInfoInterval = null,
            bool emitGlobalStopArmedStatus = false)
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
            AppliedPwmReadbackOverrides = appliedPwmReadbackOverrides is null
                ? new Dictionary<byte, ushort>()
                : new Dictionary<byte, ushort>(appliedPwmReadbackOverrides);
            PeriodicSlaveInfoInterval = periodicSlaveInfoInterval;
            EmitGlobalStopArmedStatus = emitGlobalStopArmedStatus;
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
    public sealed class DeterministicTrackTransport : ITrackTransport, ISimulatedTrackIo
    {
        private const byte Header = 0xAA;
        private const byte SlaveInfo = 0xFF;
        private const byte Footer = 0x55;
        private const byte WriteDirection = 0xAA;   // Enums.WRITE

        private readonly TrackSimulatorConfig _config;
        private readonly Dictionary<byte, SimulatedTrackAmplifier> _amplifiers = new();
        private readonly HashSet<byte> _detected = new();
        private readonly HashSet<byte> _droppedEchoSlaves = new();
        private readonly List<byte> _sentCommands = new();
        private bool _globalStopArmed;
        private readonly List<byte[]> _sentFrames = new();
        public IReadOnlyList<byte[]> SentFrames
        {
            get { lock (_sync) return _sentFrames.ConvertAll(f => (byte[])f.Clone()); }
        }
        private readonly object _sync = new();

        private Channel<byte[]>? _frames;
        private CancellationTokenSource? _cts;
        private Task? _heartbeatTask;

        public DeterministicTrackTransport(TrackSimulatorConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            foreach (var slave in _config.DetectedSlaves)
            {
                _amplifiers[slave] = new SimulatedTrackAmplifier(slave, firmwareChecksum: _config.FirmwareChecksum);
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
                if (!_amplifiers.TryGetValue(slaveNumber, out var amplifier))
                    throw new KeyNotFoundException($"No registers for slave {slaveNumber}.");

                return amplifier.Snapshot();
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
                _globalStopArmed = false;

                // Cyclic SLAVEINFO refresh: models the real master's periodic FC03 read that keeps
                // the C# holding-register view fresh. Only started when a non-null interval is set.
                if (_config.PeriodicSlaveInfoInterval is { } interval)
                {
                    var token = _cts.Token;
                    _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(interval, token));
                }
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
                    _sentFrames.Add(copy);
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
        // ISimulatedTrackIo
        // --------------------------------------------------------------------

        /// <inheritdoc />
        public IReadOnlyList<byte> DetectedSlaves
        {
            get
            {
                lock (_sync)
                {
                    return new List<byte>(_config.DetectedSlaves);
                }
            }
        }

        /// <inheritdoc />
        public void SetSlaveOccupancy(byte slaveNumber, bool occupied)
        {
            lock (_sync)
            {
                // Not-opened or unknown slave: safe no-op, mirroring the defensive dispatch behavior.
                var writer = _frames?.Writer;
                if (writer is null || !_amplifiers.TryGetValue(slaveNumber, out var amplifier))
                    return;

                var status = amplifier.Status;
                if (occupied)
                    status |= TrackAmplifierRegisters.OccupiedBit;
                else
                    status = (ushort)(status & ~TrackAmplifierRegisters.OccupiedBit);
                amplifier.SetStatus(status);

                // Emit a fresh frame immediately so the change is observed before the next heartbeat.
                writer.TryWrite(BuildSlaveInfoFrame(slaveNumber));
            }
        }

        /// <inheritdoc />
        public void SetSlaveStatus(byte slaveNumber, ushort status)
        {
            lock (_sync)
            {
                // Not-opened or unknown slave: safe no-op, mirroring the defensive dispatch behavior.
                var writer = _frames?.Writer;
                if (writer is null || !_amplifiers.TryGetValue(slaveNumber, out var amplifier))
                    return;

                amplifier.SetStatus(status);

                // Emit a fresh frame immediately so the change is observed before the next heartbeat.
                writer.TryWrite(BuildSlaveInfoFrame(slaveNumber));
            }
        }

        /// <summary>
        /// Emits a SLAVEINFO frame for every detected slave on <paramref name="interval"/> until
        /// cancellation. Only started when <see cref="TrackSimulatorConfig.PeriodicSlaveInfoInterval"/>
        /// is set; it is stopped by the transport's <c>_cts</c> cancellation in <see cref="CloseAsync"/>.
        /// </summary>
        private async Task HeartbeatLoopAsync(TimeSpan interval, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(interval, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    lock (_sync)
                    {
                        var writer = _frames?.Writer;
                        if (writer is null)
                            break;

                        foreach (var slave in _config.DetectedSlaves)
                        {
                            writer.TryWrite(BuildSlaveInfoFrame(slave));
                        }
                    }
                }
            }
            catch
            {
                // Best-effort background loop; exits on cancellation or disposal.
            }
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

                case TrackCommand.HOST_HEARTBEAT:
                    if (_config.EmitGlobalStopArmedStatus && !_globalStopArmed && frame.Length == 4)
                    {
                        _globalStopArmed = true;
                        writer.TryWrite(new byte[] { Header, TaskId.CONTROLLER, TrackCommand.GLOBAL_STOP_STATE,
                            TaskStates.DONE, GlobalStopStatusMessage.Armed });
                    }
                    break;

                case TrackCommand.EXEC_GLOBAL_STOP:
                    if (frame.Length == 4 && frame[2] == 0 && frame[3] == 0)
                        writer.TryWrite(BuildGlobalStopStateFrame(GlobalStopStatusMessage.LatchedHostInitiated));
                    // Per-amplifier neutral is delivered through the production 108 writer.
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
        /// Handles an <c>EXEC_MBUS_SLAVE_DATA_EXCH</c> exchange:
        /// <c>data = frame[2..]</c> with <c>slave=data[0]</c>, <c>direction=data[1]</c>,
        /// <c>regCount=data[2]</c>, <c>startReg=data[3]</c>, then <c>regCount</c> ushort LE values.
        /// A write (0xAA) is applied to the amplifier model; a read (0x55) advances the model one
        /// tick and returns a fresh SLAVEINFO readback (modelling the master's FC03 poll).
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

            var amplifier = GetOrCreateAmplifier(slave);

            if (direction == WriteDirection)
            {
                var values = new ushort[regCount];
                for (int i = 0; i < regCount; i++)
                {
                    int byteIndex = 6 + i * 2;
                    if (byteIndex + 1 >= frame.Length)
                        break;

                    values[i] = (ushort)(frame[byteIndex] | (frame[byteIndex + 1] << 8));
                }

                amplifier.ApplyWrite(startReg, values);
            }
            else
            {
                // A fresh FC03 read: advance the ramp/lease one tick before the readback snapshot.
                amplifier.Step();
            }

            // A configured "dropped echo" slave still receives the write but produces no
            // SLAVEINFO readback, so the control path's copy of that slave goes stale.
            if (_droppedEchoSlaves.Contains(slave))
                return;

            writer.TryWrite(BuildSlaveInfoFrame(slave));
        }

        /// <summary>Advances the ramp/lease of every known amplifier by one tick (test-visible).</summary>
        public void StepAll()
        {
            lock (_sync)
            {
                foreach (var amplifier in _amplifiers.Values)
                {
                    amplifier.Step();
                }
            }
        }

        /// <summary>Advances the ramp/lease of one amplifier by one tick (test-visible).</summary>
        public void Step(byte slave)
        {
            lock (_sync)
            {
                if (_amplifiers.TryGetValue(slave, out var amplifier))
                {
                    amplifier.Step();
                }
            }
        }

        // --------------------------------------------------------------------
        // Frame builders
        // --------------------------------------------------------------------

        private static byte[] BuildControlFrame(byte sender, byte taskCommand, byte taskState)
            => new byte[] { Header, sender, taskCommand, taskState, 0x00 };

        /// <summary>
        /// Builds the Amendment A2 <c>GLOBAL_STOP_STATE</c> (111) frame the PIC32 emits on stop
        /// delivery: <c>[HEADER, CONTROLLER, 111, DONE, reason, count, codes...]</c>. <c>count</c> is
        /// the number of detected amplifiers; the 2-bit per-amplifier codes are packed LSB-first,
        /// four per byte, in ascending detected slave-address order (mirror of
        /// <c>REPORTxGLOBALxSTOPxDELIVERY</c>). The deterministic simulator applies the stop write
        /// synchronously, so every detected amplifier reports <c>Confirmed</c> (0) and the packed
        /// code bytes are all zero.
        /// </summary>
        private byte[] BuildGlobalStopStateFrame(byte reason)
        {
            var detected = _config.DetectedSlaves;
            int count = detected.Count;
            int codeBytes = (count + 3) / 4; // ceil(count / 4)
            var frame = new byte[6 + codeBytes]; // header, sender, cmd, state, reason, count, codes
            frame[0] = Header;
            frame[1] = TaskId.CONTROLLER;
            frame[2] = TrackCommand.GLOBAL_STOP_STATE;
            frame[3] = TaskStates.DONE;
            frame[4] = reason;
            frame[5] = (byte)count;
            // frame[6..] stay zero: all-detected Confirmed (0).
            return frame;
        }

        private byte[] BuildSlaveInfoFrame(byte slave)
        {
            var detected = _detected.Contains(slave) ? (byte)1 : (byte)0;
            var registers = GetOrCreateAmplifier(slave).Snapshot();
            ushort? pwmOverride = _config.Hr0ReadbackOverrides.TryGetValue(slave, out var overrideValue)
                ? overrideValue
                : (ushort?)null;
            ushort? appliedOverride = _config.AppliedPwmReadbackOverrides.TryGetValue(slave, out var appliedValue)
                ? appliedValue
                : (ushort?)null;

            return BuildSlaveInfoFrame(slave, detected, registers, pwmOverride, appliedOverride);
        }

        /// <summary>
        /// Exact register-count-derived SLAVEINFO frame layout:
        /// <c>[0xAA, 0xFF, 0xAA, slave, detected, 0x00, HR0..HR20 LE, mbReceiveCounter LE,
        /// mbSentCounter LE, mbCommError LE(u32), mbExceptionCode, spiCommErrorCounter, 0x55]</c>.
        /// </summary>
        private static byte[] BuildSlaveInfoFrame(byte slave, byte detected, ushort[] registers,
            ushort? pwmOverride = null, ushort? appliedOverride = null)
        {
            var snapshot = (ushort[])registers.Clone();
            if (pwmOverride is { } value) snapshot[TrackAmplifierRegisters.PwmCommand] = value;
            if (appliedOverride is { } applied) snapshot[TrackAmplifierRegisters.AppliedPwm] = applied;
            return TrackSlaveInfoFrame.Encode(slave, detected, snapshot);
        }

        /// <summary>Gets the amplifier model for a slave, creating a fresh (identity-less) one when
        /// absent. Call under <c>_sync</c>.</summary>
        private SimulatedTrackAmplifier GetOrCreateAmplifier(byte slave)
        {
            if (!_amplifiers.TryGetValue(slave, out var amplifier))
            {
                amplifier = new SimulatedTrackAmplifier(slave, identity: 0);
                _amplifiers[slave] = amplifier;
            }

            return amplifier;
        }
    }
}
