using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Siebwalde_Application
{
    /// <summary>
    /// A view model for each TrackAmplifier item
    /// </summary>
    public class TrackAmplifierItemViewModel : BaseViewModel
    {
        private TrackIOHandle trackIOHandle;

        public TrackAmplifierItemViewModel(TrackIOHandle trackIOHandle)
        {
            this.trackIOHandle = trackIOHandle;
        }

        /// <summary>
        /// The slave number
        /// </summary>
        public ushort SlaveNumber { get; set; }

        /// <summary>
        /// The recieved mod bus messages counted by the master
        /// </summary>
        public ushort MbReceiveCounter { get; set; }

        public ObservableCollection<TrackAmplifierItemViewModel> Children { get; set; }


    }
}
