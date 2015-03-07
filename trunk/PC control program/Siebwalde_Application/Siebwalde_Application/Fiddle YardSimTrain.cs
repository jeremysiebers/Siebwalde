using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    public class FiddleYardSimTrain
    {
        private iFiddleYardSimulator m_iFYSim;
        private iFiddleYardController m_iFYCtrl;
        public string FYSimtrainInstance = null;
        public string ClassName { get { return "FYSimTrain"; } }
        private string m_instance = null;

        private enum State { Idle, FYActiveTrack, TrainDriveToBlock8A, TrainInBlock5B, TrainInBlock8A, TrainDriveToBlock6, TrainDriveToBlock7, TrainDriveToBuffer, TrainInBuffer,
                                TrainInBlock6};
        private State FYSimTrainState;
        private int ActionCounter = 0;

        public FiddleYardSimTrain(string Instance, iFiddleYardSimulator iFYSim, iFiddleYardController iFYCtrl)
        {
            m_iFYSim = iFYSim;
            m_iFYCtrl = iFYCtrl;
            m_instance = Instance;

            Sensor Cmd_TargetAlive = new Sensor("TargetAlive", "TargetAlive", 0, (name, val, log) => SimulatorCmd(name, val, log)); // initialize and subscribe sensors
            m_iFYSim.GetTargetAlive().Attach(Cmd_TargetAlive);

            if ("FiddleYardTOP" == m_instance)
            {
                Sensor TrackNo = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SimulatorCmd(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackNoTop.Attach(TrackNo);
            }
            else if ("FiddleYardBOT" == m_instance)
            {
                Sensor TrackNo = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SimulatorCmd(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackNoBot.Attach(TrackNo);
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
        public void SimulatorCmd(string kicksimtrain, int val, string log)
        {
            switch (FYSimTrainState)
            {
                case State.Idle:
                    if (kicksimtrain == "Track_No" && SimTrainLocation == TrackNoToTrackString(val))
                    {
                        FYSimTrainState = State.FYActiveTrack;                        
                    }
                    else if (SimTrainLocation == "Block5B")
                    {
                        m_iFYSim.GetBlock5B().Value = true;
                        FYSimTrainState = State.TrainInBlock5B; 
                    }
                    else if (SimTrainLocation == "Block8A")
                    {
                        m_iFYSim.GetBlock8A().Value = true;
                        FYSimTrainState = State.TrainInBlock8A; 
                    }
                    else if (SimTrainLocation == "Block6")
                    {
                        m_iFYSim.GetBlock6().Value = true;
                        FYSimTrainState = State.TrainInBlock6; 
                    }
                    else if (SimTrainLocation == "Buffer")
                    {                        
                        FYSimTrainState = State.TrainInBuffer; 
                    }
                    break;

                case State.FYActiveTrack:
                    if (kicksimtrain == "Reset")
                    {
                        FYSimTrainState = State.Idle;
                    }

                    m_iFYSim.GetF10().Value = true;
                    m_iFYSim.GetF11().Value = true;

                    if (m_iFYSim.GetTrackPower().Value == true)
                    {
                        m_iFYSim.GetBlock7().Value = true;
                    }
                    else
                    {
                        m_iFYSim.GetBlock7().Value = false;
                    }

                    if (m_iFYSim.GetBlock7In().Value == false && m_iFYSim.GetBlock8A().Value == false && m_iFYSim.GetTrackPower().Value == true)// check if train may leave and if block 8 is free and track is powered (coupled)
                    {
                        FYSimTrainState = State.TrainDriveToBlock8A;
                    }
                    if (kicksimtrain == "Track_No" && SimTrainLocation != TrackNoToTrackString(val))
                    {
                        FYSimTrainState = State.Idle;
                    }
                    break;

                case State.TrainDriveToBlock8A:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }

                    if (m_iFYSim.GetBlock7In().Value == false && m_iFYSim.GetTrackPower().Value == true && ActionCounter < 50)
                    {
                        ActionCounter++;
                    }
                    else if (ActionCounter >= 50)
                    {
                        ActionCounter++;
                    }

                    if (ActionCounter >= 10 && ActionCounter < 15)
                    {
                        m_iFYSim.GetF11().Value = false;
                    }

                    if (ActionCounter >= 15 && ActionCounter < 75)
                    {
                        m_iFYSim.GetF12().Value = true;
                    }
                    else
                    {
                        m_iFYSim.GetF12().Value = false;
                    }

                    if (ActionCounter >= 50 && ActionCounter < 75)
                    {
                        SimTrainLocation = "Block8A";                   // train drives into block 8A, tail is still in block7
                        m_iFYSim.GetBlock8A().Value = true;
                    }

                    if (ActionCounter > 75)
                    {
                        m_iFYSim.GetBlock7().Value = false;             // train has left block 7, also passed F10
                        m_iFYSim.GetF10().Value = false;
                    }

                    if (ActionCounter >= 100)
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }
                    break;

                case State.TrainInBlock8A:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }

                    ActionCounter++;
                    if (ActionCounter >= 50)
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainDriveToBuffer;
                    }
                    break;

                case State.TrainDriveToBuffer:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }

                    SimTrainLocation = "Buffer";                        // train drives into buffer
                    m_iFYSim.GetBlock8A().Value = false;
                    FYSimTrainState = State.TrainInBuffer;
                    break;

                case State.TrainInBuffer:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }

                    ActionCounter++;
                    if (ActionCounter >= 100)
                    {
                        ActionCounter = 0;
                        if (m_iFYSim.GetBlock5B().Value == true)
                        {
                            FYSimTrainState = State.TrainInBuffer;
                        }
                        else 
                        {
                            SimTrainLocation = "Block5B";
                            m_iFYSim.GetBlock5B().Value = true;
                            FYSimTrainState = State.TrainInBlock5B;
                        }
                    }
                    break;

                case State.TrainInBlock5B:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }

                    if (m_iFYSim.GetBlock5BIn().Value == true) // when block occupied signal is set by application trains stops moving in block 5B
                    {
                        break;
                    }
                    else if (m_iFYSim.GetBlock6().Value == false)// check if block6 is not occupied
                    {
                        FYSimTrainState = State.TrainDriveToBlock6;
                    }
                    break;

                case State.TrainDriveToBlock6:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }

                    if (m_iFYSim.GetBlock6In().Value != true)
                    {
                        ActionCounter++;// when block occupied signal is set by application trains stops moving in block 6, block 5B is also still occupied by the length of the train
                    }
                    
                    //if (ActionCounter >= 0)     // train drives up to isolation barrier just into block6 and stops if occupied input is high on block6
                    //{/   
                        m_iFYSim.GetBlock6().Value = true;
                        SimTrainLocation = "Block6";
                    //}

                    if (ActionCounter >= 50)
                    {
                        m_iFYSim.GetBlock5B().Value = false;
                        m_iFYSim.GetF10().Value = true;
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainInBlock6;
                    }
                    break;

                case State.TrainInBlock6:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                    }

                    if (m_iFYSim.GetBlock7().Value == false && m_iFYSim.GetTrackNo().Count != 0 && m_iFYSim.GetTrackPower().Value == true)// check if train may drive into block7, if the fiddle yard is aligned and if coupled
                    {
                        FYSimTrainState = State.TrainDriveToBlock7;
                    }
                    break;

                case State.TrainDriveToBlock7:
                    if (kicksimtrain == "Reset")
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;                        
                    }
                    else if (kicksimtrain == "Track_No" && SimTrainLocation != TrackNoToTrackString(val))
                    {
                        //m_iFYSim.int[] TrainsOnFYSim to be updated with new placed train!!!! <----------------------------------############################################
                        ActionCounter = 0;
                        FYSimTrainState = State.Idle;
                        break;
                    }

                    if (m_iFYSim.GetBlock7In().Value == false && m_iFYSim.GetTrackPower().Value == true)
                    {
                        ActionCounter++;// when block occupied signal is set by application trains stops moving in block 7, block 6 is also still occupied by the length of the train
                    }
                    
                    if (ActionCounter >= 0 && ActionCounter < 55)
                    {
                        SimTrainLocation = "Track" + Convert.ToString(m_iFYSim.GetTrackNo().Count);
                        m_iFYSim.GetF13().Value = true;
                        m_iFYSim.GetBlock7().Value = true;                        
                    }
                    else{m_iFYSim.GetF13().Value = false;}

                    if (ActionCounter >= 50)
                    {
                        m_iFYSim.GetF11().Value = true;
                        m_iFYSim.GetBlock6().Value = false;
                    }
                    else{m_iFYSim.GetF11().Value = false;}

                    if (ActionCounter >= 100)
                    {
                        ActionCounter = 0;
                        FYSimTrainState = State.TrainDriveToBlock8A;
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
        public string TrackNoToTrackString(int val)
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
