using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Timers;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Net.NetworkInformation;
using System.Globalization;

namespace Siebwalde_Application
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
        public FiddleYardSimMove FYMove;
        public FiddleYardSimTrainDetect FYTrDt;
        private Log2LoggingFile FiddleYardSimulatorLogging;

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
        string path = "null";

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
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardSimulatorTOP.txt"; //  different logging file per target, this is default
                FiddleYardSimulatorLogging = new Log2LoggingFile(path);
            }
            else if ("BOT" == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardSimulatorBOT.txt"; //  different logging file per target, this is default
                FiddleYardSimulatorLogging = new Log2LoggingFile(path);
            }

            FYSimVar = new FiddleYardSimulatorVariables();            
            FYMove = new FiddleYardSimMove(this, FiddleYardSimulatorLogging, FYSimVar);
            FYTrDt = new FiddleYardSimTrainDetect(this, FiddleYardSimulatorLogging, FYSimVar, FYMove);

            for (int i = 1; i <= NoOfSimTrains; i++)
            {
                TrainsOnFYSim[i] = rng.Next(0, 2);
                current = new FiddleYardSimTrain(m_instance, this, m_FYIOHandleVar, FYSimVar);
                current.FYSimtrainInstance = current.ClassName + i.ToString(fmt);
                if (TrainsOnFYSim[i] == 1)
                {
                    current.SimTrainLocation = TrackNoToTrackString(i);
                }
                else
                {
                    current.SimTrainLocation = TrackNoToTrackString(0);
                }
                FYSimTrains.Add(current);
            }
            FYSimVar.TrackNo.Count = 1;

            Sensor Sns_FYSimSpeedSetting = new Sensor("FYSimSpeedSetting", " FYSimSpeedSetting ", 0, (name, val, log) => SimulatorSettings(name, val, log)); // initialize and subscribe sensors
            Siebwalde_Application.Properties.Settings.Default.FYSimSpeedSetting.Attach(Sns_FYSimSpeedSetting);
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
            FiddleYardSimulatorLogging.StoreText("### Fiddle Yard Simulator started ###");
            FYSimVar.Reset();
            FiddleYardSimulatorLogging.StoreText("FYSim Simulator Reset()");            

            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            // Set the Interval to [x] miliseconds.
            aTimer.Interval = Convert.ToInt16(Siebwalde_Application.Properties.Settings.Default.FIDDLExYARDxSIMxSPEEDxSETTING);
            aTimer.AutoReset = true;
            // Enable the timer
            aTimer.Enabled = true;

            FiddleYardSimulatorLogging.StoreText("FYSim Simulator Timer started: aTimer.Interval = " + Convert.ToString(aTimer.Interval));
            FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from Start()");
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Stop: When simulator is not required, stop the alive kick
         *               
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
        public void Stop()
        {
            aTimer.Enabled = false;
            FiddleYardSimulatorLogging.StoreText("FYSimulator STOP() command received, stopping timer event");
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
                    if (kicksimulator == "MIP50xAbs_Pos")
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xAbs_Pos");
                        State_Machine = State.MIP50xAbs_Pos;
                        FYSimVar.MIP50Cnt = 0; // Reset this variable
                    }
                    else if (kicksimulator == "MIP50xHomexAxis" && val == GO)
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xHomexAxis");
                        State_Machine = State.MIP50xHomexAxis;
                    }
                    else if (kicksimulator == "MIP50xSetxPositioningxVelxDefault" && val == GO)
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xSetxPositioningxVelxDefault");
                        State_Machine = State.MIP50xSetxPositioningxVelxDefault;
                    }
                    else if (kicksimulator == "MIP50xSetxAcceleration" && val == GO)
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xSetxAcceleration");
                        State_Machine = State.MIP50xSetxAcceleration;
                    }
                    else if (kicksimulator == "MIP50xClearxError" && val == GO)
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xClearxError");
                        State_Machine = State.MIP50xClearxError;
                    }
                    else if (kicksimulator == "MIP50xActivatexPosxReg" && val == GO)
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xActivatexPosxReg");
                        State_Machine = State.MIP50xActivatexPosxReg;
                    }
                    else if (kicksimulator == "MIP50xDeactivatexPosxReg" && val == GO)
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xDeactivatexPosxReg");
                        State_Machine = State.MIP50xDeactivatexPosxReg;
                    }
                    else if (kicksimulator == "MIP50xReadxPosition" && val == GO)
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xReadxPosition");
                        State_Machine = State.MIP50xReadxPosition;
                    }
                    else if (kicksimulator == "Reset")
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim kicksimulator == Reset");
                        State_Machine = State.Reset;
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Reset");
                    }
                    else if (kicksimulator != "TargetAlive")
                    {
                        FYSimVar.IdleSetVariable(kicksimulator);                                                     // Only when a variable needs to be set it is done directly (manualy sending commands to target/simulator from FORM
                        //FiddleYardSimulatorLogging.StoreText("FYSim IdleSetVariable(" + kicksimulator + ")");
                        FYSimVar.uControllerReady.Mssg = true;
                    }
                    else if (kicksimulator == "TargetAlive")
                    {
                        FYSimVar.TargetAlive.UpdateSensorValue(1, true);                                             // Update all clients of TargetAlive (SimTrains)
                    }
                    break;

                /*---------------------------------------Execute Type of command -----------------------------------------------------------------------------------------------------------------
                */

                case State.MIP50xAbs_Pos:
                    if (kicksimulator == "MIP50xAbs_Pos" && val != GO)
                    {
                        FYSimVar.MIP50xAbs_Pos[FYSimVar.MIP50Cnt] = val; // first character (A) is lost due to switching from state.
                        FYSimVar.MIP50Cnt++;
                    }
                    else if (kicksimulator == "MIP50xAbs_Pos" && val == GO)
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle"); // To do--> filter out new track number, kick old move program, after program clear FYSimVar.MIP50xAbs_Pos[x]!!!!!!
                        State_Machine = State.Idle;

                        int Number = 0;
                        for (int i = 2; i < FYSimVar.MIP50Cnt; i++) // Start with LSB
                        {
                            Number += FYSimVar.MIP50xAbs_Pos[i] * Convert.ToInt32(Math.Pow(10, FYSimVar.MIP50xAbs_Pos.Length - i - 3));
                        }
                        FYSimVar.MIP50Cnt = 0;
                        Number = ConvertAbsoluteNo2TrackNo(Number);
                        FYMove.FiddleMultipleMove("Go" + Convert.ToString(Number));                                         
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.FiddleMultipleMove");
                        State_Machine = State.FiddleMultipleMove;                                        
                    }
                    break;

                case State.FiddleMultipleMove:
                    if (true == FYMove.FiddleMultipleMove(kicksimulator))
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim true == FYMove.FiddleMultipleMove(kicksimulator)");
                        State_Machine = State.MIP50xSetxPositioningxVelxDefault;
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.MIP50xSetxPositioningxVelxDefault from State.FiddleMultipleMove"); // to sen X0 after move
                        FYSimVar.uControllerReady.Mssg = true;
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
                                FYSimVar.Track1.Value = true;   // After homing Track 1 is active
                                FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle");
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
                                FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle");
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
                                FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle");
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
                                FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle");
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
                                FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle");
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
                                FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle");
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
                                if (FYSimVar.MIP50Cnt2 != MIP50xM21xHeader.Length)
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
                                FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle");
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
                    FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.Reset");
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
        private int[] TrackForward = new int[12] { 0, 0, 42800, 85600, 128400, 171200, 214000, 256800, 299600, 342400, 385200, 428000 };// New track coordinates forward movement 1 --> 11 to be moved and get from Application Variables 

        public string ConvertTrackNo2AbsoluteNo(int TrackNo)
        {
            return (Convert.ToString(TrackForward[TrackNo]));
        }

        public int ConvertAbsoluteNo2TrackNo(int AbsoluteNo)
        {
            int _Return = Array.IndexOf(TrackForward, AbsoluteNo);

            if (_Return == -1)
            {
                return (Array.IndexOf(TrackForward, AbsoluteNo + 700));
            }
            else
            {
                return (_Return);
            }

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
                FiddleYardSimulatorLogging.StoreText("FYSim Simulator aTimer.Interval = " + Convert.ToString(aTimer.Interval));
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
            string CmdNo2String;
           
            if (cmd == "pG\r"|| cmd == "qG\r")
            {
                val = GO;
            }  
            else
            {
                if (char.IsNumber(cmd[1]))                      // Check if send character is a number
                {
                    CmdNo2String = Convert.ToString(cmd[1]);    // Convert the number character only to string
                    //Int32.TryParse(CmdNo2String, 16, out val);      // parse string number only to int
                    val = Convert.ToInt32(CmdNo2String, 16);
                }
                else
                {
                    val = Convert.ToInt32(cmd[1]);  // if string is number, this is converted to dec number in int.
                }
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
                NewData(FYSimVar.CreateData("J"));
                NewData(FYSimVar.CreateData("I"));
                NewData(FYSimVar.CreateData("H"));
                NewData(FYSimVar.CreateData("A"));

            }
            else if ("BOT" == m_instance)
            {
                NewData(FYSimVar.CreateData("Z"));
                NewData(FYSimVar.CreateData("Y"));
                NewData(FYSimVar.CreateData("X"));
                NewData(FYSimVar.CreateData("W"));
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
//    FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.FiddleOneLeft");
//    State_Machine = State.FiddleOneLeft;                                                // When a sequence has to be executed, the corresponding state is started
//}
//else if (kicksimulator == "FiddleOneRight")
//{
//    FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.FiddleOneRight");
//    State_Machine = State.FiddleOneRight;
//}
//else if (kicksimulator.TrimEnd(kicksimulator[kicksimulator.Length - 1]) == "FiddleGo" || kicksimulator == "FiddleGo10") // the 0 of 10 is not recognized...therefore: || kicksimulator == "FiddleGo10"
//{
//    string test = kicksimulator.TrimEnd(kicksimulator[kicksimulator.Length - 1]);
//    FiddleYardSimulatorLogging.StoreText("FYSim FYMove.FiddleMultipleMove(" + kicksimulator + ")");
//    FYMove.FiddleMultipleMove(kicksimulator);                                           // Already pass the command which was received, else it will be lost
//    FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.FiddleMultipleMove");
//    State_Machine = State.FiddleMultipleMove;                                           // When a sequence has to be executed, the corresponding state is started
//}
//else if (kicksimulator == "TrainDetect")
//{
//    FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.TrainDetect");
//    State_Machine = State.TrainDetect;
//}

//case State.FiddleOneLeft:
//    if (true == FYMove.FiddleOneMove("Left"))
//    {
//        FiddleYardSimulatorLogging.StoreText("FYSim true == FYMove.FiddleOneMove(Left)");
//        State_Machine = State.Idle;
//        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.FiddleOneLeft");
//        FYSimVar.uControllerReady.Mssg = true;
//    }
//    break;

//case State.FiddleOneRight:
//    if (true == FYMove.FiddleOneMove("Right"))
//    {
//        FiddleYardSimulatorLogging.StoreText("FYSim true == FYMove.FiddleOneMove(Right)");
//        State_Machine = State.Idle;
//        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.FiddleOneRight");
//        FYSimVar.uControllerReady.Mssg = true;
//    }
//    break;

//case State.FiddleMultipleMove:
//    if (true == FYMove.FiddleMultipleMove(kicksimulator))
//    {
//        FiddleYardSimulatorLogging.StoreText("FYSim true == FYMove.FiddleMultipleMove(kicksimulator)");
//        State_Machine = State.Idle;
//        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.FiddleMultipleMove");
//        FYSimVar.uControllerReady.Mssg = true;
//    }
//    break;

//case State.TrainDetect:
//    if (true == FYTrDt.FiddleTrDt())
//    {
//        FiddleYardSimulatorLogging.StoreText("FYSim true == FYTrDt.FiddleTrDt()");
//        State_Machine = State.Idle;
//        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.TrainDetect");
//        FYSimVar.uControllerReady.Mssg = true;
//    }
//    break;