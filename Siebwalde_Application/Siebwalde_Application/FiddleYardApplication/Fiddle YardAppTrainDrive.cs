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
    public class FiddleYardAppTrainDrive
    {
        public iFiddleYardApplication m_iFYApp;
        private enum State { Start, CheckEmptyTrack, MoveToEmptyTrack, CheckArrivedAtEmptyTrack, CheckTrainInTrack6, CheckTrainF10, CheckTrainInTrack7, CheckTrainF11, CheckTrainStopped, TrainDriveInFailed,
        CheckFullTrack, MoveToFullTrack, CheckArrivedAtFullTrack, CheckTrainLeftTrack7, Idle, Stop, CheckArrivedAtEmptyTrack_1, CheckArrivedAtEmptyTrack_2, CheckTrainInTrack6_1, CheckTrainF10_1, CheckTrainF11_1,
        CheckTrainStopped_1, CheckTrainStopped_2, CheckTrainStopped_3, MoveToFullTrack_1, CheckArrivedAtFullTrack_1, CheckTrainLeftTrack7_1, TrainDriveThrough_1, TrainDriveThrough_2, TrainDriveThrough_3,
        TrainDriveThrough_4, TrainDriveThrough_5, TrainDriveThrough_6
        };
        private State TrainDriveIn_Machine;
        private State TrainDriveOut_Machine;
        private State TrainDriveThrough_Machine;        
        private int TrainDriveInPointer = 1;        // points to a track in which it is required to check if a train is present for traindrive in
        private int TrainDriveOutPointer = 5;       // points to a track in which it is required to check if a train is present for traindrive out  
        private uint TrainDriveDelay = 0;
        private bool uControllerReady = true;
        private uint TRAINDRIVEDELAY = 0;

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardAppTrainDrive()
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
        public FiddleYardAppTrainDrive(iFiddleYardApplication iFYApp)
        {
            m_iFYApp = iFYApp;            
            TrainDriveIn_Machine = State.Start;
            TrainDriveOut_Machine = State.Start;
            TrainDriveThrough_Machine = State.Start;

            Message Msg_uControllerReady = new Message("uControllerReady", " uControllerReady ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYApp.GetFYApp().m_iFYIOH.GetIoHandler().uControllerReady.Attach(Msg_uControllerReady);            
        }

        public void SetMessage(string name, string log)
        {
            uControllerReady = true;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardAppTrainDriveReset()
         *               
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
        public void FiddleYardAppTrainDriveReset()
        {            
            TrainDriveIn_Machine = State.Start;
            TrainDriveOut_Machine = State.Start;
            TrainDriveThrough_Machine = State.Start;
            TrainDriveInPointer = 1;
            TrainDriveOutPointer = 5;
            TrainDriveDelay = 0;
        }


        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrainDriveIn()
         *               This wil drive trains into the Fiddle Yard
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
        public string TrainDriveIn(string kickTrainDriveIn)
        {
            string _Return = "Running";

            switch (TrainDriveIn_Machine)
            {
                case State.Start:                    
                    TrainDriveInPointer = m_iFYApp.GetFYApp().GetTrackNr();        // Driving trains into the fiddle yard may be done without memorizeing, driving out is done in Fi-La
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveInPointer = m_iFYApp.GetTrackNr() = " + Convert.ToString(m_iFYApp.GetFYApp().GetTrackNr()));
                    TrainDriveIn_Machine = State.CheckEmptyTrack;
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckEmptyTrack");

                    if (m_iFYApp.GetFYApp().m_iFYIOH.GetIoHandler().m_FYSimulatorActive == true)
                    {
                        TRAINDRIVEDELAY = 10;
                    }
                    else { TRAINDRIVEDELAY = 60; }

                    break;

                case State.CheckEmptyTrack:                    
                    if (m_iFYApp.GetFYApp().TrainsOnFY[TrainDriveInPointer] == 1)
                    {
                        TrainDriveInPointer++;
                        if (TrainDriveInPointer > 11)
                        {
                            TrainDriveInPointer = 1;
                        }
                    }
                    else 
                    { 
                        TrainDriveIn_Machine = State.MoveToEmptyTrack;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn Goto Track" + Convert.ToString(TrainDriveInPointer));
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.MoveToEmptyTrack");
                    }
                    break;

                case State.MoveToEmptyTrack:
                    uControllerReady = false;
                    m_iFYApp.Cmd(" FiddleGo" + Convert.ToString(TrainDriveInPointer) + " ", "");//----------------------------------------------------- Go to empty track
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn FiddleGo" + Convert.ToString(TrainDriveInPointer));
                    TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack;                    
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack");
                    break;

                case State.CheckArrivedAtEmptyTrack:

                    if (m_iFYApp.GetFYApp().GetTrackNr() == TrainDriveInPointer && uControllerReady == true)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetTrackNr() == TrainDriveInPointer");
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc5BOnFalse();                            
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc5BOnFalse()");
                        TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack1");
                    }
                    break;

                case State.CheckArrivedAtEmptyTrack_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc6OnFalse();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc6OnFalse()");
                        TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_2;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack2");
                    }
                    break;

                case State.CheckArrivedAtEmptyTrack_2:
                    if (uControllerReady == true)
                    {
                        TrainDriveIn_Machine = State.CheckTrainInTrack6;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainInTrack6");
                    }
                    break;

                case State.CheckTrainInTrack6:
                    if (m_iFYApp.GetFYApp().Block6 == true)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetBlock6() == true");
                        // <---------------------------------------------------------------------------------------------------------delay required in real life?
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc7OnFalse();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc7OnFalse()");
                        TrainDriveIn_Machine = State.CheckTrainInTrack6_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainInTrack6_1");
                    }
                    break;

                case State.CheckTrainInTrack6_1:
                    if (uControllerReady == true)
                    {
                        TrainDriveIn_Machine = State.CheckTrainF10;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainF10");
                    }
                    break;
                    
                case State.CheckTrainF10:
                    if (m_iFYApp.GetFYApp().F10 == true)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetF10() == true");
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc5BOnTrue();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc5BOnTrue()");
                        TrainDriveIn_Machine = State.CheckTrainF10_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainF10_1");
                    }
                    break;

                case State.CheckTrainF10_1:
                    if (uControllerReady == true)
                    {
                        TrainDriveIn_Machine = State.CheckTrainInTrack7;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainInTrack7");
                    }
                    break;

                case State.CheckTrainInTrack7:
                    if (m_iFYApp.GetFYApp().Block7 == true)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetBlock7() == true");
                        TrainDriveIn_Machine = State.CheckTrainF11;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainF11");
                    }
                    break;

                case State.CheckTrainF11:
                    if (m_iFYApp.GetFYApp().F11 == true)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetF11() == true");
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc7OnTrue();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc7OnTrue()");
                        TrainDriveIn_Machine = State.CheckTrainF11_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainF11_1");
                    }
                    break;

                case State.CheckTrainF11_1:
                    if (uControllerReady == true)
                    {
                        TrainDriveIn_Machine = State.CheckTrainStopped;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainInTrack7");
                    }
                    break;

                case State.CheckTrainStopped:
                    // <---------------------------------------------------------------------------------------------------------delay required in real life? -> yes wait time for train to stop after occ on 7!!!
                    TrainDriveDelay++;
                    if (m_iFYApp.GetFYApp().F12 == false && m_iFYApp.GetFYApp().F13 == false && TrainDriveDelay > TRAINDRIVEDELAY)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetF12() == false && m_iFYApp.GetF13() == false");
                        TrainDriveDelay = 0;
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc6OnTrue();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc6OnTrue()");
                        TrainDriveIn_Machine = State.CheckTrainStopped_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainStopped_1");
                        
                    }
                    else if (m_iFYApp.GetFYApp().F12 == true || m_iFYApp.GetFYApp().F13 == true && TrainDriveDelay > TRAINDRIVEDELAY)
                    {
                        TrainDriveDelay = 0;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetF12()->" + Convert.ToString(m_iFYApp.GetFYApp().F12) + ", m_iFYApp.GetF13()->" + Convert.ToString(m_iFYApp.GetFYApp().F13));
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn Train drove to far");//<-----------------------------------------------------------------Send to FORM!!!
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc6OnFalse();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc6OnFalse()");
                        TrainDriveIn_Machine = State.CheckTrainStopped_2;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainStopped_2");
                    }
                    break;

                case State.CheckTrainStopped_1:
                    if (uControllerReady == true)
                    {
                        m_iFYApp.GetFYApp().TrainsOnFY[TrainDriveInPointer] = 1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetTrainsOnFY()[" + Convert.ToString(TrainDriveInPointer) + "] = 1 (TRAIN IN)");//<-----------------------------------------------------------------Send to FORM!!!
                        m_iFYApp.UpdateTrainsOnFY("Track" + Convert.ToString(TrainDriveInPointer), 1, "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.UpdateTrainsOnFY()");
                        _Return = "Finished";
                        TrainDriveIn_Machine = State.Start;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.Start");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn _Return = Finished");
                    }
                    break;

                case State.CheckTrainStopped_2:
                    if (uControllerReady == true)
                    {
                        m_iFYApp.GetFYApp().CmdOcc7OnFalse();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc7OnFalse()");
                        TrainDriveIn_Machine = State.CheckTrainStopped_3;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.CheckTrainStopped_3");
                    }
                    break;

                case State.CheckTrainStopped_3:
                    if (uControllerReady == true)
                    {                       
                        TrainDriveIn_Machine = State.TrainDriveInFailed;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.TrainDriveInFailed");
                    }
                    break;

                case State.TrainDriveInFailed:
                    if (m_iFYApp.GetFYApp().F10 == false && m_iFYApp.GetFYApp().F11 == false && m_iFYApp.GetFYApp().F12 == false && m_iFYApp.GetFYApp().F13 == false && m_iFYApp.GetFYApp().Block6 == false && m_iFYApp.GetFYApp().Block7 == false)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn Train has driven away");//<-----------------------------------------------------------------Send to FORM!!!
                        m_iFYApp.GetFYApp().TrainsOnFY[TrainDriveInPointer] = 0;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.GetTrainsOnFY()[" + Convert.ToString(TrainDriveInPointer) + "] = 0 (NO TRAIN IN)");
                        m_iFYApp.UpdateTrainsOnFY("Track" + Convert.ToString(TrainDriveInPointer), 0, "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.UpdateTrainsOnFY()");
                        _Return = "Finished";//<-----------------------------------------------------------------Maybe required some day to CmdOcc7OnTrue() and CmdOcc6OnTrue()
                        TrainDriveIn_Machine = State.Start;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn TrainDriveIn_Machine = State.Start");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn _Return = Finished");
                    }
                    break;
                
                default:
                    break;
            }

            return _Return;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrainDriveOut()
         *               This wil drive trains out of the Fiddle Yard
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
        public string TrainDriveOut(string kickTrainDriveOut)
        {
            string _Return = "Running";

            switch (TrainDriveOut_Machine)
            {
                case State.Start:
                    if (TrainDriveOutPointer > 11)
                    {
                        TrainDriveOutPointer = 1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOutPointer > 11 -> TrainDriveOutPointer == " + Convert.ToString(TrainDriveOutPointer));
                    }
                    TrainDriveOut_Machine = State.CheckFullTrack;
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOut_Machine = State.CheckFullTrack");
                    break;

                case State.CheckFullTrack:
                    if (m_iFYApp.GetFYApp().TrainsOnFY[TrainDriveOutPointer] == 0)
                    {
                        TrainDriveOutPointer++;
                        if (TrainDriveOutPointer > 11)
                        {
                            TrainDriveOutPointer = 1;
                        }
                    }
                    else
                    {
                        TrainDriveOut_Machine = State.MoveToFullTrack;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut Goto Track" + Convert.ToString(TrainDriveOutPointer));
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOut_Machine = State.MoveToFullTrack");
                    }
                    break;

                case State.MoveToFullTrack:
                    uControllerReady = false;
                    m_iFYApp.Cmd(" FiddleGo" + Convert.ToString(TrainDriveOutPointer) + " ", "");
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut FiddleGo" + Convert.ToString(TrainDriveOutPointer));
                    TrainDriveOut_Machine = State.MoveToFullTrack_1;
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOut_Machine = State.MoveToFullTrack_1");                    
                    break;

                case State.MoveToFullTrack_1:
                    if (uControllerReady == true)
                    {
                        TrainDriveOut_Machine = State.CheckArrivedAtFullTrack;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOut_Machine = State.CheckArrivedAtFullTrack");
                    }
                    break;

                case State.CheckArrivedAtFullTrack:
                    if (m_iFYApp.GetFYApp().GetTrackNr() == TrainDriveOutPointer && uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc7OnFalse();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut m_iFYApp.CmdOcc7OnFalse()");
                        TrainDriveOut_Machine = State.CheckArrivedAtFullTrack_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOut_Machine = State.CheckArrivedAtFullTrack_1");
                    }
                    break;

                case State.CheckArrivedAtFullTrack_1:
                    if (uControllerReady == true)
                    {
                        TrainDriveOut_Machine = State.CheckTrainLeftTrack7;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOut_Machine = State.CheckTrainLeftTrack7");
                    }
                    break;

                case State.CheckTrainLeftTrack7:
                    if (m_iFYApp.GetFYApp().F10 == false && m_iFYApp.GetFYApp().F11 == false && m_iFYApp.GetFYApp().F12 == false && m_iFYApp.GetFYApp().F13 == false && m_iFYApp.GetFYApp().Block7 == false)
                    {
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc7OnTrue();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut m_iFYApp.CmdOcc7OnTrue()");
                        TrainDriveOut_Machine = State.CheckTrainLeftTrack7_1;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOut_Machine = State.CheckTrainLeftTrack7_1");
                    }
                    break;

                case State.CheckTrainLeftTrack7_1:
                    if (uControllerReady == true)
                    {
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut Train has driven out of the Fiddle Yard"); //<-----------------------------------------------------------------Send to FORM!!!
                        m_iFYApp.GetFYApp().TrainsOnFY[TrainDriveOutPointer] = 0;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut m_iFYApp.GetTrainsOnFY()[" + Convert.ToString(TrainDriveOutPointer) + "] = 0 (TRAIN LEFT)");
                        m_iFYApp.UpdateTrainsOnFY("Track" + Convert.ToString(TrainDriveOutPointer), 0, "");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut m_iFYApp.UpdateTrainsOnFY()");
                        TrainDriveOutPointer++;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOutPointer++ -> TrainDriveOutPointer == " + Convert.ToString(TrainDriveOutPointer));
                        _Return = "Finished";
                        TrainDriveOut_Machine = State.Start;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveOut_Machine = State.Start");
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut _Return = Finished");                        
                    }
                    break;

                default:
                    break;
            }

            return _Return;
        }


        /*#--------------------------------------------------------------------------#*/
        /*  Description: Train drive through()
         *               This wil drive trains through the Fiddle Yard when collect is on
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
        public string TrainDriveThrough(string kickTrainDriveThrough)
        {
            string _Return = "Running";

            switch (TrainDriveThrough_Machine)
            {
                case State.Start:
                    uControllerReady = false;
                    m_iFYApp.GetFYApp().CmdOcc5BOnFalse();
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc5BOnFalse()");
                    TrainDriveThrough_Machine = State.TrainDriveThrough_1;
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveThrough_Machine = State.TrainDriveThrough_1");
                    break;

                case State.TrainDriveThrough_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc6OnFalse();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc6OnFalse()");
                        TrainDriveThrough_Machine = State.TrainDriveThrough_2;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveThrough_Machine = State.TrainDriveThrough_2");
                    }
                    break;

                case State.TrainDriveThrough_2:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc7OnFalse();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc7OnFalse()");
                        TrainDriveThrough_Machine = State.TrainDriveThrough_3;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveThrough_Machine = State.TrainDriveThrough_3");
                    }
                    break;

                case State.TrainDriveThrough_3:
                    if (uControllerReady == true)
                    {
                        TrainDriveThrough_Machine = State.Idle;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveThrough_Machine = State.Idle");
                        _Return = "Finished";
                        m_iFYApp.UpdateTrainsOnFY("Track" + Convert.ToString(m_iFYApp.GetFYApp().GetTrackNr()), 0, "");
                    }
                    break;

                case State.Idle:
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveThrough -> Collect has become false");
                    TrainDriveThrough_Machine = State.Stop;
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveThrough_Machine = State.Stop");                                            
                    break;

                case State.Stop:
                    uControllerReady = false;
                    m_iFYApp.GetFYApp().CmdOcc5BOnTrue();
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc5BOnTrue()");
                    TrainDriveThrough_Machine = State.TrainDriveThrough_4;
                    m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveThrough_Machine = State.TrainDriveThrough_4");
                    break;

                case State.TrainDriveThrough_4:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc6OnTrue();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc6OnTrue()");
                        TrainDriveThrough_Machine = State.TrainDriveThrough_5;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveThrough_Machine = State.TrainDriveThrough_5");
                    }
                    break;

                case State.TrainDriveThrough_5:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_iFYApp.GetFYApp().CmdOcc7OnTrue();
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveIn m_iFYApp.CmdOcc7OnTrue()");
                        TrainDriveThrough_Machine = State.TrainDriveThrough_6;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveOut TrainDriveThrough_Machine = State.TrainDriveThrough_6");
                    }
                    break;

                case State.TrainDriveThrough_6:
                    if (uControllerReady == true)
                    {
                        TrainDriveThrough_Machine = State.Start;
                        m_iFYApp.GetFYApp().FiddleYardApplicationLogging.StoreText("FYAppTrainDrive().TrainDriveThrough_Machine = State.Start");
                        _Return = "Finished";
                    }
                    break;

                default: break;
            }

            return (_Return);
        }
    }
}
