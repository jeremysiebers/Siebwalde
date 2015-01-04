using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;


namespace Siebwalde_Application
{
    public delegate void SetTextCallbackTOP(string text, int Layer, int Indicator, int Val);  // defines a delegate type TOP
    
    public partial class FiddleYardFormTop : Form
    {
        public const int TOP = 1;

        public bool[] TrackStatusLight = new bool[12]
        {
            false, false, false, false, false, false, false, false, false, false, false, false};

        public bool Initialized = false;
        public bool Btn_Bezet5BOn_TOP_Click_Toggle = false;
        public bool Btn_Bezet6On_TOP_Click_Toggle = false;
        public bool Btn_Bezet7On_TOP_Click_Toggle = false;
        
        public FiddleYardFormTop()
        {
            InitializeComponent();
        }

        public void ClearReceivedCmdTOP()
        {
            ReceivedCmdTOP.Clear();
        }

        public void FYTOPShow()
        {
            this.Opacity = 100;
            this.ShowInTaskbar = true;
            this.Show();
        }

        public void SetText_ReceivedCmdTOP(string text, int Layer, int Indicator, int Val)
        {
            if (Layer == TOP)
            {

                if (ReceivedCmdTOP.InvokeRequired)
                {
                    SetTextCallbackTOP d = new SetTextCallbackTOP(SetText_ReceivedCmdTOP);
                    ReceivedCmdTOP.Invoke(d, new object[] { text, Layer, Indicator, Val });  // invoking itself
                }
                else
                {
                    if (text != "")
                    {
                        ReceivedCmdTOP.AppendText(text);      // the "functional part", executing only on the main thread
                        byte[] info = new UTF8Encoding(true).GetBytes(text);
                        Sender.StoreText(text, "TOP ");
                    }
                    SetLedIndicator(Indicator, Val);
                }
            }
        }

        string[] CmdTOP = new string[27] {
            "a" + "1" + "\r",               //Track On
            "a" + "2" + "\r",               //Track Off
            "a" + "3" + "\r",               //Fiddle Track to the left track ++
            "a" + "4" + "\r",               //Fiddle Track to the right track --
            "a" + "5" + "\r",               //To track 1
            "a" + "6" + "\r",               //To track 2
            "a" + "7" + "\r",               //To track 3
            "a" + "8" + "\r",               //To track 4
            "a" + "9" + "\r",               //To track 5
            "a" + "A" + "\r",               //To track 6
            "a" + "B" + "\r",               //To track 7
            "a" + "C" + "\r",               //To track 8
            "a" + "D" + "\r",               //To track 9
            "a" + "E" + "\r",               //To track 10
            "a" + "F" + "\r",               //To track 11
            "a" + "G" + "\r",               //Train Detection
            "a" + "H" + "\r",               //Start Fiddle Yard
            "a" + "I" + "\r",               //Stop Fiddle Yard
            "a" + "J" + "\r",               //Stop Fiddle Yard Now (Reset)
            "a" + "K" + "\r",               //Bezet_In_5B_Switch_On
            "a" + "L" + "\r",               //Bezet_In_5B_Switch_Off
            "a" + "M" + "\r",               //Bezet_In_6_Switch_On
            "a" + "N" + "\r",               //Bezet_In_6_Switch_Off
            "a" + "O" + "\r",               //Bezet_In_7_Switch_On
            "a" + "P" + "\r",               //Bezet_In_7_Switch_Off
            "a" + "Q" + "\r",               //Resume previous operation
            "a" + "R" + "\r"                //Start Collect
        };


        private void TOP_Button_Cmd_Sender(int i)
        {
            byte[] ByteToSend = Encoding.ASCII.GetBytes(CmdTOP[i]);
            Sender.SendUdp(ByteToSend);
            
        }

        private void Btn_Bridge_Open_TOP_Click_1(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(0);
        }

        private void Btn_Bridge_Close_TOP_Click_1(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(1);
        }

        private void Btn_Track_Plus_TOP_Click_1(object sender, EventArgs e)
        {
            if (Track_No_TOP.Text != "11")
            {
                TOP_Button_Cmd_Sender(2);
            }
        }

        private void Btn_Track_Min_TOP_Click_1(object sender, EventArgs e)
        {
            if (Track_No_TOP.Text != "1")
            {
                TOP_Button_Cmd_Sender(3);
            }            
        }

        private void Nuo_Track_No_TOP_ValueChanged_1(object sender, EventArgs e)
        {
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
            int i = Convert.ToInt16(Nuo_Track_No_TOP.Value + 3);
            TOP_Button_Cmd_Sender(i);
        }

        private void Btn_Train_Detect_TOP_Click_1(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(15);
        }

        private void Btn_Start_Fiddle_TOP_Click_1(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(16);
            Btn_Collect_TOP.Enabled = false;
        }

        private void Btn_Stop_Fiddle_TOP_Click_1(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(17);
            Btn_Collect_TOP.Enabled = true;
        }

        private void Btn_Reset_TOP_Click_1(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(18);
            Initialized = false;

            // Next also force all track color to cyan including text becasue if a track is already false no update is executed on each track color.
            LLed_Track1_TOP.BackColor = Color.Cyan;
            LLed_Track1_TOP.Text = "                     Not Initialized";
            LLed_Track2_TOP.BackColor = Color.Cyan;
            LLed_Track2_TOP.Text = "                     Not Initialized";
            LLed_Track3_TOP.BackColor = Color.Cyan;
            LLed_Track3_TOP.Text = "                     Not Initialized";
            LLed_Track4_TOP.BackColor = Color.Cyan;
            LLed_Track4_TOP.Text = "                     Not Initialized";
            LLed_Track5_TOP.BackColor = Color.Cyan;
            LLed_Track5_TOP.Text = "                     Not Initialized";
            LLed_Track6_TOP.BackColor = Color.Cyan;
            LLed_Track6_TOP.Text = "                     Not Initialized";
            LLed_Track7_TOP.BackColor = Color.Cyan;
            LLed_Track7_TOP.Text = "                     Not Initialized";
            LLed_Track8_TOP.BackColor = Color.Cyan;
            LLed_Track8_TOP.Text = "                     Not Initialized";
            LLed_Track9_TOP.BackColor = Color.Cyan;
            LLed_Track9_TOP.Text = "                     Not Initialized";
            LLed_Track10_TOP.BackColor = Color.Cyan;
            LLed_Track10_TOP.Text = "                     Not Initialized";
            LLed_Track11_TOP.BackColor = Color.Cyan;
            LLed_Track11_TOP.Text = "                     Not Initialized";

            Btn_Collect_TOP.Enabled = true;
        }

        private void Btn_Bezet5BOn_TOP_Click_1(object sender, EventArgs e)
        {
            if (Btn_Bezet5BOn_TOP_Click_Toggle == true)
            {
                Btn_Bezet5BOn_TOP_Click_Toggle = false;
                TOP_Button_Cmd_Sender(19);
                Btn_Bezet5BOn_TOP.Text = "Off";
            }
            else if (Btn_Bezet5BOn_TOP_Click_Toggle == false)
            {
                Btn_Bezet5BOn_TOP_Click_Toggle = true;
                TOP_Button_Cmd_Sender(20);
                Btn_Bezet5BOn_TOP.Text = "On";
            }
        }

        private void Btn_Bezet6On_TOP_Click_1(object sender, EventArgs e)
        {
            if (Btn_Bezet6On_TOP_Click_Toggle == true)
            {
                Btn_Bezet6On_TOP_Click_Toggle = false;
                TOP_Button_Cmd_Sender(21);
                Btn_Bezet6On_TOP.Text = "Off";
            }
            else if (Btn_Bezet6On_TOP_Click_Toggle == false)
            {
                Btn_Bezet6On_TOP_Click_Toggle = true;
                TOP_Button_Cmd_Sender(22);
                Btn_Bezet6On_TOP.Text = "On";
            }
        }

        private void Btn_Bezet7On_TOP_Click_1(object sender, EventArgs e)
        {
            if (Btn_Bezet7On_TOP_Click_Toggle == true)
            {
                Btn_Bezet7On_TOP_Click_Toggle = false;
                TOP_Button_Cmd_Sender(23);
                Btn_Bezet7On_TOP.Text = "Off";
            }
            else if (Btn_Bezet7On_TOP_Click_Toggle == false)
            {
                Btn_Bezet7On_TOP_Click_Toggle = true;
                TOP_Button_Cmd_Sender(24);
                Btn_Bezet7On_TOP.Text = "On";
            }
        }

        private void Btn_Recovered_TOP_Click_1(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(25);
        }

        private void Btn_Collect_TOP_Click_1(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(26);
        }

        public void SetLedIndicator(int Indicator, int Val)
        {
            switch (Indicator)
                {
                    case 1: if (Val >= 1)
                        {
                            Led_Heart_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Heart_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 2:
                        break;

                    case 3: if (Val >= 1)
                        {
                            LLed_F11_TOP.BackColor = Color.Yellow;
                        }
                        if (Val == 0)
                        {
                            if (Initialized == false)
                            {
                                LLed_F11_TOP.BackColor = Color.Cyan;
                            }                            
                            else
                            {
                                LLed_F11_TOP.BackColor = Color.Transparent; // change color status else sensor keeps being red. Run before UpdateTrackIndicatorColor()
                                CheckWhichTrackInline();                             
                            }
                        }
                        break;

                    case 4: if (Val >= 1)
                        {
                            Led_EOS10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_EOS10_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 5: if (Val >= 1)
                        {
                            Led_EOS11_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_EOS11_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 6:
                        break;

                    case 7: if (Val >= 1)
                        {
                            LLed_F13_TOP.BackColor = Color.Red;
                            LLed_F13_TOP.ForeColor = Color.Yellow;
                        }
                        if (Val == 0)
                        {
                            if (Initialized == false)
                            {
                                LLed_F13_TOP.ForeColor = Color.Black;
                                LLed_F13_TOP.BackColor = Color.Cyan;
                            }                            
                            else
                            {
                                LLed_F13_TOP.ForeColor = Color.Black;
                                LLed_F13_TOP.BackColor = Color.Transparent; // change color status else sensor keeps being red. Run before UpdateTrackIndicatorColor()
                                CheckWhichTrackInline();                                
                            }
                        }
                        break;

                    case 8: if (Val >= 1)
                        {
                            LLed_F12_TOP.BackColor = Color.Red;
                            LLed_F12_TOP.ForeColor = Color.Yellow;
                        }
                        if (Val == 0)
                        {
                            if (Initialized == false)
                            {
                                LLed_F12_TOP.BackColor = Color.Cyan;
                                LLed_F12_TOP.ForeColor = Color.Black;
                            }                            
                            else
                            {
                                LLed_F12_TOP.ForeColor = Color.Black;
                                LLed_F12_TOP.BackColor = Color.Transparent; // change color status else sensor keeps being red. Run before UpdateTrackIndicatorColor()
                                CheckWhichTrackInline();
                            }
                        }
                        break;

                    case 9: if (Val >= 1)
                        {
                            LLed_Block5B_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            LLed_Block5B_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 10: if (Val >= 1)
                        {
                            LLed_Block8A_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            LLed_Block8A_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 11: if (Val >= 1)
                        {
                            LLed_TrackPower_TOP.BackColor = Color.Lime;
                            LLed_TrackPower_TOP.Text = "Coupled";
                            LLed_TrackPower_TOP.ForeColor = Color.Black;
                        }
                        if (Val == 0)
                        {
                            LLed_TrackPower_TOP.BackColor = Color.Red;
                            LLed_TrackPower_TOP.Text = "Uncoupled";
                            LLed_TrackPower_TOP.ForeColor = Color.Yellow;
                        }
                        break;

                    case 12: if (Val >= 1)
                        {
                            LLed_Block5BIn_TOP.BackColor = Color.Red;
                        }
                        if (Val == 0)
                        {
                            LLed_Block5BIn_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 13: if (Val >= 1)
                        {
                            LLed_Block6In_TOP.BackColor = Color.Red;
                        }
                        if (Val == 0)
                        {
                            LLed_Block6In_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 14: if (Val >= 1)
                        {
                            LLed_Block7In_TOP.BackColor = Color.Red;
                        }
                        if (Val == 0)
                        {
                            if (LLed_Block7_TOP.BackColor == Color.Lime)
                            {
                                LLed_Block7In_TOP.BackColor = Color.Lime;
                            }
                            else
                            {
                                LLed_Block7In_TOP.BackColor = Color.Transparent;
                            }
                        }
                        break;

                    case 15: if (Val >= 1)
                        {
                            Led_Resistor_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Resistor_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 16: if (Val >= 1 && TrackStatusLight[1] == true)
                        {
                            LLed_Track1_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track1_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track1_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track1_TOP.BackColor = Color.Cyan;             // After processor update from true to false set to cyan if initialized is false.
                            LLed_Track1_TOP.Text = "                     Not Initialized";  
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 17: if (Val >= 1 && TrackStatusLight[2] == true)
                        {
                            LLed_Track2_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track2_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track2_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track2_TOP.BackColor = Color.Cyan;
                            LLed_Track2_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 18: if (Val >= 1 && TrackStatusLight[3] == true)
                        {
                            LLed_Track3_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track3_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track3_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track3_TOP.BackColor = Color.Cyan;
                            LLed_Track3_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 19: if (Val >= 1 && TrackStatusLight[4] == true)
                        {
                            LLed_Track4_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track4_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track4_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track4_TOP.BackColor = Color.Cyan;
                            LLed_Track4_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 20: if (Val >= 1 && TrackStatusLight[5] == true)
                        {
                            LLed_Track5_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track5_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track5_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track5_TOP.BackColor = Color.Cyan;
                            LLed_Track5_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 21: if (Val >= 1 && TrackStatusLight[6] == true)
                        {
                            LLed_Track6_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track6_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track6_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track6_TOP.BackColor = Color.Cyan;
                            LLed_Track6_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 22: if (Val >= 1 && TrackStatusLight[7] == true)
                        {
                            LLed_Track7_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track7_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track7_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track7_TOP.BackColor = Color.Cyan;
                            LLed_Track7_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 23: if (Val >= 1 && TrackStatusLight[8] == true)
                        {
                            LLed_Track8_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track8_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track8_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track8_TOP.BackColor = Color.Cyan;
                            LLed_Track8_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 24: if (Val >= 1 && TrackStatusLight[9] == true)
                        {
                            LLed_Track9_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track9_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track9_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track9_TOP.BackColor = Color.Cyan;
                            LLed_Track9_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 25: if (Val >= 1 && TrackStatusLight[10] == true)
                        {
                            LLed_Track10_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track10_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track10_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track10_TOP.BackColor = Color.Cyan;
                            LLed_Track10_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 26: if (Val >= 1 && TrackStatusLight[11] == true)
                        {
                            LLed_Track11_TOP.BackColor = Color.Lime;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Val >= 1)
                        {
                            LLed_Track11_TOP.BackColor = Color.Green;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        if (Val == 0 && Initialized == true)
                        {
                            LLed_Track11_TOP.BackColor = Color.Transparent;
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        else if (Initialized == false)
                        {
                            LLed_Track11_TOP.BackColor = Color.Cyan;
                            LLed_Track11_TOP.Text = "                     Not Initialized";
                            CheckWhichTrackInline();                            // Sensor background color update
                        }
                        break;

                    case 27: if (Val >= 1)
                        {
                            LLed_Block6_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            LLed_Block6_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 28: if (Val >= 1)
                        {
                            LLed_Block7_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            LLed_Block7_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 29: if (Val >= 1)
                        {
                            Led_TrackPower.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_TrackPower.BackColor = Color.Transparent;
                        }
                        break;

                    case 30: if (Val >= 1)
                        {
                            LLed_F10_TOP.BackColor = Color.Yellow;
                            LLed_F10_2_TOP.BackColor = Color.Yellow;
                        }
                        if (Val == 0)
                        {
                            LLed_F10_TOP.BackColor = Color.Transparent;
                            LLed_F10_2_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 31: if (Val >= 1)
                        {
                            Led_M10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_M10_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 32: Track_No_TOP.Text = Convert.ToString(Val);
                        
                        for (int i = 0; i < 12; i++)
                        {
                            TrackStatusLight[i] = false;
                        }
                        TrackStatusLight[Val] = true;
                        ShiftIndicatorPos(Val);
                        if (Initialized == true)
                        {
                            UpdateTrackIndicatorColor();                    // After/during shift update color of tracks accordingly
                        }
                        break;

                    case 57:                                                // Traindetection (because message train detection comes first and "trains on fiddle yard trackX true/false
                        LLed_Track1_TOP.BackColor = Color.Transparent;      // comes later, all tracks get the correct color.
                        LLed_Track1_TOP.Text = "             1";
                        LLed_Track2_TOP.BackColor = Color.Transparent;
                        LLed_Track2_TOP.Text = "             2";
                        LLed_Track3_TOP.BackColor = Color.Transparent;
                        LLed_Track3_TOP.Text = "             3";
                        LLed_Track4_TOP.BackColor = Color.Transparent;
                        LLed_Track4_TOP.Text = "             4";
                        LLed_Track5_TOP.BackColor = Color.Transparent;
                        LLed_Track5_TOP.Text = "             5";
                        LLed_Track6_TOP.BackColor = Color.Transparent;
                        LLed_Track6_TOP.Text = "             6";
                        LLed_Track7_TOP.BackColor = Color.Transparent;
                        LLed_Track7_TOP.Text = "             7";
                        LLed_Track8_TOP.BackColor = Color.Transparent;
                        LLed_Track8_TOP.Text = "             8";
                        LLed_Track9_TOP.BackColor = Color.Transparent;
                        LLed_Track9_TOP.Text = "             9";
                        LLed_Track10_TOP.BackColor = Color.Transparent;
                        LLed_Track10_TOP.Text = "            10";
                        LLed_Track11_TOP.BackColor = Color.Transparent;
                        LLed_Track11_TOP.Text = "            11";
                        Initialized = true;                        
                        break;

                    default: break;
                }
            

        }

        private void UpdateTrackIndicatorColor()
        {
            if (TrackStatusLight[0] == true)            // in between tracks every occupied track becomes green
            {
                if (LLed_Track1_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track1_TOP.BackColor = Color.Green;
                }
                if (LLed_Track2_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track2_TOP.BackColor = Color.Green;
                }
                if (LLed_Track3_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track3_TOP.BackColor = Color.Green;
                }
                if (LLed_Track4_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track4_TOP.BackColor = Color.Green;
                }
                if (LLed_Track5_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track5_TOP.BackColor = Color.Green;
                }
                if (LLed_Track6_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track6_TOP.BackColor = Color.Green;
                }
                if (LLed_Track7_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track7_TOP.BackColor = Color.Green;
                }
                if (LLed_Track8_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track8_TOP.BackColor = Color.Green;
                }
                if (LLed_Track9_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track9_TOP.BackColor = Color.Green;
                }
                if (LLed_Track10_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track10_TOP.BackColor = Color.Green;
                }
                if (LLed_Track11_TOP.BackColor != Color.Transparent)
                {
                    LLed_Track11_TOP.BackColor = Color.Green;
                }

                CheckWhichTrackInline();

            }

            if (TrackStatusLight[1] == true && LLed_Track1_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track1_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track1_TOP.BackColor != Color.Transparent)
            {
                LLed_Track1_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[2] == true && LLed_Track2_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track2_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track2_TOP.BackColor != Color.Transparent)
            {
                LLed_Track2_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[3] == true && LLed_Track3_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track3_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track3_TOP.BackColor != Color.Transparent)
            {
                LLed_Track3_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[4] == true && LLed_Track4_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track4_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track4_TOP.BackColor != Color.Transparent)
            {
                LLed_Track4_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[5] == true && LLed_Track5_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track5_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track5_TOP.BackColor != Color.Transparent)
            {
                LLed_Track5_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[6] == true && LLed_Track6_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track6_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track6_TOP.BackColor != Color.Transparent)
            {
                LLed_Track6_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[7] == true && LLed_Track7_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track7_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track7_TOP.BackColor != Color.Transparent)
            {
                LLed_Track7_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[8] == true && LLed_Track8_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track8_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track8_TOP.BackColor != Color.Transparent)
            {
                LLed_Track8_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[9] == true && LLed_Track9_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track9_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track9_TOP.BackColor != Color.Transparent)
            {
                LLed_Track9_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[10] == true && LLed_Track10_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track10_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track10_TOP.BackColor != Color.Transparent)
            {
                LLed_Track10_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }

            if (TrackStatusLight[11] == true && LLed_Track11_TOP.BackColor != Color.Transparent) // track selected? when occupied color becomes Lime otherwise green
            {
                LLed_Track11_TOP.BackColor = Color.Lime;
                CheckWhichTrackInline();
            }
            else if (LLed_Track11_TOP.BackColor != Color.Transparent)
            {
                LLed_Track11_TOP.BackColor = Color.Green;
                CheckWhichTrackInline();
            }
            else { CheckWhichTrackInline(); }
        }

        private void CheckWhichTrackInline()
        {
            if (LLed_Track1_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                               // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(1);
            }

            else if (LLed_Track2_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(2);
            }

            else if (LLed_Track3_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(3);
            }

            else if (LLed_Track4_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(4);
            }

            else if (LLed_Track5_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(5);
            }

            else if (LLed_Track6_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(6);
            }

            else if (LLed_Track7_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(7);
            }

            else if (LLed_Track8_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(8);
            }

            else if (LLed_Track9_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(9);
            }

            else if (LLed_Track10_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(10);
            }

            else if (LLed_Track11_TOP.Location.Y == LLed_Block6_TOP.Location.Y)   // When shifting the color of F11/12/13 must change accordingly to the track positioned
            {                                                                    // in line. Except when the sensor is "high" (yellow/red).
                SensorBackcolorUpdate(11);
            }
        }

        private void SensorBackcolorUpdate(int track)
        {
            switch (track)
            {
                case 1 : 
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track1_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track1_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track1_TOP.BackColor;
                    }
                    break;

                case 2:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track2_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track2_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track2_TOP.BackColor;
                    }
                    break;

                case 3:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track3_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track3_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track3_TOP.BackColor;
                    }
                    break;

                case 4:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track4_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track4_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track4_TOP.BackColor;
                    }
                    break;

                case 5:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track5_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track5_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track5_TOP.BackColor;
                    }
                    break;

                case 6:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track6_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track6_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track6_TOP.BackColor;
                    }
                    break;

                case 7:
                     if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track7_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track7_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track7_TOP.BackColor;
                    }
                    break;

                case 8:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track8_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track8_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track8_TOP.BackColor;
                    }
                    break;

                case 9:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track9_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track9_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track9_TOP.BackColor;
                    }
                    break;

                case 10:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track10_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track10_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track10_TOP.BackColor;
                    }
                    break;

                case 11:
                    if (LLed_F11_TOP.BackColor != Color.Yellow)
                    {
                        LLed_F11_TOP.BackColor = LLed_Track11_TOP.BackColor;
                    }
                    if (LLed_F12_TOP.BackColor != Color.Red)
                    {
                        LLed_F12_TOP.BackColor = LLed_Track11_TOP.BackColor;
                    }
                    if (LLed_F13_TOP.BackColor != Color.Red)
                    {
                        LLed_F13_TOP.BackColor = LLed_Track11_TOP.BackColor;
                    }
                    break;

                default: break;
            }
        }

        private void ShiftIndicatorPos(int val)
        {
            switch (val)
            {
                case 1: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 90);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 120);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 150);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 180);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 210);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 240);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 270);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 300);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 15);                    
                    break;

                case 2: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 90);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 120);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 150);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 180);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 210);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 240);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 270);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 45);
                    break;

                case 3: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 90);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 120);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 150);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 180);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 210);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 240);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 75);
                    break;

                case 4: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 90);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 90);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 120);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 150);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 180);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 210);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 105);
                    break;

                case 5: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 120);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 90);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 90);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 120);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 150);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 180);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 135);
                    break;

                case 6: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 150);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 120);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 90);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 90);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 120);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 150);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 165);
                    break;

                case 7: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 180);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 150);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 120);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 90);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 90);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 120);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 195);
                    break;

                case 8: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 210);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 180);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 150);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 120);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 90);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 90);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 225);
                    break;

                case 9: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 240);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 210);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 180);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 150);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 120);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 90);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 60);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 255);
                    break;

                case 10: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 270);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 240);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 210);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 180);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 150);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 120);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 90);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y + 30);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 285);
                    break;

                case 11: LLed_Track1_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 300);
                    LLed_Track2_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 270);
                    LLed_Track3_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 240);
                    LLed_Track4_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 210);
                    LLed_Track5_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 180);
                    LLed_Track6_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 150);
                    LLed_Track7_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 120);
                    LLed_Track8_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 90);
                    LLed_Track9_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 60);
                    LLed_Track10_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y - 30);
                    LLed_Track11_TOP.Location = new System.Drawing.Point(168, LLed_Block6_TOP.Location.Y);
                    LLed_FYPLATE_TOP.Location = new System.Drawing.Point(159, LLed_Block6_TOP.Location.Y - 315);
                    break;

                default: break;
            }
        }        

        private void automaticModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            automaticModeToolStripMenuItem.Checked = true;
            manualModeToolStripMenuItem.Checked = false;
            AutomaticMode.Visible = true;
            ManualMode.Visible = false;

            Btn_Bezet5BOn_TOP.Visible = false;
            Btn_Bezet6On_TOP.Visible = false;
            Btn_Bezet7On_TOP.Visible = false;
            Btn_Collect_TOP.Visible = true;

        }

        private void manualModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            automaticModeToolStripMenuItem.Checked = false;
            manualModeToolStripMenuItem.Checked = true;
            AutomaticMode.Visible = false;
            ManualMode.Visible = true;

            Btn_Bezet5BOn_TOP.Visible = true;
            Btn_Bezet6On_TOP.Visible = true;
            Btn_Bezet7On_TOP.Visible = true;
            Btn_Collect_TOP.Visible = false;

        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

    }
}
