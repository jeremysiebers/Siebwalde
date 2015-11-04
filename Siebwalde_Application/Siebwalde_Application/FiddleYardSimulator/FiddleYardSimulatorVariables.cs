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
using System.Management;
using System.Net.NetworkInformation;
using System.Globalization;

namespace Siebwalde_Application
{
    public class FiddleYardSimulatorVariables
    {
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
        public Var CL10Heart = new Var();
        public Var F11 = new Var();
        public Var EOS10 = new Var();
        public Var EOS11 = new Var();
        public Var F13 = new Var();
        public Var F12 = new Var();
        public Var Block5B = new Var();
        public Var Block8A = new Var();
        public Var TrackPower = new Var();
        public Var Block5BIn = new Var();
        public Var Block6In = new Var();
        public Var Block7In = new Var();
        public Var Resistor = new Var();
        public Var Track1 = new Var();
        public Var Track2 = new Var();
        public Var Track3 = new Var();
        public Var Track4 = new Var();
        public Var Track5 = new Var();
        public Var Track6 = new Var();
        public Var Track7 = new Var();
        public Var Track8 = new Var();
        public Var Track9 = new Var();
        public Var Track10 = new Var();
        public Var Track11 = new Var();
        public Var Block6 = new Var();
        public Var Block7 = new Var();
        public Var F10 = new Var();
        public Var M10 = new Var();
        public Var TrackPower15V = new Var();

        public Trk TrackNo = new Trk();

        public Msg FiddleOneLeftFinished = new Msg();
        public Msg FiddleOneRightFinished = new Msg();
        public Msg FiddleMultipleLeftFinished = new Msg();
        public Msg FiddleMultipleRightFinished = new Msg();
        public Msg TrainDetectionFinished = new Msg();
        public Msg TrainOn5B = new Msg();
        public Msg TrainOn8A = new Msg();
        public Msg FiddleYardReset = new Msg();
        public Msg uControllerReady = new Msg();  

        public SensorUpdater TargetAlive;

        public List<Msg> list = new List<Msg>();

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
        public FiddleYardSimulatorVariables()
        {
            TargetAlive = new SensorUpdater();

            list.Add(FiddleOneLeftFinished);
            list.Add(FiddleOneRightFinished);
            list.Add(FiddleMultipleLeftFinished);
            list.Add(FiddleMultipleRightFinished);
            list.Add(TrainDetectionFinished);
            list.Add(TrainOn5B);
            list.Add(TrainOn8A);
            list.Add(FiddleYardReset);
            list.Add(uControllerReady);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Reset
         * 
         *  Input(s)   : Reset all variables to default
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
        public void Reset()
        {
            CL10Heart.Value = true;
            F11.Value = false;
            EOS10.Value = false;
            EOS11.Value = false;
            F13.Value = false;
            F12.Value = false;
            TrackPower.Value = false;
            Block5BIn.Value = true;
            Block6In.Value = true;
            Block7In.Value = true;
            Resistor.Value = true;
            Track1.Value = false;
            Track2.Value = false;
            Track3.Value = false;
            Track4.Value = false;
            Track5.Value = false;
            Track6.Value = false;
            Track7.Value = false;
            Track8.Value = false;
            Track9.Value = false;
            Track10.Value = false;
            Track11.Value = false;
            Block6.Value = false;
            Block7.Value = false;
            F10.Value = false;
            M10.Value = false;
            //TrackNo.Count = 1;            
            TrackPower15V.Value = true;

            FiddleOneLeftFinished.Mssg = false;
            FiddleOneLeftFinished.Data = 0x03;
            FiddleOneRightFinished.Mssg = false;
            FiddleOneRightFinished.Data = 0x04;

            FiddleMultipleLeftFinished.Mssg = false;
            FiddleMultipleLeftFinished.Data = 0x05;
            FiddleMultipleRightFinished.Mssg = false;
            FiddleMultipleRightFinished.Data = 0x06;
            TrainDetectionFinished.Mssg = false;
            TrainDetectionFinished.Data = 0x07;
            TrainOn5B.Mssg = false;
            TrainOn5B.Data = 0x0F;
            TrainOn8A.Mssg = false;
            TrainOn8A.Data = 0x11;
            FiddleYardReset.Mssg = false;
            FiddleYardReset.Data = 0x15;
            uControllerReady.Mssg = false;
            uControllerReady.Data = 0x30;
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: IdleSetVariable
         * 
         *  Input(s)   : Variable to be set, no sequence required, these are commands
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
        public void IdleSetVariable(string Variable)
        {
            if (Variable == "Occ5BOnTrue")
            {
                Block5BIn.Value = true;
            }
            else if (Variable == "Occ5BOnFalse")
            {
                Block5BIn.Value = false;
            }
            else if (Variable == "Occ6OnTrue")
            {
                Block6In.Value = true;
            }
            else if (Variable == "Occ6OnFalse")
            {
                Block6In.Value = false;
            }
            else if (Variable == "Occ7OnTrue")
            {
                Block7In.Value = true;
            }
            else if (Variable == "Occ7OnFalse")
            {
                Block7In.Value = false;
            }
            else if (Variable == "Couple")
            {
                TrackPower.Value = true;
                Resistor.Value = false;
            }
            else if (Variable == "Uncouple")
            {
                TrackPower.Value = false;
                Resistor.Value = true;
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: CreateData
         *               To create the data as it would be sent by the real target
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
        public byte[] CreateData(string group)
        {
            byte[] data = new byte[] { 0x00, 0x00 };
            byte[] _group = new byte[] { 0, 0 };
            int _data = 0;

            if ("M" == group || "Z" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(CL10Heart.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(F11.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(EOS10.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(EOS11.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(F13.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("L" == group || "Y" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(TrackNo.Count);
                _data = _data << 1;
                _data |= Convert.ToByte(F12.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block5B.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block8A.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("K" == group || "X" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(TrackPower.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block5BIn.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block6In.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block7In.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Resistor.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track1.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track2.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("J" == group || "W" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(Track4.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track5.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track6.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track7.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track8.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track9.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track10.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("I" == group || "V" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(Block6.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Block7.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(TrackPower15V.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(F10.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(M10.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track3.Value);
                _data = _data << 1;
                _data |= Convert.ToByte(Track11.Value);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("H" == group || "U" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                _data |= Convert.ToByte(false);
                _data = _data << 1;
                data[1] = Convert.ToByte(_data);
            }
            else if ("A" == group || "B" == group)
            {
                _group = Encoding.ASCII.GetBytes(group);
                data[0] = _group[0];
                data[1] = 0x00;

                foreach (Msg msg in list)
                {
                    if (msg.Mssg == true)
                    {
                        msg.Mssg = false;
                        data[1] = Convert.ToByte(msg.Data);
                        break;
                    }
                }
            }

            return (data);
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Var
         * 
         *  Input(s)   : Set New bool value for sensor
         *
         *  Output(s)  : Return bool value of the sensor
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
        public class Var
        {
            public bool Value { get; set; }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Trk
         * 
         *  Input(s)   : Set New count value for Track no
         *
         *  Output(s)  : Return track no value target like
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
        public class Trk
        {
            private int _Count;
            private int Count_Return;

            public int Count
            {
                set
                {
                    _Count = value;
                }

                get
                {
                    switch (_Count)
                    {
                        case 0: Count_Return = 0;
                            break;
                        case 1: Count_Return = 0x1;
                            break;
                        case 2: Count_Return = 0x2;
                            break;
                        case 3: Count_Return = 0x3;
                            break;
                        case 4: Count_Return = 0x4;
                            break;
                        case 5: Count_Return = 0x5;
                            break;
                        case 6: Count_Return = 0x6;
                            break;
                        case 7: Count_Return = 0x7;
                            break;
                        case 8: Count_Return = 0x8;
                            break;
                        case 9: Count_Return = 0x9;
                            break;
                        case 10: Count_Return = 0xA;
                            break;
                        case 11: Count_Return = 0xB;
                            break;
                        default: Count_Return = 0;
                            break;
                    }

                    return Count_Return;
                }
            }
        }

        /*#--------------------------------------------------------------------------#*/
        /*  Description: Msg
         * 
         *  Input(s)   : Set New Message
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
        public class Msg
        {
            public bool Mssg { get; set; }
            public int Data { get; set; }
        }
    }
}
