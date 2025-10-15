// File: SiebwaldeApp.Core/Ports/ITrackBus.cs
namespace SiebwaldeApp.Core
{
    using System;

    public interface ITrackIn
    {
        event Action<IncomingDetectedEvent> IncomingDetected;
        event Action<EntrySensorEvent> EntrySensorTriggered;
        event Action<ExitFreeChangedEvent> ExitBlockFreeChanged;
        event Action<AmplifierFeedbackEvent> AmplifierOccupiedChanged;
        event Action<TrainClearedEvent> TrainClearedFromBlock;
        event Action<HardwareAliveEvent> HardwareAliveChanged;
    }

    public interface ITrackOut
    {
        void SetAmplifierStop(int trackNumber, bool stop);
        void SetSignalEntry(bool isTopSide, bool green);
        void SetSignalExit(bool isTopSide, bool green);
        void SetSwitch(int switchId, SwitchPosition position);
        void StopBeforeStation(bool isTopSide);
    }

    public enum SwitchPosition { Straight, Diverging }

    public readonly record struct IncomingDetectedEvent(bool IsTopSide, bool IsFreight, DateTime Utc);
    public readonly record struct EntrySensorEvent(bool IsTopSide, int TrackNumber, DateTime Utc);
    public readonly record struct ExitFreeChangedEvent(bool IsTopSide, bool IsFree, DateTime Utc);
    public readonly record struct AmplifierFeedbackEvent(int TrackNumber, bool OccupiedOut, DateTime Utc);
    public readonly record struct TrainClearedEvent(bool IsTopSide, int TrackNumber, DateTime Utc);
    public readonly record struct HardwareAliveEvent(bool IsAlive, DateTime Utc);
}
