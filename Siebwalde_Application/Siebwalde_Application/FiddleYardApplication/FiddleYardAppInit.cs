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

        private enum State
        {
            Idle, Situation1, Situation2, TrainDetection, TrackNotAligned, TrainObstruction, FiddleOneLeftRight, WaitTargetUpdateTrack, Situation2_1, Situation2_2, Situation2_3, TrainObstruction_1, TrainObstruction_2,
        };
        private State State_Machine;
        private string Direction = "Left";
        private int WaitCnt = 0;
        private bool uControllerReady = true;
        
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
            Message Msg_uControllerReady = new Message("uControllerReady", " uControllerReady ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYApp.GetFYApp().m_iFYIOH.GetIoHandler().uControllerReady.Attach(Msg_uControllerReady);    
        }

        public void SetMessage(string name, string log)
        {
            uControllerReady = true;
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
        public string Init(string kickInit, int val)
        {
            string _Return = "Busy";
            switch (State_Machine)
            {
                case State.Idle:

                    if (" Reset " == kickInit)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                        break;
                    }
                    
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() started");
                    if (m_iFYApp.GetFYApp().GetTrackNr() > 0 && !m_iFYApp.GetFYApp().F12 && !m_iFYApp.GetFYApp().F13)
                    {                        
                        State_Machine = State.Situation1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Situation1");
                    }
                    else 
                    { 
                        State_Machine = State.Situation2;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Situation2");
                    }
                    break;



                case State.Situation1:
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Start Train Detection");
                    m_iFYApp.Cmd(" TrainDetect ","");                    
                    State_Machine = State.TrainDetection;
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.TrainDetection");
                    break;



                case State.Situation2:
                    if (m_iFYApp.GetFYApp().GetTrackNr() == 0)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() m_iFYApp.GetTrackNr() == 0");
                        State_Machine = State.TrackNotAligned;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.TrackNotAligned");
                        m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard track not aligned...");
                    }
                    else if (m_iFYApp.GetFYApp().F10 && (m_iFYApp.GetFYApp().Block6 || m_iFYApp.GetFYApp().F12 || m_iFYApp.GetFYApp().F13))
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() m_iFYApp.GetF10() = " + Convert.ToString(m_iFYApp.GetFYApp().F10));
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() m_iFYApp.GetF12() = " + Convert.ToString(m_iFYApp.GetFYApp().F12));
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() m_iFYApp.GetF13() = " + Convert.ToString(m_iFYApp.GetFYApp().F13));
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() m_iFYApp.GetBlock6() = " + Convert.ToString(m_iFYApp.GetFYApp().Block6));
                        uControllerReady = false;
                        m_iFYApp.Cmd(" Couple ", "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Couple");
                        State_Machine = State.Situation2_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Situation2_1");
                        m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard train obstruction...");
                    }                    
                    break;

                case State.Situation2_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.Cmd(" Occ6OnFalse ", "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Occ6OnFalse");
                        State_Machine = State.Situation2_2;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Situation2_2");
                    }
                    break;

                case State.Situation2_2:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.Cmd(" Occ7OnFalse ", "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Occ7OnFalse");
                        State_Machine = State.Situation2_3;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Situation2_3");
                    }
                    break;

                case State.Situation2_3:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        State_Machine = State.TrainObstruction;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.TrainObstruction");
                    }
                    break;

                case State.TrackNotAligned:
                    if (Direction == "Left")
                    {
                        m_iFYApp.Cmd(" FiddleOneLeft ", "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() m_iFYApp.Cmd(FiddleOneLeft, __)");
                        State_Machine = State.FiddleOneLeftRight;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.FiddleOneLeft");
                    }
                    else if (Direction == "Right")
                    {
                        m_iFYApp.Cmd(" FiddleOneRight ", "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() m_iFYApp.Cmd(FiddleOneRight, __)");
                        State_Machine = State.FiddleOneLeftRight;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.FiddleOneLeftRight");
                    }
                    break;



                case State.FiddleOneLeftRight:
                    if (kickInit == "FiddleOneLeftFinished" && Direction == "Left")
                    {
                        Direction = "Right";
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() kickapplication == FiddleOneLeftFinished"); //<---------------------------------------add here checks on EOS10 and EOS11!!!
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle; Moved and aligned to track, try again to init");
                        m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard track aligned");
                    }
                    else if (kickInit == "FiddleOneRightFinished" && Direction == "Right")
                    {
                        Direction = "Left";
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() kickapplication == FiddleOneRightFinished"); //<---------------------------------------add here checks on EOS10 and EOS11!!!
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle; Moved and aligned to track, try again to init");
                        m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard track aligned");
                    }
                    else if (" Reset " == kickInit)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;



                case State.TrainObstruction:
                    if (!m_iFYApp.GetFYApp().F10 && !m_iFYApp.GetFYApp().F11 && !m_iFYApp.GetFYApp().F12 && !m_iFYApp.GetFYApp().F13 && !m_iFYApp.GetFYApp().Block6 && !m_iFYApp.GetFYApp().Block7)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("!m_iFYApp.GetF10() && !m_iFYApp.GetF11() && !m_iFYApp.GetF12() && !m_iFYApp.GetF13() && !m_iFYApp.GetBlock6() && !m_iFYApp.GetBlock7()");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("Train has left FiddleYard successfully");
                        m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "Train has left FiddleYard successfully");
                        uControllerReady = false;
                        m_iFYApp.Cmd(" Occ6OnTrue ", "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Occ6OnTrue");
                        State_Machine = State.TrainObstruction_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.TrainObstruction_1");
                    }
                    else if ("Reset" == kickInit)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;

                case State.TrainObstruction_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.Cmd(" Occ7OnTrue ", "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Occ7OnTrue");
                        State_Machine = State.TrainObstruction_2;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.TrainObstruction_2");
                    }
                    break;

                case State.TrainObstruction_2:
                    if (uControllerReady == true)
                    {
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle, try to init again.");
                    }
                    break;


                case State.TrainDetection:
                    if (kickInit == "TrainDetectionFinished")
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init()  Train Detection Finished received");
                        State_Machine = State.WaitTargetUpdateTrack;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.WaitTargetUpdateTrack");                        
                    }
                    else if (" Reset " == kickInit)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;

                case State.WaitTargetUpdateTrack:
                    WaitCnt++;
                    if (WaitCnt > 10)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init()  WaitCnt > 10");
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                        _Return = "Finished";
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() _Return = Finished");
                        WaitCnt = 0;
                    }
                    else if (" Reset " == kickInit)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                        WaitCnt = 0;
                    }
                    break;

                default:
                    break;
            }

            return _Return;
        }
    }
}
