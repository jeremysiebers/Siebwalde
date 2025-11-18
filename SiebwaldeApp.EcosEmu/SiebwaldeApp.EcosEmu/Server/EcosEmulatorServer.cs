using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    public class EcosEmulatorServer
    {
        private readonly int _port;
        private readonly IEcosCommandParser _parser;
        private readonly IEcosBackend _backend;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public EcosEmulatorServer(int port, IEcosCommandParser parser, IEcosBackend backend)
        {
            _port = port;
            _parser = parser;
            _backend = backend;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _ = AcceptLoopAsync(_cts.Token);
            Console.WriteLine($"ECoS emulator listens on 127.0.0.1:{_port}");
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await _listener!.AcceptTcpClientAsync(ct);
                    Console.WriteLine("Koploper connected.");
                    _ = HandleClientAsync(client, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
            {
                var buffer = new byte[4096];
                var sb = new StringBuilder();

                Console.WriteLine("Koploper connected.");

                while (!ct.IsCancellationRequested)
                {
                    int bytesRead;
                    try
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine("  !! IOException in ReadAsync: " + ioEx.Message);
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        Console.WriteLine("  !! Stream closed during ReadAsync.");
                        break;
                    }

                    if (bytesRead <= 0)
                    {
                        // client heeft verbinding netjes gesloten
                        break;
                    }

                    // Bytes → ASCII
                    var chunk = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    sb.Append(chunk);

                    Console.WriteLine("RX-CHUNK: " + chunk.Replace("\0", "\\0"));

                    // Commands knippen: alles tussen vorige start en ')'
                    int cmdStart = 0;
                    for (int i = 0; i < sb.Length; i++)
                    {
                        if (sb[i] == ')')
                        {
                            int length = i - cmdStart + 1;
                            var cmdText = sb.ToString(cmdStart, length).Trim();

                            if (!string.IsNullOrWhiteSpace(cmdText))
                            {
                                Console.WriteLine("  CMD: " + cmdText);

                                try
                                {
                                    var cmd = _parser.Parse(cmdText);
                                    if (cmd == null)
                                    {
                                        Console.WriteLine("  (parser gave null)");
                                    }
                                    else
                                    {
                                        await _backend.HandleAsync(cmd, writer, ct);
                                    }
                                }
                                catch (IOException ioEx)
                                {
                                    Console.WriteLine("  !! IOException in backend.HandleAsync: " + ioEx.Message);
                                    // waarschijnlijk client disconnect → stop met verwerken
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("  !! Exception in backend.HandleAsync: " + ex);
                                    break;
                                }
                            }

                            cmdStart = i + 1;
                        }
                    }

                    // verwerkte tekst uit de buffer halen
                    if (cmdStart > 0)
                    {
                        sb.Remove(0, cmdStart);
                    }
                }

                Console.WriteLine("Koploper disconnected.");
            }
        }
    }
}
