// File: SiebwaldeApp.Core/Domain/Amplifier.cs
namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Analog amplifier model: Occupied IN (command) + Occupied OUT (feedback).
    /// </summary>
    public class Amplifier
    {
        private readonly ITrackOut _bus;
        public int TrackNumber { get; }
        public bool OccupiedOut { get; private set; }

        public Amplifier(int trackNumber, ITrackOut bus)
        {
            TrackNumber = trackNumber;
            _bus = bus;
        }

        public void Stop() => _bus.SetAmplifierStop(TrackNumber, stop: true);  // Occupied IN = stop
        public void Start() => _bus.SetAmplifierStop(TrackNumber, stop: false); // Occupied IN off = start

        public void SetFeedback(bool occupiedOut) => OccupiedOut = occupiedOut; // from ITrackIn
    }
}
