using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.EcosEmu
{
    public class EcosEmulatorServer
    {
        /// <summary>Upper bound for how long StopAsync waits for a stuck client handler.</summary>
        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

        private readonly int _port;
        private readonly IEcosCommandParser _parser;
        private readonly IEcosBackend _backend;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _acceptLoopTask;
        private readonly ConcurrentDictionary<Task, byte> _clientTasks = new();

        /// <summary>Raised when the accept loop faults unexpectedly (never on cancellation).</summary>
        public event EventHandler<RuntimeFaultEventArgs>? Faulted;

        public EcosEmulatorServer(int port, IEcosCommandParser parser, IEcosBackend backend)
        {
            _port = port;
            _parser = parser;
            _backend = backend;
        }

        public void Start()
        {
            // Clear any completed client-handler bookkeeping from a previous run so a
            // stop-then-start cycle starts clean.
            _clientTasks.Clear();

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, _port);

            // The host can switch from simulator to real mode, which stops and immediately
            // restarts the listener on the same port. Allow the rebind while a previously
            // accepted connection is still winding down.
            _listener.Server.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);

            _listener.Start();
            _acceptLoopTask = AcceptLoopAsync(_cts.Token);

            // Observe the accept loop and surface an unexpected fault. Cancellation and disposed
            // listener are handled inside AcceptLoopAsync, so they never surface here as a fault.
            _ = _acceptLoopTask.ContinueWith(
                t =>
                {
                    if (t.IsFaulted)
                    {
                        var inner = t.Exception?.InnerException ?? t.Exception;
                        Faulted?.Invoke(this, new RuntimeFaultEventArgs("EcosEmulatorServer.AcceptLoop", inner!));
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            Console.WriteLine($"ECoS emulator listens on 127.0.0.1:{_port}");
        }

        /// <summary>
        /// Synchronous, non-blocking stop for backward compatibility. Cancels the token and
        /// stops/disposes the listener, but does not wait for the accept loop or client
        /// handlers to finish. Idempotent.
        /// </summary>
        public void Stop()
        {
            var cts = Interlocked.Exchange(ref _cts, null);
            var listener = Interlocked.Exchange(ref _listener, null);

            cts?.Cancel();
            try
            {
                listener?.Stop();
            }
            catch (Exception)
            {
                // The listener may already be stopped/disposed; shutdown must not throw.
            }
            listener?.Dispose();
        }

        /// <summary>
        /// Graceful stop: cancels the token, stops and disposes the listener, then awaits the
        /// accept loop and every in-flight client handler with a bounded timeout so a stuck
        /// client cannot hang shutdown forever. Idempotent and resets state so <see cref="Start"/>
        /// can be called again.
        /// </summary>
        public async Task<bool> StopAsync(CancellationToken ct = default)
        {
            var cts = Interlocked.Exchange(ref _cts, null);
            var listener = Interlocked.Exchange(ref _listener, null);
            var acceptLoopTask = Interlocked.Exchange(ref _acceptLoopTask, null);

            cts?.Cancel();
            try
            {
                listener?.Stop();
            }
            catch (Exception)
            {
                // The listener may already be stopped/disposed; shutdown must not throw.
            }
            listener?.Dispose();

            var allCompleted = true;

            if (acceptLoopTask is not null)
            {
                allCompleted &= await WaitBoundedAsync(acceptLoopTask, ct).ConfigureAwait(false);
            }

            // Await every in-flight client handler, then drop them all.
            var handlers = _clientTasks.Keys.ToArray();
            foreach (var handler in handlers)
            {
                allCompleted &= await WaitBoundedAsync(handler, ct).ConfigureAwait(false);
            }
            _clientTasks.Clear();

            return allCompleted;
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            // Capture the listener once so Stop()/StopAsync() nulling _listener in the tiny
            // window between the loop check and AcceptTcpClientAsync cannot cause a null
            // dereference. The captured instance is the same one Stop()/StopAsync() stop+dispose,
            // so a stop still surfaces as the already-handled ObjectDisposedException,
            // SocketException, or OperationCanceledException.
            var listener = _listener
                ?? throw new InvalidOperationException("The ECoS server has not been started.");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(ct);
                    Console.WriteLine("Koploper connected.");
                    StartClientHandler(client, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
                // Stop()/StopAsync() closed the listener while an accept was pending.
            }
            catch (SocketException)
            {
                // The listener socket was shut down while an accept was pending.
            }
        }

        /// <summary>
        /// Tracks a client-handler task so StopAsync can await it, and removes it from the
        /// tracking set (and observes any exception) once it completes.
        /// </summary>
        private void StartClientHandler(TcpClient client, CancellationToken ct)
        {
            var task = HandleClientAsync(client, ct);
            _clientTasks[task] = 0;
            _ = task.ContinueWith(
                t =>
                {
                    _ = t.Exception; // observe any fault so it never becomes unobserved
                    _clientTasks.TryRemove(t, out _);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
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
                // A faulted/cancelled handler must not make shutdown throw.
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
                    catch (OperationCanceledException)
                    {
                        // The host is shutting down while Koploper is still connected.
                        break;
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
