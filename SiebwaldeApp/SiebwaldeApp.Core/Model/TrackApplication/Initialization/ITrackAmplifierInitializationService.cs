using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{ 
    public enum InitializationStatus
    {
        Idle,
        Running,
        Completed,
        Failed,
        Cancelled,

        /// <summary>
        /// Stages 1..5 (ControllerConnection .. GlobalReady) are Passed and Stage 6 (track power,
        /// <c>AmpPowerEnable</c>) was deliberately NOT performed: the automatic run stopped at
        /// GlobalReady because track power is operator-gated. This is a SUCCESS of the requested
        /// (non-energized) test scope, NOT completion of the full seven-stage operating sequence
        /// and NOT a failure. The live runtime loop must NOT be started for this status.
        /// </summary>
        StoppedAtGlobalReady,

        /// <summary>
        /// The run reached NormalOperation but a comm/host-authority fault (PIC32 loss, host-authority
        /// loss or host-initiated latch) latched a safety stop. Movement, track-power energize and
        /// re-arm are all withheld, and the evidence for Stages 4..7 (configure/validate, global ready,
        /// track power, normal operation) is stale until a successful <c>RevalidateAsync</c> re-confirms
        /// it (Stages 2/3 are retained but unconfirmed). Distinct from <see cref="Failed"/> (no stage
        /// actually failed) and from <see cref="StoppedAtGlobalReady"/> (the automatic run was
        /// deliberately stopped before the operator-gated Stage 6, not fault-latched).
        /// </summary>
        RecoveryHold
    }

    public sealed class InitializationProgress
    {
        public string StepName { get; }
        public string Message { get; }
        public double? Percent { get; }

        public InitializationProgress(string stepName, string message, double? percent = null)
        {
            StepName = stepName;
            Message = message;
            Percent = percent;
        }
    }

    public interface ITrackAmplifierInitializationService
    {
        event EventHandler<InitializationProgress>? ProgressChanged;
        event EventHandler<InitializationStatus>? StatusChanged;

        /// <summary>The current initialization status (Idle, Running, Completed, Failed or Cancelled).</summary>
        InitializationStatus Status { get; }

        /// <summary>
        /// Runs the full initialization and firmware process for all amplifiers.
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}
