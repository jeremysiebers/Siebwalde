using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siebwalde_Application
{
    /// <summary>
    /// This TrackAmplifierInitalizationSequencer initializes all the Track amplifiers and checks if firmware download is required
    /// </summary>
    public class TrackAmplifierInitalizationSequencer
    {
        #region Local variables

        private TrackApplicationVariables mTrackApplicationVariables;
        private Log2LoggingFile mTrackApplicationLogging;

        #endregion

        #region Constructor

        public TrackAmplifierInitalizationSequencer(Log2LoggingFile trackApplicationLogging, TrackApplicationVariables trackApplicationVariables)
        {
            mTrackApplicationVariables = trackApplicationVariables;
            mTrackApplicationLogging = trackApplicationLogging;
        }

        #endregion

        #region Internal methods

        internal string InitSequence()
        {
            return "Finished";
        }

        #endregion
    }
}
