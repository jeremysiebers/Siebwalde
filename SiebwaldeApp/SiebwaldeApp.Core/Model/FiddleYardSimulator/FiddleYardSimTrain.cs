using System;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Core
{
    public class FiddleYardSimTrain
    {
        private iFiddleYardSimulator m_iFYSim;
        private FiddleYardIOHandleVariables m_FYIOHandleVar;             // connect variable to connect to FYIOH class for defined variables
        private FiddleYardSimulatorVariables m_FYSimVar;
        public string FYSimtrainInstance = null;
        public string ClassName { get { return "FYSimTrain"; } }
        private string m_instance = null;        
        private int BufferWaitDelay = 0;
        private int BufferWaitDelayRandomTrigger = 0;
        private Random rng = new Random();
        private int MinimumWaitTime = 300; // value for 100ms = 8
        private int MaximumWaitTime = 1500;// value for 100ms = 50
        private int MINIMUMxWAITxTIME = 8;
        private int MAXIMUMxWAITxTIME = 50;
        private const int ActionCounter1 = 3;
        private const int ActionCounter5 = 15;
        private const int ActionCounter10 = 30;
        private const int ActionCounter15 = 45;
        private const int ActionCounter20 = 60;
        private const int ActionCounter25 = 75;

        private string LoggerInstance { get; set; }
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

        private enum State { Idle, FYActiveTrack, TrainDriveToBlock8A, TrainInBlock5B, TrainInBlock8A, TrainDriveToBlock6, TrainDriveToBlock7, TrainDriveToBuffer, TrainInBuffer,
                                TrainInBlock6};
        private State FYSimTrainState;
        private int ActionCounter = 0;

        private ILogger FiddleYardSimTrainLogging;       

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardSimTrain constructor
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

        public FiddleYardSimTrain(string instance, iFiddleYardSimulator iFYSim, FiddleYardIOHandleVariables FYIOHandleVar, FiddleYardSimulatorVariables FYSimVar)
        {
        
            m_FYSimVar = FYSimVar;
            m_iFYSim = iFYSim;
            m_FYIOHandleVar = FYIOHandleVar;
            m_instance = instance;            

            Sensor Cmd_TargetAlive = new Sensor("TargetAlive", "TargetAlive", 0, (name, val, log) => SimulatorCmd(name, val, log)); // initialize and subscribe sensors
            m_FYSimVar.TargetAlive.Attach(Cmd_TargetAlive);
            Sensor TrackNo = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SimulatorCmd(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.TrackNo.Attach(TrackNo);

            if ("TOP" == m_instance)
            {
                // Set the log instance string to the logging instance name used for directed file logging
                LoggerInstance = "FySimTrainTopLog";
                //  different logging file per target, this is default
                FiddleYardSimTrainLogging = GetLogger(Properties.CoreSettings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "FiddleYardSimTrainLogTOP.txt", LoggerInstance);
                IoC.Logger.AddLogger(FiddleYardSimTrainLogging);
            }
            else if ("BOT" == m_instance)
            {
                // Set the log instance string to the logging instance name used for directed file logging
                LoggerInstance = "FySimTrainBotLog";
                //  different logging file per target, this is default
                FiddleYardSimTrainLogging = GetLogger(Properties.CoreSettings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "FiddleYardSimTrainLogBOT.txt", LoggerInstance);
                IoC.Logger.AddLogger(FiddleYardSimTrainLogging);
            }

            Sensor Sns_FYSimSpeedSetting = new Sensor("FYSimSpeedSetting", " FYSimSpeedSetting ", 0, (name, val, log) => SimulatorSettings(name, val, log)); // initialize and subscribe sensors
            Properties.CoreSettings.Default.FYSimSpeedSetting.Attach(Sns_FYSimSpeedSetting);
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
                MINIMUMxWAITxTIME = 80 / 100 * val;
                MAXIMUMxWAITxTIME = 500 / 100 * val;
                //IoC.Logger.Log(FYSimtrainInstance + " MINIMUMxWAITxTIME = " + Convert.ToString(MINIMUMxWAITxTIME));
                //IoC.Logger.Log(FYSimtrainInstance + " MAXIMUMxWAITxTIME = " + Convert.ToString(MAXIMUMxWAITxTIME));
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SimTrainLocation
         *                  Gets the location from the simulator
         *                  
         * 
         *  Input(s)   : Loacation of simtrain
         *
         *  Output(s)  : location of simtrain
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
        private string _SimTrainLocation;

        public string SimTrainLocation
        {
            get { return _SimTrainLocation; }
            set
            {
                if (_SimTrainLocation == null)
                {
                    IoC.Logger.Log("###Fiddle Yard " + FYSimtrainInstance + " Started###", LoggerInstance);
                    IoC.Logger.Log(FYSimtrainInstance + " on " + value, LoggerInstance);
                }
                else if (_SimTrainLocation != null && _SimTrainLocation != value)
                {
                    IoC.Logger.Log(FYSimtrainInstance + " on " + value, LoggerInstance);//+ " :location change with set SimTrainLocation.");
                }
                _SimTrainLocation = value;
                
            }
        }
        
        /*#--------------------------------------------------------------------------#*/
        /*  Description: SimulatorCmd
         *                  Update SimTrain with updates or kick
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
        private void SimulatorCmd(string kicksimtrain, int val, string log)
        {
            switch (FYSimTrainState)
            {
                case State.Idle:
                    if (kicksimtrain == "Track_No" && SimTrainLocation == TrackNoToTrackString(val))
                    {
                        FYSimTrainState = State.FYActiveTrack;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.FYActiveTrack", LoggerInstance);
                    }
                    else if (SimTrainLocation == "Block5B")
                    {
                        m_FYSimVar.Block5B.Value = true;
                        FYSimTrainState = State.TrainInBlock5B;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock5B", LoggerInstance);
                    }
                    else if (SimTrainLocation == "Block8A")
                    {
                        m_FYSimVar.Block8A.Value = true;
                        FYSimTrainState = State.TrainInBlock8A;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock8A", LoggerInstance);
                    }
                    else if (SimTrainLocation == "Block6")
                    {
                        m_FYSimVar.Block6.Value = true;
                        FYSimTrainState = State.TrainInBlock6;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock6", LoggerInstance);
                    }
                    else if (SimTrainLocation == "Buffer")
                    {
                        BufferWaitDelayRandomTrigger = (Convert.ToInt16(DateTime.Now.Second) + 1) * 10;                // Use a variable millisecond value as wait time until the next train wants to enter the fiddle yard 
                        for (int i = 1; i <= BufferWaitDelayRandomTrigger; i++)
                        {
                            BufferWaitDelay = rng.Next(MinimumWaitTime, MaximumWaitTime);
                        }                        
                        FYSimTrainState = State.TrainInBuffer;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBuffer. " + "BufferWaitDelay = " + Convert.ToString(BufferWaitDelay), LoggerInstance);                        
                    }
                    break;

                case State.FYActiveTrack:
                    if (kicksimtrain == "Reset")
                    {
                        FYSimTrainState = State.Idle;
                        ActionCounter = 0;
                        break;
                    }                    

                    m_FYSimVar.F10.Value = true;
                    m_FYSimVar.F11.Value = true;

                    if (m_FYSimVar.TrackPower.Value == true)
                    {
                        m_FYSimVar.Block7.Value = true;
                    }
                    else
                    {
                        m_FYSimVar.Block7.Value = false;
                    }

                    if (m_FYSimVar.Block7In.Value == false && m_FYSimVar.Block8A.Value == false && m_FYSimVar.TrackPower.Value == true)// check if train may leave and if block 8 is free and track is powered (coupled)
                    {
                        FYSimTrainState = State.TrainDriveToBlock8A;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBlock8A", LoggerInstance);
                        ActionCounter = 0;
                    }
                    if (kicksimtrain == "Track_No" && SimTrainLocation != TrackNoToTrackString(val))
                    {
                        FYSimTrainState = State.Idle;
                        m_FYSimVar.F10.Value = false;
                        m_FYSimVar.F11.Value = false;
                        IoC.Logger.Log(FYSimtrainInstance + " kicksimtrain == Track_No && SimTrainLocation != TrackNoToTrackString(val)", LoggerInstance);
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.Idle", LoggerInstance);
                    }
                    break;

                case State.TrainDriveToBlock8A:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                   

                    if (m_FYSimVar.Block7In.Value == false && m_FYSimVar.TrackPower.Value == true && ActionCounter < ActionCounter5)
                    {
                        ActionCounter++;
                    }
                    else if (ActionCounter >= ActionCounter5)
                    {
                        ActionCounter++;
                    }

                    if (ActionCounter >= ActionCounter5 && ActionCounter < ActionCounter10)
                    {
                        m_FYSimVar.F11.Value = false;
                    }

                    if (ActionCounter >= ActionCounter5 && ActionCounter < ActionCounter15)
                    {
                        m_FYSimVar.F12.Value = true;
                        m_iFYSim.GetTrainsOnFYSim()[m_FYSimVar.TrackNo.Count] = 0; // basicaly when train is driving and passing F12 it has "left" the track of the fiddleyard, no way back!
                    }
                    else
                    {
                        m_FYSimVar.F12.Value = false;
                    }

                    if (ActionCounter >= ActionCounter10 && ActionCounter < ActionCounter15)
                    {
                        SimTrainLocation = "Block8A";                   // train drives into block 8A, tail is still in block7
                        m_FYSimVar.Block8A.Value = true;
                    }

                    if (ActionCounter > ActionCounter20)
                    {
                        m_FYSimVar.Block7.Value = false;             // train has left block 7, also passed F10
                        m_FYSimVar.F10.Value = false;
                    }

                    if (ActionCounter >= ActionCounter25)
                    {
                        m_iFYSim.GetTrainsOnFYSim()[m_FYSimVar.TrackNo.Count] = 0;
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainInBlock8A;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock8A", LoggerInstance);
                    }
                    break;

                case State.TrainInBlock8A:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }                 

                    ActionCounter++;
                    if (ActionCounter >= ActionCounter5)
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainDriveToBuffer;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBuffer", LoggerInstance);
                    }
                    break;

                case State.TrainDriveToBuffer:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }                    

                    SimTrainLocation = "Buffer";                        // train drives into buffer
                    m_FYSimVar.Block8A.Value = false;
                    FYSimTrainState = State.TrainInBuffer;
                    IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBuffer", LoggerInstance);

                    BufferWaitDelayRandomTrigger = (Convert.ToInt16(DateTime.Now.Second) + 1) * 10;                // Use a variable millisecond value as wait time until the next train wants to enter the fiddle yard 
                    for (int i = 1; i <= BufferWaitDelayRandomTrigger; i++)
                    {
                        BufferWaitDelay = rng.Next(MinimumWaitTime, MaximumWaitTime);
                    }                    
                    IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBuffer. " + "BufferWaitDelay = " + Convert.ToString(BufferWaitDelay), LoggerInstance);       
                    break;

                case State.TrainInBuffer:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                    

                    ActionCounter++;
                    if (ActionCounter >= BufferWaitDelay)
                    {
                        ActionCounter = 0;                        
                        if (m_FYSimVar.Block5B.Value == true)  // A new train may enter block 5B when a train has left block 5B
                        {
                            FYSimTrainState = State.TrainInBuffer;                            
                        }
                        else 
                        {                            
                            m_FYSimVar.Block5B.Value = true;
                            FYSimTrainState = State.TrainInBlock5B;
                            IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock5B", LoggerInstance);
                            SimTrainLocation = "Block5B";
                        }                        
                    }
                    break;

                case State.TrainInBlock5B:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }


                    if (m_FYSimVar.Block5BIn.Value == false && m_FYSimVar.Block6.Value == false) // when block occupied signal is set by application OR another train is stil in block 6 trains stops moving in block 5B
                    {
                        ActionCounter++;// when block occupied signal is set by application trains stops moving in block 6, block 5B is also still occupied by the length of the train
                    }

                    if (ActionCounter >= ActionCounter5)
                    {
                        m_FYSimVar.Block6.Value = true;
                        ActionCounter = ActionCounter5;
                    }

                    if (m_FYSimVar.Block6In.Value == false && ActionCounter >= ActionCounter5)// check if block6 is not occupied and that the trains has driven to edge of block5B towards block6
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainDriveToBlock6;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBlock6", LoggerInstance);                        
                    }
                    break;

                case State.TrainDriveToBlock6:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                   

                    if (m_FYSimVar.Block6In.Value != true)
                    {
                        ActionCounter++;// when block occupied signal is set by application trains stops moving in block 6, block 5B is also still occupied by the length of the train
                    }

                    if (ActionCounter >= ActionCounter5)
                    {
                        m_FYSimVar.Block5B.Value = false; 
                        m_FYSimVar.F10.Value = true;
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainInBlock6;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock6", LoggerInstance);
                        SimTrainLocation = "Block6";
                    }
                    break;

                case State.TrainInBlock6:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                   

                    if (m_FYSimVar.Block7.Value == false && m_FYSimVar.TrackNo.Count != 0 && m_FYSimVar.TrackPower.Value == true)// check if train may drive into block7, if the fiddle yard is aligned and if coupled
                    {                        
                        FYSimTrainState = State.TrainDriveToBlock7;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBlock7", LoggerInstance);
                        m_FYSimVar.Block7.Value = true;
                    }
                    break;

                case State.TrainDriveToBlock7:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;                        
                    }
                    else if (kicksimtrain == "Track_No" && SimTrainLocation != TrackNoToTrackString(val)) // when fiddle yard moves away, train is on track[x], update TrainsOnFYSim
                    {
                        m_iFYSim.GetTrainsOnFYSim()[Convert.ToInt16(SimTrainLocation.Substring(SimTrainLocation.IndexOf(@"k") + 1))] = 1; 
                        ActionCounter = 0;
                        m_FYSimVar.Block7.Value = false;
                        FYSimTrainState = State.Idle;
                        IoC.Logger.Log(FYSimtrainInstance + " kicksimtrain == Track_No && SimTrainLocation != TrackNoToTrackString(val)", LoggerInstance);
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.Idle", LoggerInstance);
                        break;
                    }

                    if (m_FYSimVar.Block7In.Value == false && m_FYSimVar.TrackPower.Value == true)
                    {
                        ActionCounter++;// when block occupied signal is set by application trains stops moving in block 7, block 6 is also still occupied by the length of the train
                    }

                    if (ActionCounter >= ActionCounter1 && ActionCounter < ActionCounter15)
                    {
                        SimTrainLocation = "Track" + Convert.ToString(m_FYSimVar.TrackNo.Count);
                        m_FYSimVar.F13.Value = true;                        
                        //IoC.Logger.Log(FYSimtrainInstance + " in " + SimTrainLocation); <-------------------- a lot of logging!!!
                    }
                    else 
                    { 
                        m_FYSimVar.F13.Value = false;                        
                    }

                    if (ActionCounter >= ActionCounter15)
                    {
                        m_FYSimVar.Block6.Value = false;
                        m_FYSimVar.F11.Value = true;                        
                    }
                    else { m_FYSimVar.F11.Value = false; }

                    if (ActionCounter >= ActionCounter25)
                    {
                        ActionCounter = 0;
                        IoC.Logger.Log(FYSimtrainInstance + " Train not stopped on fiddle yard...", LoggerInstance);
                        FYSimTrainState = State.TrainDriveToBlock8A;
                        IoC.Logger.Log(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBlock8A", LoggerInstance);                        
                    }
                    break;

                default: break;
            }

        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackNoToTrackString
         *                  convert received track number to string according
         *                  the active track.
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
                case 0x10: _return = "Track1";
                    break;
                case 0x20: _return = "Track2";
                    break;
                case 0x30: _return = "Track3";
                    break;
                case 0x40: _return = "Track4";
                    break;
                case 0x50: _return = "Track5";
                    break;
                case 0x60: _return = "Track6";
                    break;
                case 0x70: _return = "Track7";
                    break;
                case 0x80: _return = "Track8";
                    break;
                case 0x90: _return = "Track9";
                    break;
                case 0xA0: _return = "Track10";
                    break;
                case 0xB0: _return = "Track11";
                    break;
                default: _return = "Track0";
                    break;
            }

            return _return;
        }
    }

    
}
