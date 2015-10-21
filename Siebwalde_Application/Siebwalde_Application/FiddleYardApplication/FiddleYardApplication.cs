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
        FiddleYardApplication GetFYApp();      // interface to pass the IOHANDLER methods to the FYform/FYApp for subscribing to sensor updates etc.
        void UpdateTrainsOnFY(string Name, int Val, string Log);
        void Cmd(string name, string layer);
    }

    public class FiddleYardApplication : iFiddleYardApplication
    {
        public const bool TOP = true;
        public const bool BOT = false;
        string path = "null";
        public Log2LoggingFile FiddleYardApplicationLogging;
        public iFiddleYardIOHandle m_iFYIOH;                        // connect variable to connect to FYIOH class for defined interfaces
        public FiddleYardForm FYFORM = new FiddleYardForm();
        public FiddleYardAppInit FYAppInit;
        public FiddleYardAppRun FYAppRun;
        // Create actuators
        public ActuatorUpdater FiddleOneLeft;
        public ActuatorUpdater FiddleOneRight;
        public ActuatorUpdater Couple;
        public ActuatorUpdater Uncouple;
        public ActuatorUpdater FiddleGo1;
        public ActuatorUpdater FiddleGo2;
        public ActuatorUpdater FiddleGo3;
        public ActuatorUpdater FiddleGo4;
        public ActuatorUpdater FiddleGo5;
        public ActuatorUpdater FiddleGo6;
        public ActuatorUpdater FiddleGo7;
        public ActuatorUpdater FiddleGo8;
        public ActuatorUpdater FiddleGo9;
        public ActuatorUpdater FiddleGo10;
        public ActuatorUpdater FiddleGo11;
        public ActuatorUpdater TrainDetect;
        public ActuatorUpdater FYStart;
        public ActuatorUpdater FYStop;
        public ActuatorUpdater Reset;
        public ActuatorUpdater Occ5BOnTrue;
        public ActuatorUpdater Occ5BOnFalse;
        public ActuatorUpdater Occ6OnTrue;
        public ActuatorUpdater Occ6OnFalse;
        public ActuatorUpdater Occ7OnTrue;
        public ActuatorUpdater Occ7OnFalse;
        public ActuatorUpdater Recoverd;
        public ActuatorUpdater Collect;
        // Create FORM Track updaters, these are uncoupled from the array within the target
        public SensorUpdater Track1;
        public SensorUpdater Track2;
        public SensorUpdater Track3;
        public SensorUpdater Track4;
        public SensorUpdater Track5;
        public SensorUpdater Track6;
        public SensorUpdater Track7;
        public SensorUpdater Track8;
        public SensorUpdater Track9;
        public SensorUpdater Track10;
        public SensorUpdater Track11;

        public MessageUpdater FiddleYardStopped;
        public MessageUpdater FiddleYardStart;        

        public bool m_instance = true;
        public FiddleYardApplication GetFYApp()
        {
            return this;
        }
        
        public int GetTrackNr()
        {
            int _return = 0;
            switch (TrackNo)
            {
                case 0x10: _return = 1;
                    break;
                case 0x20: _return = 2;
                    break;
                case 0x30: _return = 3;
                    break;
                case 0x40: _return = 4;
                    break;
                case 0x50: _return = 5;
                    break;
                case 0x60: _return = 6;
                    break;
                case 0x70: _return = 7;
                    break;
                case 0x80: _return = 8;
                    break;
                case 0x90: _return = 9;
                    break;
                case 0xA0: _return = 10;
                    break;
                case 0xB0: _return = 11;
                    break;
                default: _return = 0;
                    break;
            }
            return _return;
        }
        
        public void CmdFiddleOneLeft()
        {
            FiddleOneLeft.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleOneLeft");
        }
        public void CmdFiddleOneRight()
        {
            FiddleOneRight.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleOneRight");
        }
        public void CmdCouple()
        {
            Couple.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = Couple");
        }
        public void CmdUncouple()
        {
            Uncouple.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = Uncouple");
        }
        public void CmdFiddleGo1()
        {
            FiddleGo1.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo1");
        }
        public void CmdFiddleGo2()
        {
            FiddleGo2.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo2");
        }
        public void CmdFiddleGo3()
        {
            FiddleGo3.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo3");
        }
        public void CmdFiddleGo4()
        {
            FiddleGo4.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo4");
        }
        public void CmdFiddleGo5()
        {
            FiddleGo5.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo5");
        }
        public void CmdFiddleGo6()
        {
            FiddleGo6.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo6");
        }
        public void CmdFiddleGo7()
        {
            FiddleGo7.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo7");
        }
        public void CmdFiddleGo8()
        {
            FiddleGo8.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo8");
        }
        public void CmdFiddleGo9()
        {
            FiddleGo9.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo9");
        }
        public void CmdFiddleGo10()
        {
            FiddleGo10.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo10");
        }
        public void CmdFiddleGo11()
        {
            FiddleGo11.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = FiddleGo11");
        }
        public void CmdOcc5BOnTrue()
        {
            Occ5BOnTrue.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = Occ5BOnTrue");
        }
        public void CmdOcc5BOnFalse()
        {
            Occ5BOnFalse.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = Occ5BOnFalse");
        }
        public void CmdOcc6OnTrue()
        {
            Occ6OnTrue.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = Occ6OnTrue");
        }
        public void CmdOcc6OnFalse()
        {
            Occ6OnFalse.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = Occ6OnFalse");
        }
        public void CmdOcc7OnTrue()
        {
            Occ7OnTrue.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = Occ7OnTrue");
        }
        public void CmdOcc7OnFalse()
        {
            Occ7OnFalse.UpdateActuator();
            FiddleYardApplicationLogging.StoreText("FYApp Cmd = Occ7OnFalse");
        }
        


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
        public int[] TrainsOnFY = new int[12];

        public enum State { Idle, Start, Init, Running, Stop, Reset };
        public State State_Machine;
        public string StopApplication = null;

        public bool CL_10_Heart = false;
        public bool F11 = false;
        public bool EOS10 = false;
        public bool EOS11 = false;
        public bool F13 = false;
        public bool F12 = false;
        public bool Block5B = false;
        public bool Block8A = false;
        public bool TrackPower = false;
        public bool Block5BIn = false;
        public bool Block6In = false;
        public bool Block7In = false;
        public bool Resistor = false;        
        public bool Block6 = false;
        public bool Block7 = false;
        public bool F10 = false;
        public bool M10 = false;
        public bool TrackPower15V = false;
        public bool FYCollect = false;
        public int TrackNo = 0;

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
        public FiddleYardApplication(bool instance, iFiddleYardIOHandle iFYIOH)
        {
            m_instance = instance;
            m_iFYIOH = iFYIOH;

            if (TOP == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardApplicationTOP.txt"; //  different logging file per target, this is default
                FiddleYardApplicationLogging = new Log2LoggingFile(path);
            }
            else if (BOT == m_instance)
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardApplicationBOT.txt"; //  different logging file per target, this is default
                FiddleYardApplicationLogging = new Log2LoggingFile(path);
            }

            // Sub programs            
            FYAppInit = new FiddleYardAppInit(this);
            FYAppRun = new FiddleYardAppRun(this);            

            // Instantiate actuators for sending command out to the target or simulator
            FiddleOneLeft = new ActuatorUpdater();
            FiddleOneRight = new ActuatorUpdater();
            Couple = new ActuatorUpdater();
            Uncouple = new ActuatorUpdater();
            FiddleGo1 = new ActuatorUpdater();
            FiddleGo2 = new ActuatorUpdater();
            FiddleGo3 = new ActuatorUpdater();
            FiddleGo4 = new ActuatorUpdater();
            FiddleGo5 = new ActuatorUpdater();
            FiddleGo6 = new ActuatorUpdater();
            FiddleGo7 = new ActuatorUpdater();
            FiddleGo8 = new ActuatorUpdater();
            FiddleGo9 = new ActuatorUpdater();
            FiddleGo10 = new ActuatorUpdater();
            FiddleGo11 = new ActuatorUpdater();
            TrainDetect = new ActuatorUpdater();
            FYStart = new ActuatorUpdater();
            FYStop = new ActuatorUpdater();
            Reset = new ActuatorUpdater();
            Occ5BOnTrue = new ActuatorUpdater();
            Occ5BOnFalse = new ActuatorUpdater();
            Occ6OnTrue = new ActuatorUpdater();
            Occ6OnFalse = new ActuatorUpdater();
            Occ7OnTrue = new ActuatorUpdater();
            Occ7OnFalse = new ActuatorUpdater();
            Recoverd = new ActuatorUpdater();
            Collect = new ActuatorUpdater();

            Track1 = new SensorUpdater();
            Track2 = new SensorUpdater();
            Track3 = new SensorUpdater();
            Track4 = new SensorUpdater();
            Track5 = new SensorUpdater();
            Track6 = new SensorUpdater();
            Track7 = new SensorUpdater();
            Track8 = new SensorUpdater();
            Track9 = new SensorUpdater();
            Track10 = new SensorUpdater();
            Track11 = new SensorUpdater();

            FiddleYardStopped = new MessageUpdater();
            FiddleYardStart = new MessageUpdater();

            //Init and setup FYFORM (after the creation of the sensors and commands)
            if (TOP == m_instance)
                FYFORM.Name = "FiddleYardTOP";
            else if (BOT == m_instance)
                FYFORM.Name = "FiddleYardBOT";

            FYFORM.Show();
            FYFORM.Hide();
            FYFORM.Connect(m_iFYIOH, this); // connect the Form to the FYIOHandle interface           
            
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
            if (m_instance == TOP)
                FYFORM.Location = new System.Drawing.Point(LocX + 8, LocY + 80);
            else if (m_instance == BOT)
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
            #region Inatialize and subsribe interfaces
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

            //Sensors
            Sensor Sns_CL_10_Heart = new Sensor("CL10Heart", " CL 10 Heart ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().CL10Heart.Attach(Sns_CL_10_Heart);
            Sensor Sns_F11 = new Sensor("F11", " F11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().F11.Attach(Sns_F11);
            Sensor Sns_EOS10 = new Sensor("EOS10", " EOS 10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().EOS10.Attach(Sns_EOS10);
            Sensor Sns_EOS11 = new Sensor("EOS11", " EOS 11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().EOS11.Attach(Sns_EOS11);
            Sensor Sns_F13 = new Sensor("F13", " F13 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().F13.Attach(Sns_F13);
            Sensor Sns_F12 = new Sensor("F12", " F12 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().F12.Attach(Sns_F12);
            Sensor Sns_Block5B = new Sensor("Block5B", " Occupied from 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Block5B.Attach(Sns_Block5B);
            Sensor Sns_Block8A = new Sensor("Block8A", " Occupied from 8A ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Block8A.Attach(Sns_Block8A);
            Sensor Sns_TrackPower = new Sensor("TrackPower", " Enable Track ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().TrackPower.Attach(Sns_TrackPower);
            Sensor Sns_Block5BIn = new Sensor("Block5BIn", " Occupied to 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Block5BIn.Attach(Sns_Block5BIn);
            Sensor Sns_Block6In = new Sensor("Block6In", " Occupied to 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Block6In.Attach(Sns_Block6In);
            Sensor Sns_Block7In = new Sensor("Block7In", " Occupied to 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Block7In.Attach(Sns_Block7In);
            Sensor Sns_Resistor = new Sensor("Resistor", " Occupied Resistor ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Resistor.Attach(Sns_Resistor);
            Sensor Sns_Track1 = new Sensor("Track1", " Train On Fiddle Yard Track1 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track1.Attach(Sns_Track1);
            Sensor Sns_Track2 = new Sensor("Track2", " Train On Fiddle Yard Track2 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track2.Attach(Sns_Track2);
            Sensor Sns_Track3 = new Sensor("Track3", " Train On Fiddle Yard Track3 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track3.Attach(Sns_Track3);
            Sensor Sns_Track4 = new Sensor("Track4", " Train On Fiddle Yard Track4 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track4.Attach(Sns_Track4);
            Sensor Sns_Track5 = new Sensor("Track5", " Train On Fiddle Yard Track5 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track5.Attach(Sns_Track5);
            Sensor Sns_Track6 = new Sensor("Track6", " Train On Fiddle Yard Track6 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track6.Attach(Sns_Track6);
            Sensor Sns_Track7 = new Sensor("Track7", " Train On Fiddle Yard Track7 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track7.Attach(Sns_Track7);
            Sensor Sns_Track8 = new Sensor("Track8", " Train On Fiddle Yard Track8 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track8.Attach(Sns_Track8);
            Sensor Sns_Track9 = new Sensor("Track9", " Train On Fiddle Yard Track9 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track9.Attach(Sns_Track9);
            Sensor Sns_Track10 = new Sensor("Track10", " Train On Fiddle Yard Track10 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track10.Attach(Sns_Track10);
            Sensor Sns_Track11 = new Sensor("Track11", " Train On Fiddle Yard Track11 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Track11.Attach(Sns_Track11);
            Sensor Sns_Block6 = new Sensor("Block6", " Occupied from 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Block6.Attach(Sns_Block6);
            Sensor Sns_Block7 = new Sensor("Block7", " Occupied from 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().Block7.Attach(Sns_Block7);
            Sensor Sns_F10 = new Sensor("F10", " F10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().F10.Attach(Sns_F10);
            Sensor Sns_M10 = new Sensor("M10", " M10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().M10.Attach(Sns_M10);
            Sensor Sns_TrackNo = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().TrackNo.Attach(Sns_TrackNo);
            Sensor Sns_CmdBusy = new Sensor("CmdBusy", " uController busy ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().CmdBusy.Attach(Sns_CmdBusy);
            Sensor Sns_TrackPower15V = new Sensor("15VTrackPower", " 15V Track Power ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_iFYIOH.GetIoHandler().TrackPower15V.Attach(Sns_TrackPower15V);
            //Messages
            Message Msg_FiddleOneLeft = new Message("FiddleOneLeftFinished", " Fiddle One Left Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().FiddleOneLeft.Attach(Msg_FiddleOneLeft);
            Message Msg_FiddleOneRight = new Message("FiddleOneRightFinished", " Fiddle One Right Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().FiddleOneRight.Attach(Msg_FiddleOneRight);
            Message Msg_FiddleMultipleLeft = new Message("FiddleMultipleLeftFinished", " Fiddle Multiple Left Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().FiddleMultipleLeft.Attach(Msg_FiddleMultipleLeft);
            Message Msg_FiddleMultipleRight = new Message("FiddleMultipleRightFinished", " Fiddle Multiple Right Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().FiddleMultipleRight.Attach(Msg_FiddleMultipleRight);
            Message Msg_TrainDetection = new Message("TrainDetectionFinished", " Train Detection Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().TrainDetection.Attach(Msg_TrainDetection);
            Message Msg_TrainOn5B = new Message("TrainOn5B", " Train On 5B ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().TrainOn5B.Attach(Msg_TrainOn5B);
            Message Msg_TrainOn8A = new Message("TrainOn8A", " Train On 8A ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().TrainOn8A.Attach(Msg_TrainOn8A);
            Message Msg_FiddleYardReset = new Message("FiddleYardReset", " Fiddle Yard Reset ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().FiddleYardReset.Attach(Msg_FiddleYardReset);
            Message Msg_OccfromBlock6 = new Message("OccfromBlock6", " Occupied from Block6 ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().OccfromBlock6.Attach(Msg_OccfromBlock6);
            Message Msg_SensorF12High = new Message("SensorF12High", " Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().SensorF12High.Attach(Msg_SensorF12High);
            Message Msg_OccfromBlock6AndSensorF12 = new Message("OccfromBlock6AndSensorF12", " Occupied from Block6 And Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().OccfromBlock6AndSensorF12.Attach(Msg_OccfromBlock6AndSensorF12);
            Message Msg_LastTrack = new Message("LastTrack", " Last Track ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().LastTrack.Attach(Msg_LastTrack);
            Message Msg_UniversalError = new Message("UniversalError", " Universal Error ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_iFYIOH.GetIoHandler().UniversalError.Attach(Msg_UniversalError);
            
            #endregion Inatialize and subsribe interfaces

            State_Machine = State.Idle;

            FiddleYardApplicationLogging.StoreText("### Fiddle Yard Application started ###");
            FiddleYardApplicationLogging.StoreText("State_Machine = State.Idle from Start()");

            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            // Set the Interval to [x] miliseconds.
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
                FYFORM.SetMessage("FYApp FYStart", "FiddleYard Auto mode Start");
                State_Machine = State.Start;
                FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Start");
                FYFORM.SetMessage("FYApp FYStart", "FiddleYard init...");
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
                FYFORM.SetMessage("FYApp FYStop", "FiddleYard Auto mode is going to stop...");
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
                    FYFORM.SetMessage("FYApp FYStart", "FiddleYard Reset");
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
                        FYFORM.SetMessage("FYApp FYInit", "FiddleYard init Finished");
                        State_Machine = State.Running;
                        FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Running from State.Start");
                        FYFORM.SetMessage("FYApp FYInit", "FiddleYard Application running...");
                    }
                    break;

                case State.Running:
                    if (FYAppRun.Run(kickapplication, StopApplication) == "Stop")
                    {
                        FYFORM.SetMessage("FiddleYardStopped", "FiddleYard Auto mode is Stopped");  // FYFORM reacts on: FiddleYardStopped setting the buttons visable etc
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

            switch (indicator)
            {
                case "CL10Heart": CL_10_Heart = Convert.ToBoolean(val);
                    break;
                case "F11":  F11 = Convert.ToBoolean(val);
                    break;
                case "EOS10":  EOS10=  Convert.ToBoolean(val);
                    break;
                case "EOS11":  EOS11 = Convert.ToBoolean(val);
                    break;
                case "F13":  F13 = Convert.ToBoolean(val);
                    break;
                case "F12":  F12 = Convert.ToBoolean(val);
                    break;
                case "Block5B":  Block5B = Convert.ToBoolean(val);
                    break;
                case "Block8A":  Block8A = Convert.ToBoolean(val);
                    break;
                case "TrackPower":  TrackPower = Convert.ToBoolean(val);
                    break;
                case "Block5BIn":  Block5BIn = Convert.ToBoolean(val);
                    break;
                case "Block6In":  Block6In = Convert.ToBoolean(val);
                    break;
                case "Block7In":  Block7In = Convert.ToBoolean(val);
                    break;
                case "Resistor":  Resistor = Convert.ToBoolean(val);
                    break;
                case "Block6":  Block6 = Convert.ToBoolean(val);
                    break;
                case "Block7":  Block7 = Convert.ToBoolean(val);
                    break;
                case "F10":  F10 = Convert.ToBoolean(val);
                    break;
                case "M10":  M10 = Convert.ToBoolean(val);
                    break;
                case "TrackPower15V":  TrackPower15V = Convert.ToBoolean(val);
                    break;
                case "Track_No": TrackNo = val;
                    break;
                default:
                    break;
            }

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
            switch (name)                                           // commands who must work always regardless of automatic or manual mode
            {
                case " Collect ": FYCollect = !FYCollect;
                    if (FYCollect == true)
                    {
                        FYFORM.Btn_Collect_TOP.Text = "Collect On";                        
                        FYFORM.SetMessage("Collect On", "Collecting Trains enabled");
                    }
                    else
                    {
                        FYFORM.Btn_Collect_TOP.Text = "Collect Off";                        
                        FYFORM.SetMessage("Collect Off", "Collecting Trains disabled");
                    }
                    break;

                default: break;
            }
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
                case " FiddleOneLeft ": FiddleOneLeft.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleOneRight ": FiddleOneRight.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Couple ": Couple.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Uncouple ": Uncouple.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo1 ": FiddleGo1.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo2 ": FiddleGo2.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo3 ": FiddleGo3.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo4 ": FiddleGo4.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo5 ": FiddleGo5.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo6 ": FiddleGo6.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo7 ": FiddleGo7.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo8 ": FiddleGo8.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo9 ": FiddleGo9.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo10 ": FiddleGo10.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " FiddleGo11 ": FiddleGo11.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " TrainDetect ": TrainDetect.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;                
                case " Reset ": Reset.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ5BOnTrue ": Occ5BOnTrue.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ5BOnFalse ": Occ5BOnFalse.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ6OnTrue ": Occ6OnTrue.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ6OnFalse ": Occ6OnFalse.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ7OnTrue ": Occ7OnTrue.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;
                case " Occ7OnFalse ": Occ7OnFalse.UpdateActuator(); FiddleYardApplicationLogging.StoreText("FYApp State_Machine = State.Idle Cmd = " + name);
                    break;                
                                
                default: break;
            }       
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: UpdateTrainsOnFY
         *               This is only updated when a train detection command is used
         *               because the target or simulator may not change their array
         *               content of trains on the fiddle yard.This is only allowed 
         *               when executing a train detection command. New read trains
         *               will change the status of the array in the target and there
         *               for update the array in the C# application.
         *               When this.application is running and a train has left a 
         *               track on the fiddle yard or a train has been driven in, 
         *               the UpdateTrainsOnFY() is invoked changing the array in C#.
         *               When a train detection is executed during the program the
         *               contents should match.
         *               The FORM indicators are updated according to this array and 
         *               program using Track[x].UpdateSensorValue([val])
         *  Input(s)   :
         *
         *  Output(s)  : Sets the array of the amount of trains on the FY
         *
         *  Returns    :
         *
         *  Pre.Cond.  :
         *
         *  Post.Cond. :
         *
         *  Notes      : The Target may not update these sensors only when a trains detect
         *               is executed
         */
        /*#--------------------------------------------------------------------------#*/
        public void UpdateTrainsOnFY(string Name, int Val, string Log)
        {
            switch (Name)
            {
                case "Track1": if (Val > 0)
                    {
                        TrainsOnFY[1] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[1] = 0;
                    }
                    Track1.UpdateSensorValue(TrainsOnFY[1], true); // Forced only to update FORM, not forced reading from received data (IOHANDLER)
                    break;
                case "Track2": if (Val > 0)
                    {
                        TrainsOnFY[2] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[2] = 0;
                    }
                    Track2.UpdateSensorValue(TrainsOnFY[2], true);
                    break;
                case "Track3": if (Val > 0)
                    {
                        TrainsOnFY[3] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[3] = 0;
                    }
                    Track3.UpdateSensorValue(TrainsOnFY[3], true);
                    break;
                case "Track4": if (Val > 0)
                    {
                        TrainsOnFY[4] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[4] = 0;
                    }
                    Track4.UpdateSensorValue(TrainsOnFY[4], true);
                    break;
                case "Track5": if (Val > 0)
                    {
                        TrainsOnFY[5] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[5] = 0;
                    }
                    Track5.UpdateSensorValue(TrainsOnFY[5], true);
                    break;
                case "Track6": if (Val > 0)
                    {
                        TrainsOnFY[6] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[6] = 0;
                    }
                    Track6.UpdateSensorValue(TrainsOnFY[6], true);
                    break;
                case "Track7": if (Val > 0)
                    {
                        TrainsOnFY[7] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[7] = 0;
                    }
                    Track7.UpdateSensorValue(TrainsOnFY[7], true);
                    break;
                case "Track8": if (Val > 0)
                    {
                        TrainsOnFY[8] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[8] = 0;
                    }
                    Track8.UpdateSensorValue(TrainsOnFY[8], true);
                    break;
                case "Track9": if (Val > 0)
                    {
                        TrainsOnFY[9] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[9] = 0;
                    }
                    Track9.UpdateSensorValue(TrainsOnFY[9], true);
                    break;
                case "Track10": if (Val > 0)
                    {
                        TrainsOnFY[10] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[10] = 0;
                    }
                    Track10.UpdateSensorValue(TrainsOnFY[10], true);
                    break;
                case "Track11": if (Val > 0)
                    {
                        TrainsOnFY[11] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[11] = 0;
                    }
                    Track11.UpdateSensorValue(TrainsOnFY[11], true);
                    break;
                default: break;
            }            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: TrackTrainsOnFYUpdater
         *               Force FORM update
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
        public void TrackTrainsOnFYUpdater()
        {
            Track1.UpdateSensorValue(TrainsOnFY[1], true); // Forced only to update FORM not forced reading from received data
            Track2.UpdateSensorValue(TrainsOnFY[2], true);
            Track3.UpdateSensorValue(TrainsOnFY[3], true);
            Track4.UpdateSensorValue(TrainsOnFY[4], true);
            Track5.UpdateSensorValue(TrainsOnFY[5], true);
            Track6.UpdateSensorValue(TrainsOnFY[6], true);
            Track7.UpdateSensorValue(TrainsOnFY[7], true);
            Track8.UpdateSensorValue(TrainsOnFY[8], true);
            Track9.UpdateSensorValue(TrainsOnFY[9], true);
            Track10.UpdateSensorValue(TrainsOnFY[10], true);
            Track11.UpdateSensorValue(TrainsOnFY[11], true);
        }
    }
}
