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
        private TrackIOHandle trackIOHandle;
        private TrackApplicationVariables trackApplicationVariables;

        public TrackControlMain(Main main, TrackIOHandle trackIOHandle, TrackApplicationVariables trackApplicationVariables)
        {
            this.main = main;
            this.trackIOHandle = trackIOHandle;
            this.trackApplicationVariables = trackApplicationVariables;
        }
    }
}
