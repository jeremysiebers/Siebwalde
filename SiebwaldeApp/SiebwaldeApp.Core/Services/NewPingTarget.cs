using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace SiebwaldeApp.Core
{
    public class NewPingTarget
    {
        private static readonly byte[] Buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        public string TargetFound(string target)
        {
            try
            {
                // 1) Resolve to an IP if needed
                if (!TryResolve(target, out var address))
                    return "targetnotfound";

                // 2) Send ping
                using var ping = new Ping();
                var options = new PingOptions(dontFragment: true, ttl: 128);
                const int timeoutMs = 1000;

                var reply = ping.Send(address, timeoutMs, Buffer, options);

                return reply.Status == IPStatus.Success ? "targetfound" : "targetnotfound";
            }
            catch (PingException) { return "targetnotfound"; }
            catch (SocketException) { return "targetnotfound"; }
            catch { return "targetnotfound"; }
        }

        private static bool TryResolve(string target, out IPAddress address)
        {
            address = IPAddress.None;

            // Raw IP?
            if (IPAddress.TryParse(target, out var ip))
            {
                address = PreferIPv4(ip);
                return true;
            }

            // Try DNS
            try
            {
                var addrs = Dns.GetHostAddresses(target);
                if (addrs?.Length > 0)
                {
                    address = PreferIPv4(addrs.First());
                    return true;
                }

                // Try mDNS-style if no dot (e.g., "FIDDLEYARD" -> "FIDDLEYARD.local")
                if (!target.Contains('.'))
                {
                    addrs = Dns.GetHostAddresses(target + ".local");
                    if (addrs?.Length > 0)
                    {
                        address = PreferIPv4(addrs.First());
                        return true;
                    }
                }
            }
            catch { /* ignore and return false below */ }

            return false;
        }

        private static IPAddress PreferIPv4(IPAddress ip)
            => ip.AddressFamily == AddressFamily.InterNetwork ? ip
               : ip.MapToIPv4(); // best effort if IPv6 literal came in
    }
}


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Net;
//using System.Net.NetworkInformation;

//namespace SiebwaldeApp.Core
//{
//    public class NewPingTarget
//    {
//        public string TargetFound(string target)
//        {
//            try
//            {
//                Ping pingSender = new Ping();
//                PingOptions options = new PingOptions();

//                // Use the default Ttl value which is 128, 
//                // but change the fragmentation behavior.
//                options.DontFragment = true;

//                // Create a buffer of 32 bytes of data to be transmitted. 
//                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
//                byte[] buffer = Encoding.ASCII.GetBytes(data);
//                int timeout = 120;
//                PingReply reply = pingSender.Send(target, timeout, buffer, options);
//                if (reply.Status == IPStatus.Success)
//                {
//                    return "targetfound";
//                }
//                else { return "targetnotfound"; }
//            }
//            catch(Exception e)
//            {
//                string CaughtException = e.ToString();
//                return CaughtException.Substring(0, CaughtException.IndexOf(Environment.NewLine));
//            }
//        }
//    }
//}
