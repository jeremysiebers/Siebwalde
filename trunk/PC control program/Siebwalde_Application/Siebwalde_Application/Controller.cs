using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Siebwalde_Application
{
    struct DataVariables
    {
        public int CL_10_Heart,                                         //1
                   SPARE2,                                              //2
                   F11,                                                 //3
                   EOS_10,                                              //4
                   EOS_11,                                              //5
                   SPARE3,                                              //6
                   F13,                                                 //7
                   F12,                                                 //8
                   Occ_From_5B,                                         //9
                   Occ_From_8A,                                         //10
                   Enable_Track_Status,                                 //11
                   Occ_To_5B_Status,                                    //12
                   Occ_To_6_Status,                                     //13
                   Occ_To_7_Status,                                     //14
                   Occ_Resistor_Status,                                 //15
                   Trains_On_Fiddle_Yard_Track1,                        //16
                   Trains_On_Fiddle_Yard_Track2,                        //17
                   Trains_On_Fiddle_Yard_Track3,                        //18
                   Trains_On_Fiddle_Yard_Track4,                        //19
                   Trains_On_Fiddle_Yard_Track5,                        //20
                   Trains_On_Fiddle_Yard_Track6,                        //21
                   Trains_On_Fiddle_Yard_Track7,                        //22
                   Trains_On_Fiddle_Yard_Track8,                        //23
                   Trains_On_Fiddle_Yard_Track9,                        //24
                   Trains_On_Fiddle_Yard_Track10,                       //25
                   Trains_On_Fiddle_Yard_Track11,                       //26
                   Occ_From_6,                                          //27
                   Occ_From_7,                                          //28
                   TR_MEAS,                                             //29
                   F10,                                                 //30
                   M10_Status;                                          //31

        public int Track_Nr;                                            //32
    };


    public class Controller
    {
        private Receiver _receiver;
        private int _poort;
        private SetTextCallback m_callback = null;
        private ToggleCommLinkCallback m_Toggle_Comm_Link = null;

        private const int TOP = 1;
        private const int BOTTOM = 0;

        private DataVariables[] _DataVariables = new DataVariables[2];

        #region Translator
         
        string[] Message_TOP = new string[48] {
                                "0",
                                "Bridge Open Ok",
                                "Bridge Close Ok",
                                "Fiddle One Left Ok",
                                "Fiddle One Right Ok",
                                "Fiddle Multiple Left Ok",
                                "Fiddle Multiple Right Ok",
                                "Train Detection Finished",
                                "Train Drive Out Finished",
                                "Train Drive In Finished",
                                "0",
                                "Init Done",
                                "Bridge Opening",
                                "Bridge Closing",
                                "Init Started",
                                "Train On 5B",
                                "Train Drive In Start",
                                "Train On 8A",
                                "Train Drive Out Start",
                                "Fiddle Yard Soft Start",
                                "Fiddle Yard Stopped",
                                "Fiddle Yard Reset",
                                "Bezet Uit Blok 6",
                                "Sensor F12 High",
                                "Bezet Uit Blok 6 AND Sensor F12",
                                "CL 10 Heart Sensor",
                                "Bridge Open Close Timeout Expired",
                                "Train Drive In Failed F12",
                                "BridgeMotorContact 10",
                                "Bridge 10L Contact",
                                "Bridge 10R Contact",
                                "BridgeMotorContact 11",
                                "EndOffStroke 11",
                                "Laatste Spoor",
                                "EndOffStroke 10",
                                "Universal Error",
                                "Collect Finished Fy Full",
                                "Collect On",
                                "Collect Off",
                                "Bridge Opening Msg 1",
                                "Bridge Opening Msg 2",
                                "Bridge Opening Msg 3",
                                "Bridge Opening Msg 4",
                                "Bridge Opening Msg 11",
                                "Bridge Opening Msg 12",
                                "Bridge Opening Msg 13",
                                "Bridge Opening Msg 14",
                                "Train Drive Out Cancelled"                            
                            };

        string[] Message_BOT = new string[48] {
                                "0",
                                "Bridge Open Ok",
                                "Bridge Close Ok",
                                "Fiddle One Left Ok",
                                "Fiddle One Right Ok",
                                "Fiddle Multiple Left Ok",
                                "Fiddle Multiple Right Ok",
                                "Train Detection Finished",
                                "Train Drive Out Finished",
                                "Train Drive In Finished",
                                "0",
                                "Init Done",
                                "Bridge Opening",
                                "Bridge Closing",
                                "Init Started",
                                "Train On 16B",
                                "Train Drive In Start",
                                "Train On 19A",
                                "Train Drive Out Start",
                                "Fiddle Yard Soft Start",
                                "Fiddle Yard Stopped",
                                "Fiddle Yard Reset",
                                "Bezet Uit Blok 17",
                                "Sensor F22 High",
                                "Bezet Uit Blok 17 AND Sensor F22",
                                "CL 20 Heart Sensor",
                                "Bridge Open Close Timeout Expired",
                                "Train Drive In Failed F22",
                                "BridgeMotorContact 20",
                                "Bridge 20L Contact",
                                "Bridge 20R Contact",
                                "BridgeMotorContact 21",
                                "EndOffStroke 21",
                                "Laatste Spoor",
                                "EndOffStroke 20",
                                "Universal Error",
                                "Collect Finished Fy Full",
                                "Collect On",
                                "Collect Off",
                                "Bridge Opening Msg 1",
                                "Bridge Opening Msg 2",
                                "Bridge Opening Msg 3",
                                "Bridge Opening Msg 4",
                                "Bridge Opening Msg 11",
                                "Bridge Opening Msg 12",
                                "Bridge Opening Msg 13",
                                "Bridge Opening Msg 14",
                                "Train Drive Out Cancelled"                            
                            };

        #endregion Translator


        public Controller(int poort, SetTextCallback callback, ToggleCommLinkCallback Toggle_Comm_Link)
        {
            _poort = poort;
            m_callback = callback;
            m_Toggle_Comm_Link = Toggle_Comm_Link;

        }

        public void Start()
        {
            _receiver = new Receiver(_poort);
            _receiver.NewData += HandleNewData;
            _receiver.Start();
        }

        public void HandleNewData(byte[] b)
        {
            //MessageBox.Show(Encoding.UTF8.GetString(b, 0, b.Length)); // for temp testing raw data in messagebox
            //control do something with data      
            
            string _b = Encoding.UTF8.GetString(b, 0, b.Length);        // convert received byte array to string array

            string TopBot = "";                                              // Used were text must be different between TOP and BOTTOM

            //m_callback(_b + Environment.NewLine, TOP);                // for temp testing raw data

            m_Toggle_Comm_Link();                                       // Show link activity
            
            #region Data To Variable or text
                                                                        // SetText_ReceivedCmd(string text, int Layer, int Indicator, int Val)
            if (_b[0] == 'M' || _b[0] == 'Z')
            {
                int i = 0;
                if (_b[0] == 'M')
                {
                    i = TOP;
                }
                if (_b[0] == 'Z')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].CL_10_Heart != (b[1] & 0x80))
                {
                    if (TOP == i)
                    {
                        TopBot = " CL 10 Heart ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " CL 20 Heart ";
                    }
                    _DataVariables[i].CL_10_Heart = (b[1] & 0x80);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].CL_10_Heart) + Environment.NewLine, i, 1, _DataVariables[i].CL_10_Heart);
                }
                if (_DataVariables[i].SPARE2 != (b[1] & 0x40))
                {
                    _DataVariables[i].SPARE2 = (b[1] & 0x40);
                    m_callback(DateTime.Now + " SPARE2 " + Convert.ToBoolean(_DataVariables[i].SPARE2) + Environment.NewLine, i, 2, _DataVariables[i].SPARE2);
                }
                if (_DataVariables[i].F11 != (b[1] & 0x20))
                {
                    if (TOP == i)
                    {
                        TopBot = " F11 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " F22 ";
                    }
                    _DataVariables[i].F11 = (b[1] & 0x20);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].F11) + Environment.NewLine, i, 3, _DataVariables[i].F11);
                }
                if (_DataVariables[i].EOS_10 != (b[1] & 0x10))
                {
                    if (TOP == i)
                    {
                        TopBot = " EOS 10 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " EOS 20 ";
                    }
                    _DataVariables[i].EOS_10 = (b[1] & 0x10);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].EOS_10) + Environment.NewLine, i, 4, _DataVariables[i].EOS_10);
                }
                if (_DataVariables[i].EOS_11 != (b[1] & 0x8))
                {
                    if (TOP == i)
                    {
                        TopBot = " EOS 11 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " EOS 21 ";
                    }
                    _DataVariables[i].EOS_11 = (b[1] & 0x8);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].EOS_11) + Environment.NewLine, i, 5, _DataVariables[i].EOS_11);
                }
                if (_DataVariables[i].SPARE3 != (b[1] & 0x4))
                {
                    _DataVariables[i].SPARE3 = (b[1] & 0x4);
                    m_callback(DateTime.Now + " SPARE3 " + Convert.ToBoolean(_DataVariables[i].SPARE3) + Environment.NewLine, i, 6, _DataVariables[i].SPARE3);
                }
                if (_DataVariables[i].F13 != (b[1] & 0x2))
                {
                    if (TOP == i)
                    {
                        TopBot = " F13 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " F23 ";
                    }
                    _DataVariables[i].F13 = (b[1] & 0x2);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].F13) + Environment.NewLine, i, 7, _DataVariables[i].F13);
                }                            
            }

            if (_b[0] == 'L' || _b[0] == 'Y')
            {
                int i = 0;
                if (_b[0] == 'L')
                {
                    i = TOP;
                }
                if (_b[0] == 'Y')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].Track_Nr != (b[1] & 0xF0))
                {
                    _DataVariables[i].Track_Nr = (b[1] & 0xF0);

                    int a = Convert.ToInt16(b[1]) >> 4;

                    m_callback(DateTime.Now + " Track Nr " + Convert.ToString(a) + Environment.NewLine, i, 32, a);
                }
                if (_DataVariables[i].F12 != (b[1] & 0x8))
                {
                    if (TOP == i)
                    {
                        TopBot = " F12 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " F22 ";
                    }
                    _DataVariables[i].F12 = (b[1] & 0x8);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].F12) + Environment.NewLine, i, 8, _DataVariables[i].F12);
                }
                if (_DataVariables[i].Occ_From_5B != (b[1] & 0x4))
                {
                    if (TOP == i)
                    {
                        TopBot = " Occupied from 5B ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " Occupied from 16B ";
                    }
                    _DataVariables[i].Occ_From_5B = (b[1] & 0x4);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].Occ_From_5B) + Environment.NewLine, i, 9, _DataVariables[i].Occ_From_5B);
                }
                if (_DataVariables[i].Occ_From_8A != (b[1] & 0x2))
                {
                    if (TOP == i)
                    {
                        TopBot = " Occupied from 8A ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " Occupied from 19A ";
                    }
                    _DataVariables[i].Occ_From_8A = (b[1] & 0x2);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].Occ_From_8A) + Environment.NewLine, i, 10, _DataVariables[i].Occ_From_8A);
                }
            }

            if (_b[0] == 'K' || _b[0] == 'X')
            {
                int i = 0;
                if (_b[0] == 'K')
                {
                    i = TOP;
                }
                if (_b[0] == 'X')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].Enable_Track_Status != (b[1] & 0x80))
                {
                    _DataVariables[i].Enable_Track_Status = (b[1] & 0x80);
                    m_callback(DateTime.Now + " Enable Track " + Convert.ToBoolean(_DataVariables[i].Enable_Track_Status) + Environment.NewLine, i, 11, _DataVariables[i].Enable_Track_Status);
                }
                if (_DataVariables[i].Occ_To_5B_Status != (b[1] & 0x40))
                {
                    if (TOP == i)
                    {
                        TopBot = " Occupied to 5B ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " Occupied to 16B ";
                    }
                    _DataVariables[i].Occ_To_5B_Status = (b[1] & 0x40);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].Occ_To_5B_Status) + Environment.NewLine, i, 12, _DataVariables[i].Occ_To_5B_Status);
                }
                if (_DataVariables[i].Occ_To_6_Status != (b[1] & 0x20))
                {
                    if (TOP == i)
                    {
                        TopBot = " Occupied to 6 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " Occupied to 17 ";
                    }
                    _DataVariables[i].Occ_To_6_Status = (b[1] & 0x20);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].Occ_To_6_Status) + Environment.NewLine, i, 13, _DataVariables[i].Occ_To_6_Status);
                }
                if (_DataVariables[i].Occ_To_7_Status != (b[1] & 0x10))
                {
                    if (TOP == i)
                    {
                        TopBot = " Occupied to 7 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " Occupied to 18 ";
                    }
                    _DataVariables[i].Occ_To_7_Status = (b[1] & 0x10);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].Occ_To_7_Status) + Environment.NewLine, i, 14, _DataVariables[i].Occ_To_7_Status);
                }
                if (_DataVariables[i].Occ_Resistor_Status != (b[1] & 0x8))
                {
                    _DataVariables[i].Occ_Resistor_Status = (b[1] & 0x8);
                    m_callback(DateTime.Now + " Occupied Resistor " + Convert.ToBoolean(_DataVariables[i].Occ_Resistor_Status) + Environment.NewLine, i, 15, _DataVariables[i].Occ_Resistor_Status);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track1 != (b[1] & 0x4))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track1 = (b[1] & 0x4);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track1 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track1) + Environment.NewLine, i, 16, _DataVariables[i].Trains_On_Fiddle_Yard_Track1);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track2 != (b[1] & 0x2))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track2 = (b[1] & 0x2);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track2 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track2) + Environment.NewLine, i, 17, _DataVariables[i].Trains_On_Fiddle_Yard_Track2);
                }                
            }

            if (_b[0] == 'J' || _b[0] == 'W')
            {
                int i = 0;
                if (_b[0] == 'J')
                {
                    i = TOP;
                }
                if (_b[0] == 'W')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track4 != (b[1] & 0x80))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track4 = (b[1] & 0x80);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track4 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track4) + Environment.NewLine, i, 19, _DataVariables[i].Trains_On_Fiddle_Yard_Track4);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track5 != (b[1] & 0x40))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track5 = (b[1] & 0x40);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track5 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track5) + Environment.NewLine, i, 20, _DataVariables[i].Trains_On_Fiddle_Yard_Track5);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track6 != (b[1] & 0x20))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track6 = (b[1] & 0x20);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track6 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track6) + Environment.NewLine, i, 21, _DataVariables[i].Trains_On_Fiddle_Yard_Track6);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track7 != (b[1] & 0x10))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track7 = (b[1] & 0x10);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track7 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track7) + Environment.NewLine, i, 22, _DataVariables[i].Trains_On_Fiddle_Yard_Track7);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track8 != (b[1] & 0x8))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track8 = (b[1] & 0x8);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track8 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track8) + Environment.NewLine, i, 23, _DataVariables[i].Trains_On_Fiddle_Yard_Track8);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track9 != (b[1] & 0x4))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track9 = (b[1] & 0x4);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track9 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track9) + Environment.NewLine, i, 24, _DataVariables[i].Trains_On_Fiddle_Yard_Track9);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track10 != (b[1] & 0x2))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track10 = (b[1] & 0x2);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track10 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track10) + Environment.NewLine, i, 25, _DataVariables[i].Trains_On_Fiddle_Yard_Track10);
                }                
            }

            if (_b[0] == 'I' || _b[0] == 'V')
            {
                int i = 0;
                if (_b[0] == 'I')
                {
                    i = TOP;
                }
                if (_b[0] == 'V')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].Occ_From_6 != (b[1] & 0x80))
                {
                    if (TOP == i)
                    {
                        TopBot = " Occupied from 6 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " Occupied from 17 ";
                    }
                    _DataVariables[i].Occ_From_6 = (b[1] & 0x80);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].Occ_From_6) + Environment.NewLine, i, 27, _DataVariables[i].Occ_From_6);
                }
                if (_DataVariables[i].Occ_From_7 != (b[1] & 0x40))
                {
                    if (TOP == i)
                    {
                        TopBot = " Occupied from 7 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " Occupied from 18 ";
                    }
                    _DataVariables[i].Occ_From_7 = (b[1] & 0x40);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].Occ_From_7) + Environment.NewLine, i, 28, _DataVariables[i].Occ_From_7);
                }
                if (_DataVariables[i].TR_MEAS != (b[1] & 0x20))     // TR_MEAS is connected to BOTTOM (0), TOP is SAPRE 1.
                {
                    _DataVariables[i].TR_MEAS = (b[1] & 0x20);
                    m_callback(DateTime.Now + " 15V Track Power " + Convert.ToBoolean(_DataVariables[i].TR_MEAS) + Environment.NewLine, TOP, 29, _DataVariables[i].TR_MEAS);
                    m_callback(DateTime.Now + " 15V Track Power " + Convert.ToBoolean(_DataVariables[i].TR_MEAS) + Environment.NewLine, BOTTOM, 29, _DataVariables[i].TR_MEAS);
                }
                if (_DataVariables[i].F10 != (b[1] & 0x10))
                {
                    if (TOP == i)
                    {
                        TopBot = " F10 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " F20 ";
                    }
                    _DataVariables[i].F10 = (b[1] & 0x10);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].F10) + Environment.NewLine, i, 30, _DataVariables[i].F10);
                }
                if (_DataVariables[i].M10_Status != (b[1] & 0x8))
                {
                    if (TOP == i)
                    {
                        TopBot = " M10 ";
                    }
                    else if (BOTTOM == i)
                    {
                        TopBot = " M20 ";
                    }
                    _DataVariables[i].M10_Status = (b[1] & 0x8);
                    m_callback(DateTime.Now + TopBot + Convert.ToBoolean(_DataVariables[i].M10_Status) + Environment.NewLine, i, 31, _DataVariables[i].M10_Status);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track3 != (b[1] & 0x4))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track3 = (b[1] & 0x4);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track3 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track3) + Environment.NewLine, i, 18, _DataVariables[i].Trains_On_Fiddle_Yard_Track3);
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track11 != (b[1] & 0x2))
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track11 = (b[1] & 0x2);
                    m_callback(DateTime.Now + " Trains On Fiddle Yard Track11 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track11) + Environment.NewLine, i, 26, _DataVariables[i].Trains_On_Fiddle_Yard_Track11);
                }                
            }

            if (_b[0] == 'A' || _b[0] == 'B')
            {
                if (_b[0] == 'A')
                {
                    if (b[1] <= Message_TOP.Length)
                    {
                        m_callback(DateTime.Now + " " + Message_TOP[b[1]] + Environment.NewLine, TOP, 0, 0);
                    }
                }
                if (_b[0] == 'B')
                {
                    if (b[1] <= Message_BOT.Length)
                    {
                        m_callback(DateTime.Now + " " + Message_BOT[b[1]] + Environment.NewLine, BOTTOM, 0, 0);
                    }
                }
            }

            #endregion Data To Variable or text

        }
    }
}
