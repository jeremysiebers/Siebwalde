﻿using System;
using System.Timers;

namespace SiebwaldeApp.Core
{
    public interface IFiddleYardApplication
    {
        //void OpenFYMip50SettingsForm();
    }

    public class FiddleYardApplication : IFiddleYardApplication
    {
        public FiddleYardApplicationVariables FYAppVar;                             // IOHanlder connects the event handlers via this way
        private ILogger FiddleYardApplicationLogging;
        private FiddleYardIOHandleVariables m_FYIOHandleVar;                        // connect variable to connect to FYIOH class for defined interfaces
        private iFiddleYardIOHandle m_iFYIOH;
        private FiddleYardAppInit FYAppInit;
        private FiddleYardAppRun FYAppRun;
        public FiddleYardMip50 FYMIP50;                                             // Create new MIP50 sub program       
        private FiddleYardTrainDetection FYTDT;                                     // Create new Traindetection sub program
       private string m_instance = null;        
        private object ExecuteLock = new object();
        private string LoggerInstance { get; set; }
        static ILogger GetLogger(string file, string loggerinstance)
        {
            return new FileLogger(file, loggerinstance);
        }

        private enum State { Idle, Start, Init, Running, Stop, Reset, MIP50Home, MIP50Move, TrainDetection };
        private State State_Machine;
        private string StopApplication = null;

        // Create a timer
        System.Timers.Timer aTimer = new System.Timers.Timer();
        
        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardApplication constructor
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
        public FiddleYardApplication(string instance, FiddleYardIOHandleVariables FYIOHandleVar, iFiddleYardIOHandle iFYIOH)
        {
            m_instance = instance;
            m_FYIOHandleVar = FYIOHandleVar;
            m_iFYIOH = iFYIOH;

            if ("TOP" == m_instance)
            {
                // Set the log instance string to the logging instance name used for directed file logging
                LoggerInstance = "FyAppTopLog";
                //  different logging file per target, this is default
                FiddleYardApplicationLogging = GetLogger(Core.Properties.CoreSettings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "FiddleYardAppLogTOP.txt", LoggerInstance);
                IoC.Logger.AddLogger(FiddleYardApplicationLogging);
            }
            else if ("BOT" == m_instance)
            {
                // Set the log instance string to the logging instance name used for directed file logging
                LoggerInstance = "FyAppBotLog";
                //  different logging file per target, this is default
                FiddleYardApplicationLogging = GetLogger(Core.Properties.CoreSettings.Default.LogDirectory + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + "FiddleYardAppLogBOT.txt", LoggerInstance);
                IoC.Logger.AddLogger(FiddleYardApplicationLogging);
            }

            // Sub programs  
            FYAppVar = new FiddleYardApplicationVariables(m_FYIOHandleVar);                 // FiddleYard Application variables class, holds all variables and functions regarding the application itself.            
            FYMIP50 = new FiddleYardMip50(m_instance, m_FYIOHandleVar, m_iFYIOH, FYAppVar);            
            FYAppRun = new FiddleYardAppRun(m_FYIOHandleVar, m_iFYIOH, FYAppVar, FYMIP50, LoggerInstance);            
            FYTDT = new FiddleYardTrainDetection(m_FYIOHandleVar, FYAppVar, FYMIP50, LoggerInstance);
            FYAppInit = new FiddleYardAppInit(m_FYIOHandleVar, FYAppVar, FYMIP50, FYTDT, LoggerInstance);

            
            // Init here all of the application
            
            FYMIP50.Start();        // start MIP50 data composer/API

            #region sensor subscribers
            //Sensors for update kick and logging, variables are updated in FiddleYardApplicationVariables
            Sensor Sns_CL_10_Heart = new Sensor("CL10Heart", " CL 10 Heart ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.CL10Heart.Attach(Sns_CL_10_Heart);
            Sensor Sns_F11 = new Sensor("F11", " F11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.F11.Attach(Sns_F11);
            Sensor Sns_EOS10 = new Sensor("EOS10", " EOS 10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.EOS10.Attach(Sns_EOS10);
            Sensor Sns_EOS11 = new Sensor("EOS11", " EOS 11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.EOS11.Attach(Sns_EOS11);
            Sensor Sns_F13 = new Sensor("F13", " F13 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.F13.Attach(Sns_F13);
            Sensor Sns_F12 = new Sensor("F12", " F12 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.F12.Attach(Sns_F12);
            Sensor Sns_Block5B = new Sensor("Block5B", " Occupied from 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Block5B.Attach(Sns_Block5B);
            Sensor Sns_Block8A = new Sensor("Block8A", " Occupied from 8A ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Block8A.Attach(Sns_Block8A);
            Sensor Sns_TrackPower = new Sensor("TrackPower", " Enable Track ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.TrackPower.Attach(Sns_TrackPower);
            Sensor Sns_Block5BIn = new Sensor("Block5BIn", " Occupied to 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Block5BIn.Attach(Sns_Block5BIn);
            Sensor Sns_Block6In = new Sensor("Block6In", " Occupied to 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Block6In.Attach(Sns_Block6In);
            Sensor Sns_Block7In = new Sensor("Block7In", " Occupied to 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Block7In.Attach(Sns_Block7In);
            Sensor Sns_Resistor = new Sensor("Resistor", " Occupied Resistor ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Resistor.Attach(Sns_Resistor);
            Sensor Sns_Block6 = new Sensor("Block6", " Occupied from 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Block6.Attach(Sns_Block6);
            Sensor Sns_Block7 = new Sensor("Block7", " Occupied from 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.Block7.Attach(Sns_Block7);
            Sensor Sns_F10 = new Sensor("F10", " F10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.F10.Attach(Sns_F10);
            Sensor Sns_M10 = new Sensor("M10", " M10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.M10.Attach(Sns_M10);
            Sensor Sns_TrackNo = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.TrackNo.Attach(Sns_TrackNo);
            Sensor Sns_CmdBusy = new Sensor("CmdBusy", " uController busy ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.CmdBusy.Attach(Sns_CmdBusy);
            Sensor Sns_TrackPower15V = new Sensor("TrackPower15V", " 15V Track Power ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYIOHandleVar.TrackPower15V.Attach(Sns_TrackPower15V);

            //Messages for update kick and logging            
            Message Msg_FiddleYardReset = new Message("FiddleYardReset", " Fiddle Yard Reset ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYIOHandleVar.FiddleYardReset.Attach(Msg_FiddleYardReset);
            Message Msg_uControllerReady = new Message("uControllerReady", " uControllerReady ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYIOHandleVar.uControllerReady.Attach(Msg_uControllerReady);
            Message Msg_TrackPower15VDown = new Message("TrackPower15VDown", " TrackPower15VDown ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYIOHandleVar.TrackPower15VDown.Attach(Msg_TrackPower15VDown);
            Message Msg_EndOffStroke11Assert = new Message("EndOffStroke11Assert", " EndOffStroke11Assert ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message            

            //Instantiate cmd handler for receiving commands from the Form and for update kick and logging       
            Command Act_Couple = new Command(" Couple ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdCouple.Attach(Act_Couple);
            Command Act_Uncouple = new Command(" Uncouple ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdUncouple.Attach(Act_Uncouple);
            Command Act_FiddleOneLeft = new Command(" FiddleOneLeft ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleOneLeft.Attach(Act_FiddleOneLeft);
            Command Act_FiddleOneRight = new Command(" FiddleOneRight ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleOneRight.Attach(Act_FiddleOneRight);
            Command Act_FiddleGo1 = new Command(" FiddleGo1 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo1.Attach(Act_FiddleGo1);
            Command Act_FiddleGo2 = new Command(" FiddleGo2 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo2.Attach(Act_FiddleGo2);
            Command Act_FiddleGo3 = new Command(" FiddleGo3 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo3.Attach(Act_FiddleGo3);
            Command Act_FiddleGo4 = new Command(" FiddleGo4 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo4.Attach(Act_FiddleGo4);
            Command Act_FiddleGo5 = new Command(" FiddleGo5 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo5.Attach(Act_FiddleGo5);
            Command Act_FiddleGo6 = new Command(" FiddleGo6 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo6.Attach(Act_FiddleGo6);
            Command Act_FiddleGo7 = new Command(" FiddleGo7 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo7.Attach(Act_FiddleGo7);
            Command Act_FiddleGo8 = new Command(" FiddleGo8 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo8.Attach(Act_FiddleGo8);
            Command Act_FiddleGo9 = new Command(" FiddleGo9 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo9.Attach(Act_FiddleGo9);
            Command Act_FiddleGo10 = new Command(" FiddleGo10 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo10.Attach(Act_FiddleGo10);
            Command Act_FiddleGo11 = new Command(" FiddleGo11 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFiddleGo11.Attach(Act_FiddleGo11);
            Command Act_TrainDetect = new Command(" TrainDetect ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdTrainDetect.Attach(Act_TrainDetect);
            Command Act_Start = new Command(" Start ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFYStart.Attach(Act_Start);
            Command Act_Stop = new Command(" Stop ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdFYStop.Attach(Act_Stop);
            Command Act_Reset = new Command(" Reset ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdReset.Attach(Act_Reset);
            Command Act_Occ5BOnTrue = new Command(" Occ5BOnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdOcc5BOnTrue.Attach(Act_Occ5BOnTrue);
            Command Act_Occ5BOnFalse = new Command(" Occ5BOnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdOcc5BOnFalse.Attach(Act_Occ5BOnFalse);
            Command Act_Occ6OnTrue = new Command(" Occ6OnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdOcc6OnTrue.Attach(Act_Occ6OnTrue);
            Command Act_Occ6OnFalse = new Command(" Occ6OnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdOcc6OnFalse.Attach(Act_Occ6OnFalse);
            Command Act_Occ7OnTrue = new Command(" Occ7OnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdOcc7OnTrue.Attach(Act_Occ7OnTrue);
            Command Act_Occ7OnFalse = new Command(" Occ7OnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdOcc7OnFalse.Attach(Act_Occ7OnFalse);
            Command Act_Collect = new Command(" Collect ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdCollect.Attach(Act_Collect);
            Command Act_HomeFY = new Command(" HomeFY ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYAppVar.CmdHomeFY.Attach(Act_HomeFY);
            #endregion

            State_Machine = State.Idle;

            IoC.Logger.Log("### Fiddle Yard Application started ###", LoggerInstance);
            IoC.Logger.Log("State_Machine = State.Idle", LoggerInstance);

        }
                
        /*#--------------------------------------------------------------------------#*/
        /*  Description: Start: initializing and attaching all sensors, actuators,
         *               messages etc.
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
        public void Start()
        {
            //FYMIP50.Start();        // start MIP50 data composer/API

            #region sensor subscribers
            ////Sensors for update kick and logging, variables are updated in FiddleYardApplicationVariables
            //Sensor Sns_CL_10_Heart = new Sensor("CL10Heart", " CL 10 Heart ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.CL10Heart.Attach(Sns_CL_10_Heart);
            //Sensor Sns_F11 = new Sensor("F11", " F11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.F11.Attach(Sns_F11);
            //Sensor Sns_EOS10 = new Sensor("EOS10", " EOS 10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.EOS10.Attach(Sns_EOS10);
            //Sensor Sns_EOS11 = new Sensor("EOS11", " EOS 11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.EOS11.Attach(Sns_EOS11);
            //Sensor Sns_F13 = new Sensor("F13", " F13 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.F13.Attach(Sns_F13);
            //Sensor Sns_F12 = new Sensor("F12", " F12 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.F12.Attach(Sns_F12);
            //Sensor Sns_Block5B = new Sensor("Block5B", " Occupied from 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Block5B.Attach(Sns_Block5B);
            //Sensor Sns_Block8A = new Sensor("Block8A", " Occupied from 8A ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Block8A.Attach(Sns_Block8A);
            //Sensor Sns_TrackPower = new Sensor("TrackPower", " Enable Track ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.TrackPower.Attach(Sns_TrackPower);
            //Sensor Sns_Block5BIn = new Sensor("Block5BIn", " Occupied to 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Block5BIn.Attach(Sns_Block5BIn);
            //Sensor Sns_Block6In = new Sensor("Block6In", " Occupied to 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Block6In.Attach(Sns_Block6In);
            //Sensor Sns_Block7In = new Sensor("Block7In", " Occupied to 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Block7In.Attach(Sns_Block7In);
            //Sensor Sns_Resistor = new Sensor("Resistor", " Occupied Resistor ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Resistor.Attach(Sns_Resistor);
            //Sensor Sns_Block6 = new Sensor("Block6", " Occupied from 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Block6.Attach(Sns_Block6);
            //Sensor Sns_Block7 = new Sensor("Block7", " Occupied from 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Block7.Attach(Sns_Block7);
            //Sensor Sns_F10 = new Sensor("F10", " F10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.F10.Attach(Sns_F10);
            //Sensor Sns_M10 = new Sensor("M10", " M10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.M10.Attach(Sns_M10);
            //Sensor Sns_TrackNo = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.TrackNo.Attach(Sns_TrackNo);
            //Sensor Sns_CmdBusy = new Sensor("CmdBusy", " uController busy ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.CmdBusy.Attach(Sns_CmdBusy);
            //Sensor Sns_TrackPower15V = new Sensor("TrackPower15V", " 15V Track Power ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.TrackPower15V.Attach(Sns_TrackPower15V);            

            ////Messages for update kick and logging            
            //Message Msg_FiddleYardReset = new Message("FiddleYardReset", " Fiddle Yard Reset ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            //m_FYIOHandleVar.FiddleYardReset.Attach(Msg_FiddleYardReset);            
            //Message Msg_uControllerReady = new Message("uControllerReady", " uControllerReady ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            //m_FYIOHandleVar.uControllerReady.Attach(Msg_uControllerReady);
            //Message Msg_TrackPower15VDown = new Message("TrackPower15VDown", " TrackPower15VDown ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            //m_FYIOHandleVar.TrackPower15VDown.Attach(Msg_TrackPower15VDown);
            //Message Msg_EndOffStroke11Assert = new Message("EndOffStroke11Assert", " EndOffStroke11Assert ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message            

            ////Instantiate cmd handler for receiving commands from the Form and for update kick and logging       
            //Command Act_Couple = new Command(" Couple ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdCouple.Attach(Act_Couple);
            //Command Act_Uncouple = new Command(" Uncouple ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdUncouple.Attach(Act_Uncouple);
            //Command Act_FiddleOneLeft = new Command(" FiddleOneLeft ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleOneLeft.Attach(Act_FiddleOneLeft);
            //Command Act_FiddleOneRight = new Command(" FiddleOneRight ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleOneRight.Attach(Act_FiddleOneRight);
            //Command Act_FiddleGo1 = new Command(" FiddleGo1 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo1.Attach(Act_FiddleGo1);
            //Command Act_FiddleGo2 = new Command(" FiddleGo2 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo2.Attach(Act_FiddleGo2);
            //Command Act_FiddleGo3 = new Command(" FiddleGo3 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo3.Attach(Act_FiddleGo3);
            //Command Act_FiddleGo4 = new Command(" FiddleGo4 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo4.Attach(Act_FiddleGo4);
            //Command Act_FiddleGo5 = new Command(" FiddleGo5 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo5.Attach(Act_FiddleGo5);
            //Command Act_FiddleGo6 = new Command(" FiddleGo6 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo6.Attach(Act_FiddleGo6);
            //Command Act_FiddleGo7 = new Command(" FiddleGo7 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo7.Attach(Act_FiddleGo7);
            //Command Act_FiddleGo8 = new Command(" FiddleGo8 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo8.Attach(Act_FiddleGo8);
            //Command Act_FiddleGo9 = new Command(" FiddleGo9 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo9.Attach(Act_FiddleGo9);
            //Command Act_FiddleGo10 = new Command(" FiddleGo10 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo10.Attach(Act_FiddleGo10);
            //Command Act_FiddleGo11 = new Command(" FiddleGo11 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFiddleGo11.Attach(Act_FiddleGo11);
            //Command Act_TrainDetect = new Command(" TrainDetect ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdTrainDetect.Attach(Act_TrainDetect);
            //Command Act_Start = new Command(" Start ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFYStart.Attach(Act_Start);
            //Command Act_Stop = new Command(" Stop ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdFYStop.Attach(Act_Stop);
            //Command Act_Reset = new Command(" Reset ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdReset.Attach(Act_Reset);
            //Command Act_Occ5BOnTrue = new Command(" Occ5BOnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdOcc5BOnTrue.Attach(Act_Occ5BOnTrue);
            //Command Act_Occ5BOnFalse = new Command(" Occ5BOnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdOcc5BOnFalse.Attach(Act_Occ5BOnFalse);
            //Command Act_Occ6OnTrue = new Command(" Occ6OnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdOcc6OnTrue.Attach(Act_Occ6OnTrue);
            //Command Act_Occ6OnFalse = new Command(" Occ6OnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdOcc6OnFalse.Attach(Act_Occ6OnFalse);
            //Command Act_Occ7OnTrue = new Command(" Occ7OnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdOcc7OnTrue.Attach(Act_Occ7OnTrue);
            //Command Act_Occ7OnFalse = new Command(" Occ7OnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdOcc7OnFalse.Attach(Act_Occ7OnFalse);            
            //Command Act_Collect = new Command(" Collect ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdCollect.Attach(Act_Collect);
            //Command Act_HomeFY = new Command(" HomeFY ", (name) => FormCmd(name)); // initialize and subscribe Commands
            //FYAppVar.CmdHomeFY.Attach(Act_HomeFY);
            #endregion

            //State_Machine = State.Idle;

            //IoC.Logger.Log("### Fiddle Yard Application started ###", LoggerInstance);
            //IoC.Logger.Log("State_Machine = State.Idle", LoggerInstance);

            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 50;
            aTimer.AutoReset = true;
            // Enable the timer
            aTimer.Enabled = true;

        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: OnTimedEvent
         *               Used to keep the application alive independent from the target
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
        public void OnTimedEvent(object source, ElapsedEventArgs e)
        {            
            ApplicationUpdate("TimerEvent", 0);            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: ApplicationUpdate, main application
         *               This is the main Fiddle Yard application, directing movements,
         *               controlling the contents of the tracks etc.
         *  Input(s)   : Sensors, actuators, messages and commands
         *               
         *
         *  Output(s)  : [x].UpdateActuator() to send command to target/simulator
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
        public void ApplicationUpdate(string kickapplication, int val)
        {
            lock (ExecuteLock)
            {
                aTimer.Stop();//--------------------------------------------------------------------- Stop the timer an event was received from target
                if (kickapplication == " Start ")                             // FYFORM Start FiddleYard button command
                {
                    FYAppVar.FiddleYardAutoModeStart.UpdateMessage();
                    State_Machine = State.Start;
                    IoC.Logger.Log("State_Machine = State.Start", LoggerInstance);
                    FYAppVar.FiddleYardInit.UpdateMessage();
                }
                else if (kickapplication == " Reset ")                        // FYFORM Reset FiddleYard button command
                {
                    IoC.Logger.Log("kickapplication == Reset", LoggerInstance);
                    State_Machine = State.Reset;
                    IoC.Logger.Log("State_Machine = State.Reset", LoggerInstance);
                }
                else if (kickapplication == " Stop ")                        // FYFORM Reset FiddleYard button command
                {
                    StopApplication = "Stop";
                    IoC.Logger.Log("StopApplication = Stop", LoggerInstance);
                    FYAppVar.FiddleYardAutoModeIsGoingToStop.UpdateMessage();
                }
                StateMachineUpdate(kickapplication, val);

                aTimer.Start();//-------------------------------------------------------------------- Start the timer until event from target
            }
        }
        
        /*#--------------------------------------------------------------------------#*/
        /*  Description: Application Statemachine
         *  Input(s)   : Sensors, actuators, messages and commands
         *               
         *
         *  Output(s)  : [x].UpdateActuator() to send command to target/simulator
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
        public void StateMachineUpdate(string kickapplication, int val)
        {
            string SubProgramReturnVal = null;

            switch (State_Machine)
            {
                case State.Reset:                    
                    FYAppVar.FiddleYardReset.UpdateMessage();
                    IoC.Logger.Log("Reset target", LoggerInstance);

                    FYAppInit.Init("Reset", val);
                    IoC.Logger.Log("FYAppInit.Init(Reset)", LoggerInstance);

                    FYAppRun.FiddleYardAppRunReset();   // also resets the sub programs!
                    IoC.Logger.Log("FYAppRun.FiddleYardAppRunReset()", LoggerInstance);

                    FYAppInit.FiddleYardInitReset();
                    IoC.Logger.Log("FYAppInit.FiddleYardInitReset()", LoggerInstance);

                    FYMIP50.MIP50Reset();   //Reset MIP50 program
                    IoC.Logger.Log("FYMIP50.MIP50Reset()", LoggerInstance);

                    FYTDT.FiddleYardTdtReset();
                    IoC.Logger.Log("FYTDT.FiddleYardTdtReset()", LoggerInstance);

                    Cmd(" Reset ", ""); // reset target
                    IoC.Logger.Log("reset target", LoggerInstance);

                    State_Machine = State.Idle;
                    IoC.Logger.Log("State_Machine = State.Idle from State.Reset", LoggerInstance);                    
                    break;

                case State.Idle:
                    Cmd(kickapplication, "");                    
                    break;

                case State.MIP50Home:
                    SubProgramReturnVal = FYMIP50.MIP50xHOME();
                    if (SubProgramReturnVal == "Finished")
                    {
                        IoC.Logger.Log("FYMIP50.MIP50xHOME() == Finished", LoggerInstance);                            
                        FYAppVar.Couple.UpdateActuator();
                        IoC.Logger.Log("FYMIP50.MIP50xHOME() FYAppVar.Couple.UpdateActuator()", LoggerInstance);
                        State_Machine = State.Idle;
                        IoC.Logger.Log("State_Machine = State.Idle", LoggerInstance);
                    }
                    else if (SubProgramReturnVal == "Error")
                    {
                        //TBD do something with the message or error!!!<---------------------------------------------------------------------------------------------------------------
                        State_Machine = State.Idle;
                    }
                    
                    break;

                case State.MIP50Move:
                    SubProgramReturnVal = FYMIP50.MIP50xMOVE();
                    if (SubProgramReturnVal == "Finished")
                    {
                        IoC.Logger.Log("FYMIP50.MIP50xMOVE() == Finished", LoggerInstance);
                        FYAppVar.Couple.UpdateActuator();
                        IoC.Logger.Log("FYMIP50.MIP50xMOVE() FYAppVar.Couple.UpdateActuator()", LoggerInstance);
                        State_Machine = State.Idle;
                        IoC.Logger.Log("State_Machine = State.Idle", LoggerInstance);
                    }
                    else if (SubProgramReturnVal == "Error")
                    {
                        //TBD do something with the message or error!!!<---------------------------------------------------------------------------------------------------------------
                        State_Machine = State.Idle;
                    }                    
                    break;

                case State.TrainDetection:
                    SubProgramReturnVal = FYTDT.Traindetection();
                    if (SubProgramReturnVal == "Finished")
                    {
                        IoC.Logger.Log("FYAppInit.Init() OR CMD FYTDT.TRAINDETECTION() == Finished",LoggerInstance);
                        FYAppVar.Couple.UpdateActuator();
                        IoC.Logger.Log("FYTDT.Traindetection() FYAppVar.Couple.UpdateActuator()", LoggerInstance);
                        State_Machine = State.Idle;
                        IoC.Logger.Log("FYAppInit.Init() OR CMD State_Machine = State.Idle", LoggerInstance);
                    }
                    break;

                case State.Start:
                    if (FYAppInit.Init(kickapplication, val) == "Finished")
                    {
                        IoC.Logger.Log("FYAppInit.Init() == Finished", LoggerInstance);
                        FYAppVar.FiddleYardInitFinished.UpdateMessage();//FYFORM.SetMessage("FYApp FYInit", "FiddleYard init Finished");
                        State_Machine = State.Running;
                        IoC.Logger.Log("State_Machine = State.Running from State.Start", LoggerInstance);
                        FYAppVar.FiddleYardApplicationRunning.UpdateMessage();//FYFORM.SetMessage("FYApp FYInit", "FiddleYard Application running...");
                    }
                    break;

                case State.Running:
                    if (FYAppRun.Run(kickapplication, StopApplication) == "Stop")
                    {
                        FYAppVar.FiddleYardAutoModeIsStopped.UpdateMessage();//FYFORM.SetMessage("FiddleYardStopped", "FiddleYard Auto mode is Stopped");  // FYFORM reacts on: FiddleYardStopped setting the buttons visable etc
                        IoC.Logger.Log("kickapplication = Stop", LoggerInstance);
                        State_Machine = State.Stop;
                        IoC.Logger.Log("State_Machine = State.Stop from State.Running", LoggerInstance);
                    }
                    break;

                case State.Stop:
                    StopApplication = null;
                    IoC.Logger.Log("StopApplication = null", LoggerInstance);
                    State_Machine = State.Idle;
                    IoC.Logger.Log("State_Machine = State.Idle from State.Stop", LoggerInstance);
                    break;

                default: 
                    break;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SetLedIndicator, SetMessage and FormCmd are used to catch 
         *               updates from target/simulator and process the contents in the
         *               main application loop (local piece of updated memory)
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
        public void SetLedIndicator(string indicator, int val, string log)
        {
            int a;            

            if (log != "" && indicator != "Track_No" && indicator != "Mip50Rec")           // Log every  sensor if enabled
            {
                IoC.Logger.Log("received a Sensor event:" + log + Convert.ToBoolean(val), LoggerInstance);
            }
            else if (log != "" && indicator == "Track_No")
            {
                a = Convert.ToInt16(val) >> 4;
                IoC.Logger.Log("received a Sensor event:" + log + Convert.ToString(a), LoggerInstance);
            }            
            ApplicationUpdate(indicator, val); // let the application know to update sub programs because a value of a sensor has changed
        }

        public void SetMessage(string message, string log)
        {
            //int val = 0;

            if (log != "" )           
            {
                IoC.Logger.Log("received a Message event:" + log, LoggerInstance);
            }

            if (message == "FiddleYardMoveAndF12Assert" || message == "FiddleYardMoveAndF13Assert")     // If the program is running and during fiddle yard move 1 of the 2 sensors trips, Reset the program.
            {                                                                                           // A future upgrade is to halt the running program and built in a resume command... TBD!!!
                message = " Reset ";
            }

            ApplicationUpdate(message, 0); // let the application know that a message is received from the target
        }

        public void FormCmd(string name)
        {            
            //int val = 0;
            if (name != "")
            {
                IoC.Logger.Log("received a FormCmd event:" + name, LoggerInstance);
            }
            ApplicationUpdate(name, 0); // let the application know that a command is sent from the FORM (button)
        }
        
        /*#--------------------------------------------------------------------------#*/
        /*  Description: FormCmd handles the commands recived from the FORM
         *               Also the application is kicked to deal with this
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
        public void Cmd(string name, string layer)      // layer is not used becuase the instance is already of the correct layer
        {            
            switch (name)
            {
                case " FiddleOneLeft ":
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(12); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleOneRight ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(13); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " Couple ": FYAppVar.Couple.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;
                case " Uncouple ": FYAppVar.Uncouple.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;

                case " FiddleGo1 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(1); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo2 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(2); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo3 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(3); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo4 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(4); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo5 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(5); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo6 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(6); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo7 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(7); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo8 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(8); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo9 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(9); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo10 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(10); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " FiddleGo11 ": 
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        FYMIP50.MIP50xMOVExCALC(11); IoC.Logger.Log("State_Machine = State.MIP50Move Cmd = " + name, LoggerInstance); State_Machine = State.MIP50Move;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;

                case " TrainDetect ":
                    if (FYAppVar.FYHomed.BoolVariable == true)
                    {
                        FYAppVar.Uncouple.UpdateActuator();
                        IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance); State_Machine = State.TrainDetection;
                    }
                    else { IoC.Logger.Log("Form Command Move FiddleYard Cancelled, FYHomed == false", LoggerInstance); FYAppVar.FYNotHomed.UpdateMessage(); 
                    }
                    break;
                    
                case " Reset ": FYAppVar.Reset.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;
                case " Occ5BOnTrue ": FYAppVar.Occ5BOnTrue.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;
                case " Occ5BOnFalse ": FYAppVar.Occ5BOnFalse.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;
                case " Occ6OnTrue ": FYAppVar.Occ6OnTrue.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;
                case " Occ6OnFalse ": FYAppVar.Occ6OnFalse.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;
                case " Occ7OnTrue ": FYAppVar.Occ7OnTrue.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;
                case " Occ7OnFalse ": FYAppVar.Occ7OnFalse.UpdateActuator(); IoC.Logger.Log("State_Machine = State.Idle Cmd = " + name, LoggerInstance);
                    break;

                case " HomeFY ":
                    FYAppVar.Uncouple.UpdateActuator();
                    IoC.Logger.Log("State_Machine = State.MIP50HOME Cmd = " + name, LoggerInstance);
                    State_Machine = State.MIP50Home;
                    FYAppVar.FiddleYardNotHomed.UpdateMessage();
                    break;

                default: break;
            }       
        }            
    }
}
