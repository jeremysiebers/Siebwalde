using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Siebwalde_Application
{
    public class Receiver
    {
        public Action<byte[]> NewData;
        private int _poort;

        public Receiver(int poort)
        {
            _poort = poort;
        }

        public void Start()
        {
            Task.Factory.StartNew(HardwareReadout);
        }

        private void HardwareReadout()
        {
            try
            {
                //Creates a UdpClient for reading incoming data.
                UdpClient receivingUdpClient = new UdpClient(_poort);
                //Creates an IPEndPoint to record the IP Address and port number of the sender.  
                // The IPEndPoint will allow you to read datagrams sent from any source.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            

                while (true)
                {
                    byte[] receivedData = receivingUdpClient.Receive(ref RemoteIpEndPoint); // Blocking untill new data

                    if (NewData != null)
                    {
                        // Synchroon
                        NewData(receivedData);

                        // Asynchroon
                        //Task.Factory.StartNew(() => NewData(receivedData));
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Receiver error");
            }
        }
    }
}
