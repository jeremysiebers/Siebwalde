using System;
using System.Collections.Generic;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// One detected amplifier firmware mismatch: the target amplifier address and its actual
    /// (installed) firmware checksum, against a single expected firmware identity.
    /// </summary>
    public sealed class FirmwareAmpMismatch
    {
        public byte Amplifier { get; }
        public ushort ActualChecksum { get; }

        public FirmwareAmpMismatch(byte amplifier, ushort actualChecksum)
        {
            Amplifier = amplifier;
            ActualChecksum = actualChecksum;
        }
    }

    /// <summary>
    /// Session-scoped, in-memory authorization for exactly ONE bounded firmware deployment.
    ///
    /// A firmware checksum mismatch is detected and reported as a BLOCKED / authorization-required
    /// state; it never implicitly flashes. Only an explicit operator action
    /// (<see cref="TrackApplicationRuntimeHost.AuthorizeFirmwareDeploymentAsync"/>) authorizes one
    /// bounded deployment that reuses the existing per-amplifier command-131 flash
    /// (<see cref="FlashOneAmplifierRunner"/>). Manual init Step/Retry/Backtrack and bootloader
    /// recovery never set this authorization, and an application restart never retains it (it is
    /// in-memory only and re-created fresh per runtime composition).
    /// </summary>
    public sealed class FirmwareDeploymentAuthorization
    {
        private readonly object _gate = new();

        private bool _authorized;
        private byte[] _targetAmps = Array.Empty<byte>();
        private ushort _expectedChecksum;
        private string _expectedSha256Hex = string.Empty;
        private IReadOnlyList<FirmwareAmpMismatch> _mismatches = Array.Empty<FirmwareAmpMismatch>();

        /// <summary>True only after an explicit operator authorization; false by default and after <see cref="Reset"/>.</summary>
        public bool Authorized
        {
            get { lock (_gate) return _authorized; }
        }

        /// <summary>The target amplifier set for the recorded mismatch (empty when none recorded).</summary>
        public IReadOnlyList<byte> TargetAmps
        {
            get { lock (_gate) return (byte[])_targetAmps.Clone(); }
        }

        /// <summary>The expected firmware checksum (from the configured artifact).</summary>
        public ushort ExpectedChecksum
        {
            get { lock (_gate) return _expectedChecksum; }
        }

        /// <summary>The expected firmware SHA-256 (uppercase hex), or empty when it could not be computed.</summary>
        public string ExpectedSha256Hex
        {
            get { lock (_gate) return _expectedSha256Hex; }
        }

        /// <summary>The recorded per-amplifier mismatches (empty when none recorded).</summary>
        public IReadOnlyList<FirmwareAmpMismatch> Mismatches
        {
            get { lock (_gate) return _mismatches; }
        }

        /// <summary>
        /// Records a detected firmware mismatch. Fail-closed: recording a NEW mismatch also clears
        /// any prior authorization, so a stale grant can never survive a re-detected mismatch.
        /// </summary>
        public void RecordMismatch(
            ushort expectedChecksum,
            string expectedSha256Hex,
            IReadOnlyList<FirmwareAmpMismatch> mismatches)
        {
            lock (_gate)
            {
                _expectedChecksum = expectedChecksum;
                _expectedSha256Hex = expectedSha256Hex ?? string.Empty;
                _mismatches = mismatches ?? Array.Empty<FirmwareAmpMismatch>();
                _targetAmps = _mismatches.Select(m => m.Amplifier).Distinct().OrderBy(a => a).ToArray();
                _authorized = false; // a re-detected mismatch invalidates any prior grant
            }
        }

        /// <summary>
        /// Explicit operator authorization for exactly the recorded target set — one bounded
        /// deployment. No-op (stays unauthorized) when no mismatch has been recorded.
        /// </summary>
        public void Authorize()
        {
            lock (_gate)
            {
                if (_targetAmps.Length == 0)
                {
                    _authorized = false;
                    return;
                }

                _authorized = true;
            }
        }

        /// <summary>Clears the authorization and the recorded mismatch (a fresh session starts unauthorized).</summary>
        public void Reset()
        {
            lock (_gate)
            {
                _authorized = false;
                _mismatches = Array.Empty<FirmwareAmpMismatch>();
                _targetAmps = Array.Empty<byte>();
                _expectedChecksum = 0;
                _expectedSha256Hex = string.Empty;
            }
        }
    }
}
