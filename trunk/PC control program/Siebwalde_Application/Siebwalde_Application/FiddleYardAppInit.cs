using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    public class FiddleYardAppInit
    {
        public iFiddleYardApplication m_iFYApp;        
        public MessageUpdater FiddleYardInitStarted;

        private enum State { Idle, Situation1, Situation2, TrainDetection, TrackNotAligned, TrainObstruction, FiddleOneLeftRight };
        private State State_Machine;
        private string Direction = "Left";
        
        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardAppInit()
         *               Constructor
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
         */
        /*#--------------------------------------------------------------------------#*/
        public FiddleYardAppInit(iFiddleYardApplication iFYApp)
        {
            m_iFYApp = iFYApp;            
            FiddleYardInitStarted = new MessageUpdater();
            State_Machine = State.Idle;                  
        }        
        /*#--------------------------------------------------------------------------#*/
        /*  Description: Init()
         *               This wil try to initialise the Fiddle yard, checking various
         *               start conditions and start a train detection
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
        public string Init(string kickInit)
        {
            string _Return = "Busy";
            switch (State_Machine)
            {
                case State.Idle:

                    if ("Reset" == kickInit)
                    {
                        m_iFYApp.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                        break;
                    }
                    FiddleYardInitStarted.UpdateMessage();
                    m_iFYApp.StoreText("FYAppInit.Init() started");
                    if (m_iFYApp.TrackNr() > 0 && !m_iFYApp.Sensor("F12"))
                    {
                        State_Machine = State.Situation1;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Situation1");
                    }
                    else 
                    { 
                        State_Machine = State.Situation2;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Situation2");
                    }
                    break;



                case State.Situation1:
                    m_iFYApp.StoreText("FYAppInit.Init() Start Train Detection");
                    m_iFYApp.Cmd("TrainDetect","");                    
                    State_Machine = State.TrainDetection;
                    m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.TrainDetection");
                    break;



                case State.Situation2:
                    if (m_iFYApp.TrackNr() == 0)
                    {
                        m_iFYApp.StoreText("FYAppInit.Init() m_iFYApp.TrackNr() == 0");
                        State_Machine = State.TrackNotAligned;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.TrackNotAligned");
                    }
                    else if (m_iFYApp.Sensor("F10") && (m_iFYApp.Sensor("Block6") || m_iFYApp.Sensor("F12") || m_iFYApp.Sensor("F13")))
                    {
                        m_iFYApp.StoreText("FYAppInit.Init() m_iFYApp.Sensor(F10) = " + Convert.ToString(m_iFYApp.Sensor("F10")));
                        m_iFYApp.StoreText("FYAppInit.Init() m_iFYApp.Sensor(F12) = " + Convert.ToString(m_iFYApp.Sensor("F12")));
                        m_iFYApp.StoreText("FYAppInit.Init() m_iFYApp.Sensor(F13) = " + Convert.ToString(m_iFYApp.Sensor("F13")));
                        m_iFYApp.StoreText("FYAppInit.Init() m_iFYApp.Sensor(Block6) = " + Convert.ToString(m_iFYApp.Sensor("Block6")));
                        State_Machine = State.TrainObstruction;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.TrainObstruction");
                        m_iFYApp.Cmd("Occ6OnFalse", "");
                        m_iFYApp.StoreText("FYAppInit.Init() Occ6OnFalse");
                        m_iFYApp.Cmd("Occ7OnFalse", "");
                        m_iFYApp.StoreText("FYAppInit.Init() Occ7OnFalse");
                    }                    
                    break;



                case State.TrackNotAligned:
                    if (Direction == "Left")
                    {
                        m_iFYApp.Cmd("FiddleOneLeft", "");
                        m_iFYApp.StoreText("FYAppInit.Init() m_iFYApp.Cmd(FiddleOneLeft, __)");
                        State_Machine = State.FiddleOneLeftRight;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.FiddleOneLeft");
                    }
                    else if (Direction == "Right")
                    {
                        m_iFYApp.Cmd("FiddleOneRight", "");
                        m_iFYApp.StoreText("FYAppInit.Init() m_iFYApp.Cmd(FiddleOneRight, __)");
                        State_Machine = State.FiddleOneLeftRight;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.FiddleOneLeftRight");
                    }
                    break;



                case State.FiddleOneLeftRight:
                    if (kickInit == "FiddleOneLeftFinished" && Direction == "Left")
                    {
                        Direction = "Right";
                        m_iFYApp.StoreText("FYAppInit.Init() kickapplication == FiddleOneLeftFinished");
                        State_Machine = State.Idle;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Idle; Moved and aligned to track, try again to init");
                    }
                    else if (kickInit == "FiddleOneRightFinished" && Direction == "Right")
                    {
                        Direction = "Left";
                        m_iFYApp.StoreText("FYAppInit.Init() kickapplication == FiddleOneRightFinished");
                        State_Machine = State.Idle;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Idle; Moved and aligned to track, try again to init");
                    }
                    else if ("Reset" == kickInit)
                    {
                        m_iFYApp.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;



                case State.TrainObstruction:
                    if (!m_iFYApp.Sensor("F10") && !m_iFYApp.Sensor("F11") && !m_iFYApp.Sensor("F12") && !m_iFYApp.Sensor("F13") && !m_iFYApp.Sensor("Block6") && !m_iFYApp.Sensor("Block7"))
                    {
                        m_iFYApp.StoreText("!m_iFYApp.Sensor(F10) && !m_iFYApp.Sensor(F11) && !m_iFYApp.Sensor(F12) && !m_iFYApp.Sensor(F13) && !m_iFYApp.Sensor(Block6) && !m_iFYApp.Sensor(Block7)");
                        m_iFYApp.StoreText("Train has left successfully");
                        m_iFYApp.Cmd("Occ6OnTrue", "");
                        m_iFYApp.StoreText("FYAppInit.Init() Occ6OnTrue");
                        m_iFYApp.Cmd("Occ7OnTrue", "");
                        m_iFYApp.StoreText("FYAppInit.Init() Occ7OnTrue");
                        State_Machine = State.Idle;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Idle, try to init again.");
                    }
                    else if ("Reset" == kickInit)
                    {
                        m_iFYApp.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;



                case State.TrainDetection:
                    if (kickInit == "TrainDetectionFinished")
                    {
                        m_iFYApp.StoreText("FYAppInit.Init()  Train Detection Finished");
                        State_Machine = State.Idle;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                        _Return = "Finished";
                        m_iFYApp.StoreText("FYAppInit.Init() _Return = Finished");
                    }
                    else if ("Reset" == kickInit)
                    {
                        m_iFYApp.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;



                default:
                    break;
            }

            return _Return;
        }
    }
}
