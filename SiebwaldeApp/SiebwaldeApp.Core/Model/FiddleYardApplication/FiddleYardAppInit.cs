using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SiebwaldeApp;
using SiebwaldeApp.Core;

namespace SiebwaldeApp.Core
{
    public class FiddleYardAppInit
    {        
        private FiddleYardIOHandleVariables m_FYIOHandleVar;             // connect variable to connect to FYIOH class for defined variables
        private FiddleYardApplicationVariables m_FYAppVar;
        private FiddleYardMip50 m_FYMIP50;
        private FiddleYardTrainDetection m_FYTDT;
        private MessageUpdater FiddleYardInitStarted;
        private string LoggerInstance { get; set; }

        private enum State
        {
            Idle, Situation1, Situation2, TrainDetection, TrackNotAligned, TrainObstruction, FiddleOneLeftRight, WaitTargetUpdateTrack,
            Situation2_1, Situation2_2, Situation2_3, TrainObstruction_1, TrainObstruction_2, FYHOME
        };
        private State State_Machine;        
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
        public FiddleYardAppInit(FiddleYardIOHandleVariables FYIOHandleVar, 
            FiddleYardApplicationVariables FYAppVar, 
            FiddleYardMip50 FYMIP50, 
            FiddleYardTrainDetection FYTDT, 
            string loggerInstance)
        {            
            m_FYIOHandleVar = FYIOHandleVar;
            m_FYAppVar = FYAppVar;
            m_FYMIP50 = FYMIP50;
            m_FYTDT = FYTDT;
            LoggerInstance = loggerInstance;
            FiddleYardInitStarted = new MessageUpdater();
            State_Machine = State.Idle;
            Message Msg_uControllerReady = new Message("uControllerReady", " uControllerReady ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYIOHandleVar.uControllerReady.Attach(Msg_uControllerReady);    
        }

        public void SetMessage(string name, string log)
        {
            uControllerReady = true;
        }


        /// <summary>
        /// Reset command received reset state machine
        /// </summary>
        public void FiddleYardInitReset()
        {            
            State_Machine = State.Idle;             
            uControllerReady = true;
        } 


        /// <summary>
        /// This wil try to initialise the Fiddle yard, checking various start conditions and start a train detection
        /// </summary>
        /// <param name="kickInit"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public string Init(string kickInit, int val)
        {
            string _Return = "Busy";
            string SubProgramReturnVal = null;

            switch (State_Machine)
            {
                case State.Idle:

                    if (" Reset " == kickInit)
                    {
                        IoC.Logger.Log("FYAppInit.Init() Reset == kickInit", LoggerInstance);
                        State_Machine = State.Idle;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Idle", LoggerInstance);
                        break;
                    }
                    
                    IoC.Logger.Log("FYAppInit.Init() started", LoggerInstance);
                    if (m_FYAppVar.FYHomed.BoolVariable == true && !m_FYAppVar.bF12 && !m_FYAppVar.bF13)
                    {                        
                        State_Machine = State.TrainDetection;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.TrainDetection", LoggerInstance);
                        IoC.Logger.Log("FYAppInit.Init() Start Train Detection", LoggerInstance);
                    }
                    else 
                    { 
                        State_Machine = State.Situation2;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Situation2", LoggerInstance);
                    }
                    break;

                    

                case State.TrainDetection:
                    SubProgramReturnVal = m_FYTDT.Traindetection();
                    if (SubProgramReturnVal == "Finished")
                    {
                        IoC.Logger.Log("FYAppInit.Init() FYTDT.Traindetection() == Finished", LoggerInstance);
                        State_Machine = State.Idle;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Idle", LoggerInstance);
                        _Return = "Finished";
                        IoC.Logger.Log("FYAppInit.Init() _Return = Finished", LoggerInstance);
                    }                    
                    break;



                case State.Situation2:
                    if (m_FYAppVar.FYHomed.BoolVariable == false && !m_FYAppVar.bF12 && !m_FYAppVar.bF13) // Check if track is aligned instead of checking if HOMED = true, when track is not aligned no train can be in front of F12 or F13, stil check them
                    {
                        IoC.Logger.Log("FYAppInit.Init() m_FYAppVar.FYHomed.BoolVariable == false", LoggerInstance);
                        State_Machine = State.FYHOME;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.FYHOME", LoggerInstance);
                        m_FYAppVar.FiddleYardNotHomed.UpdateMessage();
                    }
                    else if (m_FYAppVar.GetTrackNr() > 0 && m_FYAppVar.bF10 && (m_FYAppVar.bBlock6 || m_FYAppVar.bF12 || m_FYAppVar.bF13))
                    {
                        IoC.Logger.Log("FYAppInit.Init() m_iFYApp.GetF10() = " + Convert.ToString(m_FYAppVar.F10), LoggerInstance);
                        IoC.Logger.Log("FYAppInit.Init() m_iFYApp.GetF12() = " + Convert.ToString(m_FYAppVar.F12), LoggerInstance);
                        IoC.Logger.Log("FYAppInit.Init() m_iFYApp.GetF13() = " + Convert.ToString(m_FYAppVar.F13), LoggerInstance);
                        IoC.Logger.Log("FYAppInit.Init() m_iFYApp.GetBlock6() = " + Convert.ToString(m_FYAppVar.Block6), LoggerInstance);
                        uControllerReady = false;
                        m_FYAppVar.Couple.UpdateActuator();//m_iFYApp.Cmd(" Couple ", "");
                        IoC.Logger.Log("FYAppInit.Init() Couple", LoggerInstance);
                        State_Machine = State.Situation2_1;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Situation2_1", LoggerInstance);
                        m_FYAppVar.FiddleYardTrainObstruction.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "FiddleYard train obstruction...");
                    } 
                    else if (m_FYAppVar.GetTrackNr() == 0)
                    {
                        // if not aligned and trainobstruction, STOP --> put text on FYFORM, user has to solve!!!
                        // Send message
                    }
                    break;

                case State.Situation2_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnFalse.UpdateActuator();//m_iFYApp.Cmd(" Occ6OnFalse ", "");
                        IoC.Logger.Log("FYAppInit.Init() Occ6OnFalse", LoggerInstance);
                        State_Machine = State.Situation2_2;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Situation2_2", LoggerInstance);
                    }
                    break;

                case State.Situation2_2:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnFalse.UpdateActuator();//m_iFYApp.Cmd(" Occ7OnFalse ", "");
                        IoC.Logger.Log("FYAppInit.Init() Occ7OnFalse", LoggerInstance);
                        State_Machine = State.Situation2_3;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Situation2_3", LoggerInstance);
                    }
                    break;

                case State.Situation2_3:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        State_Machine = State.TrainObstruction;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.TrainObstruction", LoggerInstance);
                    }
                    break;

                case State.FYHOME:
                    SubProgramReturnVal = m_FYMIP50.MIP50xHOME();
                    if (SubProgramReturnVal == "Finished")
                    {
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Idle; FY Homed and aligned to track1, try again to init", LoggerInstance);
                        State_Machine = State.Idle;
                    }
                    if (" Reset " == kickInit)
                    {
                        IoC.Logger.Log("FYAppInit.Init() Reset == kickInit", LoggerInstance);
                        State_Machine = State.Idle;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Idle", LoggerInstance);
                    }
                    break;

                case State.TrainObstruction:
                    if (!m_FYAppVar.bF10 && !m_FYAppVar.bF11 && !m_FYAppVar.bF12 && !m_FYAppVar.bF13 && !m_FYAppVar.bBlock6 && !m_FYAppVar.bBlock7)
                    {
                        IoC.Logger.Log("!m_iFYApp.GetF10() && !m_iFYApp.GetF11() && !m_iFYApp.GetF12() && !m_iFYApp.GetF13() && !m_iFYApp.GetBlock6() && !m_iFYApp.GetBlock7()", LoggerInstance);
                        IoC.Logger.Log("Train has left FiddleYard successfully", LoggerInstance);
                        m_FYAppVar.TrainHasLeftFiddleYardSuccessfully.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "Train has left FiddleYard successfully");
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnTrue.UpdateActuator();//m_iFYApp.Cmd(" Occ6OnTrue ", "");
                        IoC.Logger.Log("FYAppInit.Init() Occ6OnTrue", LoggerInstance);
                        State_Machine = State.TrainObstruction_1;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.TrainObstruction_1", LoggerInstance);
                    }
                    else if ("Reset" == kickInit)
                    {
                        IoC.Logger.Log("FYAppInit.Init() Reset == kickInit", LoggerInstance);
                        State_Machine = State.Idle;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Idle", LoggerInstance);
                    }
                    break;

                case State.TrainObstruction_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnTrue.UpdateActuator();//m_iFYApp.Cmd(" Occ7OnTrue ", "");
                        IoC.Logger.Log("FYAppInit.Init() Occ7OnTrue", LoggerInstance);
                        State_Machine = State.TrainObstruction_2;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.TrainObstruction_2", LoggerInstance);
                    }
                    break;

                case State.TrainObstruction_2:
                    if (uControllerReady == true)
                    {
                        State_Machine = State.Idle;
                        IoC.Logger.Log("FYAppInit.Init() State_Machine = State.Idle, try to init again.", LoggerInstance);
                    }
                    break;
                    
                default:
                    break;
            }

            return _Return;
        }
    }
}
