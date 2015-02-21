using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    class FiddleYardSimOneMove
    {
        public iFiddleYardSimulator m_iFYSim;
        private int FiddleOneMoveState;
        private int AliveUpdateCnt;
        private int _GetTrackNoCnt;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardOneMove Init
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
        public FiddleYardSimOneMove(iFiddleYardSimulator iFYSim)
        {
            m_iFYSim = iFYSim;
            FiddleOneMoveState = 0;
            AliveUpdateCnt = 0;
            _GetTrackNoCnt = 0;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleOneMove
         * 
         *  Input(s)   : Shift FY one track to the left (+) or right (-)
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
        public bool FiddleOneMove(string direction)
        {
            bool _Return = false;

            switch (FiddleOneMoveState)
            {                
                case 0:
                    _GetTrackNoCnt = m_iFYSim.GetTrackNo().Count;
                    m_iFYSim.GetTrackNo().Count = 0;
                    m_iFYSim.GetCL10Heart().Value = false;
                    m_iFYSim.GetM10().Value = true;
                    m_iFYSim.GetTrackPower().Value = false;
                    m_iFYSim.GetResistor().Value = true;
                    AliveUpdateCnt = 0;
                    FiddleOneMoveState = 1;
                    break;

                case 1: 
                    if (AliveUpdateCnt >= 15)
                    {
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 2;
                    }
                    else { AliveUpdateCnt++; }
                    break;

                case 2:
                    if (direction == "Left")
                    {
                        m_iFYSim.GetTrackNo().Count = _GetTrackNoCnt + 1;
                        m_iFYSim.GetFiddleOneLeftFinished().Mssg = true;
                    }
                    else if (direction == "Right")
                    {
                        m_iFYSim.GetTrackNo().Count = _GetTrackNoCnt - 1;
                        m_iFYSim.GetFiddleOneRightFinished().Mssg = true;
                    }
                    m_iFYSim.GetCL10Heart().Value = true;
                    m_iFYSim.GetM10().Value = false;
                    m_iFYSim.GetTrackPower().Value = true;
                    m_iFYSim.GetResistor().Value = false;                    
                    AliveUpdateCnt = 0;
                    FiddleOneMoveState = 0;
                    _Return = true;
                    break;

                default: FiddleOneMoveState = 0;
                    break;
            }
            return _Return;
        }
    }
}
