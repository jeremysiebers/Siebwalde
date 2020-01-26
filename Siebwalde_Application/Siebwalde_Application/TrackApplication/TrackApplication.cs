using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application.TrackApplication
{
    public interface iTrackApplication
    {
        
    }

    public class TrackApplication : iTrackApplication
    {
        private TrackIOHandleVariables m_MTIOHandleVar;
        private iTrackIOHandle m_iMTIOH;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackApplication constructor
         * 
         *  Input(s)   :
         *
         *  Output(s)  : 
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : 
         *  
         */
        /*#--------------------------------------------------------------------------#*/
        public TrackApplication(TrackIOHandleVariables MTIOHandleVar, iTrackIOHandle iMTIOH)
        {
            m_MTIOHandleVar = MTIOHandleVar;
            m_iMTIOH = iMTIOH;
        }
    }
}
