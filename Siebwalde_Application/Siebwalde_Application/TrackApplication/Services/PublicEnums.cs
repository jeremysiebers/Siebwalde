

namespace Siebwalde_Application.TrackApplication.Services
{
    public class PublicEnums
    {
        public const byte SLAVEINFO = 0xFF;
        public const byte HEADER = 0xAA;
        public const byte FOOTER = 0x55;

        public const string TRACKTARGET = "TRACKCONTROL";

        public byte SlaveInfo()
        {
            return SLAVEINFO;
        }
        public byte Header()
        {
            return HEADER;
        }
        public byte Footer()
        {
            return FOOTER;
        }
        
        public string TrackTarget()
        {
            return TRACKTARGET;
        }
    }
}
