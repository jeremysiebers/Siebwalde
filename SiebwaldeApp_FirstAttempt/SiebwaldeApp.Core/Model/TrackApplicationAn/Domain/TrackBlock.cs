// File: SiebwaldeApp.Core/Domain/TrackBlock.cs
namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Physical block/track: entry/exit sensors, optional signal head, and amplifier.
    /// </summary>
    public class TrackBlock
    {
        public int Id { get; }
        public TrackSensor EntrySensor { get; }
        public TrackSensor ExitSensor { get; }
        public Signal Signal { get; }
        public Amplifier Amplifier { get; }

        public bool IsOccupied => EntrySensor.IsActive || ExitSensor.IsActive || Amplifier.OccupiedOut;

        public TrackBlock(int id, TrackSensor entry, TrackSensor exit, Signal signal, Amplifier amplifier)
        {
            Id = id;
            EntrySensor = entry;
            ExitSensor = exit;
            Signal = signal;
            Amplifier = amplifier;
        }

        public void CommandStop() => Amplifier.Stop();
        public void CommandStart() => Amplifier.Start();

        public void SetSignalRed() { if (Signal != null) Signal.Set(SignalAspect.Red); }
        public void SetSignalGreen() { if (Signal != null) Signal.Set(SignalAspect.Green); }
    }
}
