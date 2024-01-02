using System.Collections;
using System.Collections.Generic;

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

        public const byte CONTROLLERDATA = 60;
        public const byte CONTROLLERALIVE = 0xFF;

        //public const string HOMEPATH = @"c:\localdata\Siebwalde\";
        //public const string LOGGING = "Logging\\";
        public const string TRACKTARGET = "TRACKCONTROLLER"; //"192.168.1.50"; //"TRACKCONTROLLER";

        private static readonly Dictionary<int, string> NumberTextMap = new Dictionary<int, string>
        {
            { 1,  "MAIN_LOOP" },
            { 10, "MAIN_STATION_TOP" },
            { 20, "MAIN_STATION_BOT" },
            { 30, "WALDSEE" },
            { 40, "SIEBWALDE" },
            { 50, "WALDBERG" },
            { 60, "DATA" },

            { 1,  "MAIN_LOOP" },
            { 10, "MAIN_STATION_TOP" },
            { 20, "MAIN_STATION_BOT" },
            { 30, "WALDSEE" },
            { 40, "SIEBWALDE" },
            { 50, "WALDBERG" },
            { 60, "DATA" },
        };

        public static string TranslateNumber(int number)
        {
            if (NumberTextMap.ContainsKey(number))
            {
                return NumberTextMap[number];
            }
            else
            {
                return $"No translation found for the number: {number}";
            }
        }


    }


}

