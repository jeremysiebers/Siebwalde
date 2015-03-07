using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    public class FiddleYardSimTrainDetect
    {
        public iFiddleYardSimulator m_iFYSim;
        private int FiddleTrDtState;
        private int AliveUpdateCnt;
        

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardSimTrainDetect Init
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
         */
        /*#--------------------------------------------------------------------------#*/
        public FiddleYardSimTrainDetect(iFiddleYardSimulator iFYSim)
        {
            m_iFYSim = iFYSim;
            FiddleTrDtState = 0;
            AliveUpdateCnt = 0;
            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleTrDt
         *                Traindetection routine
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
         */
        /*#--------------------------------------------------------------------------#*/
        public bool FiddleTrDt()
        {
            bool _Return = false;

            switch (FiddleTrDtState)
            {
                case 0:
                    m_iFYSim.StoreText("FYTrDt.FiddleTrDt started");
                    if (m_iFYSim.GetTrackNo().Count < 7 && m_iFYSim.GetTrackNo().Count != 1)
                    {
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt  m_iFYSim.GetTrackNo().Count < 7");
                        FiddleTrDtState = 1;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 1");
                    }
                    else if (m_iFYSim.GetTrackNo().Count > 6 && m_iFYSim.GetTrackNo().Count != 11)
                    {
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt  m_iFYSim.GetTrackNo().Count > 6");
                        FiddleTrDtState = 2;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 2");
                    }
                    else if (m_iFYSim.GetTrackNo().Count == 1)
                    {
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt  m_iFYSim.GetTrackNo().Count == 1");
                        FiddleTrDtState = 3;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 3");
                        
                    }
                    else if (m_iFYSim.GetTrackNo().Count == 11)
                    {
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt  m_iFYSim.GetTrackNo().Count == 11");
                        FiddleTrDtState = 4;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 4");                        
                    }
                    break;

                case 1:
                    if (true == m_iFYSim.GetFYMove().FiddleMultipleMove("FiddleGo1"))
                    {
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt true == FYMove.FiddleMultipleMove(FiddleGo1)");
                        FiddleTrDtState = 0;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 0");
                    }
                    break;

                case 2:
                    if (true == m_iFYSim.GetFYMove().FiddleMultipleMove("FiddleGo11"))
                    {
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt true == FYMove.FiddleMultipleMove(FiddleGo11)");
                        FiddleTrDtState = 0;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 0");
                    }
                    break;

                case 3:
                    if (true == m_iFYSim.GetFYMove().FiddleMultipleMove("FiddleGo11"))
                    {
                        m_iFYSim.GetTrainDetectionFinished().Mssg = true;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt true == FYMove.FiddleMultipleMove(FiddleGo11)");
                        FiddleTrDtState = 5;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 5");
                    }
                    break;

                case 4:
                    if (true == m_iFYSim.GetFYMove().FiddleMultipleMove("FiddleGo1"))
                    {
                        m_iFYSim.GetTrainDetectionFinished().Mssg = true;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt true == FYMove.FiddleMultipleMove(FiddleGo1)");
                        FiddleTrDtState = 5;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 5");
                    }
                    break;

                case 5:
                    if (AliveUpdateCnt >= 0)
                    {
                        AliveUpdateCnt = 0;
                        FiddleTrDtState = 6;
                        m_iFYSim.StoreText("FYTrDt.FiddleTrDt FiddleTrDtState = 6");
                    }
                    else { AliveUpdateCnt++; }
                    break;
                    
                case 6:
                    m_iFYSim.StoreText("FYTrDt.FiddleTrDt m_iFYSim.UpdateSimArrayToAppArray()");
                    m_iFYSim.UpdateSimArrayToAppArray();
                    FiddleTrDtState = 0;
                    _Return = true;
                    m_iFYSim.StoreText("FYTrDt.FiddleTrDt _Return = true");
                    break;
                    
                default: break;
            }

            return _Return;
        }
    }
}
