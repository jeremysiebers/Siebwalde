using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace Siebwalde_Application
{
    public delegate void SetTextCallbackBOT(string text, int Layer, int Indicator, int Val);  // defines a delegate type BOT

    public partial class FiddleYardFormBot : Form
    {
        public const int BOTTOM = 0;

        public FiddleYardFormBot()
        {
            InitializeComponent();
        }

        public void ClearReceivedCmdBOTTOM()
        {
            ReceivedCmdBOTTOM.Clear();
        }

        public void FYBOTShow()
        {
            this.Opacity = 100;
            this.ShowInTaskbar = true;
            this.Show();
        }

        public void SetText_ReceivedCmdBOT(string text, int Layer, int Indicator, int Val)
        {
            if (Layer == BOTTOM)
            {

                if (ReceivedCmdBOTTOM.InvokeRequired)
                {
                    SetTextCallbackBOT d = new SetTextCallbackBOT(SetText_ReceivedCmdBOT);
                    ReceivedCmdBOTTOM.Invoke(d, new object[] { text, Layer, Indicator, Val });  // invoking itself
                }
                else
                {
                    ReceivedCmdBOTTOM.AppendText(text);      // the "functional part", executing only on the main thread
                    byte[] info = new UTF8Encoding(true).GetBytes(text);
                    Sender.StoreText(text, "BOT ");
                    SetLedIndicator(Indicator, Val);                    
                }
            }
        }

        string[] CmdBOTTOM = new string[27] {
            "b" + "1" + "\r",               //Bridge Open
            "b" + "2" + "\r",               //Brdige Close
            "b" + "3" + "\r",               //Fiddle Track to the left track ++
            "b" + "4" + "\r",               //Fiddle Track to the right track --
            "b" + "5" + "\r",               //To track 1
            "b" + "6" + "\r",               //To track 2
            "b" + "7" + "\r",               //To track 3
            "b" + "8" + "\r",               //To track 4
            "b" + "9" + "\r",               //To track 5
            "b" + "A" + "\r",               //To track 6
            "b" + "B" + "\r",               //To track 7
            "b" + "C" + "\r",               //To track 8
            "b" + "D" + "\r",               //To track 9
            "b" + "E" + "\r",               //To track 10
            "b" + "F" + "\r",               //To track 11
            "b" + "G" + "\r",               //Train Detection
            "b" + "H" + "\r",               //Start Fiddle Yard
            "b" + "I" + "\r",               //SBOTTOM Fiddle Yard
            "b" + "J" + "\r",               //SBOTTOM Fiddle Yard Now (Reset)
            "b" + "K" + "\r",               //Bezet_In_5B_Switch_On
            "b" + "L" + "\r",               //Bezet_In_5B_Switch_Off
            "b" + "M" + "\r",               //Bezet_In_6_Switch_On
            "b" + "N" + "\r",               //Bezet_In_6_Switch_Off
            "b" + "O" + "\r",               //Bezet_In_7_Switch_On
            "b" + "P" + "\r",               //Bezet_In_7_Switch_Off
            "b" + "Q" + "\r",               //Resume previous operation
            "b" + "R" + "\r"                //Start Collect
        };

        private void BOTTOM_Button_Cmd_Sender(int i)
        {
            byte[] ByteToSend = Encoding.ASCII.GetBytes(CmdBOTTOM[i]);
            Sender.SendUdp(ByteToSend);
        }

        private void Btn_Bridge_Open_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(0);
        }

        private void Btn_Bridge_Close_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(1);
        }

        private void Btn_Track_Plus_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(2);
        }

        private void Btn_Track_Min_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(3);
        }

        private void Nuo_Track_No_BOTTOM_ValueChanged(object sender, EventArgs e)
        {
            if (Nuo_Track_No_BOTTOM.Value < 1)
            {
                Nuo_Track_No_BOTTOM.Value = 1;
            }
            if (Nuo_Track_No_BOTTOM.Value > 11)
            {
                Nuo_Track_No_BOTTOM.Value = 11;
            }
        }

        private void Btn_Go_To_Track_BOTTOM_Click(object sender, EventArgs e)
        {
            int i = Convert.ToInt16(Nuo_Track_No_BOTTOM.Value + 3);
            BOTTOM_Button_Cmd_Sender(i);
        }

        private void Btn_Train_Detect_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(15);
        }

        private void Btn_Start_Fiddle_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(16);
        }

        private void Btn_Stop_Fiddle_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(17);
        }

        private void Btn_Reset_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(18);
        }

        private void Btn_Bezet5BOn_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(19);
        }

        private void Btn_Bezet5BOff_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(20);
        }

        private void Btn_Bezet6On_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(21);
        }

        private void Btn_Bezet6Off_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(22);
        }

        private void Btn_Bezet7On_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(23);
        }

        private void Btn_Bezet7Off_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(24);
        }

        private void Btn_Recovered_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(25);
        }

        private void Btn_Collect_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(26);
        }

        public void SetLedIndicator(int Indicator, int Val)
        {
            switch (Indicator)
            {
                case 1: if (Val >= 1)
                    {
                        Led_Heart_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Heart_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 2:
                    break;

                case 3: if (Val >= 1)
                    {
                        Led_F21_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_F21_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 4: if (Val >= 1)
                    {
                        Led_EOS20_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_EOS20_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 5: if (Val >= 1)
                    {
                        Led_EOS21_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_EOS21_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 6:
                    break;

                case 7: if (Val >= 1)
                    {
                        Led_F23_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_F23_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 8: if (Val >= 1)
                    {
                        Led_F22_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_F22_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 9: if (Val >= 1)
                    {
                        Led_Block16B_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Block16B_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 10: if (Val >= 1)
                    {
                        Led_Block19A_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Block19A_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 11: if (Val >= 1)
                    {
                        Led_TrackPower_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_TrackPower_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 12: if (Val >= 1)
                    {
                        Led_Block16BIn_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Block16BIn_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 13: if (Val >= 1)
                    {
                        Led_Block17In_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Block17In_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 14: if (Val >= 1)
                    {
                        Led_Block18In_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Block18In_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 15: if (Val >= 1)
                    {
                        Led_Resistor_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Resistor_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 16: if (Val >= 1)
                    {
                        Led_Track1_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track1_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 17: if (Val >= 1)
                    {
                        Led_Track2_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track2_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 18: if (Val >= 1)
                    {
                        Led_Track3_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track3_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 19: if (Val >= 1)
                    {
                        Led_Track4_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track4_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 20: if (Val >= 1)
                    {
                        Led_Track5_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track5_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 21: if (Val >= 1)
                    {
                        Led_Track6_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track6_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 22: if (Val >= 1)
                    {
                        Led_Track7_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track7_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 23: if (Val >= 1)
                    {
                        Led_Track8_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track8_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 24: if (Val >= 1)
                    {
                        Led_Track9_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track9_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 25: if (Val >= 1)
                    {
                        Led_Track10_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track10_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 26: if (Val >= 1)
                    {
                        Led_Track11_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Track11_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 27: if (Val >= 1)
                    {
                        Led_Block17_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Block17_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 28: if (Val >= 1)
                    {
                        Led_Block18_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_Block18_BOT.BackColor = Color.Transparent;
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
                        Led_F20_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_F20_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 31: if (Val >= 1)
                    {
                        Led_M20_BOT.BackColor = Color.Lime;
                    }
                    if (Val == 0)
                    {
                        Led_M20_BOT.BackColor = Color.Transparent;
                    }
                    break;

                case 32: Track_No_BOT.Text = Convert.ToString(Val);
                    break;

                default: break;
            }
        }
        
        
    }
}
