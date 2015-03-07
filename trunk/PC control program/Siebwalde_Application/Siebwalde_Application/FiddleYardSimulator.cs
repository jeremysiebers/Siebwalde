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
        Trk GetTrackNo();
        Var GetCL10Heart();
        Var GetF10();
        Var GetF11();
        Var GetF12();
        Var GetF13();
        Var GetTrackPower();
        Var GetM10();
        Var GetResistor();
        Var GetTrack1();
        Var GetTrack2();
        Var GetTrack3();
        Var GetTrack4();
        Var GetTrack5();
        Var GetTrack6();
        Var GetTrack7();
        Var GetTrack8();
        Var GetTrack9();
        Var GetTrack10();
        Var GetTrack11();
        Var GetBlock5B();
        Var GetBlock6();
        Var GetBlock7();
        Var GetBlock8A();
        Var GetBlock5BIn();
        Var GetBlock6In();
        Var GetBlock7In();
        Msg GetFiddleOneLeftFinished();
        Msg GetFiddleOneRightFinished();
        Msg GetFiddleMultipleLeftFinished();
        Msg GetFiddleMultipleRightFinished();
        Msg GetTrainDetectionFinished();
        Msg GetTrainOn5B();
        Msg GetTrainOn8A();
        Msg GetFiddleYardReset();
        void StoreText(string text);
        void UpdateSimArrayToAppArray();
        FiddleYardSimMove GetFYMove();
        SensorUpdater GetTargetAlive();        
    }

    public class FiddleYardSimulator : iFiddleYardSimulator
    {
        public iFiddleYardController m_iFYCtrl; // connect variable to connect to FYController class for defined interfaces
        public Action<byte[]> NewData;
        string m_instance = null;
        FiddleYardSimMove FYMove;
        FiddleYardSimTrainDetect FYTrDt;        
        string path = "null";

        private enum State { Idle, CL10Heart, Reset, FiddleOneLeft, FiddleOneRight, FiddleMultipleMove, TrainDetect, Start };
        private State State_Machine;
        private int[] TrainsOnFYSim = new int[14];
        private Random rng = new Random();
        private const int NoOfSimTrains = 12; // Added 1 extra for counter, 13 --> 12 trains

        // Create a timer
        System.Timers.Timer aTimer = new System.Timers.Timer();
        // Hook up the Elapsed event for the timer. 

        Var CL10Heart = new Var();
        Var F11 = new Var();		
        Var EOS10 = new Var();		
        Var EOS11 = new Var();		
        Var F13 = new Var();		
        Var F12 = new Var();		
        Var Block5B = new Var();	
        Var Block8A = new Var();
        Var TrackPower = new Var();
        Var Block5BIn = new Var();	
        Var Block6In = new Var();
        Var Block7In = new Var();
        Var Resistor = new Var();
        Var Track1 = new Var();
        Var Track2 = new Var();
        Var Track3 = new Var();
        Var Track4 = new Var();
        Var Track5 = new Var();
        Var Track6 = new Var();
        Var Track7 = new Var();
        Var Track8 = new Var();
        Var Track9 = new Var();
        Var Track10 = new Var();
        Var Track11 = new Var();
        Var Block6 = new Var();
        Var Block7 = new Var();
        Var F10 = new Var();
        Var M10 = new Var();        
        Var TrackPower15V = new Var();

        Trk TrackNo = new Trk();

        Msg FiddleOneLeftFinished = new Msg();
        Msg FiddleOneRightFinished = new Msg();
        Msg FiddleMultipleLeftFinished = new Msg();
        Msg FiddleMultipleRightFinished = new Msg();
        Msg TrainDetectionFinished = new Msg();
        Msg TrainOn5B = new Msg();
        Msg TrainOn8A = new Msg();
        Msg FiddleYardReset = new Msg();

        List<Msg> list = new List<Msg>();
        List<FiddleYardSimTrain> FYSimTrains = new List<FiddleYardSimTrain>();
        FiddleYardSimTrain current = null;

        public Trk GetTrackNo()
        {
            return TrackNo;
        }
        public Var GetBlock5B()
        {
            return Block5B;
        }
        public Var GetBlock6()
        {
            return Block6;
        }
        public Var GetBlock7()
        {
            return Block7;
        }
        public Var GetBlock8A()
        {
            return Block8A;
        }
        public Var GetBlock5BIn()
        {
            return Block5BIn;
        }
        public Var GetBlock6In()
        {
            return Block6In;
        }
        public Var GetBlock7In()
        {
            return Block7In;
        }        
        public Var GetCL10Heart()
        {
            return CL10Heart;
        }

        public Var GetF10()
        {
            return F10;
        }
        public Var GetF11()
        {
            return F11;
        }
        public Var GetF12()
        {
            return F12;
        }
        public Var GetF13()
        {
            return F13;
        }

        public Var GetTrackPower()
        {
            return TrackPower;
        }
        public Var GetM10()
        {
            return M10;
        }
        public Var GetResistor()
        {
            return Resistor;
        }
        public Var GetTrack1()
        {
            return Track1;
        }
        public Var GetTrack2()
        {
            return Track2;
        }
        public Var GetTrack3()
        {
            return Track3;
        }
        public Var GetTrack4()
        {
            return Track4;
        }
        public Var GetTrack5()
        {
            return Track5;
        }
        public Var GetTrack6()
        {
            return Track6;
        }
        public Var GetTrack7()
        {
            return Track7;
        }
        public Var GetTrack8()
        {
            return Track8;
        }
        public Var GetTrack9()
        {
            return Track9;
        }
        public Var GetTrack10()
        {
            return Track10;
        }
        public Var GetTrack11()
        {
            return Track11;
        }
        public Msg GetFiddleOneLeftFinished()
        {
            return FiddleOneLeftFinished;
        }
        public Msg GetFiddleOneRightFinished()
        {
            return FiddleOneRightFinished;
        }        
        public Msg GetFiddleMultipleLeftFinished()
        {
            return FiddleMultipleLeftFinished;
        }        
        public Msg  GetFiddleMultipleRightFinished()
        {
            return FiddleMultipleRightFinished;
        }        
        public Msg GetTrainDetectionFinished()
        {
            return TrainDetectionFinished;
        }        
        public Msg GetTrainOn5B()
        {
            return TrainOn5B;
        }
        public Msg GetTrainOn8A()
        {
            return TrainOn8A;
        }
        public Msg GetFiddleYardReset()
        {
            return FiddleYardReset;
        }
        public FiddleYardSimMove GetFYMove()
        {
            return FYMove;
        }

        public SensorUpdater TargetAlive;
        public SensorUpdater GetTargetAlive()
        {
            return TargetAlive;
        }
        
        public FiddleYardSimulator(string Instance, iFiddleYardController iFYCtrl)
        {
            m_iFYCtrl = iFYCtrl;    // connect to FYController interface, save interface in variable
            m_instance = Instance;

            TargetAlive = new SensorUpdater();
            FYMove = new FiddleYardSimMove(this);
            FYTrDt = new FiddleYardSimTrainDetect(this);
            
            if ("FiddleYardTOP" == m_instance)
            {      
                path = @"c:\localdata\FiddleYardSimTOPLogging.txt"; // different logging file per target, this is default
                Message Msg_TargetAliveTop = new Message("TargetAlive", " Target Alive ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TargetAliveTop.Attach(Msg_TargetAliveTop);

                for (int i = 1; i < NoOfSimTrains; i++)
                {
                    TrainsOnFYSim[i]=rng.Next(0, 2);
                    current = new FiddleYardSimTrain(m_instance, this, m_iFYCtrl);
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
            else if ("FiddleYardBOT" == m_instance)
            {    
                path = @"c:\localdata\FiddleYardSimBOTLogging.txt"; // different logging file per target, this is default
                Message Msg_TargetAliveBot = new Message("TargetAlive", " Target Alive ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TargetAliveBot.Attach(Msg_TargetAliveBot);

                for (int i = 1; i < NoOfSimTrains; i++)
                {
                    rng.Next(0, 2);
                }
                for (int i = 1; i < NoOfSimTrains; i++)
                {
                    TrainsOnFYSim[i] = rng.Next(0, 2);
                    current = new FiddleYardSimTrain(m_instance, this, m_iFYCtrl);
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
            StoreText("### Fiddle Yard Simulator started ###");
            Reset();            
            StoreText("FYSim Simulator Reset()");

            list.Add(FiddleOneLeftFinished);
            list.Add(FiddleOneRightFinished);
            list.Add(FiddleMultipleLeftFinished);
            list.Add(FiddleMultipleRightFinished);
            list.Add(TrainDetectionFinished);
            list.Add(TrainOn5B);
            list.Add(TrainOn8A);
            list.Add(FiddleYardReset);
            
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            // Set the Interval to [x] seconds.
            aTimer.Interval = 100;
            // Enable the timer
            aTimer.Enabled = true;
            
            StoreText("FYSim Simulator Timer started: aTimer.Interval = 100");
            StoreText("FYSim State_Machine = State.Idle from Start()");            
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
                        StoreText("FYSim State_Machine = State.FiddleOneLeft");
                        State_Machine = State.FiddleOneLeft;                                                // When a sequence has to be executed, the corresponding state is started
                    }
                    else if (kicksimulator == "FiddleOneRight")
                    {
                        StoreText("FYSim State_Machine = State.FiddleOneRight");
                        State_Machine = State.FiddleOneRight;
                    }
                    else if (kicksimulator.TrimEnd(kicksimulator[kicksimulator.Length - 1]) == "FiddleGo")
                    {
                        StoreText("FYSim FYMove.FiddleMultipleMove(" + kicksimulator + ")");
                        FYMove.FiddleMultipleMove(kicksimulator);                                           // Already pass the command which was received, else it will be lost
                        StoreText("FYSim State_Machine = State.FiddleMultipleMove");
                        State_Machine = State.FiddleMultipleMove;                                           // When a sequence has to be executed, the corresponding state is started
                    }
                    else if (kicksimulator == "TrainDetect")
                    {
                        StoreText("FYSim State_Machine = State.TrainDetect");                        
                        State_Machine = State.TrainDetect;
                    }
                    else if (kicksimulator == "Reset")
                    {
                        StoreText("FYSim kicksimulator == Reset");
                        State_Machine = State.Reset;
                        StoreText("FYSim State_Machine = State.Reset");
                    }
                    else if (kicksimulator != "TargetAlive")
                    {
                        IdleSetVariable(kicksimulator);                                                     // Only when a variable needs to be set it is done directly (manualy sending commands to target/simulator from FORM
                        StoreText("FYSim IdleSetVariable(" + kicksimulator + ")");
                    }
                    else if (kicksimulator == "TargetAlive")
                    {                        
                        TargetAlive.UpdateSensorValue(1, true);                                             // Update all clients of TargetAlive (SimTrains)
                    }
                    break;


                    
                case State.FiddleOneLeft:
                    if (true == FYMove.FiddleOneMove("Left"))
                    {
                        StoreText("FYSim true == FYMove.FiddleOneMove(Left)");
                        State_Machine = State.Idle;
                        StoreText("FYSim State_Machine = State.Idle from State.FiddleOneLeft");
                    }                    
                    break;

                case State.FiddleOneRight:
                    if (true == FYMove.FiddleOneMove("Right"))
                    {
                        StoreText("FYSim true == FYMove.FiddleOneMove(Right)");
                        State_Machine = State.Idle;
                        StoreText("FYSim State_Machine = State.Idle from State.FiddleOneRight");
                    }                    
                    break;

                case State.FiddleMultipleMove:
                    if (true == FYMove.FiddleMultipleMove(kicksimulator))
                    {
                        StoreText("FYSim true == FYMove.FiddleMultipleMove(kicksimulator)");
                        State_Machine = State.Idle;
                        StoreText("FYSim State_Machine = State.Idle from State.FiddleMultipleMove");
                    }
                    break;

                case State.TrainDetect:
                    if (true == FYTrDt.FiddleTrDt())
                    {
                        StoreText("FYSim true == FYTrDt.FiddleTrDt()");
                        State_Machine = State.Idle;
                        StoreText("FYSim State_Machine = State.Idle from State.TrainDetect");
                    }                    
                    break;

                case State.Reset:                    
                    Reset();
                    FiddleYardReset.Mssg = true;
                    State_Machine = State.Idle;
                    StoreText("FYSim State_Machine = State.Idle from State.Reset");
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
            TrackPower15V.Value = false;

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
            
            if ("FiddleYardTOP" == m_instance)
            {
                for (int i = 1; i < NoOfSimTrains; i++)
                {
                    TrainsOnFYSim[i] = rng.Next(0, 2);
                    current = FYSimTrains[i - 1];
                    if (TrainsOnFYSim[i] == 1)
                    {
                        current.SimTrainLocation = TrackNoToTrackString(i);
                    }
                    else
                    {
                        current.SimTrainLocation = TrackNoToTrackString(0);
                    }                    
                }
            }
            else if ("FiddleYardBOT" == m_instance)
            {
                for (int i = 1; i < NoOfSimTrains; i++)
                {
                    rng.Next(0, 2);
                }
                for (int i = 1; i < NoOfSimTrains; i++)
                {
                    TrainsOnFYSim[i] = rng.Next(0, 2);
                    current = FYSimTrains[i - 1];
                    if (TrainsOnFYSim[i] == 1)
                    {
                        current.SimTrainLocation = TrackNoToTrackString(i);
                    }
                    else
                    {
                        current.SimTrainLocation = TrackNoToTrackString(0);
                    }                    
                }
            }
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
        public void CommandToSend(string name, string layer, string cmd)
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

            if ("FiddleYardTOP" == m_instance)
            {
                NewData(CreateData("AliveTOP"));                                  //Send alive TOP               
                NewData(CreateData("M"));
                NewData(CreateData("L"));
                NewData(CreateData("K"));
                NewData(CreateData("J"));
                NewData(CreateData("I"));
                NewData(CreateData("A"));

            }
            else if ("FiddleYardBOT" == m_instance)
            {
                NewData(CreateData("AliveBOT"));                                  //Send alive BOT
                NewData(CreateData("Z"));
                NewData(CreateData("Y"));
                NewData(CreateData("X"));
                NewData(CreateData("W"));
                NewData(CreateData("V"));
                NewData(CreateData("B"));
            }
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

            if ("AliveTOP" == group)
            {
                data[0] = 0x41;
                data[1] = 0x30;
            }
            else if ("AliveBOT" == group)
            {
                data[0] = 0x42;
                data[1] = 0x30;
            }
            else if ("M" == group || "Z" == group)
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


        /*#--------------------------------------------------------------------------#*/
        /*  Description: StoreText
         * 
         *  Input(s)   : Store diagnostic text
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
        public void StoreText(string text)
        {
            string m_text = DateTime.Now + " " + text + " " + Environment.NewLine;

            try
            {

                using (var fs = new FileStream(path, FileMode.Append))
                {
                    Byte[] info =
                        new UTF8Encoding(true).GetBytes(m_text);
                    fs.Write(info, 0, info.Length);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
