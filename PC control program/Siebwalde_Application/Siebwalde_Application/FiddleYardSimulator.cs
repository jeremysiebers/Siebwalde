using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Siebwalde_Application
{
    public class FiddleYardSimulator
    {
        public iFiddleYardController m_iFYCtrl; // connect variable to connect to FYController class for defined interfaces
        public Action<byte[]> NewData;

        string m_instance = null;
        

        public FiddleYardSimulator(string Instance, iFiddleYardController iFYCtrl)
        {
            m_iFYCtrl = iFYCtrl;    // connect to FYController interface, save interface in variable
            m_instance = Instance;            

            if ("FiddleYardTOP" == m_instance)
            {
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
                Sensor Led_Track1Top = new Sensor("LLed_Track1", " Trains On Fiddle Yard Track1 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track1Top.Attach(Led_Track1Top);
                Sensor Led_Track2Top = new Sensor("LLed_Track2", " Trains On Fiddle Yard Track2 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track2Top.Attach(Led_Track2Top);
                Sensor Led_Track3Top = new Sensor("LLed_Track3", " Trains On Fiddle Yard Track3 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track3Top.Attach(Led_Track3Top);
                Sensor Led_Track4Top = new Sensor("LLed_Track4", " Trains On Fiddle Yard Track4 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track4Top.Attach(Led_Track4Top);
                Sensor Led_Track5Top = new Sensor("LLed_Track5", " Trains On Fiddle Yard Track5 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track5Top.Attach(Led_Track5Top);
                Sensor Led_Track6Top = new Sensor("LLed_Track6", " Trains On Fiddle Yard Track6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track6Top.Attach(Led_Track6Top);
                Sensor Led_Track7Top = new Sensor("LLed_Track7", " Trains On Fiddle Yard Track7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track7Top.Attach(Led_Track7Top);
                Sensor Led_Track8Top = new Sensor("LLed_Track8", " Trains On Fiddle Yard Track8 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track8Top.Attach(Led_Track8Top);
                Sensor Led_Track9Top = new Sensor("LLed_Track9", " Trains On Fiddle Yard Track9 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track9Top.Attach(Led_Track9Top);
                Sensor Led_Track10Top = new Sensor("LLed_Track10", " Trains On Fiddle Yard Track10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track10Top.Attach(Led_Track10Top);
                Sensor Led_Track11Top = new Sensor("LLed_Track11", " Trains On Fiddle Yard Track11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
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
                Message Msg_TrainDetectionTop = new Message("TrainDetection", " Train Detection Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
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
                Sensor Led_Track1Bot = new Sensor("LLed_Track1", " Trains On Fiddle Yard Track1 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track1Bot.Attach(Led_Track1Bot);
                Sensor Led_Track2Bot = new Sensor("LLed_Track2", " Trains On Fiddle Yard Track2 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track2Bot.Attach(Led_Track2Bot);
                Sensor Led_Track3Bot = new Sensor("LLed_Track3", " Trains On Fiddle Yard Track3 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track3Bot.Attach(Led_Track3Bot);
                Sensor Led_Track4Bot = new Sensor("LLed_Track4", " Trains On Fiddle Yard Track4 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track4Bot.Attach(Led_Track4Bot);
                Sensor Led_Track5Bot = new Sensor("LLed_Track5", " Trains On Fiddle Yard Track5 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track5Bot.Attach(Led_Track5Bot);
                Sensor Led_Track6Bot = new Sensor("LLed_Track6", " Trains On Fiddle Yard Track6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track6Bot.Attach(Led_Track6Bot);
                Sensor Led_Track7Bot = new Sensor("LLed_Track7", " Trains On Fiddle Yard Track7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track7Bot.Attach(Led_Track7Bot);
                Sensor Led_Track8Bot = new Sensor("LLed_Track8", " Trains On Fiddle Yard Track8 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track8Bot.Attach(Led_Track8Bot);
                Sensor Led_Track9Bot = new Sensor("LLed_Track9", " Trains On Fiddle Yard Track9 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track9Bot.Attach(Led_Track9Bot);
                Sensor Led_Track10Bot = new Sensor("LLed_Track10", " Trains On Fiddle Yard Track10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
                m_iFYCtrl.GetIoHandler().Track10Bot.Attach(Led_Track10Bot);
                Sensor Led_Track11Bot = new Sensor("LLed_Track11", " Trains On Fiddle Yard Track11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
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
                Message Msg_TrainDetectionBot = new Message("TrainDetection", " Train Detection Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
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
        }

        public void Start()
        {
            
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: CommandToSend
         *               When a command is send to the simulator a program must be
         *               started to simulate that command and send secuential data
         *               to NewData assynchronicaly.
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
        public void CommandToSend(string name, string layer, string cmd)
        {
            byte[] data = new byte[]{ 0x4D, 0x35};
            NewData(data);
            byte[] data2 = new byte[] { 0x4D, 0x36 };
            NewData(data2);
            NewData(data);
            NewData(data2);
        }

        public void SetLedIndicator(string name, int val, string log)
        {
        }

        public void SetMessage(string name, string log)
        {
        }        
    }
}
