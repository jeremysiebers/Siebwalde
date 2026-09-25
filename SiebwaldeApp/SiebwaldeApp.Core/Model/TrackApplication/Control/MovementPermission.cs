using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The movement-permission state of the control path.
    ///
    /// Movement is only allowed after neutral has been commanded to every configured track
    /// amplifier and that neutral has been <b>observed</b> (a fresh readback reports the neutral
    /// PWM). Until then the state is <see cref="NotGranted"/>; a stop/restart that has revoked the
    /// permission sets <see cref="Withdrawn"/>. Both are "not granted" for the purpose of the
    /// movement gate; the distinction is kept so a state transition can tell "never established"
    /// apart from "established and then revoked".
    /// </summary>
    public enum MovementPermissionState
    {
        /// <summary>Neutral has not been established; movement is not permitted.</summary>
        NotGranted = 0,

        /// <summary>Neutral was established and movement is permitted.</summary>
        Granted = 1,

        /// <summary>Neutral had been established but was revoked (for example during stop).</summary>
        Withdrawn = 2
    }

    /// <summary>
    /// Read-only view of the movement-permission state, so the movement gate and the safety
    /// interlock can share one source of truth without being able to change it.
    /// </summary>
    public interface IMovementPermissionState
    {
        /// <summary>The current permission state.</summary>
        MovementPermissionState State { get; }

        /// <summary>True only while movement is permitted (state is <see cref="MovementPermissionState.Granted"/>).</summary>
        bool IsGranted { get; }
    }

    /// <summary>
    /// Thread-safe movement-permission state with a change notification.
    ///
    /// It is the single authority for whether non-neutral movement may be commanded. The owner
    /// (the track runtime coordinator) grants it only after observed neutral, withdraws it before
    /// a stop, and resets it for a fresh start. The movement gate and the safety interlock both
    /// read this instance, so the permission can never be bypassed by one path while the other
    /// still enforces it.
    /// </summary>
    public sealed class MovementPermissionController : IMovementPermissionState
    {
        private readonly object _lock = new();
        private MovementPermissionState _state = MovementPermissionState.NotGranted;

        /// <summary>Raised on every state change, carrying the new state.</summary>
        public event EventHandler<MovementPermissionState>? StateChanged;

        /// <inheritdoc />
        public MovementPermissionState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        /// <inheritdoc />
        public bool IsGranted => State == MovementPermissionState.Granted;

        /// <summary>Permits movement (state becomes <see cref="MovementPermissionState.Granted"/>).</summary>
        public void Grant() => Set(MovementPermissionState.Granted);

        /// <summary>Revokes movement permission (state becomes <see cref="MovementPermissionState.Withdrawn"/>).</summary>
        public void Withdraw() => Set(MovementPermissionState.Withdrawn);

        /// <summary>Returns to the initial never-granted state (<see cref="MovementPermissionState.NotGranted"/>).</summary>
        public void ResetToNotGranted() => Set(MovementPermissionState.NotGranted);

        private void Set(MovementPermissionState state)
        {
            bool changed;
            lock (_lock)
            {
                changed = _state != state;
                if (changed)
                {
                    _state = state;
                }
            }

            if (changed)
            {
                StateChanged?.Invoke(this, state);
            }
        }
    }
}
