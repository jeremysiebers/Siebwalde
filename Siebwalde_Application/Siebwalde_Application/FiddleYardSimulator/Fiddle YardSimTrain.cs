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
    public class FiddleYardSimTrain
    {
        private iFiddleYardSimulator m_iFYSim;
        private iFiddleYardIOHandle m_iFYIOH;
        public string FYSimtrainInstance = null;
        public string ClassName { get { return "FYSimTrain"; } }
        private bool m_instance = true;
        private const bool TOP = true;
        private const bool BOT = false;
        private int BufferWaitDelay = 0;
        private int BufferWaitDelayRandomTrigger = 0;
        private Random rng = new Random();
        private int MinimumWaitTime = 80;//80; 100 or 10 us timer
        private int MaximumWaitTime = 500;//500; 100 or 10 us timer

        private enum State { Idle, FYActiveTrack, TrainDriveToBlock8A, TrainInBlock5B, TrainInBlock8A, TrainDriveToBlock6, TrainDriveToBlock7, TrainDriveToBuffer, TrainInBuffer,
                                TrainInBlock6};
        private State FYSimTrainState;
        private int ActionCounter = 0;

        public Log2LoggingFile FiddleYardSimTrainLogging;
        string path = "null";        

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

        public FiddleYardSimTrain(bool instance, iFiddleYardSimulator iFYSim, iFiddleYardIOHandle iFYIOH)
        {
            m_iFYSim = iFYSim;
            m_iFYIOH = iFYIOH;
            m_instance = instance;            

            Sensor Cmd_TargetAlive = new Sensor("TargetAlive", "TargetAlive", 0, (name, val, log) => SimulatorCmd(name, val, log)); // initialize and subscribe sensors
            m_iFYSim.GetFYSim().TargetAlive.Attach(Cmd_TargetAlive);
            Sensor TrackNo = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SimulatorCmd(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().TrackNo.Attach(TrackNo);
            if (TOP == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardSimTrainTOP.txt"; //  different logging file per target, this is default
                FiddleYardSimTrainLogging = new Log2LoggingFile(path);
            }
            else if (BOT == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardSimTrainBOT.txt"; //  different logging file per target, this is default
                FiddleYardSimTrainLogging = new Log2LoggingFile(path);
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
                    FiddleYardSimTrainLogging.StoreText("###Fiddle Yard " + FYSimtrainInstance + " Started###");
                    FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " on " + value);
                }
                else if (_SimTrainLocation != null && _SimTrainLocation != value)
                {
                    FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " on " + value);//+ " :location change with set SimTrainLocation.");
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
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.FYActiveTrack");
                    }
                    else if (SimTrainLocation == "Block5B")
                    {
                        m_iFYSim.GetFYSim().Block5B.Value = true;
                        FYSimTrainState = State.TrainInBlock5B;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock5B");
                    }
                    else if (SimTrainLocation == "Block8A")
                    {
                        m_iFYSim.GetFYSim().Block8A.Value = true;
                        FYSimTrainState = State.TrainInBlock8A;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock8A");
                    }
                    else if (SimTrainLocation == "Block6")
                    {
                        m_iFYSim.GetFYSim().Block6.Value = true;
                        FYSimTrainState = State.TrainInBlock6;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock6");
                    }
                    else if (SimTrainLocation == "Buffer")
                    {
                        BufferWaitDelayRandomTrigger = (Convert.ToInt16(DateTime.Now.Second) + 1) * 10;                // Use a variable millisecond value as wait time until the next train wants to enter the fiddle yard 
                        for (int i = 1; i <= BufferWaitDelayRandomTrigger; i++)
                        {
                            BufferWaitDelay = rng.Next(MinimumWaitTime, MaximumWaitTime);
                        }                        
                        FYSimTrainState = State.TrainInBuffer;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBuffer. " + "BufferWaitDelay = " + Convert.ToString(BufferWaitDelay));                        
                    }
                    break;

                case State.FYActiveTrack:
                    if (kicksimtrain == "Reset")
                    {
                        FYSimTrainState = State.Idle;
                        ActionCounter = 0;
                        break;
                    }                    

                    m_iFYSim.GetFYSim().F10.Value = true;
                    m_iFYSim.GetFYSim().F11.Value = true;

                    if (m_iFYSim.GetFYSim().TrackPower.Value == true)
                    {
                        m_iFYSim.GetFYSim().Block7.Value = true;
                    }
                    else
                    {
                        m_iFYSim.GetFYSim().Block7.Value = false;
                    }

                    if (m_iFYSim.GetFYSim().Block7In.Value == false && m_iFYSim.GetFYSim().Block8A.Value == false && m_iFYSim.GetFYSim().TrackPower.Value == true)// check if train may leave and if block 8 is free and track is powered (coupled)
                    {
                        FYSimTrainState = State.TrainDriveToBlock8A;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBlock8A");
                        ActionCounter = 0;
                    }
                    if (kicksimtrain == "Track_No" && SimTrainLocation != TrackNoToTrackString(val))
                    {
                        FYSimTrainState = State.Idle;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " kicksimtrain == Track_No && SimTrainLocation != TrackNoToTrackString(val)");
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.Idle");
                    }
                    break;

                case State.TrainDriveToBlock8A:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                    else if (kicksimtrain == "Track_No")
                    {
                        break;
                    }

                    if (m_iFYSim.GetFYSim().Block7In.Value == false && m_iFYSim.GetFYSim().TrackPower.Value == true && ActionCounter < 5)
                    {
                        ActionCounter++;
                    }
                    else if (ActionCounter >= 5)
                    {
                        ActionCounter++;
                    }

                    if (ActionCounter >= 1 && ActionCounter < 5)
                    {
                        m_iFYSim.GetFYSim().F11.Value = false;
                    }

                    if (ActionCounter >= 5 && ActionCounter < 10)
                    {
                        m_iFYSim.GetFYSim().F12.Value = true;
                        m_iFYSim.GetFYSim().TrainsOnFYSim[m_iFYSim.GetFYSim().TrackNo.Count] = 0; // basicaly when train is driving and passing F12 it has "left" the track of the fiddleyard, no way back!
                    }
                    else
                    {
                        m_iFYSim.GetFYSim().F12.Value = false;
                    }

                    if (ActionCounter >= 5 && ActionCounter < 15)
                    {
                        SimTrainLocation = "Block8A";                   // train drives into block 8A, tail is still in block7
                        m_iFYSim.GetFYSim().Block8A.Value = true;
                    }                    

                    if (ActionCounter > 15)
                    {
                        m_iFYSim.GetFYSim().Block7.Value = false;             // train has left block 7, also passed F10
                        m_iFYSim.GetFYSim().F10.Value = false;
                    }

                    if (ActionCounter >= 20)
                    {
                        m_iFYSim.GetFYSim().TrainsOnFYSim[m_iFYSim.GetFYSim().TrackNo.Count] = 0;
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainInBlock8A;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock8A");
                    }
                    break;

                case State.TrainInBlock8A:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                    else if (kicksimtrain == "Track_No")
                    {
                        break;
                    }

                    ActionCounter++;
                    if (ActionCounter >= 5)
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainDriveToBuffer;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBuffer");
                    }
                    break;

                case State.TrainDriveToBuffer:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                    else if (kicksimtrain == "Track_No")
                    {
                        break;
                    }

                    SimTrainLocation = "Buffer";                        // train drives into buffer
                    m_iFYSim.GetFYSim().Block8A.Value = false;
                    FYSimTrainState = State.TrainInBuffer;
                    FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBuffer");

                    BufferWaitDelayRandomTrigger = (Convert.ToInt16(DateTime.Now.Second) + 1) * 10;                // Use a variable millisecond value as wait time until the next train wants to enter the fiddle yard 
                    for (int i = 1; i <= BufferWaitDelayRandomTrigger; i++)
                    {
                        BufferWaitDelay = rng.Next(MinimumWaitTime, MaximumWaitTime);
                    }                    
                    FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBuffer. " + "BufferWaitDelay = " + Convert.ToString(BufferWaitDelay));       
                    break;

                case State.TrainInBuffer:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                    else if (kicksimtrain == "Track_No")
                    {
                        break;
                    }

                    ActionCounter++;
                    if (ActionCounter >= BufferWaitDelay)
                    {
                        ActionCounter = 0;                        
                        if (m_iFYSim.GetFYSim().Block5B.Value == true || m_iFYSim.GetFYSim().Block6.Value == true)  // A new train may enter block 5B when a train has left block 5B and block6 
                        {
                            FYSimTrainState = State.TrainInBuffer;                            
                        }
                        else 
                        {                            
                            m_iFYSim.GetFYSim().Block5B.Value = true;
                            FYSimTrainState = State.TrainInBlock5B;
                            FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock5B");
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
                    else if (kicksimtrain == "Track_No")
                    {
                        break;
                    }

                    if (m_iFYSim.GetFYSim().Block5BIn.Value != true) // when block occupied signal is set by application trains stops moving in block 5B
                    {
                        ActionCounter++;// when block occupied signal is set by application trains stops moving in block 6, block 5B is also still occupied by the length of the train
                    }                    
                    
                    if (ActionCounter >= 5)
                    {
                        m_iFYSim.GetFYSim().Block6.Value = true;                        
                        ActionCounter = 5;
                    }

                    if (m_iFYSim.GetFYSim().Block6In.Value == false && ActionCounter >= 5)// check if block6 is not occupied and that the trains has driven to edge of block5B towards block6
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainDriveToBlock6;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBlock6");                        
                    }
                    break;

                case State.TrainDriveToBlock6:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                    else if (kicksimtrain == "Track_No")
                    {
                        break;
                    }

                    if (m_iFYSim.GetFYSim().Block6In.Value != true)
                    {
                        ActionCounter++;// when block occupied signal is set by application trains stops moving in block 6, block 5B is also still occupied by the length of the train
                    }                                        

                    if (ActionCounter >= 5)
                    {
                        m_iFYSim.GetFYSim().Block5B.Value = false; 
                        m_iFYSim.GetFYSim().F10.Value = true;
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainInBlock6;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainInBlock6");
                        SimTrainLocation = "Block6";
                    }
                    break;

                case State.TrainInBlock6:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                    else if (kicksimtrain == "Track_No")
                    {
                        break;
                    }

                    if (m_iFYSim.GetFYSim().Block7.Value == false && m_iFYSim.GetFYSim().TrackNo.Count != 0 && m_iFYSim.GetFYSim().TrackPower.Value == true)// check if train may drive into block7, if the fiddle yard is aligned and if coupled
                    {                        
                        FYSimTrainState = State.TrainDriveToBlock7;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBlock7");
                        m_iFYSim.GetFYSim().Block7.Value = true;
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
                        m_iFYSim.GetFYSim().TrainsOnFYSim[Convert.ToInt16(SimTrainLocation.Substring(SimTrainLocation.IndexOf(@"k") + 1))] = 1; 
                        ActionCounter = 0;
                        m_iFYSim.GetFYSim().Block7.Value = false;
                        FYSimTrainState = State.Idle;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " kicksimtrain == Track_No && SimTrainLocation != TrackNoToTrackString(val)");
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.Idle");
                        break;
                    }

                    if (m_iFYSim.GetFYSim().Block7In.Value == false && m_iFYSim.GetFYSim().TrackPower.Value == true)
                    {
                        ActionCounter++;// when block occupied signal is set by application trains stops moving in block 7, block 6 is also still occupied by the length of the train
                    }
                    
                    if (ActionCounter >= 1 && ActionCounter < 5)
                    {
                        SimTrainLocation = "Track" + Convert.ToString(m_iFYSim.GetFYSim().TrackNo.Count);
                        m_iFYSim.GetFYSim().F13.Value = true;                        
                        //FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " in " + SimTrainLocation); <-------------------- a lot of logging!!!
                    }
                    else { m_iFYSim.GetFYSim().F13.Value = false; }                    

                    if (ActionCounter >= 5)
                    {
                        m_iFYSim.GetFYSim().F11.Value = true;
                        m_iFYSim.GetFYSim().Block6.Value = false;
                    }
                    else { m_iFYSim.GetFYSim().F11.Value = false; }

                    if (ActionCounter >= 25)
                    {
                        ActionCounter = 0;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " Train not stopped on fiddle yard...");
                        FYSimTrainState = State.TrainDriveToBlock8A;
                        FiddleYardSimTrainLogging.StoreText(FYSimtrainInstance + " FYSimTrainState = State.TrainDriveToBlock8A");                        
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
