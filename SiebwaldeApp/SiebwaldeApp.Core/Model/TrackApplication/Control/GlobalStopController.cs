using System;

namespace SiebwaldeApp.Core
{
    public enum GlobalStopState { Disarmed, Armed, Latched }
    public enum GlobalStopReason { Koploper, Operator, Pic32Lost, Pic32HostLoss, Pic32HostInitiated }

    /// <summary>Process-lifetime sticky stop. Neither GO, restart nor safety reset re-arms it.</summary>
    public sealed class GlobalStopController
    {
        public object SyncRoot { get; } = new();
        private GlobalStopState _state;
        private GlobalStopReason? _reason;
        private bool _stopNotificationClaimed;
        public GlobalStopState State { get { lock (SyncRoot) return _state; } }
        public GlobalStopReason? Reason { get { lock (SyncRoot) return _reason; } }
        public bool IsLatched => State == GlobalStopState.Latched;
        public bool SuppressSlaveWrites => Reason == GlobalStopReason.Pic32Lost;
        public event Action? Latched;

        /// <summary>Deduplication follows the sticky latch, including ECoS backend recreation.</summary>
        public bool TryClaimStopNotification()
        {
            lock (SyncRoot)
            {
                if (_state != GlobalStopState.Latched || _stopNotificationClaimed) return false;
                _stopNotificationClaimed = true;
                return true;
            }
        }

        /// <summary>Records PIC32 armed confirmation; heartbeat eligibility is owned by the link monitor.</summary>
        public bool Arm()
        {
            lock (SyncRoot)
            {
                if (_state != GlobalStopState.Disarmed) return false;
                _state = GlobalStopState.Armed;
                return true;
            }
        }

        public bool Latch(GlobalStopReason reason)
        {
            lock (SyncRoot)
            {
                if (_state == GlobalStopState.Latched) return false;
                _reason = reason;
                _state = GlobalStopState.Latched;
                // Subscribers synchronously clear stale writes before another writer acquires SyncRoot.
                Latched?.Invoke();
                return true;
            }
        }
    }
}
