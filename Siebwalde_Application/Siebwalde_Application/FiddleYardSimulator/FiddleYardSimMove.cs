using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    public class FiddleYardSimMove
    {
        public iFiddleYardSimulator m_iFYSim;
        private int FiddleOneMoveState;
        private int AliveUpdateCnt;
        private int GetTrackNoCnt;
        private int GetNewTrackNo;
        private string MoveDirection;

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
        public FiddleYardSimMove(iFiddleYardSimulator iFYSim)
        {
            m_iFYSim = iFYSim;
            FiddleOneMoveState = 0;
            AliveUpdateCnt = 0;
            GetTrackNoCnt = 0;
            GetNewTrackNo = 0;
            MoveDirection = null;
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
                    if (m_iFYSim.GetFYSim().F12.Value == true)
                    {
                        _Return = true;
                        break; //<------------------------------------------------------------------------error text to IOHANDLE to FORM as message here about F12!!!also into traindetect!!!
                    }
                    else if (m_iFYSim.GetFYSim().F13.Value == true)
                    {
                        _Return = true;
                        break;
                    }
                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleOneMove(" + direction + ") started");
                    GetTrackNoCnt = m_iFYSim.GetFYSim().TrackNo.Count;
                    m_iFYSim.GetFYSim().TrackPower.Value = false;
                    m_iFYSim.GetFYSim().Resistor.Value = true;
                    m_iFYSim.GetFYSim().M10.Value = true;
                    m_iFYSim.GetFYSim().CL10Heart.Value = false;
                    m_iFYSim.GetFYSim().TrackNo.Count = 0;
                    AliveUpdateCnt = 0;
                    FiddleOneMoveState = 1;
                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleOneMove FiddleOneMoveState = 1");
                    break;

                case 1: 
                    if (AliveUpdateCnt >= 7)
                    {
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 2;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleOneMove FiddleOneMoveState = 2");
                    }
                    else { AliveUpdateCnt++; }
                    break;

                case 2:
                    m_iFYSim.GetFYSim().CL10Heart.Value = true;
                    if (direction == "Left")
                    {
                        m_iFYSim.GetFYSim().TrackNo.Count = GetTrackNoCnt + 1;
                        m_iFYSim.GetFYSim().FiddleOneLeftFinished.Mssg = true;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleOneMove One left finished");
                    }
                    else if (direction == "Right")
                    {
                        m_iFYSim.GetFYSim().TrackNo.Count = GetTrackNoCnt - 1;
                        m_iFYSim.GetFYSim().FiddleOneRightFinished.Mssg = true;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleOneMove One right finished");
                    }
                    m_iFYSim.GetFYSim().TrackPower.Value = true;
                    m_iFYSim.GetFYSim().Resistor.Value = false;
                    m_iFYSim.GetFYSim().M10.Value = false;
                    AliveUpdateCnt = 0;
                    FiddleOneMoveState = 0;
                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleOneMove FiddleOneMoveState = 0");
                    _Return = true;
                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleOneMove _Return = true");
                    break;

                default: FiddleOneMoveState = 0;
                    break;
            }
            return _Return;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleMultipleMove
         * 
         *  Input(s)   : Shift FY multiple tracks to the left (+) or right (-)
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
        public bool FiddleMultipleMove(string direction)
        {
            bool _Return = false;

            switch (FiddleOneMoveState)
            {
                case 0:
                    if (m_iFYSim.GetFYSim().F12.Value == true)
                    {
                        _Return = true;
                        break; //<------------------------------------------------------------------------error text to IOHANDLE to FORM as message here about F12!!!also into traindetect!!!
                    }
                    else if (m_iFYSim.GetFYSim().F13.Value == true)
                    {
                        _Return = true;
                        break;
                    }

                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove(" + direction + ") started");
                    if (direction == "TargetAlive")
                    {
                        _Return = true;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove direction == TargetAlive --> _Return = true");
                        break;
                    }
                    try
                    {
                        GetNewTrackNo = Convert.ToInt16(direction.Substring(direction.IndexOf(@"o") + 1));
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove GetNewTrackNo = " + direction.Substring(direction.IndexOf(@"o") + 1));
                    }
                    catch
                    {
                        _Return = true;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove Error converting to int from string!!! --> _Return = true");
                        break;
                    }
                    GetTrackNoCnt = m_iFYSim.GetFYSim().TrackNo.Count;
                    if (GetNewTrackNo > GetTrackNoCnt)
                    {
                        MoveDirection = "Left";                        
                    }
                    else if (GetNewTrackNo < GetTrackNoCnt)
                    {
                        MoveDirection = "Right";
                    }
                    else if (GetNewTrackNo == GetTrackNoCnt)
                    {
                        MoveDirection = null;
                        _Return = true;
                        break;
                    }
                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove MoveDirection = " + MoveDirection);
                    m_iFYSim.GetFYSim().TrackPower.Value = false;
                    m_iFYSim.GetFYSim().Resistor.Value = true;
                    m_iFYSim.GetFYSim().M10.Value = true;
                    m_iFYSim.GetFYSim().CL10Heart.Value = false;
                    m_iFYSim.GetFYSim().TrackNo.Count = 0;
                    AliveUpdateCnt = 0;
                    FiddleOneMoveState = 1;
                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove FiddleOneMoveState = 1");
                    break;

                case 1:
                    if (AliveUpdateCnt >= 2)
                    {
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 2;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove FiddleOneMoveState = 2");
                    }
                    else { AliveUpdateCnt++; }
                    break;

                case 2:
                    m_iFYSim.GetFYSim().CL10Heart.Value = true;
                    if (MoveDirection == "Left")
                    {
                        GetTrackNoCnt += 1;
                        m_iFYSim.GetFYSim().TrackNo.Count = GetTrackNoCnt;
                    }
                    else if (MoveDirection == "Right")
                    {
                        GetTrackNoCnt -= 1;
                        m_iFYSim.GetFYSim().TrackNo.Count = GetTrackNoCnt;
                    }
                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove GetTrackNoCnt = " + Convert.ToString(GetTrackNoCnt));
                    FiddleOneMoveState = 4;
                    m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove FiddleOneMoveState = 3");
                    break;

                case 3:
                    if (AliveUpdateCnt >= 1)
                    {
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 4;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove FiddleOneMoveState = 4");
                    }
                    else { AliveUpdateCnt++; }
                    break;
                    
                case 4:
                    if (GetTrackNoCnt == GetNewTrackNo)
                    {
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove GetTrackNoCnt == GetNewTrackNo");
                        if (MoveDirection == "Left")
                        {
                            m_iFYSim.GetFYSim().FiddleMultipleLeftFinished.Mssg = true;
                        }
                        else if (MoveDirection == "Right")
                        {
                            m_iFYSim.GetFYSim().FiddleMultipleRightFinished.Mssg = true;
                        }
                        m_iFYSim.GetFYSim().TrackPower.Value = true;
                        m_iFYSim.GetFYSim().Resistor.Value = false;
                        m_iFYSim.GetFYSim().M10.Value = false;
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 0;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove FiddleOneMoveState = 0");
                        _Return = true;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove _Return = true");
                    }
                    else
                    {
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove GetTrackNoCnt != GetNewTrackNo");
                        m_iFYSim.GetFYSim().CL10Heart.Value = false;
                        m_iFYSim.GetFYSim().TrackNo.Count = 0;
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 1;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove FiddleOneMoveState = 1");
                        _Return = false;
                        m_iFYSim.GetFYSim().FiddleYardSimulatorLogging.StoreText("FYMove.FiddleMultipleMove _Return = false");
                    }
                    break;

                default: FiddleOneMoveState = 0;
                    break;
            }
            return _Return;
        }
    }
}