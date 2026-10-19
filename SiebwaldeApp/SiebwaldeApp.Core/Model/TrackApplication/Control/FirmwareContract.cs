namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Expected firmware identity for a track-amplifier verification target. Captures the SW
    /// checksum (HR11) and, for firmware that exposes identity/version registers (PIC18 1.0.0), the
    /// exact expected HR7/HR21/HR22 values. Legacy (Baseline-0) firmware does not expose those
    /// registers, so its contract leaves them unconstrained and they may legitimately read 0.
    /// </summary>
    public sealed class FirmwareContract
    {
        /// <summary>Expected HR11 SW checksum.</summary>
        public ushort TargetChecksum { get; }

        /// <summary>Expected HR7 identity, or null when the firmware does not expose identity/version.</summary>
        public ushort? Identity { get; }

        /// <summary>Expected HR21 MAJOR.MINOR, or null when not exposed.</summary>
        public ushort? FwVersionMajorMinor { get; }

        /// <summary>Expected HR22 PATCH.PROTOCOL, or null when not exposed.</summary>
        public ushort? FwVersionPatchProtocol { get; }

        /// <summary>True when this contract requires exact identity/version register values.</summary>
        public bool RequiresIdentityVersion => Identity.HasValue;

        private FirmwareContract(
            ushort targetChecksum,
            ushort? identity,
            ushort? fwVersionMajorMinor,
            ushort? fwVersionPatchProtocol)
        {
            TargetChecksum = targetChecksum;
            Identity = identity;
            FwVersionMajorMinor = fwVersionMajorMinor;
            FwVersionPatchProtocol = fwVersionPatchProtocol;
        }

        /// <summary>PIC18 1.0.0 firmware: HR11=0xE906, HR7=0xF101, HR21=0x0100, HR22=0x0001.</summary>
        public static FirmwareContract Version100 { get; } =
            new FirmwareContract(0xE906, 0xF101, 0x0100, 0x0001);

        /// <summary>Legacy Baseline-0 firmware: HR11=0x251F; identity/version registers are not exposed.</summary>
        public static FirmwareContract Legacy { get; } =
            new FirmwareContract(0x251F, null, null, null);

        /// <summary>The known contract whose checksum matches <paramref name="checksum"/>, or null when unknown.</summary>
        public static FirmwareContract? FromChecksum(ushort checksum) =>
            checksum == Version100.TargetChecksum ? Version100 :
            checksum == Legacy.TargetChecksum ? Legacy :
            null;

        /// <summary>
        /// Fail-closed verification: an amplifier is verified only when it is detected, its HR11
        /// checksum matches a known contract, and (for 1.0.0) its HR7/HR21/HR22 match exactly.
        /// An undetected amplifier, an unknown checksum (including 0), or any register mismatch all
        /// return false, so a missing or failed amplifier can never be reported as verified.
        /// </summary>
        public static bool IsVerified(TrackAmplifierItem item)
        {
            if (item is null)
                return false;
            if (item.SlaveDetected == 0)
                return false;

            var r = item.HoldingReg;
            if (r is null || r.Length < TrackAmplifierRegisters.Count)
                return false;

            var contract = FromChecksum(r[TrackAmplifierRegisters.SwChecksum]);
            return contract != null && contract.Satisfies(r);
        }

        private bool Satisfies(ushort[] r)
        {
            if (r[TrackAmplifierRegisters.SwChecksum] != TargetChecksum)
                return false;
            if (!RequiresIdentityVersion)
                return true;
            return r[TrackAmplifierRegisters.Identity] == Identity &&
                   r[TrackAmplifierRegisters.FwVersionMajorMinor] == FwVersionMajorMinor &&
                   r[TrackAmplifierRegisters.FwVersionPatchProtocol] == FwVersionPatchProtocol;
        }
    }
}
