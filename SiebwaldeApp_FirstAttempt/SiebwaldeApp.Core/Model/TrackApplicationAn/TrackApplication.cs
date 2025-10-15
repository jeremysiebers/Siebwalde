// File: SiebwaldeApp.Core/TrackApplication.cs
namespace SiebwaldeApp.Core
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Aggregate root: owns blocks and subsystem controllers.
    /// </summary>
    public class TrackApplication
    {
        private readonly ITrackIn _trackIn;
        private readonly ITrackOut _trackOut;
        private readonly IYardIn _yardIn;
        private readonly IYardOut _yardOut;

        private readonly Dictionary<int, TrackBlock> _blocks = new();

        public StationController Station { get; }

        public static readonly int[] TopTracks = { 10, 11, 12 };
        public static readonly int[] BottomTracks = { 1, 2, 3 };
        public static bool IsTop(int trackNumber) => TopTracks.Contains(trackNumber);

        public TrackApplication(ITrackIn trackIn, ITrackOut trackOut, IYardIn yardIn, IYardOut yardOut)
        {
            _trackIn = trackIn;
            _trackOut = trackOut;
            _yardIn = yardIn;
            _yardOut = yardOut;

            Station = new StationController(this, _trackIn, _trackOut, _yardIn, _yardOut);

            WireBuses();
        }

        private void WireBuses()
        {
            _trackIn.AmplifierOccupiedChanged += e =>
            {
                var b = GetBlock(e.TrackNumber);
                b?.Amplifier.SetFeedback(e.OccupiedOut);
            };
        }

        public void RegisterBlock(TrackBlock block) => _blocks[block.Id] = block;
        public TrackBlock GetBlock(int id) => _blocks.TryGetValue(id, out var b) ? b : null;
        public IEnumerable<TrackBlock> AllBlocks() => _blocks.Values;

        public void SetEntrySignal(bool isTopSide, bool green) => _trackOut.SetSignalEntry(isTopSide, green);
        public void SetExitSignal(bool isTopSide, bool green) => _trackOut.SetSignalExit(isTopSide, green);
    }
}
