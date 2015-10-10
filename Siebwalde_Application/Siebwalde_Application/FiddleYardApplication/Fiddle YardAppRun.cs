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
        public iFiddleYardApplication m_iFYApp;
        public FiddleYardAppTrainDrive FYAppTrainDrive;
        private enum State { Idle, Check5B, TrainDriveIn, Check8A, TrainDriveOut, TrainDriveTrough, TrainDriveTroughPrepare, TrainDriveTroughCleanup};
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
        public FiddleYardAppRun(iFiddleYardApplication iFYApp)
        {
            m_iFYApp = iFYApp;
            FYAppTrainDrive = new FiddleYardAppTrainDrive(m_iFYApp);
            State_Machine = State.Idle;

            Message Msg_TrackPower15VDown = new Message("TrackPower15VDown", " TrackPower15VDown ", (name, log) => SetMessage(name, 0, log)); // initialize and subscribe readback action, Message
            m_iFYApp.GetFYApp().m_iFYIOH.GetIoHandler().TrackPower15VDown.Attach(Msg_TrackPower15VDown);
            Sensor Sns_TrackPower15V = new Sensor("15VTrackPower", " 15V Track Power ", 0, (name, val, log) => SetMessage(name, val, log)); // initialize and subscribe sensors
            m_iFYApp.GetFYApp().m_iFYIOH.GetIoHandler().TrackPower15V.Attach(Sns_TrackPower15V);
            Command Act_Collect = new Command("Collect", (name) => SetMessage(name, 0, "")); // initialize and subscribe Commands
            m_iFYApp.GetFYApp().FYFORM.Collect.Attach(Act_Collect);
        }

        public void SetMessage(string name, int val, string log)
        {
            if (name == "TrackPower15VDown")
            {
                TrackPower15VDown = true;
                m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() TrackPower15VDown = true");
            }
            else if (name == "15VTrackPower" && val > 0)
            {
                TrackPower15VDown = false;
                m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() TrackPower15VDown = false");
            }
            else if (name == "Collect")
            {
                m_collect = !m_collect;
                m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() m_collect = " + Convert.ToString(m_collect));
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
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() Stop == kickrun -> State_Machine = State.Start");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() _Return = Stop");
                        break;
                    }

                    /*
                    if (true == m_collect && FYFull() > 10)
                    {                        
                        State_Machine = State.TrainDriveTroughPrepare;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() true == m_collect && FYFull() > 10 -> State_Machine = State.TrainDriveTroughPrepare");
                    }*/

                    if (FYFull() < 11)                                                                               // Always drive trains into FiddleYard regardless the status of m_collect until FYFull == 11
                    {
                        State_Machine = State.Check5B;                                                              // alway scheck 5B first, when no train is present, check then 8B
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() FYFull() < 10 -> State_Machine = State.Check5B");
                    }
                    else if (true == m_collect && m_iFYApp.GetFYApp().Block5B)                                      // When the FiddleYard is full, but m_collect is true and a train appears on 5B, then shift-pass trains
                    {
                        State_Machine = State.Check8A;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() false == m_collect && FYFull() > 0 -> State_Machine = State.Check8A");
                    }
                    else if (false == m_collect && FYFull() > 0)                                                    // When the FiddleYard is full, but m_collect is false, then check if a train may leave
                    {
                        State_Machine = State.Check8A;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() false == m_collect && FYFull() > 0 -> State_Machine = State.Check8A");
                    }
                    break;

                case State.Check5B:
                    if (m_iFYApp.GetFYApp().Block5B && FYFull() < 11)
                    {
                        State_Machine = State.TrainDriveIn;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() GetBlock5B() -> State_Machine = State.TrainDriveIn");//<-----------------------------------------------------------------Send to FORM!!!
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
                    if (FYFull() > 0 && m_iFYApp.GetFYApp().Block8A == false)
                    {
                        State_Machine = State.TrainDriveOut;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() GetBlock8A() -> State_Machine = State.TrainDriveOut");//<-----------------------------------------------------------------Send to FORM!!!
                    }
                    else { State_Machine = State.Idle; }
                    break;

                case State.TrainDriveTroughPrepare:
                    if (FYAppTrainDrive.TrainDriveThrough(kickrun) == "Finished")
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() State_Machine = State.TrainDriveTrough");
                        State_Machine = State.TrainDriveTrough;
                    }
                    break;

                case State.TrainDriveTrough:
                    if (false == m_collect)
                    {                        
                        State_Machine = State.TrainDriveTroughCleanup;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() false == m_collect -> State_Machine = State.TrainDriveTroughCleanup");
                    }
                    else if ("Stop" == StopApplication)
                    {
                        FYAppTrainDrive.FiddleYardAppTrainDriveReset();
                        State_Machine = State.Idle;
                        _Return = "Stop";
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() Stop == kickrun -> State_Machine = State.Start");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() _Return = Stop");
                    }
                    break;

                case State.TrainDriveTroughCleanup:
                    if (FYAppTrainDrive.TrainDriveThrough("CollectFalse") == "Finished")
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() State_Machine = State.Idle");
                        State_Machine = State.Idle;
                    }
                    break;


                case State.TrainDriveIn:
                    if (FYAppTrainDrive.TrainDriveIn(kickrun) == "Finished")
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() FYAppTrainDrive.TrainDriveIn(kickrun) == Finished");//<-----------------------------------------------------------------Send to FORM!!!
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() State_Machine = State.Idle");
                    }
                    break;

                case State.TrainDriveOut:
                    if (FYAppTrainDrive.TrainDriveOut(kickrun) == "Finished")
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() FYAppTrainDrive.TrainDriveOut(kickrun) == Finished");//<-----------------------------------------------------------------Send to FORM!!!
                        State_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppRun.Run() State_Machine = State.Idle");
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
        public int FYFull()
        {
            int TrainTotal = 0;

            for (int i = 1; i < 12; i++)
            {
                if (m_iFYApp.GetFYApp().TrainsOnFY[i] == 1)
                {
                    TrainTotal++;
                }
            }
            return TrainTotal;
        }
    }
}
