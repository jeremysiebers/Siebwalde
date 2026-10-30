using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Bounded, send-only PIC32 host-authority heartbeat (command 110 HOST_HEARTBEAT).
    ///
    /// The PIC32 host-authority watchdog (2 s) latches the global stop when the controller is
    /// ARMED and no 110 frame has been processed within the window. The check runs
    /// <c>CHECKxHOSTxAUTHORITY()</c> independently of the MBUS/FW-handler wait states
    /// (see TrackController5/firmware/src/controller.c), so the host-paced FW download phase must
    /// keep feeding 110 frames or the global stop latches mid-flash.
    ///
    /// Isolation contract — why a heartbeat frame can never be misread as the 131 DONE/ERROR ack:
    /// 1. SEND-ONLY. This pacer only calls <see cref="ITrackCommClient.SendAsync"/>; it never
    ///    subscribes to or reads <see cref="ITrackCommClient.ControlMessageReceived"/>, so it can
    ///    never consume (steal) the <see cref="FlashOneAmplifierCommand"/>'s 131 acknowledgement.
    /// 2. OUTBOUND vs INBOUND. A 110 frame is an outbound datagram; it never traverses the inbound
    ///    response channel that <see cref="FlashOneAmplifierCommand"/> drains.
    /// 3. PREDICATE ISOLATION. Even the PIC32's only 110-correlated reply — the one-shot
    ///    GLOBAL_STOP_STATE (111) status emitted on the Disarmed→Armed transition — carries
    ///    sender CONTROLLER (10) / command 111, which cannot satisfy any flash-command predicate
    ///    (all are FWHANDLER (30), MBUS (20), CONNECTED (6) or GET_CONTROLLER_VERSION (113)). The
    ///    flash command's wait loop consumes and discards non-matching frames.
    /// 4. BOUNDED LIFETIME. Started immediately before <see cref="FlashOneAmplifierCommand"/>'s
    ///    ExecuteAsync and cancelled + awaited immediately after, so it cannot outlive the flash.
    /// </summary>
    public sealed class FlashOneHeartbeat : IAsyncDisposable
    {
        /// <summary>Reference cadence (~500 ms), mirroring <see cref="GlobalStopMonitor.HeartbeatInterval"/>.</summary>
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(500);

        private readonly ITrackCommClient _comm;
        private readonly TimeSpan _interval;
        private CancellationTokenSource? _cts;
        private Task? _loop;
        private ushort _sequence;

        public FlashOneHeartbeat(ITrackCommClient comm, TimeSpan? interval = null)
        {
            _comm = comm ?? throw new ArgumentNullException(nameof(comm));
            _interval = interval ?? DefaultInterval;
        }

        /// <summary>
        /// Starts the bounded heartbeat loop, linked to <paramref name="flashToken"/> so it stops
        /// when the flash operation is cancelled. Throws if already started.
        /// </summary>
        public void Start(CancellationToken flashToken)
        {
            if (_cts != null)
                throw new InvalidOperationException("FlashOneHeartbeat is already started.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(flashToken);
            var token = _cts.Token;
            _loop = Task.Run(() => RunAsync(token), CancellationToken.None);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                using var timer = new PeriodicTimer(_interval);
                while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    // Wire counter is monotonic modulo 65536, scoped to this transport session.
                    var sequence = _sequence++;
                    await _comm.SendAsync(
                        new SendMessage(
                            TrackCommand.HOST_HEARTBEAT,
                            new byte[] { (byte)sequence, (byte)(sequence >> 8) }),
                        ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on stop/cancel; the heartbeat is best-effort and fire-and-forget.
            }
        }

        public async ValueTask DisposeAsync()
        {
            var cts = _cts;
            if (cts == null)
                return;

            _cts = null;
            cts.Cancel();

            if (_loop is { } loop)
            {
                try { await loop.ConfigureAwait(false); }
                catch (OperationCanceledException) { /* expected */ }
                catch { /* the heartbeat must never fault the caller's teardown */ }
            }

            cts.Dispose();
        }
    }
}
