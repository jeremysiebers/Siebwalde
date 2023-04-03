using System.Net.Sockets;

namespace SiebwaldeApp
{
    public class Sender
    {
        private UdpClient sendingUdpClient = new UdpClient(); // PC always transmits on PORT 28671 to ethernet targets        
        private string _target = "LocalHost";

        public Sender(string target)
        {
            _target = target;            
        }

        public void SendUdp(byte[] send)
        {
            sendingUdpClient.Send(send, send.Length);
        }

        public void ConnectUdp(int port)
        {
            sendingUdpClient.Connect(_target, port);// 28671);
        }

        public void ConnectUdpLocalHost(int port)
        {
            sendingUdpClient.Connect("LocalHost", port);// 28671);
        }

        public void CloseUdp()
        {
            sendingUdpClient.Close();
        }
    }
}
