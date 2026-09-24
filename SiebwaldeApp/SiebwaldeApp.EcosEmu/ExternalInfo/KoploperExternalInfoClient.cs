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
        /// <summary>Upper bound for how long StopAsync waits for the run task.</summary>
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

        private readonly string _host;
        private readonly int _port;

        private readonly ConcurrentDictionary<int, int> _locToBlock = new();
        private readonly StringBuilder _currentField = new();
        private readonly List<string> _currentFields = new();

        public event Action<int, int>? BlockEntered;

        private CancellationTokenSource? _cts;
        private Task? _runTask;
        private TcpClient? _client;
        private NetworkStream? _stream;

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
            _runTask = Task.Run(() => RunAsync(_cts.Token));
            Console.WriteLine("[EXT] ExternalInfo client started.");
        }

        /// <summary>
        /// Synchronous, non-blocking stop for backward compatibility: cancels the token and
        /// disposes the current connection so a pending read is unblocked. Does not await the
        /// run task. Idempotent.
        /// </summary>
        public void Stop()
        {
            var cts = Interlocked.Exchange(ref _cts, null);
            cts?.Cancel();

            DisposeCurrentConnection();

            Console.WriteLine("[EXT] ExternalInfo client stopped.");
        }

        /// <summary>
        /// Graceful stop: cancels the token, disposes the current connection, and awaits the
        /// run task with a bounded timeout. Idempotent and resets state so <see cref="Start"/>
        /// can be called again.
        /// </summary>
        public async Task<bool> StopAsync(CancellationToken ct = default)
        {
            var cts = Interlocked.Exchange(ref _cts, null);
            cts?.Cancel();

            DisposeCurrentConnection();

            var runTask = Interlocked.Exchange(ref _runTask, null);
            var completed = true;
            if (runTask is not null)
            {
                completed = await WaitBoundedAsync(runTask, ct).ConfigureAwait(false);
            }

            Console.WriteLine("[EXT] ExternalInfo client stopped.");
            return completed;
        }

        /// <summary>
        /// Disposes the current connection (if any) and clears the references, so a pending
        /// synchronous/async read on that stream is unblocked. Uses a reference check so a
        /// connection created by a concurrent restart is never clobbered here.
        /// </summary>
        private void DisposeCurrentConnection()
        {
            var stream = Interlocked.Exchange(ref _stream, null);
            var client = Interlocked.Exchange(ref _client, null);
            try { stream?.Dispose(); } catch { }
            try { client?.Dispose(); } catch { }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient? client = null;
                NetworkStream? stream = null;
                try
                {
                    client = new TcpClient();
                    _client = client;
                    await client.ConnectAsync(_host, _port, ct);
                    Console.WriteLine("[EXT] Connected with Koploper external information (5700).");

                    stream = client.GetStream();
                    _stream = stream;

                    var buffer = new byte[1];

                    while (!ct.IsCancellationRequested)
                    {
                        int bytesRead;
                        try
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                        }
                        catch (OperationCanceledException)
                        {
                            // Clean shutdown: the host is stopping while connected.
                            break;
                        }
                        catch (IOException)
                        {
                            // Connection dropped by the peer or disposed by Stop()/StopAsync().
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            // Connection disposed by Stop()/StopAsync().
                            break;
                        }

                        if (bytesRead <= 0)
                            break;

                        ProcessByteFromKoploper(buffer[0]);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancellation during ConnectAsync or the reconnect delay: exit cleanly.
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[EXT] Error: " + ex.Message);
                    try
                    {
                        await Task.Delay(1000, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                finally
                {
                    // Only clear the shared reference if it still points at this connection,
                    // otherwise a concurrent restart's connection would be clobbered.
                    if (stream is not null)
                    {
                        if (ReferenceEquals(_stream, stream))
                            Interlocked.CompareExchange(ref _stream, null, stream);
                        try { stream.Dispose(); } catch { }
                    }

                    if (client is not null)
                    {
                        if (ReferenceEquals(_client, client))
                            Interlocked.CompareExchange(ref _client, null, client);
                        try { client.Dispose(); } catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Waits for a task with a bounded timeout, swallowing shutdown exceptions. Returns true
        /// when the task completed within the bound, false when the bound expired (or the wait was
        /// cancelled) and the wait gave up.
        /// </summary>
        private static async Task<bool> WaitBoundedAsync(Task task, CancellationToken ct)
        {
            if (task.IsCompleted)
            {
                Observe(task);
                return true;
            }

            var completed = await Task.WhenAny(task, Task.Delay(StopTimeout, ct)).ConfigureAwait(false);
            if (completed == task)
            {
                Observe(task);
                return true;
            }
            // Otherwise the timeout elapsed (or ct was cancelled): give up so shutdown cannot hang.
            return false;
        }

        private static void Observe(Task task)
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // A faulted/cancelled run task must not make shutdown throw.
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
