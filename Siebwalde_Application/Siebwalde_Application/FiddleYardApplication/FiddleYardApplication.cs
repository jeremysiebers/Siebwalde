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
    public interface iFiddleYardApplication
    {        
        
    }

    public class FiddleYardApplication : iFiddleYardApplication
    {
        public FiddleYardApplicationVariables FYAppVar;                             // IOHanlder connects the event handlers via this way
        public Log2LoggingFile FiddleYardApplicationLogging;        
        public FiddleYardForm FYFORM = new FiddleYardForm();
        private FiddleYardIOHandleVariables m_FYIOHandleVar;                        // connect variable to connect to FYIOH class for defined interfaces
        private iFiddleYardIOHandle m_iFYIOH;
        private FiddleYardAppInit FYAppInit;
        private FiddleYardAppRun FYAppRun;        
        private string m_instance = null;
        private string path = "null";

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Application variables
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

        public enum State { Idle, Start, Init, Running, Stop, Reset };
        public State State_Machine;
        public string StopApplication = null;

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
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardApplicationTOP.txt"; //  different logging file per target, this is default
                FiddleYardApplicationLogging = new Log2LoggingFile(path);
            }
            else if ("BOT" == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardApplicationBOT.txt"; //  different logging file per target, this is default
                FiddleYardApplicationLogging = new Log2LoggingFile(path);
            }

            // Sub programs  
            FYAppVar = new FiddleYardApplicationVariables(m_FYIOHandleVar, FYFORM);                 // FiddleYard Application variables class, holds all variables and functions regarding variables
            FYAppInit = new FiddleYardAppInit(m_FYIOHandleVar, FYAppVar, FiddleYardApplicationLogging);
            FYAppRun = new FiddleYardAppRun(m_FYIOHandleVar, m_iFYIOH, FYAppVar, FiddleYardApplicationLogging);

            //Init and setup FYFORM (after the creation of the sensors and commands)
            if ("TOP" == m_instance)
                FYFORM.Name = "FiddleYardTOP";
            else if ("BOT" == m_instance)
                FYFORM.Name = "FiddleYardBOT";

            FYFORM.Show();
            FYFORM.Hide();
            FYFORM.Connect(m_FYIOHandleVar, FYAppVar); // connect the Form to the FYIOHandle interface           
            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FYFORMShow show or hide the FORM and set width and etc.
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
        public void FYFORMShow(bool autoscroll, int height, int width, int LocX, int LocY, bool View)
        {

            FYFORM.Height = height - 60 - 27;
            if (autoscroll == true)
            {                
                FYFORM.Width = width / 2 - 8;
            }
            else
            {
                FYFORM.Width = width / 2;
            }
            if (m_instance == "TOP")
                FYFORM.Location = new System.Drawing.Point(LocX + 8, LocY + 80);
            else if (m_instance == "BOT")
                FYFORM.Location = new System.Drawing.Point(LocX + width / 2, LocY + 80);  //960
            FYFORM.AutoScroll = autoscroll;
            FYFORM.FYFORMShow(View);
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

            Message Msg_TrainDetectionTop = new Message("TrainDetectionFinished", "TrainDetectionFinished", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYIOHandleVar.TrainDetection.Attach(Msg_TrainDetectionTop);

             //Instantiate cmd handler for receiving commands from the Form            
            Command Act_Couple = new Command(" Couple ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Couple.Attach(Act_Couple);
            Command Act_Uncouple = new Command(" Uncouple ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Uncouple.Attach(Act_Uncouple);
            Command Act_FiddleOneLeft = new Command(" FiddleOneLeft ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleOneLeft.Attach(Act_FiddleOneLeft);
            Command Act_FiddleOneRight = new Command(" FiddleOneRight ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleOneRight.Attach(Act_FiddleOneRight);
            Command Act_FiddleGo1 = new Command(" FiddleGo1 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo1.Attach(Act_FiddleGo1);
            Command Act_FiddleGo2 = new Command(" FiddleGo2 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo2.Attach(Act_FiddleGo2);
            Command Act_FiddleGo3 = new Command(" FiddleGo3 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo3.Attach(Act_FiddleGo3);
            Command Act_FiddleGo4 = new Command(" FiddleGo4 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo4.Attach(Act_FiddleGo4);
            Command Act_FiddleGo5 = new Command(" FiddleGo5 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo5.Attach(Act_FiddleGo5);
            Command Act_FiddleGo6 = new Command(" FiddleGo6 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo6.Attach(Act_FiddleGo6);
            Command Act_FiddleGo7 = new Command(" FiddleGo7 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo7.Attach(Act_FiddleGo7);
            Command Act_FiddleGo8 = new Command(" FiddleGo8 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo8.Attach(Act_FiddleGo8);
            Command Act_FiddleGo9 = new Command(" FiddleGo9 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo9.Attach(Act_FiddleGo9);
            Command Act_FiddleGo10 = new Command(" FiddleGo10 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo10.Attach(Act_FiddleGo10);
            Command Act_FiddleGo11 = new Command(" FiddleGo11 ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FiddleGo11.Attach(Act_FiddleGo11);
            Command Act_TrainDetect = new Command(" TrainDetect ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.TrainDetect.Attach(Act_TrainDetect);
            Command Act_Start = new Command(" Start ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FYStart.Attach(Act_Start);
            Command Act_Stop = new Command(" Stop ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.FYStop.Attach(Act_Stop);
            Command Act_Reset = new Command(" Reset ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Reset.Attach(Act_Reset);
            Command Act_Occ5BOnTrue = new Command(" Occ5BOnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Occ5BOnTrue.Attach(Act_Occ5BOnTrue);
            Command Act_Occ5BOnFalse = new Command(" Occ5BOnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Occ5BOnFalse.Attach(Act_Occ5BOnFalse);
            Command Act_Occ6OnTrue = new Command(" Occ6OnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Occ6OnTrue.Attach(Act_Occ6OnTrue);
            Command Act_Occ6OnFalse = new Command(" Occ6OnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Occ6OnFalse.Attach(Act_Occ6OnFalse);
            Command Act_Occ7OnTrue = new Command(" Occ7OnTrue ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Occ7OnTrue.Attach(Act_Occ7OnTrue);
            Command Act_Occ7OnFalse = new Command(" Occ7OnFalse ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Occ7OnFalse.Attach(Act_Occ7OnFalse);            
            Command Act_Collect = new Command(" Collect ", (name) => FormCmd(name)); // initialize and subscribe Commands
            FYFORM.Collect.Attach(Act_Collect);

            State_Machine = State.Idle;

            FiddleYardApplicationLogging.StoreText("### Fiddle Yard Application started ###");
            FiddleYardApplicationLogging.StoreText("State_Machine = State.Idle from Start()");

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
            aTimer.Stop();//--------------------------------------------------------------------- Stop the timer an event was received from target

            if (kickapplication == " Start ")                             // FYFORM Start FiddleYard button command
            {
                FYAppVar.FiddleYardAutoModeStart.UpdateMessage();//FYFORM.SetMessage("FYApp FYStart", "FiddleYard Auto mode Start");
                State_Machine = State.Start;
                FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Start");
                FYAppVar.FiddleYardInit.UpdateMessage();//FYFORM.SetMessage("FYApp FYStart", "FiddleYard init...");
            }
            else if (kickapplication == " Reset ")                        // FYFORM Reset FiddleYard button command
            {
                FiddleYardApplicationLogging.StoreText("FYApp kickapplication == Reset");
                State_Machine = State.Reset;
                FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Reset");
            }
            else if (kickapplication == " Stop ")                        // FYFORM Reset FiddleYard button command
            {
                StopApplication = "Stop";
                FiddleYardApplicationLogging.StoreText("FYApp StopApplication = Stop");
                FYAppVar.FiddleYardAutoModeIsGoingToStop.UpdateMessage();//FYFORM.SetMessage("FYApp FYStop", "FiddleYard Auto mode is going to stop...");
            }

            StateMachineUpdate(kickapplication, val);

            aTimer.Start();//-------------------------------------------------------------------- Start the timer until event from target
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
            switch (State_Machine)
            {
                case State.Reset:
                    FYAppVar.FiddleYardReset.UpdateMessage();//FYFORM.SetMessage("FYApp FYStart", "FiddleYard Reset");
                    FiddleYardApplicationLogging.StoreText("FYApp Reset target");
                    FYAppInit.Init("Reset", val);
                    FiddleYardApplicationLogging.StoreText("FYApp FYAppInit.Init(Reset)");
                    FYAppRun.FiddleYardAppRunReset();   // also resets the sub programs!
                    FiddleYardApplicationLogging.StoreText("FYApp FYAppRun.FiddleYardAppRunReset()");
                    Cmd(" Reset ", ""); // reset target
                    FiddleYardApplicationLogging.StoreText("FYApp reset target");
                    State_Machine = State.Idle;
                    FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle from State.Reset");                    
                    break;

                case State.Idle:
                    Cmd(kickapplication, "");                    
                    break;

                case State.Start:
                    if (FYAppInit.Init(kickapplication, val) == "Finished")
                    {
                        FiddleYardApplicationLogging.StoreText("FYApp FYAppInit.Init() == Finished");
                        FYAppVar.FiddleYardInitFinished.UpdateMessage();//FYFORM.SetMessage("FYApp FYInit", "FiddleYard init Finished");
                        State_Machine = State.Running;
                        FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Running from State.Start");
                        FYAppVar.FiddleYardApplicationRunning.UpdateMessage();//FYFORM.SetMessage("FYApp FYInit", "FiddleYard Application running...");
                    }
                    break;

                case State.Running:
                    if (FYAppRun.Run(kickapplication, StopApplication) == "Stop")
                    {
                        FYAppVar.FiddleYardAutoModeIsStopped.UpdateMessage();//FYFORM.SetMessage("FiddleYardStopped", "FiddleYard Auto mode is Stopped");  // FYFORM reacts on: FiddleYardStopped setting the buttons visable etc
                        FiddleYardApplicationLogging.StoreText("FYApp kickapplication = Stop");
                        State_Machine = State.Stop;
                        FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Stop from State.Running");
                    }                    
                    break;

                case State.Stop:
                    StopApplication = null;
                    FiddleYardApplicationLogging.StoreText("FYApp StopApplication = null");
                    State_Machine = State.Idle;
                    FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle from State.Stop");
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

            if (log != "" && indicator != "Track_No")           // Log every  sensor if enabled
            {
                FiddleYardApplicationLogging.StoreText("FYApp received a Sensor event:" + log + Convert.ToBoolean(val));
            }
            else if (log != "" && indicator == "Track_No")
            {
                a = Convert.ToInt16(val) >> 4;
                FiddleYardApplicationLogging.StoreText("FYApp received a Sensor event:" + log + Convert.ToString(a));
            }
            ApplicationUpdate(indicator, val); // let the application know to update sub programs because a value of a sensor has changed
        }

        public void SetMessage(string message, string log)
        {
            int val = 0;

            if (log != "" )           
            {
                FiddleYardApplicationLogging.StoreText("FYApp received a Message event:" + log);
            }
            ApplicationUpdate(message, val); // let the application know that a message is received from the target
        }

        public void FormCmd(string name)
        {            
            int val = 0;
            if (name != "")
            {
                FiddleYardApplicationLogging.StoreText("FYApp received a FormCmd event:" + name);
            }
            ApplicationUpdate(name, val); // let the application know that a command is sent from the FORM (button)
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
                case " FiddleOneLeft ": FYAppVar.FiddleOneLeft.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleOneRight ": FYAppVar.FiddleOneRight.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Couple ": FYAppVar.Couple.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Uncouple ": FYAppVar.Uncouple.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo1 ": FYAppVar.FiddleGo1.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo2 ": FYAppVar.FiddleGo2.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo3 ": FYAppVar.FiddleGo3.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo4 ": FYAppVar.FiddleGo4.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo5 ": FYAppVar.FiddleGo5.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo6 ": FYAppVar.FiddleGo6.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo7 ": FYAppVar.FiddleGo7.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo8 ": FYAppVar.FiddleGo8.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo9 ": FYAppVar.FiddleGo9.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo10 ": FYAppVar.FiddleGo10.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo11 ": FYAppVar.FiddleGo11.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " TrainDetect ": FYAppVar.TrainDetect.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Reset ": FYAppVar.Reset.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ5BOnTrue ": FYAppVar.Occ5BOnTrue.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ5BOnFalse ": FYAppVar.Occ5BOnFalse.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ6OnTrue ": FYAppVar.Occ6OnTrue.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ6OnFalse ": FYAppVar.Occ6OnFalse.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ7OnTrue ": FYAppVar.Occ7OnTrue.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ7OnFalse ": FYAppVar.Occ7OnFalse.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;                
                                
                default: break;
            }       
        }            
    }
}
