using System;
using System.IO;
using System.Security.Cryptography;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// SHA-256 based executable verifier. It does not read PE version resources; it only hashes
    /// the file bytes and compares them against the profile's supported hash.
    /// </summary>
    public sealed class KoploperExecutableVerifier : IKoploperExecutableVerifier
    {
        private const string ProductName = "Koploper";

        private readonly IKoploperMemoryLayout _layout;

        public KoploperExecutableVerifier(IKoploperMemoryLayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        /// <inheritdoc />
        public KoploperVersionGateResult Verify(string executablePath, out KoploperExecutableIdentity? observed)
        {
            observed = null;

            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                return KoploperVersionGateResult.NotPresent;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(executablePath);
            }
            catch (FileNotFoundException)
            {
                // The file disappeared between the existence check and the read.
                return KoploperVersionGateResult.NotPresent;
            }
            catch (DirectoryNotFoundException)
            {
                return KoploperVersionGateResult.NotPresent;
            }
            catch (UnauthorizedAccessException)
            {
                // Present but unreadable.
                return KoploperVersionGateResult.NotPresent;
            }
            catch (IOException)
            {
                // Present but a genuine I/O error prevented reading it.
                return KoploperVersionGateResult.Unknown;
            }
            catch (Exception)
            {
                return KoploperVersionGateResult.Unknown;
            }

            string sha256Hex;
            try
            {
                sha256Hex = Convert.ToHexString(SHA256.HashData(bytes));
            }
            catch (Exception)
            {
                return KoploperVersionGateResult.Unknown;
            }

            observed = new KoploperExecutableIdentity(
                ProductName,
                _layout.Version,
                sha256Hex,
                _layout.PreferredImageBase);

            return string.Equals(sha256Hex, _layout.SupportedSha256Hex, StringComparison.OrdinalIgnoreCase)
                ? KoploperVersionGateResult.Supported
                : KoploperVersionGateResult.UnsupportedVersion;
        }
    }
}
