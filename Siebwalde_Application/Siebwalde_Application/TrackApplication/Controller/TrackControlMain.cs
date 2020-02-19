using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siebwalde_Application
{
    public class TrackControlMain
    {
        private Main main;
        private PublicEnums publicEnums;
        private TrackIOHandle trackIOHandle;
        private TrackApplicationVariables trackApplicationVariables;

        public TrackControlMain(Main main, PublicEnums publicEnums, TrackIOHandle trackIOHandle, TrackApplicationVariables trackApplicationVariables)
        {
            this.main = main;
            this.publicEnums = publicEnums;
            this.trackIOHandle = trackIOHandle;
            this.trackApplicationVariables = trackApplicationVariables;
        }
    }
}
