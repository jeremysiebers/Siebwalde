using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Converts an ECoS speed step (0..127) plus direction into a bidirectional
    /// track amplifier PWM setpoint.
    ///
    /// The amplifier PWM range is 1..799 with a neutral point at 399 (standstill).
    /// Forward uses 400..799; reverse uses 398..1. PWM 0 is never produced because
    /// it causes a clipping artefact.
    /// </summary>
    public static class AmplifierSpeedMapper
    {
        /// <summary>Lowest usable PWM value.</summary>
        public const int MinPwm = 1;

        /// <summary>Highest usable PWM value.</summary>
        public const int MaxPwm = 799;

        /// <summary>PWM value that represents standstill.</summary>
        public const int NeutralPwm = 399;

        /// <summary>Lowest PWM value used for forward motion.</summary>
        public const int ForwardMinPwm = 400;

        /// <summary>Highest PWM value used for reverse motion.</summary>
        public const int ReverseMaxPwm = 398;

        /// <summary>Highest ECoS speed step (128 steps: 0..127).</summary>
        public const int MaxEcosSpeed = 127;

        /// <summary>
        /// Maps an ECoS speed step and direction to an amplifier PWM setpoint.
        /// </summary>
        /// <param name="ecosSpeed">ECoS speed step, 0..127. Values outside the range are clamped.</param>
        /// <param name="direction">0 = forward, non-zero = reverse.</param>
        /// <returns>A PWM setpoint in the range 1..799, never 0.</returns>
        public static int ToPwm(int ecosSpeed, int direction)
        {
            if (ecosSpeed <= 0)
            {
                return NeutralPwm;
            }

            if (ecosSpeed > MaxEcosSpeed)
            {
                ecosSpeed = MaxEcosSpeed;
            }

            var fraction = ecosSpeed / (double)MaxEcosSpeed;

            if (direction == 0)
            {
                // Forward: 400 .. 799
                return ForwardMinPwm + (int)Math.Round((MaxPwm - ForwardMinPwm) * fraction);
            }

            // Reverse: 398 .. 1
            return ReverseMaxPwm - (int)Math.Round((ReverseMaxPwm - MinPwm) * fraction);
        }
    }
}
