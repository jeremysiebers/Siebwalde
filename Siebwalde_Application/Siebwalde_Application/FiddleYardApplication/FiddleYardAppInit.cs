using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    public class FiddleYardAppInit
    {        
        private FiddleYardIOHandleVariables m_FYIOHandleVar;             // connect variable to connect to FYIOH class for defined variables
        private FiddleYardApplicationVariables m_FYAppVar;
        private Log2LoggingFile m_FYAppLog;
        private MessageUpdater FiddleYardInitStarted;

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
        public FiddleYardAppInit(FiddleYardIOHandleVariables FYIOHandleVar, FiddleYardApplicationVariables FYAppVar, Log2LoggingFile FiddleYardApplicationLogging)
        {            
            m_FYIOHandleVar = FYIOHandleVar;
            m_FYAppVar = FYAppVar;
            m_FYAppLog = FiddleYardApplicationLogging;
            FiddleYardInitStarted = new MessageUpdater();
            State_Machine = State.Idle;
            Message Msg_uControllerReady = new Message("uControllerReady", " uControllerReady ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYIOHandleVar.uControllerReady.Attach(Msg_uControllerReady);    
        }

        public void SetMessage(string name, string log)
        {
            uControllerReady = true;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardInitReset()
         *               Reset
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
        public void FiddleYardInitReset()
        {            
            State_Machine = State.Idle; 
            WaitCnt = 0;
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
                        m_FYAppLog.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                        break;
                    }
                    
                    m_FYAppLog.StoreText("FYAppInit.Init() started");
                    if (m_FYAppVar.GetTrackNr() > 0 && !m_FYAppVar.bF12 && !m_FYAppVar.bF13)
                    {                        
                        State_Machine = State.Situation1;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Situation1");
                    }
                    else 
                    { 
                        State_Machine = State.Situation2;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Situation2");
                    }
                    break;



                case State.Situation1:
                    m_FYAppLog.StoreText("FYAppInit.Init() Start Train Detection");
                    m_FYAppVar.TrainDetect.UpdateActuator();// m_iFYApp.Cmd(" TrainDetect ","");                    
                    State_Machine = State.TrainDetection;
                    m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.TrainDetection");
                    break;



                case State.Situation2:
                    if (m_FYAppVar.GetTrackNr() == 0)
                    {
                        m_FYAppLog.StoreText("FYAppInit.Init() m_iFYApp.GetTrackNr() == 0");
                        State_Machine = State.TrackNotAligned;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.TrackNotAligned");
                        m_FYAppVar.FiddleYardTrackNotAligned.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard track not aligned..."); // create event for text to form, if form does not exist no error will occur, via FYAppVar
                    }
                    else if (m_FYAppVar.bF10 && (m_FYAppVar.bBlock6 || m_FYAppVar.bF12 || m_FYAppVar.bF13))
                    {
                        m_FYAppLog.StoreText("FYAppInit.Init() m_iFYApp.GetF10() = " + Convert.ToString(m_FYAppVar.F10));
                        m_FYAppLog.StoreText("FYAppInit.Init() m_iFYApp.GetF12() = " + Convert.ToString(m_FYAppVar.F12));
                        m_FYAppLog.StoreText("FYAppInit.Init() m_iFYApp.GetF13() = " + Convert.ToString(m_FYAppVar.F13));
                        m_FYAppLog.StoreText("FYAppInit.Init() m_iFYApp.GetBlock6() = " + Convert.ToString(m_FYAppVar.Block6));
                        uControllerReady = false;
                        m_FYAppVar.Couple.UpdateActuator();//m_iFYApp.Cmd(" Couple ", "");
                        m_FYAppLog.StoreText("FYAppInit.Init() Couple");
                        State_Machine = State.Situation2_1;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Situation2_1");
                        m_FYAppVar.FiddleYardTrainObstruction.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard train obstruction...");
                    }                    
                    break;

                case State.Situation2_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnFalse.UpdateActuator();//m_iFYApp.Cmd(" Occ6OnFalse ", "");
                        m_FYAppLog.StoreText("FYAppInit.Init() Occ6OnFalse");
                        State_Machine = State.Situation2_2;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Situation2_2");
                    }
                    break;

                case State.Situation2_2:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnFalse.UpdateActuator();//m_iFYApp.Cmd(" Occ7OnFalse ", "");
                        m_FYAppLog.StoreText("FYAppInit.Init() Occ7OnFalse");
                        State_Machine = State.Situation2_3;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Situation2_3");
                    }
                    break;

                case State.Situation2_3:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        State_Machine = State.TrainObstruction;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.TrainObstruction");
                    }
                    break;

                case State.TrackNotAligned:
                    if (Direction == "Left")
                    {
                        m_FYAppVar.FiddleOneLeft.UpdateActuator();//m_iFYApp.Cmd(" FiddleOneLeft ", "");
                        m_FYAppLog.StoreText("FYAppInit.Init() m_iFYApp.Cmd(FiddleOneLeft, __)");
                        State_Machine = State.FiddleOneLeftRight;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.FiddleOneLeft");
                    }
                    else if (Direction == "Right")
                    {
                        m_FYAppVar.FiddleOneRight.UpdateActuator();//m_iFYApp.Cmd(" FiddleOneRight ", "");
                        m_FYAppLog.StoreText("FYAppInit.Init() m_iFYApp.Cmd(FiddleOneRight, __)");
                        State_Machine = State.FiddleOneLeftRight;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.FiddleOneLeftRight");
                    }
                    break;



                case State.FiddleOneLeftRight:
                    if (kickInit == "FiddleOneLeftFinished" && Direction == "Left")
                    {
                        Direction = "Right";
                        m_FYAppLog.StoreText("FYAppInit.Init() kickapplication == FiddleOneLeftFinished"); //<---------------------------------------add here checks on EOS10 and EOS11!!!
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle; Moved and aligned to track, try again to init");
                        m_FYAppVar.FiddleYardTrackAligned.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard track aligned");
                    }
                    else if (kickInit == "FiddleOneRightFinished" && Direction == "Right")
                    {
                        Direction = "Left";
                        m_FYAppLog.StoreText("FYAppInit.Init() kickapplication == FiddleOneRightFinished"); //<---------------------------------------add here checks on EOS10 and EOS11!!!
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle; Moved and aligned to track, try again to init");
                        m_FYAppVar.FiddleYardTrackAligned.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard track aligned");
                    }
                    else if (" Reset " == kickInit)
                    {
                        m_FYAppLog.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;



                case State.TrainObstruction:
                    if (!m_FYAppVar.bF10 && !m_FYAppVar.bF11 && !m_FYAppVar.bF12 && !m_FYAppVar.bF13 && !m_FYAppVar.bBlock6 && !m_FYAppVar.bBlock7)
                    {
                        m_FYAppLog.StoreText("!m_iFYApp.GetF10() && !m_iFYApp.GetF11() && !m_iFYApp.GetF12() && !m_iFYApp.GetF13() && !m_iFYApp.GetBlock6() && !m_iFYApp.GetBlock7()");
                        m_FYAppLog.StoreText("Train has left FiddleYard successfully");
                        m_FYAppVar.TrainHasLeftFiddleYardSuccessfully.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "Train has left FiddleYard successfully");
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnTrue.UpdateActuator();//m_iFYApp.Cmd(" Occ6OnTrue ", "");
                        m_FYAppLog.StoreText("FYAppInit.Init() Occ6OnTrue");
                        State_Machine = State.TrainObstruction_1;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.TrainObstruction_1");
                    }
                    else if ("Reset" == kickInit)
                    {
                        m_FYAppLog.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;

                case State.TrainObstruction_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnTrue.UpdateActuator();//m_iFYApp.Cmd(" Occ7OnTrue ", "");
                        m_FYAppLog.StoreText("FYAppInit.Init() Occ7OnTrue");
                        State_Machine = State.TrainObstruction_2;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.TrainObstruction_2");
                    }
                    break;

                case State.TrainObstruction_2:
                    if (uControllerReady == true)
                    {
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle, try to init again.");
                    }
                    break;


                case State.TrainDetection:
                    if (kickInit == "TrainDetectionFinished")
                    {
                        m_FYAppLog.StoreText("FYAppInit.Init()  Train Detection Finished received");
                        State_Machine = State.WaitTargetUpdateTrack;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.WaitTargetUpdateTrack");                        
                    }
                    else if (" Reset " == kickInit)
                    {
                        m_FYAppLog.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                    }
                    break;

                case State.WaitTargetUpdateTrack:
                    WaitCnt++;
                    if (WaitCnt > 10)
                    {
                        m_FYAppLog.StoreText("FYAppInit.Init()  WaitCnt > 10");
                        m_FYAppVar.TrackTrainsOnFYUpdater();
                        m_FYAppLog.StoreText("FYAppInit.Init()  Update FYFORM Tracks to display in correct color: TrackTrainsOnFYUpdater()");
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle");
                        _Return = "Finished";
                        m_FYAppLog.StoreText("FYAppInit.Init() _Return = Finished");
                        WaitCnt = 0;
                    }
                    else if (" Reset " == kickInit)
                    {
                        m_FYAppLog.StoreText("FYAppInit.Init() Reset == kickInit");
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppInit.Init() State_Machine = State.Idle");
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
