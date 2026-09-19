using System;
using System.Collections.Generic;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Central movement interlock for the control path. Every ECoS movement command passes
    /// through here before it reaches a hardware backend, so the safety policy cannot be
    /// bypassed by a later command from Koploper.
    ///
    /// While a <see cref="DiagnosticSeverity.StopRequired"/> fault is latched:
    ///
    /// - a fault attributed to one locomotive blocks non-zero movement for that locomotive only;
    /// - a layout-wide fault blocks non-zero movement for every locomotive and refuses to
    ///   re-enable power;
    /// - stopping is always allowed (speed 0, and power off);
    /// - switch commands are always forwarded, because a corrective switch change is often the
    ///   only way to resolve the divergence.
    ///
    /// A rejected movement is reported once per affected locomotive while the latch holds, so a
    /// repeated command cannot flood the diagnostics, and it is returned as "not applied" so the
    /// ECoS backend never acknowledges it.
    /// </summary>
    public sealed class ControlSafetyInterlockBackend : IHardwareBackend
    {
        /// <summary>Rejection subject used for layout-wide (power) rejections.</summary>
        private const int LayoutSubject = -1;

        private readonly IHardwareBackend _inner;
        private readonly ControlSafetyGuard _guard;
        private readonly ControlDiagnostics _diagnostics;
        private readonly Action<string>? _log;
        private readonly HashSet<int> _reportedRejections = new();
        private readonly object _lock = new();

        public ControlSafetyInterlockBackend(
            IHardwareBackend inner,
            ControlSafetyGuard guard,
            ControlDiagnostics diagnostics,
            Action<string>? log = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _guard = guard ?? throw new ArgumentNullException(nameof(guard));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _log = log;
        }

        /// <inheritdoc />
        public bool SetPower(bool on)
        {
            if (!on)
            {
                // Removing power can only make things safer, so it is never refused.
                return _inner.SetPower(false);
            }

            if (_guard.AllowsPowerOn())
            {
                return _inner.SetPower(true);
            }

            ReportRejection(LayoutSubject, "power-on refused: a layout-wide safety fault is latched.");
            return false;
        }

        /// <inheritdoc />
        public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
        {
            // A stop command is always allowed, and an unlatched path is unaffected.
            if (ecosSpeed == 0 || _guard.AllowsMovement(address))
            {
                return _inner.SetLocoSpeed(address, ecosSpeed, direction);
            }

            ReportRejection(
                address,
                $"movement for loco {address} refused: a safety fault is latched for it.");

            return false;
        }

        /// <inheritdoc />
        public bool SetSwitch(int decoderAddress, int outputIndex, bool on)
            => _inner.SetSwitch(decoderAddress, outputIndex, on);

        private void ReportRejection(int subject, string detail)
        {
            lock (_lock)
            {
                if (!_guard.IsLatched)
                {
                    // The latch was cleared, so a future rejection is a new event again.
                    _reportedRejections.Clear();
                }

                if (!_reportedRejections.Add(subject))
                {
                    _log?.Invoke($"Movement rejected again ({detail}) - already reported for this latch.");
                    return;
                }
            }

            _log?.Invoke($"Movement rejected ({detail})");
            _diagnostics.Report(new ControlDiagnostic
            {
                Code = DiagnosticCode.MovementRejectedBySafety,
                Severity = DiagnosticSeverity.Rejected,
                Subject = subject == LayoutSubject ? "layout movement" : $"loco {subject}",
                LocoAddress = subject == LayoutSubject ? null : subject,
                Detail = detail
            });
        }
    }
}
