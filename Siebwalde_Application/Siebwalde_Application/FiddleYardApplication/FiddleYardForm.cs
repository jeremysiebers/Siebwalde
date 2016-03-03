using System;
using System.Drawing;
using System.Windows.Forms;

namespace Siebwalde_Application
{
    public delegate void SetLedIndicatorCallback(string Indicator, int Val, string Log);  // defines a delegate type TOP/BOT if caller runs on other thread
    public delegate void SetMessageCallback(string name, string Log);  // defines a delegate type TOP/BOT if caller runs on other thread

    public partial class FiddleYardForm : Form
    {
        string path = "null";
        public Log2LoggingFile FiddleYardFormLogging;
        
        public const int TOP = 1;
        public const int BOT = 0;

        public int GWinHalf;    // The Height of GWin devided by 2 plus the offset in Y is the center line of GWin
        public int GWinX;       // The X location of Gwin border line.
        public int GWinY;       // The Y location of Gwin border line.
        public int Track6LocY;
        public int Track6LocX;

        public int TrackLocDelta = 32;
        public int Track1Loc = 0;
        public int Track2Loc = 0;
        public int Track3Loc = 0;
        public int Track4Loc = 0;
        public int Track5Loc = 0;
        public int Track6Loc = 0;
        public int Track7Loc = 0;
        public int Track8Loc = 0;
        public int Track9Loc = 0;
        public int Track10Loc = 0;
        public int Track11Loc = 0;
        public int FYPlateLoc = 0;

        public bool[] TrackStatusLight = new bool[12]
        {
            false, false, false, false, false, false, false, false, false, false, false, false};

        public bool Initialized = false;
        public bool Btn_Bezet5BOn_TOP_Click_Toggle = false;
        public bool Btn_Bezet6On_TOP_Click_Toggle = false;
        public bool Btn_Bezet7On_TOP_Click_Toggle = false;
        
        private FiddleYardIOHandleVariables m_FYIOHandleVar;             // connect variable to connect to FYIOH class for traindetection
        public FiddleYardApplicationVariables m_FYAppVar;

        // Create actuators, when a button is pressed an event is generated to send a command to target via IOHandle and to the controlling program

        private static int FYTrackHeight = 16;
        private static int PBHeight = FYTrackHeight - 2;        
        public Image SeinGreen = Siebwalde_Application.Properties.Resources.SeinGreen.ToBitmap();
        public Image SeinRed = Siebwalde_Application.Properties.Resources.SeinRed.ToBitmap();
        public Image SeinGreen270 = Siebwalde_Application.Properties.Resources.SeinGreen.ToBitmap();
        public Image SeinRed270 = Siebwalde_Application.Properties.Resources.SeinRed.ToBitmap();
        public Image Train = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.TrainWithWagon.ToBitmap()), PBHeight));
        public Image Train90 = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.TrainWithWagon.ToBitmap()), PBHeight));
        public Image Train270 = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.TrainWithWagon.ToBitmap()), PBHeight));
        public Image Active = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.StopSign.ToBitmap()), PBHeight));
        public Image Inline = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.Arrow.ToBitmap()), PBHeight));
        public Image HalfTrain = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.HalfTrainWithWagon.ToBitmap()), PBHeight));
        public Image HalfTrain270 = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.HalfTrainWithWagon.ToBitmap()), PBHeight));
        public Image HalfWagon = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.HalfWagon.ToBitmap()), PBHeight));
        public Image HalfWagon90 = CropBitmap.Crop(ScaleBitmap.Scale(CropBitmap.Crop(Siebwalde_Application.Properties.Resources.HalfWagon.ToBitmap()), PBHeight));
        /*#--------------------------------------------------------------------------#*/
        /*  Description: FiddleYardForm
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
        public FiddleYardForm()
        {
            InitializeComponent();

            #region Indicator init
            // size of window resolution on Modeltrain PC is 1920; 1200
            // Size of window on Modeltrain PC is 960; 1085
            // Placed on 0; 85

            SeinGreen270.RotateFlip(RotateFlipType.Rotate270FlipNone);
            SeinRed270.RotateFlip(RotateFlipType.Rotate270FlipNone);
            Train90.RotateFlip(RotateFlipType.Rotate90FlipNone);           
            Train270.RotateFlip(RotateFlipType.Rotate270FlipNone);
            HalfTrain270.RotateFlip(RotateFlipType.Rotate270FlipNone);
            HalfWagon90.RotateFlip(RotateFlipType.Rotate90FlipNone);

            GWinX = 5;                      // The X location of Gwin border line.
            GWinY = 32;                     // The X location of Gwin border line.
            GWinHalf = 740 / 2 + GWinY;     // The Height of GWin devided by 2 plus the offset in Y is the center line of GWin
            Track6LocY = GWinHalf;// - 8;
            Track6LocX = GWinX + 170;
            

            GWin.Size = new Size (940,740);
            GWin.Location = new System.Drawing.Point(GWinX, GWinY); // was 5, 32

            LLed_F10.Size = new Size (30, 16);
            LLed_F10.Location = new System.Drawing.Point(GWinX + 10, GWinHalf - 8);
            LLed_F10_2_TOP.Size = new Size(30, 16);
            LLed_F10_2_TOP.Location = new System.Drawing.Point(GWinX + 900, GWinHalf - 8);

            LLed_Block6.Size = new Size(100, 16);
            LLed_Block6.Location = new System.Drawing.Point(GWinX + 50, GWinHalf - 8);

            LLed_Block6In.Size = new Size(26, 14);
            LLed_Block6In.Location = new System.Drawing.Point(GWinX + 50 + (100 / 2 - 26 / 2), GWinHalf - 7 + 25);
            //LLed_Block6In.Location = new System.Drawing.Point(GWinX + 50 + (100 / 2 - 26 / 2), GWinHalf -7);
            LLed_Block6In.Hide();
            PB_Block6In.Location = new System.Drawing.Point(GWinX + 50 + (100 / 2 - 26 / 2 - 4), GWinHalf - 7 + 20);
            PB_Block6In.Image = SeinRed;
            PB_Block6In.Enabled = false;

            LLed_Block5B.Size = new Size(16, 250);
            LLed_Block5B.Location = new System.Drawing.Point(GWinX + 50, GWinHalf - 18 - 250);

            LLed_Block5BIn.Size = new Size(14, 26);
            LLed_Block5BIn.Location = new System.Drawing.Point(GWinX + 50 + 16 + 10, GWinHalf - 18 - (250 / 2 + 26 / 2));            
            //LLed_Block5BIn.Location = new System.Drawing.Point(GWinX + 50 + 1, GWinHalf - 18 - (250/2 + 26/2));
            LLed_Block5BIn.Hide();
            PB_Block5BIn.Location = new System.Drawing.Point(GWinX + 50 + 16, GWinHalf - 20 - (250 / 2 + 26 / 2));            
            PB_Block5BIn.Image = SeinRed;
            PB_Block5BIn.Enabled = false;

            LLed_FYPLATE_TOP.Size = new Size(620, 368);
            LLed_FYPLATE_TOP.Location = new System.Drawing.Point(GWinX + 160, GWinHalf - 368 / 2);

            LLed_F13.Size = new Size(30, 14);
            LLed_F13.Location = new System.Drawing.Point(GWinX + 180, GWinHalf - 7);

            LLed_F11.Size = new Size(30, 14);
            LLed_F11.Location = new System.Drawing.Point(GWinX + 580, GWinHalf - 7);

            LLed_TrackPower.Size = new Size(100, 14);
            LLed_TrackPower.Location = new System.Drawing.Point(GWinX + 620, GWinHalf - 7);

            LLed_F12.Size = new Size(30, 14);
            LLed_F12.Location = new System.Drawing.Point(GWinX + 730, GWinHalf - 7);

            LLed_Block7.Size = new Size(121,16);
            LLed_Block7.Location = new System.Drawing.Point(GWinX + 769, GWinHalf - 8);

            LLed_Block7In.Size = new Size(26, 14);
            LLed_Block7In.Location = new System.Drawing.Point(GWinX + 790 + (100 / 2 - 26 / 2), GWinHalf - 7 + 25);
            //LLed_Block7In.Location = new System.Drawing.Point(GWinX + 790 + (100 / 2 - 26 / 2), GWinHalf - 7);
            LLed_Block7In.Hide();
            PB_Block7In.Location = new System.Drawing.Point(GWinX + 790 + (100 / 2 - 26 / 2 - 4), GWinHalf - 7 + 20);
            PB_Block7In.Image = SeinRed;
            PB_Block7In.Enabled = false;

            LLed_Block8A.Size = new Size(16, 250);
            LLed_Block8A.Location = new System.Drawing.Point(GWinX + 790 + (100 - 16), GWinHalf - 250 - 18);
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track1.Size = new Size(600, FYTrackHeight);            

            PB_Tr1_Train.Size = new Size(Train.Width, Train.Height);            
            PB_Tr1_Train.Image = null;
            PB_Tr1_Train.Show();                                                                
            
            PB_Tr1_Inline.Size = new Size(Inline.Width, Inline.Height);            
            PB_Tr1_Inline.Image = Inline;
            PB_Tr1_Inline.Hide();                                                               

            PB_Tr1_Active.Size = new Size(Active.Width, Active.Height);            
            PB_Tr1_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track2.Size = new Size(600, FYTrackHeight);
            
            PB_Tr2_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr2_Train.Image = null;
            PB_Tr2_Train.Show();
                       
            PB_Tr2_Inline.Size = new Size(Inline.Width, Inline.Height);            
            PB_Tr2_Inline.Image = Inline;
            PB_Tr2_Inline.Hide();           
                        
            PB_Tr2_Active.Size = new Size(Active.Width, Active.Height);            
            PB_Tr2_Active.Image = Active;                 
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track3.Size = new Size(600, FYTrackHeight);

            PB_Tr3_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr3_Train.Image = null;
            PB_Tr3_Train.Show();

            PB_Tr3_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr3_Inline.Image = Inline;
            PB_Tr3_Inline.Hide();

            PB_Tr3_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr3_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track4.Size = new Size(600, FYTrackHeight);

            PB_Tr4_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr4_Train.Image = null;
            PB_Tr4_Train.Show();

            PB_Tr4_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr4_Inline.Image = Inline;
            PB_Tr4_Inline.Hide();

            PB_Tr4_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr4_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track5.Size = new Size(600, FYTrackHeight);

            PB_Tr5_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr5_Train.Image = null;
            PB_Tr5_Train.Show();

            PB_Tr5_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr5_Inline.Image = Inline;
            PB_Tr5_Inline.Hide();

            PB_Tr5_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr5_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track6.Size = new Size(600, FYTrackHeight);

            PB_Tr6_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr6_Train.Image = null;
            PB_Tr6_Train.Show();

            PB_Tr6_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr6_Inline.Image = Inline;
            PB_Tr6_Inline.Hide();

            PB_Tr6_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr6_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track7.Size = new Size(600, FYTrackHeight);

            PB_Tr7_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr7_Train.Image = null;
            PB_Tr7_Train.Show();

            PB_Tr7_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr7_Inline.Image = Inline;
            PB_Tr7_Inline.Hide();

            PB_Tr7_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr7_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track8.Size = new Size(600, FYTrackHeight);

            PB_Tr8_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr8_Train.Image = null;
            PB_Tr8_Train.Show();

            PB_Tr8_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr8_Inline.Image = Inline;
            PB_Tr8_Inline.Hide();

            PB_Tr8_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr8_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track9.Size = new Size(600, FYTrackHeight);

            PB_Tr9_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr9_Train.Image = null;
            PB_Tr9_Train.Show();

            PB_Tr9_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr9_Inline.Image = Inline;
            PB_Tr9_Inline.Hide();

            PB_Tr9_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr9_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track10.Size = new Size(600, FYTrackHeight);

            PB_Tr10_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr10_Train.Image = null;
            PB_Tr10_Train.Show();

            PB_Tr10_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr10_Inline.Image = Inline;
            PB_Tr10_Inline.Hide();

            PB_Tr10_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr10_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_Track11.Size = new Size(600, FYTrackHeight);

            PB_Tr11_Train.Size = new Size(Train.Width, Train.Height);
            PB_Tr11_Train.Image = null;
            PB_Tr11_Train.Show();

            PB_Tr11_Inline.Size = new Size(Inline.Width, Inline.Height);
            PB_Tr11_Inline.Image = Inline;
            PB_Tr11_Inline.Hide();

            PB_Tr11_Active.Size = new Size(Active.Width, Active.Height);
            PB_Tr11_Active.Image = Active;
            //----------------------------------------------------------------------------------------------------------------------------//
            PB_Bk5_Train.Size = new Size(Train90.Width, Train90.Height);
            PB_Bk5_Train.Location = new System.Drawing.Point(LLed_Block5B.Location.X + 1, LLed_Block5B.Location.Y + LLed_Block5B.Location.Y / 2 + PB_Bk5_Train.Height / 2);
            PB_Bk5_Train.Image = Train90;
            PB_Bk5_Train.Show();
            //----------------------------------------------------------------------------------------------------------------------------//
            PB_Bk6_Train.Size = new Size(Train.Width, Train.Height);
            PB_Bk6_Train.Location = new System.Drawing.Point(LLed_Block6.Location.X + PB_Bk6_Train.Width / 2, LLed_Block6.Location.Y + 2);
            PB_Bk6_Train.Image = HalfTrain;
            PB_Bk6_Train.Show();
            //----------------------------------------------------------------------------------------------------------------------------//
            PB_Bk7_Train.Size = new Size(Train.Width, Train.Height);
            PB_Bk7_Train.Location = new System.Drawing.Point(LLed_Block7.Location.X + PB_Bk7_Train.Width / 2, LLed_Block7.Location.Y + 2);
            PB_Bk7_Train.Image = Train;
            PB_Bk7_Train.Show();
            //----------------------------------------------------------------------------------------------------------------------------//
            PB_Bk8A_Train.Size = new Size(Train270.Width, Train270.Height);
            PB_Bk8A_Train.Location = new System.Drawing.Point(LLed_Block8A.Location.X + 2, LLed_Block8A.Location.Y + LLed_Block8A.Location.Y / 2 + PB_Bk8A_Train.Height / 2);
            PB_Bk8A_Train.Image = HalfTrain270;
            PB_Bk8A_Train.Show();
            //----------------------------------------------------------------------------------------------------------------------------//
            LLed_EOS11.Size = new Size(50, 16);
            LLed_EOS11.Location = new System.Drawing.Point(GWinX + 180, GWinHalf - 360);

            LLed_EOS10.Size = new Size(50, 16);
            LLed_EOS10.Location = new System.Drawing.Point(GWinX + 180, GWinHalf + 360 - 16);

            Btn_Bezet5BOn_TOP.Size = new Size(32, 24);
            Btn_Bezet5BOn_TOP.Location = new System.Drawing.Point(GWinX + 50 + 16 + 30, GWinHalf - 18 - (250 / 2) - (23 / 2));
            //Btn_Bezet5BOn_TOP.Location = new System.Drawing.Point(GWinX + 50 + 16 + 10, GWinHalf - 18 - (250/2) - (23/2) );

            Btn_Bezet6On_TOP.Size = new Size(32, 24);
            Btn_Bezet6On_TOP.Location = new System.Drawing.Point(GWinX + 100 - (32 / 2), GWinHalf + 8 + 40);
            //Btn_Bezet6On_TOP.Location = new System.Drawing.Point(GWinX + 100 - (32/2), GWinHalf + 8 + 10);

            Btn_Bezet7On_TOP.Size = new Size(32, 24);
            Btn_Bezet7On_TOP.Location = new System.Drawing.Point(GWinX + 840 - (32 / 2), GWinHalf + 8 + 40);
            //Btn_Bezet7On_TOP.Location = new System.Drawing.Point(GWinX + 840 - (32/2), GWinHalf + 8 + 10);

            LabelBlock6.Size = new Size(44, 13);
            LabelBlock6.Location = new System.Drawing.Point(GWinX + 100 - (44 / 2), GWinHalf - 8 - 10 - 13);

            LabelBlock7.Size = new Size(44, 13);
            LabelBlock7.Location = new System.Drawing.Point(GWinX + 840 - (44 / 2), GWinHalf - 8 - 10 - 13);

            LabelBlock5B.Size = new Size(44, 13);
            LabelBlock5B.Location = new System.Drawing.Point(GWinX + 60 + 8, GWinHalf - 18 - 250);

            LabelBlock8A.Size = new Size(44, 13);
            LabelBlock8A.Location = new System.Drawing.Point(GWinX + 790 + (100 - 16) - 10 - 50, GWinHalf - 18 - 250);

            LLed_15VTrackPower.Size = new Size(88, 16);
            LLed_15VTrackPower.Location = new System.Drawing.Point(GWinX + 835, GWinHalf - 360);

            LLed_Heart.Size = new Size(60, 16);
            LLed_Heart.Location = new System.Drawing.Point(GWinX + 15, GWinHalf + 360 - 16);

            LLed_M10.Size = new Size(50, 16);
            LLed_M10.Location = new System.Drawing.Point(GWinX + 270, GWinHalf + 360 - 16);

            LLed_Resistor.Size = new Size(100, 16);
            LLed_Resistor.Location = new System.Drawing.Point(GWinX + 620, GWinHalf + 360 - 16);

            Btn_Reset_TOP.Size = new Size(80, 32);
            Btn_Reset_TOP.Location = new System.Drawing.Point(GWinX + 10, GWinHalf - 360);

            //LLed_Alive.Size = new Size(16, 16);
            //LLed_Alive.Location = new System.Drawing.Point(GWinX + 880, GWinHalf + 360 - 16);

            SimulationMode.Visible = false;
            alwaysOnTopToolStripMenuItem.Checked = true;
            this.TopMost = true;

            checkBoxTrack1.Checked = false;
            checkBoxTrack2.Checked = false;
            checkBoxTrack3.Checked = false;
            checkBoxTrack4.Checked = false;
            checkBoxTrack5.Checked = false;
            checkBoxTrack6.Checked = false;
            checkBoxTrack7.Checked = false;
            checkBoxTrack8.Checked = false;
            checkBoxTrack9.Checked = false;
            checkBoxTrack10.Checked = false;
            checkBoxTrack11.Checked = false;
            checkBoxToggle.Checked = false;            

            #endregion Indicator init            

            Btn_Stop_Fiddle_TOP.Enabled = false;    
                    
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Connect
         *               hookup sensors/messages etc
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
        public void Connect(FiddleYardIOHandleVariables FYIOHandleVar, FiddleYardApplicationVariables FYAppVar)
        {
            m_FYIOHandleVar = FYIOHandleVar;    // connect to FYIOHandle interface, save interface in variable
            m_FYAppVar = FYAppVar;                  // connect to FYApplication variables, save interface in variable

            if (this.Name == "FiddleYardTOP")
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardFormTOP.txt"; //  different logging file per target, this is default
                FiddleYardFormLogging = new Log2LoggingFile(path);
            }
            else if (this.Name == "FiddleYardBOT")
            {
                path = @"c:\localdata\Siebwalde\" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_FiddleYardFormBOT.txt"; //  different logging file per target, this is default
                FiddleYardFormLogging = new Log2LoggingFile(path);
            }

            #region Attach sensors
            //Sensors -----------------------------> to be pushed from Application variables
            Sensor Led_CL_10_Heart = new Sensor("LLed_Heart", " CL 10 Heart ",0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.CL10Heart.Attach(Led_CL_10_Heart);
            Sensor Led_F11 = new Sensor("LLed_F11", " F11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.F11.Attach(Led_F11);
            Sensor Led_EOS10 = new Sensor("LLed_EOS10", " EOS 10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.EOS10.Attach(Led_EOS10);
            Sensor Led_EOS11 = new Sensor("LLed_EOS11", " EOS 11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.EOS11.Attach(Led_EOS11);
            Sensor Led_F13 = new Sensor("LLed_F13", " F13 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.F13.Attach(Led_F13);
            Sensor Led_F12 = new Sensor("LLed_F12", " F12 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.F12.Attach(Led_F12);
            Sensor Led_Block5B = new Sensor("LLed_Block5B", " Occupied from 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Block5B.Attach(Led_Block5B);
            Sensor Led_Block8A = new Sensor("LLed_Block8A", " Occupied from 8A ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Block8A.Attach(Led_Block8A);
            Sensor Led_TrackPowerTop = new Sensor("LLed_TrackPower", " Enable Track ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.TrackPower.Attach(Led_TrackPowerTop);
            Sensor Led_Block5BIn = new Sensor("LLed_Block5BIn", " Occupied to 5B ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Block5BIn.Attach(Led_Block5BIn);
            Sensor Led_Block6In = new Sensor("LLed_Block6In", " Occupied to 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Block6In.Attach(Led_Block6In);
            Sensor Led_Block7In = new Sensor("LLed_Block7In", " Occupied to 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Block7In.Attach(Led_Block7In);
            Sensor Led_ResistorTop = new Sensor("LLed_Resistor", " Occupied Resistor ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Resistor.Attach(Led_ResistorTop);
            Sensor Led_Track1Top = new Sensor("LLed_Track1", " Trains On Fiddle Yard Track1 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track1.Attach(Led_Track1Top);
            Sensor Led_Track2Top = new Sensor("LLed_Track2", " Trains On Fiddle Yard Track2 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track2.Attach(Led_Track2Top);
            Sensor Led_Track3Top = new Sensor("LLed_Track3", " Trains On Fiddle Yard Track3 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track3.Attach(Led_Track3Top);
            Sensor Led_Track4Top = new Sensor("LLed_Track4", " Trains On Fiddle Yard Track4 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track4.Attach(Led_Track4Top);
            Sensor Led_Track5Top = new Sensor("LLed_Track5", " Trains On Fiddle Yard Track5 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track5.Attach(Led_Track5Top);
            Sensor Led_Track6Top = new Sensor("LLed_Track6", " Trains On Fiddle Yard Track6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track6.Attach(Led_Track6Top);
            Sensor Led_Track7Top = new Sensor("LLed_Track7", " Trains On Fiddle Yard Track7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track7.Attach(Led_Track7Top);
            Sensor Led_Track8Top = new Sensor("LLed_Track8", " Trains On Fiddle Yard Track8 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track8.Attach(Led_Track8Top);
            Sensor Led_Track9Top = new Sensor("LLed_Track9", " Trains On Fiddle Yard Track9 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track9.Attach(Led_Track9Top);
            Sensor Led_Track10Top = new Sensor("LLed_Track10", " Trains On Fiddle Yard Track10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track10.Attach(Led_Track10Top);
            Sensor Led_Track11Top = new Sensor("LLed_Track11", " Trains On Fiddle Yard Track11 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Track11.Attach(Led_Track11Top);
            Sensor Led_Block6 = new Sensor("LLed_Block6", " Occupied from 6 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Block6.Attach(Led_Block6);
            Sensor Led_Block7 = new Sensor("LLed_Block7", " Occupied from 7 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.Block7.Attach(Led_Block7);
            Sensor Led_F10 = new Sensor("LLed_F10", " F10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.F10.Attach(Led_F10);
            Sensor Led_M10 = new Sensor("LLed_M10", " M10 ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.M10.Attach(Led_M10);
            Sensor Led_TrackNoTop = new Sensor("Track_No", " Track Nr ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.TrackNo.Attach(Led_TrackNoTop);
            Sensor Led_TrackPower15VTOP = new Sensor("LLed_TrackPower15V", " 15V Track Power ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.TrackPower15V.Attach(Led_TrackPower15VTOP);

            Sensor Sns_ForceNextTrack = new Sensor("ForceNextTrack", " ForceNextTrack ", 0, (name, val, log) => SetLedIndicator(name, val, log)); // initialize and subscribe sensors
            m_FYAppVar.TrainDriveOutPointer.Attach(Sns_ForceNextTrack);  
             
            //Messages
            Message Msg_TrainDetectionTop = new Message("TrainDetectionFinished", " Train Detection Finished ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYAppVar.TrainDetection.Attach(Msg_TrainDetectionTop);
            Message Msg_TrainDetectionStarted = new Message("TrainDetectionStarted", " Train Detection Started ", (name, log) => SetMessage(name, log)); // initialize and subscribe readback action, Message
            m_FYAppVar.TrainDetectionStarted.Attach(Msg_TrainDetectionStarted);
            Message Msg_FiddleYardStopped = new Message("FiddleYardStopped", " FiddleYard Auto mode Stopped ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardStopped.Attach(Msg_FiddleYardStopped);
            Message Msg_FiddleYardStart = new Message("FiddleYardStart", " FiddleYard Auto mode Start ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardStart.Attach(Msg_FiddleYardStart);
            Message Msg_FiddleYardNotHomed = new Message("FiddleYardNotHomed", " FiddleYard not homed, start homing ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardNotHomed.Attach(Msg_FiddleYardNotHomed);
            Message Msg_FiddleYardHomingFinished = new Message("FiddleYardHomingFinished", " FiddleYard homing finished ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardHomingFinished.Attach(Msg_FiddleYardHomingFinished);
            Message Msg_FiddleYardTrainObstruction = new Message("FiddleYardTrainObstruction", " FiddleYard train obstruction... ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardTrainObstruction.Attach(Msg_FiddleYardTrainObstruction);
            Message Msg_FiddleYardTrackAligned = new Message("FiddleYardTrackAligned", " FiddleYard track aligned ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardTrackAligned.Attach(Msg_FiddleYardTrackAligned);
            Message Msg_TrainHasLeftFiddleYardSuccessfully = new Message("TrainHasLeftFiddleYardSuccessfully", " Train has left FiddleYard successfully ", (name, log) => SetMessage(name, log));
            m_FYAppVar.TrainHasLeftFiddleYardSuccessfully.Attach(Msg_TrainHasLeftFiddleYardSuccessfully);
            Message Msg_EMOPressed15VTrackPowerDown = new Message("EMOPressed15VTrackPowerDown", " EMO pressed, 15V Track Power down! ", (name, log) => SetMessage(name, log));
            m_FYAppVar.EMOPressed15VTrackPowerDown.Attach(Msg_EMOPressed15VTrackPowerDown);
            Message Msg_EMOPressed15VTrackPowerUp = new Message("EMOPressed15VTrackPowerUp", " EMO released, 15V Track Power up! ", (name, log) => SetMessage(name, log));
            m_FYAppVar.EMOPressed15VTrackPowerUp.Attach(Msg_EMOPressed15VTrackPowerUp);
            Message Msg_FiddleYardAutoModeStart = new Message("FiddleYardAutoModeStart", " FiddleYard Auto mode Start ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardAutoModeStart.Attach(Msg_FiddleYardAutoModeStart);
            Message Msg_FiddleYardInit = new Message("FiddleYardInit", " FiddleYard init... ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardInit.Attach(Msg_FiddleYardInit);
            Message Msg_FiddleYardAutoModeIsGoingToStop = new Message("FiddleYardAutoModeIsGoingToStop", " FiddleYard Auto mode is going to stop... ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardAutoModeIsGoingToStop.Attach(Msg_FiddleYardAutoModeIsGoingToStop);
            Message Msg_FiddleYardReset = new Message("FiddleYardReset", " FiddleYard Reset ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardReset.Attach(Msg_FiddleYardReset);
            Message Msg_FiddleYardInitFinished = new Message("FiddleYardInitFinished", " FiddleYard init Finished ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardInitFinished.Attach(Msg_FiddleYardInitFinished);
            Message Msg_FiddleYardApplicationRunning = new Message("FiddleYardApplicationRunning", " FiddleYard Application running... ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardApplicationRunning.Attach(Msg_FiddleYardApplicationRunning);
            Message Msg_FiddleYardAutoModeIsStopped = new Message("FiddleYardAutoModeIsStopped", " FiddleYard Auto mode is Stopped ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FiddleYardAutoModeIsStopped.Attach(Msg_FiddleYardAutoModeIsStopped);
            Message Msg_CollectingTrainsEnabled = new Message("CollectingTrainsEnabled", " Collecting Trains enabled ", (name, log) => SetMessage(name, log));
            m_FYAppVar.CollectingTrainsEnabled.Attach(Msg_CollectingTrainsEnabled);
            Message Msg_CollectingTrainsDisabled = new Message("CollectingTrainsDisabled", " Collecting Trains disabled ", (name, log) => SetMessage(name, log));
            m_FYAppVar.CollectingTrainsDisabled.Attach(Msg_CollectingTrainsDisabled);
            //Message Msg_FiddleYardMoveAndF12Assert = new Message("FiddleYardMoveAndF12Assert", " FiddleYard Moving And F12 Assert!!! FiddleYard Stopped!!! ", (name, log) => SetMessage(name, log));
            //m_FYIOHandleVar.FiddleYardMoveAndF12Assert.Attach(Msg_FiddleYardMoveAndF12Assert);
            //Message Msg_FiddleYardMoveAndF13Assert = new Message("FiddleYardMoveAndF13Assert", " FiddleYard Moving And F13 Assert!!! FiddleYard Stopped!!! ", (name, log) => SetMessage(name, log));
            //m_FYIOHandleVar.FiddleYardMoveAndF13Assert.Attach(Msg_FiddleYardMoveAndF13Assert);
            Message Msg_FYNotHomed = new Message("FYNotHomed", " Fiddle Yard Not Homed! Press Home FY Button to start Homing... ", (name, log) => SetMessage(name, log));
            m_FYAppVar.FYNotHomed.Attach(Msg_FYNotHomed);
            #endregion Attach sensors

            // Init Track colors
            LLed_Track1.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track2.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track3.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track4.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track5.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track5.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track6.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track7.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track8.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track9.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track10.BackColor = m_FYAppVar.TrackNotInitializedColor;
            LLed_Track11.BackColor = m_FYAppVar.TrackNotInitializedColor;

            LLed_F10.BackColor = m_FYAppVar.TrackNotInitializedColor; ;
            LLed_F10.ForeColor = Color.Black;
            LLed_F11.BackColor = m_FYAppVar.TrackNotInitializedColor; ;
            LLed_F11.ForeColor = Color.Black;
            LLed_F12.BackColor = m_FYAppVar.TrackNotInitializedColor; ;
            LLed_F12.ForeColor = Color.Black;
            LLed_F13.BackColor = m_FYAppVar.TrackNotInitializedColor; ;
            LLed_F13.ForeColor = Color.Black;

            PB_Tr1_Active.BackColor = LLed_Track1.BackColor;
            PB_Tr1_Inline.BackColor = LLed_Track1.BackColor;
            PB_Tr1_Train.BackColor = LLed_Track1.BackColor;
            PB_Tr2_Active.BackColor = LLed_Track2.BackColor;
            PB_Tr2_Inline.BackColor = LLed_Track2.BackColor;
            PB_Tr2_Train.BackColor = LLed_Track2.BackColor;
            PB_Tr3_Active.BackColor = LLed_Track3.BackColor;
            PB_Tr3_Inline.BackColor = LLed_Track3.BackColor;
            PB_Tr3_Train.BackColor = LLed_Track3.BackColor;
            PB_Tr4_Active.BackColor = LLed_Track4.BackColor;
            PB_Tr4_Inline.BackColor = LLed_Track4.BackColor;
            PB_Tr4_Train.BackColor = LLed_Track4.BackColor;
            PB_Tr5_Active.BackColor = LLed_Track5.BackColor;
            PB_Tr5_Inline.BackColor = LLed_Track5.BackColor;
            PB_Tr5_Train.BackColor = LLed_Track5.BackColor;
            PB_Tr6_Active.BackColor = LLed_Track6.BackColor;
            PB_Tr6_Inline.BackColor = LLed_Track6.BackColor;
            PB_Tr6_Train.BackColor = LLed_Track6.BackColor;
            PB_Tr7_Active.BackColor = LLed_Track7.BackColor;
            PB_Tr7_Inline.BackColor = LLed_Track7.BackColor;
            PB_Tr7_Train.BackColor = LLed_Track7.BackColor;
            PB_Tr8_Active.BackColor = LLed_Track8.BackColor;
            PB_Tr8_Inline.BackColor = LLed_Track8.BackColor;
            PB_Tr8_Train.BackColor = LLed_Track8.BackColor;
            PB_Tr9_Active.BackColor = LLed_Track9.BackColor;
            PB_Tr9_Inline.BackColor = LLed_Track9.BackColor;
            PB_Tr9_Train.BackColor = LLed_Track9.BackColor;
            PB_Tr10_Active.BackColor = LLed_Track10.BackColor;
            PB_Tr10_Inline.BackColor = LLed_Track10.BackColor;
            PB_Tr10_Train.BackColor = LLed_Track10.BackColor;
            PB_Tr11_Active.BackColor = LLed_Track11.BackColor;
            PB_Tr11_Inline.BackColor = LLed_Track11.BackColor;
            PB_Tr11_Train.BackColor = LLed_Track11.BackColor;

            PB_Bk5_Train.Hide();
            PB_Bk6_Train.Hide();
            PB_Bk8A_Train.Hide();
            PB_Bk7_Train.Hide();

            PB_Bk7_Train.Image = null;
            PB_Bk8A_Train.Image = null;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SimMode
         *               
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
        public void SimMode(bool val)
        {
            SimulationMode.Visible = val;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SimMode
         *               
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
         * 
         *  Post.Cond. :
         *
         *  Notes      :
         */
        /*#--------------------------------------------------------------------------#*/
        public void ClearReceivedCmd()
        {
            ReceivedCmd.Clear();
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SimMode
         *               
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
        public void FYFORMShow(bool View)
        {
            this.Opacity = 100;
            this.ShowInTaskbar = true;
            if (View)
            {
                this.Show();
            }
            else { this.Hide(); }
            this.TopLevel = true;
            
            if (this.Name == "FiddleYardTOP")
            {
                //this.Location = new System.Drawing.Point(0, 75);
                //this.Size = new Size (960, 1085);
                this.Text = "Fiddle Yard Top Layer";
                LabelBlock5B.Text = "Block 5B";
                LabelBlock6.Text = "Block 6";
                LabelBlock7.Text = "Block 7";
                LabelBlock8A.Text = "Block 8A";
                LLed_F10.Text = "F 10";
                LLed_F10_2_TOP.Text = "F 10";
                LLed_F11.Text = "F 11";
                LLed_F12.Text = "F 12";
                LLed_F13.Text = "F 13";
                LLed_EOS10.Text = "EOS 10";
                LLed_EOS11.Text = "EOS 11";
                LLed_M10.Text = "M 10";
            }
            else if (this.Name == "FiddleYardBOT")
            {
                //this.Location = new System.Drawing.Point(960, 75);
                //this.Size = new Size(960, 1085);
                this.Text = "Fiddle Yard Bottom Layer";
                LabelBlock5B.Text = "Block 16B";
                LabelBlock6.Text = "Block 17";
                LabelBlock7.Text = "Block 18";
                LabelBlock8A.Text = "Block 19A";
                LLed_F10.Text = "F 20";
                LLed_F10_2_TOP.Text = "F 20";
                LLed_F11.Text = "F 21";
                LLed_F12.Text = "F 22";
                LLed_F13.Text = "F 23";
                LLed_EOS10.Text = "EOS 20";
                LLed_EOS11.Text = "EOS 21";
                LLed_M10.Text = "M 20";
            }  
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: FYFORM Buttons
         *               
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
        private void Btn_Bridge_Open_TOP_Click_1(object sender, EventArgs e) //Couple
        {
            m_FYAppVar.CmdCouple.UpdateCommand();
        }

        private void Btn_Bridge_Close_TOP_Click_1(object sender, EventArgs e) //Uncouple
        {
            m_FYAppVar.CmdUncouple.UpdateCommand();            
        }

        private void Btn_Track_Plus_TOP_Click_1(object sender, EventArgs e)
        {
            // Check if the the fiddle yard is not at the limit, last track 11
            if (Track_No.Text != "11")
            {
                m_FYAppVar.CmdFiddleOneLeft.UpdateCommand();
            }
        }

        private void Btn_Track_Min_TOP_Click_1(object sender, EventArgs e)
        {
            // Check if the the fiddle yard is not at the limit, first track 11
            if (Track_No.Text != "1")
            {
                m_FYAppVar.CmdFiddleOneRight.UpdateCommand();
            }            
        }

        private void Nuo_Track_No_TOP_ValueChanged_1(object sender, EventArgs e)
        {
            // Limit the Go track number from 1 to 11
            if (Nuo_Track_No_TOP.Value < 1)
            {
                Nuo_Track_No_TOP.Value = 1;
            }
            if (Nuo_Track_No_TOP.Value > 11)
            {
                Nuo_Track_No_TOP.Value = 11;
            }
        }

        private void Btn_Go_To_Track_TOP_Click_1(object sender, EventArgs e)
        {
            if (Nuo_Track_No_TOP.Value == 1)
            {
                m_FYAppVar.CmdFiddleGo1.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 2)
            {
                m_FYAppVar.CmdFiddleGo2.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 3)
            {
                m_FYAppVar.CmdFiddleGo3.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 4)
            {
                m_FYAppVar.CmdFiddleGo4.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 5)
            {
                m_FYAppVar.CmdFiddleGo5.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 6)
            {
                m_FYAppVar.CmdFiddleGo6.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 7)
            {
                m_FYAppVar.CmdFiddleGo7.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 8)
            {
                m_FYAppVar.CmdFiddleGo8.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 9)
            {
                m_FYAppVar.CmdFiddleGo9.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 10)
            {
                m_FYAppVar.CmdFiddleGo10.UpdateCommand(); 
            }
            else if (Nuo_Track_No_TOP.Value == 11)
            {
                m_FYAppVar.CmdFiddleGo11.UpdateCommand(); 
            }
        }

        private void Btn_Train_Detect_TOP_Click_1(object sender, EventArgs e)
        {
            m_FYAppVar.CmdTrainDetect.UpdateCommand();
        }

        private void Btn_Start_Fiddle_TOP_Click_1(object sender, EventArgs e)
        {
            m_FYAppVar.CmdFYStart.UpdateCommand(); 
        }

        private void Btn_Stop_Fiddle_TOP_Click_1(object sender, EventArgs e)
        {
            m_FYAppVar.CmdFYStop.UpdateCommand();                
        }

        private void Btn_Reset_TOP_Click_1(object sender, EventArgs e)
        {
            m_FYAppVar.CmdReset.UpdateCommand();            
        }

        private void Btn_Bezet5BOn_TOP_Click_1(object sender, EventArgs e)
        {
            if (Btn_Bezet5BOn_TOP_Click_Toggle == true)
            {
                Btn_Bezet5BOn_TOP_Click_Toggle = false;
                m_FYAppVar.CmdOcc5BOnTrue.UpdateCommand();
                Btn_Bezet5BOn_TOP.Text = "Off";
            }
            else if (Btn_Bezet5BOn_TOP_Click_Toggle == false)
            {
                Btn_Bezet5BOn_TOP_Click_Toggle = true;
                m_FYAppVar.CmdOcc5BOnFalse.UpdateCommand();
                Btn_Bezet5BOn_TOP.Text = "On";
            }
        }

        private void Btn_Bezet6On_TOP_Click_1(object sender, EventArgs e)
        {
            if (Btn_Bezet6On_TOP_Click_Toggle == true)
            {
                Btn_Bezet6On_TOP_Click_Toggle = false;
                m_FYAppVar.CmdOcc6OnTrue.UpdateCommand();
                Btn_Bezet6On_TOP.Text = "Off";
            }
            else if (Btn_Bezet6On_TOP_Click_Toggle == false)
            {
                Btn_Bezet6On_TOP_Click_Toggle = true;
                m_FYAppVar.CmdOcc6OnFalse.UpdateCommand();
                Btn_Bezet6On_TOP.Text = "On";
            }
        }

        private void Btn_Bezet7On_TOP_Click_1(object sender, EventArgs e)
        {
            if (Btn_Bezet7On_TOP_Click_Toggle == true)
            {
                Btn_Bezet7On_TOP_Click_Toggle = false;
                m_FYAppVar.CmdOcc7OnTrue.UpdateCommand();
                Btn_Bezet7On_TOP.Text = "Off";
            }
            else if (Btn_Bezet7On_TOP_Click_Toggle == false)
            {
                Btn_Bezet7On_TOP_Click_Toggle = true;
                m_FYAppVar.CmdOcc7OnFalse.UpdateCommand();
                Btn_Bezet7On_TOP.Text = "On";
            }
        }

        private void Btn_Recovered_TOP_Click_1(object sender, EventArgs e)
        {
            m_FYAppVar.CmdHomeFY.UpdateCommand();
        }

        private void Btn_Collect_TOP_Click_1(object sender, EventArgs e)
        {
            m_FYAppVar.CmdCollect.UpdateCommand();
        }


        /*#--------------------------------------------------------------------------#*/
        /*  Description: SetMessage
         *               
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
        private void SetMessage(string name, string log)
        {
            if (ReceivedCmd.InvokeRequired)
            {
                SetMessageCallback d = new SetMessageCallback(SetMessage);
                ReceivedCmd.Invoke(d, new object[] { name, log });  // invoking itself
            }
            else
            {
                if (log != "")
                {
                    FiddleYardFormLogging.StoreText(log);
                    string fmt = "000";
                    int m_Millisecond = DateTime.Now.Millisecond;
                    string m_text = DateTime.Now + ":" + m_Millisecond.ToString(fmt) + " " + log + " " + Environment.NewLine;                    
                    ReceivedCmd.AppendText(m_text);                    
                }

                switch (name)
                {
                    case "TrainDetectionFinished":                          // Traindetection (because message train detection comes first and "trains on fiddle yard trackX true/false                        
                        if (Initialized == false)
                        {
                            LLed_Track1.BackColor = Color.Transparent;      // comes later, all tracks get the correct color.
                            LLed_Track1.Text = "                                   1";
                            LLed_Track2.BackColor = Color.Transparent;
                            LLed_Track2.Text = "                                   2";
                            LLed_Track3.BackColor = Color.Transparent;
                            LLed_Track3.Text = "                                   3";
                            LLed_Track4.BackColor = Color.Transparent;
                            LLed_Track4.Text = "                                   4";
                            LLed_Track5.BackColor = Color.Transparent;
                            LLed_Track5.Text = "                                   5";
                            LLed_Track6.BackColor = Color.Transparent;
                            LLed_Track6.Text = "                                   6";
                            LLed_Track7.BackColor = Color.Transparent;
                            LLed_Track7.Text = "                                   7";
                            LLed_Track8.BackColor = Color.Transparent;
                            LLed_Track8.Text = "                                   8";
                            LLed_Track9.BackColor = Color.Transparent;
                            LLed_Track9.Text = "                                   9";
                            LLed_Track10.BackColor = Color.Transparent;
                            LLed_Track10.Text = "                                  10";
                            LLed_Track11.BackColor = Color.Transparent;
                            LLed_Track11.Text = "                                  11";
                        }
                        Initialized = true;
                        break; 
                        
                    case "CollectingTrainsEnabled":
                        Btn_Collect_TOP.Text = "Collect On";
                        break;

                    case "CollectingTrainsDisabled":
                        Btn_Collect_TOP.Text = "Collect Off";
                        break;

                    case "FiddleYardAutoModeIsStopped":
                        Btn_Start_Fiddle_TOP.Enabled = true;
                        Btn_Stop_Fiddle_TOP.Enabled = false;
                        manualModeToolStripMenuItem.Enabled = true;
                        break;

                    case "FiddleYardAutoModeStart":
                        Btn_Start_Fiddle_TOP.Enabled = false;
                        Btn_Stop_Fiddle_TOP.Enabled = true;
                        manualModeToolStripMenuItem.Enabled = false;
                        break;

                    case "FiddleYardReset":
                        Initialized = false;            
                        // Next also force all track color to cyan including text becasue if a track is already false no update is executed on each track color.
                        LLed_Track1.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track1.Text = "                     Not Initialized";                        
                        LLed_Track2.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track2.Text = "                     Not Initialized";                        
                        LLed_Track3.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track3.Text = "                     Not Initialized";
                        LLed_Track4.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track4.Text = "                     Not Initialized";
                        LLed_Track5.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track5.Text = "                     Not Initialized";
                        LLed_Track6.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track6.Text = "                     Not Initialized";
                        LLed_Track7.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track7.Text = "                     Not Initialized";
                        LLed_Track8.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track8.Text = "                     Not Initialized";
                        LLed_Track9.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track9.Text = "                     Not Initialized";
                        LLed_Track10.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track10.Text = "                     Not Initialized";
                        LLed_Track11.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                        LLed_Track11.Text = "                     Not Initialized";
                        /*PB_Tr1_Train.Hide();*/ PB_Tr1_Inline.Hide();
                        PB_Tr2_Train.Hide(); PB_Tr2_Inline.Hide();
                        PB_Tr3_Train.Hide(); PB_Tr3_Inline.Hide();
                        PB_Tr4_Train.Hide(); PB_Tr4_Inline.Hide();
                        PB_Tr5_Train.Hide(); PB_Tr5_Inline.Hide();
                        PB_Tr6_Train.Hide(); PB_Tr6_Inline.Hide();
                        PB_Tr7_Train.Hide(); PB_Tr7_Inline.Hide();
                        PB_Tr8_Train.Hide(); PB_Tr8_Inline.Hide();
                        PB_Tr9_Train.Hide(); PB_Tr9_Inline.Hide();
                        PB_Tr10_Train.Hide(); PB_Tr10_Inline.Hide();
                        PB_Tr11_Train.Hide(); PB_Tr11_Inline.Hide();


                        Btn_Collect_TOP.Enabled = true;
                        Btn_Start_Fiddle_TOP.Enabled = true;
                        Btn_Stop_Fiddle_TOP.Enabled = false;
                        manualModeToolStripMenuItem.Enabled = true;
                        break;

                    default: break;
                }
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SetLedIndicator
         *               
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
        public void SetLedIndicator(string Indicator, int Val, string Log)
        {
            if (ReceivedCmd.InvokeRequired)
            {
                SetLedIndicatorCallback d = new SetLedIndicatorCallback(SetLedIndicator);
                ReceivedCmd.Invoke(d, new object[] { Indicator, Val, Log });  // invoking itself
            }
            else
            {
                int a = 0;
                a = Convert.ToInt16(Val) >> 4;
                
                switch (Indicator)
                {
                    case "LLed_Heart": if (Val >= 1)
                        {
                            LLed_Heart.BackColor = Color.Lime;
                            LLed_Heart.ForeColor = Color.Black;
                            LLed_Heart.Text = "Aligned";
                        }
                        if (Val == 0)
                        {
                            LLed_Heart.BackColor = Color.Red;
                            LLed_Heart.ForeColor = Color.Yellow;
                            LLed_Heart.Text = "Unaligned";
                        }
                        break;

                    case "2":
                        break;

                    case "LLed_F11": if (Val >= 1)
                        {
                            LLed_F11.BackColor = Color.Yellow;
                            MoveTrainImage("LLed_F11", true);
                        }
                        if (Val == 0)
                        {
                            if (Initialized == false)
                            {
                                LLed_F11.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                            }
                            else
                            {
                                LLed_F11.BackColor = Color.Transparent; // change color status else sensor keeps being red. Run before UpdateTrackIndicatorColor()
                                CheckWhichTrackInline();
                            }
                            MoveTrainImage("LLed_F11", false);
                        }
                        break;

                    case "LLed_EOS10": if (Val >= 1)
                        {
                            LLed_EOS10.BackColor = Color.Red;
                            LLed_EOS10.ForeColor = Color.Yellow;
                        }
                        if (Val == 0)
                        {
                            LLed_EOS10.BackColor = Color.Transparent;
                            LLed_EOS10.ForeColor = Color.Black;
                        }
                        break;

                    case "LLed_EOS11": if (Val >= 1)
                        {
                            LLed_EOS11.BackColor = Color.Red;
                            LLed_EOS11.ForeColor = Color.Yellow;
                        }
                        if (Val == 0)
                        {
                            LLed_EOS11.BackColor = Color.Transparent;
                            LLed_EOS11.ForeColor = Color.Black;
                        }
                        break;

                    case "6":
                        break;

                    case "LLed_F13": if (Val >= 1)
                        {
                            LLed_F13.BackColor = Color.Red;
                            LLed_F13.ForeColor = Color.Yellow;
                            MoveTrainImage("LLed_F13", true);
                        }
                        if (Val == 0)
                        {
                            if (Initialized == false)
                            {
                                LLed_F13.ForeColor = Color.Black;
                                LLed_F13.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                            }
                            else
                            {
                                LLed_F13.ForeColor = Color.Black;
                                LLed_F13.BackColor = Color.Transparent; // change color status else sensor keeps being red. Run before UpdateTrackIndicatorColor()
                                CheckWhichTrackInline();
                            }
                            MoveTrainImage("LLed_F13", false);
                        }
                        break;

                    case "LLed_F12": if (Val >= 1)
                        {
                            LLed_F12.BackColor = Color.Red;
                            LLed_F12.ForeColor = Color.Yellow;
                            MoveTrainImage("LLed_F12", true);
                        }
                        if (Val == 0)
                        {
                            if (Initialized == false)
                            {
                                LLed_F12.BackColor = m_FYAppVar.TrackNotInitializedColor;;
                                LLed_F12.ForeColor = Color.Black;
                            }
                            else
                            {
                                LLed_F12.ForeColor = Color.Black;
                                LLed_F12.BackColor = Color.Transparent; // change color status else sensor keeps being red. Run before UpdateTrackIndicatorColor()
                                CheckWhichTrackInline();
                            }
                            MoveTrainImage("LLed_F12", false);
                        }
                        break;

                    case "LLed_Block5B": if (Val >= 1)
                        {
                            LLed_Block5B.BackColor = m_FYAppVar.TrackOccupiedColor;//     Color.Lime;//GetBackColor when active  
                            MoveTrainImage("LLed_Block5B", true);
                            PB_Bk5_Train.Show();
                        }
                        if (Val == 0)
                        {
                            LLed_Block5B.BackColor = Color.Transparent;//    Color.Transparent;//GetBackcolor when not active
                            PB_Bk5_Train.Hide();
                            MoveTrainImage("LLed_Block5B", false);
                        }
                        PB_Bk5_Train.BackColor = LLed_Block5B.BackColor;
                        break;

                    case "LLed_Block8A": if (Val >= 1)
                        {
                            LLed_Block8A.BackColor = m_FYAppVar.TrackOccupiedColor;//Color.Lime;
                            MoveTrainImage("LLed_Block8A", true);
                            PB_Bk8A_Train.Show();                            
                        }
                        if (Val == 0)
                        {
                            LLed_Block8A.BackColor = Color.Transparent;//   Color.Transparent;
                            //PB_Bk8A_Train.Hide();
                            PB_Bk8A_Train.Image = null;                     // clear image for next train drive_out
                            MoveTrainImage("LLed_Block8A", false);
                        }
                        PB_Bk8A_Train.BackColor = LLed_Block8A.BackColor;
                        break;

                    case "LLed_TrackPower": if (Val >= 1)
                        {
                            LLed_TrackPower.BackColor = m_FYAppVar.TrackOccupiedColor;//Color.Lime;
                            LLed_TrackPower.Text = "Coupled";
                            LLed_TrackPower.ForeColor = Color.Black;
                        }
                        if (Val == 0)
                        {
                            LLed_TrackPower.BackColor = Color.Red;
                            LLed_TrackPower.Text = "Uncoupled";
                            LLed_TrackPower.ForeColor = Color.Yellow;
                        }
                        break;

                    case "LLed_Block5BIn": if (Val >= 1)
                        {
                            PB_Block5BIn.Image = SeinRed;
                            LLed_Block5BIn.BackColor = Color.Red;
                            Btn_Bezet5BOn_TOP.Text = "Off";
                            Btn_Bezet5BOn_TOP_Click_Toggle = false;
                        }
                        if (Val == 0)
                        {
                            PB_Block5BIn.Image = SeinGreen;
                            LLed_Block5BIn.BackColor = m_FYAppVar.TrackOccupiedColor;             //Color.Lime;
                            Btn_Bezet5BOn_TOP.Text = "On";
                            Btn_Bezet5BOn_TOP_Click_Toggle = true;
                            MoveTrainImage("LLed_Block5BIn", true);
                        }
                        break;

                    case "LLed_Block6In": if (Val >= 1)
                        {
                            PB_Block6In.Image = SeinRed;
                            LLed_Block6In.BackColor = Color.Red;
                            Btn_Bezet6On_TOP.Text = "Off";
                            Btn_Bezet6On_TOP_Click_Toggle = false;
                        }
                        if (Val == 0)
                        {
                            PB_Block6In.Image = SeinGreen;
                            LLed_Block6In.BackColor = m_FYAppVar.TrackOccupiedColor;             //Color.Lime;
                            Btn_Bezet6On_TOP.Text = "On";
                            Btn_Bezet6On_TOP_Click_Toggle = true;
                        }
                        break;

                    case "LLed_Block7In": if (Val >= 1)
                        {
                            PB_Block7In.Image = SeinRed; 
                            LLed_Block7In.BackColor = Color.Red;
                            Btn_Bezet7On_TOP.Text = "Off";
                            Btn_Bezet7On_TOP_Click_Toggle = false;
                        }
                        if (Val == 0)
                        {
                            PB_Block7In.Image = SeinGreen; 
                            LLed_Block7In.BackColor = m_FYAppVar.TrackOccupiedColor;             //Color.Lime;                                                        
                            Btn_Bezet7On_TOP.Text = "On";
                            Btn_Bezet7On_TOP_Click_Toggle = true;
                            MoveTrainImage("LLed_Block7In", true);
                        }
                        break;

                    case "LLed_Resistor": if (Val >= 1)
                        {
                            LLed_Resistor.BackColor = m_FYAppVar.TrackOccupiedColor;             //Color.Lime;
                        }
                        if (Val == 0)
                        {
                            LLed_Resistor.BackColor = Color.Transparent;
                        }
                        break;

                    case "LLed_Track1": 
                        if (Val >= 1 && TrackStatusLight[1] == true)       // If train present and Track is active and track is enabled
                        {
                            LLed_Track1.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr1_Train.Show();                                                                // Train present?
                            PB_Tr1_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[1] == false) // If train present and Track is not active and track is enabled
                        {
                            LLed_Track1.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr1_Train.Show();                                                                // Train present?
                            PB_Tr1_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        } 
                        else if (Val == 0 && Initialized == true)                                                 // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track1.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr1_Train.Hide();                                                                // Train present?
                            PB_Tr1_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track1.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track1.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr1_Train.Hide();                                                                // Train present?
                            PB_Tr1_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr1_Active.BackColor = LLed_Track1.BackColor;
                        PB_Tr1_Inline.BackColor = LLed_Track1.BackColor;
                        PB_Tr1_Train.BackColor = LLed_Track1.BackColor;
                        break;

                    case "LLed_Track2":
                        if (Val >= 1 && TrackStatusLight[2] == true)                                            // If train present and Track is active and track is enabled
                        {
                            LLed_Track2.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr2_Train.Show();                                                                // Train present?
                            PB_Tr2_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[2] == false)                                      // If train present and Track is not active and track is enabled
                        {
                            LLed_Track2.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr2_Train.Show();                                                                // Train present?
                            PB_Tr2_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                               // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track2.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr2_Train.Hide();                                                                // Train present?
                            PB_Tr2_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track2.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track2.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr2_Train.Hide();                                                                // Train present?
                            PB_Tr2_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr2_Active.BackColor = LLed_Track2.BackColor;
                        PB_Tr2_Inline.BackColor = LLed_Track2.BackColor;
                        PB_Tr2_Train.BackColor = LLed_Track2.BackColor;
                        break;

                    case "LLed_Track3":
                        if (Val >= 1 && TrackStatusLight[3] == true)                                            // If train present and Track is active and track is enabled
                        {
                            LLed_Track3.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr3_Train.Show();                                                                // Train present?
                            PB_Tr3_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[3] == false)                                      // If train present and Track is not active and track is enabled
                        {
                            LLed_Track3.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr3_Train.Show();                                                                // Train present?
                            PB_Tr3_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                               // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track3.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr3_Train.Hide();                                                                // Train present?
                            PB_Tr3_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track3.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track3.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr3_Train.Hide();                                                                // Train present?
                            PB_Tr3_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr3_Active.BackColor = LLed_Track3.BackColor;
                        PB_Tr3_Inline.BackColor = LLed_Track3.BackColor;
                        PB_Tr3_Train.BackColor = LLed_Track3.BackColor;
                        break;

                    case "LLed_Track4":
                        if (Val >= 1 && TrackStatusLight[4] == true)                                            // If train present and Track is active and track is enabled
                        {
                            LLed_Track4.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr4_Train.Show();                                                                // Train present?
                            PB_Tr4_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[4] == false)                                      // If train present and Track is not active and track is enabled
                        {
                            LLed_Track4.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr4_Train.Show();                                                                // Train present?
                            PB_Tr4_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                                   // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track4.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr4_Train.Hide();                                                                // Train present?
                            PB_Tr4_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track4.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track4.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr4_Train.Hide();                                                                // Train present?
                            PB_Tr4_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr4_Active.BackColor = LLed_Track4.BackColor;
                        PB_Tr4_Inline.BackColor = LLed_Track4.BackColor;
                        PB_Tr4_Train.BackColor = LLed_Track4.BackColor;
                        break;

                    case "LLed_Track5":
                        if (Val >= 1 && TrackStatusLight[5] == true)                                            // If train present and Track is active and track is enabled
                        {
                            LLed_Track5.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr5_Train.Show();                                                                // Train present?
                            PB_Tr5_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[5] == false)                                      // If train present and Track is not active and track is enabled
                        {
                            LLed_Track5.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr5_Train.Show();                                                                // Train present?
                            PB_Tr5_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                                 // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track5.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr5_Train.Hide();                                                                // Train present?
                            PB_Tr5_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track5.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track5.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr5_Train.Hide();                                                                // Train present?
                            PB_Tr5_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr5_Active.BackColor = LLed_Track5.BackColor;
                        PB_Tr5_Inline.BackColor = LLed_Track5.BackColor;
                        PB_Tr5_Train.BackColor = LLed_Track5.BackColor;
                        break;

                    case "LLed_Track6":
                        if (Val >= 1 && TrackStatusLight[6] == true)                                            // If train present and Track is active and track is enabled
                        {
                            LLed_Track6.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr6_Train.Show();                                                                // Train present?
                            PB_Tr6_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[6] == false)                                      // If train present and Track is not active and track is enabled
                        {
                            LLed_Track6.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr6_Train.Show();                                                                // Train present?
                            PB_Tr6_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                                 // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track6.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr6_Train.Hide();                                                                // Train present?
                            PB_Tr6_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track6.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track6.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr6_Train.Hide();                                                                // Train present?
                            PB_Tr6_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr6_Active.BackColor = LLed_Track6.BackColor;
                        PB_Tr6_Inline.BackColor = LLed_Track6.BackColor;
                        PB_Tr6_Train.BackColor = LLed_Track6.BackColor;
                        break;

                    case "LLed_Track7":
                        if (Val >= 1 && TrackStatusLight[7] == true)                                            // If train present and Track is active and track is enabled
                        {
                            LLed_Track7.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr7_Train.Show();                                                                // Train present?
                            PB_Tr7_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[7] == false)                                      // If train present and Track is not active and track is enabled
                        {
                            LLed_Track7.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr7_Train.Show();                                                                // Train present?
                            PB_Tr7_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                               // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track7.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr7_Train.Hide();                                                                // Train present?
                            PB_Tr7_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track7.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track7.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr7_Train.Hide();                                                                // Train present?
                            PB_Tr7_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr7_Active.BackColor = LLed_Track7.BackColor;
                        PB_Tr7_Inline.BackColor = LLed_Track7.BackColor;
                        PB_Tr7_Train.BackColor = LLed_Track7.BackColor;
                        break;

                    case "LLed_Track8":
                        if (Val >= 1 && TrackStatusLight[8] == true)                                            // If train present and Track is active and track is enabled
                        {
                            LLed_Track8.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr8_Train.Show();                                                                // Train present?
                            PB_Tr8_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[8] == false)                                      // If train present and Track is not active and track is enabled
                        {
                            LLed_Track8.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr8_Train.Show();                                                                // Train present?
                            PB_Tr8_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }                        
                        else if (Val == 0 && Initialized == true)                                               // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track8.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr8_Train.Hide();                                                                // Train present?
                            PB_Tr8_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track8.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track8.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr8_Train.Hide();                                                                // Train present?
                            PB_Tr8_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr8_Active.BackColor = LLed_Track8.BackColor;
                        PB_Tr8_Inline.BackColor = LLed_Track8.BackColor;
                        PB_Tr8_Train.BackColor = LLed_Track8.BackColor;
                        break;

                    case "LLed_Track9":
                        if (Val >= 1 && TrackStatusLight[9] == true)                                            // If train present and Track is active and track is enabled
                        {
                            LLed_Track9.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr9_Train.Show();                                                                // Train present?
                            PB_Tr9_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[9] == false)                                      // If train present and Track is not active and track is enabled
                        {
                            LLed_Track9.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr9_Train.Show();                                                                // Train present?
                            PB_Tr9_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                               // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track9.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr9_Train.Hide();                                                                // Train present?
                            PB_Tr9_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track9.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track9.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr9_Train.Hide();                                                                // Train present?
                            PB_Tr9_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr9_Active.BackColor = LLed_Track9.BackColor;
                        PB_Tr9_Inline.BackColor = LLed_Track9.BackColor;
                        PB_Tr9_Train.BackColor = LLed_Track9.BackColor;
                        break;

                    case "LLed_Track10":
                        if (Val >= 1 && TrackStatusLight[10] == true)                                           // If train present and Track is active and track is enabled
                        {
                            LLed_Track10.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr10_Train.Show();                                                                // Train present?
                            PB_Tr10_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[10] == false)                                     // If train present and Track is not active and track is enabled
                        {
                            LLed_Track10.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr10_Train.Show();                                                                // Train present?
                            PB_Tr10_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                                 // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track10.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr10_Train.Hide();                                                                // Train present?
                            PB_Tr10_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track10.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track10.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr10_Train.Hide();                                                                // Train present?
                            PB_Tr10_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr10_Active.BackColor = LLed_Track10.BackColor;
                        PB_Tr10_Inline.BackColor = LLed_Track10.BackColor;
                        PB_Tr10_Train.BackColor = LLed_Track10.BackColor;
                        break;

                    case "LLed_Track11":
                        if (Val >= 1 && TrackStatusLight[11] == true)                                           // If train present and Track is active and track is enabled
                        {
                            LLed_Track11.BackColor = m_FYAppVar.TrackOccupiedColor;                              // Set TrackOccupiedColor
                            //PB_Tr11_Train.Show();                                                                // Train present?
                            PB_Tr11_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val >= 1 && TrackStatusLight[11] == false)                                     // If train present and Track is not active and track is enabled
                        {
                            LLed_Track11.BackColor = m_FYAppVar.TrackNotActiveColor;                             // Set TrackNotActiveColor (but is occupied)
                            //PB_Tr11_Train.Show();                                                                // Train present?
                            PB_Tr11_Train.Image = Train;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        else if (Val == 0 && Initialized == true)                                                   // If train not present and FY is initialized and Track is enabled
                        {
                            LLed_Track11.BackColor = Color.Transparent;                                          // Set Color.Transparent
                            //PB_Tr11_Train.Hide();                                                                // Train present?
                            PB_Tr11_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }

                        if (Initialized == false)                                                               // If FY is not initialized
                        {
                            LLed_Track11.BackColor = m_FYAppVar.TrackNotInitializedColor;                        // Set TrackNotInitializedColor
                            LLed_Track11.Text = "                     Not Initialized";                          // Set Track text
                            //PB_Tr11_Train.Hide();                                                                // Train present?
                            PB_Tr11_Train.Image = null;
                            CheckWhichTrackInline();                                                            // Sensor background color update
                        }
                        PB_Tr11_Active.BackColor = LLed_Track11.BackColor;
                        PB_Tr11_Inline.BackColor = LLed_Track11.BackColor;
                        PB_Tr11_Train.BackColor = LLed_Track11.BackColor;
                        break;                    

                    case "LLed_Block6": if (Val >= 1)
                        {
                            LLed_Block6.BackColor = m_FYAppVar.TrackOccupiedColor;//Color.Lime; 
                            MoveTrainImage("LLed_Block6", true);
                            PB_Bk6_Train.Show();
                        }
                        if (Val == 0)
                        {
                            LLed_Block6.BackColor = Color.Transparent;//   Color.Transparent;
                            PB_Bk6_Train.Hide();
                            MoveTrainImage("LLed_Block6", false);
                        }
                        PB_Bk6_Train.BackColor = LLed_Block6.BackColor;
                        break;

                    case "LLed_Block7": if (Val >= 1)
                        {
                            LLed_Block7.BackColor = m_FYAppVar.TrackOccupiedColor;//Color.Lime;
                            MoveTrainImage("LLed_Block7", true);
                            PB_Bk7_Train.Show();
                        }
                        if (Val == 0)
                        {
                            LLed_Block7.BackColor = Color.Transparent;//   Color.Transparent;
                            PB_Bk7_Train.Hide();
                            MoveTrainImage("LLed_Block7", false);
                        }
                        PB_Bk7_Train.BackColor = LLed_Block7.BackColor;
                        break;

                    case "LLed_TrackPower15V": if (Val >= 1)
                        {
                            LLed_15VTrackPower.BackColor = Color.Lime;
                            LLed_15VTrackPower.ForeColor = Color.Black;
                            LLed_15VTrackPower.Text = "Drive Power On";
                        }
                        if (Val == 0)
                        {
                            LLed_15VTrackPower.BackColor = Color.Red;
                            LLed_15VTrackPower.ForeColor = Color.Yellow;
                            LLed_15VTrackPower.Text = "Drive Power Off";                            
                        }
                        break;

                    case "LLed_F10": if (Val >= 1)
                        {
                            LLed_F10.BackColor = Color.Yellow;
                            LLed_F10_2_TOP.BackColor = Color.Yellow;
                            MoveTrainImage("LLed_F10", true);
                        }
                        if (Val == 0)
                        {
                            LLed_F10.BackColor = Color.Transparent;
                            LLed_F10_2_TOP.BackColor = Color.Transparent;
                            MoveTrainImage("LLed_F10", false);
                        }
                        break;

                    case "LLed_M10": if (Val >= 1)
                        {
                            LLed_M10.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            LLed_M10.BackColor = Color.Transparent;
                        }
                        break;

                    case "Track_No": Track_No.Text = Convert.ToString(a);

                        for (int i = 0; i < 12; i++)
                        {
                            TrackStatusLight[i] = false;
                        }
                        ALlPB_InlineOff();
                        TrackStatusLight[a] = true;
                        TrackPB_InlineOn(a);
                        ShiftIndicatorPos(a);
                        if (Initialized == true)
                        {
                            UpdateTrackIndicatorColor();                    // After/during shift update color of tracks accordingly
                        }
                        break;

                    case "ForceNextTrack": ForceNextTrack.Value = Val;
                        break;

                    default: break;
                }
            }
        }
        /*#--------------------------------------------------------------------------#*/
        /*  Description: MoveTrainImage(string Indicator, bool Active)
         *               Sets the images of a "moving train" to various locations
         *              
         *               
         *
         *  Input(s)   : Indicator and status
         *
         *  Output(s)  : sets images to various block/track
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
        private enum State
        {
            Start, DriveIn_1, DriveIn_2, DriveIn_3, DriveIn_4, DriveOut_1, DriveOut_2, DriveOut_3, DriveOut_4, DriveOut_5
        };
        private State TrainMove = State.Start;
        private object ExecuteLock = new object();

        private void MoveTrainImage(string Indicator, bool Active)
        {
            //lock (ExecuteLock)
            //{
                switch (TrainMove)
                {
                    case State.Start:
                        if (PB_Block5BIn.Image == SeinGreen)
                        {
                            if (Indicator == "LLed_Block6" && Active == true)
                            {
                                PB_Bk5_Train.Image = HalfWagon90;
                                TrainMove = State.DriveIn_2;
                            }
                            else { TrainMove = State.DriveIn_1; }
                        }
                        else if (PB_Block7In.Image == SeinGreen)
                        {
                            TrainMove = State.DriveOut_1;
                        }
                        break;

                    case State.DriveIn_1:
                        if (Indicator == "LLed_Block6" && Active == true)
                        {
                            PB_Bk5_Train.Image = HalfWagon90;                            
                            TrainMove = State.DriveIn_2;
                        }
                        break;

                    case State.DriveIn_2:
                        if ((Indicator == "LLed_Block5B" && Active == false) || (Indicator == "LLed_F10" && Active == true)) 
                        {
                            PB_Bk5_Train.Image = Train90;
                            PB_Bk6_Train.Image = Train;
                            TrainMove = State.DriveIn_3;
                        }
                        break;

                    case State.DriveIn_3:
                        if (Indicator == "LLed_F13" && Active == true)
                        {
                            PB_Bk6_Train.Image = HalfWagon;
                            MoveTrainUpdateTrack(HalfTrain);
                            TrainMove = State.DriveIn_4;
                        }
                        break;

                    case State.DriveIn_4:
                        if (Indicator == "LLed_F13" && Active == false)
                        {
                            PB_Bk6_Train.Image = HalfTrain;
                            MoveTrainUpdateTrack(Train);
                            TrainMove = State.Start;
                        }
                        break;
                    /*##################################################################*/
                    case State.DriveOut_1:
                        if (Indicator == "LLed_F12" && Active == true)
                        {
                            PB_Bk7_Train.Image = HalfTrain;
                            MoveTrainUpdateTrack(HalfWagon);
                            TrainMove = State.DriveOut_2;
                        }                        
                        break;

                    case State.DriveOut_2:
                        if ((Indicator == "LLed_F12" && Active == false) || (Indicator == "LLed_Block8A" && Active == true)) // for real life check on F12 first, check on block8A due to simulator
                        {
                            PB_Bk7_Train.Image = Train;
                            MoveTrainUpdateTrack(null);
                            TrainMove = State.DriveOut_3;
                        }
                        break;

                    case State.DriveOut_3:
                        if ((Indicator == "LLed_Block8A" && Active == true) || (Indicator == "LLed_F12" && Active == false)) // for real life check on block8A first, check on F12 due to simulator
                        {
                            PB_Bk7_Train.Image = HalfWagon;
                            PB_Bk8A_Train.Image = HalfTrain270;
                            TrainMove = State.DriveOut_4;
                        }
                        break;

                    case State.DriveOut_4:
                        if (Indicator == "LLed_Block7" && Active == false)
                        {
                            PB_Bk7_Train.Image = null;
                            PB_Bk8A_Train.Image = Train270;
                            TrainMove = State.DriveOut_5;
                        }
                        break;

                    case State.DriveOut_5:
                        if (PB_Block7In.Image == SeinRed)
                        {
                            TrainMove = State.Start;

                        }
                        break;

                    default: break;
                }
           // }
        }
        /*#--------------------------------------------------------------------------#*/
        /*  Description: 
         *              
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
        private void MoveTrainUpdateTrack(Image NewTrackImage)
        {
            if (TrackStatusLight[1] == true)
            {
                PB_Tr1_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[2] == true)
            {
                PB_Tr2_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[3] == true)
            {
                PB_Tr3_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[4] == true)
            {
                PB_Tr4_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[5] == true)
            {
                PB_Tr5_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[6] == true)
            {
                PB_Tr6_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[7] == true)
            {
                PB_Tr7_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[8] == true)
            {
                PB_Tr8_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[9] == true)
            {
                PB_Tr9_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[10] == true)
            {
                PB_Tr10_Train.Image = NewTrackImage;
            }
            else if (TrackStatusLight[11] == true)
            {
                PB_Tr11_Train.Image = NewTrackImage;
            }
        }
        /*#--------------------------------------------------------------------------#*/
        /*  Description: ALlPB_InlineOff and TrackPB_InlineOn
         *              Hides all inline arrows, and shows the arrow on the active track 
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
        private void ALlPB_InlineOff()
        {
            PB_Tr1_Inline.Hide();
            PB_Tr2_Inline.Hide();
            PB_Tr3_Inline.Hide();
            PB_Tr4_Inline.Hide();
            PB_Tr5_Inline.Hide();
            PB_Tr6_Inline.Hide();
            PB_Tr7_Inline.Hide();
            PB_Tr8_Inline.Hide();
            PB_Tr9_Inline.Hide();
            PB_Tr10_Inline.Hide();
            PB_Tr11_Inline.Hide();

        }
        private void TrackPB_InlineOn(int a)
        {
            switch (a)
            {
                case 1:
                    PB_Tr1_Inline.Show();
                    break;
                case 2:
                    PB_Tr2_Inline.Show();
                    break;
                case 3:
                    PB_Tr3_Inline.Show();
                    break;
                case 4:
                    PB_Tr4_Inline.Show();
                    break;
                case 5:
                    PB_Tr5_Inline.Show();
                    break;
                case 6:
                    PB_Tr6_Inline.Show();
                    break;
                case 7:
                    PB_Tr7_Inline.Show();
                    break;
                case 8:
                    PB_Tr8_Inline.Show();
                    break;
                case 9:
                    PB_Tr9_Inline.Show();
                    break;
                case 10:
                    PB_Tr10_Inline.Show();
                    break;
                case 11:
                    PB_Tr11_Inline.Show();
                    break;
                default: break;
            }
        }
        /*#--------------------------------------------------------------------------#*/
        /*  Description: UpdateTrackIndicatorColor
         *               
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
        private void UpdateTrackIndicatorColor()
        {
            if (TrackStatusLight[0] == true)                                                    // in between tracks every occupied track becomes TrackNotActiveColor or TrackDisabledColor or TrackDisabledNotOccupiedColor
            {
                if (LLed_Track1.BackColor != Color.Transparent && 
                    LLed_Track1.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[1] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track1.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                    //else
                    //{
                    //    LLed_Track1.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                    //}
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[1] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track1.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track2.BackColor != Color.Transparent &&
                    LLed_Track2.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[2] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track2.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track2.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[2] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track2.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track3.BackColor != Color.Transparent &&
                                    LLed_Track3.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[3] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track3.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track3.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[3] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track3.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track4.BackColor != Color.Transparent &&
                                    LLed_Track4.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[4] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track4.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track4.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[4] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track4.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track5.BackColor != Color.Transparent &&
                                    LLed_Track5.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[5] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track5.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track5.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[5] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track5.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track6.BackColor != Color.Transparent &&
                                    LLed_Track6.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[6] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track6.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track6.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[6] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track6.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track7.BackColor != Color.Transparent &&
                                    LLed_Track7.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[7] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track7.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track7.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[7] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track7.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track8.BackColor != Color.Transparent &&
                                    LLed_Track8.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[8] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track8.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track8.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[8] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track8.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track9.BackColor != Color.Transparent &&
                                    LLed_Track9.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[9] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track9.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track9.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[9] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track9.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track10.BackColor != Color.Transparent &&
                                    LLed_Track10.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[10] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track10.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track10.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[10] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track10.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                if (LLed_Track11.BackColor != Color.Transparent &&
                                    LLed_Track11.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor)          // If the track is not empty (transparent or TrackDisabledNotOccupiedColor)
                {
                    if (m_FYAppVar.icheckBoxTrack[11] == 0)                                      // If the track is not disabled
                    {
                        LLed_Track11.BackColor = m_FYAppVar.TrackNotActiveColor;                 // Set the track not active color (old color was Color.Green)
                    }
                //    else
                //    {
                //        LLed_Track11.BackColor = m_FYAppVar.TrackDisabledColor;                  // Else the track is disabled set the TrackDisabledColor color
                //    }
                }
                //else                                                                            // If the track is empty
                //{
                //    if (m_FYAppVar.icheckBoxTrack[11] == 1)                                      // If the track is disabled
                //    {
                //        LLed_Track11.BackColor = m_FYAppVar.TrackDisabledNotOccupiedColor;       // Set the track not active empty color
                //    }
                //}
                /*---------------------------------------------------------------------------------------------------------------------------------------------*/
                CheckWhichTrackInline();

            }

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/
            // On a track and track becomes TrackOccupiedColor or TrackNotActiveColor when not TrackDisabledColor and not Color.Transparent and not TrackDisabledNotOccupiedColor

            if (TrackStatusLight[1] == true && 
                LLed_Track1.BackColor != Color.Transparent &&
                LLed_Track1.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track1.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant and not TrackDisabledNotOccupiedColor
            {
                LLed_Track1.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track1.BackColor
            }
            else if (TrackStatusLight[1] == false && 
                LLed_Track1.BackColor != Color.Transparent &&
                LLed_Track1.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track1.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant and not TrackDisabledNotOccupiedColor
            {
                LLed_Track1.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track1.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track1.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[2] == true &&
                LLed_Track2.BackColor != Color.Transparent &&
                LLed_Track2.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track2.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track2.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track2.BackColor                
            }
            else if (TrackStatusLight[2] == false &&
                LLed_Track2.BackColor != Color.Transparent &&
                LLed_Track2.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track2.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track2.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track2.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track2.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[3] == true &&
                LLed_Track3.BackColor != Color.Transparent &&
                LLed_Track3.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track3.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track3.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track3.BackColor
            }
            else if (TrackStatusLight[3] == false &&
                LLed_Track3.BackColor != Color.Transparent &&
                LLed_Track3.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track3.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track3.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track3.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track3.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[4] == true &&
                LLed_Track4.BackColor != Color.Transparent &&
                LLed_Track4.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track4.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track4.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track4.BackColor
            }
            else if (TrackStatusLight[4] == false &&
                LLed_Track4.BackColor != Color.Transparent &&
                LLed_Track4.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track4.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track4.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track4.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track4.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[5] == true &&
                LLed_Track5.BackColor != Color.Transparent &&
                LLed_Track5.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track5.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track5.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track5.BackColor
            }
            else if (TrackStatusLight[5] == false &&
                LLed_Track5.BackColor != Color.Transparent &&
                LLed_Track5.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track5.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track5.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track5.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track5.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[6] == true &&
                LLed_Track6.BackColor != Color.Transparent &&
                LLed_Track6.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track6.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track6.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track6.BackColor
            }
            else if (TrackStatusLight[6] == false &&
                LLed_Track6.BackColor != Color.Transparent &&
                LLed_Track6.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track6.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track6.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track6.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track6.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[7] == true &&
                LLed_Track7.BackColor != Color.Transparent &&
                LLed_Track7.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track7.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track7.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track7.BackColor
            }
            else if (TrackStatusLight[7] == false &&
                LLed_Track7.BackColor != Color.Transparent &&
                LLed_Track7.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track7.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track7.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track7.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track7.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[8] == true &&
                LLed_Track8.BackColor != Color.Transparent &&
                LLed_Track8.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track8.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track8.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track8.BackColor
            }
            else if (TrackStatusLight[8] == false &&
                LLed_Track8.BackColor != Color.Transparent &&
                LLed_Track8.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track8.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track8.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track8.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track8.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[9] == true &&
                LLed_Track9.BackColor != Color.Transparent &&
                LLed_Track9.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track9.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track9.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track9.BackColor
            }
            else if (TrackStatusLight[9] == false &&
                LLed_Track9.BackColor != Color.Transparent &&
                LLed_Track9.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track9.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track9.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track9.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track9.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[10] == true &&
                LLed_Track10.BackColor != Color.Transparent &&
                LLed_Track10.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track10.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track10.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track10.BackColor
            }
            else if (TrackStatusLight[10] == false &&
                LLed_Track10.BackColor != Color.Transparent &&
                LLed_Track10.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track10.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track10.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track10.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track10.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            if (TrackStatusLight[11] == true &&
                LLed_Track11.BackColor != Color.Transparent &&
                LLed_Track11.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track11.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is active and not disabled and not transparant
            {
                LLed_Track11.BackColor = m_FYAppVar.TrackOccupiedColor;      // Set the track TrackOccupiedColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track11.BackColor
            }
            else if (TrackStatusLight[11] == false &&
                LLed_Track11.BackColor != Color.Transparent &&
                LLed_Track11.BackColor != m_FYAppVar.TrackDisabledNotOccupiedColor &&
                LLed_Track11.BackColor != m_FYAppVar.TrackDisabledColor)     // If this track is not active and not disabled and not transparant
            {
                LLed_Track11.BackColor = m_FYAppVar.TrackNotActiveColor;     // Set the track TrackNotActiveColor
                CheckWhichTrackInline();                                    // Set the color to the corresponding sensors to the LLed_Track11.BackColor
            }
            else { CheckWhichTrackInline(); }                               // Set the color to the corresponding sensors to the LLed_Track11.BackColor

            /*---------------------------------------------------------------------------------------------------------------------------------------------*/

            PB_Tr1_Active.BackColor = LLed_Track1.BackColor;
            PB_Tr1_Inline.BackColor = LLed_Track1.BackColor;
            PB_Tr1_Train.BackColor = LLed_Track1.BackColor;
            PB_Tr2_Active.BackColor = LLed_Track2.BackColor;
            PB_Tr2_Inline.BackColor = LLed_Track2.BackColor;
            PB_Tr2_Train.BackColor = LLed_Track2.BackColor;
            PB_Tr3_Active.BackColor = LLed_Track3.BackColor;
            PB_Tr3_Inline.BackColor = LLed_Track3.BackColor;
            PB_Tr3_Train.BackColor = LLed_Track3.BackColor;
            PB_Tr4_Active.BackColor = LLed_Track4.BackColor;
            PB_Tr4_Inline.BackColor = LLed_Track4.BackColor;
            PB_Tr4_Train.BackColor = LLed_Track4.BackColor;
            PB_Tr5_Active.BackColor = LLed_Track5.BackColor;
            PB_Tr5_Inline.BackColor = LLed_Track5.BackColor;
            PB_Tr5_Train.BackColor = LLed_Track5.BackColor;
            PB_Tr6_Active.BackColor = LLed_Track6.BackColor;
            PB_Tr6_Inline.BackColor = LLed_Track6.BackColor;
            PB_Tr6_Train.BackColor = LLed_Track6.BackColor;
            PB_Tr7_Active.BackColor = LLed_Track7.BackColor;
            PB_Tr7_Inline.BackColor = LLed_Track7.BackColor;
            PB_Tr7_Train.BackColor = LLed_Track7.BackColor;
            PB_Tr8_Active.BackColor = LLed_Track8.BackColor;
            PB_Tr8_Inline.BackColor = LLed_Track8.BackColor;
            PB_Tr8_Train.BackColor = LLed_Track8.BackColor;
            PB_Tr9_Active.BackColor = LLed_Track9.BackColor;
            PB_Tr9_Inline.BackColor = LLed_Track9.BackColor;
            PB_Tr9_Train.BackColor = LLed_Track9.BackColor;
            PB_Tr10_Active.BackColor = LLed_Track10.BackColor;
            PB_Tr10_Inline.BackColor = LLed_Track10.BackColor;
            PB_Tr10_Train.BackColor = LLed_Track10.BackColor;
            PB_Tr11_Active.BackColor = LLed_Track11.BackColor;
            PB_Tr11_Inline.BackColor = LLed_Track11.BackColor;
            PB_Tr11_Train.BackColor = LLed_Track11.BackColor;


        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: CheckWhichTrackInline
         *               
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
        private void CheckWhichTrackInline()
        {
            if ((LLed_Track1.Location.Y + LLed_Track1.Height / 2) == (LLed_Block6.Location.Y + LLed_Block6.Height / 2))   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                               // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(1);
            }

            else if (LLed_Track2.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(2);
            }

            else if (LLed_Track3.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(3);
            }

            else if (LLed_Track4.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(4);
            }

            else if (LLed_Track5.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(5);
            }

            else if (LLed_Track6.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(6);
            }

            else if (LLed_Track7.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(7);
            }

            else if (LLed_Track8.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(8);
            }

            else if (LLed_Track9.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(9);
            }

            else if (LLed_Track10.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(10);
            }

            else if (LLed_Track11.Location.Y == LLed_Block6.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(11);
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: SensorBackcolorUpdate
         *               
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
        private void SensorBackcolorUpdate(int track)
        {
            switch (track)
            {
                case 1 : 
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track1.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track1.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track1.BackColor;
                    }
                    break;

                case 2:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track2.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track2.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track2.BackColor;
                    }
                    break;

                case 3:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track3.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track3.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track3.BackColor;
                    }
                    break;

                case 4:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track4.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track4.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track4.BackColor;
                    }
                    break;

                case 5:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track5.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track5.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track5.BackColor;
                    }
                    break;

                case 6:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track6.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track6.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track6.BackColor;
                    }
                    break;

                case 7:
                     if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track7.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track7.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track7.BackColor;
                    }
                    break;

                case 8:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track8.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track8.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track8.BackColor;
                    }
                    break;

                case 9:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track9.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track9.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track9.BackColor;
                    }
                    break;

                case 10:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track10.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track10.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track10.BackColor;
                    }
                    break;

                case 11:
                    if (LLed_F11.BackColor != Color.Yellow)
                    {
                        LLed_F11.BackColor = LLed_Track11.BackColor;
                    }
                    if (LLed_F12.BackColor != Color.Red)
                    {
                        LLed_F12.BackColor = LLed_Track11.BackColor;
                    }
                    if (LLed_F13.BackColor != Color.Red)
                    {
                        LLed_F13.BackColor = LLed_Track11.BackColor;
                    }
                    break;

                default: break;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: ShiftIndicatorPos
         *               
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
        private void ShiftIndicatorPos(int val)
        {
            // Check position of Gwin
            GWinHalf = 740 / 2 + GWin.Location.Y;
            GWinX = GWin.Location.X;
            Track6LocY = GWinHalf;// - 8;
            Track6LocX = GWinX + 170;

            switch (val)
            {
                case 1:
                    Track1Loc = 0;
                    Track2Loc = TrackLocDelta * 1;
                    Track3Loc = TrackLocDelta * 2;
                    Track4Loc = TrackLocDelta * 3;
                    Track5Loc = TrackLocDelta * 4;
                    Track6Loc = TrackLocDelta * 5;
                    Track7Loc = TrackLocDelta * 6;
                    Track8Loc = TrackLocDelta * 7;
                    Track9Loc = TrackLocDelta * 8;
                    Track10Loc = TrackLocDelta * 9;
                    Track11Loc = TrackLocDelta * 10;
                    FYPlateLoc = TrackLocDelta * 5;
                    break;

                case 2:
                    Track1Loc = -(TrackLocDelta * 1);
                    Track2Loc = 0;
                    Track3Loc = TrackLocDelta * 1;
                    Track4Loc = TrackLocDelta * 2;
                    Track5Loc = TrackLocDelta * 3;
                    Track6Loc = TrackLocDelta * 4;
                    Track7Loc = TrackLocDelta * 5;
                    Track8Loc = TrackLocDelta * 6;
                    Track9Loc = TrackLocDelta * 7;
                    Track10Loc =TrackLocDelta * 8;
                    Track11Loc = TrackLocDelta * 9; 
                    FYPlateLoc = TrackLocDelta * 4;
                    break;

                case 3:
                    Track1Loc = -(TrackLocDelta * 2);
                    Track2Loc = -(TrackLocDelta * 1);
                    Track3Loc = 0;
                    Track4Loc = TrackLocDelta * 1;
                    Track5Loc = TrackLocDelta * 2;
                    Track6Loc = TrackLocDelta * 3;
                    Track7Loc = TrackLocDelta * 4;
                    Track8Loc = TrackLocDelta * 5;
                    Track9Loc = TrackLocDelta * 6;
                    Track10Loc =TrackLocDelta * 7;
                    Track11Loc =TrackLocDelta * 8; 
                    FYPlateLoc = TrackLocDelta * 3;
                    break;

                case 4:
                    Track1Loc = -(TrackLocDelta * 3);
                    Track2Loc = -(TrackLocDelta * 2);
                    Track3Loc = -(TrackLocDelta * 1);
                    Track4Loc = 0;
                    Track5Loc = TrackLocDelta * 1;
                    Track6Loc = TrackLocDelta * 2;
                    Track7Loc = TrackLocDelta * 3;
                    Track8Loc = TrackLocDelta * 4;
                    Track9Loc = TrackLocDelta * 5;
                    Track10Loc =TrackLocDelta * 6;
                    Track11Loc = TrackLocDelta * 7;
                    FYPlateLoc = TrackLocDelta * 2;
                    break;

                case 5:
                    Track1Loc = -(TrackLocDelta * 4);
                    Track2Loc = -(TrackLocDelta * 3);
                    Track3Loc = -(TrackLocDelta * 2);
                    Track4Loc = -(TrackLocDelta * 1);
                    Track5Loc = 0;
                    Track6Loc = TrackLocDelta * 1;
                    Track7Loc = TrackLocDelta * 2;
                    Track8Loc = TrackLocDelta * 3;
                    Track9Loc = TrackLocDelta * 4;
                    Track10Loc =TrackLocDelta * 5;
                    Track11Loc = TrackLocDelta * 6;
                    FYPlateLoc = TrackLocDelta * 1;
                    break;

                case 6:
                    Track1Loc = -(TrackLocDelta * 5);
                    Track2Loc = -(TrackLocDelta * 4);
                    Track3Loc = -(TrackLocDelta * 3);
                    Track4Loc = -(TrackLocDelta * 2);
                    Track5Loc = -(TrackLocDelta * 1);
                    Track6Loc = 0;
                    Track7Loc = TrackLocDelta * 1;
                    Track8Loc = TrackLocDelta * 2;
                    Track9Loc = TrackLocDelta * 3;
                    Track10Loc =TrackLocDelta * 4;
                    Track11Loc = TrackLocDelta * 5;
                    FYPlateLoc = 0;
                    break;

                case 7:
                    Track1Loc = -(TrackLocDelta * 6);
                    Track2Loc = -(TrackLocDelta * 5);
                    Track3Loc = -(TrackLocDelta * 4);
                    Track4Loc = -(TrackLocDelta * 3);
                    Track5Loc = -(TrackLocDelta * 2);
                    Track6Loc = -(TrackLocDelta * 1);
                    Track7Loc = 0;
                    Track8Loc = TrackLocDelta * 1;
                    Track9Loc = TrackLocDelta * 2;
                    Track10Loc =TrackLocDelta * 3;
                    Track11Loc = TrackLocDelta * 4;
                    FYPlateLoc = -(TrackLocDelta * 1);
                    break;

                case 8:
                    Track1Loc = -(TrackLocDelta * 7);
                    Track2Loc = -(TrackLocDelta * 6);
                    Track3Loc = -(TrackLocDelta * 5);
                    Track4Loc = -(TrackLocDelta * 4);
                    Track5Loc = -(TrackLocDelta * 3);
                    Track6Loc = -(TrackLocDelta * 2);
                    Track7Loc = -(TrackLocDelta * 1);
                    Track8Loc = 0;
                    Track9Loc = TrackLocDelta * 1;
                    Track10Loc =TrackLocDelta * 2;
                    Track11Loc = TrackLocDelta * 3;
                    FYPlateLoc = -(TrackLocDelta * 2);
                    break;

                case 9:
                    Track1Loc = -(TrackLocDelta * 8);
                    Track2Loc = -(TrackLocDelta * 7);
                    Track3Loc = -(TrackLocDelta * 6);
                    Track4Loc = -(TrackLocDelta * 5);
                    Track5Loc = -(TrackLocDelta * 4);
                    Track6Loc = -(TrackLocDelta * 3);
                    Track7Loc = -(TrackLocDelta * 2);
                    Track8Loc = -(TrackLocDelta * 1);
                    Track9Loc = 0;
                    Track10Loc = TrackLocDelta * 1;
                    Track11Loc = TrackLocDelta * 2;
                    FYPlateLoc = -(TrackLocDelta * 3);
                    break;

                case 10:
                    Track1Loc = -(TrackLocDelta * 9);
                    Track2Loc = -(TrackLocDelta * 8);
                    Track3Loc = -(TrackLocDelta * 7);
                    Track4Loc = -(TrackLocDelta * 6);
                    Track5Loc = -(TrackLocDelta * 5);
                    Track6Loc = -(TrackLocDelta * 4);
                    Track7Loc = -(TrackLocDelta * 3);
                    Track8Loc = -(TrackLocDelta * 2);
                    Track9Loc = -(TrackLocDelta * 1);
                    Track10Loc = 0;
                    Track11Loc = TrackLocDelta * 1;
                    FYPlateLoc = -(TrackLocDelta * 4);
                    break;

                case 11:
                    Track1Loc = -(TrackLocDelta * 10);
                    Track2Loc = -(TrackLocDelta * 9);
                    Track3Loc = -(TrackLocDelta * 8);
                    Track4Loc = -(TrackLocDelta * 7);
                    Track5Loc = -(TrackLocDelta * 6);
                    Track6Loc = -(TrackLocDelta * 5);
                    Track7Loc = -(TrackLocDelta * 4);
                    Track8Loc = -(TrackLocDelta * 3);
                    Track9Loc = -(TrackLocDelta * 2);
                    Track10Loc = -(TrackLocDelta * 1);
                    Track11Loc = 0;
                    FYPlateLoc = -(TrackLocDelta * 5);
                    break;

                default: break;
            }

            LLed_Track1.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track1.Height / 2 + Track1Loc);
            PB_Tr1_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track1Loc);
            PB_Tr1_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track1Loc);
            PB_Tr1_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track1Loc);

            LLed_Track2.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track2.Height / 2 + Track2Loc);
            PB_Tr2_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track2Loc);
            PB_Tr2_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track2Loc);
            PB_Tr2_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track2Loc);

            LLed_Track3.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track3.Height / 2 + Track3Loc);
            PB_Tr3_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track3Loc);
            PB_Tr3_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track3Loc);
            PB_Tr3_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track3Loc);

            LLed_Track4.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track4.Height / 2 + Track4Loc);
            PB_Tr4_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track4Loc);
            PB_Tr4_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track4Loc);
            PB_Tr4_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track4Loc);

            LLed_Track5.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track5.Height / 2 + Track5Loc);
            PB_Tr5_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track5Loc);
            PB_Tr5_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track5Loc);
            PB_Tr5_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track5Loc);

            LLed_Track6.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track6.Height / 2 + Track6Loc);
            PB_Tr6_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track6Loc);
            PB_Tr6_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track6Loc);
            PB_Tr6_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track6Loc);

            LLed_Track7.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track7.Height / 2 + Track7Loc);
            PB_Tr7_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track7Loc);
            PB_Tr7_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track7Loc);
            PB_Tr7_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track7Loc);

            LLed_Track8.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track8.Height / 2 + Track8Loc);
            PB_Tr8_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track8Loc);
            PB_Tr8_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track8Loc);
            PB_Tr8_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track8Loc);

            LLed_Track9.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track9.Height / 2 + Track9Loc);
            PB_Tr9_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track9Loc);
            PB_Tr9_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track9Loc);
            PB_Tr9_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track9Loc);

            LLed_Track10.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track10.Height / 2 + Track10Loc);
            PB_Tr10_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track10Loc);
            PB_Tr10_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track10Loc);
            PB_Tr10_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track10Loc);

            LLed_Track11.Location = new System.Drawing.Point(Track6LocX, Track6LocY - LLed_Track11.Height / 2 + Track11Loc);
            PB_Tr11_Train.Location = new System.Drawing.Point(Track6LocX + 150, Track6LocY - Train.Height / 2 + Track11Loc);
            PB_Tr11_Inline.Location = new System.Drawing.Point(Track6LocX + 270, Track6LocY - Inline.Height / 2 + Track11Loc);
            PB_Tr11_Active.Location = new System.Drawing.Point(Track6LocX + 370, Track6LocY - Active.Height / 2 + Track11Loc);

            LLed_FYPLATE_TOP.Location = new System.Drawing.Point(Track6LocX - 10, Track6LocY - (LLed_FYPLATE_TOP.Height / 2) + FYPlateLoc);// - 16);                    
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: automaticModeToolStripMenuItem_Click
         *               
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
        private void automaticModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            automaticModeToolStripMenuItem.Checked = true;
            manualModeToolStripMenuItem.Checked = false;
            //AutomaticMode.Location = new System.Drawing.Point(625, 776);
            AutomaticMode.Visible = true;
            ManualMode.Visible = false;

            Btn_Bezet5BOn_TOP.Visible = false;
            Btn_Bezet6On_TOP.Visible = false;
            Btn_Bezet7On_TOP.Visible = false;
            Btn_Collect_TOP.Visible = true;

            PB_Block5BIn.Enabled = false;
            PB_Block6In.Enabled = false;
            PB_Block7In.Enabled = false;

        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: manualModeToolStripMenuItem_Click
         *               
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
        private void manualModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            automaticModeToolStripMenuItem.Checked = false;
            manualModeToolStripMenuItem.Checked = true;
            AutomaticMode.Visible = false;            
            ManualMode.Visible = true;                       

            Btn_Bezet5BOn_TOP.Location = new System.Drawing.Point(GWin.Location.X + 50 + 16 + 30, (740 / 2 + GWin.Location.Y) - 18 - (250 / 2) - (23 / 2));
            Btn_Bezet6On_TOP.Location = new System.Drawing.Point(GWin.Location.X + 100 - (32 / 2), (740 / 2 + GWin.Location.Y) + 8 + 40);
            Btn_Bezet7On_TOP.Location = new System.Drawing.Point(GWin.Location.X + 840 - (32 / 2), (740 / 2 + GWin.Location.Y) + 8 + 40);
            //ManualMode.Location = new System.Drawing.Point(GWin.Location.X + 745, GWin.Location.Y + 761);  //(750,793);
            
            Btn_Bezet5BOn_TOP.Visible = false;
            Btn_Bezet6On_TOP.Visible = false;
            Btn_Bezet7On_TOP.Visible = false;
            Btn_Collect_TOP.Visible = false;

            PB_Block5BIn.Enabled = true;
            PB_Block6In.Enabled = true;
            PB_Block7In.Enabled = true;

        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: quitToolStripMenuItem_Click
         *               
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
        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: alwaysOnTopToolStripMenuItem_Click
         *               
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
        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            alwaysOnTopToolStripMenuItem.Checked = !alwaysOnTopToolStripMenuItem.Checked;
            this.TopMost = alwaysOnTopToolStripMenuItem.Checked;                    
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: LLed_Trackx_Click
         *               
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
        private void LLed_Track1_Click(object sender, EventArgs e)
        {

        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: PB_Block5BIn_Click
         *               
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
        private void PB_Block5BIn_Click(object sender, EventArgs e)
        {
            if (Btn_Bezet5BOn_TOP_Click_Toggle == true)
            {
                Btn_Bezet5BOn_TOP_Click_Toggle = false;
                m_FYAppVar.CmdOcc5BOnTrue.UpdateCommand();                
            }
            else if (Btn_Bezet5BOn_TOP_Click_Toggle == false)
            {
                Btn_Bezet5BOn_TOP_Click_Toggle = true;
                m_FYAppVar.CmdOcc5BOnFalse.UpdateCommand();                
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: PB_Block6In_Click
         *               
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
        private void PB_Block6In_Click(object sender, EventArgs e)
        {
            if (Btn_Bezet6On_TOP_Click_Toggle == true)
            {
                Btn_Bezet6On_TOP_Click_Toggle = false;
                m_FYAppVar.CmdOcc6OnTrue.UpdateCommand();                
            }
            else if (Btn_Bezet6On_TOP_Click_Toggle == false)
            {
                Btn_Bezet6On_TOP_Click_Toggle = true;
                m_FYAppVar.CmdOcc6OnFalse.UpdateCommand();                
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: PB_Block7In_Click
         *               
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
        private void PB_Block7In_Click(object sender, EventArgs e)
        {
            if (Btn_Bezet7On_TOP_Click_Toggle == true)
            {
                Btn_Bezet7On_TOP_Click_Toggle = false;
                m_FYAppVar.CmdOcc7OnTrue.UpdateCommand();                
            }
            else if (Btn_Bezet7On_TOP_Click_Toggle == false)
            {
                Btn_Bezet7On_TOP_Click_Toggle = true;
                m_FYAppVar.CmdOcc7OnFalse.UpdateCommand();                
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: checkBoxTrack<x>_CheckedChanged
         *               
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
        private void checkBoxTrack1_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(1, checkBoxTrack1.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack1.Checked == true)
            {
                PB_Tr1_Active.Hide();
            }
            else { PB_Tr1_Active.Show(); }
        }

        private void checkBoxTrack2_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(2, checkBoxTrack2.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack2.Checked == true)
            {
                PB_Tr2_Active.Hide();
            }
            else { PB_Tr2_Active.Show(); }
        }

        private void checkBoxTrack3_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(3, checkBoxTrack3.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack3.Checked == true)
            {
                PB_Tr3_Active.Hide();
            }
            else { PB_Tr3_Active.Show(); }
        }

        private void checkBoxTrack4_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(4, checkBoxTrack4.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack4.Checked == true)
            {
                PB_Tr4_Active.Hide();
            }
            else { PB_Tr4_Active.Show(); }
        }

        private void checkBoxTrack5_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(5, checkBoxTrack5.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack5.Checked == true)
            {
                PB_Tr5_Active.Hide();
            }
            else { PB_Tr5_Active.Show(); }
        }

        private void checkBoxTrack6_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(6, checkBoxTrack6.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack6.Checked == true)
            {
                PB_Tr6_Active.Hide();
            }
            else { PB_Tr6_Active.Show(); }
        }

        private void checkBoxTrack7_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(7, checkBoxTrack7.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack7.Checked == true)
            {
                PB_Tr7_Active.Hide();
            }
            else { PB_Tr7_Active.Show(); }
        }

        private void checkBoxTrack8_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(8, checkBoxTrack8.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack8.Checked == true)
            {
                PB_Tr8_Active.Hide();
            }
            else { PB_Tr8_Active.Show(); }
        }

        private void checkBoxTrack9_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(9, checkBoxTrack9.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack9.Checked == true)
            {
                PB_Tr9_Active.Hide();
            }
            else { PB_Tr9_Active.Show(); }
        }

        private void checkBoxTrack10_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(10, checkBoxTrack10.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack10.Checked == true)
            {
                PB_Tr10_Active.Hide();
            }
            else { PB_Tr10_Active.Show(); }
        }

        private void checkBoxTrack11_CheckedChanged(object sender, EventArgs e)
        {
            SetcheckBoxTrackIntArray(11, checkBoxTrack11.Checked);
            m_FYAppVar.TrackTrainsOnFYUpdater();
            if (checkBoxTrack11.Checked == true)
            {
                PB_Tr11_Active.Hide();
            }
            else { PB_Tr11_Active.Show(); }
        }

        private void checkBoxToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxToggle.Checked == false)
            {
                checkBoxTrack1.Checked = false;                
                checkBoxTrack2.Checked = false;                
                checkBoxTrack3.Checked = false;
                checkBoxTrack4.Checked = false;
                checkBoxTrack5.Checked = false;
                checkBoxTrack6.Checked = false;
                checkBoxTrack7.Checked = false;
                checkBoxTrack8.Checked = false;
                checkBoxTrack9.Checked = false;
                checkBoxTrack10.Checked = false;
                checkBoxTrack11.Checked = false;
                PB_Tr1_Active.Show();
                PB_Tr2_Active.Show();
                PB_Tr3_Active.Show();
                PB_Tr4_Active.Show();
                PB_Tr5_Active.Show();
                PB_Tr6_Active.Show();
                PB_Tr7_Active.Show();
                PB_Tr8_Active.Show();
                PB_Tr9_Active.Show();
                PB_Tr10_Active.Show();
                PB_Tr11_Active.Show();

            }
            else
            {
                checkBoxTrack1.Checked = true;                
                checkBoxTrack2.Checked = true;                
                checkBoxTrack3.Checked = true;
                checkBoxTrack4.Checked = true;
                checkBoxTrack5.Checked = true;
                checkBoxTrack6.Checked = true;
                checkBoxTrack7.Checked = true;
                checkBoxTrack8.Checked = true;
                checkBoxTrack9.Checked = true;
                checkBoxTrack10.Checked = true;
                checkBoxTrack11.Checked = true;
                PB_Tr1_Active.Hide();
                PB_Tr2_Active.Hide();
                PB_Tr3_Active.Hide();
                PB_Tr4_Active.Hide();
                PB_Tr5_Active.Hide();
                PB_Tr6_Active.Hide();
                PB_Tr7_Active.Hide();
                PB_Tr8_Active.Hide();
                PB_Tr9_Active.Hide();
                PB_Tr10_Active.Hide();
                PB_Tr11_Active.Hide();

            }
            m_FYAppVar.TrackTrainsOnFYUpdater();
        }

        private void ForceNextTrack_ValueChanged(object sender, EventArgs e)
        {
            m_FYAppVar.iTrainDriveOutPointer = Convert.ToInt16(ForceNextTrack.Value);
        }

        public void SetcheckBoxTrackIntArray(int Track, bool Checked)
        {
            if (Checked == true)
            {
                m_FYAppVar.icheckBoxTrack[Track] = 0;                
            }
            else
            {
                m_FYAppVar.icheckBoxTrack[Track] = 1;                       
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }
    }    
}
