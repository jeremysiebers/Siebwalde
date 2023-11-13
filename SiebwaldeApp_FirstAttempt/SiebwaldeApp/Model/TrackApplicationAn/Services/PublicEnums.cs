namespace SiebwaldeApp
{
    public struct ReceivedMessage
    {
        public ushort TaskId;
        public ushort Taskcommand;
        public ushort Taskstate;
        public ushort Taskmessage;

        public ReceivedMessage(ushort taskid, ushort taskcommand, ushort taskstate, ushort taskmessage)
        {
            TaskId = taskid;
            Taskcommand = taskcommand;
            Taskstate = taskstate;
            Taskmessage = taskmessage;
        }

        public static bool operator ==(ReceivedMessage c1, ReceivedMessage c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(ReceivedMessage c1, ReceivedMessage c2)
        {
            return !c1.Equals(c2);
        }
    }

    public struct SendMessage
    {        
        public byte Command;
        public byte[] Data;

        public SendMessage(byte command, byte[] data)
        {
            Command = command;
            Data = data;
        }

        public static bool operator ==(SendMessage c1, SendMessage c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(SendMessage c1, SendMessage c2)
        {
            return !c1.Equals(c2);
        }
    }

    public static class Enums
    {
        public const byte HEADER = 0xAA;
        public const byte FOOTER = 0x55;

        public const byte SLAVEINFO = 0xFF;

        //public const string HOMEPATH = @"c:\localdata\Siebwalde\";
        //public const string LOGGING = "Logging\\";
        public const string TRACKTARGET = "TRACKCONTROLLER";
        public const string SLAVEHEXFILE = "TrackAmplifier4.X.production.hex";
        public const int SPACELENGTH = 40;

        public const uint Busy = 0;
        public const uint Finished = 1;
        public const uint Next = 2;
        public const uint Standby = 2;
        public const uint Error = 11;
    }
}

