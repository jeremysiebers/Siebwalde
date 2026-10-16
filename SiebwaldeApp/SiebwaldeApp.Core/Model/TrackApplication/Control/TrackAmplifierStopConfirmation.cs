using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>Per-amplifier delivery-confirmation code of the PIC32 global-stop fan-out (R5-A / R8).</summary>
    public enum AmplifierStopConfirmation
    {
        /// <summary>Stop write ACKed and applied-PWM/state readback confirmed neutral.</summary>
        Confirmed = 0,
        /// <summary>Queued/transmitted/acked but not yet confirmed.</summary>
        Pending = 1,
        /// <summary>Delivery or confirmation failed (or reserved/truncated — fail closed).</summary>
        Failed = 2
    }

    /// <summary>
    /// C# consumer of the <c>GLOBAL_STOP_STATE</c> (111) per-amplifier confirmation payload.
    /// The frozen contract encodes 2 bits per detected amplifier (0=confirmed, 1=pending, 2=failed,
    /// 3=reserved) packed LSB-first in the order of the detected slave addresses. A truncated payload
    /// decodes as <see cref="AmplifierStopConfirmation.Failed"/> so a partial frame never reads as
    /// confirmed. Release/exit is blocked while any amplifier is non-confirmed.
    /// </summary>
    public static class TrackAmplifierStopConfirmation
    {
        public const int BitsPerAmplifier = 2;

        /// <summary>Decodes the 111 payload against the ordered detected-amplifier list.</summary>
        public static IReadOnlyDictionary<ushort, AmplifierStopConfirmation> Decode(
            byte[]? payload,
            IReadOnlyList<ushort> detectedAmplifiers)
        {
            var result = new Dictionary<ushort, AmplifierStopConfirmation>();
            if (detectedAmplifiers is null)
            {
                return result;
            }

            for (int i = 0; i < detectedAmplifiers.Count; i++)
            {
                result[detectedAmplifiers[i]] = CodeAt(payload, i);
            }

            return result;
        }

        /// <summary>True only when every detected amplifier reports <see cref="AmplifierStopConfirmation.Confirmed"/>.
        /// An empty amplifier list is vacuously confirmed; a null/truncated payload is not.</summary>
        public static bool AllConfirmed(
            byte[]? payload,
            IReadOnlyList<ushort> detectedAmplifiers)
        {
            if (detectedAmplifiers is null || detectedAmplifiers.Count == 0)
            {
                return true;
            }

            if (payload is null)
            {
                return false;
            }

            for (int i = 0; i < detectedAmplifiers.Count; i++)
            {
                if (CodeAt(payload, i) != AmplifierStopConfirmation.Confirmed)
                {
                    return false;
                }
            }

            return true;
        }

        private static AmplifierStopConfirmation CodeAt(byte[]? payload, int amplifierIndex)
        {
            int bitOffset = amplifierIndex * BitsPerAmplifier;
            int byteIndex = bitOffset / 8;
            int shift = bitOffset % 8;

            if (payload is null || byteIndex >= payload.Length)
            {
                return AmplifierStopConfirmation.Failed; // truncated => not confirmed
            }

            int code = (payload[byteIndex] >> shift) & 0x3;
            return code switch
            {
                0 => AmplifierStopConfirmation.Confirmed,
                1 => AmplifierStopConfirmation.Pending,
                _ => AmplifierStopConfirmation.Failed // 2 = failed, 3 = reserved (fail closed)
            };
        }
    }
}
