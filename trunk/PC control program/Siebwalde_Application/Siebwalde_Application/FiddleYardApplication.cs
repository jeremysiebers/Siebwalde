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
        void Cmd(string name, string layer);
        bool Sensor(string sensor);
        bool FYOcc(string track);
        int TrackNr();
        void StoreText(string text);
    }

    public class FiddleYardApplication : iFiddleYardApplication
    {
        string path = "null";
        public iFiddleYardController m_iFYCtrl; // connect variable to connect to FYController class for defined interfaces
        public FiddleYardAppInit FYAppInit;
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
        public ActuatorUpdater Bezet5BOnTrue;
        public ActuatorUpdater Bezet5BOnFalse;
        public ActuatorUpdater Bezet6OnTrue;
        public ActuatorUpdater Bezet6OnFalse;
        public ActuatorUpdater Bezet7OnTrue;
        public ActuatorUpdater Bezet7OnFalse;
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

        public string m_instance = null;

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
        private int[] TrainsOnFY = new int[12];

        private enum State { Idle, Start, Init, Running, Stop, Reset };
        private State State_Machine;

        private bool CL_10_Heart = false;
        private bool F11 = false;
        private bool EOS10 = false;
        private bool EOS11 = false;
        private bool F13 = false;
        private bool F12 = false;
        private bool Block5B = false;
        private bool Block8A = false;
        private bool TrackPower = false;
        private bool Block5BIn = false;
        private bool Block6In = false;
        private bool Block7In = false;
        private bool Resistor = false;        
        private bool Block6 = false;
        private bool Block7 = false;
        private bool F10 = false;
        private bool M10 = false;
        private bool TrackPower15V = false;
        private int TrackNo = 0;


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
        public FiddleYardApplication(string Instance, iFiddleYardController iFYCtrl)
        {
            m_instance = Instance;
            m_iFYCtrl = iFYCtrl;

            // Sub programs
            FYAppInit = new FiddleYardAppInit(this);//, iFYCtrl.GetFYFormTop);            
            

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
            Bezet5BOnTrue = new ActuatorUpdater();
            Bezet5BOnFalse = new ActuatorUpdater();
            Bezet6OnTrue = new ActuatorUpdater();
            Bezet6OnFalse = new ActuatorUpdater();
            Bezet7OnTrue = new ActuatorUpdater();
            Bezet7OnFalse = new ActuatorUpdater();
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
            if ("FiddleYardTOP" == m_instance)
            {
                path = @"c:\localdata\FiddleYardAppTOPLogging.txt"; // different logging file per target, this is default
                //Instantiate cmd handler for receiving commands from the TOP Form            
                Command Act_Couple = new Command("Couple", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Couple.Attach(Act_Couple);
                Command Act_Uncouple = new Command("Uncouple", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Uncouple.Attach(Act_Uncouple);
                Command Act_FiddleOneLeft = new Command("FiddleOneLeft", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleOneLeft.Attach(Act_FiddleOneLeft);
                Command Act_FiddleOneRight = new Command("FiddleOneRight", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleOneRight.Attach(Act_FiddleOneRight);
                Command Act_FiddleGo1 = new Command("FiddleGo1", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo1.Attach(Act_FiddleGo1);
                Command Act_FiddleGo2 = new Command("FiddleGo2", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo2.Attach(Act_FiddleGo2);
                Command Act_FiddleGo3 = new Command("FiddleGo3", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo3.Attach(Act_FiddleGo3);
                Command Act_FiddleGo4 = new Command("FiddleGo4", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo4.Attach(Act_FiddleGo4);
                Command Act_FiddleGo5 = new Command("FiddleGo5", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo5.Attach(Act_FiddleGo5);
                Command Act_FiddleGo6 = new Command("FiddleGo6", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo6.Attach(Act_FiddleGo6);
                Command Act_FiddleGo7 = new Command("FiddleGo7", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo7.Attach(Act_FiddleGo7);
                Command Act_FiddleGo8 = new Command("FiddleGo8", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo8.Attach(Act_FiddleGo8);
                Command Act_FiddleGo9 = new Command("FiddleGo9", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo9.Attach(Act_FiddleGo9);
                Command Act_FiddleGo10 = new Command("FiddleGo10", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo10.Attach(Act_FiddleGo10);
                Command Act_FiddleGo11 = new Command("FiddleGo11", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo11.Attach(Act_FiddleGo11);
                Command Act_TrainDetect = new Command("TrainDetect", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().TrainDetect.Attach(Act_TrainDetect);
                Command Act_Start = new Command("Start", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FYStart.Attach(Act_Start);
                Command Act_Stop = new Command("Stop", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FYStop.Attach(Act_Stop);
                Command Act_Reset = new Command("Reset", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Reset.Attach(Act_Reset);
                Command Act_Bezet5BOnTrue = new Command("Bezet5BOnTrue", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Occ5BOnTrue.Attach(Act_Bezet5BOnTrue);
                Command Act_Bezet5BOnFalse = new Command("Bezet5BOnFalse", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Occ5BOnFalse.Attach(Act_Bezet5BOnFalse);
                Command Act_Bezet6OnTrue = new Command("Bezet6OnTrue", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Occ6OnTrue.Attach(Act_Bezet6OnTrue);
                Command Act_Bezet6OnFalse = new Command("Bezet6OnFalse", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Occ6OnFalse.Attach(Act_Bezet6OnFalse);
                Command Act_Bezet7OnTrue = new Command("Bezet7OnTrue", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Occ7OnTrue.Attach(Act_Bezet7OnTrue);
                Command Act_Bezet7OnFalse = new Command("Bezet7OnFalse", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Occ7OnFalse.Attach(Act_Bezet7OnFalse);
                Command Act_Recoverd = new Command("Recoverd", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Recoverd.Attach(Act_Recoverd);
                Command Act_Collect = new Command("Collect", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Collect.Attach(Act_Collect);

                //Sensors
                Sensor Sns_CL_10_Heart = new Sensor("CL10Heart", " CL 10 Heart ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().CL10Heart.Attach(Sns_CL_10_Heart);
                Sensor Sns_F11 = new Sensor("F11", " F11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F11.Attach(Sns_F11);
                Sensor Sns_EOS10 = new Sensor("EOS10", " EOS 10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().EOS10.Attach(Sns_EOS10);
                Sensor Sns_EOS11 = new Sensor("EOS11", " EOS 11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().EOS11.Attach(Sns_EOS11);
                Sensor Sns_F13 = new Sensor("F13", " F13 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F13.Attach(Sns_F13);
                Sensor Sns_F12 = new Sensor("F12", " F12 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F12.Attach(Sns_F12);
                Sensor Sns_Block5B = new Sensor("Block5B", " Occupied from 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block5B.Attach(Sns_Block5B);
                Sensor Sns_Block8A = new Sensor("Block8A", " Occupied from 8A ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block8A.Attach(Sns_Block8A);
                Sensor Sns_TrackPower = new Sensor("TrackPower", " Enable Track ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackPowerTop.Attach(Sns_TrackPower);
                Sensor Sns_Block5BIn = new Sensor("Block5BIn", " Occupied to 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block5BIn.Attach(Sns_Block5BIn);
                Sensor Sns_Block6In = new Sensor("Block6In", " Occupied to 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block6In.Attach(Sns_Block6In);
                Sensor Sns_Block7In = new Sensor("Block7In", " Occupied to 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block7In.Attach(Sns_Block7In);
                Sensor Sns_Resistor = new Sensor("Resistor", " Occupied Resistor ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().ResistorTop.Attach(Sns_Resistor);
                Sensor Sns_Track1 = new Sensor("Track1", " Train On Fiddle Yard Track1 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track1Top.Attach(Sns_Track1);
                Sensor Sns_Track2 = new Sensor("Track2", " Train On Fiddle Yard Track2 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track2Top.Attach(Sns_Track2);
                Sensor Sns_Track3 = new Sensor("Track3", " Train On Fiddle Yard Track3 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track3Top.Attach(Sns_Track3);
                Sensor Sns_Track4 = new Sensor("Track4", " Train On Fiddle Yard Track4 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track4Top.Attach(Sns_Track4);
                Sensor Sns_Track5 = new Sensor("Track5", " Train On Fiddle Yard Track5 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track5Top.Attach(Sns_Track5);
                Sensor Sns_Track6 = new Sensor("Track6", " Train On Fiddle Yard Track6 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track6Top.Attach(Sns_Track6);
                Sensor Sns_Track7 = new Sensor("Track7", " Train On Fiddle Yard Track7 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track7Top.Attach(Sns_Track7);
                Sensor Sns_Track8 = new Sensor("Track8", " Train On Fiddle Yard Track8 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track8Top.Attach(Sns_Track8);
                Sensor Sns_Track9 = new Sensor("Track9", " Train On Fiddle Yard Track9 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track9Top.Attach(Sns_Track9);
                Sensor Sns_Track10 = new Sensor("Track10", " Train On Fiddle Yard Track10 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track10Top.Attach(Sns_Track10);
                Sensor Sns_Track11 = new Sensor("Track11", " Train On Fiddle Yard Track11 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track11Top.Attach(Sns_Track11);
                Sensor Sns_Block6 = new Sensor("Block6", " Occupied from 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block6.Attach(Sns_Block6);
                Sensor Sns_Block7 = new Sensor("Block7", " Occupied from 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block7.Attach(Sns_Block7);
                Sensor Sns_F10 = new Sensor("F10", " F10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F10.Attach(Sns_F10);
                Sensor Sns_M10 = new Sensor("M10", " M10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().M10.Attach(Sns_M10);
                Sensor Sns_TrackNo = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackNoTop.Attach(Sns_TrackNo);
                Sensor Sns_TrackPower15V = new Sensor("15VTrackPower", " 15V Track Power ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackPower15V.Attach(Sns_TrackPower15V);
                //Messages
                Message Msg_FiddleOneLeft = new Message("FiddleOneLeftFinished", " Fiddle One Left Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleOneLeftTop.Attach(Msg_FiddleOneLeft);
                Message Msg_FiddleOneRight = new Message("FiddleOneRightFinished", " Fiddle One Right Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleOneRightTop.Attach(Msg_FiddleOneRight);
                Message Msg_FiddleMultipleLeft = new Message("FiddleMultipleLeftFinished", " Fiddle Multiple Left Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleMultipleLeftTop.Attach(Msg_FiddleMultipleLeft);
                Message Msg_FiddleMultipleRight = new Message("FiddleMultipleRightFinished", " Fiddle Multiple Right Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleMultipleRightTop.Attach(Msg_FiddleMultipleRight);
                Message Msg_TrainDetection = new Message("TrainDetectionFinished", " Train Detection Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDetectionTop.Attach(Msg_TrainDetection);
                Message Msg_TrainDriveOutFinished = new Message("TrainDriveOutFinished", " Train Drive Out Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutFinishedTop.Attach(Msg_TrainDriveOutFinished);
                Message Msg_TrainDriveInFinished = new Message("TrainDriveInFinished", " Train Drive In Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInFinishedTop.Attach(Msg_TrainDriveInFinished);
                Message Msg_InitDone = new Message("InitDone", " Init Done ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().InitDoneTop.Attach(Msg_InitDone);
                Message Msg_InitStarted = new Message("InitStarted", " Init Started ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().InitStartedTop.Attach(Msg_InitStarted);
                Message Msg_TrainOn5B = new Message("TrainOn5B", " Train On 5B ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainOn5BTop.Attach(Msg_TrainOn5B);
                Message Msg_TrainDriveInStart = new Message("TrainDriveInStart", " Train Drive In Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInStartTop.Attach(Msg_TrainDriveInStart);
                Message Msg_TrainOn8A = new Message("TrainOn8A", " Train On 8A ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainOn8ATop.Attach(Msg_TrainOn8A);
                Message Msg_TrainDriveOutStart = new Message("TrainDriveOutStart", " Train Drive Out Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutStartTop.Attach(Msg_TrainDriveOutStart);
                //Message Msg_FiddleYardSoftStart = new Message("FiddleYardSoftStart", " Fiddle Yard Soft Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                //m_iFYCtrl.GetIoHandler().FiddleYardSoftStartTop.Attach(Msg_FiddleYardSoftStart);
                //Message Msg_FiddleYardStopped = new Message("FiddleYardStopped", " Fiddle Yard Stopped ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                //m_iFYCtrl.GetIoHandler().FiddleYardStoppedTop.Attach(Msg_FiddleYardStopped);
                Message Msg_FiddleYardReset = new Message("FiddleYardReset", " Fiddle Yard Reset ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardResetTop.Attach(Msg_FiddleYardReset);
                Message Msg_OccfromBlock6 = new Message("OccfromBlock6", " Occupied from Block6 ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().OccfromBlock6Top.Attach(Msg_OccfromBlock6);
                Message Msg_SensorF12High = new Message("SensorF12High", " Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().SensorF12HighTop.Attach(Msg_SensorF12High);
                Message Msg_OccfromBlock6AndSensorF12 = new Message("OccfromBlock6AndSensorF12", " Occupied from Block6 And Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().OccfromBlock6AndSensorF12Top.Attach(Msg_OccfromBlock6AndSensorF12);
                Message Msg_TrainDriveInFailedF12 = new Message("TrainDriveInFailedF12", " Train Drive In Failed F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInFailedF12Top.Attach(Msg_TrainDriveInFailedF12);
                Message Msg_LastTrack = new Message("LastTrack", " Last Track ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().LastTrackTop.Attach(Msg_LastTrack);
                Message Msg_UniversalError = new Message("UniversalError", " Universal Error ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().UniversalErrorTop.Attach(Msg_UniversalError);
                Message Msg_CollectFinishedFYFull = new Message("CollectFinishedFYFull", " Collect Finished FY Full ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectFinishedFYFullTop.Attach(Msg_CollectFinishedFYFull);
                Message Msg_CollectOn = new Message("CollectOn", " Collect On ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectOnTop.Attach(Msg_CollectOn);
                Message Msg_CollectOff = new Message("CollectOff", " Collect Off ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectOffTop.Attach(Msg_CollectOff);
                Message Msg_TrainDriveOutCancelled = new Message("TrainDriveOutCancelled", " Train Drive Out Cancelled ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutCancelledTop.Attach(Msg_TrainDriveOutCancelled);
                Message Msg_TargetAlive = new Message("TargetAlive", " Target Alive ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TargetAliveTop.Attach(Msg_TargetAlive); 
            }
            else if ("FiddleYardBOT" == m_instance)
            {
                path = @"c:\localdata\FiddleYardAppBOTLogging.txt"; // different logging file per target, this is default
                //Instantiate cmd handler for receiving commands from the BOT Form
                Command Act_Couple = new Command("Couple", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Couple.Attach(Act_Couple);
                Command Act_Uncouple = new Command("Uncouple", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Uncouple.Attach(Act_Uncouple);
                Command Act_FiddleOneLeft = new Command("FiddleOneLeft", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleOneLeft.Attach(Act_FiddleOneLeft);
                Command Act_FiddleOneRight = new Command("FiddleOneRight", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleOneRight.Attach(Act_FiddleOneRight);
                Command Act_FiddleGo1 = new Command("FiddleGo1", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo1.Attach(Act_FiddleGo1);
                Command Act_FiddleGo2 = new Command("FiddleGo2", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo2.Attach(Act_FiddleGo2);
                Command Act_FiddleGo3 = new Command("FiddleGo3", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo3.Attach(Act_FiddleGo3);
                Command Act_FiddleGo4 = new Command("FiddleGo4", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo4.Attach(Act_FiddleGo4);
                Command Act_FiddleGo5 = new Command("FiddleGo5", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo5.Attach(Act_FiddleGo5);
                Command Act_FiddleGo6 = new Command("FiddleGo6", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo6.Attach(Act_FiddleGo6);
                Command Act_FiddleGo7 = new Command("FiddleGo7", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo7.Attach(Act_FiddleGo7);
                Command Act_FiddleGo8 = new Command("FiddleGo8", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo8.Attach(Act_FiddleGo8);
                Command Act_FiddleGo9 = new Command("FiddleGo9", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo9.Attach(Act_FiddleGo9);
                Command Act_FiddleGo10 = new Command("FiddleGo10", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo10.Attach(Act_FiddleGo10);
                Command Act_FiddleGo11 = new Command("FiddleGo11", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo11.Attach(Act_FiddleGo11);
                Command Act_TrainDetect = new Command("TrainDetect", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().TrainDetect.Attach(Act_TrainDetect);
                Command Act_Start = new Command("Start", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FYStart.Attach(Act_Start);
                Command Act_Stop = new Command("Stop", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FYStop.Attach(Act_Stop);
                Command Act_Reset = new Command("Reset", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Reset.Attach(Act_Reset);
                Command Act_Bezet5BOnTrue = new Command("Bezet5BOnTrue", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Occ5BOnTrue.Attach(Act_Bezet5BOnTrue);
                Command Act_Bezet5BOnFalse = new Command("Bezet5BOnFalse", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Occ5BOnFalse.Attach(Act_Bezet5BOnFalse);
                Command Act_Bezet6OnTrue = new Command("Bezet6OnTrue", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Occ6OnTrue.Attach(Act_Bezet6OnTrue);
                Command Act_Bezet6OnFalse = new Command("Bezet6OnFalse", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Occ6OnFalse.Attach(Act_Bezet6OnFalse);
                Command Act_Bezet7OnTrue = new Command("Bezet7OnTrue", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Occ7OnTrue.Attach(Act_Bezet7OnTrue);
                Command Act_Bezet7OnFalse = new Command("Bezet7OnFalse", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Occ7OnFalse.Attach(Act_Bezet7OnFalse);
                Command Act_Recoverd = new Command("Recoverd", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Recoverd.Attach(Act_Recoverd);
                Command Act_Collect = new Command("Collect", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Collect.Attach(Act_Collect);

                //Sensors
                Sensor Sns_CL_10_Heart = new Sensor("CL10Heart", " CL 20 Heart ", 0, (name, val, log) => SetLedIndicator(name, val, log));
                m_iFYCtrl.GetIoHandler().CL20Heart.Attach(Sns_CL_10_Heart);
                Sensor Sns_F11 = new Sensor("F11", " F21 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F21.Attach(Sns_F11);
                Sensor Sns_EOS10 = new Sensor("EOS10", " EOS 20 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().EOS20.Attach(Sns_EOS10);
                Sensor Sns_EOS11 = new Sensor("EOS11", " EOS 21 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().EOS21.Attach(Sns_EOS11);
                Sensor Sns_F13 = new Sensor("F13", " F23 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F23.Attach(Sns_F13);
                Sensor Sns_F12 = new Sensor("F12", " F22 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F22.Attach(Sns_F12);
                Sensor Sns_Block5B = new Sensor("Block5B", " Occupied from 16B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block16B.Attach(Sns_Block5B);
                Sensor Sns_Block8A = new Sensor("Block8A", " Occupied from 19A ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block19A.Attach(Sns_Block8A);
                Sensor Sns_TrackPower = new Sensor("TrackPower", " Enable Track ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackPowerBot.Attach(Sns_TrackPower);
                Sensor Sns_Block5BIn = new Sensor("Block5BIn", " Occupied to 16B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block16BIn.Attach(Sns_Block5BIn);
                Sensor Sns_Block6In = new Sensor("Block6In", " Occupied to 17 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block17In.Attach(Sns_Block6In);
                Sensor Sns_Block7In = new Sensor("Block7In", " Occupied to 18 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block18In.Attach(Sns_Block7In);
                Sensor Sns_Resistor = new Sensor("Resistor", " Occupied Resistor ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().ResistorBot.Attach(Sns_Resistor);
                Sensor Sns_Track1 = new Sensor("Track1", " Train On Fiddle Yard Track1 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track1Bot.Attach(Sns_Track1);
                Sensor Sns_Track2 = new Sensor("Track2", " Train On Fiddle Yard Track2 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track2Bot.Attach(Sns_Track2);
                Sensor Sns_Track3 = new Sensor("Track3", " Train On Fiddle Yard Track3 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track3Bot.Attach(Sns_Track3);
                Sensor Sns_Track4 = new Sensor("Track4", " Train On Fiddle Yard Track4 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track4Bot.Attach(Sns_Track4);
                Sensor Sns_Track5 = new Sensor("Track5", " Train On Fiddle Yard Track5 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track5Bot.Attach(Sns_Track5);
                Sensor Sns_Track6 = new Sensor("Track6", " Train On Fiddle Yard Track6 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track6Bot.Attach(Sns_Track6);
                Sensor Sns_Track7 = new Sensor("Track7", " Train On Fiddle Yard Track7 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track7Bot.Attach(Sns_Track7);
                Sensor Sns_Track8 = new Sensor("Track8", " Train On Fiddle Yard Track8 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track8Bot.Attach(Sns_Track8);
                Sensor Sns_Track9 = new Sensor("Track9", " Train On Fiddle Yard Track9 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track9Bot.Attach(Sns_Track9);
                Sensor Sns_Track10 = new Sensor("Track10", " Train On Fiddle Yard Track10 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track10Bot.Attach(Sns_Track10);
                Sensor Sns_Track11 = new Sensor("Track11", " Train On Fiddle Yard Track11 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track11Bot.Attach(Sns_Track11);
                Sensor Sns_Block6 = new Sensor("Block6", " Occupied from 17 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block17.Attach(Sns_Block6);
                Sensor Sns_Block7 = new Sensor("Block7", " Occupied from 18 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block18.Attach(Sns_Block7);
                Sensor Sns_F10 = new Sensor("F10", " F20 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F20.Attach(Sns_F10);
                Sensor Sns_M10 = new Sensor("M10", " M20 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().M20.Attach(Sns_M10);
                Sensor Track_No = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackNoBot.Attach(Track_No);
                Sensor Sns_15VTrackPower = new Sensor("15VTrackPower", " 15V Track Power ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackPower15V.Attach(Sns_15VTrackPower);
                //Messages
                Message Msg_FiddleOneLeft = new Message("FiddleOneLeftFinished", " Fiddle One Left Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleOneLeftBot.Attach(Msg_FiddleOneLeft);
                Message Msg_FiddleOneRight = new Message("FiddleOneRightFinished", " Fiddle One Right Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleOneRightBot.Attach(Msg_FiddleOneRight);
                Message Msg_FiddleMultipleLeft = new Message("FiddleMultipleLeftFinished", " Fiddle Multiple Left Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleMultipleLeftBot.Attach(Msg_FiddleMultipleLeft);
                Message Msg_FiddleMultipleRight = new Message("FiddleMultipleRightFinished", " Fiddle Multiple Right Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleMultipleRightBot.Attach(Msg_FiddleMultipleRight);
                Message Msg_TrainDetection = new Message("TrainDetectionFinished", " Train Detection Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDetectionBot.Attach(Msg_TrainDetection);
                Message Msg_TrainDriveOutFinished = new Message("TrainDriveOutFinished", " Train Drive Out Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutFinishedBot.Attach(Msg_TrainDriveOutFinished);
                Message Msg_TrainDriveInFinished = new Message("TrainDriveInFinished", " Train Drive In Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInFinishedBot.Attach(Msg_TrainDriveInFinished);
                Message Msg_InitDone = new Message("InitDone", " Init Done ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().InitDoneBot.Attach(Msg_InitDone);
                Message Msg_InitStarted = new Message("InitStarted", " Init Started ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().InitStartedBot.Attach(Msg_InitStarted);
                Message Msg_TrainOn5B = new Message("TrainOn5B", " Train On 5B ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainOn5BBot.Attach(Msg_TrainOn5B);
                Message Msg_TrainDriveInStart = new Message("TrainDriveInStart", " Train Drive In Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInStartBot.Attach(Msg_TrainDriveInStart);
                Message Msg_TrainOn8A = new Message("TrainOn8A", " Train On 8A ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainOn8ABot.Attach(Msg_TrainOn8A);
                Message Msg_TrainDriveOutStart = new Message("TrainDriveOutStart", " Train Drive Out Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutStartBot.Attach(Msg_TrainDriveOutStart);
                Message Msg_FiddleYardSoftStart = new Message("FiddleYardSoftStart", " Fiddle Yard Soft Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardSoftStartBot.Attach(Msg_FiddleYardSoftStart);
                Message Msg_FiddleYardSBotped = new Message("FiddleYardStopped", " Fiddle Yard Stopped ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardStoppedBot.Attach(Msg_FiddleYardSBotped);
                Message Msg_FiddleYardReset = new Message("FiddleYardReset", " Fiddle Yard Reset ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardResetBot.Attach(Msg_FiddleYardReset);
                Message Msg_OccfromBlock6 = new Message("OccfromBlock6", " Occupied from Block6 ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().OccfromBlock6Bot.Attach(Msg_OccfromBlock6);
                Message Msg_SensorF12High = new Message("SensorF12High", " Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().SensorF12HighBot.Attach(Msg_SensorF12High);
                Message Msg_OccfromBlock6AndSensorF12 = new Message("OccfromBlock6AndSensorF12", " Occupied from Block6 And Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().OccfromBlock6AndSensorF12Bot.Attach(Msg_OccfromBlock6AndSensorF12);
                Message Msg_TrainDriveInFailedF12 = new Message("TrainDriveInFailedF12", " Train Drive In Failed F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInFailedF12Bot.Attach(Msg_TrainDriveInFailedF12);
                Message Msg_LastTrack = new Message("LastTrack", " Last Track ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().LastTrackBot.Attach(Msg_LastTrack);
                Message Msg_UniversalError = new Message("UniversalError", " Universal Error ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().UniversalErrorBot.Attach(Msg_UniversalError);
                Message Msg_CollectFinishedFYFull = new Message("CollectFinishedFYFull", " Collect Finished FY Full ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectFinishedFYFullBot.Attach(Msg_CollectFinishedFYFull);
                Message Msg_CollectOn = new Message("CollectOn", " Collect On ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectOnBot.Attach(Msg_CollectOn);
                Message Msg_CollectOff = new Message("CollectOff", " Collect Off ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectOffBot.Attach(Msg_CollectOff);
                Message Msg_TrainDriveOutCancelled = new Message("TrainDriveOutCancelled", " Train Drive Out Cancelled ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutCancelledBot.Attach(Msg_TrainDriveOutCancelled);
                Message Msg_TargetAlive = new Message("TargetAlive", " Target Alive ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TargetAliveBot.Attach(Msg_TargetAlive); 
            }
            #endregion Inatialize and subsribe interfaces

            State_Machine = State.Idle;

            StoreText("### Fiddle Yard Application started ###");
            StoreText("State_Machine = State.Idle from Start()");

        }


        /*#--------------------------------------------------------------------------#*/
        /*  Description: ApplicationUpdate, main application
         *               This is the main Fiddle Yard application, directing movements,
         *               controlling the contents of the tracks etc.
         *  Input(s)   : Sensors, actuators, messages and commands
         *               In case of simulator active, target alive is updating
         *               the application.
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
            
            if (kickapplication == "Start")
            {
                FiddleYardStart.UpdateMessage();
                State_Machine = State.Start;
                StoreText("State_Machine = State.Start");
            }
            else if (kickapplication == "Reset" && State_Machine != State.Idle)
            {
                State_Machine = State.Reset;
                StoreText("State_Machine = State.Reset");
            }
                    
            switch (State_Machine)
            {
                case State.Reset:
                    FiddleYardStopped.UpdateMessage();
                    State_Machine = State.Stop;
                    StoreText("State_Machine = State.Stop from State.Reset");
                    break;

                case State.Idle:
                    Cmd(kickapplication, "");
                    break;

                case State.Start:
                    if (FYAppInit.Init(kickapplication) == "Finished")
                    {
                        StoreText("FYAppInit.Init() == Finished");
                        State_Machine = State.Running;
                        StoreText("State_Machine = State.Running from State.Start");
                    }
                    break;

                case State.Running:
                    if (kickapplication == "Stop")
                    {
                        FiddleYardStopped.UpdateMessage();
                        StoreText("kickapplication = Stop");
                        State_Machine = State.Stop;
                        StoreText("State_Machine = State.Stop from State.Running");
                    }
                    break;

                case State.Stop:
                    State_Machine = State.Idle;
                    StoreText("State_Machine = State.Idle from State.Stop");
                    break;

                default: 
                    break;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SetLedIndicator, SetMessage and FormCmd are used to catch 
         *               updates from target/simulator and process the contents in the
         *               main application loop
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
        public void SetLedIndicator(string name, int val, string log)
        {
            switch (name)
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
            ApplicationUpdate(name, val);
        }
        public void SetMessage(string name, string log)
        {
            int val = 0;
            ApplicationUpdate(name, val);
        }
        public void FormCmd(string name, string layer)
        {
            int val = 0;
            ApplicationUpdate(name, val);
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
                case "FiddleOneLeft": FiddleOneLeft.UpdateActuator();
                    break;
                case "FiddleOneRight": FiddleOneRight.UpdateActuator();
                    break;
                case "Couple": Couple.UpdateActuator();
                    break;
                case "Uncouple": Uncouple.UpdateActuator();
                    break;
                case "FiddleGo1": FiddleGo1.UpdateActuator();
                    break;
                case "FiddleGo2": FiddleGo2.UpdateActuator();
                    break;
                case "FiddleGo3": FiddleGo3.UpdateActuator();
                    break;
                case "FiddleGo4": FiddleGo4.UpdateActuator();
                    break;
                case "FiddleGo5": FiddleGo5.UpdateActuator();
                    break;
                case "FiddleGo6": FiddleGo6.UpdateActuator();
                    break;
                case "FiddleGo7": FiddleGo7.UpdateActuator();
                    break;
                case "FiddleGo8": FiddleGo8.UpdateActuator();
                    break;
                case "FiddleGo9": FiddleGo9.UpdateActuator();
                    break;
                case "FiddleGo10": FiddleGo10.UpdateActuator();
                    break;
                case "FiddleGo11": FiddleGo11.UpdateActuator();
                    break;
                case "TrainDetect": TrainDetect.UpdateActuator();
                    break;
                case "Start": //ApplicationUpdate("FYStart", 0);                    
                    break;
                case "Stop": //ApplicationUpdate("FYStop", 0);
                    break;
                case "Reset": Reset.UpdateActuator();
                    break;
                case "Occ5BOnTrue": Bezet5BOnTrue.UpdateActuator();
                    break;
                case "Occ5BOnFalse": Bezet5BOnFalse.UpdateActuator();
                    break;
                case "Occ6OnTrue": Bezet6OnTrue.UpdateActuator();
                    break;
                case "Occ6OnFalse": Bezet6OnFalse.UpdateActuator();
                    break;
                case "Occ7OnTrue": Bezet7OnTrue.UpdateActuator();
                    break;
                case "Occ7OnFalse": Bezet7OnFalse.UpdateActuator();
                    break;
                case "Recoverd": //ApplicationUpdate("Recoverd", 0);
                    break;
                case "Collect": //ApplicationUpdate("Collect", 0);
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

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Sensor
         *               Return the actual sensor status to a sub program
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
        public bool Sensor(string sensor)
        {
            bool _Return = false;

            switch (sensor)
            {
                case "CL_10_Heart": _Return = CL_10_Heart;
                    break;
                case "F11": _Return = F11;
                    break;
                case "EOS10": _Return = EOS10;
                    break;
                case "EOS11": _Return = EOS11;
                    break;
                case "F13": _Return = F13;
                    break;
                case "F12": _Return = F12;
                    break;
                case "Block5B": _Return = Block5B;
                    break;
                case "Block8A": _Return = Block8A;
                    break;
                case "TrackPower": _Return = TrackPower;
                    break;
                case "Block5BIn": _Return = Block5BIn;
                    break;
                case "Block6In": _Return = Block6In;
                    break;
                case "Block7In": _Return = Block7In;
                    break;
                case "Resistor": _Return = Resistor;
                    break;
                case "Block6": _Return = Block6;
                    break;
                case "Block7": _Return = Block7;
                    break;
                case "F10": _Return = F10;
                    break;
                case "M10": _Return = M10;
                    break;
                case "TrackPower15V": _Return = TrackPower15V;
                    break;                
                default: _Return = false;
                    break;
            }
            return _Return;
        }
        public int TrackNr()
        {
            return TrackNo;
        }
        /*#--------------------------------------------------------------------------#*/
        /*  Description: FYOccupation
         *               Return the actual track status to a sub program
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
        public bool FYOcc(string track)
        {
            bool _Return = false;

            switch (track)
            {
                case "Track1": _Return = Convert.ToBoolean(TrainsOnFY[1]);
                    break;
                case "Track2": _Return = Convert.ToBoolean(TrainsOnFY[2]);
                    break;
                case "Track3": _Return = Convert.ToBoolean(TrainsOnFY[3]);
                    break;
                case "Track4": _Return = Convert.ToBoolean(TrainsOnFY[4]);
                    break;
                case "Track5": _Return = Convert.ToBoolean(TrainsOnFY[5]);
                    break;
                case "Track6": _Return = Convert.ToBoolean(TrainsOnFY[6]);
                    break;
                case "Track7": _Return = Convert.ToBoolean(TrainsOnFY[7]);
                    break;
                case "Track8": _Return = Convert.ToBoolean(TrainsOnFY[8]);
                    break;
                case "Track9": _Return = Convert.ToBoolean(TrainsOnFY[9]);
                    break;
                case "Track10": _Return = Convert.ToBoolean(TrainsOnFY[10]);
                    break;
                case "Track11": _Return = Convert.ToBoolean(TrainsOnFY[11]);
                    break;                
                default: _Return = false;
                    break;
            }
            return _Return;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: StoreText
         *               Store application specific logging in seperate file
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
        public void StoreText(string text)
        {
            string m_text = DateTime.Now + " " + text + " " + Environment.NewLine;
            
            try
            {

                using (var fs = new FileStream(path, FileMode.Append))
                {
                    Byte[] info =
                        new UTF8Encoding(true).GetBytes(m_text);
                    fs.Write(info, 0, info.Length);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
