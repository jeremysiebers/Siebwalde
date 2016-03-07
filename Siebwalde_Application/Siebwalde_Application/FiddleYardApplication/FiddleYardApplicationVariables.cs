using System;
using System.Drawing;

namespace Siebwalde_Application
{

    //public interface iFiddleYardApplicationVariables
    //{
    //    PublicBool FYHomed { get; set; }
    //}

    public class FiddleYardApplicationVariables //: iFiddleYardApplicationVariables
    {
        private FiddleYardIOHandleVariables m_FYIOHandleVar;                // connect variable to connect to FYIOH class for defined variables       

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
        public CommandUpdater CmdFiddleOneLeft;
        public CommandUpdater CmdFiddleOneRight;
        public CommandUpdater CmdCouple;
        public CommandUpdater CmdUncouple;
        public CommandUpdater CmdFiddleGo1;
        public CommandUpdater CmdFiddleGo2;
        public CommandUpdater CmdFiddleGo3;
        public CommandUpdater CmdFiddleGo4;
        public CommandUpdater CmdFiddleGo5;
        public CommandUpdater CmdFiddleGo6;
        public CommandUpdater CmdFiddleGo7;
        public CommandUpdater CmdFiddleGo8;
        public CommandUpdater CmdFiddleGo9;
        public CommandUpdater CmdFiddleGo10;
        public CommandUpdater CmdFiddleGo11;
        public CommandUpdater CmdTrainDetect;
        public CommandUpdater CmdFYStart;
        public CommandUpdater CmdFYStop;
        public CommandUpdater CmdReset;
        public CommandUpdater CmdOcc5BOnTrue;
        public CommandUpdater CmdOcc5BOnFalse;
        public CommandUpdater CmdOcc6OnTrue;
        public CommandUpdater CmdOcc6OnFalse;
        public CommandUpdater CmdOcc7OnTrue;
        public CommandUpdater CmdOcc7OnFalse;       
        public CommandUpdater CmdCollect;
        public CommandUpdater CmdHomeFY;

        public ActuatorUpdater FiddleOneLeft;
        public ActuatorUpdater FiddleOneRight;
        public ActuatorUpdater Couple;
        public ActuatorUpdater Uncouple;
        //public ActuatorUpdater FiddleGo1;
        //public ActuatorUpdater FiddleGo2;
        //public ActuatorUpdater FiddleGo3;
        //public ActuatorUpdater FiddleGo4;
        //public ActuatorUpdater FiddleGo5;
        //public ActuatorUpdater FiddleGo6;
        //public ActuatorUpdater FiddleGo7;
        //public ActuatorUpdater FiddleGo8;
        //public ActuatorUpdater FiddleGo9;
        //public ActuatorUpdater FiddleGo10;
        //public ActuatorUpdater FiddleGo11;
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
        public ActuatorUpdater HomeFY;
        public ActuatorUpdater Collect;
        // Create FORM updaters, these are uncoupled from the target and used to update the FYForm
        public SensorUpdater CL10Heart;
        public SensorUpdater F11;
        public SensorUpdater EOS10;
        public SensorUpdater EOS11;
        public SensorUpdater F13;
        public SensorUpdater F12;
        public SensorUpdater Block5B;
        public SensorUpdater Block8A;
        public SensorUpdater TrackPower;
        public SensorUpdater Block5BIn;
        public SensorUpdater Block6In;
        public SensorUpdater Block7In;
        public SensorUpdater Resistor;
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
        public SensorUpdater Block6;
        public SensorUpdater Block7;
        public SensorUpdater F10;
        public SensorUpdater M10;
        public SensorUpdater TrackNo;
        public SensorUpdater CmdBusy;
        public SensorUpdater TrackPower15V;
        public SensorUpdater TrainDriveOutPointer;
        public SensorUpdater ReceivedDataFromMip50;

        public MessageUpdater FiddleYardStopped;
        public MessageUpdater FiddleYardStart;
        public MessageUpdater FiddleYardNotHomed;
        public MessageUpdater FiddleYardTrainObstruction;
        public MessageUpdater FiddleYardTrackAligned;
        public MessageUpdater TrainHasLeftFiddleYardSuccessfully;
        public MessageUpdater EMOPressed15VTrackPowerDown;
        public MessageUpdater EMOPressed15VTrackPowerUp;
        public MessageUpdater FiddleYardAutoModeStart;
        public MessageUpdater FiddleYardInit;
        public MessageUpdater FiddleYardAutoModeIsGoingToStop;
        public MessageUpdater FiddleYardReset;
        public MessageUpdater FiddleYardInitFinished;
        public MessageUpdater FiddleYardApplicationRunning;
        public MessageUpdater FiddleYardAutoModeIsStopped;
        public MessageUpdater CollectingTrainsEnabled;
        public MessageUpdater CollectingTrainsDisabled;
        public MessageUpdater FYNotHomed;   // Message to FORM
        public MessageUpdater TrainDetection;
        public MessageUpdater FiddleYardHomingFinished;
        public MessageUpdater TrainDetectionStarted;

        public CommandUpdater FormCollect;          // pass Form command through FiddleYardApplicationVariables, in case Form gets replaced or closed

        public int[] iTrainsOnFY = new int[12];
        public int[] icheckBoxTrack = new int[12];
        public bool bCL_10_Heart = false;
        public bool bF11 = false;
        public bool bEOS10 = false;
        public bool bEOS11 = false;
        public bool bF13 = false;
        public bool bF12 = false;
        public bool bBlock5B = false;
        public bool bBlock8A = false;
        public bool bTrackPower = false;
        public bool bBlock5BIn = false;
        public bool bBlock6In = false;
        public bool bBlock7In = false;
        public bool bResistor = false;
        public bool bBlock6 = false;
        public bool bBlock7 = false;
        public bool bF10 = false;
        public bool bM10 = false;
        public bool bTrackPower15V = false;
        public bool bFYCollect = false;
        public int iTrackNo = 0;
        public int iTrainDriveOutPointer = 5;        

        public Color TrackOccupiedColor = Siebwalde_Application.Properties.Settings.Default.SETxCOLORxTRACKxOCCUPIED;
        public Color TrackNotInitializedColor = Siebwalde_Application.Properties.Settings.Default.SETxCOLORxTRACKxNOTxINITIALIZED;
        public Color TrackNotActiveColor = Siebwalde_Application.Properties.Settings.Default.SETxCOLORxTRACKxNOTxACTIVE;
        public Color TrackDisabledColor = Siebwalde_Application.Properties.Settings.Default.SETxCOLORxTRACKxDISABLED;
        public Color TrackDisabledNotOccupiedColor = Siebwalde_Application.Properties.Settings.Default.SETxCOLORxTRACKxDISABLEDxNOTxOCCUPIED;

        public PublicBool FYHomed = new PublicBool();

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardVariables constructor
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
        public FiddleYardApplicationVariables(FiddleYardIOHandleVariables FYIOHandleVar)
        {
            m_FYIOHandleVar = FYIOHandleVar;            

            // Instantiate Commands from FYFORM
            CmdFiddleOneLeft = new CommandUpdater();
            CmdFiddleOneRight = new CommandUpdater();
            CmdCouple = new CommandUpdater();
            CmdUncouple = new CommandUpdater();
            CmdFiddleGo1 = new CommandUpdater();
            CmdFiddleGo2 = new CommandUpdater();
            CmdFiddleGo3 = new CommandUpdater();
            CmdFiddleGo4 = new CommandUpdater();
            CmdFiddleGo5 = new CommandUpdater();
            CmdFiddleGo6 = new CommandUpdater();
            CmdFiddleGo7 = new CommandUpdater();
            CmdFiddleGo8 = new CommandUpdater();
            CmdFiddleGo9 = new CommandUpdater();
            CmdFiddleGo10 = new CommandUpdater();
            CmdFiddleGo11 = new CommandUpdater();
            CmdTrainDetect = new CommandUpdater();
            CmdFYStart = new CommandUpdater();
            CmdFYStop = new CommandUpdater();
            CmdReset = new CommandUpdater();
            CmdOcc5BOnTrue = new CommandUpdater();
            CmdOcc5BOnFalse = new CommandUpdater();
            CmdOcc6OnTrue = new CommandUpdater();
            CmdOcc6OnFalse = new CommandUpdater();
            CmdOcc7OnTrue = new CommandUpdater();
            CmdOcc7OnFalse = new CommandUpdater();
            CmdCollect = new CommandUpdater();
            CmdHomeFY = new CommandUpdater();

            // Instantiate actuators for sending command out to the target or simulator            
            FiddleOneLeft = new ActuatorUpdater();
            FiddleOneRight = new ActuatorUpdater();
            Couple = new ActuatorUpdater();
            Uncouple = new ActuatorUpdater();
            //FiddleGo1 = new ActuatorUpdater();
            //FiddleGo2 = new ActuatorUpdater();
            //FiddleGo3 = new ActuatorUpdater();
            //FiddleGo4 = new ActuatorUpdater();
            //FiddleGo5 = new ActuatorUpdater();
            //FiddleGo6 = new ActuatorUpdater();
            //FiddleGo7 = new ActuatorUpdater();
            //FiddleGo8 = new ActuatorUpdater();
            //FiddleGo9 = new ActuatorUpdater();
            //FiddleGo10 = new ActuatorUpdater();
            //FiddleGo11 = new ActuatorUpdater();
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
            HomeFY = new ActuatorUpdater();
            Collect = new ActuatorUpdater();

            CL10Heart = new SensorUpdater();
            F11 = new SensorUpdater();
            EOS10 = new SensorUpdater();
            EOS11 = new SensorUpdater();
            F13 = new SensorUpdater();
            F12 = new SensorUpdater();
            Block5B = new SensorUpdater();
            Block8A = new SensorUpdater();
            TrackPower = new SensorUpdater();
            Block5BIn = new SensorUpdater();
            Block6In = new SensorUpdater();
            Block7In = new SensorUpdater();
            Resistor = new SensorUpdater();
            // Trackx sensor is used internally in C#, not from target anymore
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
            Block6 = new SensorUpdater();
            Block7 = new SensorUpdater();
            F10 = new SensorUpdater();
            M10 = new SensorUpdater();
            TrackNo = new SensorUpdater();
            TrackPower15V = new SensorUpdater();
            CmdBusy = new SensorUpdater();
            TrainDriveOutPointer = new SensorUpdater();
            ReceivedDataFromMip50 = new SensorUpdater();

            FiddleYardStopped = new MessageUpdater();
            FiddleYardStart = new MessageUpdater();
            FiddleYardNotHomed = new MessageUpdater();
            FiddleYardTrainObstruction = new MessageUpdater();
            FiddleYardTrackAligned = new MessageUpdater();
            TrainHasLeftFiddleYardSuccessfully = new MessageUpdater();
            EMOPressed15VTrackPowerDown = new MessageUpdater();
            EMOPressed15VTrackPowerUp = new MessageUpdater();
            FiddleYardAutoModeStart = new MessageUpdater();
            FiddleYardInit = new MessageUpdater();
            FiddleYardAutoModeIsGoingToStop = new MessageUpdater();
            FiddleYardReset = new MessageUpdater();
            FiddleYardInitFinished = new MessageUpdater();
            FiddleYardApplicationRunning = new MessageUpdater();
            FiddleYardAutoModeIsStopped = new MessageUpdater();
            CollectingTrainsEnabled = new MessageUpdater();
            CollectingTrainsDisabled = new MessageUpdater();
            FYNotHomed = new MessageUpdater();
            TrainDetection = new MessageUpdater();
            FiddleYardHomingFinished = new MessageUpdater();
            TrainDetectionStarted = new MessageUpdater();

            FormCollect = new CommandUpdater();
            
            //Sensors
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
            //Sensor Sns_Track1 = new Sensor("Track1", " Train On Fiddle Yard Track1 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track1.Attach(Sns_Track1);
            //Sensor Sns_Track2 = new Sensor("Track2", " Train On Fiddle Yard Track2 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track2.Attach(Sns_Track2);
            //Sensor Sns_Track3 = new Sensor("Track3", " Train On Fiddle Yard Track3 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track3.Attach(Sns_Track3);
            //Sensor Sns_Track4 = new Sensor("Track4", " Train On Fiddle Yard Track4 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track4.Attach(Sns_Track4);
            //Sensor Sns_Track5 = new Sensor("Track5", " Train On Fiddle Yard Track5 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track5.Attach(Sns_Track5);
            //Sensor Sns_Track6 = new Sensor("Track6", " Train On Fiddle Yard Track6 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track6.Attach(Sns_Track6);
            //Sensor Sns_Track7 = new Sensor("Track7", " Train On Fiddle Yard Track7 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track7.Attach(Sns_Track7);
            //Sensor Sns_Track8 = new Sensor("Track8", " Train On Fiddle Yard Track8 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track8.Attach(Sns_Track8);
            //Sensor Sns_Track9 = new Sensor("Track9", " Train On Fiddle Yard Track9 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track9.Attach(Sns_Track9);
            //Sensor Sns_Track10 = new Sensor("Track10", " Train On Fiddle Yard Track10 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track10.Attach(Sns_Track10);
            //Sensor Sns_Track11 = new Sensor("Track11", " Train On Fiddle Yard Track11 ", 0, (name, val, log) => UpdateTrainsOnFY(name, val, log)); // initialize and subscribe sensors
            //m_FYIOHandleVar.Track11.Attach(Sns_Track11);
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

            Command Act_Collect = new Command(" Collect ", (name) => FormCmd(name)); // Catch Form command pass to subscribers
            CmdCollect.Attach(Act_Collect);

            Colorc Clr_FYTrackOccupiedColorSetting = new Colorc(Color.Transparent, "Clr_FYTrackOccupiedColorSetting", (NewColor, log) => SetColorIndicator(NewColor, log));
            Siebwalde_Application.Properties.Settings.Default.SWSetColorTrainOccupied.Attach(Clr_FYTrackOccupiedColorSetting);

            Colorc Clr_FYTrackNotInitializedColorSetting = new Colorc(Color.Transparent, "Clr_FYTrackNotInitializedColorSetting", (NewColor, log) => SetColorIndicator(NewColor, log));
            Siebwalde_Application.Properties.Settings.Default.SWSetColorTrackNotInitialized.Attach(Clr_FYTrackNotInitializedColorSetting);

            Colorc Clr_FYTrackNotActiveColorSetting = new Colorc(Color.Transparent, "Clr_FYTrackNotActiveColorSetting", (NewColor, log) => SetColorIndicator(NewColor, log));
            Siebwalde_Application.Properties.Settings.Default.SWSetColorTrackNotActive.Attach(Clr_FYTrackNotActiveColorSetting);

            Colorc Clr_FYTrackDisabledColorSetting = new Colorc(Color.Transparent, "Clr_FYTrackDisabledColorSetting", (NewColor, log) => SetColorIndicator(NewColor, log));
            Siebwalde_Application.Properties.Settings.Default.SWSetColorTrackDisabled.Attach(Clr_FYTrackDisabledColorSetting);

            Colorc Clr_FYTrackDisabledNotOccupiedColorSetting = new Colorc(Color.Transparent, "Clr_FYTrackDisabledNotOccupiedColorSetting", (NewColor, log) => SetColorIndicator(NewColor, log));
            Siebwalde_Application.Properties.Settings.Default.SWSetColorTrackDisabledNotOccupied.Attach(Clr_FYTrackDisabledNotOccupiedColorSetting);
            

            for (int i = 1; i < 12; i++)
            {
                icheckBoxTrack[i] = 1;      // Init all tracks disabled
            }
                        
            FYHomed.BoolVariable = false;   // Init variable
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SetColorIndicator(NewColor, log)
         *               Force all colors to all sub routines
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
        public void SetColorIndicator(Color NewColor, string log)
        {
            switch (log)
            {
                case "Clr_FYTrackOccupiedColorSetting":
                    TrackOccupiedColor = NewColor;
                    break;

                case "Clr_FYTrackNotInitializedColorSetting":
                    TrackNotInitializedColor = NewColor;
                    break;

                case "Clr_FYTrackNotActiveColorSetting":
                    TrackNotActiveColor = NewColor;
                    break;

                case "Clr_FYTrackDisabledColorSetting":
                    TrackDisabledColor = NewColor;
                    break;

                case "Clr_FYTrackDisabledNotOccupiedColorSetting":
                    TrackDisabledNotOccupiedColor = NewColor;
                    break;

                default: break;
            }
            Block5B.UpdateSensorValue(Convert.ToInt16(bBlock5B), true);
            Block8A.UpdateSensorValue(Convert.ToInt16(bBlock8A), true);
            Block6.UpdateSensorValue(Convert.ToInt16(bBlock6), true);
            Block7.UpdateSensorValue(Convert.ToInt16(bBlock7), true);
            TrackTrainsOnFYUpdater();
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FormCmd(string name)
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
        public void FormCmd(string name)
        {
            switch (name)                                           // commands who must work always regardless of automatic or manual mode
            {
                case " Collect ": 
                    bFYCollect = !bFYCollect;
                    FormCollect.UpdateCommand();
                    if (bFYCollect == true)
                    {
                        CollectingTrainsEnabled.UpdateMessage();}
                    else{
                        CollectingTrainsDisabled.UpdateMessage();}
                    break;

                default: break;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleGo(int TrackNo)
         *               Get command from C# train drive program and send FY to 
         *               required track.
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
        //public void FiddleGo(int TrackNo)
        //{
        //    switch (TrackNo)
        //    {
        //        case 1: FiddleGo1.UpdateActuator();
        //            break;

        //        case 2: FiddleGo2.UpdateActuator();
        //            break;

        //        case 3: FiddleGo3.UpdateActuator();
        //            break;

        //        case 4: FiddleGo4.UpdateActuator();
        //            break;

        //        case 5: FiddleGo5.UpdateActuator();
        //            break;

        //        case 6: FiddleGo6.UpdateActuator();
        //            break;

        //        case 7: FiddleGo7.UpdateActuator();
        //            break;

        //        case 8: FiddleGo8.UpdateActuator();
        //            break;

        //        case 9: FiddleGo9.UpdateActuator();
        //            break;

        //        case 10: FiddleGo10.UpdateActuator();
        //            break;

        //        case 11: FiddleGo11.UpdateActuator();
        //            break;

        //        default : break;
                                    
        //    }
        //}

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SetLedIndicator updates from target/simulator
         *               (local piece of updated memory)
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
            switch (indicator)
            {
                case "CL10Heart": 
                    bCL_10_Heart = Convert.ToBoolean(val);
                    CL10Heart.UpdateSensorValue(Convert.ToInt16(bCL_10_Heart), false);
                    break;

                case "F11": 
                    bF11 = Convert.ToBoolean(val);
                    F11.UpdateSensorValue(Convert.ToInt16(bF11), false);
                    break;

                case "EOS10": 
                    bEOS10 = Convert.ToBoolean(val);
                    EOS10.UpdateSensorValue(Convert.ToInt16(bEOS10), false);
                    break;

                case "EOS11": 
                    bEOS11 = Convert.ToBoolean(val);
                    EOS11.UpdateSensorValue(Convert.ToInt16(bEOS11), false);
                    break;

                case "F13": 
                    bF13 = Convert.ToBoolean(val);
                    F13.UpdateSensorValue(Convert.ToInt16(bF13), false);
                    break;

                case "F12": 
                    bF12 = Convert.ToBoolean(val);
                    F12.UpdateSensorValue(Convert.ToInt16(bF12), false);
                    break;

                case "Block5B": 
                    bBlock5B = Convert.ToBoolean(val);
                    Block5B.UpdateSensorValue(Convert.ToInt16(bBlock5B), false);
                    break;

                case "Block8A": 
                    bBlock8A = Convert.ToBoolean(val);
                    Block8A.UpdateSensorValue(Convert.ToInt16(bBlock8A), false);
                    break;

                case "Block5BIn": 
                    bBlock5BIn = Convert.ToBoolean(val);
                    Block5BIn.UpdateSensorValue(Convert.ToInt16(bBlock5BIn), false);
                    break;

                case "Block6In": 
                    bBlock6In = Convert.ToBoolean(val);
                    Block6In.UpdateSensorValue(Convert.ToInt16(bBlock6In), false);
                    break;

                case "Block7In": 
                    bBlock7In = Convert.ToBoolean(val);
                    Block7In.UpdateSensorValue(Convert.ToInt16(bBlock7In), false);
                    break;

                case "Resistor": 
                    bResistor = Convert.ToBoolean(val);
                    Resistor.UpdateSensorValue(Convert.ToInt16(bResistor), false);
                    break;

                case "Block6": 
                    bBlock6 = Convert.ToBoolean(val);
                    Block6.UpdateSensorValue(Convert.ToInt16(bBlock6), false);
                    break;

                case "Block7": 
                    bBlock7 = Convert.ToBoolean(val);
                    Block7.UpdateSensorValue(Convert.ToInt16(bBlock7), false);
                    break;

                case "F10": 
                    bF10 = Convert.ToBoolean(val);
                    F10.UpdateSensorValue(Convert.ToInt16(bF10), false);
                    break;

                case "M10": 
                    bM10 = Convert.ToBoolean(val);
                    M10.UpdateSensorValue(Convert.ToInt16(bM10), false);
                    break;

                case "TrackPower":
                    bTrackPower = Convert.ToBoolean(val);
                    TrackPower.UpdateSensorValue(Convert.ToInt16(bTrackPower), false);
                    break;

                case "TrackPower15V": 
                    bTrackPower15V = Convert.ToBoolean(val);
                    TrackPower15V.UpdateSensorValue(Convert.ToInt16(bTrackPower15V), false);
                    break;

                case "Track_No": 
                    iTrackNo = val;
                    TrackNo.UpdateSensorValue(iTrackNo, false);
                    break;

                default:
                    break;
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
                        iTrainsOnFY[1] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[1] = 0;
                    }
                    Track1.UpdateSensorValue(iTrainsOnFY[1], true); // Forced only to update FORM, not forced reading from received data (IOHANDLER)
                    break;
                case "Track2": if (Val > 0)
                    {
                        iTrainsOnFY[2] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[2] = 0;
                    }
                    Track2.UpdateSensorValue(iTrainsOnFY[2], true);
                    break;
                case "Track3": if (Val > 0)
                    {
                        iTrainsOnFY[3] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[3] = 0;
                    }
                    Track3.UpdateSensorValue(iTrainsOnFY[3], true);
                    break;
                case "Track4": if (Val > 0)
                    {
                        iTrainsOnFY[4] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[4] = 0;
                    }
                    Track4.UpdateSensorValue(iTrainsOnFY[4], true);
                    break;
                case "Track5": if (Val > 0)
                    {
                        iTrainsOnFY[5] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[5] = 0;
                    }
                    Track5.UpdateSensorValue(iTrainsOnFY[5], true);
                    break;
                case "Track6": if (Val > 0)
                    {
                        iTrainsOnFY[6] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[6] = 0;
                    }
                    Track6.UpdateSensorValue(iTrainsOnFY[6], true);
                    break;
                case "Track7": if (Val > 0)
                    {
                        iTrainsOnFY[7] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[7] = 0;
                    }
                    Track7.UpdateSensorValue(iTrainsOnFY[7], true);
                    break;
                case "Track8": if (Val > 0)
                    {
                        iTrainsOnFY[8] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[8] = 0;
                    }
                    Track8.UpdateSensorValue(iTrainsOnFY[8], true);
                    break;
                case "Track9": if (Val > 0)
                    {
                        iTrainsOnFY[9] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[9] = 0;
                    }
                    Track9.UpdateSensorValue(iTrainsOnFY[9], true);
                    break;
                case "Track10": if (Val > 0)
                    {
                        iTrainsOnFY[10] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[10] = 0;
                    }
                    Track10.UpdateSensorValue(iTrainsOnFY[10], true);
                    break;
                case "Track11": if (Val > 0)
                    {
                        iTrainsOnFY[11] = 1;
                    }
                    else if (Val == 0)
                    {
                        iTrainsOnFY[11] = 0;
                    }
                    Track11.UpdateSensorValue(iTrainsOnFY[11], true);
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
            Track1.UpdateSensorValue(iTrainsOnFY[1], true); // Forced only to update FORM not forced reading from received data
            Track2.UpdateSensorValue(iTrainsOnFY[2], true);
            Track3.UpdateSensorValue(iTrainsOnFY[3], true);
            Track4.UpdateSensorValue(iTrainsOnFY[4], true);
            Track5.UpdateSensorValue(iTrainsOnFY[5], true);
            Track6.UpdateSensorValue(iTrainsOnFY[6], true);
            Track7.UpdateSensorValue(iTrainsOnFY[7], true);
            Track8.UpdateSensorValue(iTrainsOnFY[8], true);
            Track9.UpdateSensorValue(iTrainsOnFY[9], true);
            Track10.UpdateSensorValue(iTrainsOnFY[10], true);
            Track11.UpdateSensorValue(iTrainsOnFY[11], true);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: GetTrackNr
         * 
         *  Input(s)   :
         *
         *  Output(s)  : Converts the received track number into number 1 to 11
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
        public int GetTrackNr()
        {
            int _return = 0;
            switch (iTrackNo)
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
    }

    public class PublicBool
    {
        private bool boolvariable;
        public bool BoolVariable
        {
            get { return boolvariable; }
            set { boolvariable = value; }
        }
    }
}
