using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Non-UI diagnostics surface for the control path. Keeps a bounded recent history and
    /// separately tracks the latched unsafe state, so a consumer can show "current health"
    /// without scanning history, and history without the critical state being overwritten.
    ///
    /// Latching: a <see cref="DiagnosticSeverity.StopRequired"/> diagnostic latches, and the
    /// first one latched is kept as the root cause. It is never cleared automatically - not
    /// even by a later command or by a lower-severity diagnostic - only by an explicit
    /// <see cref="ClearLatch"/> during recovery.
    /// </summary>
    public sealed class ControlDiagnostics
    {
        /// <summary>Default number of history entries kept.</summary>
        public const int DefaultCapacity = 100;

        private readonly int _capacity;
        private readonly Queue<ControlDiagnostic> _recent = new();
        private readonly object _lock = new();

        public ControlDiagnostics(int capacity = DefaultCapacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            }

            _capacity = capacity;
        }

        /// <summary>Raised for every reported diagnostic, before it is stored.</summary>
        public event EventHandler<ControlDiagnostic>? Reported;

        /// <summary>The most recent diagnostics, oldest first.</summary>
        public IReadOnlyList<ControlDiagnostic> Recent
        {
            get
            {
                lock (_lock)
                {
                    return new List<ControlDiagnostic>(_recent);
                }
            }
        }

        /// <summary>
        /// The latched unsafe diagnostic (the root cause), or null when the control path is not
        /// latched unsafe.
        /// </summary>
        public ControlDiagnostic? LatchedUnsafe { get; private set; }

        /// <summary>True while an unsafe divergence is latched.</summary>
        public bool IsUnsafe
        {
            get
            {
                lock (_lock)
                {
                    return LatchedUnsafe is not null;
                }
            }
        }

        /// <summary>
        /// The most severe diagnostic currently known, for a compact health indicator.
        /// </summary>
        public DiagnosticSeverity CurrentSeverity
        {
            get
            {
                lock (_lock)
                {
                    if (LatchedUnsafe is not null)
                    {
                        return LatchedUnsafe.Severity;
                    }

                    var severity = DiagnosticSeverity.Info;
                    foreach (var diagnostic in _recent)
                    {
                        if (diagnostic.Severity > severity)
                        {
                            severity = diagnostic.Severity;
                        }
                    }

                    return severity;
                }
            }
        }

        /// <summary>The most recent diagnostic, or null when nothing has been reported.</summary>
        public ControlDiagnostic? Latest
        {
            get
            {
                lock (_lock)
                {
                    return _recent.Count == 0 ? null : _recent.ToArray()[^1];
                }
            }
        }

        /// <summary>Records a diagnostic and latches it when it requires a stop.</summary>
        public void Report(ControlDiagnostic diagnostic)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            lock (_lock)
            {
                _recent.Enqueue(diagnostic);
                while (_recent.Count > _capacity)
                {
                    _recent.Dequeue();
                }

                // Keep the first latched fault: it is the root cause, and replacing it would
                // hide the reason the layout stopped.
                if (diagnostic.Severity == DiagnosticSeverity.StopRequired && LatchedUnsafe is null)
                {
                    LatchedUnsafe = diagnostic;
                }
            }

            Reported?.Invoke(this, diagnostic);
        }

        /// <summary>
        /// Clears the latched unsafe state. This is the explicit recovery step; nothing else
        /// clears a latched fault.
        /// </summary>
        public void ClearLatch()
        {
            lock (_lock)
            {
                LatchedUnsafe = null;
            }
        }
    }
}
