using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    public class FiddleYardSimMove
    {
        public iFiddleYardSimulator m_iFYSim;
        private ILogger m_FYSimLog;
        private FiddleYardSimulatorVariables m_FYSimVar;
        private int FiddleOneMoveState;
        private int AliveUpdateCnt;
        private int GetTrackNoCnt;
        private int GetNewTrackNo;
        private string MoveDirection;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardOneMove Constructor
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
        public FiddleYardSimMove(iFiddleYardSimulator iFYSim, ILogger FiddleYardSimulatorLogging, FiddleYardSimulatorVariables FYSimVar)
        {
            m_iFYSim = iFYSim;
            m_FYSimLog = FiddleYardSimulatorLogging;
            m_FYSimVar = FYSimVar;
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
                    if (m_FYSimVar.F12.Value == true)
                    {
                        _Return = true;
                        break; //<------------------------------------------------------------------------error text to IOHANDLE to FORM as message here about F12!!!also into traindetect!!!
                    }
                    else if (m_FYSimVar.F13.Value == true)
                    {
                        _Return = true;
                        break;
                    }
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleOneMove(" + direction + ") started");
                    GetTrackNoCnt = m_FYSimVar.TrackNo.Count;
                    m_FYSimVar.TrackPower.Value = false;
                    m_FYSimVar.Resistor.Value = true;
                    m_FYSimVar.M10.Value = true;
                    m_FYSimVar.CL10Heart.Value = false;
                    m_FYSimVar.F10.Value = false;
                    m_FYSimVar.F11.Value = false;
                    m_FYSimVar.F12.Value = false;
                    m_FYSimVar.F13.Value = false;
                    m_FYSimVar.TrackNo.Count = 0;
                    AliveUpdateCnt = 0;
                    FiddleOneMoveState = 2;                                                                                                         // pass wait state
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleOneMove FiddleOneMoveState = 1");
                    break;

                case 1: 
                    if (AliveUpdateCnt >= 2)
                    {
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 2;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleOneMove FiddleOneMoveState = 2");
                    }
                    else { AliveUpdateCnt++; }
                    break;

                case 2:
                    m_FYSimVar.CL10Heart.Value = true;
                    if (direction == "Left")
                    {
                        m_FYSimVar.TrackNo.Count = GetTrackNoCnt + 1;
                        m_FYSimVar.FiddleOneLeftFinished.Mssg = true;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleOneMove One left finished");
                    }
                    else if (direction == "Right")
                    {
                        m_FYSimVar.TrackNo.Count = GetTrackNoCnt - 1;
                        m_FYSimVar.FiddleOneRightFinished.Mssg = true;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleOneMove One right finished");
                    }
                    m_FYSimVar.TrackPower.Value = true;
                    m_FYSimVar.Resistor.Value = false;
                    m_FYSimVar.M10.Value = false;
                    AliveUpdateCnt = 0;
                    FiddleOneMoveState = 0;
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleOneMove FiddleOneMoveState = 0");
                    _Return = true;
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleOneMove _Return = true");
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
                    if (m_FYSimVar.F12.Value == true)
                    {
                        _Return = true;
                        break; //<------------------------------------------------------------------------error text to IOHANDLE to FORM as message here about F12!!!also into traindetect!!!
                    }
                    else if (m_FYSimVar.F13.Value == true)
                    {
                        _Return = true;
                        break;
                    }

                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove(" + direction + ") started");
                    if (direction == "TargetAlive")
                    {
                        _Return = true;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove direction == TargetAlive --> _Return = true");
                        break;
                    }
                    try
                    {
                        GetNewTrackNo = Convert.ToInt16(direction.Substring(direction.IndexOf(@"o") + 1));
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove GetNewTrackNo = " + direction.Substring(direction.IndexOf(@"o") + 1));
                    }
                    catch
                    {
                        _Return = true;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove Error converting to int from string!!! --> _Return = true");
                        break;
                    }
                    GetTrackNoCnt = m_FYSimVar.TrackNo.Count;
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
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove MoveDirection = " + MoveDirection);
                    m_FYSimVar.TrackPower.Value = false;
                    m_FYSimVar.Resistor.Value = true;
                    m_FYSimVar.M10.Value = true;
                    m_FYSimVar.CL10Heart.Value = false;
                    m_FYSimVar.TrackNo.Count = 0;
                    AliveUpdateCnt = 0;
                    FiddleOneMoveState = 2;                                                                                                        
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove FiddleOneMoveState = 1");
                    break;

                case 1:
                    if (AliveUpdateCnt >= 2)
                    {
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 2;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove FiddleOneMoveState = 2");
                    }
                    else { AliveUpdateCnt++; }
                    break;

                case 2:
                    m_FYSimVar.CL10Heart.Value = true;
                    if (MoveDirection == "Left")
                    {
                        GetTrackNoCnt += 1;
                        if (GetTrackNoCnt > 11)
                        {
                            GetTrackNoCnt = 0;
                        }
                        m_FYSimVar.TrackNo.Count = GetTrackNoCnt;
                    }
                    else if (MoveDirection == "Right")
                    {
                        GetTrackNoCnt -= 1;                        
                        if (GetTrackNoCnt < 1)
                        {
                            GetTrackNoCnt = 12;
                        }
                        m_FYSimVar.TrackNo.Count = GetTrackNoCnt;
                    }
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove GetTrackNoCnt = " + Convert.ToString(GetTrackNoCnt));

                    // Update info about track occupiance on FY to the variables which are sent to the Application train detection program
                    bool TrackOccupied = false;
                    switch (GetTrackNoCnt)
                    {
                        case 1: TrackOccupied = m_FYSimVar.Track1.Value; break;
                        case 2: TrackOccupied = m_FYSimVar.Track2.Value; break;
                        case 3: TrackOccupied = m_FYSimVar.Track3.Value; break;
                        case 4: TrackOccupied = m_FYSimVar.Track4.Value; break;
                        case 5: TrackOccupied = m_FYSimVar.Track5.Value; break;
                        case 6: TrackOccupied = m_FYSimVar.Track6.Value; break;
                        case 7: TrackOccupied = m_FYSimVar.Track7.Value; break;
                        case 8: TrackOccupied = m_FYSimVar.Track8.Value; break;
                        case 9: TrackOccupied = m_FYSimVar.Track9.Value; break;
                        case 10: TrackOccupied = m_FYSimVar.Track10.Value; break;
                        case 11: TrackOccupied = m_FYSimVar.Track11.Value; break;
                        default: TrackOccupied = false; break;
                    }
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove TrackOccupied = " + Convert.ToString(TrackOccupied));
                    if (TrackOccupied)
                    {
                        m_FYSimVar.F10.Value = true;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove m_FYSimVar.F10.Value = true");
                    }
                    else { m_FYSimVar.F10.Value = false; m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove m_FYSimVar.F10.Value = false"); }
                    

                    FiddleOneMoveState = 3;                                                                                                        
                    m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove FiddleOneMoveState = 3");
                    break;

                case 3:
                    if (AliveUpdateCnt >= 1)
                    {
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 4;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove FiddleOneMoveState = 4");
                    }
                    else { AliveUpdateCnt++; }
                    break;
                    
                case 4:
                    if (GetTrackNoCnt == GetNewTrackNo)
                    {
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove GetTrackNoCnt == GetNewTrackNo");
                        if (MoveDirection == "Left")
                        {
                            m_FYSimVar.FiddleMultipleLeftFinished.Mssg = true;
                        }
                        else if (MoveDirection == "Right")
                        {
                            m_FYSimVar.FiddleMultipleRightFinished.Mssg = true;
                        }
                        m_FYSimVar.TrackPower.Value = true;
                        m_FYSimVar.Resistor.Value = false;
                        m_FYSimVar.M10.Value = false;
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 0;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove FiddleOneMoveState = 0");
                        _Return = true;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove _Return = true");
                    }
                    else
                    {
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove GetTrackNoCnt != GetNewTrackNo");
                        m_FYSimVar.CL10Heart.Value = false;
                        m_FYSimVar.F10.Value = false;
                        m_FYSimVar.F11.Value = false;
                        m_FYSimVar.F12.Value = false;
                        m_FYSimVar.F13.Value = false;
                        m_FYSimVar.TrackNo.Count = 0;
                        AliveUpdateCnt = 0;
                        FiddleOneMoveState = 2;                                                                                                         
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove FiddleOneMoveState = 1");
                        _Return = false;
                        m_FYSimLog.Log(GetType().Name, "FYSimMove.FiddleMultipleMove _Return = false");
                    }
                    break;

                default: FiddleOneMoveState = 0;
                    break;
            }
            return _Return;
        }
    }
}