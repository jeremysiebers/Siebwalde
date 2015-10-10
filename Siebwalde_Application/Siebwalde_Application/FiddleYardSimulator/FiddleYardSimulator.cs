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
        FiddleYardSimulator GetFYSim();      // interface to pass the FYSIm methods        
        void UpdateSimArrayToAppArray();        
    }

    public class FiddleYardSimulator : iFiddleYardSimulator
    {
        public iFiddleYardIOHandle m_iFYIOH; // connect variable to connect to FYIOH class for defined interfaces
        public Action<byte[]> NewData;
        bool m_instance = true;
        public FiddleYardSimMove FYMove;
        FiddleYardSimTrainDetect FYTrDt; 
        private const bool TOP = true;
        private const bool BOT = false;
        private enum State { Idle, CL10Heart, Reset, FiddleOneLeft, FiddleOneRight, FiddleMultipleMove, TrainDetect, Start };
        private State State_Machine;
        public int[] TrainsOnFYSim = new int[NoOfSimTrains + 1]; // counting from 1!!!
        private Random rng = new Random();
        private const int NoOfSimTrains = 12; // counting from 1!!!
        string path = "null";
        public Log2LoggingFile FiddleYardSimulatorLogging;

        // Create a timer
        System.Timers.Timer aTimer = new System.Timers.Timer();
        // Hook up the Elapsed event for the timer. 

        public Var CL10Heart = new Var();
        public Var F11 = new Var();		
        public Var EOS10 = new Var();		
        public Var EOS11 = new Var();		
        public Var F13 = new Var();		
        public Var F12 = new Var();		
        public Var Block5B = new Var();	
        public Var Block8A = new Var();
        public Var TrackPower = new Var();
        public Var Block5BIn = new Var();	
        public Var Block6In = new Var();
        public Var Block7In = new Var();
        public Var Resistor = new Var();
        public Var Track1 = new Var();
        public Var Track2 = new Var();
        public Var Track3 = new Var();
        public Var Track4 = new Var();
        public Var Track5 = new Var();
        public Var Track6 = new Var();
        public Var Track7 = new Var();
        public Var Track8 = new Var();
        public Var Track9 = new Var();
        public Var Track10 = new Var();
        public Var Track11 = new Var();
        public Var Block6 = new Var();
        public Var Block7 = new Var();
        public Var F10 = new Var();
        public Var M10 = new Var();        
        public Var TrackPower15V = new Var();        
        
        public Trk TrackNo = new Trk();
        
        public Msg FiddleOneLeftFinished = new Msg();
        public Msg FiddleOneRightFinished = new Msg();
        public Msg FiddleMultipleLeftFinished = new Msg();
        public Msg FiddleMultipleRightFinished = new Msg();
        public Msg TrainDetectionFinished = new Msg();
        public Msg TrainOn5B = new Msg();
        public Msg TrainOn8A = new Msg();
        public Msg FiddleYardReset = new Msg();
        public Msg uControllerReady = new Msg();                

        List<Msg> list = new List<Msg>();
        List<FiddleYardSimTrain> FYSimTrains = new List<FiddleYardSimTrain>();
        FiddleYardSimTrain current = null;

        public SensorUpdater TargetAlive;

        public FiddleYardSimulator GetFYSim()
        {
            return (this);
        }    

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

        public FiddleYardSimulator(bool Instance, iFiddleYardIOHandle iFYIOH)
        {
            m_iFYIOH = iFYIOH;    // connect to FYIOHandle interface, save interface in variable
            m_instance = Instance;

            if (TOP == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardSimulatorTOP.txt"; //  different logging file per target, this is default
                FiddleYardSimulatorLogging = new Log2LoggingFile(path);
            }
            else if (BOT == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardSimulatorBOT.txt"; //  different logging file per target, this is default
                FiddleYardSimulatorLogging = new Log2LoggingFile(path);
            }

            TargetAlive = new SensorUpdater();
            FYMove = new FiddleYardSimMove(this);
            FYTrDt = new FiddleYardSimTrainDetect(this);

            for (int i = 1; i <= NoOfSimTrains; i++)
            {
                TrainsOnFYSim[i] = rng.Next(0, 2);
                current = new FiddleYardSimTrain(m_instance, this, m_iFYIOH);
                current.FYSimtrainInstance = current.ClassName + i.ToString();
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
            TrackNo.Count = 1;
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
        public string TrackNoToTrackString(int val)
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
            Reset();
            FiddleYardSimulatorLogging.StoreText("FYSim Simulator Reset()");

            list.Add(FiddleOneLeftFinished);
            list.Add(FiddleOneRightFinished);
            list.Add(FiddleMultipleLeftFinished);
            list.Add(FiddleMultipleRightFinished);
            list.Add(TrainDetectionFinished);
            list.Add(TrainOn5B);
            list.Add(TrainOn8A);
            list.Add(FiddleYardReset);
            list.Add(uControllerReady);
            
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            // Set the Interval to [x] miliseconds.
            aTimer.Interval = 1;
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
                case State.Idle:
                    if (kicksimulator == "FiddleOneLeft")
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.FiddleOneLeft");
                        State_Machine = State.FiddleOneLeft;                                                // When a sequence has to be executed, the corresponding state is started
                        
                        
                    }
                    else if (kicksimulator == "FiddleOneRight")
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.FiddleOneRight");
                        State_Machine = State.FiddleOneRight;
                        
                        
                    }
                    else if (kicksimulator.TrimEnd(kicksimulator[kicksimulator.Length - 1]) == "FiddleGo" || kicksimulator == "FiddleGo10") // the 0 of 10 is not recognized...therefore: || kicksimulator == "FiddleGo10"
                    {
                        string test = kicksimulator.TrimEnd(kicksimulator[kicksimulator.Length - 1]);
                        FiddleYardSimulatorLogging.StoreText("FYSim FYMove.FiddleMultipleMove(" + kicksimulator + ")");
                        FYMove.FiddleMultipleMove(kicksimulator);                                           // Already pass the command which was received, else it will be lost
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.FiddleMultipleMove");
                        State_Machine = State.FiddleMultipleMove;                                           // When a sequence has to be executed, the corresponding state is started
                        
                        
                    }
                    else if (kicksimulator == "TrainDetect")
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.TrainDetect");                        
                        State_Machine = State.TrainDetect;
                    }
                    else if (kicksimulator == "Reset")
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim kicksimulator == Reset");
                        State_Machine = State.Reset;
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Reset");
                    }

                    else if (kicksimulator != "TargetAlive")
                    {
                       IdleSetVariable(kicksimulator);                                                     // Only when a variable needs to be set it is done directly (manualy sending commands to target/simulator from FORM
                       FiddleYardSimulatorLogging.StoreText("FYSim IdleSetVariable(" + kicksimulator + ")");
                       uControllerReady.Mssg = true;
                    }

                    else if (kicksimulator == "TargetAlive")
                    {                        
                        TargetAlive.UpdateSensorValue(1, true);                                             // Update all clients of TargetAlive (SimTrains)
                    }
                    break;


                    
                case State.FiddleOneLeft:
                    if (true == FYMove.FiddleOneMove("Left"))
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim true == FYMove.FiddleOneMove(Left)");
                        State_Machine = State.Idle;
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.FiddleOneLeft");
                        uControllerReady.Mssg = true;
                    }                    
                    break;

                case State.FiddleOneRight:
                    if (true == FYMove.FiddleOneMove("Right"))
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim true == FYMove.FiddleOneMove(Right)");
                        State_Machine = State.Idle;
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.FiddleOneRight");
                        uControllerReady.Mssg = true;                        
                    }                    
                    break;

                case State.FiddleMultipleMove:
                    if (true == FYMove.FiddleMultipleMove(kicksimulator))
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim true == FYMove.FiddleMultipleMove(kicksimulator)");
                        State_Machine = State.Idle;
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.FiddleMultipleMove");
                        uControllerReady.Mssg = true;                        
                    }
                    break;

                case State.TrainDetect:
                    if (true == FYTrDt.FiddleTrDt())
                    {
                        FiddleYardSimulatorLogging.StoreText("FYSim true == FYTrDt.FiddleTrDt()");
                        State_Machine = State.Idle;
                        FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.TrainDetect");
                        uControllerReady.Mssg = true;                        
                    }                    
                    break;

                case State.Reset:                    
                    Reset();
                    FiddleYardReset.Mssg = true;
                    State_Machine = State.Idle;
                    FiddleYardSimulatorLogging.StoreText("FYSim State_Machine = State.Idle from State.Reset");                    
                    break;

                default:
                    break;
            }

            // reset certain simulated sensor signals when shifting the fiddle yard
            if (TrackNo.Count == 0)
            {
                F10.Value = false;
                F11.Value = false;
                F12.Value = false;
                F12.Value = false;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Reset
         * 
         *  Input(s)   : Reset all variables to default
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
        public void Reset()
        {
            CL10Heart.Value = true;
            F11.Value = false;
            EOS10.Value = false;
            EOS11.Value = false;
            F13.Value = false;
            F12.Value = false;
            Block5B.Value = false;
            Block8A.Value = false;
            TrackPower.Value = false;
            Block5BIn.Value = true;
            Block6In.Value = true;
            Block7In.Value = true;
            Resistor.Value = true;
            Track1.Value = false;
            Track2.Value = false;
            Track3.Value = false;
            Track4.Value = false;
            Track5.Value = false;
            Track6.Value = false;
            Track7.Value = false;
            Track8.Value = false;
            Track9.Value = false;
            Track10.Value = false;
            Track11.Value = false;
            Block6.Value = false;
            Block7.Value = false;
            F10.Value = false;
            M10.Value = false;
            //TrackNo.Count = 1;            
            TrackPower15V.Value = true;

            FiddleOneLeftFinished.Mssg = false;
            FiddleOneLeftFinished.Data = 0x03;
            FiddleOneRightFinished.Mssg = false;
            FiddleOneRightFinished.Data = 0x04;

            FiddleMultipleLeftFinished.Mssg = false;
            FiddleMultipleLeftFinished.Data = 0x05;
            FiddleMultipleRightFinished.Mssg = false;
            FiddleMultipleRightFinished.Data = 0x06;
            TrainDetectionFinished.Mssg = false;
            TrainDetectionFinished.Data = 0x07;
            TrainOn5B.Mssg = false;
            TrainOn5B.Data = 0x0F;
            TrainOn8A.Mssg = false;
            TrainOn8A.Data = 0x11;
            FiddleYardReset.Mssg = false;
            FiddleYardReset.Data = 0x15;
            uControllerReady.Mssg = false;
            uControllerReady.Data = 0x30;
        }


        /*#--------------------------------------------------------------------------#*/
        /*  Description: IdleSetVariable
         * 
         *  Input(s)   : Variable to be set, no sequence required, these are commands
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
        public void IdleSetVariable(string Variable)
        {
            if (Variable == "Occ5BOnTrue")
            {
                Block5BIn.Value = true;
            }
            else if (Variable == "Occ5BOnFalse")
            {
                Block5BIn.Value = false;
            }
            else if (Variable == "Occ6OnTrue")
            {
                Block6In.Value = true;
            }
            else if (Variable == "Occ6OnFalse")
            {
                Block6In.Value = false;
            }
            else if (Variable == "Occ7OnTrue")
            {
                Block7In.Value = true;
            }
            else if (Variable == "Occ7OnFalse")
            {
                Block7In.Value = false;
            }
            else if (Variable == "Couple")
            {
                TrackPower.Value = true;
                Resistor.Value = false;
            }
            else if (Variable == "Uncouple")
            {
                TrackPower.Value = false;
                Resistor.Value = true;
            }            
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
            Track1.Value = Convert.ToBoolean(TrainsOnFYSim[1]);
            Track2.Value = Convert.ToBoolean(TrainsOnFYSim[2]);
            Track3.Value = Convert.ToBoolean(TrainsOnFYSim[3]);
            Track4.Value = Convert.ToBoolean(TrainsOnFYSim[4]);
            Track5.Value = Convert.ToBoolean(TrainsOnFYSim[5]);
            Track6.Value = Convert.ToBoolean(TrainsOnFYSim[6]);
            Track7.Value = Convert.ToBoolean(TrainsOnFYSim[7]);
            Track8.Value = Convert.ToBoolean(TrainsOnFYSim[8]);
            Track9.Value = Convert.ToBoolean(TrainsOnFYSim[9]);
            Track10.Value = Convert.ToBoolean(TrainsOnFYSim[10]);
            Track11.Value = Convert.ToBoolean(TrainsOnFYSim[11]);     
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
            aTimer.Enabled = false;
            byte[] data = new byte[] { 0x00, 0x00 };

            if (TOP == m_instance)
            {                           
                NewData(CreateData("M"));
                NewData(CreateData("L"));
                NewData(CreateData("K"));
                NewData(CreateData("J"));
                NewData(CreateData("I"));
                NewData(CreateData("H"));
                NewData(CreateData("A"));

            }
            else if (BOT == m_instance)
            {                
                NewData(CreateData("Z"));
                NewData(CreateData("Y"));
                NewData(CreateData("X"));
                NewData(CreateData("W"));
                NewData(CreateData("V"));
                NewData(CreateData("U"));
                NewData(CreateData("B"));
            }
            SetMessage("TargetAlive", " Target Alive ");
            aTimer.Enabled = true;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: CreateData
         *               To create the data as it would be sent by the real target
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
        public byte[] CreateData(string group)
        {
            byte[] data = new byte[] { 0x00, 0x00 };
            byte[] _group = new byte[] {0, 0};
            int _data = 0;

            if ("M" == group || "Z" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(CL10Heart.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(F11.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(EOS10.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(EOS11.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(F13.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("L" == group || "Y" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(TrackNo.Count);
                _data = _data << 1;
                _data |= Convert.ToByte(F12.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block5B.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block8A.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("K" == group || "X" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(TrackPower.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block5BIn.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block6In.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block7In.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Resistor.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track1.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track2.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("J" == group || "W" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(Track4.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track5.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track6.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track7.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track8.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track9.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track10.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("I" == group || "V" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(Block6.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block7.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(TrackPower15V.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(F10.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(M10.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track3.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track11.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("H" == group || "U" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("A" == group || "B" == group)
            {                
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                data[1] = 0x00;

                foreach (Msg msg in list)
                {
                    if (msg.Mssg == true)
                    {
                        msg.Mssg = false;
                        data[1] = Convert.ToByte(msg.Data);
                        break;
                    }
                }
            }  

            return(data);
        }
}


    /*#--------------------------------------------------------------------------#*/
    /*  Description: Var
     * 
     *  Input(s)   : Set New bool value for sensor
     *
     *  Output(s)  : Return bool value of the sensor
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
    public class Var
    {
        public bool Value { get; set; }
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: Trk
     * 
     *  Input(s)   : Set New count value for Track no
     *
     *  Output(s)  : Return track no value target like
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
    public class Trk
    {
        private int _Count;
        private int Count_Return;

        public int Count 
        { 
            set 
            {
                _Count = value;
            }

            get 
            {
                switch (_Count)
                {
                    case 0: Count_Return = 0;
                        break;
                    case 1: Count_Return = 0x1;
                        break;
                    case 2: Count_Return = 0x2;
                        break;
                    case 3: Count_Return = 0x3;
                        break;
                    case 4: Count_Return = 0x4;
                        break;
                    case 5: Count_Return = 0x5;
                        break;
                    case 6: Count_Return = 0x6;
                        break;
                    case 7: Count_Return = 0x7;
                        break;
                    case 8: Count_Return = 0x8;
                        break;
                    case 9: Count_Return = 0x9;
                        break;
                    case 10: Count_Return = 0xA;
                        break;
                    case 11: Count_Return = 0xB;
                        break;
                    default: Count_Return = 0;
                        break;
                }

                return Count_Return;
            }
        }
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: Msg
     * 
     *  Input(s)   : Set New Message
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
    public class Msg
    {
        public bool Mssg { get; set; }
        public int Data { get; set; }
    }
}
