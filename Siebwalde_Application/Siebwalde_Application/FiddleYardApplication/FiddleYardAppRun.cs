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
    public class FiddleYardAppRun
    {
        private iFiddleYardIOHandle m_iFYIOH;
        private FiddleYardIOHandleVariables m_FYIOHandleVar;             // connect variable to connect to FYIOH class for defined variables
        private FiddleYardApplicationVariables m_FYAppVar;
        private Log2LoggingFile m_FYAppLog;
        private FiddleYardAppTrainDrive FYAppTrainDrive;
        public enum State { Idle, Check5B, TrainDriveIn, Check8A, TrainDriveOut, TrainDriveTrough, TrainDriveTroughPrepare, TrainDriveTroughCleanup};        
        private State State_Machine;
        private bool m_collect = false;
        private bool TrackPower15VDown = true;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardAppRun()
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
        public FiddleYardAppRun(FiddleYardIOHandleVariables FYIOHandleVar, iFiddleYardIOHandle iFYIOH, FiddleYardApplicationVariables FYAppVar, Log2LoggingFile FiddleYardApplicationLogging)
        {
            m_iFYIOH = iFYIOH;
            m_FYIOHandleVar = FYIOHandleVar;
            m_FYAppVar = FYAppVar;
            m_FYAppLog = FiddleYardApplicationLogging;
            FYAppTrainDrive = new FiddleYardAppTrainDrive(m_FYIOHandleVar, m_iFYIOH, m_FYAppVar, m_FYAppLog);
            State_Machine = State.Idle;

            Message Msg_TrackPower15VDown = new Message("TrackPower15VDown", " TrackPower15VDown ", (name, log) => SetMessage(name, 0, log)); // initialize and subscribe readback action, Message
            m_FYIOHandleVar.TrackPower15VDown.Attach(Msg_TrackPower15VDown);
            Sensor Sns_TrackPower15V = new Sensor("15VTrackPower", " 15V Track Power ", 0, (name, val, log) => SetMessage(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.TrackPower15V.Attach(Sns_TrackPower15V);
            Command Act_Collect = new Command("Collect", (name) => SetMessage(name, 0, "")); // initialize and subscribe Commands
            m_FYAppVar.FormCollect.Attach(Act_Collect);
        }

        public void SetMessage(string name, int val, string log)
        {
            if (name == "TrackPower15VDown")
            {
                TrackPower15VDown = true;
                m_FYAppLog.StoreText("FYAppRun.Run() TrackPower15VDown = true");
                m_FYAppVar.EMOPressed15VTrackPowerDown.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "EMO pressed, 15V Track Power down!");
            }
            else if (name == "15VTrackPower" && val > 0)
            {
                TrackPower15VDown = false;
                m_FYAppLog.StoreText("FYAppRun.Run() TrackPower15VDown = false");
                m_FYAppLog.StoreText("FYAppRun.Run() Wait aditional time for signals to arrive: System.Threading.Thread.Sleep(500)");
                System.Threading.Thread.Sleep(500);
                m_FYAppVar.EMOPressed15VTrackPowerUp.UpdateMessage();//m_iFYApp.GetFYApp().FYFORM.SetMessage("FYAppInit", "EMO released, 15V Track Power up!");
            }
            else if (name == "Collect")
            {
                m_collect = !m_collect;
                m_FYAppLog.StoreText("FYAppRun.Run() m_collect = " + Convert.ToString(m_collect));
            }
        }


        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardAppRun()
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
        public void FiddleYardAppRunReset()
        {            
            State_Machine = State.Idle;
            FYAppTrainDrive.FiddleYardAppTrainDriveReset();
        } 

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Run()
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
        public string Run(string kickrun, string StopApplication)
        {
            string _Return = "Running";
                        
            if (TrackPower15VDown == true)
            {
                return _Return;
            }

            switch (State_Machine)
            {
                case State.Idle:
                    if ("Stop" == StopApplication)
                    {
                        State_Machine = State.Idle;
                        _Return = "Stop";
                        m_FYAppLog.StoreText("FYAppRun.Run() Stop == kickrun -> State_Machine = State.Start");
                        m_FYAppLog.StoreText("FYAppRun.Run() _Return = Stop");
                        break;
                    }

                    if (FYFull(State.Check5B) < 11)                                                                               // Always drive trains into FiddleYard regardless the status of m_collect until FYFull == 11
                    {
                        State_Machine = State.Check5B;                                                              // alway scheck 5B first, when no train is present, check then 8B
                        //m_FYAppLog.StoreText("FYAppRun.Run() FYFull() < 10 -> State_Machine = State.Check5B");
                    }
                    else if (true == m_collect && m_FYAppVar.bBlock5B)                                      // When the FiddleYard is full, but m_collect is true and a train appears on 5B, then shift-pass trains
                    {
                        State_Machine = State.Check8A;
                        //m_FYAppLog.StoreText("FYAppRun.Run() false == m_collect && FYFull() > 0 -> State_Machine = State.Check8A");
                    }
                    else if (false == m_collect && FYFull(State.Check8A) > 0)                                                    // When the FiddleYard is full, but m_collect is false, then check if a train may leave
                    {
                        State_Machine = State.Check8A;
                        //m_FYAppLog.StoreText("FYAppRun.Run() false == m_collect && FYFull() > 0 -> State_Machine = State.Check8A");
                    }
                    break;

                case State.Check5B:
                    if (m_FYAppVar.bBlock5B && FYFull(State.Check5B) < 11)
                    {
                        State_Machine = State.TrainDriveIn;
                        m_FYAppLog.StoreText("FYAppRun.Run() State_Machine = State.Check5B -> State_Machine = State.TrainDriveIn");//<-----------------------------------------------------------------Send to FORM!!!
                    }
                    else if (false == m_collect)
                    { 
                        State_Machine = State.Check8A; 
                    }
                    else
                    {
                        State_Machine = State.Idle;
                    }
                    break;

                case State.Check8A:
                    if (FYFull(State.Check8A) > 0 && m_FYAppVar.bBlock8A == false)
                    {
                        State_Machine = State.TrainDriveOut;
                        m_FYAppLog.StoreText("FYAppRun.Run() State_Machine = State.Check8A -> State_Machine = State.TrainDriveOut");//<-----------------------------------------------------------------Send to FORM!!!
                    }
                    else { State_Machine = State.Idle; }
                    break;

                case State.TrainDriveIn:
                    if (FYAppTrainDrive.TrainDriveIn(kickrun) == "Finished")
                    {
                        m_FYAppLog.StoreText("FYAppRun.Run() FYAppTrainDrive.TrainDriveIn(kickrun) == Finished");//<-----------------------------------------------------------------Send to FORM!!!
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppRun.Run() State_Machine = State.Idle");
                    }
                    break;

                case State.TrainDriveOut:
                    if (FYAppTrainDrive.TrainDriveOut(kickrun) == "Finished")
                    {
                        m_FYAppLog.StoreText("FYAppRun.Run() FYAppTrainDrive.TrainDriveOut(kickrun) == Finished");//<-----------------------------------------------------------------Send to FORM!!!
                        State_Machine = State.Idle;
                        m_FYAppLog.StoreText("FYAppRun.Run() State_Machine = State.Idle");
                    }
                    break;

                default: 
                    break;
            }

            return _Return;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FYFull
         *               This method will check how many trains there are on the 
         *               fiddle yard
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
        public int FYFull(State state)
        {
            int TrainTotal = 0;

            for (int i = 1; i < 12; i++)
            {
                if (state == State.Check5B && (m_FYAppVar.iTrainsOnFY[i] == 1 || m_FYAppVar.icheckBoxTrack[i] == 1))
                {
                    TrainTotal++;
                }
                else if (state == State.Check8A && (m_FYAppVar.iTrainsOnFY[i] == 1 && m_FYAppVar.icheckBoxTrack[i] == 0))
                {
                    TrainTotal++;
                }
            }
            return TrainTotal;
        }
    }
}
