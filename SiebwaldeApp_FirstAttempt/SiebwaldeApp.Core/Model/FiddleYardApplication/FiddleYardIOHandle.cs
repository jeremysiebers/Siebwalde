using System.Text;

namespace SiebwaldeApp.Core
{
    public interface iFiddleYardIOHandle
    {        
        bool GetFYSimulatorActive();
        void ActuatorCmd(string name, string cmd);
    }

    public class FiddleYardIOHandle : iFiddleYardIOHandle
    {        
        private string m_instance = null;
        private bool m_FYSimulatorActive = false;
		public FiddleYardIOHandleVariables FYIOHandleVar;
        public iFiddleYardController m_iFYCtrl; // connect variable to connect to FYController class for defined interfaces
        public FiddleYardSimulator FYSimulator; // create Simulator
        public FiddleYardApplication FYApp;      

        public char Identifier1 = 'M';
        public char Identifier2 = 'L';
        public char Identifier3 = 'K';
        public char Identifier4 = 'J';
        public char Identifier5 = 'I';
        public char Identifier6 = 'H';
        public char Identifier7 = 'A';

        public bool GetFYSimulatorActive()
        {
            return m_FYSimulatorActive;
        }

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
        public FiddleYardIOHandle(string instance, iFiddleYardController iFYCtrl)
        {
            m_instance = instance;
            m_iFYCtrl = iFYCtrl;                            // connect to FYController interface, save interface in variable   
         
			FYIOHandleVar = new FiddleYardIOHandleVariables();
            FYSimulator = new FiddleYardSimulator(m_instance, FYIOHandleVar);
            FYApp = new FiddleYardApplication(m_instance, FYIOHandleVar, this);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: IO Handle start
         *               to  Couple  real target to application to get real sensor feedback
         *               or to  Couple  simulator output back to application
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
        
        public void Init(bool FYSimulatorActive)
        {
            m_FYSimulatorActive = FYSimulatorActive;
            
            string Layer = "0";
            if (m_instance == "TOP")
                Layer = "a";
            else if (m_instance == "BOT")
                Layer = "b";

            #region Data and signal subscribers
            // Instantiate actuators  here, after all source files are generated and items are created, otherwise deadlock (egg - chicken story)
            Actuator Act_Couple = new Actuator("Couple",  Layer + "1\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Couple.Attach(Act_Couple);
            Actuator Act_Uncouple = new Actuator("Uncouple",  Layer + "2\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Uncouple.Attach(Act_Uncouple);
            #region old
            //Actuator Act_MIP50_Enable = new Actuator("MIP50_Enable",  Layer + "3\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.MIP50_Enable.Attach(Act_MIP50_Enable);
            //Actuator Act_MIP50_Disable = new Actuator("MIP50_Disable",  Layer + "4\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.MIP50_Disable.Attach(Act_MIP50_Disable);
            //Actuator Act_FiddleGo1 = new Actuator("FiddleGo1",  Layer + "5\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo1.Attach(Act_FiddleGo1);
            //Actuator Act_FiddleGo2 = new Actuator("FiddleGo2",  Layer + "6\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo2.Attach(Act_FiddleGo2);
            //Actuator Act_FiddleGo3 = new Actuator("FiddleGo3",  Layer + "7\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo3.Attach(Act_FiddleGo3);
            //Actuator Act_FiddleGo4 = new Actuator("FiddleGo4",  Layer + "8\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo4.Attach(Act_FiddleGo4);
            //Actuator Act_FiddleGo5 = new Actuator("FiddleGo5",  Layer + "9\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo5.Attach(Act_FiddleGo5);
            //Actuator Act_FiddleGo6 = new Actuator("FiddleGo6",  Layer + "A\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo6.Attach(Act_FiddleGo6);
            //Actuator Act_FiddleGo7 = new Actuator("FiddleGo7",  Layer + "B\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo7.Attach(Act_FiddleGo7);
            //Actuator Act_FiddleGo8 = new Actuator("FiddleGo8",  Layer + "C\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo8.Attach(Act_FiddleGo8);
            //Actuator Act_FiddleGo9 = new Actuator("FiddleGo9",  Layer + "D\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo9.Attach(Act_FiddleGo9);
            //Actuator Act_FiddleGo10 = new Actuator("FiddleGo10",  Layer + "E\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo10.Attach(Act_FiddleGo10);
            //Actuator Act_FiddleGo11 = new Actuator("FiddleGo11",  Layer + "F\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FiddleGo11.Attach(Act_FiddleGo11);
            //Actuator Act_TrainDetect = new Actuator("TrainDetect",  Layer + "G\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.TrainDetect.Attach(Act_TrainDetect);
            //Actuator Act_Start = new Actuator("Start",  Layer + "H\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FYStart.Attach(Act_Start);
            //Actuator Act_Stop = new Actuator("Stop",  Layer + "I\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.FYStop.Attach(Act_Stop);
            #endregion
            Actuator Act_Reset = new Actuator("Reset",  Layer + "5\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Reset.Attach(Act_Reset);
            Actuator Act_Occ5BOnTrue = new Actuator("Occ5BOnTrue",  Layer + "6\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Occ5BOnTrue.Attach(Act_Occ5BOnTrue);
            Actuator Act_Occ5BOnFalse = new Actuator("Occ5BOnFalse",  Layer + "7\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Occ5BOnFalse.Attach(Act_Occ5BOnFalse);
            Actuator Act_Occ6OnTrue = new Actuator("Occ6OnTrue",  Layer + "8\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Occ6OnTrue.Attach(Act_Occ6OnTrue);
            Actuator Act_Occ6OnFalse = new Actuator("Occ6OnFalse",  Layer + "9\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Occ6OnFalse.Attach(Act_Occ6OnFalse);
            Actuator Act_Occ7OnTrue = new Actuator("Occ7OnTrue",  Layer + "A\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Occ7OnTrue.Attach(Act_Occ7OnTrue);
            Actuator Act_Occ7OnFalse = new Actuator("Occ7OnFalse",  Layer + "B\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            FYApp.FYAppVar.Occ7OnFalse.Attach(Act_Occ7OnFalse);
            //Actuator Act_HomeFY = new Actuator("HomeFY",  Layer + "Q\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.HomeFY.Attach(Act_HomeFY);
            //Actuator Act_Collect = new Actuator("Collect",  Layer + "R\r", (name, cmd) => ActuatorCmd(name, cmd)); // initialize and subscribe actuators
            //FYApp.FYAppVar.Collect.Attach(Act_Collect);

            #endregion

            if (m_instance == "TOP")
            {
                Identifier1 = 'M';
                Identifier2 = 'L';
                Identifier3 = 'K';
                Identifier4 = 'J';
                Identifier5 = 'I';
                Identifier6 = 'H';
                Identifier7 = 'A';
            }
            else if (m_instance == "BOT")
            {
                Identifier1 = 'Z';
                Identifier2 = 'Y';
                Identifier3 = 'X';
                Identifier4 = 'W';
                Identifier5 = 'V';
                Identifier6 = 'U';
                Identifier7 = 'B';
            }

            if (m_FYSimulatorActive == false)
            {
                FYSimulator.Stop();
                FYSimulator.NewData -= HandleNewData;
                m_iFYCtrl.GetFYReceiver().NewData += HandleNewData;                
            }
            else if (m_FYSimulatorActive == true)
            {                
                m_iFYCtrl.GetFYReceiver().NewData -= HandleNewData;
                FYSimulator.NewData += HandleNewData;
                //FYSimulator.Start();
            }

            //ActuatorCmd("Reset", Layer + "J\r");            // Reset Fiddle Yard layer to reset target in order to sync C# application and C embedded software --> not required due to all intelligence to FY application
            System.Threading.Thread.Sleep(50);              // Add aditional wait time for the target to process the reset command
            
        }

        /// <summary>
        /// Starting of the timers after all loggers etc are created.
        /// </summary>
        public void Start()
        {
            FYApp.Start();
            if (m_FYSimulatorActive == true)
            {
                FYSimulator.Start();
            }
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
         *  Notes      : Kicked by this application and MIP50
         */
        /*#--------------------------------------------------------------------------#*/
        public void ActuatorCmd(string name, string cmd)
        {
            if (false == m_FYSimulatorActive)
            {
                m_iFYCtrl.GetFYSender().SendUdp(Encoding.ASCII.GetBytes(cmd));                
            }
            else if (true == m_FYSimulatorActive)
            {
                FYSimulator.CommandToSend(name, cmd);
            }
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
            
            if (_b[0] == Identifier1)
            {
                FYIOHandleVar.CL10Heart.UpdateSensorValue(b[1] & 0x80, false);
                FYIOHandleVar.F11.UpdateSensorValue(b[1] & 0x20, false);
                FYIOHandleVar.EOS10.UpdateSensorValue(b[1] & 0x10, false);
                FYIOHandleVar.EOS11.UpdateSensorValue(b[1] & 0x8, false);
                FYIOHandleVar.F13.UpdateSensorValue(b[1] & 0x2, false);
            }

            else if (_b[0] == Identifier2)
            {
                FYIOHandleVar.TrackNo.UpdateSensorValue(b[1] & 0xF0, false);
                FYIOHandleVar.F12.UpdateSensorValue(b[1] & 0x8, false);
                FYIOHandleVar.Block5B.UpdateSensorValue(b[1] & 0x4, false);
                FYIOHandleVar.Block8A.UpdateSensorValue(b[1] & 0x2, false);
            }

            else if (_b[0] == Identifier3)
            {
                FYIOHandleVar.TrackPower.UpdateSensorValue(b[1] & 0x80, false);
                FYIOHandleVar.Block5BIn.UpdateSensorValue(b[1] & 0x40, false);
                FYIOHandleVar.Block6In.UpdateSensorValue(b[1] & 0x20, false);
                FYIOHandleVar.Block7In.UpdateSensorValue(b[1] & 0x10, false);
                FYIOHandleVar.Resistor.UpdateSensorValue(b[1] & 0x08, false);
                //FYIOHandleVar.Track1.UpdateSensorValue(b[1] & 0x04, false);
                //FYIOHandleVar.Track2.UpdateSensorValue(b[1] & 0x02, false);
            }

            else if (_b[0] == Identifier4)
            {
                //FYIOHandleVar.Track4.UpdateSensorValue(b[1] & 0x80, false);
                //FYIOHandleVar.Track5.UpdateSensorValue(b[1] & 0x40, false);
                //FYIOHandleVar.Track6.UpdateSensorValue(b[1] & 0x20, false);
                //FYIOHandleVar.Track7.UpdateSensorValue(b[1] & 0x10, false);
                //FYIOHandleVar.Track8.UpdateSensorValue(b[1] & 0x08, false);
                //FYIOHandleVar.Track9.UpdateSensorValue(b[1] & 0x04, false);
                //FYIOHandleVar.Track10.UpdateSensorValue(b[1] & 0x02, false);
            }

            else if (_b[0] == Identifier5)
            {
                FYIOHandleVar.Block6.UpdateSensorValue(b[1] & 0x80, false);
                FYIOHandleVar.Block7.UpdateSensorValue(b[1] & 0x40, false);
                FYIOHandleVar.TrackPower15V.UpdateSensorValue(b[1] & 0x20, false);
                FYIOHandleVar.F10.UpdateSensorValue(b[1] & 0x10, false);
                FYIOHandleVar.M10.UpdateSensorValue(b[1] & 0x08, false);
                //FYIOHandleVar.Track3.UpdateSensorValue(b[1] & 0x04, false);
                //FYIOHandleVar.Track11.UpdateSensorValue(b[1] & 0x02, false);
            }

            else if (_b[0] == Identifier6)
            {
                FYIOHandleVar.Mip50Rec.UpdateSensorValue(b[1] & 0xFF, true);        // Set this force to true to receive and update everytime data is received from MIP50!
            }

            else if (_b[0] == Identifier7)
            {
                switch (b[1])
                {
                    case 0x01: FYIOHandleVar.FiddleYardReset.UpdateMessage();
                        break;
                    case 0x02: FYIOHandleVar.uControllerReady.UpdateMessage();
                        break;
                    case 0x03: FYIOHandleVar.TrackPower15VDown.UpdateMessage();
                        break;
                    default: break;
                }
            }
            
            //m_iFYCtrl.FYLinkActivityUpdate();
        }
    }    
}
