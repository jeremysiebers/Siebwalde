// SiebwaldeApp.Tests/TestDoubles/TestTrackIn.cs
using System;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Tests
{
    public sealed class TestTrackIn : ITrackIn
    {
        public event Action<IncomingDetectedEvent> IncomingDetected;
        public event Action<EntrySensorEvent> EntrySensorTriggered;
        public event Action<ExitFreeChangedEvent> ExitBlockFreeChanged;
        public event Action<AmplifierFeedbackEvent> AmplifierOccupiedChanged;
        public event Action<TrainClearedEvent> TrainClearedFromBlock;
        public event Action<HardwareAliveEvent> HardwareAliveChanged;

        // helpers to raise events
        public void RaiseIncoming(bool isTop, bool isFreight) =>
            IncomingDetected?.Invoke(new IncomingDetectedEvent(isTop, isFreight, DateTime.UtcNow));

        public void RaiseEntry(bool isTop, int track) =>
            EntrySensorTriggered?.Invoke(new EntrySensorEvent(isTop, track, DateTime.UtcNow));

        public void RaiseExitFree(bool isTop, bool free) =>
            ExitBlockFreeChanged?.Invoke(new ExitFreeChangedEvent(isTop, free, DateTime.UtcNow));

        public void RaiseCleared(bool isTop, int track) =>
            TrainClearedFromBlock?.Invoke(new TrainClearedEvent(isTop, track, DateTime.UtcNow));
    }
}
