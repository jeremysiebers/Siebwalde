using System;
using System.Collections.Generic;
using System.Text;

namespace SiebwaldeApp.Core.Koploper.Validation
{
    /// <summary>
    /// Incremental, stateful parser for the Koploper external-information stream (port 5700).
    /// It buffers partial records across arbitrary byte chunks and dequeues complete five-field
    /// records, so it can sit directly on top of a socket read loop. Framing and field-level
    /// decoding are shared with <see cref="Koploper5700Parser"/> (separator <c>0x1B</c>, record
    /// end <c>0x00</c>, <see cref="Koploper5700Parser.TryParseFields"/>). No I/O is performed
    /// here; callers push bytes and pull records.
    /// </summary>
    public sealed class Koploper5700StreamParser
    {
        private readonly List<string> _fields = new(Koploper5700Parser.FieldCount);
        private readonly StringBuilder _field = new();
        private readonly Queue<Koploper5700Record> _records = new();

        /// <summary>
        /// Feeds a chunk of raw stream bytes. Complete records are queued and are available via
        /// <see cref="TryDequeue"/>. Any trailing partial record is retained for the next call.
        /// </summary>
        public void Feed(ReadOnlySpan<byte> bytes)
        {
            foreach (byte b in bytes)
            {
                if (b == Koploper5700Parser.Separator)
                {
                    CompleteField();
                    if (_fields.Count >= Koploper5700Parser.FieldCount)
                    {
                        CompleteRecord();
                    }
                }
                else if (b == Koploper5700Parser.RecordEnd)
                {
                    CompleteRecord();
                }
                else
                {
                    _field.Append((char)b);
                }
            }
        }

        /// <summary>
        /// Dequeues the next complete record, or returns <c>false</c> when none is buffered.
        /// </summary>
        public bool TryDequeue(out Koploper5700Record? record)
        {
            if (_records.Count == 0)
            {
                record = null;
                return false;
            }

            record = _records.Dequeue();
            return true;
        }

        private void CompleteField()
        {
            _fields.Add(_field.ToString());
            _field.Clear();
        }

        private void CompleteRecord()
        {
            if (_fields.Count > 0 && Koploper5700Parser.TryParseFields(_fields, out Koploper5700Record? record))
            {
                _records.Enqueue(record);
            }

            _fields.Clear();
            _field.Clear();
        }
    }
}
