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
    class Sender
    {
        static public UdpClient sendingUdpClient = new UdpClient(28671);
        static public string path = @"c:\localdata\Logging.txt";        

        static public void SendUdp(byte[] send)
        {
            sendingUdpClient.Send(send, send.Length);
        }

        static public void ConnectUdp()
        {
            sendingUdpClient.Connect("FIDDLEYARD", 28671);
        }

        static public void ConnectUdpLocalHost()
        {
            sendingUdpClient.Connect("LocalHost", 28671);
        }

        static public void CloseUdp()
        {
            sendingUdpClient.Close();
        }

        static public void StoreText(string text, string Layer)
        {
            try
            {

                using (var fs = new FileStream(path, FileMode.Append))
                {
                    Byte[] info =
                        new UTF8Encoding(true).GetBytes(Layer + text);
                    fs.Write(info, 0, info.Length);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
