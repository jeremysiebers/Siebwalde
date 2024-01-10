using System;
using SiebwaldeApp.Core;

namespace SiebwaldeApp
{
    public class FiddleYardAppTrainDrive
    {
        private iFiddleYardIOHandle m_iFYIOH;
        private FiddleYardIOHandleVariables m_FYIOHandleVar;             // connect variable to connect to FYIOH class for defined variables
        private FiddleYardApplicationVariables m_FYAppVar;
        private FiddleYardMip50 m_FYMIP50;                               // Create new MIP50 sub program       
        private string LoggerInstance { get; set; }
        private enum State { Start, CheckEmptyTrack, MoveToEmptyTrack, CheckArrivedAtEmptyTrack, CheckTrainInTrack6, CheckTrainF10, CheckTrainInTrack7, CheckTrainF11, CheckTrainStopped, TrainDriveInFailed,
            CheckFullTrack, MoveToFullTrack, CheckArrivedAtFullTrack, CheckTrainLeftTrack7, Idle, Stop, CheckArrivedAtEmptyTrack_1, CheckArrivedAtEmptyTrack_2, CheckArrivedAtEmptyTrack_3,
            CheckArrivedAtEmptyTrack_4, CheckArrivedAtEmptyTrack_5, CheckTrainInTrack6_1, CheckTrainF10_1, CheckTrainF11_1,CheckTrainStopped_1, CheckTrainStopped_2, CheckTrainStopped_3, MoveToFullTrack_1,
            CheckArrivedAtFullTrack_1, CheckArrivedAtFullTrack_2, CheckArrivedAtFullTrack_3, CheckTrainLeftTrack7_1, TrainDriveThrough_1, TrainDriveThrough_2, TrainDriveThrough_3,TrainDriveThrough_4, TrainDriveThrough_5,
            TrainDriveThrough_6
        };
        private State TrainDriveIn_Machine;
        private State TrainDriveOut_Machine;
        private State TrainDriveThrough_Machine;        
        private int TrainDriveInPointer = 1;            // points to a track in which it is required to check if a train is present for traindrive in
        private int TrainDriveOutPointer = 5;           // points to a track in which it is required to check if a train is present for traindrive out  
        private int TrainDriveInPointerCheckedAll = 1;  // stores the start value, in case all tracks are disabled, exit traindrive-in program
        private int TrainDriveOutPointerCheckedAll = 1; // stores the start value, in case all tracks are disabled, exit traindrive-out program
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
        public FiddleYardAppTrainDrive(FiddleYardIOHandleVariables FYIOHandleVar, 
            iFiddleYardIOHandle iFYIOH, 
            FiddleYardApplicationVariables FYAppVar,
            FiddleYardMip50 FYMIP50,
            string loggerInstance)
        {
            m_FYIOHandleVar = FYIOHandleVar;
            m_iFYIOH = iFYIOH;
            m_FYAppVar = FYAppVar;
            m_FYMIP50 = FYMIP50;
            LoggerInstance = loggerInstance;
            TrainDriveIn_Machine = State.Start;
            TrainDriveOut_Machine = State.Start;
            TrainDriveThrough_Machine = State.Start;

            Message Msg_uControllerReady = new Message("uControllerReady", " uControllerReady ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYIOHandleVar.uControllerReady.Attach(Msg_uControllerReady);            
        }

        public void SetMessage(string name, string log)
        {
            if (TrainDriveIn_Machine != State.Start || TrainDriveOut_Machine != State.Start)
            {
                IoC.Logger.Log("recieved uControllerReady = true", LoggerInstance);
            }            
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
            TrainDriveInPointerCheckedAll = 1;
            TrainDriveOutPointerCheckedAll = 1;
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
            string SubProgramReturnVal = null;

            switch (TrainDriveIn_Machine)
            {
                case State.Start:                    
                    TrainDriveInPointer = m_FYAppVar.GetTrackNr();        // Driving trains into the fiddle yard may be done without memorizeing, driving out is done in Fi-La
                    TrainDriveInPointerCheckedAll = TrainDriveInPointer;
                    IoC.Logger.Log("TrainDriveInPointer = m_iFYApp.GetTrackNr() = " + Convert.ToString(m_FYAppVar.GetTrackNr()), LoggerInstance);
                    TrainDriveIn_Machine = State.CheckEmptyTrack;
                    IoC.Logger.Log("TrainDriveIn_Machine = State.CheckEmptyTrack", LoggerInstance);

                    if (m_iFYIOH.GetFYSimulatorActive() == true)
                    {
                        TRAINDRIVEDELAY = 10;
                    }
                    else { TRAINDRIVEDELAY = 60; }

                    break;

                case State.CheckEmptyTrack:
                    if ((m_FYAppVar.iTrainsOnFY[TrainDriveInPointer] == 1) || (m_FYAppVar.icheckBoxTrack[TrainDriveInPointer] == 1))
                    {
                        TrainDriveInPointer++;
                        if (TrainDriveInPointer > 11)
                        {
                            TrainDriveInPointer = 1;
                        }
                        if (TrainDriveInPointerCheckedAll == TrainDriveInPointer)
                        {
                            TrainDriveIn_Machine = State.Start;
                            _Return = "Finished";
                            IoC.Logger.Log("All tracks are disabled, cancelling Traindrive in Program", LoggerInstance);
                        }
                    }
                    else 
                    { 
                        TrainDriveIn_Machine = State.MoveToEmptyTrack;
                        IoC.Logger.Log("Goto Track" + Convert.ToString(TrainDriveInPointer), LoggerInstance);
                        IoC.Logger.Log("TrainDriveIn_Machine = State.MoveToEmptyTrack", LoggerInstance);
                    }
                    break;

                case State.MoveToEmptyTrack:
                    uControllerReady = false;
                    m_FYAppVar.Uncouple.UpdateActuator(); // This will give back uCOntroller ready. 
                    IoC.Logger.Log("m_FYAppVar.Uncouple.UpdateActuator()", LoggerInstance);                    
                    TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack;                    
                    IoC.Logger.Log("TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack", LoggerInstance);
                    break;

                case State.CheckArrivedAtEmptyTrack:
                    if (uControllerReady == true)   // Check if Uncoupled
                    {
                        m_FYMIP50.MIP50xMOVExCALC(Convert.ToUInt16(TrainDriveInPointer));
                        IoC.Logger.Log("m_FYMIP50.MIP50xMOVExCALC(" + Convert.ToString(TrainDriveInPointer) + ")", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_1;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_1", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtEmptyTrack_1:
                    SubProgramReturnVal = m_FYMIP50.MIP50xMOVE();
                    if (SubProgramReturnVal == "Finished")
                    {
                        IoC.Logger.Log("m_FYMIP50.MIP50xMOVE == Finished", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_2;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_2", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtEmptyTrack_2:
                    if (m_FYAppVar.GetTrackNr() == TrainDriveInPointer && uControllerReady == true)
                    {
                        IoC.Logger.Log("m_iFYApp.GetTrackNr() == TrainDriveInPointer", LoggerInstance);
                        uControllerReady = false;
                        m_FYAppVar.Couple.UpdateActuator();
                        IoC.Logger.Log("m_FYAppVar.Couple.UpdateActuator()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_3;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack3", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtEmptyTrack_3:
                    if (uControllerReady == true)   // Check if coupled
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ5BOnFalse.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc5BOnFalse();  
                        IoC.Logger.Log("m_FYAppVar.Occ5BOnFalse.UpdateActuator()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_4;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack4", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtEmptyTrack_4:
                    if (uControllerReady == true)   // Check if Occ5BOnFalse
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnFalse.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc6OnFalse();
                        IoC.Logger.Log("m_FYAppVar.Occ6OnFalse.UpdateActuator()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack_5;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckArrivedAtEmptyTrack5", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtEmptyTrack_5:
                    if (uControllerReady == true)   // Check if Occ6OnFalse
                    {
                        TrainDriveIn_Machine = State.CheckTrainInTrack6;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainInTrack6", LoggerInstance);
                    }
                    break;

                case State.CheckTrainInTrack6:
                    if (m_FYAppVar.bBlock6 == true)
                    {
                        IoC.Logger.Log("m_iFYApp.GetBlock6() == true", LoggerInstance);
                        // <---------------------------------------------------------------------------------------------------------delay required in real life?
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnFalse.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc7OnFalse();
                        IoC.Logger.Log("m_iFYApp.CmdOcc7OnFalse()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckTrainInTrack6_1;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainInTrack6_1", LoggerInstance);
                    }
                    break;

                case State.CheckTrainInTrack6_1:
                    if (uControllerReady == true)
                    {
                        TrainDriveIn_Machine = State.CheckTrainF10;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainF10", LoggerInstance);
                    }
                    break;
                    
                case State.CheckTrainF10:
                    if (m_FYAppVar.bF10 == true)
                    {
                        IoC.Logger.Log("m_iFYApp.GetF10() == true", LoggerInstance);
                        uControllerReady = false;
                        m_FYAppVar.Occ5BOnTrue.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc5BOnTrue();
                        IoC.Logger.Log("m_iFYApp.CmdOcc5BOnTrue()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckTrainF10_1;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainF10_1", LoggerInstance);
                    }
                    break;

                case State.CheckTrainF10_1:
                    if (uControllerReady == true)
                    {
                        TrainDriveIn_Machine = State.CheckTrainInTrack7;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainInTrack7", LoggerInstance);
                    }
                    break;

                case State.CheckTrainInTrack7:
                    if (m_FYAppVar.bBlock7 == true)
                    {
                        IoC.Logger.Log("m_iFYApp.GetBlock7() == true", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckTrainF11;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainF11", LoggerInstance);
                    }
                    break;

                case State.CheckTrainF11:
                    if (m_FYAppVar.bF11 == true)
                    {
                        IoC.Logger.Log("m_iFYApp.GetF11() == true", LoggerInstance);
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnTrue.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc7OnTrue();
                        IoC.Logger.Log("m_iFYApp.CmdOcc7OnTrue()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckTrainF11_1;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainF11_1", LoggerInstance);
                    }
                    break;

                case State.CheckTrainF11_1:
                    if (uControllerReady == true)
                    {
                        TrainDriveIn_Machine = State.CheckTrainStopped;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainInTrack7", LoggerInstance);
                    }
                    break;

                case State.CheckTrainStopped:
                    // <---------------------------------------------------------------------------------------------------------delay required in real life? -> yes wait time for train to stop after occ on 7!!!
                    TrainDriveDelay++;
                    if (m_FYAppVar.bF12 == false && m_FYAppVar.bF13 == false && TrainDriveDelay > TRAINDRIVEDELAY)
                    {
                        IoC.Logger.Log("m_iFYApp.GetF12() == false && m_iFYApp.GetF13() == false", LoggerInstance);
                        TrainDriveDelay = 0;
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnTrue.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc6OnTrue();
                        IoC.Logger.Log("m_iFYApp.CmdOcc6OnTrue()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckTrainStopped_1;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainStopped_1", LoggerInstance);
                        
                    }
                    else if (m_FYAppVar.bF12 == true || m_FYAppVar.bF13 == true && TrainDriveDelay > TRAINDRIVEDELAY)
                    {
                        TrainDriveDelay = 0;
                        IoC.Logger.Log("m_iFYApp.GetF12()->" + Convert.ToString(m_FYAppVar.F12) + ", m_iFYApp.GetF13()->" + Convert.ToString(m_FYAppVar.F13), LoggerInstance);
                        IoC.Logger.Log("Train drove to far", LoggerInstance);//<-----------------------------------------------------------------Send to FORM!!!
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnFalse.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc6OnFalse();
                        IoC.Logger.Log("m_iFYApp.CmdOcc6OnFalse()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckTrainStopped_2;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainStopped_2", LoggerInstance);
                    }
                    break;

                case State.CheckTrainStopped_1:
                    if (uControllerReady == true)
                    {
                        m_FYAppVar.iTrainsOnFY[TrainDriveInPointer] = 1;
                        IoC.Logger.Log("m_iFYApp.GetTrainsOnFY()[" + Convert.ToString(TrainDriveInPointer) + "] = 1 (TRAIN IN)", LoggerInstance);//<-----------------------------------------------------------------Send to FORM!!!
                        m_FYAppVar.UpdateTrainsOnFY("Track" + Convert.ToString(TrainDriveInPointer), 1, "");
                        IoC.Logger.Log("m_iFYApp.UpdateTrainsOnFY()", LoggerInstance);
                        _Return = "Finished";
                        TrainDriveIn_Machine = State.Start;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.Start", LoggerInstance);
                        IoC.Logger.Log("_Return = Finished", LoggerInstance);
                    }
                    break;

                case State.CheckTrainStopped_2:
                    if (uControllerReady == true)
                    {
                        m_FYAppVar.Occ7OnFalse.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc7OnFalse();
                        IoC.Logger.Log("m_iFYApp.CmdOcc7OnFalse()", LoggerInstance);
                        TrainDriveIn_Machine = State.CheckTrainStopped_3;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.CheckTrainStopped_3", LoggerInstance);
                    }
                    break;

                case State.CheckTrainStopped_3:
                    if (uControllerReady == true)
                    {                       
                        TrainDriveIn_Machine = State.TrainDriveInFailed;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.TrainDriveInFailed", LoggerInstance);
                    }
                    break;

                case State.TrainDriveInFailed:
                    if (m_FYAppVar.bF10 == false && m_FYAppVar.bF11 == false && m_FYAppVar.bF12 == false && m_FYAppVar.bF13 == false && m_FYAppVar.bBlock6 == false && m_FYAppVar.bBlock7 == false)
                    {
                        IoC.Logger.Log("Train has driven away", LoggerInstance);//<-----------------------------------------------------------------Send to FORM!!!
                        m_FYAppVar.iTrainsOnFY[TrainDriveInPointer] = 0;
                        IoC.Logger.Log("m_iFYApp.GetTrainsOnFY()[" + Convert.ToString(TrainDriveInPointer) + "] = 0 (NO TRAIN IN)", LoggerInstance);
                        m_FYAppVar.UpdateTrainsOnFY("Track" + Convert.ToString(TrainDriveInPointer), 0, "");
                        IoC.Logger.Log("m_iFYApp.UpdateTrainsOnFY()", LoggerInstance);
                        _Return = "Finished";//<-----------------------------------------------------------------Maybe required some day to CmdOcc7OnTrue() and CmdOcc6OnTrue()
                        TrainDriveIn_Machine = State.Start;
                        IoC.Logger.Log("TrainDriveIn_Machine = State.Start", LoggerInstance);
                        IoC.Logger.Log("_Return = Finished", LoggerInstance);
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
            string SubProgramReturnVal = null;

            switch (TrainDriveOut_Machine)
            {
                case State.Start:                    
                    TrainDriveOutPointer = m_FYAppVar.iTrainDriveOutPointer;                        // get latest next traindriveout pointer number, if not changed in FYFORM it is th elast value created here
                    TrainDriveOutPointerCheckedAll = TrainDriveOutPointer;                    
                    TrainDriveOut_Machine = State.CheckFullTrack;
                    IoC.Logger.Log("TrainDriveOut_Machine = State.CheckFullTrack", LoggerInstance);
                    break;

                case State.CheckFullTrack:
                    if ((m_FYAppVar.iTrainsOnFY[TrainDriveOutPointer] == 0) || (m_FYAppVar.icheckBoxTrack[TrainDriveOutPointer] == 1))
                    {                        
                        TrainDriveOutPointer++;
                        if (TrainDriveOutPointer > 11)
                        {
                            TrainDriveOutPointer = 1;
                            IoC.Logger.Log("TrainDriveOutPointer > 11 -> TrainDriveOutPointer == " + Convert.ToString(TrainDriveOutPointer), LoggerInstance);
                        }
                        m_FYAppVar.TrainDriveOutPointer.UpdateSensorValue(TrainDriveOutPointer, false); // When Traindrive out created a new number for TrainDriveOutPointer then update FYFORM with this numeber

                        if (TrainDriveOutPointerCheckedAll == TrainDriveOutPointer)
                        {
                            TrainDriveOut_Machine = State.Start;
                            _Return = "Finished";
                            IoC.Logger.Log("All tracks are disabled, cancelling Traindrive out Program", LoggerInstance);
                        }

                        if (m_FYAppVar.iTrainsOnFY[TrainDriveOutPointer] == 0)
                        {
                            IoC.Logger.Log("track: " + Convert.ToString(TrainDriveOutPointer) + " is empty", LoggerInstance);
                        }
                        if (m_FYAppVar.icheckBoxTrack[TrainDriveOutPointer] == 1)
                        {
                            IoC.Logger.Log("track: " + Convert.ToString(TrainDriveOutPointer) + " is disabled in FYFORM", LoggerInstance);
                        }
                    }
                    else
                    {
                        TrainDriveOut_Machine = State.MoveToFullTrack;
                        IoC.Logger.Log("Goto Track" + Convert.ToString(TrainDriveOutPointer), LoggerInstance);
                        IoC.Logger.Log("TrainDriveOut_Machine = State.MoveToFullTrack", LoggerInstance);
                    }
                    break;

                case State.MoveToFullTrack:
                    uControllerReady = false;
                    m_FYAppVar.Uncouple.UpdateActuator(); // This will give back uCOntroller ready. 
                    IoC.Logger.Log("m_FYAppVar.Uncouple.UpdateActuator()", LoggerInstance);
                    TrainDriveOut_Machine = State.MoveToFullTrack_1;
                    IoC.Logger.Log("TrainDriveOut_Machine = State.MoveToFullTrack_1", LoggerInstance);                    
                    break;

                case State.MoveToFullTrack_1:
                    if (uControllerReady == true)   // Check if uncoupled
                    {
                        m_FYMIP50.MIP50xMOVExCALC(Convert.ToUInt16(TrainDriveOutPointer));// Do the calculation on the track to move to                    
                        IoC.Logger.Log("m_FYMIP50.MIP50xMOVExCALC(" + Convert.ToString(TrainDriveOutPointer) + ")", LoggerInstance);
                        TrainDriveOut_Machine = State.CheckArrivedAtFullTrack;
                        IoC.Logger.Log("TrainDriveOut_Machine = State.CheckArrivedAtFullTrack", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtFullTrack:   // Check if Moved
                    SubProgramReturnVal = (m_FYMIP50.MIP50xMOVE());
                    if (SubProgramReturnVal == "Finished")
                    {
                        TrainDriveOut_Machine = State.CheckArrivedAtFullTrack_1;
                        IoC.Logger.Log("TrainDriveOut_Machine = State.CheckArrivedAtFullTrack_1", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtFullTrack_1:   // Check if Moved
                    if (m_FYAppVar.GetTrackNr() == TrainDriveOutPointer && uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Couple.UpdateActuator();
                        IoC.Logger.Log("m_FYAppVar.Couple.UpdateActuator()", LoggerInstance);
                        TrainDriveOut_Machine = State.CheckArrivedAtFullTrack_2;
                        IoC.Logger.Log("TrainDriveOut_Machine = State.CheckArrivedAtFullTrack_2", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtFullTrack_2:
                    if (uControllerReady == true)   // Check if Coupled
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnFalse.UpdateActuator();
                        IoC.Logger.Log("m_FYAppVar.Occ7OnFalse.UpdateActuator()", LoggerInstance);
                        TrainDriveOut_Machine = State.CheckArrivedAtFullTrack_3;
                        IoC.Logger.Log("TrainDriveOut_Machine = State.CheckArrivedAtFullTrack_3", LoggerInstance);
                    }
                    break;

                case State.CheckArrivedAtFullTrack_3:
                    if (uControllerReady == true)   // Check if Occ7OnFalse
                    {
                        TrainDriveOut_Machine = State.CheckTrainLeftTrack7;
                        IoC.Logger.Log("TrainDriveOut_Machine = State.CheckTrainLeftTrack7", LoggerInstance);
                    }
                    break;

                case State.CheckTrainLeftTrack7:
                    if (m_FYAppVar.bF10 == false && m_FYAppVar.bF11 == false && m_FYAppVar.bF12 == false && m_FYAppVar.bF13 == false && m_FYAppVar.bBlock7 == false)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnTrue.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc7OnTrue();
                        IoC.Logger.Log("m_iFYApp.CmdOcc7OnTrue()", LoggerInstance);
                        TrainDriveOut_Machine = State.CheckTrainLeftTrack7_1;
                        IoC.Logger.Log("TrainDriveOut_Machine = State.CheckTrainLeftTrack7_1", LoggerInstance);
                    }
                    break;

                case State.CheckTrainLeftTrack7_1:
                    if (uControllerReady == true)
                    {
                        IoC.Logger.Log("Train has driven out of the Fiddle Yard", LoggerInstance); //<-----------------------------------------------------------------Send to FORM!!!
                        m_FYAppVar.iTrainsOnFY[TrainDriveOutPointer] = 0;
                        IoC.Logger.Log("m_iFYApp.GetTrainsOnFY()[" + Convert.ToString(TrainDriveOutPointer) + "] = 0 (TRAIN LEFT)", LoggerInstance);
                        m_FYAppVar.UpdateTrainsOnFY("Track" + Convert.ToString(TrainDriveOutPointer), 0, "");
                        IoC.Logger.Log("m_iFYApp.UpdateTrainsOnFY()", LoggerInstance);

                        if (m_FYAppVar.iTrainDriveOutPointer == TrainDriveOutPointer)   // check if user has changed next track to drive train out of
                        {
                            TrainDriveOutPointer++;
                            if (TrainDriveOutPointer > 11)
                            {
                                TrainDriveOutPointer = 1;
                                IoC.Logger.Log("TrainDriveOutPointer > 11 -> TrainDriveOutPointer == " + Convert.ToString(TrainDriveOutPointer), LoggerInstance);
                            }
                            m_FYAppVar.TrainDriveOutPointer.UpdateSensorValue(TrainDriveOutPointer, false); // When Traindrive out created a new number for TrainDriveOutPointer then update FYFORM with this numeber
                        }

                        IoC.Logger.Log("TrainDriveOutPointer++ -> TrainDriveOutPointer == " + Convert.ToString(TrainDriveOutPointer), LoggerInstance);
                        _Return = "Finished";
                        TrainDriveOut_Machine = State.Start;
                        IoC.Logger.Log("TrainDriveOut_Machine = State.Start", LoggerInstance);
                        IoC.Logger.Log("_Return = Finished", LoggerInstance);                        
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
                    m_FYAppVar.Occ5BOnFalse.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc5BOnFalse();
                    IoC.Logger.Log("m_iFYApp.CmdOcc5BOnFalse()", LoggerInstance);
                    TrainDriveThrough_Machine = State.TrainDriveThrough_1;
                    IoC.Logger.Log("TrainDriveThrough_Machine = State.TrainDriveThrough_1", LoggerInstance);
                    break;

                case State.TrainDriveThrough_1:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnFalse.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc6OnFalse();
                        IoC.Logger.Log("m_iFYApp.CmdOcc6OnFalse()", LoggerInstance);
                        TrainDriveThrough_Machine = State.TrainDriveThrough_2;
                        IoC.Logger.Log("TrainDriveThrough_Machine = State.TrainDriveThrough_2", LoggerInstance);
                    }
                    break;

                case State.TrainDriveThrough_2:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnFalse.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc7OnFalse();
                        IoC.Logger.Log("m_iFYApp.CmdOcc7OnFalse()", LoggerInstance);
                        TrainDriveThrough_Machine = State.TrainDriveThrough_3;
                        IoC.Logger.Log("TrainDriveThrough_Machine = State.TrainDriveThrough_3", LoggerInstance);
                    }
                    break;

                case State.TrainDriveThrough_3:
                    if (uControllerReady == true)
                    {
                        TrainDriveThrough_Machine = State.Idle;
                        IoC.Logger.Log("TrainDriveThrough_Machine = State.Idle", LoggerInstance);
                        _Return = "Finished";
                        m_FYAppVar.UpdateTrainsOnFY("Track" + Convert.ToString(m_FYAppVar.GetTrackNr()), 0, "");
                    }
                    break;

                case State.Idle:
                    IoC.Logger.Log("TrainDriveThrough -> Collect has become false", LoggerInstance);
                    TrainDriveThrough_Machine = State.Stop;
                    IoC.Logger.Log("TrainDriveThrough_Machine = State.Stop", LoggerInstance);                                            
                    break;

                case State.Stop:
                    uControllerReady = false;
                    m_FYAppVar.Occ5BOnTrue.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc5BOnTrue();
                    IoC.Logger.Log("m_iFYApp.CmdOcc5BOnTrue()", LoggerInstance);
                    TrainDriveThrough_Machine = State.TrainDriveThrough_4;
                    IoC.Logger.Log("TrainDriveThrough_Machine = State.TrainDriveThrough_4", LoggerInstance);
                    break;

                case State.TrainDriveThrough_4:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ6OnTrue.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc6OnTrue();
                        IoC.Logger.Log("m_iFYApp.CmdOcc6OnTrue()", LoggerInstance);
                        TrainDriveThrough_Machine = State.TrainDriveThrough_5;
                        IoC.Logger.Log("TrainDriveThrough_Machine = State.TrainDriveThrough_5", LoggerInstance);
                    }
                    break;

                case State.TrainDriveThrough_5:
                    if (uControllerReady == true)
                    {
                        uControllerReady = false;
                        m_FYAppVar.Occ7OnTrue.UpdateActuator();//m_iFYApp.GetFYApp().CmdOcc7OnTrue();
                        IoC.Logger.Log("m_iFYApp.CmdOcc7OnTrue()", LoggerInstance);
                        TrainDriveThrough_Machine = State.TrainDriveThrough_6;
                        IoC.Logger.Log("TrainDriveThrough_Machine = State.TrainDriveThrough_6", LoggerInstance);
                    }
                    break;

                case State.TrainDriveThrough_6:
                    if (uControllerReady == true)
                    {
                        TrainDriveThrough_Machine = State.Start;
                        IoC.Logger.Log("TrainDriveThrough_Machine = State.Start", LoggerInstance);
                        _Return = "Finished";
                    }
                    break;

                default: break;
            }

            return (_Return);
        }
    }
}
