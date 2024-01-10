using System.Net.Sockets;

namespace SiebwaldeApp.Core
{
    public class NewSender
    {
        private UdpClient sendingUdpClient = new UdpClient(); // PC always transmits on PORT 28671 to ethernet targets        
        private string _target = "LocalHost";

        public NewSender(string target)
        {
            _target = target;            
        }

        public void SendUdp(byte[] send)
        {
            sendingUdpClient.Send(send, send.Length);
        }

        public void ConnectUdp(int port)
        {
            sendingUdpClient.Connect(_target, port);
        }

        public void ConnectUdpLocalHost(int port)
        {
            sendingUdpClient.Connect("LocalHost", port);
        }

        public void CloseUdp()
        {
            sendingUdpClient.Close();
        }
    }
}
