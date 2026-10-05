namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// The identity observed for a Koploper executable file: the product name, the profile
    /// version it is being gated against, the actual file hash, and the profile image base.
    /// </summary>
    public sealed record KoploperExecutableIdentity(
        string ProductName,
        string Version,
        string Sha256Hex,
        uint PreferredImageBase);

    /// <summary>Result of gating an executable against the supported Koploper profile.</summary>
    public enum KoploperVersionGateResult
    {
        /// <summary>The executable hash matches the supported layout profile exactly.</summary>
        Supported = 0,

        /// <summary>The file was readable but its hash differs from the supported profile.</summary>
        UnsupportedVersion = 1,

        /// <summary>The file is missing or could not be read.</summary>
        NotPresent = 2,

        /// <summary>The result could not be determined (unexpected I/O or error).</summary>
        Unknown = 3
    }

    /// <summary>
    /// Verifies a Koploper executable against a supported memory-layout profile. Only an exact
    /// hash match activates the layout; any mismatch fails closed.
    /// </summary>
    public interface IKoploperExecutableVerifier
    {
        /// <summary>
        /// Computes the SHA-256 of <paramref name="executablePath"/> and reports whether it is
        /// the supported binary, together with the observed identity.
        /// </summary>
        KoploperVersionGateResult Verify(string executablePath, out KoploperExecutableIdentity? observed);
    }
}
