namespace SiebwaldeApp.Core
{
    /// <summary>Kind of device a firmware-deployment session may target.</summary>
    public enum DeploymentTargetKind
    {
        /// <summary>A PIC18 track amplifier (ModBus slave address 1..50).</summary>
        Pic18Amplifier,

        /// <summary>The PIC32 master controller (read-only versioning in this phase; no flash path).</summary>
        Pic32Master,
    }

    /// <summary>
    /// Identifies a single firmware-deployment target: either a specific PIC18 track amplifier
    /// (by ModBus slave address) or the PIC32 master. Value equality is by kind and address, so
    /// targets can be used in hash sets and authorization lookups.
    /// </summary>
    public readonly struct DeploymentTarget : IEquatable<DeploymentTarget>
    {
        public DeploymentTargetKind Kind { get; }

        /// <summary>The PIC18 ModBus slave address (1..50), or null for the PIC32 master.</summary>
        public ushort? AmplifierAddress { get; }

        private DeploymentTarget(DeploymentTargetKind kind, ushort? amplifierAddress)
        {
            Kind = kind;
            AmplifierAddress = amplifierAddress;
        }

        /// <summary>Creates a target for a specific PIC18 track amplifier.</summary>
        public static DeploymentTarget ForAmplifier(ushort amplifierAddress)
        {
            if (!TrackAmplifierAddress.IsTrackAmplifierAddress(amplifierAddress))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amplifierAddress),
                    amplifierAddress,
                    "A PIC18 deployment target must be a valid track-amplifier slave address (1..50).");
            }

            return new DeploymentTarget(DeploymentTargetKind.Pic18Amplifier, amplifierAddress);
        }

        /// <summary>Creates a target for the PIC32 master controller.</summary>
        public static DeploymentTarget ForMaster()
            => new DeploymentTarget(DeploymentTargetKind.Pic32Master, null);

        public bool Equals(DeploymentTarget other)
            => Kind == other.Kind && AmplifierAddress == other.AmplifierAddress;

        public override bool Equals(object? obj) => obj is DeploymentTarget other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Kind, AmplifierAddress);

        public static bool operator ==(DeploymentTarget left, DeploymentTarget right) => left.Equals(right);

        public static bool operator !=(DeploymentTarget left, DeploymentTarget right) => !left.Equals(right);

        public override string ToString()
            => Kind == DeploymentTargetKind.Pic18Amplifier
                ? $"PIC18 amplifier #{AmplifierAddress}"
                : "PIC32 master";
    }
}
