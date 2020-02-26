using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siebwalde_Application
{
    public class TrackAmplifierInitalizationSequencer
    {
        private TrackApplicationVariables mTrackApplicationVariables;

        public TrackAmplifierInitalizationSequencer(Log2LoggingFile trackApplicationLogging, TrackApplicationVariables trackApplicationVariables)
        {
            mTrackApplicationVariables = trackApplicationVariables;
        }
    }
}
