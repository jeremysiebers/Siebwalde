namespace SiebwaldeApp.Core
{
    /// <summary>Outcome of evaluating whether a target may be flashed under a deployment session.</summary>
    public sealed class DeploymentAuthorization
    {
        public bool Granted { get; }

        /// <summary>Non-null denial reason when <see cref="Granted"/> is false.</summary>
        public string? DenialReason { get; }

        private DeploymentAuthorization(bool granted, string? denialReason)
        {
            Granted = granted;
            DenialReason = denialReason;
        }

        public static DeploymentAuthorization Allow() => new(true, null);

        public static DeploymentAuthorization Deny(string reason)
            => new(false, reason ?? throw new ArgumentNullException(nameof(reason)));

        public override string ToString() => Granted ? "GRANTED" : $"DENIED: {DenialReason}";
    }

    /// <summary>
    /// Pure, fail-closed authorization decision for firmware deployment. It is deliberately
    /// separate from the read-only identification and compatibility assessment: those only observe,
    /// whereas this decides whether a flash side effect is permitted.
    /// </summary>
    public static class DeploymentAuthorizer
    {
        public const string NoActiveSession = "no active deployment session";
        public const string SessionNotAuthorized = "deployment session is not authorized";
        public const string SessionExpired = "deployment session has expired";
        public const string TargetOutOfScope = "target is outside the deployment session scope";
        public const string ArtifactNotVerified = "firmware artifact was not verified";

        /// <summary>
        /// Decides whether <paramref name="target"/> may be flashed. The artifact must already have
        /// been verified by <see cref="FirmwareArtifactVerifier"/>; <paramref name="artifactVerified"/>
        /// carries that result. This decision performs no file or hardware I/O.
        /// </summary>
        public static DeploymentAuthorization Authorize(
            FirmwareDeploymentSession? session,
            DeploymentTarget target,
            bool artifactVerified,
            DateTimeOffset nowUtc)
        {
            if (session is null)
            {
                return DeploymentAuthorization.Deny(NoActiveSession);
            }

            if (!session.Authorized)
            {
                return DeploymentAuthorization.Deny(SessionNotAuthorized);
            }

            if (!session.IsActive(nowUtc))
            {
                return DeploymentAuthorization.Deny(SessionExpired);
            }

            if (!session.AllowsTarget(target))
            {
                return DeploymentAuthorization.Deny(TargetOutOfScope);
            }

            if (!artifactVerified)
            {
                return DeploymentAuthorization.Deny(ArtifactNotVerified);
            }

            return DeploymentAuthorization.Allow();
        }
    }
}
