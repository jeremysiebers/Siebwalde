using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Globalization;

namespace SiebwaldeApp
{
    public class MAC_IP_Conditioner
    {
        public string ipAddr = null;
        public string macAddr = null;



        public MAC_IP_Conditioner()         // During creation get MAC and IP adress of PC
        {
            macAddr = LocalMACAddress();
            ipAddr = LocalIPAddress();
        }

        public byte[,] MAC()                // Return preformatted array for MAC address to be send using UDP
        {
            return ProgramMAC(macAddr);
        }

        public byte[,] IP()                 // Return preformatted array for IP address to be send using UDP
        {   
            return ProgramIP(ipAddr);
        }

        public string MACstring()           // Return MAC address in string format, to be used for logging purposes
        {
            return macAddr;
        }

        public string IPstring()            // Return IP address in string format, to be used for logging purposes
        {
            return ipAddr;
        }





        private string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private string LocalMACAddress()
        {
            return (from nic in NetworkInterface.GetAllNetworkInterfaces()
                    where nic.OperationalStatus == OperationalStatus.Up
                    select nic.GetPhysicalAddress().ToString()
                    ).FirstOrDefault();
        }        

        private byte[,] ProgramMAC(string macAddr)
        {
            string[] Identifier = new string[12] { "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5" };
            byte[] _Identifier = new byte[1];
            byte[,] Send = new byte[12,3];

            for (int i = 0; i <= 11; i++)
            {
                _Identifier = Encoding.ASCII.GetBytes(Identifier[i]);
                Send[i,0] = _Identifier[0];
                Send[i,1] = Convert.ToByte(int.Parse(Convert.ToString(macAddr[i]), NumberStyles.HexNumber));
                Send[i,2] = 0x0D; // send CR afterwards
                //SEND XXX
            }
            return Send;
        }

        private byte[,] ProgramIP(string ipAddr)
        {
            // Identifiers for sending IP number are: { "6", "7", "8", "9"};
            string IpSend = "";
            byte[,] Send = new byte[4,3];

            int Length = ipAddr.Length - 1;             // start at 0 so 1 less then leght to use array indexing
            int[] Dot = new int[3];                     // dot index number in the ipAddr string
            int _DotPoint = 0;                          // counter to point to dot in ip number

            for (int Scan = 0; Scan <= Length; Scan++) // check were the dots are in the IP number
            {
                if (ipAddr[Scan] == '.')
                {
                    Dot[_DotPoint] = Scan;          // Store the location of the dot in the IP number
                    _DotPoint++;
                }
            }

            for (int scan = 0; scan <= (Dot[0] - 1); scan++)    // First part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[0, 0] = Convert.ToByte('6');
            Send[0, 1] = Convert.ToByte(IpSend);
            Send[0, 2] = 0x0D; // send CR afterwards
            


            IpSend = "";
            for (int scan = Dot[0] + 1; scan <= (Dot[1] - 1); scan++)   // Second part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[1, 0] = Convert.ToByte('7');
            Send[1, 1] = Convert.ToByte(IpSend);
            Send[1, 2] = 0x0D; // send CR afterwards
            


            IpSend = "";
            for (int scan = Dot[1] + 1; scan <= (Dot[2] - 1); scan++)   // Third part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[2, 0] = Convert.ToByte('8');
            Send[2, 1] = Convert.ToByte(IpSend);
            Send[2, 2] = 0x0D; // send CR afterwards
            


            IpSend = "";
            for (int scan = Dot[2] + 1; scan <= Length; scan++)   // Fourth part of the IP number
            {
                IpSend += ipAddr[scan];
            }
            Send[3, 0] = Convert.ToByte('9');
            Send[3, 1] = Convert.ToByte(IpSend);
            Send[3, 2] = 0x0D; // send CR afterwards

            return Send;
        }
    }
}
