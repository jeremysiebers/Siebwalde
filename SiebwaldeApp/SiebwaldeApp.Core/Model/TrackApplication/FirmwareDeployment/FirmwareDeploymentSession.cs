namespace SiebwaldeApp.Core
{
    /// <summary>
    /// An explicit, auditable firmware-deployment session. Flashing is permitted only while a
    /// session exists, is <see cref="Authorized"/>, has not expired, targets the specific device,
    /// and its artifact has been independently verified (SHA-256 + embedded checksum).
    ///
    /// Phase-1 scope is bounded to a single prototype amplifier (not a fleet): the constructor
    /// rejects sessions that name more than <see cref="MaxAmplifierTargets"/> PIC18 amplifier.
    /// A session is intentionally in-memory and non-self-resuming: it is never persisted or
    /// reconstructed automatically, so an interrupted/restarted deployment cannot silently resume.
    /// </summary>
    public sealed class FirmwareDeploymentSession
    {
        /// <summary>Maximum number of PIC18 amplifier targets a Phase-1 session may carry.</summary>
        public const int MaxAmplifierTargets = 1;

        /// <summary>Stable identifier for audit correlation.</summary>
        public string Id { get; }

        /// <summary>Explicit operator/authority grant. A session without this flag never flashes.</summary>
        public bool Authorized { get; }

        /// <summary>The exact set of devices this session is allowed to flash.</summary>
        public IReadOnlyList<DeploymentTarget> Targets { get; }

        /// <summary>The identity of the single artifact this session is allowed to flash.</summary>
        public FirmwareArtifactIdentity Artifact { get; }

        /// <summary>Absolute expiry. After this instant the session is inactive even if authorized.</summary>
        public DateTimeOffset ExpiresAtUtc { get; }

        public FirmwareDeploymentSession(
            string id,
            bool authorized,
            IEnumerable<DeploymentTarget> targets,
            FirmwareArtifactIdentity artifact,
            DateTimeOffset expiresAtUtc)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Session id must not be empty.", nameof(id));
            }

            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));

            var list = targets?.ToList() ?? throw new ArgumentNullException(nameof(targets));
            if (list.Count == 0)
            {
                throw new ArgumentException("A deployment session must target at least one device.", nameof(targets));
            }

            var amplifierCount = list.Count(t => t.Kind == DeploymentTargetKind.Pic18Amplifier);
            if (amplifierCount > MaxAmplifierTargets)
            {
                throw new ArgumentException(
                    $"A Phase-1 deployment session may target at most {MaxAmplifierTargets} amplifier(s), not a fleet.",
                    nameof(targets));
            }

            Id = id;
            Authorized = authorized;
            Targets = list;
            ExpiresAtUtc = expiresAtUtc;
        }

        /// <summary>True while the session is authorized and within its lifetime.</summary>
        public bool IsActive(DateTimeOffset nowUtc) => Authorized && nowUtc <= ExpiresAtUtc;

        /// <summary>True when the session explicitly names the given target.</summary>
        public bool AllowsTarget(DeploymentTarget target) => Targets.Contains(target);

        /// <summary>The PIC18 amplifier slave addresses in scope, in declaration order.</summary>
        public IReadOnlyList<ushort> AmplifierAddresses => Targets
            .Where(t => t.Kind == DeploymentTargetKind.Pic18Amplifier)
            .Select(t => t.AmplifierAddress!.Value)
            .ToArray();
    }
}
