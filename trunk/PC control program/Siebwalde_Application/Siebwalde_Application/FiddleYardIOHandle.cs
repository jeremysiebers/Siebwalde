using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Siebwalde_Application
{
    public class FiddleYardIOHandle
    {
        public const string Target = "FIDDLEYARD";
        public Sender FYSender = new Sender(Target);
        private Receiver FYreceiver;        
        private int m_FYReceivingPort;
        private bool m_FYSimulatorActive;
        public iFiddleYardController m_iFYCtrl; // connect variable to connect to FYController class for defined interfaces

        #region Create Sensors TOP/BOT---------------------------------------------------------------------------------------

        // Create sensors TOP
        public SensorUpdater CL10Heart;
        public SensorUpdater F11;
        public SensorUpdater EOS10;
        public SensorUpdater EOS11;
        public SensorUpdater F13;
        public SensorUpdater F12;
        public SensorUpdater Block5B;
        public SensorUpdater Block8A;
        public SensorUpdater TrackPowerTop;
        public SensorUpdater Block5BIn;
        public SensorUpdater Block6In;
        public SensorUpdater Block7In;
        public SensorUpdater ResistorTop;
        public SensorUpdater Track1Top;
        public SensorUpdater Track2Top;
        public SensorUpdater Track3Top;
        public SensorUpdater Track4Top;
        public SensorUpdater Track5Top;
        public SensorUpdater Track6Top;
        public SensorUpdater Track7Top;
        public SensorUpdater Track8Top;
        public SensorUpdater Track9Top;
        public SensorUpdater Track10Top;
        public SensorUpdater Track11Top;
        public SensorUpdater Block6;
        public SensorUpdater Block7;
        public SensorUpdater F10;
        public SensorUpdater M10;
        public SensorUpdater TrackNoTop;
        // Create Messages TOP
        public MessageUpdater FiddleOneLeftTop;
        public MessageUpdater FiddleOneRightTop;
        public MessageUpdater FiddleMultipleLeftTop;
        public MessageUpdater FiddleMultipleRightTop;
        public MessageUpdater TrainDetectionTop;        
        public MessageUpdater TrainDriveOutFinishedTop;
        public MessageUpdater TrainDriveInFinishedTop;
        public MessageUpdater InitDoneTop;
        public MessageUpdater InitStartedTop;
        public MessageUpdater TrainOn5BTop;
        public MessageUpdater TrainDriveInStartTop;
        public MessageUpdater TrainOn8ATop;
        public MessageUpdater TrainDriveOutStartTop;
        //public MessageUpdater FiddleYardStartTop;  --> moved to FYapplication
        //public MessageUpdater FiddleYardStoppedTop;  --> moved to FYapplication
        public MessageUpdater FiddleYardResetTop;
        public MessageUpdater OccfromBlock6Top;
        public MessageUpdater SensorF12HighTop;
        public MessageUpdater OccfromBlock6AndSensorF12Top;
        public MessageUpdater TrainDriveInFailedF12Top;
        public MessageUpdater LastTrackTop;
        public MessageUpdater UniversalErrorTop;
        public MessageUpdater CollectFinishedFYFullTop;
        public MessageUpdater CollectOnTop;
        public MessageUpdater CollectOffTop;
        public MessageUpdater TrainDriveOutCancelledTop;
        public MessageUpdater TargetAliveTop;
                                   
        // Create sensors BOT
        public SensorUpdater CL20Heart;
        public SensorUpdater F21;
        public SensorUpdater EOS20;
        public SensorUpdater EOS21;
        public SensorUpdater F23;
        public SensorUpdater F22;
        public SensorUpdater Block16B;
        public SensorUpdater Block19A;
        public SensorUpdater TrackPowerBot;
        public SensorUpdater Block16BIn;
        public SensorUpdater Block17In;
        public SensorUpdater Block18In;
        public SensorUpdater ResistorBot;
        public SensorUpdater Track1Bot;
        public SensorUpdater Track2Bot;
        public SensorUpdater Track3Bot;
        public SensorUpdater Track4Bot;
        public SensorUpdater Track5Bot;
        public SensorUpdater Track6Bot;
        public SensorUpdater Track7Bot;
        public SensorUpdater Track8Bot;
        public SensorUpdater Track9Bot;
        public SensorUpdater Track10Bot;
        public SensorUpdater Track11Bot;
        public SensorUpdater Block17;
        public SensorUpdater Block18;
        public SensorUpdater F20;
        public SensorUpdater M20;
        public SensorUpdater TrackNoBot;
        // Create Messages BOT
        public MessageUpdater FiddleOneLeftBot;
        public MessageUpdater FiddleOneRightBot;
        public MessageUpdater FiddleMultipleLeftBot;
        public MessageUpdater FiddleMultipleRightBot;
        public MessageUpdater TrainDetectionBot;
        public MessageUpdater TrainDriveOutFinishedBot;
        public MessageUpdater TrainDriveInFinishedBot;
        public MessageUpdater InitDoneBot;
        public MessageUpdater InitStartedBot;
        public MessageUpdater TrainOn5BBot;
        public MessageUpdater TrainDriveInStartBot;
        public MessageUpdater TrainOn8ABot;
        public MessageUpdater TrainDriveOutStartBot;
        public MessageUpdater FiddleYardSoftStartBot;
        public MessageUpdater FiddleYardStoppedBot;
        public MessageUpdater FiddleYardResetBot;
        public MessageUpdater OccfromBlock6Bot;
        public MessageUpdater SensorF12HighBot;
        public MessageUpdater OccfromBlock6AndSensorF12Bot;
        public MessageUpdater TrainDriveInFailedF12Bot;
        public MessageUpdater LastTrackBot;
        public MessageUpdater UniversalErrorBot;
        public MessageUpdater CollectFinishedFYFullBot;
        public MessageUpdater CollectOnBot;
        public MessageUpdater CollectOffBot;
        public MessageUpdater TrainDriveOutCancelledBot;
        public MessageUpdater TargetAliveBot;

        // Creating sensors TOP/BOT
        public SensorUpdater TrackPower15V;

        #endregion Create Sensors TOP/BOT---------------------------------------------------------------------------------------


        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardIOHandle
         *               Constructor
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
        public FiddleYardIOHandle(int FYReceivingPort, bool FYSimulatorActive, iFiddleYardController iFYCtrl)
        {
            m_FYReceivingPort = FYReceivingPort;
            m_FYSimulatorActive = FYSimulatorActive;
            m_iFYCtrl = iFYCtrl;                            // connect to FYController interface, save interface in variable
            FYreceiver = new Receiver(m_FYReceivingPort);
            FYreceiver.Start();           

            #region Instantiate Sensors/Actuators TOP/BOT---------------------------------------------------------------------------------------
            // Instantiate sensors TOP
            CL10Heart = new SensorUpdater();
            F11 = new SensorUpdater();
            EOS10 = new SensorUpdater();
            EOS11 = new SensorUpdater();
            F13 = new SensorUpdater();
            F12 = new SensorUpdater();
            Block5B = new SensorUpdater();
            Block8A = new SensorUpdater();
            TrackPowerTop = new SensorUpdater();
            Block5BIn = new SensorUpdater();
            Block6In = new SensorUpdater();
            Block7In = new SensorUpdater();
            ResistorTop = new SensorUpdater();
            Track1Top = new SensorUpdater();
            Track2Top = new SensorUpdater();
            Track3Top = new SensorUpdater();
            Track4Top = new SensorUpdater();
            Track5Top = new SensorUpdater();
            Track6Top = new SensorUpdater();
            Track7Top = new SensorUpdater();
            Track8Top = new SensorUpdater();
            Track9Top = new SensorUpdater();
            Track10Top = new SensorUpdater();
            Track11Top = new SensorUpdater();
            Block6 = new SensorUpdater();
            Block7 = new SensorUpdater();
            F10 = new SensorUpdater();
            M10 = new SensorUpdater();
            TrackNoTop = new SensorUpdater();
            // Instantiate messages TOP
            FiddleOneLeftTop = new MessageUpdater();
            FiddleOneRightTop = new MessageUpdater();
            FiddleMultipleLeftTop = new MessageUpdater();
            FiddleMultipleRightTop = new MessageUpdater();
            TrainDetectionTop = new MessageUpdater();
            TrainDriveOutFinishedTop = new MessageUpdater();
            TrainDriveInFinishedTop = new MessageUpdater();
            InitDoneTop = new MessageUpdater();
            InitStartedTop = new MessageUpdater();
            TrainOn5BTop = new MessageUpdater();
            TrainDriveInStartTop = new MessageUpdater();
            TrainOn8ATop = new MessageUpdater();
            TrainDriveOutStartTop = new MessageUpdater();
            //FiddleYardStartTop = new MessageUpdater();    --> moved to FYapplication
            //FiddleYardStoppedTop = new MessageUpdater();  --> moved to FYapplication
            FiddleYardResetTop = new MessageUpdater();
            OccfromBlock6Top = new MessageUpdater();
            SensorF12HighTop = new MessageUpdater();
            OccfromBlock6AndSensorF12Top = new MessageUpdater();
            TrainDriveInFailedF12Top = new MessageUpdater();
            LastTrackTop = new MessageUpdater();
            UniversalErrorTop = new MessageUpdater();
            CollectFinishedFYFullTop = new MessageUpdater();
            CollectOnTop = new MessageUpdater();
            CollectOffTop = new MessageUpdater();
            TrainDriveOutCancelledTop = new MessageUpdater();
            TargetAliveTop = new MessageUpdater();

            // Instantiate sensors BOT
            CL20Heart = new SensorUpdater();
            F21 = new SensorUpdater();
            EOS20 = new SensorUpdater();
            EOS21 = new SensorUpdater();
            F23 = new SensorUpdater();
            F22 = new SensorUpdater();
            Block16B = new SensorUpdater();
            Block19A = new SensorUpdater();
            TrackPowerBot = new SensorUpdater();
            Block16BIn = new SensorUpdater();
            Block17In = new SensorUpdater();
            Block18In = new SensorUpdater();
            ResistorBot = new SensorUpdater();
            Track1Bot = new SensorUpdater();
            Track2Bot = new SensorUpdater();
            Track3Bot = new SensorUpdater();
            Track4Bot = new SensorUpdater();
            Track5Bot = new SensorUpdater();
            Track6Bot = new SensorUpdater();
            Track7Bot = new SensorUpdater();
            Track8Bot = new SensorUpdater();
            Track9Bot = new SensorUpdater();
            Track10Bot = new SensorUpdater();
            Track11Bot = new SensorUpdater();
            Block17 = new SensorUpdater();
            Block18 = new SensorUpdater();
            F20 = new SensorUpdater();
            M20 = new SensorUpdater();
            TrackNoBot = new SensorUpdater();
            // Instantiate messages BOT
            FiddleOneLeftBot = new MessageUpdater();
            FiddleOneRightBot = new MessageUpdater();
            FiddleMultipleLeftBot = new MessageUpdater();
            FiddleMultipleRightBot = new MessageUpdater();
            TrainDetectionBot = new MessageUpdater();
            TrainDriveOutFinishedBot = new MessageUpdater();
            TrainDriveInFinishedBot = new MessageUpdater();
            InitDoneBot = new MessageUpdater();
            InitStartedBot = new MessageUpdater();
            TrainOn5BBot = new MessageUpdater();
            TrainDriveInStartBot = new MessageUpdater();
            TrainOn8ABot = new MessageUpdater();
            TrainDriveOutStartBot = new MessageUpdater();
            FiddleYardSoftStartBot = new MessageUpdater();
            FiddleYardStoppedBot = new MessageUpdater();
            FiddleYardResetBot = new MessageUpdater();
            OccfromBlock6Bot = new MessageUpdater();
            SensorF12HighBot = new MessageUpdater();
            OccfromBlock6AndSensorF12Bot = new MessageUpdater();
            TrainDriveInFailedF12Bot = new MessageUpdater();
            LastTrackBot = new MessageUpdater();
            UniversalErrorBot = new MessageUpdater();
            CollectFinishedFYFullBot = new MessageUpdater();
            CollectOnBot = new MessageUpdater();
            CollectOffBot = new MessageUpdater();
            TrainDriveOutCancelledBot = new MessageUpdater();
            TargetAliveBot = new MessageUpdater();

            // Instantiate sensors TOP/BOT
            TrackPower15V = new SensorUpdater();

            #endregion Instantiate Sensors/Actuators TOP/BOT---------------------------------------------------------------------------------------
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: ActuatorCmd
         *               Sends all commands from FYApplication to real target or
         *               to simulator.
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
        void ActuatorCmd(string name, string layer, string cmd)
        {
            if (false == m_FYSimulatorActive)
            {
                FYSender.SendUdp(Encoding.ASCII.GetBytes(cmd));          
            }
            else if (true == m_FYSimulatorActive && "FiddleYardTOP" == layer)
            {
                m_iFYCtrl.GetFYSimulatorTop().CommandToSend(name, layer, cmd);
            }
            else if (true == m_FYSimulatorActive && "FiddleYardBOT" == layer)
            {
                m_iFYCtrl.GetFYSimulatorBot().CommandToSend(name, layer, cmd);
            }        
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: IO Handle start
         *               to couple real target to application to get real sensor feedback
         *               or to couple simulator output back to application
         *               Also reset target/simulator to achieve known startup, target
         *               maybe already be running/initialized
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
        public void Start(bool FYSimulatorActive)
        {
            // Instantiate actuators TOP here, after all source files are generated and itmes are created, otherwise deadlock (egg - chicken story)
            Actuator Act_CoupleTop = new Actuator("Couple", "FiddleYardTOP", "a1\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Couple.Attach(Act_CoupleTop);
            Actuator Act_UncoupleTop = new Actuator("Uncouple", "FiddleYardTOP", "a2\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Uncouple.Attach(Act_UncoupleTop);
            Actuator Act_FiddleOneLeftTop = new Actuator("FiddleOneLeft", "FiddleYardTOP", "a3\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleOneLeft.Attach(Act_FiddleOneLeftTop);
            Actuator Act_FiddleOneRightTop = new Actuator("FiddleOneRight", "FiddleYardTOP", "a4\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleOneRight.Attach(Act_FiddleOneRightTop);
            Actuator Act_FiddleGo1Top = new Actuator("FiddleGo1", "FiddleYardTOP", "a5\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo1.Attach(Act_FiddleGo1Top);
            Actuator Act_FiddleGo2Top = new Actuator("FiddleGo2", "FiddleYardTOP", "a6\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo2.Attach(Act_FiddleGo2Top);
            Actuator Act_FiddleGo3Top = new Actuator("FiddleGo3", "FiddleYardTOP", "a7\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo3.Attach(Act_FiddleGo3Top);
            Actuator Act_FiddleGo4Top = new Actuator("FiddleGo4", "FiddleYardTOP", "a8\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo4.Attach(Act_FiddleGo4Top);
            Actuator Act_FiddleGo5Top = new Actuator("FiddleGo5", "FiddleYardTOP", "a9\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo5.Attach(Act_FiddleGo5Top);
            Actuator Act_FiddleGo6Top = new Actuator("FiddleGo6", "FiddleYardTOP", "aA\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo6.Attach(Act_FiddleGo6Top);
            Actuator Act_FiddleGo7Top = new Actuator("FiddleGo7", "FiddleYardTOP", "aB\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo7.Attach(Act_FiddleGo7Top);
            Actuator Act_FiddleGo8Top = new Actuator("FiddleGo8", "FiddleYardTOP", "aC\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo8.Attach(Act_FiddleGo8Top);
            Actuator Act_FiddleGo9Top = new Actuator("FiddleGo9", "FiddleYardTOP", "aD\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo9.Attach(Act_FiddleGo9Top);
            Actuator Act_FiddleGo10Top = new Actuator("FiddleGo10", "FiddleYardTOP", "aE\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo10.Attach(Act_FiddleGo10Top);
            Actuator Act_FiddleGo11Top = new Actuator("FiddleGo11", "FiddleYardTOP", "aF\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FiddleGo11.Attach(Act_FiddleGo11Top);
            Actuator Act_TrainDetectTop = new Actuator("TrainDetect", "FiddleYardTOP", "aG\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().TrainDetect.Attach(Act_TrainDetectTop);
            Actuator Act_StartTop = new Actuator("Start", "FiddleYardTOP", "aH\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FYStart.Attach(Act_StartTop);
            Actuator Act_StopTop = new Actuator("Stop", "FiddleYardTOP", "aI\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().FYStop.Attach(Act_StopTop);
            Actuator Act_ResetTop = new Actuator("Reset", "FiddleYardTOP", "aJ\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Reset.Attach(Act_ResetTop);
            Actuator Act_Occ5BOnTrueTop = new Actuator("Occ5BOnTrue", "FiddleYardTOP", "aK\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Occ5BOnTrue.Attach(Act_Occ5BOnTrueTop);
            Actuator Act_Occ5BOnFalseTop = new Actuator("Occ5BOnFalse", "FiddleYardTOP", "aL\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Occ5BOnFalse.Attach(Act_Occ5BOnFalseTop);
            Actuator Act_Occ6OnTrueTop = new Actuator("Occ6OnTrue", "FiddleYardTOP", "aM\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Occ6OnTrue.Attach(Act_Occ6OnTrueTop);
            Actuator Act_Occ6OnFalseTop = new Actuator("Occ6OnFalse", "FiddleYardTOP", "aN\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Occ6OnFalse.Attach(Act_Occ6OnFalseTop);
            Actuator Act_Occ7OnTrueTop = new Actuator("Occ7OnTrue", "FiddleYardTOP", "aO\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Occ7OnTrue.Attach(Act_Occ7OnTrueTop);
            Actuator Act_Occ7OnFalseTop = new Actuator("Occ7OnFalse", "FiddleYardTOP", "aP\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Occ7OnFalse.Attach(Act_Occ7OnFalseTop);
            Actuator Act_RecoverdTop = new Actuator("Recoverd", "FiddleYardTOP", "aQ\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Recoverd.Attach(Act_RecoverdTop);
            Actuator Act_CollectTop = new Actuator("Collect", "FiddleYardTOP", "aR\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppTop().Collect.Attach(Act_CollectTop);

            // Instantiate actuators BOT here, after all source files are generated and itmes are created, otherwise deadlock (egg - chicken story)
            Actuator Act_CoupleBot = new Actuator("Couple", "FiddleYardBOT", "b1\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Couple.Attach(Act_CoupleBot);
            Actuator Act_UncoupleBot = new Actuator("Uncouple", "FiddleYardBOT", "b2\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Uncouple.Attach(Act_UncoupleBot);
            Actuator Act_FiddleOneLeftBot = new Actuator("FiddleOneLeft", "FiddleYardBOT", "b3\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleOneLeft.Attach(Act_FiddleOneLeftBot);
            Actuator Act_FiddleOneRightBot = new Actuator("FiddleOneRight", "FiddleYardBOT", "b4\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleOneRight.Attach(Act_FiddleOneRightBot);
            Actuator Act_FiddleGo1Bot = new Actuator("FiddleGo1", "FiddleYardBOT", "b5\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo1.Attach(Act_FiddleGo1Bot);
            Actuator Act_FiddleGo2Bot = new Actuator("FiddleGo2", "FiddleYardBOT", "b6\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo2.Attach(Act_FiddleGo2Bot);
            Actuator Act_FiddleGo3Bot = new Actuator("FiddleGo3", "FiddleYardBOT", "b7\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo3.Attach(Act_FiddleGo3Bot);
            Actuator Act_FiddleGo4Bot = new Actuator("FiddleGo4", "FiddleYardBOT", "b8\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo4.Attach(Act_FiddleGo4Bot);
            Actuator Act_FiddleGo5Bot = new Actuator("FiddleGo5", "FiddleYardBOT", "b9\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo5.Attach(Act_FiddleGo5Bot);
            Actuator Act_FiddleGo6Bot = new Actuator("FiddleGo6", "FiddleYardBOT", "bA\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo6.Attach(Act_FiddleGo6Bot);
            Actuator Act_FiddleGo7Bot = new Actuator("FiddleGo7", "FiddleYardBOT", "bB\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo7.Attach(Act_FiddleGo7Bot);
            Actuator Act_FiddleGo8Bot = new Actuator("FiddleGo8", "FiddleYardBOT", "bC\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo8.Attach(Act_FiddleGo8Bot);
            Actuator Act_FiddleGo9Bot = new Actuator("FiddleGo9", "FiddleYardBOT", "bD\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo9.Attach(Act_FiddleGo9Bot);
            Actuator Act_FiddleGo10Bot = new Actuator("FiddleGo10", "FiddleYardBOT", "bE\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo10.Attach(Act_FiddleGo10Bot);
            Actuator Act_FiddleGo11Bot = new Actuator("FiddleGo11", "FiddleYardBOT", "bF\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FiddleGo11.Attach(Act_FiddleGo11Bot);
            Actuator Act_TrainDetectBot = new Actuator("TrainDetect", "FiddleYardBOT", "bG\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().TrainDetect.Attach(Act_TrainDetectBot);
            Actuator Act_StartBot = new Actuator("Start", "FiddleYardBOT", "bH\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FYStart.Attach(Act_StartBot);
            Actuator Act_StopBot = new Actuator("Stop", "FiddleYardBOT", "bI\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().FYStop.Attach(Act_StopBot);
            Actuator Act_ResetBot = new Actuator("Reset", "FiddleYardBOT", "bJ\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Reset.Attach(Act_ResetBot);
            Actuator Act_Occ5BOnTrueBot = new Actuator("Occ5BOnTrue", "FiddleYardBOT", "bK\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Occ5BOnTrue.Attach(Act_Occ5BOnTrueBot);
            Actuator Act_Occ5BOnFalseBot = new Actuator("Occ5BOnFalse", "FiddleYardBOT", "bL\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Occ5BOnFalse.Attach(Act_Occ5BOnFalseBot);
            Actuator Act_Occ6OnTrueBot = new Actuator("Occ6OnTrue", "FiddleYardBOT", "bM\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Occ6OnTrue.Attach(Act_Occ6OnTrueBot);
            Actuator Act_Occ6OnFalseBot = new Actuator("Occ6OnFalse", "FiddleYardBOT", "bN\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Occ6OnFalse.Attach(Act_Occ6OnFalseBot);
            Actuator Act_Occ7OnTrueBot = new Actuator("Occ7OnTrue", "FiddleYardBOT", "bO\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Occ7OnTrue.Attach(Act_Occ7OnTrueBot);
            Actuator Act_Occ7OnFalseBot = new Actuator("Occ7OnFalse", "FiddleYardBOT", "bP\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Occ7OnFalse.Attach(Act_Occ7OnFalseBot);
            Actuator Act_RecoverdBot = new Actuator("Recoverd", "FiddleYardBOT", "bQ\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Recoverd.Attach(Act_RecoverdBot);
            Actuator Act_CollectBot = new Actuator("Collect", "FiddleYardBOT", "bR\r", (name, layer, cmd) => ActuatorCmd(name, layer, cmd)); // initialize and subscribe actuators
            m_iFYCtrl.GetFYAppBot().Collect.Attach(Act_CollectBot);


            m_FYSimulatorActive = FYSimulatorActive;            

            if (m_FYSimulatorActive == false)
            {
                m_iFYCtrl.GetFYSimulatorTop().NewData -= HandleNewData;
                m_iFYCtrl.GetFYSimulatorBot().NewData -= HandleNewData;
                FYreceiver.NewData += HandleNewData;                
            }
            else if (m_FYSimulatorActive == true)
            {
                FYreceiver.NewData -= HandleNewData;
                m_iFYCtrl.GetFYSimulatorTop().NewData += HandleNewData;
                m_iFYCtrl.GetFYSimulatorBot().NewData += HandleNewData; 
            }
            
            ActuatorCmd("Reset", "FiddleYardTOP", "aJ\r");  // Reset Fiddle Yard TOP layer to reset target in order to sync C# application and C embedded software
            System.Threading.Thread.Sleep(50);              // Add aditional wait time for the target to process the reset command
            ActuatorCmd("Reset", "FiddleYardBOT", "bJ\r");  // Reset Fiddle Yard BOT layer to reset target in order to sync C# application and C embedded software
            System.Threading.Thread.Sleep(50);              // Add aditional wait time for the target to process the reset command
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: HandleNewData
         *               to handle new data from target or from simulator
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
        public void HandleNewData(byte[] b)
        {
            string _b = Encoding.UTF8.GetString(b, 0, b.Length);        // convert received byte array to string array            

            if (_b[0] == 'M' || _b[0] == 'Z')
            {
                if (_b[0] == 'M')
                {
                    CL10Heart.UpdateSensorValue(b[1] & 0x80, false);
                    F11.UpdateSensorValue(b[1] & 0x20, false);
                    EOS10.UpdateSensorValue(b[1] & 0x10, false);
                    EOS11.UpdateSensorValue(b[1] & 0x8, false);
                    F13.UpdateSensorValue(b[1] & 0x2, false);
                }
                else if (_b[0] == 'Z')
                {
                    CL20Heart.UpdateSensorValue(b[1] & 0x80, false);
                    F21.UpdateSensorValue(b[1] & 0x20, false);
                    EOS20.UpdateSensorValue(b[1] & 0x10, false);
                    EOS21.UpdateSensorValue(b[1] & 0x8, false);
                    F23.UpdateSensorValue(b[1] & 0x2, false);
                }
            }

            else if (_b[0] == 'L' || _b[0] == 'Y')
            {
                if (_b[0] == 'L')
                {
                    TrackNoTop.UpdateSensorValue(b[1] & 0xF0, false);
                    F12.UpdateSensorValue(b[1] & 0x8, false);
                    Block5B.UpdateSensorValue(b[1] & 0x4, false);
                    Block8A.UpdateSensorValue(b[1] & 0x2, false);
                }
                else if (_b[0] == 'Y')
                {
                    TrackNoBot.UpdateSensorValue(b[1] & 0xF0, false);
                    F22.UpdateSensorValue(b[1] & 0x8, false);
                    Block16B.UpdateSensorValue(b[1] & 0x4, false);
                    Block19A.UpdateSensorValue(b[1] & 0x2, false);
                }
            }

            else if (_b[0] == 'K' || _b[0] == 'X')
            {
                if (_b[0] == 'K')
                {
                    TrackPowerTop.UpdateSensorValue(b[1] & 0x80, false);
                    Block5BIn.UpdateSensorValue(b[1] & 0x40, false);
                    Block6In.UpdateSensorValue(b[1] & 0x20, false);
                    Block7In.UpdateSensorValue(b[1] & 0x10, false);
                    ResistorTop.UpdateSensorValue(b[1] & 0x08, false);
                    Track1Top.UpdateSensorValue(b[1] & 0x04, false);
                    Track2Top.UpdateSensorValue(b[1] & 0x02, false);
                }
                else if (_b[0] == 'X')
                {
                    TrackPowerBot.UpdateSensorValue(b[1] & 0x80, false);
                    Block16BIn.UpdateSensorValue(b[1] & 0x40, false);
                    Block17In.UpdateSensorValue(b[1] & 0x20, false);
                    Block18In.UpdateSensorValue(b[1] & 0x10, false);
                    ResistorBot.UpdateSensorValue(b[1] & 0x08, false);
                    Track1Bot.UpdateSensorValue(b[1] & 0x04, false);
                    Track2Bot.UpdateSensorValue(b[1] & 0x02, false);
                }
            }

            else if (_b[0] == 'J' || _b[0] == 'W')
            {
                if (_b[0] == 'J')
                {
                    Track4Top.UpdateSensorValue(b[1] & 0x80, false);
                    Track5Top.UpdateSensorValue(b[1] & 0x40, false);
                    Track6Top.UpdateSensorValue(b[1] & 0x20, false);
                    Track7Top.UpdateSensorValue(b[1] & 0x10, false);
                    Track8Top.UpdateSensorValue(b[1] & 0x08, false);
                    Track9Top.UpdateSensorValue(b[1] & 0x04, false);
                    Track10Top.UpdateSensorValue(b[1] & 0x02, false);
                }
                else if (_b[0] == 'W')
                {
                    Track4Bot.UpdateSensorValue(b[1] & 0x80, false);
                    Track5Bot.UpdateSensorValue(b[1] & 0x40, false);
                    Track6Bot.UpdateSensorValue(b[1] & 0x20, false);
                    Track7Bot.UpdateSensorValue(b[1] & 0x10, false);
                    Track8Bot.UpdateSensorValue(b[1] & 0x08, false);
                    Track9Bot.UpdateSensorValue(b[1] & 0x04, false);
                    Track10Bot.UpdateSensorValue(b[1] & 0x02, false);
                }
            }

            else if (_b[0] == 'I' || _b[0] == 'V')
            {
                if (_b[0] == 'I')
                {
                    Block6.UpdateSensorValue(b[1] & 0x80, false);
                    Block7.UpdateSensorValue(b[1] & 0x40, false);
                    //Spare.UpdateSensorValue(b[1] & 0x20, false);
                    F10.UpdateSensorValue(b[1] & 0x10, false);
                    M10.UpdateSensorValue(b[1] & 0x08, false);
                    Track3Top.UpdateSensorValue(b[1] & 0x04, false);
                    Track11Top.UpdateSensorValue(b[1] & 0x02, false);
                }
                else if (_b[0] == 'V')
                {
                    Block17.UpdateSensorValue(b[1] & 0x80, false);
                    Block18.UpdateSensorValue(b[1] & 0x40, false);
                    TrackPower15V.UpdateSensorValue(b[1] & 0x20, false);                    
                    F20.UpdateSensorValue(b[1] & 0x10, false);
                    M20.UpdateSensorValue(b[1] & 0x08, false);
                    Track3Bot.UpdateSensorValue(b[1] & 0x04, false);
                    Track11Bot.UpdateSensorValue(b[1] & 0x02, false);
                }
            }

            else if (_b[0] == 'A' || _b[0] == 'B')
            {
                if (_b[0] == 'A')
                {
                    switch (b[1])
                    {
                        case 0x03: FiddleOneLeftTop.UpdateMessage();
                            break;
                        case 0x04: FiddleOneRightTop.UpdateMessage();
                            break;
                        case 0x05: FiddleMultipleLeftTop.UpdateMessage();
                            break;
                        case 0x06: FiddleMultipleRightTop.UpdateMessage();
                            break;
                        case 0x07: TrainDetectionTop.UpdateMessage();
                            break;
                        case 0x08: TrainDriveOutFinishedTop.UpdateMessage();
                            break;
                        case 0x09: TrainDriveInFinishedTop.UpdateMessage();
                            break;
                        case 0x0B: InitDoneTop.UpdateMessage();
                            break;
                        case 0x0E: InitStartedTop.UpdateMessage();
                            break;
                        case 0x0F: TrainOn5BTop.UpdateMessage();
                            break;
                        case 0x10: TrainDriveInStartTop.UpdateMessage();
                            break;
                        case 0x11: TrainOn8ATop.UpdateMessage();
                            break;
                        case 0x12: TrainDriveOutStartTop.UpdateMessage();
                            break;
                        case 0x13: //FiddleYardSoftStartTop.UpdateMessage();
                            break;
                        case 0x14: //FiddleYardStoppedTop.UpdateMessage();
                            break;
                        case 0x15: FiddleYardResetTop.UpdateMessage();
                            break;
                        case 0x16: OccfromBlock6Top.UpdateMessage();
                            break;
                        case 0x17: SensorF12HighTop.UpdateMessage();
                            break;
                        case 0x18: OccfromBlock6AndSensorF12Top.UpdateMessage();
                            break;
                        case 0x1B: TrainDriveInFailedF12Top.UpdateMessage();
                            break;
                        case 0x21: LastTrackTop.UpdateMessage();
                            break;
                        case 0x23: UniversalErrorTop.UpdateMessage();
                            break;
                        case 0x24: CollectFinishedFYFullTop.UpdateMessage();
                            break;
                        case 0x25: CollectOnTop.UpdateMessage();
                            break;
                        case 0x26: CollectOffTop.UpdateMessage();
                            break;
                        case 0x2F: TrainDriveOutCancelledTop.UpdateMessage();
                            break;
                        case 0x30: TargetAliveTop.UpdateMessage();
                            break;
                        default: break;
                    }

                }
                else if (_b[0] == 'B')
                {
                    switch (b[1])
                    {
                        case 0x03: FiddleOneLeftBot.UpdateMessage();
                            break;
                        case 0x04: FiddleOneRightBot.UpdateMessage();
                            break;
                        case 0x05: FiddleMultipleLeftBot.UpdateMessage();
                            break;
                        case 0x06: FiddleMultipleRightBot.UpdateMessage();
                            break;
                        case 0x07: TrainDetectionBot.UpdateMessage();
                            break;
                        case 0x08: TrainDriveOutFinishedBot.UpdateMessage();
                            break;
                        case 0x09: TrainDriveInFinishedBot.UpdateMessage();
                            break;
                        case 0x0B: InitDoneBot.UpdateMessage();
                            break;
                        case 0x0E: InitStartedBot.UpdateMessage();
                            break;
                        case 0x0F: TrainOn5BBot.UpdateMessage();
                            break;
                        case 0x10: TrainDriveInStartBot.UpdateMessage();
                            break;
                        case 0x11: TrainOn8ABot.UpdateMessage();
                            break;
                        case 0x12: TrainDriveOutStartBot.UpdateMessage();
                            break;
                        case 0x13: FiddleYardSoftStartBot.UpdateMessage();
                            break;
                        case 0x14: FiddleYardStoppedBot.UpdateMessage();
                            break;
                        case 0x15: FiddleYardResetBot.UpdateMessage();
                            break;
                        case 0x16: OccfromBlock6Bot.UpdateMessage();
                            break;
                        case 0x17: SensorF12HighBot.UpdateMessage();
                            break;
                        case 0x18: OccfromBlock6AndSensorF12Bot.UpdateMessage();
                            break;
                        case 0x1B: TrainDriveInFailedF12Bot.UpdateMessage();
                            break;
                        case 0x21: LastTrackBot.UpdateMessage();
                            break;
                        case 0x23: UniversalErrorBot.UpdateMessage();
                            break;
                        case 0x24: CollectFinishedFYFullBot.UpdateMessage();
                            break;
                        case 0x25: CollectOnBot.UpdateMessage();
                            break;
                        case 0x26: CollectOffBot.UpdateMessage();
                            break;
                        case 0x2F: TrainDriveOutCancelledBot.UpdateMessage();
                            break;
                        case 0x30: TargetAliveBot.UpdateMessage();
                            break;
                        default: break;
                    }
                }
            }

            m_iFYCtrl.FYLinkActivityUpdate();
        }
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: ASensor class interface 
     *               to set sensors received from target
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
    public abstract class ASensor
    {

        public delegate void StatusUpdate(int Value, bool ForceUpdate);        
        public event StatusUpdate OnStatusUpdate = null;
        

        public void Attach(Sensor SensorUpdate)
        {
            OnStatusUpdate += new StatusUpdate(SensorUpdate.Update);            
        }

        public void Detach(Sensor SensorUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(SensorUpdate.Update);
            
        }      

        public void Notify(int Value, bool ForceUpdate)
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate(Value, ForceUpdate);
            }
        }        
    }

    public class SensorUpdater : ASensor
    {
        public void UpdateSensorValue(int Value, bool ForceUpdate)
        {
            Notify(Value, ForceUpdate);
        }        
    }

    interface SensorUpdate
    {
        void Update(int NewSensorValue, bool NewForceUpdate);        
    }

    public class Sensor : SensorUpdate
    {
        //Name of the Sensor
        string name;
        //String to be written when updated
        string LogString;
        //Variable to hold actual value
        int Value;
        //When variable has changed generate an event
        Action<string, int, string> m_OnChangedAction;

        public Sensor(string name, string LogString, int Value, Action<string, int, string> OnChangedAction)
        {
            this.name = name;
            this.LogString = LogString;
            this.Value = Value;
            m_OnChangedAction = OnChangedAction;
        }

        public void Update(int NewSensorValue, bool ForceUpdate)
        {
            if (this.Value != NewSensorValue && ForceUpdate != true) //When variable is not equal to stored variable
            {
                this.Value = NewSensorValue;
                m_OnChangedAction(this.name, this.Value, this.LogString);
            }
            else if (ForceUpdate == true)
            {
                this.Value = NewSensorValue;
                m_OnChangedAction(this.name, this.Value, this.LogString);
            }
        }
        public int GetValue()
        {
            return this.Value;
        }
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: BActuator class interface 
     *               to set actuators and send command to target
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
    public abstract class BActuator
    {

        public delegate void StatusUpdate();
        public event StatusUpdate OnStatusUpdate = null;

        public void Attach(Actuator ActuatorUpdate)
        {
            OnStatusUpdate += new StatusUpdate(ActuatorUpdate.Update);
        }

        public void Detach(Actuator ActuatorUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(ActuatorUpdate.Update);
        }

        public void Notify()
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate();
            }
        }
    }

    public class ActuatorUpdater : BActuator
    {
        public void UpdateActuator()
        {
            Notify();
        }
    }

    interface ActuatorUpdate
    {
        void Update();
    }

    public class Actuator : ActuatorUpdate
    {
        //Name of the Actuator
        string name;
        //String of the layer were it was written from
        string Layer;
        //Cmd to be send
        string cmd;
        //When variable has changed generate an event
        Action<string, string, string> m_OnChangedAction;

        public Actuator(string name, string Layer, string cmd, Action<string, string, string> OnChangedAction)
        {
            this.name = name;
            this.Layer = Layer;
            this.cmd = cmd;
            m_OnChangedAction = OnChangedAction;
        }

        public void Update()
        {
            m_OnChangedAction(this.name, this.Layer, this.cmd);            
        }
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: Message class interface 
     *               to push messages
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
    public abstract class CMessage
    {

        public delegate void StatusUpdate();
        public event StatusUpdate OnStatusUpdate = null;

        public void Attach(Message MessageUpdate)
        {
            OnStatusUpdate += new StatusUpdate(MessageUpdate.Update);
        }

        public void Detach(Message MessageUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(MessageUpdate.Update);
        }

        public void Notify()
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate();
            }
        }
    }

    public class MessageUpdater : CMessage
    {
        public void UpdateMessage()
        {
            Notify();
        }
    }

    interface MessageUpdate
    {
        void Update();
    }

    public class Message : MessageUpdate
    {
        //Name of Message
        string name;
        //String to be written when updated
        string logString;        
        //When variable has changed generate an event
        Action<string, string> m_OnChangedAction;

        public Message(string name, string logString, Action<string, string> OnChangedAction)
        {
            this.name = name;
            this.logString = logString;            
            m_OnChangedAction = OnChangedAction;
        }

        public void Update()
        {
            m_OnChangedAction(this.name, this.logString);            
        }
    }

    /*#--------------------------------------------------------------------------#*/
    /*  Description: Command class interface 
     *               to push commands to Application
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
    public abstract class DCommand
    {

        public delegate void StatusUpdate();
        public event StatusUpdate OnStatusUpdate = null;

        public void Attach(Command CommandUpdate)
        {
            OnStatusUpdate += new StatusUpdate(CommandUpdate.Update);
        }

        public void Detach(Command CommandUpdate)
        {
            OnStatusUpdate -= new StatusUpdate(CommandUpdate.Update);
        }

        public void Notify()
        {
            if (OnStatusUpdate != null)
            {
                OnStatusUpdate();
            }
        }
    }

    public class CommandUpdater : DCommand
    {
        public void UpdateCommand()
        {
            Notify();
        }
    }

    interface CommandUpdate
    {
        void Update();
    }

    public class Command : CommandUpdate
    {
        //Command to application
        string Cmd;
        //String of the layer were it was written from
        string Layer;
        //When variable has changed generate an event
        Action<string, string> m_OnChangedAction;

        public Command(string Cmd, string Layer, Action<string, string> OnChangedAction)
        {
            this.Cmd = Cmd;
            this.Layer = Layer;
            m_OnChangedAction = OnChangedAction;
        }

        public void Update()
        {
            m_OnChangedAction(this.Cmd, this.Layer);
        }
    }
}
