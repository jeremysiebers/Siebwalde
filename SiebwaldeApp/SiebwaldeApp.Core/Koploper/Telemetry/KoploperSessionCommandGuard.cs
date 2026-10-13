using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Stateful outbound session-command boundary for a LIVE_HARDWARE bounded telemetry-enablement
    /// host. It wraps an <see cref="ITrackCommClient"/> and forwards ONLY the six telemetry-enablement
    /// commands declared in <see cref="KoploperSessionCommandAllowlist"/>, in the mandated cold-start
    /// order (250 → 100 → 101 → 104 → 106 → 105), AND waits (bounded) for each command's expected
    /// master acknowledgement (a status frame with the matching TaskId/Taskcommand/Taskstate == DONE)
    /// before <see cref="SendAsync"/> returns. Only then may the host send the next command.
    /// <para>
    /// 108 (EXEC_MBUS_SLAVE_DATA_EXCH — the only HR0/PWM/movement write vector), 107
    /// (EXEC_MBUS_STATE_RESET) and every other command are fail-closed: counted as a rejected write,
    /// never forwarded, and reported via <see cref="KoploperSessionCommandRejectedException"/>. An
    /// out-of-order permitted command is DENIED and the session is STOPPED (via
    /// <see cref="KoploperSessionCommandOrderViolationException"/>); a transmitted command whose
    /// acknowledgement times out is NOT-acknowledged and the session is STOPPED (via
    /// <see cref="KoploperSessionCommandAcknowledgementTimeoutException"/>). "Transmitted" (forwarded
    /// to the transport) and "Acknowledged" (matching DONE received) are tracked separately.
    /// </para>
    /// </summary>
    public sealed class KoploperSessionCommandGuard : ITrackCommClient
    {
        private static readonly TimeSpan DefaultAcknowledgementTimeout = TimeSpan.FromSeconds(10);

        private readonly ITrackCommClient _inner;
        private readonly object _sync = new object();
        private readonly TimeSpan _acknowledgementTimeout;
        private readonly Func<DateTimeOffset> _clock;

        private readonly Dictionary<byte, long> _transmittedByCommand = new Dictionary<byte, long>();
        private readonly Dictionary<byte, long> _acknowledgedByCommand = new Dictionary<byte, long>();
        private readonly List<KoploperSessionCommandEvent> _events = new List<KoploperSessionCommandEvent>();
        private long _rejectedCommandCount;
        private long _orderViolationCount;
        private long _acknowledgementTimeoutCount;
        private long _sequence;

        private KoploperSessionCommandMode _mode = KoploperSessionCommandMode.ColdStart;
        private int _coldStartIndex;
        private bool _stopped;

        private PendingAcknowledgement? _pendingAck;
        private ReceivedMessage? _lastMasterState;

        /// <summary>
        /// Creates the guard. The default acknowledgement timeout is 10 seconds (a bounded session
        /// timeout, NOT a safety/control budget); pass a shorter value in tests.
        /// </summary>
        public KoploperSessionCommandGuard(
            ITrackCommClient inner,
            TimeSpan? acknowledgementTimeout = null,
            Func<DateTimeOffset>? clock = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _acknowledgementTimeout = acknowledgementTimeout ?? DefaultAcknowledgementTimeout;
            _clock = clock ?? (() => DateTimeOffset.UtcNow);

            _inner.ControlMessageReceived += OnInnerControlMessage;
        }

        /// <inheritdoc />
        public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived
        {
            add => _inner.AmplifierDataReceived += value;
            remove => _inner.AmplifierDataReceived -= value;
        }

        /// <inheritdoc />
        public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived
        {
            add => _inner.ControlMessageReceived += value;
            remove => _inner.ControlMessageReceived -= value;
        }

        /// <inheritdoc />
        public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
            => _inner.StartAsync(realHardwareMode, cancellationToken);

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken = default)
            => _inner.StopAsync(cancellationToken);

        /// <summary>
        /// Resets the session to a fresh cold start: the full six-command sequence must be sent again
        /// in order. Counters/event history are cumulative and are NOT reset.
        /// </summary>
        public void BeginNewSession()
        {
            lock (_sync)
            {
                _mode = KoploperSessionCommandMode.ColdStart;
                _coldStartIndex = 0;
                _stopped = false;
            }
        }

        /// <summary>
        /// Switches to warm-reconnect mode: only CLIENT_CONNECTION_REQUEST (250) is accepted, and no
        /// enablement command is re-sent. Counters/event history are cumulative.
        /// </summary>
        public void BeginWarmReconnect()
        {
            lock (_sync)
            {
                _mode = KoploperSessionCommandMode.WarmReconnect;
                _coldStartIndex = 0;
                _stopped = false;
            }
        }

        /// <summary>
        /// Forwards <paramref name="message"/> only when its <see cref="SendMessage.Command"/> is in
        /// <see cref="KoploperSessionCommandAllowlist"/> AND satisfies the session mode/order, then
        /// waits (bounded) for the expected acknowledgement. A non-permitted command raises
        /// <see cref="KoploperSessionCommandRejectedException"/>; an out-of-order/state violation
        /// raises <see cref="KoploperSessionCommandOrderViolationException"/>; a missing/timeout
        /// acknowledgement raises <see cref="KoploperSessionCommandAcknowledgementTimeoutException"/>
        /// and stops the session. Only a fully-acknowledged command returns normally.
        /// </summary>
        public async Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
        {
            byte command = message.Command;
            DateTimeOffset requestedAt = _clock();

            long sequence;
            TaskCompletionSource<ReceivedMessage>? tcs = null;
            PendingAcknowledgement? pending = null;

            lock (_sync)
            {
                sequence = _sequence + 1;
                _sequence = sequence;

                // 1. Structural allowlist boundary: 108/107/arbitrary never pass, regardless of state.
                if (!KoploperSessionCommandAllowlist.IsPermitted(command))
                {
                    _rejectedCommandCount++;
                    _events.Add(new KoploperSessionCommandEvent(
                        sequence, command, requestedAt, null, null,
                        KoploperSessionCommandOutcome.Rejected, null));
                    throw new KoploperSessionCommandRejectedException(command);
                }

                // 2. Session-order/state boundary.
                if (_stopped)
                {
                    _orderViolationCount++;
                    _events.Add(new KoploperSessionCommandEvent(
                        sequence, command, requestedAt, null, null,
                        KoploperSessionCommandOutcome.OrderViolation, null));
                    throw new KoploperSessionCommandOrderViolationException(
                        command, KoploperSessionCommandOrderViolationReason.SessionStopped, null, _mode);
                }

                if (_mode == KoploperSessionCommandMode.WarmReconnect)
                {
                    if (command != KoploperSessionCommandAllowlist.ClientConnectionRequest)
                    {
                        _stopped = true;
                        _orderViolationCount++;
                        _events.Add(new KoploperSessionCommandEvent(
                            sequence, command, requestedAt, null, null,
                            KoploperSessionCommandOutcome.OrderViolation, null));
                        throw new KoploperSessionCommandOrderViolationException(
                            command,
                            KoploperSessionCommandOrderViolationReason.WarmReconnectEnablementNotAllowed,
                            KoploperSessionCommandAllowlist.ClientConnectionRequest,
                            _mode);
                    }
                }
                else
                {
                    if (_coldStartIndex >= KoploperSessionCommandAllowlist.ColdStartOrder.Count)
                    {
                        _stopped = true;
                        _orderViolationCount++;
                        _events.Add(new KoploperSessionCommandEvent(
                            sequence, command, requestedAt, null, null,
                            KoploperSessionCommandOutcome.OrderViolation, null));
                        throw new KoploperSessionCommandOrderViolationException(
                            command, KoploperSessionCommandOrderViolationReason.ColdSequenceComplete, null, _mode);
                    }

                    byte expected = KoploperSessionCommandAllowlist.ColdStartOrder[_coldStartIndex];
                    if (command != expected)
                    {
                        _stopped = true;
                        _orderViolationCount++;
                        _events.Add(new KoploperSessionCommandEvent(
                            sequence, command, requestedAt, null, null,
                            KoploperSessionCommandOutcome.OrderViolation, null));
                        throw new KoploperSessionCommandOrderViolationException(
                            command, KoploperSessionCommandOrderViolationReason.OutOfOrder, expected, _mode);
                    }

                    _coldStartIndex++;
                }

                // 3. Mark transmitted and arm the acknowledgement wait.
                _transmittedByCommand[command] =
                    _transmittedByCommand.TryGetValue(command, out long current) ? current + 1 : 1;

                if (KoploperSessionCommandAllowlist.TryGetExpectedAcknowledgement(
                        command, out ushort taskId, out ushort taskCommand, out ushort taskState))
                {
                    tcs = new TaskCompletionSource<ReceivedMessage>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    pending = new PendingAcknowledgement(taskId, taskCommand, taskState, tcs);
                    _pendingAck = pending;
                }
            }

            DateTimeOffset transmittedAt = _clock();
            await _inner.SendAsync(message, cancellationToken).ConfigureAwait(false);

            if (tcs is null || pending is null)
            {
                // Defensive: no acknowledgement expected (never occurs for the six permitted commands).
                lock (_sync)
                {
                    _events.Add(new KoploperSessionCommandEvent(
                        sequence, command, requestedAt, transmittedAt, null,
                        KoploperSessionCommandOutcome.Acknowledged, null));
                }

                return;
            }

            Task winner = await Task
                .WhenAny(tcs.Task, Task.Delay(_acknowledgementTimeout, cancellationToken))
                .ConfigureAwait(false);

            if (winner == tcs.Task)
            {
                ReceivedMessage acknowledgement = await tcs.Task.ConfigureAwait(false);
                DateTimeOffset responseAt = _clock();

                lock (_sync)
                {
                    _acknowledgedByCommand[command] =
                        _acknowledgedByCommand.TryGetValue(command, out long acked) ? acked + 1 : 1;
                    _pendingAck = null;
                    _events.Add(new KoploperSessionCommandEvent(
                        sequence, command, requestedAt, transmittedAt, responseAt,
                        KoploperSessionCommandOutcome.Acknowledged, acknowledgement));
                }
            }
            else
            {
                lock (_sync)
                {
                    _acknowledgementTimeoutCount++;
                    _stopped = true;
                    _pendingAck = null;
                    _events.Add(new KoploperSessionCommandEvent(
                        sequence, command, requestedAt, transmittedAt, null,
                        KoploperSessionCommandOutcome.NotAcknowledged, null));
                }

                throw new KoploperSessionCommandAcknowledgementTimeoutException(
                    command, pending.TaskId, pending.TaskCommand, pending.TaskState);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            _inner.ControlMessageReceived -= OnInnerControlMessage;
            await _inner.DisposeAsync().ConfigureAwait(false);
        }

        private void OnInnerControlMessage(object? sender, ControlMessageEventArgs e)
        {
            ReceivedMessage message = e.Message;
            TaskCompletionSource<ReceivedMessage>? toComplete = null;

            lock (_sync)
            {
                _lastMasterState = message;

                if (_pendingAck is not null && _pendingAck.Matches(message))
                {
                    toComplete = _pendingAck.Tcs;
                    _pendingAck = null;
                }
            }

            toComplete?.TrySetResult(message);
        }

        /// <summary>The current session mode (cold start or warm reconnect).</summary>
        public KoploperSessionCommandMode Mode
        {
            get
            {
                lock (_sync)
                {
                    return _mode;
                }
            }
        }

        /// <summary>True once the session has been stopped by an order/state violation or a timeout.</summary>
        public bool IsStopped
        {
            get
            {
                lock (_sync)
                {
                    return _stopped;
                }
            }
        }

        /// <summary>
        /// The next expected command: the cold-start successor, or 250 in warm-reconnect mode;
        /// <c>null</c> when the sequence is complete or the session is stopped.
        /// </summary>
        public byte? ExpectedCommand
        {
            get
            {
                lock (_sync)
                {
                    if (_stopped)
                    {
                        return null;
                    }

                    if (_mode == KoploperSessionCommandMode.WarmReconnect)
                    {
                        return KoploperSessionCommandAllowlist.ClientConnectionRequest;
                    }

                    if (_coldStartIndex >= KoploperSessionCommandAllowlist.ColdStartOrder.Count)
                    {
                        return null;
                    }

                    return KoploperSessionCommandAllowlist.ColdStartOrder[_coldStartIndex];
                }
            }
        }

        /// <summary>The most recent master status frame observed (TaskId/Taskcommand/Taskstate), or null.</summary>
        public ReceivedMessage? LastMasterState
        {
            get
            {
                lock (_sync)
                {
                    return _lastMasterState;
                }
            }
        }

        /// <summary>
        /// Per-command-type TRANSMITTED counts for the six permitted session commands. A snapshot copy.
        /// </summary>
        public IReadOnlyDictionary<byte, long> TransmittedByCommand
        {
            get
            {
                lock (_sync)
                {
                    return new Dictionary<byte, long>(_transmittedByCommand);
                }
            }
        }

        /// <summary>
        /// Per-command-type ACKNOWLEDGED counts for the six permitted session commands. A snapshot copy.
        /// </summary>
        public IReadOnlyDictionary<byte, long> AcknowledgedByCommand
        {
            get
            {
                lock (_sync)
                {
                    return new Dictionary<byte, long>(_acknowledgedByCommand);
                }
            }
        }

        /// <summary>Total permitted session-command transmissions forwarded to the inner client.</summary>
        public long TotalTransmitted
        {
            get
            {
                lock (_sync)
                {
                    long total = 0;
                    foreach (long count in _transmittedByCommand.Values)
                    {
                        total += count;
                    }

                    return total;
                }
            }
        }

        /// <summary>Total permitted session commands whose DONE acknowledgement was received.</summary>
        public long TotalAcknowledged
        {
            get
            {
                lock (_sync)
                {
                    long total = 0;
                    foreach (long count in _acknowledgedByCommand.Values)
                    {
                        total += count;
                    }

                    return total;
                }
            }
        }

        /// <summary>Number of outbound commands refused by the allowlist (108/107/arbitrary).</summary>
        public long RejectedCommandCount
        {
            get
            {
                lock (_sync)
                {
                    return _rejectedCommandCount;
                }
            }
        }

        /// <summary>Number of permitted commands denied for order/state violations.</summary>
        public long OrderViolationCount
        {
            get
            {
                lock (_sync)
                {
                    return _orderViolationCount;
                }
            }
        }

        /// <summary>Number of transmitted commands whose acknowledgement timed out.</summary>
        public long AcknowledgementTimeoutCount
        {
            get
            {
                lock (_sync)
                {
                    return _acknowledgementTimeoutCount;
                }
            }
        }

        /// <summary>The ordered lifecycle of every outbound command attempt, for the trace.</summary>
        public IReadOnlyList<KoploperSessionCommandEvent> SessionCommandEvents
        {
            get
            {
                lock (_sync)
                {
                    return _events.ToArray();
                }
            }
        }

        /// <summary>
        /// An immutable, read-only audit snapshot of every counter. The application/movement-control
        /// counters are always zero by construction (the guard has no write path for them).
        /// </summary>
        public KoploperSessionCommandCounters GetCounters()
        {
            lock (_sync)
            {
                return new KoploperSessionCommandCounters(
                    ClientConnectionRequestTransmitted: CountOf(_transmittedByCommand, KoploperSessionCommandAllowlist.ClientConnectionRequest),
                    ClientConnectionRequestAcknowledged: CountOf(_acknowledgedByCommand, KoploperSessionCommandAllowlist.ClientConnectionRequest),
                    ExecMbusStateSlavesOnTransmitted: CountOf(_transmittedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn),
                    ExecMbusStateSlavesOnAcknowledged: CountOf(_acknowledgedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn),
                    ExecMbusStateSlaveDetectTransmitted: CountOf(_transmittedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateSlaveDetect),
                    ExecMbusStateSlaveDetectAcknowledged: CountOf(_acknowledgedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateSlaveDetect),
                    ExecMbusStateSlaveInitTransmitted: CountOf(_transmittedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateSlaveInit),
                    ExecMbusStateSlaveInitAcknowledged: CountOf(_acknowledgedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateSlaveInit),
                    ExecMbusStateStartDataUploadTransmitted: CountOf(_transmittedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload),
                    ExecMbusStateStartDataUploadAcknowledged: CountOf(_acknowledgedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload),
                    ExecMbusStateSlaveEnableTransmitted: CountOf(_transmittedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable),
                    ExecMbusStateSlaveEnableAcknowledged: CountOf(_acknowledgedByCommand, KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable),
                    RejectedCommands: _rejectedCommandCount,
                    OrderViolations: _orderViolationCount,
                    AcknowledgementTimeouts: _acknowledgementTimeoutCount,
                    AmplifierRegisterWrite: 0,
                    AmplifierHr0Writes: 0,
                    PwmWrites: 0,
                    DirectionWrites: 0,
                    BrakeWrites: 0,
                    EmoStopWrites: 0,
                    SwitchWrites: 0,
                    LocomotiveCommands: 0);
            }
        }

        private static long CountOf(IReadOnlyDictionary<byte, long> source, byte command)
            => source.TryGetValue(command, out long count) ? count : 0;

        private sealed class PendingAcknowledgement
        {
            public ushort TaskId { get; }
            public ushort TaskCommand { get; }
            public ushort TaskState { get; }
            public TaskCompletionSource<ReceivedMessage> Tcs { get; }

            public PendingAcknowledgement(
                ushort taskId,
                ushort taskCommand,
                ushort taskState,
                TaskCompletionSource<ReceivedMessage> tcs)
            {
                TaskId = taskId;
                TaskCommand = taskCommand;
                TaskState = taskState;
                Tcs = tcs;
            }

            public bool Matches(ReceivedMessage message)
                => message.TaskId == TaskId
                    && message.Taskcommand == TaskCommand
                    && message.Taskstate == TaskState;
        }
    }
}
