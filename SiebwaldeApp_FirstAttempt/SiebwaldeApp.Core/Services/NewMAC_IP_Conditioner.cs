using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace SiebwaldeApp.Core
{
    public sealed class NewMAC_IP_Conditioner
    {
        private readonly string? _macAddr;
        private readonly string _ipAddr;

        public NewMAC_IP_Conditioner() // During creation get MAC and IP address of PC
        {
            _macAddr = LocalMACAddress();
            _ipAddr = LocalIPAddress() ?? string.Empty;
        }

        // ---- Public API -----------------------------------------------------

        // Throwing version (fail-fast). Caller must handle exceptions.
        public byte[,] MAC()
        {
            if (_macAddr is null)
                throw new InvalidOperationException("No active NIC with a valid MAC was found.");

            return ProgramMAC(_macAddr);
        }

        // Non-throwing version (Try-pattern). Returns false if no MAC.
        public bool TryGetMAC(out byte[,] send)
        {
            if (_macAddr is null)
            {
                send = default!;
                return false;
            }

            send = ProgramMAC(_macAddr);
            return true;
        }

        public byte[,] IP() => ProgramIP(_ipAddr);

        public string MACstring() => _macAddr ?? string.Empty;

        public string IPstring() => _ipAddr;

        // ---- Helpers --------------------------------------------------------

        private static string? LocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                return ip?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string? LocalMACAddress()
        {
            // Pick first UP NIC with any physical address
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Select(nic => nic.GetPhysicalAddress()?.ToString())
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

            if (string.IsNullOrWhiteSpace(mac))
                return null;

            mac = mac.Replace(":", "").Replace("-", "");

            // Require at least 12 hex chars (EUI-48). If longer, take first 12 nibbles.
            if (mac.Length < 12)
                return null;

            return mac[..12].ToUpperInvariant();
        }

        private static byte[,] ProgramMAC(string macAddr)
        {
            if (string.IsNullOrWhiteSpace(macAddr))
                throw new ArgumentException("MAC address is null/empty", nameof(macAddr));

            macAddr = macAddr.Replace(":", "").Replace("-", "");
            if (macAddr.Length < 12)
                throw new ArgumentException("MAC must contain at least 12 hex characters (EUI-48).", nameof(macAddr));

            string[] identifiers = { "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5" };
            var send = new byte[12, 3];

            for (int i = 0; i < 12; i++)
            {
                send[i, 0] = Encoding.ASCII.GetBytes(identifiers[i])[0];

                char c = macAddr[i];
                if (!Uri.IsHexDigit(c))
                    throw new FormatException($"Invalid hex character '{c}' at position {i} in MAC '{macAddr}'.");

                send[i, 1] = byte.Parse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                send[i, 2] = 0x0D; // CR
            }

            return send;
        }

        private static byte[,] ProgramIP(string ipAddr)
        {
            if (string.IsNullOrWhiteSpace(ipAddr))
                throw new ArgumentException("IP address is null/empty", nameof(ipAddr));

            // Identifiers for sending IP number are: { "6", "7", "8", "9" }
            var parts = ipAddr.Split('.');
            if (parts.Length != 4 || !parts.All(p => byte.TryParse(p, out _)))
                throw new FormatException($"Invalid IPv4 address '{ipAddr}'.");

            var send = new byte[4, 3];

            send[0, 0] = (byte)'6'; send[0, 1] = byte.Parse(parts[0], CultureInfo.InvariantCulture); send[0, 2] = 0x0D;
            send[1, 0] = (byte)'7'; send[1, 1] = byte.Parse(parts[1], CultureInfo.InvariantCulture); send[1, 2] = 0x0D;
            send[2, 0] = (byte)'8'; send[2, 1] = byte.Parse(parts[2], CultureInfo.InvariantCulture); send[2, 2] = 0x0D;
            send[3, 0] = (byte)'9'; send[3, 1] = byte.Parse(parts[3], CultureInfo.InvariantCulture); send[3, 2] = 0x0D;

            return send;
        }
    }
}


//using System.Data;
//using System.Text;
//using System.Net.Sockets;
//using System.Net;
//using System.Net.NetworkInformation;
//using System.Globalization;

//namespace SiebwaldeApp.Core
//{
//    public class NewMAC_IP_Conditioner
//    {
//        public string ipAddr;
//        public string macAddr;



//        public NewMAC_IP_Conditioner()         // During creation get MAC and IP adress of PC
//        {
//            macAddr = LocalMACAddress();
//            ipAddr = LocalIPAddress();
//        }

//        public byte[,] MAC()                // Return preformatted array for MAC address to be send using UDP
//        {
//            return ProgramMAC(macAddr);
//        }

//        public byte[,] IP()                 // Return preformatted array for IP address to be send using UDP
//        {   
//            return ProgramIP(ipAddr);
//        }

//        public string MACstring()           // Return MAC address in string format, to be used for logging purposes
//        {
//            return macAddr;
//        }

//        public string IPstring()            // Return IP address in string format, to be used for logging purposes
//        {
//            return ipAddr;
//        }





//        private string LocalIPAddress()
//        {
//            IPHostEntry host;
//            string localIP = "";
//            host = Dns.GetHostEntry(Dns.GetHostName());
//            foreach (IPAddress ip in host.AddressList)
//            {
//                if (ip.AddressFamily == AddressFamily.InterNetwork)
//                {
//                    localIP = ip.ToString();
//                    break;
//                }
//            }
//            return localIP;
//        }

//        //private string LocalMACAddress()
//        //{
//        //    return (from nic in NetworkInterface.GetAllNetworkInterfaces()
//        //            where nic.OperationalStatus == OperationalStatus.Up
//        //            select nic.GetPhysicalAddress().ToString()
//        //            ).FirstOrDefault();
//        //}        

//        //private byte[,] ProgramMAC(string macAddr)
//        //{
//        //    string[] Identifier = new string[12] { "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5" };
//        //    byte[] _Identifier = new byte[1];
//        //    byte[,] Send = new byte[12,3];

//        //    for (int i = 0; i <= 11; i++)
//        //    {
//        //        _Identifier = Encoding.ASCII.GetBytes(Identifier[i]);
//        //        Send[i,0] = _Identifier[0];
//        //        Send[i,1] = Convert.ToByte(int.Parse(Convert.ToString(macAddr[i]), NumberStyles.HexNumber));
//        //        Send[i,2] = 0x0D; // send CR afterwards
//        //        //SEND XXX
//        //    }
//        //    return Send;
//        //}

//        private string? LocalMACAddress()
//        {
//            // Pick the first UP NIC with a valid hex MAC, strip separators
//            var mac = NetworkInterface.GetAllNetworkInterfaces()
//                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
//                .Select(nic => nic.GetPhysicalAddress()?.ToString())
//                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

//            if (string.IsNullOrWhiteSpace(mac))
//                return null;

//            mac = mac.Replace(":", "").Replace("-", "");   // just in case
//                                                           // Require at least 12 hex chars (EUI-48). If longer (e.g. 16), take the first 12 nibbles.
//            if (mac.Length < 12) return null;
//            return mac.Substring(0, 12).ToUpperInvariant();
//        }

//        private byte[,] ProgramMAC(string macAddr)
//        {
//            if (string.IsNullOrWhiteSpace(macAddr))
//                throw new ArgumentException("MAC address is null/empty", nameof(macAddr));

//            macAddr = macAddr.Replace(":", "").Replace("-", "");
//            if (macAddr.Length < 12)
//                throw new ArgumentException("MAC must contain at least 12 hex characters (EUI-48).", nameof(macAddr));

//            string[] identifiers = { "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5" };
//            var send = new byte[12, 3];

//            for (int i = 0; i < 12; i++)
//            {
//                send[i, 0] = Encoding.ASCII.GetBytes(identifiers[i])[0];

//                char c = macAddr[i];
//                // validate hex nibble
//                if (!Uri.IsHexDigit(c))
//                    throw new FormatException($"Invalid hex character '{c}' at position {i} in MAC '{macAddr}'.");

//                // convert single hex char to byte 0..15
//                send[i, 1] = byte.Parse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

//                send[i, 2] = 0x0D; // CR
//            }
//            return send;
//        }

//        private byte[,] ProgramIP(string ipAddr)
//        {
//            // Identifiers for sending IP number are: { "6", "7", "8", "9"};
//            string IpSend = "";
//            byte[,] Send = new byte[4,3];

//            int Length = ipAddr.Length - 1;             // start at 0 so 1 less then leght to use array indexing
//            int[] Dot = new int[3];                     // dot index number in the ipAddr string
//            int _DotPoint = 0;                          // counter to point to dot in ip number

//            for (int Scan = 0; Scan <= Length; Scan++) // check were the dots are in the IP number
//            {
//                if (ipAddr[Scan] == '.')
//                {
//                    Dot[_DotPoint] = Scan;          // Store the location of the dot in the IP number
//                    _DotPoint++;
//                }
//            }

//            for (int scan = 0; scan <= (Dot[0] - 1); scan++)    // First part of the IP number
//            {
//                IpSend += ipAddr[scan];
//            }
//            Send[0, 0] = Convert.ToByte('6');
//            Send[0, 1] = Convert.ToByte(IpSend);
//            Send[0, 2] = 0x0D; // send CR afterwards



//            IpSend = "";
//            for (int scan = Dot[0] + 1; scan <= (Dot[1] - 1); scan++)   // Second part of the IP number
//            {
//                IpSend += ipAddr[scan];
//            }
//            Send[1, 0] = Convert.ToByte('7');
//            Send[1, 1] = Convert.ToByte(IpSend);
//            Send[1, 2] = 0x0D; // send CR afterwards



//            IpSend = "";
//            for (int scan = Dot[1] + 1; scan <= (Dot[2] - 1); scan++)   // Third part of the IP number
//            {
//                IpSend += ipAddr[scan];
//            }
//            Send[2, 0] = Convert.ToByte('8');
//            Send[2, 1] = Convert.ToByte(IpSend);
//            Send[2, 2] = 0x0D; // send CR afterwards



//            IpSend = "";
//            for (int scan = Dot[2] + 1; scan <= Length; scan++)   // Fourth part of the IP number
//            {
//                IpSend += ipAddr[scan];
//            }
//            Send[3, 0] = Convert.ToByte('9');
//            Send[3, 1] = Convert.ToByte(IpSend);
//            Send[3, 2] = 0x0D; // send CR afterwards

//            return Send;
//        }
//    }
//}
