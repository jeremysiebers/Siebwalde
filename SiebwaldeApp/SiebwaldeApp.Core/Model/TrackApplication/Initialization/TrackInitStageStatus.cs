using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>Lifecycle state of a single initialization stage.</summary>
    public enum TrackInitStageState
    {
        Pending,
        Running,
        Passed,
        Failed,
        Blocked
    }

    /// <summary>Per-amplifier status within a stage (address + human-readable detail + ok flag).</summary>
    public sealed class TrackInitAmpStatus
    {
        public byte SlaveNumber { get; }
        public string Detail { get; }
        public bool Ok { get; }

        public TrackInitAmpStatus(byte slaveNumber, string detail, bool ok)
        {
            SlaveNumber = slaveNumber;
            Detail = detail;
            Ok = ok;
        }
    }

    /// <summary>Snapshot of one stage: its state, failure reason, recovery action and per-amplifier status.</summary>
    public sealed class TrackInitStageStatus
    {
        public TrackInitStage Stage { get; }
        public TrackInitStageState State { get; }
        public string? FailureReason { get; }
        public string? RecoveryAction { get; }
        public IReadOnlyList<TrackInitAmpStatus> Amps { get; }

        public TrackInitStageStatus(
            TrackInitStage stage,
            TrackInitStageState state,
            string? failureReason = null,
            string? recoveryAction = null,
            IReadOnlyList<TrackInitAmpStatus>? amps = null)
        {
            Stage = stage;
            State = state;
            FailureReason = failureReason;
            RecoveryAction = recoveryAction;
            Amps = amps ?? Array.Empty<TrackInitAmpStatus>();
        }
    }

    /// <summary>Raised when a stage's status changes.</summary>
    public sealed class TrackInitStageStatusChangedEventArgs : EventArgs
    {
        public TrackInitStageStatus Status { get; }

        public TrackInitStageStatusChangedEventArgs(TrackInitStageStatus status)
        {
            Status = status;
        }
    }

    /// <summary>
    /// A state-machine event for the Dutch operator log: human-readable message correlated with
    /// timestamp, session id, stage id and (optionally) amplifier address, plus a raw technical
    /// reference for the forensic log.
    /// </summary>
    public sealed class TrackInitLogEventArgs : EventArgs
    {
        public DateTimeOffset Timestamp { get; }
        public string SessionId { get; }
        public TrackInitStage Stage { get; }
        public byte? AmpAddress { get; }
        public string Message { get; }
        public string RawTechnical { get; }

        public TrackInitLogEventArgs(
            DateTimeOffset timestamp,
            string sessionId,
            TrackInitStage stage,
            string message,
            byte? ampAddress = null,
            string rawTechnical = "")
        {
            Timestamp = timestamp;
            SessionId = sessionId;
            Stage = stage;
            Message = message;
            AmpAddress = ampAddress;
            RawTechnical = rawTechnical;
        }
    }
}
