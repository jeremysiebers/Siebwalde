using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Siebwalde_Application
{
    public class FiddleYardApplication
    {
        public iFiddleYardController m_iFYCtrl; // connect variable to connect to FYController class for defined interfaces
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

        string m_instance = null;

        private int[] TrainsOnFY = new int[12];

        public FiddleYardApplication(string Instance, iFiddleYardController iFYCtrl)
        {
            m_instance = Instance;
            m_iFYCtrl = iFYCtrl;
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
        }

        public void Start()
        {
            #region Inatialize and subsribe interfaces
            if ("FiddleYardTOP" == m_instance)
            {
                //Instantiate cmd handler for receiving commands from the TOP Form            
                Command Act_CoupleTop = new Command("Couple", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Couple.Attach(Act_CoupleTop);
                Command Act_UncoupleTop = new Command("Uncouple", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Uncouple.Attach(Act_UncoupleTop);
                Command Act_FiddleOneLeftTop = new Command("FiddleOneLeft", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleOneLeft.Attach(Act_FiddleOneLeftTop);
                Command Act_FiddleOneRightTop = new Command("FiddleOneRight", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleOneRight.Attach(Act_FiddleOneRightTop);
                Command Act_FiddleGo1Top = new Command("FiddleGo1", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo1.Attach(Act_FiddleGo1Top);
                Command Act_FiddleGo2Top = new Command("FiddleGo2", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo2.Attach(Act_FiddleGo2Top);
                Command Act_FiddleGo3Top = new Command("FiddleGo3", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo3.Attach(Act_FiddleGo3Top);
                Command Act_FiddleGo4Top = new Command("FiddleGo4", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo4.Attach(Act_FiddleGo4Top);
                Command Act_FiddleGo5Top = new Command("FiddleGo5", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo5.Attach(Act_FiddleGo5Top);
                Command Act_FiddleGo6Top = new Command("FiddleGo6", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo6.Attach(Act_FiddleGo6Top);
                Command Act_FiddleGo7Top = new Command("FiddleGo7", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo7.Attach(Act_FiddleGo7Top);
                Command Act_FiddleGo8Top = new Command("FiddleGo8", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo8.Attach(Act_FiddleGo8Top);
                Command Act_FiddleGo9Top = new Command("FiddleGo9", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo9.Attach(Act_FiddleGo9Top);
                Command Act_FiddleGo10Top = new Command("FiddleGo10", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo10.Attach(Act_FiddleGo10Top);
                Command Act_FiddleGo11Top = new Command("FiddleGo11", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FiddleGo11.Attach(Act_FiddleGo11Top);
                Command Act_TrainDetectTop = new Command("TrainDetect", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().TrainDetect.Attach(Act_TrainDetectTop);
                Command Act_StartTop = new Command("Start", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FYStart.Attach(Act_StartTop);
                Command Act_StopTop = new Command("Stop", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().FYStop.Attach(Act_StopTop);
                Command Act_ResetTop = new Command("Reset", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Reset.Attach(Act_ResetTop);
                Command Act_Bezet5BOnTrueTop = new Command("Bezet5BOnTrue", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Bezet5BOnTrue.Attach(Act_Bezet5BOnTrueTop);
                Command Act_Bezet5BOnFalseTop = new Command("Bezet5BOnFalse", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Bezet5BOnFalse.Attach(Act_Bezet5BOnFalseTop);
                Command Act_Bezet6OnTrueTop = new Command("Bezet6OnTrue", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Bezet6OnTrue.Attach(Act_Bezet6OnTrueTop);
                Command Act_Bezet6OnFalseTop = new Command("Bezet6OnFalse", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Bezet6OnFalse.Attach(Act_Bezet6OnFalseTop);
                Command Act_Bezet7OnTrueTop = new Command("Bezet7OnTrue", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Bezet7OnTrue.Attach(Act_Bezet7OnTrueTop);
                Command Act_Bezet7OnFalseTop = new Command("Bezet7OnFalse", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Bezet7OnFalse.Attach(Act_Bezet7OnFalseTop);
                Command Act_RecoverdTop = new Command("Recoverd", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Recoverd.Attach(Act_RecoverdTop);
                Command Act_CollectTop = new Command("Collect", "FiddleYardTOP", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormTop().Collect.Attach(Act_CollectTop);

                //Sensors
                Sensor Led_CL_10_Heart = new Sensor("LLed_Heart", " CL 10 Heart ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().CL10Heart.Attach(Led_CL_10_Heart);
                Sensor Led_F11 = new Sensor("LLed_F11", " F11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F11.Attach(Led_F11);
                Sensor Led_EOS10 = new Sensor("LLed_EOS10", " EOS 10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().EOS10.Attach(Led_EOS10);
                Sensor Led_EOS11 = new Sensor("LLed_EOS11", " EOS 11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().EOS11.Attach(Led_EOS11);
                Sensor Led_F13 = new Sensor("LLed_F13", " F13 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F13.Attach(Led_F13);
                Sensor Led_F12 = new Sensor("LLed_F12", " F12 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F12.Attach(Led_F12);
                Sensor Led_Block5B = new Sensor("LLed_Block5B", " Occupied from 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block5B.Attach(Led_Block5B);
                Sensor Led_Block8A = new Sensor("LLed_Block8A", " Occupied from 8A ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block8A.Attach(Led_Block8A);
                Sensor Led_TrackPowerTop = new Sensor("LLed_TrackPower", " Enable Track ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackPowerTop.Attach(Led_TrackPowerTop);
                Sensor Led_Block5BIn = new Sensor("LLed_Block5BIn", " Occupied to 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block5BIn.Attach(Led_Block5BIn);
                Sensor Led_Block6In = new Sensor("LLed_Block6In", " Occupied to 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block6In.Attach(Led_Block6In);
                Sensor Led_Block7In = new Sensor("LLed_Block7In", " Occupied to 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block7In.Attach(Led_Block7In);
                Sensor Led_ResistorTop = new Sensor("LLed_Resistor", " Occupied Resistor ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().ResistorTop.Attach(Led_ResistorTop);
                Sensor Led_Track1Top = new Sensor("LLed_Track1", " Trains On Fiddle Yard Track1 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track1Top.Attach(Led_Track1Top);
                Sensor Led_Track2Top = new Sensor("LLed_Track2", " Trains On Fiddle Yard Track2 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track2Top.Attach(Led_Track2Top);
                Sensor Led_Track3Top = new Sensor("LLed_Track3", " Trains On Fiddle Yard Track3 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track3Top.Attach(Led_Track3Top);
                Sensor Led_Track4Top = new Sensor("LLed_Track4", " Trains On Fiddle Yard Track4 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track4Top.Attach(Led_Track4Top);
                Sensor Led_Track5Top = new Sensor("LLed_Track5", " Trains On Fiddle Yard Track5 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track5Top.Attach(Led_Track5Top);
                Sensor Led_Track6Top = new Sensor("LLed_Track6", " Trains On Fiddle Yard Track6 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track6Top.Attach(Led_Track6Top);
                Sensor Led_Track7Top = new Sensor("LLed_Track7", " Trains On Fiddle Yard Track7 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track7Top.Attach(Led_Track7Top);
                Sensor Led_Track8Top = new Sensor("LLed_Track8", " Trains On Fiddle Yard Track8 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track8Top.Attach(Led_Track8Top);
                Sensor Led_Track9Top = new Sensor("LLed_Track9", " Trains On Fiddle Yard Track9 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track9Top.Attach(Led_Track9Top);
                Sensor Led_Track10Top = new Sensor("LLed_Track10", " Trains On Fiddle Yard Track10 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track10Top.Attach(Led_Track10Top);
                Sensor Led_Track11Top = new Sensor("LLed_Track11", " Trains On Fiddle Yard Track11 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track11Top.Attach(Led_Track11Top);
                Sensor Led_Block6 = new Sensor("LLed_Block6", " Occupied from 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block6.Attach(Led_Block6);
                Sensor Led_Block7 = new Sensor("LLed_Block7", " Occupied from 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block7.Attach(Led_Block7);
                Sensor Led_F10 = new Sensor("LLed_F10", " F10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F10.Attach(Led_F10);
                Sensor Led_M10 = new Sensor("LLed_M10", " M10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().M10.Attach(Led_M10);
                Sensor Led_TrackNoTop = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackNoTop.Attach(Led_TrackNoTop);
                Sensor Led_TrackPower15VTOP = new Sensor("LLed_15VTrackPower", " 15V Track Power ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackPower15V.Attach(Led_TrackPower15VTOP);
                //Messages
                Message Msg_FiddleOneLeftTop = new Message("FiddleOneLeft", " Fiddle One Left Ok ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleOneLeftTop.Attach(Msg_FiddleOneLeftTop);
                Message Msg_FiddleOneRightTop = new Message("FiddleOneRight", " Fiddle One Right Ok ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleOneRightTop.Attach(Msg_FiddleOneRightTop);
                Message Msg_FiddleMultipleLeftTop = new Message("FiddleMultipleLeft", " Fiddle Multiple Left Ok ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleMultipleLeftTop.Attach(Msg_FiddleMultipleLeftTop);
                Message Msg_FiddleMultipleRightTop = new Message("FiddleMultipleRight", " Fiddle Multiple Right Ok ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleMultipleRightTop.Attach(Msg_FiddleMultipleRightTop);
                Message Msg_TrainDetectionTop = new Message("TrainDetectionFinished", " Train Detection Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDetectionTop.Attach(Msg_TrainDetectionTop);
                Message Msg_TrainDriveOutFinishedTop = new Message("TrainDriveOut", " Train Drive Out Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutFinishedTop.Attach(Msg_TrainDriveOutFinishedTop);
                Message Msg_TrainDriveInFinishedTop = new Message("TrainDriveIn", " Train Drive In Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInFinishedTop.Attach(Msg_TrainDriveInFinishedTop);
                Message Msg_InitDoneTop = new Message("InitDone", " Init Done ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().InitDoneTop.Attach(Msg_InitDoneTop);
                Message Msg_InitStartedTop = new Message("InitStarted", " Init Started ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().InitStartedTop.Attach(Msg_InitStartedTop);
                Message Msg_TrainOn5BTop = new Message("TrainOn5BTop", " Train On 5B ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainOn5BTop.Attach(Msg_TrainOn5BTop);
                Message Msg_TrainDriveInStartTop = new Message("TrainDriveInStart", " Train Drive In Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInStartTop.Attach(Msg_TrainDriveInStartTop);
                Message Msg_TrainOn8ATop = new Message("TrainOn8A", " Train On 8A ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainOn8ATop.Attach(Msg_TrainOn8ATop);
                Message Msg_TrainDriveOutStartTop = new Message("TrainDriveOut", " Train Drive Out Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutStartTop.Attach(Msg_TrainDriveOutStartTop);
                Message Msg_FiddleYardSoftStartTop = new Message("FiddleYardSoftStart", " Fiddle Yard Soft Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardSoftStartTop.Attach(Msg_FiddleYardSoftStartTop);
                Message Msg_FiddleYardStoppedTop = new Message("FiddleYardStopped", " Fiddle Yard Stopped ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardStoppedTop.Attach(Msg_FiddleYardStoppedTop);
                Message Msg_FiddleYardResetTop = new Message("FiddleYardReset", " Fiddle Yard Reset ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardResetTop.Attach(Msg_FiddleYardResetTop);
                Message Msg_OccfromBlock6Top = new Message("OccfromBlock6", " Occupied from Block6 ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().OccfromBlock6Top.Attach(Msg_OccfromBlock6Top);
                Message Msg_SensorF12HighTop = new Message("SensorF12High", " Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().SensorF12HighTop.Attach(Msg_SensorF12HighTop);
                Message Msg_OccfromBlock6AndSensorF12Top = new Message("OccfromBlock6AndSensorF12", " Occupied from Block6 And Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().OccfromBlock6AndSensorF12Top.Attach(Msg_OccfromBlock6AndSensorF12Top);
                Message Msg_TrainDriveInFailedF12Top = new Message("TrainDriveInFailedF12", " Train Drive In Failed F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInFailedF12Top.Attach(Msg_TrainDriveInFailedF12Top);
                Message Msg_LastTrackTop = new Message("LastTrack", " Last Track ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().LastTrackTop.Attach(Msg_LastTrackTop);
                Message Msg_UniversalErrorTop = new Message("UniversalError", " Universal Error ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().UniversalErrorTop.Attach(Msg_UniversalErrorTop);
                Message Msg_CollectFinishedFYFullTop = new Message("CollectFinishedFYFull", " Collect Finished FY Full ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectFinishedFYFullTop.Attach(Msg_CollectFinishedFYFullTop);
                Message Msg_CollectOnTop = new Message("CollectOn", " Collect On ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectOnTop.Attach(Msg_CollectOnTop);
                Message Msg_CollectOffTop = new Message("CollectOff", " Collect Off ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectOffTop.Attach(Msg_CollectOffTop);
                Message Msg_TrainDriveOutCancelledTop = new Message("TrainDriveOutCancelled", " Train Drive Out Cancelled ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutCancelledTop.Attach(Msg_TrainDriveOutCancelledTop);          
            }
            else if ("FiddleYardBOT" == m_instance)
            {

                //Instantiate cmd handler for receiving commands from the BOT Form
                Command Act_CoupleBot = new Command("Couple", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Couple.Attach(Act_CoupleBot);
                Command Act_UncoupleBot = new Command("Uncouple", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Uncouple.Attach(Act_UncoupleBot);
                Command Act_FiddleOneLeftBot = new Command("FiddleOneLeft", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleOneLeft.Attach(Act_FiddleOneLeftBot);
                Command Act_FiddleOneRightBot = new Command("FiddleOneRight", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleOneRight.Attach(Act_FiddleOneRightBot);
                Command Act_FiddleGo1Bot = new Command("FiddleGo1", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo1.Attach(Act_FiddleGo1Bot);
                Command Act_FiddleGo2Bot = new Command("FiddleGo2", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo2.Attach(Act_FiddleGo2Bot);
                Command Act_FiddleGo3Bot = new Command("FiddleGo3", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo3.Attach(Act_FiddleGo3Bot);
                Command Act_FiddleGo4Bot = new Command("FiddleGo4", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo4.Attach(Act_FiddleGo4Bot);
                Command Act_FiddleGo5Bot = new Command("FiddleGo5", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo5.Attach(Act_FiddleGo5Bot);
                Command Act_FiddleGo6Bot = new Command("FiddleGo6", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo6.Attach(Act_FiddleGo6Bot);
                Command Act_FiddleGo7Bot = new Command("FiddleGo7", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo7.Attach(Act_FiddleGo7Bot);
                Command Act_FiddleGo8Bot = new Command("FiddleGo8", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo8.Attach(Act_FiddleGo8Bot);
                Command Act_FiddleGo9Bot = new Command("FiddleGo9", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo9.Attach(Act_FiddleGo9Bot);
                Command Act_FiddleGo10Bot = new Command("FiddleGo10", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo10.Attach(Act_FiddleGo10Bot);
                Command Act_FiddleGo11Bot = new Command("FiddleGo11", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FiddleGo11.Attach(Act_FiddleGo11Bot);
                Command Act_TrainDetectBot = new Command("TrainDetect", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().TrainDetect.Attach(Act_TrainDetectBot);
                Command Act_StartBot = new Command("Start", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FYStart.Attach(Act_StartBot);
                Command Act_StopBot = new Command("Stop", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().FYStop.Attach(Act_StopBot);
                Command Act_ResetBot = new Command("Reset", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Reset.Attach(Act_ResetBot);
                Command Act_Bezet5BOnTrueBot = new Command("Bezet5BOnTrue", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Bezet5BOnTrue.Attach(Act_Bezet5BOnTrueBot);
                Command Act_Bezet5BOnFalseBot = new Command("Bezet5BOnFalse", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Bezet5BOnFalse.Attach(Act_Bezet5BOnFalseBot);
                Command Act_Bezet6OnTrueBot = new Command("Bezet6OnTrue", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Bezet6OnTrue.Attach(Act_Bezet6OnTrueBot);
                Command Act_Bezet6OnFalseBot = new Command("Bezet6OnFalse", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Bezet6OnFalse.Attach(Act_Bezet6OnFalseBot);
                Command Act_Bezet7OnTrueBot = new Command("Bezet7OnTrue", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Bezet7OnTrue.Attach(Act_Bezet7OnTrueBot);
                Command Act_Bezet7OnFalseBot = new Command("Bezet7OnFalse", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Bezet7OnFalse.Attach(Act_Bezet7OnFalseBot);
                Command Act_RecoverdBot = new Command("Recoverd", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Recoverd.Attach(Act_RecoverdBot);
                Command Act_CollectBot = new Command("Collect", "FiddleYardBOT", (name, layer) => FormCmd(name, layer)); // initialize and subscribe Commands
                m_iFYCtrl.GetFYFormBot().Collect.Attach(Act_CollectBot);

                //Sensors
                Sensor Led_CL_20_Heart = new Sensor("LLed_Heart", " CL 20 Heart ", 0, (name, val, log) => SetLedIndicator(name, val, log));
                m_iFYCtrl.GetIoHandler().CL20Heart.Attach(Led_CL_20_Heart);
                Sensor Led_F21 = new Sensor("LLed_F11", " F21 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F21.Attach(Led_F21);
                Sensor Led_EOS20 = new Sensor("LLed_EOS10", " EOS 20 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().EOS20.Attach(Led_EOS20);
                Sensor Led_EOS21 = new Sensor("LLed_EOS11", " EOS 21 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().EOS21.Attach(Led_EOS21);
                Sensor Led_F23 = new Sensor("LLed_F13", " F23 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F23.Attach(Led_F23);
                Sensor Led_F22 = new Sensor("LLed_F12", " F22 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F22.Attach(Led_F22);
                Sensor Led_Block16B = new Sensor("LLed_Block5B", " Occupied from 16B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block16B.Attach(Led_Block16B);
                Sensor Led_Block19A = new Sensor("LLed_Block8A", " Occupied from 19A ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block19A.Attach(Led_Block19A);
                Sensor Led_TrackPowerBot = new Sensor("LLed_TrackPower", " Enable Track ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackPowerBot.Attach(Led_TrackPowerBot);
                Sensor Led_Block16BIn = new Sensor("LLed_Block5BIn", " Occupied to 16B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block16BIn.Attach(Led_Block16BIn);
                Sensor Led_Block17In = new Sensor("LLed_Block6In", " Occupied to 17 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block17In.Attach(Led_Block17In);
                Sensor Led_Block18In = new Sensor("LLed_Block7In", " Occupied to 18 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block18In.Attach(Led_Block18In);
                Sensor Led_ResistorBot = new Sensor("LLed_Resistor", " Occupied Resistor ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().ResistorBot.Attach(Led_ResistorBot);
                Sensor Led_Track1Bot = new Sensor("LLed_Track1", " Trains On Fiddle Yard Track1 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track1Bot.Attach(Led_Track1Bot);
                Sensor Led_Track2Bot = new Sensor("LLed_Track2", " Trains On Fiddle Yard Track2 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track2Bot.Attach(Led_Track2Bot);
                Sensor Led_Track3Bot = new Sensor("LLed_Track3", " Trains On Fiddle Yard Track3 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track3Bot.Attach(Led_Track3Bot);
                Sensor Led_Track4Bot = new Sensor("LLed_Track4", " Trains On Fiddle Yard Track4 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track4Bot.Attach(Led_Track4Bot);
                Sensor Led_Track5Bot = new Sensor("LLed_Track5", " Trains On Fiddle Yard Track5 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track5Bot.Attach(Led_Track5Bot);
                Sensor Led_Track6Bot = new Sensor("LLed_Track6", " Trains On Fiddle Yard Track6 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track6Bot.Attach(Led_Track6Bot);
                Sensor Led_Track7Bot = new Sensor("LLed_Track7", " Trains On Fiddle Yard Track7 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track7Bot.Attach(Led_Track7Bot);
                Sensor Led_Track8Bot = new Sensor("LLed_Track8", " Trains On Fiddle Yard Track8 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track8Bot.Attach(Led_Track8Bot);
                Sensor Led_Track9Bot = new Sensor("LLed_Track9", " Trains On Fiddle Yard Track9 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track9Bot.Attach(Led_Track9Bot);
                Sensor Led_Track10Bot = new Sensor("LLed_Track10", " Trains On Fiddle Yard Track10 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track10Bot.Attach(Led_Track10Bot);
                Sensor Led_Track11Bot = new Sensor("LLed_Track11", " Trains On Fiddle Yard Track11 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track11Bot.Attach(Led_Track11Bot);
                Sensor Led_Block17 = new Sensor("LLed_Block6", " Occupied from 17 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block17.Attach(Led_Block17);
                Sensor Led_Block18 = new Sensor("LLed_Block7", " Occupied from 18 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Block18.Attach(Led_Block18);
                Sensor Led_F20 = new Sensor("LLed_F10", " F20 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().F20.Attach(Led_F20);
                Sensor Led_M20 = new Sensor("LLed_M10", " M20 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().M20.Attach(Led_M20);
                Sensor Led_TrackNoBot = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackNoBot.Attach(Led_TrackNoBot);
                Sensor Led_TrackPower15VBot = new Sensor("LLed_15VTrackPower", " 15V Track Power ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().TrackPower15V.Attach(Led_TrackPower15VBot);
                //Messages
                Message Msg_FiddleOneLeftBot = new Message("FiddleOneLeft", " Fiddle One Left Ok ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleOneLeftBot.Attach(Msg_FiddleOneLeftBot);
                Message Msg_FiddleOneRightBot = new Message("FiddleOneRight", " Fiddle One Right Ok ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleOneRightBot.Attach(Msg_FiddleOneRightBot);
                Message Msg_FiddleMultipleLeftBot = new Message("FiddleMultipleLeft", " Fiddle Multiple Left Ok ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleMultipleLeftBot.Attach(Msg_FiddleMultipleLeftBot);
                Message Msg_FiddleMultipleRightBot = new Message("FiddleMultipleRight", " Fiddle Multiple Right Ok ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleMultipleRightBot.Attach(Msg_FiddleMultipleRightBot);
                Message Msg_TrainDetectionBot = new Message("TrainDetectionFinished", " Train Detection Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDetectionBot.Attach(Msg_TrainDetectionBot);
                Message Msg_TrainDriveOutFinishedBot = new Message("TrainDriveOut", " Train Drive Out Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutFinishedBot.Attach(Msg_TrainDriveOutFinishedBot);
                Message Msg_TrainDriveInFinishedBot = new Message("TrainDriveIn", " Train Drive In Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInFinishedBot.Attach(Msg_TrainDriveInFinishedBot);
                Message Msg_InitDoneBot = new Message("InitDone", " Init Done ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().InitDoneBot.Attach(Msg_InitDoneBot);
                Message Msg_InitStartedBot = new Message("InitStarted", " Init Started ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().InitStartedBot.Attach(Msg_InitStartedBot);
                Message Msg_TrainOn5BBot = new Message("TrainOn5BBot", " Train On 5B ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainOn5BBot.Attach(Msg_TrainOn5BBot);
                Message Msg_TrainDriveInStartBot = new Message("TrainDriveInStart", " Train Drive In Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInStartBot.Attach(Msg_TrainDriveInStartBot);
                Message Msg_TrainOn8ABot = new Message("TrainOn8A", " Train On 8A ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainOn8ABot.Attach(Msg_TrainOn8ABot);
                Message Msg_TrainDriveOutStartBot = new Message("TrainDriveOut", " Train Drive Out Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutStartBot.Attach(Msg_TrainDriveOutStartBot);
                Message Msg_FiddleYardSoftStartBot = new Message("FiddleYardSoftStart", " Fiddle Yard Soft Start ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardSoftStartBot.Attach(Msg_FiddleYardSoftStartBot);
                Message Msg_FiddleYardSBotpedBot = new Message("FiddleYardSBotped", " Fiddle Yard SBotped ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardStoppedBot.Attach(Msg_FiddleYardSBotpedBot);
                Message Msg_FiddleYardResetBot = new Message("FiddleYardReset", " Fiddle Yard Reset ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().FiddleYardResetBot.Attach(Msg_FiddleYardResetBot);
                Message Msg_OccfromBlock6Bot = new Message("OccfromBlock6", " Occupied from Block6 ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().OccfromBlock6Bot.Attach(Msg_OccfromBlock6Bot);
                Message Msg_SensorF12HighBot = new Message("SensorF12High", " Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().SensorF12HighBot.Attach(Msg_SensorF12HighBot);
                Message Msg_OccfromBlock6AndSensorF12Bot = new Message("OccfromBlock6AndSensorF12", " Occupied from Block6 And Message F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().OccfromBlock6AndSensorF12Bot.Attach(Msg_OccfromBlock6AndSensorF12Bot);
                Message Msg_TrainDriveInFailedF12Bot = new Message("TrainDriveInFailedF12", " Train Drive In Failed F12 High ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveInFailedF12Bot.Attach(Msg_TrainDriveInFailedF12Bot);
                Message Msg_LastTrackBot = new Message("LastTrack", " Last Track ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().LastTrackBot.Attach(Msg_LastTrackBot);
                Message Msg_UniversalErrorBot = new Message("UniversalError", " Universal Error ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().UniversalErrorBot.Attach(Msg_UniversalErrorBot);
                Message Msg_CollectFinishedFYFullBot = new Message("CollectFinishedFYFull", " Collect Finished FY Full ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectFinishedFYFullBot.Attach(Msg_CollectFinishedFYFullBot);
                Message Msg_CollectOnBot = new Message("CollectOn", " Collect On ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectOnBot.Attach(Msg_CollectOnBot);
                Message Msg_CollectOffBot = new Message("CollectOff", " Collect Off ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().CollectOffBot.Attach(Msg_CollectOffBot);
                Message Msg_TrainDriveOutCancelledBot = new Message("TrainDriveOutCancelled", " Train Drive Out Cancelled ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
                m_iFYCtrl.GetIoHandler().TrainDriveOutCancelledBot.Attach(Msg_TrainDriveOutCancelledBot);    
            }
            #endregion Inatialize and subsribe interfaces
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SetLedIndicator and SetMessage are used to catch updates
         *               from target/simulator and process the contents in the
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
            ApplicationUpdate(name, val);
        }
        public void SetMessage(string name, string log)
        {
            int val = 0;
            ApplicationUpdate(name, val);
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
        /*  Description: FormCmd handles the commands recived from the FORM
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
        public void FormCmd(string name, string layer)      // layer is not used becuase the instance is already of the correct layer
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
                case "Start": ApplicationUpdate("FYStart", 0);
                    TrackTrainsOnFYUpdater();
                    break;
                case "Stop": ApplicationUpdate("FYStop", 0);
                    break;
                case "Reset": Reset.UpdateActuator();
                    break;
                case "Bezet5BOnTrue": Bezet5BOnTrue.UpdateActuator();
                    break;
                case "Bezet5BOnFalse": Bezet5BOnFalse.UpdateActuator();
                    break;
                case "Bezet6OnTrue": Bezet6OnTrue.UpdateActuator();
                    break;
                case "Bezet6OnFalse": Bezet6OnFalse.UpdateActuator();
                    break;
                case "Bezet7OnTrue": Bezet7OnTrue.UpdateActuator();
                    break;
                case "Bezet7OnFalse": Bezet7OnFalse.UpdateActuator();
                    break;
                case "Recoverd": ApplicationUpdate("Recoverd", 0);
                    break;
                case "Collect": ApplicationUpdate("Collect", 0);
                    break;                
                default: break;
            }        
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: UpdateTrainsOnFY
         *               This is only updated when a trains detection command is used
         *               The FORM indicators are updated according to this array and 
         *               program
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
                case "LLed_Track1": if (Val > 0)
                    {
                        TrainsOnFY[1] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[1] = 0;
                    }
                    Track1.UpdateSensorValue(TrainsOnFY[1], true); // Forced only to update FORM not forced reading from received data
                    break;
                case "LLed_Track2": if (Val > 0)
                    {
                        TrainsOnFY[2] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[2] = 0;
                    }
                    Track2.UpdateSensorValue(TrainsOnFY[2], true);
                    break;
                case "LLed_Track3": if (Val > 0)
                    {
                        TrainsOnFY[3] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[3] = 0;
                    }
                    Track3.UpdateSensorValue(TrainsOnFY[3], true);
                    break;
                case "LLed_Track4": if (Val > 0)
                    {
                        TrainsOnFY[4] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[4] = 0;
                    }
                    Track4.UpdateSensorValue(TrainsOnFY[4], true);
                    break;
                case "LLed_Track5": if (Val > 0)
                    {
                        TrainsOnFY[5] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[5] = 0;
                    }
                    Track5.UpdateSensorValue(TrainsOnFY[5], true);
                    break;
                case "LLed_Track6": if (Val > 0)
                    {
                        TrainsOnFY[6] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[6] = 0;
                    }
                    Track6.UpdateSensorValue(TrainsOnFY[6], true);
                    break;
                case "LLed_Track7": if (Val > 0)
                    {
                        TrainsOnFY[7] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[7] = 0;
                    }
                    Track7.UpdateSensorValue(TrainsOnFY[7], true);
                    break;
                case "LLed_Track8": if (Val > 0)
                    {
                        TrainsOnFY[8] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[8] = 0;
                    }
                    Track8.UpdateSensorValue(TrainsOnFY[8], true);
                    break;
                case "LLed_Track9": if (Val > 0)
                    {
                        TrainsOnFY[9] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[9] = 0;
                    }
                    Track9.UpdateSensorValue(TrainsOnFY[9], true);
                    break;
                case "LLed_Track10": if (Val > 0)
                    {
                        TrainsOnFY[10] = 1;
                    }
                    else if (Val == 0)
                    {
                        TrainsOnFY[10] = 0;
                    }
                    Track10.UpdateSensorValue(TrainsOnFY[10], true);
                    break;
                case "LLed_Track11": if (Val > 0)
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
        /*  Description: ApplicationUpdate, main application
         * 
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
        public void ApplicationUpdate(string sensor, int val)
        {

        }
    }
}
