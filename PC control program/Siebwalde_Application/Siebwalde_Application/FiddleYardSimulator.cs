using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Siebwalde_Application
{
    public interface iFiddleYardSimulator
    {
        Trk GetTrackNo();
        Var GetCL10Heart();
        Var GetTrackPower();
        Var GetM10();
        Var GetResistor();
        Msg GetFiddleOneLeftFinished();
        Msg GetFiddleOneRightFinished();
        Msg GetFiddleMultipleLeftFinished();
        Msg  GetFiddleMultipleRightFinished();
        Msg GetTrainDetectionFinished();
        Msg GetTrainOn5B();
        Msg GetTrainOn8A();
        Msg GetFiddleYardReset();
    }

    public class FiddleYardSimulator : iFiddleYardSimulator
    {
        public iFiddleYardController m_iFYCtrl; // connect variable to connect to FYController class for defined interfaces
        public Action<byte[]> NewData;
        string m_instance = null;
        FiddleYardSimOneMove FYOneMove;

        private enum State { Idle, CL10Heart, Reset, FiddleOneLeft, FiddleOneRight };
        private State State_Machine;        

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
        Trk TrackNo = new Trk();
        Var TrackPower15V = new Var();
        Msg FiddleOneLeftFinished = new Msg();
        Msg FiddleOneRightFinished = new Msg();
        Msg FiddleMultipleLeftFinished = new Msg();
        Msg FiddleMultipleRightFinished = new Msg();
        Msg TrainDetectionFinished = new Msg();
        Msg TrainOn5B = new Msg();
        Msg TrainOn8A = new Msg();
        Msg FiddleYardReset = new Msg();

        List<Msg> list = new List<Msg>();

        public Trk GetTrackNo()
        {
            return TrackNo;
        }
        public Var GetCL10Heart()
        {
            return CL10Heart;
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
        
        
        public FiddleYardSimulator(string Instance, iFiddleYardController iFYCtrl)
        {
            m_iFYCtrl = iFYCtrl;    // connect to FYController interface, save interface in variable
            m_instance = Instance;

            FYOneMove = new FiddleYardSimOneMove(this);           

            if ("FiddleYardTOP" == m_instance)
            {                
                Message Msg_TargetAliveTop = new Message("TargetAlive", " Target Alive ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TargetAliveTop.Attach(Msg_TargetAliveTop); 
            }
            else if ("FiddleYardBOT" == m_instance)
            {                
                Message Msg_TargetAliveBot = new Message("TargetAlive", " Target Alive ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TargetAliveBot.Attach(Msg_TargetAliveBot); 
            }            
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
            TrackNo.Count = 1;
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
            TrainDetectionFinished.Data = 0x09;
            TrainOn5B.Mssg = false;
            TrainOn5B.Data = 0x0F;
            TrainOn8A.Mssg = false;
            TrainOn8A.Data = 0x11;
            FiddleYardReset.Mssg = false;
            FiddleYardReset.Data = 0x15;

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
                        State_Machine = State.FiddleOneLeft;                                // When a sequence has to be executed, the corresponding state is started
                    }
                    else if (kicksimulator == "FiddleOneRight")
                    {
                        State_Machine = State.FiddleOneRight;
                    }
                    else if (kicksimulator != "TargetAlive")
                    {
                        IdleSetVariable(kicksimulator);                                   // When only a variable has to be set it is done directly (manualy sending commands to target/simulator from FORM
                    }                          
                    break;

                case State.FiddleOneLeft:
                    if (true == FYOneMove.FiddleOneMove("Left"))
                    {
                        State_Machine = State.Idle;
                    }                    
                    break;

                case State.FiddleOneRight:
                    if (true == FYOneMove.FiddleOneMove("Right"))
                    {
                        State_Machine = State.Idle;
                    }                    
                    break;

                case State.Reset:
                    
                    break;

                default:
                    break;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: IdleSetVariable
         * 
         *  Input(s)   : Variable to be set, no sequence required
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
            if (Variable == "Bezet5BOnTrue")
            {
                Block5BIn.Value = true;
            }
            else if (Variable == "Bezet5BOnFalse")
            {
                Block5BIn.Value = false;
            }
            else if (Variable == "Bezet6OnTrue")
            {
                Block6In.Value = true;
            }
            else if (Variable == "Bezet6OnFalse")
            {
                Block6In.Value = false;
            }
            else if (Variable == "Bezet7OnTrue")
            {
                Block7In.Value = true;
            }
            else if (Variable == "Bezet7OnFalse")
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
            else if (Variable == "Reset")
            {
                //Reset.Value = true;           // Send message here              
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
