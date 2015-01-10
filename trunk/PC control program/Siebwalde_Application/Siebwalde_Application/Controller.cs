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
        private SetTextCallbackTOP m_callbackTOP = null;
        private SetTextCallbackBOT m_callbackBOT = null;
        private ToggleCommLinkCallback m_Toggle_Comm_Link = null;

        private const int TOP = 1;
        private const int BOTTOM = 0;
        private const bool RAW_UPDATE = false;

        private DataVariables[] _DataVariables = new DataVariables[2];

        #region Translator
         
        string[,] Message_TOP = new string[48,2] {
                                {"0","50"                                        },
                                {"Bridge Open Ok", "51"                          },
                                {"Bridge Close Ok", "52"                         },
                                {"Fiddle One Left Ok", "53"                      },
                                {"Fiddle One Right Ok", "54"                     },
                                {"Fiddle Multiple Left Ok", "55"                 },
                                {"Fiddle Multiple Right Ok", "56"                },
                                {"Train Detection Finished", "57"                },
                                {"Train Drive Out Finished", "58"                },
                                {"Train Drive In Finished", "59"                 },
                                {"0", "60"                                       },
                                {"Init Done", "61"                               },
                                {"Bridge Opening", "62"                          },
                                {"Bridge Closing", "63"                          },
                                {"Init Started", "64"                            },
                                {"Train On 5B", "65"                             },
                                {"Train Drive In Start", "66"                    },
                                {"Train On 8A", "67"                             },
                                {"Train Drive Out Start", "68"                   },
                                {"Fiddle Yard Soft Start", "69"                  },
                                {"Fiddle Yard Stopped", "70"                     },
                                {"Fiddle Yard Reset", "71"                       },
                                {"Bezet Uit Blok 6", "72"                        },
                                {"Sensor F12 High", "73"                         },
                                {"Bezet Uit Blok 6 AND Sensor F12", "74"         },
                                {"CL 10 Heart Sensor", "75"                      },
                                {"Bridge Open Close Timeout Expired", "76"       },
                                {"Train Drive In Failed F12", "77"               },
                                {"BridgeMotorContact 10", "78"                   },
                                {"Bridge 10L Contact", "79"                      },
                                {"Bridge 10R Contact", "80"                      },
                                {"BridgeMotorContact 11", "81"                   },
                                {"EndOffStroke 11", "82"                         },
                                {"Laatste Spoor", "83"                           },
                                {"EndOffStroke 10", "84"                         },
                                {"Universal Error", "85"                         },
                                {"Collect Finished Fy Full", "86"                },
                                {"Collect On", "87"                              },
                                {"Collect Off", "88"                             },
                                {"Bridge Opening Msg 1", "89"                    },
                                {"Bridge Opening Msg 2", "90"                    },
                                {"Bridge Opening Msg 3", "91"                    },
                                {"Bridge Opening Msg 4", "92"                    },
                                {"Bridge Opening Msg 11", "93"                   },
                                {"Bridge Opening Msg 12", "94"                   },
                                {"Bridge Opening Msg 13", "95"                   },
                                {"Bridge Opening Msg 14", "96"                   },
                                {"Train Drive Out Cancelled", "97"               }            
                            };

        string[,] Message_BOT = new string[48,2] {
                                {"0","50"                                        },
                                {"Bridge Open Ok", "51"                          },
                                {"Bridge Close Ok", "52"                         },
                                {"Fiddle One Left Ok", "53"                      },
                                {"Fiddle One Right Ok", "54"                     },
                                {"Fiddle Multiple Left Ok", "55"                 },
                                {"Fiddle Multiple Right Ok", "56"                },
                                {"Train Detection Finished", "57"                },
                                {"Train Drive Out Finished", "58"                },
                                {"Train Drive In Finished", "59"                 },
                                {"0", "60"                                       },
                                {"Init Done", "61"                               },
                                {"Bridge Opening", "62"                          },
                                {"Bridge Closing", "63"                          },
                                {"Init Started", "64"                            },
                                {"Train On 16B", "65"                            },
                                {"Train Drive In Start", "66"                    },
                                {"Train On 19A", "67"                            },
                                {"Train Drive Out Start", "68"                   },
                                {"Fiddle Yard Soft Start", "69"                  },
                                {"Fiddle Yard Stopped", "70"                     },
                                {"Fiddle Yard Reset", "71"                       },
                                {"Bezet Uit Blok 17", "72"                       },
                                {"Sensor F22 High", "73"                         },
                                {"Bezet Uit Blok 17 AND Sensor F22", "74"        },
                                {"CL 20 Heart Sensor", "75"                      },
                                {"Bridge Open Close Timeout Expired", "76"       },
                                {"Train Drive In Failed F22", "77"               },
                                {"BridgeMotorContact 20", "78"                   },
                                {"Bridge 20L Contact", "79"                      },
                                {"Bridge 20R Contact", "80"                      },
                                {"BridgeMotorContact 21", "81"                   },
                                {"EndOffStroke 21", "82"                         },
                                {"Laatste Spoor", "83"                           },
                                {"EndOffStroke 20", "84"                         },
                                {"Universal Error", "85"                         },
                                {"Collect Finished Fy Full", "86"                },
                                {"Collect On", "87"                              },
                                {"Collect Off", "88"                             },
                                {"Bridge Opening Msg 1", "89"                    },
                                {"Bridge Opening Msg 2", "90"                    },
                                {"Bridge Opening Msg 3", "91"                    },
                                {"Bridge Opening Msg 4", "92"                    },
                                {"Bridge Opening Msg 11", "93"                   },
                                {"Bridge Opening Msg 12", "94"                   },
                                {"Bridge Opening Msg 13", "95"                   },
                                {"Bridge Opening Msg 14", "96"                   },
                                {"Train Drive Out Cancelled", "97"               }                                            
                            };

        #endregion Translator


        public Controller(int poort, SetTextCallbackTOP callbackTOP, SetTextCallbackBOT callbackBOT, ToggleCommLinkCallback Toggle_Comm_Link)
        {
            _poort = poort;
            m_callbackTOP = callbackTOP;
            m_callbackBOT = callbackBOT;
            m_Toggle_Comm_Link = Toggle_Comm_Link;

        }

        public void Start()
        {
            _receiver = new Receiver(_poort);
            _receiver.NewData += HandleNewData;
            _receiver.Start();

            _DataVariables[0].CL_10_Heart = 999;
            _DataVariables[0].SPARE2 = 999;
            _DataVariables[0].F11 = 999;
            _DataVariables[0].EOS_10 = 999;
            _DataVariables[0].EOS_11 = 999;
            _DataVariables[0].SPARE3 = 999;
            _DataVariables[0].F13 = 999;
            _DataVariables[0].F12 = 999;
            _DataVariables[0].Occ_From_5B = 999;
            _DataVariables[0].Occ_From_8A = 999;                       
            _DataVariables[0].Enable_Track_Status = 999;               
            _DataVariables[0].Occ_To_5B_Status = 999;                  
            _DataVariables[0].Occ_To_6_Status = 999;                   
            _DataVariables[0].Occ_To_7_Status = 999;                   
            _DataVariables[0].Occ_Resistor_Status = 999;               
            _DataVariables[0].Trains_On_Fiddle_Yard_Track1 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track2 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track3 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track4 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track5 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track6 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track7 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track8 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track9 = 999;      
            _DataVariables[0].Trains_On_Fiddle_Yard_Track10 = 999;     
            _DataVariables[0].Trains_On_Fiddle_Yard_Track11 = 999;     
            _DataVariables[0].Occ_From_6 = 999;                        
            _DataVariables[0].Occ_From_7 = 999;                        
            _DataVariables[0].TR_MEAS = 999;                           
            _DataVariables[0].F10 = 999;                               
            _DataVariables[0].M10_Status = 999;                        
            _DataVariables[0].Track_Nr = 999;

            _DataVariables[1].CL_10_Heart = 999;
            _DataVariables[1].SPARE2 = 999;
            _DataVariables[1].F11 = 999;
            _DataVariables[1].EOS_10 = 999;
            _DataVariables[1].EOS_11 = 999;
            _DataVariables[1].SPARE3 = 999;
            _DataVariables[1].F13 = 999;
            _DataVariables[1].F12 = 999;
            _DataVariables[1].Occ_From_5B = 999;
            _DataVariables[1].Occ_From_8A = 999;
            _DataVariables[1].Enable_Track_Status = 999;
            _DataVariables[1].Occ_To_5B_Status = 999;
            _DataVariables[1].Occ_To_6_Status = 999;
            _DataVariables[1].Occ_To_7_Status = 999;
            _DataVariables[1].Occ_Resistor_Status = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track1 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track2 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track3 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track4 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track5 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track6 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track7 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track8 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track9 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track10 = 999;
            _DataVariables[1].Trains_On_Fiddle_Yard_Track11 = 999;
            _DataVariables[1].Occ_From_6 = 999;
            _DataVariables[1].Occ_From_7 = 999;
            _DataVariables[1].TR_MEAS = 999;
            _DataVariables[1].F10 = 999;
            _DataVariables[1].M10_Status = 999;
            _DataVariables[1].Track_Nr = 999;   


        }

        public void HandleNewData(byte[] b)
        {
            
            string _b = Encoding.UTF8.GetString(b, 0, b.Length);        // convert received byte array to string array
            
            m_Toggle_Comm_Link();                                       // Show link activity
            
            #region Data To Variable or text
                                                                        // SetText_ReceivedCmd(string text, int Layer, int Indicator, int Val)
            if (_b[0] == 'M' || _b[0] == 'Z')
            {
                int i = 9;
                if (_b[0] == 'M')
                {
                    i = TOP;
                }
                if (_b[0] == 'Z')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].CL_10_Heart != (b[1] & 0x80) || RAW_UPDATE)
                {
                    _DataVariables[i].CL_10_Heart = (b[1] & 0x80);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " CL 10 Heart " + Convert.ToBoolean(_DataVariables[i].CL_10_Heart) + Environment.NewLine, i, 1, _DataVariables[i].CL_10_Heart);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " CL 20 Heart " + Convert.ToBoolean(_DataVariables[i].CL_10_Heart) + Environment.NewLine, i, 1, _DataVariables[i].CL_10_Heart);
                    }                    
                }
                if (_DataVariables[i].SPARE2 != (b[1] & 0x40) || RAW_UPDATE)
                {
                    _DataVariables[i].SPARE2 = (b[1] & 0x40);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " SPARE2 " + Convert.ToBoolean(_DataVariables[i].SPARE2) + Environment.NewLine, i, 2, _DataVariables[i].SPARE2);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " SPARE2 " + Convert.ToBoolean(_DataVariables[i].SPARE2) + Environment.NewLine, i, 2, _DataVariables[i].SPARE2);
                    }                    
                }
                if (_DataVariables[i].F11 != (b[1] & 0x20) || RAW_UPDATE)
                {
                    _DataVariables[i].F11 = (b[1] & 0x20);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " F11 " + Convert.ToBoolean(_DataVariables[i].F11) + Environment.NewLine, i, 3, _DataVariables[i].F11);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " F22 " + Convert.ToBoolean(_DataVariables[i].F11) + Environment.NewLine, i, 3, _DataVariables[i].F11);
                    }
                }/*
                if (TOP == i)
                {
                    m_callbackTOP("", i, 3, _DataVariables[i].F11); // to force indicator update for sensors which are positioned on the tracks, F11,12 and 13
                }
                if (BOTTOM == i)
                {
                    m_callbackBOT("", i, 3, _DataVariables[i].F11); // to force indicator update for sensors which are positioned on the tracks, F11,12 and 13
                }*/

                if (_DataVariables[i].EOS_10 != (b[1] & 0x10) || RAW_UPDATE)
                {
                    _DataVariables[i].EOS_10 = (b[1] & 0x10);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " EOS 10 " + Convert.ToBoolean(_DataVariables[i].EOS_10) + Environment.NewLine, i, 4, _DataVariables[i].EOS_10);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " EOS 20 " + Convert.ToBoolean(_DataVariables[i].EOS_10) + Environment.NewLine, i, 4, _DataVariables[i].EOS_10);
                    }                                                            
                }
                if (_DataVariables[i].EOS_11 != (b[1] & 0x8) || RAW_UPDATE)
                {
                    _DataVariables[i].EOS_11 = (b[1] & 0x8);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " EOS 11 " + Convert.ToBoolean(_DataVariables[i].EOS_11) + Environment.NewLine, i, 5, _DataVariables[i].EOS_11);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " EOS 21 " + Convert.ToBoolean(_DataVariables[i].EOS_11) + Environment.NewLine, i, 5, _DataVariables[i].EOS_11);
                    }                   
                }
                if (_DataVariables[i].SPARE3 != (b[1] & 0x4) || RAW_UPDATE)
                {
                    _DataVariables[i].SPARE3 = (b[1] & 0x4); 
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " SPARE3 " + Convert.ToBoolean(_DataVariables[i].SPARE3) + Environment.NewLine, i, 6, _DataVariables[i].SPARE3);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " SPARE3 " + Convert.ToBoolean(_DataVariables[i].SPARE3) + Environment.NewLine, i, 6, _DataVariables[i].SPARE3);
                    }                                       
                }
                if (_DataVariables[i].F13 != (b[1] & 0x2) || RAW_UPDATE)
                {
                    _DataVariables[i].F13 = (b[1] & 0x2);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " F13 " + Convert.ToBoolean(_DataVariables[i].F13) + Environment.NewLine, i, 7, _DataVariables[i].F13);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " F23 " + Convert.ToBoolean(_DataVariables[i].F13) + Environment.NewLine, i, 7, _DataVariables[i].F13);
                    }                   
                }/*
                if (TOP == i)
                {
                    m_callbackTOP("", i, 7, _DataVariables[i].F13); // to force indicator update for sensors which are positioned on the tracks, F11,12 and 13
                }
                if (BOTTOM == i)
                {
                    m_callbackBOT("", i, 7, _DataVariables[i].F13); // to force indicator update for sensors which are positioned on the tracks, F11,12 and 13
                }*/
            }

            if (_b[0] == 'L' || _b[0] == 'Y')
            {
                int i = 9;
                if (_b[0] == 'L')
                {
                    i = TOP;
                }
                if (_b[0] == 'Y')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].Track_Nr != (b[1] & 0xF0) || RAW_UPDATE)
                {
                    _DataVariables[i].Track_Nr = (b[1] & 0xF0);

                    int a = Convert.ToInt16(b[1]) >> 4;
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Track Nr " + Convert.ToString(a) + Environment.NewLine, i, 32, a);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Track Nr " + Convert.ToString(a) + Environment.NewLine, i, 32, a);
                    }
                    
                }
                if (_DataVariables[i].F12 != (b[1] & 0x8) || RAW_UPDATE)
                {
                    _DataVariables[i].F12 = (b[1] & 0x8);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " F12 " + Convert.ToBoolean(_DataVariables[i].F12) + Environment.NewLine, i, 8, _DataVariables[i].F12);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " F22 " + Convert.ToBoolean(_DataVariables[i].F12) + Environment.NewLine, i, 8, _DataVariables[i].F12);
                    }                   
                }/*
                if (TOP == i)
                {
                    m_callbackTOP("", i, 8, _DataVariables[i].F12); // to force indicator update for sensors which are positioned on the tracks, F11,12 and 13
                }
                if (BOTTOM == i)
                {
                    m_callbackBOT("", i, 8, _DataVariables[i].F12); // to force indicator update for sensors which are positioned on the tracks, F11,12 and 13
                }*/

                if (_DataVariables[i].Occ_From_5B != (b[1] & 0x4) || RAW_UPDATE)
                {
                    _DataVariables[i].Occ_From_5B = (b[1] & 0x4);  
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Occupied from 5B " + Convert.ToBoolean(_DataVariables[i].Occ_From_5B) + Environment.NewLine, i, 9, _DataVariables[i].Occ_From_5B);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Occupied from 16B " + Convert.ToBoolean(_DataVariables[i].Occ_From_5B) + Environment.NewLine, i, 9, _DataVariables[i].Occ_From_5B);
                    }                 
                }
                if (_DataVariables[i].Occ_From_8A != (b[1] & 0x2) || RAW_UPDATE)
                {
                    _DataVariables[i].Occ_From_8A = (b[1] & 0x2);   
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Occupied from 8A " + Convert.ToBoolean(_DataVariables[i].Occ_From_8A) + Environment.NewLine, i, 10, _DataVariables[i].Occ_From_8A);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Occupied from 19A " + Convert.ToBoolean(_DataVariables[i].Occ_From_8A) + Environment.NewLine, i, 10, _DataVariables[i].Occ_From_8A);
                    }                
                }
            }

            if (_b[0] == 'K' || _b[0] == 'X')
            {
                int i = 9;
                if (_b[0] == 'K')
                {
                    i = TOP;
                }
                if (_b[0] == 'X')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].Enable_Track_Status != (b[1] & 0x80) || RAW_UPDATE)
                {
                    _DataVariables[i].Enable_Track_Status = (b[1] & 0x80);    
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Enable Track " + Convert.ToBoolean(_DataVariables[i].Enable_Track_Status) + Environment.NewLine, i, 11, _DataVariables[i].Enable_Track_Status);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Enable Track " + Convert.ToBoolean(_DataVariables[i].Enable_Track_Status) + Environment.NewLine, i, 11, _DataVariables[i].Enable_Track_Status);
                    }               
                }
                if (_DataVariables[i].Occ_To_5B_Status != (b[1] & 0x40) || RAW_UPDATE)
                {
                    _DataVariables[i].Occ_To_5B_Status = (b[1] & 0x40);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Occupied to 5B " + Convert.ToBoolean(_DataVariables[i].Occ_To_5B_Status) + Environment.NewLine, i, 12, _DataVariables[i].Occ_To_5B_Status);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Occupied to 16B " + Convert.ToBoolean(_DataVariables[i].Occ_To_5B_Status) + Environment.NewLine, i, 12, _DataVariables[i].Occ_To_5B_Status);
                    }
                }
                if (_DataVariables[i].Occ_To_6_Status != (b[1] & 0x20) || RAW_UPDATE)
                {
                    _DataVariables[i].Occ_To_6_Status = (b[1] & 0x20);       
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Occupied to 6 " + Convert.ToBoolean(_DataVariables[i].Occ_To_6_Status) + Environment.NewLine, i, 13, _DataVariables[i].Occ_To_6_Status);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Occupied to 17 " + Convert.ToBoolean(_DataVariables[i].Occ_To_6_Status) + Environment.NewLine, i, 13, _DataVariables[i].Occ_To_6_Status);
                    }            
                }
                if (_DataVariables[i].Occ_To_7_Status != (b[1] & 0x10) || RAW_UPDATE)
                {
                    _DataVariables[i].Occ_To_7_Status = (b[1] & 0x10);                    
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Occupied to 7 " + Convert.ToBoolean(_DataVariables[i].Occ_To_7_Status) + Environment.NewLine, i, 14, _DataVariables[i].Occ_To_7_Status);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Occupied to 18 " + Convert.ToBoolean(_DataVariables[i].Occ_To_7_Status) + Environment.NewLine, i, 14, _DataVariables[i].Occ_To_7_Status);
                    }
                }
                if (_DataVariables[i].Occ_Resistor_Status != (b[1] & 0x8) || RAW_UPDATE)
                {
                    _DataVariables[i].Occ_Resistor_Status = (b[1] & 0x8);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Occupied Resistor " + Convert.ToBoolean(_DataVariables[i].Occ_Resistor_Status) + Environment.NewLine, i, 15, _DataVariables[i].Occ_Resistor_Status);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Occupied Resistor " + Convert.ToBoolean(_DataVariables[i].Occ_Resistor_Status) + Environment.NewLine, i, 15, _DataVariables[i].Occ_Resistor_Status);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track1 != (b[1] & 0x4) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track1 = (b[1] & 0x4);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track1 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track1) + Environment.NewLine, i, 16, _DataVariables[i].Trains_On_Fiddle_Yard_Track1);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track1 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track1) + Environment.NewLine, i, 16, _DataVariables[i].Trains_On_Fiddle_Yard_Track1);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track2 != (b[1] & 0x2) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track2 = (b[1] & 0x2);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track2 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track2) + Environment.NewLine, i, 17, _DataVariables[i].Trains_On_Fiddle_Yard_Track2);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track2 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track2) + Environment.NewLine, i, 17, _DataVariables[i].Trains_On_Fiddle_Yard_Track2);
                    }
                }                
            }

            if (_b[0] == 'J' || _b[0] == 'W')
            {
                int i = 9;
                if (_b[0] == 'J')
                {
                    i = TOP;
                }
                if (_b[0] == 'W')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track4 != (b[1] & 0x80) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track4 = (b[1] & 0x80);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track4 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track4) + Environment.NewLine, i, 19, _DataVariables[i].Trains_On_Fiddle_Yard_Track4);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track4 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track4) + Environment.NewLine, i, 19, _DataVariables[i].Trains_On_Fiddle_Yard_Track4);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track5 != (b[1] & 0x40) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track5 = (b[1] & 0x40);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track5 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track5) + Environment.NewLine, i, 20, _DataVariables[i].Trains_On_Fiddle_Yard_Track5);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track5 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track5) + Environment.NewLine, i, 20, _DataVariables[i].Trains_On_Fiddle_Yard_Track5);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track6 != (b[1] & 0x20) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track6 = (b[1] & 0x20);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track6 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track6) + Environment.NewLine, i, 21, _DataVariables[i].Trains_On_Fiddle_Yard_Track6);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track6 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track6) + Environment.NewLine, i, 21, _DataVariables[i].Trains_On_Fiddle_Yard_Track6);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track7 != (b[1] & 0x10) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track7 = (b[1] & 0x10);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track7 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track7) + Environment.NewLine, i, 22, _DataVariables[i].Trains_On_Fiddle_Yard_Track7);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track7 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track7) + Environment.NewLine, i, 22, _DataVariables[i].Trains_On_Fiddle_Yard_Track7);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track8 != (b[1] & 0x8) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track8 = (b[1] & 0x8);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track8 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track8) + Environment.NewLine, i, 23, _DataVariables[i].Trains_On_Fiddle_Yard_Track8);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track8 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track8) + Environment.NewLine, i, 23, _DataVariables[i].Trains_On_Fiddle_Yard_Track8);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track9 != (b[1] & 0x4) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track9 = (b[1] & 0x4);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track9 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track9) + Environment.NewLine, i, 24, _DataVariables[i].Trains_On_Fiddle_Yard_Track9);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track9 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track9) + Environment.NewLine, i, 24, _DataVariables[i].Trains_On_Fiddle_Yard_Track9);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track10 != (b[1] & 0x2) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track10 = (b[1] & 0x2);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track10 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track10) + Environment.NewLine, i, 25, _DataVariables[i].Trains_On_Fiddle_Yard_Track10);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track10 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track10) + Environment.NewLine, i, 25, _DataVariables[i].Trains_On_Fiddle_Yard_Track10);
                    }
                }                
            }

            if (_b[0] == 'I' || _b[0] == 'V')
            {
                int i = 9;
                if (_b[0] == 'I')
                {
                    i = TOP;
                }
                if (_b[0] == 'V')
                {
                    i = BOTTOM;
                }
                if (_DataVariables[i].Occ_From_6 != (b[1] & 0x80) || RAW_UPDATE)
                {
                    _DataVariables[i].Occ_From_6 = (b[1] & 0x80);   
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Occupied from 6 " + Convert.ToBoolean(_DataVariables[i].Occ_From_6) + Environment.NewLine, i, 27, _DataVariables[i].Occ_From_6);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Occupied from 17 " + Convert.ToBoolean(_DataVariables[i].Occ_From_6) + Environment.NewLine, i, 27, _DataVariables[i].Occ_From_6);
                    }                
                }
                if (_DataVariables[i].Occ_From_7 != (b[1] & 0x40) || RAW_UPDATE)
                {
                    _DataVariables[i].Occ_From_7 = (b[1] & 0x40);                    
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Occupied from 7 " + Convert.ToBoolean(_DataVariables[i].Occ_From_7) + Environment.NewLine, i, 28, _DataVariables[i].Occ_From_7);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Occupied from 18 " + Convert.ToBoolean(_DataVariables[i].Occ_From_7) + Environment.NewLine, i, 28, _DataVariables[i].Occ_From_7);
                    }
                }
                if (_DataVariables[i].TR_MEAS != (b[1] & 0x20) || RAW_UPDATE)     // TR_MEAS is connected to BOTTOM (0), TOP is SAPRE 1.
                {
                    _DataVariables[i].TR_MEAS = (b[1] & 0x20);
                    m_callbackTOP(DateTime.Now + " 15V Track Power " + Convert.ToBoolean(_DataVariables[i].TR_MEAS) + Environment.NewLine, TOP, 29, _DataVariables[i].TR_MEAS);
                    m_callbackBOT(DateTime.Now + " 15V Track Power " + Convert.ToBoolean(_DataVariables[i].TR_MEAS) + Environment.NewLine, BOTTOM, 29, _DataVariables[i].TR_MEAS);
                    
                }
                if (_DataVariables[i].F10 != (b[1] & 0x10) || RAW_UPDATE)
                {
                    _DataVariables[i].F10 = (b[1] & 0x10);        
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " F10 " + Convert.ToBoolean(_DataVariables[i].F10) + Environment.NewLine, i, 30, _DataVariables[i].F10);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " F20 " + Convert.ToBoolean(_DataVariables[i].F10) + Environment.NewLine, i, 30, _DataVariables[i].F10);
                    }           
                }
                if (_DataVariables[i].M10_Status != (b[1] & 0x8) || RAW_UPDATE)
                {
                    _DataVariables[i].M10_Status = (b[1] & 0x8);              
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " M10 " + Convert.ToBoolean(_DataVariables[i].M10_Status) + Environment.NewLine, i, 31, _DataVariables[i].M10_Status);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " M20 " + Convert.ToBoolean(_DataVariables[i].M10_Status) + Environment.NewLine, i, 31, _DataVariables[i].M10_Status);
                    }     
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track3 != (b[1] & 0x4) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track3 = (b[1] & 0x4);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track3 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track3) + Environment.NewLine, i, 18, _DataVariables[i].Trains_On_Fiddle_Yard_Track3);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track3 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track3) + Environment.NewLine, i, 18, _DataVariables[i].Trains_On_Fiddle_Yard_Track3);
                    }
                }
                if (_DataVariables[i].Trains_On_Fiddle_Yard_Track11 != (b[1] & 0x2) || RAW_UPDATE)
                {
                    _DataVariables[i].Trains_On_Fiddle_Yard_Track11 = (b[1] & 0x2);
                    if (TOP == i)
                    {
                        m_callbackTOP(DateTime.Now + " Trains On Fiddle Yard Track11 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track11) + Environment.NewLine, i, 26, _DataVariables[i].Trains_On_Fiddle_Yard_Track11);
                    }
                    else if (BOTTOM == i)
                    {
                        m_callbackBOT(DateTime.Now + " Trains On Fiddle Yard Track11 " + Convert.ToBoolean(_DataVariables[i].Trains_On_Fiddle_Yard_Track11) + Environment.NewLine, i, 26, _DataVariables[i].Trains_On_Fiddle_Yard_Track11);
                    }
                }                
            }

            if (_b[0] == 'A' || _b[0] == 'B')
            {
                if (_b[0] == 'A')
                {
                    if (b[1] <= Message_TOP.Length)
                    {
                        m_callbackTOP(DateTime.Now + " " + Message_TOP[b[1], 0] + Environment.NewLine, TOP, Convert.ToInt16(Message_TOP[b[1], 1]), 0);
                    }
                }
                if (_b[0] == 'B')
                {
                    if (b[1] <= Message_BOT.Length)
                    {
                        m_callbackBOT(DateTime.Now + " " + Message_BOT[b[1], 0] + Environment.NewLine, BOTTOM, Convert.ToInt16(Message_BOT[b[1], 1]), 0);
                    }
                }
            }
            
            #endregion Data To Variable or text
            
        }
    }
}
