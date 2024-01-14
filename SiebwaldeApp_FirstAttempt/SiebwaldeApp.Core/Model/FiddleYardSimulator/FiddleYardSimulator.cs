using System;
using System.Collections.Generic;
using System.Timers;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Core
{
    public interface iFiddleYardSimulator
    {        
        void UpdateSimArrayToAppArray();
        int[] GetTrainsOnFYSim();
    }

    public class FiddleYardSimulator : iFiddleYardSimulator
    {
        private FiddleYardSimulatorVariables FYSimVar;
        public FiddleYardIOHandleVariables m_FYIOHandleVar;             // connect variable to connect to FYIOH class for defined variables
        public Action<byte[]> NewData;
        private string m_instance = null;
        public FiddleYardSimMove FYSimMove;
        public FiddleYardSimTrainDetect FYSimTrDt;
        private ILogger FiddleYardSimulatorLogging;

        private string LoggerInstance { get; set; }
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

        private enum State { Idle, Reset, MIP50xAbs_Pos, MIP50xHomexAxis, MIP50xSetxPositioningxVelxDefault, MIP50xSetxAcceleration, MIP50xClearxError, MIP50xActivatexPosxReg, MIP50xDeactivatexPosxReg,
                                MIP50xReadxPosition, FiddleMultipleMove
        };
        private State State_Machine;

        private const int NoOfSimTrains = 12; // counting from 1!!!
        public int[] TrainsOnFYSim = new int[NoOfSimTrains + 1]; // counting from 1!!!
        private Random rng = new Random();

        
        private const int GO = 0x47;
        private const string MIP50xM20xHeader = "M#20    1   ";
        private const string MIP50xM21xHeader = "M#21    1      ";
        

        public int[] GetTrainsOnFYSim()
        {
            return TrainsOnFYSim;
        }

        private string fmt = "00";

        // Create a timer
        System.Timers.Timer aTimer = new System.Timers.Timer();

        List<FiddleYardSimTrain> FYSimTrains = new List<FiddleYardSimTrain>();
        FiddleYardSimTrain current = null;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardSimulator constructor
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

        public FiddleYardSimulator(string Instance, FiddleYardIOHandleVariables FYIOHandleVar)
        {
            m_FYIOHandleVar = FYIOHandleVar;
            m_instance = Instance;

            if ("TOP" == m_instance)
            {
                // Set the log instance string to the logging instance name used for directed file logging
                LoggerInstance = "FySimulatorTopLog";
                //  different logging file per target, this is default
                FiddleYardSimulatorLogging = GetLogger(Properties.Settings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "FiddleYardSimulatorLogTOP.txt", LoggerInstance);
                IoC.Logger.AddLogger(FiddleYardSimulatorLogging);
            }
            else if ("BOT" == m_instance)
            {
                // Set the log instance string to the logging instance name used for directed file logging
                LoggerInstance = "FySimulatorBotLog";
                //  different logging file per target, this is default
                FiddleYardSimulatorLogging = GetLogger(Properties.Settings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "FiddleYardSimulatorLogBOT.txt", LoggerInstance);
                IoC.Logger.AddLogger(FiddleYardSimulatorLogging);
            }

            FYSimVar = new FiddleYardSimulatorVariables();            
            FYSimMove = new FiddleYardSimMove(this, FiddleYardSimulatorLogging, FYSimVar);
            FYSimTrDt = new FiddleYardSimTrainDetect(this, FiddleYardSimulatorLogging, FYSimVar, FYSimMove);
            
            FYSimVar.TrackNo.Count = 1;            

            Sensor Sns_FYSimSpeedSetting = new Sensor("FYSimSpeedSetting", " FYSimSpeedSetting ", 0, (name, val, log) => SimulatorSettings(name, val, log)); // initialize and subscribe sensors
            Properties.Settings.Default.FYSimSpeedSetting.Attach(Sns_FYSimSpeedSetting);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Start: When simulator is required, start the alive kick
         *               and create the simulator variables
         *                  
         * 
         *  Input(s)   : 
         *
         *  Output(s)  : timer timed event
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
        public void Start()
        {
            IoC.Logger.Log("### Fiddle Yard Simulator started ###", LoggerInstance);


            // Create simtrains here, when the simulator is not started the simulated trains are not created
            for (int i = 1; i <= NoOfSimTrains; i++)
            {
                TrainsOnFYSim[i] = rng.Next(0, 2);
                current = new FiddleYardSimTrain(m_instance, this, m_FYIOHandleVar, FYSimVar);
                current.FYSimtrainInstance = current.ClassName + i.ToString(fmt);
                if (TrainsOnFYSim[i] == 1)
                {
                    current.SimTrainLocation = TrackNoToTrackString(i); // on FY OR Block 5B/7/8A OR buffer
                }
                else
                {
                    current.SimTrainLocation = TrackNoToTrackString(0); // Always in buffer in this case
                }
                FYSimTrains.Add(current);
            }
            IoC.Logger.Log("FYSim created "+ Convert.ToString(NoOfSimTrains) +" FYSimTrains.", LoggerInstance);

            FYSimVar.Reset();
            IoC.Logger.Log("FYSim Simulator Reset()", LoggerInstance);
            UpdateSimArrayToAppArray(); // Update track variables if a train is present or not  
            IoC.Logger.Log("FYSim Simulator UpdateSimArrayToAppArray()", LoggerInstance);

            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            // Set the Interval to [x] miliseconds.
            aTimer.Interval = Convert.ToInt16(Properties.Settings.Default.FIDDLExYARDxSIMxSPEEDxSETTING);
            aTimer.AutoReset = true;
            // Enable the timer
            aTimer.Enabled = true;

            IoC.Logger.Log("FYSim Simulator Timer started: aTimer.Interval = " + Convert.ToString(aTimer.Interval), LoggerInstance);
            IoC.Logger.Log("FYSim State_Machine = State.Idle from Start()", LoggerInstance);            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Stop: When simulator is not required, stop the alive kick
         *               
         *                  
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
        public void Stop()
        {
            aTimer.Enabled = false;
            IoC.Logger.Log("FYSimulator STOP() command received, stopping timer event", LoggerInstance);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SimulatorUpdate, simulator application
         *               This is the main Fiddle Yard simulator, simulating movements,
         *               controlling the contents of the tracks etc.
         *  Input(s)   : Sensors, actuators, messages and commands and alive ping
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
        public void SimulatorUpdate(string kicksimulator, int val)
        {
            switch (State_Machine)
            {
                /*---------------------------------------CHECK FOR Type of command -----------------------------------------------------------------------------------------------------------------
                */
                case State.Idle:                    
                    if (kicksimulator == "MIP50xAbs_Pos" && val == GO)
                    {
                        IoC.Logger.Log("FYSim State_Machine = State.MIP50xAbs_Pos", LoggerInstance);
                        State_Machine = State.MIP50xAbs_Pos;                        
                    }
                    else if (kicksimulator == "MIP50xHomexAxis" && val == GO)
                    {
                        IoC.Logger.Log("FYSim State_Machine = State.MIP50xHomexAxis", LoggerInstance);
                        State_Machine = State.MIP50xHomexAxis;
                    }
                    else if (kicksimulator == "MIP50xSetxPositioningxVelxDefault" && val == GO)
                    {
                        IoC.Logger.Log("FYSim State_Machine = State.MIP50xSetxPositioningxVelxDefault", LoggerInstance);
                        State_Machine = State.MIP50xSetxPositioningxVelxDefault;
                    }
                    else if (kicksimulator == "MIP50xSetxAcceleration" && val == GO)
                    {
                        IoC.Logger.Log("FYSim State_Machine = State.MIP50xSetxAcceleration", LoggerInstance);
                        State_Machine = State.MIP50xSetxAcceleration;
                    }
                    else if (kicksimulator == "MIP50xClearxError" && val == GO)
                    {
                        IoC.Logger.Log("FYSim State_Machine = State.MIP50xClearxError", LoggerInstance);
                        State_Machine = State.MIP50xClearxError;
                    }
                    else if (kicksimulator == "MIP50xActivatexPosxReg" && val == GO)
                    {
                        IoC.Logger.Log("FYSim State_Machine = State.MIP50xActivatexPosxReg", LoggerInstance);
                        State_Machine = State.MIP50xActivatexPosxReg;
                    }
                    else if (kicksimulator == "MIP50xDeactivatexPosxReg" && val == GO)
                    {
                        IoC.Logger.Log("FYSim State_Machine = State.MIP50xDeactivatexPosxReg", LoggerInstance);
                        State_Machine = State.MIP50xDeactivatexPosxReg;
                    }
                    else if (kicksimulator == "MIP50xReadxPosition" && val == GO)
                    {
                        IoC.Logger.Log("FYSim State_Machine = State.MIP50xReadxPosition", LoggerInstance);
                        State_Machine = State.MIP50xReadxPosition;
                    }
                    else if (kicksimulator == "Reset")
                    {
                        IoC.Logger.Log("FYSim kicksimulator == Reset", LoggerInstance);
                        State_Machine = State.Reset;
                        IoC.Logger.Log("FYSim State_Machine = State.Reset", LoggerInstance);
                    }
                    else if (kicksimulator != "TargetAlive")
                    {
                        FYSimVar.IdleSetVariable(kicksimulator); // Only when a variable needs to be set it is done directly (manualy sending commands to target/simulator from FORM
                        
                    }
                    else if (kicksimulator == "TargetAlive")
                    {
                        FYSimVar.TargetAlive.UpdateSensorValue(1, true);  // Update all clients of TargetAlive (SimTrains)
                    }
                    break;

                /*---------------------------------------Execute Type of command -----------------------------------------------------------------------------------------------------------------
                */

                case State.MIP50xAbs_Pos:
                    if (kicksimulator == "MIP50xDataxToxSend")
                    {                        
                        FYSimVar.MIP50xReceivedxCmdxStringxTemp = FYSimVar.MIP50xReceivedxCmdxStringxTemp.Split('x')[1];
                        
                        UInt32 Number = Convert.ToUInt32(FYSimVar.MIP50xReceivedxCmdxStringxTemp);                      // To UInt32 because otherwise the number is recognized as large positive number
                        FYSimVar.MIP50xReceivedxCmdxArrayxCounterxTemp = 0;
                        Number = Convert.ToUInt32(ConvertAbsoluteNo2TrackNo(Number));
                        FYSimMove.FiddleMultipleMove("Go" + Convert.ToString(Number));
                        IoC.Logger.Log("FYSim State_Machine = State.FiddleMultipleMove", LoggerInstance);
                        State_Machine = State.FiddleMultipleMove;
                    }
                    break;

                case State.FiddleMultipleMove:
                    if (kicksimulator == "MIP50xDataxToxSend") // also here only 1 thread from timer event
                    {
                        if (true == FYSimMove.FiddleMultipleMove(kicksimulator))
                        {
                            IoC.Logger.Log("FYSim true == FYMove.FiddleMultipleMove(kicksimulator)", LoggerInstance);
                            State_Machine = State.MIP50xSetxPositioningxVelxDefault;
                            IoC.Logger.Log("FYSim State_Machine = State.MIP50xSetxPositioningxVelxDefault from State.FiddleMultipleMove", LoggerInstance); // to sen X0 after move
                            FYSimVar.uControllerReady.Mssg = true;
                        }
                    }
                    break;

                case State.MIP50xHomexAxis:
                    if (kicksimulator == "MIP50xDataxToxSend")
                    {
                        switch (FYSimVar.MIP50Cnt)
                        {
                            case 0:
                                FYSimVar.MIP50DataReturn = 0xD;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 1:
                                FYSimVar.MIP50DataReturn = 0xA;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 2:
                                FYSimVar.MIP50DataReturn = 0x58;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 3:
                                FYSimVar.MIP50DataReturn = 0x30;
                                FYSimVar.MIP50Cnt = 0;
                                FYSimVar.TrackNo.Count = 1;   // After homing Track 1 is active
                                if (FYSimVar.Track1.Value == true) // Check if track 1 is occupied, prep for traindetection etc.
                                {
                                    FYSimVar.F10.Value = true;
                                }
                                IoC.Logger.Log("FYSim State_Machine = State.Idle", LoggerInstance);
                                State_Machine = State.Idle;
                                break;
                        }
                    }
                    break;

                case State.MIP50xSetxPositioningxVelxDefault:
                    if (kicksimulator == "MIP50xDataxToxSend")
                    {
                        switch (FYSimVar.MIP50Cnt)
                        {
                            case 0:
                                FYSimVar.MIP50DataReturn = 0xD;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 1:
                                FYSimVar.MIP50DataReturn = 0xA;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 2:
                                FYSimVar.MIP50DataReturn = 0x58;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 3:
                                FYSimVar.MIP50DataReturn = 0x30;
                                FYSimVar.MIP50Cnt = 0;
                                IoC.Logger.Log("FYSim State_Machine = State.Idle", LoggerInstance);
                                State_Machine = State.Idle;
                                break;
                        }
                    }
                    break;

                case State.MIP50xSetxAcceleration:
                    if (kicksimulator == "MIP50xDataxToxSend")
                    {
                        switch (FYSimVar.MIP50Cnt)
                        {
                            case 0:
                                FYSimVar.MIP50DataReturn = 0xD;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 1:
                                FYSimVar.MIP50DataReturn = 0xA;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 2:
                                FYSimVar.MIP50DataReturn = 0x58;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 3:
                                FYSimVar.MIP50DataReturn = 0x30;
                                FYSimVar.MIP50Cnt = 0;
                                IoC.Logger.Log("FYSim State_Machine = State.Idle", LoggerInstance);
                                State_Machine = State.Idle;
                                break;
                        }
                    }
                    break;


                case State.MIP50xClearxError:
                    if (kicksimulator == "MIP50xDataxToxSend")
                    {
                        switch (FYSimVar.MIP50Cnt)
                        {
                            case 0:
                                FYSimVar.MIP50DataReturn = 0xD;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 1:
                                FYSimVar.MIP50DataReturn = 0xA;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 2:
                                FYSimVar.MIP50DataReturn = 0x58;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 3:
                                FYSimVar.MIP50DataReturn = 0x30;
                                FYSimVar.MIP50Cnt = 0;
                                IoC.Logger.Log("FYSim State_Machine = State.Idle", LoggerInstance);
                                State_Machine = State.Idle;
                                break;
                        }
                    }
                    break;

                case State.MIP50xActivatexPosxReg:
                    if (kicksimulator == "MIP50xDataxToxSend")
                    {
                        switch (FYSimVar.MIP50Cnt)
                        {
                            case 0:
                                FYSimVar.MIP50DataReturn = 0xD;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 1:
                                FYSimVar.MIP50DataReturn = 0xA;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 2:
                                FYSimVar.MIP50DataReturn = 0x58;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 3:
                                FYSimVar.MIP50DataReturn = 0x30;
                                FYSimVar.MIP50Cnt = 0;
                                IoC.Logger.Log("FYSim State_Machine = State.Idle", LoggerInstance);
                                State_Machine = State.Idle;
                                break;
                        }
                    }
                    break;

                case State.MIP50xDeactivatexPosxReg:
                    if (kicksimulator == "MIP50xDataxToxSend")
                    {
                        switch (FYSimVar.MIP50Cnt)
                        {
                            case 0:
                                FYSimVar.MIP50DataReturn = 0xD;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 1:
                                FYSimVar.MIP50DataReturn = 0xA;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 2:
                                FYSimVar.MIP50DataReturn = 0x58;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 3:
                                FYSimVar.MIP50DataReturn = 0x30;
                                FYSimVar.MIP50Cnt = 0;
                                IoC.Logger.Log("FYSim State_Machine = State.Idle", LoggerInstance);
                                State_Machine = State.Idle;
                                break;                                
                        }
                    }
                    break;

                case State.MIP50xReadxPosition:
                    if (kicksimulator == "MIP50xDataxToxSend")
                    {
                        switch (FYSimVar.MIP50Cnt)
                        {
                            case 0:
                                FYSimVar.MIP50DataReturn = 0xD;
                                FYSimVar.MIP50Cnt++;
                                break;

                            case 1:
                                FYSimVar.MIP50DataReturn = 0xA;
                                FYSimVar.MIP50Cnt++;
                                break;

                            case 2:
                                if (FYSimVar.MIP50Cnt2 != MIP50xM21xHeader.Length)      // Send message header
                                {
                                    FYSimVar.MIP50DataReturn = Convert.ToInt16(MIP50xM21xHeader[FYSimVar.MIP50Cnt2]);
                                    FYSimVar.MIP50Cnt2++;
                                }
                                else { FYSimVar.MIP50Cnt2 = 0; FYSimVar.MIP50Cnt++; }
                                break;

                            case 3:
                                if (FYSimVar.MIP50Cnt2 != ConvertTrackNo2AbsoluteNo(FYSimVar.TrackNo.Count).Length)
                                {
                                    FYSimVar.MIP50DataReturn = Convert.ToInt16(ConvertTrackNo2AbsoluteNo(FYSimVar.TrackNo.Count)[FYSimVar.MIP50Cnt2]);
                                    FYSimVar.MIP50Cnt2++;
                                }
                                else
                                {
                                    FYSimVar.MIP50Cnt2 = 0;
                                    FYSimVar.MIP50Cnt++;                                    
                                }
                                break;

                            case 4:
                                FYSimVar.MIP50DataReturn = 0xD;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 5:
                                FYSimVar.MIP50DataReturn = 0xA;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 6:
                                FYSimVar.MIP50DataReturn = 0x58;
                                FYSimVar.MIP50Cnt++;
                                break;
                            case 7:
                                FYSimVar.MIP50DataReturn = 0x30;
                                FYSimVar.MIP50Cnt = 0;
                                IoC.Logger.Log("FYSim State_Machine = State.Idle", LoggerInstance);
                                State_Machine = State.Idle;
                                break;
                        }
                    }
                    break;

                case State.Reset:
                    FYSimVar.Reset(); // <-------------------------------------------------------------------------------- Also reset SUB programs of simulator !!!! TBD !!!
                    FYSimVar.FiddleYardReset.Mssg = true;
                    State_Machine = State.Idle;
                    FYSimVar.MIP50Cnt = 0;
                    FYSimVar.MIP50Cnt2 = 0;
                    IoC.Logger.Log("FYSim State_Machine = State.Idle from State.Reset", LoggerInstance);
                    break;

                default:
                    break;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: ConvertTrackNo2AbsoluteNo
         * 
         *  Input(s)   : TrackNo 1 <> 11
         *
         *  Output(s)  : Track number in absolute POS count
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
        private UInt32[] TrackForward = new UInt32[12] { 0, 0, 42800, 85600, 128400, 171200, 214000, 256800, 299600, 342400, 385200, 428000 };// New track coordinates forward movement 1 --> 11 to be moved and get from Application Variables 

        public string ConvertTrackNo2AbsoluteNo(int TrackNo)
        {
            return (Convert.ToString(TrackForward[TrackNo]));
        }

        public int ConvertAbsoluteNo2TrackNo(UInt32 AbsoluteNo)
        {
            int _Return = 0;
            _Return = Array.IndexOf(TrackForward, Convert.ToUInt32(AbsoluteNo));    // when not negative give track number back when track movement is forward it will return valid track number            

            if (_Return == -1)                                                      // When returned track number is -1, a backward motion is probably used
            {
                _Return = (Array.IndexOf(TrackForward, AbsoluteNo + 700));          // so add 700 to the number
                if (_Return == 0)                                                   // If 0 is returned, track 1 is ment..
                {
                    _Return = 1;                                                    // return then track 1 for the FYMove.FiddleMultipleMove("Go" + Convert.ToString(Number)) which accpets only numbers 1 to 11.
                }
            }

            

            return (_Return);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Simulator settings from settings form
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
        public void SimulatorSettings(string name, int val, string log)
        {
            if (name == "FYSimSpeedSetting")
            {
                aTimer.Interval = val;
                IoC.Logger.Log("FYSim Simulator aTimer.Interval = " + Convert.ToString(aTimer.Interval), LoggerInstance);
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackNoToTrackString
         *                  convert chosen track number to string according
         *                  the active track and if a train is present
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
        private string TrackNoToTrackString(int val)
        {
            string _return = null;
            switch (val)
            {
                case 1: _return = "Track1";
                    break;
                case 2: _return = "Track2";
                    break;
                case 3: _return = "Track3";
                    break;
                case 4: _return = "Track4";
                    break;
                case 5: _return = "Track5";
                    break;
                case 6: _return = "Track6";
                    break;
                case 7: _return = "Track7";
                    break;
                case 8: _return = "Track8";
                    break;
                case 9: _return = "Track9";
                    break;
                case 10: _return = "Track10";
                    break;
                case 11: _return = "Track11";
                    break;
                case 12: _return = "Block5B";
                    break;
                case 13: _return = "Block8A";
                    break;
                default: _return = "Buffer";
                    break;
            }

            return _return;
        }       

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SetMessage and CommandToSend are used to catch 
         *               updates from simulator/application and process the contents in 
         *               the main simulator loop
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
        public void SetMessage(string name, string log)
        {
            int val = 0;            
            SimulatorUpdate(name, val);
        }
        public void CommandToSend(string name, string cmd)
        {
            int val = 0;
                       
            if (cmd == "pG\r"|| cmd == "qG\r")
            {
                val = GO;
                FYSimVar.MIP50xReceivedxCmdxArrayxCounterxTemp = FYSimVar.MIP50xReceivedxCmdxArrayxCounter;     // Store counter value in temp counter
                FYSimVar.MIP50xReceivedxCmdxStringxTemp = FYSimVar.MIP50xReceivedxCmdxString;
                FYSimVar.MIP50xReceivedxCmdxArrayxCounter = 0;                                                  // Reset FYSimVar.MIP50xReceivedxCmdxArrayxCounter for next command
                FYSimVar.MIP50xReceivedxCmdxString = "";
            }  
            else if (cmd[1] != '\r' && cmd[1] != '\n')          // Discard carriage return and line feed
            {
                FYSimVar.MIP50xReceivedxCmdxString = string.Concat(FYSimVar.MIP50xReceivedxCmdxString, cmd[1]);
                FYSimVar.MIP50xReceivedxCmdxArrayxCounter++;              
            }
            SimulatorUpdate(name, val);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: UpdateSimArrayToAppArray
         *                  Update the Track[x] VAR to imitate an update after a
         *                  traindetection() command which tracks are occupied
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
        public void UpdateSimArrayToAppArray()
        {
            FYSimVar.Track1.Value = Convert.ToBoolean(TrainsOnFYSim[1]);
            FYSimVar.Track2.Value = Convert.ToBoolean(TrainsOnFYSim[2]);
            FYSimVar.Track3.Value = Convert.ToBoolean(TrainsOnFYSim[3]);
            FYSimVar.Track4.Value = Convert.ToBoolean(TrainsOnFYSim[4]);
            FYSimVar.Track5.Value = Convert.ToBoolean(TrainsOnFYSim[5]);
            FYSimVar.Track6.Value = Convert.ToBoolean(TrainsOnFYSim[6]);
            FYSimVar.Track7.Value = Convert.ToBoolean(TrainsOnFYSim[7]);
            FYSimVar.Track8.Value = Convert.ToBoolean(TrainsOnFYSim[8]);
            FYSimVar.Track9.Value = Convert.ToBoolean(TrainsOnFYSim[9]);
            FYSimVar.Track10.Value = Convert.ToBoolean(TrainsOnFYSim[10]);
            FYSimVar.Track11.Value = Convert.ToBoolean(TrainsOnFYSim[11]);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: OnTimedEvent
         *               Only used when simulator is active to kick the main application
         *               and to kick the simulator. real target returns always every
         *               second values to C# application. The otherway around is 
         *               important to let the target know the C# application is still
         *               responding, this is not handled here.
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
        public void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            aTimer.Stop();
            byte[] data = new byte[] { 0x00, 0x00 };

            SimulatorUpdate("MIP50xDataxToxSend", 1);   // Kick data senders to simulate data that is passing from MIP50 via uController to C#. These may only be send after the 
                                                        // previous character of a command has been send by OnTimedEvent.

            if ("TOP" == m_instance)
            {
                NewData(FYSimVar.CreateData("M"));
                NewData(FYSimVar.CreateData("L"));
                NewData(FYSimVar.CreateData("K"));
                //NewData(FYSimVar.CreateData("J"));    // Tracks not sent anymore due to traindetection by FYapplication iso target
                NewData(FYSimVar.CreateData("I"));
                NewData(FYSimVar.CreateData("H"));
                NewData(FYSimVar.CreateData("A"));

            }
            else if ("BOT" == m_instance)
            {
                NewData(FYSimVar.CreateData("Z"));
                NewData(FYSimVar.CreateData("Y"));
                NewData(FYSimVar.CreateData("X"));
                //NewData(FYSimVar.CreateData("W"));    // Tracks not sent anymore due to traindetection by FYapplication iso target
                NewData(FYSimVar.CreateData("V"));
                NewData(FYSimVar.CreateData("U"));
                NewData(FYSimVar.CreateData("B"));
            }
            SetMessage("TargetAlive", " Target Alive ");
            aTimer.Start();
        }        
    }
}








//if (kicksimulator == "FiddleOneLeft")
//{
//    IoC.Logger.Log("FYSim State_Machine = State.FiddleOneLeft");
//    State_Machine = State.FiddleOneLeft;                                                // When a sequence has to be executed, the corresponding state is started
//}
//else if (kicksimulator == "FiddleOneRight")
//{
//    IoC.Logger.Log("FYSim State_Machine = State.FiddleOneRight");
//    State_Machine = State.FiddleOneRight;
//}
//else if (kicksimulator.TrimEnd(kicksimulator[kicksimulator.Length - 1]) == "FiddleGo" || kicksimulator == "FiddleGo10") // the 0 of 10 is not recognized...therefore: || kicksimulator == "FiddleGo10"
//{
//    string test = kicksimulator.TrimEnd(kicksimulator[kicksimulator.Length - 1]);
//    IoC.Logger.Log("FYSim FYMove.FiddleMultipleMove(" + kicksimulator + ")");
//    FYMove.FiddleMultipleMove(kicksimulator);                                           // Already pass the command which was received, else it will be lost
//    IoC.Logger.Log("FYSim State_Machine = State.FiddleMultipleMove");
//    State_Machine = State.FiddleMultipleMove;                                           // When a sequence has to be executed, the corresponding state is started
//}
//else if (kicksimulator == "TrainDetect")
//{
//    IoC.Logger.Log("FYSim State_Machine = State.TrainDetect");
//    State_Machine = State.TrainDetect;
//}

//case State.FiddleOneLeft:
//    if (true == FYMove.FiddleOneMove("Left"))
//    {
//        IoC.Logger.Log("FYSim true == FYMove.FiddleOneMove(Left)");
//        State_Machine = State.Idle;
//        IoC.Logger.Log("FYSim State_Machine = State.Idle from State.FiddleOneLeft");
//        FYSimVar.uControllerReady.Mssg = true;
//    }
//    break;

//case State.FiddleOneRight:
//    if (true == FYMove.FiddleOneMove("Right"))
//    {
//        IoC.Logger.Log("FYSim true == FYMove.FiddleOneMove(Right)");
//        State_Machine = State.Idle;
//        IoC.Logger.Log("FYSim State_Machine = State.Idle from State.FiddleOneRight");
//        FYSimVar.uControllerReady.Mssg = true;
//    }
//    break;

//case State.FiddleMultipleMove:
//    if (true == FYMove.FiddleMultipleMove(kicksimulator))
//    {
//        IoC.Logger.Log("FYSim true == FYMove.FiddleMultipleMove(kicksimulator)");
//        State_Machine = State.Idle;
//        IoC.Logger.Log("FYSim State_Machine = State.Idle from State.FiddleMultipleMove");
//        FYSimVar.uControllerReady.Mssg = true;
//    }
//    break;

//case State.TrainDetect:
//    if (true == FYTrDt.FiddleTrDt())
//    {
//        IoC.Logger.Log("FYSim true == FYTrDt.FiddleTrDt()");
//        State_Machine = State.Idle;
//        IoC.Logger.Log("FYSim State_Machine = State.Idle from State.TrainDetect");
//        FYSimVar.uControllerReady.Mssg = true;
//    }
//    break;