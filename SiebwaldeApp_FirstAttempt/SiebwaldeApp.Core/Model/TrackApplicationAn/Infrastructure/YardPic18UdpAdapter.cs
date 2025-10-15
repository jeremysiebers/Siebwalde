// File: SiebwaldeApp.Core/Infrastructure/YardPic18UdpAdapter.cs


namespace SiebwaldeApp.Core
{
    /// <summary>
    /// UDP adapter for the Yard PIC:
    /// - Implements IYardIn/IYardOut (separate from the mainline Track adapter).
    /// - Parses yard-specific frames into domain events (no debounce; PIC already filters).
    /// - Encodes yard commands (merge signal, yard switches, authorize merge) to UDP frames.
    /// Usage:
    ///   var tr = new RawUdpTransport("192.168.1.202", 5001);
    ///   var yard = new YardPic18UdpAdapter(tr);
    ///   await yard.StartAsync(ct);
    /// </summary>
    public sealed class YardPic18UdpAdapter : IYardIn, IYardOut, IDisposable
    {
        private readonly IRawUdpTransport _transport;
        private CancellationTokenSource _cts;

        private IoSnapshot _last = IoSnapshot.Empty;

        public YardPic18UdpAdapter(IRawUdpTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        // ---- Lifecycle ----

        public Task StartAsync(CancellationToken token = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            return _transport.StartReceiveLoopAsync(async payload =>
            {
                ProcessIncomingBytes(payload);
                await Task.CompletedTask;
            }, _cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            try { _transport?.Dispose(); } catch { }
        }

        // ---- IYardIn events ----

        public event Action<YardMergeRequestEvent> MergeRequest;
        public event Action<YardSignalFeedbackEvent> YardSignalChanged;
        public event Action<HardwareAliveEvent> HardwareAliveChanged;

        // ---- Inbound parsing (edge detection only) ----

        /// <summary>
        /// Parse a Yard PIC frame and raise events on state changes.
        /// NOTE: Replace IoSnapshot.Parse mapping with your actual firmware layout.
        /// </summary>
        public void ProcessIncomingBytes(byte[] frame)
        {
            var now = DateTime.UtcNow;
            var cur = IoSnapshot.Parse(frame);

            // Consider PIC alive on any packet
            HardwareAliveChanged?.Invoke(new HardwareAliveEvent(true, now));

            // Merge request: false->true means operator requested to inject a train
            if (!_last.MergeRequested && cur.MergeRequested)
                MergeRequest?.Invoke(new YardMergeRequestEvent(RequestGrantedByOperator: cur.OperatorGranted, now));

            // Yard signal feedback change
            if (_last.MergeSignalGreen != cur.MergeSignalGreen)
                YardSignalChanged?.Invoke(new YardSignalFeedbackEvent(cur.MergeSignalGreen, now));

            _last = cur;
        }

        // ---- IYardOut commands ----

        public void SetYardSignal(bool mergeSignalGreen)
        {
            Send(BuildYardSignalPayload(mergeSignalGreen));
        }

        public void SetYardSwitch(int switchId, SwitchPosition position)
        {
            Send(BuildYardSwitchPayload(switchId, position));
        }

        public void AuthorizeMerge(bool authorize)
        {
            Send(BuildAuthorizeMergePayload(authorize));
        }

        private void Send(byte[] payload) => _transport.Send(payload);

        // ---- Encoding helpers (TODO: encode according to your Yard PIC protocol) ----

        private static byte[] BuildYardSignalPayload(bool green) =>
            new byte[] { 0xE1, (byte)(green ? 1 : 0) };

        private static byte[] BuildYardSwitchPayload(int switchId, SwitchPosition pos) =>
            new byte[] { 0xE2, (byte)switchId, (byte)(pos == SwitchPosition.Straight ? 0 : 1) };

        private static byte[] BuildAuthorizeMergePayload(bool authorize) =>
            new byte[] { 0xE3, (byte)(authorize ? 1 : 0) };

        // ---- Snapshot (bytes -> typed yard state). Replace mapping with your real layout. ----

        private sealed class IoSnapshot
        {
            // Operator requested to insert train into mainline
            public bool MergeRequested { get; init; }
            // If operator granted it (e.g., RC confirms) – if present in your protocol
            public bool OperatorGranted { get; init; }
            // Yard's own merge signal feedback
            public bool MergeSignalGreen { get; init; }

            public static IoSnapshot Empty => new IoSnapshot();

            public static IoSnapshot Parse(byte[] b)
            {
                // TODO: Replace with actual bit mapping coming from Yard PIC
                bool HasBit(int i, int bit) => b.Length > i && (b[i] & (1 << bit)) != 0;

                var mergeRequested = HasBit(0, 0); // example only
                var operatorGranted = HasBit(0, 1); // example only
                var signalGreen = HasBit(0, 2); // example only

                return new IoSnapshot
                {
                    MergeRequested = mergeRequested,
                    OperatorGranted = operatorGranted,
                    MergeSignalGreen = signalGreen
                };
            }
        }
    }
}
