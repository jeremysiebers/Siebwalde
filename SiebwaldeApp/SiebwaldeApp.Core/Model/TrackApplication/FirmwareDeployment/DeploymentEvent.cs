namespace SiebwaldeApp.Core
{
    /// <summary>Lifecycle stage of a firmware-deployment decision or side effect.</summary>
    public enum DeploymentEventKind
    {
        /// <summary>Read-only observation (identification / compatibility assessment); no flash.</summary>
        Diagnostic,

        /// <summary>Authorization or planning refused flashing for this target.</summary>
        NotAuthorized,

        /// <summary>Authorization granted flashing for this target.</summary>
        Authorized,

        /// <summary>The artifact passed independent verification (SHA-256 + embedded checksum).</summary>
        ArtifactVerified,

        /// <summary>The artifact failed independent verification.</summary>
        ArtifactVerificationFailed,

        /// <summary>Flash execution started for this target.</summary>
        FlashStarted,

        /// <summary>Flash execution completed successfully.</summary>
        FlashSucceeded,

        /// <summary>Flash execution failed (no automatic retry).</summary>
        FlashFailed,

        /// <summary>Deployment was blocked fail-closed (e.g. fleet isolation cannot be guaranteed).</summary>
        DeploymentBlocked,
    }

    /// <summary>
    /// An auditable record of a deployment-related decision or side effect. Immutable; carries the
    /// target, the artifact hash, a result description, and a UTC timestamp.
    /// </summary>
    public sealed record DeploymentEvent(
        DeploymentEventKind Kind,
        DeploymentTarget? Target,
        string? ArtifactSha256Hex,
        string Result,
        DateTimeOffset TimestampUtc);

    /// <summary>
    /// An auditable rollback record, produced when a deployment is interrupted or fails and a
    /// previous known-good image must be retained or restored.
    /// </summary>
    public sealed record RollbackEvent(
        DeploymentTarget? Target,
        string? ArtifactSha256Hex,
        string Result,
        DateTimeOffset TimestampUtc);

    /// <summary>
    /// Thread-safe in-memory audit log for deployment and rollback events. This implementation does
    /// not persist; consumers snapshot it for durable recording. No side-effecting I/O occurs from
    /// the authorization decision path itself.
    /// </summary>
    public sealed class DeploymentEventLog
    {
        private readonly object _gate = new();
        private readonly List<DeploymentEvent> _deploymentEvents = new();
        private readonly List<RollbackEvent> _rollbackEvents = new();

        public void Record(DeploymentEvent @event)
        {
            lock (_gate)
            {
                _deploymentEvents.Add(@event);
            }
        }

        public void RecordRollback(RollbackEvent rollbackEvent)
        {
            lock (_gate)
            {
                _rollbackEvents.Add(rollbackEvent);
            }
        }

        public IReadOnlyList<DeploymentEvent> DeploymentEvents
        {
            get
            {
                lock (_gate)
                {
                    return _deploymentEvents.ToArray();
                }
            }
        }

        public IReadOnlyList<RollbackEvent> RollbackEvents
        {
            get
            {
                lock (_gate)
                {
                    return _rollbackEvents.ToArray();
                }
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _deploymentEvents.Clear();
                _rollbackEvents.Clear();
            }
        }
    }
}
