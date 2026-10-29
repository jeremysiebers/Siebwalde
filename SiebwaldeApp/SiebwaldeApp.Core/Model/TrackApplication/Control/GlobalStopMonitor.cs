using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>Runtime-owned PIC32 status, heartbeat and receive freshness monitor. No slave writes on link loss.</summary>
    public sealed class GlobalStopMonitor : IDisposable
    {
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMilliseconds(500);
        private readonly ITrackCommClient _comm;
        private readonly TrackApplicationVariables _variables;
        private readonly GlobalStopController _stop;
        private DateTimeOffset? _lastStatus;
        private DateTimeOffset? _lastHeartbeat;
        private ushort _sequence;
        private bool _stopWasLatched;
        private bool _linkEstablished;
        public byte? LastObservedPic32Status { get; private set; }

        /// <summary>Called after full-chain startup/connection succeeds, never merely on object creation.
        /// A new transport session must establish its own link, regardless of retained latch state.</summary>
        public void EstablishLink(DateTimeOffset now)
        {
            lock (_stop.SyncRoot)
            {
                if (_linkEstablished) return;
                _linkEstablished = true;
                _lastStatus = now; // receive-loss grace starts at link establishment
            }
        }

        public GlobalStopMonitor(ITrackCommClient comm, TrackApplicationVariables variables)
        {
            _comm = comm;
            _variables = variables;
            _stop = variables.GlobalStop;
            _comm.ControlMessageReceived += OnStatus;
        }

        private void OnStatus(object? sender, ControlMessageEventArgs e)
            => ObserveStatus(e.Message, DateTimeOffset.UtcNow);

        public void ObserveStatus(ReceivedMessage message, DateTimeOffset now)
        {
            if (message.TaskId != TaskId.CONTROLLER || message.Taskcommand != TrackCommand.GLOBAL_STOP_STATE ||
                message.Taskstate != TaskStates.DONE || message.Taskmessage > GlobalStopStatusMessage.Armed) return;
            lock (_stop.SyncRoot)
            {
                _lastStatus = now;
                LastObservedPic32Status = (byte)message.Taskmessage;
                if (message.Taskmessage == GlobalStopStatusMessage.Armed) _stop.Arm();
                else if (message.Taskmessage == GlobalStopStatusMessage.LatchedHostLoss)
                    _stop.Latch(GlobalStopReason.Pic32HostLoss);
                else if (message.Taskmessage == GlobalStopStatusMessage.LatchedHostInitiated)
                    _stop.Latch(GlobalStopReason.Pic32HostInitiated);
                // Disarmed is informational, never a latch reset or a heartbeat prohibition.
            }
        }

        /// <summary>Called serially by the runtime loop; explicit time makes cadence/loss boundaries testable.</summary>
        public async Task TickAsync(DateTimeOffset now, CancellationToken ct = default)
        {
            Task? send = null;
            lock (_stop.SyncRoot)
            {
                if (_linkEstablished && !_stop.IsLatched)
                {
                    var freshStatus = _lastStatus is { } status && now >= status &&
                        now - status < TrackAmplifierDataFreshness.DefaultStaleAfter;
                    var freshReadback = _variables.trackAmpItems.Any(a =>
                        TrackAmplifierDataFreshness.HasFreshProtocolReadback(a, now,
                            TrackAmplifierDataFreshness.DefaultStaleAfter));
                    if (!freshStatus && !freshReadback) _stop.Latch(GlobalStopReason.Pic32Lost);
                }
                if (_stop.IsLatched)
                {
                    // 109 (EXEC_GLOBAL_STOP) on the latch rising edge, not once-per-lifetime: after an
                    // authorized re-arm clears the latch, a subsequent latch must re-assert the stop.
                    if (!_stopWasLatched && !_stop.SuppressSlaveWrites)
                    {
                        send = _comm.SendAsync(new SendMessage(TrackCommand.EXEC_GLOBAL_STOP, new byte[] { 0, 0 }), ct);
                    }
                    _stopWasLatched = true;
                }
                else
                {
                    _stopWasLatched = false;
                    if (_linkEstablished &&
                        (_lastHeartbeat is null || now - _lastHeartbeat >= HeartbeatInterval))
                    {
                        _lastHeartbeat = now;
                        // Wire counter is monotonic modulo 65536, scoped to this transport session.
                        var sequence = _sequence++;
                        send = _comm.SendAsync(new SendMessage(TrackCommand.HOST_HEARTBEAT,
                            new byte[] { (byte)sequence, (byte)(sequence >> 8) }), ct);
                    }
                }
            }
            if (send is not null) await send.ConfigureAwait(false);
        }

        public void Dispose() => _comm.ControlMessageReceived -= OnStatus;
    }
}
