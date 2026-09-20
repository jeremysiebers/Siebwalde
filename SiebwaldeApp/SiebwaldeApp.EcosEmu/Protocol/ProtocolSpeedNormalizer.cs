using System;

namespace SiebwaldeApp.EcosEmu
{
    /// <summary>
    /// Converts a protocol-specific ECoS speed step (for example a DCC28 step 0..28) into the
    /// normalized <c>0..127</c> speed domain that <see cref="IHardwareBackend.SetLocoSpeed"/> and
    /// <c>SiebwaldeApp.Core.AmplifierSpeedMapper</c> expect.
    ///
    /// This is deliberately the only place that knows about decoder protocols. The hardware
    /// backend and the amplifier model keep operating on the normalized domain and must stay
    /// independent of DCC28/DCC128 details.
    ///
    /// The ECoS command station protocol uses two distinct properties:
    /// <list type="bullet">
    ///   <item><description><c>speed[...]</c> already carries the normalized <c>0..127</c> value, so it is passed through unchanged.</description></item>
    ///   <item><description><c>speedstep[...]</c> carries a protocol-specific step and must be normalized here.</description></item>
    /// </list>
    /// </summary>
    public static class ProtocolSpeedNormalizer
    {
        /// <summary>Highest value of the normalized ECoS speed domain (128 steps: 0..127).</summary>
        public const int NormalizedMaxSpeed = 127;

        /// <summary>Highest speed step of the DCC28 protocol.</summary>
        public const int Dcc28MaxStep = 28;

        /// <summary>Highest speed step of the DCC128 protocol (already normalized).</summary>
        public const int Dcc128MaxStep = 127;

        /// <summary>
        /// Tries to normalize a protocol-specific speed step into the <c>0..127</c> domain.
        /// </summary>
        /// <remarks>
        /// A stop (step 0 or below) is accepted for every protocol, because standstill has the
        /// same meaning everywhere. For a non-zero step the protocol must be known, otherwise the
        /// step cannot be interpreted safely and the method returns <see langword="false"/> so the
        /// caller can refuse the command explicitly instead of guessing a scale.
        /// </remarks>
        /// <param name="protocol">Protocol name as stored on the locomotive (for example <c>DCC28</c>).</param>
        /// <param name="protocolSpeed">The protocol-specific speed step received from the client.</param>
        /// <param name="normalizedSpeed">The normalized speed in <c>0..127</c> when the method returns true.</param>
        /// <returns>True when the step could be interpreted; otherwise false.</returns>
        public static bool TryNormalize(string? protocol, int protocolSpeed, out int normalizedSpeed)
        {
            // Standstill is protocol-independent, so a stop is always accepted.
            if (protocolSpeed <= 0)
            {
                normalizedSpeed = 0;
                return true;
            }

            switch (NormalizeProtocolName(protocol))
            {
                case "DCC28":
                    normalizedSpeed = ScaleStep(protocolSpeed, Dcc28MaxStep);
                    return true;

                case "DCC128":
                    // DCC128 already uses the normalized 0..127 domain: do not scale twice.
                    normalizedSpeed = Clamp(protocolSpeed, 0, NormalizedMaxSpeed);
                    return true;

                default:
                    normalizedSpeed = 0;
                    return false;
            }
        }

        /// <summary>
        /// Scales a speed step linearly onto <c>0..127</c> using round-half-up integer arithmetic.
        /// The result is clamped so it can never leave the normalized domain.
        /// </summary>
        /// <param name="step">The protocol step to scale.</param>
        /// <param name="maxStep">The highest step of the protocol.</param>
        public static int ScaleStep(int step, int maxStep)
        {
            if (maxStep <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStep), maxStep, "The maximum step must be positive.");
            }

            if (step <= 0)
            {
                return 0;
            }

            if (step >= maxStep)
            {
                return NormalizedMaxSpeed;
            }

            // Round half up: floor(step * 127 / maxStep + 0.5) expressed with integers.
            var scaled = ((long)step * NormalizedMaxSpeed + maxStep / 2) / maxStep;
            return Clamp((int)scaled, 0, NormalizedMaxSpeed);
        }

        private static string NormalizeProtocolName(string? protocol)
            => (protocol ?? string.Empty).Trim().ToUpperInvariant();

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
