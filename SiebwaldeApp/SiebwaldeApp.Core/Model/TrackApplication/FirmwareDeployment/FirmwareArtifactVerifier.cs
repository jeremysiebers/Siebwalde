using System.Security.Cryptography;

namespace SiebwaldeApp.Core
{
    /// <summary>Result of verifying a firmware artifact identity against its file on disk.</summary>
    public sealed class ArtifactVerificationResult
    {
        public bool IsVerified { get; }

        /// <summary>Human-readable failure reason, or null when verified.</summary>
        public string? Reason { get; }

        /// <summary>Computed SHA-256 of the file (uppercase hex), or null when it could not be hashed.</summary>
        public string? ComputedSha256Hex { get; }

        /// <summary>The embedded PIC18 checksum word, or null when verification failed early.</summary>
        public ushort? EmbeddedChecksum { get; }

        private ArtifactVerificationResult(bool isVerified, string? reason, string? sha256, ushort? embedded)
        {
            IsVerified = isVerified;
            Reason = reason;
            ComputedSha256Hex = sha256;
            EmbeddedChecksum = embedded;
        }

        internal static ArtifactVerificationResult Verified(string sha256, ushort embedded)
            => new(true, null, sha256, embedded);

        internal static ArtifactVerificationResult Failed(string reason, string? sha256 = null, ushort? embedded = null)
            => new(false, reason, sha256, embedded);
    }

    /// <summary>
    /// Verifies a firmware artifact file on disk against its declared identity: file SHA-256,
    /// Intel HEX structural integrity, the embedded PIC18 bootloader checksum word, and the
    /// expected embedded checksum. All checks must pass before an artifact may be authorized.
    /// </summary>
    public static class FirmwareDeploymentArtifactVerifier
    {
        public static ArtifactVerificationResult Verify(FirmwareArtifactIdentity identity)
        {
            if (identity is null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (!File.Exists(identity.Path))
            {
                return ArtifactVerificationResult.Failed($"artifact file not found: {identity.Path}");
            }

            string sha256;
            try
            {
                sha256 = ComputeSha256Hex(identity.Path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return ArtifactVerificationResult.Failed($"failed to hash artifact: {e.Message}");
            }

            if (!string.Equals(sha256, identity.ExpectedSha256Hex, StringComparison.OrdinalIgnoreCase))
            {
                return ArtifactVerificationResult.Failed(
                    $"artifact SHA-256 mismatch: expected {identity.ExpectedSha256Hex}, computed {sha256}",
                    sha256);
            }

            var checksum = Pic18HexChecksumVerifier.Verify(identity.Path);
            if (!checksum.IsVerified)
            {
                return ArtifactVerificationResult.Failed(
                    $"artifact checksum verification failed: {checksum.Error}",
                    sha256,
                    checksum.EmbeddedChecksum);
            }

            if (checksum.EmbeddedChecksum != identity.ExpectedPic18EmbeddedChecksum)
            {
                return ArtifactVerificationResult.Failed(
                    $"artifact embedded checksum 0x{checksum.EmbeddedChecksum:X4} != expected 0x{identity.ExpectedPic18EmbeddedChecksum:X4}",
                    sha256,
                    checksum.EmbeddedChecksum);
            }

            return ArtifactVerificationResult.Verified(sha256, checksum.EmbeddedChecksum);
        }

        /// <summary>Computes the uppercase hex SHA-256 of the file at <paramref name="path"/>.</summary>
        public static string ComputeSha256Hex(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(stream));
        }
    }
}
