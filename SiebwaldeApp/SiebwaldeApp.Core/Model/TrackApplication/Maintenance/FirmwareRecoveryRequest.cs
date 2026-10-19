namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Operation type of a firmware deployment. This keeps "normal deployment authorization"
    /// (coordinated, all mapped amplifiers) distinct from "emergency recovery" (a single,
    /// operator-identified, isolated bootloader target). They must never be collapsed: the two
    /// operations have different authorization, targeting and safety requirements.
    /// </summary>
    public enum FirmwareOperationType
    {
        /// <summary>Coordinated deployment of every mapped amplifier (requires every target authorized).</summary>
        NormalDeployment,

        /// <summary>Emergency recovery of a single, operator-identified, isolated bootloader target.</summary>
        EmergencyRecovery,
    }

    /// <summary>
    /// An explicit, authorized emergency-recovery request. It carries the single target amplifier
    /// and a firmware artifact that has already passed independent verification
    /// (<see cref="FirmwareArtifactVerifier"/>: SHA-256 + embedded checksum). Recovery is
    /// fail-closed: without this request, the recovery step must never write firmware.
    /// </summary>
    public sealed class FirmwareRecoveryRequest
    {
        /// <summary>The single amplifier this recovery is authorized to flash (ModBus address 1..50).</summary>
        public byte TargetAmplifierAddress { get; }

        /// <summary>How this deployment is classified (emergency recovery vs normal deployment).</summary>
        public FirmwareOperationType OperationType { get; }

        /// <summary>The independently verified firmware artifact (SHA-256 + embedded checksum).</summary>
        public FirmwareArtifact Artifact { get; }

        public FirmwareRecoveryRequest(
            byte targetAmplifierAddress,
            FirmwareArtifact artifact,
            FirmwareOperationType operationType = FirmwareOperationType.EmergencyRecovery)
        {
            if (!TrackAmplifierAddress.IsTrackAmplifierAddress(targetAmplifierAddress))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetAmplifierAddress),
                    targetAmplifierAddress,
                    "A recovery target must be a valid track-amplifier slave address (1..50).");
            }

            TargetAmplifierAddress = targetAmplifierAddress;
            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            OperationType = operationType;
        }
    }
}
