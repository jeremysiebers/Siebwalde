// File: SiebwaldeApp.Core/Infrastructure/Pic18UdpAdapter.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Bridges PIC18 UDP frames to domain events (ITrackIn) and encodes domain commands to UDP (ITrackOut).
    /// - Edge detection only (no debounce) because the PIC already filters signals.
    /// - Two usage patterns:
    ///     (A) Passive: call ProcessIncomingBytes(byte[]) from your existing TrackIOHandle.
    ///     (B) Active : provide an IRawUdpTransport and call StartAsync() to run a receive loop.
    /// </summary>
    public sealed class TrackPic18UdpAdapter : ITrackIn, ITrackOut, IDisposable
    {
        private readonly IRawUdpTransport _transport; // optional; null when used in Passive mode
        private CancellationTokenSource _cts;

        // Last snapshot for edge detection
        private IoSnapshot _last = IoSnapshot.Empty;

        // --- Ctors ---

        /// <summary>
        /// Active mode: the adapter owns a UDP transport and can StartAsync() a receive loop.
        /// </summary>
        public TrackPic18UdpAdapter(IRawUdpTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        /// <summary>
        /// Passive mode: construct without transport and call ProcessIncomingBytes(...) yourself.
        /// Useful when you already have a TrackIOHandle that receives bytes.
        /// </summary>
        public TrackPic18UdpAdapter() { /* no transport, passive injection */ }

        // --- Lifecycle for Active mode ---

        public Task StartAsync(CancellationToken token = default)
        {
            if (_transport == null)
                throw new InvalidOperationException("This adapter has no transport. Use the active-mode constructor.");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            return _transport.StartReceiveLoopAsync(async payload =>
            {
                ProcessIncomingBytes(payload);
                await Task.CompletedTask;
            }, _cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        // --- ITrackIn events ---

        public event Action<IncomingDetectedEvent> IncomingDetected;
        public event Action<EntrySensorEvent> EntrySensorTriggered;
        public event Action<ExitFreeChangedEvent> ExitBlockFreeChanged;
        public event Action<AmplifierFeedbackEvent> AmplifierOccupiedChanged;
        public event Action<TrainClearedEvent> TrainClearedFromBlock;
        public event Action<HardwareAliveEvent> HardwareAliveChanged;

        // --- Inbound: PIC -> Domain (edge detection only) ---

        /// <summary>
        /// Call this on every received PIC frame. Parses bytes to IoSnapshot and raises domain events on changes.
        /// </summary>
        public void ProcessIncomingBytes(byte[] frame)
        {
            var now = DateTime.UtcNow;
            var cur = IoSnapshot.Parse(frame);

            // Mark hardware alive on any packet (optional)
            HardwareAliveChanged?.Invoke(new HardwareAliveEvent(true, now));

            // Approaching (false -> true)
            if (!_last.TopIncoming && cur.TopIncoming)
                IncomingDetected?.Invoke(new IncomingDetectedEvent(true, cur.TopIsFreight, now));
            if (!_last.BottomIncoming && cur.BottomIncoming)
                IncomingDetected?.Invoke(new IncomingDetectedEvent(false, cur.BottomIsFreight, now));

            // Exit free changes
            if (_last.TopExitFree != cur.TopExitFree)
                ExitBlockFreeChanged?.Invoke(new ExitFreeChangedEvent(true, cur.TopExitFree, now));
            if (_last.BottomExitFree != cur.BottomExitFree)
                ExitBlockFreeChanged?.Invoke(new ExitFreeChangedEvent(false, cur.BottomExitFree, now));

            // Entry sensors per track (false -> true)
            foreach (var t in cur.TopTracks)
            {
                var prev = _last.TopTracks.FirstOrDefault(x => x.Number == t.Number);
                if (prev is not null && !prev.Entry && t.Entry)
                    EntrySensorTriggered?.Invoke(new EntrySensorEvent(true, t.Number, now));
            }
            foreach (var t in cur.BottomTracks)
            {
                var prev = _last.BottomTracks.FirstOrDefault(x => x.Number == t.Number);
                if (prev is not null && !prev.Entry && t.Entry)
                    EntrySensorTriggered?.Invoke(new EntrySensorEvent(false, t.Number, now));
            }

            // Amplifier feedback (Occupied OUT) change events, and cleared → block free
            foreach (var t in cur.TopTracks)
            {
                var prev = _last.TopTracks.FirstOrDefault(x => x.Number == t.Number);
                if (prev is not null && prev.OccupiedOut != t.OccupiedOut)
                    AmplifierOccupiedChanged?.Invoke(new AmplifierFeedbackEvent(t.Number, t.OccupiedOut, now));

                // Example: when we detect a transition to not occupied, signal that the block cleared
                if (prev is not null && prev.OccupiedOut && !t.OccupiedOut)
                    TrainClearedFromBlock?.Invoke(new TrainClearedEvent(true, t.Number, now));
            }
            foreach (var t in cur.BottomTracks)
            {
                var prev = _last.BottomTracks.FirstOrDefault(x => x.Number == t.Number);
                if (prev is not null && prev.OccupiedOut != t.OccupiedOut)
                    AmplifierOccupiedChanged?.Invoke(new AmplifierFeedbackEvent(t.Number, t.OccupiedOut, now));

                if (prev is not null && prev.OccupiedOut && !t.OccupiedOut)
                    TrainClearedFromBlock?.Invoke(new TrainClearedEvent(false, t.Number, now));
            }

            _last = cur;
        }

        // --- Outbound: Domain -> PIC commands ---

        public void SetAmplifierStop(int trackNumber, bool stop)
        {
            Send(BuildAmplifierStopPayload(trackNumber, stop));
        }

        public void SetSignalEntry(bool isTopSide, bool green)
        {
            Send(BuildSignalEntryPayload(isTopSide, green));
        }

        public void SetSignalExit(bool isTopSide, bool green)
        {
            Send(BuildSignalExitPayload(isTopSide, green));
        }

        public void SetSwitch(int switchId, SwitchPosition position)
        {
            Send(BuildSwitchPayload(switchId, position));
        }

        public void StopBeforeStation(bool isTopSide)
        {
            Send(BuildStopBeforeStationPayload(isTopSide));
        }

        private void Send(byte[] payload)
        {
            if (_transport == null)
                throw new InvalidOperationException("No transport configured. Use active-mode constructor, or route sends via your TrackIOHandle.");
            _transport.Send(payload);
        }

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            try { _transport?.Dispose(); } catch { }
        }

        // --- Encoding helpers (TODO: replace with your real protocol) ---

        private static byte[] BuildAmplifierStopPayload(int trackNumber, bool stop) =>
            new byte[] { 0xA1, (byte)trackNumber, (byte)(stop ? 1 : 0) };

        private static byte[] BuildSignalEntryPayload(bool isTopSide, bool green) =>
            new byte[] { 0xB1, (byte)(isTopSide ? 1 : 0), (byte)(green ? 1 : 0) };

        private static byte[] BuildSignalExitPayload(bool isTopSide, bool green) =>
            new byte[] { 0xB2, (byte)(isTopSide ? 1 : 0), (byte)(green ? 1 : 0) };

        private static byte[] BuildSwitchPayload(int switchId, SwitchPosition pos) =>
            new byte[] { 0xC1, (byte)switchId, (byte)(pos == SwitchPosition.Straight ? 0 : 1) };

        private static byte[] BuildStopBeforeStationPayload(bool isTopSide) =>
            new byte[] { 0xD1, (byte)(isTopSide ? 1 : 0) };

        // --- Snapshot parsing (bytes -> typed state). Replace mapping with your real layout. ---

        private sealed class IoSnapshot
        {
            public bool TopIncoming { get; init; }
            public bool BottomIncoming { get; init; }
            public bool TopExitFree { get; init; }
            public bool BottomExitFree { get; init; }
            public bool TopIsFreight { get; init; }
            public bool BottomIsFreight { get; init; }

            public TrackIo[] TopTracks { get; init; } = Array.Empty<TrackIo>();
            public TrackIo[] BottomTracks { get; init; } = Array.Empty<TrackIo>();

            public static IoSnapshot Empty => new IoSnapshot
            {
                TopTracks = new TrackIo[]
                {
                    new TrackIo(10,false,false),
                    new TrackIo(11,false,false),
                    new TrackIo(12,false,false),
                },
                BottomTracks = new TrackIo[]
                {
                    new TrackIo(1,false,false),
                    new TrackIo(2,false,false),
                    new TrackIo(3,false,false),
                }
            };

            public sealed record TrackIo(int Number, bool Entry, bool OccupiedOut);

            public static IoSnapshot Parse(byte[] b)
            {
                // TODO: Replace with your actual bit/byte mapping from the PIC firmware.
                bool HasBit(int i, int bit) => b.Length > i && (b[i] & (1 << bit)) != 0;

                var topIncoming = HasBit(0, 0);
                var bottomIncoming = HasBit(0, 1);
                var topExitFree = HasBit(0, 2);
                var bottomExitFree = HasBit(0, 3);
                var topIsFreight = HasBit(0, 4);
                var bottomIsFreight = HasBit(0, 5);

                var topTracks = new[]
                {
                    new TrackIo(10, HasBit(1,0), HasBit(2,0)),
                    new TrackIo(11, HasBit(1,1), HasBit(2,1)),
                    new TrackIo(12, HasBit(1,2), HasBit(2,2)),
                };

                var bottomTracks = new[]
                {
                    new TrackIo(1, HasBit(3,0), HasBit(4,0)),
                    new TrackIo(2, HasBit(3,1), HasBit(4,1)),
                    new TrackIo(3, HasBit(3,2), HasBit(4,2)),
                };

                return new IoSnapshot
                {
                    TopIncoming = topIncoming,
                    BottomIncoming = bottomIncoming,
                    TopExitFree = topExitFree,
                    BottomExitFree = bottomExitFree,
                    TopIsFreight = topIsFreight,
                    BottomIsFreight = bottomIsFreight,
                    TopTracks = topTracks,
                    BottomTracks = bottomTracks
                };
            }
        }
    }
}
