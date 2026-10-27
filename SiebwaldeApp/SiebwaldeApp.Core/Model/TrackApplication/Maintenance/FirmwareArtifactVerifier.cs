using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// A fully parsed and validated PIC18 firmware artifact: SHA-256 identity, the recomputed
    /// host checksum, the linker-embedded checksum word, the extracted config word and the
    /// parsed HEX rows (via <see cref="TrackAmplifierBootloaderHelpers"/>).
    /// </summary>
    public sealed class FirmwareArtifact
    {
        public string Sha256Hex { get; }
        public ushort RecomputedChecksum { get; }
        public ushort EmbeddedChecksum { get; }
        public TrackAmplifierBootloaderHelpers Helpers { get; }

        internal FirmwareArtifact(
            string sha256Hex,
            ushort recomputedChecksum,
            ushort embeddedChecksum,
            TrackAmplifierBootloaderHelpers helpers)
        {
            Sha256Hex = sha256Hex;
            RecomputedChecksum = recomputedChecksum;
            EmbeddedChecksum = embeddedChecksum;
            Helpers = helpers;
        }
    }

    public readonly struct FirmwareArtifactValidationResult
    {
        public bool Success { get; }
        public string? Error { get; }
        public FirmwareArtifact? Artifact { get; }

        private FirmwareArtifactValidationResult(bool success, string? error, FirmwareArtifact? artifact)
        {
            Success = success;
            Error = error;
            Artifact = artifact;
        }

        public static FirmwareArtifactValidationResult Ok(FirmwareArtifact artifact)
            => new(true, null, artifact);

        public static FirmwareArtifactValidationResult Fail(string error)
            => new(false, error, null);
    }

    /// <summary>
    /// Offline validation of a PIC18 firmware HEX before any flash write is attempted:
    /// (i)  the file's SHA-256 must match the caller-supplied expected digest;
    /// (ii) the linker-embedded 16-bit checksum word (0x7FFE/0x7FFF) must equal the host
    ///      recomputed little-endian word sum over 0x800..0x7FFD.
    /// </summary>
    public static class FirmwareArtifactVerifier
    {
        /// <summary>SHA-256 of the raw file bytes, uppercase hex (no separators).</summary>
        public static string ComputeSha256Hex(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        }

        /// <summary>
        /// Reads the linker-embedded checksum word from the last 16-byte HEX row (bytes 14/15,
        /// little-endian) and reports whether it equals the recomputed host checksum.
        /// </summary>
        public static bool TryVerifyEmbeddedChecksum(
            IReadOnlyList<byte[][]> rows,
            ushort recomputedChecksum,
            out ushort embeddedChecksum)
        {
            if (rows is null || rows.Count == 0)
                throw new ArgumentException("No HEX rows were parsed.", nameof(rows));

            var lastRow = rows[rows.Count - 1][1];
            if (lastRow is null || lastRow.Length < 16)
                throw new InvalidDataException("Last HEX row does not contain a full 16-byte data payload.");

            embeddedChecksum = (ushort)(lastRow[14] | (lastRow[15] << 8));
            return embeddedChecksum == recomputedChecksum;
        }

        /// <summary>
        /// Validates and parses <paramref name="hexPath"/>. On success the parsed
        /// <see cref="TrackAmplifierBootloaderHelpers"/> is reused by the caller (no double parse).
        /// A SHA-256 mismatch or an embedded-checksum mismatch yields a failure (no write).
        /// </summary>
        public static FirmwareArtifactValidationResult Validate(
            string hexPath,
            string? expectedSha256Hex,
            string loggerInstance = "FlashOneAmplifier")
        {
            if (string.IsNullOrWhiteSpace(hexPath))
                return FirmwareArtifactValidationResult.Fail("No firmware HEX path supplied.");

            if (!File.Exists(hexPath))
                return FirmwareArtifactValidationResult.Fail($"Firmware HEX file not found: {hexPath}");

            string sha256;
            try
            {
                sha256 = ComputeSha256Hex(hexPath);
            }
            catch (Exception ex)
            {
                return FirmwareArtifactValidationResult.Fail($"Could not hash firmware file: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(expectedSha256Hex) &&
                !string.Equals(sha256, expectedSha256Hex.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return FirmwareArtifactValidationResult.Fail(
                    $"Firmware SHA-256 mismatch: expected {expectedSha256Hex.Trim()}, got {sha256}.");
            }

            var helpers = new TrackAmplifierBootloaderHelpers(hexPath, loggerInstance);
            helpers.Execute();

            if (!helpers.HexFileReadSuccessful)
                return FirmwareArtifactValidationResult.Fail("Firmware HEX could not be parsed (checksum/config-word extraction failed).");

            if (helpers.GetFileCheckSum == 0)
                return FirmwareArtifactValidationResult.Fail("Firmware recomputed checksum is zero; the image is unusable.");

            if (!TryVerifyEmbeddedChecksum(helpers.GetHexFileData, helpers.GetFileCheckSum, out var embedded))
            {
                return FirmwareArtifactValidationResult.Fail(
                    $"Firmware embedded-checksum mismatch: embedded 0x{embedded:X4}, recomputed 0x{helpers.GetFileCheckSum:X4}.");
            }

            return FirmwareArtifactValidationResult.Ok(
                new FirmwareArtifact(sha256, helpers.GetFileCheckSum, embedded, helpers));
        }
    }
}
