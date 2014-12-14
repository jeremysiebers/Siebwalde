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

namespace Siebwalde_Application
{

    public delegate void SetTextCallback(string text, int Layer, int Indicator, int Val);  // defines a delegate type

    public partial class Main : Form
    {
        public Controller _controller1;

        public UdpClient sendingUdpClient = new UdpClient(28671);

        public string path = @"c:\localdata\Logging.txt";
                
        #region Constructor

        private const int TOP = 1;
        private const int BOTTOM = 0;

        public Main()
        {
            try
            {
                InitializeComponent();
                
                int poort1 = 28672;
                _controller1 = new Controller(poort1, SetText_ReceivedCmd);
                _controller1.Start();

                try
                {
                    //Creates a UdpClient for sending data.
                    sendingUdpClient.Connect("FIDDLEYARD", 28671);
                }
                catch
                {
                    //Fiddle Yard not found, swiching to localhost...
                    sendingUdpClient.Connect("LocalHost", 28671);
                }
                
            }
            catch (Exception ex)
            {
                //logger.error(.../.)
                MessageBox.Show(ex.Message);
                
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox about_box = new AboutBox();
            about_box.Show();
        }

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            sendingUdpClient.Close();
            Application.Exit();
        }

        

        #endregion Constructor        

        #region Received Data to Event Logger
        private void SetText_ReceivedCmd(string text, int Layer, int Indicator, int Val)
        {
            if (Layer == TOP)
            {

                if (ReceivedCmdTOP.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText_ReceivedCmd);
                    ReceivedCmdTOP.Invoke(d, new object[] { text, Layer, Indicator, Val });  // invoking itself
                }
                else
                {
                    ReceivedCmdTOP.AppendText(text);      // the "functional part", executing only on the main thread
                    byte[] info = new UTF8Encoding(true).GetBytes(text);
                    StoreText(text, "TOP ");
                    SetLedIndicator(Layer, Indicator, Val);
                }
            }

            if (Layer == BOTTOM)
            {

                if (ReceivedCmdBOTTOM.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText_ReceivedCmd);
                    ReceivedCmdBOTTOM.Invoke(d, new object[] { text, Layer, Indicator, Val });  // invoking itself
                }
                else
                {
                    ReceivedCmdBOTTOM.AppendText(text);      // the "functional part", executing only on the main thread
                    byte[] info = new UTF8Encoding(true).GetBytes(text);
                    StoreText(text, "BOT ");
                    SetLedIndicator(Layer, Indicator, Val);
                }
            }            
        }

        private void StoreText(string text, string Layer)
        {
            using (var fs = new FileStream(path, FileMode.Append))
            {
                Byte[] info =
                    new UTF8Encoding(true).GetBytes(Layer + text);
                fs.Write(info, 0, info.Length);
                fs.Close();
            }
        }

        private void SetLedIndicator(int Layer, int Indicator, int Val)
        {
            if (Layer == TOP)
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
                            Led_F11_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F11_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 4: if (Val >= 1)
                        {
                            Led_F10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F10_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 5: if (Val >= 1)
                        {
                            Led_F11_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F11_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 6: 
                        break;

                    case 7: if (Val >= 1)
                        {
                            Led_F13_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F13_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 8: if (Val >= 1)
                        {
                            Led_F12_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F12_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 9: if (Val >= 1)
                        {
                            Led_Block5B_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block5B_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 10: if (Val >= 1)
                        {
                            Led_Block8A_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block8A_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 11: //NOG MAKEN
                        break;

                    case 12: //NOG MAKEN
                        break;

                    case 13: //NOG MAKEN
                        break;

                    case 14: //NOG MAKEN
                        break;

                    case 15: //NOG MAKEN
                        break;

                    case 16: if (Val >= 1)
                        {
                            Led_Track1_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track1_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 17: if (Val >= 1)
                        {
                            Led_Track2_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track2_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 18: if (Val >= 1)
                        {
                            Led_Track3_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track3_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 19: if (Val >= 1)
                        {
                            Led_Track4_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track4_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 20: if (Val >= 1)
                        {
                            Led_Track5_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track5_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 21: if (Val >= 1)
                        {
                            Led_Track6_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track6_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 22: if (Val >= 1)
                        {
                            Led_Track7_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track7_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 23: if (Val >= 1)
                        {
                            Led_Track8_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track8_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 24: if (Val >= 1)
                        {
                            Led_Track9_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track9_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 25: if (Val >= 1)
                        {
                            Led_Track10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track10_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 26: if (Val >= 1)
                        {
                            Led_Track11_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Track11_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 27: if (Val >= 1)
                        {
                            Led_Block6_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block6_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 28: if (Val >= 1)
                        {
                            Led_Block7_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_Block7_TOP.BackColor = Color.Transparent;
                        }
                        break;

                    case 29: 
                        break;

                    case 30: if (Val >= 1)
                        {
                            Led_F10_TOP.BackColor = Color.Lime;
                        }
                        if (Val == 0)
                        {
                            Led_F10_TOP.BackColor = Color.Transparent;
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
                        break;

                    default: break;
                }

            }
        }

        #endregion Received Data to Event Logger              

        #region Form Button Handler TOP

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
            sendingUdpClient.Send(ByteToSend, ByteToSend.Length);
        }
               
        private void Btn_Bridge_Open_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(0);
        }

        private void Btn_Bridge_Close_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(1);
        }

        private void Btn_Track_Plus_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(2);
        }

        private void Btn_Track_Min_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(3);
        }

        private void Nuo_Track_No_TOP_ValueChanged(object sender, EventArgs e)
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

        private void Btn_Go_To_Track_TOP_Click(object sender, EventArgs e)
        {
            int i = Convert.ToInt16(Nuo_Track_No_TOP.Value + 3);
            TOP_Button_Cmd_Sender(i);
        }

        private void Btn_Train_Detect_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(15);
        }

        private void Btn_Start_Fiddle_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(16);
        }

        private void Btn_Stop_Fiddle_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(17);
        }

        private void Btn_Reset_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(18);
        }

        private void Btn_Bezet5BOn_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(19);
        }

        private void Btn_Bezet5BOff_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(20);
        }

        private void Btn_Bezet6On_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(21);
        }

        private void Btn_Bezet6Off_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(22);
        }

        private void Btn_Bezet7On_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(23);
        }

        private void Btn_Bezet7Off_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(24);
        }

        private void Btn_Recovered_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(25);
        }

        private void Btn_Collect_TOP_Click(object sender, EventArgs e)
        {
            TOP_Button_Cmd_Sender(26);
        }
        #endregion Form Button Handler TOP

        #region Form Button Handler BOTTOM

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
            sendingUdpClient.Send(ByteToSend, ByteToSend.Length);
        }

        private void Btn_Bridge_Open_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(0);
        }

        private void Btn_Bridge_Close_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(1);
        }

        private void Btn_Track_Plus_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(2);
        }

        private void Btn_Track_Min_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(3);
        }

        private void Nuo_Track_No_BOTTOM_ValueChanged_1(object sender, EventArgs e)
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

        private void Btn_Go_To_Track_BOTTOM_Click_1(object sender, EventArgs e)
        {
            int i = Convert.ToInt16(Nuo_Track_No_BOTTOM.Value + 3);
            BOTTOM_Button_Cmd_Sender(i);
        }

        private void Btn_Train_Detect_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(15);
        }

        private void Btn_Start_Fiddle_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(16);
        }

        private void Btn_Stop_Fiddle_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(17);
        }

        private void Btn_Reset_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(18);
        }

        private void Btn_Bezet5BOn_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(19);
        }

        private void Btn_Bezet5BOff_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(20);
        }

        private void Btn_Bezet6On_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(21);
        }

        private void Btn_Bezet6Off_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(22);
        }

        private void Btn_Bezet7On_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(23);
        }

        private void Btn_Bezet7Off_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(24);
        }

        private void Btn_Recovered_BOTTOM_Click(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(25);
        }

        private void Btn_Collect_BOTTOM_Click_1(object sender, EventArgs e)
        {
            BOTTOM_Button_Cmd_Sender(26);
        }
        #endregion Form Button Handler BOTTOM  

       
    }
}
