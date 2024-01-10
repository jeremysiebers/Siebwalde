using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;

namespace SiebwaldeApp.Core
{
    public class NewPingTarget
    {
        public string TargetFound(string target)
        {
            try
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128, 
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                PingReply reply = pingSender.Send(target, timeout, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    return "targetfound";
                }
                else { return "targetnotfound"; }
            }
            catch(Exception e)
            {
                string CaughtException = e.ToString();
                return CaughtException.Substring(0, CaughtException.IndexOf(Environment.NewLine));
            }
        }
    }
}
