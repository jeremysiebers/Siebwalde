namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The cryptographic and structural identity of the firmware artifact a deployment session is
    /// allowed to flash: its path, its expected SHA-256, and the expected PIC18 bootloader checksum
    /// word embedded at program-memory 0x7FFE/0x7FFF (Phase-1 target = 0xE906).
    /// </summary>
    public sealed class FirmwareArtifactIdentity
    {
        /// <summary>Absolute or relative path to the Intel HEX artifact file.</summary>
        public string Path { get; }

        /// <summary>Expected SHA-256 of the artifact file, normalized to 64 uppercase hex characters.</summary>
        public string ExpectedSha256Hex { get; }

        /// <summary>Expected PIC18 embedded checksum word (e.g. Phase-1 target 0xE906).</summary>
        public ushort ExpectedPic18EmbeddedChecksum { get; }

        public FirmwareArtifactIdentity(
            string path,
            string expectedSha256Hex,
            ushort expectedPic18EmbeddedChecksum)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Artifact path must not be empty.", nameof(path));
            }

            if (string.IsNullOrWhiteSpace(expectedSha256Hex) ||
                expectedSha256Hex.Length != 64 ||
                !expectedSha256Hex.All(IsHexDigit))
            {
                throw new ArgumentException(
                    "Expected SHA-256 must be exactly 64 hexadecimal characters.",
                    nameof(expectedSha256Hex));
            }

            if (expectedPic18EmbeddedChecksum == 0)
            {
                throw new ArgumentException(
                    "Expected PIC18 embedded checksum must be non-zero (an uninitialized identity must not authorize flashing).",
                    nameof(expectedPic18EmbeddedChecksum));
            }

            Path = path;
            ExpectedSha256Hex = expectedSha256Hex.ToUpperInvariant();
            ExpectedPic18EmbeddedChecksum = expectedPic18EmbeddedChecksum;
        }

        private static bool IsHexDigit(char c)
            => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }
}
