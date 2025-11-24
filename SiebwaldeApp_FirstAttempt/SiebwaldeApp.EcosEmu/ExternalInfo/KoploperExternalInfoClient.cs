using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    public class KoploperExternalInfoClient : IBlockPositionProvider
    {
        private readonly string _host;
        private readonly int _port;

        private readonly ConcurrentDictionary<int, int> _locToBlock = new();
        private readonly StringBuilder _currentField = new();
        private readonly List<string> _currentFields = new();

        public event Action<int, int>? BlockEntered;

        private CancellationTokenSource? _cts;

        public KoploperExternalInfoClient(string host = "127.0.0.1", int port = 5700)
        {
            _host = host;
            _port = port;
        }

        public int? TryGetBlockForLoc(int loc)
        {
            return _locToBlock.TryGetValue(loc, out var block) ? block : (int?)null;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => RunAsync(_cts.Token));
            Console.WriteLine("[EXT] ExternalInfo client started.");
        }

        public void Stop()
        {
            _cts?.Cancel();
            Console.WriteLine("[EXT] ExternalInfo client stopped.");
        }

        private async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(_host, _port, ct);
                    Console.WriteLine("[EXT] Connected with Koploper external information (5700).");

                    using var stream = client.GetStream();

                    while (!ct.IsCancellationRequested)
                    {
                        int value = stream.ReadByte();
                        if (value < 0)
                            break;

                        ProcessByteFromKoploper((byte)value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[EXT] Error: " + ex.Message);
                    await Task.Delay(1000, ct);
                }
            }
        }

        /// <summary>
        /// Processes a single byte from the Koploper external info stream.
        /// The record format as observed:
        ///   &4 0x1B 4 0x1B 11:26:51 0x1B 23:37:16 0x1B Route onbekend 0x1B
        /// That is: 5 ASCII fields separated by 0x1B.
        /// Field 0: header containing the loco number (e.g. "&4")
        /// Field 1: block number (e.g. "4")
        /// Field 2: model time (e.g. "11:26:51")
        /// Field 3: PC time (e.g. "23:37:16")
        /// Field 4: description (e.g. "Route onbekend")
        /// </summary>
        private void ProcessByteFromKoploper(byte b)
        {
            const byte Separator = 0x1B;

            if (b == 0x00)
            {
                // End-of-record marker in some descriptions. For now we ignore it,
                // because we already work with 0x1B as separator.
                return;
            }

            if (b == Separator)
            {
                // End of current field
                if (_currentField.Length > 0)
                {
                    _currentFields.Add(_currentField.ToString());
                    _currentField.Clear();
                }

                // When we have 5 fields, we consider the record complete
                if (_currentFields.Count >= 5)
                {
                    HandleCompletedRecord(_currentFields);
                    _currentFields.Clear();
                }

                return;
            }

            // Normal ASCII character, append to current field
            _currentField.Append((char)b);
        }

        /// <summary>
        /// Handles a completed record from Koploper (5 fields).
        /// </summary>
        private void HandleCompletedRecord(List<string> fields)
        {
            //Console.WriteLine("[EXT] ----- Record received -----");
            //for (int i = 0; i < fields.Count; i++)
            //{
            //    Console.WriteLine($"[EXT] Field {i}: \"{fields[i]}\"");
            //}

            // Expected:
            // fields[0] = header (e.g. "&4")
            // fields[1] = block (e.g. "4")
            // fields[2] = model time ("11:26:51")
            // fields[3] = PC time ("23:37:16")
            // fields[4] = description ("Route onbekend")

            if (fields.Count < 2)
                return;

            string header = fields[0];
            string blockText = fields[1];
            string modelTime = fields.Count > 2 ? fields[2] : string.Empty;
            string pcTime = fields.Count > 3 ? fields[3] : string.Empty;
            string description = fields.Count > 4 ? fields[4] : string.Empty;

            // Extract loco number from header. In the sample we have "&4".
            // We strip leading '&' characters and whitespace.
            string locoPart = header.Trim().TrimStart('&');
            if (!int.TryParse(locoPart, out int loco))
            {
                Console.WriteLine($"[EXT] Could not parse loco number from '{header}'.");
                return;
            }

            if (!int.TryParse(blockText.Trim(), out int block))
            {
                Console.WriteLine($"[EXT] Could not parse block number from '{blockText}'.");
                return;
            }

            _locToBlock[loco] = block;

            Console.WriteLine(
                $"[EXT] Loc {loco} -> Block {block} (modelTime={modelTime}, pcTime={pcTime}, desc=\"{description}\")");

            BlockEntered?.Invoke(loco, block);
        }

        //private void ParseRecord(NetworkStream stream, CancellationToken ct)
        //{
        //    // Binary record from Koploper external info:
        //    // 0x1B <len> 0x1B <loc> 0x1B <block> 0x1B <modelTime> 0x1B <pcTime> 0x1B <description> 0x00

        //    // 1. Length byte (we currently do not use it)
        //    int len = stream.ReadByte();
        //    if (len < 0)
        //        return;

        //    // 2. Loc number (ASCII, prefixed by 0x1B and terminated by next 0x1B)
        //    if (stream.ReadByte() != 0x1B)
        //        return;
        //    string locText = ReadAsciiField(stream, lastField: false);
        //    if (!int.TryParse(locText, out int loc))
        //        return;

        //    // 3. Block number (ASCII)
        //    if (stream.ReadByte() != 0x1B)
        //        return;
        //    string blockText = ReadAsciiField(stream, lastField: false);
        //    if (!int.TryParse(blockText, out int block))
        //        return;

        //    // 4. Model time (we do not use the value yet, just consume it)
        //    if (stream.ReadByte() != 0x1B)
        //        return;
        //    string modelTime = ReadAsciiField(stream, lastField: false);

        //    // 5. PC time (also not used yet)
        //    if (stream.ReadByte() != 0x1B)
        //        return;
        //    string pcTime = ReadAsciiField(stream, lastField: false);

        //    // 6. Description (last field, terminated by 0x00)
        //    if (stream.ReadByte() != 0x1B)
        //        return;
        //    string description = ReadAsciiField(stream, lastField: true);
        //    // After this call we have consumed the trailing 0x00 as well.

        //    // Update internal state
        //    _locToBlock[loc] = block;

        //    Console.WriteLine($"[EXT] Loc {loc} -> Block {block} (modelTime={modelTime}, pcTime={pcTime}, desc=\"{description}\")");

        //    BlockEntered?.Invoke(loc, block);
        //}

        /// <summary>
        /// Reads an ASCII field value from the stream.
        /// For non-last fields, the field is terminated by 0x1B.
        /// For the last field in the record, it is terminated by 0x00.
        /// The leading separator (0x1B) is expected to be already consumed by the caller.
        /// </summary>
        private string ReadAsciiField(NetworkStream stream, bool lastField)
        {
            var sb = new StringBuilder();

            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0)
                {
                    // Connection closed
                    return string.Empty;
                }

                if (!lastField && b == 0x1B)
                {
                    // Separator before the next field
                    break;
                }

                if (lastField && b == 0x00)
                {
                    // End of record
                    break;
                }

                sb.Append((char)b);
            }

            return sb.ToString();
        }

        private int ReadNumber(NetworkStream stream)
        {
            // ASCII digits until 0x1B
            string s = "";
            int b;

            while ((b = stream.ReadByte()) >= 0)
            {
                if (b == 0x1B)
                    break;
                s += (char)b;
            }

            if (int.TryParse(s, out var num))
                return num;

            return -1;
        }

        private void SkipField(NetworkStream stream)
        {
            // Skip until 0x1B
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0 || b == 0x1B)
                    break;
            }
        }

        private void SkipStringField(NetworkStream stream)
        {
            // Skip ASCII until 0x1B
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0 || b == 0x1B)
                    break;

                // Could store chars here, but we skip them
            }
        }
    }
}
