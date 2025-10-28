using System;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Minimal fake that implements both ITrackIn and ITrackOut so the app can run without hardware.
    /// </summary>
    public sealed class FakeTrackAdapter : ITrackIn, ITrackOut, IDisposable
    {
        // ---- ITrackIn events (no-op) ----
        public event Action<IncomingDetectedEvent> IncomingDetected { add { } remove { } }
        public event Action<EntrySensorEvent> EntrySensorTriggered { add { } remove { } }
        public event Action<ExitFreeChangedEvent> ExitBlockFreeChanged { add { } remove { } }
        public event Action<AmplifierFeedbackEvent> AmplifierOccupiedChanged { add { } remove { } }
        public event Action<TrainClearedEvent> TrainClearedFromBlock { add { } remove { } }
        public event Action<HardwareAliveEvent> HardwareAliveChanged { add { } remove { } }

        // ---- ITrackOut commands (no-op) ----
        public void SetAmplifierStop(int trackNumber, bool stop) { }
        public void SetSignalEntry(bool isTopSide, bool green) { }
        public void SetSignalExit(bool isTopSide, bool green) { }
        public void SetSwitch(int switchId, bool position) { }
        public void StopBeforeStation(bool isTopSide) { }

        public void Dispose() { }
    }
}
