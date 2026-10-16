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
    ///
    /// The frozen Amendment A2 contract (mirror of the PIC32
    /// <c>REPORTxGLOBALxSTOPxDELIVERY</c>, <c>slavehandler.c</c>) lays out the payload as:
    /// <list type="bullet">
    /// <item><c>payload[0]</c> = <b>count</b> — the number of detected amplifiers encoded;</item>
    /// <item><c>payload[1 + i / 4]</c> = packed 2-bit codes, little-endian (LSB-first): entry
    /// <c>i</c> lives at bits <c>(i % 4) * 2 .. +1</c> of that byte.</item>
    /// </list>
    /// The entry order is the <b>physical detected-amplifier list in ascending slave-address
    /// order</b> (<c>SlaveDetected</c> / <c>StopDelivery[i] != STOP_UNUSED</c>), NOT the
    /// effective/configured domain. A null/truncated payload, or an entry index at or beyond the
    /// count byte, decodes as <see cref="AmplifierStopConfirmation.Failed"/> so a partial frame (or
    /// the zero-padding of the final code byte) never reads as confirmed. Release/exit is blocked
    /// while any amplifier is non-confirmed.
    /// </summary>
    public static class TrackAmplifierStopConfirmation
    {
        public const int BitsPerAmplifier = 2;

        /// <summary>Number of 2-bit codes packed into one payload byte.</summary>
        public const int CodesPerByte = 8 / BitsPerAmplifier; // 4

        /// <summary>
        /// Reads the count byte — the number of detected amplifiers the PIC32 encoded. The count is
        /// <c>payload[0]</c>; the 2-bit codes begin at <c>payload[1]</c>.
        /// </summary>
        public static int Count(byte[]? payload)
            => payload is { Length: > 0 } ? payload[0] : 0;

        /// <summary>Decodes the 111 payload against the ordered physical detected-amplifier list.</summary>
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
            if (payload is null || payload.Length == 0)
            {
                return AmplifierStopConfirmation.Failed;
            }

            int count = payload[0];
            // An entry at or beyond the PIC32's encoded count is absent, so it cannot be confirmed.
            // This also prevents the zero-padding of the final code byte from being misread as
            // additional "confirmed" amplifiers when the C# list is longer than the encoded count.
            if (amplifierIndex >= count)
            {
                return AmplifierStopConfirmation.Failed;
            }

            // Codes begin AFTER the count byte; entry i is at payload[1 + i/4], bits (i%4)*2 .. +1.
            int bitOffset = amplifierIndex * BitsPerAmplifier;
            int byteIndex = 1 + bitOffset / 8;
            int shift = bitOffset % 8;

            if (byteIndex >= payload.Length)
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
