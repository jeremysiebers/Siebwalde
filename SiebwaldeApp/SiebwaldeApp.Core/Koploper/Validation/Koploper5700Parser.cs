using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Pure parser for the Koploper external-information stream (port 5700). Records are ASCII
    /// fields separated by <see cref="Separator"/> (<c>0x1B</c>) and terminated by
    /// <see cref="RecordEnd"/> (<c>0x00</c>). A record is the five fields:
    /// header (<c>&amp;loc</c>), block, model time, PC time and description. Parsing has no side
    /// effects and performs no I/O.
    /// </summary>
    public static class Koploper5700Parser
    {
        /// <summary>Byte separating fields within a record.</summary>
        public const byte Separator = 0x1B;

        /// <summary>Byte terminating a record.</summary>
        public const byte RecordEnd = 0x00;

        /// <summary>The number of fields in a complete record.</summary>
        public const int FieldCount = 5;

        /// <summary>Parses a complete byte buffer into zero or more 5700 records.</summary>
        public static IReadOnlyList<Koploper5700Record> Parse(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            return Parse(bytes.AsSpan());
        }

        /// <summary>Parses a byte span into zero or more 5700 records.</summary>
        public static IReadOnlyList<Koploper5700Record> Parse(ReadOnlySpan<byte> bytes)
        {
            var records = new List<Koploper5700Record>();
            var fields = new List<string>(FieldCount);
            var field = new StringBuilder();

            void CompleteField()
            {
                fields.Add(field.ToString());
                field.Clear();
            }

            void CompleteRecord()
            {
                if (fields.Count > 0 && TryParseFields(fields, out Koploper5700Record? record))
                {
                    records.Add(record);
                }

                fields.Clear();
                field.Clear();
            }

            foreach (byte b in bytes)
            {
                if (b == Separator)
                {
                    CompleteField();
                    if (fields.Count >= FieldCount)
                    {
                        CompleteRecord();
                    }
                }
                else if (b == RecordEnd)
                {
                    CompleteRecord();
                }
                else
                {
                    field.Append((char)b);
                }
            }

            // Finalize a trailing record that was not explicitly terminated.
            if (field.Length > 0)
            {
                CompleteField();
            }

            if (fields.Count > 0)
            {
                CompleteRecord();
            }

            return records;
        }

        /// <summary>
        /// Maps a completed field list onto a <see cref="Koploper5700Record"/>. Returns
        /// <c>false</c> when the locomotive or block identity cannot be parsed. Public so the
        /// streaming parser and unit tests can reuse the same field-level decoding rules.
        /// </summary>
        public static bool TryParseFields(
            IReadOnlyList<string> fields,
            [NotNullWhen(true)] out Koploper5700Record? record)
        {
            record = null;

            if (fields.Count < 2)
            {
                return false;
            }

            string header = fields[0];
            string blockText = fields[1];

            string locomotivePart = header.Trim().TrimStart('&');
            if (!int.TryParse(locomotivePart, out int locomotiveId))
            {
                return false;
            }

            if (!int.TryParse(blockText.Trim(), out int blockId))
            {
                return false;
            }

            string? modelTime = fields.Count > 2 ? fields[2] : null;
            string? pcTime = fields.Count > 3 ? fields[3] : null;
            string? description = fields.Count > 4 ? fields[4] : null;

            record = new Koploper5700Record(locomotiveId, blockId, modelTime, pcTime, description);
            return true;
        }
    }
}
