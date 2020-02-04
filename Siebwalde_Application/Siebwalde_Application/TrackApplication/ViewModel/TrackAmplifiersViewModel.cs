using System.Collections.ObjectModel;

namespace Siebwalde_Application
{
    public class TrackAmplifiersViewModel
    {
        public ObservableCollection<TrackAmplifier> trackAmplifiersList;

        private TrackApplicationVariables mTrackApplicationVariables;

        public TrackAmplifiersViewModel(TrackApplicationVariables mTrackApplicationVariables)
        {
            this.mTrackApplicationVariables = mTrackApplicationVariables;

            ObservableCollection<TrackAmplifier> trackAmplifiersList = new ObservableCollection<TrackAmplifier>(mTrackApplicationVariables.TrackAmplifiers);
        }
    }
}

